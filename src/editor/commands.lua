-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local notebook = frame.notebook
local openDocuments = ide.openDocuments
local uimgr = frame.uimgr

local CURRENT_LINE_MARKER = StylesGetMarker("currentline")
local CURRENT_LINE_MARKER_VALUE = 2^CURRENT_LINE_MARKER
local BREAKPOINT_MARKER = StylesGetMarker("breakpoint")

function NewFile(event)
  local editor = CreateEditor()
  SetupKeywords(editor, "lua")
  AddEditor(editor, ide.config.default.fullname)
  return editor
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

function LoadFile(filePath, editor, file_must_exist, skipselection)
  local filePath = wx.wxFileName(filePath)
  filePath:Normalize() -- make it absolute and remove all .. and . if possible

  -- prevent files from being reopened again
  if (not editor) then
    for id, doc in pairs(openDocuments) do
      if doc.filePath and filePath:SameAs(wx.wxFileName(doc.filePath)) then
        if not skipselection then notebook:SetSelection(doc.index) end
        return doc.editor
      end
    end
  end
  filePath = filePath:GetFullPath()

  -- if not opened yet, try open now
  local file_text = FileRead(filePath)
  if file_text then
    if GetConfigIOFilter("input") then
      file_text = GetConfigIOFilter("input")(filePath,file_text)
    end
  elseif file_must_exist then
    return nil
  end

  local current = editor and editor:GetCurrentPos()
  editor = editor or findDocumentToReuse() or CreateEditor()

  editor:Freeze()
  editor:Clear()
  editor:ClearAll()
  SetupKeywords(editor, GetFileExt(filePath))
  editor:MarkerDeleteAll(BREAKPOINT_MARKER)
  editor:MarkerDeleteAll(CURRENT_LINE_MARKER)
  editor:AppendText(file_text or "")

  -- check the editor as it can be empty if the file has malformed UTF8
  if file_text and #file_text > 0 and #(editor:GetText()) == 0 then
    local replacement, invalid = "\022"
    file_text, invalid = fixUTF8(file_text, replacement)
    if #invalid > 0 then
      editor:AppendText(file_text)
      local lastline = nil
      for _, n in ipairs(invalid) do
        local line = editor:LineFromPosition(n)
        if line ~= lastline then
          DisplayOutputLn(("%s:%d: %s")
            :format(filePath, line+1, TR("Replaced an invalid UTF8 character with %s."):format(replacement)))
          lastline = line
        end
      end
    end
  end

  editor:Colourise(0, -1)
  editor:Thaw()

  if current then editor:GotoPos(current) end
  if (ide.config.editor.autotabs) then
    local found = string.find(file_text,"\t") ~= nil
    editor:SetUseTabs(found)
  end

  editor:EmptyUndoBuffer()
  local id = editor:GetId()
  if not openDocuments[id] then -- the editor has not been added to notebook
    AddEditor(editor, wx.wxFileName(filePath):GetFullName()
      or ide.config.default.fullname)
  end
  openDocuments[id].filePath = filePath
  openDocuments[id].fileName = wx.wxFileName(filePath):GetFullName()
  openDocuments[id].modTime = GetFileModTime(filePath)
  SetDocumentModified(id, false)

  IndicateFunctions(editor)

  SettingsAppendFileToHistory(filePath)

  -- activate the editor; this is needed for those cases when the editor is
  -- created from some other element, for example, from a project tree.
  if not skipselection then SetEditorSelection() end

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

  local exts = knownexts and TR("Known Files").." ("..knownexts..")|"..knownexts.."|" or ""
  exts = exts..TR("All files").." (*)|*"

  return exts
end

function OpenFile(event)
  local exts = getExtsString()
  local fileDialog = wx.wxFileDialog(ide.frame, TR("Open file"),
    "",
    "",
    exts,
    wx.wxFD_OPEN + wx.wxFD_FILE_MUST_EXIST)
  if fileDialog:ShowModal() == wx.wxID_OK then
    if not LoadFile(fileDialog:GetPath(), nil, true) then
      wx.wxMessageBox(TR("Unable to load file '%s'."):format(fileDialog:GetPath()),
        TR("Error"),
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
    if (ide.config.savebak) then FileRename(filePath, filePath..".bak") end

    local st = editor:GetText()
    if GetConfigIOFilter("output") then
      st = GetConfigIOFilter("output")(filePath,st)
    end

    local ok, err = FileWrite(filePath, st)
    if ok then
      editor:SetSavePoint()
      local id = editor:GetId()
      openDocuments[id].filePath = filePath
      openDocuments[id].fileName = wx.wxFileName(filePath):GetFullName()
      openDocuments[id].modTime = GetFileModTime(filePath)
      SetDocumentModified(id, false)
      SetAutoRecoveryMark()
      return true
    else
      wx.wxMessageBox(TR("Unable to save file '%s': %s"):format(filePath, err),
        TR("Error"),
        wx.wxICON_ERROR + wx.wxOK + wx.wxCENTRE, ide.frame)
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
    filePath = (filePath or "")..ide.config.default.name
  end

  local fn = wx.wxFileName(filePath)
  fn:Normalize() -- want absolute path for dialog

  local ext = fn:GetExt()
  local fileDialog = wx.wxFileDialog(ide.frame, TR("Save file as"),
    fn:GetPath(wx.wxPATH_GET_VOLUME),
    fn:GetFullName(),
    "*."..(ext and #ext > 0 and ext or "*"),
    wx.wxFD_SAVE)

  if fileDialog:ShowModal() == wx.wxID_OK then
    local filePath = fileDialog:GetPath()

    if SaveFile(editor, filePath) then
      SetEditorSelection() -- update title of the editor
      FileTreeRefresh() -- refresh the tree to reflect the new file
      FileTreeMarkSelected(filePath)
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

    if document.isModified or not document.filePath then
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

  SetEditorSelection() -- will use notebook GetSelection to update
end

function ClosePage(selection)
  local editor = GetEditor(selection)
  local id = editor:GetId()
  if SaveModifiedDialog(editor, true) ~= wx.wxID_CANCEL then
    DynamicWordsRemoveAll(editor)
    local debugger = ide.debugger
    -- check if the window with the scratchpad running is being closed
    if debugger and debugger.scratchpad and debugger.scratchpad.editors
    and debugger.scratchpad.editors[editor] then
      DebuggerScratchpadOff()
    end
    -- check if the debugger is running and is using the current window
    -- abort the debugger if the current marker is in the window being closed
    -- also abort the debugger if it is running, as we don't know what
    -- window will need to be activated when the debugger is paused
    if debugger and debugger.server and
      (debugger.running or editor:MarkerNext(0, CURRENT_LINE_MARKER_VALUE) >= 0) then
      debugger.terminate()
    end
    removePage(ide.openDocuments[id].index)

    -- disable full screen if the last tab is closed
    if not (notebook:GetSelection() >= 0) then ShowFullScreen(false) end
  end
end

function CloseAllPagesExcept(selection)
  local toclose = {}
  for _, document in pairs(ide.openDocuments) do
    table.insert(toclose, document.index)
  end

  table.sort(toclose)

  -- close pages for those files that match the project in the reverse order
  -- (as ids shift when pages are closed)
  for i = #toclose, 1, -1 do
    if toclose[i] ~= selection then ClosePage(toclose[i]) end
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
    local message = TR("Do you want to save the changes to '%s'?")
      :format(fileName or ide.config.default.name)
    local dlg_styles = wx.wxYES_NO + wx.wxCENTRE + wx.wxICON_QUESTION
    if allow_cancel then dlg_styles = dlg_styles + wx.wxCANCEL end
    local dialog = wx.wxMessageDialog(ide.frame, message,
      TR("Save Changes?"),
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
  end

  -- if all documents have been saved or refused to save, then mark those that
  -- are still modified as not modified (they don't need to be saved)
  -- to keep their tab names correct
  for id, document in pairs(openDocuments) do
    if document.isModified then SetDocumentModified(id, false) end
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

local compileOk, compileTotal = 0, 0
function CompileProgram(editor, quiet)
  -- remove shebang line (#!) as it throws a compilation error as
  -- loadstring() doesn't allow it even though lua/loadfile accepts it.
  -- replace with a new line to keep the number of lines the same.
  local editorText = editor:GetText():gsub("^#!.-\n", "\n")
  local id = editor:GetId()
  local filePath = DebuggerMakeFileName(editor, openDocuments[id].filePath)
  local func, line_num, errMsg, _ = loadstring(editorText, filePath), -1
  if not func then
    _, errMsg, line_num = wxlua.CompileLuaScript(editorText, filePath)
  end

  if ide.frame.menuBar:IsChecked(ID_CLEAROUTPUT) then ClearOutput() end

  compileTotal = compileTotal + 1
  if line_num > -1 then
    DisplayOutput(TR("Compilation error")
      .." "..TR("on line %d"):format(line_num)
      ..":\n"..errMsg:gsub("Lua:.-\n", ""))
    if not quiet then editor:GotoLine(line_num-1) end
  else
    compileOk = compileOk + 1
    if not quiet then
      DisplayOutputLn(TR("Compilation successful; %.0f%% success rate (%d/%d).")
        :format(compileOk/compileTotal*100, compileOk, compileTotal))
    end
  end

  return line_num == -1, editorText -- return true if it compiled ok
end

------------------
-- Save & Close

function SaveIfModified(editor)
  local id = editor:GetId()
  if openDocuments[id].isModified then
    local saved = false
    if not openDocuments[id].filePath then
      local ret = wx.wxMessageBox(
        TR("You must save the program first.").."\n"..TR("Press cancel to abort."),
        TR("Save file?"), wx.wxOK + wx.wxCANCEL + wx.wxCENTRE, ide.frame)
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

      table.insert(opendocs, {filename=wxfname:GetFullPath(),
        id=document.index, cursorpos = document.editor:GetCurrentPos()})
    end
  end

  -- to keep tab order
  table.sort(opendocs,function(a,b) return (a.id < b.id) end)

  local id = GetEditor()
  id = id and id:GetId()
  return opendocs, {index = (id and openDocuments[id].index or 0)}
end

function SetOpenFiles(nametab,params)
  for i,doc in ipairs(nametab) do
    local editor = LoadFile(doc.filename,nil,true,true) -- skip selection
    if editor then
      editor:SetCurrentPos(doc.cursorpos or 0)
      editor:SetSelectionStart(doc.cursorpos or 0)
      editor:SetSelectionEnd(doc.cursorpos or 0)
      editor:EnsureCaretVisible()
    end
  end
  notebook:SetSelection(params and params.index or 0)
  SetEditorSelection()
end

local beforeFullScreenPerspective
function ShowFullScreen(setFullScreen)
  if setFullScreen then
    beforeFullScreenPerspective = uimgr:SavePerspective()
    uimgr:GetPane("bottomnotebook"):Show(false)
    uimgr:GetPane("projpanel"):Show(false)
    SetEditorSelection() -- make sure the focus is on the editor
  elseif beforeFullScreenPerspective then
    uimgr:LoadPerspective(beforeFullScreenPerspective)
    beforeFullScreenPerspective = nil
  end

  uimgr:GetPane("toolBar"):Show(not setFullScreen)
  uimgr:Update()
  -- protect from systems that don't have ShowFullScreen (GTK on linux?)
  pcall(function() frame:ShowFullScreen(setFullScreen) end)
end

local function restoreFiles(files)
  -- open files, but ignore some functions that are not needed;
  -- as we may be opening multiple files, it doesn't make sense to
  -- select editor and do some other similar work after each file.
  local noop, func = function() end, LoadFile
  local genv = {SetEditorSelection = noop, SettingsAppendFileToHistory = noop}
  setmetatable(genv, {__index = _G})
  local env = getfenv(func)
  setfenv(func, genv)
  -- provide fake index so that it doesn't activate it as the index may be not
  -- quite correct if some of the existing files are already open in the IDE.
  SetOpenFiles(files, {index = #files + notebook:GetPageCount()})
  setfenv(func, env)
end

function ProjectConfig(dir, config)
  if config then ide.session.projects[dir] = config
  else return unpack(ide.session.projects[dir] or {}) end
end

function SetOpenTabs(params)
  local recovery, nametab = loadstring("return "..params.recovery)
  if recovery then recovery, nametab = pcall(recovery) end
  if not recovery then
    DisplayOutputLn(TR("Can't process auto-recovery record; invalid format: %s."):format(nametab))
    return
  end
  DisplayOutputLn(TR("Found auto-recovery record and restored saved session."))
  for _,doc in ipairs(nametab) do
    local editor = doc.filename and LoadFile(doc.filename,nil,true,true) or NewFile()
    local opendoc = openDocuments[editor:GetId()]
    if doc.content then
      notebook:SetPageText(opendoc.index, doc.tabname)
      editor:SetText(doc.content)
      if doc.filename and doc.modified < opendoc.modTime:GetTicks() then
        DisplayOutputLn(TR("File '%s' has more recent timestamp than restored '%s'; please review before saving.")
          :format(doc.filename, doc.tabname))
      end
    end
    editor:SetCurrentPos(doc.cursorpos or 0)
    editor:SetSelectionStart(doc.cursorpos or 0)
    editor:SetSelectionEnd(doc.cursorpos or 0)
    editor:EnsureCaretVisible()
  end
  notebook:SetSelection(params and params.index or 0)
  SetEditorSelection()
end

local function getOpenTabs()
  local opendocs = {}
  for id, document in pairs(ide.openDocuments) do
    table.insert(opendocs, {
      filename = document.filePath,
      tabname = notebook:GetPageText(document.index),
      modified = document.modTime and document.modTime:GetTicks(), -- get number of seconds
      content = document.isModified and document.editor:GetText() or nil,
      id = document.index, cursorpos = document.editor:GetCurrentPos()})
  end

  -- to keep tab order
  table.sort(opendocs, function(a,b) return (a.id < b.id) end)

  local id = GetEditor()
  id = id and id:GetId()
  return opendocs, {index = (id and openDocuments[id].index or 0)}
end

function SetAutoRecoveryMark()
  ide.session.lastupdated = os.time()
end

local function saveAutoRecovery(event)
  if not ide.config.autorecoverinactivity or not ide.session.lastupdated then return end
  if ide.session.lastupdated < (ide.session.lastsaved or 0)
  or ide.session.lastupdated + ide.config.autorecoverinactivity > os.time()
    then return end

  -- find all open modified files and save them
  local opentabs, params = getOpenTabs()
  if #opentabs > 0 then
    params.recovery = require('mobdebug').line(opentabs, {comment = false})
    SettingsSaveAll()
    SettingsSaveFileSession({}, params)
    ide.settings:Flush()
  end
  ide.session.lastsaved = os.time()
  ide.frame.statusBar:SetStatusText(
    TR("Saved auto-recover at %s."):format(os.date("%H:%M:%S")), 1)
end

function StoreRestoreProjectTabs(curdir, newdir)
  local win = ide.osname == 'Windows'
  local interpreter = ide.interpreter.fname
  local current, closing, restore = notebook:GetSelection(), 0, false

  if curdir and #curdir > 0 then
    local lowcurdir = win and string.lower(curdir) or curdir
    local lownewdir = win and string.lower(newdir) or newdir
    local projdocs, closdocs = {}, {}
    for _, document in ipairs(GetOpenFiles()) do
      local dpath = win and string.lower(document.filename) or document.filename
      if dpath:find(lowcurdir, 1, true) then
        table.insert(projdocs, document)
        closing = closing + (document.id < current and 1 or 0)
        -- only close if the file is not in new project as it would be reopened
        if not dpath:find(lownewdir, 1, true) then
          table.insert(closdocs, document)
        end
      elseif document.id == current then restore = true end
    end

    -- adjust for the number of closing tabs on the left from the current one
    current = current - closing

    -- save opened files from this project
    ProjectConfig(curdir, {projdocs,
      {index = notebook:GetSelection() - current, interpreter = interpreter}})

    -- close pages for those files that match the project in the reverse order
    -- (as ids shift when pages are closed)
    for i = #closdocs, 1, -1 do ClosePage(closdocs[i].id) end
  end

  local files, params = ProjectConfig(newdir)
  if files then restoreFiles(files) end

  if params and params.interpreter and ide.interpreter.fname ~= params.interpreter then
    ProjectSetInterpreter(params.interpreter) -- set the interpreter
  end

  local index = params and params.index
  if notebook:GetPageCount() == 0 then NewFile()
  elseif restore and current >= 0 then notebook:SetSelection(current)
  elseif index and index >= 0 and files[index+1] then
    -- move the editor tab to the front with the file from the config
    LoadFile(files[index+1].filename, nil, true)
    SetEditorSelection() -- activate the editor in the active tab
  end

  -- remove current config as it may change; the current configuration is
  -- stored with the general config.
  -- The project configuration will be updated when the project is changed.
  ProjectConfig(newdir, {})
end

function CloseWindow(event)
  -- if the app is already exiting, then help it exit; wxwidgets on Windows
  -- is supposed to report Shutdown/logoff events by setting CanVeto() to
  -- false, but it doesn't happen. We simply leverage the fact that
  -- CloseWindow is called several times in this case and exit. Similar
  -- behavior has been also seen on Linux, so this logic applies everywhere.
  if ide.exitingProgram then os.exit() end

  ide.exitingProgram = true -- don't handle focus events

  if not SaveOnExit(event:CanVeto()) then
    event:Veto()
    ide.exitingProgram = false
    return
  end

  ShowFullScreen(false)
  ide.frame:Hide() -- hide everything while the IDE exits
  SettingsSaveAll()
  if DebuggerCloseWatchWindow then DebuggerCloseWatchWindow() end
  if DebuggerCloseStackWindow then DebuggerCloseStackWindow() end
  if DebuggerShutdown then DebuggerShutdown() end
  ide.settings:delete() -- always delete the config
  if ide.session.timer then ide.session.timer:Stop() end
  event:Skip()

  -- without explicit exit() the IDE crashes with SIGILL exception when closed
  -- on MacOS compiled under 64bit with wxwidgets 2.9.3
  if ide.osname == "Macintosh" then os.exit() end
end
frame:Connect(wx.wxEVT_CLOSE_WINDOW, CloseWindow)

frame:Connect(wx.wxEVT_TIMER, saveAutoRecovery)

if ide.config.autorecoverinactivity then
  ide.session.timer = wx.wxTimer(frame)
  -- check at least 5s to be never more than 5s off
  ide.session.timer:Start(math.min(5, ide.config.autorecoverinactivity)*1000)
end
