local editor = NewFile()
ok(editor, "Open New file.")
ok(editor.assignscache ~= nil, "Auto-complete cache is assigned.")

editor:AddText([[
  local line = '123'
  line = line:gsub('1','4')
  line:]])

ok(limit(10000, function() CreateAutoCompList(editor, "line:") end),
  "Auto-complete doesn't loop for 'line:' after 'line:gsub'.")

ok(limit(10000, function() CreateAutoCompList(editor, "line.") end),
  "Auto-complete doesn't loop for 'line.' after 'line:gsub'.")

editor:SetText('') -- use Set/Add to position cursor after added text
editor:AddText([[
  smth = smth:new()
  smth:]])

ok(limit(10000, function() CreateAutoCompList(editor, "smth:") end),
  "Auto-complete doesn't loop for 'smth:'.")

ok(pcall(CreateAutoCompList, editor, "%1000"),
  "Auto-complete doesn't trigger 'invalid capture index' on '%...'.")

editor:SetText('') -- use Set/Add to position cursor after added text
editor:AddText([[
  result = result.list[1]  --> "does the test" test
  result.1
]])

ok(limit(10000, function() CreateAutoCompList(editor, "result.1") end),
  "Auto-complete doesn't loop for table index reference.")

local interpreter = ide:GetInterpreter():GetFileName()
ProjectSetInterpreter("gideros")

local ac = CreateAutoCompList(editor, "Bitmap.n")
local _, c = ac:gsub("new", "new")
ok(c == 1,
  ("Auto-complete doesn't offer duplicates with the same name ('%s').")
    :format(ac))

ProjectSetInterpreter(interpreter)

-- cleanup
ide:GetDocument(editor).isModified = false
ClosePage()
