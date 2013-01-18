--
-- MobDebug 0.515
-- Copyright 2011-12 Paul Kulchenko
-- Based on RemDebug 1.0 Copyright Kepler Project 2005
--

local mobdebug = {
  _NAME = "mobdebug",
  _VERSION = 0.515,
  _COPYRIGHT = "Paul Kulchenko",
  _DESCRIPTION = "Mobile Remote Debugger for the Lua programming language",
  port = os and os.getenv and os.getenv("MOBDEBUG_PORT") or 8172,
  yieldtimeout = 0.02,
}

local coroutine = coroutine
local error = error
local getfenv = getfenv
local setfenv = setfenv
local loadstring = loadstring or load -- "load" replaced "loadstring" in Lua 5.2
local io = io
local os = os
local pairs = pairs
local require = require
local setmetatable = setmetatable
local string = string
local tonumber = tonumber

-- if strict.lua is used, then need to avoid referencing some global
-- variables, as they can be undefined;
-- use rawget to to avoid complaints from strict.lua at run-time.
-- it's safe to do the initialization here as all these variables
-- should get defined values (of any) before the debugging starts.
-- there is also global 'wx' variable, which is checked as part of
-- the debug loop as 'wx' can be loaded at any time during debugging.
local genv = _G or _ENV
local jit = rawget(genv, "jit")
local mosync = rawget(genv, "mosync")
local MOAICoroutine = rawget(genv, "MOAICoroutine")

if not setfenv then -- Lua 5.2
  -- based on http://lua-users.org/lists/lua-l/2010-06/msg00314.html
  -- this assumes f is a function
  local function findenv(f)
    local level = 1
    repeat
      local name, value = debug.getupvalue(f, level)
      if name == '_ENV' then return level, value end
      level = level + 1
    until name == nil
    return nil end
  getfenv = function (f) return(select(2, findenv(f)) or _G) end
  setfenv = function (f, t)
    local level = findenv(f)
    if level then debug.setupvalue(f, level, t) end
    return f end
end

-- check for OS and convert file names to lower case on windows
-- (its file system is case insensitive, but case preserving), as setting a
-- breakpoint on x:\Foo.lua will not work if the file was loaded as X:\foo.lua.
local iswindows = os and os.getenv and (os.getenv('WINDIR')
  or (os.getenv('OS') or ''):match('[Ww]indows'))
  or pcall(require, "winapi")

-- this is a socket class that implements maConnect interface
local function socketMobileLua() 
  local self = {}
  self.select = function(readfrom) -- writeto and timeout parameters are ignored
    local canread = {}
    for _,s in ipairs(readfrom) do
      if s:receive(0) then canread[s] = true end
    end
    return canread
  end
  self.connect = coroutine.wrap(function(host, port)
    while true do
      local connection = mosync.maConnect("socket://" .. host .. ":" .. port)
  
      if connection > 0 then
        local event = mosync.SysEventCreate()
        while true do
          mosync.maWait(0)
          mosync.maGetEvent(event)
          local eventType = mosync.SysEventGetType(event)
          if (mosync.EVENT_TYPE_CONN == eventType and
            mosync.SysEventGetConnHandle(event) == connection and
            mosync.SysEventGetConnOpType(event) == mosync.CONNOP_CONNECT) then
              -- result > 0 ? success : error
              if not (mosync.SysEventGetConnResult(event) > 0) then connection = nil end
              break
          elseif mosync.EventMonitor and mosync.EventMonitor.HandleEvent then
            mosync.EventMonitor:HandleEvent(event)
          end
        end
        mosync.SysFree(event)
      end
  
      host, port = coroutine.yield(connection and (function ()
        local self = {}
        local outBuffer = mosync.SysAlloc(1000)
        local inBuffer = mosync.SysAlloc(1000)
        local event = mosync.SysEventCreate()
        local recvBuffer = ""
        function stringToBuffer(s, buffer)
          local i = 0
          for c in s:gmatch(".") do
            i = i + 1
            local b = s:byte(i)
            mosync.SysBufferSetByte(buffer, i - 1, b)
          end
          return i
        end
        function bufferToString(buffer, len)
          local s = ""
          for i = 0, len - 1 do
            local c = mosync.SysBufferGetByte(buffer, i)
            s = s .. string.char(c)
          end
          return s
        end
        self.send = coroutine.wrap(function(self, msg)
          while true do
            local numberOfBytes = stringToBuffer(msg, outBuffer)
            mosync.maConnWrite(connection, outBuffer, numberOfBytes)
            while true do
              mosync.maWait(0)
              mosync.maGetEvent(event)
              local eventType = mosync.SysEventGetType(event)
              if (mosync.EVENT_TYPE_CONN == eventType and
                  mosync.SysEventGetConnHandle(event) == connection and
                  mosync.SysEventGetConnOpType(event) == mosync.CONNOP_WRITE) then
                break
              elseif mosync.EventMonitor and mosync.EventMonitor.HandleEvent then
                mosync.EventMonitor:HandleEvent(event)
              end
            end
            self, msg = coroutine.yield()
          end
        end)
        self.receive = coroutine.wrap(function(self, len)
          while true do
            local line = recvBuffer
            while (len and string.len(line) < len)     -- either we need len bytes
               or (not len and not line:find("\n")) -- or one line (if no len specified)
               or (len == 0) do -- only check for new data (select-like)
              mosync.maConnRead(connection, inBuffer, 1000)
              while true do
                if len ~= 0 then mosync.maWait(0) end
                mosync.maGetEvent(event)
                local eventType = mosync.SysEventGetType(event)
                if (mosync.EVENT_TYPE_CONN == eventType and
                    mosync.SysEventGetConnHandle(event) == connection and
                    mosync.SysEventGetConnOpType(event) == mosync.CONNOP_READ) then
                  local result = mosync.SysEventGetConnResult(event)
                  if result > 0 then line = line .. bufferToString(inBuffer, result) end
                  if len == 0 then self, len = coroutine.yield("") end
                  break -- got the event we wanted; now check if we have all we need
                elseif len == 0 then
                  self, len = coroutine.yield(nil)
                elseif mosync.EventMonitor and mosync.EventMonitor.HandleEvent then
                  mosync.EventMonitor:HandleEvent(event)
                end
              end  
            end
    
            if not len then
              len = string.find(line, "\n") or string.len(line)
            end
    
            recvBuffer = string.sub(line, len+1)
            line = string.sub(line, 1, len)
    
            self, len = coroutine.yield(line)
          end
        end)
        self.close = coroutine.wrap(function(self) 
          while true do
            mosync.SysFree(inBuffer)
            mosync.SysFree(outBuffer)
            mosync.SysFree(event)
            mosync.maConnClose(connection)
            coroutine.yield(self)
          end
        end)
        return self
      end)())
    end
  end)

  return self
end

-- overwrite RunEventLoop in MobileLua as it conflicts with the event
-- loop that needs to run to process debugger events (socket read/write).
-- event loop functionality is implemented by calling HandleEvent
-- while waiting for debugger events.
if mosync and mosync.EventMonitor then
  mosync.EventMonitor.RunEventLoop = function(self) end
end

-- turn jit off based on Mike Pall's comment in this discussion:
-- http://www.freelists.org/post/luajit/Debug-hooks-and-JIT,2
-- "You need to turn it off at the start if you plan to receive
-- reliable hook calls at any later point in time."
if jit and jit.off then jit.off() end

local socket = mosync and socketMobileLua() or (require "socket")

local debug = require "debug"
local coro_debugger
local coro_debugee
local coroutines = {}; setmetatable(coroutines, {__mode = "k"}) -- "weak" keys
local events = { BREAK = 1, WATCH = 2, RESTART = 3, STACK = 4 }
local breakpoints = {}
local watches = {}
local lastsource
local lastfile
local watchescnt = 0
local abort -- default value is nil; this is used in start/loop distinction
local seen_hook = false
local skip
local skipcount = 0
local step_into = false
local step_over = false
local step_level = 0
local stack_level = 0
local server
local rset
local outputs = {}
local iobase = {print = print}
local basedir = ""
local deferror = "execution aborted at default debugee"
local debugee = function () 
  local a = 1
  for _ = 1, 10 do a = a + 1 end
  error(deferror)
end
local function q(s) return s:gsub('([%(%)%.%%%+%-%*%?%[%^%$%]])','%%%1') end

local serpent = (function() ---- include Serpent module for serialization
local n, v = "serpent", 0.22 -- (C) 2012 Paul Kulchenko; MIT License
local c, d = "Paul Kulchenko", "Serializer and pretty printer of Lua data types"
local snum = {[tostring(1/0)]='1/0 --[[math.huge]]',[tostring(-1/0)]='-1/0 --[[-math.huge]]',[tostring(0/0)]='0/0'}
local badtype = {thread = true, userdata = true}
local keyword, globals, G = {}, {}, (_G or _ENV)
for _,k in ipairs({'and', 'break', 'do', 'else', 'elseif', 'end', 'false',
  'for', 'function', 'goto', 'if', 'in', 'local', 'nil', 'not', 'or', 'repeat',
  'return', 'then', 'true', 'until', 'while'}) do keyword[k] = true end
for k,v in pairs(G) do globals[v] = k end -- build func to name mapping
for _,g in ipairs({'coroutine', 'debug', 'io', 'math', 'string', 'table', 'os'}) do
  for k,v in pairs(G[g]) do globals[v] = g..'.'..k end end

local function s(t, opts)
  local name, indent, fatal = opts.name, opts.indent, opts.fatal
  local sparse, custom, huge = opts.sparse, opts.custom, not opts.nohuge
  local space, maxl = (opts.compact and '' or ' '), (opts.maxlevel or math.huge)
  local comm = opts.comment and (tonumber(opts.comment) or math.huge)
  local seen, sref, syms, symn = {}, {}, {}, 0
  local function gensym(val) return (tostring(val):gsub("[^%w]",""):gsub("(%d%w+)",
    function(s) if not syms[s] then symn = symn+1; syms[s] = symn end return syms[s] end)) end
  local function safestr(s) return type(s) == "number" and (huge and snum[tostring(s)] or s)
    or type(s) ~= "string" and tostring(s) -- escape NEWLINE/010 and EOF/026
    or ("%q"):format(s):gsub("\010","n"):gsub("\026","\\026") end
  local function comment(s,l) return comm and (l or 0) < comm and ' --[['..tostring(s)..']]' or '' end
  local function globerr(s,l) return globals[s] and globals[s]..comment(s,l) or not fatal
    and safestr(select(2, pcall(tostring, s))) or error("Can't serialize "..tostring(s)) end
  local function safename(path, name) -- generates foo.bar, foo[3], or foo['b a r']
    local n = name == nil and '' or name
    local plain = type(n) == "string" and n:match("^[%l%u_][%w_]*$") and not keyword[n]
    local safe = plain and n or '['..safestr(n)..']'
    return (path or '')..(plain and path and '.' or '')..safe, safe end
  local alphanumsort = type(opts.sortkeys) == 'function' and opts.sortkeys or function(o, n)
    local maxn, to = tonumber(n) or 12, {number = 'a', string = 'b'}
    local function padnum(d) return ("%0"..maxn.."d"):format(d) end
    table.sort(o, function(a,b)
      return (o[a] and 0 or to[type(a)] or 'z')..(tostring(a):gsub("%d+",padnum))
           < (o[b] and 0 or to[type(b)] or 'z')..(tostring(b):gsub("%d+",padnum)) end) end
  local function val2str(t, name, indent, insref, path, plainindex, level)
    local ttype, level = type(t), (level or 0)
    local spath, sname = safename(path, name)
    local tag = plainindex and
      ((type(name) == "number") and '' or name..space..'='..space) or
      (name ~= nil and sname..space..'='..space or '')
    if seen[t] then -- if already seen and in sref processing,
      if insref then return tag..seen[t] end -- then emit right away
      table.insert(sref, spath..space..'='..space..seen[t])
      return tag..'nil'..comment('ref', level)
    elseif badtype[ttype] then
      seen[t] = spath
      return tag..globerr(t, level)
    elseif ttype == 'function' then
      seen[t] = insref or spath
      local ok, res = pcall(string.dump, t)
      local func = ok and ((opts.nocode and "function() --[[..skipped..]] end" or
        "loadstring("..safestr(res)..",'@serialized')")..comment(t, level))
      return tag..(func or globerr(t, level))
    elseif ttype == "table" then
      if level >= maxl then return tag..'{}'..comment('max', level) end
      seen[t] = insref or spath -- set path to use as reference
      if getmetatable(t) and getmetatable(t).__tostring
        then return tag..val2str(tostring(t),nil,indent,false,nil,nil,level+1)..comment("meta", level) end
      if next(t) == nil then return tag..'{}'..comment(t, level) end -- table empty
      local maxn, o, out = #t, {}, {}
      for key = 1, maxn do table.insert(o, key) end
      for key in pairs(t) do if not o[key] then table.insert(o, key) end end
      if opts.sortkeys then alphanumsort(o, opts.sortkeys) end
      for n, key in ipairs(o) do
        local value, ktype, plainindex = t[key], type(key), n <= maxn and not sparse
        if opts.valignore and opts.valignore[value] -- skip ignored values; do nothing
        or opts.keyallow and not opts.keyallow[key]
        or opts.valtypeignore and opts.valtypeignore[type(value)] -- skipping ignored value types
        or sparse and value == nil then -- skipping nils; do nothing
        elseif ktype == 'table' or ktype == 'function' or badtype[ktype] then
          if not seen[key] and not globals[key] then
            table.insert(sref, 'placeholder')
            sref[#sref] = 'local '..val2str(key,gensym(key),indent,gensym(key)) end
          table.insert(sref, 'placeholder')
          local path = seen[t]..'['..(seen[key] or globals[key] or gensym(key))..']'
          sref[#sref] = path..space..'='..space..(seen[value] or val2str(value,nil,indent,path))
        else
          table.insert(out,val2str(value,key,indent,insref,seen[t],plainindex,level+1))
        end
      end
      local prefix = string.rep(indent or '', level)
      local head = indent and '{\n'..prefix..indent or '{'
      local body = table.concat(out, ','..(indent and '\n'..prefix..indent or space))
      local tail = indent and "\n"..prefix..'}' or '}'
      return (custom and custom(tag,head,body,tail) or tag..head..body..tail)..comment(t, level)
    else return tag..safestr(t) end -- handle all other types
  end
  local sepr = indent and "\n" or ";"..space
  local body = val2str(t, name, indent) -- this call also populates sref
  local tail = #sref>0 and table.concat(sref, sepr)..sepr or ''
  return not name and body or "do local "..body..sepr..tail.."return "..name..sepr.."end"
end

local function merge(a, b) if b then for k,v in pairs(b) do a[k] = v end end; return a; end
return { _NAME = n, _COPYRIGHT = c, _DESCRIPTION = d, _VERSION = v, serialize = s,
  dump = function(a, opts) return s(a, merge({name = '_', compact = true, sparse = true}, opts)) end,
  line = function(a, opts) return s(a, merge({sortkeys = true, comment = true}, opts)) end,
  block = function(a, opts) return s(a, merge({indent = '  ', sortkeys = true, comment = true}, opts)) end }
end)() ---- end of Serpent module

local function removebasedir(path, basedir)
  if iswindows then
    -- check if the lowercased path matches the basedir
    -- if so, return substring of the original path (to not lowercase it)
    return path:lower():find('^'..q(basedir:lower()))
      and path:sub(#basedir+1) or path
  else
    return string.gsub(path, '^'..q(basedir), '')
  end
end

local function stack(start)
  local function vars(f)
    local func = debug.getinfo(f, "f").func
    local i = 1
    local locals = {}
    while true do
      local name, value = debug.getlocal(f, i)
      if not name then break end
      if string.sub(name, 1, 1) ~= '(' then locals[name] = {value, tostring(value)} end
      i = i + 1
    end
    i = 1
    local ups = {}
    while func and true do -- check for func as it may be nil for tail calls
      local name, value = debug.getupvalue(func, i)
      if not name then break end
      ups[name] = {value, tostring(value)}
      i = i + 1
    end
    return locals, ups
  end

  local stack = {}
  for i = (start or 0), 100 do
    local source = debug.getinfo(i, "Snl")
    if not source then break end

    -- remove basedir from source
    local src = source.source
    if src:find("@") == 1 then
      src = src:sub(2):gsub("\\", "/")
      if src:find("%./") == 1 then src = src:sub(3) end
    end

    table.insert(stack, {
      {source.name, removebasedir(src, basedir), source.linedefined,
       source.currentline, source.what, source.namewhat, source.short_src},
      vars(i+1)})
    if source.what == 'main' then break end
  end
  return stack
end

local function set_breakpoint(file, line)
  if file == '-' and lastfile then file = lastfile
  elseif iswindows then file = string.lower(file) end
  if not breakpoints[file] then breakpoints[file] = {} end
  breakpoints[file][line] = true  
end

local function remove_breakpoint(file, line)
  if file == '-' and lastfile then file = lastfile
  elseif iswindows then file = string.lower(file) end
  if breakpoints[file] then breakpoints[file][line] = nil end
end

-- this file name is already converted to lower case on windows.
local function has_breakpoint(file, line)
  return breakpoints[file] and breakpoints[file][line]
end

local function restore_vars(vars)
  if type(vars) ~= 'table' then return end

  -- locals need to be processed in the reverse order, starting from
  -- the inner block out, to make sure that the localized variables
  -- are correctly updated with only the closest variable with
  -- the same name being changed
  -- first loop find how many local variables there is, while
  -- the second loop processes them from i to 1
  local i = 1
  while true do
    local name = debug.getlocal(3, i)
    if not name then break end
    i = i + 1
  end
  i = i - 1
  local written_vars = {}
  while i > 0 do
    local name = debug.getlocal(3, i)
    if not written_vars[name] then
      if string.sub(name, 1, 1) ~= '(' then debug.setlocal(3, i, vars[name]) end
      written_vars[name] = true
    end
    i = i - 1
  end

  i = 1
  local func = debug.getinfo(3, "f").func
  while true do
    local name = debug.getupvalue(func, i)
    if not name then break end
    if not written_vars[name] then
      if string.sub(name, 1, 1) ~= '(' then debug.setupvalue(func, i, vars[name]) end
      written_vars[name] = true
    end
    i = i + 1
  end
end

local function capture_vars(level)
  local vars = {}
  local func = debug.getinfo(level or 3, "f").func
  local i = 1
  while true do
    local name, value = debug.getupvalue(func, i)
    if not name then break end
    if string.sub(name, 1, 1) ~= '(' then vars[name] = value end
    i = i + 1
  end
  i = 1
  while true do
    local name, value = debug.getlocal(level or 3, i)
    if not name then break end
    if string.sub(name, 1, 1) ~= '(' then vars[name] = value end
    i = i + 1
  end
  setmetatable(vars, { __index = getfenv(func), __newindex = getfenv(func) })
  return vars
end

local function stack_depth(start_depth)
  for i = start_depth, 0, -1 do
    if debug.getinfo(i, "l") then return i+1 end
  end
  return start_depth
end

local function is_safe(stack_level)
  -- the stack grows up: 0 is getinfo, 1 is is_safe, 2 is debug_hook, 3 is user function
  if stack_level == 3 then return true end
  for i = 3, stack_level do
    -- return if it is not safe to abort
    local info = debug.getinfo(i, "S")
    if not info then return true end
    if info.what == "C" then return false end
  end
  return true
end

local function in_debugger()
  local this = debug.getinfo(1, "S").source
  -- only need to check few frames as mobdebug frames should be close
  for i = 3, 7 do
    local info = debug.getinfo(i, "S")
    if not info then return false end
    if info.source == this then return true end
  end
  return false
end

local function debug_hook(event, line)
  -- (1) LuaJIT needs special treatment. Because debug_hook is set for
  -- *all* coroutines, and not just the one being debugged as in regular Lua
  -- (http://lua-users.org/lists/lua-l/2011-06/msg00513.html),
  -- need to avoid debugging mobdebug's own code as LuaJIT doesn't
  -- always correctly generate call/return hook events (there are more
  -- calls than returns, which breaks stack depth calculation and
  -- 'step' and 'step over' commands stop working; possibly because
  -- 'tail return' events are not generated by LuaJIT).
  -- the next line checks if the debugger is run under LuaJIT and if
  -- one of debugger methods is present in the stack, it simply returns.
  if jit then
    local coro = coroutine.running()
    if coro_debugee and coro ~= coro_debugee and not coroutines[coro]
      or not coro_debugee and (in_debugger() or coro and not coroutines[coro])
    then return end
  end

  -- (2) check if abort has been requested and it's safe to abort
  if abort and is_safe(stack_level) then error(abort) end

  -- (3) also check if this debug hook has not been visited for any reason.
  -- this check is needed to avoid stepping in too early
  -- (for example, when coroutine.resume() is executed inside start()).
  if not seen_hook and in_debugger() then return end

  if event == "call" then
    stack_level = stack_level + 1
  elseif event == "return" or event == "tail return" then
    stack_level = stack_level - 1
  elseif event == "line" then

    -- check if we need to skip some callbacks (to save time)
    if skip then
      skipcount = skipcount + 1
      if skipcount < skip or not is_safe(stack_level) then return end
      skipcount = 0
    end

    -- this is needed to check if the stack got shorter or longer.
    -- unfortunately counting call/return calls is not reliable.
    -- the discrepancy may happen when "pcall(load, '')" call is made
    -- or when "error()" is called in a function.
    -- in either case there are more "call" than "return" events reported.
    -- this validation is done for every "line" event, but should be "cheap"
    -- as it checks for the stack to get shorter (or longer by one call).
    -- start from one level higher just in case we need to grow the stack.
    -- this may happen after coroutine.resume call to a function that doesn't
    -- have any other instructions to execute. it triggers three returns:
    -- "return, tail return, return", which needs to be accounted for.
    stack_level = stack_depth(stack_level+1)
    local caller = debug.getinfo(2, "S")

    -- grab the filename and fix it if needed
    local file = lastfile
    if (lastsource ~= caller.source) then
      lastsource = caller.source
      file = lastsource
      if file:find("@") == 1 then
        file = file:sub(2):gsub("\\", "/")
        -- need this conversion to be applied to relative and absolute
        -- file names as you may write "require 'Foo'" on Windows to
        -- load "foo.lua" (as it's case insensitive) and breakpoints
        -- set on foo.lua will not work if not converted to the same case.
        if iswindows then file = string.lower(file) end
        if file:find("%./") == 1 then file = file:sub(3)
        else file = file:gsub('^'..q(basedir), '') end
      end

      -- fix filenames for loaded strings that may contain scripts with newlines;
      -- some filesystems may allow "\n" in filenames, which is not supported here.
      if file:find("\n") then
        file = file:gsub("\n", ' '):sub(1, 32) -- limit to 32 chars
      end

      -- set to true if we got here; this only needs to be done once per
      -- session, so do it here to at least avoid setting it for every line.
      seen_hook = true
      lastfile = file
    end

    local vars, status, res
    if (watchescnt > 0) then
      vars = capture_vars()
      for index, value in pairs(watches) do
        setfenv(value, vars)
        local ok, fired = pcall(value)
        if ok and fired then
          status, res = coroutine.resume(coro_debugger, events.WATCH, vars, file, line, index)
          break -- any one watch is enough; don't check multiple times
        end
      end
    end

    -- need to get into the "regular" debug handler, but only if there was
    -- no watch that was fired. If there was a watch, handle its result.
    local getin = (status == nil) and
      (step_into
      or (step_over and stack_level <= step_level)
      or has_breakpoint(file, line)
      or (socket.select(rset, nil, 0))[server])

    if getin then
      vars = vars or capture_vars()
      step_into = false
      step_over = false
      status, res = coroutine.resume(coro_debugger, events.BREAK, vars, file, line)
    end

    -- handle 'stack' command that provides stack() information to the debugger
    if status and res == 'stack' then
      while status and res == 'stack' do
        -- resume with the stack trace and variables
        if vars then restore_vars(vars) end -- restore vars so they are reflected in stack values
        -- this may fail if __tostring method fails at run-time
        local ok, snapshot = pcall(stack, 4)
        status, res = coroutine.resume(coro_debugger, ok and events.STACK or events.BREAK, snapshot, file, line)
      end
    end

    -- need to recheck once more as resume after 'stack' command may
    -- return something else (for example, 'exit'), which needs to be handled
    if status and res and res ~= 'stack' then
      if abort == nil and res == "exit" then os.exit(1) end
      abort = res
      -- only abort if safe; if not, there is another (earlier) check inside
      -- debug_hook, which will abort execution at the first safe opportunity
      if is_safe(stack_level) then error(abort) end
    elseif not status and res then
      error(res, 2) -- report any other (internal) errors back to the application
    end

    if vars then restore_vars(vars) end
  end
end

local function stringify_results(status, ...)
  if not status then return status, ... end -- on error report as it

  local t = {...}
  for i,v in pairs(t) do -- stringify each of the returned values
    local ok, res = pcall(serpent.line, v, {nocode = true, comment = 1})
    t[i] = ok and res or ("%q"):format(res):gsub("\010","n"):gsub("\026","\\026")
  end
  -- stringify table with all returned values
  -- this is done to allow each returned value to be used (serialized or not)
  -- intependently and to preserve "original" comments
  return pcall(serpent.dump, t, {sparse = false})
end

local function debugger_loop(sfile, sline)
  local command
  local app, osname
  local eval_env = {}
  local function emptyWatch () return false end
  local loaded = {}
  for k in pairs(package.loaded) do loaded[k] = true end

  while true do
    local line, err
    local wx = rawget(genv, "wx") -- use rawread to make strict.lua happy
    if (wx or mobdebug.yield) and server.settimeout then server:settimeout(mobdebug.yieldtimeout) end
    while true do
      line, err = server:receive()
      if not line and err == "timeout" then
        -- yield for wx GUI applications if possible to avoid "busyness"
        app = app or (wx and wx.wxGetApp and wx.wxGetApp())
        if app then
          local win = app:GetTopWindow()
          local inloop = app:IsMainLoopRunning()
          osname = osname or wx.wxPlatformInfo.Get():GetOperatingSystemFamilyName()
          if win and not inloop then
            -- process messages in a regular way
            -- and exit as soon as the event loop is idle
            if osname == 'Unix' then wx.wxTimer(app):Start(10, true) end
            local exitLoop = function()
              win:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_IDLE)
              win:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_TIMER)
              app:ExitMainLoop()
            end
            win:Connect(wx.wxEVT_IDLE, exitLoop)
            win:Connect(wx.wxEVT_TIMER, exitLoop)
            app:MainLoop()
          end
        elseif mobdebug.yield then mobdebug.yield()
        end
      elseif not line and err == "closed" then
        error("Debugger connection unexpectedly closed", 0)
      else
        break
      end
    end
    if server.settimeout then server:settimeout() end -- back to blocking
    command = string.sub(line, string.find(line, "^[A-Z]+"))
    if command == "SETB" then
      local _, _, _, file, line = string.find(line, "^([A-Z]+)%s+(.-)%s+(%d+)%s*$")
      if file and line then
        set_breakpoint(file, tonumber(line))
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "DELB" then
      local _, _, _, file, line = string.find(line, "^([A-Z]+)%s+(.-)%s+(%d+)%s*$")
      if file and line then
        remove_breakpoint(file, tonumber(line))
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "EXEC" then
      local _, _, chunk = string.find(line, "^[A-Z]+%s+(.+)$")
      if chunk then 
        local func, res = loadstring(chunk)
        local status
        if func then
          setfenv(func, eval_env)
          status, res = stringify_results(pcall(func))
        end
        if status then
          server:send("200 OK " .. #res .. "\n")
          server:send(res)
        else
          server:send("401 Error in Expression " .. #res .. "\n")
          server:send(res)
        end
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "LOAD" then
      local _, _, size, name = string.find(line, "^[A-Z]+%s+(%d+)%s+(%S.-)%s*$")
      size = tonumber(size)

      if abort == nil then -- no LOAD/RELOAD allowed inside start()
        if size > 0 then server:receive(size) end
        if sfile and sline then
          server:send("201 Started " .. sfile .. " " .. sline .. "\n")
        else
          server:send("200 OK 0\n")
        end
      else
        -- reset environment to allow required modules to load again
        -- remove those packages that weren't loaded when debugger started
        for k in pairs(package.loaded) do
          if not loaded[k] then package.loaded[k] = nil end
        end

        if size == 0 then -- RELOAD the current script being debugged
          server:send("200 OK 0\n")
          coroutine.yield("load")
        else
          local chunk = server:receive(size)
          if chunk then -- LOAD a new script for debugging
            local func, res = loadstring(chunk, name)
            if func then
              server:send("200 OK 0\n")
              debugee = func
              coroutine.yield("load")
            else
              server:send("401 Error in Expression " .. #res .. "\n")
              server:send(res)
            end
          else
            server:send("400 Bad Request\n")
          end
        end
      end
    elseif command == "SETW" then
      local _, _, exp = string.find(line, "^[A-Z]+%s+(.+)%s*$")
      if exp then 
        local func, res = loadstring("return(" .. exp .. ")")
        if func then
          watchescnt = watchescnt + 1
          local newidx = #watches + 1
          watches[newidx] = func
          server:send("200 OK " .. newidx .. "\n") 
        else
          server:send("401 Error in Expression " .. #res .. "\n")
          server:send(res)
        end
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "DELW" then
      local _, _, index = string.find(line, "^[A-Z]+%s+(%d+)%s*$")
      index = tonumber(index)
      if index > 0 and index <= #watches then
        watchescnt = watchescnt - (watches[index] ~= emptyWatch and 1 or 0)
        watches[index] = emptyWatch
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "RUN" then
      server:send("200 OK\n")

      local ev, vars, file, line, idx_watch = coroutine.yield()
      eval_env = vars
      if ev == events.BREAK then
        server:send("202 Paused " .. file .. " " .. line .. "\n")
      elseif ev == events.WATCH then
        server:send("203 Paused " .. file .. " " .. line .. " " .. idx_watch .. "\n")
      elseif ev == events.RESTART then
        -- nothing to do
      else
        server:send("401 Error in Execution " .. #file .. "\n")
        server:send(file)
      end
    elseif command == "STEP" then
      server:send("200 OK\n")
      step_into = true

      local ev, vars, file, line, idx_watch = coroutine.yield()
      eval_env = vars
      if ev == events.BREAK then
        server:send("202 Paused " .. file .. " " .. line .. "\n")
      elseif ev == events.WATCH then
        server:send("203 Paused " .. file .. " " .. line .. " " .. idx_watch .. "\n")
      elseif ev == events.RESTART then
        -- nothing to do
      else
        server:send("401 Error in Execution " .. #file .. "\n")
        server:send(file)
      end
    elseif command == "OVER" or command == "OUT" then
      server:send("200 OK\n")
      step_over = true
      
      -- OVER and OUT are very similar except for 
      -- the stack level value at which to stop
      if command == "OUT" then step_level = stack_level - 1
      else step_level = stack_level end

      local ev, vars, file, line, idx_watch = coroutine.yield()
      eval_env = vars
      if ev == events.BREAK then
        server:send("202 Paused " .. file .. " " .. line .. "\n")
      elseif ev == events.WATCH then
        server:send("203 Paused " .. file .. " " .. line .. " " .. idx_watch .. "\n")
      elseif ev == events.RESTART then
        -- nothing to do
      else
        server:send("401 Error in Execution " .. #file .. "\n")
        server:send(file)
      end
    elseif command == "BASEDIR" then
      local _, _, dir = string.find(line, "^[A-Z]+%s+(.+)%s*$")
      if dir then
        basedir = iswindows and string.lower(dir) or dir
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "SUSPEND" then
      -- do nothing; it already fulfilled its role
    elseif command == "STACK" then
      -- first check if we can execute the stack command
      -- as it requires yielding back to debug_hook it cannot be executed
      -- if we have not seen the hook yet as happens after start().
      -- in this case we simply return an empty result
      local vars, ev = {}
      if seen_hook then
        ev, vars = coroutine.yield("stack")
      end
      if ev and ev ~= events.STACK then
        server:send("401 Error in Execution " .. #vars .. "\n")
        server:send(vars)
      else
        local ok, res = pcall(serpent.dump, vars, {nocode = true, sparse = false})
        if ok then
          server:send("200 OK " .. res .. "\n")
        else
          server:send("401 Error in Execution " .. #res .. "\n")
          server:send(res)
        end
      end
    elseif command == "OUTPUT" then
      local _, _, stream, mode = string.find(line, "^[A-Z]+%s+(%w+)%s+([dcr])%s*$")
      if stream and mode and stream == "stdout" then
        -- assign "print" in the global environment
        genv.print = mode == 'd' and iobase.print or coroutine.wrap(function(...)
          -- wrapping into coroutine.wrap protects this function from
          -- being stepped through in the debugger
          local tbl = {...}
          while true do
            if mode == 'c' then iobase.print(unpack(tbl)) end
            for n = 1, #tbl do
              tbl[n] = select(2, pcall(serpent.line, tbl[n], {nocode = true, comment = false})) end
            local file = table.concat(tbl, "\t").."\n"
            server:send("204 Output " .. stream .. " " .. #file .. "\n" .. file)
            tbl = {coroutine.yield()}
          end
        end)
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "EXIT" then
      server:send("200 OK\n")
      coroutine.yield("exit")
    else
      server:send("400 Bad Request\n")
    end
  end
end

local function connect(controller_host, controller_port)
  return (socket.connect4 or socket.connect)(controller_host, controller_port)
end

local function isrunning()
  return coro_debugger and coroutine.status(coro_debugger) == 'suspended'
end

-- Starts a debug session by connecting to a controller
local function start(controller_host, controller_port)
  -- only one debugging session can be run (as there is only one debug hook)
  if isrunning() then return end

  controller_host = controller_host or "localhost"
  controller_port = controller_port or mobdebug.port

  server = (socket.connect4 or socket.connect)(controller_host, controller_port)
  if server then
    rset = {server} -- store hash to avoid recreating it later
    -- check if we are called from the debugger as this may happen
    -- when another debugger function calls start(); only check one level deep
    local this = debug.getinfo(1, "S").source
    local info = debug.getinfo(2, "Sl")
    if info.source == this then info = debug.getinfo(3, "Sl") end

    local file = info.source
    if string.find(file, "@") == 1 then file = string.sub(file, 2) end
    if string.find(file, "%.[/\\]") == 1 then file = string.sub(file, 3) end

    -- correct stack depth which already has some calls on it
    -- so it doesn't go into negative when those calls return
    -- as this breaks subsequence checks in stack_depth().
    -- start from 16th frame, which is sufficiently large for this check.
    stack_level = stack_depth(16)

    -- provide our own traceback function to report the error remotely
    do
      local dtraceback = debug.traceback
      debug.traceback = function (err) genv.print(dtraceback(err, 3)) end
    end
    coro_debugger = coroutine.create(debugger_loop)
    debug.sethook(debug_hook, "lcr")
    local ok, res = coroutine.resume(coro_debugger, file, info.currentline)
    if not ok and res then error(res, 2) end
    return true
  else
    print("Could not connect to " .. controller_host .. ":" .. controller_port)
  end
end

local function controller(controller_host, controller_port)
  -- only one debugging session can be run (as there is only one debug hook)
  if isrunning() then return end

  controller_host = controller_host or "localhost"
  controller_port = controller_port or mobdebug.port

  local exitonerror = not skip -- exit if not running a scratchpad
  server = (socket.connect4 or socket.connect)(controller_host, controller_port)
  if server then
    rset = {server} -- store hash to avoid recreating it later

    local function report(trace, err)
      local msg = err .. "\n" .. trace
      server:send("401 Error in Execution " .. #msg .. "\n")
      server:send(msg)
      return err
    end

    seen_hook = true -- allow to accept all commands
    coro_debugger = coroutine.create(debugger_loop)

    while true do
      step_into = true -- start with step command
      abort = false -- reset abort flag from the previous loop
      if skip then skipcount = skip end -- force suspend right away

      coro_debugee = coroutine.create(debugee)
      debug.sethook(coro_debugee, debug_hook, "lcr")
      local status, err = coroutine.resume(coro_debugee)

      -- was there an error or is the script done?
      -- 'abort' state is allowed here; ignore it
      if abort then
        if tostring(abort) == 'exit' then break end
      else
        if status then -- normal execution is done
          break
        elseif err and not tostring(err):find(deferror) then
          -- report the error back
          -- err is not necessarily a string, so convert to string to report
          report(debug.traceback(coro_debugee), tostring(err))
          if exitonerror then break end
          -- resume once more to clear the response the debugger wants to send
          local status, err = coroutine.resume(coro_debugger, events.RESTART, capture_vars(2))
          if not status or status and err == "exit" then break end
        end
      end
    end
  else
    print("Could not connect to " .. controller_host .. ":" .. controller_port)
    return false
  end
  return true
end

local function scratchpad(controller_host, controller_port, frequency)
  skip = frequency or 100
  return controller(controller_host, controller_port)
end

local function loop(controller_host, controller_port)
  skip = nil -- just in case if loop() is called after scratchpad()
  return controller(controller_host, controller_port)
end

local function on()
  if not (isrunning() and server) then return end

  local co = coroutine.running()
  if co then
    if not coroutines[co] then
      coroutines[co] = true
      debug.sethook(co, debug_hook, "lcr")
    end
  else
    debug.sethook(debug_hook, "lcr")
  end
end

local function off()
  if not (isrunning() and server) then return end

  local co = coroutine.running()
  if co then
    if coroutines[co] then coroutines[co] = false end
    -- don't remove coroutine hook under LuaJIT as there is only one (global) hook
    if not jit then debug.sethook(co) end
  else
    debug.sethook()
  end
end

-- Handles server debugging commands 
local function handle(params, client, options)
  local _, _, command = string.find(params, "^([a-z]+)")
  local file, line, watch_idx
  if command == "run" or command == "step" or command == "out"
  or command == "over" or command == "exit" then
    client:send(string.upper(command) .. "\n")
    client:receive() -- this should consume the first '200 OK' response
    while true do
      local done = true
      local breakpoint = client:receive()
      if not breakpoint then
        print("Program finished")
        os.exit()
        return -- use return here for those cases where os.exit() is not wanted
      end
      local _, _, status = string.find(breakpoint, "^(%d+)")
      if status == "200" then
        -- don't need to do anything
      elseif status == "202" then
        _, _, file, line = string.find(breakpoint, "^202 Paused%s+(.-)%s+(%d+)%s*$")
        if file and line then
          print("Paused at file " .. file .. " line " .. line)
        end
      elseif status == "203" then
        _, _, file, line, watch_idx = string.find(breakpoint, "^203 Paused%s+(.-)%s+(%d+)%s+(%d+)%s*$")
        if file and line and watch_idx then
          print("Paused at file " .. file .. " line " .. line .. " (watch expression " .. watch_idx .. ": [" .. watches[watch_idx] .. "])")
        end
      elseif status == "204" then
        local _, _, stream, size = string.find(breakpoint, "^204 Output (%w+) (%d+)$")
        if stream and size then
          local msg = client:receive(tonumber(size))
          print(msg)
          if outputs[stream] then outputs[stream](msg) end
          -- this was just the output, so go back reading the response
          done = false
        end
      elseif status == "401" then
        local _, _, size = string.find(breakpoint, "^401 Error in Execution (%d+)$")
        if size then
          local msg = client:receive(tonumber(size))
          print("Error in remote application: " .. msg)
          os.exit(1)
          return nil, nil, msg -- use return here for those cases where os.exit() is not wanted
        end
      else
        print("Unknown error")
        os.exit(1)
        -- use return here for those cases where os.exit() is not wanted
        return nil, nil, "Debugger error: unexpected response '" .. breakpoint .. "'"
      end
      if done then break end
    end
  elseif command == "setb" then
    _, _, _, file, line = string.find(params, "^([a-z]+)%s+(.-)%s+(%d+)%s*$")
    if file and line then
      file = string.gsub(file, "\\", "/") -- convert slash
      file = removebasedir(file, basedir)
      client:send("SETB " .. file .. " " .. line .. "\n")
      if client:receive() == "200 OK" then 
        if not breakpoints[file] then breakpoints[file] = {} end
        breakpoints[file][line] = true
      else
        print("Error: breakpoint not inserted")
      end
    else
      print("Invalid command")
    end
  elseif command == "setw" then
    local _, _, exp = string.find(params, "^[a-z]+%s+(.+)$")
    if exp then
      client:send("SETW " .. exp .. "\n")
      local answer = client:receive()
      local _, _, watch_idx = string.find(answer, "^200 OK (%d+)%s*$")
      if watch_idx then
        watches[watch_idx] = exp
        print("Inserted watch exp no. " .. watch_idx)
      else
        local _, _, size = string.find(answer, "^401 Error in Expression (%d+)$")
        if size then
          local err = client:receive(tonumber(size)):gsub(".-:%d+:%s*","")
          print("Error: watch expression not set: " .. err)
        else
          print("Error: watch expression not set")
        end
      end
    else
      print("Invalid command")
    end
  elseif command == "delb" then
    _, _, _, file, line = string.find(params, "^([a-z]+)%s+(.-)%s+(%d+)%s*$")
    if file and line then
      file = string.gsub(file, "\\", "/") -- convert slash
      file = removebasedir(file, basedir)
      client:send("DELB " .. file .. " " .. line .. "\n")
      if client:receive() == "200 OK" then 
        if breakpoints[file] then breakpoints[file][line] = nil end
      else
        print("Error: breakpoint not removed")
      end
    else
      print("Invalid command")
    end
  elseif command == "delallb" then
    for file, breaks in pairs(breakpoints) do
      for line, _ in pairs(breaks) do
        client:send("DELB " .. file .. " " .. line .. "\n")
        if client:receive() == "200 OK" then 
          breakpoints[file][line] = nil
        else
          print("Error: breakpoint at file " .. file .. " line " .. line .. " not removed")
        end
      end
    end
  elseif command == "delw" then
    local _, _, index = string.find(params, "^[a-z]+%s+(%d+)%s*$")
    if index then
      client:send("DELW " .. index .. "\n")
      if client:receive() == "200 OK" then 
        watches[index] = nil
      else
        print("Error: watch expression not removed")
      end
    else
      print("Invalid command")
    end
  elseif command == "delallw" then
    for index, exp in pairs(watches) do
      client:send("DELW " .. index .. "\n")
      if client:receive() == "200 OK" then 
        watches[index] = nil
      else
        print("Error: watch expression at index " .. index .. " [" .. exp .. "] not removed")
      end
    end    
  elseif command == "eval" or command == "exec" 
      or command == "load" or command == "loadstring"
      or command == "reload" then
    local _, _, exp = string.find(params, "^[a-z]+%s+(.+)$")
    if exp or (command == "reload") then 
      if command == "eval" or command == "exec" then
        exp = (exp:gsub("%-%-%[(=*)%[.-%]%1%]", "") -- remove comments
                  :gsub("%-%-.-\n", " ") -- remove line comments
                  :gsub("\n", " ")) -- convert new lines
        if command == "eval" then exp = "return " .. exp end
        client:send("EXEC " .. exp .. "\n")
      elseif command == "reload" then
        client:send("LOAD 0 -\n")
      elseif command == "loadstring" then
        local _, _, _, file, lines = string.find(exp, "^([\"'])(.-)%1%s+(.+)")
        if not file then
           _, _, file, lines = string.find(exp, "^(%S+)%s+(.+)")
        end
        client:send("LOAD " .. #lines .. " " .. file .. "\n")
        client:send(lines)
      else
        local file = io.open(exp, "r")
        if not file and pcall(require, "winapi") then
          -- if file is not open and winapi is there, try with a short path;
          -- this may be needed for unicode paths on windows
          winapi.set_encoding(winapi.CP_UTF8)
          file = io.open(winapi.short_path(exp), "r")
        end
        if not file then error("Cannot open file " .. exp) end
        -- read the file and remove the shebang line as it causes a compilation error
        local lines = file:read("*all"):gsub("^#!.-\n", "\n")
        file:close()

        local file = string.gsub(exp, "\\", "/") -- convert slash
        file = removebasedir(file, basedir)
        client:send("LOAD " .. #lines .. " " .. file .. "\n")
        client:send(lines)
      end
      while true do
        local params, err = client:receive()
        if not params then
          return nil, nil, "Debugger connection " .. (err or "error")
        end
        local done = true
        local _, _, status, len = string.find(params, "^(%d+).-%s+(%d+)%s*$")
        if status == "200" then
          len = tonumber(len)
          if len > 0 then
            local status, res
            local str = client:receive(len)
            -- handle serialized table with results
            local func, err = loadstring(str)
            if func then
              status, res = pcall(func)
              if not status then err = res
              elseif type(res) ~= "table" then
                err = "received "..type(res).." instead of expected 'table'"
              end
            end
            if err then
              print("Error in processing results: " .. err)
              return nil, nil, "Error in processing results: " .. err
            end
            print((table.unpack or unpack)(res))
            return res[1], res
          end
        elseif status == "201" then
          _, _, file, line = string.find(params, "^201 Started%s+(.-)%s+(%d+)%s*$")
        elseif status == "202" or params == "200 OK" then
          -- do nothing; this only happens when RE/LOAD command gets the response
          -- that was for the original command that was aborted
        elseif status == "204" then
          local _, _, stream, size = string.find(params, "^204 Output (%w+) (%d+)$")
          if stream and size then
            local msg = client:receive(tonumber(size))
            print(msg)
            if outputs[stream] then outputs[stream](msg) end
            -- this was just the output, so go back reading the response
            done = false
          end
        elseif status == "401" then
          len = tonumber(len)
          local res = client:receive(len)
          print("Error in expression: " .. res)
          return nil, nil, res
        else
          print("Unknown error")
          return nil, nil, "Debugger error: unexpected response after EXEC/LOAD '" .. params .. "'"
        end
        if done then break end
      end
    else
      print("Invalid command")
    end
  elseif command == "listb" then
    for k, v in pairs(breakpoints) do
      local b = k .. ": " -- get filename
      for k in pairs(v) do
        b = b .. k .. " " -- get line numbers
      end
      print(b)
    end
  elseif command == "listw" then
    for i, v in pairs(watches) do
      print("Watch exp. " .. i .. ": " .. v)
    end    
  elseif command == "suspend" then
    client:send("SUSPEND\n")
  elseif command == "stack" then
    client:send("STACK\n")
    local resp = client:receive()
    local _, _, status, res = string.find(resp, "^(%d+)%s+%w+%s+(.+)%s*$")
    if status == "200" then
      local func, err = loadstring(res)
      if func == nil then
        print("Error in stack information: " .. err)
        return nil, nil, err
      end
      local ok, stack = pcall(func)
      if not ok then
        print("Error in stack information: " .. stack)
        return nil, nil, stack
      end
      for _,frame in ipairs(stack) do
        print(serpent.line(frame[1], {comment = false}))
      end
      return stack
    elseif status == "401" then
      local _, _, len = string.find(resp, "%s+(%d+)%s*$")
      len = tonumber(len)
      local res = len > 0 and client:receive(len) or "Invalid stack information."
      print("Error in expression: " .. res)
      return nil, nil, res
    else
      print("Unknown error")
      return nil, nil, "Debugger error: unexpected response after STACK"
    end
  elseif command == "output" then
    local _, _, stream, mode = string.find(params, "^[a-z]+%s+(%w+)%s+([dcr])%s*$")
    if stream and mode then
      client:send("OUTPUT "..stream.." "..mode.."\n")
      local resp = client:receive()
      local _, _, status = string.find(resp, "^(%d+)%s+%w+%s*$")
      if status == "200" then
        print("Stream "..stream.." redirected")
        outputs[stream] = type(options) == 'table' and options.handler or nil
      else
        print("Unknown error")
        return nil, nil, "Debugger error: can't redirect "..stream
      end
    else
      print("Invalid command")
    end
  elseif command == "basedir" then
    local _, _, dir = string.find(params, "^[a-z]+%s+(.+)$")
    if dir then
      dir = string.gsub(dir, "\\", "/") -- convert slash
      if not string.find(dir, "/$") then dir = dir .. "/" end

      local remdir = dir:match("\t(.+)")
      if remdir then dir = dir:gsub("/?\t.+", "/") end
      basedir = dir

      client:send("BASEDIR "..(remdir or dir).."\n")
      local resp = client:receive()
      local _, _, status = string.find(resp, "^(%d+)%s+%w+%s*$")
      if status == "200" then
        print("New base directory is " .. basedir)
      else
        print("Unknown error")
        return nil, nil, "Debugger error: unexpected response after BASEDIR"
      end
    else
      print(basedir)
    end
  elseif command == "help" then
    print("setb <file> <line>    -- sets a breakpoint")
    print("delb <file> <line>    -- removes a breakpoint")
    print("delallb               -- removes all breakpoints")
    print("setw <exp>            -- adds a new watch expression")
    print("delw <index>          -- removes the watch expression at index")
    print("delallw               -- removes all watch expressions")
    print("run                   -- runs until next breakpoint")
    print("step                  -- runs until next line, stepping into function calls")
    print("over                  -- runs until next line, stepping over function calls")
    print("out                   -- runs until line after returning from current function")
    print("listb                 -- lists breakpoints")
    print("listw                 -- lists watch expressions")
    print("eval <exp>            -- evaluates expression on the current context and returns its value")
    print("exec <stmt>           -- executes statement on the current context")
    print("load <file>           -- loads a local file for debugging")
    print("reload                -- restarts the current debugging session")
    print("stack                 -- reports stack trace")
    print("output stdout <d|c|r> -- capture and redirect io stream (default|copy|redirect)")
    print("basedir [<path>]      -- sets the base path of the remote application, or shows the current one")
    print("exit                  -- exits debugger")
  else
    local _, _, spaces = string.find(params, "^(%s*)$")
    if not spaces then
      print("Invalid command")
      return nil, nil, "Invalid command"
    end
  end
  return file, line
end

-- Starts debugging server
local function listen(host, port)
  host = host or "*"
  port = port or mobdebug.port

  local socket = require "socket"

  print("Lua Remote Debugger")
  print("Run the program you wish to debug")

  local server = socket.bind(host, port)
  local client = server:accept()

  client:send("STEP\n")
  client:receive()

  local breakpoint = client:receive()
  local _, _, file, line = string.find(breakpoint, "^202 Paused%s+(.-)%s+(%d+)%s*$")
  if file and line then
    print("Paused at file " .. file )
    print("Type 'help' for commands")
  else
    local _, _, size = string.find(breakpoint, "^401 Error in Execution (%d+)%s*$")
    if size then
      print("Error in remote application: ")
      print(client:receive(size))
    end
  end

  while true do
    io.write("> ")
    local line = io.read("*line")
    handle(line, client)
  end
end

local cocreate
local function coro()
  if cocreate then return end -- only set once
  cocreate = cocreate or coroutine.create
  coroutine.create = function(f, ...)
    return cocreate(function(...)
      require("mobdebug").on()
      return f(...)
    end, ...)
  end
end

local moconew
local function moai()
  if moconew then return end -- only set once
  moconew = moconew or (MOAICoroutine and MOAICoroutine.new)
  if not moconew then return end
  MOAICoroutine.new = function(...)
    local thread = moconew(...)
    local mt = getmetatable(thread)
    local patched = mt.run
    mt.run = function(self, f, ...)
      return patched(self,  function(...)
        require("mobdebug").on()
        return f(...)
      end, ...)
    end
    return thread
  end
end

-- make public functions available
mobdebug.listen = listen
mobdebug.loop = loop
mobdebug.scratchpad = scratchpad
mobdebug.handle = handle
mobdebug.connect = connect
mobdebug.start = start
mobdebug.on = on
mobdebug.off = off
mobdebug.moai = moai
mobdebug.coro = coro
mobdebug.line = serpent.line
mobdebug.dump = serpent.dump
mobdebug.yield = nil -- callback

-- this is needed to make "require 'modebug'" to work when mobdebug
-- module is loaded manually
package.loaded.mobdebug = mobdebug

return mobdebug
