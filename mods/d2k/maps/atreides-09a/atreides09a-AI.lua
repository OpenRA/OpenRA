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
	easy = { DateTime.Seconds(1), DateTime.Seconds(3) },
	normal = { 20, 30 },
	hard = { 1, 10 }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenVehicleTypes = { "trike", "trike", "quad" }

HarkonnenTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank" },
	LateGame = { "combat_tank_h", "siege_tank", "missile_tank", "devastator" }
}

CorrinoInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

CorrinoTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank" },
	LateGame = { "combat_tank_h", "siege_tank", "missile_tank" }
}

ActivateAI = function()
	Defending[Harkonnen] = { }
	Defending[HarkonnenSmall] = { }
	Defending[Corrino] = { }

	AttackDelay[Harkonnen] = 22000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 12000 * DifficultyModifier[Difficulty]
	AttackDelay[HarkonnenSmall] = 20000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[HarkonnenSmall] = 7000 * DifficultyModifier[Difficulty]
	AttackDelay[Corrino] = 16000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Corrino] = 12000 * DifficultyModifier[Difficulty]

	HarvesterCount[Harkonnen] = 2
	HarvesterCount[HarkonnenSmall] = 1
	HarvesterCount[Corrino] = 1

	PatrolPoints[Harkonnen] = { HarkonnenReinforcementsPoint1.Location, EnemyReinforcementsPoint1.Location, EnemyReinforcementsPoint2.Location, HarkonnenReinforcementsPoint3.Location }
	PatrolPoints[HarkonnenSmall] = { HarkonnenReinforcementsPoint5.Location, HarkonnenReinforcementsPoint6.Location, EnemyReinforcementsPoint5.Location, EnemyReinforcementsPoint3.Location }
	PatrolPoints[Corrino] = { EnemyReinforcementsPoint4.Location, EnemyReinforcementsPoint6.Location, EnemyReinforcementsPoint5.Location, HarkonnenReinforcementsPoint5.Location, HarkonnenReinforcementsPoint6.Location, EnemyReinforcementsPoint2.Location, EnemyReinforcementsPoint3.Location }

	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(74,33), CPos.New(98,63))
	DefencePerimeter[HarkonnenSmall] = GetCellsInRectangle(CPos.New(2,31), CPos.New(15,53))
	DefencePerimeter[Corrino] = GetCellsInRectangle(CPos.New(70,4), CPos.New(93, 19))

	LastHarvesterEaten[Harkonnen] = true
	LastHarvesterEaten[HarkonnenSmall] = true
	LastHarvesterEaten[Corrino] = true

	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino, EnemyInitialUnitSpawn[1], InitialUnitSpawnPaths[1])
	IdlingUnits[HarkonnenSmall] = Reinforcements.Reinforce(HarkonnenSmall, EnemyInitialUnitSpawn[2], InitialUnitSpawnPaths[2])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[3], InitialUnitSpawnPaths[3])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, EnemyInitialUnitSpawn[4], InitialUnitSpawnPaths[4])


	DefendAndRepairBase(Harkonnen, HarkonnenMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(HarkonnenSmall, HarkonnenSmallBase, 0.75, AttackGroupSize[Difficulty])
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
	local attackThresholdSize = AttackGroupSize[Difficulty] * 3


	ProduceUnits(Harkonnen, HBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HHeavyFactory1, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

	ProduceUnits(HarkonnenSmall, HBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(HarkonnenSmall, HLightFactory2, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(HarkonnenSmall, HHeavyFactory2, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

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
