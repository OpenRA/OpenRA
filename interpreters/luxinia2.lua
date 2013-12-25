
return {
  name = "Luxinia2",
  description = "Luxinia2",
  api = {"baselib","glfw","glewgl","assimp20","luxmath","luxscene","luajit2",},

  frun = function(self,wfilename,rundebug)
    local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
    local luxDir = ide.config.path.luxinia2 or os.getenv("LUXINIA2")
    if (wx.wxFileName(luxDir):IsRelative()) then
      luxDir = editorDir..luxDir
    end
    
    if (not luxDir) then
      DisplayOutputLn("Error: path.luxinia2 not set in config or LUXINIA2 environment missing")
      return
    end
    local exe = luxDir.."/luajit.exe"
    
    local wdir = self:fworkdir(wfilename)
    if (wx.wxFileExists(wdir.."/main.lua")) then
      wfilename = wx.wxFileName(wdir.."/main.lua")
      DisplayOutputLn("luxinia2: using project main.lua")
    end

    local pid, proc
    if (CommandLineRunning(self:fuid(wfilename))) then
      -- kill process
      wx.wxProcess.Kill(pid)
    end
    

    local filepath = wfilename:GetFullName()
    local args = ide.config.luxinia2jitargs or ""
    if rundebug then
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})

      local script = "package.path=package.path..';"..editorDir.."lualibs/?/?.lua';"
      args = args..' -e "'..script..'" '
    end
    args = args..(rundebug 
      and ([[ -e "io.stdout:setvbuf('no'); %s"]]):format(rundebug)
       or ([[ -e "io.stdout:setvbuf('no')" "%s"]]):format(filepath))
    local cmd = '"'..exe..'" '..args

    local pid = CommandLineRun(cmd,wdir,true,true,nil,self:fuid(wfilename),
      function() ide.debugger.pid = nil end)
    
    return pid
  end,
  fuid = function(self,wfilename) return "luxinia2 "..wfilename:GetFullName() end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  --scratchextloop = false, -- not supported yet
}
