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
	easy = { DateTime.Seconds(7), DateTime.Seconds(10) },
	normal = { DateTime.Seconds(4), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}
LateProductionDelays =
{
	easy = { 50, 100 },
	normal = { 20, 40 },
	hard = { 1, 10 }
}

InfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }
OrdosTankTypes =
{
	EarlyGame = { "combat_tank_o", "combat_tank_o", "siege_tank"},
	LateGame = { "combat_tank_o", "combat_tank_o", "siege_tank", "deviator" }
}
HarkonnenVehicleTypes = { "trike", "trike", "quad" }
HarkonnenTankTypes = { "combat_tank_h", "combat_tank_h", "siege_tank" }


ActivateAI = function()
	Defending[Ordos] = { }
	Defending[OrdosSmall] = { }
	Defending[Harkonnen] = { }
	AttackDelay[Ordos] = 10000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Ordos] = 10000 * DifficultyModifier[Difficulty]
	AttackDelay[OrdosSmall] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[OrdosSmall] = 6000 * DifficultyModifier[Difficulty]
	AttackDelay[Harkonnen] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 7500 * DifficultyModifier[Difficulty]
	HarvesterCount[Ordos] = 2
	HarvesterCount[Harkonnen] = 1

	PatrolPoints[Ordos] = { OPatrolPoint1.Location, OPatrolPoint2.Location,OPatrolPoint3.Location }
	PatrolPoints[OrdosSmall] = { OPatrolPoint4.Location, OPatrolPoint5.Location,OPatrolPoint6.Location }
	PatrolPoints[Harkonnen] = { HPatrolPoint1.Location, HPatrolPoint2.Location,HPatrolPoint3.Location }

	DefencePerimeter[Ordos] = Utils.Concat(GetCellsInRectangle(CPos.New(2,71), CPos.New(17,96)), GetCellsInRectangle(CPos.New(18,65), CPos.New(32,92)))
	DefencePerimeter[OrdosSmall] = GetCellsInRectangle(CPos.New(26, 22), CPos.New(46, 31))
	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(74, 63), CPos.New(95, 79))

	LastHarvesterEaten[Ordos] = true
	LastHarvesterEaten[Harkonnen] = true

	IdlingUnits[Ordos] = Reinforcements.Reinforce(Ordos, InitialUnitSpawn[1], InitialUnitSpawnPaths[1])
	IdlingUnits[Ordos] = Reinforcements.Reinforce(Ordos, InitialUnitSpawn[2], InitialUnitSpawnPaths[2])
	IdlingUnits[Smugglers] = Reinforcements.Reinforce(Smugglers, InitialUnitSpawn[3], InitialUnitSpawnPaths[3])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, InitialUnitSpawn[4], InitialUnitSpawnPaths[4])
	IdlingUnits[OrdosSmall] = Reinforcements.Reinforce(OrdosSmall, InitialUnitSpawn[5], InitialUnitSpawnPaths[5])

	DefendAndRepairBase(Ordos, OrdosBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(OrdosSmall, OrdosSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
	Utils.Do(SmugglersBase, function(building)
		RepairBuilding(Smugglers, building, 0.75)
	end)

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
	local infantryToBuild = function() return { Utils.Random(InfantryTypes) } end
	local vehilcesToBuildOrdos = function() return { Utils.Random(OrdosVehicleTypes) } end
	local vehilcesToBuildHarkonnen = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local tanksToBuildHarkonnen = function() return { Utils.Random(HarkonnenTankTypes) } end
	local tanksToBuildOrdos = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(OrdosTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(OrdosTankTypes["LateGame"]) }
		end
	 end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5


		ProduceUnits(Ordos, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OLightFactory1, delay, vehilcesToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OHeavyFactory, delay, tanksToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)

		ProduceUnits(OrdosSmall, OBarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(OrdosSmall, OLightFactory2, delay, vehilcesToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)

		ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HLightFactory, delay, vehilcesToBuildHarkonnen, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HHeavyFactory, delay, tanksToBuildHarkonnen, AttackGroupSize[Difficulty], attackThresholdSize)


	ActivateCrushLogic()

	if Difficulty == "normal" then
		Ordos.GrantCondition("base-rebuilder")
		Harkonnen.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Ordos.GrantCondition("defense-rebuilder")
		Harkonnen.GrantCondition("defense-rebuilder2")
	end

	local productionTypesOrdos =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuildOrdos,
		heavy_factory = tanksToBuildOrdos
	}

	local productionTypesHarkonnen =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuildHarkonnen,
		heavy_factory = tanksToBuildHarkonnen
	}

	Trigger.OnBuildingPlaced(Ordos, function(p, building)
		table.insert(OrdosBase, building)
		DefendAndRepairBase(Ordos, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesOrdos[building.Type] == nil then return end
		ProduceUnits(Ordos, building, delay, productionTypesOrdos[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesHarkonnen[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypesHarkonnen[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

end
