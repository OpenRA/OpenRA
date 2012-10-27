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
  fSubDirs = false, -- search in subdirectories
  fMakeBak = true, -- make bak files for replace in files

  findTextArray = {}, -- array of last entered find text
  findText = "", -- string to find
  replaceTextArray = {}, -- array of last entered replace text
  replaceText = "", -- string to replace find string with
  filemaskText = "*.*",
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
  local flags = 0
  if findReplace.fWholeWord then flags = wxstc.wxSTC_FIND_WHOLEWORD end
  if findReplace.fMatchCase then flags = flags + wxstc.wxSTC_FIND_MATCHCASE end
  if findReplace.fRegularExpr then flags = flags + wxstc.wxSTC_FIND_REGEXP end
  editor:SetSearchFlags(flags)
end

local function setTarget(editor, fDown, fInclude)
  local selStart = editor:GetSelectionStart()
  local selEnd = editor:GetSelectionEnd()
  local len = editor:GetLength()
  local s, e
  if fDown then
    e= len
    s = iff(fInclude, selStart, selEnd +1)
  else
    s = 0
    e = iff(fInclude, selEnd, selStart-1)
  end
  if not fDown and not fInclude then s, e = e, s end
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
      findReplace.findText = editor:GetSelectedText()
      findReplace.foundString = true
    end
  end
  return editor and findReplace.foundString
end

function findReplace:FindString(reverse)
  if findReplace:HasText() then
    local editor = findReplace:GetEditor()
    local fDown = iff(reverse, not findReplace.fDown, findReplace.fDown)
    local lenFind = string.len(findReplace.findText)
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
      ide.frame:SetStatusText("Text not found.")
    else
      findReplace.foundString = true
      local start = editor:GetTargetStart()
      local finish = editor:GetTargetEnd()
      EnsureRangeVisible(start, finish)
      editor:SetSelection(start, finish)
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

function findReplace:ReplaceString(fReplaceAll,inFileRegister)
  local replaced = false

  if findReplace:HasText() then
    local replaceLen = string.len(findReplace.replaceText)
    local findLen = string.len(findReplace.findText)
    local editor = findReplace:GetEditor()
    local endTarget = inFileRegister and setTargetAll(editor) or
    setTarget(editor, findReplace.fDown, fReplaceAll)

    if fReplaceAll then
      setSearchFlags(editor)
      local posFind = editor:SearchInTarget(findReplace.findText)
      if (posFind ~= -1) then
        if(not inFileRegister) then editor:BeginUndoAction() end
        while posFind ~= -1 do
          if(inFileRegister) then inFileRegister(posFind) end

          editor:ReplaceTarget(findReplace.replaceText)
          editor:SetTargetStart(posFind + replaceLen)
          endTarget = endTarget + replaceLen - findLen
          editor:SetTargetEnd(endTarget)
          posFind = editor:SearchInTarget(findReplace.findText)
        end
        if(not inFileRegister) then editor:EndUndoAction() end

        replaced = true
      end
    else
      if findReplace.foundString then
        local start = editor:GetSelectionStart()
        editor:ReplaceSelection(findReplace.replaceText)
        editor:SetSelection(start, start + replaceLen)
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
  DisplayOutput(findReplace.curfilename..result)
  findReplace.occurrences = findReplace.occurrences + 1
end

local function ProcInFiles(startdir,mask,subdirs,replace)
  if (subdirs) then
    local dirs = FileSysGet(startdir..string_Pathsep.."*",wx.wxDIR)
    for _,dir in ipairs(dirs) do
      ProcInFiles(dir,mask,true,replace)
    end
  end

  local files = FileSysGet(startdir..string_Pathsep..mask,wx.wxFILE)
  for _,file in ipairs(files) do
    -- ignore .bak files when replacing and asked to store .bak files
    if not (replace and findReplace.fMakeBak and file:find('.bak$')) then
      findReplace.curfilename = file

      local filetext = FileRead(file)
      if filetext then
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
      end
    end
  end
end

function findReplace:RunInFiles(replace)
  if (not findReplace:HasText()) then
    return
  end

  findReplace.oveditor = wxstc.wxStyledTextCtrl(findReplace.dialog, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(1,1), wx.wxBORDER_STATIC)
  findReplace.occurrences = 0

  local fname = wx.wxFileName(findReplace.filedirText)
  local startdir = findReplace.filedirText
  DisplayOutput("FindInFiles: "..(replace and "Replacing" or "Searching for").." '"..findReplace.findText.."'.\n")

  ProcInFiles(startdir, findReplace.filemaskText, findReplace.fSubDirs, replace)

  DisplayOutput("FindInFiles: "..findReplace.occurrences.." instance(s) have been "..
    (replace and "replaced" or "found")..".\n")
  findReplace.oveditor = nil
end

local function createFindReplaceDialog(replace,infiles)
  local ID_FIND_NEXT = 1
  local ID_REPLACE = 2
  local ID_REPLACE_ALL = 3
  local ID_SETDIR = 4

  findReplace.replace = replace
  findReplace.infiles = infiles

  local findDialog = wx.wxDialog(ide.frame, wx.wxID_ANY, infiles and "Find In Files" or "Find",
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxDEFAULT_DIALOG_STYLE)

  -- Create right hand buttons and sizer
  local findButton = wx.wxButton(findDialog, ID_FIND_NEXT, infiles and "&Find All" or "&Find Next")
  findButton:SetDefault()
  local replaceButton = wx.wxButton(findDialog, ID_REPLACE, infiles and replace and "&Replace All" or "&Replace")
  local replaceAllButton = nil
  if (replace and not infiles) then
    replaceAllButton = wx.wxButton(findDialog, ID_REPLACE_ALL, "Replace &All")
  end
  local cancelButton = wx.wxButton(findDialog, wx.wxID_CANCEL, "Cancel")

  local buttonsSizer = wx.wxBoxSizer(wx.wxVERTICAL)
  buttonsSizer:Add(findButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  buttonsSizer:Add(replaceButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  if replaceAllButton then
    buttonsSizer:Add(replaceAllButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
  end
  buttonsSizer:Add(cancelButton, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)

  -- Create find/replace text entry sizer
  local findStatText = wx.wxStaticText( findDialog, wx.wxID_ANY, "Find: ")
  local findTextCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.findText,
    wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.findTextArray, wx.wxCB_DROPDOWN)
  findTextCombo:SetFocus()

  local infilesMaskStat,infilesMaskCombo
  local infilesDirStat,infilesDirCombo,infilesDirButton
  if (infiles) then
    infilesMaskStat = wx.wxStaticText( findDialog, wx.wxID_ANY, "File Type: ")
    infilesMaskCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.filemaskText,
      wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.filemaskTextArray)

    local fname = GetEditorFileAndCurInfo(true)
    if (fname) then
      findReplace.filedirText = fname:GetPath(wx.wxPATH_GET_VOLUME)
    end

    infilesDirStat = wx.wxStaticText( findDialog, wx.wxID_ANY, "Directory: ")
    infilesDirCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.filedirText, wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.filedirTextArray)
    infilesDirButton = wx.wxButton(findDialog, ID_SETDIR, "...",wx.wxDefaultPosition, wx.wxSize(26,20))
  end

  local replaceStatText, replaceTextCombo
  if (replace) then
    replaceStatText = wx.wxStaticText( findDialog, wx.wxID_ANY, "Replace: ")
    replaceTextCombo = wx.wxComboBox(findDialog, wx.wxID_ANY, findReplace.replaceText, wx.wxDefaultPosition, wx.wxDefaultSize, findReplace.replaceTextArray)
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
  local optionsSizer = wx.wxStaticBoxSizer(wx.wxVERTICAL, findDialog, "Options" )

  -- Create find/replace option checkboxes
  local wholeWordCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, "Match &whole word")
  local matchCaseCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, "Match &case")
  local wrapAroundCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, "Wrap ar&ound")
  local regexCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, "Regular &expression")
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
    scopeSizer = wx.wxStaticBoxSizer(wx.wxVERTICAL, findDialog, "In Files")

    subDirCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, "&Subdirectories")
    makeBakCheckBox = wx.wxCheckBox(findDialog, wx.wxID_ANY, ".&bak on Replace")
    subDirCheckBox:SetValue(findReplace.fSubDirs)
    makeBakCheckBox:SetValue(findReplace.fMakeBak)

    local optionSizer = wx.wxBoxSizer(wx.wxVERTICAL, findDialog)
    optionSizer:Add(subDirCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)
    optionSizer:Add(makeBakCheckBox, 0, wx.wxALL + wx.wxGROW + wx.wxCENTER, 3)

    scopeSizer:Add(optionSizer, 0, 0, 5)
  else
    scopeRadioBox = wx.wxRadioBox(findDialog, wx.wxID_ANY, "Scope", wx.wxDefaultPosition, wx.wxDefaultSize, {"&Up", "&Down"}, 1, wx.wxRA_SPECIFY_COLS)
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

  mainSizer:SetSizeHints( findDialog )
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

  findDialog:Connect(ID_FIND_NEXT, wx.wxEVT_COMMAND_BUTTON_CLICKED,
    function()
      TransferDataFromWindow()
      if (findReplace.infiles) then
        findReplace:RunInFiles()
        findReplace.dialog:Destroy()
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
        else
          findReplace:ReplaceString()
        end
      else
        findReplace.dialog:Destroy()
        findReplace.dialog = createFindReplaceDialog(true,infiles)
        findReplace.dialog:Show(true)
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
        local filePicker = wx.wxDirDialog(findDialog, "Choose a project directory",
          findReplace.filedirText~="" and findReplace.filedirText or wx.wxGetCwd(),wx.wxFLP_USE_TEXTCTRL)

        local res = filePicker:ShowModal(true)
        if res == wx.wxID_OK then
          infilesDirCombo:SetValue(filePicker:GetPath())
        end
      end)

  end

  findDialog:Connect(wx.wxID_ANY, wx.wxEVT_CLOSE_WINDOW,
    function (event)
      TransferDataFromWindow()
      event:Skip()
      findDialog:Show(false)
      findDialog:Destroy()
    end)

  return findDialog
end

function findReplace:Show(replace,infiles)
  self.dialog = nil
  self.dialog = createFindReplaceDialog(replace,infiles)
  self.dialog:Show(true)
end
