return {
		name = "Lua Debug",
		description = "Commandline Lua interpreter",
		api = {"wxwidgets","baselib"},
		frun = function(self,wfilename,rundebug)
			local mainpath = ide.editorFilename:gsub("[^/\\]+$","")
			local filepath = wfilename:GetFullPath()
			local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
			local script
			if rundebug then
				DebuggerAttachDefault()
				script = ""..
					"package.path=package.path..';"..editorDir.."lualibs/?/?.lua';"..
					"package.cpath=package.cpath..';"..editorDir.."bin/clibs/?.dll';"..
					"require 'mobdebug'; io.stdout:setvbuf('no'); mobdebug.loop('" .. wx.wxGetHostName().."',"..ide.debugger.portnumber..")"
			else
				script = ([[dofile '%s']]):format(filepath:gsub("\\","/")) 
			end
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
		fattachdebug = function(self) 
			DebuggerAttachDefault()
		end,
	}
