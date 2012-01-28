return {
  name = "Estrela Shell",
  description = "Estrela Lua Shell",
  api = {"wxwidgets","baselib"},
  frun = function(self,wfilename)
    -- set shellbox for focus
    local bottomnotebook = ide.frame.bottomnotebook
    bottomnotebook:SetSelection(1)

    ShellExecuteFile(wfilename)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
}
