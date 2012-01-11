-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local uimgr = frame.uimgr

local debugger = ide.debugger

local viewMenu = wx.wxMenu{
  -- NYI { ID "view.preferences", "&Preferences...", "Brings up dialog for settings (TODO)" },
  -- NYI { },
  { ID "view.filetree.show", "&FileTree Window", "View the filetree window" },
  { ID "view.output.show", "&Output/Shell Window", "View the output/shell window" },
  { ID "view.defaultlayout", "&Default Layout", "Reset to default ui layout"},
  { },
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

  
