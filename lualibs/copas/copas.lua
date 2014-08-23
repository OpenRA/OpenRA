-------------------------------------------------------------------------------
-- Copas - Coroutine Oriented Portable Asynchronous Services
--
-- A dispatcher based on coroutines that can be used by TCP/IP servers.
-- Uses LuaSocket as the interface with the TCP/IP stack.
--
-- Authors: Andre Carregal, Javier Guerra, and Fabio Mascarenhas
-- Contributors: Diego Nehab, Mike Pall, David Burgess, Leonardo Godinho,
--               Thomas Harning Jr., and Gary NG
--
-- Copyright 2005 - Kepler Project (www.keplerproject.org)
--
-- $Id: copas.lua,v 1.37 2009/04/07 22:09:52 carregal Exp $
-------------------------------------------------------------------------------

if package.loaded["socket.http"] then
  error("you must require copas before require'ing socket.http")
end

local socket = require "socket"
local gettime = socket.gettime
local coxpcall = require "coxpcall"

local WATCH_DOG_TIMEOUT = 120
local UDP_DATAGRAM_MAX = 8192

-- Redefines LuaSocket functions with coroutine safe versions
-- (this allows the use of socket.http from within copas)
local function statusHandler(status, ...)
  if status then return ... end
  local err = (...)
  if type(err) == "table" then
    return nil, err[1]
  else
    error(err)
  end
end

function socket.protect(func)
  return function (...)
           return statusHandler(coxpcall.pcall(func, ...))
         end
end

function socket.newtry(finalizer)
  return function (...)
           local status = (...)
           if not status then
             coxpcall.pcall(finalizer, select(2, ...))
             error({ (select(2, ...)) }, 0)
           end
           return ...
         end
end

-- end of LuaSocket redefinitions

local copas = {}

-- Meta information is public even if beginning with an "_"
copas._COPYRIGHT   = "Copyright (C) 2005-2010 Kepler Project"
copas._DESCRIPTION = "Coroutine Oriented Portable Asynchronous Services"
copas._VERSION     = "Copas 1.2.1"

-- Close the socket associated with the current connection after the handler finishes
copas.autoclose = true

-------------------------------------------------------------------------------
-- Simple set implementation based on LuaSocket's tinyirc.lua example
-- adds a FIFO queue for each value in the set
-------------------------------------------------------------------------------
local function newset()
  local reverse = {}
  local set = {}
  local q = {}
  setmetatable(set, { __index = {
                        insert = function(set, value)
                                   if not reverse[value] then
                                     set[#set + 1] = value
                                     reverse[value] = #set
                                   end
                                 end,

                        remove = function(set, value)
                                   local index = reverse[value]
                                   if index then
                                     reverse[value] = nil
                                     local top = set[#set]
                                     set[#set] = nil
                                     if top ~= value then
                                       reverse[top] = index
                                       set[index] = top
                                     end
                                   end
                                 end,

                        push = function (set, key, itm)
                                 local qKey = q[key]
                                 if qKey == nil then
                                   q[key] = {itm}
                                 else
                                   qKey[#qKey + 1] = itm
                                 end
                               end,

                        pop = function (set, key)
                                local t = q[key]
                                if t ~= nil then
                                  local ret = table.remove (t, 1)
                                  if t[1] == nil then
                                    q[key] = nil
                                  end
                                  return ret
                                end
                              end
                    }})
  return set
end

local fnil = function()end
local _sleeping = {
    times = {},  -- list with wake-up times
    cos = {},    -- list with coroutines, index matches the 'times' list
    lethargy = {}, -- list of coroutines sleeping without a wakeup time

    insert = fnil,
    remove = fnil,
    push = function(self, sleeptime, co)
        if not co then return end
        if sleeptime<0 then
            --sleep until explicit wakeup through copas.wakeup
            self.lethargy[co] = true
            return
        else
            sleeptime = gettime() + sleeptime
        end
        local t, c = self.times, self.cos
        local i, cou = 1, #t
        --TODO: do a binary search
        while i<=cou and t[i]<=sleeptime do i=i+1 end
        table.insert(t, i, sleeptime)
        table.insert(c, i, co)
    end,
    getnext = function(self)  -- returns delay until next sleep expires, or nil if there is none
        local t = self.times
        local delay = t[1] and t[1] - gettime() or nil

        return delay and math.max(delay, 0) or nil
    end,
    -- find the thread that should wake up to the time
    pop = function(self, time)
        local t, c = self.times, self.cos
        if #t==0 or time<t[1] then return end
        local co = c[1]
        table.remove(t, 1)
        table.remove(c, 1)
        return co
    end,
    wakeup = function(self, co)
        local let = self.lethargy
        if let[co] then
            self:push(0, co)
            let[co] = nil
        else
            let = self.cos
            for i=1,#let do
                if let[i]==co then
                    table.remove(let, i)
                    local tm = self.times[i]
                    table.remove(self.times, i)
                    self:push(0, co)
                    return
                end
            end
        end
    end
} --_sleeping

local _servers = newset() -- servers being handled
local _reading_log = {}
local _writing_log = {}

local _reading = newset() -- sockets currently being read
local _writing = newset() -- sockets currently being written

-------------------------------------------------------------------------------
-- Coroutine based socket I/O functions.
-------------------------------------------------------------------------------
-- reads a pattern from a client and yields to the reading set on timeouts
-- UDP: a UDP socket expects a second argument to be a number, so it MUST
-- be provided as the 'pattern' below defaults to a string. Will throw a
-- 'bad argument' error if omitted.
function copas.receive(client, pattern, part)
  local s, err
  pattern = pattern or "*l"
  repeat
    s, err, part = client:receive(pattern, part)
    if s or err ~= "timeout" then
      _reading_log[client] = nil
      return s, err, part
    end
    _reading_log[client] = gettime()
    coroutine.yield(client, _reading)
  until false
end

-- receives data from a client over UDP. Not available for TCP.
-- (this is a copy of receive() method, adapted for receivefrom() use)
function copas.receivefrom(client, size)
  local s, err, port
  size = size or UDP_DATAGRAM_MAX
  repeat
    s, err, port = client:receivefrom(size) -- upon success err holds ip address
    if s or err ~= "timeout" then
      _reading_log[client] = nil
      return s, err, port
    end
    _reading_log[client] = gettime()
    coroutine.yield(client, _reading)
  until false
end

-- same as above but with special treatment when reading chunks,
-- unblocks on any data received.
function copas.receivePartial(client, pattern, part)
  local s, err
  pattern = pattern or "*l"
  repeat
    s, err, part = client:receive(pattern, part)
    if s or ( (type(pattern)=="number") and part~="" and part ~=nil ) or
      err ~= "timeout" then
      _reading_log[client] = nil
      return s, err, part
    end
    _reading_log[client] = gettime()
    coroutine.yield(client, _reading)
  until false
end

-- sends data to a client. The operation is buffered and
-- yields to the writing set on timeouts
-- Note: from and to parameters will be ignored by/for UDP sockets
function copas.send(client, data, from, to)
  local s, err,sent
  from = from or 1
  local lastIndex = from - 1

  repeat
    s, err, lastIndex = client:send(data, lastIndex + 1, to)
    -- adds extra corrotine swap
    -- garantees that high throuput dont take other threads to starvation
    if (math.random(100) > 90) then
      _writing_log[client] = gettime()
      coroutine.yield(client, _writing)
    end
    if s or err ~= "timeout" then
      _writing_log[client] = nil
      return s, err,lastIndex
    end
    _writing_log[client] = gettime()
    coroutine.yield(client, _writing)
  until false
end

-- sends data to a client over UDP. Not available for TCP.
-- (this is a copy of send() method, adapted for sendto() use)
function copas.sendto(client, data, ip, port)
  local s, err,sent

  repeat
    s, err = client:sendto(data, ip, port)
    -- adds extra corrotine swap
    -- garantees that high throuput dont take other threads to starvation
    if (math.random(100) > 90) then
      _writing_log[client] = gettime()
      coroutine.yield(client, _writing)
    end
    if s or err ~= "timeout" then
      _writing_log[client] = nil
      return s, err
    end
    _writing_log[client] = gettime()
    coroutine.yield(client, _writing)
  until false
end

-- waits until connection is completed
function copas.connect(skt, host, port)
  skt:settimeout(0)
  local ret, err
  repeat
    ret, err = skt:connect (host, port)
    if ret or err ~= "timeout" then
      _writing_log[skt] = nil
      return ret, err
    end
    _writing_log[skt] = gettime()
    coroutine.yield(skt, _writing)
  until false
  return ret, err
end

-- flushes a client write buffer (deprecated)
function copas.flush(client)
end

-- wraps a TCP socket to use Copas methods (send, receive, flush and settimeout)
local _skt_mt = {__index = {
                   send = function (self, data, from, to)
                            return copas.send (self.socket, data, from, to)
                          end,

                   receive = function (self, pattern, prefix)
                               if (self.timeout==0) then
                                 return copas.receivePartial(self.socket, pattern, prefix)
                               end
                               return copas.receive(self.socket, pattern, prefix)
                             end,

                   flush = function (self)
                             return copas.flush(self.socket)
                           end,

                   settimeout = function (self,time)
                                  self.timeout=time
                                  return true
                                end,

                   skip = function(self, ...) return self.socket:skip(...) end,

                   close = function(self, ...) return self.socket:close(...) end,
               }}

-- wraps a UDP socket, copy of TCP one adapted for UDP.
-- Mainly adds sendto() and receivefrom()
local _skt_mt_udp = {__index = {
                   send = function (self, data)
                            return copas.send (self.socket, data)
                          end,

                   sendto = function (self, data, ip, port)
                            return copas.sendto (self.socket, data, ip, port)
                          end,

                   receive = function (self, size)
                               return copas.receive (self.socket, (size or UDP_DATAGRAM_MAX))
                             end,

                   receivefrom = function (self, size)
                               return copas.receivefrom (self.socket, (size or UDP_DATAGRAM_MAX))
                             end,

                   flush = function (self)
                             return copas.flush (self.socket)
                           end,

                   settimeout = function (self,time)
                                  self.timeout=time
                                  return true
                                end,
               }}

function copas.wrap (skt)
  if string.sub(tostring(skt),1,3) == "udp" then
    return  setmetatable ({socket = skt}, _skt_mt_udp)
  else
    return  setmetatable ({socket = skt}, _skt_mt)
  end
end

--------------------------------------------------
-- Error handling
--------------------------------------------------

local _errhandlers = {}   -- error handler per coroutine

function copas.setErrorHandler (err)
  local co = coroutine.running()
  if co then
    _errhandlers [co] = err
  end
end

local function _deferror (msg, co, skt)
  print (msg, co, skt)
end

-------------------------------------------------------------------------------
-- Thread handling
-------------------------------------------------------------------------------

local function _doTick (co, skt, ...)
  if not co then return end

  local ok, res, new_q = coroutine.resume(co, skt, ...)

  if ok and res and new_q then
    new_q:insert (res)
    new_q:push (res, co)
  else
    if not ok then coxpcall.pcall (_errhandlers [co] or _deferror, res, co, skt) end
    if skt and copas.autoclose then skt:close() end
    _errhandlers [co] = nil
  end
end

-- accepts a connection on socket input
local function _accept(input, handler)
  local client = input:accept()
  if client then
    client:settimeout(0)
    local co = coroutine.create(handler)
    _doTick (co, client)
    --_reading:insert(client)
  end
  return client
end

-- handle threads on a queue
local function _tickRead (skt)
  _doTick (_reading:pop (skt), skt)
end

local function _tickWrite (skt)
  _doTick (_writing:pop (skt), skt)
end

-------------------------------------------------------------------------------
-- Adds a server/handler pair to Copas dispatcher
-------------------------------------------------------------------------------
local function addTCPserver(server, handler, timeout)
  server:settimeout(timeout or 0.1)
  _servers[server] = handler
  _reading:insert(server)
end

local function addUDPserver(server, handler, timeout)
    server:settimeout(timeout or 0)
    local co = coroutine.create(handler)
    _reading:insert(server)
    _doTick (co, server)
end

function copas.addserver(server, handler, timeout)
    if string.sub(tostring(server),1,3) == "udp" then
        addUDPserver(server, handler, timeout)
    else
        addTCPserver(server, handler, timeout)
    end
end

function copas.removeserver(server)
  _servers[server] = nil
  _reading:remove(server)
  return server:close()
end

-------------------------------------------------------------------------------
-- Adds an new courotine thread to Copas dispatcher
-------------------------------------------------------------------------------
function copas.addthread(thread, ...)
  if type(thread) ~= "thread" then
    thread = coroutine.create(thread)
  end
  _doTick (thread, nil, ...)
  return thread
end

-------------------------------------------------------------------------------
-- tasks registering
-------------------------------------------------------------------------------

local _tasks = {}

local function addtaskRead (tsk)
  -- lets tasks call the default _tick()
  tsk.def_tick = _tickRead

  _tasks [tsk] = true
end

local function addtaskWrite (tsk)
  -- lets tasks call the default _tick()
  tsk.def_tick = _tickWrite

  _tasks [tsk] = true
end

local function tasks ()
  return next, _tasks
end

-------------------------------------------------------------------------------
-- main tasks: manage readable and writable socket sets
-------------------------------------------------------------------------------
-- a task to check ready to read events
local _readable_t = {
  events = function(self)
             local i = 0
             return function ()
                      i = i + 1
                      return self._evs [i]
                    end
           end,

  tick = function (self, input)
           local handler = _servers[input]
           if handler then
             input = _accept(input, handler)
           else
             _reading:remove (input)
             self.def_tick (input)
           end
         end
}

addtaskRead (_readable_t)


-- a task to check ready to write events
local _writable_t = {
  events = function (self)
             local i = 0
             return function ()
                      i = i + 1
                      return self._evs [i]
                    end
           end,

  tick = function (self, output)
           _writing:remove (output)
           self.def_tick (output)
         end
}

addtaskWrite (_writable_t)
--
--sleeping threads task
local _sleeping_t = {
    tick = function (self, time, ...)
       _doTick(_sleeping:pop(time), ...)
    end
}

-- yields the current coroutine and wakes it after 'sleeptime' seconds.
-- If sleeptime<0 then it sleeps until explicitly woken up using 'wakeup'
function copas.sleep(sleeptime)
    coroutine.yield((sleeptime or 0), _sleeping)
end

-- Wakes up a sleeping coroutine 'co'.
function copas.wakeup(co)
    _sleeping:wakeup(co)
end

local last_cleansing = 0

-------------------------------------------------------------------------------
-- Checks for reads and writes on sockets
-------------------------------------------------------------------------------
local function _select (timeout)
  local err
  local now = gettime()
  local duration = function(t2, t1) return t2-t1 end

  _readable_t._evs, _writable_t._evs, err = socket.select(_reading, _writing, timeout)
  local r_evs, w_evs = _readable_t._evs, _writable_t._evs

  if duration(now, last_cleansing) > WATCH_DOG_TIMEOUT then
    last_cleansing = now
    for k,v in pairs(_reading_log) do
      if not r_evs[k] and duration(now, v) > WATCH_DOG_TIMEOUT then
        _reading_log[k] = nil
        r_evs[#r_evs + 1] = k
        r_evs[k] = #r_evs
      end
    end

    for k,v in pairs(_writing_log) do
      if not w_evs[k] and duration(now, v) > WATCH_DOG_TIMEOUT then
        _writing_log[k] = nil
        w_evs[#w_evs + 1] = k
        w_evs[k] = #w_evs
      end
    end
  end

  if err == "timeout" and #r_evs + #w_evs > 0 then
    return nil
  else
    return err
  end
end


-------------------------------------------------------------------------------
-- Dispatcher loop step.
-- Listen to client requests and handles them
-- Returns false if no data was handled (timeout), or true if there was data
-- handled (or nil + error message)
-------------------------------------------------------------------------------
function copas.step(timeout)
  _sleeping_t:tick(gettime())

  -- Need to wake up the select call it time for the next sleeping event
  local nextwait = _sleeping:getnext()
  if nextwait then
    timeout = timeout and math.min(nextwait, timeout) or nextwait
  end

  local err = _select (timeout)
  if err == "timeout" then return false end

  if err then
    error(err)
  end

  for tsk in tasks() do
    for ev in tsk:events() do
      tsk:tick (ev)
    end
  end
  return true
end

-------------------------------------------------------------------------------
-- Dispatcher endless loop.
-- Listen to client requests and handles them forever
-------------------------------------------------------------------------------
function copas.loop(timeout)
  while true do
    copas.step(timeout)
  end
end

return copas
