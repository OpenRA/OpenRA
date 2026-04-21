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
		easy = DateTime.Seconds(130),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(50)
	},

	HarkonnenSmall =
	{
		easy = DateTime.Seconds(120),
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
	easy = { DateTime.Seconds(7), DateTime.Seconds(10) },
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

AtreidesTankTypes = { "combat_tank_a", "siege_tank", "missile_tank" }

HarkonnenTankTypes = { "combat_tank_h", "siege_tank" }

InitAIUnits = function(house)
	LastHarvesterEaten[house] = true
	IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.InternalName], InitialReinforcementsPaths[house.InternalName])
	DefendAndRepairBase(house, Base[house.InternalName], 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	Defending[Atreides] = {}
	Defending[Harkonnen] = {}
	AttackDelay[Atreides] = 15000 * DifficultyModifier[Difficulty]
	AttackDelay[Harkonnen] = 12000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Atreides] = 4000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 6000 * DifficultyModifier[Difficulty]
	HarvesterCount[Atreides] = 2
	HarvesterCount[Harkonnen] = 0
	PatrolPoints[Atreides] = { HarkonnenRally7.Location, APatrolPoint1.Location, APatrolPoint2.Location }
	PatrolPoints[Harkonnen] = {HarkonnenRally4.Location, HarkonnenRally5.Location, HPatrolPoint1.Location, HPatrolPoint2.Location }
	DefencePerimeter[Atreides] = GetCellsInRectangle(CPos.New(3,4), CPos.New(20,27))
	DefencePerimeter[Harkonnen] = Utils.Concat(GetCellsInRectangle(CPos.New(15,73), CPos.New(44,80)), GetCellsInRectangle(CPos.New(27,55), CPos.New(38,68)))
	InitAIUnits(Atreides)
	InitAIUnits(Harkonnen)

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
	local tanksToBuildAtreides = function() return { Utils.Random(AtreidesTankTypes) } end
	local tanksToBuildHarkonnen = function() return { Utils.Random(HarkonnenTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["AtreidesMain"][Difficulty], function()
		ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Atreides, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["HarkonnenSmall"][Difficulty], function()
		ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HHeavyFactory, delay, tanksToBuildHarkonnen, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Atreides.GrantCondition("base-rebuilder")
		Harkonnen.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Atreides.GrantCondition("defense-rebuilder")
		Harkonnen.GrantCondition("defense-rebuilder2")
	end

	local productionTypesAtreides =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildAtreides
	}

	local productionTypesHarkonnen =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuildHarkonnen
	}

	Trigger.OnBuildingPlaced(Atreides, function(p, building)
		table.insert(Base["Atreides"], building)
		DefendAndRepairBase(Atreides, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesAtreides[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypesAtreides[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(Base["Harkonnen"], building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesHarkonnen[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypesHarkonnen[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
