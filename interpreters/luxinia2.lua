
return {
  name = "Luxinia2",
  description = "Luxinia2",
  api = {"baselib","glfw","glewgl","assimp20","luxmath","luxscene","luajit2",},

  frun = function(self,wfilename,rundebug)
    local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
    local luxDir = ide.config.path.luxinia2 or os.getenv("LUXINIA2")
    local scratchpad = rundebug and rundebug:match("scratchpad")
    local filename = wfilename:GetFullName()
    
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
    
    if (scratchpad and filename ~= wfilename:GetFullName()) then
      DisplayOutputLn("luxinia2: scratchpad currently requires starting with main.lua (if exists)\n However, do not edit its content, but add other files to scratchpad.\n In general you should start with the file that hosts the initialization\n and main loop, then edit other files.")
      return
    end

    local pid, proc
    if (CommandLineRunning(self:fuid(wfilename))) then
      -- kill process
      wx.wxProcess.Kill(pid)
    end
    
    local filename = wfilename:GetFullName()
    local args = [[ -e "io.stdout:setvbuf('no');" ]]..(ide.config.luxinia2args or "")
    -- ensure luxinia's libs come first, to allow 32- and 64-bit debugging
    -- or running from zbstudio in general, as zbs modifies LUA_CPATH
    args = args..' -e "dofile [['..luxDir..'/../setup_package_paths.lua]];"'
      
    if rundebug then
      DebuggerAttachDefault({ runstart = ide.config.debugger.runonstart == true,
                              startwith = wfilename:GetFullPath(),
                            })
      if (scratchpad) then
        args = args..' -e "_IS_SCRATCH = true;"'
      else
        args = args..' -e "_IS_DEBUG = true;"'
      end
    end
    
    args = args..(rundebug 
      and ([[ -e "%s"]]):format(rundebug)
       or ([[ "%s"]]):format(filename))
       
    local cmd = '"'..exe..'" '..args

    local pid = CommandLineRun(cmd,wdir,true,true,nil,self:fuid(wfilename),
      function() ide.debugger.pid = nil end)
    
    return pid
  end,
  fuid = function(self,wfilename) return "luxinia2: luajit "..wfilename:GetFullName() end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = true,
}
