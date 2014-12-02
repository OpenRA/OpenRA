-- Copyright 2011-14 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local q = EscapeMagic
local unpack = table.unpack or unpack

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
}
findMenu:Append(ID_NAVIGATE, TR("Navigate"), wx.wxMenu {
  { ID_NAVIGATETOFILE, TR("To File...")..KSC(ID_NAVIGATETOFILE), TR("Navigate to file") },
  { ID_NAVIGATETOLINE, TR("To Line...")..KSC(ID_NAVIGATETOLINE), TR("Navigate to line") },
})

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

local markername = "commandbar.background"
local mac = ide.osname == 'Macintosh'
local win = ide.osname == 'Windows'
local function navigateTo(default)
  local styles = ide.config.styles
  local marker = ide:AddMarker(markername,
    wxstc.wxSTC_MARK_BACKGROUND, styles.text.fg, styles.caretlinebg.bg)

  local nb = ide:GetEditorNotebook()
  local selection = nb:GetSelection()
  local projectFiles, preview, origline

  CommandBarShow(
    function(t, enter, text) -- onDone
      if not mac then nb:Freeze() end

      -- delete all current line markers if any; restore line position
      local ed = ide:GetEditor()
      if ed and origline then
        ed:MarkerDeleteAll(marker)
        ed:EnsureVisibleEnforcePolicy(origline-1)
      end

      if enter then
        local _, file, tabindex = unpack(t or {})
        if tabindex or preview then -- switch to existing tab
          SetEditorSelection(tabindex or nb:GetPageIndex(preview))
        elseif file then -- load a new file
          LoadFile(MergeFullPath(ide:GetProject(), file), nil, true)
        end

        -- set line position in the (current) editor if requested
        if text and text:find(':') then
          local toline = tonumber(text:match(':(%d+)'))
          local ed = ide:GetEditor()
          if toline and ed then
            ed:GotoLine(toline-1)
            ed:EnsureVisibleEnforcePolicy(toline-1)
            ed:SetFocus() -- in case the focus is on some other panel
          end
        end
      else
        -- close preview
        if preview then ClosePage(nb:GetPageIndex(preview)) end
        -- restore original selection if canceled
        if nb:GetSelection() ~= selection then nb:SetSelection(selection) end
      end
      preview = nil
      if not mac then nb:Thaw() end
    end,
    function(text) -- onUpdate
      local lines = {}
      local projdir = ide:GetProject()

      -- delete all current line markers if any
      -- restore the original position in case "goto line" is removed from bar
      local ed = ide:GetEditor()
      if ed and origline then
        ed:MarkerDeleteAll(marker)
        ed:EnsureVisibleEnforcePolicy(origline-1)
      end

      if text and text:find(':') then
        local toline = tonumber(text:match(':(%d+)'))
        if toline and ed then
          ed:MarkerDefine(ide:GetMarker(markername))
          ed:MarkerAdd(toline-1, marker)
          local curline = ed:GetCurrentLine() -- TODO find the middle line
          origline = origline or (curline+1)
          ed:EnsureVisibleEnforcePolicy(toline-1)
        end
      elseif text and #text > 0 and projdir and #projdir > 0 then
        -- populate the list of files
        if not projectFiles then
          projectFiles = FileSysGetRecursive(projdir, true)
          for k = #projectFiles, 1, -1 do
            if IsDirectory(projectFiles[k]) then
              table.remove(projectFiles, k)
            else
              projectFiles[k] = projectFiles[k]:gsub("^"..q(projdir), "")
            end
          end
        end
        local topscore
        for _, item in ipairs(CommandBarScoreFiles(projectFiles, text, 100)) do
          local file, score = unpack(item)
          topscore = topscore or score
          if score > topscore / 4 and score > 1 then
            table.insert(lines, {
                ("%2d %s"):format(score, wx.wxFileName(file):GetFullName()),
                file,
            })
          end
        end
      else
        for _, doc in pairs(ide:GetDocuments()) do
          lines[doc:GetTabIndex()+1] = {doc:GetFileName(), doc:GetFilePath(), doc:GetTabIndex()}
        end
      end
      return lines
    end,
    function(t) return unpack(t) end, -- onItem
    function(t) -- onSelection
      local _, file, tabindex = unpack(t)
      if file then file = MergeFullPath(ide:GetProject(), file) end
      -- disabling event handlers for the notebook and the editor
      -- to minimize changes in the UI when editors are switched
      -- or files in the preview are updated.
      nb:SetEvtHandlerEnabled(false)
      local doc = file and ide:FindDocument(file)
      if doc and not tabindex then tabindex = doc:GetTabIndex() end
      if tabindex then
        local ed = nb:GetPage(tabindex)
        ed:SetEvtHandlerEnabled(false)
        if nb:GetSelection() ~= tabindex then nb:SetSelection(tabindex) end
        ed:SetEvtHandlerEnabled(true)
      elseif file then
        -- skip binary files with unknown extensions
        if #ide:GetKnownExtensions(GetFileExt(file)) > 0
        or not isBinary(FileRead(file, 2048)) then
          preview = preview or NewFile()
          preview:SetEvtHandlerEnabled(false)
          LoadFile(file, preview, true, true)
          preview:SetFocus()
          -- force refresh since the panel covers the editor on OSX/Linux
          -- this fixes the preview window not always redrawn on Linux
          if not win then preview:Update() preview:Refresh() end
          preview:SetEvtHandlerEnabled(true)
        elseif preview then
          ClosePage(nb:GetPageIndex(preview))
          preview = nil
        end
      end
      nb:SetEvtHandlerEnabled(true)
    end,
    default or ""
  )
end

frame:Connect(ID_NAVIGATETOFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function() navigateTo("") end)
frame:Connect(ID_NAVIGATETOLINE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function() navigateTo(":") end)
