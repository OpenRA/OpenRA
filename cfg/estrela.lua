-- If you have used Estrela before, please copy this content
-- to your user.lua file in the cfg/ directory

-- load all tools, specs, and interpreters
local all = function() return true end
load.tools(all)
load.specs(all)
load.interpreters(all)

-- this flag means that toggling between projects, will not affect the 
-- list of opened files (old estrela default).
-- zbstudio defaults here to true, meaning that you get 
-- per-project list of openend files
projectautoopen = false

-- default search paths for luxinia
local luxpath = os.getenv("LUXINIA")
path.luxinia = luxpath and luxpath.."/" or "../luxinia/engine/"
local luxpath2 = os.getenv("LUXINIA2")
path.luxinia2 = luxpath2 and luxpath2.."/" or "../luxinia2/runtime/bin_Windows_x86/"
