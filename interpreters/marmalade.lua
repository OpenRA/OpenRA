-- Copyright 2011-13 Paul Kulchenko, ZeroBrane LLC

--[[

Support for Hub added by Marmalade.

--]]

local quick
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"
local exe = win and [[win32\s3e_simulator_release.exe]] or [[loader/osx/s3e_simulator_release]]
local exe_d = win and [[win32\s3e_simulator_debug.exe]] or [[loader/osx/s3e_simulator_debug]]
local s3e = os.getenv("S3E_DIR")
local prev_debug_loader

--[[
 Parse project file
--]]
function ProjectSettings(hub_project)
  local project_settings = {}
  local in_section = false
  for line in io.lines(hub_project) do
    -- Ignore comments and blank lines
    if not line:find('^%s*;') and not line:match('^%s*$') then
      section = line:match('^%s*%[([^%]]+)%]%s*$')
      if section then
        in_section = "GENERAL" == section:upper()
      elseif in_section then
        local key, value = line:match('^%s*(%w+)%s*=%s*(.-)%s*$')
        if tonumber(value) then
          project_settings[key] = tonumber(value)
        elseif value:upper() == "TRUE" then
          project_settings[key] = true
        elseif value:upper() == "FALSE" then
          project_settings[key] = false
        else
          project_settings[key] = value
        end
      end
    end
  end
  return project_settings
end

function GetFileName(filePath)
  if (not filePath) then return end
  local wxn = wx.wxFileName(filePath)
  return wxn:GetName()
end

function GetQuickBuildFolder(mkb_quick)
  for line in io.lines(mkb_quick) do
    if line:find('^QUICK') then
      local dir = line:match('^QUICK;([^;]+);')
      return dir
    end
  end
end

local project_settings = {}

function LauncherFromHubProject(projdir, project_name)

    local sep = GetPathSeparator()

    local mkb_quick = projdir..sep.."project_"..project_name..sep.."mkb-quick.txt"

    if not mkb_quick or not wx.wxFileExists(mkb_quick) then
      DisplayOutputLn(("Warning: can't find '%s' file."):format(mkb_quick))
      return
    end
    
    local build_folder = GetQuickBuildFolder(mkb_quick)

    if not build_folder or not wx.wxDirExists(projdir..sep..build_folder) then
      DisplayOutputLn(("Warning: can't find '%s' Quick build folder."):format(build_folder))
      return
    end

    local project_file = projdir..sep.."project_"..project_name..sep.."project.ini"

    if not project_file or not wx.wxFileExists(project_file) then
      DisplayOutputLn(("Warning: can't find '%s' Hub project file."):format(project_file))
    else
      project_settings = ProjectSettings(project_file)
    end
    
    if next(project_settings) == nil then
      DisplayOutputLn(("Warning: can't read settings from '%s' project file. Using defaults."):format(project_file))
      project_settings = {}
    end

    -- Use simulator config
    local via = projdir..sep..build_folder..sep..project_name.."_"..(project_settings.isDebug and 'debug' or 'release')..".via"

    if not via or not wx.wxFileExists(via) then
      DisplayOutputLn(("Warning: can't find '%s' via file."):format(via))
      via = projdir..sep..build_folder..sep.."web_"..(project_settings.isDebug and 'debug' or 'release')..".via"
    end

    if not via or not wx.wxFileExists(via) then
      DisplayOutputLn(("Warning: can't find '%s' via file."):format(via))
      via = nil
    end

    return via
end

return {
  name = "Marmalade Quick",
  description = "Marmalade Quick mobile framework",
  api = {"baselib", "marmalade"},
  frun = function(self,wfilename,rundebug)
    local projdir = self:fworkdir(wfilename)
    -- check for *.mkb file; it can be in the same or in the parent folder
    local mproj, mfile = MergeFullPath(projdir, "./")
    for _, file in ipairs(FileSysGetRecursive(mproj, false, "*.mkb")) do mfile = file end
    if not mfile then
      mproj, mfile = MergeFullPath(projdir, "../")
      for _, file in ipairs(FileSysGetRecursive(mproj, false, "*.mkb")) do mfile = file end
    end
    if not mfile then
      DisplayOutputLn(("Can't find '%s' project file."):format(mproj))
      return
    end

    -- Check Marmalade project configuration
    local via = LauncherFromHubProject(mproj, GetFileName(mfile))
    
    quick = prev_debug_loader == project_settings.isDebugLoader and quick or nil

    quick = quick or ide.config.path.quick or (project_settings.isDebugLoader and (s3e and GetFullPathIfExists(s3e, exe_d)) or (s3e and GetFullPathIfExists(s3e, exe)))
    
    prev_debug_loader = project_settings.isDebugLoader
    
    if not quick then
      local sep = wx.wxPATH_SEP
      local path =
           win and ([[C:\Marmalade]]..sep..[[D:\Marmalade]]..sep..
                    GenerateProgramFilesPath('Marmalade', sep)..sep)
        or mac and ([[/Applications/Marmalade.app/Contents]]..sep..
                    [[/Developer/Marmalade]]..sep)
        or ''
      -- Marmalade can be installed in a folder with version number or without
      -- so it may be c:\Marmalade\s3e\... or c:\Marmalade\6.2\s3e\...
      local candidates, paths = {}, {}
      for p in path:gmatch("[^"..sep.."]+") do
        table.insert(paths, p)
        for _, candidate in ipairs(FileSysGetRecursive(p, false, "*")) do
          if GetFullPathIfExists(candidate, exe) then table.insert(candidates, candidate) end
          if GetFullPathIfExists(candidate.."/s3e", exe) then table.insert(candidates, candidate.."/s3e") end
        end
        -- stop on Mac if found something in /Applications (7.0+)
        if mac and #candidates > 0 then break end
      end
      -- multiple candidates may be present, so sort and use the latest.
      -- only happens if multiple versions are installed and S3E_DIR is not set.
      table.sort(candidates)
      if #candidates > 0 then
        s3e = candidates[#candidates]
        quick = GetFullPathIfExists(s3e, exe) -- guaranteed to exist
      else
        DisplayOutputLn("Can't find Marmalade installation in any of these folders (and S3E_DIR environmental variable is not set): "
          ..table.concat(paths, ", "))
        return
      end
    end

    if not s3e then s3e = quick:gsub(exe, '') end

    local options
    local datadir

    if via then
      options = ([[--via="%s"]]):format(via)
      datadir = FileRead(via):match('--data="([^"]+)"')
    else
      local mkb = FileRead(mfile)
      datadir = mkb:match("options%s*%{[^%}]*s3e%-data%-dir%s*=%s*(.-)%s*[\r\n%}]")
      datadir = datadir and datadir:gsub("^['\"]", ""):gsub("['\"][\r\n]*$", "")
      local icf1, icf2 = mkb:match("options%s*%{[^%}]*app%-icf%s*=%s*(.-)%s*[\r\n%}]")
      icf1 = icf1 and icf1:gsub("^['\"]", ""):gsub("['\"][\r\n]*$", "")
      if icf1 and icf1:find(",") then
        icf1, icf2 = icf1:match("(.+),(.*)")
      end

      datadir = datadir and (wx.wxIsAbsolutePath(datadir) and datadir or MergeFullPath(mproj, datadir))
      icf1 = icf1 and (wx.wxIsAbsolutePath(icf1) and icf1 or MergeFullPath(mproj, icf1))
      icf2 = icf2 and (wx.wxIsAbsolutePath(icf2) and icf2 or MergeFullPath(mproj, icf2))

      local quick_prebuilt = project_settings.isDebug and "quick_prebuilt_d.s86" or "quick_prebuilt.s86"

      local dll = GetFullPathIfExists(s3e, "../quick/target/"..quick_prebuilt)
        or MergeFullPath(s3e, ("../quick/target/%s/"..quick_prebuilt):format(mac and 'osx' or 'win'))
        options = table.concat({
        ([[--dll="%s"]]):format(dll),
        (datadir and ([[--data="%s"]]):format(datadir) or ''),
        -- Quick doesn't handle correctly spaces in quoted parameters on OSX,
        -- so replace those with escaped spaces; still quote on Windows
        (icf1 and ([[--app-icf1=%s]]):format(mac and icf1:gsub(" ", "\\ ") or '"'..icf1..'"') or ''),
        (icf2 and ([[--app-icf2=%s]]):format(mac and icf2:gsub(" ", "\\ ") or '"'..icf2..'"') or nil),
      }, " ")
    end

    if not datadir then
      DisplayOutputLn("Failed to determine data dir")
      return
    end

    if rundebug then
      -- start running the application right away
      DebuggerAttachDefault({redirect = mac and "r" or "c", basedir = datadir,
        runstart = ide.config.debugger.runonstart ~= false})

      -- copy mobdebug.lua to the configured datadir or project folder
      local mdbc = MergeFullPath(datadir or projdir, "mobdebug.lua")
      local mdbl = MergeFullPath(GetPathWithSep(ide.editorFilename), "lualibs/mobdebug/mobdebug.lua")
      if not wx.wxFileExists(mdbc)
      or GetFileModTime(mdbc):GetTicks() < GetFileModTime(mdbl):GetTicks() then
        local copied = FileCopy(mdbl, mdbc)
        local message = copied
          and ("Copied debugger ('mobdebug.lua') to '%s'."):format(mdbc)
          or ("Failed to copy debugger ('mobdebug.lua') to '%s': %s")
            :format(mdbc, wx.wxSysErrorMsg())
        DisplayOutputLn(message)
        if not copied then return end
      end
    end

    local cmd = ('"%s" %s'):format(quick, options)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,GetPathWithSep(projdir),true,true)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
