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
      local default = win and GenerateProgramFilesPath('gsl-shell', sep)..sep or ''
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
        DisplayOutputLn("Can't find gsl-shell executable in any of the following folders: "
          ..table.concat(paths, ", "))
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

    local filepath = wfilename:GetFullPath()
    if rundebug then
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})

      local tmpfile = wx.wxFileName()
      tmpfile:AssignTempFileName(".")
      filepath = tmpfile:GetFullPath()
      local f = io.open(filepath, "w")
      if not f then
        DisplayOutputLn("Can't open temporary file '"..filepath.."' for writing.")
        return
      end
      f:write(rundebug)
      f:close()
    else
      -- if running on Windows and can't open the file, this may mean that
      -- the file path includes unicode characters that need special handling
      local fh = io.open(filepath, "r")
      if fh then fh:close() end
      if ide.osname == 'Windows' and pcall(require, "winapi")
      and wfilename:FileExists() and not fh then
        winapi.set_encoding(winapi.CP_UTF8)
        filepath = winapi.short_path(filepath)
      end
    end
    local params = ide.config.arg.any or ide.config.arg.gslshell
    local code = ([[-e "io.stdout:setvbuf('no')" "%s"]]):format(filepath)
    local cmd = '"'..gslshell..'" '..code..(params and " "..params or "")

    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() if rundebug then wx.wxRemoveFile(filepath) end end)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  skipcompile = true,
  unhideanywindow = true,
  scratchextloop = false,
  takeparameters = true,
}
