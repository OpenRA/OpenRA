-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar

local openDocuments = ide.openDocuments

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
    fn = function(wxfilename,projectdir),
  }
}

]=]

local toolArgs = {{},}
local cnt = 1

-- fill in tools that have a automatic execution
-- function
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
  for i,tool in ipairs(tools) do
    local exec = tool.exec
    if (exec and cnt < maxcnt and exec.name and exec.fn and exec.description) then
      local id = ID("tools.exec."..tool.fname)
      table.insert(toolArgs,{id , exec.name, exec.description})
      -- flag it
      tool._execid = id
      cnt = cnt + 1
    end
  end
end

if (cnt > 1) then

  -- Build Menu
  local toolMenu = wx.wxMenu{
    unpack(toolArgs)
  }
  menuBar:Append(toolMenu, "&Tools")

  -- connect auto execs
  for name,tool in pairs(ide.tools) do
    if (tool._execid) then
      frame:Connect(tool._execid, wx.wxEVT_COMMAND_MENU_SELECTED,
        function (event)
          local editor = GetEditor()
          if (not editor) then return end

          local id = editor:GetId()
          local saved = false
          local fn = wx.wxFileName(openDocuments[id].filePath or "")
          fn:Normalize()

          tool.exec.fn(fn,ide.config.path.projectdir)

          return true
        end)
    end
  end
end

-- Generate Custom Menus/Init
for name,tool in pairs(ide.tools) do
  if (tool.fninit) then
    tool.fninit(frame,menuBar)
  end
end
