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

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

InitialProductionDelay =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(15),
	hard = 0
}

AtreidesInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
AtreidesVehicleTypes = { "trike", "trike", "quad" }
AtreidesMainTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank", "sonic_tank" }
AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }
AtreidesStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }

CorrinoInfantryTypes = { "light_inf", "trooper", "sardaukar" }

ActivateAI = function()
	IdlingUnits[AtreidesMain] = Utils.Concat(Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[Difficulty][1], InitialAtreidesPaths[1]), Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[Difficulty][2], InitialAtreidesPaths[2]))
	IdlingUnits[AtreidesSmall] = Reinforcements.Reinforce(AtreidesSmall, InitialAtreidesReinforcements[Difficulty][1], InitialAtreidesPaths[3])
	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino, InitialCorrinoReinforcements, InitialCorrinoPath)

	DefendAndRepairBase(AtreidesMain, AtreidesMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall, AtreidesSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Corrino, CorrinoBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuildAtreides = function() return { Utils.Random(AtreidesInfantryTypes) } end
	local infantryToBuildCorrino = function() return { Utils.Random(CorrinoInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local tanksToBuildMain = function() return { Utils.Random(AtreidesMainTankTypes) } end
	local tanksToBuildSmall = function() return { Utils.Random(AtreidesSmallTankTypes) } end
	local unitsToBuy = function() return { Utils.Random(AtreidesStarportTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay[Difficulty], function()
		ProduceUnits(AtreidesMain, ALightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, AHeavyFactory1, delay, tanksToBuildMain, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, AStarport, delay, unitsToBuy, AttackGroupSize[Difficulty], attackThresholdSize)

		ProduceUnits(AtreidesSmall, ABarracks, delay, infantryToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesSmall, AHeavyFactory2, delay, tanksToBuildSmall, AttackGroupSize[Difficulty], attackThresholdSize)

		ProduceUnits(Corrino, CBarracks, delay, infantryToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
