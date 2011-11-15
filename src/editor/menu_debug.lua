-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- Create the Debug menu and attach the callback functions

local frame    = ide.frame
local menuBar  = frame.menuBar
local vsplitter= frame.vsplitter
local sidenotebook = vsplitter.sidenotebook
local splitter = vsplitter.splitter
local errorlog = splitter.bottomnotebook.errorlog
local notebook = splitter.notebook

local openDocuments = ide.openDocuments
local debugger      = ide.debugger
local filetree      = ide.filetree

--------------
-- Interpreters
local interpreters = {}
local lastinterpreter
for i,v in pairs(ide.interpreters) do
	interpreters[ID ("debug.interpreter."..i)] = v
	v.fname = i
	lastinterpreter = v.name
end
assert(lastinterpreter,"no interpreters defined")

local debugMenu = wx.wxMenu{
		{ ID_TOGGLEBREAKPOINT, "Toggle &Breakpoint\tF9", "Toggle Breakpoint" },
		{ },
		{ ID_COMPILE,          "&Compile\tF7",           "Test compile the Lua file" },
		{ ID_RUN,              "&Run\tF6",               "Execute the current project/file" },
		{ ID_ATTACH_DEBUG,     "&Attach\tShift-F6",      "Allow a client to start a debugging session" },
		--{ ID_START_DEBUG,      "&Start Debugging\tShift-F5", "Start a debugging session" },
		--{ ID_USECONSOLE,       "Console",               "Use console when running",  wx.wxITEM_CHECK },
		{ },
		{ ID_STOP_DEBUG,       "S&top Debugging\tShift-F12", "Stop and end the debugging session" },
		{ ID_STEP,             "St&ep\tF11",             "Step into the next line" },
		{ ID_STEP_OVER,        "Step &Over\tShift-F11",  "Step over the next line" },
		{ ID_STEP_OUT,         "Step O&ut\tF8",          "Step out of the current function" },
		{ ID_CONTINUE,         "Co&ntinue\tF5",          "Run the program at full speed" },
		{ ID_BREAK,            "&Break\tF12",            "Stop execution of the program at the next executed line of code" },
		{ },
		{ ID_CLEAROUTPUT,      "C&lear Output Window",    "Clear the output window before compiling or debugging", wx.wxITEM_CHECK },
		--{ }, { ID_DEBUGGER_PORT,    "Set debugger socket port...", "Chose what port to use for debugger sockets." },
		{ },
		}

local targetargs = {}
for id,inter in pairs(interpreters) do
	table.insert(targetargs,{id,inter.name,inter.description,wx.wxITEM_CHECK})
end
local target = wx.wxMenu{
		unpack(targetargs)
	}
	
local targetworkdir = wx.wxMenu{
		{ID "debug.projectdir.choose","Choose ..."},
		{ID "debug.projectdir.fromfile","From current filepath"},
		{},
		{ID "debug.projectdir.currentdir",""}
	}

debugMenu:Append(0,"Lua &interpreter",target,"Set the interpreter to be used")
debugMenu:Append(0,"Project directory",targetworkdir,"Set the project directory to be used")
menuBar:Append(debugMenu, "&Project")
--menuBar:Check(ID_USECONSOLE, true)

function UpdateProjectDir(projdir,skiptree)
	ide.config.path.projectdir = projdir
	menuBar:SetLabel(ID "debug.projectdir.currentdir",projdir)
	frame:SetStatusText(projdir)
	if (not skiptree) then
		ide.filetree:UpdateProjectDir(projdir)
	end
end
UpdateProjectDir(ide.config.path.projectdir)

-- interpreter setup
local curinterpreterid = 	IDget("debug.interpreter."..ide.config.interpreter)  or 
							ID ("debug.interpreter."..lastinterpreter)
ide.config.interpreterClass = interpreters[curinterpreterid]

	menuBar:Check(curinterpreterid, true)
	
	local function selectInterpreter (id)
		for i,inter in pairs(interpreters) do
			menuBar:Check(i, false)
		end
		menuBar:Check(id, true)
		curinterpreterid = id
		ide.config.interpreter = interpreters[id].fname
		ide.config.interpreterClass = interpreters[id]
		ReloadLuaAPI()
	end
	
	local function evSelectInterpreter (event)
		local chose = event:GetId()
		selectInterpreter(chose)
	end
	
	for id,inter in pairs(interpreters) do
		frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectInterpreter)
	end
	
function SetInterpreter(name)
	local id = IDget("debug.interpreter."..name)
	if (not interpreters[id]) then return end
	selectInterpreter(id)
end

local function projChoose(event)
	local editor = GetEditor()
	local id       = editor:GetId()
	local saved    = false
	local fn       = wx.wxFileName(openDocuments[id].filePath or "")
	fn:Normalize() -- want absolute path for dialog
	
	local projectdir = ide.config.path.projectdir
	
	--filePicker:Show(true)
	--local diag = wx.wxDialog()
	--diag:ShowModal(true)
	local filePicker = wx.wxDirDialog(frame, "Chose a project directory", 
			projectdir~="" and projectdir or wx.wxGetCwd(),wx.wxFLP_USE_TEXTCTRL)
	local res = filePicker:ShowModal(true)
	--for i,v in pairs(wx) do if v == res then print(i) end end
	--print(res)
	if res == wx.wxID_OK then
		UpdateProjectDir(filePicker:GetPath())
	end
	--filePicker:Destroy()
	return true
end
	
frame:Connect(ID "debug.projectdir.choose", wx.wxEVT_COMMAND_MENU_SELECTED,
	projChoose)
frame:Connect(ID "debug.projectdir.choose", wx.wxEVT_COMMAND_BUTTON_CLICKED,
	projChoose)
		
local function projFromFile(event)
	local editor = GetEditor()
	if not editor then return end
	local id       = editor:GetId()
	local filepath = openDocuments[id].filePath	
	if not filepath then return end
	local fn       = wx.wxFileName(filepath)
	fn:Normalize() -- want absolute path for dialog
	
	UpdateProjectDir(interpreters[curinterpreterid]:fprojdir(fn))
end
frame:Connect(ID "debug.projectdir.fromfile", wx.wxEVT_COMMAND_MENU_SELECTED,
	projFromFile)
	
frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			local editor = GetEditor()
			local line = editor:LineFromPosition(editor:GetCurrentPos())
			ToggleDebugMarker(editor, line)
		end)
frame:Connect(ID_TOGGLEBREAKPOINT, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)


frame:Connect(ID_COMPILE, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			local editor = GetEditor()
			CompileProgram(editor)
		end)
frame:Connect(ID_COMPILE, wx.wxEVT_UPDATE_UI, OnUpdateUIEditMenu)


frame:Connect(ID_RUN, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			-- SaveAll()

			local editor = GetEditor()
			
			-- test compile it before we run it, if successful then ask to save
			-- only compile if lua api
			if (editor.spec.apitype and editor.spec.apitype == "lua" and 
								not CompileProgram(editor)) then
				return
			end
			if not SaveIfModified(editor) then
				return
			end
			
			local id = editor:GetId();
			local wfilename = wx.wxFileName(openDocuments[id].filePath)
			local interpreter = interpreters[curinterpreterid]
			interpreter:frun(wfilename)
		end)
frame:Connect(ID_RUN, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server == nil) and (editor ~= nil))
		end)

frame:Connect(ID_ATTACH_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
		        debugger.connect()
			DisplayOutput("Client connected to "..wx.wxGetHostName()..":"..debugger.portnumber.."\n")
		end)
frame:Connect(ID_ATTACH_DEBUG, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server == nil) and (editor ~= nil))
		end)

frame:Connect(ID_START_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			local editor = GetEditor()
			-- test compile it before we run it
			if not CompileProgram(editor) then
				return
			end

			debugger.pid = 0
			debugger.server = CreateDebuggerServer()
			if debugger.server then
				debugger.pid = debugger.server:StartClient()
			end

			if debugger.server and (debugger.pid > 0) then
				SetAllEditorsReadOnly(true)
				DisplayOutput("Waiting for client connection, process "..tostring(debugger.pid)..".\n")
			else
				DisplayOutput("Unable to start debuggee process.\n")
				if debugger.server then
					DestroyDebuggerServer()
				end
			end

			NextDebuggerPort()
		end)
frame:Connect(ID_START_DEBUG, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server == nil) and (editor ~= nil))
		end)

frame:Connect(ID_STOP_DEBUG, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			ClearAllCurrentLineMarkers()

			if debugger.server then
				debugger.server:Reset();
				--DestroyDebuggerServer()
			end
			SetAllEditorsReadOnly(false)
			ignoredFilesList = {}
			debugger.running = false
			DisplayOutput("\nDebuggee client stopped.\n\n")
		end)
frame:Connect(ID_STOP_DEBUG, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server ~= nil) and (editor ~= nil))
		end)

frame:Connect(ID_STEP, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			ClearAllCurrentLineMarkers()

			if debugger.server then
				local file, line = debugger.handle("step")
   			        local editor = GetEditor()
				editor:MarkerAdd(line-1, CURRENT_LINE_MARKER)
				editor:EnsureVisibleEnforcePolicy(line-1)
			end
		end)
frame:Connect(ID_STEP, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server ~= nil) and (not debugger.running) and (editor ~= nil))
		end)

frame:Connect(ID_STEP_OVER, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			ClearAllCurrentLineMarkers()

			if debugger.server then
				debugger.server:StepOver()
				debugger.running = true
			end
		end)
frame:Connect(ID_STEP_OVER, wx.wxEVT_UPDATE_UI,
		function (event)
			local editor = GetEditor()
			event:Enable((debugger.server ~= nil) and (not debugger.running) and (editor ~= nil))
		end)

frame:Connect(ID_STEP_OUT, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			ClearAllCurrentLineMarkers()

			if debugger.server then
				debugger.server:StepOut()
				debugger.running = true
			end
		end)
frame:Connect(ID_STEP_OUT, wx.wxEVT_UPDATE_UI,
		function (event)
			event:Enable((debugger.server ~= nil) and (not debugger.running))
		end)

frame:Connect(ID_CONTINUE, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			ClearAllCurrentLineMarkers()

			if debugger.server then
				debugger.server:Continue()
				debugger.running = true
			end
		end)
frame:Connect(ID_CONTINUE, wx.wxEVT_UPDATE_UI,
		function (event)
			event:Enable((debugger.server ~= nil) and (not debugger.running))
		end)

frame:Connect(ID_BREAK, wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			if debugger.server then
				debugger.server:Break()
			end
		end)
frame:Connect(ID_BREAK, wx.wxEVT_UPDATE_UI,
		function (event)
			event:Enable((debugger.server ~= nil) and debugger.running)
		end)
		
frame:Connect(ID_DEBUGGER_PORT, wx.wxEVT_COMMAND_MENU_SELECTED,
		function(event)
		end)
frame:Connect(ID_DEBUGGER_PORT, wx.wxEVT_UPDATE_UI,
		function(event)
			event:Enable(debugger.server == nil)
		end)
