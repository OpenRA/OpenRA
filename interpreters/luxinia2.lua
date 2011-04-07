return {
		name = "Luxinia2",
		description = "Luxinia2",
		api = {"baselib","cg30","cggl30","glfw3","glewgl"},
		
		finitclient = function(self)
			if (not CommandLineRunning(self:fuid(wfilename))) then return end
			local init = dofile(ide.config.path.luxinia2.."/../comserver/client.lua")
			local fenv = {}
			setmetatable(fenv,{__index = _G})
			fenv.print = function(...) DisplayOutput(...); DisplayOutput("\n"); end
			
			setfenv(init,fenv)
			local client = init()
			
			self.fclient = client
			return client
		end,
		
		frun = function(self,wfilename)
				local luxdir  = ide.config.path.luxinia2
				local projdir = ide.config.path.projectdir
				assert(projdir and projdir:len()>0,"no project directory")
				local args = " -e "..projdir.."/main.lua"
				
				if (CommandLineRunning(self:fuid(wfilename))) then
					if (not self.fclient) then
						self:finitclient()
					end
					-- try to communicate with server
					self.fclient("dofile([["..wfilename:GetFullPath().."]])")
					return
				end
				
				self.fclient = nil
				local fname = wfilename:GetFullName()
				args = args..(fname and (" -f "..fname) or "")
				
				local cmd = luxdir..'/luajit.exe ../main.lua -s'..args
				
				if(CommandLineRun(cmd,ide.config.path.luxinia2,true,true,nil,self:fuid(wfilename),
					function() ShellSupportRemote(nil) end)) then return end
				
				local client = self:finitclient()
				ShellSupportRemote(client,self:fuid(wfilename))
			end,
		fuid = function(self,wfilename) return "luxinia2 "..(ide.config.path.projectdir or "") end,
		fprojdir = function(self,wfilename)
				local path = GetPathWithSep(wfilename)
				filepath = wx.wxFileName(path)
				
				while ((not wx.wxFileExists(path.."main.lua")) and (filepath:GetDirCount() > 0)) do
					filepath:RemoveDir(filepath:GetDirCount()-1)
					path = GetPathWithSep(filepath)
				end
				
				return path:sub(0,-2)
			end,
	}