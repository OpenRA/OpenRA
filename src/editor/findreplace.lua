-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local searchpanel = 'searchpanel'
local q = EscapeMagic
local unpack = table.unpack or unpack
ide.findReplace = {
  panel = nil, -- the control for find/replace
  replace = false, -- is it a find or replace
  infiles = false,
  backfocus = nil, -- editor and position to return focus to
  cureditor = nil, -- the editor being searched
  reseditor = nil, -- the editor for search results
  oveditor = nil, -- the editor is used for search during find-in-files
  findCtrl = nil, -- the control that has the search text
  replaceCtrl = nil, -- the control that has the replace text
  scopeText = nil,
  foundString = false, -- was the string found for the last search
  curfilename = "", -- for search in files
  inselection = false,
  occurrences = 0,
  files = 0,

  settings = {
    flags = {
      WholeWord = false, -- match whole words
      MatchCase = false, -- case sensitive
      RegularExpr = false, -- use regex
      Wrap = true, -- search wraps around
      Down = true, -- search downwards in doc
      Context = true, -- include context in search results
      SubDirs = true, -- search in subdirectories
      MultiResults = false, -- show multiple result tabs
    },
    flist = {},
    rlist = {},
    slist = {},
  },

  -- HasText() is there a string to search for
  -- GetSelection() get currently selected string if it's on one line
  -- Find(reverse) find the text
  -- Show(replace) create the dialog
  -- GetEditor() which editor to use
}
local findReplace = ide.findReplace
local NOTFOUND = -1
local replaceHintText = '<replace with>'
local sep = ';'

function findReplace:GetEditor(reset)
  if reset or not ide:IsValidCtrl(self.cureditor) then self.cureditor = nil end
  self.cureditor = ide:GetEditorWithLastFocus() or self.cureditor
  return self.oveditor or self.cureditor or GetEditor()
end

-------------------- Find replace dialog

local function setSearchFlags(editor)
  local flags = wxstc.wxSTC_FIND_POSIX
  local f = findReplace.settings.flags
  if f.WholeWord then flags = flags + wxstc.wxSTC_FIND_WHOLEWORD end
  if f.MatchCase then flags = flags + wxstc.wxSTC_FIND_MATCHCASE end
  if f.RegularExpr then flags = flags + wxstc.wxSTC_FIND_REGEXP end
  editor:SetSearchFlags(flags)
end

local function setTarget(editor, flags)
  flags = flags or {}
  local fDown, fAll, fWrap = flags.Down, flags.All, flags.Wrap
  local len = editor:GetLength()
  local selStart, selEnd = editor:GetSelectionStart(), editor:GetSelectionEnd()
  local s, e
  if fDown then
    e = flags.EndPos or len
    s = math.min(e, math.max(flags.StartPos or 0, iff(fAll, selStart, selEnd)))
  else -- reverse the range for the backward search
    e = flags.StartPos or 0
    s = math.max(e, math.min(flags.EndPos or len, iff(fAll, selEnd, selStart)))
  end
  -- if wrap around and search all requested, then search the entire document
  if fAll and fWrap then s, e = 0, len end
  editor:SetTargetStart(s)
  editor:SetTargetEnd(e)
  return e
end

function findReplace:IsPreview(editor)
  local ok, ispreview = pcall(function() return editor and editor.searchpreview end)
  return ok and ispreview and true or false
end

function findReplace:CanSave(editor)
  return editor and editor:GetModify() and self:IsPreview(editor) and editor or nil
end

function findReplace:HasText()
  if not self.panel then self:createPanel() end
  local findText = self.findCtrl:GetValue()
  return findText ~= nil and #findText > 0 and findText or nil
end

function findReplace:SetStatus(msg)
  if self.status then self.status:SetLabel(msg) end
end

function findReplace:SetFind(text)
  if not self.panel then self:createPanel() end
  local ctrl = self.findCtrl
  if text and ctrl then
    if ctrl:GetValue() ~= text then ctrl:ChangeValue(text) end
    return text
  end
  return
end

function findReplace:GetFind(...) return self:HasText() end

function findReplace:GetFlags() return self.settings.flags end

function findReplace:SetReplace(text)
  if not self.panel then self:createPanel() end
  local ctrl = self.replaceCtrl
  if text and ctrl then
    if ctrl:GetValue() ~= text then ctrl:ChangeValue(text) end
    return text
  end
  return
end

function findReplace:GetScope()
  local scopeval = self.scope:GetValue()
  local dir, mask = scopeval:match(('([^%s]*)%s%%s*(.+)'):format(sep,sep))
  if not dir then dir = scopeval end
  -- trip leading/trailing spaces from the directory
  dir = dir:gsub("^%s+",""):gsub("%s+$","")
  -- if the directory doesn't exist, treat it as the extension(s)
  if not mask and not wx.wxDirExists(dir) and dir:find('%*') then
    dir, mask = ide:GetProject() or wx.wxGetCwd(), (#dir > 0 and dir or nil)
  end
  return dir, mask
end

function findReplace:SetScope(dir, mask)
  return dir .. (mask and (sep..' '..mask) or "")
end

function findReplace:GetScopeMRU(head)
  local patt, match = "^"..q(head)
  for _, v in ipairs(findReplace.settings.slist) do
    if v:find(patt) then match = v; break end
  end
  return match
end

function findReplace:GetWordAtCaret()
  local editor = self:GetEditor()
  if editor then
    local pos = editor:GetCurrentPos()
    local text = editor:GetTextRangeDyn( -- try to select a word under caret
      editor:WordStartPosition(pos, true), editor:WordEndPosition(pos, true))
    if #text == 0 then
      editor:GetTextRangeDyn( -- try to select a non-word under caret
        editor:WordStartPosition(pos, false), editor:WordEndPosition(pos, false))
    end
    return #text > 0 and text or nil
  end
  return
end

function findReplace:GetSelection()
  local editor = self:GetEditor()
  if editor then
    local startSel = editor:GetSelectionStart()
    local endSel = editor:GetSelectionEnd()
    if (startSel ~= endSel)
    and (editor:LineFromPosition(startSel) == editor:LineFromPosition(endSel)) then
      return editor:GetTextRangeDyn(startSel, endSel)
    end
  end
  return
end

function findReplace:Find(reverse)
  if not self.panel then self:createPanel() end
  local findText = self.findCtrl:GetValue()

  local msg = ""
  local editor = self:GetEditor()
  if editor and self:HasText() then
    local fDown = iff(reverse, not self:GetFlags().Down, self:GetFlags().Down)
    local bf = self.inselection and self.backfocus or {}
    setSearchFlags(editor)
    setTarget(editor, {Down = fDown, StartPos = bf.spos, EndPos = bf.epos})
    local posFind = editor:SearchInTarget(findText)
    if (posFind == NOTFOUND) and self:GetFlags().Wrap then
      editor:SetTargetStart(iff(fDown, bf.spos or 0, bf.epos or editor:GetLength()))
      editor:SetTargetEnd(iff(fDown, bf.epos or editor:GetLength(), bf.spos or 0))
      posFind = editor:SearchInTarget(findText)
      msg = (self.inselection
        and TR("Reached end of selection and wrapped around.")
        or TR("Reached end of text and wrapped around.")
      )
    end
    if posFind == NOTFOUND then
      self.foundString = false
      msg = TR("Text not found.")
    else
      self.foundString = true
      local start = editor:GetTargetStart()
      local finish = editor:GetTargetEnd()
      editor:ShowPosEnforcePolicy(finish)
      editor:SetSelection(start, finish)
    end
  end
  self:SetStatus(msg)
  return self.foundString
end

-- returns true if something was found
-- [inFileRegister(pos)] passing function will
-- register every position item was found

function findReplace:FindAll(inFileRegister)
  if not self.panel then self:createPanel() end
  local findText = self.findCtrl:GetValue()

  local found = false
  local editor = self:GetEditor()
  if editor and self:HasText() then
    local e = setTarget(editor, {All = true, Wrap = true})

    setSearchFlags(editor)
    while true do
      local posFind = editor:SearchInTarget(findText)
      if posFind == NOTFOUND then break end
      inFileRegister(posFind, editor:GetTargetEnd()-posFind)
      editor:SetTargetStart(editor:GetTargetEnd())
      editor:SetTargetEnd(e)
      found = true
    end
    if inFileRegister and found then inFileRegister() end
  end

  return found
end

local indicator = {
  SEARCHMATCH = ide:GetIndicator("core.searchmatch"),
}

-- returns true if replacements were done
function findReplace:Replace(fReplaceAll, resultsEditor)
  if not self.panel then self:createPanel() end

  local findText = self.findCtrl:GetValue()
  local replaceText = self.replaceCtrl:GetValue()
  if replaceText == replaceHintText then replaceText = "" end

  local replaced = false
  local editor = resultsEditor or self:GetEditor()
  if editor and self:HasText() then
    -- don't replace in read-only editors
    if editor:GetReadOnly() then
      self:SetStatus(TR("Can't replace in read-only text."))
      return false
    end

    -- in the preview results always replace in the entire file
    local bf = self.inselection and self.backfocus
    local endTarget = (resultsEditor and setTarget(editor, {All = true, Wrap = true})
      -- when selection is marked, only replace in the selection
      or (bf and setTarget(editor, {Down = self:GetFlags().Down, All = fReplaceAll, StartPos = bf.spos, EndPos = bf.epos}))
      -- in all other cases, replace as selected
      or setTarget(editor, {Down = self:GetFlags().Down, All = fReplaceAll, Wrap = self:GetFlags().Wrap})
    )

    if fReplaceAll then
      if resultsEditor then editor:SetIndicatorCurrent(indicator.SEARCHMATCH) end

      setSearchFlags(editor)
      local occurrences = 0
      local posFind = editor:SearchInTarget(findText)
      if posFind ~= NOTFOUND then
        editor:BeginUndoAction()
        while posFind ~= NOTFOUND do
          local length = editor:GetLength()
          -- if replace-in-files (resultsEditor) is being done,
          -- then check that the match starts with %d+:
          local match = true
          if resultsEditor then
            local line = editor:LineFromPosition(posFind)
            local _, _, prefix = editor:GetLineDyn(line):find("^(%s*%d+: )")
            match = prefix and posFind >= editor:PositionFromLine(line)+#prefix
          end
          if match then
            local replaced = self:GetFlags().RegularExpr
              and editor:ReplaceTargetRE(replaceText)
              or editor:ReplaceTarget(replaceText)

            -- mark replaced text
            if resultsEditor then editor:IndicatorFillRange(posFind, replaced) end
            occurrences = occurrences + 1
          end

          editor:SetTargetStart(editor:GetTargetEnd())
          -- adjust the endTarget as the position could have changed;
          -- can't simply subtract text length as it could be a regexp
          local adjusted = editor:GetLength() - length
          endTarget = endTarget + adjusted
          -- also adjust the selection as the end marker can move after replacement
          if bf and bf.epos then bf.epos = bf.epos + adjusted end
          editor:SetTargetEnd(endTarget)
          posFind = editor:SearchInTarget(findText)
        end
        editor:EndUndoAction()
        replaced = true
      end
      self:SetStatus(
        TR("Replaced %d instance.", occurrences):format(occurrences))
    else
      editor:TargetFromSelection()
      -- check if there is anything selected as well as the user can
      -- move the cursor after successful search
      if editor:GetSelectionStart() ~= editor:GetSelectionEnd()
      -- check that the current selection matches what's being searched for
      and editor:SearchInTarget(findText) ~= NOTFOUND then
        local length = editor:GetLength()
        local start = editor:GetSelectionStart()
        local replaced = self:GetFlags().RegularExpr
          and editor:ReplaceTargetRE(replaceText)
          or editor:ReplaceTarget(replaceText)
        local adjusted = editor:GetLength() - length
        if bf and bf.epos then bf.epos = bf.epos + adjusted end

        editor:SetSelection(start, start + replaced)
        self.foundString = false

        replaced = true
      end
      self:Find()
    end
  end

  return replaced
end

local oldline
local FILE_MARKER = ide:GetMarker("searchmatchfile")
local FILE_MARKER_VALUE = 2^FILE_MARKER
local function getRawLine(ed, line) return (ed:GetLineDyn(line):gsub("[\n\r]+$","")) end
local function onFileRegister(pos, length)
  local editor = findReplace.oveditor
  local reseditor = findReplace.reseditor
  local posline = pos and editor:LineFromPosition(pos) + 1
  local text = ""
  local cfg = ide.config.search
  local contextb = findReplace:GetFlags().Context and cfg.contextlinesbefore or 0
  local contexta = findReplace:GetFlags().Context and cfg.contextlinesafter or 0
  local lines = reseditor:GetLineCount() -- current number of lines

  -- check if there is another match on the same line; do not add anything
  if oldline ~= posline then
    if posline and not oldline then
      -- show file name and a bookmark marker
      reseditor:AppendTextDyn(findReplace.curfilename.."\n")
      reseditor:MarkerAdd(lines-1, FILE_MARKER)
      reseditor:SetFoldLevel(lines-1, reseditor:GetFoldLevel(lines-1)
        + wxstc.wxSTC_FOLDLEVELHEADERFLAG)
      findReplace:SetStatus(GetFileName(findReplace.curfilename))

      lines = lines + 1

      -- show context lines before posline
      for line = math.max(1, posline-contextb), posline-1 do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
    end
    if posline and oldline then
      -- show context lines between oldposline and posline
      for line = oldline+1, math.min(posline-1, oldline+contexta) do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
      if contextb + contexta > 0 and posline-oldline > contextb + contexta + 1 then
        text = text .. ("%5s\n"):format(("."):rep(#tostring(posline)))
      end
      for line = math.max(oldline+contexta+1, posline-contextb), posline-1 do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
    end
    if posline then
      text = text .. ("%5d: %s\n"):format(posline, getRawLine(editor, posline-1))
      findReplace.lines = findReplace.lines + 1
    elseif oldline then
      -- show context lines after posline
      for line = oldline+1, math.min(editor:GetLineCount(), oldline+contexta) do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
      text = text .. "\n"
    end
    oldline = posline

    reseditor:AppendTextDyn(text)

    for line = lines-1, reseditor:GetLineCount()-2 do
      reseditor:SetFoldLevel(line, wxstc.wxSTC_FOLDLEVELBASE + 1)
    end
  end

  if posline then
    findReplace.occurrences = findReplace.occurrences + 1

    -- get the added line
    local markline = reseditor:GetLineCount()-2
    -- get the match position in the file relative to the beginning of the line
    local localpos = pos - editor:PositionFromLine(posline-1)
    -- recalculate position in the search results relative to the line
    local newpos = reseditor:PositionFromLine(markline)+localpos+7 -- add indent
    reseditor:SetIndicatorCurrent(indicator.SEARCHMATCH)
    reseditor:IndicatorFillRange(newpos, length)
  end
end

local firstReadSize = 2048
local knownBinary = {}
local function checkBinary(ext, content)
  if not content then return knownBinary[ext] end
  if ext == "" then return IsBinary(content) end
  if knownBinary[ext] == nil then knownBinary[ext] = IsBinary(content) end
  return knownBinary[ext]
end

function findReplace:ProcInFiles(startdir,mask,subdirs)
  if not self.panel then self:createPanel() end

  local text = not self:GetFlags().RegularExpr and q(self.findCtrl:GetValue()) or nil
  if text and not self:GetFlags().MatchCase then
    text = text:gsub("%w",function(s) return "["..s:lower()..s:upper().."]" end)
  end

  local files = coroutine.wrap(function() FileSysGetRecursive(startdir, subdirs, mask, {yield = true, folder = false}) end)
  while true do
    local file = files()
    if not file then break end

    if checkBinary(GetFileExt(file)) ~= true then
      self.curfilename = file
      local filetext, err = FileRead(file, firstReadSize)
      if not filetext then
        DisplayOutputLn(TR("Can't open file '%s': %s"):format(file, err))
      elseif not checkBinary(GetFileExt(file), filetext) then
        -- read the rest if there is more to read in the file
        if #filetext == firstReadSize then filetext = FileRead(file) end
        if filetext and (not text or filetext:find(text)) then
          self.oveditor:SetTextDyn(filetext)

          if self:FindAll(onFileRegister) then self.files = self.files + 1 end

          -- give time to the UI to refresh
          ide:Yield()
          -- the IDE may be quitting after Yield or the tab may be closed,
          local ok, mgr = pcall(function() return ide:GetUIManager() end)
          -- so check to make sure the manager is still active
          if not (ok and mgr:GetPane(searchpanel):IsShown())
          -- and check that the search results tab is still open
          or not ide:IsValidCtrl(self.reseditor) then
            return false
          end
        end
      end
    end
  end
  return true
end

local function makePlural(word, counter) return word..(counter == 1 and '' or 's') end

function findReplace:RunInFiles(replace)
  if not self.panel then self:createPanel() end
  if not self:HasText() or self.oveditor then return end

  self.oveditor = ide:CreateStyledTextCtrl(self.panel, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(0,0), wx.wxBORDER_NONE)
  self.occurrences = 0
  self.lines = 0
  self.files = 0
  self.toolbar:UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
  ide:Yield() -- let the update of the UI happen

  -- save focus to restore after adding a page with search results
  local ctrl = ide:GetMainFrame():FindFocus()
  local findText = self.findCtrl:GetValue()
  local flags = self:GetFlags()
  local showaseditor = ide.config.search.showaseditor
  local nb = ide:GetOutputNotebook()
  local reseditor = self.reseditor
  local resultsText = "Search Results"
  local previewText = resultsText..": "
  local valid = self:IsPreview(reseditor)
  -- open new tab if the current one is not valid
  -- or if multiple tabs are requested, but when searching for different text
  if not valid or (flags.MultiResults and reseditor.searchpreview ~= findText) then
    -- enable folds in the preview even if disabled in the editor
    local fold = ide.config.editor.fold
    ide.config.editor.fold = true
    if showaseditor then
      reseditor = NewFile(resultsText)
    else
      reseditor = ide:CreateBareEditor()
      reseditor:SetupKeywords("")

      local modpref = ide.MODPREF
      local function setModified(modified)
        local index = nb:GetPageIndex(reseditor)
        local text = nb:GetPageText(index):gsub("^"..q(modpref), "")
        nb:SetPageText(index, (modified and modpref or '')..text)
      end
      reseditor:Connect(wxstc.wxEVT_STC_SAVEPOINTREACHED,
        function () setModified(false) end)
      reseditor:Connect(wxstc.wxEVT_STC_SAVEPOINTLEFT,
        function () setModified(true) end)
      reseditor:Connect(wxstc.wxEVT_STC_MARGINCLICK,
        function (event)
          local editor = event:GetEventObject():DynamicCast('wxStyledTextCtrl')
          local line = editor:LineFromPosition(event:GetPosition())
          local header = bit.band(editor:GetFoldLevel(line),
            wxstc.wxSTC_FOLDLEVELHEADERFLAG) == wxstc.wxSTC_FOLDLEVELHEADERFLAG
          if wx.wxGetKeyState(wx.WXK_SHIFT) and wx.wxGetKeyState(wx.WXK_CONTROL) then
            editor:FoldSome()
          elseif header then
            editor:ToggleFold(line)
          end
        end)

      -- mark as searchpreview to allow AddPage to add "close" button
      reseditor.searchpreview = findText
      nb:AddPage(reseditor, previewText, true)
    end
    reseditor:SetWrapMode(wxstc.wxSTC_WRAP_NONE)
    reseditor:SetIndentationGuides(false)
    if tonumber(ide.config.search.zoom) then
      reseditor:SetZoom(tonumber(ide.config.search.zoom))
    end
    for m = 0, ide.MAXMARGIN do -- hide all margins except folding
      if reseditor:GetMarginWidth(m) > 0
      and reseditor:GetMarginMask(m) ~= wxstc.wxSTC_MASK_FOLDERS then
        reseditor:SetMarginWidth(m, 0)
      end
    end
    reseditor:MarkerDefine(ide:GetMarker("searchmatchfile"))
    reseditor:Connect(wx.wxEVT_LEFT_DCLICK, function(event)
        if not wx.wxGetKeyState(wx.WXK_SHIFT)
        and not wx.wxGetKeyState(wx.WXK_CONTROL)
        and not wx.wxGetKeyState(wx.WXK_ALT) then
          local point = event:GetPosition()
          local margin = 0
          for m = 0, ide.MAXMARGIN do margin = margin + reseditor:GetMarginWidth(m) end
          if point:GetX() <= margin then return end

          local pos = reseditor:PositionFromPoint(point)
          local line = reseditor:LineFromPosition(pos)
          local text = reseditor:GetLineDyn(line):gsub("[\n\r]+$","")
          -- get line with the line number
          local jumpline = text:match("^%s*(%d+)")
          local file
          if jumpline then
            -- search back to find the file name
            for curline = line-1, 0, -1 do
              local text = reseditor:GetLineDyn(curline):gsub("[\n\r]+$","")
              if not text:find("^%s") and wx.wxFileExists(text) then
                file = text
                break
              end
            end
          else
            file = text
            jumpline = 1
          end

          -- activate the file and the line number
          local editor = file and LoadFile(file,nil,true)
          if editor then
            editor:GotoLine(jumpline-1)
            editor:EnsureVisibleEnforcePolicy(jumpline-1)
            editor:SetFocus()
          end
          return
        end

        event:Skip()
      end)

    ide.config.editor.fold = fold
    self.reseditor = reseditor
  else
    if showaseditor then
      ide:GetDocument(reseditor):SetActive()
    else
      local index = nb:GetPageIndex(reseditor)
      if nb:GetSelection() ~= index then nb:SetSelection(index) end
    end
  end
  reseditor.replace = replace -- keep track of the current status
  reseditor:ShowLines(0, reseditor:GetLineCount()-1)
  reseditor:SetReadOnly(false)
  reseditor:SetTextDyn('')
  do -- update the preview name
    local nb = showaseditor and ide:GetEditorNotebook() or nb
    nb:SetPageText(nb:GetPageIndex(reseditor), previewText .. findText)
  end
  if not showaseditor and nb then -- show the bottom notebook if hidden
    local uimgr = ide:GetUIManager()
    if not uimgr:GetPane(nb):IsShown() then
      uimgr:GetPane(nb):Show(true)
      uimgr:Update()
    end
  end

  self:SetStatus(TR("Searching for '%s'."):format(findText))
  wx.wxSafeYield() -- allow the status to update

  local startdir, mask = self:GetScope()
  local completed = self:ProcInFiles(startdir, mask or "*", flags.SubDirs)

  -- reseditor may already be closed, so check if it's valid first
  if ide:IsValidCtrl(reseditor) then
    reseditor:GotoPos(reseditor:GetLength())
    reseditor:AppendTextDyn(("Searched for '%s'. "):format(findText))
    if not completed then reseditor:AppendTextDyn("Cancelled by the user. ") end
    reseditor:AppendTextDyn(("Found %d %s on %d %s in %d %s.")
      :format(
        self.occurrences, makePlural("instance", self.occurrences),
        self.lines, makePlural("line", self.lines),
        self.files, makePlural("file", self.files)))
    reseditor:EmptyUndoBuffer() -- don't undo the changes in the results
    reseditor:SetSavePoint() -- set unmodified status

    if completed and replace and self.occurrences > 0 then
      reseditor:AppendTextDyn("\n\n"
        .."Review the changes and save this preview to apply them.\n"
        .."You can also make other changes; only lines with : will be updated.\n"
        .."Context lines (if any) are used as safety checks during the update.")
      self:Replace(true, reseditor)
    else
      reseditor:SetReadOnly(true)
    end
    reseditor:EnsureVisibleEnforcePolicy(reseditor:GetLineCount()-1)
    reseditor.searchpreview = findText
  end

  self:SetStatus(not completed and TR("Cancelled by the user.")
    or TR("Found %d instance.", self.occurrences):format(self.occurrences))
  self.oveditor:Destroy()
  self.oveditor = nil
  self.toolbar:UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)

  -- return focus to the control that had it if it's on the search panel
  -- (as it could be changed by added results tab)
  if ctrl and (ctrl:GetParent():GetId() == self.panel:GetId() or not showaseditor) then
    -- set the focus temporarily on the search results tab as this provides a workaround
    -- for the cursor disappearing in Search/Replace controls after results shown
    -- in the same tab (somehow caused by `oveditor:Destroy()` call).
    if ide:IsValidCtrl(reseditor) then reseditor:SetFocus() end
    ctrl:SetFocus()
  end

  if completed and ide.config.search.autohide then self:Hide() end
end

local icons = {
  find = {
    internal = {
      ID_FINDNEXT, ID_SEPARATOR,
      ID_FINDOPTDIRECTION, ID_FINDOPTWRAPWROUND, ID_FINDOPTSELECTION,
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
    infiles = {
      ID_FINDNEXT, ID_SEPARATOR,
      ID_FINDOPTCONTEXT, ID_FINDOPTMULTIRESULTS, ID_FINDOPTWORD,
      ID_FINDOPTCASE, ID_FINDOPTREGEX, ID_FINDOPTSUBDIR,
      ID_FINDOPTSCOPE, ID_FINDSETDIR,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
  },
  replace = {
    internal = {
      ID_FINDNEXT, ID_FINDREPLACENEXT, ID_FINDREPLACEALL, ID_SEPARATOR,
      ID_FINDOPTDIRECTION, ID_FINDOPTWRAPWROUND, ID_FINDOPTSELECTION,
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
    infiles = {
      ID_FINDNEXT, ID_FINDREPLACEALL, ID_SEPARATOR,
      ID_FINDOPTCONTEXT, ID_FINDOPTMULTIRESULTS, ID_FINDOPTWORD,
      ID_FINDOPTCASE, ID_FINDOPTREGEX, ID_FINDOPTSUBDIR,
      ID_FINDOPTSCOPE, ID_FINDSETDIR,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
  },
}

function findReplace:createToolbar()
  local ctrl, tb, scope, status =
    self.panel, self.toolbar, self.scope, self.status
  local icons = icons[self.replace and "replace" or "find"][self.infiles and "infiles" or "internal"]

  local toolBmpSize = wx.wxSize(16, 16)
  tb:Freeze()
  tb:Clear()
  for _, id in ipairs(icons) do
    if id == ID_SEPARATOR then
      tb:AddSeparator()
    elseif id == ID_FINDOPTSCOPE then
      tb:AddControl(scope)
    elseif id == ID_FINDOPTSTATUS then
      tb:AddControl(status)
    else
      local iconmap = ide.config.toolbar.iconmap[id]
      if iconmap then
        local icon, description = unpack(iconmap)
        local isbitmap = type(icon) == "userdata" and icon:GetClassInfo():GetClassName() == "wxBitmap"
        local bitmap = isbitmap and icon or ide:GetBitmap(icon, "TOOLBAR", toolBmpSize)
        tb:AddTool(id, "", bitmap, (TR)(description))
      end
    end
  end

  local options = {
    [ID_FINDOPTDIRECTION] = 'Down',
    [ID_FINDOPTWRAPWROUND] = 'Wrap',
    [ID_FINDOPTWORD] = 'WholeWord',
    [ID_FINDOPTCASE] = 'MatchCase',
    [ID_FINDOPTREGEX] = 'RegularExpr',
    [ID_FINDOPTSUBDIR] = 'SubDirs',
    [ID_FINDOPTCONTEXT] = 'Context',
    [ID_FINDOPTMULTIRESULTS] = 'MultiResults',
  }

  for id, var in pairs(options) do
    local tool = tb:FindTool(id)
    if tool then
      local flags = self:GetFlags()
      tool:SetSticky(flags[var])
      ctrl:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
        function ()
          flags[var] = not flags[var]
          self:SaveSettings()

          tb:FindTool(id):SetSticky(flags[var])
          tb:Refresh()
        end)
    end
  end

  local optseltool = tb:FindTool(ID_FINDOPTSELECTION)
  if optseltool then
    optseltool:SetSticky(self.inselection)
    tb:EnableTool(ID_FINDOPTSELECTION, self.inselection)
    ctrl:Connect(ID_FINDOPTSELECTION, wx.wxEVT_COMMAND_MENU_SELECTED,
      function (event)
        self.inselection = not self.inselection
        tb:FindTool(event:GetId()):SetSticky(self.inselection)
        tb:Refresh()
      end)
  end

  tb:SetToolDropDown(ID_FINDSETDIR, true)
  tb:Connect(ID_FINDSETDIR, wxaui.wxEVT_COMMAND_AUITOOLBAR_TOOL_DROPDOWN, function(event)
      if event:IsDropDownClicked() then
        local menu = wx.wxMenu()
        local pos = tb:GetToolRect(event:GetId()):GetBottomLeft()
        menu:Append(ID_FINDSETDIR, TR("Choose..."))
        menu:Append(ID_FINDSETTOPROJDIR, TR("Set To Project Directory"))
        menu:Enable(ID_FINDSETTOPROJDIR, ide:GetProject() ~= nil)
        menu:Connect(ID_FINDSETTOPROJDIR, wx.wxEVT_COMMAND_MENU_SELECTED,
          function()
            local _, mask = self:GetScope()
            self:refreshToolbar(self:SetScope(ide:GetProject(), mask))
          end)
        if #self.settings.slist > 0 then menu:AppendSeparator() end
        for i, text in ipairs(self.settings.slist) do
          local id = ID("findreplace.scope."..i)
          menu:Append(id, text)
          menu:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
            function() self:refreshToolbar(text) end)
        end
        menu:AppendSeparator()
        menu:Append(ID_RECENTSCOPECLEAR, TR("Clear Items"))
        menu:Enable(ID_RECENTSCOPECLEAR, #self.settings.slist > 0)
        menu:Connect(ID_RECENTSCOPECLEAR, wx.wxEVT_COMMAND_MENU_SELECTED,
          function()
            self.settings.slist = {}
            self:SaveSettings()
          end)
        tb:PopupMenu(menu, pos)
      else
        event:Skip()
      end
    end)

  tb:Realize()
  tb:Thaw()

  local sizer = ctrl:GetSizer()
  if sizer then sizer:Layout() end
end

function findReplace:refreshToolbar(value)
  local scope = self.scope
  value = value or self.scope:GetValue()
  self.scope:SetMinSize(wx.wxSize(scope:GetTextExtent(value..'AZ'), -1))
  self:createToolbar()
  self.scope:SetValue(value)
end

function findReplace:createPanel()
  local ctrl = wx.wxPanel(ide:GetMainFrame(), wx.wxID_ANY, wx.wxDefaultPosition,
      wx.wxDefaultSize, wx.wxFULL_REPAINT_ON_RESIZE)
  local mgr = ide:GetUIManager()
  mgr:AddPane(ctrl, wxaui.wxAuiPaneInfo()
    :Name(searchpanel):CaptionVisible(false):PaneBorder(false):Hide())
  mgr:Update()

  local tb = wxaui.wxAuiToolBar(ctrl, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize, wxaui.wxAUI_TB_PLAIN_BACKGROUND)
  local status = wx.wxStaticText(tb, wx.wxID_ANY, "")
  local scope = wx.wxTextCtrl(tb, wx.wxID_ANY, "",
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)
  -- limit the scope control height as it gets too large on Linux
  scope:SetMaxSize(wx.wxSize(-1, 22))

  self.panel = ctrl
  self.status = status
  self.toolbar = tb
  self.scope = scope

  self:createToolbar()

  local style, styledef = ide.config.styles, StylesGetDefault()
  local textcolor = wx.wxColour(unpack(style.text.fg or styledef.text.fg))
  local backcolor = wx.wxColour(unpack(style.text.bg or styledef.text.bg))
  local pancolor = tb:GetBackgroundColour()
  local borcolor = ide:GetUIManager():GetArtProvider():GetColor(wxaui.wxAUI_DOCKART_BORDER_COLOUR)
  local bpen = wx.wxPen(borcolor, 1, wx.wxSOLID)
  local bbrush = wx.wxBrush(pancolor, wx.wxSOLID)
  local tfont = ide:GetProjectTree():GetFont()
  -- don't increase font size on Linux as it gets too large
  tfont:SetPointSize(tfont:GetPointSize() + (ide.osname == 'Unix' and 0 or 1))

  local findCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, "",
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)
  local replaceCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, replaceHintText,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)
  self.ac = {[findCtrl:GetId()] = {}, [replaceCtrl:GetId()] = {}, [scope:GetId()] = {}}

  local findSizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
  findSizer:Add(findCtrl, 1, wx.wxLEFT + wx.wxRIGHT + wx.wxALIGN_LEFT + wx.wxEXPAND + wx.wxFIXED_MINSIZE, 1)
  findSizer:Add(replaceCtrl, 1, wx.wxLEFT + wx.wxRIGHT + wx.wxALIGN_LEFT + wx.wxEXPAND + wx.wxFIXED_MINSIZE, 1)
  findSizer:Hide(1)

  local mainSizer = wx.wxBoxSizer(wx.wxVERTICAL)
  mainSizer:Add(tb, 0, wx.wxTOP + wx.wxLEFT + wx.wxRIGHT + wx.wxALIGN_LEFT + wx.wxEXPAND, 2)
  mainSizer:Add(findSizer, 0, wx.wxALL + wx.wxALIGN_LEFT + wx.wxEXPAND, 2)

  ctrl:SetSizer(mainSizer)
  ctrl:GetSizer():Fit(ctrl)

  for _, control in ipairs({findCtrl, replaceCtrl}) do
    control:SetBackgroundColour(backcolor)
    control:SetForegroundColour(textcolor)
    control:SetFont(tfont)
  end
  scope:SetBackgroundColour(pancolor) -- set toolbar background
  scope:SetFont(tfont)
  status:SetFont(tfont)

  local function updateLists()
    PrependStringToArray(self.settings.flist, findCtrl:GetValue())
    if self.replace then
      local replaceText = replaceCtrl:GetValue()
      if replaceText == replaceHintText then replaceText = "" end
      PrependStringToArray(self.settings.rlist, replaceText)
    end
    if self.infiles then
      PrependStringToArray(self.settings.slist, self.scope:GetValue())
    end
    self:SaveSettings()
    return true
  end

  local function findNext()
    updateLists()
    if findReplace.infiles then
      findReplace:RunInFiles(false)
    else
      findReplace:Find()
    end
  end

  local function autoComplete(event)
    if not ide.config.search.autocomplete then return end

    local obj = event:GetEventObject():DynamicCast('wxTextCtrl')
    local ac = self.ac[obj:GetId()]
    if not ac then return end

    local keycode, needac = ac.lastkeycode, ac.needautocomplete
    if needac then ac.needautocomplete = false end
    if not needac or not keycode then return end

    -- if the last key was Delete or Backspace, don't autocomplete
    if keycode == wx.WXK_DELETE or keycode == wx.WXK_BACK then return end

    -- find match for the current text and add it to the control
    local value = obj:GetValue()
    if not value or #value == 0 then return end

    local patt, match = "^"..q(value)
    for _, v in ipairs(
      obj:GetId() == self.findCtrl:GetId() and self.settings.flist or
      obj:GetId() == self.replaceCtrl:GetId() and self.settings.rlist or
      {}
    ) do
      if v:find(patt) then match = v; break end
    end
    if match then
      obj:ChangeValue(match)
      obj:SetSelection(#value, #match)
    end
  end

  local function findIncremental(event)
    -- don't do any incremental search when search in selection
    if self.inselection then return end

    if not self.infiles and self.backfocus and self.backfocus.position then
      self:GetEditor():SetSelection(self.backfocus.position, self.backfocus.position)
    end
    -- don't search when used with "infiles", but still trigger autocomplete
    if self.infiles or self:Find() then
      self.ac[event:GetEventObject():DynamicCast('wxTextCtrl'):GetId()].needautocomplete = true
    end
  end

  local function findReplaceNext()
    updateLists()
    if findReplace.replace then
      if findReplace.infiles then
        findReplace:RunInFiles(true)
      else
        local replaceAll = (wx.wxGetKeyState(wx.WXK_ALT)
          and not wx.wxGetKeyState(wx.WXK_SHIFT) and not wx.wxGetKeyState(wx.WXK_CONTROL))
        findReplace:Replace(replaceAll)
      end
    end
  end

  local function findReplaceAll()
    updateLists()
    if findReplace.replace then
      if findReplace.infiles then
        findReplace:RunInFiles(true)
      else
        findReplace:Replace(true)
      end
    end
  end

  local function onPanelPaint()
    local dc = wx.wxBufferedPaintDC(ctrl)
    local psize = ctrl:GetClientSize()
    dc:SetBrush(bbrush)
    dc:SetPen(bpen)
    dc:DrawRectangle(0, 0, psize:GetWidth(), psize:GetHeight())
    dc:SetPen(wx.wxNullPen)
    dc:SetBrush(wx.wxNullBrush)
    dc:delete()
  end

  ctrl:Connect(wx.wxEVT_PAINT, onPanelPaint)
  ctrl:Connect(wx.wxEVT_ERASE_BACKGROUND, function() end)

  local taborder = {findCtrl, replaceCtrl, scope}
  local function keyHandle(event)
    local keycode = event:GetKeyCode()
    self.ac[event:GetEventObject():DynamicCast('wxTextCtrl'):GetId()].lastkeycode = keycode
    if keycode == wx.WXK_ESCAPE then
      self:Hide(event:ShiftDown())
    elseif keycode == wx.WXK_TAB then
      local id = event:GetId()
      local order, pos = {}
      for _, v in ipairs(taborder) do
        if v:IsEnabled() and v:IsShown() then table.insert(order, v) end
        if v:GetId() == id then pos = #order end
      end
      if not pos then return end
      pos = pos + (event:ShiftDown() and -1 or 1)
      if pos == 0 then pos = #order
      elseif pos > #order then pos = 1
      end
      order[pos]:SetFocus()
      if order[pos] ~= scope then order[pos]:SetSelection(-1, -1) end
    else
      event:Skip()
    end
  end

  -- remember the current position in the editor when setting focus on find
  local function refreshEditorInfo()
    local ed = self:GetEditor()
    if ed and ed ~= self.oveditor then
      local spos, epos = ed:GetSelectionStart(), ed:GetSelectionEnd()
      if not self.backfocus or self.backfocus.editor ~= ed then
        self.backfocus = { editor = ed, spos = spos, epos = epos }
      end
      local bf = self.backfocus
      bf.position = spos == epos and ed:GetCurrentPos() or spos
      local inselection = ed:LineFromPosition(spos) ~= ed:LineFromPosition(epos)

      -- when the focus is changed, don't remove current "inselection" status as the
      -- selection may change to highlight the match; not doing this makes it difficult
      -- to switch between searching and replacing without losing the current match
      if inselection and (not self.inselection or bf.spos ~= spos or bf.epos ~= epos) then
        bf.spos = spos
        bf.epos = epos
        self.inselection = inselection
        self:refreshToolbar()
      end
    end
  end
  findCtrl:Connect(wx.wxEVT_SET_FOCUS,
    function(event)
      event:Skip()
      refreshEditorInfo()
    end)
  findCtrl:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findNext)
  findCtrl:Connect(wx.wxEVT_COMMAND_TEXT_UPDATED, findIncremental)
  findCtrl:Connect(wx.wxEVT_KEY_DOWN, keyHandle)
  replaceCtrl:Connect(wx.wxEVT_SET_FOCUS, function(event)
      event:Skip()
      refreshEditorInfo()
      -- hide the replace hint; should be done with SetHint method,
      -- but it's not yet available in wxlua 2.8.12
      if replaceCtrl:GetValue() == replaceHintText then replaceCtrl:ChangeValue('') end
    end)
  replaceCtrl:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findReplaceNext)
  replaceCtrl:Connect(wx.wxEVT_COMMAND_TEXT_UPDATED, function(event)
      self.ac[event:GetEventObject():DynamicCast('wxTextCtrl'):GetId()].needautocomplete = true
    end)
  replaceCtrl:Connect(wx.wxEVT_KEY_DOWN, keyHandle)

  -- autocomplete for find/replace can be done from TEXT_UPDATED event,
  -- but SetSelection doesn't work from TEXT_UPDATED event on Linux,
  -- which makes it impossible to select the suggested part.
  -- IDLE event is used instead to provide autocomplete suggestions.
  findCtrl:Connect(wx.wxEVT_IDLE, autoComplete)
  replaceCtrl:Connect(wx.wxEVT_IDLE, autoComplete)

  scope:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findNext)
  scope:Connect(wx.wxEVT_KEY_DOWN, keyHandle)

  local function notSearching(event) event:Enable(not self.oveditor) end
  ctrl:Connect(ID_FINDNEXT, wx.wxEVT_UPDATE_UI, notSearching)
  ctrl:Connect(ID_FINDREPLACENEXT, wx.wxEVT_UPDATE_UI, notSearching)
  ctrl:Connect(ID_FINDREPLACEALL, wx.wxEVT_UPDATE_UI, notSearching)

  ctrl:Connect(ID_FINDNEXT, wx.wxEVT_COMMAND_MENU_SELECTED, findNext)
  ctrl:Connect(ID_FINDREPLACENEXT, wx.wxEVT_COMMAND_MENU_SELECTED, findReplaceNext)
  ctrl:Connect(ID_FINDREPLACEALL, wx.wxEVT_COMMAND_MENU_SELECTED, findReplaceAll)

  ctrl:Connect(ID_FINDSETDIR, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      local dir, mask = self:GetScope()
      local filePicker = wx.wxDirDialog(ctrl, TR("Choose a search directory"),
        dir or wx.wxGetCwd(), wx.wxFLP_USE_TEXTCTRL)
      if filePicker:ShowModal(true) == wx.wxID_OK then
        self:refreshToolbar(self:SetScope(FixDir(filePicker:GetPath()), mask))
      end
    end)

  self.findCtrl = findCtrl
  self.replaceCtrl = replaceCtrl
  self.findSizer = findSizer
end

function findReplace:refreshPanel(replace, infiles)
  if not self.panel then self:createPanel() end

  self:GetEditor(true) -- remember the current editor

  local ctrl = self.panel

  -- check if a proper pane is already populated
  if self.replace ~= replace or self.infiles ~= infiles then
    self.replace = replace
    self.infiles = infiles

    if replace then
      self.findSizer:Show(1)
      if self.replaceCtrl:GetValue() == '' then
        self.replaceCtrl:ChangeValue(replaceHintText)
      end
    else
      self.findSizer:Hide(1)
    end
    self.findSizer:Layout()

    self.scope:Show(infiles)
  end

  local value = self.scope:GetValue()
  local ed = ide:GetEditor()
  if ed and (not value or #value == 0) then
    local doc = ide:GetDocument(ed)
    local ext = doc:GetFileExt()
    local proj = ide:GetProject()
    value = (proj and self:GetScopeMRU(proj..sep) or
      self:SetScope(proj or wx.wxGetCwd(), '*.'..(#ext > 0 and ext or '*')))
  end
  if ed then -- check if there is any selection
    self.backfocus = nil
    self.inselection = ed:LineFromPosition(ed:GetSelectionStart()) ~=
      ed:LineFromPosition(ed:GetSelectionEnd())
  end
  self:refreshToolbar(value)

  local mgr = ide:GetUIManager()
  local pane = mgr:GetPane(searchpanel)
  if not pane:IsShown() then
    local size = ctrl:GetSize()
    pane:Dock():Bottom():BestSize(size):MinSize(size):Layer(0):Row(1):Show()
    mgr:Update()

    self:SetStatus(TR("Use %s to close."):format("`Escape`"))
  end

  -- set value from the current selection (if any)
  self.findCtrl:ChangeValue(self:GetSelection() or self.findCtrl:GetValue())

  -- reset search when re-creating dialog to avoid modifying selected
  -- fragment after successful search and updated replacement
  self.foundString = false
  self.findCtrl:SetFocus()
  self.findCtrl:SetSelection(-1, -1) -- select the content
end

function findReplace:Show(replace,infiles)
  self:refreshPanel(replace,infiles)
end

function findReplace:IsShown()
  local pane = ide:GetUIManager():GetPane(searchpanel)
  return pane:IsOk() and pane:IsShown()
end

function findReplace:Hide(restorepos)
  local ctrl = self.panel:FindFocus()
  if not ctrl or ctrl:GetParent():GetId() ~= self.panel:GetId() then
    -- if focus outside of the search panel, do nothing
  elseif self.backfocus and ide:IsValidCtrl(self.backfocus.editor) then
    local editor = self.backfocus.editor
    -- restore original position for Shift-Esc or failed search
    if restorepos or self.foundString == false then
      editor:SetSelection(self.backfocus.spos, self.backfocus.epos)
    end
    editor:SetFocus()
  elseif self:IsPreview(self.reseditor) then -- there is a preview, go there
    self.reseditor:SetFocus()
  end

  local mgr = ide:GetUIManager()
  mgr:GetPane(searchpanel):Hide()
  mgr:Update()
end

local package = ide:AddPackage('core.findreplace', {
    onProjectLoad = function()
      if not findReplace.panel then return end -- not set yet
      local _, mask = findReplace:GetScope()
      local proj = ide:GetProject()
      -- find the last used scope for the same project on the scope history
      findReplace:refreshToolbar(findReplace:GetScopeMRU(proj..sep)
        or findReplace:SetScope(proj, mask))
    end,

    onEditorPreSave = function(self, editor, filePath)
      if not findReplace:IsPreview(editor) then return end

      local isModified = editor:GetModify()
      if editor.replace and isModified then
        findReplace:SetStatus("")

        local line = NOTFOUND
        local oveditor = ide:CreateStyledTextCtrl(findReplace.panel, wx.wxID_ANY,
          wx.wxDefaultPosition, wx.wxSize(0,0), wx.wxBORDER_NONE)
        local files, lines = 0, 0
        local report
        while true do
          -- for each marker that marks a file (MarkerNext)
          line = editor:MarkerNext(line + 1, FILE_MARKER_VALUE)
          if line == NOTFOUND then break end

          local fname = getRawLine(editor, line) -- get the file name
          local filetext, err = FileRead(fname)
          local mismatch = false
          if filetext then
            findReplace:SetStatus(GetFileName(fname))
            wx.wxSafeYield()

            oveditor:SetTextDyn(filetext)
            while true do -- for each line following the file name
              line = line + 1
              local text = getRawLine(editor, line)
              local lnum, lmark, ltext = text:match("^%s*(%d+)([ :]) (.*)")
              if lnum then
                lnum = tonumber(lnum)
                if lmark == ':' then -- if the change line, then apply the change
                  local pos = oveditor:PositionFromLine(lnum-1)
                  if pos == NOTFOUND then
                    mismatch = lnum
                    break
                  end
                  oveditor:SetTargetStart(pos)
                  oveditor:SetTargetEnd(pos+#getRawLine(oveditor, lnum-1))
                  oveditor:ReplaceTarget(ltext)
                  lines = lines + 1
                -- if the context line, then check the context
                elseif getRawLine(oveditor, lnum-1) ~= ltext then
                  mismatch = lnum
                  break
                end
              -- if not placeholder line " ...", then abort
              elseif not text:find("^%s*%.+$") then
                break
              end
            end
            if lines > 0 and not mismatch then -- save the file
              local ok
              ok, err = FileWrite(fname, oveditor:GetTextDyn())
              if ok then files = files + 1 end
            end
          end
          if err or mismatch then
            report = (report or "") .. (("\n%s: %s")
              :format(fname, mismatch and "mismatch on line "..mismatch or err))
          end
        end
        oveditor:Destroy() -- destroy the editor to release its memory
        if report then editor:AppendTextDyn("\n"..report) end
        editor:AppendTextDyn(("\n\nUpdated %d %s in %d %s.")
          :format(
            lines, makePlural("line", lines),
            files, makePlural("file", files)))
        editor:EnsureVisibleEnforcePolicy(editor:GetLineCount()-1)
        editor:SetSavePoint() -- set unmodified status when done
        findReplace:SetStatus(TR("Updated %d file.", files):format(files))
        return false

      -- don't offer to save file if called from SaveFile;
      -- can still be used with explicit SaveFileAs
      elseif not filePath and not isModified then
        return false
      end
    end
  })

function findReplace:SaveSettings() package:SetSettings(self.settings) end
MergeSettings(findReplace.settings, package:GetSettings())
