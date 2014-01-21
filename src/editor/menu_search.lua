-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- Create the Search menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar

local findReplace = ide.findReplace

local findMenu = wx.wxMenu{
  { ID_FIND, TR("&Find")..KSC(ID_FIND), TR("Find text") },
  { ID_FINDNEXT, TR("Find &Next")..KSC(ID_FINDNEXT), TR("Find the next text occurrence") },
  { ID_FINDPREV, TR("Find &Previous")..KSC(ID_FINDPREV), TR("Find the earlier text occurence") },
  { ID_FINDSELECTNEXT, TR("Select and Find Next")..KSC(ID_FINDSELECTNEXT), TR("Select the word under cursor and find its next occurrence") },
  { ID_FINDSELECTPREV, TR("Select and Find Previous")..KSC(ID_FINDSELECTPREV), TR("Select the word under cursor and find its previous occurrence") },
  { ID_REPLACE, TR("&Replace")..KSC(ID_REPLACE), TR("Find and replace text") },
  { },
  { ID_FINDINFILES, TR("Find &In Files")..KSC(ID_FINDINFILES), TR("Find text in files") },
  { ID_REPLACEINFILES, TR("Re&place In Files")..KSC(ID_REPLACEINFILES), TR("Find and replace text in files") },
  { },
  { ID_GOTOLINE, TR("&Goto Line")..KSC(ID_GOTOLINE), TR("Go to a selected line") },
}
menuBar:Append(findMenu, TR("&Search"))

local function onUpdateUISearchMenu(event) event:Enable(GetEditor() ~= nil) end

frame:Connect(ID_FIND, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:Show(false)
  end)
frame:Connect(ID_FIND, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

frame:Connect(ID_REPLACE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:Show(true)
  end)
frame:Connect(ID_REPLACE, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

frame:Connect(ID_FINDINFILES, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:Show(false,true)
  end)
frame:Connect(ID_REPLACEINFILES, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:Show(true,true)
  end)

frame:Connect(ID_FINDNEXT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    if editor and ide.wxver >= "2.9.5" and editor:GetSelections() > 1 then
      local selection = editor:GetMainSelection() + 1
      if selection >= editor:GetSelections() then selection = 0 end
      editor:SetMainSelection(selection)
      editor:EnsureCaretVisible()
    else
      if findReplace:GetSelectedString() or findReplace:HasText() then
        findReplace:FindString()
      else
        findReplace:Show(false)
      end
    end
  end)
frame:Connect(ID_FINDNEXT, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

frame:Connect(ID_FINDPREV, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    if editor and ide.wxver >= "2.9.5" and editor:GetSelections() > 1 then
      local selection = editor:GetMainSelection() - 1
      if selection < 0 then selection = editor:GetSelections() - 1 end
      editor:SetMainSelection(selection)
      editor:EnsureCaretVisible()
    else
      if findReplace:GetSelectedString() or findReplace:HasText() then
        findReplace:FindString(true) -- search up
      else
        findReplace:Show(false)
      end
    end
  end)
frame:Connect(ID_FINDPREV, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

-- Select and Find behaves like Find if there is a current selection;
-- if not, it selects a word under cursor (if any) and does find.

local function selectWordUnderCaret(editor)
  local pos = editor:GetCurrentPos()
  local text = editor:GetTextRange( -- try to select a word under caret
    editor:WordStartPosition(pos, true), editor:WordEndPosition(pos, true))
  return #text > 0 and text or editor:GetTextRange( -- try to select a non-word under caret
      editor:WordStartPosition(pos, false), editor:WordEndPosition(pos, false))
end
frame:Connect(ID_FINDSELECTNEXT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    if editor:GetSelectionStart() ~= editor:GetSelectionEnd() then
      ide.frame:AddPendingEvent(
      wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FINDNEXT))
      return
    end

    local text = selectWordUnderCaret(editor)
    if #text > 0 then
      findReplace.findText = text
      findReplace:FindString()
    end
  end)
frame:Connect(ID_FINDSELECTNEXT, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

frame:Connect(ID_FINDSELECTPREV, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    if editor:GetSelectionStart() ~= editor:GetSelectionEnd() then
      ide.frame:AddPendingEvent(
      wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, ID_FINDPREV))
      return
    end

    local text = selectWordUnderCaret(editor)
    if #text > 0 then
      findReplace.findText = text
      findReplace:FindString(true)
    end
  end)
frame:Connect(ID_FINDSELECTPREV, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)

-------------------- Find replace end

frame:Connect(ID_GOTOLINE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local linecur = editor:LineFromPosition(editor:GetCurrentPos())
    local linemax = editor:LineFromPosition(editor:GetLength()) + 1
    local linenum = wx.wxGetNumberFromUser(TR("Enter line number"),
      "1 .. "..tostring(linemax),
      TR("Goto Line"),
      linecur, 1, linemax,
      frame)
    if linenum > 0 then
      editor:GotoLine(linenum-1)
    end
  end)
frame:Connect(ID_GOTOLINE, wx.wxEVT_UPDATE_UI, onUpdateUISearchMenu)
