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
  { ID_NAVIGATETOFILE, TR("Go To File...")..KSC(ID_NAVIGATETOFILE), TR("Go to file") },
  { ID_NAVIGATETOLINE, TR("Go To Line...")..KSC(ID_NAVIGATETOLINE), TR("Go to line") },
  { ID_NAVIGATETOSYMBOL, TR("Go To Symbol...")..KSC(ID_NAVIGATETOSYMBOL), TR("Go to symbol") },
  { ID_NAVIGATETOMETHOD, TR("Insert Library Function...")..KSC(ID_NAVIGATETOMETHOD), TR("Find and insert library function") },
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
      editor:ShowPosEnforcePolicy(editor:GetCurrentPos())
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
      editor:ShowPosEnforcePolicy(editor:GetCurrentPos())
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
local special = {SYMBOL = '@', LINE = ':', METHOD = ';'}
local function navigateTo(default, selected)
  local styles = ide.config.styles
  local marker = ide:AddMarker(markername,
    wxstc.wxSTC_MARK_BACKGROUND, styles.text.fg, styles.caretlinebg.bg)

  local nb = ide:GetEditorNotebook()
  local selection = nb:GetSelection()
  local files, preview, origline, functions, methods

  local function markLine(ed, toline)
    ed:MarkerDefine(ide:GetMarker(markername))
    ed:MarkerDeleteAll(marker)
    ed:MarkerAdd(toline-1, marker)
    -- store the original line if not stored yet
    origline = origline or (ed:GetCurrentLine()+1)
    ed:EnsureVisibleEnforcePolicy(toline-1)
  end

  CommandBarShow({
    defaultText = default or "",
    selectedText = selected or "",
    onDone = function(t, enter, text)
      if not mac then nb:Freeze() end

      -- delete all current line markers if any; restore line position
      local ed = ide:GetEditor()
      if ed and origline then
        ed:MarkerDeleteAll(marker)
        ed:EnsureVisibleEnforcePolicy(origline-1)
      end

      if enter then
        local fline, sline, tabindex = unpack(t or {})
        local ed = ide:GetEditor()

        -- jump to symbol; tabindex has the position of the symbol
        if text and text:find(special.SYMBOL) and tabindex then
          ed:GotoPos(tabindex-1)
          ed:EnsureVisibleEnforcePolicy(ed:LineFromPosition(tabindex-1))
          ed:SetFocus() -- in case the focus is on some other panel
        -- insert selected method
        elseif text and text:find('^%s*'..special.METHOD) then
          if ed then -- clean up text and insert at the current location
            local method = sline
            local isfunc = methods.desc[method][1]:find(q(method).."%s*%(")
            local text = method .. (isfunc and "()" or "")
            local pos = ed:GetCurrentPos()
            ed:InsertText(pos, text)
            ed:EnsureVisibleEnforcePolicy(ed:LineFromPosition(pos))
            ed:GotoPos(pos + #method + (isfunc and 1 or 0))
            if isfunc then -- show the tooltip
              ide.frame:AddPendingEvent(wx.wxCommandEvent(
                wx.wxEVT_COMMAND_MENU_SELECTED, ID_SHOWTOOLTIP))
            end
          end
        -- set line position in the (current) editor if requested
        elseif text and text:find(special.LINE) then
          local toline = tonumber(text:match(special.LINE..'(%d+)'))
          if toline and ed then
            ed:GotoLine(toline-1)
            ed:EnsureVisibleEnforcePolicy(toline-1)
            ed:SetFocus() -- in case the focus is on some other panel
          end
        elseif tabindex then -- switch to existing tab
          SetEditorSelection(tabindex)
          if preview then -- close preview if not selected
            local pindex = nb:GetPageIndex(preview)
            if pindex ~= tabindex then ClosePage(pindex) end
          end
        elseif sline then -- load a new file (into preview if set)
          LoadFile(MergeFullPath(ide:GetProject(), sline), preview or nil, true)
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
    onUpdate = function(text)
      local lines = {}
      local projdir = ide:GetProject()

      -- delete all current line markers if any
      -- restore the original position if search text is updated
      local ed = ide:GetEditor()
      if ed and origline then ed:MarkerDeleteAll(marker) end

      -- reset cached functions if no symbol search
      if text and not text:find(special.SYMBOL) then
        functions = nil
        if ed and origline then ed:EnsureVisibleEnforcePolicy(origline-1) end
      end
      -- reset cached methods if no method search
      if text and not text:find(special.METHOD) then methods = nil end

      if ed and text and text:find(special.SYMBOL) then
        if not functions then
          local funcs, nums = OutlineFunctions(ed), {}
          functions = {pos = {}, src = {}}
          for _, func in ipairs(funcs) do
            table.insert(functions, func.name)
            nums[func.name] = (nums[func.name] or 0) + 1
            local num = nums[func.name]
            local line = ed:LineFromPosition(func.pos-1)
            functions.src[func.name..num] = ed:GetLine(line):gsub("^%s+","")
            functions.pos[func.name..num] = func.pos
          end
        end
        local symbol = text:match(special.SYMBOL..'(.*)')
        local nums = {}
        if #symbol > 0 then
          local topscore
          for _, item in ipairs(CommandBarScoreItems(functions, symbol, 100)) do
            local func, score = unpack(item)
            topscore = topscore or score
            nums[func] = (nums[func] or 0) + 1
            local num = nums[func]
            if score > topscore / 4 and score > 1 then
              table.insert(lines, {("%2d %s"):format(score, func),
                  functions.src[func..num], functions.pos[func..num]})
            end
          end
        else
          for n, name in ipairs(functions) do
            nums[name] = (nums[name] or 0) + 1
            local num = nums[name]
            lines[n] = {name, functions.src[name..num], functions.pos[name..num]}
          end
        end
      elseif ed and text and text:find('^%s*'..special.METHOD) then
        if not methods then
          methods = {desc = {}}
          local num = 1
          if ed.api and ed.api.tip and ed.api.tip.shortfinfoclass then
            for libname, lib in pairs(ed.api.tip.shortfinfoclass) do
              for method, val in pairs(lib) do
                local signature, desc = val:match('(.-)\n(.*)')
                local m = libname..'.'..method
                desc = desc and desc:gsub("\n", " ") or val
                methods[num] = m
                methods.desc[m] = {signature or (libname..'.'..method), desc}
                num = num + 1
              end
            end
          end
        end
        local method = text:match(special.METHOD..'(.*)')
        if #method > 0 then
          local topscore
          for _, item in ipairs(CommandBarScoreItems(methods, method, 100)) do
            local method, score = unpack(item)
            topscore = topscore or score
            if score > topscore / 4 and score > 1 then
              table.insert(lines, { score, method })
            end
          end
        end
      elseif text and text:find(special.LINE) then
        local toline = tonumber(text:match(special.LINE..'(%d+)'))
        if toline and ed then markLine(ed, toline) end
      elseif text and #text > 0 and projdir and #projdir > 0 then
        -- populate the list of files
        if not files then
          files = FileSysGetRecursive(projdir, true)
          for k = #files, 1, -1 do
            if IsDirectory(files[k]) then
              table.remove(files, k)
            else
              files[k] = files[k]:gsub("^"..q(projdir), "")
            end
          end
        end
        local topscore
        for _, item in ipairs(CommandBarScoreItems(files, text, 100)) do
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
    onItem = function(t)
      if methods then
        local score, method = unpack(t)
        return ("%2d %s"):format(score, methods.desc[method][1]), methods.desc[method][2]
      else
        return unpack(t)
      end
    end,
    onSelection = function(t, text)
      local _, file, tabindex = unpack(t)
      if text and text:find(special.SYMBOL) then
        local ed = ide:GetEditor()
        if ed then markLine(ed, ed:LineFromPosition(tabindex-1)+1) end
        return
      elseif text and text:find(special.METHOD) then
        return
      end

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
  })
end

frame:Connect(ID_NAVIGATETOFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function() navigateTo("") end)
frame:Connect(ID_NAVIGATETOLINE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function() navigateTo(special.LINE) end)
frame:Connect(ID_NAVIGATETOMETHOD, wx.wxEVT_COMMAND_MENU_SELECTED,
  function() navigateTo(special.METHOD) end)
frame:Connect(ID_NAVIGATETOSYMBOL, wx.wxEVT_COMMAND_MENU_SELECTED,
  function()
    local ed = GetEditor()
    navigateTo(special.SYMBOL, ed and ed:ValueFromPosition(ed:GetCurrentPos()))
  end)
