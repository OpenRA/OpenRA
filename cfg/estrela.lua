-- If you have used Estrela for graphics shader authoring or luxinia,
-- create/modify the `user.lua` file in the current folder
-- and add `include "estrela"` (1.21+) to load all tools and specifications by default again.

-- load all tools, specs, and interpreters
local all = function() return true end
load.tools(all)
load.specs(all)
load.interpreters(all)

-- this flag means that toggling between projects, will not affect the 
-- list of opened files (old estrela default).
projectautoopen = false

-- default search paths for luxinia
local luxpath = os.getenv("LUXINIA")
path.luxinia = luxpath and luxpath.."/" or "../luxinia/engine/"
local luxpath2 = os.getenv("LUXINIA2")
path.luxinia2 = luxpath2 and luxpath2.."/" or "../luxinia2/runtime/bin_Windows_x86/"
