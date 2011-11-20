-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- 
--	shellbox - a lua testbed environment within estrela
--

local shellbox = ide.frame.vsplitter.splitter.bottomnotebook.shellbox
local out = shellbox.output
local code = shellbox.input
local remote = shellbox.remote

local remotesend
local remoteuid

local frame = ide.frame
out:SetFont(ide.ofont)
out:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.ofont)
out:StyleClearAll()
out:SetBufferedDraw(true)
out:WrapCount(80)
out:MarkerDefine(BREAKPOINT_MARKER, wxstc.wxSTC_MARK_BACKGROUND, wx.wxBLACK, wx.wxColour(255, 220, 220))
out:SetReadOnly(true)
StylesApplyToEditor(ide.config.stylesoutshell,out,ide.ofont,ide.ofontItalic)

local function shellPrint(...)
	out:SetReadOnly(false)
	local cnt = select('#',...)
	for i=1,cnt do
		local x = select(i,...)
		out:InsertText(out:GetLength(),tostring(x)..(i < cnt and "\t" or ""))
	end
	out:InsertText(out:GetLength(),"\n")
	out:GotoPos(out:GetLength())
	out:SetReadOnly(true)
end

DisplayShell = shellPrint

local function createenv ()
	local env = {}
	setmetatable(env,{__index = _G})
	
	local function luafilename(level)
		level = level and level + 1 or 2
		local src
		while (true) do
			src = debug.getinfo(level)
			if (src == nil) then return nil,level end
			if (string.byte(src.source) == string.byte("@")) then
				return string.sub(src.source,2),level
			end
			level = level + 1
		end
	end
	
	local function luafilepath(level)
		local src,level = luafilename(level)
		if (src == nil) then return src,level end
		src = string.gsub(src,"[\\/][^\\//]*$","")
		return src,level
	end
	
	local function relativeFilename(file)
		assert(type(file)=='string',"String as filename expected")
		local name = file
		local level = 3
		while (name) do
			if (wx.wxFileName(name):FileExists()) then return name end
			name,level = luafilepath(level)
			if (name == nil) then break end
			name = name .. "/" .. file
		end
		
		return file
	end
	
	local function relativeFilepath(file)
		local name,level = luafilepath(3)
		return (file and name) and name.."/"..file or file or name
	end
	
	
	local _loadfile = loadfile
	local function loadfile(file)
		assert(type(file)=='string',"String as filename expected")
		local name = relativeFilename(file)
		
		return _loadfile(name)
	end

	local function dofile(file, ...)
		assert(type(file) == 'string',"String as filename expected")
		local fn,err = loadfile(file)
		local args = {...}
		if not fn then
			shellPrint("Error: "..err)
		else
			setfenv(fn,env)
			xpcall(function() return fn(unpack(args)) end,function(err)
				shellPrint(debug.traceback(err))
			end)
		end
	end

	env.print = shellPrint
	env.dofile = dofile
	env.loadfile = loadfile
	env.RELFILE = relativeFilename
	env.RELPATH = relativeFilepath
	
	return env
end

local env = createenv()

code:SetBufferedDraw(true)
code:SetFont(ide.ofont)
code:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.ofont)
code:StyleClearAll()
code:SetTabWidth(4)
code:SetIndent(ide.config.editor.tabwidth or 4)
code:SetUseTabs(ide.config.editor.usetabs and true or false)
code:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)
code:SetIndentationGuides(true)
--StylesApplyToEditor(ide.config.stylesoutshell,code,ide.ofont,ide.ofontItalic)
SetupKeywords(code,"lua",nil,ide.config.stylesoutshell,ide.ofont,ide.ofontItalic)

local accel = wx.wxAcceleratorTable{
	{wx.wxACCEL_CTRL,13,ID "shellbox.execute"},
	{wx.wxACCEL_CTRL+wx.wxACCEL_ALT,string.byte "\b",ID "shellbox.eraseall"}
}
code:SetAcceleratorTable(accel)

function ShellExecuteCode(ev,wfilename)
	local fn,err
        local tx
        local marker = "> " -- local execution
	if (wfilename) then
		fn,err = loadfile(wfilename:GetFullPath())
	elseif(remotesend and remote:IsChecked()) then
                marker = ">> " -- remote execution
		tx = code:GetText()
		remotesend(tx)
	else
		tx = code:GetText()
		fn,err = loadstring(tx)
	end

	if (tx ~= nil) then 
          shellPrint(marker .. tx)
        end

	if fn==nil and err then
                out:MarkerAdd(out:GetLineCount()-1, BREAKPOINT_MARKER)
		shellPrint("! " .. err)
	elseif fn then
		setfenv(fn,env)
		xpcall(fn,function(err)
                        out:MarkerAdd(out:GetLineCount()-1, BREAKPOINT_MARKER)
			shellPrint("! " .. debug.traceback(err))
		end)
	end
end

function ShellSupportRemote(client,uid)
	remote:Enable(client and true or false)
	remotesend = client
	remoteuid  = client and uid
	if (not client) then
		remote:SetValue(false)
	end
end

shellbox:Connect(wxstc.wxEVT_STC_CHARADDED,
	function (event)
		frame:SetStatusText("Execute your code pressing CTRL+ENTER")
	end)
frame:Connect(ID "shellbox.eraseall", wx.wxEVT_COMMAND_MENU_SELECTED, function()
	code:SetText ""
end)
frame:Connect(ID "shellbox.execute", wx.wxEVT_COMMAND_MENU_SELECTED, ShellExecuteCode)
shellbox:Connect(ID "shellbox.run",wx.wxEVT_COMMAND_BUTTON_CLICKED, ShellExecuteCode)
shellbox:Connect(ID "shellbox.remote",wx.wxEVT_COMMAND_CHECKBOX_CLICKED, function(event)
	if (remotesend) then 
		CommandLineToShell(remoteuid,event:IsChecked())
	end
	end)