local LPATH = os.getenv('LUA_PATH')
local LCPATH = os.getenv('LUA_CPATH')
local LCPATH52 = os.getenv('LUA_CPATH_5_2')
local mainpath = GetPathWithSep(ide.editorFilename)

-- LUA_(C)PATH is specified, no ;; is added
wx.wxSetEnv('LUA_PATH', 'foo')
wx.wxSetEnv('LUA_CPATH', 'bar')
ide.test.setLuaPaths(mainpath, 'Windows')
ok(os.getenv('LUA_PATH'), "LUA_PATH is set.")
ok(os.getenv('LUA_CPATH'), "LUA_CPATH is set.")

ok(not (os.getenv('LUA_PATH') or ""):find(';;'),
  "No ;; is added when LUA_PATH is specified.")
ok(not (os.getenv('LUA_CPATH') or ""):find(';;'),
  "No ;; is added when LUA_CPATH is specified.")

-- LUA_(C)PATH is not specified, ;; is added at the beginning
wx.wxSetEnv('LUA_PATH', '')
wx.wxSetEnv('LUA_CPATH', '')
ide.test.setLuaPaths(mainpath, 'Windows')
ok((os.getenv('LUA_PATH') or ""):find(';;'),
  ";; is added when LUA_PATH is not specified.")
ok((os.getenv('LUA_CPATH') or ""):find(';;'),
  ";; is added when LUA_CPATH is not specified.")

-- ide.osclibs are added
ok((os.getenv('LUA_CPATH') or ""):find(ide.osclibs, 1, true),
  "OS clibs is included in LUA_CPATH.")
ok(ide.osclibs:find('/clibs/'),
  "OS clibs includes '/clibs/' folder.")

local luadev = MergeFullPath(GetPathWithSep(ide.editorFilename), '../'):gsub('[\\/]$','')

-- LUA_DEV is not used on non-Windows
wx.wxSetEnv('LUA_PATH', 'foo')
wx.wxSetEnv('LUA_CPATH', 'bar')
ide.test.setLuaPaths(mainpath, 'Unix')
ok(not os.getenv('LUA_PATH'):find(luadev..'/?.lua', 1, true),
  "LUA_DEV is not used in LUA_PATH on non-Windows.")
ok(not os.getenv('LUA_CPATH'):find(luadev..'/?51.dll', 1, true),
  "LUA_DEV is not used in LUA_CPATH on non-Windows.")
wx.wxSetEnv('LUA_DEV', '')

-- LUA_DEV is used on Windows
wx.wxSetEnv('LUA_PATH', 'foo')
wx.wxSetEnv('LUA_CPATH', 'bar')
wx.wxSetEnv('LUA_DEV', luadev)
ide.test.setLuaPaths(mainpath, 'Windows')
ok(os.getenv('LUA_PATH'):find(luadev..'/?.lua', 1, true), "LUA_DEV is used in LUA_PATH on Windows.")
ok(os.getenv('LUA_CPATH'):find(luadev..'/?51.dll', 1, true), "LUA_DEV is used in LUA_CPATH on Windows.")

-- stub CommandLineRun and check if interpreters set paths correctly
local CLR = CommandLineRun
local lp, lcp
local fn = wx.wxFileName("foo")
_G.CommandLineRun = function(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
  lp, lcp, lcp52 = os.getenv('LUA_PATH'), os.getenv('LUA_CPATH'), os.getenv('LUA_CPATH_5_2')
  if endcallback then endcallback() end
  return
end
ide.interpreters.luadeb:frun(fn, "")
ok(lcp:find(ide.osclibs, 1, true) == 1,
  "Prepend clibs to LUA_CPATH if path.lua is not set.")

local CPL = ide.config.path.lua
ide.config.path.lua = "foo"
ide.interpreters.luadeb:frun(fn, "")
ok(lcp:find(ide.osclibs, 1, true) ~= 1,
  "Don't prepend clibs to LUA_CPATH if path.lua is set.")
ide.config.path.lua = CPL

-- LUA_CPATH_5_2 modified if it's already set
wx.wxSetEnv('LUA_CPATH_5_2', 'foo')
ide.interpreters.luadeb52:frun(fn, "")
ok(lcp:find(ide.osclibs, 1, true) ~= 1,
  "LUA_CPATH_5_2 is modified if it is already set.")

_G.CommandLineRun = CLR

wx.wxSetEnv('LUA_PATH', LPATH or "")
wx.wxSetEnv('LUA_CPATH', LCPATH or "")
wx.wxSetEnv('LUA_CPATH_5_2', LCPATH52 or "")
ide.test.setLuaPaths(mainpath, ide.osname)
