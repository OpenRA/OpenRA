local output = ide:GetOutput()
DisplayOutputLn(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
local cmptext, text = "\022\022\022\n", output:GetText()

local ro = output:GetReadOnly()
output:SetReadOnly(false)
output:SetTargetStart(output:GetLength()-1-#cmptext)
output:SetTargetEnd(output:GetLength()-1)
output:ReplaceTarget("")
output:SetReadOnly(ro)

is(text:sub(-#cmptext), cmptext, "Output with invalid UTF-8 characters is displayed.")

local shell = ide:GetConsole()
DisplayShell(string.char(0x80, 0x81, 0x82)) -- three invalid UTF-8 chars
local cmptext = [[\128\129\130]].."\n"
is(shell:GetText():sub(-#cmptext), cmptext,
  "Console output with invalid UTF-8 characters is displayed.")
shell:SetTargetStart(shell:GetLength()-1-#cmptext)
shell:SetTargetEnd(shell:GetLength()-1)
shell:ReplaceTarget("")
