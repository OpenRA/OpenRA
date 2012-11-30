-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

local gslshell
local win = ide.osname == "Windows"

return {
  name = "GSL-shell",
  description = "GSL-shell interpreter",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    gslshell = gslshell or ide.config.path.gslshell -- check if the path is configured
    if not gslshell then
      local sep = win and ';' or ':'
      local default =
           win and ([[C:\Program Files\gsl-shell]]..sep..[[D:\Program Files\gsl-shell]]..sep..
                    [[C:\Program Files (x86)\gsl-shell]]..sep..[[D:\Program Files (x86)\gsl-shell]]..sep)
        or ''
      local path = default
                 ..(os.getenv('PATH') or '')..sep
                 ..(GetPathWithSep(self:fworkdir(wfilename)))..sep
                 ..(os.getenv('HOME') and GetPathWithSep(os.getenv('HOME'))..'bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        gslshell = gslshell or GetFullPathIfExists(p, win and 'gsl-shell.exe' or 'gsl-shell')
        table.insert(paths, p)
      end
      if not gslshell then
        DisplayOutput("Can't find gsl-shell executable in any of the following folders: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end

    do
      -- add templates/?.lua.in
      local luain = GetPathWithSep(gslshell).."templates/?.lua.in"
      local _, path = wx.wxGetEnv("LUA_PATH")
      if path and not path:find(luain, 1, true) then
        wx.wxSetEnv("LUA_PATH", path..";"..luain)
      end
    end

    if rundebug then DebuggerAttachDefault() end

    local cmd = ('"%s" "%s"'):format(gslshell, wfilename:GetFullPath())
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() ide.debugger.pid = nil end)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function(self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  skipcompile = true,
  unhideanywindow = true,
}
