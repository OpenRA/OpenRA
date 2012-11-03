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
}
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

  local alwaysOn = { [ID_SELECTALL] = true, [ID_FOLD] = true,
    [ID_COMMENT] = true, [ID_AUTOCOMPLETE] = true}
  local menu_id = event:GetId()
  local enable =
    ((menu_id == ID_COPY or menu_id == ID_CUT) and
     (editor:GetClassInfo():GetClassName() ~= 'wxStyledTextCtrl'
      or editor:GetSelectionStart() ~= editor:GetSelectionEnd())) or
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
  if menu_id == ID_CUT then editor:Cut()
  elseif menu_id == ID_COPY then editor:Copy()
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
    local buf = {}
    if editor:GetSelectionStart() == editor:GetSelectionEnd() then
      local lineNumber = editor:GetCurrentLine()
      editor:SetSelection(editor:PositionFromLine(lineNumber), editor:GetLineEndPosition(lineNumber))
    end
    local lc = editor.spec.linecomment
    for line in string.gmatch(editor:GetSelectedText()..'\n', "(.-)\r?\n") do
      if string.sub(line,1,2) == lc then
        line = string.sub(line,3)
      else
        line = lc..line
      end
      table.insert(buf, line)
    end
    editor:ReplaceSelection(table.concat(buf,"\n"))
  end)
frame:Connect(ID_COMMENT, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)

frame:Connect(ID_FOLD, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    FoldSome()
  end)
frame:Connect(ID_FOLD, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)
