return {
  name = "Estrela Editor",
  description = "Estrela Editor as run target (IDE development)",
  api = {"wxwidgets","baselib"},
  frun = function(self,wfilename)
    local cmd = ide.editorFilename and '"'..ide.editorFilename..'" '..(wfilename and wfilename:GetFullPath() or "")..' -cfg "singleinstance=false;"' or nil
    CommandLineRun(cmd,nil,false,true)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
}
