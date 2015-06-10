-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local openDocuments = ide.openDocuments
local debugger = ide.debugger
local bottomnotebook = frame.bottomnotebook
local uimgr = frame.uimgr

------------------------
-- Interpreters and Menu

local debugTab = {
  { ID_RUN, TR("&Run")..KSC(ID_RUN), TR("Execute the current project/file") },
  { ID_RUNNOW, TR("Run As Scratchpad")..KSC(ID_RUNNOW), TR("Execute the current project/file and keep updating the code to see immediate results"), wx.wxITEM_CHECK },
  { ID_COMPILE, TR("&Compile")..KSC(ID_COMPILE), TR("Compile the current file") },
  { ID_STARTDEBUG, TR("Start &Debugging")..KSC(ID_STARTDEBUG), TR("Start or continue debugging") },
  { ID_ATTACHDEBUG, TR("&Start Debugger Server")..KSC(ID_ATTACHDEBUG), TR("Allow external process to start debugging"), wx.wxITEM_CHECK },
  { },
  { ID_STOPDEBUG, TR("S&top Debugging")..KSC(ID_STOPDEBUG), TR("Stop the currently running process") },
  { ID_DETACHDEBUG, TR("Detach &Process")..KSC(ID_DETACHDEBUG), TR("Stop debugging and continue running the process") },
  { ID_STEP, TR("Step &Into")..KSC(ID_STEP), TR("Step into") },
  { ID_STEPOVER, TR("Step &Over")..KSC(ID_STEPOVER), TR("Step over") },
  { ID_STEPOUT, TR("Step O&ut")..KSC(ID_STEPOUT), TR("Step out of the current function") },
  { ID_RUNTO, TR("Run To Cursor")..KSC(ID_RUNTO), TR("Run to cursor") },
  { ID_TRACE, TR("Tr&ace")..KSC(ID_TRACE), TR("Trace execution showing each executed line") },
  { ID_BREAK, TR("&Break")..KSC(ID_BREAK), TR("Break execution at the next executed line of code") },
  { },
  { ID_TOGGLEBREAKPOINT, TR("Toggle Break&point")..KSC(ID_TOGGLEBREAKPOINT), TR("Toggle breakpoint") },
  { },
  { ID_CLEAROUTPUT, TR("C&lear Output Window")..KSC(ID_CLEAROUTPUT), TR("Clear the output window before compiling or debugging"), wx.wxITEM_CHECK },
  { ID_COMMANDLINEPARAMETERS, TR("Command Line Parameters...")..KSC(ID_COMMANDLINEPARAMETERS), TR("Provide command line parameters") },
}

local targetDirMenu = wx.wxMenu{
  {ID_PROJECTDIRCHOOSE, TR("Choose...")..KSC(ID_PROJECTDIRCHOOSE), TR("Choose a project directory")},
  {ID_PROJECTDIRFROMFILE, TR("Set From Current File")..KSC(ID_PROJECTDIRFROMFILE), TR("Set project directory from current file")},
}
local targetMenu = wx.wxMenu({})
local debugMenu = wx.wxMenu(debugTab)
local debugMenuRun = {
  start=TR("Start &Debugging")..KSC(ID_STARTDEBUG), continue=TR("Co&ntinue")..KSC(ID_STARTDEBUG)}
local debugMenuStop = {
  debugging=TR("S&top Debugging")..KSC(ID_STOPDEBUG), process=TR("S&top Process")..KSC(ID_STOPDEBUG)}
debugMenu:Append(ID_PROJECTDIR, TR("Project Directory"), targetDirMenu, TR("Set the project directory to be used"))
debugMenu:Append(ID_INTERPRETER, TR("Lua &Interpreter"), targetMenu, TR("Set the interpreter to be used"))
menuBar:Append(debugMenu, TR("&Project"))

local interpreters
local function selectInterpreter(id)
  for id in pairs(interpreters) do
    menuBar:Check(id, false)
    menuBar:Enable(id, true)
  end
  menuBar:Check(id, true)
  menuBar:Enable(id, false)

  local changed = ide.interpreter ~= interpreters[id]
  if ide.interpreter and changed then
    PackageEventHandle("onInterpreterClose", ide.interpreter)
  end
  if interpreters[id] and changed then
    PackageEventHandle("onInterpreterLoad", interpreters[id])
  end

  ide.interpreter = interpreters[id]

  DebuggerShutdown()

  ide.frame.statusBar:SetStatusText(ide.interpreter.name or "", 5)
  if changed then ReloadLuaAPI() end
end

function ProjectSetInterpreter(name)
  local id = IDget("debug.interpreter."..name)
  if id and interpreters[id] then
    selectInterpreter(id)
  else
    DisplayOutputLn(("Can't find interpreter '%s'; using the default interpreter instead.")
      :format(name))
  end
end

local function evSelectInterpreter(event)
  selectInterpreter(event:GetId())
end

function ProjectUpdateInterpreters()
  assert(ide.interpreters, "no interpreters defined")

  -- delete all existing items (if any)
  local items = targetMenu:GetMenuItemCount()
  for i = items, 1, -1 do
    targetMenu:Delete(targetMenu:FindItemByPosition(i-1))
  end

  local names = {}
  for file in pairs(ide.interpreters) do table.insert(names, file) end
  table.sort(names)

  interpreters = {}
  for _, file in ipairs(names) do
    local inter = ide.interpreters[file]
    local id = ID("debug.interpreter."..file)
    inter.fname = file
    interpreters[id] = inter
    targetMenu:Append(
      wx.wxMenuItem(targetMenu, id, inter.name, inter.description, wx.wxITEM_CHECK))
    frame:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, evSelectInterpreter)
  end

  local id = (
    -- interpreter is set and is (still) on the list of known interpreters
    IDget("debug.interpreter."
      ..(ide.interpreter and ide.interpreters[ide.interpreter.fname]
         and ide.interpreter.fname or ide.config.interpreter)) or
    -- otherwise use default interpreter
    ID("debug.interpreter."..ide.config.default.interpreter)
  )
  selectInterpreter(id)
end

-----------------------------
-- Project directory handling

function ProjectUpdateProjectDir(projdir,skiptree)
  -- strip trailing spaces as this may create issues with "path/ " on Windows
  projdir = projdir:gsub("%s+$","")
  local dir = wx.wxFileName.DirName(FixDir(projdir))
  dir:Normalize() -- turn into absolute path if needed
  if not wx.wxDirExists(dir:GetFullPath()) then return end

  projdir = dir:GetPath(wx.wxPATH_GET_VOLUME) -- no trailing slash

  ide.config.path.projectdir = projdir ~= "" and projdir or nil
  frame:SetStatusText(projdir)
  frame:SetTitle(ExpandPlaceholders(ide.config.format.apptitle))
  if (not skiptree) then ide.filetree:updateProjectDir(projdir) end
  return true
end

local function projChoose(event)
  local editor = GetEditor()
  local fn = wx.wxFileName(
    editor and openDocuments[editor:GetId()].filePath or "")
  fn:Normalize() -- want absolute path for dialog

  local projectdir = ide:GetProject()
  local filePicker = wx.wxDirDialog(frame, TR("Choose a project directory"),
    projectdir ~= "" and projectdir or wx.wxGetCwd(), wx.wxDIRP_DIR_MUST_EXIST)
  if filePicker:ShowModal(true) == wx.wxID_OK then
    return ProjectUpdateProjectDir(filePicker:GetPath())
  end
  return false
end

frame:Connect(ID_PROJECTDIRCHOOSE, wx.wxEVT_COMMAND_MENU_SELECTED, projChoose)

local function projFromFile(event)
  local editor = GetEditor()
  if not editor then return end
  local id = editor:GetId()
  local filepath = openDocuments[id].filePath
  if not filepath then return end
  local fn = wx.wxFileName(filepath)
  fn:Normalize() -- want absolute path for dialog

  if ide.interpreter then
    ProjectUpdateProjectDir(ide.interpreter:fprojdir(fn)) end
end
frame:Connect(ID_PROJECTDIRFROMFILE, wx.wxEVT_COMMAND_MENU_SELECTED, projFromFile)
frame:Connect(ID_PROJECTDIRFROMFILE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable(editor ~= nil and ide:GetDocument(editor):GetFilePath() ~= nil)
  end)

----------------------
-- Interpreter Running

local function getNameToRun(skipcheck)
  local editor = GetEditor()

  -- test compile it before we run it, if successful then ask to save
  -- only compile if lua api
  if editor.spec.apitype and
    editor.spec.apitype == "lua" and
    (not skipcheck) and
    (not ide.interpreter.skipcompile) and
    (not CompileProgram(editor, { reportstats = false })) then
    return
  end

  local doc = ide:GetDocument(editor)
  if not doc:GetFilePath() then doc:SetModified(true) end
  if not SaveIfModified(editor) then return end
  if ide.config.editor.saveallonrun then SaveAll(true) end

  return wx.wxFileName(ide:GetProjectStartFile() or doc:GetFilePath())
end

function ActivateOutput()
  if not ide.config.activateoutput then return end
  -- show output/errorlog pane
  if not uimgr:GetPane("bottomnotebook"):IsShown() then
    uimgr:GetPane("bottomnotebook"):Show(true)
    uimgr:Update()
  end
  -- activate output/errorlog window
  local index = bottomnotebook:GetPageIndex(bottomnotebook.errorlog)
  if bottomnotebook:GetSelection() ~= index then
    bottomnotebook:SetSelection(index)
  end
end

local function runInterpreter(wfilename, withdebugger)
  ClearOutput()
  ActivateOutput()

  ClearAllCurrentLineMarkers()
  if not wfilename then return end
  debugger.pid = ide.interpreter:frun(wfilename, withdebugger)
  return debugger.pid
end

function ProjectRun(skipcheck)
  local fname = getNameToRun(skipcheck)
  if not fname then return end
  return runInterpreter(fname)
end

local debuggers = {
  debug = "require('mobdebug').loop('%s',%d)",
  scratchpad = "require('mobdebug').scratchpad('%s',%d)"
}

function ProjectDebug(skipcheck, debtype)
  if (debugger.server ~= nil) then
    if (debugger.scratchpad and debugger.scratchpad.paused) then
      debugger.scratchpad.paused = nil
      debugger.scratchpad.updated = true
      ShellSupportRemote(nil) -- disable remote while Scratchpad running
    elseif (not debugger.running) then
      debugger.run()
    end
  else
    local debcall = (debuggers[debtype or "debug"]):
      format(ide.debugger.hostname, ide.debugger.portnumber)
    local fname = getNameToRun(skipcheck)
    if not fname then return end
    return runInterpreter(fname, debcall) -- this may be pid or nil
  end
  return true
end

-----------------------
-- Actions

frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = GetEditor()
    local line = editor:LineFromPosition(editor:GetCurrentPos())
    DebuggerToggleBreakpoint(editor, line)
  end)
frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditorWithFocus(GetEditor())
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and (editor ~= nil)
      and (not debugger.scratchpad))
  end)

frame:Connect(ID_COMPILE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    ActivateOutput()
    CompileProgram(GetEditor(), {
        keepoutput = ide:GetLaunchedProcess() ~= nil or ide:GetDebugger():IsConnected()
    })
  end)
frame:Connect(ID_COMPILE, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)

frame:Connect(ID_RUN, wx.wxEVT_COMMAND_MENU_SELECTED, function () ProjectRun() end)
frame:Connect(ID_RUN, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)

frame:Connect(ID_RUNNOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if debugger.scratchpad then
      DebuggerScratchpadOff()
    else
      DebuggerScratchpadOn(GetEditor())
    end
  end)
frame:Connect(ID_RUNNOW, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    -- allow scratchpad if there is no server or (there is a server and it is
    -- allowed to turn it into a scratchpad) and we are not debugging anything
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and
                 (ide.interpreter.scratchextloop ~= nil) and -- nil == no scratchpad support
                 (editor ~= nil) and ((debugger.server == nil or debugger.scratchable)
                 and debugger.pid == nil or debugger.scratchpad ~= nil))
    local isscratchpad = debugger.scratchpad ~= nil
    menuBar:Check(ID_RUNNOW, isscratchpad)
    local tool = ide:GetToolBar():FindTool(ID_RUNNOW)
    if tool and tool:IsSticky() ~= isscratchpad then
      tool:SetSticky(isscratchpad)
      ide:GetToolBar():Refresh()
    end
  end)

frame:Connect(ID_ATTACHDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if event:IsChecked() then
      if (ide.interpreter.fattachdebug) then ide.interpreter:fattachdebug() end
    else
      debugger.listen(false) -- stop listening
    end
  end)
frame:Connect(ID_ATTACHDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(ide.interpreter and ide.interpreter.fattachdebug and true or false)
    ide.frame.menuBar:Check(event:GetId(), debugger.listening and true or false)
  end)

frame:Connect(ID_STARTDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED, function () ProjectDebug() end)
frame:Connect(ID_STARTDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and
      ((debugger.server == nil and debugger.pid == nil and editor ~= nil) or
       (debugger.server ~= nil and not debugger.running)) and
      (not debugger.scratchpad or debugger.scratchpad.paused))
    local label = (debugger.server ~= nil)
      and debugMenuRun.continue or debugMenuRun.start
    if debugMenu:GetLabel(ID_STARTDEBUG) ~= label then
      debugMenu:SetLabel(ID_STARTDEBUG, label)
    end
  end)

frame:Connect(ID_STOPDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () DebuggerShutdown() end)
frame:Connect(ID_STOPDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(debugger.server ~= nil or debugger.pid ~= nil)
    local label = (debugger.server == nil and debugger.pid ~= nil)
      and debugMenuStop.process or debugMenuStop.debugging
    if debugMenu:GetLabel(ID_STOPDEBUG) ~= label then
      debugMenu:SetLabel(ID_STOPDEBUG, label)
    end
  end)

frame:Connect(ID_DETACHDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () debugger.detach() end)
frame:Connect(ID_DETACHDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable((debugger.server ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_RUNTO, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = GetEditor()
    debugger.runto(editor, editor:GetCurrentLine())
  end)
frame:Connect(ID_RUNTO, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEP, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () debugger.step() end)
frame:Connect(ID_STEP, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEPOVER, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () debugger.over() end)
frame:Connect(ID_STEPOVER, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEPOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () debugger.out() end)
frame:Connect(ID_STEPOUT, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_TRACE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () debugger.trace() end)
frame:Connect(ID_TRACE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_BREAK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    if debugger.server then
      debugger.breaknow()
      if debugger.scratchpad then
        debugger.scratchpad.paused = true
        ShellSupportRemote(debugger.shell)
      end
    end
  end)
frame:Connect(ID_BREAK, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(debugger.server ~= nil
      and (debugger.running
           or (debugger.scratchpad and not debugger.scratchpad.paused)))
  end)

frame:Connect(ID_COMMANDLINEPARAMETERS, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local params = wx.wxGetTextFromUser(TR("Enter command line parameters (use Cancel to clear)"),
      TR("Command line parameters"), ide.config.arg.any or "")
    ide.config.arg.any = params and #params > 0 and params or nil
  end)
frame:Connect(ID_COMMANDLINEPARAMETERS, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(ide.interpreter and ide.interpreter.takeparameters and true or false)
  end)
