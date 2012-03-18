-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local notebook = frame.notebook
local openDocuments = ide.openDocuments
local uimgr = frame.uimgr

function NewFile(event)
  local editor = CreateEditor("untitled.lua")
  SetupKeywords(editor, "lua")
end

-- Find an editor page that hasn't been used at all, eg. an untouched NewFile()
local function findDocumentToReuse()
  local editor = nil
  for id, document in pairs(openDocuments) do
    if (document.editor:GetLength() == 0) and
    (not document.isModified) and (not document.filePath) and
    not (document.editor:GetReadOnly() == true) then
      editor = document.editor
      break
    end
  end
  return editor
end

function LoadFile(filePath, editor, file_must_exist)
  filePath = wx.wxFileName(filePath):GetFullPath()
  local cmpName = string.lower(string.gsub(filePath, "\\", "/"))
  
  -- prevent files from being reopened again
  if (not editor) then
    for id, doc in pairs(openDocuments) do
      local docName = doc.filePath and string.lower(string.gsub(doc.filePath, "\\", "/"))
      if cmpName == docName then
        notebook:SetSelection(doc.index)
        return doc.editor
      end
    end
  end

  -- if not opened yet, try open now
  local file_text = ""
  local handle = io.open(filePath, "rb")
  if handle then
    file_text = handle:read("*a")
    if GetConfigIOFilter("input") then
      file_text = GetConfigIOFilter("input")(filePath,file_text)
    end
    handle:close()
  elseif file_must_exist then
    return nil
  end

  if not editor then
    editor = findDocumentToReuse()
  end
  if not editor then
    editor = CreateEditor(wx.wxFileName(filePath):GetFullName() or "untitled.lua")
  end

  editor:Clear()
  editor:ClearAll()
  SetupKeywords(editor, GetFileExt(filePath))
  editor:MarkerDeleteAll(BREAKPOINT_MARKER)
  editor:MarkerDeleteAll(CURRENT_LINE_MARKER)
  editor:AppendText(file_text)
  if (ide.config.editor.autotabs) then
    local found = string.find(file_text,"\t") ~= nil
    editor:SetUseTabs(found)
  end

  editor:EmptyUndoBuffer()
  local id = editor:GetId()
  openDocuments[id].filePath = filePath
  openDocuments[id].fileName = wx.wxFileName(filePath):GetFullName()
  openDocuments[id].modTime = GetFileModTime(filePath)
  SetDocumentModified(id, false)
  editor:Colourise(0, -1)

  IndicateFunctions(editor)

  SettingsAppendFileToHistory(filePath)
  
  SetEditorSelection(nil)

  return editor
end

local function getExtsString()
  local knownexts = ""
  for i,spec in pairs(ide.specs) do
    if (spec.exts) then
      for n,ext in ipairs(spec.exts) do
        knownexts = knownexts.."*."..ext..";"
      end
    end
  end
  knownexts = knownexts:len() > 0 and knownexts:sub(1,-2) or nil

  local exts = knownexts and "Known Files ("..knownexts..")|"..knownexts.."|" or ""
  exts = exts.."All files (*)|*"

  return exts
end

function OpenFile(event)

  local exts = getExtsString()

  local fileDialog = wx.wxFileDialog(ide.frame, "Open file",
    "",
    "",
    exts,
    wx.wxOPEN + wx.wxFILE_MUST_EXIST)
  if fileDialog:ShowModal() == wx.wxID_OK then
    if not LoadFile(fileDialog:GetPath(), nil, true) then
      wx.wxMessageBox("Unable to load file '"..fileDialog:GetPath().."'.",
        "Error",
        wx.wxOK + wx.wxCENTRE, ide.frame)
    end
  end
  fileDialog:Destroy()
end

-- save the file to filePath or if filePath is nil then call SaveFileAs
function SaveFile(editor, filePath)
  if not filePath then
    return SaveFileAs(editor)
  else
    if (ide.config.savebak) then
      local backPath = filePath..".bak"
      os.remove(backPath)
      os.rename(filePath, backPath)
    end

    local handle = io.open(filePath, "wb")
    if handle then
      local st = editor:GetText()

      if GetConfigIOFilter("output") then
        st = GetConfigIOFilter("output")(filePath,st)
      end
      handle:write(st)
      handle:close()
      --editor:EmptyUndoBuffer()
      editor:SetSavePoint()
      local id = editor:GetId()
      openDocuments[id].filePath = filePath
      openDocuments[id].fileName = wx.wxFileName(filePath):GetFullName()
      openDocuments[id].modTime = GetFileModTime(filePath)
      SetDocumentModified(id, false)
      return true
    else
      wx.wxMessageBox("Unable to save file '"..filePath.."'.",
        "Error",
        wx.wxOK + wx.wxCENTRE, ide.frame)
    end
  end

  return false
end

function SaveFileAs(editor)
  local id = editor:GetId()
  local saved = false
  local filePath = openDocuments[id].filePath
  if (not filePath) then
    filePath = FileTreeGetDir()
    filePath = (filePath or "").."untitled"
  end

  local fn = wx.wxFileName(filePath)
  fn:Normalize() -- want absolute path for dialog

  local exts = getExtsString()

  local fileDialog = wx.wxFileDialog(ide.frame, "Save file as",
    fn:GetPath(wx.wxPATH_GET_VOLUME),
    fn:GetFullName(),
    exts,
    wx.wxSAVE)

  if fileDialog:ShowModal() == wx.wxID_OK then
    local filePath = fileDialog:GetPath()

    if SaveFile(editor, filePath) then
      SetupKeywords(editor, GetFileExt(filePath))
      IndicateFunctions(editor)
      if MarkupStyle then MarkupStyle(editor) end
      saved = true
    end
  end

  fileDialog:Destroy()
  return saved
end

function SaveAll()
  for id, document in pairs(openDocuments) do
    local editor = document.editor
    local filePath = document.filePath

    if document.isModified then
      SaveFile(editor, filePath) -- will call SaveFileAs if necessary
    end
  end
end

local function removePage(index)
  local prevIndex = nil
  local nextIndex = nil
  
  -- try to preserve old selection
  local selectIndex = notebook:GetSelection()
  selectIndex = selectIndex ~= index and selectIndex

  local delid = nil
  for id, document in pairsSorted(openDocuments,
    function(a, b) -- sort by document index
      return openDocuments[a].index < openDocuments[b].index
    end) do
    local wasselected = document.index == selectIndex
    if document.index < index then
      prevIndex = document.index
    elseif document.index == index then
      delid = id
      document.editor:Destroy()
    elseif document.index > index then
      document.index = document.index - 1
      if nextIndex == nil then
        nextIndex = document.index
      end
    end
    if (wasselected) then
      selectIndex = document.index
    end
  end

  if (delid) then
    openDocuments[delid] = nil
  end

  notebook:RemovePage(index)
  
  if selectIndex then
    notebook:SetSelection(selectIndex)
  elseif nextIndex then
    notebook:SetSelection(nextIndex)
  elseif prevIndex then
    notebook:SetSelection(prevIndex)
  end

  SetEditorSelection(nil) -- will use notebook GetSelection to update
end

function ClosePage(selection)
  local editor = GetEditor(selection)
  local id = editor:GetId()
  if SaveModifiedDialog(editor, true) ~= wx.wxID_CANCEL then
    DynamicWordsRemoveAll(editor)
    removePage(ide.openDocuments[id].index)
  end
end

-- Show a dialog to save a file before closing editor.
-- returns wxID_YES, wxID_NO, or wxID_CANCEL if allow_cancel
function SaveModifiedDialog(editor, allow_cancel)
  local result = wx.wxID_NO
  local id = editor:GetId()
  local document = openDocuments[id]
  local filePath = document.filePath
  local fileName = document.fileName
  if document.isModified then
    local message = "Do you want to save the changes to '"..(fileName or 'untitled').."'?"
    local dlg_styles = wx.wxYES_NO + wx.wxCENTRE + wx.wxICON_QUESTION
    if allow_cancel then dlg_styles = dlg_styles + wx.wxCANCEL end
    local dialog = wx.wxMessageDialog(ide.frame, message,
      "Save Changes?",
      dlg_styles)
    result = dialog:ShowModal()
    dialog:Destroy()
    if result == wx.wxID_YES then
      if not SaveFile(editor, filePath) then
        return wx.wxID_CANCEL -- cancel if canceled save dialog
      end
    end
  end

  return result
end

function SaveOnExit(allow_cancel)
  for id, document in pairs(openDocuments) do
    if (SaveModifiedDialog(document.editor, allow_cancel) == wx.wxID_CANCEL) then
      return false
    end

    document.isModified = false
  end

  return true
end

function FoldSome()
  local editor = GetEditor()
  editor:Colourise(0, -1) -- update doc's folding info
  local visible, baseFound, expanded, folded
  for ln = 2, editor.LineCount - 1 do
    local foldRaw = editor:GetFoldLevel(ln)
    local foldLvl = math.mod(foldRaw, 4096)
    local foldHdr = math.mod(math.floor(foldRaw / 8192), 2) == 1
    if not baseFound and (foldLvl == wxstc.wxSTC_FOLDLEVELBASE) then
      baseFound = true
      visible = editor:GetLineVisible(ln)
    end
    if foldHdr then
      if editor:GetFoldExpanded(ln) then
        expanded = true
      else
        folded = true
      end
    end
    if expanded and folded and baseFound then break end
  end
  local show = not visible or (not baseFound and expanded) or (expanded and folded)
  local hide = visible and folded

  if show then
    editor:ShowLines(1, editor.LineCount-1)
  end

  for ln = 1, editor.LineCount - 1 do
    local foldRaw = editor:GetFoldLevel(ln)
    local foldLvl = math.mod(foldRaw, 4096)
    local foldHdr = math.mod(math.floor(foldRaw / 8192), 2) == 1
    if show then
      if foldHdr then
        if not editor:GetFoldExpanded(ln) then editor:ToggleFold(ln) end
      end
    elseif hide and (foldLvl == wxstc.wxSTC_FOLDLEVELBASE) then
      if not foldHdr then
        editor:HideLines(ln, ln)
      end
    elseif foldHdr then
      if editor:GetFoldExpanded(ln) then
        editor:ToggleFold(ln)
      end
    end
  end
  editor:EnsureCaretVisible()
end

function EnsureRangeVisible(posStart, posEnd)
  local editor = GetEditor()
  if posStart > posEnd then
    posStart, posEnd = posEnd, posStart
  end

  local lineStart = editor:LineFromPosition(posStart)
  local lineEnd = editor:LineFromPosition(posEnd)
  for line = lineStart, lineEnd do
    editor:EnsureVisibleEnforcePolicy(line)
  end
end

function SetAllEditorsReadOnly(enable)
  for id, document in pairs(openDocuments) do
    local editor = document.editor
    editor:SetReadOnly(enable)
  end
end

-----------------
-- Debug related

function ClearAllCurrentLineMarkers()
  for id, document in pairs(openDocuments) do
    local editor = document.editor
    editor:MarkerDeleteAll(CURRENT_LINE_MARKER)
  end
end

function CompileProgram(editor)
  local editorText = editor:GetText()
  local id = editor:GetId()
  local filePath = DebuggerMakeFileName(editor, openDocuments[id].filePath)
  local ret, errMsg, line_num = wxlua.CompileLuaScript(editorText, filePath)
  if ide.frame.menuBar:IsChecked(ID_CLEAROUTPUT) then
    ClearOutput()
  end

  if line_num > -1 then
    DisplayOutput("Compilation error on line number :"..tostring(line_num).."\n"..errMsg.."\n")
    editor:GotoLine(line_num-1)
  else
    DisplayOutput("Compilation successful.\n")
  end

  return line_num == -1 -- return true if it compiled ok
end

------------------
-- Save & Close

function SaveIfModified(editor)
  local id = editor:GetId()
  if openDocuments[id].isModified then
    local saved = false
    if not openDocuments[id].filePath then
      local ret = wx.wxMessageBox("You must save the program before running it.\nPress cancel to abort running.",
        "Save file?", wx.wxOK + wx.wxCANCEL + wx.wxCENTRE, ide.frame)
      if ret == wx.wxOK then
        saved = SaveFileAs(editor)
      end
    else
      saved = SaveFile(editor, openDocuments[id].filePath)
    end

    if saved then
      openDocuments[id].isModified = false
    else
      return false -- not saved
    end
  end

  return true -- saved
end

function GetOpenFiles()
  local opendocs = {}
  for id, document in pairs(ide.openDocuments) do
    if (document.filePath) then
      local wxfname = wx.wxFileName(document.filePath)
      wxfname:Normalize()

      table.insert(opendocs,{fname=wxfname:GetFullPath(),id=document.index,
          cursorpos = document.editor:GetCurrentPos()})
    end
  end

  -- to keep tab order
  table.sort(opendocs,function(a,b) return (a.id < b.id) end)

  local openfiles = {}
  for i,doc in ipairs(opendocs) do
    table.insert(openfiles,{filename = doc.fname, cursorpos = doc.cursorpos} )
  end

  local id = GetEditor()
  id = id and id:GetId()

  return openfiles, id and openDocuments[id].index or 0
end

function SetOpenFiles(nametab,index)
  for i,doc in ipairs(nametab) do
    local editor = LoadFile(doc.filename,nil,true)
    if editor then
      editor:SetCurrentPos(doc.cursorpos or 0)
      editor:SetSelectionStart(doc.cursorpos or 0)
      editor:SetSelectionEnd(doc.cursorpos or 0)
      editor:EnsureCaretVisible()
    end
  end
  notebook:SetSelection(index or 0)
end

local beforeFullScreenPerspective
function ShowFullScreen(setFullScreen)
  if setFullScreen then
    beforeFullScreenPerspective = uimgr:SavePerspective()
    uimgr:GetPane("bottomnotebook"):Show(false)
    uimgr:GetPane("projpanel"):Show(false)
  elseif beforeFullScreenPerspective then
    uimgr:LoadPerspective(beforeFullScreenPerspective)
    beforeFullScreenPerspective = nil
  end

  uimgr:GetPane("toolBar"):Show(not setFullScreen)
  uimgr:Update()
  frame:ShowFullScreen(setFullScreen)
end

function CloseWindow(event)
  exitingProgram = true -- don't handle focus events

  if not SaveOnExit(event:CanVeto()) then
    event:Veto()
    exitingProgram = false
    return
  end

  ShowFullScreen(false)
  SettingsSaveProjectSession(FileTreeGetProjects())
  SettingsSaveFileSession(GetOpenFiles())
  SettingsSaveView()
  SettingsSaveFramePosition(ide.frame, "MainFrame")
  SettingsSaveEditorSettings()
  DebuggerCloseWatchWindow()
  DebuggerKillClient()
  ide.settings:delete() -- always delete the config
  event:Skip()
end
frame:Connect(wx.wxEVT_CLOSE_WINDOW, CloseWindow)
