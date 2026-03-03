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
	Harkonnen =
	{
		easy = DateTime.Seconds(60),
		normal = DateTime.Seconds(30),
		hard = DateTime.Seconds(0)
	},
	HarkonnenSmall =
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
	easy = { DateTime.Seconds(7), DateTime.Seconds(10) },
	normal = { DateTime.Seconds(4), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenVehicleTypes = { "trike", "trike", "quad" }
HarkonnenTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank" },
	LateGame = { "combat_tank_h", "siege_tank", "missile_tank", "devastator" }
}

ActivateAI = function()
	Defending[Harkonnen] = { }
	Defending[HarkonnenSmall] = { }
	AttackDelay[Harkonnen] = 12000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 12000 * DifficultyModifier[Difficulty]
	AttackDelay[HarkonnenSmall] = 12000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[HarkonnenSmall] = 11000 * DifficultyModifier[Difficulty]
	HarvesterCount[Harkonnen] = 2
	PatrolPoints[Harkonnen] = { HPatrolPoint1.Location, HPatrolPoint2.Location,EnemyReinforcementsPoint4.Location }
	PatrolPoints[HarkonnenSmall] = { EnemyReinforcementsPoint1.Location, TileReveal1.Location, UnitSpawn3.Location, EnemyReinforcementsPoint5.Location }
	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(6,8), CPos.New(22,26))
	DefencePerimeter[HarkonnenSmall] = GetCellsInRectangle(CPos.New(50,33), CPos.New(60,50))
	LastHarvesterEaten[Harkonnen] = true
	LastHarvesterEaten[HarkonnenSmall] = true

	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[1], InitialUnitSpawnPaths[1])
	IdlingUnits[HarkonnenSmall] = Reinforcements.Reinforce(HarkonnenSmall, EnemyInitialUnitSpawn[2], InitialUnitSpawnPaths[2])
	Reinforcements.Reinforce(HarkonnenSmall, EnemyInitialUnitSpawn[3], InitialUnitSpawnPaths[3])

	DefendAndRepairBase(Harkonnen, HarkonnenMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(HarkonnenSmall, HarkonnenSmallBase, 0.75, AttackGroupSize[Difficulty])

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

	local infantryToBuild = function() return { Utils.Random(HarkonnenInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local tanksToBuild = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(HarkonnenTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(HarkonnenTankTypes["LateGame"]) }
		end
	end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["Harkonnen"][Difficulty], function()
		ProduceUnits(Harkonnen, HBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["HarkonnenSmall"][Difficulty], function()
		ProduceUnits(HarkonnenSmall, HBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Harkonnen.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		Harkonnen.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenMainBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
