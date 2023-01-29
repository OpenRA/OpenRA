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

EnemyInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }

OrdosVehicleTypes = { "raider", "raider", "quad" }
OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank", "deviator" }
OrdosStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_o.starport", "combat_tank_o.starport", "siege_tank.starport", "missile_tank.starport" }

AtreidesVehicleTypes = { "trike", "trike", "quad" }
AtreidesTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }
AtreidesStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }

MercenaryTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }

ActivateAI = function()
	IdlingUnits[Ordos] = Utils.Concat(Reinforcements.Reinforce(Ordos, InitialOrdosReinforcements[1], InitialOrdosPaths[1]), Reinforcements.Reinforce(Ordos, InitialOrdosReinforcements[2], InitialOrdosPaths[2]))
	IdlingUnits[AtreidesEnemy] = Reinforcements.Reinforce(AtreidesEnemy, InitialAtreidesReinforcements, InitialAtreidesPath)
	IdlingUnits[AtreidesNeutral] = { }
	IdlingUnits[MercenaryEnemy] = Reinforcements.Reinforce(MercenaryEnemy, InitialMercenaryReinforcements, InitialMercenaryPath)
	IdlingUnits[MercenaryAlly] = { }

	DefendAndRepairBase(Ordos, OrdosBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesEnemy, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(MercenaryEnemy, MercenaryBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local vehilcesToBuildOrdos = function() return { Utils.Random(OrdosVehicleTypes) } end
	local vehilcesToBuildAtreides = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local tanksToBuildOrdos = function() return { Utils.Random(OrdosTankTypes) } end
	local tanksToBuildAtreides = function() return { Utils.Random(AtreidesTankTypes) } end
	local tanksToBuildMercenary = function() return { Utils.Random(MercenaryTankTypes) } end
	local unitsToBuyOrdos = function() return { Utils.Random(OrdosStarportTypes) } end
	local unitsToBuyAtreides = function() return { Utils.Random(AtreidesStarportTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(Ordos, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Ordos, OLightFactory, delay, vehilcesToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Ordos, OHeavyFactory, delay, tanksToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Ordos, OStarport, delay, unitsToBuyOrdos, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(AtreidesEnemy, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesEnemy, ALightFactory, delay, vehilcesToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesEnemy, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesEnemy, AStarport, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(MercenaryEnemy, MHeavyFactory, delay, tanksToBuildMercenary, AttackGroupSize[Difficulty], attackThresholdSize)
end
