return {
  name = "Luxinia",
  description = "Luxinia project",
  api = {"luxiniaapi","baselib"},
  frun = function(self,wfilename,withdebug)
    if not ide.config.path.luxinia then wx.wxMessageBox("Please define 'path.luxinia' in your cfg/user.lua (see estrela.lua for examples)"); return end
    local projdir = ide.config.path.projectdir
    local args = (projdir and projdir:len()>0
      and " -p "..projdir or "")

    local fname = wfilename:GetFullName()
    args = args..(fname and (" -t "..fname) or "")
    
    if withdebug and ide.config.luxinia1debug then
      DebuggerAttachDefault({
          basedir=projdir,
          run=true, }
      )
      local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
      script = ""..
      "package.path=package.path..';"..editorDir.."lualibs/?/?.lua';"..
      "io.stdout:setvbuf('no'); mobdebug = require('mobdebug'); mobdebug.start('" .. ide.debugger.hostname.."',"..ide.debugger.portnumber..");"..
      "jit.debug();mobdebug.off();"

      args = args..' -b "'..script..'"'
    end

    local cmd = 'luxinia.exe --nologo'..args
    CommandLineRun(cmd,ide.config.path.luxinia,true,true)
  end,
  fuid = function(self,wfilename) return "luxinia "..(ide.config.path.projectdir or "") end,
  fprojdir = function(self,wfilename)
    local path = GetPathWithSep(wfilename)
    fname = wx.wxFileName(path)

    while ((not wx.wxFileExists(path.."main.lua")) and (fname:GetDirCount() > 0)) do
      fname:RemoveDir(fname:GetDirCount()-1)
      path = GetPathWithSep(fname)
    end

    return path:sub(0,-2)
  end,
  hasdebugger = ide.config.luxinia1debug or false,
}
