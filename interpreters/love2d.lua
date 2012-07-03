return {
  name = "Love2d",
  description = "Love2d game engine",
  api = {"baselib", "love2d"},
  frun = function(self,wfilename,rundebug)
    if rundebug then DebuggerAttachDefault() end
    local love2d = ide.config.path.love2d
      or wx.wxFileName(self:fprojdir(wfilename)):GetPath(wx.wxPATH_GET_VOLUME)
      .. '/love'
    local cmd = ('"%s" "%s"%s'):format(string.gsub(love2d, "\\","/"),
      self:fprojdir(wfilename), rundebug and ' -debug' or '')
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() ide.debugger.pid = nil end)
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
  scratchextloop = true,
}
