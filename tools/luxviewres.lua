-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exec = {
    name = "Luxinia Viewer",
    description = "sends current file to luxinia viewer",
    fn = function(wxfname,projectdir)
      if not ide.config.path.luxinia then wx.wxMessageBox("Please define 'path.luxinia' in your cfg/user.lua  (see estrela.lua for examples)"); return end
      local endstr = projectdir and projectdir:len()>0
      and " -p "..projectdir or ""

      local cmd = ide.config.path.luxinia.."luxinia.exe --nologo"..endstr
      cmd = cmd.." -v "..wxfname:GetFullPath()

      CommandLineRun(cmd,nil,nil,true)
    end,
  },
}
