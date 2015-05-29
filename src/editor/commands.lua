-- Copyright 2011-14 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local frame = ide.frame
local notebook = frame.notebook
local openDocuments = ide.openDocuments
local uimgr = frame.uimgr
local unpack = table.unpack or unpack

local CURRENT_LINE_MARKER = StylesGetMarker("currentline")
local CURRENT_LINE_MARKER_VALUE = 2^CURRENT_LINE_MARKER

function NewFile(filename)
  filename = filename or ide.config.default.fullname
  local editor = CreateEditor()
  editor:SetupKeywords(GetFileExt(filename))
  local doc = AddEditor(editor, filename)
  if doc then
    PackageEventHandle("onEditorNew", editor)
    SetEditorSelection(doc.index)
  end
  return editor
end

-- Find an editor page that hasn't been used at all, eg. an untouched NewFile()
local function findUnusedEditor()
  local editor
  for _, document in pairs(openDocuments) do
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
  filePath = filePath:gsub("%s+$","")
  filePath = wx.wxFileName(filePath)
  filePath:Normalize() -- make it absolute and remove all .. and . if possible
  filePath = filePath:GetFullPath()

  -- if the file name is empty or is a directory, don't do anything
  if filePath == '' or wx.wxDirExists(filePath) then return nil end

  -- prevent files from being reopened again
  if (not editor) then
    local doc = ide:FindDocument(filePath)
    if doc then
      if not skipselection and doc.index ~= notebook:GetSelection() then
        -- selecting the same tab doesn't trigger PAGE_CHANGE event,
        -- but moves the focus to the tab bar, which needs to be avoided.
        notebook:SetSelection(doc.index)
      end
      return doc.editor
    end
  end

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
  editor = editor or findUnusedEditor() or CreateEditor()

  editor:Freeze()
  editor:SetupKeywords(GetFileExt(filePath))
  editor:MarkerDeleteAll(-1)

  -- remove BOM from UTF-8 encoded files; store BOM to add back when saving
  editor.bom = string.char(0xEF,0xBB,0xBF)
  if file_text and editor:GetCodePage() == wxstc.wxSTC_CP_UTF8
  and file_text:find("^"..editor.bom) then
    file_text = file_text:gsub("^"..editor.bom, "")
  else
    -- set to 'false' as checks for nil on wxlua objects may fail at run-time
    editor.bom = false
  end
  editor:SetText(file_text or "")

  -- check the editor as it can be empty if the file has malformed UTF8;
  -- skip binary files with unknown extensions as they may have any sequences;
  -- can't show them anyway.
  if file_text and #file_text > 0 and #(editor:GetText()) == 0
  and (editor.spec ~= ide.specs.none or not IsBinary(file_text)) then
    local replacement, invalid = "\022"
    file_text, invalid = FixUTF8(file_text, replacement)
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
  editor:ResetTokenList() -- reset list of tokens if this is a reused editor
  editor:Thaw()

  local edcfg = ide.config.editor
  if current then editor:GotoPos(current) end
  if (file_text and edcfg.autotabs) then
    -- use tabs if they are already used
    -- or if "usetabs" is set and no space indentation is used in a file
    editor:SetUseTabs(string.find(file_text, "\t") ~= nil
      or edcfg.usetabs and (file_text:find("%f[^\r\n] ") or file_text:find("^ ")) == nil)
  end
  
  if (file_text and edcfg.checkeol) then
    -- Auto-detect CRLF/LF line-endings
    local foundcrlf = string.find(file_text,"\r\n") ~= nil
    local foundlf = (string.find(file_text,"[^\r]\n") ~= nil)
      or (string.find(file_text,"^\n") ~= nil) -- edge case: file beginning with LF and having no other LF
    if foundcrlf and foundlf then -- file with mixed line-endings
      DisplayOutputLn(("%s: %s")
        :format(filePath, TR("Mixed end-of-line encodings detected.")..' '..
          TR("Use '%s' to show line endings and '%s' to convert them.")
        :format("GetEditor():SetViewEOL(1)", "GetEditor():ConvertEOLs(GetEditor():GetEOLMode())")))
    elseif foundcrlf then
      editor:SetEOLMode(wxstc.wxSTC_EOL_CRLF)
    elseif foundlf then
      editor:SetEOLMode(wxstc.wxSTC_EOL_LF)
    -- else (e.g. file is 1 line long or uses another line-ending): use default EOL mode
    end
  end

  editor:EmptyUndoBuffer()
  local doc = ide:GetDocument(editor)
  if doc then -- existing editor; switch to the tab
    notebook:SetSelection(doc:GetTabIndex())
  else -- the editor has not been added to notebook
    doc = AddEditor(editor, wx.wxFileName(filePath):GetFullName()
      or ide.config.default.fullname)
  end
  doc.filePath = filePath
  doc.fileName = wx.wxFileName(filePath):GetFullName()
  doc.modTime = GetFileModTime(filePath)

  doc:SetModified(false)
  doc:SetTabText(doc:GetFileName())

  -- activate the editor; this is needed for those cases when the editor is
  -- created from some other element, for example, from a project tree.
  if not skipselection then SetEditorSelection() end

  PackageEventHandle("onEditorLoad", editor)

  return editor
end

function ReLoadFile(filePath, editor, ...)
  if not editor then return LoadFile(filePath, editor, ...) end

  -- save all markers
  local maskany = 2^24-1
  local markers = {}
  local line = editor:MarkerNext(0, maskany)
  while line > -1 do
    table.insert(markers, {line, editor:MarkerGet(line), editor:GetLine(line)})
    line = editor:MarkerNext(line + 1, maskany)
  end
  local lines = editor:GetLineCount()

  -- load file into the same editor
  editor = LoadFile(filePath, editor, ...)
  if not editor then return end

  if #markers > 0 then -- restore all markers
    local samelinecount = lines == editor:GetLineCount()
    for _, marker in ipairs(markers) do
      local line, mask, text = unpack(marker)
      if samelinecount then
        -- restore marker at the same line number
        editor:MarkerAddSet(line, mask)
      else
        -- find matching line in the surrounding area and restore marker there
        for _, l in ipairs({line, line-1, line-2, line+1, line+2}) do
          if text == editor:GetLine(l) then
            editor:MarkerAddSet(l, mask)
            break
          end
        end
      end
    end
  end

  return editor
end

function ActivateFile(filename)
  local name, suffix, value = filename:match('(.+):([lLpP]?)(%d+)$')
  if name and not wx.wxFileExists(filename) then filename = name end

  -- check if non-existing file can be loaded from the project folder;
  -- this is to handle: "project file" used on the command line
  if not wx.wxFileExists(filename) and not wx.wxIsAbsolutePath(filename) then
    filename = GetFullPathIfExists(ide:GetProject(), filename) or filename
  end

  local opened = LoadFile(filename, nil, true)
  if opened and value then
    if suffix:upper() == 'P' then opened:GotoPosDelayed(tonumber(value))
    else opened:GotoPosDelayed(opened:PositionFromLine(value-1))
    end
  end
  return opened
end

local function getExtsString()
  local exts = ide:GetKnownExtensions()
  local knownexts = #exts > 0 and "*."..table.concat(exts, ";*.") or nil
  return (knownexts and TR("Known Files").." ("..knownexts..")|"..knownexts.."|" or "")
  .. TR("All files").." (*)|*"
end

function ReportError(msg)
  return wx.wxMessageBox(msg, TR("Error"), wx.wxICON_ERROR + wx.wxOK + wx.wxCENTRE, ide.frame)
end

function OpenFile(event)
  local editor = GetEditor()
  local path = editor and ide:GetDocument(editor):GetFilePath() or nil
  local fileDialog = wx.wxFileDialog(ide.frame, TR("Open file"),
    (path and GetPathWithSep(path) or FileTreeGetDir() or ""),
    "",
    getExtsString(),
    wx.wxFD_OPEN + wx.wxFD_FILE_MUST_EXIST)
  if fileDialog:ShowModal() == wx.wxID_OK then
    if not LoadFile(fileDialog:GetPath(), nil, true) then
      ReportError(TR("Unable to load file '%s'."):format(fileDialog:GetPath()))
    end
  end
  fileDialog:Destroy()
end

-- save the file to filePath or if filePath is nil then call SaveFileAs
function SaveFile(editor, filePath)
  -- this event can be aborted
  -- as SaveFileAs calls SaveFile, this event may be called two times:
  -- first without filePath and then with filePath
  if PackageEventHandle("onEditorPreSave", editor, filePath) == false then
    return false
  end

  if not filePath then
    return SaveFileAs(editor)
  else
    if ide.config.savebak then
      local ok, err = FileRename(filePath, filePath..".bak")
      if not ok then
        ReportError(TR("Unable to save file '%s': %s"):format(filePath..".bak", err))
        return
      end
    end

    local st = (editor:GetCodePage() == wxstc.wxSTC_CP_UTF8 and editor.bom or "")
      .. editor:GetText()
    if GetConfigIOFilter("output") then
      st = GetConfigIOFilter("output")(filePath,st)
    end

    local ok, err = FileWrite(filePath, st)
    if ok then
      editor:SetSavePoint()
      local doc = ide:GetDocument(editor)
      doc.filePath = filePath
      doc.fileName = wx.wxFileName(filePath):GetFullName()
      doc.modTime = GetFileModTime(filePath)
      doc:SetModified(false)
      doc:SetTabText(doc:GetFileName())
      SetAutoRecoveryMark()
      FileTreeMarkSelected(filePath)

      PackageEventHandle("onEditorSave", editor)

      return true
    else
      ReportError(TR("Unable to save file '%s': %s"):format(filePath, err))
    end
  end

  return false
end

function ApproveFileOverwrite()
  return wx.wxMessageBox(
    TR("File already exists.").."\n"..TR("Do you want to overwrite it?"),
    GetIDEString("editormessage"),
    wx.wxYES_NO + wx.wxCENTRE, ide.frame) == wx.wxYES
end

function SaveFileAs(editor)
  local id = editor:GetId()
  local saved = false
  local filePath = (openDocuments[id].filePath
    or ((FileTreeGetDir() or "")
        ..(openDocuments[id].fileName or ide.config.default.name)))

  local fn = wx.wxFileName(filePath)
  fn:Normalize() -- want absolute path for dialog

  local ext = fn:GetExt()
  if (not ext or #ext == 0) and editor.spec and editor.spec.exts then
    ext = editor.spec.exts[1]
    -- set the extension on the file if assigned as this is used by OSX/Linux
    -- to present the correct default "save as type" choice.
    if ext then fn:SetExt(ext) end
  end
  local fileDialog = wx.wxFileDialog(ide.frame, TR("Save file as"),
    fn:GetPath(wx.wxPATH_GET_VOLUME),
    fn:GetFullName(),
    -- specify the current extension plus all other extensions based on specs
    (ext and #ext > 0 and "*."..ext.."|*."..ext.."|" or "")..getExtsString(),
    wx.wxFD_SAVE)

  if fileDialog:ShowModal() == wx.wxID_OK then
    local filePath = fileDialog:GetPath()

    -- check if there is another tab with the same name and prepare to close it
    local existing = (ide:FindDocument(filePath) or {}).index
    local cansave = fn:GetFullName() == filePath -- saving into the same file
       or not wx.wxFileName(filePath):FileExists() -- or a new file
       or ApproveFileOverwrite()

    if cansave and SaveFile(editor, filePath) then
      SetEditorSelection() -- update title of the editor
      if ext ~= GetFileExt(filePath) then
        -- new extension, so setup new keywords and re-apply indicators
        editor:ClearDocumentStyle() -- remove styles from the document
        editor:SetupKeywords(GetFileExt(filePath))
        IndicateAll(editor)
        IndicateFunctionsOnly(editor)
        MarkupStyle(editor)
      end
      saved = true

      if existing then
        -- save the current selection as it may change after closing
        local current = notebook:GetSelection()
        ClosePage(existing)
        -- restore the selection if it changed
        if current ~= notebook:GetSelection() then
          notebook:SetSelection(current)
        end
      end
    end
  end

  fileDialog:Destroy()
  return saved
end

function SaveAll(quiet)
  for _, document in pairs(openDocuments) do
    local editor = document.editor
    local filePath = document.filePath

    if (document.isModified or not document.filePath) -- need to save
    and (document.filePath or not quiet) then -- have path or can ask user
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

  -- need to set editor selection as it's called *after* PAGE_CHANGED event
  SetEditorSelection()
end

function ClosePage(selection)
  local editor = GetEditor(selection)
  local id = editor:GetId()

  if PackageEventHandle("onEditorPreClose", editor) == false then
    return false
  end

  if SaveModifiedDialog(editor, true) ~= wx.wxID_CANCEL then
    DynamicWordsRemoveAll(editor)
    local debugger = ide.debugger
    -- check if the window with the scratchpad running is being closed
    if debugger and debugger.scratchpad and debugger.scratchpad.editors
    and debugger.scratchpad.editors[editor] then
      DebuggerScratchpadOff()
    end
    -- check if the debugger is running and is using the current window;
    -- abort the debugger if the current marker is in the window being closed
    if debugger and debugger.server and
      (editor:MarkerNext(0, CURRENT_LINE_MARKER_VALUE) >= 0) then
      debugger.terminate()
    end
    PackageEventHandle("onEditorClose", editor)
    removePage(ide.openDocuments[id].index)

    -- disable full screen if the last tab is closed
    if not (notebook:GetSelection() >= 0) then ShowFullScreen(false) end
    return true
  end
  return false
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
  for _, document in pairs(openDocuments) do
    if (SaveModifiedDialog(document.editor, allow_cancel) == wx.wxID_CANCEL) then
      return false
    end
  end

  -- if all documents have been saved or refused to save, then mark those that
  -- are still modified as not modified (they don't need to be saved)
  -- to keep their tab names correct
  for _, document in pairs(openDocuments) do
    if document.isModified then document:SetModified(false) end
  end

  return true
end

function SetAllEditorsReadOnly(enable)
  for _, document in pairs(openDocuments) do
    document.editor:SetReadOnly(enable)
  end
end

-----------------
-- Debug related

function ClearAllCurrentLineMarkers()
  for _, document in pairs(openDocuments) do
    document.editor:MarkerDeleteAll(CURRENT_LINE_MARKER)
    document.editor:Refresh() -- needed for background markers that don't get refreshed (wx2.9.5)
  end
end

-- remove shebang line (#!) as it throws a compilation error as
-- loadstring() doesn't allow it even though lua/loadfile accepts it.
-- replace with a new line to keep the number of lines the same.
function StripShebang(code) return (code:gsub("^#!.-\n", "\n")) end

local compileOk, compileTotal = 0, 0
function CompileProgram(editor, params)
  local params = {
    jumponerror = (params or {}).jumponerror ~= false,
    reportstats = (params or {}).reportstats ~= false,
    keepoutput = (params or {}).keepoutput,
  }
  local doc = ide:GetDocument(editor)
  local filePath = doc:GetFilePath() or doc:GetFileName()
  local func, err = loadstring(StripShebang(editor:GetText()), '@'..filePath)
  local line = not func and tonumber(err:match(":(%d+)%s*:")) or nil

  if not params.keepoutput then ClearOutput() end

  compileTotal = compileTotal + 1
  if func then
    compileOk = compileOk + 1
    if params.reportstats then
      DisplayOutputLn(TR("Compilation successful; %.0f%% success rate (%d/%d).")
        :format(compileOk/compileTotal*100, compileOk, compileTotal))
    end
  else
    DisplayOutputLn(TR("Compilation error").." "..TR("on line %d"):format(line)..":")
    DisplayOutputLn((err:gsub("\n$", "")))
    -- check for escapes invalid in LuaJIT/Lua 5.2 that are allowed in Lua 5.1
    if err:find('invalid escape sequence') then
      local s = editor:GetLine(line-1)
      local cleaned = s
        :gsub('\\[abfnrtv\\"\']', '  ')
        :gsub('(\\x[0-9a-fA-F][0-9a-fA-F])', function(s) return string.rep(' ', #s) end)
        :gsub('(\\%d%d?%d?)', function(s) return string.rep(' ', #s) end)
        :gsub('(\\z%s*)', function(s) return string.rep(' ', #s) end)
      local invalid = cleaned:find("\\")
      if invalid then
        DisplayOutputLn(TR("Consider removing backslash from escape sequence '%s'.")
          :format(s:sub(invalid,invalid+1)))
      end
    end
    if line and params.jumponerror and line-1 ~= editor:GetCurrentLine() then
      editor:GotoLine(line-1)
    end
  end

  return func ~= nil -- return true if it compiled ok
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
  for _, document in pairs(ide.openDocuments) do
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
  for _, doc in ipairs(nametab) do
    local editor = LoadFile(doc.filename,nil,true,true) -- skip selection
    if editor then editor:GotoPosDelayed(doc.cursorpos or 0) end
  end
  notebook:SetSelection(params and params.index or 0)
  SetEditorSelection()
end

local beforeFullScreenPerspective
local statusbarShown

function ShowFullScreen(setFullScreen)
  if setFullScreen then
    beforeFullScreenPerspective = uimgr:SavePerspective()

    local panes = frame.uimgr:GetAllPanes()
    for index = 0, panes:GetCount()-1 do
      local name = panes:Item(index).name
      if name ~= "notebook" then frame.uimgr:GetPane(name):Hide() end
    end
    uimgr:Update()
    SetEditorSelection() -- make sure the focus is on the editor
  elseif beforeFullScreenPerspective then
    uimgr:LoadPerspective(beforeFullScreenPerspective, true)
    beforeFullScreenPerspective = nil
  end

  -- On OSX, status bar is not hidden when switched to
  -- full screen: http://trac.wxwidgets.org/ticket/14259; do manually.
  -- need to turn off before showing full screen and turn on after,
  -- otherwise the window is restored incorrectly and is reduced in size.
  if ide.osname == 'Macintosh' and setFullScreen then
    statusbarShown = frame:GetStatusBar():IsShown()
    frame:GetStatusBar():Hide()
  end

  -- protect from systems that don't have ShowFullScreen (GTK on linux?)
  pcall(function() frame:ShowFullScreen(setFullScreen) end)

  if ide.osname == 'Macintosh' and not setFullScreen then
    if statusbarShown then
      frame:GetStatusBar():Show()
      -- refresh AuiManager as the statusbar may be shown below the border
      uimgr:Update()
    end
  end
end

function ProjectConfig(dir, config)
  if config then ide.session.projects[dir] = config
  else return unpack(ide.session.projects[dir] or {}) end
end

function SetOpenTabs(params)
  local recovery, nametab = LoadSafe("return "..params.recovery)
  if not recovery then
    DisplayOutputLn(TR("Can't process auto-recovery record; invalid format: %s."):format(nametab))
    return
  end
  if not params.quiet then
    DisplayOutputLn(TR("Found auto-recovery record and restored saved session."))
  end
  for _,doc in ipairs(nametab) do
    -- check for missing file if no content is stored
    if doc.filepath and not doc.content and not wx.wxFileExists(doc.filepath) then
      DisplayOutputLn(TR("File '%s' is missing and can't be recovered.")
        :format(doc.filepath))
    else
      local editor = (doc.filepath and LoadFile(doc.filepath,nil,true,true)
        or findUnusedEditor() or NewFile(doc.filename))
      local opendoc = ide:GetDocument(editor)
      if doc.content then
        editor:SetText(doc.content)
        if doc.filepath and opendoc.modTime and doc.modified < opendoc.modTime:GetTicks() then
          DisplayOutputLn(TR("File '%s' has more recent timestamp than restored '%s'; please review before saving.")
            :format(doc.filepath, opendoc:GetTabText()))
        end
        opendoc:SetModified(true)
      end
      editor:GotoPosDelayed(doc.cursorpos or 0)
    end
  end
  notebook:SetSelection(params and params.index or 0)
  SetEditorSelection()
end

local function getOpenTabs()
  local opendocs = {}
  for _, document in pairs(ide.openDocuments) do
    local editor = document:GetEditor()
    table.insert(opendocs, {
      filename = document:GetFileName(),
      filepath = document:GetFilePath(),
      tabname = document:GetTabText(),
      modified = document:GetModTime() and document:GetModTime():GetTicks(), -- get number of seconds
      content = document:IsModified() and editor:GetText() or nil,
      id = document:GetTabIndex(),
      cursorpos = editor:GetCurrentPos()})
  end

  -- to keep tab order
  table.sort(opendocs, function(a,b) return (a.id < b.id) end)

  local ed = GetEditor()
  local doc = ed and ide:GetDocument(ed)
  return opendocs, {index = (doc and doc:GetTabIndex() or 0)}
end

function SetAutoRecoveryMark()
  ide.session.lastupdated = os.time()
end

local function generateRecoveryRecord(opentabs)
  return require('mobdebug').line(opentabs, {comment = false})
end

local function saveHotExit()
  local opentabs, params = getOpenTabs()
  if #opentabs > 0 then
    params.recovery = generateRecoveryRecord(opentabs)
    params.quiet = true
    SettingsSaveFileSession({}, params)
  end
end

local function saveAutoRecovery(force)
  if not ide.config.autorecoverinactivity then return end

  local lastupdated = ide.session.lastupdated
  if not force then
    if not lastupdated or lastupdated < (ide.session.lastsaved or 0) then return end
  end

  local now = os.time()
  if not force and lastupdated + ide.config.autorecoverinactivity > now then return end

  -- find all open modified files and save them
  local opentabs, params = getOpenTabs()
  if #opentabs > 0 then
    params.recovery = generateRecoveryRecord(opentabs)
    SettingsSaveAll()
    SettingsSaveFileSession({}, params)
    ide.settings:Flush()
  end
  ide.session.lastsaved = now
  ide.frame.statusBar:SetStatusText(
    TR("Saved auto-recover at %s."):format(os.date("%H:%M:%S")), 1)
end

local function fastWrap(func, ...)
  -- ignore SetEditorSelection that is not needed as `func` may work on
  -- multipe files, but editor needs to be selected once.
  local SES = SetEditorSelection
  SetEditorSelection = function() end
  func(...)
  SetEditorSelection = SES
end

function StoreRestoreProjectTabs(curdir, newdir)
  local win = ide.osname == 'Windows'
  local interpreter = ide.interpreter.fname
  local current, closing, restore = notebook:GetSelection(), 0, false

  if ide.osname ~= 'Macintosh' then notebook:Freeze() end

  if curdir and #curdir > 0 then
    local lowcurdir = win and string.lower(curdir) or curdir
    local lownewdir = win and string.lower(newdir) or newdir
    local projdocs, closdocs = {}, {}
    for _, document in ipairs(GetOpenFiles()) do
      local dpath = win and string.lower(document.filename) or document.filename
      -- check if the filename is in the same folder
      if dpath:find(lowcurdir, 1, true) == 1
      and dpath:find("^[\\/]", #lowcurdir+1) then
        table.insert(projdocs, document)
        closing = closing + (document.id < current and 1 or 0)
        -- only close if the file is not in new project as it would be reopened
        if not dpath:find(lownewdir, 1, true)
        or not dpath:find("^[\\/]", #lownewdir+1) then
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
    for i = #closdocs, 1, -1 do fastWrap(ClosePage, closdocs[i].id) end
  end

  local files, params = ProjectConfig(newdir)
  if files then
    -- provide fake index so that it doesn't activate it as the index may be not
    -- quite correct if some of the existing files are already open in the IDE.
    fastWrap(SetOpenFiles, files, {index = #files + notebook:GetPageCount()})
  end

  if params and params.interpreter then
    ProjectSetInterpreter(params.interpreter) -- set the interpreter
  end

  if ide.osname ~= 'Macintosh' then notebook:Thaw() end

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

local function closeWindow(event)
  -- if the app is already exiting, then help it exit; wxwidgets on Windows
  -- is supposed to report Shutdown/logoff events by setting CanVeto() to
  -- false, but it doesn't happen. We simply leverage the fact that
  -- CloseWindow is called several times in this case and exit. Similar
  -- behavior has been also seen on Linux, so this logic applies everywhere.
  if ide.exitingProgram then os.exit() end

  ide.exitingProgram = true -- don't handle focus events

  if not ide.config.hotexit and not SaveOnExit(event:CanVeto()) then
    event:Veto()
    ide.exitingProgram = false
    return
  end

  ShowFullScreen(false)

  PackageEventHandle("onAppClose")

  -- first need to detach all processes IDE has launched as the current
  -- process is likely to terminate before child processes are terminated,
  -- which may lead to a crash when EVT_END_PROCESS event is called.
  DetachChildProcess()
  DebuggerShutdown()

  SettingsSaveAll()
  if ide.config.hotexit then saveHotExit() end
  ide.settings:Flush()

  do -- hide all floating panes first
    local panes = frame.uimgr:GetAllPanes()
    for index = 0, panes:GetCount()-1 do
      local pane = frame.uimgr:GetPane(panes:Item(index).name)
      if pane:IsFloating() then pane:Hide() end
    end
  end
  frame.uimgr:Update() -- hide floating panes
  frame.uimgr:UnInit()
  frame:Hide() -- hide the main frame while the IDE exits

  -- stop all the timers
  for _, timer in pairs(ide.timers) do timer:Stop() end

  event:Skip()
end
frame:Connect(wx.wxEVT_CLOSE_WINDOW, closeWindow)

frame:Connect(wx.wxEVT_TIMER, function() saveAutoRecovery() end)

-- in the presence of wxAuiToolbar, when (1) the app gets focus,
-- (2) a floating panel is closed or (3) a toolbar dropdown is closed,
-- the focus is always on the toolbar when the app gets focus,
-- so to restore the focus correctly, need to track where the control is
-- and to set the focus to the last element that had focus.
-- it would be easier to track KILL_FOCUS events, but controls on OSX
-- don't always generate KILL_FOCUS events (see relevant wxwidgets
-- tickets: http://trac.wxwidgets.org/ticket/14142
-- and http://trac.wxwidgets.org/ticket/14269)

ide.editorApp:Connect(wx.wxEVT_SET_FOCUS, function(event)
  if ide.exitingProgram then return end

  local win = ide.frame:FindFocus()
  if win then
    local class = win:GetClassInfo():GetClassName()
    -- don't set focus on the main frame or toolbar
    if ide.infocus and (class == 'wxAuiToolBar' or class == 'wxFrame') then
      -- check if the window is shown before returning focus to it,
      -- as it may lead to a recursion in event handlers on OSX (wxwidgets 2.9.5).
      pcall(function() if ide:IsWindowShown(ide.infocus) then ide.infocus:SetFocus() end end)
      return
    end

    -- keep track of the current control in focus, but only on the main frame
    -- don't try to "remember" any of the focus changes on various dialog
    -- windows as those will disappear along with their controls
    local grandparent = win:GetGrandParent()
    local frameid = ide.frame:GetId()
    local mainwin = grandparent and grandparent:GetId() == frameid
    local parent = win:GetParent()
    while parent do
      local class = parent:GetClassInfo():GetClassName()
      if (class == 'wxFrame' or class:find('^wx.*Dialog$'))
      and parent:GetId() ~= frameid then
        mainwin = false; break
      end
      parent = parent:GetParent()
    end
    if mainwin then
      if ide.infocus and ide.infocus ~= win and ide.osname == 'Macintosh' then
        -- kill focus on the control that had the focus as wxwidgets on OSX
        -- doesn't do it: http://trac.wxwidgets.org/ticket/14142;
        -- wrap into pcall in case the window is already deleted
        local ev = wx.wxFocusEvent(wx.wxEVT_KILL_FOCUS)
        pcall(function() ide.infocus:GetEventHandler():ProcessEvent(ev) end)
      end
      ide.infocus = win
    end
  end

  event:Skip()
end)

local updateInterval = 250 -- time in ms
wx.wxUpdateUIEvent.SetUpdateInterval(updateInterval)

ide.editorApp:Connect(wx.wxEVT_ACTIVATE_APP,
  function(event)
    if not ide.exitingProgram then
      if ide.osname == 'Macintosh' and ide.infocus and event:GetActive() then
        -- restore focus to the last element that received it;
        -- wrap into pcall in case the element has disappeared
        -- while the application was out of focus
        pcall(function() if ide:IsWindowShown(ide.infocus) then ide.infocus:SetFocus() end end)
      end

      local active = event:GetActive()
      -- save auto-recovery record when making the app inactive
      if not active then saveAutoRecovery(true) end

      -- disable UI refresh when app is inactive, but only when not running
      wx.wxUpdateUIEvent.SetUpdateInterval(
        (active or ide:GetLaunchedProcess()) and updateInterval or -1)

      PackageEventHandle(active and "onAppFocusSet" or "onAppFocusLost", ide.editorApp)
    end
    event:Skip()
  end)

if ide.config.autorecoverinactivity then
  ide.timers.session = wx.wxTimer(frame)
  -- check at least 5s to be never more than 5s off
  ide.timers.session:Start(math.min(5, ide.config.autorecoverinactivity)*1000)
end

function PaneFloatToggle(window)
  local pane = uimgr:GetPane(window)
  if pane:IsFloating() then
    pane:Dock()
  else
    pane:Float()
    pane:FloatingPosition(pane.window:GetScreenPosition())
    pane:FloatingSize(pane.window:GetSize())
  end
  uimgr:Update()
end

frame:Connect(wx.wxEVT_IDLE,
  function(event)
    local debugger = ide.debugger
    if (debugger.update) then debugger.update() end
    if (debugger.scratchpad) then DebuggerRefreshScratchpad() end
    if IndicateIfNeeded() then event:RequestMore(true) end
    PackageEventHandleOnce("onIdleOnce", event)
    PackageEventHandle("onIdle", event)

    -- process onidle events if any
    while #ide.onidle > 0 do table.remove(ide.onidle)() end

    event:Skip() -- let other EVT_IDLE handlers to work on the event
  end)
