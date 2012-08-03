local moai
local win = ide.osname == "Windows"

local function fullNameIfExists(p, f)
  if not p or not f then return end
  local file = wx.wxFileName(p, f)
  return file:FileExists() and file:GetFullName()
end

return {
  name = "Moai",
  description = "Moai mobile platform",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    -- add check for MOAI_CONFIG
    if not moai then
      local sep = win and ';' or ':'
      local path = (os.getenv('PATH') or '')..sep..(os.getenv('MOAI_BIN') or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        moai = moai or fullNameIfExists(p, win and 'moai.exe' or 'moai')
        table.insert(paths, p)
      end
      moai = moai or ide.config.path.moai
      if not moai then
        DisplayOutput("Can't find moai executable in any of the folders in PATH or MOAI_BIN: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end
    local file = string.gsub(wfilename:GetFullPath(), "\\","/")
    if rundebug then
      DebuggerAttachDefault()
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
    end
    local config = fullNameIfExists(os.getenv('MOAI_CONFIG'), 'config.lua') or ''
    local cmd = ('"%s" %s"%s"'):format(string.gsub(moai, "\\","/"), config, file)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    -- use nohide=true on windows to show moai window
    -- TODO: remove when winapi provides ShowWindowAsync or PostMessage
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,win,nil,nil,
      function() ide.debugger.pid = nil if rundebug then wx.wxRemoveFile(file) end end)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = true,
}
