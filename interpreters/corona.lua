local corona
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"

return {
  name = "Corona",
  description = "Corona SDK mobile framework",
  api = {"baselib"},
  frun = function(self,wfilename,rundebug)
    corona = corona or ide.config.path.corona -- check if the path is configured
    if not corona then
      local sep = win and ';' or ':'
      local default =
           win and ([[C:\Program Files\Corona SDK]]..sep..[[D:\Program Files\Corona SDK]]..sep)
        or mac and ('/Applications/CoronaSDK/Corona Simulator.app/Contents/MacOS'..sep)
        or ''
      local path = default
                 ..(os.getenv('PATH') or '')..sep
                 ..(os.getenv('HOME') and os.getenv('HOME') .. '/bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        corona = corona or GetFullPathIfExists(p, win and 'Corona Simulator.exe' or 'Corona Simulator')
        table.insert(paths, p)
      end
      if not corona then
        DisplayOutput("Can't find corona executable in any of the folders in PATH: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end

    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({runstart=true})

      -- copy mobdebug.lua to corona/Resources folder
      local mdbc = MergeFullPath(GetPathWithSep(corona), "Resources/mobdebug.lua")
      local mdbl = MergeFullPath(GetPathWithSep(ide.editorFilename), "lualibs/mobdebug/mobdebug.lua")
      if not wx.wxFileExists(mdbc)
      or GetFileModTime(mdbc):GetTicks() < GetFileModTime(mdbl):GetTicks() then
        FileCopy(mdbl, mdbc)
        DisplayOutput("Copied ZeroBrane Studio debugger ('mobdebug.lua') to 'Corona SDK/Resource' folder.\n")
      end
    end

    local cmd = ('"%s" -debug "%s"'):format(corona, self:fworkdir(wfilename).."/main.lua")
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,false,nil,nil,
      function() ide.debugger.pid = nil end)
  end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function(self,wfilename)
    return ide.config.path.projectdir or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
