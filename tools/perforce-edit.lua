-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exec = {
    name = "Perforce edit",
    description = "does p4 edit",
    fn = function(wxfname,projectdir)
      local cmd = 'p4 edit "'..wxfname:GetFullPath()..'"'

      CommandLineRun(cmd,nil,true)
    end,
  },
}
