-- Copyright 2011-13 Paul Kulchenko, ZeroBrane LLC

local quick
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"
local exe = win and [[win32\s3e_simulator_debug.exe]] or [[loader/osx/s3e_simulator_debug]]
local s3e = os.getenv("S3E_DIR")

return {
  name = "Marmalade Quick",
  description = "Marmalade Quick mobile framework",
  api = {"baselib", "marmalade"},
  frun = function(self,wfilename,rundebug)
    quick = quick or ide.config.path.quick or (s3e and GetFullPathIfExists(s3e, exe))
    if not quick then
      local sep = win and ';' or ':'
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

    local projdir = self:fworkdir(wfilename)
    local file = GetFullPathIfExists(projdir, 'main.lua')
    if not file then
      DisplayOutputLn("Can't find 'main.lua' file in the current project folder.")
      return
    end

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

    local mkb = FileRead(mfile)
    local datadir = mkb:match("options%s*%{[^%}]*s3e%-data%-dir%s*=%s*(.-)%s*[\r\n%}]")
    datadir = datadir and datadir:gsub("^['\"]", ""):gsub("['\"][\r\n]*$", "")
    local icf1, icf2 = mkb:match("options%s*%{[^%}]*app%-icf%s*=%s*(.-)%s*[\r\n%}]")
    icf1 = icf1 and icf1:gsub("^['\"]", ""):gsub("['\"][\r\n]*$", "")
    if icf1 and icf1:find(",") then
      icf1, icf2 = icf1:match("(.+),(.*)")
    end

    datadir = datadir and (wx.wxIsAbsolutePath(datadir) and datadir or MergeFullPath(mproj, datadir))
    icf1 = icf1 and (wx.wxIsAbsolutePath(icf1) and icf1 or MergeFullPath(mproj, icf1))
    icf2 = icf2 and (wx.wxIsAbsolutePath(icf2) and icf2 or MergeFullPath(mproj, icf2))

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

    local dll = GetFullPathIfExists(s3e, "../quick/target/quick_prebuilt_d.s86")
      or MergeFullPath(s3e, ("../quick/target/%s/quick_prebuilt_d.s86"):format(mac and 'osx' or 'win'))
    local options = table.concat({
      ([[--dll="%s"]]):format(dll),
      (datadir and ([[--data="%s"]]):format(datadir) or ''),
      -- Quick doesn't handle correctly spaces in quoted parameters on OSX,
      -- so replace those with escaped spaces; still quote on Windows
      (icf1 and ([[--app-icf1=%s]]):format(mac and icf1:gsub(" ", "\\ ") or '"'..icf1..'"') or ''),
      (icf2 and ([[--app-icf2=%s]]):format(mac and icf2:gsub(" ", "\\ ") or '"'..icf2..'"') or nil),
    }, " ")

    local cmd = ('"%s" %s'):format(quick, options)
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,GetPathWithSep(projdir),true,true)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
}
