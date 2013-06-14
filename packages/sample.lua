local G = ...
local id = G.ID("sample.samplemenu")

local P = {
  name = "Sample plugin",
  description = "Sample plugin to demonstrate various event types.",
  author = "Paul Kulchenko",

  onRegister = function(self) end,
  onUnRegister = function(self) end,
  onEditorLoad = function(self, editor) end,
  onEditorClose = function(self, editor) end,
  onEditorNew = function(self, editor) end,
  onEditorPreSave = function(self, editor, filepath) end,
  onEditorPostSave = function(self, editor) end,
  onEditorActivated = function(self, editor) end,
  onMenuEditor = function(self, menu, editor, event) end,
  onMenuEditorTab = function(self, menu, notebook, event) end,
  onMenuFiletree = function(self, menu, tree, event) end,
  onProjectLoad = function(self, project) end,
  onProjectClose = function(self, project) end,
  onInterpreterLoad = function(self, interpreter) end,
  onInterpreterClose = function(self, interpreter) end,
}

--[[ Uncomment this to see event names printed in the Output window
  for k in pairs(P) do
    if k:find("^on") then
      P[k] = k:find("^onEditor")
        and function(self, ed) DisplayOutputLn(self:GetFileName(), k, ide:GetDocument(ed):GetFilePath()) end
        or function(self, ...) DisplayOutputLn(self:GetFileName(), k, ...) end
    end
  end

  P.onMenuEditor = function(self, menu, editor, event)
    local point = editor:ScreenToClient(event:GetPosition())
    pos = editor:PositionFromPointClose(point.x, point.y)
    menu:Append(id, ">> Sample item; pos "..pos)
    menu:Enable(id, true)

    editor:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
      function() DisplayOutputLn("Selected "..pos) end)

    DisplayOutputLn(self:GetFileName(), "onMenuEditor")
  end

  P.onMenuEditorTab = function(self, menu, notebook, event)
    local index = event:GetSelection()
    menu:Append(id, ">> Sample item; tab "..index)
    menu:Enable(id, true)

    notebook:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
      function() DisplayOutputLn("Selected "..index) end)

    DisplayOutputLn(self:GetFileName(), "onMenuEditorTab")
  end

  P.onMenuFiletree = function(self, menu, tree, event)
    local item_id = event:GetItem()
    local name = tree:GetItemFullName(item_id)
    menu:Append(id, ">> Sample item; name "..name)
    menu:Enable(id, true)

    tree:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
      function() DisplayOutputLn("Selected "..name) end)

    DisplayOutputLn(self:GetFileName(), "onMenuFiletree")
  end

  P.onInterpreterLoad = function(self, interpreter)
    DisplayOutputLn(self:GetFileName(), "onInterpreterLoad", interpreter:GetFileName())
  end

  P.onInterpreterClose = function(self, interpreter)
    DisplayOutputLn(self:GetFileName(), "onInterpreterClose", interpreter:GetFileName())
  end

  P.onEditorPreSave = function(self, editor, filepath)
    if filepath:find("%.txt$") then
      DisplayOutputLn(self:GetFileName(), "onEditorPreSave", "Aborted saving a .txt file")
      return false
    else
      DisplayOutputLn(self:GetFileName(), "onEditorPreSave", filepath)
    end
  end

--]]

return P
