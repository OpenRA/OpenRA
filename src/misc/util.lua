-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
-- David Manura
---------------------------------------------------------

-- Equivalent to C's "cond ? a : b", all terms will be evaluated
function iff(cond, a, b) if cond then return a else return b end end

-- Does the num have all the bits in value
function HasBit(value, num)
  for n = 32, 0, -1 do
    local b = 2^n
    local num_b = num - b
    local value_b = value - b
    if num_b >= 0 then
      num = num_b
    else
      return true -- already tested bits in num
    end
    if value_b >= 0 then
      value = value_b
    end
    if (num_b >= 0) and (value_b < 0) then
      return false
    end
  end

  return true
end

-- ASCII values for common chars
char_CR = string.byte("\r")
char_LF = string.byte("\n")
char_Tab = string.byte("\t")
char_Sp = string.byte(" ")

string_Pathsep = string.char(wx.wxFileName.GetPathSeparator())
stringset_File = '[^"%?%*:\\/<>|]'

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

function PrependStringToArray(t, s, maxstrings)
  if string.len(s) == 0 then return end
  for i, v in ipairs(t) do
    if v == s then
      table.remove(t, i) -- remove old copy
      break
    end
  end
  table.insert(t, 1, s)
  if #t > (maxstrings or 15) then table.remove(t, #t) end -- keep reasonable length
end

-- ----------------------------------------------------------------------------
-- Get file modification time, returns a wxDateTime (check IsValid) or nil if
-- the file doesn't exist
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
  local match = filePath and filePath:match("%.([a-zA-Z_0-9]+)$")
  return match and (string.lower(match))
end

function IsLuaFile(filePath)
  return filePath and (string.len(filePath) > 4) and
  (string.lower(string.sub(filePath, -4)) == ".lua")
end

function GetFileNameExt(filePath)
  if (not filePath) then return end
  local wxn = wx.wxFileName(filePath)
  return (wxn:GetName()..(wxn:HasExt() and ("."..wxn:GetExt()) or ""))
end

function GetPathWithSep(wxfn)
  if type(wxfn) == 'string' then wxfn = wx.wxFileName(wxfn) end
  return wxfn:GetPath(bit.bor(wx.wxPATH_GET_VOLUME, wx.wxPATH_GET_SEPARATOR))
end

function FileSysHasContent(dir)
  local f = wx.wxFindFirstFile(dir,wx.wxFILE + wx.wxDIR)
  return #f>0
end

function FileSysGet(dir,spec)
  local content = {}
  local browse = wx.wxFileSystem()
  if not wx.wxFileName(dir):DirExists() then
    return content
  end
  local f = browse:FindFirst(dir,spec)
  while #f>0 do
    if f:match("^file:") then -- remove file: protocol (wx2.9+)
      f = f:gsub("^file:/?","")
        :gsub('%%(%x%x)', function(n) return string.char(tonumber(n, 16)) end)
    end
    local file = wx.wxFileName(f)
    -- normalize path if possible to correct separators for the local FS
    table.insert(content,
      file:Normalize(wx.wxPATH_NORM_ALL) and file:GetFullPath() or f)
    f = browse:FindNext()
  end
  if ide.osname == 'Unix' then table.sort(content) end
  return content
end

function GetFullPathIfExists(p, f)
  if not p or not f then return end
  local file = wx.wxFileName(f)
  -- Normalize call is needed to make the case of p = '/abc/def' and
  -- f = 'xyz/main.lua' work correctly. Normalize() returns true if done.
  return (file:Normalize(wx.wxPATH_NORM_ALL, p)
    and file:FileExists()
    and file:GetFullPath())
end

function MergeFullPath(p, f)
  if not p or not f then return end
  local file = wx.wxFileName(f)
  -- Normalize call is needed to make the case of p = '/abc/def' and
  -- f = 'xyz/main.lua' work correctly. Normalize() returns true if done.
  return (file:Normalize(wx.wxPATH_NORM_ALL, p)
    and file:GetFullPath())
end

function FileWrite(file,content)
  local log = wx.wxLogNull() -- disable error reporting; will report as needed
  local file = wx.wxFile(file, wx.wxFile.write)
  if not file:IsOpened() then return nil, wx.wxSysErrorMsg() end

  file:Write(content, #content)
  file:Close()
  return true
end

function FileRead(file)
  local log = wx.wxLogNull() -- disable error reporting; will report as needed
  local file = wx.wxFile(file, wx.wxFile.read)
  if not file:IsOpened() then return end

  local _, content = file:Read(file:Length())
  file:Close()
  return content, wx.wxSysErrorMsg()
end

function FileRename(file1, file2) return wx.wxRenameFile(file1, file2) end

function FileCopy(file1, file2) return wx.wxCopyFile(file1, file2) end

TimeGet = pcall(require, "socket") and socket.gettime or os.clock

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

function fixUTF8(s, replacement)
  local p, len, invalid = 1, #s, {}
  while p <= len do
    if     p == s:find("[%z\1-\127]", p) then p = p + 1
    elseif p == s:find("[\194-\223][\123-\191]", p) then p = p + 2
    elseif p == s:find(       "\224[\160-\191][\128-\191]", p)
        or p == s:find("[\225-\236][\128-\191][\128-\191]", p)
        or p == s:find(       "\237[\128-\159][\128-\191]", p)
        or p == s:find("[\238-\239][\128-\191][\128-\191]", p)
        or p == s:find(       "\240[\144-\191][\128-\191]", p) then p = p + 3
    elseif p == s:find("[\241-\243][\128-\191][\128-\191][\128-\191]", p)
        or p == s:find(       "\244[\128-\143][\128-\191][\128-\191]", p) then p = p + 4
    else
      s = s:sub(1, p-1)..replacement..s:sub(p+1)
      table.insert(invalid, p)
    end
  end
  return s, invalid
end

local messages, lang, counter
function TR(msg, count)
  lang = lang or ide.config.language
  messages = messages or ide.config.messages
  counter = counter or (messages[lang] and messages[lang][0])
  local message = messages[lang] and messages[lang][msg]
  return count and counter and message and type(message) == 'table'
    and message[counter(count)] or message or msg
end
