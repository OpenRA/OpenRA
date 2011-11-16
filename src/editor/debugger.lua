--
-- RemDebug 1.0 Beta
-- Copyright Kepler Project 2005 (http://www.keplerproject.org/remdebug)
--

local copas  = require "copas"
local socket = require "socket"
local mobdebug = require "mobdebug"

local debugger = {}
debugger.server     = nil    -- DebuggerServer object when debugging, else nil
debugger.running    = false  -- true when the debuggee is running
debugger.portnumber = 8171   -- the port # to use for debugging
debugger.watchWindow      = nil    -- the watchWindow, nil when not created
debugger.watchListCtrl    = nil    -- the child listctrl in the watchWindow

ide.debugger = debugger

debugger.listen = function () 
  local server = socket.bind("*", debugger.portnumber)
  DisplayOutput("Started server on " .. debugger.portnumber .. "\n")
  copas.autoclose = false
  copas.addserver(server, function (skt)
    debugger.server = copas.wrap(skt)
    debugger.run("step")
    SetAllEditorsReadOnly(true)
    DisplayOutput("Established session with "..wx.wxGetHostName()..":"..debugger.portnumber.."\n")
  end)
end

debugger.handle = function(line)
  local _G = _G
  local os = os
  os.exit = function () end
  _G.print = function (line) DisplayOutput(line .. "\n") end
  return mobdebug.handle(line, debugger.server);
end

debugger.run = function (command)
  if debugger.server then
    copas.addthread(function ()
      debugger.running = true
      local file, line = debugger.handle(command)
      debugger.running = false
      if line == nil then
        debugger.server = nil
      else
        local editor = GetEditor()
        editor:MarkerAdd(line-1, CURRENT_LINE_MARKER)
        editor:EnsureVisibleEnforcePolicy(line-1)
      end
    end)
  end
end

debugger.update = function() copas.step(0) end

debugger.listen()
