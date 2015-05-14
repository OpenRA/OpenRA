-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local unpack = table.unpack or unpack

--[=[
-- tool definition
-- main entries are optional
tool = {
  fnmenu = function(frame,menubar),
  -- can be used for init
  -- and custom menu
  exec = {
    -- quick exec action
    name = "",
    description = "",
    fn = function(filename, projectdir),
  }
}

]=]

local toolArgs = {}
local cnt = 1

local function name2id(name) return ID("tools.exec."..name) end

do
  local maxcnt = 10

  local tools = {}
  for name,tool in pairs(ide.tools) do
    if (tool.exec and tool.exec.name) then
      tool.fname = name
      table.insert(tools,tool)
    end
  end

  table.sort(tools,function(a,b) return a.exec.name < b.exec.name end)

  -- todo config specifc ignore/priority list
  for _, tool in ipairs(tools) do
    local exec = tool.exec
    if (exec and cnt < maxcnt and exec.name and exec.fn and exec.description) then
      local id = name2id(tool.fname)
      table.insert(toolArgs,{id, exec.name, exec.description})
      -- flag it
      tool._execid = id
      cnt = cnt + 1
    end
  end
end

local function addHandler(menu, id, command, updateui)
  menu:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      local editor = GetEditor()
      if (not editor) then return end

      command(ide:GetDocument(editor):GetFilePath(), ide:GetProject())

      return true
    end)
  menu:Connect(id, wx.wxEVT_UPDATE_UI,
    updateui or function(event) event:Enable(GetEditor() ~= nil) end)
end

if (cnt > 1) then

  -- Build Menu
  local toolMenu = wx.wxMenu{
    unpack(toolArgs)
  }
  menuBar:Append(toolMenu, "&Tools")

  -- connect auto execs
  for _, tool in pairs(ide.tools) do
    if tool._execid then addHandler(toolMenu, tool._execid, tool.exec.fn) end
  end
end

-- Generate Custom Menus/Init
for _, tool in pairs(ide.tools) do
  if tool.fninit then tool.fninit(frame, menuBar) end
end

function ToolsAddTool(name, command, updateui)
  local toolMenu = ide:FindTopMenu('&Tools')
  if not toolMenu then
    local helpMenu, helpindex = ide:FindTopMenu('&Help')
    if not helpMenu then helpindex = ide:GetMenuBar():GetMenuCount() end

    toolMenu = wx.wxMenu{}
    menuBar:Insert(helpindex, toolMenu, "&Tools")
  end
  local id = name2id(name)
  toolMenu:Append(id, name)
  addHandler(toolMenu, id, command, updateui)
end

function ToolsRemoveTool(name)
  ide:RemoveMenuItem(name2id(name))
  local toolMenu, toolindex = ide:FindTopMenu('&Tools')
  if toolMenu and toolMenu:GetMenuItemCount() == 0 then
    ide:GetMenuBar():Remove(toolindex)
  end
end
