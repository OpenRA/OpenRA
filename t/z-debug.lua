local pkg = ...
local debugger = ide:GetDebugger()
-- start debugger server
debugger:Listen()
-- save a test file and then load it
local debugfile = MergeFullPath(wx.wxFileName.GetCwd(), "debug.lua")
FileWrite(debugfile, "print('step 1')\nprint('step 2')\n")
ok(wx.wxFileExists(debugfile), "File created before starting debugging.")
local editor = ActivateFile(debugfile)

ide:GetMenuBar():Check(ID_CLEAROUTPUT, false)
ProjectDebug()

local commands = {
  {debugfile, 1, "Step"},
  {debugfile, 2, "Step"},
}
local command = 1

-- wait for the connection to be initiated
pkg.onDebuggerActivate = function(self, debugger, file, line)
  if not commands[command] then
    debugger:Step()
    return
  end
  local afile, aline, cmd = unpack(commands[command])
  is(file, afile, "Filename is reported as expected after debugger activation ("..command.."/"..#commands..")")
  is(line, aline, "Line number is reported as expected after debugger activation ("..command.."/"..#commands..")")
  debugger[cmd](debugger)
  command = command + 1
end
pkg.onDebuggerClose = function()
  local doc = ide:IsValidCtrl(editor) and ide:GetDocument(editor)
  if doc then doc:Close() end
  FileRemove(debugfile)
  ok(not wx.wxFileExists(debugfile), "File removed after completing debugging.")

  pkg.onDebuggerActivate = nil
  pkg.onDebuggerClose = nil

  pkg:report()
end
