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

-- change font size to 12
editor.fontsize = 12 -- this is mapped to ide.config.editor.fontsize
filehistorylength = 20 -- this is mapped to ide.config.filehistorylength

-- specify full path to love2d executable; this is only needed
-- if the game folder and the executable are NOT in the same folder.
path.love2d = 'd:/lua/love/love' -- set the path of love executable

-- specify full path to moai executable if it's not in one of PATH folders
path.moai = 'd:/lua/moai/moai' -- set the path of moai executable

--]]--
