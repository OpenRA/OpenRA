local gideros
local win = ide.osname == "Windows"

return {
  name = "Gideros",
  description = "Gideros mobile platform",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
     if not win then
       DisplayOutput("Can't run the application. Gideros integration is currently only available on Windows.\n")
       return
    end

    gideros = gideros or ide.config.path.gideros -- check if the path is configured
    if not gideros then
      local sep = win and ';' or ':'
      local path = (os.getenv('PATH') or '')..sep
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        gideros = gideros or GetFullPathIfExists(p, win and 'GiderosPlayer.exe' or '')
        table.insert(paths, p)
      end
      if not gideros then
        DisplayOutput("Can't find gideros executable in any of the folders in PATH: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end

    local giderospath = wx.wxFileName(gideros):GetPath(wx.wxPATH_GET_VOLUME)
    local gdrbridge = GetFullPathIfExists(giderospath.."/Tools", win and 'gdrbridge.exe' or '')
    if not gdrbridge then
      DisplayOutput("Can't find gideros bridge executable in '"..giderospath.."/Tools'.\n")
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
    local pid = CommandLineRun(cmd,self:fworkdir(wfilename),true,not false,nil,nil,
      function() ide.debugger.pid = nil end)

    do
      DisplayOutput("Connecting to the player.\n")
      local cmd = ('"%s" %s'):format(gdrbridge, 'isconnected')
      local attempts, connected = 10
      for _ = 1, attempts do
        local proc = wx.wxProcess.Open(cmd)
        proc:Redirect()
        local streamin = proc:GetInputStream()
        if tonumber(streamin:Read(4096)) == 1 then connected = true; break end
        wx.wxSafeYield()
        wx.wxWakeUpIdle()
        wx.wxMilliSleep(300)
      end
      if not connected then
        DisplayOutput("Couldn't connect after "..attempts.." attempts. Try to start the player manually.\n")
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
