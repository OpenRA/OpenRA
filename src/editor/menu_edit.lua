-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- ---------------------------------------------------------------------------
-- Create the Edit menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar

local editMenu = wx.wxMenu{
  { ID_CUT, TR("Cu&t")..KSC(ID_CUT), TR("Cut selected text to clipboard") },
  { ID_COPY, TR("&Copy")..KSC(ID_COPY), TR("Copy selected text to clipboard") },
  { ID_PASTE, TR("&Paste")..KSC(ID_PASTE), TR("Paste text from the clipboard") },
  { ID_SELECTALL, TR("Select &All")..KSC(ID_SELECTALL), TR("Select all text in the editor") },
  { },
  { ID_UNDO, TR("&Undo")..KSC(ID_UNDO), TR("Undo last edit") },
  { ID_REDO, TR("&Redo")..KSC(ID_REDO), TR("Redo last edit undone") },
  { },
  { ID_SHOWTOOLTIP, TR("Show &Tooltip")..KSC(ID_SHOWTOOLTIP), TR("Show tooltip for current position; place cursor after opening bracket of function") },
  { ID_AUTOCOMPLETE, TR("Complete &Identifier")..KSC(ID_AUTOCOMPLETE), TR("Complete the current identifier") },
  { ID_AUTOCOMPLETEENABLE, TR("Auto Complete Identifiers")..KSC(ID_AUTOCOMPLETEENABLE), TR("Auto complete while typing"), wx.wxITEM_CHECK },
  { },
  { ID_COMMENT, TR("C&omment/Uncomment")..KSC(ID_COMMENT), TR("Comment or uncomment current or selected lines") },
  { },
  { ID_FOLD, TR("&Fold/Unfold All")..KSC(ID_FOLD), TR("Fold or unfold all code folds") },
  { ID_CLEARDYNAMICWORDS, TR("Clear &Dynamic Words")..KSC(ID_CLEARDYNAMICWORDS), TR("Resets the dynamic word list for autocompletion") },
  { ID_SORT, TR("&Sort")..KSC(ID_SORT), TR("Sort selected lines") },
  { },
}

local preferencesMenu = wx.wxMenu{
  {ID_PREFERENCESSYSTEM, TR("Settings: System")..KSC(ID_PREFERENCESSYSTEM)},
  {ID_PREFERENCESUSER, TR("Settings: User")..KSC(ID_PREFERENCESUSER)},
}
editMenu:Append(ID_PREFERENCES, TR("Preferences"), preferencesMenu)
menuBar:Append(editMenu, TR("&Edit"))

editMenu:Check(ID_AUTOCOMPLETEENABLE, ide.config.autocomplete)

local function getControlWithFocus()
  local editor = GetEditor()
  for _,e in pairs({frame.bottomnotebook.shellbox, frame.bottomnotebook.errorlog}) do
    local ctrl = e:FindFocus()
    if ctrl and
      (ctrl:GetId() == e:GetId()
       or ide.osname == 'Macintosh' and
         ctrl:GetParent():GetId() == e:GetId()) then editor = e end
  end
  return editor
end

function OnUpdateUIEditMenu(event)
  local editor = getControlWithFocus()
  if editor == nil then event:Enable(false); return end

  local alwaysOn = { [ID_SELECTALL] = true, [ID_FOLD] = ide.config.editor.fold,
    -- allow Cut and Copy commands as these work on a line if no selection
    [ID_COPY] = true, [ID_CUT] = true,
    [ID_COMMENT] = true, [ID_AUTOCOMPLETE] = true, [ID_SORT] = true}
  local menu_id = event:GetId()
  local enable =
    menu_id == ID_PASTE and editor:CanPaste() or
    menu_id == ID_UNDO and editor:CanUndo() or
    menu_id == ID_REDO and editor:CanRedo() or
    alwaysOn[menu_id]
  -- wxComboBox doesn't have SELECT ALL, so disable it
  if editor:GetClassInfo():GetClassName() == 'wxComboBox'
  and menu_id == ID_SELECTALL then enable = false end
  event:Enable(enable)
end

function OnEditMenu(event)
  local editor = getControlWithFocus()

  -- if there is no editor, or if it's not the editor we care about,
  -- then allow normal processing to take place
  if editor == nil or
     (editor:FindFocus() and editor:FindFocus():GetId() ~= editor:GetId()) or
     editor:GetClassInfo():GetClassName() ~= 'wxStyledTextCtrl'
    then event:Skip(); return end

  local menu_id = event:GetId()
  if menu_id == ID_CUT then
    if editor:GetSelectionStart() == editor:GetSelectionEnd()
      then editor:LineCut() else editor:Cut() end
  elseif menu_id == ID_COPY then
    if editor:GetSelectionStart() == editor:GetSelectionEnd()
      then editor:LineCopy() else editor:Copy() end
  elseif menu_id == ID_PASTE then editor:Paste()
  elseif menu_id == ID_SELECTALL then editor:SelectAll()
  elseif menu_id == ID_UNDO then editor:Undo()
  elseif menu_id == ID_REDO then editor:Redo()
  end
end

for _, event in pairs({ID_CUT, ID_COPY, ID_PASTE, ID_SELECTALL, ID_UNDO, ID_REDO}) do
  frame:Connect(event, wx.wxEVT_COMMAND_MENU_SELECTED, OnEditMenu)
  frame:Connect(event, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)
end

local function generateConfigMessage(type)
  return ([==[--[[--
  Use this file to specify %s preferences.
  Review [examples](+%s) or check [online documentation](%s) for details.
--]]--
]==])
    :format(type, MergeFullPath(ide.editorFilename, "../cfg/user-sample.lua"),
      "http://studio.zerobrane.com/documentation.html")
end

frame:Connect(ID_PREFERENCESSYSTEM, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = LoadFile(ide.configs.system)
    if editor and #editor:GetText() == 0 then
      editor:AddText(generateConfigMessage("System")) end
  end)

frame:Connect(ID_PREFERENCESUSER, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = LoadFile(ide.configs.user)
    if editor and #editor:GetText() == 0 then
      editor:AddText(generateConfigMessage("User")) end
  end)
frame:Connect(ID_PREFERENCESUSER, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(ide.configs.user ~= nil) end)

frame:Connect(ID_CLEARDYNAMICWORDS, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () DynamicWordsReset() end)

frame:Connect(ID_SHOWTOOLTIP, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()

    if (editor:CallTipActive()) then
      editor:CallTipCancel()
      return
    end

    EditorCallTip(editor, editor:GetCurrentPos())
  end)
frame:Connect(ID_SHOWTOOLTIP, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)

frame:Connect(ID_AUTOCOMPLETE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    EditorAutoComplete(GetEditor())
  end)
frame:Connect(ID_AUTOCOMPLETE, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)

frame:Connect(ID_AUTOCOMPLETEENABLE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ide.config.autocomplete = event:IsChecked()
  end)

frame:Connect(ID_COMMENT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()

    -- capture the current position in line to restore later
    local curline = editor:GetCurrentLine()
    local curlen = #editor:GetLine(curline)
    local curpos = editor:GetCurrentPos()-editor:PositionFromLine(curline)

    -- for multi-line selection, always start the first line at the beginning
    local ssel, esel = editor:GetSelectionStart(), editor:GetSelectionEnd()
    local sline = editor:LineFromPosition(ssel)
    local eline = editor:LineFromPosition(esel)
    local sel = ssel ~= esel
    local rect = editor:SelectionIsRectangle()
    local lc = editor.spec.linecomment
    local qlc = lc:gsub(".", "%%%1")

    -- figure out how to toggle comments; if there is at least one non-empty
    -- line that doesn't start with a comment, need to comment
    local comment = false
    for line = sline, eline do
      local pos = sel and (sline == eline or rect)
        and ssel-editor:PositionFromLine(sline)+1 or 1
      local text = editor:GetLine(line)
      local _, cpos = text:find("^%s*"..qlc, pos)
      if not cpos and text:find("%S")
      -- ignore last line when the end of selection is at the first position
      and (line == sline or line < eline or esel-editor:PositionFromLine(line) > 0) then
        comment = true
        break
      end
    end

    editor:BeginUndoAction()
    -- go last to first as selection positions we captured may be affected
    -- by text changes
    for line = eline, sline, -1 do
      local pos = sel and (sline == eline or rect)
        and ssel-editor:PositionFromLine(sline)+1 or 1
      local text = editor:GetLine(line)
      local _, cpos = text:find("^%s*"..qlc, pos)
      if not comment and cpos then
        editor:DeleteRange(cpos-#lc+editor:PositionFromLine(line), #lc)
      elseif comment and text:find("%S")
      and (line == sline or line < eline or esel-editor:PositionFromLine(line) > 0) then
        editor:InsertText(pos+editor:PositionFromLine(line)-1, lc)
      end
    end
    editor:EndUndoAction()

    -- fix position if it was after where the selection started
    if editor:PositionFromLine(curline)+curpos > ssel then
      -- position the cursor exactly where its position was, which
      -- could have shifted depending on whether the text was added or removed.
      editor:GotoPos(editor:PositionFromLine(curline)
        + math.max(0, curpos+#editor:GetLine(curline)-curlen))
    end
  end)
frame:Connect(ID_COMMENT, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)

frame:Connect(ID_SORT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local buf = {}
    for line in string.gmatch(editor:GetSelectedText()..'\n', "(.-)\r?\n") do
      table.insert(buf, line)
    end
    if #buf > 0 then
      local newline
      if #(buf[#buf]) == 0 then newline = table.remove(buf) end
      table.sort(buf)
      -- add new line at the end if it was there
      if newline then table.insert(buf, newline) end
      editor:ReplaceSelection(table.concat(buf,"\n"))
    end
  end)
frame:Connect(ID_SORT, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)

frame:Connect(ID_FOLD, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    FoldSome()
  end)
frame:Connect(ID_FOLD, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)
