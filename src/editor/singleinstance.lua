-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--[[ single instance
open an UDP port - if it fails it is either because
- Estrela is running already
- an application is already blocking that port
if it fails it tries to contact the running application
- if it confirms being the estrela IDE we let that instance open it, finish our application
- otherwise we throw an error message on the user and start like normal

probably a pitfal: an estreala instance is running but is not visible (because it was finished
  though the UDP thing still runs)
]]

if not ide.config.singleinstance then return end

require "socket"

local port = ide.config.singleinstanceport

local svr = socket.udp()

local success, errmsg = svr:setsockname("127.0.0.1",port) -- bind on local host

local protocoll = {client = {}, server = {}}

protocoll.client.greeting = "Is this you, Estrela? It's me, a new instance."
protocoll.server.greeting = "Yes it is me, how may I serve you?"
protocoll.client.requestloading = "Could you please load this file for me: %s"
protocoll.server.answerok = "Sure. You may now leave."

if success then -- ok, server was started, we are solo
  --TODO: if multiple files are to be opened, each file is handled one by one - we could create a single string instead...
  ide.idletimer = wx.wxTimer(wx.wxGetApp())
  ide.idletimer:Start(200,false)
  svr:settimeout(0) -- don't block
  wx.wxGetApp():Connect(wx.wxEVT_TIMER,function (evt)
      if ide.exitingProgram then -- if exiting, terminate the timer loop
        wx.wxGetApp():Disconnect(wx.wxEVT_TIMER)
        return
      end

      local msg, err, port = svr:receivefrom() -- receive a msg
      if msg then
        local ip = err -- the errmsg is actually the IP
        -- DisplayOutput("client sent request: "..tostring(ip)..","..tostring(port).."\n")
        -- DisplayOutput("UDP.SingleInstanceServer: "..msg.."\n")
        if msg == protocoll.client.greeting then -- just send back hi
          svr:sendto(protocoll.server.greeting,ip,port)
        elseif msg:match(protocoll.client.requestloading:gsub("%%s",".+$")) then -- ok we need to open something
          local filename = msg:match(protocoll.client.requestloading:gsub("%%s","(.+)$"))
          -- DisplayOutput("UDP.SingleInstanceServer: open file "..filename.."\n")

          LoadFile(filename, nil, true)
          svr:sendto(protocoll.server.answerok,ip,port)

          ide.frame:RequestUserAttention() -- let's let the user know we want his attention

        end

      end
      --DisplayOutput("check "..os.clock().."\n")
    end)
else -- something different is running on our port
  local cln = socket.udp()
  cln:setpeername("127.0.0.1",port)
  cln:settimeout(5) -- two seconds of waiting should be enough, if no response, we asume we are running

  cln:send(protocoll.client.greeting)

  local msg,err = cln:receive()
  local arg = ide.arg
  if msg and msg == protocoll.server.greeting then
    local failed = false
    for index = 2, #arg do
      local fileName = arg[index]
      if fileName ~= "--" then
        cln:send(protocoll.client.requestloading:format(fileName))

        local msg,err = cln:receive()
        if msg~=protocoll.server.answerok then
          failed = true
          print(err,msg)
        else
        end
      end
    end
    if failed then
      print("The server instance failed to open the files, this instance will continue running.")
    else -- done
      os.exit(0)
    end
    --DisplayOutput("OK\n")
  else
    print("The Single instance communication has failed, it is not certain if this is the only running instance")
  end
end

--print("UDP Server: "..success.." - "..tostring(errmsg).."\n")
