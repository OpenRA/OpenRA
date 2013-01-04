local moai
local win = ide.osname == "Windows"

return {
  name = "Moai",
  description = "Moai mobile platform",
  api = {"baselib", "moai"},
  frun = function(self,wfilename,rundebug)
    moai = moai or ide.config.path.moai -- check if the path is configured
    if not moai then
      local sep = win and ';' or ':'
      local default =
           win and ([[C:\Program Files\moai]]..sep..[[D:\Program Files\moai]]..sep..
                    [[C:\Program Files (x86)\moai]]..sep..[[D:\Program Files (x86)\moai]]..sep)
        or ''
      local path = default
                 ..(os.getenv('PATH') or '')..sep
                 ..(os.getenv('MOAI_BIN') or '')..sep
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        moai = moai or GetFullPathIfExists(p, win and 'moai.exe' or 'moai')
        table.insert(paths, p)
      end
      if not moai then
        DisplayOutput("Can't find moai executable in any of the folders in PATH or MOAI_BIN: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end

    local file
    local epoints = ide.config.moai and ide.config.moai.entrypoints
    if epoints then
      epoints = type(epoints) == 'table' and epoints or {epoints}
      for _,entry in pairs(epoints) do
        file = GetFullPathIfExists(self:fworkdir(wfilename), entry)
        if file then break end
      end
      if not file then
        DisplayOutput("Can't find any of the specified entry points ("
          ..table.concat(epoints, ", ")
          ..") in the current project; continuing with the current file...\n")
      end
    end

    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({runstart=true, startwith = file})
      local code = (
[[xpcall(function() 
    io.stdout:setvbuf('no')
    require("mobdebug").moai() -- enable debugging for coroutines
    %s
  end, function(err) print(debug.traceback(err)) end)]]):format(rundebug)
      local tmpfile = wx.wxFileName()
      tmpfile:AssignTempFileName(".")
      file = tmpfile:GetFullPath()
      local f = io.open(file, "w")
      if not f then
        DisplayOutput("Can't open temporary file '"..file.."' for writing\n")
        return 
      end
      f:write(code)
      f:close()

      -- add mobdebug as the first path to LUA_PATH to provide a workaround
      -- for a MOAI issue: https://github.com/pkulchenko/ZeroBraneStudio/issues/96
      local mdb = MergeFullPath(GetPathWithSep(ide.editorFilename), "lualibs/mobdebug/?.lua")
      local _, path = wx.wxGetEnv("LUA_PATH")
      if path and path:find(mdb, 1, true) ~= 1 then
        wx.wxSetEnv("LUA_PATH", mdb..";"..path)
      end
    end

    file = file or wfilename:GetFullPath()

    -- try to find a config file: (1) MOAI_CONFIG, (2) project directory,
    -- (3) folder with the current file, (4) folder with moai executable
    local config = GetFullPathIfExists(os.getenv('MOAI_CONFIG') or self:fworkdir(wfilename), 'config.lua')
      or GetFullPathIfExists(wfilename:GetPath(wx.wxPATH_GET_VOLUME), 'config.lua')
      or GetFullPathIfExists(wx.wxFileName(moai):GetPath(wx.wxPATH_GET_VOLUME), 'config.lua')
    local cmd = config and ('"%s" "%s" "%s"'):format(moai, config, file)
      or ('"%s" "%s"'):format(moai, file)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() ide.debugger.pid = nil if rundebug then wx.wxRemoveFile(file) end end)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function(self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = true,
}
