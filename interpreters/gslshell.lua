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
      -- add path to GSL-shell modules and templates/?.lua.in
      local gslpath = GetPathWithSep(gslshell)
      local luapath = gslpath.."gsl-shell/?.lua;"..gslpath.."gsl-shell/templates/?.lua.in"
      local luacpath = gslpath.."gsl-shell/?.dll"

      -- add GSL-shell modules to the end of LUA_PATH
      local _, path = wx.wxGetEnv("LUA_PATH")
      if path and not path:find(gslpath, 1, true) then
        wx.wxSetEnv("LUA_PATH", path..";"..luapath)
      end

      -- add GSL-shell modules to the beginning of LUA_CPATH to make luajit
      -- friendly luasocket to load before it loads luasocket shipped with ZBS
      local _, cpath = wx.wxGetEnv("LUA_CPATH")
      if cpath and not cpath:find(gslpath, 1, true) then
        wx.wxSetEnv("LUA_CPATH", luacpath..";"..cpath)
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
