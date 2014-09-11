local findReplace = ide.findReplace

local editor = NewFile()
ok(editor, "Open New file.")

local search = "123"
local replace = "4"
editor:AppendText(search..search.."\n"..search..search)

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FIND))
ok(findReplace.dialog, "Open Find/Replace dialog.")

findReplace.findText = search
ok(findReplace:HasText(), "Update text to search.")

findReplace:FindString()
ok(editor:GetSelectionStart() ~= editor:GetSelectionEnd(), "Find text with Find Next.")

local selend = editor:GetSelectionEnd()
findReplace:FindString()
is(editor:GetSelectionStart(), selend, "Find Next doesn't skip consecutive matches.")

editor:GotoPos(0) -- reset current selection
local findnext = wx.wxUpdateUIEvent(ID_FINDNEXT)
ide.frame:ProcessEvent(findnext)
ok(findnext:GetEnabled(), "Quick find is enabled without current selection.")

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FINDNEXT))
is(editor:GetSelectionEnd(), selend, "Quick Find works based on previous search.")

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FINDNEXT))
is(editor:GetSelectionStart(), selend, "Quick Find finds next match.")

-- check that text in "find" control is checked against selection with replacing
findReplace.findText = "something else"
findReplace.replaceText = replace
findReplace.foundString = true
findReplace:ReplaceString()
is(select(2, editor:GetText():gsub(search, search)), 4, "Replace doesn't replace selection that doesn't match 'find' text.")

-- restore position and selection
editor:GotoPos(4) -- reset current selection
findReplace.findText = search
findReplace:FindString()

-- replace the text once
findReplace:ReplaceString()
local _, replacements = editor:GetText():gsub(replace, replace)
is(replacements, 1, "Replace replaces once.")

-- replace the current text and to the end of file
editor:GotoPos(3)
findReplace.fWrap = false
findReplace:ReplaceString(true)
local _, replacements = editor:GetText():gsub(replace, replace)
is(replacements, 3, "Replace All without wrapping replaces to the end of file.")

local expected = search..replace.."\n"..replace..replace
is(editor:GetText(), expected, "Replace All with Wrap Around result is as expected.")

-- start after the match to test wrapping
editor:AppendText("\n"..search..search)
editor:GotoPos(3)
findReplace.fWrap = true
findReplace:ReplaceString(true)
ok(not editor:GetText():find(search), "Replace All with Wrap Around replaces everything.")

local expected = replace..replace.."\n"..replace..replace.."\n"..replace..replace
is(editor:GetText(), expected, "Replace All without Wrap Around result is as expected.")

-- cleanup
ide.findReplace.dialog:Hide()
while editor:CanUndo() do editor:Undo() end
ClosePage()
