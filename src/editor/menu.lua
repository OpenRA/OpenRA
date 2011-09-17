-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- CONNECTING STUFF

-- force all the wxEVT_UPDATE_UI handlers to be called
function UpdateUIMenuItems()
	local frame = ide.frame
	if frame and frame:GetMenuBar() then
		for n = 0, frame:GetMenuBar():GetMenuCount()-1 do
			frame:GetMenuBar():GetMenu(n):UpdateUI()
		end
	end
end


dofile "src/editor/menu_file.lua"
dofile "src/editor/menu_edit.lua"
dofile "src/editor/menu_search.lua"
dofile "src/editor/menu_view.lua"
dofile "src/editor/menu_debug.lua"
dofile "src/editor/menu_tools.lua"
dofile "src/editor/menu_help.lua"

ide.frame:SetMenuBar(ide.frame.menuBar )