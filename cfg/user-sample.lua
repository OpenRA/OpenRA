--[[-- Rename this file to `user.lua` to get loaded

Configuration files are loaded in the following order

1. <application>\config.lua
2. cfg\user.lua
3. ~\.zbs\user.lua
4. -cfg commandline strings

-- an example of how loaded configuration can be modified from this file

local G = ... -- this now points to the global environment in the script
local luaspec = G.ide.specs['lua']
luaspec.exts[2] = "luaz"
luaspec.keywords[1] = luaspec.keywords[1] .. ' foo'

-- these changes are going to be mapped to ide.config.editor...
-- change encoding to Cyrillic
editor.fontencoding = G.wx.wxFONTENCODING_ISO8859_5
-- or WinCyrillic
editor.fontencoding = G.wx.wxFONTENCODING_CP1251
outputshell.fontencoding = G.wx.wxFONTENCODING_CP1251

-- specify full path to love2d executable; this is only needed
-- if the game folder and the executable are NOT in the same folder.
path.love2d = 'd:/lua/love/love' -- set the path of love executable

--]]--
