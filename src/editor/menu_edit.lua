-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide

-- ---------------------------------------------------------------------------
-- Create the Edit menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar

local editMenu = wx.wxMenu {
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
}

editMenu:Append(ID_SOURCE, TR("Source"), wx.wxMenu {
  { ID_COMMENT, TR("C&omment/Uncomment")..KSC(ID_COMMENT), TR("Comment or uncomment current or selected lines") },
  { ID_REINDENT, TR("Correct &Indentation")..KSC(ID_REINDENT), TR("Re-indent selected lines") },
  { ID_FOLD, TR("&Fold/Unfold All")..KSC(ID_FOLD), TR("Fold or unfold all code folds") },
  { ID_SORT, TR("&Sort")..KSC(ID_SORT), TR("Sort selected lines") },
})
editMenu:Append(ID_BOOKMARK, TR("Bookmark"), wx.wxMenu {
  { ID_BOOKMARKTOGGLE, TR("Toggle Bookmark")..KSC(ID_BOOKMARKTOGGLE), TR("Toggle bookmark") },
  { ID_BOOKMARKNEXT, TR("Go To Next Bookmark")..KSC(ID_BOOKMARKNEXT) },
  { ID_BOOKMARKPREV, TR("Go To Previous Bookmark")..KSC(ID_BOOKMARKPREV) },
})
editMenu:AppendSeparator()
editMenu:Append(ID_PREFERENCES, TR("Preferences"), wx.wxMenu {
  { ID_PREFERENCESSYSTEM, TR("Settings: System")..KSC(ID_PREFERENCESSYSTEM) },
  { ID_PREFERENCESUSER, TR("Settings: User")..KSC(ID_PREFERENCESUSER) },
})
menuBar:Append(editMenu, TR("&Edit"))

editMenu:Check(ID_AUTOCOMPLETEENABLE, ide.config.autocomplete)

local function onUpdateUIEditorInFocus(event)
  event:Enable(GetEditorWithFocus(GetEditor()) ~= nil)
end

local function onUpdateUIEditMenu(event)
  local editor = GetEditorWithFocus()
  if editor == nil then event:Enable(false); return end

  local alwaysOn = {
    [ID_SELECTALL] = true,
    -- allow Cut and Copy commands as these work on a line if no selection
    [ID_COPY] = true, [ID_CUT] = true,
  }
  local menu_id = event:GetId()
  local enable =
    -- pasting is allowed when the document is not read-only and the selection
    -- (if any) has no protected text; since pasting handles protected text,
    -- use GetReadOnly() instead of CanPaste()
    menu_id == ID_PASTE and (not editor:GetReadOnly()) or
    menu_id == ID_UNDO and editor:CanUndo() or
    menu_id == ID_REDO and editor:CanRedo() or
    alwaysOn[menu_id]
  event:Enable(enable)
end

local function onEditMenu(event)
  local editor = GetEditorWithFocus()
  if editor == nil then event:Skip(); return end

  if PackageEventHandle("onEditorAction", editor, event) == false then
    return
  end

  local menu_id = event:GetId()
  local copytext
  if (menu_id == ID_CUT or menu_id == ID_COPY)
  and ide.wxver >= "2.9.5" and editor:GetSelections() > 1 then
    local main = editor:GetMainSelection()
    copytext = editor:GetTextRange(editor:GetSelectionNStart(main), editor:GetSelectionNEnd(main))
    for s = 0, editor:GetSelections()-1 do
      if copytext ~= editor:GetTextRange(editor:GetSelectionNStart(s), editor:GetSelectionNEnd(s)) then
        copytext = nil
        break
      end
    end
  end

  local spos, epos = editor:GetSelectionStart(), editor:GetSelectionEnd()
  if menu_id == ID_CUT then
    if spos == epos then editor:LineCopy() else editor:Copy() end
    if spos == epos then
      local line = editor:LineFromPosition(spos)
      spos, epos = editor:PositionFromLine(line), editor:PositionFromLine(line+1)
      editor:SetSelectionStart(spos)
      editor:SetSelectionEnd(epos)
    end
    if spos ~= epos then editor:ClearAny() end
  elseif menu_id == ID_COPY then
    if spos == epos then editor:LineCopy() else editor:Copy() end
  elseif menu_id == ID_PASTE then
    -- first clear the text in case there is any hidden markup
    if spos ~= epos then editor:ClearAny() end
    editor:Paste()
  elseif menu_id == ID_SELECTALL then editor:SelectAll()
  elseif menu_id == ID_UNDO then editor:Undo()
  elseif menu_id == ID_REDO then editor:Redo()
  end

  if copytext then editor:CopyText(#copytext, copytext) end
end

for _, event in pairs({ID_CUT, ID_COPY, ID_PASTE, ID_SELECTALL, ID_UNDO, ID_REDO}) do
  frame:Connect(event, wx.wxEVT_COMMAND_MENU_SELECTED, onEditMenu)
  frame:Connect(event, wx.wxEVT_UPDATE_UI, onUpdateUIEditMenu)
end

for _, event in pairs({
    ID_BOOKMARKTOGGLE, ID_BOOKMARKNEXT, ID_BOOKMARKPREV,
    ID_AUTOCOMPLETE, ID_SORT, ID_REINDENT, ID_SHOWTOOLTIP,
}) do
  frame:Connect(event, wx.wxEVT_UPDATE_UI, onUpdateUIEditorInFocus)
end

frame:Connect(ID_COMMENT, wx.wxEVT_UPDATE_UI,
  function(event)
    local editor = GetEditorWithFocus(GetEditor())
    event:Enable(editor ~= nil
      and pcall(function() return editor.spec end) and editor.spec
      and editor.spec.linecomment and true or false)
  end)

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

frame:Connect(ID_AUTOCOMPLETE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) EditorAutoComplete(GetEditor()) end)

frame:Connect(ID_AUTOCOMPLETEENABLE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) ide.config.autocomplete = event:IsChecked() end)

frame:Connect(ID_COMMENT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local lc = editor.spec.linecomment
    if not lc then return end

    -- for multi-line selection, always start the first line at the beginning
    local ssel, esel = editor:GetSelectionStart(), editor:GetSelectionEnd()
    local sline = editor:LineFromPosition(ssel)
    local eline = editor:LineFromPosition(esel)
    local sel = ssel ~= esel
    local rect = editor:SelectionIsRectangle()
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
        editor:SetTargetStart(pos+editor:PositionFromLine(line)-1)
        editor:SetTargetEnd(editor:GetTargetStart())
        editor:ReplaceTarget(lc)
      end
    end
    editor:EndUndoAction()
  end)

local function processSelection(editor, func)
  local text = editor:GetSelectedText()
  local line = editor:GetCurrentLine()
  local posinline = editor:GetCurrentPos() - editor:PositionFromLine(line)
  if #text == 0 then
    editor:SelectAll()
    text = editor:GetSelectedText()
  end
  local wholeline = text:find('\n$')
  local buf = {}
  for line in string.gmatch(text..(wholeline and '' or '\n'), "(.-\r?\n)") do
    table.insert(buf, line)
  end
  if #buf > 0 then
    if func then func(buf) end
    -- add new line at the end if it was there
    local newtext = table.concat(buf, ''):gsub('(\r?\n)$', wholeline and '%1' or '')
    -- straightforward editor:ReplaceSelection() doesn't work reliably as
    -- it sometimes doubles the context when the entire file is selected.
    -- this seems like Scintilla issue, so use ReplaceTarget instead.
    -- Since this doesn't work with rectangular selection, which
    -- ReplaceSelection should handle (after wxwidgets 3.x upgrade), this
    -- will need to be revisited when ReplaceSelection is updated.
    if newtext ~= text then
      editor:TargetFromSelection()
      editor:ReplaceTarget(newtext)
    end
  end
  editor:GotoPosEnforcePolicy(math.min(
      editor:PositionFromLine(line)+posinline, editor:GetLineEndPosition(line)))
end

frame:Connect(ID_SORT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) processSelection(GetEditor(), table.sort) end)

local function reIndent(editor, buf)
  local decindent, incindent = editor.spec.isdecindent, editor.spec.isincindent
  if not (decindent and incindent) then return end

  local edline = editor:LineFromPosition(editor:GetSelectionStart())
  local indent = 0
  local text = ''
  -- find the last non-empty line in the previous block (if any)
  for n = edline-1, 1, -1 do
    indent = editor:GetLineIndentation(n)
    text = editor:GetLine(n)
    if text:match('[^\r\n]') then break end
  end

  local ut = editor:GetUseTabs()
  local tw = ut and editor:GetTabWidth() or editor:GetIndent()

  local indents = {}
  local isstatic = {}
  for line = 1, #buf+1 do
    local ls = editor:PositionFromLine(edline+line-1)
    local style = bit.band(editor:GetStyleAt(ls), 31)
    -- don't reformat multi-line comments or strings
    isstatic[line] = (editor.spec.iscomment[style]
      or editor.spec.isstring[style]
      or (MarkupIsAny and MarkupIsAny(style)))
    if not isstatic[line] or line == 1 or not isstatic[line-1] then
      local closed, blockend = decindent(text)
      local opened = incindent(text)

      -- ignore impact from initial block endings as they are already indented
      if line == 1 then blockend = 0 end

      -- this only needs to be done for 2, #buf+1; do it and get out when done
      if line > 1 then indents[line-1] = indents[line-1] - tw * closed end
      if line > #buf then break end

      indent = indent + tw * (opened - blockend)
      if indent < 0 then indent = 0 end
    end

    indents[line] = indent
    text = buf[line]
  end

  for line = 1, #buf do
    if not isstatic[line] then
      buf[line] = buf[line]:gsub("^[ \t]*",
        not buf[line]:match('%S') and ''
        or ut and ("\t"):rep(indents[line] / tw) or (" "):rep(indents[line]))
    end
  end
end

frame:Connect(ID_REINDENT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    processSelection(editor, function(buf) reIndent(editor, buf) end)
  end)

frame:Connect(ID_FOLD, wx.wxEVT_UPDATE_UI,
  function(event)
    local editor = GetEditorWithFocus()
    event:Enable(editor and editor:CanFold() or false)
  end)
frame:Connect(ID_FOLD, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) GetEditorWithFocus():FoldSome() end)

local BOOKMARK_MARKER = StylesGetMarker("bookmark")
local BOOKMARK_MARKER_VALUE = 2^BOOKMARK_MARKER

local function bookmarkToggle()
  local editor = GetEditor()
  local line = editor:GetCurrentLine()
  local markers = editor:MarkerGet(line)
  if bit.band(markers, BOOKMARK_MARKER_VALUE) > 0 then
    editor:MarkerDelete(line, BOOKMARK_MARKER)
  else
    editor:MarkerAdd(line, BOOKMARK_MARKER)
  end
end

local function bookmarkNext()
  local editor = GetEditor()
  local line = editor:MarkerNext(editor:GetCurrentLine()+1, BOOKMARK_MARKER_VALUE)
  if line == -1 then line = editor:MarkerNext(0, BOOKMARK_MARKER_VALUE) end
  if line ~= -1 then
    editor:GotoLine(line)
    editor:EnsureVisibleEnforcePolicy(line)
  end
end

local function bookmarkPrev()
  local editor = GetEditor()
  local line = editor:MarkerPrevious(editor:GetCurrentLine()-1, BOOKMARK_MARKER_VALUE)
  if line == -1 then line = editor:MarkerPrevious(editor:GetLineCount(), BOOKMARK_MARKER_VALUE) end
  if line ~= -1 then
    editor:GotoLine(line)
    editor:EnsureVisibleEnforcePolicy(line)
  end
end

frame:Connect(ID_BOOKMARKTOGGLE, wx.wxEVT_COMMAND_MENU_SELECTED, bookmarkToggle)
frame:Connect(ID_BOOKMARKNEXT, wx.wxEVT_COMMAND_MENU_SELECTED, bookmarkNext)
frame:Connect(ID_BOOKMARKPREV, wx.wxEVT_COMMAND_MENU_SELECTED, bookmarkPrev)
