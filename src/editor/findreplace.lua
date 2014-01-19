-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
ide.findReplace = {
  dialog = nil, -- the wxDialog for find/replace
  replace = false, -- is it a find or replace dialog
  infiles = false,

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
  filemaskText = nil,
  filemaskTextArray= {},
  filedirText = "",
  filedirTextArray = {},

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

function findReplace:GetEditor()
  return findReplace.oveditor or GetEditor()
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

function findReplace:GetSelectedString()
  local editor = findReplace:GetEditor()
  if editor then
    local startSel = editor:GetSelectionStart()
    local endSel = editor:GetSelectionEnd()
    if (startSel ~= endSel) and (editor:LineFromPosition(startSel) == editor:LineFromPosition(endSel)) then
      findReplace.findText = editor:GetTextRange(startSel, endSel)
      return true
    end
  end
  return false
end

local function shake(window, shakes, duration, vigour)
  shakes = shakes or 4
  duration = duration or 0.5
  vigour = vigour or 0.05

  if not window then return end

  local delay = math.floor(duration/shakes/2)
  local position = window:GetPosition() -- get current position
  local deltax = window:GetSize():GetWidth()*vigour
  for s = 1, shakes do
    window:Move(position:GetX()-deltax, position:GetY())
    wx.wxMilliSleep(delay)
    window:Move(position:GetX()+deltax, position:GetY())
    wx.wxMilliSleep(delay)
  end
  window:Move(position) -- restore position
end

function findReplace:FindString(reverse)
  if findReplace:HasText() then
    local editor = findReplace:GetEditor()
    local fDown = iff(reverse, not findReplace.fDown, findReplace.fDown)
    setSearchFlags(editor)
    setTarget(editor, fDown)
    local posFind = editor:SearchInTarget(findReplace.findText)
    if (posFind == -1) and findReplace.fWrap then
      editor:SetTargetStart(iff(fDown, 0, editor:GetLength()))
      editor:SetTargetEnd(iff(fDown, editor:GetLength(), 0))
      posFind = editor:SearchInTarget(findReplace.findText)
    end
    if posFind == -1 then
      findReplace.foundString = false
      ide.frame:SetStatusText(TR("Text not found."))
      shake(findReplace.dialog)
    else
      findReplace.foundString = true
      local start = editor:GetTargetStart()
      local finish = editor:GetTargetEnd()
      EnsureRangeVisible(start, finish)
      editor:SetSelection(start, finish)
      ide.frame:SetStatusText("")
    end
  end
end

-- returns if something was found
-- [inFileRegister(pos)] passing function will
-- register every position item was found
-- supposed for "Search/Replace in Files"

function findReplace:FindStringAll(inFileRegister)
  local found = false
  if findReplace:HasText() then
    local findLen = string.len(findReplace.findText)
    local editor = findReplace:GetEditor()
    local e = setTargetAll(editor)

    setSearchFlags(editor)
    local posFind = editor:SearchInTarget(findReplace.findText)
    if (posFind ~= -1) then
      while posFind ~= -1 do
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

  if findReplace:HasText() then
    local editor = findReplace:GetEditor()
    local endTarget = inFileRegister and setTargetAll(editor) or
      setTarget(editor, findReplace.fDown, fReplaceAll, findReplace.fWrap)

    if fReplaceAll then
      setSearchFlags(editor)
      local occurrences = 0
      local posFind = editor:SearchInTarget(findReplace.findText)
      if (posFind ~= -1) then
        if (not inFileRegister) then editor:BeginUndoAction() end
        while posFind ~= -1 do
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
      ide.frame:SetStatusText(("%s %s."):format(
        TR("Replaced"), TR("%d instance", occurrences):format(occurrences)))
    else
      -- check if there is anything selected as well as the user can
      -- move the cursor after successful search
      if findReplace.foundString
      and editor:GetSelectionStart() ~= editor:GetSelectionEnd() then
        local start = editor:GetSelectionStart()

        -- convert selection to target as we need TargetRE support
        editor:TargetFromSelection()

        local length = editor:GetLength()
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
  -- accept "*.lua; .txt;.wlua" combinations
  local masks = {}
  for m in mask:gmatch("[^%s;]+") do
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
            and findReplace.fMakeBak and FileWrite(file..".bak",filetext) then
              FileWrite(file,findReplace.oveditor:GetText())
            end
          else
            findReplace:FindStringAll(onFileRegister)
          end

          -- give time to the UI to refresh
          if TimeGet() - start > 0.25 then wx.wxYield() end
          if not findReplace.dialog:IsShown() then
            DisplayOutputLn(TR("Cancelled by the user."))
            break
          end
        end
      end
    end
  end
end

function findReplace:RunInFiles(replace)
  if not findReplace:HasText() then return end

  findReplace.oveditor = wxstc.wxStyledTextCtrl(findReplace.dialog, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(1,1), wx.wxBORDER_STATIC)
  findReplace.occurrences = 0

  ActivateOutput()

  local startdir = findReplace.filedirText
  DisplayOutputLn(("%s '%s'."):format(
    (replace and TR("Replacing") or TR("Searching for")),
    findReplace.findText))

  ProcInFiles(startdir, findReplace.filemaskText, findReplace.fSubDirs, replace)

  DisplayOutputLn(("%s %s."):format(
    (replace and TR("Replaced") or TR("Found")),
    TR("%d instance", findReplace.occurrences):format(findReplace.occurrences)))

  findReplace.oveditor = nil
end

local function getExts()
  local knownexts = {}
  for i,spec in pairs(ide.specs) do
    if (spec.exts) then
      for n,ext in ipairs(spec.exts) do
        table.insert(knownexts, "*."..ext)
      end
    end
  end
  return #knownexts > 0 and table.concat(knownexts, "; ") or nil
end

function findReplace:createDialog(replace,infiles)
  local ID_FIND_NEXT = 1
  local ID_REPLACE = 2
  local ID_REPLACE_ALL = 3
  local ID_SETDIR = 4

  local mac = ide.osname == 'Macintosh'

  local findReplace = self

  local position = wx.wxDefaultPosition
  if findReplace.dialog then
    -- grab current position before destroying the dialog
    position = findReplace.dialog:GetPosition()
    findReplace.dialog:Destroy()
  end

  local findDialog = wx.wxDialog(ide.frame, wx.wxID_ANY, infiles and TR("Find In Files") or TR("Find"),
    position, wx.wxDefaultSize, wx.wxDEFAULT_DIALOG_STYLE)

  findReplace.replace = replace
  findReplace.infiles = infiles

  -- Create right hand buttons and sizer
  local findButton = wx.wxButton(findDialog, ID_FIND_NEXT, infiles and TR("&Find All") or TR("&Find Next"))
  local replaceButton = wx.wxButton(findDialog, ID_REPLACE, infiles and replace and TR("&Replace All") or TR("&Replace"))
  local replaceAllButton = nil
  if (replace and not infiles) then
    replaceAllButton = wx.wxButton(findDialog, ID_REPLACE_ALL, TR("Replace A&ll"))
  end
  local cancelButton = wx.wxButton(findDialog, wx.wxID_CANCEL, TR("Cancel"))

  local buttonsSizer = wx.wxBoxSizer(wx.wxVERTICAL)
  buttonsSizer:Add(findButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  buttonsSizer:Add(replaceButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  if replaceAllButton then
    buttonsSizer:Add(replaceAllButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  end
  buttonsSizer:Add(cancelButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)

  -- Create find/replace text entry sizer
  local findStatText = wx.wxStaticText(findDialog, wx.wxID_ANY, TR("Find")..": ")
  local findTextCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.findText,
    wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.findTextArray,
    wx.wxCB_DROPDOWN + (mac and wx.wxTE_PROCESS_ENTER or 0))
  findTextCombo:SetFocus()

  local infilesMaskStat,infilesMaskCombo
  local infilesDirStat,infilesDirCombo,infilesDirButton
  if (infiles) then
    infilesMaskStat = wx.wxStaticText(findDialog, wx.wxID_ANY, TR("File Type")..": ")
    infilesMaskCombo = wx.wxComboBox(findDialog, wx.wxID_ANY,
      findReplace.filemaskText or getExts() or "*.*",
      wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.filemaskTextArray)

    local fname = GetEditorFileAndCurInfo(true)
    if #(findReplace.filedirText) == 0 then
      findReplace.filedirText = ide.config.path.projectdir
        or fname and fname:GetPath(wx.wxPATH_GET_VOLUME)
        or ""
    end

    infilesDirStat = wx.wxStaticText(findDialog, wx.wxID_ANY, TR("Directory")..": ")
    infilesDirCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.filedirText,
      wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.filedirTextArray)
    infilesDirButton = wx.wxButton(findDialog, ID_SETDIR, "...", wx.wxDefaultPosition, wx.wxSize(26,20))
  end

  local replaceStatText, replaceTextCombo
  if (replace) then
    replaceStatText = wx.wxStaticText(findDialog, wx.wxID_ANY, TR("Replace")..": ")
    replaceTextCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.replaceText,
      wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.replaceTextArray,
      wx.wxCB_DROPDOWN + (mac and wx.wxTE_PROCESS_ENTER or 0))
  end

  local findReplaceSizer = wx.wxFlexGridSizer(2, 3, 0, 0)
  findReplaceSizer:AddGrowableCol(1)
  findReplaceSizer:Add(findStatText, 0, wx.wxALL + wx.wxALIGN_RIGHT + wx.wxALIGN_CENTER_VERTICAL, 0)
  findReplaceSizer:Add(findTextCombo, 1, wx.wxALL + wx.wxGROW + wx.wxCENTER+ wx.wxALIGN_CENTER_VERTICAL, 0)
  findReplaceSizer:Add(16, 8, 0, wx.wxALL + wx.wxALIGN_RIGHT + wx.wxADJUST_MINSIZE,0)

  if (infiles) then
    findReplaceSizer:Add(infilesMaskStat, 0, wx.wxTOP + wx.wxALIGN_RIGHT + wx.wxALIGN_CENTER_VERTICAL, 5)
    findReplaceSizer:Add(infilesMaskCombo, 1, wx.wxTOP + wx.wxGROW + wx.wxCENTER+ wx.wxALIGN_CENTER_VERTICAL, 5)
    findReplaceSizer:Add(16, 8, 0, wx.wxTOP, 5)

    findReplaceSizer:Add(infilesDirStat, 0, wx.wxTOP + wx.wxALIGN_RIGHT + wx.wxALIGN_CENTER_VERTICAL, 5)
    findReplaceSizer:Add(infilesDirCombo, 1, wx.wxTOP + wx.wxGROW + wx.wxCENTER+ wx.wxALIGN_CENTER_VERTICAL, 5)
    findReplaceSizer:Add(infilesDirButton, 0, wx.wxTOP + wx.wxALIGN_RIGHT + wx.wxADJUST_MINSIZE+ wx.wxALIGN_CENTER_VERTICAL, 5)
  end

  if (replace) then
    findReplaceSizer:Add(replaceStatText, 0, wx.wxTOP + wx.wxALIGN_RIGHT + wx.wxALIGN_CENTER_VERTICAL, 5)
    findReplaceSizer:Add(replaceTextCombo, 1, wx.wxTOP + wx.wxGROW + wx.wxCENTER+ wx.wxALIGN_CENTER_VERTICAL, 5)
  end

  -- the StaticBox(Sizer) needs to be created before checkboxes, otherwise
  -- checkboxes don't get any clicks on OSX (ide.osname == 'Macintosh')
  -- as the z-order for event traversal appears to be incorrect.
  local optionsSizer = wx.wxStaticBoxSizer(wx.wxVERTICAL, findDialog, TR("Options"))

  -- Create find/replace option checkboxes
  local wholeWordCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR("Match &whole word"))
  local matchCaseCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR("Match &case"))
  local wrapAroundCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR("Wrap ar&ound"))
  local regexCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR("Regular &expression"))
  wholeWordCheckBox:SetValue(findReplace.fWholeWord)
  matchCaseCheckBox:SetValue(findReplace.fMatchCase)
  wrapAroundCheckBox:SetValue(findReplace.fWrap)
  regexCheckBox:SetValue(findReplace.fRegularExpr)

  local optionSizer = wx.wxBoxSizer(wx.wxVERTICAL, findDialog)
  optionSizer:Add(wholeWordCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  optionSizer:Add(matchCaseCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  optionSizer:Add(wrapAroundCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  optionSizer:Add(regexCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)

  optionsSizer:Add(optionSizer, 0, 0, 5)

  -- Create scope radiobox
  local scopeRadioBox
  local subDirCheckBox
  local makeBakCheckBox

  local scopeSizer
  if (infiles) then
    -- the StaticBox(Sizer) needs to be created before checkboxes
    scopeSizer = wx.wxStaticBoxSizer(wx.wxVERTICAL, findDialog, TR("In Files"))

    subDirCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR("&Subdirectories"))
    makeBakCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, TR(".&bak on Replace"))
    subDirCheckBox:SetValue(findReplace.fSubDirs)
    makeBakCheckBox:SetValue(findReplace.fMakeBak)

    local optionSizer = wx.wxBoxSizer(wx.wxVERTICAL, findDialog)
    optionSizer:Add(subDirCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
    optionSizer:Add(makeBakCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)

    scopeSizer:Add(optionSizer, 0, 0, 5)
  else
    scopeRadioBox = wx.wxRadioBox(findDialog, wx.wxID_ANY, TR("Scope"), wx.wxDefaultPosition,
      wx.wxDefaultSize, {TR("&Up"), TR("&Down")}, 1, wx.wxRA_SPECIFY_COLS)
    scopeRadioBox:SetSelection(iff(findReplace.fDown, 1, 0))

    scopeSizer = wx.wxBoxSizer(wx.wxVERTICAL, findDialog)
    scopeSizer:Add(scopeRadioBox, 0, 0, 0)
  end

  -- Add all the sizers to the dialog
  local optionScopeSizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
  optionScopeSizer:Add(optionsSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 5)
  optionScopeSizer:Add(scopeSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 5)

  local leftSizer = wx.wxBoxSizer(wx.wxVERTICAL)
  leftSizer:Add(findReplaceSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 0)
  leftSizer:Add(optionScopeSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 0)

  local mainSizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
  mainSizer:Add(leftSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 10)
  mainSizer:Add(buttonsSizer, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 10)

  mainSizer:SetSizeHints(findDialog)
  findDialog:SetSizer(mainSizer)

  local function TransferDataFromWindow()
    findReplace.fWholeWord = wholeWordCheckBox:GetValue()
    findReplace.fMatchCase = matchCaseCheckBox:GetValue()
    findReplace.fWrap = wrapAroundCheckBox:GetValue()
    if (findReplace.infiles) then
      findReplace.fSubDirs = subDirCheckBox:GetValue()
      findReplace.fMakeBak = makeBakCheckBox:GetValue()
    else
      findReplace.fDown = scopeRadioBox:GetSelection() == 1
    end

    findReplace.fRegularExpr = regexCheckBox:GetValue()
    findReplace.findText = findTextCombo:GetValue()
    PrependStringToArray(findReplace.findTextArray, findReplace.findText)
    if findReplace.replace then
      findReplace.replaceText = replaceTextCombo:GetValue()
      PrependStringToArray(findReplace.replaceTextArray, findReplace.replaceText)
    end
    if findReplace.infiles then
      findReplace.filemaskText = infilesMaskCombo:GetValue()
      PrependStringToArray(findReplace.filemaskTextArray, findReplace.filemaskText)

      findReplace.filedirText = infilesDirCombo:GetValue()
      PrependStringToArray(findReplace.filedirTextArray, findReplace.filedirText)
    end
    return true
  end

  -- this is a workaround for Enter issue in wxComboBox on OSX:
  -- https://groups.google.com/d/msg/wx-users/EVJr8GqyNUA/CUALp585E78J
  if (mac and ide.wxver >= "2.9.5") then
    local function simulateEnter()
      findDialog:AddPendingEvent(wx.wxCommandEvent(
        wx.wxEVT_COMMAND_BUTTON_CLICKED, ID_FIND_NEXT))
    end
    findTextCombo:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, simulateEnter)
    if replace then
      replaceTextCombo:Connect(wx.wxEVT_COMMAND_TEXT_ENTER, simulateEnter)
    end
  end

  findDialog:Connect(ID_FIND_NEXT, wx.wxEVT_COMMAND_BUTTON_CLICKED,
    function()
      TransferDataFromWindow()
      if (findReplace.infiles) then
        findReplace:RunInFiles()
        findReplace.dialog:Destroy()
        findReplace.dialog = nil
      else
        findReplace:FindString()
      end
    end)

  findDialog:Connect(ID_REPLACE, wx.wxEVT_COMMAND_BUTTON_CLICKED,
    function(event)
      TransferDataFromWindow()
      event:Skip()
      if findReplace.replace then
        if (findReplace.infiles) then
          findReplace:RunInFiles(true)
          findReplace.dialog:Destroy()
          findReplace.dialog = nil
        else
          findReplace:ReplaceString()
        end
      else
        findReplace:createDialog(true,infiles)
      end
    end)

  if replaceAllButton then
    findDialog:Connect(ID_REPLACE_ALL, wx.wxEVT_COMMAND_BUTTON_CLICKED,
      function(event)
        TransferDataFromWindow()
        event:Skip()
        findReplace:ReplaceString(true)
      end)
  end

  if infilesDirButton then
    findDialog:Connect(ID_SETDIR, wx.wxEVT_COMMAND_BUTTON_CLICKED,
      function()
        local filePicker = wx.wxDirDialog(findDialog, TR("Choose a project directory"),
          findReplace.filedirText~="" and findReplace.filedirText or wx.wxGetCwd(),wx.wxFLP_USE_TEXTCTRL)

        local res = filePicker:ShowModal(true)
        if res == wx.wxID_OK then
          infilesDirCombo:SetValue(FixDir(filePicker:GetPath()))
        end
      end)
  end

  -- if on OSX then select the current value of the default dropdown
  -- and don't set the default as it doesn't make Enter to work, but
  -- prevents associated hotkey (Cmd-F) from working (wx2.9.5).
  if ide.osname == 'Macintosh' then
    findTextCombo:SetSelection(0, #findTextCombo:GetValue())
  else
    findButton:SetDefault()
  end

  -- reset search when re-creating dialog to avoid modifying selected
  -- fragment after successful search and updated replacement
  findReplace.foundString = false
  findReplace.dialog = findDialog
  findDialog:Show(true)
  return findDialog
end

function findReplace:Show(replace,infiles)
  self:GetSelectedString()
  self:createDialog(replace,infiles)
end
