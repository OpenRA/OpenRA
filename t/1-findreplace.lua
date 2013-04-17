local editor = NewFile()
ok(editor, "Open New file.")

local search = "123"
editor:AddText(search..search.."\n"..search)

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FIND))
ok(ide.findReplace.dialog, "Open Find/Replace dialog.")

ide.findReplace.findText = search
ok(ide.findReplace:HasText(), "Update text to search.")

ide.findReplace:FindString()
ok(editor:GetSelectionStart() ~= editor:GetSelectionEnd(), "Find text with 'Find Next'.")

local selend = editor:GetSelectionEnd()
ide.findReplace:FindString()
ok(editor:GetSelectionStart() == selend, "'Find Next' doesn't skip consecutive matches.")

editor:GotoPos(0) -- reset current selection
local findnext = wx.wxUpdateUIEvent(ID_FINDNEXT)
ide.frame:ProcessEvent(findnext)
ok(findnext:GetEnabled(), "Quick find is enabled without current selection.")

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FINDNEXT))
ok(editor:GetSelectionEnd() == selend, "Quick find works based on previous search.")

-- cleanup
ide.findReplace.dialog:Hide()
editor:Undo()
ClosePage()
