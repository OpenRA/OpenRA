local output = ide:GetOutput()
local shell = ide:GetConsole()

local text = output:GetText()
output:SetReadOnly(false)
output:SetText("")
DisplayOutputLn(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
is(output:GetText(), '\022\022\022\n',
  "Output with invalid UTF-8 characters is displayed.")
output:SetText(text)
output:SetReadOnly(true)

local text = shell:GetText()
shell:SetText("")
DisplayShell(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
is(shell:GetText(), [[\128\129\130]].."\n",
  "Console output with invalid UTF-8 characters is displayed.")
shell:SetText(text)

