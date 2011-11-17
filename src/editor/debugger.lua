--
-- RemDebug 1.0 Beta
-- Copyright Kepler Project 2005 (http://www.keplerproject.org/remdebug)
--

local copas  = require "copas"
local socket = require "socket"
local mobdebug = require "mobdebug"

local debugger = {}
debugger.server     = nil    -- DebuggerServer object when debugging, else nil
debugger.running    = false  -- true when the debuggee is running
debugger.portnumber = 8171   -- the port # to use for debugging
debugger.watchWindow      = nil    -- the watchWindow, nil when not created
debugger.watchListCtrl    = nil    -- the child listctrl in the watchWindow

ide.debugger = debugger

debugger.listen = function() 
  local server = socket.bind("*", debugger.portnumber)
  DisplayOutput("Started server on " .. debugger.portnumber .. ".\n")
  copas.autoclose = false
  copas.addserver(server, function (skt)
    debugger.server = copas.wrap(skt)
    debugger.run("step")
    SetAllEditorsReadOnly(true)
    DisplayOutput("Established session with "..wx.wxGetHostName()..":"..debugger.portnumber..".\n")
  end)
end

debugger.handle = function(line)
  local _G = _G
  local os = os
  os.exit = function () end
  _G.print = function () end
  return mobdebug.handle(line, debugger.server);
end

debugger.run = function(command)
  if debugger.server then
    copas.addthread(function ()
      debugger.running = true
      local file, line = debugger.handle(command)
      debugger.running = false
      if line == nil then
        debugger.server = nil
        SetAllEditorsReadOnly(false)
        DisplayOutput("Program finished.\n")
      else
        local editor = GetEditor()
        editor:MarkerAdd(line-1, CURRENT_LINE_MARKER)
        editor:EnsureVisibleEnforcePolicy(line-1)
        debugger.updateWatches()
      end
    end)
  end
end

debugger.updateWatches = function()
  local watchListCtrl = debugger.watchListCtrl
  if watchListCtrl and debugger.server then
    copas.addthread(function ()
      for idx = 0, watchListCtrl:GetItemCount() - 1 do
        local expression = watchListCtrl:GetItemText(idx)
        local value = debugger.handle('eval ' .. expression)
        watchListCtrl:SetItem(idx, 1, value)
      end
    end)
  end
end

debugger.update = function() copas.step(0) end

debugger.listen()

function CloseWatchWindow()
	if (debugger.watchWindow) then
		debugger.watchListCtrl = nil
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
				debugger.updateWatches()
			end)
	watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_UPDATE_UI,
			function (event)
				event:Enable(watchListCtrl:GetItemCount() > 0)
			end)

	watchListCtrl:Connect(wx.wxEVT_COMMAND_LIST_END_LABEL_EDIT,
			function (event)
				watchListCtrl:SetItem(event:GetIndex(), 0, event:GetText())
				debugger.updateWatches()
				event:Skip()
			end)
end
