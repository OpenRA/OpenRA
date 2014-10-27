-- This scrips extracts all text messages for ZeroBrane Studio and generates
-- a file that can be used to provide translation to a particular language.
-- The name of the file -- ll-cc.lua -- has two segments:
-- ll (mandatory) -- two letter [language code](http://en.wikipedia.org/wiki/List_of_ISO_639-1_codes);
-- cc (optional) -- two letter [country code](http://en.wikipedia.org/wiki/ISO_3166-1#Officially_assigned_code_elements).

-- You can generate an empty file with all messages by running:
--  > bin\lua.exe build/messages.lua
-- You can also update an existing file with new messages by running:
--  > bin\lua.exe build/messages.lua cfg/i18n/ru.lua

-- store `print` function as it's modified by wxlua and LuaJIT doesn't like
-- what wxlua has done in that function.
local print = print

local iswindows = os.getenv('WINDIR') or (os.getenv('OS') or ''):match('[Ww]indows')
if iswindows or not pcall(require, "wx") then
  package.cpath = (iswindows and 'bin/?.dll;' or 'bin/lib?.dylib;') .. package.cpath
end

require "wx"

function FileSysGet(dir,spec)
  local content = {}
  local browse = wx.wxFileSystem()
  local cwd = wx.wxGetCwd()
  if not wx.wxFileName(dir):DirExists() then return content end
  local f = browse:FindFirst(dir,spec)
  while #f>0 do
    if f:match("^file:") then -- remove file: protocol (wx2.9+)
      f = f:gsub(iswindows and "^file:/?" or "^file:","")
        :gsub('%%(%x%x)', function(n) return string.char(tonumber(n, 16)) end)
    end
    -- wx2.9+ return absolute path here instead of expected relative; fix it
    if wx.wxIsAbsolutePath(f) then
      local relative = wx.wxFileName(f)
      relative:MakeRelativeTo(cwd)
      f = relative:GetFullPath()
    end
    table.insert(content, f)
    f = browse:FindNext()
  end
  return content
end

function FileRead(file)
  local f = wx.wxFile(file, wx.wxFile.read)
  if not f:IsOpened() then
    error("Can't open file '"..file.."': "..wx.wxSysErrorMsg()) end
  local _, content = f:Read(f:Length())
  f:Close()
  return content
end

local file = ... -- pass an existing file name as a parameter
local messages = {}
for _, mask in ipairs({"zbstudio/*.lua", "src/main.lua", "src/editor/*.lua"}) do
  for _, file in ipairs(FileSysGet(mask, wx.wxFILE)) do
    local content = FileRead(file)
    for msg in content:gmatch("[^%w]TR(%b())") do
      -- remove brackets aroung ("foo")
      -- extract message from ("foo", count)
      msg = msg:gsub("^%(", ""):gsub("%)$", ""):gsub([[(["']), .+]], "%1")
      if not msg:find([=[^["']]=]) or not msg:find([=[["']$]=]) then
        io.stderr:write(("Call with a non-string 'TR(%s)' ignored in '%s'.\n")
          :format(msg, file))
      else
        messages[msg] = messages[msg] or {}
        messages[msg][file] = (messages[msg][file] or 0) + 1
      end
    end
  end
end

local existing = {}
if file then
  local content = FileRead(file)
  for msg, translation in content:gmatch("%s*%[([^%]]+)%]%s*=%s*([^\010\013]+)") do
    if not translation:find('^nil') then existing[tonumber(msg) or msg] = translation end
  end
end

local plural = existing[0] and ("  [0] = "..existing[0].."\n") or ""
existing[0] = nil

local msgs = {}
for m, files in pairs(messages) do
  local str = "  ["..m.."] = "..(existing[m] or 'nil, --')
  str = str:gsub(" %-%-.*$", "").." -- "
  for f in pairs(files) do str = str .. f .. ", " end
  msgs[#msgs+1] = str:gsub(", $", "")
  existing[m] = nil -- remove from the list of existing elements
end

table.sort(msgs)
print("return {\n"..plural..table.concat(msgs, "\n").."\n}")
if next(existing) then
  local str = "No match found for the following elements: "
  for msg in pairs(existing) do str = str .. msg .. ", " end
  io.stderr:write((str:gsub(", $", "\n")))
end
