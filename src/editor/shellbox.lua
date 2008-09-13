-- 
--	shellbox - a lua testbed environment within estrela
--

local shellbox = ide.frame.vsplitter.splitter.bottomnotebook.shellbox
local out = shellbox.output
local code = shellbox.input
local frame = ide.frame

out:WrapCount(80)
out:SetReadOnly(true)

local function print(...)
	out:SetReadOnly(false)
	for i=1,select('#',...) do
		local x = select(i,...)
		out:InsertText(out:GetLength(),tostring(x))
	end
	out:InsertText(out:GetLength(),"\n")
	out:GotoPos(out:GetLength())
	out:SetReadOnly(true)
end
local env
local function createenv ()
	env = {}
	setmetatable(env,{__index = _G})
	env.print = print
end

createenv()

code:SetBufferedDraw(true)
code:StyleClearAll()
code:SetFont(ide.font)
code:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font)
code:SetTabWidth(4)
code:SetIndent(4)
code:SetUseTabs(true)
code:SetIndentationGuides(true)
SetupKeywords(code,"lua")

local accel = wx.wxAcceleratorTable{
	{wx.wxACCEL_CTRL,13,ID "shellbox.execute"},
	{wx.wxACCEL_CTRL+wx.wxACCEL_ALT,string.byte "\b",ID "shellbox.eraseall"}
}
code:SetAcceleratorTable(accel)

function ExecuteShellboxCode ()
	local tx = code:GetText()
	local fn,err = loadstring(tx)
	if not fn then
		print("Error: "..err)
	else
		setfenv(fn,env)
		xpcall(fn,function(err)
			print(debug.traceback(err))
		end)
	end
end

shellbox:Connect(wxstc.wxEVT_STC_CHARADDED,
	function (event)
		frame:SetStatusText("Execute your code pressing CTRL+ENTER or erase it all with CTRL+ALT+DEL")
	end)
frame:Connect(ID "shellbox.eraseall", wx.wxEVT_COMMAND_MENU_SELECTED, function()
	code:SetText""
end)
frame:Connect(ID "shellbox.execute", wx.wxEVT_COMMAND_MENU_SELECTED, ExecuteShellboxCode)
shellbox:Connect(ID "shellbox.run",wx.wxEVT_COMMAND_BUTTON_CLICKED, ExecuteShellboxCode)
