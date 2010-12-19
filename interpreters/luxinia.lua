return {
		name = "Luxinia",
		description = "Luxinia project",
		api = {"luxiniaapi","baselib"},
		fcmdline = function(filepath) 
				local projdir = ide.config.path.projectdir
				local endstr = projdir and projdir:len()>0
							and " -p "..projdir or ""
				
				local fname = GetFileNameExt(filepath)
				endstr = endstr..(fname and (" -t "..fname) or "")
				
				return ide.config.path.luxinia..'luxinia.exe --nologo'..endstr
			end,
		fworkdir = function() end, -- overriden by luxinia anyway
		capture = true,
		nohide  = true,
		fprojdir = function(fname)
				local path = GetPathWithSep(fname)
				fname = wx.wxFileName(path)
				
				while ((not wx.wxFileExists(path.."main.lua")) and (fname:GetDirCount() > 0)) do
					fname:RemoveDir(fname:GetDirCount()-1)
					path = GetPathWithSep(fname)
				end
				
				return path:sub(0,-2)
			end,
	}