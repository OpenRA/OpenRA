-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local uimgr = frame.uimgr

local viewMenu = wx.wxMenu {
  { ID_VIEWFILETREE, TR("Project/&FileTree Window")..KSC(ID_VIEWFILETREE), TR("View the project/filetree window"), wx.wxITEM_CHECK },
  { ID_VIEWOUTPUT, TR("&Output/Console Window")..KSC(ID_VIEWOUTPUT), TR("View the output/console window"), wx.wxITEM_CHECK },
  { ID_VIEWWATCHWINDOW, TR("&Watch Window")..KSC(ID_VIEWWATCHWINDOW), TR("View the watch window"), wx.wxITEM_CHECK },
  { ID_VIEWCALLSTACK, TR("&Stack Window")..KSC(ID_VIEWCALLSTACK), TR("View the stack window"), wx.wxITEM_CHECK },
  { },
  { ID_VIEWDEFAULTLAYOUT, TR("&Default Layout")..KSC(ID_VIEWDEFAULTLAYOUT), TR("Reset to default layout") },
  { ID_VIEWFULLSCREEN, TR("Full &Screen")..KSC(ID_VIEWFULLSCREEN), TR("Switch to or from full screen mode") },
}
menuBar:Append(viewMenu, TR("&View"))

local panels = {
  [ID_VIEWOUTPUT] = "bottomnotebook",
  [ID_VIEWFILETREE] = "projpanel",
  [ID_VIEWWATCHWINDOW] = "watchpanel",
  [ID_VIEWCALLSTACK] = "stackpanel"
}

local function togglePanel(event)
  local panel = panels[event:GetId()]
  local mgr = ide.frame.uimgr
  local shown = not mgr:GetPane(panel):IsShown()
  mgr:GetPane(panel):Show(shown)
  mgr:Update()

  return shown
end

local function checkPanel(event)
  local menubar = ide.frame.menuBar
  local pane = ide.frame.uimgr:GetPane(panels[event:GetId()])
  menubar:Enable(event:GetId(), pane:IsOk()) -- disable if doesn't exist
  menubar:Check(event:GetId(), pane:IsOk() and pane:IsShown())
end

frame:Connect(ID_VIEWDEFAULTLAYOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:LoadPerspective(uimgr.defaultPerspective, true)
  end)
  
frame:Connect(ID_VIEWMINIMIZE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) ide.frame:Iconize(true) end)

frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
    pcall(function() ShowFullScreen(not frame:IsFullScreen()) end)
  end)
frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)

frame:Connect(ID_VIEWOUTPUT, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWFILETREE, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWWATCHWINDOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) if togglePanel(event) then DebuggerRefreshPanels() end end)
frame:Connect(ID_VIEWCALLSTACK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) if togglePanel(event) then DebuggerRefreshPanels() end end)

for id in pairs(panels) do frame:Connect(id, wx.wxEVT_UPDATE_UI, checkPanel) end
