-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- put bin/ and lualibs/ first to avoid conflicts with included modules
-- that may have other versions present somewhere else in path/cpath.
local iswindows = os.getenv('WINDIR') or (os.getenv('OS') or ''):match('[Ww]indows')
local islinux = not iswindows and not os.getenv('DYLD_LIBRARY_PATH') and io.open("/proc")
local arch = "x86" -- use 32bit by default
local unpack = table.unpack or unpack

if islinux then
  local file = io.popen("uname -m")
  if file then
    arch = file:read("*a"):find("x86_64") and "x64" or "x86"
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
  config = {
    path = {
      projectdir = "",
      app = nil,
    },
    editor = {
      foldcompact = true,
      checkeol = true,
    },
    debugger = {
      verbose = false,
      hostname = nil,
      port = nil,
      runonstart = nil,
      redirect = nil,
    },
    default = {
      name = 'untitled',
      fullname = 'untitled.lua',
      interpreter = 'luadeb',
    },
    outputshell = {},
    filetree = {},
    funclist = {},

    keymap = {},
    messages = {},
    language = "en",

    styles = nil,
    stylesoutshell = nil,

    autocomplete = true,
    autoanalizer = true,
    acandtip = {
      shorttip = false,
      ignorecase = false,
      strategy = 2,
      width = 60,
    },

    activateoutput = false, -- activate output/console on Run/Debug/Compile
    unhidewindow = false, -- to unhide a gui window
    allowinteractivescript = false, -- allow interaction in the output window
    filehistorylength = 20,
    projecthistorylength = 15,
    savebak = false,
    singleinstance = false,
    singleinstanceport = 0xe493,
    -- HiDPI/Retina display support;
    -- `false` by default because of issues with indicators with alpha setting
    hidpi = false,
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
    dNormal = nil,
  },

  osname = wx.wxPlatformInfo.Get():GetOperatingSystemFamilyName(),
  osarch = arch,
  oshome = os.getenv("HOME") or (iswindows and os.getenv('HOMEDRIVE') and os.getenv('HOMEPATH')
    and (os.getenv('HOMEDRIVE')..os.getenv('HOMEPATH'))),
  wxver = string.match(wx.wxVERSION_STRING, "[%d%.]+"),
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

for _, file in ipairs({"ids", "style", "keymap", "proto"}) do
  dofile("src/editor/"..file..".lua")
end

ide.config.styles = StylesGetDefault()
ide.config.stylesoutshell = StylesGetDefault()

local function setLuaPaths(mainpath, osname)
  -- use LUA_DEV to setup paths for Lua for Windows modules if installed
  local luadev = osname == "Windows" and os.getenv('LUA_DEV')
  local luadev_path = (luadev and wx.wxDirExists(luadev)
    and ('LUA_DEV/?.lua;LUA_DEV/?/init.lua;LUA_DEV/lua/?.lua;LUA_DEV/lua/?/init.lua')
      :gsub('LUA_DEV', (luadev:gsub('[\\/]$','')))
    or "")
  local luadev_cpath = (luadev and wx.wxDirExists(luadev)
    and ('LUA_DEV/?.dll;LUA_DEV/?51.dll;LUA_DEV/clibs/?.dll;LUA_DEV/clibs/?51.dll')
      :gsub('LUA_DEV', (luadev:gsub('[\\/]$','')))
    or "")

  -- (luaconf.h) in Windows, any exclamation mark ('!') in the path is replaced
  -- by the path of the directory of the executable file of the current process.
  -- this effectively prevents any path with an exclamation mark from working.
  -- if the path has an excamation mark, allow Lua to expand it as this
  -- expansion happens only once.
  if osname == "Windows" and mainpath:find('%!') then mainpath = "!/../" end
  wx.wxSetEnv("LUA_PATH", package.path .. ";"
    .. "./?.lua;./?/init.lua;./lua/?.lua;./lua/?/init.lua" .. ';'
    .. mainpath.."lualibs/?/?.lua;"..mainpath.."lualibs/?.lua" .. ';'
    .. luadev_path)

  local clibs =
    osname == "Windows" and mainpath.."bin/?.dll;"..mainpath.."bin/clibs/?.dll" or
    osname == "Macintosh" and mainpath.."bin/lib?.dylib;"..mainpath.."bin/clibs/?.dylib" or
    osname == "Unix" and mainpath..("bin/linux/%s/lib?.so;"):format(arch)
                       ..mainpath..("bin/linux/%s/clibs/?.so"):format(arch) or
    nil
  if clibs then wx.wxSetEnv("LUA_CPATH",
    package.cpath .. ';' .. clibs .. ';' .. luadev_cpath) end
end

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
  ide.config.path.app = fullPath:match("([%w_-%.]+)$"):gsub("%.[^%.]*$","")
  assert(ide.config.path.app, "no application path defined")

  for index = 2, #arg do
    if (arg[index] == "-cfg" and index+1 <= #arg) then
      table.insert(configs,arg[index+1])
    elseif arg[index-1] ~= "-cfg" then
      table.insert(filenames,arg[index])
    end
  end

  setLuaPaths(GetPathWithSep(ide.editorFilename), ide.osname)
end

----------------------
-- process application

ide.app = dofile(ide.config.path.app.."/app.lua")
local app = assert(ide.app)

local function loadToTab(filter, folder, tab, recursive, proto)
  filter = filter and type(filter) ~= 'function' and app.loadfilters[filter] or nil
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
    local userpackages = MergeFullPath(ide.oshome, ".zbstudio/packages")
    loadToTab(filter, userpackages, ide.packages, false, ide.proto.Plugin)
  end
  -- assign file names to each package
  for fname, package in pairs(ide.packages) do package.fname = fname end
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

-----------------------
-- load config
local function addConfig(filename,isstring)
  if not filename then return end
  -- skip those files that don't exist
  if not isstring and not wx.wxFileName(filename):FileExists() then return end
  -- if it's marked as command, but exists as a file, load it as a file
  if isstring and wx.wxFileName(filename):FileExists() then isstring = false end

  local cfgfn, err, msg
  if isstring
  then msg, cfgfn, err = "string", loadstring(filename)
  else msg, cfgfn, err = "file", loadfile(filename) end

  if not cfgfn then
    print(("Error while loading configuration %s: '%s'."):format(msg, err))
  else
    ide.config.os = os
    ide.config.wxstc = wxstc
    ide.config.load = { interpreters = loadInterpreters,
      specs = loadSpecs, tools = loadTools }
    setfenv(cfgfn,ide.config)
    local _, err = pcall(function()cfgfn(assert(_G or _ENV))end)
    if err then
      print(("Error while processing configuration %s: '%s'."):format(msg, err))
    end
  end
end

function GetIDEString(keyword, default)
  return app.stringtable[keyword] or default or keyword
end

----------------------
-- process config

addConfig(ide.config.path.app.."/config.lua")

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
    user = ide.oshome and MergeFullPath(ide.oshome, ".zbstudio/user.lua"),
  }

  -- process configs
  addConfig(ide.configs.system)
  addConfig(ide.configs.user)

  -- process all other configs (if any)
  for _, v in ipairs(configs) do addConfig(v, true) end

  configs = nil
  local sep = GetPathSeparator()
  if ide.config.language then
    LoadLuaFileExt(ide.config.messages, "cfg"..sep.."i18n"..sep..ide.config.language..".lua")
  end
end

loadPackages()

---------------
-- Load App

for _, file in ipairs({
    "markup", "settings", "singleinstance", "iofilters",
    "package", "gui", "filetree", "output", "debugger",
    "editor", "findreplace", "commands", "autocomplete", "shellbox",
    "menu_file", "menu_edit", "menu_search",
    "menu_view", "menu_project", "menu_tools", "menu_help",
    "inspect" }) do
  dofile("src/editor/"..file..".lua")
end

dofile "src/version.lua"

-- register all the plugins
PackageEventHandle("onRegister")

-- load rest of settings
SettingsRestoreEditorSettings()
SettingsRestoreFramePosition(ide.frame, "MainFrame")
SettingsRestoreFileHistory(SetFileHistory)
SettingsRestoreFileSession(function(tabs, params)
  if params and params.recovery
  then return SetOpenTabs(params)
  else return SetOpenFiles(tabs, params) end
end)
SettingsRestoreProjectSession(FileTreeSetProjects)
SettingsRestoreView()

-- ---------------------------------------------------------------------------
-- Load the filenames

do
  for _, fileName in ipairs(filenames) do
    if fileName ~= "--" then
      if wx.wxDirExists(fileName) then
        local dir = wx.wxFileName.DirName(fileName)
        dir:Normalize() -- turn into absolute path if needed
        ProjectUpdateProjectDir(dir:GetFullPath())
      else
        LoadFile(fileName, nil, true)
      end
    end
  end

  local notebook = ide.frame.notebook
  if notebook:GetPageCount() == 0 then NewFile() end
end

if app.postinit then app.postinit() end

-- only set menu bar *after* postinit handler as it may include adding
-- app-specific menus (Help/About), which are not recognized by MacOS
-- as special items unless SetMenuBar is done after menus are populated.
ide.frame:SetMenuBar(ide.frame.menuBar)
if ide.wxver < "2.9.5" and ide.osname == 'Macintosh' then -- force refresh to fix the filetree
  pcall(function() ide.frame:ShowFullScreen(true) ide.frame:ShowFullScreen(false) end)
end

resumePrint()

PackageEventHandle("onAppLoad")

ide.frame:Show(true)
wx.wxGetApp():MainLoop()

-- There are several reasons for this call:
-- (1) to fix a crash on OSX when closing with debugging in progress.
-- (2) to fix a crash on Linux 32/64bit during GC cleanup in wxlua
-- after an external process has been started from the IDE.
-- (3) to fix exit on Windows when started as "bin\lua src\main.lua".
os.exit()
