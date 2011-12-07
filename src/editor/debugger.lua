-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
--          Paul Kulchenko
---------------------------------------------------------
local ide = ide
local debugger      = ide.debugger
local frame         = ide.frame
local notebook      = frame.vsplitter.splitter.notebook

debugger.server     = nil    -- debugger engine provided by interpreter
debugger.running    = false  -- true when the debuggee is running

debugger.watchWindow      = nil    -- the watchWindow, nil when not created
debugger.watchListCtrl    = nil    -- the child listctrl in the watchWindow


-- ---------------------------------------------------------------------------


local function updateWatches()
	local watchListCtrl = debugger.watchListCtrl
	if watchListCtrl and debugger.server and not debugger.running then
		local expressions = {}
		local cnt = watchListCtrl:GetItemCount()
		for idx = 0, cnt - 1 do
			table.insert(expressions,watchListCtrl:GetItemText(idx))
		end
		
		local function submitResults(values)
			if (not debugger.watchListCtrl) then return end
			local watchListCtrl = debugger.watchListCtrl
			if (values and #values == cnt) then
				for idx = 0, cnt - 1 do
					watchListCtrl:SetItem(idx, 1, values[idx + 1])
				end
			else
				for idx = 0, cnt - 1 do
					watchListCtrl:SetItem(idx, 1, "")
				end
			end
		end
		
		debugger.server:evaluate(expressions,submitResults)
	end
end

local function DebuggerFileAction( fileName, line )
	if (not debugger.server)     then return end
	if (not (fileName and line)) then return end

	if not wx.wxIsAbsolutePath(fileName) then
		fileName = wx.wxGetCwd().."/"..fileName
	end

	if wx.__WXMSW__ then
		fileName = wx.wxUnix2DosFilename(fileName)
	end
	fileName = string.gsub(fileName, "\\", "/")
	fileName = string.upper(fileName)

	for id, document in pairs(ide.openDocuments) do
		local editor   = document.editor
		local fileOpen = string.gsub(document.filePath, "\\", "/")
		      fileOpen = string.upper(fileOpen)
		if fileOpen == fileName then
			local selection = document.index
			notebook:SetSelection(selection)
			SetEditorSelection(selection)
			editor:MarkerAdd(line-1, CURRENT_LINE_MARKER)
			editor:EnsureVisibleEnforcePolicy(line-1)
			
			updateWatches()
			return
		end
	end
	
end


frame:Connect(wx.wxEVT_IDLE,
		function(event)
			if (debugger.server) then
				debugger.server:update()
			end
		end)

-- ---------------------------------------------------------------------------
-- generic debugger setup

function DebuggerStart(server)
	if (debugger.server) then return end
	debugger.server = server
	
	SetAllEditorsReadOnly(true)
	-- go over all windows and find all breakpoints
	for id, document in pairs(ide.openDocuments) do
		local editor   = document.editor
		local filePath = string.gsub(document.filePath, "\\", "/")
		line = editor:MarkerNext(0, BREAKPOINT_MARKER_VALUE)
		while line ~= -1 do
			debugger.server:breakpoint(filePath,line + 1,true)
			line = editor:MarkerNext(line + 1, BREAKPOINT_MARKER_VALUE)
		end
	end
end

function DebuggerEnd()
	if (not debugger.server) then return end
	debugger.server:close()
	debugger.server = nil
	SetAllEditorsReadOnly(false)
end

-- ---------------------------------------------------------------------------
-- Create the watch window


function CloseWatchWindow()
	if (debugger.watchWindow) then
		SettingsSaveFramePosition(debugger.watchWindow, "WatchWindow")
		debugger.watchListCtrl = nil
		debugger.watchWindow = nil
	end
end

function DebuggerCreateWatchWindow()
	local width = 200
	local watchWindow = wx.wxFrame(ide.frame, wx.wxID_ANY, 
			"Watch Window",
			wx.wxDefaultPosition, wx.wxSize(width, 160), 
			wx.wxDEFAULT_FRAME_STYLE + wx.wxFRAME_FLOAT_ON_PARENT)

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
	info:SetWidth(width * 0.45)
	watchListCtrl:InsertColumn(0, info)

	info:SetText("Value")
	info:SetWidth(width * 0.45)
	watchListCtrl:InsertColumn(1, info)

	watchWindow:CentreOnParent()
	SettingsRestoreFramePosition(watchWindow, "WatchWindow")
	watchWindow:Show(true)

	local function findSelectedWatchItem()
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
				DebuggerCloseWatchWindow()
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
				local row = findSelectedWatchItem()
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
				local row = findSelectedWatchItem()
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
				updateWatches()
			end)
	watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_UPDATE_UI,
			function (event)
				event:Enable(watchListCtrl:GetItemCount() > 0)
			end)

	watchListCtrl:Connect(wx.wxEVT_COMMAND_LIST_END_LABEL_EDIT,
			function (event)
				watchListCtrl:SetItem(event:GetIndex(), 0, event:GetText())
				updateWatches()
				event:Skip()
			end)
end

local function makeDebugFileName(editor, filePath)
	if not filePath then
		filePath = "file"..tostring(editor)
	end
	return filePath
end

function DebuggerToggleBreakpoint(editor, line)
	if (not ide.interpreter.hasdebugger) then return end
	
	local markers = editor:MarkerGet(line)
	if markers >= CURRENT_LINE_MARKER_VALUE then
		markers = markers - CURRENT_LINE_MARKER_VALUE
	end
	local id       = editor:GetId()
	local filePath = makeDebugFileName(editor, ide.openDocuments[id].filePath)
	if markers >= BREAKPOINT_MARKER_VALUE then
		editor:MarkerDelete(line, BREAKPOINT_MARKER)
		if (debugger.server) then
			debugger.server:breakpoint(filePath,line+1,false)
		end
	else
		editor:MarkerAdd(line, BREAKPOINT_MARKER)
		if (debugger.server) then
			debugger.server:breakpoint(filePath,line+1,true)
		end
	end
end


