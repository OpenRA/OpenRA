--[[-- Copy snippets from this file to `user.lua` --]]--

--[[ Add a shortcut to generate `~` if your keyboard doesn't have one
local G = ... -- this now points to the global environment in the script
local ide, wx, TR, ID = G.ide, G.wx, G.TR, G.ID
local postinit = ide.app.postinit
ide.app.postinit = function()
  if postinit then postinit() end
  local menu = ide.frame.menuBar:GetMenu(ide.frame.menuBar:FindMenu(TR("&Edit")))
  menu:Append(ID "tilde", "Tilde\tAlt-'")
  ide.frame:Connect(ID "tilde", wx.wxEVT_COMMAND_MENU_SELECTED,
    function () GetEditor():AddText("~") end)
end
--]]

--[[ Add `Evaluate in Console` option to the Edit menu
local G = ... -- this now points to the global environment in the script
local ide, wx, TR, ID = G.ide, G.wx, G.TR, G.ID
local postinit = ide.app.postinit
ide.app.postinit = function()
  if postinit then postinit() end
  local menu = ide.frame.menuBar:GetMenu(ide.frame.menuBar:FindMenu(TR("&Edit")))
  menu:Append(ID "eval", "Evaluate in Console\tCtrl-E")
  ide.frame:Connect(ID "eval", wx.wxEVT_COMMAND_MENU_SELECTED,
    function () ShellExecuteCode(GetEditor():GetSelectedText()) end)
  ide.frame:Connect(ID "eval", wx.wxEVT_UPDATE_UI,
    function (event) event:Enable(GetEditor() and #GetEditor():GetSelectedText() > 0) end)
end
--]]

--[[ Add `Zoom` menu to increase/decrease/reset font in the editor
local G = ... -- this now points to the global environment in the script
local ide, wx, TR, ID = G.ide, G.wx, G.TR, G.ID
local postinit = ide.app.postinit
ide.app.postinit = function()
  if postinit then postinit() end

  local zoomMenu = wx.wxMenu{
    {ID "zoomreset", "Zoom to 100%\tCtrl-0"},
    {ID "zoomin", "Zoom In\tCtrl-+"},
    {ID "zoomout", "Zoom Out\tCtrl--"},
  }
  local menu = ide.frame.menuBar:GetMenu(ide.frame.menuBar:FindMenu(TR("&View")))
  menu:Append(ID "zoom", "Zoom", zoomMenu)

  ide.frame:Connect(ID "zoomreset", wx.wxEVT_COMMAND_MENU_SELECTED,
    function () GetEditor():SetZoom(1) end)
  ide.frame:Connect(ID "zoomin", wx.wxEVT_COMMAND_MENU_SELECTED,
    function () GetEditor():SetZoom(GetEditor():GetZoom()+1) end)
  ide.frame:Connect(ID "zoomout", wx.wxEVT_COMMAND_MENU_SELECTED,
    function () GetEditor():SetZoom(GetEditor():GetZoom()-1) end)

  -- only enable if there is an editor
  for _, m in G.ipairs({"zoomreset", "zoomin", "zoomout"}) do
    ide.frame:Connect(ID(m), wx.wxEVT_UPDATE_UI,
      function (event) event:Enable(GetEditor() ~= nil) end)
  end
end
--]]
