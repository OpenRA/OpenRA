local pkg = ...
local unpack = table.unpack or unpack
local debugger = ide:GetDebugger()
-- start debugger server
debugger:Listen()
-- save a test file and then load it
local debugfile = MergeFullPath(wx.wxFileName.GetCwd(), "debug.lua")
FileWrite(debugfile, "local a = 1+2\na = 2+3\na = 3+4\na = 4+5\n")
ok(wx.wxFileExists(debugfile), "File created before starting debugging.")
local editor = ActivateFile(debugfile)

ide:GetMenuBar():Check(ID_CLEAROUTPUT, false)
ProjectDebug()

local commands = {
  {debugfile, 1, "Step"},
  {debugfile, 2, "RunTo", {editor, 4-1}},
  {debugfile, 4, "Run"},
}
local command = 1

-- wait for the connection to be initiated
pkg.onDebuggerActivate = function(self, debugger, file, line)
  if not commands[command] then
    debugger:Step()
    return
  end
  local afile, aline, cmd, args = unpack(commands[command])
  is(file, afile, "Filename is reported as expected after debugger activation ("..command.."/"..#commands..")")
  is(line, aline, "Line number is reported as expected after debugger activation ("..command.."/"..#commands..")")
  if debugger:IsRunning() then debugger:wait() end
  debugger[cmd](debugger, unpack(args or {}))
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
