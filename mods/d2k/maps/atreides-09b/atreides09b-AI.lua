--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = 10000

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyProductionDelays =
{
	easy = { 600, 800 },
	normal = {500, 600 },
	hard = { 400, 500 }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(3), DateTime.Seconds(5) },
	normal = { 30, 60 },
	hard = { 1, 10 }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenVehicleTypes = { "trike", "trike", "quad" }

HarkonnenTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" },
	LateGame = { "combat_tank_h", "siege_tank", "missile_tank", "devastator" }
}

HarkonnenSmallTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank" }

CorrinoInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

CorrinoTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank" },
	LateGame = { "combat_tank_h", "siege_tank", "missile_tank" }
}

ActivateAI = function()
	Defending[Harkonnen] = { }
	Defending[HarkonnenSmall] = { }
	Defending[HarkonnenSmall2] = { }
	Defending[Corrino] = { }

	AttackDelay[Harkonnen] = 26000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 12000 * DifficultyModifier[Difficulty]
	AttackDelay[HarkonnenSmall] = 24000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[HarkonnenSmall] = 8000 * DifficultyModifier[Difficulty]
	AttackDelay[HarkonnenSmall2] = 30000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[HarkonnenSmall2] = 9000 * DifficultyModifier[Difficulty]
	AttackDelay[Corrino] = 26000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Corrino] = 10000 * DifficultyModifier[Difficulty]

	HarvesterCount[Harkonnen] = 2
	HarvesterCount[HarkonnenSmall] = 1
	HarvesterCount[Corrino] = 1

	PatrolPoints[Harkonnen] = { HarkonnenPatrolPoint1.Location, HarkonnenPatrolPoint2.Location, UnitSpawn2.Location }
	PatrolPoints[HarkonnenSmall] = { HarkonnenPatrolPoint5.Location, HarkonnenPatrolPoint6.Location, HarkonnenReinforcementsPoint3.Location }
	PatrolPoints[HarkonnenSmall2] = { HarkonnenPatrolPoint3.Location, HarkonnenPatrolPoint2.Location, HarkonnenPatrolPoint1.Location, HarkonnenPatrolPoint4.Location }
	PatrolPoints[Corrino] = { HarkonnenReinforcementsPoint1.Location, HarkonnenReinforcementsPoint3.Location, HarkonnenReinforcementsPoint2.Location, HarkonnenReinforcementsPoint6.Location }

	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(12,10), CPos.New(38,41))
	DefencePerimeter[HarkonnenSmall] = GetCellsInRectangle(CPos.New(88, 65), CPos.New(100, 84))
	DefencePerimeter[HarkonnenSmall2] = GetCellsInRectangle(CPos.New(3, 66), CPos.New(15, 83))
	DefencePerimeter[Corrino] = Utils.Concat(GetCellsInRectangle(CPos.New(52,31), CPos.New(69, 52)), GetCellsInRectangle(CPos.New(56,5), CPos.New(73, 22)))

	LastHarvesterEaten[Harkonnen] = true
	LastHarvesterEaten[HarkonnenSmall] = true
	LastHarvesterEaten[HarkonnenSmall2] = true
	LastHarvesterEaten[Corrino] = true

	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino, EnemyInitialUnitSpawn[1], InitialUnitSpawnPaths[1])
	IdlingUnits[HarkonnenSmall2] = Reinforcements.Reinforce(HarkonnenSmall, EnemyInitialUnitSpawn[2], InitialUnitSpawnPaths[2])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[3], InitialUnitSpawnPaths[3])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[4], InitialUnitSpawnPaths[4])
	IdlingUnits[HarkonnenSmall] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[5], InitialUnitSpawnPaths[5])


	DefendAndRepairBase(Harkonnen, HarkonnenMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(HarkonnenSmall, HarkonnenSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(HarkonnenSmall2, HarkonnenSmall2Base, 0.75, AttackGroupSize[Difficulty])
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
	local infantryToBuild = function() return { Utils.Random(HarkonnenInfantryTypes) } end
	local infantryToBuildCorrino = function() return { Utils.Random(CorrinoInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local tanksToBuild = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(HarkonnenTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(HarkonnenTankTypes["LateGame"]) }
		end
	end
	local tanksToBuildCorrino = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(CorrinoTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(CorrinoTankTypes["LateGame"]) }
		end
	end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5
	local tanksToBuildHarkonnenSmall = function() return { Utils.Random(HarkonnenSmallTankTypes) } end

	ProduceUnits(Harkonnen, HBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HHeavyFactory1, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(HarkonnenSmall, HBarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(HarkonnenSmall, HLightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(HarkonnenSmall, HHeavyFactory2, delay, tanksToBuildHarkonnenSmall, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(HarkonnenSmall2, HBarracks4, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(HarkonnenSmall2, HLightFactory3, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(Corrino, CBarracks2, delay, infantryToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Corrino, CLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Corrino, CHeavyFactory, delay, tanksToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Harkonnen.GrantCondition("base-rebuilder")
		Corrino.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Harkonnen.GrantCondition("defense-rebuilder")
		Corrino.GrantCondition("defense-rebuilder2")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuild,
		heavy_factory = tanksToBuild
	}

	local productionTypesCorrino =
	{
		barracks = infantryToBuildCorrino,
		light_factory = vehilcesToBuild,
		heavy_factory = tanksToBuildCorrino
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenMainBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(Corrino, function(p, building)
		table.insert(CorrinoBase, building)
		DefendAndRepairBase(Corrino, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesCorrino[building.Type] == nil then return end
		ProduceUnits(Corrino, building, delay, productionTypesCorrino[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
