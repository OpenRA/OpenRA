local gideros
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"

return {
  name = "Gideros",
  description = "Gideros mobile platform",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    gideros = gideros or ide.config.path.gideros -- check if the path is configured
    if not gideros then
      local sep = win and ';' or ':'
      local default =
           win and ([[C:\Program Files\Gideros]]..sep..[[D:\Program Files\Gideros]]..sep)
        or mac and ('/Applications/Gideros Studio/Gideros Player.app/Contents/MacOS'..sep)
        or ''
      local path = (os.getenv('PATH') or '')..sep
                 ..default
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        gideros = gideros or GetFullPathIfExists(p, win and 'GiderosPlayer.exe' or 'Gideros Player')
        table.insert(paths, p)
      end
      if not gideros then
        DisplayOutput("Can't find gideros executable in any of the folders in PATH: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end
    if gideros and not wx.wxFileName(gideros):FileExists() then
      DisplayOutput("Can't find the specified gideros executable '"..gideros.."'.\n")
      return
    end

    local giderostools = wx.wxFileName.DirName(wx.wxFileName(gideros)
      :GetPath(wx.wxPATH_GET_VOLUME)
      ..(win and '/Tools' or '/../../../Gideros Studio.app/Contents/Tools'))
    giderostools:Normalize()
    local giderospath = giderostools:GetPath(wx.wxPATH_GET_VOLUME)
    local gdrbridge = GetFullPathIfExists(giderospath, win and 'gdrbridge.exe' or 'gdrbridge')
    if not gdrbridge then
      DisplayOutput("Can't find gideros bridge executable in '"..giderospath.."'.\n")
      return
    end

    -- find *.gproj file in the project directory
    local file
    for _, proj in ipairs(FileSysGet(self:fworkdir(wfilename).."/*.gproj", wx.wxFILE)) do
      if file then
        DisplayOutput("Found multiple .gproj files in the project directory; ignored '"..proj.."'\n")
      end
      file = proj
    end
    if not file then
      DisplayOutput("Can't find gideros project file in the project directory.\n")
      return
    end

    if rundebug then DebuggerAttachDefault() end

    local cmd = ('"%s"'):format(gideros)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    local pid = CommandLineRun(cmd,self:fworkdir(wfilename),not mac,true,nil,nil,
      function() ide.debugger.pid = nil end)

    do
      DisplayOutput("Starting the player and waiting for the bridge to connect at '"..gdrbridge.."'.\n")
      local cmd = ('"%s" %s'):format(gdrbridge, 'isconnected')
      local attempts, connected = 10
      for _ = 1, attempts do
        local proc = wx.wxProcess()
        proc:Redirect()
        proc:Connect(wx.wxEVT_END_PROCESS, function(event) proc = nil end)
        local pid = wx.wxExecute(cmd, wx.wxEXEC_ASYNC + wx.wxEXEC_MAKE_GROUP_LEADER, proc)
        if not pid or pid == -1 or pid == 0 then
          DisplayOutput(("Program unable to run as '%s'\n"):format(cmd))
          return
        end

        local streamin = proc:GetInputStream()
        for _ = 1, 10 do
          if streamin:CanRead() then
            connected = tonumber(streamin:Read(4096)) == 1
            break end
          wx.wxSafeYield()
          wx.wxWakeUpIdle()
          wx.wxMilliSleep(250)
        end

        if connected then break end
        if connected == nil and proc then
          wx.wxProcess.Kill(pid, wx.wxSIGKILL, wx.wxKILL_CHILDREN)
          DisplayOutput("Couldn't connect to the player. Try again or check starting the player and the bridge manually.\n")
          return
        end
      end
      if not connected then
        DisplayOutput("Couldn't connect after "..attempts.." attempts. Try starting the player manually.\n")
        return
      end

      DisplayOutput("Starting project file '"..file.."'.\n")

      cmd = ('"%s" %s "%s"'):format(gdrbridge, 'play', file)
      wx.wxExecute(cmd, wx.wxEXEC_ASYNC)
    end
    return pid
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function(self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
