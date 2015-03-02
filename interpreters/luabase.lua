function MakeLuaInterpreter(version, name)

local function exePath(self, version)
  local version = tostring(version):gsub('%.','')
  local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
  local macExe = mainpath..([[bin/lua.app/Contents/MacOS/lua%s]]):format(version)
  return ide.config.path['lua'..version]
     or (ide.osname == "Windows" and mainpath..([[bin\lua%s.exe]]):format(version))
     or (ide.osname == "Unix" and mainpath..([[bin/linux/%s/lua%s]]):format(ide.osarch, version))
     or (wx.wxFileExists(macExe) and macExe or mainpath..([[bin/lua%s]]):format(version))
end

return {
  name = ("Lua%s"):format(name or version or ""),
  description = ("Lua%s interpreter with debugger"):format(name or version or ""),
  api = {"baselib"},
  luaversion = version or '5.1',
  fexepath = exePath,
  frun = function(self,wfilename,rundebug)
    local exe = self:fexepath(version or "")
    local filepath = wfilename:GetFullPath()
    if rundebug then
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})

      -- update arg to point to the proper file
      rundebug = ('if arg then arg[0] = [[%s]] end '):format(filepath)..rundebug

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
    local params = ide.config.arg.any or ide.config.arg.lua
    local code = ([[-e "io.stdout:setvbuf('no')" "%s"]]):format(filepath)
    local cmd = '"'..exe..'" '..code..(params and " "..params or "")

    -- modify CPATH to work with other Lua versions
    local envname = "LUA_CPATH"
    if version then
      local env = "LUA_CPATH_"..string.gsub(version, '%.', '_')
      if os.getenv(env) then envname = env end
    end

    local cpath = os.getenv(envname)
    if rundebug and cpath and not ide.config.path['lua'..(version or "")] then
      -- prepend osclibs as the libraries may be needed for debugging,
      -- but only if no path.lua is set as it may conflict with system libs
      wx.wxSetEnv(envname, ide.osclibs..';'..cpath)
    end
    if version and cpath then
      local cpath = os.getenv(envname)
      local clibs = string.format('/clibs%s/', version):gsub('%.','')
      if not cpath:find(clibs, 1, true) then cpath = cpath:gsub('/clibs/', clibs) end
      wx.wxSetEnv(envname, cpath)
    end

    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    local pid = CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() if rundebug then wx.wxRemoveFile(filepath) end end)

    if (rundebug or version) and cpath then wx.wxSetEnv(envname, cpath) end
    return pid
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = false,
  unhideanywindow = true,
  takeparameters = true,
}

end

return nil -- as this is not a real interpreter
