local editor = NewFile()
ok(editor, "Open New file.")
ok(editor.assignscache ~= nil, "Auto-complete cache is assigned.")

local maxstat = 20000 -- maximum number of statements to detect looping
local strategy = ide.config.acandtip.strategy

for s = 2, 0, -1 do -- execute all tests for different `strategy` values
ide.config.acandtip.strategy = s

editor:SetText('') -- use Set/Add to position cursor after added text
editor:AddText([[
  local line = '123'
  line = line:gsub('1','4')
  line:]])

ok(limit(maxstat, function() CreateAutoCompList(editor, "line:") end),
  ("Auto-complete (strategy=%s) doesn't loop for 'line:' after 'line:gsub'."):format(s))

ok(limit(maxstat, function() CreateAutoCompList(editor, "line.") end),
  ("Auto-complete (strategy=%s) doesn't loop for 'line.' after 'line:gsub'."):format(s))

editor:SetText('') -- use Set/Add to position cursor after added text
editor:AddText([[
  smth = smth:new()
  smth:]])

ok(limit(maxstat, function() CreateAutoCompList(editor, "smth:") end),
  ("Auto-complete (strategy=%s) doesn't loop for 'smth:'."):format(s))

ok(pcall(CreateAutoCompList, editor, "%1000"),
  ("Auto-complete (strategy=%s) doesn't trigger 'invalid capture index' on '%%...'."):format(s))

editor:SetText('')
editor:AddText([[
  local tweaks = require("tweaks")
  local require = tweaks.require
  local modules = tweaks.modules]])

ok(limit(maxstat, function() CreateAutoCompList(editor, "tweaks.modules") end),
  ("Auto-complete (strategy=%s) doesn't loop for recursive 'modules'."):format(s))

editor:SetText('')
editor:AddText([[
  result = result.list[1]  --> "does the test" test
  result.1
]])

ok(limit(maxstat, function() CreateAutoCompList(editor, "result.1") end),
  ("Auto-complete (strategy=%s) doesn't loop for table index reference 1/2."):format(s))

editor:SetText('')
editor:AddText([[
  self.popUpObjs = self.undoBuffer[0].sub
  self.undoBuffer = self.undoBuffer[0]
  self.popUpObjs[popUpNo].]])

ok(limit(maxstat, function() EditorAutoComplete(editor) end),
  ("Auto-complete (strategy=%s) doesn't loop for table index reference 2/2."):format(s))

editor:SetText('')
editor:AddText([[
  local a = ...
  local b = a.b
  local c = b.]])

ok(limit(maxstat, function() EditorAutoComplete(editor) end),
  ("Auto-complete (strategy=%s) doesn't loop for classes that reference '...'."):format(s))

editor:SetText('')
editor:AddText([[
  buf = str
  str = buf..str
  buf = buf..]])

ok(limit(maxstat, function() EditorAutoComplete(editor) end),
  ("Auto-complete (strategy=%s) doesn't loop for string concatenations with self-reference."):format(s))

-- create a valuetype self-reference
-- this is to test "s = Scan(); s:" fragment
ide.apis.lua.baselib.io.valuetype = "io"
ReloadLuaAPI()

editor:SetText('')
editor:AddText([[
  s = io;
  s:]])

ok(limitdepth(1000, function() EditorAutoComplete(editor) end),
  ("Auto-complete (strategy=%s) doesn't loop for classes that self-reference with 'valuetype'."):format(s))

-- restore valuetype
ide.apis.lua.baselib.io.valuetype = nil
ReloadLuaAPI()

local interpreter = ide:GetInterpreter():GetFileName()
ProjectSetInterpreter("gideros")

local ac = CreateAutoCompList(editor, "Bitmap.n")
local _, c = ac:gsub("new", "new")
ok(c == 1,
  ("Auto-complete (strategy=%s) doesn't offer duplicates with the same name ('%s')."):format(s, ac))

for k, v in pairs({
    -- the following results differ depending on `strategy` settings
    ree = s == 2 and "repeat require" or "",
    ret = s == 2 and "return repeat rawget rawset" or "return",
}) do
  local ac = CreateAutoCompList(editor, k)
  is(ac, v,
    ("Auto-complete (strategy=%s) for '%s' offers results in the expected order."):format(s, k))
end

ProjectSetInterpreter(interpreter)

editor:SetText('')
editor:AddText('local t = require("table")\nt.')
local ac = CreateAutoCompList(editor, "t.")
ok(ac ~= nil and ac:find("concat") ~= nil,
  ("Auto-complete (strategy=%s) recognizes variables set based on `require`."):format(s))

editor:SetText('')
editor:AddText('local table = require("io")\nt = require("table")\nt.')
local ac = CreateAutoCompList(editor, "t.")
ok(ac ~= nil and ac:find("concat") ~= nil,
  ("Auto-complete (strategy=%s) recognizes variables set based on `require` even when it's re-assigned."):format(s))

editor:SetText('')
editor:AddText('print(1,io.')

local value
local ULS = editor.UserListShow
editor.UserListShow = function(editor, pos, list) value = list end
EditorAutoComplete(editor)
editor.UserListShow = ULS

ok(value and value:find("close"), "Auto-complete is shown after comma.")

ok(not (CreateAutoCompList(editor, "pri.") or ""):match('print'),
  ("Auto-complete (strategy=%s) doesn't offer 'print' after 'pri.'."):format(s))

editor:SetText('')
editor:AddText('local name = "abc"; local namelen = #name')
IndicateAll(editor)
EditorAutoComplete(editor)
local isactive = editor:AutoCompActive()
editor:AutoCompCancel() -- cleanup

ok(not isactive,
  ("Auto-complete (strategy=%s) is not shown if typed sequence matches one of the options."):format(s))

editor:SetText('')
editor:AddText(' -- a = io\na:')
editor:Colourise(0, -1) -- set proper styles
editor.assignscache = false

ok((CreateAutoCompList(editor, "a:") or "") == "",
  ("Auto-complete (strategy=%s) doesn't process assignments in comments."):format(s))

editor:SetText('')
editor:AddText('-- @tparam string foo\n')
editor.assignscache = false

ok((CreateAutoCompList(editor, "foo.") or ""):match('byte'),
  ("Auto-complete (strategy=%s) offers methods for variable defined as '@tparam string'."):format(s))

editor:SetText('')
editor:AddText('-- @param[type=string] foo\n')
editor.assignscache = false

ok((CreateAutoCompList(editor, "foo:") or ""):match('byte'),
  ("Auto-complete (strategy=%s) offers methods for variable defined as '@param[type=string]'."):format(s))

editor:SetText('')
editor:AddText('local value\nprint(va')
IndicateAll(editor)

local status, res = pcall(CreateAutoCompList, editor, "va")
ok(status and (res or ""):match('value'),
  ("Auto-complete (strategy=%s) offers completions for variables' (1/2)."):format(s))

editor:SetText('')
editor:AddText('local value\nprint(va')

local status, res = pcall(CreateAutoCompList, editor, "va")
ok(status and (res or ""):match('value'),
  ("Auto-complete (strategy=%s) offers completions for variables' (2/2)."):format(s))

end

-- cleanup
ide.config.acandtip.strategy = strategy
ide:GetDocument(editor):SetModified(false)
ClosePage()
