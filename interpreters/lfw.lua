-- add `lfw = {chdirtofile = true}` to the configuration file to set file
-- directory as the current one when Running or Debugging LuaForWindows projects.

if ide.osname ~= "Windows" or not os.getenv("LUA_DEV") then return end

local exe

local function exePath()
  local defaultPath = ide.config.path.lfw or os.getenv("LUA_DEV")
  return MergeFullPath(defaultPath, 'lua.exe')
end

return {
  name = "LuaForWindows",
  description = "Lua For Windows interpreter with debugger",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    exe = exe or exePath()
    local filepath = wfilename:GetFullPath()
    local script
    if rundebug then
      DebuggerAttachDefault({basedir = self:fworkdir(wfilename),
        runstart = ide.config.debugger.runonstart == true})
      script = rundebug
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

      script = ('dofile [[%s]]'):format(filepath)
    end
    local code = ([[xpcall(function() io.stdout:setvbuf('no'); %s end,function(err) print(debug.traceback(err)) end)]]):format(script)
    local cmd = '"'..exe..'" -e "'..code..'"'

    -- add "LUA_DEV\clibs" to PATH to allow required DLLs to load
    local _, path = wx.wxGetEnv("PATH")
    local clibs = MergeFullPath(GetPathWithSep(exe), 'clibs')
    if path and not path:find(clibs, 1, true) then
      wx.wxSetEnv("PATH", path..';'..clibs)
    end

    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    local pid = CommandLineRun(cmd,self:fworkdir(wfilename),true,false)

    -- restore PATH
    wx.wxSetEnv("PATH", path)
    return pid
  end,
  fworkdir = function (self,wfilename)
    return (not ide.config.lfw or ide.config.lfw.chdirtofile ~= true)
      and ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = false,
  unhideanywindow = true,
}
