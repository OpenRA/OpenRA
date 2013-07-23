local editor = NewFile()

editor:SetText('print("select")')
-- this is to set proper styles, which are needed for EditorCallTip
editor:Colourise(0, -1)

local value
local CTS = editor.CallTipShow
editor.CallTipShow = function(editor, pos, tip) value = tip end
EditorCallTip(editor, 10)
editor.CallTipShow = CTS

ok(value:find("print") and not value:find("select"),
  "Tooltip ignores values in strings.")

-- cleanup
ide:GetDocument(editor).isModified = false
ClosePage()
