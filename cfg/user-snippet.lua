--[[-- Copy snippets from this file to `user.lua` --]]--

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

--[[ An example of how individual keywords can be styled
local G = ... -- this now points to the global environment in the script
local luaspec = G.ide.specs['lua']

local num = #luaspec.keywords
-- take a new slot in the list of keywords (starting from 1)
luaspec.keywords[num+1] = 'return'
-- remove 'return' from the list of "regular" keywords
luaspec.keywords[1] = luaspec.keywords[1]:gsub(' return', '')

-- assign new style to the added slot (starting from 0)
styles["keywords"..num] = {fg = {240, 0, 0}, b = true}
--]]
