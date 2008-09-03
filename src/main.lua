-- Load the wxLua module, does nothing if running from wxLua, wxLuaFreeze, or wxLuaEdit
package.cpath = package.cpath..";./?.dll;./?.so;../lib/?.so;../lib/vc_dll/?.dll;../lib/bcc_dll/?.dll;../lib/mingw_dll/?.dll;"
require("wx")
require("bit")

dofile "src/misc/util.lua"


-----------
-- IDE
--
-- Setup important defaults
dofile "src/editor/ids.lua"
dofile "src/editor/style.lua"

ide = {
	config = {
		path = {
			projectdir = "",
		}, 
		editor = {},
		styles = StylesGetDefault(),
		interpreter = "EstrelaEditor",
		autocomplete = true,
		filehistorylength = 20,
	},
	specs = {
		none = {
			linecomment = ">",
		}
	},
	tools = {
	},
	
	frame            = nil,    -- gui related
	debugger         = nil,    -- debugger
	findReplace      = nil,    -- find & replace handling
	settings         = nil,    -- user settings (window pos, last files..)
	
	-- misc
	exitingProgram   = false,  -- are we currently exiting, ID_EXIT
	editorFilename   = nil,    -- the name of the wxLua program to be used when starting debugger
	editorApp        = wx.wxGetApp(),
	openDocuments    = {},-- open notebook editor documents[winId] = {
						  --   editor     = wxStyledTextCtrl,
						  --   index      = wxNotebook page index,
						  --   filePath   = full filepath, nil if not saved,
						  --   fileName   = just the filename,
						  --   modTime    = wxDateTime of disk file or nil,
						  --   isModified = bool is the document modified? }
	ignoredFilesList = {},
	font			 = nil,
	fontItalic		 = nil,
}

-- load config
local function loadCFG()
	local function addConfig(filename,showerror)
		local cfgfn,err = loadfile(filename)
		if not cfgfn then
			if (showerror) then
				print("Error while loading configuration file: \n",debug.traceback(err))
			end
		else
			setfenv(cfgfn,ide.config)
			xpcall(cfgfn,function(err)print("Error while executing configuration file: \n",debug.traceback(err))end)
		end
	end
	
	addConfig("cfg/config.lua",true)
	-- TODO alternate search path
	addConfig("cfg/user.lua",false)
end
loadCFG()


local function addToTab(tab,file)
	local cfgfn,err = loadfile(file)
	if not cfgfn then
		print("Error while loading configuration file: \n",debug.traceback(err))
	else
		local name = file:match("([a-zA-Z_0-9]+)%.lua$")
		
		local success
		success, result = xpcall(cfgfn,function(err)print("Error while executing configuration file: \n",debug.traceback(err))end)
		if (name and success) then
			tab[name] = result
		end
	end
end
	
-- load specs
local function loadSpecs()
	
	local files = FileSysGet(".\\spec\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" then
			addToTab(ide.specs,file)
		end
	end
end
loadSpecs()


-- load tools
local function loadTools()
	
	local files = FileSysGet(".\\tools\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" then
			addToTab(ide.tools,file)
		end
	end
end
loadTools()

---------------
-- Load App

dofile "src/editor/settings.lua"

dofile "src/editor/singleinstance.lua" 


dofile "src/editor/gui.lua"
dofile "src/editor/output.lua"
dofile "src/editor/debugger.lua"


dofile "src/editor/editor.lua"
dofile "src/editor/autocomplete.lua"
dofile "src/editor/findreplace.lua"
dofile "src/editor/commands.lua"

dofile "src/editor/shellbox.lua"

dofile "src/editor/menu.lua"







-- load rest of settings
SettingsRestoreFramePosition(ide.frame, "MainFrame")

SettingsRestoreFileSession(SetOpenFiles)
SettingsRestoreFileHistory(UpdateFileHistoryUI)

-- ---------------------------------------------------------------------------
-- Load the args that this script is run with

--for k, v in pairs(arg) do print(k, v) end

if arg then
	local notebook = ide.frame.splitter.notebook
	
	-- arguments pushed into wxLua are
	--   [C++ app and it's args][lua prog at 0][args for lua start at 1]
	ide.editorFilename = arg[0] 
	
	local loaded 

	for index = 1, #arg do
		fileName = arg[index]
		if fileName ~= "--" then
			LoadFile(fileName, nil, true)
			loaded = true
		end
	end

	if notebook:GetPageCount() > 0 then
		if not loaded then 
			notebook:SetSelection(0) 
		end
	else
	   local editor = CreateEditor("untitled.lua")
	   SetupKeywords(editor, "lua")
	end
else
	local editor = CreateEditor("untitled.lua")
	SetupKeywords(editor, "lua")
end

ide.frame:Show(true)

icon = wx.wxIcon()
icon:LoadFile("res/estrela.ico",wx.wxBITMAP_TYPE_ICO)
ide.frame:SetIcon(icon)
--wx.wxTaskBarIcon():SetIcon(icon,"Luxinia IDE") <-- not necessary? Adds luxinia icon to tray in some cases

DisplayOutput("Starting mainloop.\n")



-- Call wx.wxGetApp():MainLoop() last to start the wxWidgets event loop,
-- otherwise the wxLua program will exit immediately.
-- Does nothing if running from wxLua, wxLuaFreeze, or wxLuaEdit since the
-- MainLoop is already running or will be started by the C++ program.
wx.wxGetApp():MainLoop()

