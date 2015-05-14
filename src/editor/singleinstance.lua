-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
--[[ single instance
open an UDP port - if it fails it is either because
- IDE is running already
- an application is already blocking that port
if it fails it tries to contact the running application
- if it confirms being the IDE we let that instance open it, finish our application
- otherwise we throw an error message on the user and start like normal

probably a pitfal: an instance is running but is not visible
  (because it was finished though the UDP thing still runs)
]]

if not ide.config.singleinstance then return end

local socket = require "socket"
local port = ide.config.singleinstanceport
local delay = tonumber(ide.config.singleinstance) or 1000 -- in ms
local svr = socket.udp()
local success = svr:setsockname("127.0.0.1",port) -- bind on local host
local protocol = {client = {}, server = {}}

protocol.client.greeting = "Is this you, my IDE? It's me, a new instance."
protocol.server.greeting = "Yes it is me, how may I serve you?"
protocol.client.requestloading = "Could you please load this file for me: %s"
protocol.server.answerok = "Sure. You may now leave."

if success then -- ok, server was started, we are solo
  --TODO: if multiple files are to be opened, each file is handled one by one - we could create a single string instead...
  ide.idletimer = wx.wxTimer(wx.wxGetApp())
  ide.idletimer:Start(delay,false)
  svr:settimeout(0) -- don't block
  wx.wxGetApp():Connect(wx.wxEVT_TIMER, function()
      if ide.exitingProgram then -- if exiting, terminate the timer loop
        wx.wxGetApp():Disconnect(wx.wxEVT_TIMER)
        return
      end

      local msg, ip, port = svr:receivefrom() -- receive a msg
      if msg then
        if msg == protocol.client.greeting then -- just send back hi
          svr:sendto(protocol.server.greeting,ip,port)
        elseif msg:match(protocol.client.requestloading:gsub("%%s",".+$")) then -- ok we need to open something
          svr:sendto(protocol.server.answerok,ip,port)
          local filename = msg:match(protocol.client.requestloading:gsub("%%s","(.+)$"))
          if filename then
            RequestAttention()
            if wx.wxDirExists(filename) then
              ProjectUpdateProjectDir(filename)
            elseif not ActivateFile(filename) then
              DisplayOutputLn(TR("Can't open file '%s': %s"):format(filename, wx.wxSysErrorMsg()))
            end
          end
        end
      end
    end)
else -- something different is running on our port
  local cln = socket.udp()
  cln:setpeername("127.0.0.1",port)
  cln:settimeout(2)
  cln:send(protocol.client.greeting)

  local msg = cln:receive()
  local arg = ide.arg
  if msg and msg == protocol.server.greeting then
    local failed = false
    for index = 2, #arg do
      local fileName = arg[index]
      if fileName ~= "--"
      -- on OSX, the command line includes -psn parameter, so ignore it
      and (ide.osname ~= 'Macintosh' or not fileName:find("^-psn")) then
        cln:send(protocol.client.requestloading:format(fileName))

        local msg, err = cln:receive()
        if msg ~= protocol.server.answerok then
          failed = true
          print(err,msg)
        end
      end
    end
    if failed then
      print("The server instance failed to open the files, this instance will continue running.")
    else -- done
      os.exit(0)
    end
  else
    print("The single instance communication has failed; there may be another instance running, which may cause interference with the debugger.")
  end
end
