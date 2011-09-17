-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local debugger = {}

--debuggerServer     = nil    -- wxLuaDebuggerServer object when debugging, else nil
--debuggerServer_    = nil    -- temp wxLuaDebuggerServer object for deletion
--debuggee_running   = false  -- true when the debuggee is running
--debugger_destroy   = 0      -- > 0 if the debugger is to be destroyed in wxEVT_IDLE
--debuggee_pid       = 0      -- pid of the debuggee process
--debuggerPortNumber = 1551   -- the port # to use for debugging

debugger.server     = nil    -- wxLuaDebuggerServer object when debugging, else nil
debugger.server_    = nil    -- temp wxLuaDebuggerServer object for deletion
debugger.running    = false  -- true when the debuggee is running
debugger.destroy    = 0      -- > 0 if the debugger is to be destroyed in wxEVT_IDLE
debugger.pid        = 0      -- pid of the debuggee process
debugger.portnumber = 1551   -- the port # to use for debugging

debugger.watchWindow      = nil    -- the watchWindow, nil when not created
debugger.watchListCtrl    = nil    -- the child listctrl in the watchWindow

ide.debugger = debugger


-- ---------------------------------------------------------------------------
-- Create the watch window

local notebook 		= ide.frame.vsplitter.splitter.notebook
local openDocuments = ide.openDocuments

function ProcessWatches()
	local watchListCtrl = debugger.watchListCtrl
	if watchListCtrl and debugger.server then
		for idx = 0, watchListCtrl:GetItemCount() - 1 do
			local expression = watchListCtrl:GetItemText(idx)
			debugger.server:EvaluateExpr(idx, expression)
		end
	end
end

function CloseWatchWindow()
	if (debugger.watchWindow) then
		debugger.watchListCtrl = nil
		debugger.watchWindow:Destroy()
		debugger.watchWindow = nil
	end
end

function CreateWatchWindow()
	local width = 180
	local watchWindow = wx.wxFrame(ide.frame, wx.wxID_ANY, "Estrela Editor Watch Window",
							 wx.wxDefaultPosition, wx.wxSize(width, 160))

	debugger.watchWindow = watchWindow

	local watchMenu = wx.wxMenu{
			{ ID_ADDWATCH,      "&Add Watch"        },
			{ ID_EDITWATCH,     "&Edit Watch\tF2"   },
			{ ID_REMOVEWATCH,   "&Remove Watch"     },
			{ ID_EVALUATEWATCH, "Evaluate &Watches" }}

	local watchMenuBar = wx.wxMenuBar()
	watchMenuBar:Append(watchMenu, "&Watches")
	watchWindow:SetMenuBar(watchMenuBar)

	local watchListCtrl = wx.wxListCtrl(watchWindow, ID_WATCH_LISTCTRL,
								  wx.wxDefaultPosition, wx.wxDefaultSize,
								  wx.wxLC_REPORT + wx.wxLC_EDIT_LABELS)
								  
	debugger.watchListCtrl = watchListCtrl

	local info = wx.wxListItem()
	info:SetMask(wx.wxLIST_MASK_TEXT + wx.wxLIST_MASK_WIDTH)
	info:SetText("Expression")
	info:SetWidth(width / 2)
	watchListCtrl:InsertColumn(0, info)

	info:SetText("Value")
	info:SetWidth(width / 2)
	watchListCtrl:InsertColumn(1, info)

	watchWindow:CentreOnParent()
	ConfigRestoreFramePosition(watchWindow, "WatchWindow")
	watchWindow:Show(true)

	local function FindSelectedWatchItem()
		local count = watchListCtrl:GetSelectedItemCount()
		if count > 0 then
			for idx = 0, watchListCtrl:GetItemCount() - 1 do
				if watchListCtrl:GetItemState(idx, wx.wxLIST_STATE_FOCUSED) ~= 0 then
					return idx
				end
			end
		end
		return -1
	end

	watchWindow:Connect( wx.wxEVT_CLOSE_WINDOW,
			function (event)
				ConfigSaveFramePosition(watchWindow, "WatchWindow")
				watchWindow = nil
				watchListCtrl = nil
				event:Skip()
			end)

	watchWindow:Connect(ID_ADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
			function (event)
				local row = watchListCtrl:InsertItem(watchListCtrl:GetItemCount(), "Expr")
				watchListCtrl:SetItem(row, 0, "Expr")
				watchListCtrl:SetItem(row, 1, "Value")
				watchListCtrl:EditLabel(row)
			end)

	watchWindow:Connect(ID_EDITWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
			function (event)
				local row = FindSelectedWatchItem()
				if row >= 0 then
					watchListCtrl:EditLabel(row)
				end
			end)
	watchWindow:Connect(ID_EDITWATCH, wx.wxEVT_UPDATE_UI,
			function (event)
				event:Enable(watchListCtrl:GetSelectedItemCount() > 0)
			end)

	watchWindow:Connect(ID_REMOVEWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
			function (event)
				local row = FindSelectedWatchItem()
				if row >= 0 then
					watchListCtrl:DeleteItem(row)
				end
			end)
	watchWindow:Connect(ID_REMOVEWATCH, wx.wxEVT_UPDATE_UI,
			function (event)
				event:Enable(watchListCtrl:GetSelectedItemCount() > 0)
			end)

	watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
			function (event)
				ProcessWatches()
			end)
	watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_UPDATE_UI,
			function (event)
				event:Enable(watchListCtrl:GetItemCount() > 0)
			end)

	watchListCtrl:Connect(wx.wxEVT_COMMAND_LIST_END_LABEL_EDIT,
			function (event)
				watchListCtrl:SetItem(event:GetIndex(), 0, event:GetText())
				ProcessWatches()
				event:Skip()
			end)
end


function NextDebuggerPort()
	-- limit the number if ports we use, for people who need to open
	-- their firewall
	debugger.portnumber = debugger.portnumber + 1
	if (debugger.portnumber > 1559) then
		debugger.portnumber = 1551
	end
end

function CreateDebuggerServer()
	if (debugger.server) then
		-- we just delete it here, but this shouldn't happen
		debugger.destroy = 0
		local ds = debugger.server
		debugger.server = nil
		ds:Reset()
		ds:StopServer()
		ds:delete()
	end

	debugger.running = false
	debugger.server = wxlua.wxLuaDebuggerServer(debugger.portnumber)
	local ds = debugger.server

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_DEBUGGEE_CONNECTED,
		function (event)
			local ok = false
			-- FIXME why would you want to run all the notebook pages?
			--for id, document in pairs(openDocuments) do
				local editor     = GetEditor() -- MUST use document.editor userdata!
				local document   = openDocuments[editor:GetId()]
				local editor     = document.editor
				local editorText = editor:GetText()
				local filePath   = MakeDebugFileName(editor, document.filePath)
				ok = ds:Run(filePath, editorText)

				local nextLine = editor:MarkerNext(0, BREAKPOINT_MARKER_VALUE)
				while ok and (nextLine ~= -1) do
					ok = ds:AddBreakPoint(filePath, nextLine)
					nextLine = editor:MarkerNext(nextLine + 1, BREAKPOINT_MARKER_VALUE)
				end
			--end

			if ok then
				ok = ds:Step()
			end
			debugger.running = ok

			UpdateUIMenuItems()

			if ok then
				DisplayOutput("Client connected ok.\n")
			else
				DisplayOutput("Error connecting to client.\n")
			end
		end)

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_DEBUGGEE_DISCONNECTED,
		function (event)
			DisplayOutput("Debug server disconnected.\n")
			DisplayOutput(event:GetMessage().."\n\n")
			DestroyDebuggerServer()
		end)

	local function DebuggerIgnoreFile(fileName)
		local ignoreFlag = false
		for idx, ignoreFile in pairs(ignoredFilesList) do
			if string.upper(ignoreFile) == string.upper(fileName) then
				ignoreFlag = true
			end
		end
		return ignoreFlag
	end

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_BREAK,
		function (event)
			if exitingProgram then return end
			local line = event:GetLineNumber()
			local eventFileName = event:GetFileName()

			if string.sub(eventFileName, 1, 1) == '@' then -- FIXME what is this?
				eventFileName = string.sub(eventFileName, 2, -1)
				if wx.wxIsAbsolutePath(eventFileName) == false then
					eventFileName = wx.wxGetCwd().."/"..eventFileName
				end
			end
			if wx.__WXMSW__ then
				eventFileName = wx.wxUnix2DosFilename(eventFileName)
			end
			local fileFound = false
			DisplayOutput("At Breakpoint line: "..tostring(line).." file: "..eventFileName.."\n")
			for id, document in pairs(openDocuments) do
				local editor   = document.editor
				local filePath = MakeDebugFileName(editor, document.filePath)
				-- for running in cygwin, use same type of separators
				filePath = string.gsub(filePath, "\\", "/")
				local eventFileName_ = string.gsub(eventFileName, "\\", "/")
				if string.upper(filePath) == string.upper(eventFileName_) then
					local selection = document.index
					notebook:SetSelection(selection)
					SetEditorSelection(selection)
					editor:MarkerAdd(line, CURRENT_LINE_MARKER)
					editor:EnsureVisibleEnforcePolicy(line)
					fileFound = true
					break
				end
			end
			-- if don't ignore file and its not in the notebook, ask to load
			if not DebuggerIgnoreFile(eventFileName) then
				if not fileFound then
					local fileDialog = wx.wxFileDialog(ide.frame,
													   "Select file for debugging",
													   "",
													   eventFileName,
													   "Lua files (*.lua)|*.lua|Text files (*.txt)|*.txt|All files (*)|*",
													   wx.wxOPEN + wx.wxFILE_MUST_EXIST)
					if fileDialog:ShowModal() == wx.wxID_OK then
						local editor = LoadFile(fileDialog:GetPath(), nil, true)
						if editor then
							editor:MarkerAdd(line, CURRENT_LINE_MARKER)
							editor:EnsureVisibleEnforcePolicy(line)
							editor:SetReadOnly(true)
							fileFound = true
						end
					end
					fileDialog:Destroy()
				end
				if not fileFound then -- they canceled opening the file
					table.insert(ignoredFilesList, eventFileName)
				end
			end

			if fileFound then
				debugger.running = false
				ProcessWatches()
			elseif debugger.server then
				debugger.server:Continue()
				debugger.running = true
			end
		end)

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_PRINT,
		function (event)
			DisplayOutput(event:GetMessage().."\n")
		end)

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_ERROR,
		function (event)
			DisplayOutput("wxLua ERROR: "..event:GetMessage().."\n\n")
		end)

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_EXIT,
		function (event)
			ClearAllCurrentLineMarkers()

			if debuggerServer then
				DestroyDebuggerServer()
			end
			SetAllEditorsReadOnly(false)
			ignoredFilesList = {}
		end)

	ds:Connect(wxlua.wxEVT_WXLUA_DEBUGGER_EVALUATE_EXPR,
		function (event)
			local watchListCtrl = debugger.watchListCtrl
			if watchListCtrl then
				watchListCtrl:SetItem(event:GetReference(),
									  1,
									  event:GetMessage())
			end
		end)

	local ok = ds:StartServer()
	if not ok then
		DestroyDebuggerServer()
		DisplayOutput("Error starting the debug server.\n")
		return nil
	end

	return ds
end

function DestroyDebuggerServer()
	-- nil debuggerServer so it won't be used and set flag to destroy it in idle
	if (debugger.server) then
		debugger.server_ = debugger.server
		debugger.server = nil
		debugger.destroy = 1 -- set > 0 to initiate deletion in idle
	end
end
