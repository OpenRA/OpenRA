-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
	exec = {
		name = "Luxinia Viewer",
		description = "sends current file to luxinia viewer",
		fn = function(wxfname,projectdir)
			local endstr = projectdir and projectdir:len()>0
							and " -p "..projectdir or ""
				
			local cmd = ide.config.path.luxinia.."luxinia.exe --nologo"..endstr
			cmd = cmd.." -v "..wxfname:GetFullPath()
			
			RunCommandLine(cmd,nil,nil,true)
		end,
	},
}