-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

local corona
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"

return {
  name = "Corona",
  description = "Corona SDK mobile framework",
  api = {"baselib", "corona"},
  frun = function(self,wfilename,rundebug)
    corona = corona or ide.config.path.corona -- check if the path is configured
    if not corona then
      local sep = win and ';' or ':'
      local default =
           win and ([[C:\Program Files\Corona SDK]]..sep..[[D:\Program Files\Corona SDK]]..sep..
                    [[C:\Program Files\Corona Labs\Corona SDK]]..sep..[[D:\Program Files\Corona Labs\Corona SDK]]..sep..
                    [[C:\Program Files (x86)\Corona SDK]]..sep..[[D:\Program Files (x86)\Corona SDK]]..sep..
                    [[C:\Program Files (x86)\Corona Labs\Corona SDK]]..sep..[[D:\Program Files (x86)\Corona Labs\Corona SDK]]..sep)
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

    local file = GetFullPathIfExists(self:fworkdir(wfilename), 'main.lua')
    if not file then
      DisplayOutput("Can't find 'main.lua' file in the current project folder.\n")
      return
    end

    -- can we really do debugging? do if asked and if not on mac OSX where it's not supported
    local debug = rundebug and not mac
    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({runstart=true, startwith = file,
        redirect = debug and "c", noshell = mac or nil, noeval = mac or nil})

      -- copy mobdebug.lua to Resources/ folder on Win and to the project folder on OSX
      -- as copying it to Resources/ folder seems to break the signature of the app.
      local mdbc = mac and MergeFullPath(self:fworkdir(wfilename), "mobdebug.lua")
        or MergeFullPath(GetPathWithSep(corona), "Resources/mobdebug.lua")
      local mdbl = MergeFullPath(GetPathWithSep(ide.editorFilename), "lualibs/mobdebug/mobdebug.lua")
      if not wx.wxFileExists(mdbc)
      or GetFileModTime(mdbc):GetTicks() < GetFileModTime(mdbl):GetTicks() then
        FileCopy(mdbl, mdbc)
        DisplayOutput(("Copied ZeroBrane Studio debugger ('mobdebug.lua') to '%s' folder.\n"):format(mdbc))
      end
    end

    local cmd = ('"%s" %s"%s"'):format(corona, debug  and "-debug " or "", file)
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
  scratchextloop = true,
}
