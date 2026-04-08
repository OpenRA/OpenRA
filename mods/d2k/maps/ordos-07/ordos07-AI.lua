--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(7)

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

CorrinoInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesTankTypes = { "combat_tank_a", "siege_tank" }

CorrinoTankTypes = { "combat_tank_h", "siege_tank" }

ActivateAI = function()
	Defending[Atreides] = { }
	Defending[AtreidesSmall] = { }
	Defending[AtreidesSmall2] = { }
	Defending[Corrino] = { }
	Defending[Mercenaries] = { }
	AttackDelay[Atreides] = 10000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Atreides] = 10500 * DifficultyModifier[Difficulty]
	AttackDelay[AtreidesSmall] = 7000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall] = 5500 * DifficultyModifier[Difficulty]
	AttackDelay[AtreidesSmall2] = 9000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesSmall2] = 5500 * DifficultyModifier[Difficulty]
	AttackDelay[Corrino] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Corrino] = 9000 * DifficultyModifier[Difficulty]
	AttackDelay[Mercenaries] = 14500 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Mercenaries] = 8000 * DifficultyModifier[Difficulty]
	HarvesterCount[Atreides] = 2
	HarvesterCount[Corrino] = 0
	LastHarvesterEaten[Atreides] = true
	LastHarvesterEaten[AtreidesSmall] = true
	LastHarvesterEaten[AtreidesSmall2] = true
	LastHarvesterEaten[Corrino] = true

	PatrolPoints[Atreides] = { APatrolPoint1.Location, AReinforcementsPoint2.Location, APatrolPoint2.Location, AtreidesSmallUnitSpawn.Location }
	PatrolPoints[AtreidesSmall] = { APatrolPoint2.Location, AReinforcementsPoint2.Location, APatrolPoint3.Location}
	PatrolPoints[Atreides] = { APatrolPoint3.Location, AReinforcementsPoint3.Location }


	DefencePerimeter[Atreides] = Utils.Concat(GetCellsInRectangle(CPos.New(65,17), CPos.New(76,29)), GetCellsInRectangle(CPos.New(59,11), CPos.New(77,15)))
	DefencePerimeter[AtreidesSmall] = GetCellsInRectangle(CPos.New(56,39), CPos.New(68,50))
	DefencePerimeter[AtreidesSmall2] = GetCellsInRectangle(CPos.New(28,61), CPos.New(41,73))
	DefencePerimeter[Mercenaries] = GetCellsInRectangle(CPos.New(4,2), CPos.New(18,12))
	DefencePerimeter[Corrino] = GetCellsInRectangle(CPos.New(61,3), CPos.New(80,10))

	IdlingUnits[Atreides] = Reinforcements.Reinforce(Atreides, InitialReinforcementsSquads[1], InitialSpawnPaths[1])
	IdlingUnits[AtreidesSmall] = Reinforcements.Reinforce(AtreidesSmall,InitialReinforcementsSquads[3], InitialSpawnPaths[3])
	IdlingUnits[AtreidesSmall2] = Reinforcements.Reinforce(AtreidesSmall2,InitialReinforcementsSquads[4], InitialSpawnPaths[4])
	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino,InitialReinforcementsSquads[2], InitialSpawnPaths[2])
	IdlingUnits[Mercenaries] = { }
	local units = Reinforcements.ReinforceWithTransport(Mercenaries,"carryall.reinforce",MercenaryReinforcements ,MercenaryReinforcementsPath, {MercenaryReinforcementsPath[2], MercenaryReinforcementsPath[1]})
	IdlingUnits[Mercenaries] = units[2]

	DefendAndRepairBase(Atreides, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall, AtreidesSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesSmall2, AtreidesSmall2Base, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Corrino, CorrinoBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Mercenaries, MercenaryBase, 0.75, AttackGroupSize[Difficulty])

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
	local corrinoinfantryToBuild = function() return { Utils.Random(CorrinoInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreides = function() return { Utils.Random(AtreidesTankTypes) } end
	local tanksToBuildCorrino = function() return { Utils.Random(CorrinoTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5


	ProduceUnits(Atreides, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Atreides, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	Trigger.AfterDelay(150, function()
		ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ProduceUnits(AtreidesSmall, ABarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(AtreidesSmall2, ABarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)


	ProduceUnits(Corrino, CBarracks, delay, corrinoinfantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Corrino, CLightFactory, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Corrino, CHeavyFactory, delay, tanksToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)


	ActivateCrushLogic()

	if Difficulty == "normal" then
		Atreides.GrantCondition("base-rebuilder")
		Corrino.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Atreides.GrantCondition("defense-rebuilder")
		Corrino.GrantCondition("defense-rebuilder2")
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
		heavy_factory = tanksToBuildCorrino
	}

	Trigger.OnBuildingPlaced(Atreides, function(p, building)
		table.insert(AtreidesBase, building)
		DefendAndRepairBase(Atreides, {building}, 0.75, AttackGroupSize[Difficulty] )
		if building.Type == "high_tech_factory" then
			AirstrikeLogic(building)
		end
		if productionTypesAtreides[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypesAtreides[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(Corrino, function(p, building)
		table.insert(CorrinoBase, building)
		DefendAndRepairBase(Corrino, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesHarkonnen[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypesHarkonnen[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Utils.Do(MercenaryBase, function(building)
		Trigger.OnDamaged(building, function(self, attacker, damage)
			if not attacker.Owner.IsAlliedWith(Mercenaries) and #IdlingUnits[Mercenaries] > 1 and damage > 1 then
				CheckArea(Mercenaries, self.Location)
			end
		end)
	end)

	Trigger.OnBuildingPlaced(Ordos, function(p, building)
		if building.Type == "concretea" or building.Type == "concreteb" or building.Type == "wall" then return end
		Trigger.OnDamaged(building, function(self, attacker, damage)
			if not attacker.Owner.IsAlliedWith(Mercenaries) and #IdlingUnits[Mercenaries] > 1 and damage > 1 then
				CheckArea(Mercenaries, self.Location)
			end
		end)
	end)
end
