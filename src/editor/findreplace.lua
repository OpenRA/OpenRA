-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local searchpanel = 'searchpanel'
local q = EscapeMagic
ide.findReplace = {
  panel = nil, -- the control for find/replace
  replace = false, -- is it a find or replace
  infiles = false,
  startpos = nil,
  cureditor = nil, -- the editor being searched
  reseditor = nil, -- the editor for search results
  oveditor = nil, -- the editor is used for search during find-in-files
  searchCtrl = nil, -- the control that has the search text
  replaceCtrl = nil, -- the control that has the replace text

  fWholeWord = false, -- match whole words
  fMatchCase = false, -- case sensitive
  fRegularExpr = false, -- use regex
  fWrap = true, -- search wraps around
  fDown = true, -- search downwards in doc
  fContext = true, -- include context in search results
  fSubDirs = true, -- search in subdirectories

  findTextArray = {}, -- array of last entered find text
  findText = "", -- string to find
  replaceTextArray = {}, -- array of last entered replace text
  replaceText = "", -- string to replace find string with
  scopeText = nil,
  scopeTextArray = {},

  foundString = false, -- was the string found for the last search

  curfilename = "", -- for search in files
  occurrences = 0,
  files = 0,

  -- HasText() is there a string to search for
  -- GetSelectedString() get currently selected string if it's on one line
  -- FindString(reverse) find the findText string
  -- Show(replace) create the dialog
  -- GetEditor() which editor to use
}
local findReplace = ide.findReplace
local NOTFOUND = -1
local replaceHintText = '<replace with>'
local sep = ';'

function findReplace:GetEditor(reset)
  if reset then self.cureditor = nil end
  self.cureditor = ide:GetEditorWithLastFocus() or self.cureditor
  return self.oveditor or self.cureditor or GetEditor()
end

-------------------- Find replace dialog

local function setSearchFlags(editor)
  local flags = wxstc.wxSTC_FIND_POSIX
  if findReplace.fWholeWord then flags = flags + wxstc.wxSTC_FIND_WHOLEWORD end
  if findReplace.fMatchCase then flags = flags + wxstc.wxSTC_FIND_MATCHCASE end
  if findReplace.fRegularExpr then flags = flags + wxstc.wxSTC_FIND_REGEXP end
  editor:SetSearchFlags(flags)
end

local function setTarget(editor, fDown, fAll, fWrap)
  local selStart = editor:GetSelectionStart()
  local selEnd = editor:GetSelectionEnd()
  local len = editor:GetLength()
  local s, e
  if fDown then
    s = iff(fAll, selStart, selEnd)
    e = len
  else
    s = 0
    e = iff(fAll, selEnd, selStart)
  end
  -- if going up and not search/replace All, then switch the range to
  -- allow the next match to be properly marked
  if not fDown and not fAll then s, e = e, s end
  -- if wrap around and search all requested, then search the entire document
  if fAll and fWrap then s, e = 0, len end
  editor:SetTargetStart(s)
  editor:SetTargetEnd(e)
  return e
end

local function setTargetAll(editor)
  local s = 0
  local e = editor:GetLength()

  editor:SetTargetStart(s)
  editor:SetTargetEnd(e)

  return e
end

function findReplace:HasText()
  return (self.findText ~= nil) and (string.len(self.findText) > 0)
end

function findReplace:SetStatus(msg) self.status:SetLabel(msg) end

function findReplace:GetScope()
  if not self.scopeText then return end
  local dir, mask = self.scopeText:match(('([^%s]*)%s%%s*(.+)'):format(sep,sep))
  if not dir then dir = self.scopeText end
  -- trip leading/trailing spaces from the directory
  dir = dir:gsub("^%s+",""):gsub("%s+$","")
  -- if the directory doesn't exist, treat it as the extension(s)
  if not wx.wxDirExists(dir) then
    dir, mask = ide:GetProject() or wx.wxGetCwd(), (#dir > 0 and dir or nil)
  end
  return dir, mask
end

function findReplace:SetScope(dir, mask)
  self.scopeText = dir .. (mask and (sep..' '..mask) or "")
  return self.scopeText
end

function findReplace:GetSelectedString()
  local editor = self:GetEditor()
  if editor then
    local startSel = editor:GetSelectionStart()
    local endSel = editor:GetSelectionEnd()
    if (startSel ~= endSel)
    and (editor:LineFromPosition(startSel) == editor:LineFromPosition(endSel)) then
      return editor:GetTextRange(startSel, endSel)
    end
  end
  return
end

function findReplace:FindString(reverse)
  local editor = self:GetEditor()
  if editor and self:HasText() then
    local fDown = iff(reverse, not self.fDown, self.fDown)
    setSearchFlags(editor)
    setTarget(editor, fDown)
    local posFind = editor:SearchInTarget(self.findText)
    local msg = ""
    if (posFind == NOTFOUND) and self.fWrap then
      editor:SetTargetStart(iff(fDown, 0, editor:GetLength()))
      editor:SetTargetEnd(iff(fDown, editor:GetLength(), 0))
      posFind = editor:SearchInTarget(self.findText)
      msg = TR("Reached end of text and wrapped around.")
    end
    if posFind == NOTFOUND then
      self.foundString = false
      self:SetStatus(TR("Text not found."))
    else
      self.foundString = true
      local start = editor:GetTargetStart()
      local finish = editor:GetTargetEnd()
      editor:ShowPosEnforcePolicy(finish)
      editor:SetSelection(start, finish)
      self:SetStatus(msg)
    end
  end
end

-- returns if something was found
-- [inFileRegister(pos)] passing function will
-- register every position item was found
-- supposed for "Search/Replace in Files"

function findReplace:FindStringAll(inFileRegister)
  local found = false
  local editor = self:GetEditor()
  if editor and self:HasText() then
    local e = setTargetAll(editor)

    setSearchFlags(editor)
    while true do
      local posFind = editor:SearchInTarget(self.findText)
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

-- returns if replacements were done
-- [inFileRegister(pos)] passing function will disable "undo"
-- registers every position item was found
-- supposed for "Search/Replace in Files"

local indicator = {SEARCHMATCH = 5}

function findReplace:ReplaceString(fReplaceAll, resultsEditor)
  local replaced = false
  local editor = resultsEditor or self:GetEditor()
  if editor and self:HasText() then
    -- don't replace in read-only editors
    if editor:GetReadOnly() then
      self:SetStatus(TR("Can't replace in read-only text."))
      return false
    end

    local endTarget = resultsEditor and setTargetAll(editor) or
      setTarget(editor, self.fDown, fReplaceAll, self.fWrap)

    if fReplaceAll then
      if resultsEditor then editor:SetIndicatorCurrent(indicator.SEARCHMATCH) end

      setSearchFlags(editor)
      local occurrences = 0
      local posFind = editor:SearchInTarget(self.findText)
      if posFind ~= NOTFOUND then
        editor:BeginUndoAction()
        while posFind ~= NOTFOUND do
          local length = editor:GetLength()
          -- if no replace-in-files or the match doesn't start with %d:
          if not resultsEditor
          or editor:GetLine(editor:LineFromPosition(posFind)):find("^%s*%d:") then
            local replaced = self.fRegularExpr
              and editor:ReplaceTargetRE(self.replaceText)
              or editor:ReplaceTarget(self.replaceText)

            -- mark replaced text
            if resultsEditor then editor:IndicatorFillRange(posFind, replaced) end
          end

          editor:SetTargetStart(editor:GetTargetEnd())
          -- adjust the endTarget as the position could have changed;
          -- can't simply subtract findText length as it could be a regexp
          endTarget = endTarget + (editor:GetLength() - length)
          editor:SetTargetEnd(endTarget)
          posFind = editor:SearchInTarget(self.findText)
          occurrences = occurrences + 1
        end
        editor:EndUndoAction()
        replaced = true
      end
      self:SetStatus(("%s %s."):format(
        TR("Replaced"), TR("%d instance", occurrences):format(occurrences)))
    else
      editor:TargetFromSelection()
      -- check if there is anything selected as well as the user can
      -- move the cursor after successful search
      if editor:GetSelectionStart() ~= editor:GetSelectionEnd()
      -- check that the current selection matches what's being searched for
      and editor:SearchInTarget(self.findText) ~= NOTFOUND then
        local start = editor:GetSelectionStart()
        local replaced = self.fRegularExpr
          and editor:ReplaceTargetRE(self.replaceText)
          or editor:ReplaceTarget(self.replaceText)

        editor:SetSelection(start, start + replaced)
        self.foundString = false

        replaced = true
      end
      self:FindString()
    end
  end

  return replaced
end

local oldline
local FILE_MARKER = ide:GetMarker("searchmatchfile")
local FILE_MARKER_VALUE = 2^FILE_MARKER
local function getRawLine(ed, line) return (ed:GetLine(line):gsub("[\n\r]+$","")) end
local function onFileRegister(pos, length)
  local editor = findReplace.oveditor
  local reseditor = findReplace.reseditor
  local posline = pos and editor:LineFromPosition(pos) + 1
  local text = ""
  local context = findReplace.fContext and 2 or 0
  local lines = reseditor:GetLineCount() -- current number of lines

  -- check if there is another match on the same line; do not add anything
  if oldline ~= posline then
    if posline and not oldline then
      -- show file name and a bookmark marker
      reseditor:AppendText(findReplace.curfilename.."\n")
      reseditor:MarkerAdd(lines-1, FILE_MARKER)
      reseditor:SetFoldLevel(lines-1, reseditor:GetFoldLevel(lines-1)
        + wxstc.wxSTC_FOLDLEVELHEADERFLAG)
      reseditor:EnsureVisibleEnforcePolicy(lines)

      lines = lines + 1

      -- show context lines before posline
      for line = math.max(1, posline-context), posline-1 do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
    end
    if posline and oldline then
      -- show context lines between oldposline and posline
      for line = oldline+1, math.min(posline-1, oldline+context) do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
      if posline-oldline > context * 2 + 1 then
        text = text .. ("%5s\n"):format(("."):rep(#tostring(posline)))
      end
      for line = math.max(oldline+context+1, posline-context), posline-1 do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
    end
    if posline then
      text = text .. ("%5d: %s\n"):format(posline, getRawLine(editor, posline-1))
      findReplace.lines = findReplace.lines + 1
    elseif oldline then
      -- show context lines after posline
      for line = oldline+1, math.min(editor:GetLineCount(), oldline+context) do
        text = text .. ("%5d  %s\n"):format(line, getRawLine(editor, line-1))
      end
      text = text .. "\n"
    end
    oldline = posline

    reseditor:AppendText(text)

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

local function ProcInFiles(startdir,mask,subdirs)
  local files = FileSysGetRecursive(startdir, subdirs, mask)
  local start = TimeGet()

  for _,file in ipairs(files) do
    -- skip folders as these are included in the list as well
    if not IsDirectory(file) then
      findReplace.curfilename = file

      local filetext = FileRead(file)
      if filetext and not isBinary(filetext:sub(1, 2048)) then
        findReplace.oveditor:SetText(filetext)

        if findReplace:FindStringAll(onFileRegister) then
          findReplace.files = findReplace.files + 1
        end

        -- give time to the UI to refresh
        if TimeGet() - start > 0.25 then wx.wxSafeYield() end
        -- the IDE may be quitting after Yield or the tab may be closed,
        local ok, mgr = pcall(function() return ide:GetUIManager() end)
        -- so check to make sure the manager is still active
        if not (ok and mgr:GetPane(searchpanel):IsShown())
        -- and check that the search results tab is still open
        or not pcall(function() findReplace.reseditor:GetId() end) then
          return false
        end
      end
    end
  end
  return true
end

function findReplace:RunInFiles(replace)
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
  local reseditor = findReplace.reseditor
  if not reseditor or not pcall(function() reseditor:GetId() end) then
    reseditor = NewFile("Search Results")
    reseditor:SetWrapMode(wxstc.wxSTC_WRAP_NONE)
    reseditor:SetIndentationGuides(false)
    reseditor:SetMarginWidth(0, 0) -- hide line numbers
    reseditor:MarkerDefine(ide:GetMarker("searchmatchfile"))
    reseditor:Connect(wxstc.wxEVT_STC_DOUBLECLICK, function(event)
      if event:GetModifiers() == wx.wxMOD_NONE then
        local pos = event:GetPosition()
        if pos == wxstc.wxSTC_INVALID_POSITION then return end

        local line = reseditor:LineFromPosition(pos)
        local text = reseditor:GetLine(line):gsub("[\n\r]+$","")
        -- get line with the line number
        local jumpline = text:match("^%s*(%d+)")
        local file
        if jumpline then
          -- search back to find the file name
          for curline = line-1, 0, -1 do
            local text = reseditor:GetLine(curline):gsub("[\n\r]+$","")
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

          -- doubleclick can set selection, so reset it
          reseditor:SetSelection(pos, pos)
        end
        return
      end

      event:Skip()
    end)

    findReplace.reseditor = reseditor
  else
    ide:GetDocument(reseditor):SetActive()
  end
  if ctrl then ctrl:SetFocus() end

  reseditor.replace = replace -- keep track of the current status
  reseditor:SetText('')

  self:SetStatus(("%s '%s'."):format(
    (replace and TR("Replacing") or TR("Searching for")), self.findText))

  local startdir, mask = self:GetScope()
  local completed = ProcInFiles(startdir, mask or "*.*", self.fSubDirs)

  -- reseditor may already be closed, so check if it's valid first
  if pcall(function() reseditor:GetId() end) then
    reseditor:AppendText(("Found %d instance(s) on %d line(s) in %d file(s).")
      :format(self.occurrences, self.lines, self.files))
    reseditor:EmptyUndoBuffer() -- don't undo the changes in the results
    reseditor:SetSavePoint() -- set unmodified status

    if completed and replace and self.occurrences > 0 then
      reseditor:AppendText("\n\n"
        .."Review the changes and save this preview to apply them.\n"
        .."You can also make other changes; only lines with : will be updated.\n"
        .."Context lines (if any) are used as safety checks during the update.")
      findReplace:ReplaceString(true, reseditor)
    end
    reseditor:EnsureVisibleEnforcePolicy(reseditor:GetLineCount()-1)
  end

  self:SetStatus(
    TR("Found %d instance.", self.occurrences):format(self.occurrences))
  self.oveditor = nil
  self.toolbar:UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
end

local icons = {
  find = {
    internal = {
      ID_FINDNEXT, ID_SEPARATOR,
      ID_FINDOPTDIRECTION, ID_FINDOPTWRAPWROUND,
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
    infiles = {
      ID_FINDNEXT, ID_SEPARATOR,
      ID_FINDOPTCONTEXT, ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_FINDOPTSUBDIR,
      ID_FINDOPTSCOPE, ID_FINDSETDIR,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
  },
  replace = {
    internal = {
      ID_FINDNEXT, ID_FINDREPLACENEXT, ID_FINDREPLACEALL, ID_SEPARATOR,
      ID_FINDOPTDIRECTION, ID_FINDOPTWRAPWROUND,
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_SEPARATOR, ID_FINDOPTSTATUS,
    },
    infiles = {
      ID_FINDNEXT, ID_FINDREPLACEALL, ID_SEPARATOR,
      ID_FINDOPTCONTEXT, ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_FINDOPTSUBDIR,
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
    [ID_FINDOPTDIRECTION] = 'fDown',
    [ID_FINDOPTWRAPWROUND] = 'fWrap',
    [ID_FINDOPTWORD] = 'fWholeWord',
    [ID_FINDOPTCASE] = 'fMatchCase',
    [ID_FINDOPTREGEX] = 'fRegularExpr',
    [ID_FINDOPTSUBDIR] = 'fSubDirs',
    [ID_FINDOPTCONTEXT] = 'fContext',
  }

  for id, var in pairs(options) do
    local tool = tb:FindTool(id)
    if tool then
      tool:SetSticky(self[var])
      ctrl:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
        function ()
          self[var] = not self[var]
          tb:FindTool(id):SetSticky(self[var])
          tb:Refresh()
        end)
    end
  end

  tb:SetToolDropDown(ID_FINDSETDIR, true)
  tb:Connect(ID_FINDSETDIR, wxaui.wxEVT_COMMAND_AUITOOLBAR_TOOL_DROPDOWN, function(event)
      if event:IsDropDownClicked() then
        local menu = wx.wxMenu()
        local pos = tb:GetToolRect(event:GetId()):GetBottomLeft()
        for i, text in ipairs(self.scopeTextArray) do
          local id = ID("findreplace.scope."..i)
          menu:Append(id, text)
          menu:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
            function() self:refreshToolbar(text) end)
        end
        menu:AppendSeparator()
        menu:Append(ID_FINDSETTOPROJDIR, TR("Set To Project Directory"))
        menu:Enable(ID_FINDSETTOPROJDIR, ide:GetProject() ~= nil)
        menu:Connect(ID_FINDSETTOPROJDIR, wx.wxEVT_COMMAND_MENU_SELECTED,
          function()
            local _, mask = self:GetScope()
            self:refreshToolbar(self:SetScope(ide:GetProject(), mask))
          end)
        menu:Append(ID_FINDSETDIR, TR("Choose..."))
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

  local findCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, self.findText,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)
  local replaceCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, replaceHintText,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)

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

  local function transferDataFromWindow(incremental)
    findReplace.findText = findCtrl:GetValue()
    if not incremental then PrependStringToArray(findReplace.findTextArray, findReplace.findText) end
    if findReplace.replace then
      findReplace.replaceText = replaceCtrl:GetValue()
      if findReplace.replaceText == replaceHintText then findReplace.replaceText = "" end
      if not incremental then PrependStringToArray(findReplace.replaceTextArray, findReplace.replaceText) end
    end
    if findReplace.infiles then
      findReplace.scopeText = findReplace.scope:GetValue()
      PrependStringToArray(findReplace.scopeTextArray, findReplace.scopeText)
    end
    return true
  end

  local function findNext()
    transferDataFromWindow()
    if findReplace.infiles then
      findReplace:RunInFiles(false)
    else
      findReplace:FindString()
    end
  end

  local function findIncremental()
    if self.infiles then return end
    if self.startpos then
      self:GetEditor():SetSelection(findReplace.startpos, findReplace.startpos)
    end
    transferDataFromWindow(true)
    self:FindString()
  end

  local function findReplaceNext()
    transferDataFromWindow()
    if findReplace.replace then
      if findReplace.infiles then
        findReplace:RunInFiles(true)
      else
        findReplace:ReplaceString()
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
  local function charHandle(event)
    if event:GetKeyCode() == wx.WXK_ESCAPE then
      local mgr = ide:GetUIManager()
      mgr:GetPane(searchpanel):Hide()
      mgr:Update()
      local editor = self:GetEditor()
      if editor then
        -- restore original position for Shift-Esc
        if event:ShiftDown() and findReplace.startpos then
          editor:GotoPos(findReplace.startpos)
        end
        editor:SetFocus()
      end
    elseif event:GetKeyCode() == 9 then
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
  findCtrl:Connect(wx.wxEVT_SET_FOCUS,
    function(event)
      event:Skip()
      local ed = findReplace:GetEditor()
      self.startpos = ed and (ed:GetSelectionStart() == ed:GetSelectionEnd()
        and ed:GetCurrentPos() or ed:GetSelectionStart()) or nil
    end)
  findCtrl:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findNext)
  findCtrl:Connect(wx.wxEVT_COMMAND_TEXT_UPDATED, findIncremental)
  findCtrl:Connect(wx.wxEVT_CHAR, charHandle)
  replaceCtrl:Connect(wx.wxEVT_SET_FOCUS, function(event)
      event:Skip()
      -- hide the replace hint; should be done with SetHint method,
      -- but it's not yet available in wxlua 2.8.12
      if replaceCtrl:GetValue() == replaceHintText then replaceCtrl:ChangeValue('') end
    end)
  replaceCtrl:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findReplaceNext)
  replaceCtrl:Connect(wx.wxEVT_CHAR, charHandle)
  scope:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, findNext)
  scope:Connect(wx.wxEVT_CHAR, charHandle)

  local function notSearching(event) event:Enable(not self.oveditor) end
  ctrl:Connect(ID_FINDNEXT, wx.wxEVT_UPDATE_UI, notSearching)
  ctrl:Connect(ID_FINDREPLACENEXT, wx.wxEVT_UPDATE_UI, notSearching)
  ctrl:Connect(ID_FINDREPLACEALL, wx.wxEVT_UPDATE_UI, notSearching)

  ctrl:Connect(ID_FINDNEXT, wx.wxEVT_COMMAND_MENU_SELECTED, findNext)
  ctrl:Connect(ID_FINDREPLACENEXT, wx.wxEVT_COMMAND_MENU_SELECTED, findReplaceNext)
  ctrl:Connect(ID_FINDREPLACEALL, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      transferDataFromWindow()
      findReplace:ReplaceString(true)
    end)

  ctrl:Connect(ID_FINDSETDIR, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      local dir, mask = self:GetScope()
      local filePicker = wx.wxDirDialog(ctrl, TR("Choose a search directory"),
        dir or wx.wxGetCwd(), wx.wxFLP_USE_TEXTCTRL)
      if filePicker:ShowModal(true) == wx.wxID_OK then
        self:refreshToolbar(self:SetScope(FixDir(filePicker:GetPath()), mask))
      end
    end)

  self.searchCtrl = findCtrl
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
    value = self:SetScope(ide:GetProject() or wx.wxGetCwd(),
      '*.'..(#ext > 0 and ext or '*'))
  end
  self:refreshToolbar(value)

  local mgr = ide:GetUIManager()
  local pane = mgr:GetPane(searchpanel)
  if not pane:IsShown() then
    -- if not shown, set value from the current selection
    self.searchCtrl:ChangeValue(self:GetSelectedString() or self.searchCtrl:GetValue())
    local size = ctrl:GetSize()
    pane:Dock():Bottom():BestSize(size):MinSize(size):Layer(0):Row(1):Show()
    mgr:Update()
  end

  -- reset search when re-creating dialog to avoid modifying selected
  -- fragment after successful search and updated replacement
  self.foundString = false
  self.searchCtrl:SetFocus()
  self.searchCtrl:SetSelection(-1, -1) -- select the content
end

function findReplace:Show(replace,infiles)
  self:refreshPanel(replace,infiles)
end

ide:AddPackage('core.findreplace', {
    -- reset ngram cache when switching projects to conserve memory
    onEditorPreSave = function(self, editor)
      if editor == findReplace.reseditor and findReplace.reseditor.replace then
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

          editor:EnsureVisibleEnforcePolicy(line) -- scroll to the line
          wx.wxSafeYield()

          local fname = getRawLine(editor, line) -- get the file name
          local filetext, err = FileRead(fname)
          local mismatch = false
          if filetext then
            oveditor:SetText(filetext)
            while true do -- for each line following the file name
              line = line + 1
              local text = getRawLine(editor, line)
              local lnum, lmark, ltext = text:match("^%s*(%d+)([ :]) (.*)")
              if lnum then
                lnum = tonumber(lnum)
                if lmark == ':' then -- if the change line, then apply the change
                  local pos = oveditor:PositionFromLine(lnum-1)
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
              ok, err = FileWrite(fname, oveditor:GetText())
              if ok then files = files + 1 end
            end
          end
          if err or mismatch then
            report = (report or "") .. (("\n%s: %s")
              :format(fname, mismatch and "mismatch on line "..mismatch or err))
          end
        end
        if report then editor:AppendText("\n"..report) end
        editor:AppendText(("\n\nUpdated %d line(s) in %d file(s)."):format(lines, files))
        editor:EnsureVisibleEnforcePolicy(editor:GetLineCount()-1)
        editor:SetSavePoint() -- set unmodified status when done
        findReplace:SetStatus(TR("Updated %d file.", files):format(files))
        return false
      end
    end
  })
