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

OrdosInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }

InitAIUnits = function()
	IdlingUnits[ordos] = Reinforcements.Reinforce(ordos, InitialOrdosReinforcements, OrdosPaths[2])
	IdlingUnits[ordos][#IdlingUnits + 1] = OTrooper1
	IdlingUnits[ordos][#IdlingUnits + 1] = OTrooper2
	IdlingUnits[ordos][#IdlingUnits + 1] = ORaider

	DefendAndRepairBase(ordos, OrdosBase, 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	LastHarvesterEaten[ordos] = true
	Trigger.AfterDelay(0, InitAIUnits)

	OConyard.Produce(AtreidesUpgrades[1])
	OConyard.Produce(AtreidesUpgrades[2])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	-- Finish the upgrades first before trying to build something
	Trigger.AfterDelay(DateTime.Seconds(14), function()
		ProduceUnits(ordos, OBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(ordos, OLightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
