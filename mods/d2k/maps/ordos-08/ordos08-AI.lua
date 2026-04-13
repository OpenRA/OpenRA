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
	easy = { DateTime.Seconds(15), DateTime.Seconds(25) },
	normal = { DateTime.Seconds(12), DateTime.Seconds(18) },
	hard = { DateTime.Seconds(7), DateTime.Seconds(10) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(5), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(3), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(2), DateTime.Seconds(3) }
}

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

CorrinoInfantryTypes = { "light_inf", "light_inf", "trooper", "sardaukar" }

EnemyVehicleTypes = { "trike", "trike", "quad" }

AtreidesTankTypes =
{
	EarlyGame = { "combat_tank_a","combat_tank_a", "siege_tank" },
	LateGame = { "combat_tank_a", "missile_tank", "siege_tank", "sonic_tank" }
}

CorrinoTankTypes = { "combat_tank_h", "siege_tank" }

HarkonnenTankTypes =
{
	EarlyGame = { "combat_tank_h", "combat_tank_h", "siege_tank" },
	LateGame = { "combat_tank_h", "missile_tank", "siege_tank", "devastator" }

}
MercenaryTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }

ActivateAI = function()
	Defending[Atreides] = { }
	Defending[Harkonnen] = { }
	Defending[Corrino] = { }
	Defending[Mercenaries] = { }
	AttackDelay[Atreides] = 13000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Atreides] = 6000 * DifficultyModifier[Difficulty]
	AttackDelay[Harkonnen] = 11000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 10000 * DifficultyModifier[Difficulty]
	AttackDelay[Corrino] = 6000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Corrino] = 7000 * DifficultyModifier[Difficulty]
	AttackDelay[Mercenaries] = 10000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Mercenaries] = 7000 * DifficultyModifier[Difficulty]
	HarvesterCount[Atreides] = 2
	HarvesterCount[Harkonnen] = 2
	HarvesterCount[Corrino] = 0
	HarvesterCount[Mercenaries] = 0
	LastHarvesterEaten[Atreides] = true
	LastHarvesterEaten[Harkonnen] = true

	PatrolPoints[Atreides] = { APatrolPoint1.Location, AReinforcementsPoint10.Location, APatrolPoint2.Location}
	PatrolPoints[Harkonnen] = { HPatrolPoint1.Location, HPatrolPoint2.Location, HPatrolPoint3.Location}


	DefencePerimeter[Atreides] = Utils.Concat(GetCellsInRectangle(CPos.New(51,2), CPos.New(58,7)), GetCellsInRectangle(CPos.New(58,2), CPos.New(77,20)))
	DefencePerimeter[Harkonnen] = Utils.Concat(GetCellsInRectangle(CPos.New(62,76), CPos.New(80,100)), GetCellsInRectangle(CPos.New(74,65), CPos.New(81,72)))

	DefencePerimeter[Mercenaries] = Utils.Concat(GetCellsInRectangle(CPos.New(2,2), CPos.New(18,10)), GetCellsInRectangle(CPos.New(2,11), CPos.New(9,17)))
	DefencePerimeter[Corrino] = GetCellsInRectangle(CPos.New(60,53), CPos.New(69,63))

	IdlingUnits[Atreides] = Reinforcements.Reinforce(Atreides, InitialReinforcementsSquads[1], InitialSpawnPaths[1])
	IdlingUnits[Corrino] = Reinforcements.Reinforce(Corrino,InitialReinforcementsSquads[2], InitialSpawnPaths[2])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen,InitialReinforcementsSquads[3], InitialSpawnPaths[3])
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen,InitialReinforcementsSquads[4], InitialSpawnPaths[4])
	IdlingUnits[Mercenaries] = Reinforcements.Reinforce(Mercenaries,InitialReinforcementsSquads[5], InitialSpawnPaths[5])


	DefendAndRepairBase(Atreides, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
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
	local vehiclesToBuild = function() return { Utils.Random(EnemyVehicleTypes) } end
	local tanksToBuildAtreides = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(AtreidesTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(AtreidesTankTypes["LateGame"]) }
		end
	end
	local tanksToBuildHarkonnen = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(HarkonnenTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(HarkonnenTankTypes["LateGame"]) }
		end
	end
	local tanksToBuildCorrino = function() return { Utils.Random(CorrinoTankTypes) } end
	local tanksToBuildMercenary = function() return { Utils.Random(MercenaryTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5


	ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	Trigger.AfterDelay(150, function()
		ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HLightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	Trigger.AfterDelay(150, function()
		ProduceUnits(Harkonnen, HHeavyFactory, delay, tanksToBuildHarkonnen, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ProduceUnits(Corrino, CHeavyFactory, delay, tanksToBuildCorrino, AttackGroupSize[Difficulty], attackThresholdSize)

	local mercenaryProductionDelay = function() return Utils.RandomInteger(300,700) end
	ProduceUnits(Mercenaries, MHeavyFactory, mercenaryProductionDelay, tanksToBuildMercenary, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Mercenaries, MLightFactory, mercenaryProductionDelay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)


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
		table.insert(AtreidesBase, building)
		DefendAndRepairBase(Atreides, {building}, 0.75, AttackGroupSize[Difficulty] )
		if building.Type == "high_tech_factory" then
			AirstrikeLogic(building)
		end
		if productionTypesAtreides[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypesAtreides[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if building.Type == "palace" then
			DeathHandLogic(building)
		end
		if productionTypesHarkonnen[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypesHarkonnen[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
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

	Trigger.OnEnteredFootprint(
	Utils.Concat(GetCellsInRectangle(CPos.New(9,19), CPos.New(15,29)),GetCellsInRectangle(CPos.New(5,41), CPos.New(17,42))),
	function(actor, id)
		if actor.Owner.IsAlliedWith(Mercenaries) or actor.IsDead or not actor.HasProperty("Move") then return end
		CheckArea(Mercenaries, actor.Location)
	end)
end
