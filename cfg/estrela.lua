-- If you have used Estrela before, please copy this content
-- to your user.lua file in the cfg/ directory

local G = ... -- point to the global environment in the script
-- update app configuration to load all tools, specs, and interpreters
G.ide.app.loadfilters.tools = function() return true end
G.ide.app.loadfilters.specs = function() return true end
G.ide.app.loadfilters.interpreters = function() return true end

-- this flag means that toggling between projects, will not affect the 
-- list of openend files (old estrela default). 
-- zbstudio defaults here to true, meaning that you get 
-- per-project list of openend files
projectautoopen = false