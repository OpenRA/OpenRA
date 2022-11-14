--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Tick = function()
	if (Lighting.Red > 1.5) then
		Lighting.Red = Lighting.Red - 0.001
	end

	if (Lighting.Ambient < 0.5) then
		Lighting.Ambient = Lighting.Ambient + 0.001
	end
end
