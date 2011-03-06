return {
		name = "Lua",
		description = "Commandline Lua interpreter",
		api = {"wx","baselib"},
		fcmdline = function(filepath) 
				local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
				local code = ([[
					xpcall(function() dofile '%s' end,
						function(err) print(debug.traceback(err)) end)
				]]):format(filepath:gsub("\\","/"))
				return '"'..mainpath..'/bin/lua.exe" -e "'..code..'"'
			end,
		fprojdir = function(fname) 
				return fname:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		capture = true,
		fworkdir = function (filepath) 
			return ide.config.path.projectdir and ide.config.path.projectdir:len()>0 and 
					ide.config.path.projectdir
			end,
			--return filepath and filepath:gsub("[\\/]+$","") end,
	}