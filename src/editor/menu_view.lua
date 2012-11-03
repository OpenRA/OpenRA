-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local uimgr = frame.uimgr

local viewMenu = wx.wxMenu {
  { ID_VIEWFILETREE, TR("Project/&FileTree Window")..KSC(ID_VIEWFILETREE), TR("View the project/filetree window") },
  { ID_VIEWOUTPUT, TR("&Output/Console Window")..KSC(ID_VIEWOUTPUT), TR("View the output/console window") },
  { ID_VIEWWATCHWINDOW, TR("&Watch Window")..KSC(ID_VIEWWATCHWINDOW), TR("View the watch window") },
  { ID_VIEWCALLSTACK, TR("&Stack Window")..KSC(ID_VIEWCALLSTACK), TR("View the stack window") },
  { },
  { ID_VIEWDEFAULTLAYOUT, TR("&Default Layout")..KSC(ID_VIEWDEFAULTLAYOUT), TR("Reset to default layout") },
  { ID_VIEWFULLSCREEN, TR("Full &Screen")..KSC(ID_VIEWFULLSCREEN), TR("Switch to or from full screen mode") },
}
menuBar:Append(viewMenu, TR("&View"))

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
