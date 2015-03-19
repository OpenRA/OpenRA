-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local searchpanel = 'searchpanel'
ide.findReplace = {
  panel = nil, -- the control for find/replace
  replace = false, -- is it a find or replace
  infiles = false,
  startpos = nil,
  cureditor = nil,
  tofocus = nil, -- the control to set focus on when search is requested

  fWholeWord = false, -- match whole words
  fMatchCase = false, -- case sensitive
  fRegularExpr = false, -- use regex
  fWrap = true, -- search wraps around

  fDown = true, -- search downwards in doc
  fSubDirs = true, -- search in subdirectories
  fMakeBak = true, -- make bak files for replace in files

  findTextArray = {}, -- array of last entered find text
  findText = "", -- string to find
  replaceTextArray = {}, -- array of last entered replace text
  replaceText = "", -- string to replace find string with
  scopeText = nil,
  scopeTextArray = {},

  foundString = false, -- was the string found for the last search

  oveditor = nil,
  curfilename = "", -- for search in files
  occurrences = 0,

  -- HasText() is there a string to search for
  -- GetSelectedString() get currently selected string if it's on one line
  -- FindString(reverse) find the findText string
  -- Show(replace) create the dialog
  -- GetEditor() which editor to use
}
local findReplace = ide.findReplace
local NOTFOUND = -1
local sep = ';'

function findReplace:GetEditor(reset)
  if reset then findReplace.cureditor = nil end
  findReplace.cureditor = ide:GetEditorWithLastFocus() or findReplace.cureditor
  return findReplace.oveditor or findReplace.cureditor or GetEditor()
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
  return (findReplace.findText ~= nil) and (string.len(findReplace.findText) > 0)
end

function findReplace:GetScope()
  if not self.scopeText then return end
  local dir, mask = self.scopeText:match(('([^%s]*)%s%%s*(.+)'):format(sep,sep))
  if not dir then dir = self.scopeText end
  -- trip leading/trailing spaces from the directory
  dir = dir:gsub("^%s+",""):gsub("%s+$","")
  -- if the directory doesn't exist, treat it as the extension(s)
  if not wx.wxDirExists(dir) then
    dir, mask = ide:GetProject() or wx.wxGetCwd(), dir
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
  local editor = findReplace:GetEditor()
  if editor and findReplace:HasText() then
    local fDown = iff(reverse, not findReplace.fDown, findReplace.fDown)
    setSearchFlags(editor)
    setTarget(editor, fDown)
    local posFind = editor:SearchInTarget(findReplace.findText)
    if (posFind == NOTFOUND) and findReplace.fWrap then
      editor:SetTargetStart(iff(fDown, 0, editor:GetLength()))
      editor:SetTargetEnd(iff(fDown, editor:GetLength(), 0))
      posFind = editor:SearchInTarget(findReplace.findText)
    end
    if posFind == NOTFOUND then
      findReplace.foundString = false
      findReplace.status:SetLabel(TR("Text not found."))
    else
      findReplace.foundString = true
      local start = editor:GetTargetStart()
      local finish = editor:GetTargetEnd()
      editor:ShowPosEnforcePolicy(finish)
      editor:SetSelection(start, finish)
      findReplace.status:SetLabel("")
    end
  end
end

-- returns if something was found
-- [inFileRegister(pos)] passing function will
-- register every position item was found
-- supposed for "Search/Replace in Files"

function findReplace:FindStringAll(inFileRegister)
  local found = false
  local editor = findReplace:GetEditor()
  if editor and findReplace:HasText() then
    local findLen = string.len(findReplace.findText)
    local e = setTargetAll(editor)

    setSearchFlags(editor)
    local posFind = editor:SearchInTarget(findReplace.findText)
    if (posFind ~= NOTFOUND) then
      while posFind ~= NOTFOUND do
        inFileRegister(posFind)
        editor:SetTargetStart(posFind + findLen)
        editor:SetTargetEnd(e)
        posFind = editor:SearchInTarget(findReplace.findText)
      end

      found = true
    end
  end
  return found
end

-- returns if replacements were done
-- [inFileRegister(pos)] passing function will disable "undo"
-- registers every position item was found
-- supposed for "Search/Replace in Files"

function findReplace:ReplaceString(fReplaceAll, inFileRegister)
  local replaced = false
  local editor = findReplace:GetEditor()
  if editor and findReplace:HasText() then
    -- don't replace in read-only editors
    if editor:GetReadOnly() then return false end

    local endTarget = inFileRegister and setTargetAll(editor) or
      setTarget(editor, findReplace.fDown, fReplaceAll, findReplace.fWrap)

    if fReplaceAll then
      setSearchFlags(editor)
      local occurrences = 0
      local posFind = editor:SearchInTarget(findReplace.findText)
      if (posFind ~= NOTFOUND) then
        if (not inFileRegister) then editor:BeginUndoAction() end
        while posFind ~= NOTFOUND do
          if (inFileRegister) then inFileRegister(posFind) end

          local length = editor:GetLength()
          local replaced = findReplace.fRegularExpr
            and editor:ReplaceTargetRE(findReplace.replaceText)
            or editor:ReplaceTarget(findReplace.replaceText)

          editor:SetTargetStart(posFind + replaced)
          -- adjust the endTarget as the position could have changed;
          -- can't simply subtract findText length as it could be a regexp
          endTarget = endTarget + (editor:GetLength() - length)
          editor:SetTargetEnd(endTarget)
          posFind = editor:SearchInTarget(findReplace.findText)
          occurrences = occurrences + 1
        end
        if (not inFileRegister) then editor:EndUndoAction() end

        replaced = true
      end
      findReplace.status:SetLabel(("%s %s."):format(
        TR("Replaced"), TR("%d instance", occurrences):format(occurrences)))
    else
      editor:TargetFromSelection()
      -- check if there is anything selected as well as the user can
      -- move the cursor after successful search
      if editor:GetSelectionStart() ~= editor:GetSelectionEnd()
      -- check that the current selection matches what's being searched for
      and editor:SearchInTarget(findReplace.findText) ~= NOTFOUND then
        local start = editor:GetSelectionStart()
        local replaced = findReplace.fRegularExpr
          and editor:ReplaceTargetRE(findReplace.replaceText)
          or editor:ReplaceTarget(findReplace.replaceText)

        editor:SetSelection(start, start + replaced)
        findReplace.foundString = false

        replaced = true
      end
      findReplace:FindString()
    end
  end

  return replaced
end

local function onFileRegister(pos)
  local editor = findReplace.oveditor
  local line = editor:LineFromPosition(pos)
  local linepos = pos - editor:PositionFromLine(line)
  local result = "("..(line+1)..","..(linepos+1).."): "..editor:GetLine(line)
  DisplayOutputLn(findReplace.curfilename..result:gsub("\r?\n$",""))
  findReplace.occurrences = findReplace.occurrences + 1
end

local function ProcInFiles(startdir,mask,subdirs,replace)
  local files = FileSysGetRecursive(startdir,subdirs,"*")
  local start = TimeGet()

  -- mask could be a list, so generate a table with matching patterns
  -- accept "*.lua; .txt,.wlua" combinations
  local masks = {}
  for m in mask:gmatch("[^%s;,]+") do
    table.insert(masks, m:gsub("%.", "%%."):gsub("%*", ".*").."$")
  end
  for _,file in ipairs(files) do
    -- ignore .bak files when replacing and asked to store .bak files
    -- and skip folders as these are included in the list as well
    if not (replace and findReplace.fMakeBak and file:find('.bak$'))
    and not IsDirectory(file) then
      local match = false
      for _, mask in ipairs(masks) do match = match or file:find(mask) end
      if match then
        findReplace.curfilename = file

        local filetext = FileRead(file)
        if filetext and not isBinary(filetext:sub(1, 2048)) then
          findReplace.oveditor:SetText(filetext)

          if replace then
            -- check if anything replaced, store changed content, make .bak
            if findReplace:ReplaceString(true,onFileRegister)
            and (not findReplace.fMakeBak or FileWrite(file..".bak",filetext)) then
              FileWrite(file,findReplace.oveditor:GetText())
            end
          else
            findReplace:FindStringAll(onFileRegister)
          end

          -- give time to the UI to refresh
          if TimeGet() - start > 0.25 then ide:Yield() end
          if not ide:GetUIManager():GetPane(searchpanel):IsShown() then
            DisplayOutputLn(TR("Cancelled by the user."))
            break
          end
        end
      end
    end
  end
end

function findReplace:RunInFiles(replace)
  if not findReplace:HasText() or findReplace.oveditor then return end

  findReplace.oveditor = ide:CreateStyledTextCtrl(findReplace.panel, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(1,1), wx.wxBORDER_NONE)
  findReplace.occurrences = 0
  findReplace.toolbar:UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
  ide:Yield() -- let the update of the UI happen

  ClearOutput()
  ActivateOutput()

  local startdir, mask = findReplace:GetScope()
  DisplayOutputLn(("%s '%s' (%s)."):format(
    (replace and TR("Replacing") or TR("Searching for")),
    findReplace.findText, startdir))

  ProcInFiles(startdir, mask or "*.*", findReplace.fSubDirs, replace)

  DisplayOutputLn(("%s %s."):format(
    (replace and TR("Replaced") or TR("Found")),
    TR("%d instance", findReplace.occurrences):format(findReplace.occurrences)))

  findReplace.oveditor = nil
  findReplace.toolbar:UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
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
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_FINDOPTSUBDIR, ID_FINDOPTBACKUP,
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
      ID_FINDOPTWORD, ID_FINDOPTCASE, ID_FINDOPTREGEX,
      ID_FINDOPTSUBDIR, ID_FINDOPTBACKUP,
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
    [ID_FINDOPTBACKUP] = 'fMakeBak',
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
        menu:Append(ID_FINDSCOPECLEAR, TR("Clear Items"))
        menu:Connect(ID_FINDSCOPECLEAR, wx.wxEVT_COMMAND_MENU_SELECTED,
          function() self.scopeTextArray = {} end)
        menu:Enable(ID_FINDSCOPECLEAR, #self.scopeTextArray > 0)
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

  local findCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, findReplace.findText,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxTE_PROCESS_ENTER + wx.wxTE_PROCESS_TAB + wx.wxBORDER_STATIC)
  local replaceCtrl = wx.wxTextCtrl(ctrl, wx.wxID_ANY, findReplace.replaceText,
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

  for _, control in ipairs({findCtrl, replaceCtrl, scope}) do
    control:SetBackgroundColour(backcolor)
    control:SetForegroundColour(textcolor)
    control:SetFont(tfont)
  end
  status:SetFont(tfont)

  local mgr = ide:GetUIManager()
  mgr:AddPane(ctrl, wxaui.wxAuiPaneInfo()
    :Name(searchpanel):CaptionVisible(false):PaneBorder(false):Hide())

  local function transferDataFromWindow(incremental)
    findReplace.findText = findCtrl:GetValue()
    if not incremental then PrependStringToArray(findReplace.findTextArray, findReplace.findText) end
    if findReplace.replace then
      findReplace.replaceText = replaceCtrl:GetValue()
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
      findReplace:RunInFiles()
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

  local function findReplaceNext(event)
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
      local filePicker = wx.wxDirDialog(ctrl, TR("Choose a project directory"),
        dir or wx.wxGetCwd(), wx.wxFLP_USE_TEXTCTRL)
      if filePicker:ShowModal(true) == wx.wxID_OK then
        self:refreshToolbar(self:SetScope(FixDir(filePicker:GetPath()), mask))
      end
    end)

  self.tofocus = findCtrl
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
    else
      self.findSizer:Hide(1)
    end
    self.findSizer:Layout()

    self.scope:Show(infiles)
  end

  local value = self.scope:GetValue()
  local ed = ide:GetEditor()
  if ed then
    local doc = ide:GetDocument(ed)
    local ext = doc:GetFileExt()
    value = self:SetScope(
      ide:GetProject() or wx.wxGetCwd(),
      '*.'..(#ext > 0 and ext or '*'))
  end
  self:refreshToolbar(value)

  local mgr = ide:GetUIManager()
  local pane = mgr:GetPane(searchpanel)
  if not pane:IsShown() then
    -- if not shown, set value from the current selection
    self.tofocus:ChangeValue(self:GetSelectedString() or self.tofocus:GetValue())
    local size = ctrl:GetSize()
    pane:Dock():Bottom():BestSize(size):MinSize(size):Layer(0):Row(1):Show()
    mgr:Update()
  end

  -- reset search when re-creating dialog to avoid modifying selected
  -- fragment after successful search and updated replacement
  self.foundString = false
  self.tofocus:SetFocus()
  self.tofocus:SetSelection(-1, -1) -- select the content
end

function findReplace:Show(replace,infiles)
  self:refreshPanel(replace,infiles)
end
