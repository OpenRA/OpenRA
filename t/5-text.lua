local output = ide:GetOutput()
local shell = ide:GetConsole()

local text = output:GetText()
local ro = output:GetReadOnly()
output:SetReadOnly(false)
DisplayOutputLn(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
local cmptext1, cmptext2 = output:GetText(), "\022\022\022\n"
output:SetTargetStart(output:GetLength()-1-#cmptext2)
output:SetTargetEnd(output:GetLength()-1)
output:ReplaceTarget("")
output:SetReadOnly(ro)
is(cmptext1:sub(-#cmptext2), cmptext2, "Output with invalid UTF-8 characters is displayed.")

local text = shell:GetText()
shell:SetText("")
DisplayShell(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
is(shell:GetText(), [[\128\129\130]].."\n",
  "Console output with invalid UTF-8 characters is displayed.")
shell:SetText(text)

