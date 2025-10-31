--[[
   Copyright (c) The OpenRA Developers and Contributors
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

ProductionDelays =
{
	easy = { DateTime.Seconds(16), DateTime.Seconds(30) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesTankTypes = { "combat_tank_a", "siege_tank", "missile_tank" }
HarkonnenTankTypes = { "combat_tank_h", "siege_tank" }

InitAIUnits = function(house)
	LastHarvesterEaten[house] = true
	IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.InternalName], InitialReinforcementsPaths[house.InternalName])

	DefendAndRepairBase(house, Base[house.InternalName], 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	InitAIUnits(Atreides)
	InitAIUnits(Harkonnen)

	local delay = function() return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreides = function() return { Utils.Random(AtreidesTankTypes) } end
	local tanksToBuildHarkonnen = function() return { Utils.Random(HarkonnenTankTypes) } end
	local attackTresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Atreides, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackTresholdSize)

	ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Harkonnen, HHeavyFactory, delay, tanksToBuildHarkonnen, AttackGroupSize[Difficulty], attackTresholdSize)
end
