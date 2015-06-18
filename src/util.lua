-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
-- David Manura
---------------------------------------------------------

-- Equivalent to C's "cond ? a : b", all terms will be evaluated
function iff(cond, a, b) if cond then return a else return b end end

function EscapeMagic(s) return s:gsub('([%(%)%.%%%+%-%*%?%[%^%$%]])','%%%1') end

function GetPathSeparator()
  return string.char(wx.wxFileName.GetPathSeparator())
end

do
  local sep = GetPathSeparator()
  function IsDirectory(dir) return dir:find(sep.."$") end
end

function StripCommentsC(tx)
  local out = ""
  local lastc = ""
  local skip
  local skipline
  local skipmulti
  local tx = string.gsub(tx, "\r\n", "\n")
  for c in tx:gmatch(".") do
    local oc = c
    local tu = lastc..c
    skip = c == '/'

    if ( not (skipmulti or skipline)) then
      if (tu == "//") then
        skipline = true
      elseif (tu == "/*") then
        skipmulti = true
        c = ""
      elseif (lastc == '/') then
        oc = tu
      end
    elseif (skipmulti and tu == "*/") then
      skipmulti = false
      c = ""
    elseif (skipline and lastc == "\n") then
      out = out.."\n"
      skipline = false
    end

    lastc = c
    if (not (skip or skipline or skipmulti)) then
      out = out..oc
    end
  end

  return out..lastc
end

-- http://lua-users.org/wiki/EnhancedFileLines
function FileLines(f)
  local CHUNK_SIZE = 1024
  local buffer = ""
  local pos_beg = 1
  return function()
    local pos, chars
    while 1 do
      pos, chars = buffer:match('()([\r\n].)', pos_beg)
      if pos or not f then
        break
      elseif f then
        local chunk = f:read(CHUNK_SIZE)
        if chunk then
          buffer = buffer:sub(pos_beg) .. chunk
          pos_beg = 1
        else
          f = nil
        end
      end
    end
    if not pos then
      pos = #buffer
    elseif chars == '\r\n' then
      pos = pos + 1
    end
    local line = buffer:sub(pos_beg, pos)
    pos_beg = pos + 1
    if #line > 0 then
      return line
    end
  end
end

function PrependStringToArray(t, s, maxstrings, issame)
  if string.len(s) == 0 then return end
  for i = #t, 1, -1 do
    local v = t[i]
    if v == s or issame and issame(s, v) then
      table.remove(t, i) -- remove old copy
      -- don't break here in case there are multiple copies to remove
    end
  end
  table.insert(t, 1, s)
  if #t > (maxstrings or 15) then table.remove(t, #t) end -- keep reasonable length
end

function GetFileModTime(filePath)
  if filePath and #filePath > 0 then
    local fn = wx.wxFileName(filePath)
    if fn:FileExists() then
      return fn:GetModificationTime()
    end
  end

  return nil
end

function GetFileExt(filePath)
  local match = filePath and filePath:gsub("%s+$",""):match("%.([^./\\]*)$")
  return match and match:lower() or ''
end

function GetFileName(filePath)
  return filePath and filePath:gsub("%s+$",""):match("([^/\\]*)$") or ''
end

function IsLuaFile(filePath)
  return filePath and (string.len(filePath) > 4) and
  (string.lower(string.sub(filePath, -4)) == ".lua")
end

function GetPathWithSep(wxfn)
  if type(wxfn) == 'string' then wxfn = wx.wxFileName(wxfn) end
  return wxfn:GetPath(bit.bor(wx.wxPATH_GET_VOLUME, wx.wxPATH_GET_SEPARATOR))
end

function FileDirHasContent(dir)
  local f = wx.wxFindFirstFile(dir, wx.wxFILE + wx.wxDIR)
  return #f>0
end

function FileSysGetRecursive(path, recursive, spec, opts)
  local content = {}
  local sep = GetPathSeparator()
  local queue = {path}
  local pathpatt = "^"..EscapeMagic(path)
  local optyield = (opts or {}).yield
  local optfolder = (opts or {}).folder ~= false
  local optsort = (opts or {}).sort ~= false
  local optpath = (opts or {}).path ~= false
  local optskipbinary = (opts or {}).skipbinary

  local function spec2list(spec, list)
    -- return empty list if no spec is provided
    if spec == nil or spec == "*" or spec == "*.*" then return {}, 0 end
    -- accept "*.lua" and "*.txt,*.wlua" combinations
    if type(spec) == 'table' then spec = table.concat(spec, ",") end
    local masknum, list = 0, list or {}
    for m in spec:gmatch("[^%s;,]+") do
      m = m:gsub("[\\/]", sep)
      if m:find("^%*%.%w+"..sep.."?$") then
        list[m:sub(2)] = true
      else
        -- escape all special characters
        -- and replace (escaped) ** with .* and (escaped) * with [^\//]*
        table.insert(list, EscapeMagic(m)
          :gsub("%%%*%%%*", ".*"):gsub("%%%*", "[^/\\]*").."$")
      end
      masknum = masknum + 1
    end
    return list, masknum
  end

  local inmasks, masknum = spec2list(spec)
  if masknum >= 2 then spec = nil end

  local exmasks = spec2list(ide.config.excludelist or {})
  if optskipbinary then -- add any binary files to the list to skip
    exmasks = spec2list(type(optskipbinary) == 'table' and optskipbinary
      or ide.config.binarylist or {}, exmasks)
  end

  local function ismatch(file, inmasks, exmasks)
    -- convert extension 'foo' to '.foo', as need to distinguish file
    -- from extension with the same name
    local ext = '.'..GetFileExt(file)
    -- check exclusions if needed
    if exmasks[file] or exmasks[ext] then return false end
    for _, mask in ipairs(exmasks) do
      if file:find(mask) then return false end
    end

    -- return true if none of the exclusions match and no inclusion list
    if not inmasks or not next(inmasks) then return true end

    -- now check inclusions
    if inmasks[file] or inmasks[ext] then return true end
    for _, mask in ipairs(inmasks) do
      if file:find(mask) then return true end
    end
    return false
  end

  local function report(fname)
    if optyield then return coroutine.yield(fname) end
    table.insert(content, fname)
  end

  local dir = wx.wxDir()
  local function getDir(path)
    dir:Open(path)
    if not dir:IsOpened() then
      if DisplayOutputLn and TR then
        DisplayOutputLn(TR("Can't open '%s': %s"):format(path, wx.wxSysErrorMsg()))
      end
      return
    end

    -- recursion is done in all folders if requested,
    -- but only those folders that match the spec are returned
    local _ = wx.wxLogNull() -- disable error reporting; will report as needed
    local found, file = dir:GetFirst("*", wx.wxDIR_DIRS)
    while found do
      local fname = wx.wxFileName(path, file):GetFullPath()
      if optfolder and ismatch(fname..sep, inmasks, exmasks) then
        report((optpath and fname or fname:gsub(pathpatt, ""))..sep)
      end

      -- check if this name already appears in the path earlier;
      -- Skip the processing if it does as it could lead to infinite
      -- recursion with circular references created by symlinks.
      if recursive and ismatch(fname..sep, nil, exmasks)
      and select(2, fname:gsub(EscapeMagic(file..sep),'')) <= 2 then
        table.insert(queue, fname)
      end
      found, file = dir:GetNext()
    end
    found, file = dir:GetFirst(spec or "*", wx.wxDIR_FILES)
    while found do
      local fname = wx.wxFileName(path, file):GetFullPath()
      if ismatch(fname, inmasks, exmasks) then
        report(optpath and fname or fname:gsub(pathpatt, ""))
      end
      found, file = dir:GetNext()
    end
  end
  while #queue > 0 do getDir(table.remove(queue)) end

  if optyield then return end

  if optsort then
    local prefix = '\001' -- prefix to sort directories first
    local shadow = {}
    for _, v in ipairs(content) do
      shadow[v] = (v:sub(-1) == sep and prefix or '')..v:lower()
    end
    table.sort(content, function(a,b) return shadow[a] < shadow[b] end)
  end

  return content
end

local normalflags = wx.wxPATH_NORM_ABSOLUTE + wx.wxPATH_NORM_DOTS + wx.wxPATH_NORM_TILDE
function GetFullPathIfExists(p, f)
  if not p or not f then return end
  local file = wx.wxFileName(f)
  -- Normalize call is needed to make the case of p = '/abc/def' and
  -- f = 'xyz/main.lua' work correctly. Normalize() returns true if done.
  return (file:Normalize(normalflags, p)
    and file:FileExists()
    and file:GetFullPath()
    or nil)
end

function MergeFullPath(p, f)
  if not p or not f then return end
  local file = wx.wxFileName(f)
  -- Normalize call is needed to make the case of p = '/abc/def' and
  -- f = 'xyz/main.lua' work correctly. Normalize() returns true if done.
  return (file:Normalize(normalflags, p)
    and file:GetFullPath()
    or nil)
end

function FileWrite(file, content)
  local _ = wx.wxLogNull() -- disable error reporting; will report as needed

  if not wx.wxFileExists(file)
  and not wx.wxFileName(file):Mkdir(tonumber(755,8), wx.wxPATH_MKDIR_FULL) then
    return nil, wx.wxSysErrorMsg()
  end

  local file = wx.wxFile(file, wx.wxFile.write)
  if not file:IsOpened() then return nil, wx.wxSysErrorMsg() end

  local ok = file:Write(content, #content) == #content
  file:Close()
  return ok, not ok and wx.wxSysErrorMsg() or nil
end

function FileSize(fname) return wx.wxFileExists(fname) and wx.wxFileSize(fname) or nil end

function FileRead(fname, length)
  -- on OSX "Open" dialog allows to open applications, which are folders
  if wx.wxDirExists(fname) then return nil, "Can't read directory as file." end

  local _ = wx.wxLogNull() -- disable error reporting; will report as needed
  local file = wx.wxFile(fname, wx.wxFile.read)
  if not file:IsOpened() then return nil, wx.wxSysErrorMsg() end

  local _, content = file:Read(length or wx.wxFileSize(fname))
  file:Close()
  return content, wx.wxSysErrorMsg()
end

function FileRename(file1, file2)
  local _ = wx.wxLogNull() -- disable error reporting; will report as needed
  return wx.wxRenameFile(file1, file2), wx.wxSysErrorMsg()
end

function FileCopy(file1, file2)
  local _ = wx.wxLogNull() -- disable error reporting; will report as needed
  return wx.wxCopyFile(file1, file2), wx.wxSysErrorMsg()
end

local ok, socket = pcall(require, "socket")
TimeGet = ok and socket.gettime or os.clock

function IsBinary(text) return text:find("[^\7\8\9\10\12\13\27\32-\255]") and true or false end

function pairsSorted(t, f)
  local a = {}
  for n in pairs(t) do table.insert(a, n) end
  table.sort(a, f)
  local i = 0 -- iterator variable
  local iter = function () -- iterator function
    i = i + 1
    if a[i] == nil then return nil
    else return a[i], t[a[i]]
    end
  end
  return iter
end

function FixUTF8(s, repl)
  local p, len, invalid = 1, #s, {}
  while p <= len do
    if     p == s:find("[%z\1-\127]", p) then p = p + 1
    elseif p == s:find("[\194-\223][\128-\191]", p) then p = p + 2
    elseif p == s:find(       "\224[\160-\191][\128-\191]", p)
        or p == s:find("[\225-\236][\128-\191][\128-\191]", p)
        or p == s:find(       "\237[\128-\159][\128-\191]", p)
        or p == s:find("[\238-\239][\128-\191][\128-\191]", p) then p = p + 3
    elseif p == s:find(       "\240[\144-\191][\128-\191][\128-\191]", p)
        or p == s:find("[\241-\243][\128-\191][\128-\191][\128-\191]", p)
        or p == s:find(       "\244[\128-\143][\128-\191][\128-\191]", p) then p = p + 4
    else
      local repl = type(repl) == 'function' and repl(s:sub(p,p)) or repl
      s = s:sub(1, p-1)..repl..s:sub(p+1)
      table.insert(invalid, p)
      -- adjust position/length as the replacement may be longer than one char
      p = p + #repl
      len = len + #repl - 1
    end
  end
  return s, invalid
end

function RequestAttention()
  local frame = ide.frame
  if not frame:IsActive() then
    frame:RequestUserAttention()
    if ide.osname == "Macintosh" then
      local cmd = [[osascript -e 'tell application "%s" to activate']]
      wx.wxExecute(cmd:format(ide.editorApp:GetAppName()), wx.wxEXEC_ASYNC)
    elseif ide.osname == "Windows" then
      if frame:IsIconized() then frame:Iconize(false) end
      frame:Raise() -- raise the window

      local winapi = require 'winapi'
      if winapi then
        local pid = winapi.get_current_pid()
        local wins = winapi.find_all_windows(function(w)
          return w:get_process():get_pid() == pid
             and w:get_class_name() == 'wxWindowNR'
        end)
        if wins and #wins > 0 then
          -- found the window, now need to activate it:
          -- send some input to the window and then
          -- bring our window to foreground (doesn't work without some input)
          -- send Attn key twice (down and up)
          winapi.send_to_window(0xF6, false)
          winapi.send_to_window(0xF6, true)
          for _, w in ipairs(wins) do w:set_foreground() end
        end
      end
    end
  end
end

function TR(msg, count)
  local messages = ide.config.messages
  local lang = ide.config.language
  local counter = messages[lang] and messages[lang][0]
  local message = messages[lang] and messages[lang][msg]
  -- if there is count and no corresponding message, then
  -- get the message from the (default) english language,
  -- otherwise the message is not going to be pluralized properly
  if count and not message then
    message, counter = messages.en[msg], messages.en[0]
  end
  return count and counter and message and type(message) == 'table'
    and message[counter(count)] or message or msg
end

-- wxwidgets 2.9.x may report the last folder twice (depending on how the
-- user selects the folder), which makes the selected folder incorrect.
-- check if the last segment is repeated and drop it.
function FixDir(path)
  if wx.wxDirExists(path) then return path end

  local dir = wx.wxFileName.DirName(path)
  local dirs = dir:GetDirs()
  if #dirs > 1 and dirs[#dirs] == dirs[#dirs-1] then dir:RemoveLastDir() end
  return dir:GetFullPath()
end

function ShowLocation(fname)
  local osxcmd = [[osascript -e 'tell application "Finder" to reveal POSIX file "%s"']]
    .. [[ -e 'tell application "Finder" to activate']]
  local wincmd = [[explorer /select,"%s"]]
  local lnxcmd = [[xdg-open "%s"]] -- takes path, not a filename
  local cmd =
    ide.osname == "Windows" and wincmd:format(fname) or
    ide.osname == "Macintosh" and osxcmd:format(fname) or
    ide.osname == "Unix" and lnxcmd:format(wx.wxFileName(fname):GetPath())
  if cmd then wx.wxExecute(cmd, wx.wxEXEC_ASYNC) end
end

function LoadLuaFileExt(tab, file, proto)
  local cfgfn,err = loadfile(file)
  local report = DisplayOutputLn or print
  if not cfgfn then
    report(("Error while loading file: '%s'."):format(err))
  else
    local name = file:match("([a-zA-Z_0-9%-]+)%.lua$")
    if not name then return end

    -- check if os/arch matches to allow packages for different systems
    local osvals = {windows = true, unix = true, macintosh = true}
    local archvals = {x64 = true, x86 = true}
    local os, arch = name:match("-(%w+)-?(%w*)")
    if os and os:lower() ~= ide.osname:lower() and osvals[os:lower()]
    or arch and #arch > 0 and arch:lower() ~= ide.osarch:lower() and archvals[arch:lower()]
    then return end
    if os and osvals[os:lower()] then name = name:gsub("-.*","") end

    local success, result = pcall(function()return cfgfn(assert(_G or _ENV))end)
    if not success then
      report(("Error while processing file: '%s'."):format(result))
    else
      if (tab[name]) then
        local out = tab[name]
        for i,v in pairs(result) do
          out[i] = v
        end
      else
        tab[name] = proto and result and setmetatable(result, proto) or result
      end
    end
  end
end

function LoadLuaConfig(filename,isstring)
  if not filename then return end
  -- skip those files that don't exist
  if not isstring and not wx.wxFileName(filename):FileExists() then return end
  -- if it's marked as command, but exists as a file, load it as a file
  if isstring and wx.wxFileName(filename):FileExists() then isstring = false end

  local cfgfn, err, msg
  if isstring
  then msg, cfgfn, err = "string", loadstring(filename)
  else msg, cfgfn, err = "file", loadfile(filename) end

  local report = DisplayOutputLn or print
  if not cfgfn then
    report(("Error while loading configuration %s: '%s'."):format(msg, err))
  else
    setfenv(cfgfn,ide.config)
    local _, err = pcall(function()cfgfn(assert(_G or _ENV))end)
    if err then
      report(("Error while processing configuration %s: '%s'."):format(msg, err))
    end
  end
end

function LoadSafe(data)
  local f, res = loadstring(data)
  if not f then return f, res end

  local count = 0
  debug.sethook(function ()
    count = count + 1
    if count >= 3 then error("cannot call functions") end
  end, "c")
  local ok, res = pcall(f)
  count = 0
  debug.sethook()
  return ok, res
end

local function isCtrlFocused(e)
  local ctrl = e and e:FindFocus()
  return ctrl and
    (ctrl:GetId() == e:GetId()
     or ide.osname == 'Macintosh' and
       ctrl:GetParent():GetId() == e:GetId()) and ctrl or nil
end

function GetEditorWithFocus(...)
  -- need to distinguish GetEditorWithFocus() and GetEditorWithFocus(nil)
  -- as the latter may happen when GetEditor() is passed and returns `nil`
  if select('#', ...) > 0 then
    local ed = ...
    return isCtrlFocused(ed) and ed or nil
  end

  local editor = GetEditor()
  if isCtrlFocused(editor) then return editor end

  local nb = ide:GetOutputNotebook()
  for p = 0, nb:GetPageCount()-1 do
    local ctrl = nb:GetPage(p)
    if ctrl:GetClassInfo():GetClassName() == "wxStyledTextCtrl"
    and isCtrlFocused(ctrl) then
      return ctrl:DynamicCast("wxStyledTextCtrl")
    end
  end
  return nil
end

function GenerateProgramFilesPath(exec, sep)
  local env = os.getenv('ProgramFiles')
  return
    (env and env..'\\'..exec..sep or '')..
    [[C:\Program Files\]]..exec..sep..
    [[D:\Program Files\]]..exec..sep..
    [[C:\Program Files (x86)\]]..exec..sep..
    [[D:\Program Files (x86)\]]..exec
end

--[[ format placeholders
    - %f -- full project name (project path)
    - %s -- short project name (directory name)
    - %i -- interpreter name
    - %S -- file name
    - %F -- file path
    - %n -- line number
    - %c -- line content
    - %T -- application title
    - %v -- application version
    - %t -- current tab name
--]]
function ExpandPlaceholders(msg, ph)
  ph = ph or {}
  if type(msg) == 'function' then return msg(ph) end
  local editor = ide:GetEditor()
  local proj = ide:GetProject() or ""
  local dirs = wx.wxFileName(proj):GetDirs()
  local doc = editor and ide:GetDocument(editor)
  local nb = ide:GetEditorNotebook()
  local def = {
    f = proj,
    s = dirs[#dirs] or "",
    i = ide:GetInterpreter():GetName() or "",
    S = doc and doc:GetFileName() or "",
    F = doc and doc:GetFilePath() or "",
    n = editor and editor:GetCurrentLine()+1 or 0,
    c = editor and editor:GetLine(editor:GetCurrentLine()) or "",
    T = GetIDEString("editor") or "",
    v = ide.VERSION,
    t = editor and nb:GetPageText(nb:GetPageIndex(editor)) or "",
  }
  return(msg:gsub('%%(%w)', function(p) return ph[p] or def[p] or '?' end))
end

function MergeSettings(localSettings, savedSettings)
  for name in pairs(localSettings) do
    if savedSettings[name] ~= nil
    and type(savedSettings[name]) == type(localSettings[name]) then
      if type(localSettings[name]) == 'table'
      and next(localSettings[name]) ~= nil then
        -- check every value in the table to make sure that it's possible
        -- to add new keys to the table and they get correct default values
        -- (even though that are absent in savedSettings)
        for setting in pairs(localSettings[name]) do
          if savedSettings[name][setting] ~= nil then
            localSettings[name][setting] = savedSettings[name][setting]
           end
        end
      else
        localSettings[name] = savedSettings[name]
      end
    end
  end
end
