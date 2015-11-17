local output = ide:GetOutput()
-- three invalid UTF-8 chars
local invalid = "\128\129\130"
DisplayOutputLn(invalid)
local cmptext, text = "\022\022\022\n", output:GetTextDyn()

local ro = output:GetReadOnly()
output:SetReadOnly(false)
output:SetTargetStart(output:GetLength()-1-#cmptext)
output:SetTargetEnd(output:GetLength()-1)
output:ReplaceTarget("")
output:SetReadOnly(ro)

is(text:sub(-#cmptext), output.useraw and invalid.."\n" or cmptext,
  "Output with invalid UTF-8 characters is displayed.")

local shell = ide:GetConsole()
DisplayShell(invalid)
local cmptext = [[\128\129\130]].."\n"
local outlength = shell.useraw and #invalid+1 or #cmptext
-- when Raw is used, the output is as-is, otherwise invalid chars are shown as \code
is(shell:GetTextDyn():sub(-outlength), shell.useraw and invalid.."\n" or cmptext,
  "Console output with invalid UTF-8 characters is displayed.")
shell:SetTargetStart(shell:GetLength()-1-outlength)
shell:SetTargetEnd(shell:GetLength()-1)
shell:ReplaceTarget("")
