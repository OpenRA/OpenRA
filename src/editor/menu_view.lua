-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame    = ide.frame
local menuBar  = frame.menuBar
local vsplitter= frame.vsplitter
local sidenotebook = vsplitter.sidenotebook
local splitter = vsplitter.splitter
local errorlog = splitter.bottomnotebook.errorlog
local notebook = splitter.notebook

local debugger 		= ide.debugger

local viewMenu = wx.wxMenu{
-- NYI		{ ID "view.preferences", "&Preferences...", "Brings up dialog for settings (TODO)" },
-- NYI		{ },
		{ ID "view.filetree.show", "View &FileTree Window",  "View or Hide the filetree window",wx.wxITEM_CHECK  },
		{ ID "view.output.show",   "View &Output/Shell Window", "View or Hide the output/shell window",wx.wxITEM_CHECK  },
		{ },
		{ ID "view.style.loadconfig", "&Load Config Style...", "Load and apply style from config file (must contain .styles)"},
		}
menuBar:Append(viewMenu, "&View")

menuBar:Check(ID "view.filetree.show", true)
menuBar:Check(ID "view.output.show", true)

--frame:Connect(ID "view.preferences", wx.wxEVT_COMMAND_MENU_SELECTED,preferencesDialog.show)

frame:Connect(ID "view.style.loadconfig", wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			LoadConfigStyle()
		end)
		
frame:Connect(ID "view.output.show", wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			local w, h = frame:GetClientSizeWH()
			if splitter:IsSplit() then
				ide.config.view.splitterheight = h - splitter:GetSashPosition()
				splitter:Unsplit()
			else
				splitter:SplitHorizontally(notebook, splitter.bottomnotebook, h - ide.config.view.splitterheight)
			end
		end)
		
frame:Connect(ID "view.filetree.show", wx.wxEVT_COMMAND_MENU_SELECTED,
		function (event)
			if vsplitter:IsSplit() then
				ide.config.view.vsplitterpos = vsplitter:GetSashPosition()
				vsplitter:Unsplit(sidenotebook)
			else
				vsplitter:SplitVertically(sidenotebook,splitter,ide.config.view.vsplitterpos)
			end
		end)
