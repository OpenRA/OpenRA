-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

package.cpath = package.cpath..';bin/?.dll;bin/clibs/?.dll;bin/clibs/?/?.dll;bin/clibs/?/?/?.dll'
package.cpath = package.cpath..';bin/?.so;bin/clibs/?.so;bin/clibs/?/?.so;bin/clibs/?/?/?.so'
package.path = package.path..'lualibs/?.lua;lualibs/?/?.lua;lualibs/?/init.lua;lualibs/?/?/?.lua;lualibs/?/?/init.lua'

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
			app = nil,
		}, 
		editor = {
			usetabs = true,
			autotabs = true,
		},
		debugger = {
			verbose = false,
		},
		outputshell = {},
		
		styles = StylesGetDefault(),
		stylesoutshell = StylesGetDefault(),
		interpreter = "_undefined_",
		
		autocomplete = true,
		acandtip = {
			shorttip = false,
			ignorecase = false,
			strategy = 2,
		},
		
		filehistorylength = 20,
		projecthistorylength = 15,
		savebak = false,
		singleinstance = false,
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
	
	app              = nil,    -- application engine
	interpreter      = nil,    -- current Lua interpreter
	frame            = nil,    -- gui related
	debugger         = {},     -- debugger related info
	filetree         = nil,    -- filetree
	findReplace      = nil,    -- find & replace handling
	settings         = nil,    -- user settings (window pos, last files..)
	
	-- misc
	exitingProgram   = false,  -- are we currently exiting, ID_EXIT
	editorApp        = wx.wxGetApp(),
	editorFilename   = nil,
	openDocuments    = {},-- open notebook editor documents[winId] = {
						  --   editor     = wxStyledTextCtrl,
						  --   index      = wxNotebook page index,
						  --   filePath   = full filepath, nil if not saved,
						  --   fileName   = just the filename,
						  --   modTime    = wxDateTime of disk file or nil,
						  --   isModified = bool is the document modified? }
	ignoredFilesList = {},
	font             = nil,
	fontItalic       = nil,
	ofont            = nil,
	ofontItalic      = nil,
}


---------------
-- process args
local filenames = {}
local configs = {}
do
	local arg = {...}
	ide.arg = arg
	-- first argument must be the application name
	assert(type(arg[1]) == "string","first argument must be application name")
	ide.editorFilename = arg[1]
	ide.config.path.app = arg[1]:match("([%w_-]+)%.?[^%.]*$")
	assert(ide.config.path.app, "no application path defined")
	for index = 2, #arg do
		if (arg[index] == "-cfg" and index+1 <= #arg) then
			local str = arg[index+1]
			if #str < 4 then
				print("Comandline: -cfg arg data not passed as string")
			else
				table.insert(configs,str)
			end
			index = index+1
		else
			table.insert(filenames,arg[index])
		end
	end
end

-----------------------
-- load config
local function addConfig(filename,showerror,isstring)
	local cfgfn,err = isstring and loadstring(filename) or loadfile(filename)
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

do
	addConfig(ide.config.path.app.."/config.lua",true)
	addConfig("cfg/user.lua",false)
	for i,v in ipairs(configs) do
		addConfig(v,true,true)
	end
	configs = nil
end


----------------------
-- process application

ide.app = dofile(ide.config.path.app.."/app.lua")
local app = ide.app
assert(app)

do
	local app = ide.app
	function GetIDEString(keyword, default)
		return app.stringtable[keyword] or default or keyword
	end

end

----------------------
-- process plugins 

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
		if file:match "%.lua$" and app.loadfilters.interpreters(file) then
			addToTab(ide.interpreters,file)
		end
	end
end
loadInterpreters()


-- load specs
local function loadSpecs()
	
	local files = FileSysGet(".\\spec\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" and app.loadfilters.specs(file) then
			addToTab(ide.specs,file)
		end
	end
	
	for n,spec in pairs(ide.specs) do
		spec.sep = spec.sep or ""
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

-- load tools
local function loadTools()
	
	local files = FileSysGet(".\\tools\\*.*",wx.wxFILE)
	for i,file in ipairs(files) do
		if file:match "%.lua$" and app.loadfilters.tools(file) then
			addToTab(ide.tools,file)
		end
	end
end
loadTools()

if app.preinit then app.preinit() end

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
SettingsRestoreProjectSession(FileTreeSetProjects)


-- ---------------------------------------------------------------------------
-- Load the filenames

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

if app.postinit then app.postinit() end

ide.frame:Show(true)

-- Call wx.wxGetApp():MainLoop() last to start the wxWidgets event loop,
-- otherwise the wxLua program will exit immediately.
-- Does nothing if running from wxLua, wxLuaFreeze, or wxLuaEdit since the
-- MainLoop is already running or will be started by the C++ program.
wx.wxGetApp():MainLoop()

