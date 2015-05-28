local G = ...
local id = G.ID("sample.samplemenu")

local P = {
  name = "Sample plugin",
  description = "Sample plugin to demonstrate various event types.",
  author = "Paul Kulchenko",
}

-- Events that are marked with "return false" can return `false` to
-- abort further processing.
-- For `onEditorPreSave` event it means that file saving will be aborted.
-- For `onEditorPreClose` event it means that closing an editor tab will be aborted.
-- For `onEditorKeyDown` event it means that the key will be "eaten".
-- For `onEditorAction` event it means that the action will not be executed.
-- For `onFiletreeActivate` event it means that no further processing is done.
-- For `onEditorCharAdded` event it means that no further processing is done
-- (but the character is still added to the editor).

local events = {
  onRegister =         function(self) end,
  onUnRegister =       function(self) end,
  onEditorLoad =       function(self, editor) end,
  onEditorPreClose =   function(self, editor) end, -- return false
  onEditorClose =      function(self, editor) end,
  onEditorNew =        function(self, editor) end,
  onEditorPreSave =    function(self, editor, filepath) end, -- return false
  onEditorSave =       function(self, editor) end,
  onEditorFocusLost =  function(self, editor) end,
  onEditorFocusSet =   function(self, editor) end,
  onEditorAction =     function(self, editor, event) end, -- return false
  onEditorKeyDown =    function(self, editor, event) end, -- return false
  onEditorCharAdded =  function(self, editor, event) end, -- return false
  onEditorUserlistSelection = function(self, editor, event) end, -- return false
  onEditorUpdateUI =   function(self, editor, event) end,
  onEditorPainted =    function(self, editor, event) end,
  onEditorCallTip =    function(self, editor, tip, value, eval) end, -- return false
  onFiletreeActivate = function(self, tree, event, item) end, -- return false
  onFiletreeLDown =    function(self, tree, event, item) end,
  onFiletreeRDown =    function(self, tree, event, item) end,
  onMenuEditor =       function(self, menu, editor, event) end,
  onMenuEditorTab =    function(self, menu, notebook, event, index) end,
  onMenuFiletree =     function(self, menu, tree, event) end,
  onProjectPreLoad =   function(self, project) end, -- before project is changed
  onProjectLoad =      function(self, project) end, -- after project is changed
  onProjectClose =     function(self, project) end,
  onInterpreterLoad =  function(self, interpreter) end,
  onInterpreterClose = function(self, interpreter) end,
  onIdle =             function(self, event) end,
  onIdleOnce =         function(self, event) end,
  onAppFocusLost =     function(self, app) end,
  onAppFocusSet =      function(self, app) end,
  onAppLoad =          function(self, app) end,
  onAppClose =         function(self, app) end,
}

--[[ Uncomment this to see event names printed in the Output window
  for k in pairs(events) do
    if k:find("^on") then
      P[k] = k:find("^onEditor")
        and function(self, ed)
          -- document can be empty for newly added documents
          local doc = ide:GetDocument(ed)
          DisplayOutputLn(self:GetFileName(), k, doc and doc:GetFilePath() or "new document") end
        or function(self, ...)
          DisplayOutputLn(self:GetFileName(), k, ...) end
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

  P.onMenuEditorTab = function(self, menu, notebook, event, index)
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

  P.onEditorCharAdded = function(self, editor, event)
    DisplayOutputLn(self:GetFileName(), "onEditorCharAdded", event:GetKey())
  end

  P.onEditorKeyDown = function(self, editor, event)
    DisplayOutputLn(self:GetFileName(), "onEditorKeyDown", event:GetKeyCode())
  end

--]]

return P
