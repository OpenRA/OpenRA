return {
		name = "Lua with remote debugger",
		description = "Commandline Lua interpreter",
		api = {"wxwidgets","baselib"},
		frun = function(self,wfilename,script)
				local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
				local filepath = wfilename:GetFullPath()
				if not script then script = ([[dofile '%s']]):format(filepath:gsub("\\","/")) end
				local code = ([[xpcall(function() %s end,function(err) print(debug.traceback(err)) end)]]):format(script)
				local cmd = '"'..mainpath..'/bin/lua.exe" -e "'..code..'"'
				CommandLineRun(cmd,self:fworkdir(wfilename),true,false)
			end,
		fprojdir = function(self,wfilename) 
                                return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		fworkdir = function (self,wfilename) 
				return ide.config.path.projectdir
                                    or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		hasdebugger = true,
	}
