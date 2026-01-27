--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(7)

InitialProductionDelay = {
	AtreidesMain =
	{
		easy = DateTime.Seconds(180),
		normal = DateTime.Seconds(120),
		hard = DateTime.Seconds(60)
	},

	AtreidesSmall =
	{
		easy = DateTime.Seconds(120),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(0)
	},

	CorrinoMain =
	{
		easy = DateTime.Seconds(160),
		normal = DateTime.Seconds(120),
		hard = DateTime.Seconds(80)
	},

	CorrinoSmall =
	{
		easy = DateTime.Seconds(80),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(30)
	}
}

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyProductionDelays =
{
	easy = { DateTime.Seconds(9), DateTime.Seconds(12) },
	normal = { DateTime.Seconds(6), DateTime.Seconds(8) },
	hard = { DateTime.Seconds(4), DateTime.Seconds(7) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

EnemyInfantryTypes =  { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesMainTankTypes = {
	EarlyGame = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank" },
	LateGame = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank", "sonic_tank" }
}

AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }

CorrinoMainInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

CorrinoTankTypes = {
	EarlyGame = { "combat_tank_h" },
	LateGame = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" }
}

ActivateAI = function()
	Defending[AtreidesMain] = {}
	Defending[AtreidesSmall] = {}
	Defending[CorrinoMain] = {}
	Defending[CorrinoSmall] = {}
	-- this is also first attack timing
	AttackDelay[AtreidesMain] = 15000 * DifficultyModifier[Difficulty]
	AttackDelay[AtreidesSmall] = 10000 * DifficultyModifier[Difficulty]
	AttackDelay[CorrinoMain] = 15000 * DifficultyModifier[Difficulty]
	AttackDelay[CorrinoSmall] = 15000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesMain] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall] = 4000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[CorrinoMain] = 8600 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[CorrinoSmall] = 8600 * DifficultyModifier[Difficulty]

	Trigger.OnKilledOrCaptured(AConYard1, function()
		TimeBetweenAttacks[AtreidesMain] = 0
		TimeBetweenAttacks[CorrinoMain] = 0
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoMainBase, function()
			TimeBetweenAttacks[CorrinoSmall] = 0
			TimeBetweenAttacks[AtreidesMain] = 0
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoSmallBase, function()
			TimeBetweenAttacks[CorrinoMain] = 0
	end)

	HarvesterCount[AtreidesMain] = 4
	HarvesterCount[AtreidesSmall] = 2
	HarvesterCount[CorrinoMain] = 4
	HarvesterCount[CorrinoSmall] = 1

	PatrolPoints[AtreidesMain] = {APatrolPoint1.Location, APatrolPoint2.Location, APatrolPoint3.Location, APatrolPoint4.Location }
	PatrolPoints[AtreidesSmall] = {APatrolPoint5.Location, APatrolPoint6.Location, APatrolPoint7.Location }
	PatrolPoints[CorrinoMain] = {APatrolPoint7.Location, AtreidesRally5.Location, CPatrolPoint1.Location }
	PatrolPoints[CorrinoSmall] = {CPatrolPoint2.Location, AtreidesRally4.Location, APatrolPoint4.Location }
	DefencePerimeter[AtreidesMain] = GetCellsInRectangle(CPos.New(25,58), CPos.New(47,70))
	DefencePerimeter[AtreidesSmall] = GetCellsInRectangle(CPos.New(103,5), CPos.New(119,18))
	DefencePerimeter[CorrinoMain] = GetCellsInRectangle(CPos.New(65,38), CPos.New(91,55))
	DefencePerimeter[CorrinoSmall] = GetCellsInRectangle(CPos.New(33,35), CPos.New(49,52))

	IdlingUnits[AtreidesMain] = Utils.Concat(Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[1], InitialAtreidesPaths[1]), Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[2], InitialAtreidesPaths[2]))
	IdlingUnits[AtreidesSmall] = Reinforcements.Reinforce(AtreidesSmall, InitialAtreidesReinforcements[3], InitialAtreidesPaths[3])
	IdlingUnits[CorrinoMain] = Utils.Concat(Reinforcements.Reinforce(CorrinoMain, InitialCorrinoReinforcements[1], InitialCorrinoPaths[1]), Reinforcements.Reinforce(CorrinoMain, InitialCorrinoReinforcements[2], InitialCorrinoPaths[2]))
	IdlingUnits[CorrinoSmall] = Reinforcements.Reinforce(CorrinoMain, InitialCorrinoReinforcements[3], InitialCorrinoPaths[3])

	DefendAndRepairBase(AtreidesMain, AtreidesMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall, AtreidesSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(CorrinoMain, CorrinoMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(CorrinoSmall, CorrinoSmallBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function(player)
		if EmergencyBuildRate[player] and Difficulty ~= "easy" then
			return 1
		end
		if EarlyGameStage >= DateTime.GameTime then
			return Utils.RandomInteger(EarlyProductionDelays[Difficulty][1], EarlyProductionDelays[Difficulty][2] + 1)
		else
			return Utils.RandomInteger(LateProductionDelays[Difficulty][1], LateProductionDelays[Difficulty][2] + 1)
		end
	end

	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local infantryToBuildCorrinoMain = function() return { Utils.Random(CorrinoMainInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreidesMain = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(AtreidesMainTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(AtreidesMainTankTypes["LateGame"]) }
		end
	end

	local tanksToBuildAtreidesSmall = function() return { Utils.Random(AtreidesSmallTankTypes) } end
	local tanksToBuildCorrino = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(CorrinoTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(CorrinoTankTypes["LateGame"]) }
		end
	end

	local attackThresholdSize = AttackGroupSize[Difficulty] * 3
	local attackThresholdSizeSmall = AttackGroupSize[Difficulty] * 2
	local attackThresholdSizeEmperor = AttackGroupSize[Difficulty] * 5

	Trigger.AfterDelay(InitialProductionDelay["AtreidesMain"][Difficulty], function ()
		ProduceUnits(AtreidesMain, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, ALightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, AHeavyFactory1, delay, tanksToBuildAtreidesMain, AttackGroupSize[Difficulty], attackThresholdSize)
		--ProduceUnits(AtreidesMain, AStarport1, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], AttackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesSmall"][Difficulty], function ()
		ProduceUnits(AtreidesSmall, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmall)
		ProduceUnits(AtreidesSmall, ALightFactory2, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmall)
		ProduceUnits(AtreidesSmall, AHeavyFactory2, delay, tanksToBuildAtreidesSmall, AttackGroupSize[Difficulty], attackThresholdSizeSmall)
	end)

	Trigger.AfterDelay(InitialProductionDelay["CorrinoMain"][Difficulty], function ()
		ProduceUnits(CorrinoMain, CBarracks1, delay, infantryToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSizeEmperor)
		ProduceUnits(CorrinoMain, CHeavyFactory, delay, tanksToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSizeEmperor)
	end)

	Trigger.AfterDelay(InitialProductionDelay["CorrinoSmall"][Difficulty], function ()
		ProduceUnits(CorrinoSmall, CBarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmall)
		ProduceUnits(CorrinoSmall, CLightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmall)
	end)

	local productionTypesAtreidis =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildAtreidesMain
	}

	local productionTypesCorrino =
	{
		barracks = infantryToBuildCorrinoMain,
		heavy_factory = tanksToBuildCorrino
	}

	if Difficulty == "normal" then
		AtreidesMain.GrantCondition("base-rebuilder")
		AtreidesSmall.GrantCondition("base-rebuilder2")
		CorrinoMain.GrantCondition("base-rebuilder3")
	end

	if Difficulty == "hard" then
		AtreidesMain.GrantCondition("defense-rebuilder")
		AtreidesSmall.GrantCondition("defense-rebuilder2")
		CorrinoMain.GrantCondition("defense-rebuilder3")
	end

	Trigger.OnBuildingPlaced(AtreidesMain, function(p, building)
		table.insert(AtreidesMainBase, building)
		DefendAndRepairBase(AtreidesMain, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesAtreidis[building.Type] == nil then return end
		ProduceUnits(AtreidesMain, building, delay, productionTypesAtreidis[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(AtreidesSmall, function(p, building)
		table.insert(AtreidesSmallBase, building)
		DefendAndRepairBase(AtreidesSmall, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesAtreidis[building.Type] == nil then return end
		ProduceUnits(AtreidesSmall, building, delay, productionTypesAtreidis[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(CorrinoMain, function(p, building)
		table.insert(CorrinoMainBase, building)
		DefendAndRepairBase(CorrinoMain, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesCorrino[building.Type] == nil then return end
		ProduceUnits(CorrinoMain, building, delay, productionTypesCorrino[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()
end
