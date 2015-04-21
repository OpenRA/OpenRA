-- Copyright 2011-13 Paul Kulchenko, ZeroBrane LLC

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
           win and (GenerateProgramFilesPath('Corona SDK', sep)..sep..
                    GenerateProgramFilesPath('Corona Labs\\Corona SDK', sep)..sep)
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
        DisplayOutputLn("Can't find corona executable in any of the folders in PATH: "
          ..table.concat(paths, ", "))
        return
      end
    end

    local file = GetFullPathIfExists(self:fworkdir(wfilename), 'main.lua')
    if not file then
      DisplayOutputLn(("Can't find 'main.lua' file in the current project folder: '%s'.")
        :format(self:fworkdir(wfilename)))
      return
    end

    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({startwith = file, redirect = "r",
        runstart = ide.config.debugger.runonstart ~= false})

      local function needRefresh(mdbl, mdbc)
        return not wx.wxFileExists(mdbc)
        or GetFileModTime(mdbc):GetTicks() < GetFileModTime(mdbl):GetTicks()
      end

      -- copy mobdebug.lua to Resources/ folder on Win and to the project folder on OSX
      -- as copying it to Resources/ folder seems to break the signature of the app.
      local mdbc = mac and MergeFullPath(self:fworkdir(wfilename), "mobdebug.lua")
        or MergeFullPath(GetPathWithSep(corona), "Resources/mobdebug.lua")
      local mdbl = MergeFullPath(GetPathWithSep(ide.editorFilename), "lualibs/mobdebug/mobdebug.lua")
      local needed = needRefresh(mdbl, mdbc)
      local mdbcplugin = win and MergeFullPath(wx.wxStandardPaths.Get():GetUserLocalDataDir(),
        "../../Roaming/Corona Labs/Corona Simulator/Plugins/mobdebug.lua")
      if needed then
        local copied = FileCopy(mdbl, mdbc)
        -- couldn't copy to the Resources/ folder; not have permissions?
        if not copied and win then
          mdbc = mdbcplugin
          needed = needRefresh(mdbl, mdbc)
          copied = needed and FileCopy(mdbl, mdbc)
        end
        if needed then
          local message = copied
            and ("Copied debugger ('mobdebug.lua') to '%s'."):format(mdbc)
            or ("Failed to copy debugger ('mobdebug.lua') to '%s': %s")
              :format(mdbc, wx.wxSysErrorMsg())
          DisplayOutputLn(message)
          if not copied then return end
        end
      end
      -- remove debugger if copied to plugin directory as it may be obsolete
      if mdbcplugin and mdbcplugin ~= mdbc and wx.wxFileExists(mdbcplugin) then
        wx.wxRemoveFile(mdbcplugin)
      end
    end

    local cfg = ide.config.corona or {}
    local debugopt = mac and "-debug 1 -project " or "-debug "
    local skin = cfg.skin and (" -skin "..ide.config.corona.skin) or ""
    local noconsole = (cfg.showconsole and ""
      or (mac and "-no-console YES " or "-no-console "))
    local cmd = ('"%s" %s%s"%s"%s')
      :format(corona, noconsole, rundebug and debugopt or "", file, skin)

    local uhw = ide.config.unhidewindow
    local cwc = uhw and uhw.ConsoleWindowClass
    if uhw and cfg.showconsole then uhw.ConsoleWindowClass = 0 end
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,true,nil,nil,
      function() if uhw and cfg.showconsole then uhw.ConsoleWindowClass = cwc end end)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = true,
}
