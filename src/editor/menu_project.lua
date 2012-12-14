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
local targetMenu
local interpreters = {}
local lastinterpreter
do
  local interpreternames = {}
  local lkinterpreters = {}
  for i,v in pairs(ide.interpreters) do
    interpreters[ID ("debug.interpreter."..i)] = v
    v.fname = i
    lastinterpreter = i
    table.insert(interpreternames,v.name)
    lkinterpreters[v.name] = i
  end
  assert(lastinterpreter,"no interpreters defined")
  table.sort(interpreternames)

  local targetargs = {}
  for _,v in ipairs(interpreternames) do
    local id = ID("debug.interpreter."..lkinterpreters[v])
    local inter = interpreters[id]
    table.insert(targetargs,{id,inter.name,inter.description,wx.wxITEM_CHECK})
  end
  targetMenu = wx.wxMenu(targetargs)
end

local debugTab = {
  { ID_RUN, TR("&Run")..KSC(ID_RUN), TR("Execute the current project/file") },
  { ID_RUNNOW, TR("Run as Scratchpad")..KSC(ID_RUNNOW), TR("Execute the current project/file and keep updating the code to see immediate results"), wx.wxITEM_CHECK },
  { ID_COMPILE, TR("&Compile")..KSC(ID_COMPILE), TR("Compile the current file") },
  { ID_STARTDEBUG, TR("Start &Debugging")..KSC(ID_STARTDEBUG), TR("Start debugging") },
  { ID_ATTACHDEBUG, TR("&Start Debugger Server")..KSC(ID_ATTACHDEBUG), TR("Allow external process to start debugging") },
  { },
  { ID_STOPDEBUG, TR("S&top Debugging")..KSC(ID_STOPDEBUG), TR("Stop the currently running process") },
  { ID_STEP, TR("Step &Into")..KSC(ID_STEP), TR("Step into") },
  { ID_STEPOVER, TR("Step &Over")..KSC(ID_STEPOVER), TR("Step over") },
  { ID_STEPOUT, TR("Step O&ut")..KSC(ID_STEPOUT), TR("Step out of the current function") },
  { ID_TRACE, TR("Tr&ace")..KSC(ID_TRACE), TR("Trace execution showing each executed line") },
  { ID_BREAK, TR("&Break")..KSC(ID_BREAK), TR("Break execution at the next executed line of code") },
  { },
  { ID_TOGGLEBREAKPOINT, TR("Toggle Break&point")..KSC(ID_TOGGLEBREAKPOINT), TR("Toggle breakpoint") },
  { },
  { ID_CLEAROUTPUT, TR("C&lear Output Window")..KSC(ID_CLEAROUTPUT), TR("Clear the output window before compiling or debugging"), wx.wxITEM_CHECK },
}

local targetDirMenu = wx.wxMenu{
  {ID_PROJECTDIRCHOOSE, TR("Choose ...")..KSC(ID_PROJECTDIRCHOOSE), TR("Choose a project directory")},
  {ID_PROJECTDIRFROMFILE, TR("Set From Current File")..KSC(ID_PROJECTDIRFROMFILE), TR("Set project directory from current file")},
}
local debugMenu = wx.wxMenu(debugTab)
local debugMenuRun = {
  start=TR("Start &Debugging")..KSC(ID_STARTDEBUG), continue=TR("Co&ntinue")..KSC(ID_STARTDEBUG)}
local debugMenuStop = {
  debugging=TR("S&top Debugging")..KSC(ID_STOPDEBUG), process=TR("S&top Process")..KSC(ID_STOPDEBUG)}
debugMenu:Append(ID_PROJECTDIR, TR("Project Directory"), targetDirMenu, TR("Set the project directory to be used"))
debugMenu:Append(ID_INTERPRETER, TR("Lua &Interpreter"), targetMenu, TR("Set the interpreter to be used"))
menuBar:Append(debugMenu, TR("&Project"))

-----------------------------
-- Project directory handling

function ProjectUpdateProjectDir(projdir,skiptree)
  local dir = wx.wxFileName.DirName(projdir)

  -- wxwidgets 2.9.x may report the last folder twice (depending on how the
  -- user selects the folder), which makes the selected folder incorrect.
  -- check if the last segment is repeated and drop it.
  if not wx.wxDirExists(projdir) then
    local dirs = dir:GetDirs()
    if #dirs > 1 and dirs[-1] == dirs[-2] then dir:RemoveLastDir() end
    if not wx.wxDirExists(dir:GetFullPath()) then return end
  end

  projdir = dir:GetPath(wx.wxPATH_GET_VOLUME) -- no trailing slash

  ide.config.path.projectdir = projdir ~= "" and projdir or nil
  frame:SetStatusText(projdir)
  if (not skiptree) then
    ide.filetree:updateProjectDir(projdir)
  end
end
ProjectUpdateProjectDir(ide.config.path.projectdir)

local function projChoose(event)
  local editor = GetEditor()
  local fn = wx.wxFileName(
    editor and openDocuments[editor:GetId()].filePath or "")
  fn:Normalize() -- want absolute path for dialog

  local projectdir = ide.config.path.projectdir

  local filePicker = wx.wxDirDialog(frame, TR("Choose a project directory"),
    projectdir ~= "" and projectdir or wx.wxGetCwd(), wx.wxDIRP_DIR_MUST_EXIST)
  if filePicker:ShowModal(true) == wx.wxID_OK then
    ProjectUpdateProjectDir(filePicker:GetPath())
  end
  return true
end

frame:Connect(ID_PROJECTDIRCHOOSE, wx.wxEVT_COMMAND_MENU_SELECTED, projChoose)
frame:Connect(ID_PROJECTDIRCHOOSE, wx.wxEVT_COMMAND_BUTTON_CLICKED, projChoose)

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

------------------------------------
-- Interpreter Selection and Running

local function selectInterpreter(id)
  for id in pairs(interpreters) do
    menuBar:Check(id, false)
    menuBar:Enable(id, true)
  end
  menuBar:Check(id, true)
  menuBar:Enable(id, false)

  ide.interpreter = interpreters[id]

  if DebuggerShutdown then DebuggerShutdown() end
  ide.frame.statusBar:SetStatusText(ide.interpreter.name or "", 5)
  ReloadLuaAPI()
end

function ProjectSetInterpreter(name)
  local id = IDget("debug.interpreter."..name)
  if (not interpreters[id]) then return end
  selectInterpreter(id)
end

local function evSelectInterpreter(event)
  selectInterpreter(event:GetId())
end

for id in pairs(interpreters) do
  frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectInterpreter)
end

do
  local defaultid = (
    IDget("debug.interpreter."..ide.config.interpreter) or
    ID("debug.interpreter."..lastinterpreter)
  )
  ide.interpreter = interpreters[defaultid]
  menuBar:Check(defaultid, true)
end

local function getNameToRun(skipcheck)
  local editor = GetEditor()

  -- test compile it before we run it, if successful then ask to save
  -- only compile if lua api
  if editor.spec.apitype and
    editor.spec.apitype == "lua" and
    (not skipcheck) and
    (not ide.interpreter.skipcompile) and
    (not CompileProgram(editor, true)) then
    return
  end

  local id = editor:GetId()
  if not openDocuments[id].filePath then SetDocumentModified(id, true) end
  if not SaveIfModified(editor) then return end

  return wx.wxFileName(openDocuments[id].filePath)
end

function ActivateOutput()
  if not ide.config.activateoutput then return end
  -- show output/errorlog pane
  uimgr:GetPane("bottomnotebook"):Show(true)
  uimgr:Update()
  -- activate output/errorlog window
  local index = bottomnotebook:GetPageIndex(bottomnotebook.errorlog)
  if bottomnotebook:GetSelection() ~= index then
    bottomnotebook:SetSelection(index)
  end
end

local function runInterpreter(wfilename, withdebugger)
  if ide.frame.menuBar:IsChecked(ID_CLEAROUTPUT) then ClearOutput() end
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
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and (editor ~= nil)
      and (not debugger.running) and (not debugger.scratchpad))
  end)

frame:Connect(ID_COMPILE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = GetEditor()
    ActivateOutput()
    CompileProgram(editor)
  end)
frame:Connect(ID_COMPILE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)

frame:Connect(ID_RUN, wx.wxEVT_COMMAND_MENU_SELECTED, function () ProjectRun() end)
frame:Connect(ID_RUN, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)

frame:Connect(ID_RUNNOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if event:IsChecked() then
      if not DebuggerScratchpadOn(GetEditor()) then
        menuBar:Check(ID_RUNNOW, false) -- disable if couldn't start scratchpad
      end
    else DebuggerScratchpadOff() end
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
  end)

frame:Connect(ID_ATTACHDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    if (ide.interpreter.fattachdebug) then ide.interpreter:fattachdebug() end
  end)
frame:Connect(ID_ATTACHDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.fattachdebug)
      and (not debugger.listening) and (debugger.server == nil)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STARTDEBUG, wx.wxEVT_COMMAND_MENU_SELECTED, function () ProjectDebug() end)
frame:Connect(ID_STARTDEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and
      ((debugger.server == nil and debugger.pid == nil) or
       (debugger.server ~= nil and not debugger.running)) and
      (editor ~= nil) and (not debugger.scratchpad or debugger.scratchpad.paused))
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
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil or debugger.pid ~= nil) and (editor ~= nil))
    local label = (debugger.server == nil and debugger.pid ~= nil)
      and debugMenuStop.process or debugMenuStop.debugging
    if debugMenu:GetLabel(ID_STOPDEBUG) ~= label then
      debugMenu:SetLabel(ID_STOPDEBUG, label)
    end
  end)

frame:Connect(ID_STEP, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    debugger.step()
  end)
frame:Connect(ID_STEP, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEPOVER, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    debugger.over()
  end)
frame:Connect(ID_STEPOVER, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEPOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    debugger.out()
  end)
frame:Connect(ID_STEPOUT, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_TRACE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    debugger.trace()
  end)
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
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (editor ~= nil)
      and (debugger.running
           or (debugger.scratchpad and not debugger.scratchpad.paused)))
  end)

frame:Connect(wx.wxEVT_IDLE,
  function(event)
    if (debugger.update) then debugger.update() end
    if (debugger.scratchpad) then DebuggerRefreshScratchpad() end
    event:Skip() -- let other EVT_IDLE handlers to work on the event
  end)
