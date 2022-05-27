--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

HarkonnenInfantryTypes = { "light_inf" }

ActivateAI = function()
	IdlingUnits[harkonnen] = { }
	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local toBuild = function() return HarkonnenInfantryTypes end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	DefendAndRepairBase(harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
	ProduceUnits(harkonnen, HBarracks, delay, toBuild, AttackGroupSize[Difficulty], attackThresholdSize)
end
