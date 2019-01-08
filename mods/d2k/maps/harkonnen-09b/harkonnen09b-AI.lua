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

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesMainTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank", "sonic_tank" }
AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }
AtreidesStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }

CorrinoMainInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }
CorrinoTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" }
CorrinoStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_h.starport", "combat_tank_h.starport", "siege_tank.starport", "missile_tank.starport" }

ActivateAI = function()
	IdlingUnits[atreides_main] = Reinforcements.Reinforce(atreides_main, InitialAtreidesReinforcements[1], InitialAtreidesPaths[1]), Reinforcements.Reinforce(atreides_main, InitialAtreidesReinforcements[2], InitialAtreidesPaths[2])
	IdlingUnits[atreides_small] = Reinforcements.Reinforce(atreides_small, InitialAtreidesReinforcements[3], InitialAtreidesPaths[3])
	IdlingUnits[corrino_main] = Reinforcements.Reinforce(corrino_main, InitialCorrinoReinforcements[1], InitialCorrinoPaths[1]), Reinforcements.Reinforce(corrino_main, InitialCorrinoReinforcements[2], InitialCorrinoPaths[2])
	IdlingUnits[corrino_small] = Reinforcements.Reinforce(corrino_main, InitialCorrinoReinforcements[3], InitialCorrinoPaths[3])

	DefendAndRepairBase(atreides_main, AtreidesMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(atreides_small, AtreidesSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(corrino_main, CorrinoMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(corrino_small, CorrinoSmallBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local infantryToBuildCorrinoMain = function() return { Utils.Random(CorrinoMainInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreidesMain = function() return { Utils.Random(AtreidesMainTankTypes) } end
	local tanksToBuildAtreidesSmall = function() return { Utils.Random(AtreidesSmallTankTypes) } end
	local tanksToBuildCorrino = function() return { Utils.Random(CorrinoTankTypes) } end
	local unitsToBuyAtreides = function() return { Utils.Random(AtreidesStarportTypes) } end
	local unitsToBuyCorrino = function() return { Utils.Random(CorrinoStarportTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(atreides_main, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_main, ALightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_main, AHeavyFactory1, delay, tanksToBuildAtreidesMain, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_main, AStarport1, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(atreides_small, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_small, ALightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_small, AHeavyFactory2, delay, tanksToBuildAtreidesSmall, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(atreides_small, AStarport2, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(corrino_main, CBarracks1, delay, infantryToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(corrino_main, CHeavyFactory, delay, tanksToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(corrino_small, CBarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(corrino_small, CLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(corrino_small, CStarport, delay, unitsToBuyCorrino, AttackGroupSize[Difficulty], attackThresholdSize)
end
