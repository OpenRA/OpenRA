local editor = NewFile()

editor:SetText('print("select")')
-- this is to set proper styles, which are needed for EditorCallTip
editor:Colourise(0, -1)

local value = ''
local CTS = editor.CallTipShow
editor.CallTipShow = function(editor, pos, tip) value = tip end
EditorCallTip(editor, 10)
editor.CallTipShow = CTS

ok(value:find("print") and not value:find("select"),
  "Tooltip ignores values in strings.")

local api = {
 t = {
  childs = {
   func = {
    args = "(args)",
    description = [[Description Line1
Line2

Line4
	Line5
	
	Line7]],
    returns = "(returns)",
    type = "function"
   }
  }
 }
}

ide:AddAPI("lua", "test", api)
table.insert(ide.interpreter.api, "test")
ReloadLuaAPI()
local tooltip = GetTipInfo(editor, "t.func(")
ok(tooltip:find("Line1\nLine2"), "Newlines are shown as newlines in tooltip")
ok(tooltip:find("Line2\n\nLine4"), "Empty lines are shown as empty lines in tooltip")
ok(tooltip:find("Line4\n\tLine5"), "Newlines with indentation are shown as lines with indentation in tooltip")
ok(tooltip:find("Line5\n\t\n\tLine7"), "Empty lines with indentation are shown in tooltip")
ide:RemoveAPI("lua", "test")
ReloadLuaAPI()

-- cleanup
ide:GetDocument(editor).isModified = false
ClosePage()
