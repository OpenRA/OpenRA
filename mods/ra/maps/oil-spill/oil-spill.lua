--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SpawnBuildings =
{
	{ FCOMTopLeft, OilTopLeft1, OilTopLeft2, OilTopLeft3 },
	{ FCOMTopRight, OilTopRight1, OilTopRight2, OilTopRight3 },
	{ FCOMBottomLeft, OilBottomLeft1, OilBottomLeft2, OilBottomLeft3 },
	{ FCOMBottomRight, OilBottomRight1, OilBottomRight2, OilBottomRight3 },
}

WorldLoaded = function()
	for i = 0, 4 do
		local player = Player.GetPlayer("Multi" .. i)
		if player then
			Utils.Do(SpawnBuildings[player.Spawn], function(actor)
				actor.Owner = player
			end)
		end
	end
end
