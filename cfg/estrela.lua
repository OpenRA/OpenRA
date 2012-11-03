-- If you have used Estrela before, please copy this content
-- to your user.lua file in the cfg/ directory

-- load all tools, specs, and interpreters
local all = function() return true end
load.tools(all)
load.specs(all)
load.interpreters(all)

-- this flag means that toggling between projects, will not affect the 
-- list of openend files (old estrela default). 
-- zbstudio defaults here to true, meaning that you get 
-- per-project list of openend files
projectautoopen = false
