
return {
  name = "Luxinia2",
  description = "Luxinia2",
  api = {"baselib","glfw","glewgl","assimp20","luxmath","luxgfx","luxscene","luajit2",},

  finitclient = function(self)
    if (not CommandLineRunning(self:fuid(wfilename))) then return end
    if not ide.config.path.luxinia2 then wx.wxMessageBox("Please define 'path.luxinia2' in your cfg/user.lua (see estrela.lua for examples)"); return end
    local init = dofile(ide.config.path.luxinia2.."/../comserver/client.lua")
    local fenv = {}
    setmetatable(fenv,{__index = _G})
    fenv.print = function(...) DisplayOutput(...); DisplayOutput("\n"); end

    setfenv(init,fenv)
    local client = init()

    self.fclient = client
    return client
  end,

  frun = function(self,wfilename,rundebug)
    local luxdir = ide.config.path.luxinia2
    local projdir = ide.config.path.projectdir
    assert(projdir and projdir:len()>0,"no project directory")
    local basedir = luxdir
    local startfile = projdir.."/main.lua"
    local startargs = " -e "..startfile

    if (CommandLineRunning(self:fuid(wfilename))) then
      if (not self.fclient) then
        self:finitclient()
      end
      -- try to communicate with server
      self.fclient("dofile([["..wfilename:GetFullPath().."]])")
      return
    end

    self.fclient = nil
    local fname = wfilename:GetFullName()
    local args = (fname and (" -f "..fname) or "")

    if rundebug then
      DebuggerAttachDefault({
          basedir=basedir,
          startfile=startfile,
          run=true, noshell=true,}
      )
      local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
      script = ""..
      "package.path=package.path..';"..editorDir.."lualibs/?/?.lua';"..
      "io.stdout:setvbuf('no'); require('mobdebug').start('" .. ide.debugger.hostname.."',"..ide.debugger.portnumber..")"

      args = args..' -es "'..script..'"'..startargs
    else
      args = " -s "..args..startargs
    end

    local jitargs = ide.config.luxinia2jitargs
    jitargs = jitargs or ""
    local cmd = 'luajit.exe '..jitargs..' ../main.lua '..args


    local pid = CommandLineRun(cmd,ide.config.path.luxinia2,true,true,nil,self:fuid(wfilename),
      function()
        ShellSupportRemote(nil)
        if (rundebug) then
          DebuggerStop()
        end
      end)
    
    if(not pid) then return end

    if not rundebug then
      local client = self:finitclient()
      ShellSupportRemote(client,self:fuid(wfilename))
      pid = nil
    end
    
    return pid
  end,
  fuid = function(self,wfilename) return "luxinia2 "..(ide.config.path.projectdir or "") end,
  fprojdir = function(self,wfilename)
    local path = GetPathWithSep(wfilename)
    filepath = wx.wxFileName(path)

    while ((not wx.wxFileExists(path.."main.lua")) and (filepath:GetDirCount() > 0)) do
      filepath:RemoveDir(filepath:GetDirCount()-1)
      path = GetPathWithSep(filepath)
    end

    return path:sub(0,-2)
  end,
  hasdebugger = true,
}
