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
      local path = (os.getenv('PATH') or '')..sep
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        busted = busted or GetFullPathIfExists(p, win and 'busted.exe' or 'busted')
        table.insert(paths, p)
      end
      if not busted then
        DisplayOutputLn("Can't find busted executable in any of the folders in PATH: "
          ..table.concat(paths, ", "))
        return
      end
    end

    local file
    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})
      local code = (
[=[xpcall(function() io.stdout:setvbuf('no')
    require('mobdebug').start(); dofile [[%s]]
  end, function(err) print(debug.traceback(err)) end)]=])
        :format(wfilename:GetFullPath())
      local tmpfile = wx.wxFileName()
      tmpfile:AssignTempFileName(".")
      file = tmpfile:GetFullPath()
      local f = io.open(file, "w")
      if not f then
        DisplayOutputLn("Can't open temporary file '"..file.."' for writing.")
        return 
      end
      f:write(code)
      f:close()
    end

    file = file or wfilename:GetFullPath()

    local options = ide.config.busted and ide.config.busted.options
      or "--output=TAP"
    local cmd = ('"%s" %s "%s"'):format(busted, options, file)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() if rundebug then wx.wxRemoveFile(file) end end)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
