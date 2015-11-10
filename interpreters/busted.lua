-- Copyright 2011-13 Paul Kulchenko, ZeroBrane LLC

local busted
local win = ide.osname == "Windows"

return {
  name = "Busted",
  description = "Busted Lua testing",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    busted = busted or ide.config.path.busted -- check if the path is configured
    if not busted then
      local sep = win and ';' or ':'
      local default =
           win and GenerateProgramFilesPath('LuaRocks\\systree\\bin', sep)..sep
        or ''
      local path = default
                 ..(os.getenv('PATH') or '')..sep
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        busted = busted or GetFullPathIfExists(p, win and 'busted.bat' or 'busted')
        table.insert(paths, p)
      end
      if not busted then
        DisplayOutputLn("Can't find busted executable in any of the folders in PATH: "
          ..table.concat(paths, ", "))
        return
      end
    end

    local file = wfilename:GetFullPath()
    local helper
    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart ~= false})
      local tmpfile = wx.wxFileName()
      tmpfile:AssignTempFileName(".")
      helper = tmpfile:GetFullPath()..".lua" -- busted likes .lua files more than .tmp files
      local f = io.open(helper, "w")
      if not f then
        DisplayOutputLn("Can't open temporary file '"..helper.."' for writing.")
        return 
      end
      f:write("require('mobdebug').start()")
      f:close()
    end

    local options = ide.config.busted and ide.config.busted.options or "--output=TAP"
    local cmd = ('"%s" %s %s "%s"'):format(busted, helper and "--helper="..helper or "", options, file)
     -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() if helper then wx.wxRemoveFile(helper) end end)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
