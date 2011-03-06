-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

require("wx")
require("bit")

dofile "src/misc/util.lua"

function DummyConfig()
	return {path = {}, editor = {}, view ={}, acandtip = {}, outputshell = {},}
end


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
		editor = {
			usetabs = true,
			autotabs = true,
		},
		outputshell = {},
		
		styles = StylesGetDefault(),
		stylesoutshell = StylesGetDefault(),
		interpreter = "EstrelaEditor",
		
		autocomplete = true,
		acandtip = {
			shorttip = false,
		},
		
		filehistorylength = 20,
		projecthistorylength = 15,
		savebak = false,
		singleinstance = true,
		singleinstanceport = 0xe493,
		
		view = {
			vsplitterpos = 150,
			splitterheight = 200,
		},
	},
	specs = {
		none = {
			linecomment = ">",
			sep = "\1",
		}
	},
	tools = {
	},
	iofilters = {
	},
	interpreters = {
	},
	
	frame            = nil,    -- gui related
	debugger         = nil,    -- debugger
	filetree         = nil,    -- filetree
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
	ofont			 = nil,
	ofontItalic		 = nil,
}

-- load config
local function addConfig(filename,showerror,isstring)
	local cfgfn,err = isstring and loadstring(filename) or loadfile(filename)
		-- 							^^                     ^^ wtf?
	if not cfgfn then
		if (showerror) then
			print(("Error while loading configuration file: %s\n%s"):format(filename,err))
		end
	else
		ide.config.os = os
		ide.config.wxstc = wxstc
		setfenv(cfgfn,ide.config)
		xpcall(function()cfgfn(assert(_G))end,
			function(err)
				print("Error while executing configuration file: \n",
					debug.traceback(err))end)
	end
end
local function loadCFG()
	addConfig("cfg/config.lua",true)
	-- TODO alternate search path
	addConfig("cfg/user.lua",false)
end
loadCFG()


local function addToTab(tab,file)
	local cfgfn,err = loadfile(file)
	if not cfgfn then
		print(("Error while loading configuration file (%s): \n%s"):format(file,err))
	else
		local name = file:match("([a-zA-Z_0-9]+)%.lua$")
		
		local success,result
		success, result = xpcall(
			function()return cfgfn(_G)end,
			function(err)
				print(("Error while executing configuration file (%s): \n%s"):
					format(file,debug.traceback(err)))
			end)
		if (name and success) then
			if (tab[name]) then
				local out = tab[name]
				for i,v in pairs(result) do
					out[i] = v
				end
			else
				tab[name] = result
			end
		end
	end
end

-- load interpreters
local function loadInterpreters()
	
	local files = FileSysGet(".\\interpreters\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" then
			addToTab(ide.interpreters,file)
		end
	end
end
loadInterpreters()


	
-- load specs
local function loadSpecs()
	
	local files = FileSysGet(".\\spec\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" then
			addToTab(ide.specs,file)
		end
	end
	
	for n,spec in pairs(ide.specs) do
		spec.sep = spec.sep or "\1"
		spec.iscomment = {}
		spec.iskeyword0 = {}
		if (spec.lexerstyleconvert) then
			if (spec.lexerstyleconvert.comment) then
				for i,s in pairs(spec.lexerstyleconvert.comment) do
					spec.iscomment[s] = true
				end
			end
			if (spec.lexerstyleconvert.keywords0) then
				for i,s in pairs(spec.lexerstyleconvert.keywords0) do
					spec.iskeyword0[s] = true
				end
			end
		end
	end
end
loadSpecs()
ide.loadSpecs = loadSpecs

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
-- process args
local filenames = {}
if arg then
	-- arguments pushed into wxLua are
	--   [C++ app and it's args][lua prog at 0][args for lua start at 1]
	ide.editorFilename = arg[0] 
	
	for index = 1, #arg do
		if (arg[index] == "-cfg" and index+1 <= #arg) then
			local str = arg[index+1]
			
			if #str < 4 then
				print("Comandline: -cfg arg data not passed as string")
			else
				addConfig(str,true,true)
			end
			index = index+1
		else
			table.insert(filenames,arg[index])
		end
	end
end

---------------
-- Load App

dofile "src/editor/settings.lua"
dofile "src/editor/singleinstance.lua" 
dofile "src/editor/iofilters.lua"

dofile "src/editor/gui.lua"
dofile "src/editor/output.lua"
dofile "src/editor/debugger.lua"
dofile "src/editor/filetree.lua"
dofile "src/editor/preferences.lua"

dofile "src/editor/editor.lua"
dofile "src/editor/autocomplete.lua"
dofile "src/editor/findreplace.lua"
dofile "src/editor/commands.lua"

dofile "src/editor/shellbox.lua"

dofile "src/editor/menu.lua"

dofile "src/preferences/editor.lua"
dofile "src/preferences/project.lua"





-- load rest of settings
SettingsRestoreEditorSettings()
SettingsRestoreFramePosition(ide.frame, "MainFrame")
SettingsRestoreView()
SettingsRestoreFileSession(SetOpenFiles)
SettingsRestoreFileHistory(UpdateFileHistoryUI)
SettingsRestoreProjectSession(SetProjects)


-- ---------------------------------------------------------------------------
-- Load the args that this script is run with

--for k, v in pairs(arg) do print(k, v) end

do
	local notebook = ide.frame.vsplitter.splitter.notebook
	local loaded 

	for i,fileName in ipairs(filenames) do
		if fileName ~= "--" then
			LoadFile(fileName, nil, true)
			loaded = true
		end
	end

	if notebook:GetPageCount() > 0 then
		
	else
	   local editor = CreateEditor("untitled.lua")
	   SetupKeywords(editor, "lua")
	end
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

