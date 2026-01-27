--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(10)

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyProductionDelays =
{

	easy = { DateTime.Seconds(12), DateTime.Seconds(15) },
	normal = { DateTime.Seconds(10), DateTime.Seconds(13) },
	hard = { DateTime.Seconds(9), DateTime.Seconds(10) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(5), DateTime.Seconds(8) },
	normal = { DateTime.Seconds(3), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(2), DateTime.Seconds(4) }
}

InitialProductionDelay = {
	AtreidesMain =
	{
		easy = DateTime.Seconds(200),
		normal = DateTime.Seconds(150),
		hard = DateTime.Seconds(100)
	},

	AtreidesSmall =
	{
		easy = DateTime.Seconds(160),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(50)
	},

	CorrinoMain =
	{
		easy = DateTime.Seconds(250),
		normal = DateTime.Seconds(200),
		hard = DateTime.Seconds(150)
	},

	CorrinoSmall =
	{
		easy = DateTime.Seconds(100),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(60)
	}
}

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesMainTankTypes = {
	EarlyGame = { "combat_tank_a", "combat_tank_a", "siege_tank", "missile_tank" },
	LateGame = {"combat_tank_a", "siege_tank", "missile_tank", "sonic_tank"}
}

AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }

CorrinoMainInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

CorrinoMainTankTypes = {
	EarlyGame = { "combat_tank_h" },
	LateGame = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" }
}

CorrinoSmallTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank" }

ActivateAI = function()
	Defending[AtreidesMain] = {}
	Defending[AtreidesSmall1] = {}
	Defending[AtreidesSmall2] = {}
	Defending[CorrinoMain] = {}
	Defending[CorrinoSmall] = {}
	-- this is also first attack timing
	AttackDelay[AtreidesMain] = 16000
	AttackDelay[AtreidesSmall1] = 16000
	AttackDelay[AtreidesSmall2] = 16000
	AttackDelay[CorrinoMain] = 16000
	AttackDelay[CorrinoSmall] = 16000
	TimeBetweenAttacks[AtreidesMain] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall1] = 1000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall2] = 4000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[CorrinoMain] = 12000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[CorrinoSmall] = 10000 * DifficultyModifier[Difficulty]

	Trigger.OnAllKilledOrCaptured(AtreidesSmall1Base, function()
			TimeBetweenAttacks[AtreidesMain] = 0
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoMainBase, function()
			TimeBetweenAttacks[CorrinoSmall] = 0
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoSmallBase, function()
			TimeBetweenAttacks[CorrinoMain] = 0
	end)

	HarvesterCount[AtreidesMain] = 4
	HarvesterCount[AtreidesSmall1] = 2
	HarvesterCount[AtreidesSmall2] = 2
	HarvesterCount[CorrinoMain] = 4
	HarvesterCount[CorrinoSmall] = 2

	PatrolPoints[AtreidesMain] = {AtreidesPatrolPoint3.Location, AtreidesPatrolPoint4.Location, AtreidesPatrolPoint2.Location, AtreidesRally10.Location, AtreidesPatrolPoint5.Location }
	PatrolPoints[AtreidesSmall1] = {AtreidesPatrolPoint1.Location, AtreidesPatrolPoint2.Location, AtreidesRally3.Location }
	PatrolPoints[CorrinoMain] = {CorrinoPatrolPoint1.Location, CorrinoPatrolPoint2.Location, AtreidesPatrolPoint5.Location }
	PatrolPoints[CorrinoSmall] = {CorrinoPatrolPoint3.Location, AtreidesPatrolPoint3.Location, AtreidesPatrolPoint5.Location }
	DefencePerimeter[AtreidesMain] = GetCellsInRectangle(CPos.New(36,15), CPos.New(57,30))
	DefencePerimeter[AtreidesSmall1] = GetCellsInRectangle(CPos.New(34,51), CPos.New(46,74))
	DefencePerimeter[AtreidesSmall2] = GetCellsInRectangle(CPos.New(18,32), CPos.New(30,37))
	DefencePerimeter[CorrinoMain] = GetCellsInRectangle(CPos.New(3,13), CPos.New(24,26))
	DefencePerimeter[CorrinoSmall] = GetCellsInRectangle(CPos.New(74,27), CPos.New(96,48))

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

	local tanksToBuildCorrinoMain = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(CorrinoMainTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(CorrinoMainTankTypes["LateGame"]) }
		end
	end

	local tanksToBuildCorrinoSmall = function() return { Utils.Random(CorrinoSmallTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 3
	local attackThresholdSizeCorrino = AttackGroupSize[Difficulty] * 4
	local attackThresholdSizeSmallBase = AttackGroupSize[Difficulty] * 2
	Trigger.AfterDelay(InitialProductionDelay["AtreidesMain"][Difficulty], function ()
		ProduceUnits(AtreidesMain, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, ALightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, AHeavyFactory1, delay, tanksToBuildAtreidesMain, AttackGroupSize[Difficulty], attackThresholdSize)
		--ProduceUnits(AtreidesMain, AStarport, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], AttackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesSmall"][Difficulty], function ()
		ProduceUnits(AtreidesSmall1, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
		ProduceUnits(AtreidesSmall1, ALightFactory2, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
		ProduceUnits(AtreidesSmall1, AHeavyFactory2, delay, tanksToBuildAtreidesSmall, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
	end)

	ProduceUnits(AtreidesSmall2, ABarracks4, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)

	Trigger.AfterDelay(InitialProductionDelay["CorrinoMain"][Difficulty], function ()
		ProduceUnits(CorrinoMain, CBarracks1, delay, infantryToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSizeCorrino)
		ProduceUnits(CorrinoMain, CLightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSizeCorrino)
		ProduceUnits(CorrinoMain, CHeavyFactory1, delay, tanksToBuildCorrinoMain, AttackGroupSize[Difficulty], attackThresholdSizeCorrino)
		--ProduceUnits(CorrinoMain, CStarport, delay, unitsToBuyCorrino, AttackGroupSize[Difficulty], AttackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["CorrinoSmall"][Difficulty], function ()
		ProduceUnits(CorrinoSmall, CBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
		ProduceUnits(CorrinoSmall, CLightFactory2, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
		ProduceUnits(CorrinoSmall, CHeavyFactory2, delay, tanksToBuildCorrinoSmall, AttackGroupSize[Difficulty], attackThresholdSizeSmallBase)
	end)

	local crusherTypes = { "combat_tank_a", "combat_tank_a.starport", "combat_tank_h", "combat_tank_h.starport" }
	local crusherFactories = { AStarport, AHeavyFactory1, AHeavyFactory2, CHeavyFactory1, CHeavyFactory2, CStarport }
	ActivateCrusherLogic(crusherTypes, crusherFactories)

	if Difficulty == "normal" then
		AtreidesMain.GrantCondition("base-rebuilder")
		AtreidesSmall1.GrantCondition("base-rebuilder2")
		CorrinoSmall.GrantCondition("base-rebuilder3")
	end

	if Difficulty == "hard" then
		AtreidesMain.GrantCondition("defense-rebuilder")
		AtreidesSmall1.GrantCondition("defense-rebuilder2")
		CorrinoSmall.GrantCondition("defense-rebuilder3")
	end

	local productionTypesAtreidis =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildAtreidesMain
	}

	local productionTypesCorrino =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildCorrinoSmall
	}

	Trigger.OnBuildingPlaced(AtreidesMain, function(p, building)
		table.insert(AtreidesMainBase, building)
		DefendAndRepairBase(AtreidesMain, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesAtreidis[building.Type] == nil then return end
		ProduceUnits(AtreidesMain, building, delay, productionTypesAtreidis[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(AtreidesSmall1, function(p, building)
		table.insert(AtreidesSmallBase, building)
		DefendAndRepairBase(AtreidesSmall1, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesAtreidis[building.Type] == nil then return end
		ProduceUnits(AtreidesSmall1, building, delay, productionTypesAtreidis[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(CorrinoMain, function(p, building)
		table.insert(CorrinoMainBase, building)
		DefendAndRepairBase(CorrinoMain, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesCorrino[building.Type] == nil then return end
		ProduceUnits(CorrinoMain, building, delay, productionTypesCorrino[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
