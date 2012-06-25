-- specify full path to love2d executable; this is only needed
-- if the game folder and the executable are NOT in the same folder.
local love2d -- = "d:/lua/love/love"
return {
  name = "Love2d",
  description = "Love2d game engine",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    if rundebug then DebuggerAttachDefault() end
    local love2d = love2d
      or wx.wxFileName(self:fprojdir(wfilename)):GetPath(wx.wxPATH_GET_VOLUME)
      .. '/love'
    local cmd = string.gsub(love2d, "\\","/") .. ' "' .. self:fprojdir(wfilename) .. '"'
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false)
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
}
