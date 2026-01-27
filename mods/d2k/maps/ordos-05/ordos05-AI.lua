--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(5)

InitialProductionDelay = {
	AtreidesMain =
	{
		easy = DateTime.Seconds(130),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(50)
	},

	AtreidesSmall1 =
	{
		easy = DateTime.Seconds(120),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(30)
	},

	AtreidesSmall2 =
	{
		easy = DateTime.Seconds(60),
		normal = DateTime.Seconds(30),
		hard = DateTime.Seconds(0)
	},
}

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyProductionDelays =
{
	easy = { DateTime.Seconds(7), DateTime.Seconds(11) },
	normal = { DateTime.Seconds(5), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}

LateProductionDelays =
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
	if house ~= AtreidesSmall3 then
		IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.InternalName], InitialReinforcementsPaths[house.InternalName])
	else
		IdlingUnits[house] = { }
	end

	DefendAndRepairBase(house, Base[house.InternalName], 0.75, AttackGroupSize[Difficulty])
end

ActivateAIProduction = function()
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
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuild = function() return EnemyTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["AtreidesMain"][Difficulty], function()
		ProduceUnits(AtreidesMain, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesMain, AHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesSmall1"][Difficulty], function()
		ProduceUnits(AtreidesSmall1, ABarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesSmall2"][Difficulty], function()
		ProduceUnits(AtreidesSmall2, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()
	AIProductionActivated = true

	if Difficulty == "normal" then
		AtreidesMain.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		AtreidesMain.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(AtreidesMain, function(p, building)
		table.insert(Base[AtreidesMainBase], building)
		DefendAndRepairBase(AtreidesMain, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(AtreidesMain, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end

ActivateAI = function()
	InitAIUnits(AtreidesMain)
	InitAIUnits(AtreidesSmall1)
	InitAIUnits(AtreidesSmall2)
	InitAIUnits(AtreidesSmall3)
	Defending[AtreidesMain] = {}
	Defending[AtreidesSmall1] = {}
	Defending[AtreidesSmall2] = {}
	Defending[AtreidesSmall3] = {}
	AttackDelay[AtreidesMain] = 14000 * DifficultyModifier[Difficulty]
	AttackDelay[AtreidesSmall1] = 14000 * DifficultyModifier[Difficulty]
	AttackDelay[AtreidesSmall2] = 14000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesMain] = 7000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall1] = 9000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall2] = 9000 * DifficultyModifier[Difficulty]
	HarvesterCount[AtreidesMain] = 2
	PatrolPoints[AtreidesMain] = { APatrolPoint1.Location, APatrolPoint2.Location, AtreidesRally8.Location }
	DefencePerimeter[AtreidesMain] = GetCellsInRectangle(CPos.New(3,3), CPos.New(18,27))
	DefencePerimeter[AtreidesSmall1] = GetCellsInRectangle(CPos.New(29,17), CPos.New(34,29))
	DefencePerimeter[AtreidesSmall2] = GetCellsInRectangle(CPos.New(39,46), CPos.New(48,59))
end
