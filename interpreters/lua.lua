return {
		name = "Lua",
		description = "Commandline Lua interpreter",
		api = {"wxwidgets","baselib"},
		frun = function(self,wfilename) 
				local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
				local filepath = wfilename:GetFullPath()
				local code = ([[
					xpcall(function() dofile '%s' end,
						function(err) print(debug.traceback(err)) end)
				]]):format(filepath:gsub("\\","/"))
				local cmd = '"'..mainpath..'/bin/lua.exe" -e "'..code..'"'
				CommandLineRun(cmd,self:fworkdir(wfilename),true,false)
			end,
		fprojdir = function(self,wfilename) 
				return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		fworkdir = function (self,wfilename) 
			return ide.config.path.projectdir and ide.config.path.projectdir:len()>0 and 
					ide.config.path.projectdir
			end,
			--return filepath and filepath:gsub("[\\/]+$","") end,
	}