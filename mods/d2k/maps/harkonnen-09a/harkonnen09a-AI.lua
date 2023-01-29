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

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesMainTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank", "sonic_tank" }
AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }
AtreidesStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }

CorrinoMainInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }
CorrinoMainTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" }
CorrinoSmallTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank" }
CorrinoStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_h.starport", "combat_tank_h.starport", "siege_tank.starport", "missile_tank.starport" }

ActivateAI = function()
	IdlingUnits[AtreidesMain] = Utils.Concat(Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[1], InitialAtreidesPaths[1]), Utils.Concat(Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[2], InitialAtreidesPaths[2]), Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[3], InitialAtreidesPaths[3])))
	IdlingUnits[AtreidesSmall1] = Utils.Concat(Reinforcements.Reinforce(AtreidesSmall1, InitialAtreidesReinforcements[4], InitialAtreidesPaths[4]), Reinforcements.Reinforce(AtreidesSmall1, InitialAtreidesReinforcements[5], InitialAtreidesPaths[5]))
	IdlingUnits[AtreidesSmall2] = Reinforcements.Reinforce(AtreidesSmall2, InitialAtreidesReinforcements[6], InitialAtreidesPaths[6])
	IdlingUnits[CorrinoMain] = Reinforcements.Reinforce(CorrinoMain, InitialCorrinoReinforcements, InitialCorrinoPaths[1])
	IdlingUnits[CorrinoSmall] = Reinforcements.Reinforce(CorrinoMain, InitialCorrinoReinforcements, InitialCorrinoPaths[2])

	DefendAndRepairBase(AtreidesMain, AtreidesMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall1, AtreidesSmall1Base, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall2, AtreidesSmall2Base, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(CorrinoMain, CorrinoMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(CorrinoSmall, CorrinoSmallBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local infantryToBuildCorrinoMain = function() return { Utils.Random(CorrinoMainInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreidesMain = function() return { Utils.Random(AtreidesMainTankTypes) } end
	local tanksToBuildAtreidesSmall = function() return { Utils.Random(AtreidesSmallTankTypes) } end
	local tanksToBuildCorrinoMain = function() return { Utils.Random(CorrinoMainTankTypes) } end
	local tanksToBuildCorrinoSmall = function() return { Utils.Random(CorrinoSmallTankTypes) } end
	local unitsToBuyAtreides = function() return { Utils.Random(AtreidesStarportTypes) } end
	local unitsToBuyCorrino = function() return { Utils.Random(CorrinoStarportTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(AtreidesMain, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesMain, ALightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesMain, AHeavyFactory1, delay, tanksToBuildAtreidesMain, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesMain, AStarport, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(AtreidesSmall1, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesSmall1, ALightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesSmall1, AHeavyFactory2, delay, tanksToBuildAtreidesSmall, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(AtreidesSmall2, ABarracks4, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(CorrinoMain, CBarracks1, delay, infantryToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(CorrinoMain, CLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(CorrinoMain, CHeavyFactory1, delay, tanksToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(CorrinoMain, CStarport, delay, unitsToBuyCorrino, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(CorrinoSmall, CBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(CorrinoSmall, CLightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(CorrinoSmall, CHeavyFactory2, delay, tanksToBuildCorrinoSmall, AttackGroupSize[Difficulty], attackThresholdSize)
end
