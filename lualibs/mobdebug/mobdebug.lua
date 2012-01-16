--
-- MobDebug 0.43
-- Copyright Paul Kulchenko 2011
-- Based on RemDebug 1.0 (http://www.keplerproject.org/remdebug)
--

(function()

module("mobdebug", package.seeall)

_COPYRIGHT = "Paul Kulchenko"
_DESCRIPTION = "Mobile Remote Debugger for the Lua programming language"
_VERSION = "0.43"

-- this is a socket class that implements maConnect interface
local function socketMobileLua() 
  local self = {}
  self.select = function() return {} end
  self.connect = coroutine.wrap(function(host, port)
    while true do
      local connection = maConnect("socket://" .. host .. ":" .. port)
  
      if connection > 0 then
        local event = SysEventCreate()
        while true do
          maWait(0)
          maGetEvent(event)
          local eventType = SysEventGetType(event)
          if (EVENT_TYPE_CLOSE == eventType) then maExit(0) end
          if (EVENT_TYPE_CONN == eventType and
            SysEventGetConnHandle(event) == connection and
            SysEventGetConnOpType(event) == CONNOP_CONNECT) then
              -- result > 0 ? success : error
              if not (SysEventGetConnResult(event) > 0) then connection = nil end
              break
          end
        end
        SysFree(event)
      end
  
      host, port = coroutine.yield(connection and (function ()
        local self = {}
        local outBuffer = SysAlloc(1000)
        local inBuffer = SysAlloc(1000)
        local event = SysEventCreate()
        local recvBuffer = ""
        function stringToBuffer(s, buffer)
          local i = 0
          for c in s:gmatch(".") do
            i = i + 1
            local b = s:byte(i)
            SysBufferSetByte(buffer, i - 1, b)
          end
          return i
        end
        function bufferToString(buffer, len)
          local s = ""
          for i = 0, len - 1 do
            local c = SysBufferGetByte(buffer, i)
            s = s .. string.char(c)
          end
          return s
        end
        self.send = coroutine.wrap(function(self, msg) 
          while true do
            local numberOfBytes = stringToBuffer(msg, outBuffer)
            maConnWrite(connection, outBuffer, numberOfBytes)
            local result = 0
            while true do
              maWait(0)
              maGetEvent(event)
              local eventType = SysEventGetType(event)
              if (EVENT_TYPE_CLOSE == eventType) then maExit(0) end
              if (EVENT_TYPE_CONN == eventType and
                  SysEventGetConnHandle(event) == connection and
                  SysEventGetConnOpType(event) == CONNOP_WRITE) then
                break
              end
            end
            self, msg = coroutine.yield()
          end
        end)
        self.receive = coroutine.wrap(function(self, len) 
          while true do
            local line = recvBuffer
            while (len and string.len(line) < len)     -- either we need len bytes
               or (not len and not line:find("\n")) do -- or one line (if no len specified)
              maConnRead(connection, inBuffer, 1000)
              while true do
                maWait(0)
                maGetEvent(event)
                local eventType = SysEventGetType(event)
                if (EVENT_TYPE_CLOSE == eventType) then maExit(0) end
                if (EVENT_TYPE_CONN == eventType and
                    SysEventGetConnHandle(event) == connection and
                    SysEventGetConnOpType(event) == CONNOP_READ) then
                  local result = SysEventGetConnResult(event);
                  if result > 0 then line = line .. bufferToString(inBuffer, result) end
                  break; -- got the event we wanted; now check if we have all we need
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
            SysFree(inBuffer)
            SysFree(outBuffer)
            SysFree(event)
            maConnClose(connection)
            coroutine.yield(self)
          end
        end)
        return self
      end)())
    end
  end)

  return self
end

local socket = maConnect and socketMobileLua() or (require "socket")

--
-- RemDebug 1.0 Beta
-- Copyright Kepler Project 2005 (http://www.keplerproject.org/remdebug)
--

local debug = require "debug"
local coro_debugger
local events = { BREAK = 1, WATCH = 2 }
local breakpoints = {}
local watches = {}
local lastsource
local lastfile
local watchescnt = 0
local abort -- default value is nil; this is used in start/loop distinction
local check_break = false
local step_into = false
local step_over = false
local step_level = 0
local stack_level = 0
local server
local debugee = function () 
  local a = 1
  a = a + 1
  return "ok"
end

local function set_breakpoint(file, line)
  if file == '-' and lastfile then file = lastfile end
  if not breakpoints[file] then
    breakpoints[file] = {}
  end
  breakpoints[file][line] = true  
end

local function remove_breakpoint(file, line)
  if file == '-' and lastfile then file = lastfile end
  if breakpoints[file] then
    breakpoints[file][line] = nil
  end
end

local function has_breakpoint(file, line)
  return breakpoints[file] and breakpoints[file][line]
end

local function restore_vars(vars)
  if type(vars) ~= 'table' then return end
  local func = debug.getinfo(3, "f").func
  local i = 1
  local written_vars = {}
  while true do
    local name = debug.getlocal(3, i)
    if not name then break end
    debug.setlocal(3, i, vars[name])
    written_vars[name] = true
    i = i + 1
  end
  i = 1
  while true do
    local name = debug.getupvalue(func, i)
    if not name then break end
    if not written_vars[name] then
      debug.setupvalue(func, i, vars[name])
      written_vars[name] = true
    end
    i = i + 1
  end
end

local function capture_vars()
  local vars = {}
  local func = debug.getinfo(3, "f").func
  local i = 1
  while true do
    local name, value = debug.getupvalue(func, i)
    if not name then break end
    vars[name] = value
    i = i + 1
  end
  i = 1
  while true do
    local name, value = debug.getlocal(3, i)
    if not name then break end
    vars[name] = value
    i = i + 1
  end
  setmetatable(vars, { __index = getfenv(func), __newindex = getfenv(func) })
  return vars
end

local function debug_hook(event, line)
  if abort then error("aborted") end -- abort execution for RE/LOAD
  if event == "call" then
    stack_level = stack_level + 1
  elseif event == "return" or event == "tail return" then
    stack_level = stack_level - 1
  elseif event == "line" then
    local caller = debug.getinfo(2, "S")

    -- grab the filename and fix it if needed
    local file = lastfile
    if (lastsource ~= caller.source) then
      lastsource = caller.source
      file = lastsource
      if string.find(file, "@") == 1 then file = string.sub(file, 2) end
      -- remove references to the current folder (./ or .\)
      if string.find(file, "%.[/\\]") == 1 then file = string.sub(file, 3) end
      -- fix filenames for loaded strings that may contain scripts with newlines
      if string.find(file, "\n") then
        file = string.sub(string.gsub(file, "\n", ' '), 1, 32) -- limit to 32 chars
      end
      file = string.gsub(file, "\\", "/") -- convert slash
      lastfile = file
    end

    local vars
    if (watchescnt > 0) then
      vars = capture_vars()
      for index, value in pairs(watches) do
        setfenv(value, vars)
        local status, res = pcall(value)
        if status and res then
          coroutine.resume(coro_debugger, events.WATCH, vars, file, line, index)
          restore_vars(vars)
        end
      end
    end

    if step_into
    or (step_over and stack_level <= step_level)
    or has_breakpoint(file, line)
    or (check_break and (socket.select({server}, {}, 0))[server]) then
      vars = vars or capture_vars()
      check_break = true -- this is only needed to avoid breaking too early when debugging is starting
      step_into = false
      step_over = false
      coroutine.resume(coro_debugger, events.BREAK, vars, file, line)
    end
  end
end

local function debugger_loop(sfile, sline)
  local command
  local eval_env = {}
  local function emptyWatch () return false end

  while true do
    local line, err
    if server.settimeout then server:settimeout(0.010) end
    while true do
      line, err = server:receive()
      if not line and err == "timeout" then
        -- yield for wx GUI applications if possible to avoid "busyness"
        if wx and wx.wxGetApp then
          local app = wx.wxGetApp()
          local win = app:GetTopWindow()
          if win then
            -- process messages in a regular way
            -- and exit as soon as the event loop is idle
            win:Connect(wx.wxEVT_IDLE, function(event)
              app:ExitMainLoop()
              win:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_IDLE)
            end)
            app:MainLoop()
          end
        end
      else
        break
      end
    end
    if server.settimeout then server:settimeout() end -- back to blocking
    command = string.sub(line, string.find(line, "^[A-Z]+"))
    if command == "SETB" then
      local _, _, _, file, line = string.find(line, "^([A-Z]+)%s+([%w%p%s]+)%s+(%d+)%s*$")
      if file and line then
        set_breakpoint(file, tonumber(line))
        server:send("200 OK\n")
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "DELB" then
      _, _, _, file, line = string.find(line, "^([A-Z]+)%s+([%w%p%s]+)%s+(%d+)%s*$")
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
          status, res = pcall(func)
        end
        res = tostring(res)
        if status then
          server:send("200 OK " .. string.len(res) .. "\n") 
          server:send(res)
        else
          server:send("401 Error in Expression " .. string.len(res) .. "\n")
          server:send(res)
        end
      else
        server:send("400 Bad Request\n")
      end
    elseif command == "LOAD" then
      local _, _, size, name = string.find(line, "^[A-Z]+%s+(%d+)%s+([%w%p%s]*[%w%p]+)%s*$")
      size = tonumber(size)

      if abort == nil then -- no LOAD/RELOAD allowed inside start()
        if size > 0 then local _ = server:receive(size) end
        if sfile and sline then
          server:send("201 Started " .. sfile .. " " .. sline .. "\n")
        else
          server:send("200 OK 0\n")
        end
      else
        if size == 0 then -- RELOAD the current script being debugged
          server:send("200 OK 0\n")
          abort = true
          coroutine.yield() -- this should not return as the hook will abort
        end

        local chunk = server:receive(size)
        if chunk then -- LOAD a new script for debugging
          local func, res = loadstring(chunk, name)
          if func then
            server:send("200 OK 0\n")
            debugee = func
            abort = true
            coroutine.yield() -- this should not return as the hook will abort
          else
            server:send("401 Error in Expression " .. string.len(res) .. "\n")
            server:send(res)
          end
        else
          server:send("400 Bad Request\n")
        end
      end
    elseif command == "SETW" then
      local _, _, exp = string.find(line, "^[A-Z]+%s+(.+)%s*$")
      if exp then 
        local func = loadstring("return(" .. exp .. ")")
        if func then
          watchescnt = watchescnt + 1
          local newidx = #watches + 1
          watches[newidx] = func
          server:send("200 OK " .. newidx .. "\n") 
        else
          server:send("400 Bad Request\n")
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
      else
        server:send("401 Error in Execution " .. string.len(file) .. "\n")
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
      else
        server:send("401 Error in Execution " .. string.len(file) .. "\n")
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
      else
        server:send("401 Error in Execution " .. string.len(file) .. "\n")
        server:send(file)
      end
    elseif command == "EXIT" then
      server:send("200 OK\n")
      os.exit()
    else
      server:send("400 Bad Request\n")
    end
  end
end

function connect(controller_host, controller_port)
  return socket.connect(controller_host, controller_port)
end

-- Tries to start the debug session by connecting with a controller
function start(controller_host, controller_port)
  server = socket.connect(controller_host, controller_port)
  if server then
    local info = debug.getinfo(2, "Sl")
    local file = info.source
    if string.find(file, "@") == 1 then file = string.sub(file, 2) end
    if string.find(file, "%.[/\\]") == 1 then file = string.sub(file, 3) end

    debug.sethook(debug_hook, "lcr")
    coro_debugger = coroutine.create(debugger_loop)
    return coroutine.resume(coro_debugger, file, info.currentline)
  else
    print("Could not connect to " .. controller_host .. ":" .. controller_port)
  end
end

function loop(controller_host, controller_port)
  server = socket.connect(controller_host, controller_port)
  if server then
    local function report(trace, err)
      local msg = err .. "\n" .. trace
      server:send("401 Error in Execution " .. string.len(msg) .. "\n")
      server:send(msg)
      server:close()
      return err
    end

    while true do 
      step_into = true
      abort = false
      coro_debugger = coroutine.create(debugger_loop)

      local coro_debugee = coroutine.create(debugee)
      debug.sethook(coro_debugee, debug_hook, "lcr")
      local status, error = coroutine.resume(coro_debugee)

      -- was there an error or is the script done?
      if not abort then -- this is an expected error; ignore it
        if not status then -- this is something to be reported
          return false,report(debug.traceback(coro_debugee), error) 
        end
        break
      end
    end
    server:close()
  else
    print("Could not connect to " .. controller_host .. ":" .. controller_port)
    return false
  end
  return true
end

local basedir = ""

-- Handles server debugging commands 
function handle(params, client)
  local _, _, command = string.find(params, "^([a-z]+)")
  local file, line, watch_idx
  if command == "run" or command == "step" or command == "out"
  or command == "over" or command == "exit" then
    client:send(string.upper(command) .. "\n")
    client:receive()
    local breakpoint = client:receive()
    if not breakpoint then
      print("Program finished")
      os.exit()
      return -- use return here for those cases where os.exit() is not wanted
    end
    local _, _, status = string.find(breakpoint, "^(%d+)")
    if status == "202" then
      _, _, file, line = string.find(breakpoint, "^202 Paused%s+([%w%p%s]+)%s+(%d+)%s*$")
      if file and line then 
        print("Paused at file " .. file .. " line " .. line)
      end
    elseif status == "203" then
      _, _, file, line, watch_idx = string.find(breakpoint, "^203 Paused%s+([%w%p%s]+)%s+(%d+)%s+(%d+)%s*$")
      if file and line and watch_idx then
        print("Paused at file " .. file .. " line " .. line .. " (watch expression " .. watch_idx .. ": [" .. watches[watch_idx] .. "])")
      end
    elseif status == "401" then 
      local _, _, size = string.find(breakpoint, "^401 Error in Execution (%d+)$")
      if size then
        local msg = client:receive(tonumber(size))
        print("Error in remote application: " .. msg)
        os.exit()
        return nil, nil, msg -- use return here for those cases where os.exit() is not wanted
      end
    else
      print("Unknown error")
      os.exit()
      return nil, nil, "Unknown error" -- use return here for those cases where os.exit() is not wanted
    end
  elseif command == "setb" then
    _, _, _, file, line = string.find(params, "^([a-z]+)%s+([%w%p%s]+)%s+(%d+)%s*$")
    if file and line then
      file = string.gsub(file, "\\", "/") -- convert slash
      file = string.gsub(file, basedir, '') -- remove basedir
      if not breakpoints[file] then breakpoints[file] = {} end
      client:send("SETB " .. file .. " " .. line .. "\n")
      if client:receive() == "200 OK" then 
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
        print("Error: Watch expression not inserted")
      end
    else
      print("Invalid command")
    end
  elseif command == "delb" then
    _, _, _, file, line = string.find(params, "^([a-z]+)%s+([%w%p%s]+)%s+(%d+)%s*$")
    if file and line then
      file = string.gsub(file, "\\", "/") -- convert slash
      file = string.gsub(file, basedir, '') -- remove basedir
      if not breakpoints[file] then breakpoints[file] = {} end
      client:send("DELB " .. file .. " " .. line .. "\n")
      if client:receive() == "200 OK" then 
        breakpoints[file][line] = nil
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
      or command == "load" or command == "reload" then
    local _, _, exp = string.find(params, "^[a-z]+%s+(.+)$")
    if exp or (command == "reload") then 
      if command == "eval" then
        exp = string.gsub(exp, "\n", " ") -- convert new lines
        client:send("EXEC return " .. exp .. "\n")
      elseif command == "exec" then
        exp = string.gsub(exp, "\n", " ") -- convert new lines
        client:send("EXEC " .. exp .. "\n")
      elseif command == "reload" then
        client:send("LOAD 0 -\n")
      else
        local file = io.open(exp, "r")
        if not file then print("Cannot open file " .. exp); return end
        local lines = file:read("*all")
        file:close()

        local file = string.gsub(exp, "\\", "/") -- convert slash
        file = string.gsub(file, basedir, '') -- remove basedir
        client:send("LOAD " .. string.len(lines) .. " " .. file .. "\n")
        client:send(lines)
      end
      local params = client:receive()
      local _, _, status, len = string.find(params, "^(%d+)[%w%p%s]+%s+(%d+)%s*$")
      if status == "200" then
        len = tonumber(len)
        if len > 0 then 
          local res = client:receive(len)
          print(res)
          return res
        end
      elseif status == "201" then
        _, _, file, line = string.find(params, "^201 Started%s+([%w%p%s]+)%s+(%d+)%s*$")
      elseif status == "401" then
        len = tonumber(len)
        local res = client:receive(len)
        print("Error in expression: " .. res)
        return nil, nil, res
      else
        print("Unknown error")
        return nil, nil, "Unknown error"
      end
    else
      print("Invalid command")
    end
  elseif command == "listb" then
    for k, v in pairs(breakpoints) do
      local b = k .. ": " -- get filename
      for k, v in pairs(v) do
        b = b .. k .. " " -- get line numbers
      end
      print(b)
    end
  elseif command == "listw" then
    for i, v in pairs(watches) do
      print("Watch exp. " .. i .. ": " .. v)
    end    
  elseif command == "basedir" then
    local _, _, dir = string.find(params, "^[a-z]+%s+(.+)$")
    if dir then
      dir = string.gsub(dir, "\\", "/") -- convert slash
      if not string.find(dir, "/$") then dir = dir .. "/" end
      basedir = dir
      print("New base directory is " .. basedir)
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
    print("run                   -- run until next breakpoint")
    print("step                  -- run until next line, stepping into function calls")
    print("over                  -- run until next line, stepping over function calls")
    print("out                   -- run until line after returning from current function")
    print("listb                 -- lists breakpoints")
    print("listw                 -- lists watch expressions")
    print("eval <exp>            -- evaluates expression on the current context and returns its value")
    print("exec <stmt>           -- executes statement on the current context")
    print("load <file>           -- loads a local file for debugging")
    print("reload                -- restarts the current debugging session")
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
function listen(host, port)

  local socket = require "socket"

  print("Lua Remote Debugger")
  print("Run the program you wish to debug")

  local server = socket.bind(host, port)
  local client = server:accept()

  client:send("STEP\n")
  client:receive()

  local breakpoint = client:receive()
  local _, _, file, line = string.find(breakpoint, "^202 Paused%s+([%w%p%s]+)%s+(%d+)%s*$")
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

end)()
