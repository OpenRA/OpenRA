-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
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
  { ID_VIEWOUTLINE, TR("Outline Window")..KSC(ID_VIEWOUTLINE), TR("View the outline window"), wx.wxITEM_CHECK },
  { },
  { ID_VIEWTOOLBAR, TR("&Tool Bar")..KSC(ID_VIEWTOOLBAR), TR("Show/Hide the toolbar"), wx.wxITEM_CHECK },
  { ID_VIEWSTATUSBAR, TR("&Status Bar")..KSC(ID_VIEWSTATUSBAR), TR("Show/Hide the status bar"), wx.wxITEM_CHECK },
  { },
  { ID_VIEWDEFAULTLAYOUT, TR("&Default Layout")..KSC(ID_VIEWDEFAULTLAYOUT), TR("Reset to default layout") },
  { ID_VIEWFULLSCREEN, TR("Full &Screen")..KSC(ID_VIEWFULLSCREEN), TR("Switch to or from full screen mode") },
}

do -- Add zoom submenu
  local zoomMenu = wx.wxMenu{
    {ID_ZOOMRESET, TR("Zoom to 100%")..KSC(ID_ZOOMRESET)},
    {ID_ZOOMIN, TR("Zoom In")..KSC(ID_ZOOMIN)},
    {ID_ZOOMOUT, TR("Zoom Out")..KSC(ID_ZOOMOUT)},
  }

  frame:Connect(ID_ZOOMRESET, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() local editor = GetEditorWithFocus()
      if editor then editor:SetZoom(0) end end)
  frame:Connect(ID_ZOOMIN, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() local editor = GetEditorWithFocus()
      if editor then editor:SetZoom(editor:GetZoom()+1) end end)
  frame:Connect(ID_ZOOMOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() local editor = GetEditorWithFocus()
      if editor then editor:SetZoom(editor:GetZoom()-1) end end)

  -- only enable if there is an editor
  local iseditor = function (event) event:Enable(GetEditorWithFocus() ~= nil) end
  for _, id in ipairs({ID_ZOOMRESET, ID_ZOOMIN, ID_ZOOMOUT}) do
    frame:Connect(id, wx.wxEVT_UPDATE_UI, iseditor)
  end

  viewMenu:Append(ID_ZOOM, TR("Zoom"), zoomMenu)
end

menuBar:Append(viewMenu, TR("&View"))

local panels = {
  [ID_VIEWOUTPUT] = "bottomnotebook",
  [ID_VIEWFILETREE] = "projpanel",
  [ID_VIEWWATCHWINDOW] = "watchpanel",
  [ID_VIEWCALLSTACK] = "stackpanel",
  [ID_VIEWOUTLINE] = "outlinepanel",
  [ID_VIEWTOOLBAR] = "toolbar",
}

local function togglePanel(event)
  local panel = panels[event:GetId()]
  local pane = uimgr:GetPane(panel)
  local shown = not pane:IsShown()
  if not shown then pane:BestSize(pane.window:GetSize()) end
  pane:Show(shown)
  uimgr:Update()

  return shown
end

local function checkPanel(event)
  local pane = uimgr:GetPane(panels[event:GetId()])
  event:Enable(pane:IsOk()) -- disable if doesn't exist
  menuBar:Check(event:GetId(), pane:IsOk() and pane:IsShown())
end

frame:Connect(ID_VIEWDEFAULTLAYOUT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    uimgr:LoadPerspective(uimgr.defaultPerspective, true)
  end)
  
frame:Connect(ID_VIEWMINIMIZE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) ide.frame:Iconize(true) end)

frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
    ShowFullScreen(not frame:IsFullScreen())
  end)
frame:Connect(ID_VIEWFULLSCREEN, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)

frame:Connect(ID_VIEWOUTPUT, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWFILETREE, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWTOOLBAR, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWOUTLINE, wx.wxEVT_COMMAND_MENU_SELECTED, togglePanel)
frame:Connect(ID_VIEWWATCHWINDOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) if togglePanel(event) then DebuggerRefreshPanels() end end)
frame:Connect(ID_VIEWCALLSTACK, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) if togglePanel(event) then DebuggerRefreshPanels() end end)

frame:Connect(ID_VIEWSTATUSBAR, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    frame:GetStatusBar():Show(menuBar:IsChecked(event:GetId()))
    uimgr:Update()
  end)
frame:Connect(ID_VIEWSTATUSBAR, wx.wxEVT_UPDATE_UI,
  function (event) menuBar:Check(event:GetId(), frame:GetStatusBar():IsShown()) end)

for id in pairs(panels) do frame:Connect(id, wx.wxEVT_UPDATE_UI, checkPanel) end
