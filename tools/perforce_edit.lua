-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exec = {
    name = "Perforce edit",
    description = "does p4 edit",
    fn = function(fname,projectdir)
      local cmd = 'p4 edit "'..fname..'"'

      CommandLineRun(cmd,nil,true)
    end,
  },
}
