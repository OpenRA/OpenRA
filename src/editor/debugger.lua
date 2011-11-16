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

debugger.connect = function () 
  local server = socket.bind("*", debugger.portnumber)
  DisplayOutput("Started server on " .. debugger.portnumber .. "\n")
  copas.autoclose = false
  copas.addserver(server, function (skt)
    local client = copas.wrap(skt)

    DisplayOutput("Client connected to "..wx.wxGetHostName()..":"..debugger.portnumber.."\n")

    debugger.server = client
    debugger.running = true

    client:send("STEP\n")
    client:receive()
    client:receive()

    debugger.running = false
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
