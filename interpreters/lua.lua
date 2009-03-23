return {
		name = "Lua",
		description = "Commandline Lua interpreter",
		fcmdline = function(filepath) 
				local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
				local code = ([[
					--require 'lfs'
					xpcall(function() dofile '%s' end,
						function(err) print(debug.traceback(err)) end)
				]]):format(filepath:gsub("\\","/"))
				return '"'..mainpath..'/lualibs/lua" -e "'..code..'"'
			end,
		fprojdir = function(fname) 
				return ide.editorFilename..'/lualibs/' --fname:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		capture = true,
		fworkdir = function (filepath) 
			return ide.config.path.projectdir and ide.config.path.projectdir:len()>0 and 
					ide.config.path.projectdir
			end,
			--return filepath and filepath:gsub("[\\/]+$","") end,
	}