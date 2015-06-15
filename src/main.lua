-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- put bin/ and lualibs/ first to avoid conflicts with included modules
-- that may have other versions present somewhere else in path/cpath.
local function isproc()
  local file = io.open("/proc")
  if file then file:close() end
  return file ~= nil
end
local iswindows = os.getenv('WINDIR') or (os.getenv('OS') or ''):match('[Ww]indows')
local islinux = not iswindows and isproc()
local arch = "x86" -- use 32bit by default
local unpack = table.unpack or unpack

if islinux then
  local file = io.popen("uname -m")
  if file then
    local machine=file:read("*l")
    local archtype= { x86_64="x64", armv7l="armhf" }
    arch = archtype[machine] or "x86"
    file:close()
  end
end

package.cpath = (
  iswindows and 'bin/?.dll;bin/clibs/?.dll;' or
  islinux and ('bin/linux/%s/lib?.so;bin/linux/%s/clibs/?.so;'):format(arch,arch) or
  --[[isosx]] 'bin/lib?.dylib;bin/clibs/?.dylib;')
    .. package.cpath
package.path  = 'lualibs/?.lua;lualibs/?/?.lua;lualibs/?/init.lua;lualibs/?/?/?.lua;lualibs/?/?/init.lua;'
              .. package.path

require("wx")
require("bit")

dofile "src/util.lua"

-----------
-- IDE
--
ide = {
  MODPREF = "* ",
  MAXMARGIN = 4,
  config = {
    path = {
      projectdir = "",
      app = nil,
    },
    editor = {
      foldcompact = true,
      checkeol = true,
      saveallonrun = false,
      caretline = true,
      showfncall = false,
      autotabs = false,
      usetabs  = false,
      tabwidth = 2,
      usewrap = true,
      calltipdelay = 500,
      smartindent = true,
      fold = true,
      autoreload = true,
      indentguide = true,
      backspaceunindent = true,
    },
    debugger = {
      verbose = false,
      hostname = nil,
      port = nil,
      runonstart = nil,
      redirect = nil,
      requestattention = true,
      maxdatalength = 400,
      maxdatanum = 400,
      maxdatalevel = 3,
    },
    default = {
      name = 'untitled',
      fullname = 'untitled.lua',
      interpreter = 'luadeb',
    },
    outputshell = {
      usewrap = true,
    },
    filetree = {
      mousemove = true,
    },
    outline = {
      showanonymous = '~',
      showflat = false,
      showmethodindicator = false,
      showonefile = false,
      sort = false,
    },
    commandbar = {
      prefilter = 250, -- number of records after which to apply filtering
    },
    staticanalyzer = {
      infervalue = false, -- off by default as it's a slower mode
    },
    search = {
      autocomplete = true,
      contextlinesbefore = 2,
      contextlinesafter = 2,
      showaseditor = false,
      zoom = 0,
      autohide = false,
    },

    toolbar = {
      icons = {},
      iconmap = {},
    },

    keymap = {},
    imagemap = {
      ['VALUE-MCALL'] = 'VALUE-SCALL',
    },
    messages = {},
    language = "en",

    styles = nil,
    stylesoutshell = nil,

    autocomplete = true,
    autoanalyzer = true,
    acandtip = {
      shorttip = true,
      nodynwords = true,
      ignorecase = false,
      symbols = true,
      droprest = true,
      strategy = 2,
      width = 60,
      maxlength = 450,
      warning = true,
    },
    arg = {}, -- command line arguments
    api = {}, -- additional APIs to load

    format = { -- various formatting strings
      menurecentprojects = "%f | %i",
      apptitle = "%T - %F",
    },

    activateoutput = true, -- activate output/console on Run/Debug/Compile
    unhidewindow = false, -- to unhide a gui window
    allowinteractivescript = true, -- allow interaction in the output window
    projectautoopen = true,
    autorecoverinactivity = 10, -- seconds
    outlineinactivity = 0.250, -- seconds
    filehistorylength = 20,
    projecthistorylength = 20,
    bordersize = 2,
    savebak = false,
    singleinstance = false,
    singleinstanceport = 0xe493,
    interpreter = "luadeb",
    hidpi = false, -- HiDPI/Retina display support
    hotexit = false,
    -- file exclusion lists
    excludelist = {".svn/", ".git/", ".hg/", "CVS/", "*.pyc", "*.pyo", "*.exe", "*.dll", "*.obj","*.o", "*.a", "*.lib", "*.so", "*.dylib", "*.ncb", "*.sdf", "*.suo", "*.pdb", "*.idb", ".DS_Store", "*.class", "*.psd", "*.db"},
    binarylist = {"*.jpg", "*.jpeg", "*.png", "*.gif", "*.ttf", "*.tga", "*.dds", "*.ico", "*.eot", "*.pdf", "*.swf", "*.jar", "*.zip", ".gz", ".rar"},
  },
  specs = {
    none = {
      sep = "\1",
    }
  },
  tools = {},
  iofilters = {},
  interpreters = {},
  packages = {},
  apis = {},
  timers = {},
  onidle = {},

  proto = {}, -- prototypes for various classes

  app = nil, -- application engine
  interpreter = nil, -- current Lua interpreter
  frame = nil, -- gui related
  debugger = {}, -- debugger related info
  filetree = nil, -- filetree
  findReplace = nil, -- find & replace handling
  settings = nil, -- user settings (window pos, last files..)
  session = {
    projects = {}, -- project configuration for the current session
    lastupdated = nil, -- timestamp of the last modification in any of the editors
    lastsaved = nil, -- timestamp of the last recovery information saved
  },

  -- misc
  exitingProgram = false, -- are we currently exiting, ID_EXIT
  infocus = nil, -- last component with a focus
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
  },

  osname = wx.wxPlatformInfo.Get():GetOperatingSystemFamilyName(),
  osarch = arch,
  oshome = os.getenv("HOME") or (iswindows and os.getenv('HOMEDRIVE') and os.getenv('HOMEPATH')
    and (os.getenv('HOMEDRIVE')..os.getenv('HOMEPATH'))),
  wxver = string.match(wx.wxVERSION_STRING, "[%d%.]+"),

  test = {}, -- local functions used for testing
}

-- add wx.wxMOD_RAW_CONTROL as it's missing in wxlua 2.8.12.3;
-- provide default for wx.wxMOD_CONTROL as it's missing in wxlua 2.8 that
-- is available through Linux package managers
if not wx.wxMOD_CONTROL then wx.wxMOD_CONTROL = 0x02 end
if not wx.wxMOD_RAW_CONTROL then
  wx.wxMOD_RAW_CONTROL = ide.osname == 'Macintosh' and 0x10 or wx.wxMOD_CONTROL
end
-- ArchLinux running 2.8.12.2 doesn't have wx.wxMOD_SHIFT defined
if not wx.wxMOD_SHIFT then wx.wxMOD_SHIFT = 0x04 end
-- wxDIR_NO_FOLLOW is missing in wxlua 2.8.12 as well
if not wx.wxDIR_NO_FOLLOW then wx.wxDIR_NO_FOLLOW = 0x10 end
if not wxaui.wxAUI_TB_PLAIN_BACKGROUND then wxaui.wxAUI_TB_PLAIN_BACKGROUND = 2^8 end

if not setfenv then -- Lua 5.2
  -- based on http://lua-users.org/lists/lua-l/2010-06/msg00314.html
  -- this assumes f is a function
  local function findenv(f)
    local level = 1
    repeat
      local name, value = debug.getupvalue(f, level)
      if name == '_ENV' then return level, value end
      level = level + 1
    until name == nil
    return nil end
  getfenv = function (f) return(select(2, findenv(f)) or _G) end
  setfenv = function (f, t)
    local level = findenv(f)
    if level then debug.setupvalue(f, level, t) end
    return f end
end

dofile "src/version.lua"

for _, file in ipairs({"proto", "ids", "style", "keymap", "toolbar"}) do
  dofile("src/editor/"..file..".lua")
end

ide.config.styles = StylesGetDefault()
ide.config.stylesoutshell = StylesGetDefault()

local function setLuaPaths(mainpath, osname)
  -- use LUA_DEV to setup paths for Lua for Windows modules if installed
  local luadev = osname == "Windows" and os.getenv('LUA_DEV')
  if luadev and not wx.wxDirExists(luadev) then luadev = nil end
  local luadev_path = (luadev
    and ('LUA_DEV/?.lua;LUA_DEV/?/init.lua;LUA_DEV/lua/?.lua;LUA_DEV/lua/?/init.lua')
      :gsub('LUA_DEV', (luadev:gsub('[\\/]$','')))
    or nil)
  local luadev_cpath = (luadev
    and ('LUA_DEV/?.dll;LUA_DEV/?51.dll;LUA_DEV/clibs/?.dll;LUA_DEV/clibs/?51.dll')
      :gsub('LUA_DEV', (luadev:gsub('[\\/]$','')))
    or nil)

  if luadev then
    local path, clibs = os.getenv('PATH'), luadev:gsub('[\\/]$','')..'\\clibs'
    if not path:find(clibs, 1, true) then wx.wxSetEnv('PATH', path..';'..clibs) end
  end

  -- (luaconf.h) in Windows, any exclamation mark ('!') in the path is replaced
  -- by the path of the directory of the executable file of the current process.
  -- this effectively prevents any path with an exclamation mark from working.
  -- if the path has an excamation mark, allow Lua to expand it as this
  -- expansion happens only once.
  if osname == "Windows" and mainpath:find('%!') then mainpath = "!/../" end

  -- if LUA_PATH or LUA_CPATH is not specified, then add ;;
  -- ;; will be replaced with the default (c)path by the Lua interpreter
  wx.wxSetEnv("LUA_PATH",
    (os.getenv("LUA_PATH") or ';') .. ';'
    .. "./?.lua;./?/init.lua;./lua/?.lua;./lua/?/init.lua" .. ';'
    .. mainpath.."lualibs/?/?.lua;"..mainpath.."lualibs/?.lua;"
    .. mainpath.."lualibs/?/?/init.lua;"..mainpath.."lualibs/?/init.lua"
    .. (luadev_path and (';' .. luadev_path) or ''))

  ide.osclibs = -- keep the list to use for other Lua versions
    osname == "Windows" and mainpath.."bin/?.dll;"..mainpath.."bin/clibs/?.dll" or
    osname == "Macintosh" and mainpath.."bin/lib?.dylib;"..mainpath.."bin/clibs/?.dylib" or
    osname == "Unix" and mainpath..("bin/linux/%s/lib?.so;"):format(arch)
                       ..mainpath..("bin/linux/%s/clibs/?.so"):format(arch) or
    assert(false, "Unexpected OS name")

  wx.wxSetEnv("LUA_CPATH",
    (os.getenv("LUA_CPATH") or ';') .. ';' .. ide.osclibs
    .. (luadev_cpath and (';' .. luadev_cpath) or ''))
end

ide.test.setLuaPaths = setLuaPaths

---------------
-- process args
local filenames = {}
local configs = {}
do
  local arg = {...}
  -- application name is expected as the first argument
  local fullPath = arg[1] or "zbstudio"

  ide.arg = arg

  -- on Windows use GetExecutablePath, which is Unicode friendly,
  -- whereas wxGetCwd() is not (at least in wxlua 2.8.12.2).
  -- some wxlua version on windows report wx.dll instead of *.exe.
  local exepath = wx.wxStandardPaths.Get():GetExecutablePath()
  if ide.osname == "Windows" and exepath:find("%.exe$") then
    fullPath = exepath
  elseif not wx.wxIsAbsolutePath(fullPath) then
    fullPath = wx.wxGetCwd().."/"..fullPath
  end

  ide.editorFilename = fullPath
  ide.appname = fullPath:match("([%w_-%.]+)$"):gsub("%.[^%.]*$","")
  assert(ide.appname, "no application path defined")

  for index = 2, #arg do
    if (arg[index] == "-cfg" and index+1 <= #arg) then
      table.insert(configs,arg[index+1])
    elseif arg[index-1] ~= "-cfg"
    -- on OSX command line includes -psn... parameter, don't include these
    and (ide.osname ~= 'Macintosh' or not arg[index]:find("^-psn")) then
      table.insert(filenames,arg[index])
    end
  end

  setLuaPaths(GetPathWithSep(ide.editorFilename), ide.osname)
end

----------------------
-- process application

ide.app = dofile(ide.appname.."/app.lua")
local app = assert(ide.app)

local function loadToTab(filter, folder, tab, recursive, proto)
  if filter and type(filter) ~= 'function' then
    filter = app.loadfilters[filter] or nil
  end
  for _, file in ipairs(FileSysGetRecursive(folder, recursive, "*.lua")) do
    if not filter or filter(file) then
      LoadLuaFileExt(tab, file, proto)
    end
  end
end

local function loadInterpreters(filter)
  loadToTab(filter or "interpreters", "interpreters", ide.interpreters, false,
    ide.proto.Interpreter)
end

-- load tools
local function loadTools(filter)
  loadToTab(filter or "tools", "tools", ide.tools, false)
end

-- load packages
local function loadPackages(filter)
  loadToTab(filter, "packages", ide.packages, false, ide.proto.Plugin)
  if ide.oshome then
    local userpackages = MergeFullPath(ide.oshome, "."..ide.appname.."/packages")
    if wx.wxDirExists(userpackages) then
      loadToTab(filter, userpackages, ide.packages, false, ide.proto.Plugin)
    end
  end

  -- check dependencies and assign file names to each package
  local unload = {}
  for fname, package in pairs(ide.packages) do
    if type(package.dependencies) == 'table'
    and package.dependencies.osname
    and not package.dependencies.osname:find(ide.osname, 1, true) then
      (DisplayOutputLn or print)(
        ("Package '%s' not loaded: requires %s platform, but you are running %s.")
          :format(fname, package.dependencies.osname, ide.osname)
      )
      table.insert(unload, fname)
    end

    local needsversion = tonumber(package.dependencies)
      or type(package.dependencies) == 'table' and tonumber(package.dependencies[1])
      or -1
    local isversion = tonumber(ide.VERSION)
    if isversion and needsversion > isversion then
      (DisplayOutputLn or print)(
        ("Package '%s' not loaded: requires version %s, but you are running version %s.")
          :format(fname, needsversion, ide.VERSION)
      )
      table.insert(unload, fname)
    end
    package.fname = fname
  end

  -- remove packages that need to be unloaded
  for _, fname in ipairs(unload) do ide.packages[fname] = nil end
end

function UpdateSpecs()
  for _, spec in pairs(ide.specs) do
    spec.sep = spec.sep or "\1" -- default separator doesn't match anything
    spec.iscomment = {}
    spec.iskeyword0 = {}
    spec.isstring = {}
    if (spec.lexerstyleconvert) then
      if (spec.lexerstyleconvert.comment) then
        for _, s in pairs(spec.lexerstyleconvert.comment) do
          spec.iscomment[s] = true
        end
      end
      if (spec.lexerstyleconvert.keywords0) then
        for _, s in pairs(spec.lexerstyleconvert.keywords0) do
          spec.iskeyword0[s] = true
        end
      end
      if (spec.lexerstyleconvert.stringtxt) then
        for _, s in pairs(spec.lexerstyleconvert.stringtxt) do
          spec.isstring[s] = true
        end
      end
    end
  end
end

-- load specs
local function loadSpecs(filter)
  loadToTab(filter or "specs", "spec", ide.specs, true)
  UpdateSpecs()
end

-- temporarily replace print() to capture reported error messages to show
-- them later in the Output window after everything is loaded.
local resumePrint do
  local errors = {}
  local origprint = print
  print = function(...) errors[#errors+1] = {...} end
  resumePrint = function()
    print = origprint
    for _, e in ipairs(errors) do DisplayOutputLn(unpack(e)) end
  end
end

function GetIDEString(keyword, default)
  return app.stringtable[keyword] or default or keyword
end

----------------------
-- process config

-- set ide.config environment
setmetatable(ide.config, {__index = {os = os, wxstc = wxstc, wx = wx, ID = ID}})
ide.config.load = { interpreters = loadInterpreters, specs = loadSpecs,
  tools = loadTools }
do
  local num = 0
  ide.config.package = function(p)
    if p then
      num = num + 1
      local name = 'config'..num..'package'
      ide.packages[name] = setmetatable(p, ide.proto.Plugin)
    end
  end
end

LoadLuaConfig(ide.appname.."/config.lua")

ide.editorApp:SetAppName(GetIDEString("settingsapp"))

-- check if the .ini file needs to be migrated on Windows
if ide.osname == 'Windows' and ide.wxver >= "2.9.5" then
  -- Windows used to have local ini file kept in wx.wxGetHomeDir (before 2.9),
  -- but since 2.9 it's in GetUserConfigDir(), so migrate it.
  local ini = ide.editorApp:GetAppName() .. ".ini"
  local old = wx.wxFileName(wx.wxGetHomeDir(), ini)
  local new = wx.wxFileName(wx.wxStandardPaths.Get():GetUserConfigDir(), ini)
  if old:FileExists() and not new:FileExists() then
    FileCopy(old:GetFullPath(), new:GetFullPath())
    print(("Migrated configuration file from '%s' to '%s'.")
      :format(old:GetFullPath(), new:GetFullPath()))
  end
end

----------------------
-- process plugins

if app.preinit then app.preinit() end

loadInterpreters()
loadSpecs()
loadTools()

do
  ide.configs = {
    system = MergeFullPath("cfg", "user.lua"),
    user = ide.oshome and MergeFullPath(ide.oshome, "."..ide.appname.."/user.lua"),
  }

  -- process configs
  LoadLuaConfig(ide.configs.system)
  LoadLuaConfig(ide.configs.user)

  -- process all other configs (if any)
  for _, v in ipairs(configs) do LoadLuaConfig(v, true) end
  configs = nil

  -- check and apply default styles in case a user resets styles in the config
  for _, styles in ipairs({"styles", "stylesoutshell"}) do
    if not ide.config[styles] then
      print(("Ignored incorrect value of '%s' setting in the configuration file")
        :format(styles))
      ide.config[styles] = StylesGetDefault()
    end
  end

  local sep = GetPathSeparator()
  if ide.config.language then
    LoadLuaFileExt(ide.config.messages, "cfg"..sep.."i18n"..sep..ide.config.language..".lua")
  end
  -- always load 'en' as it's requires as a fallback for pluralization
  if ide.config.language ~= 'en' then
    LoadLuaFileExt(ide.config.messages, "cfg"..sep.."i18n"..sep.."en.lua")
  end
end

loadPackages()

---------------
-- Load App

for _, file in ipairs({
    "settings", "singleinstance", "iofilters", "package", "markup",
    "gui", "filetree", "output", "debugger", "outline", "commandbar",
    "editor", "findreplace", "commands", "autocomplete", "shellbox",
    "menu_file", "menu_edit", "menu_search",
    "menu_view", "menu_project", "menu_tools", "menu_help",
    "inspect" }) do
  dofile("src/editor/"..file..".lua")
end

-- register all the plugins
PackageEventHandle("onRegister")

-- initialization that was delayed until configs processed and packages loaded
ProjectUpdateInterpreters()

-- load rest of settings
SettingsRestoreEditorSettings()
SettingsRestoreFramePosition(ide.frame, "MainFrame")
SettingsRestoreFileHistory(SetFileHistory)
SettingsRestoreProjectSession(FileTreeSetProjects)
SettingsRestoreFileSession(function(tabs, params)
  if params and params.recovery
  then return SetOpenTabs(params)
  else return SetOpenFiles(tabs, params) end
end)
SettingsRestoreView()

-- ---------------------------------------------------------------------------
-- Load the filenames

do
  for _, filename in ipairs(filenames) do
    if filename ~= "--" then
      if wx.wxDirExists(filename) then
        ProjectUpdateProjectDir(filename)
      elseif not ActivateFile(filename) then
        DisplayOutputLn(("Can't open file '%s': %s"):format(filename, wx.wxSysErrorMsg()))
      end
    end
  end
  if ide:GetEditorNotebook():GetPageCount() == 0 then NewFile() end
end

if app.postinit then app.postinit() end

-- this is a workaround for a conflict between global shortcuts and local
-- shortcuts (like F2) used in the file tree or a watch panel.
-- because of several issues on OSX (as described in details in this thread:
-- https://groups.google.com/d/msg/wx-dev/juJj_nxn-_Y/JErF1h24UFsJ),
-- the workaround installs a global event handler that manually re-routes
-- conflicting events when the current focus is on a proper object.
-- non-conflicting shortcuts are handled through key-down events.
local remap = {
  [ID_ADDWATCH]    = ide:GetWatch(),
  [ID_EDITWATCH]   = ide:GetWatch(),
  [ID_DELETEWATCH] = ide:GetWatch(),
  [ID_RENAMEFILE]  = ide:GetProjectTree(),
  [ID_DELETEFILE]  = ide:GetProjectTree(),
}

local function rerouteMenuCommand(obj, id)
  -- check if the conflicting shortcut is enabled:
  -- (1) SetEnabled wasn't called or (2) Enabled was set to `true`.
  local uievent = wx.wxUpdateUIEvent(id)
  obj:ProcessEvent(uievent)
  if not uievent:GetSetEnabled() or uievent:GetEnabled() then
    obj:AddPendingEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, id))
  end
end

local function remapkey(event)
  local keycode = event:GetKeyCode()
  local mod = event:GetModifiers()
  for id, obj in pairs(remap) do
    local focus = obj:FindFocus()
    if focus and focus:GetId() == obj:GetId() then
      local ae = wx.wxAcceleratorEntry(); ae:FromString(KSC(id))
      if ae:GetFlags() == mod and ae:GetKeyCode() == keycode then
        rerouteMenuCommand(obj, id)
        return
      end
    end
  end
  event:Skip()
end
ide:GetWatch():Connect(wx.wxEVT_KEY_DOWN, remapkey)
ide:GetProjectTree():Connect(wx.wxEVT_KEY_DOWN, remapkey)

local function resolveConflict(localid, globalid)
  return function(event)
    local shortcut = ide.config.keymap[localid]
    for id, obj in pairs(remap) do
      if ide.config.keymap[id]:lower() == shortcut:lower() then
        local focus = obj:FindFocus()
        if focus and focus:GetId() == obj:GetId() then
          obj:AddPendingEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, id))
          return
        -- also need to check for children of objects
        -- to avoid re-triggering events when labels are being edited
        elseif focus and focus:GetParent():GetId() == obj:GetId() then
          return
        end
      end
    end
    rerouteMenuCommand(ide.frame, globalid)
  end
end

local at = {}
for lid in pairs(remap) do
  local shortcut = ide.config.keymap[lid]
  -- find a (potential) conflict for this shortcut (if any)
  for gid, ksc in pairs(ide.config.keymap) do
    -- if the same shortcut is used elsewhere (not one of IDs being checked)
    if shortcut:lower() == ksc:lower() and not remap[gid] then
      local fakeid = NewID()
      ide.frame:Connect(fakeid, wx.wxEVT_COMMAND_MENU_SELECTED,
        resolveConflict(lid, gid))

      local ae = wx.wxAcceleratorEntry(); ae:FromString(ksc)
      table.insert(at, wx.wxAcceleratorEntry(ae:GetFlags(), ae:GetKeyCode(), fakeid))
    end
  end
end

if ide.osname == 'Macintosh' then
  table.insert(at, wx.wxAcceleratorEntry(wx.wxACCEL_CTRL, ('M'):byte(), ID_VIEWMINIMIZE))
end
ide.frame:SetAcceleratorTable(wx.wxAcceleratorTable(at))

-- only set menu bar *after* postinit handler as it may include adding
-- app-specific menus (Help/About), which are not recognized by MacOS
-- as special items unless SetMenuBar is done after menus are populated.
ide.frame:SetMenuBar(ide.frame.menuBar)

resumePrint()

PackageEventHandle("onAppLoad")

-- The status bar content is drawn incorrectly if it is shown
-- after being initially hidden.
-- Show the statusbar and hide it after showing the frame, which fixes the issue.
local statusbarfix = ide.osname == 'Windows' and not ide.frame:GetStatusBar():IsShown()
if statusbarfix then ide.frame:GetStatusBar():Show(true) end

ide.frame:Show(true)

if statusbarfix then ide.frame:GetStatusBar():Show(false) end

-- somehow having wxAuiToolbar "steals" the focus from the editor on OSX;
-- have to set the focus implicitly on the current editor (if any)
if ide.osname == 'Macintosh' then
  local editor = GetEditor()
  if editor then editor:SetFocus() end
end

wx.wxGetApp():MainLoop()

-- There are several reasons for this call:
-- (1) to fix a crash on OSX when closing with debugging in progress.
-- (2) to fix a crash on Linux 32/64bit during GC cleanup in wxlua
-- after an external process has been started from the IDE.
-- (3) to fix exit on Windows when started as "bin\lua src\main.lua".
os.exit()
