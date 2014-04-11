function MakeLuaInterpreter(version, name)

local exe

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
  api = {"wxwidgets","baselib"},
  luaversion = version or '5.1',
  fexepath = exePath,
  frun = function(self,wfilename,rundebug)
    exe = exe or self:fexepath(version or "")
    local filepath = wfilename:GetFullPath()
    if rundebug then
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})

      local tmpfile = wx.wxFileName()
      tmpfile:AssignTempFileName(".")
      filepath = tmpfile:GetFullPath()
      local f = io.open(filepath, "w")
      if not f then
        DisplayOutput("Can't open temporary file '"..filepath.."' for writing\n")
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
    local clibs = ('/clibs%s/'):format(version and tostring(version):gsub('%.','') or '')
    local _, cpath = wx.wxGetEnv("LUA_CPATH")
    if version and cpath and not cpath:find(clibs, 1, true) then
      wx.wxSetEnv("LUA_CPATH", cpath:gsub('/clibs/', clibs)) end

    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    local pid = CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() if rundebug then wx.wxRemoveFile(filepath) end end)

    if version and cpath then wx.wxSetEnv("LUA_CPATH", cpath) end
    return pid
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = false,
  unhideanywindow = true,
  takeparameters = true,
}

end

return nil -- as this is not a real interpreter
