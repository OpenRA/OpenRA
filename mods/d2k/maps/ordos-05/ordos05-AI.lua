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
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
EnemyVehicleTypes = { "trike", "trike", "quad" }
EnemyTankType = { "combat_tank_a" }

InitAIUnits = function(house)
	LastHarvesterEaten[house] = true
	if house ~= atreides_small_3 then
		IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.InternalName], InitialReinforcementsPaths[house.InternalName])
	else
		IdlingUnits[house] = { }
	end

	DefendAndRepairBase(house, Base[house.InternalName], 0.75, AttackGroupSize[Difficulty])
end

ActivateAIProduction = function()
	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuild = function() return EnemyTankType end
	local attackTresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(atreides_main, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(atreides_main, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(atreides_main, AHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	ProduceUnits(atreides_small_1, ABarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	ProduceUnits(atreides_small_2, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	AIProductionActivated = true
end

ActivateAI = function()
	InitAIUnits(atreides_main)
	InitAIUnits(atreides_small_1)
	InitAIUnits(atreides_small_2)
	InitAIUnits(atreides_small_3)
end
