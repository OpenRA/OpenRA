return {
  name = "Luxinia",
  description = "Luxinia project",
  api = {"luxiniaapi","baselib"},
  fcmdline = function(self,wfilename)
    local projdir = ide.config.path.projectdir
    local endstr = (projdir and projdir:len()>0
      and " -p "..projdir or "")

    local fname = wfilename:GetFullName()
    endstr = endstr..(fname and (" -t "..fname) or "")

    local cmd = ide.config.path.luxinia..'luxinia.exe --nologo'..endstr
    CommandLineRun(cmd,ide.config.path.luxinia,true,true)
  end,
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
