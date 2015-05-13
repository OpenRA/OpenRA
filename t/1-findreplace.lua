local findReplace = ide.findReplace

local editor = NewFile()
ok(editor, "Open New file.")

local search = "123"
local replace = "4"
editor:AppendText(search..search.."\n"..search..search)

ide.frame:ProcessEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FIND))
ok(findReplace.panel, "Open Find/Replace panel.")

findReplace:SetFind(search)
ok(findReplace:HasText(), "Update text to search.")

findReplace:Find()
ok(editor:GetSelectionStart() ~= editor:GetSelectionEnd(), "Find text with Find Next.")

local selend = editor:GetSelectionEnd()
findReplace:Find()
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
findReplace:SetFind("something else")
findReplace:SetReplace(replace)
findReplace.foundString = true
findReplace:Replace()
is(select(2, editor:GetText():gsub(search, search)), 4, "Replace doesn't replace selection that doesn't match 'find' text.")

-- restore position and selection
editor:GotoPos(4) -- reset current selection
findReplace:SetFind(search)
findReplace:Find()

-- replace the text once
findReplace:Replace()
local _, replacements = editor:GetText():gsub(replace, replace)
is(replacements, 1, "Replace replaces once.")

-- replace the current text and to the end of file
editor:GotoPos(3)
findReplace:GetFlags().Wrap = false
findReplace:Replace(true)
local _, replacements = editor:GetText():gsub(replace, replace)
is(replacements, 3, "Replace All without wrapping replaces to the end of file.")

local expected = search..replace.."\n"..replace..replace
is(editor:GetText(), expected, "Replace All with Wrap Around result is as expected.")

-- start after the match to test wrapping
editor:AppendText("\n"..search..search)
editor:GotoPos(3)
findReplace:GetFlags().Wrap = true
findReplace:Replace(true)
ok(not editor:GetText():find(search), "Replace All with Wrap Around replaces everything.")

local expected = replace..replace.."\n"..replace..replace.."\n"..replace..replace
is(editor:GetText(), expected, "Replace All without Wrap Around result is as expected.")

-- check that the replacement only happens in the matched text in preview
editor:SetText("1: 123")
findReplace:SetFind("1")
findReplace:SetReplace("9")
findReplace:Replace(true, editor)
ok(editor:GetText():find("923") ~= nil, "Replace in preview replaces matched text.")
ok(editor:GetText():find("^1:") ~= nil, "Replace in preview doesn't replace line numbers.")

editor:SetText("")
editor:AppendText([[
t/1-findreplace.lua
99999: some text
]])
editor.searchpreview = true
editor.replace = true
local FILE_MARKER = ide:GetMarker("searchmatchfile")
editor:MarkerAdd(0, FILE_MARKER)
ide:GetDocument(editor):Save()
is(editor:GetText():match("Updated %d"), "Updated 0", "Replace fails on invalid line numbers.")

-- cleanup
findReplace.panel:Hide()
while editor:CanUndo() do editor:Undo() end
ide:GetDocument(editor):SetModified(false)
ClosePage()
