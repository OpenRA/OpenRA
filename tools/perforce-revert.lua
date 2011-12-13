-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exec = {
    name = "Perforce revert",
    description = "does p4 revert",
    fn = function(wxfname,projectdir)
      local cmd = 'p4 revert "'..wxfname:GetFullPath()..'"'

      CommandLineRun(cmd,nil,true)
    end,
  },
}
