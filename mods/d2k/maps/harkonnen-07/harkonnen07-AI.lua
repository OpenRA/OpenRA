--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
EarlyGameStage = DateTime.Minutes(10)

InitialProductionDelay = {
	AtreidesMain =
	{
		easy = DateTime.Seconds(400),
		normal = DateTime.Seconds(300),
		hard = DateTime.Seconds(150)
	},

	AtreidesSmall =
	{
		easy = DateTime.Seconds(300),
		normal = DateTime.Seconds(200),
		hard = DateTime.Seconds(100)
	},

	Corrino =
	{
		easy = DateTime.Seconds(100),
		normal = DateTime.Seconds(120),
		hard = DateTime.Seconds(60)
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
	easy = { DateTime.Seconds(11), DateTime.Seconds(15) },
	normal = { DateTime.Seconds(10), DateTime.Seconds(12) },
	hard = { DateTime.Seconds(8), DateTime.Seconds(10) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(6), DateTime.Seconds(10) },
	normal = { DateTime.Seconds(4), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(2), DateTime.Seconds(4) }
}

AtreidesInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

AtreidesVehicleTypes = { "trike", "trike", "quad" }

AtreidesMainTankTypes =
{
	EarlyGame = { "combat_tank_a", "combat_tank_a" },
	LateGame = { "combat_tank_a", "siege_tank", "missile_tank", "sonic_tank" }
}

AtreidesSmallTankTypes = { "combat_tank_a", "combat_tank_a", "siege_tank" }

AtreidesStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }

CorrinoInfantryTypes = { "light_inf", "trooper", "sardaukar" }

ActivateAI = function()
	Defending[AtreidesMain] = {}
	Defending[AtreidesSmall] = {}
	Defending[Corrino] = {}
	-- this is also first attack timing
	AttackDelay[AtreidesMain] = 10000
	AttackDelay[AtreidesSmall] = 8000
	AttackDelay[Corrino] = 7000
	TimeBetweenAttacks[AtreidesMain] = 12000
	TimeBetweenAttacks[AtreidesSmall] = 5000
	TimeBetweenAttacks[Corrino] = 11000
	HarvesterCount[AtreidesMain] = 2
	HarvesterCount[AtreidesSmall] = 2

	Trigger.OnAllKilledOrCaptured(AtreidesSmallBase, function()
			TimeBetweenAttacks[AtreidesMain] = 0
	end)

	PatrolPoints[AtreidesMain] = {AtreidesPoint1.Location, AtreidesPoint2.Location, AtreidesPoint3.Location }
	PatrolPoints[AtreidesSmall] = {AtreidesPoint4.Location, AtreidesPoint5.Location, AtreidesPoint6.Location }
	DefencePerimeter[AtreidesMain] = Utils.Concat( GetCellsInRectangle(CPos.New(73,9), CPos.New(88,26)), GetCellsInRectangle(CPos.New(65,44), CPos.New(82,56)))
	DefencePerimeter[AtreidesSmall] = GetCellsInRectangle(CPos.New(5,18), CPos.New(16,32))
	DefencePerimeter[Corrino] = GetCellsInRectangle(CPos.New(89,27), CPos.New(95,35))

	IdlingUnits[AtreidesMain] = Utils.Concat(Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[Difficulty][1], InitialAtreidesPaths[1]), Reinforcements.Reinforce(AtreidesMain, InitialAtreidesReinforcements[Difficulty][2], InitialAtreidesPaths[2]))
	IdlingUnits[AtreidesSmall] = Reinforcements.Reinforce(AtreidesSmall, InitialAtreidesReinforcements[Difficulty][1], InitialAtreidesPaths[3])
	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino, InitialCorrinoReinforcements, InitialCorrinoPath)

	DefendAndRepairBase(AtreidesMain, AtreidesMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall, AtreidesSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Corrino, CorrinoBase, 0.75, AttackGroupSize[Difficulty])

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

	local infantryToBuildAtreides = function() return { Utils.Random(AtreidesInfantryTypes) } end
	local infantryToBuildCorrino = function() return { Utils.Random(CorrinoInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local tanksToBuildMain = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(AtreidesMainTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(AtreidesMainTankTypes["LateGame"]) }
		end
	end

	local tanksToBuildSmall = function() return { Utils.Random(AtreidesSmallTankTypes) } end
	local unitsToBuy = function() return { Utils.Random(AtreidesStarportTypes) } end
	local attackThresholdSize = {}
	attackThresholdSize[AtreidesMain] = AttackGroupSize[Difficulty] * 3
	attackThresholdSize[AtreidesSmall] = AttackGroupSize[Difficulty] * 2

	Trigger.AfterDelay(InitialProductionDelay["AtreidesMain"][Difficulty], function()
		ProduceUnits(AtreidesMain, ALightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesMain])
		ProduceUnits(AtreidesMain, AHeavyFactory1, delay, tanksToBuildMain, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesMain])
		ProduceUnits(AtreidesMain, AStarport, delay, unitsToBuy, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesMain])
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesSmall"][Difficulty], function()
		ProduceUnits(AtreidesSmall, ABarracks, delay, infantryToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesSmall])
		ProduceUnits(AtreidesSmall, AHeavyFactory2, delay, tanksToBuildSmall, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesSmall])
	end)

	Trigger.AfterDelay(InitialProductionDelay["Corrino"][Difficulty], function()
		ProduceUnits(Corrino, CBarracks, delay, infantryToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize[AtreidesSmall])
	end)

	if Difficulty == "normal" then
		AtreidesMain.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		AtreidesMain.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuildAtreides,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildMain
	}

	Trigger.OnBuildingPlaced(AtreidesMain, function(p, building)
		table.insert(AtreidesMainBase, building)
		DefendAndRepairBase(AtreidesMain, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(AtreidesMain, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize[AtreidesMain])
	end)

	Trigger.OnBuildingPlaced(AtreidesSmall, function(p, building)
		table.insert(AtreidesSmallBase, building)
		DefendAndRepairBase(AtreidesSmall, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(AtreidesSmall, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize[AtreidesSmall])
	end)

	ActivateCrushLogic()
end
