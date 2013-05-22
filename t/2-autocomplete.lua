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

editor:SetText('')
editor:AddText([[
  smth = smth:new()
  smth:]])

ok(limit(10000, function() CreateAutoCompList(editor, "smth:") end),
  "Auto-complete doesn't loop for 'smth:'.")

ok(pcall(CreateAutoCompList, editor, "%1000"),
  "Auto-complete doesn't trigger 'invalid capture index' on '%...'.")

-- cleanup
ide.openDocuments[editor:GetId()].isModified = false
ClosePage()
