-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- put bin/ and lualibs/ first to avoid conflicts with included modules
-- that may have other versions present somewhere else in path/cpath
local iswindows = os.getenv('WINDIR') or (os.getenv('OS') or ''):match('[Ww]indows')
package.cpath = (iswindows
  and 'bin/?.dll;bin/clibs/?.dll;'
   or 'bin/clibs/?.dylib;bin/lib?.dylib;bin/?.so;bin/clibs/?.so;')
  .. package.cpath
package.path  = 'lualibs/?.lua;lualibs/?/?.lua;lualibs/?/init.lua;lualibs/?/?/?.lua;lualibs/?/?/init.lua;'
              .. package.path

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
    filetree = {},

    styles = StylesGetDefault(),
    stylesoutshell = StylesGetDefault(),
    interpreter = "_undefined_",

    autocomplete = true,
    acandtip = {
      shorttip = false,
      ignorecase = false,
      strategy = 2,
    },

    activateoutput = false, -- activate output/console on Run/Debug/Compile
    unhidewxwindow = false, -- try to unhide a wx window
    allowinteractivescript = false, -- allow interaction in the output window
    filehistorylength = 20,
    projecthistorylength = 15,
    savebak = false,
    singleinstance = false,
    singleinstanceport = 0xe493,
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

  app = nil, -- application engine
  interpreter = nil, -- current Lua interpreter
  frame = nil, -- gui related
  debugger = {}, -- debugger related info
  filetree = nil, -- filetree
  findReplace = nil, -- find & replace handling
  settings = nil, -- user settings (window pos, last files..)

  -- misc
  exitingProgram = false, -- are we currently exiting, ID_EXIT
  editorApp = wx.wxGetApp(),
  editorFilename = nil,
  openDocuments = {},-- open notebook editor documents[winId] = {
  -- editor = wxStyledTextCtrl,
  -- index = wxNotebook page index,
  -- filePath = full filepath, nil if not saved,
  -- fileName = just the filename,
  -- modTime = wxDateTime of disk file or nil,
  -- isModified = bool is the document modified? }
  ignoredFilesList = {},
  font = {
    eNormal = nil,
    eItalic = nil,
    oNormal = nil,
    oItalic = nil,
    fNormal = nil,
  }
}

---------------
-- process args
local filenames = {}
local configs = {}
do
  local arg = {...}
  local fullPath = arg[1] -- first argument must be the application name
  assert(type(fullPath) == "string", "first argument must be application name")

  if not wx.wxIsAbsolutePath(fullPath) then
    fullPath = wx.wxGetCwd().."/"..fullPath
    if wx.__WXMSW__ then fullPath = wx.wxUnix2DosFilename(fullPath) end
  end

  ide.arg = arg
  ide.editorFilename = fullPath
  ide.osname = wx.wxPlatformInfo.Get():GetOperatingSystemFamilyName()
  ide.config.path.app = fullPath:match("([%w_-%.]+)$"):gsub("%.[^%.]*$","")
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
    xpcall(function()cfgfn(assert(_G or _ENV))end,
      function(err)
        print("Error while executing configuration file: \n",
          debug.traceback(err))
      end)
  end
end

do
  addConfig(ide.config.path.app.."/config.lua",true)
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
      function()return cfgfn(_G or _ENV)end,
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
  local files = FileSysGet("./interpreters/*.*",wx.wxFILE)
  for i,file in ipairs(files) do
    if file:match "%.lua$" and app.loadfilters.interpreters(file) then
      addToTab(ide.interpreters,file)
    end
  end
end
loadInterpreters()

-- load specs
local function loadSpecs()
  local files = FileSysGet("./spec/*.*",wx.wxFILE)
  for i,file in ipairs(files) do
    if file:match "%.lua$" and app.loadfilters.specs(file) then
      addToTab(ide.specs,file)
    end
  end

  for n,spec in pairs(ide.specs) do
    spec.sep = spec.sep or ""
    spec.iscomment = {}
    spec.iskeyword0 = {}
    spec.isstring = {}
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
      if (spec.lexerstyleconvert.stringtxt) then
        for i,s in pairs(spec.lexerstyleconvert.stringtxt) do
          spec.isstring[s] = true
        end
      end
    end
  end
end
loadSpecs()

-- load tools
local function loadTools()
  local files = FileSysGet("./tools/*.*",wx.wxFILE)
  for i,file in ipairs(files) do
    if file:match "%.lua$" and app.loadfilters.tools(file) then
      addToTab(ide.tools,file)
    end
  end
end
loadTools()

if app.preinit then app.preinit() end

do
  addConfig("cfg/user.lua",false)
  addConfig(os.getenv( "HOME" ) .. "/.zbs/user.lua",false)
  for i,v in ipairs(configs) do
    addConfig(v,true,true)
  end
  configs = nil
end

---------------
-- Load App

dofile "src/editor/settings.lua"
dofile "src/editor/singleinstance.lua"
dofile "src/editor/iofilters.lua"

dofile "src/editor/gui.lua"
dofile "src/editor/filetree.lua"
dofile "src/editor/output.lua"
dofile "src/editor/debugger.lua"
dofile "src/editor/preferences.lua"

dofile "src/editor/editor.lua"
dofile "src/editor/autocomplete.lua"
dofile "src/editor/findreplace.lua"
dofile "src/editor/commands.lua"

dofile "src/editor/shellbox.lua"

dofile "src/editor/menu.lua"

dofile "src/preferences/editor.lua"
dofile "src/preferences/project.lua"

dofile "src/version.lua"

-- load rest of settings
SettingsRestoreEditorSettings()
SettingsRestoreFramePosition(ide.frame, "MainFrame")
SettingsRestoreFileSession(SetOpenFiles)
SettingsRestoreFileHistory(UpdateFileHistoryUI)
SettingsRestoreProjectSession(FileTreeSetProjects)
SettingsRestoreView()

-- ---------------------------------------------------------------------------
-- Load the filenames

do
  local notebook = ide.frame.notebook
  local loaded

  for i,fileName in ipairs(filenames) do
    if fileName ~= "--" then
      LoadFile(fileName, nil, true)
      loaded = true
    end
  end

  if notebook:GetPageCount() == 0 then
    local editor = CreateEditor("untitled.lua")
    SetupKeywords(editor, "lua")
  end
end

if app.postinit then app.postinit() end

-- only set menu bar *after* postinit handler as it may include adding
-- app-specific menus (Help/About), which are not recognized by MacOS
-- as special items unless SetMenuBar is done after menus are populated.
ide.frame:SetMenuBar(ide.frame.menuBar)
if ide.osname == 'Macintosh' then -- force refresh to fix the filetree
  pcall(function() ide.frame:ShowFullScreen(true) ide.frame:ShowFullScreen(false) end)
end
ide.frame:Show(true)

-- call wx.wxGetApp():MainLoop() last to start the wxWidgets event loop,
-- otherwise the program will exit immediately.
-- Does nothing if the MainLoop is already running.
wx.wxGetApp():MainLoop()
