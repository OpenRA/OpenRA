local exe

local function exePath()
  local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
  local os = ide.osname

  local macExe = mainpath..'bin/lua.app/Contents/MacOS/lua'
  local exe = ide.config.path.lua or
    (os == "Windows" and mainpath..[[bin\lua]]
     or (wx.wxFileExists(macExe) and macExe or mainpath..'bin/lua'))

  -- (luaconf.h) in Windows, any exclamation mark ('!') in the path is replaced
  -- by the path of the directory of the executable file of the current process.
  -- this effectively prevents any path with an exclamation mark from working.
  -- if the path has an excamation mark, we allow Lua to expand it
  -- (for use in LUA_PATH/LUA_CPATH)
  if os == "Windows" and mainpath:find('%!') then mainpath = "!/../" end

  local clibs =
    os == "Windows" and mainpath.."bin/?.dll;"..mainpath.."bin/clibs/?.dll" or
    os == "Macintosh" and mainpath.."bin/lib?.dylib;"..mainpath.."bin/clibs/?.dylib" or
    os == "Unix" and mainpath.."bin/?.so;"..mainpath.."bin/clibs/?.so" or nil
  wx.wxSetEnv("LUA_PATH", package.path .. ';'
    .. mainpath.."lualibs/?/?.lua;"..mainpath.."lualibs/?.lua")
  if clibs then wx.wxSetEnv("LUA_CPATH", package.cpath .. ';' .. clibs) end
  return exe
end

return {
  name = "Lua with Debugger",
  description = "Lua interpreter with debugger",
  api = {"wxwidgets","baselib"},
  frun = function(self,wfilename,rundebug)
    exe = exe or exePath()
    local filepath = wfilename:GetFullPath()
    local script
    if rundebug then
      DebuggerAttachDefault()
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
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() ide.debugger.pid = nil end)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
