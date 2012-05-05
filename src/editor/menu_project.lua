-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide

-- Create the Debug menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar

local openDocuments = ide.openDocuments
local debugger = ide.debugger
local filetree = ide.filetree
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
  for i,v in ipairs(interpreternames) do
    local id = ID ("debug.interpreter."..lkinterpreters[v])
    local inter = interpreters[id]
    table.insert(targetargs,{id,inter.name,inter.description,wx.wxITEM_CHECK})
  end
  targetMenu = wx.wxMenu(targetargs)
end

local debugTab = {
  { ID_RUN, "&Run\tF6", "Execute the current project/file" },
  { ID_RUNNOW, "Run as Scratchpad", "Execute the current project/file and keep updating the code to see immediate results", wx.wxITEM_CHECK },
  { ID_COMPILE, "&Compile\tF7", "Test compile the Lua file" },
  { ID_START_DEBUG, "Start &Debugging\tF5", "Start a debugging session" },
  { ID_ATTACH_DEBUG, "&Start Debugger Server\tShift-F6", "Allow a client to start a debugging session" },
  { },
  { ID_STOP_DEBUG, "S&top Debugging\tShift-F12", "Stop the currently running process" },
  { ID_STEP, "St&ep\tF11", "Step into the next line" },
  { ID_STEP_OVER, "Step &Over\tF10", "Step over the next line" },
  { ID_STEP_OUT, "Step O&ut\tShift-F10", "Step out of the current function" },
  { ID_TRACE, "Tr&ace", "Trace execution showing each executed line" },
  { ID_BREAK, "&Break", "Stop execution of the program at the next executed line of code" },
  { },
  { ID_TOGGLEBREAKPOINT, "Toggle &Breakpoint\tF9", "Toggle Breakpoint" },
  --{ ID "view.debug.callstack", "V&iew Call Stack", "View the call stack" },
  { },
  { ID_CLEAROUTPUT, "C&lear Output Window", "Clear the output window before compiling or debugging", wx.wxITEM_CHECK },
}

local debugMenu = wx.wxMenu(debugTab)
local debugMenuRun = {start="Start &Debugging\tF5", continue="Co&ntinue\tF5"}
local debugMenuStop = {debugging="S&top Debugging\tShift-F12", process="S&top Process\tShift-F12"}

local targetDirMenu = wx.wxMenu{
  {ID "debug.projectdir.choose","Choose ..."},
  {ID "debug.projectdir.fromfile","From current filepath"},
  {},
  {ID "debug.projectdir.currentdir",""}
}

debugMenu:Append(0,"Lua &interpreter",targetMenu,"Set the interpreter to be used")
debugMenu:Append(0,"Project directory",targetDirMenu,"Set the project directory to be used")
menuBar:Append(debugMenu, "&Project")

-----------------------------
-- Project directory handling

function ProjectUpdateProjectDir(projdir,skiptree)
  ide.config.path.projectdir = projdir
  menuBar:SetLabel(ID "debug.projectdir.currentdir",projdir)
  frame:SetStatusText(projdir)
  if (not skiptree) then
    ide.filetree:updateProjectDir(projdir)
  end
end
ProjectUpdateProjectDir(ide.config.path.projectdir)

local function projChoose(event)
  local editor = GetEditor()
  local saved = false
  local fn = wx.wxFileName(
    editor and openDocuments[editor:GetId()].filePath or "")
  fn:Normalize() -- want absolute path for dialog

  local projectdir = ide.config.path.projectdir

  local filePicker = wx.wxDirDialog(frame, "Chose a project directory",
    projectdir~="" and projectdir or wx.wxGetCwd(),wx.wxFLP_USE_TEXTCTRL)
  local res = filePicker:ShowModal(true)
  if res == wx.wxID_OK then
    ProjectUpdateProjectDir(filePicker:GetPath())
  end
  return true
end

frame:Connect(ID "debug.projectdir.choose", wx.wxEVT_COMMAND_MENU_SELECTED,
  projChoose)
frame:Connect(ID "debug.projectdir.choose", wx.wxEVT_COMMAND_BUTTON_CLICKED,
  projChoose)

local function projFromFile(event)
  local editor = GetEditor()
  if not editor then return end
  local id = editor:GetId()
  local filepath = openDocuments[id].filePath
  if not filepath then return end
  local fn = wx.wxFileName(filepath)
  fn:Normalize() -- want absolute path for dialog

  if ide.interpreter then ProjectUpdateProjectDir(ide.interpreter:fprojdir(fn)) end
end
frame:Connect(ID "debug.projectdir.fromfile", wx.wxEVT_COMMAND_MENU_SELECTED,
  projFromFile)

------------------------------------
-- Interpreter Selection and Running

local function selectInterpreter(id)
  for i,inter in pairs(interpreters) do
    menuBar:Check(i, false)
  end
  menuBar:Check(id, true)
  ide.interpreter = interpreters[id]
  ReloadLuaAPI()
end

function ProjectSetInterpreter(name)
  local id = IDget("debug.interpreter."..name)
  if (not interpreters[id]) then return end
  selectInterpreter(id)
end

local function evSelectInterpreter (event)
  local chose = event:GetId()
  selectInterpreter(chose)
end

for id,inter in pairs(interpreters) do
  frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectInterpreter)
end

do
  local defaultid = (
    IDget("debug.interpreter."..ide.config.interpreter) or
    ID ("debug.interpreter."..lastinterpreter)
  )
  ide.interpreter = interpreters[defaultid]
  menuBar:Check(defaultid, true)
end

local function getNameToRun(skipcheck)
  local editor = GetEditor()

  -- test compile it before we run it, if successful then ask to save
  -- only compile if lua api
  if (editor.spec.apitype and
    editor.spec.apitype == "lua" and
    (not CompileProgram(editor, true) and not skipcheck)) then
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
  ActivateOutput()

  ClearAllCurrentLineMarkers()
  if not wfilename then return end
  debugger.pid = ide.interpreter:frun(wfilename, withdebugger)
end

function ProjectRun(skipcheck)
  runInterpreter(getNameToRun(skipcheck))
end

local debuggers = {
  debug = "require('mobdebug').loop('%s',%d)",
  scratchpad = "require('mobdebug').scratchpad('%s',%d)"
}

function ProjectDebug(skipcheck, debtype)
  if (debugger.server ~= nil) then
    if (not debugger.running) then
      ClearAllCurrentLineMarkers()
      debugger.run()
    end
  else
    local debcall = (debuggers[debtype or "debug"]):
      format(wx.wxGetHostName(), ide.debugger.portnumber)
    runInterpreter(getNameToRun(skipcheck), debcall)
  end
end

-----------------------
-- Actions

frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local line = editor:LineFromPosition(editor:GetCurrentPos())
    DebuggerToggleBreakpoint(editor, line)
  end)
frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and (editor ~= nil))
  end)

frame:Connect(ID_COMPILE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    ActivateOutput()
    CompileProgram(editor)
  end)
frame:Connect(ID_COMPILE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)

frame:Connect(ID_RUN, wx.wxEVT_COMMAND_MENU_SELECTED, ProjectRun)
frame:Connect(ID_RUN, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)

frame:Connect(ID_RUNNOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if event:IsChecked() then DebuggerScratchpadOn(GetEditor())
    else DebuggerScratchpadOff() end
  end)
frame:Connect(ID_RUNNOW, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil) and (editor ~= nil) and (debugger.pid == nil)
                 or debugger.scratchpad ~= nil)
  end)

frame:Connect(ID_ATTACH_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    if (ide.interpreter.fattachdebug) then ide.interpreter:fattachdebug() end
  end)
frame:Connect(ID_ATTACH_DEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.fattachdebug)
      and (not debugger.listening) and (debugger.server == nil)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_START_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED, ProjectDebug)
frame:Connect(ID_START_DEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((ide.interpreter) and (ide.interpreter.hasdebugger) and
      ((debugger.server == nil and debugger.pid == nil) or
       (debugger.server ~= nil and not debugger.running)) and
      (editor ~= nil) and (not debugger.scratchpad))
    local label = (debugger.server ~= nil)
      and debugMenuRun.continue or debugMenuRun.start
    if debugMenu:GetLabel(ID_START_DEBUG) ~= label then
      debugMenu:SetLabel(ID_START_DEBUG, label)
    end
  end)

frame:Connect(ID_STOP_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    if debugger.server then debugger.terminate() end
    if debugger.pid then DebuggerKillClient() end
  end)
frame:Connect(ID_STOP_DEBUG, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil or debugger.pid ~= nil) and (editor ~= nil))
    local label = (debugger.server == nil and debugger.pid ~= nil)
      and debugMenuStop.process or debugMenuStop.debugging
    if debugMenu:GetLabel(ID_STOP_DEBUG) ~= label then
      debugMenu:SetLabel(ID_STOP_DEBUG, label)
    end
  end)

frame:Connect(ID_STEP, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    debugger.step()
  end)
frame:Connect(ID_STEP, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEP_OVER, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    debugger.over()
  end)
frame:Connect(ID_STEP_OVER, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_STEP_OUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    debugger.out()
  end)
frame:Connect(ID_STEP_OUT, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_TRACE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClearAllCurrentLineMarkers()
    debugger.trace()
  end)
frame:Connect(ID_TRACE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (not debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

frame:Connect(ID_BREAK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if debugger.server then
      debugger.breaknow()
    end
  end)
frame:Connect(ID_BREAK, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server ~= nil) and (debugger.running)
      and (editor ~= nil) and (not debugger.scratchpad))
  end)

--[[
frame:Connect(ID "view.debug.callstack", wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if debugger.server then
      DebuggerCreateStackWindow()
    end
  end)
frame:Connect(ID "view.debug.callstack", wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable((debugger.server ~= nil) and (not debugger.running))
  end)
]]

frame:Connect(wx.wxEVT_IDLE,
  function(event)
    if (debugger.update) then debugger.update() end
    if (debugger.scratchpad) then DebuggerRefreshScratchpad() end
    event:Skip() -- let other EVT_IDLE handlers to work on the event
  end)
