--[[-- Copy required content from this file to `user.lua`

Configuration files are loaded in the following order

1. <application>\config.lua
2. cfg\user.lua
3. ~\.zbs\user.lua
4. -cfg commandline strings

--]]--

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
path.love2d = 'd:/lua/love/love'

-- specify full path to moai executable if it's not in one of PATH folders
path.moai = 'd:/lua/moai/moai'

-- specify full path to lua interpreter if you need to use your own version
path.lua = 'd:/lua/lua'

-- fix an issue with 0d0d0a line endings in MOAI examples,
-- which may negatively affect breakpoints during debugging
editor.iofilter = "0d0d0aFix"

-- to have 4 spaces when TAB is used in the editor
editor.tabwidth = 4

-- to have TABs stored in the file (to allow mixing tabs and spaces)
editor.usetabs  = true

-- to disable wrapping of long lines in the editor
editor.usewrap = false

-- to turn dynamic words on and to start suggestions after 4 characters
acandtip.nodynwords = false
acandtip.startat = 4

-- specify a list of MOAI entrypoints
moai = { entrypoints = { "main.lua", "source/main.lua" } }
