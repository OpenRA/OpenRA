-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local uimgr = frame.uimgr

local debugger = ide.debugger

local viewMenu = wx.wxMenu {
  -- NYI { ID "view.preferences", "&Preferences...", "Brings up dialog for settings (TODO)" },
  -- NYI { },
  { ID "view.filetree.show", "Project/&FileTree Window\tCtrl-Shift-P", "View the project/filetree window" },
  { ID "view.output.show", "&Output/Console Window\tCtrl-Shift-O", "View the output/console window" },
  { ID_VIEWWATCHWINDOW, "&Watch Window\tCtrl-Shift-W", "View the Watch window" },
  { ID_VIEWCALLSTACK, "&Stack Window\tCtrl-Shift-S", "View the Stack window" },
  { },
  { ID "view.defaultlayout", "&Default Layout", "Reset to default layout"},
  { ID_FULLSCREEN, "Full &Screen\tCtrl-Shift-A", "Switch to or from full screen mode"},
  { ID "view.style.loadconfig", "&Load Config Style...", "Load and apply style from config file (must contain .styles)"},
}
menuBar:Append(viewMenu, "&View")

--frame:Connect(ID "view.preferences", wx.wxEVT_COMMAND_MENU_SELECTED,preferencesDialog.show)

frame:Connect(ID "view.style.loadconfig", wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    LoadConfigStyle()
  end)
  
frame:Connect(ID "view.defaultlayout", wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:LoadPerspective(uimgr.defaultPerspective)
    uimgr:Update()
  end)
  
frame:Connect(ID "view.output.show", wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:GetPane("bottomnotebook"):Show(true)
    uimgr:Update()
  end)
  
frame:Connect(ID "view.filetree.show", wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:GetPane("projpanel"):Show(true)
    uimgr:Update()
  end)

frame:Connect(ID_FULLSCREEN, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
    pcall(function() ShowFullScreen(not frame:IsFullScreen()) end)
  end)

frame:Connect(ID_VIEWWATCHWINDOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) DebuggerCreateWatchWindow() end)

frame:Connect(ID_VIEWCALLSTACK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) DebuggerCreateStackWindow() end)
