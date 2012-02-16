if (not(ide.config.path.luxinia and 
        wx.wxFileExists(ide.config.path.luxinia..'luxinia.exe'))) then 
  return 
end

return {
  name = "Luxinia",
  description = "Luxinia project",
  api = {"luxiniaapi","baselib"},
  frun = function(self,wfilename,withdebug)
    local projdir = ide.config.path.projectdir
    local endstr = (projdir and projdir:len()>0
      and " -p "..projdir or "")

    local fname = wfilename:GetFullName()
    endstr = endstr..(fname and (" -t "..fname) or "")

    local cmd = 'luxinia.exe --nologo'..endstr
    CommandLineRun(cmd,ide.config.path.luxinia,true,true)
  end,
  fuid = function(self,wfilename) return "luxinia "..(ide.config.path.projectdir or "") end,
  fprojdir = function(self,wfilename)
    local path = GetPathWithSep(wfilename)
    fname = wx.wxFileName(path)

    while ((not wx.wxFileExists(path.."main.lua")) and (fname:GetDirCount() > 0)) do
      fname:RemoveDir(fname:GetDirCount()-1)
      path = GetPathWithSep(fname)
    end

    return path:sub(0,-2)
  end,
}
