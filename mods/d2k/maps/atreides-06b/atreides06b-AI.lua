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
	OrdosMain =
	{
		easy = DateTime.Seconds(90),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(30)
	},
	Smugglers =
	{
		easy = DateTime.Seconds(150),
		normal = DateTime.Seconds(100),
		hard = DateTime.Seconds(50)
	},
	Mercenaries =
	{
		easy = DateTime.Seconds(120),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(60)
	}
}
AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyAttackDelays =
{
	easy = { DateTime.Seconds(6), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(4), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}
LateAttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }
OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }


ActivateAI = function()
	Defending[Ordos] = { }
	Defending[Smugglers] = { }
	Defending[Mercenaries] = { }
	AttackDelay[Ordos] = 12000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Ordos] = 6000 * DifficultyModifier[Difficulty]
	AttackDelay[Smugglers] = 16000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Smugglers] = 8000 * DifficultyModifier[Difficulty]
	AttackDelay[Mercenaries] = 16000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Mercenaries] = 8000 * DifficultyModifier[Difficulty]
	HarvesterCount[Ordos] = 2
	PatrolPoints[Ordos] = { OPatrolPoint1.Location, OPatrolPoint2.Location,OPatrolPoint3.Location }
	PatrolPoints[Mercenaries] = { MercenariesUnitSpawn.Location, OPatrolPoint3.Location }
	DefencePerimeter[Ordos] = GetCellsInRectangle(CPos.New(25,60), CPos.New(67,80))
	DefencePerimeter[Smugglers] = GetCellsInRectangle(CPos.New(63,61), CPos.New(81,77))
	DefencePerimeter[Mercenaries] = GetCellsInRectangle(CPos.New(6,23), CPos.New(18,36))
	LastHarvesterEaten[Ordos] = true
	LastHarvesterEaten[Mercenaries] = true


	IdlingUnits[Ordos] = Reinforcements.Reinforce(Ordos, InitialReinforcementsSquads["ordos"], InitialPaths["ordos"])
	IdlingUnits[Smugglers] = Reinforcements.Reinforce(Smugglers, InitialReinforcementsSquads["smugglers"], InitialPaths["smugglers"])
	IdlingUnits[OrdosSmall] = Reinforcements.Reinforce(OrdosSmall, InitialReinforcementsSquads["ordosSmall"], InitialPaths["ordosSmall"])
	IdlingUnits[Mercenaries] = Reinforcements.Reinforce(Mercenaries, InitialReinforcementsSquads["mercenaries"], InitialPaths["mercenaries"])

	DefendAndRepairBase(Ordos, OrdosMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Smugglers, SmugglersBase, 0.75, AttackGroupSize[Difficulty])
	RepairBuilding(OrdosSmall, OStarport, 0.75)
	DefendAndRepairBase(Mercenaries, MercenariesBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function(player)
		if EarlyGameStage >= DateTime.GameTime then
			return Utils.RandomInteger(EarlyAttackDelays[Difficulty][1], EarlyAttackDelays[Difficulty][2] + 1)
		else
			return Utils.RandomInteger(LateAttackDelays[Difficulty][1], LateAttackDelays[Difficulty][2] + 1)
		end
	end
	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local tanksToBuild = function() return { Utils.Random(OrdosTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5
	Trigger.AfterDelay(InitialProductionDelay["OrdosMain"][Difficulty], function()
		ProduceUnits(Ordos, OBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
	Trigger.AfterDelay(InitialProductionDelay["Smugglers"][Difficulty], function()
		ProduceUnits(Smugglers, SBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Smugglers, SLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
	Trigger.AfterDelay(InitialProductionDelay["Mercenaries"][Difficulty], function()
		ProduceUnits(Mercenaries, MBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Mercenaries, MLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Ordos.GrantCondition("base-rebuilder")
		Mercenaries.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Ordos.GrantCondition("defense-rebuilder")
		Mercenaries.GrantCondition("defense-rebuilder2")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuild,
		heavy_factory = tanksToBuild
	}
	Trigger.OnBuildingPlaced(Ordos, function(p, building)
		table.insert(OrdosMainBase, building)
		DefendAndRepairBase(Ordos, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Ordos, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
	Trigger.OnBuildingPlaced(Mercenaries, function(p, building)
		table.insert(MercenariesBase, building)
		DefendAndRepairBase(Mercenaries, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Mercenaries, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

end
