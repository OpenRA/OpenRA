--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }
OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }
OrdosStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_o.starport", "combat_tank_o.starport", "siege_tank.starport", "missile_tank.starport" }

ActivateAI = function()
	IdlingUnits[ordos_main] = Reinforcements.Reinforce(ordos_main, InitialOrdosReinforcements[1], InitialOrdosPaths[1]), Reinforcements.Reinforce(ordos_main, InitialOrdosReinforcements[2], InitialOrdosPaths[2])
	IdlingUnits[ordos_small] = Reinforcements.Reinforce(ordos_small, InitialOrdosReinforcements[1], InitialOrdosPaths[3])

	DefendAndRepairBase(ordos_main, OrdosMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(ordos_small, OrdosSmallBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local tanksToBuild = function() return { Utils.Random(OrdosTankTypes) } end
	local unitsToBuy = function() return { Utils.Random(OrdosStarportTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(ordos_main, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(ordos_main, OLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(ordos_main, OHeavyFactory1, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(ordos_main, OStarport, delay, unitsToBuy, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(ordos_small, OBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(ordos_small, OLightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
end
