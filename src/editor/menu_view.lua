-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local uimgr = frame.uimgr

local viewMenu = wx.wxMenu {
  { ID_VIEWFILETREE, "Project/&FileTree Window"..KSC(ID_VIEWFILETREE), "View the project/filetree window" },
  { ID_VIEWOUTPUT, "&Output/Console Window"..KSC(ID_VIEWOUTPUT), "View the output/console window" },
  { ID_VIEWWATCHWINDOW, "&Watch Window"..KSC(ID_VIEWWATCHWINDOW), "View the Watch window" },
  { ID_VIEWCALLSTACK, "&Stack Window"..KSC(ID_VIEWCALLSTACK), "View the Stack window" },
  { },
  { ID_VIEWDEFAULTLAYOUT, "&Default Layout"..KSC(ID_VIEWDEFAULTLAYOUT), "Reset to default layout"},
  { ID_VIEWFULLSCREEN, "Full &Screen"..KSC(ID_VIEWFULLSCREEN), "Switch to or from full screen mode"},
}
menuBar:Append(viewMenu, "&View")

frame:Connect(ID_VIEWDEFAULTLAYOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:LoadPerspective(uimgr.defaultPerspective)
    uimgr:Update()
  end)
  
frame:Connect(ID_VIEWOUTPUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    uimgr:GetPane("bottomnotebook"):Show(true)
    uimgr:Update()
  end)
  
frame:Connect(ID_VIEWFILETREE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    uimgr:GetPane("projpanel"):Show(true)
    uimgr:Update()
  end)

frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
    pcall(function() ShowFullScreen(not frame:IsFullScreen()) end)
  end)
frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)

frame:Connect(ID_VIEWWATCHWINDOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () DebuggerCreateWatchWindow() end)

frame:Connect(ID_VIEWCALLSTACK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function () DebuggerCreateStackWindow() end)
