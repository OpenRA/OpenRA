--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(6)

InitialProductionDelay = {
	OrdosMain =
	{
		easy = DateTime.Seconds(150),
		normal = DateTime.Seconds(100),
		hard = DateTime.Seconds(50)
	},

	OrdosSmall =
	{
		easy = DateTime.Seconds(120),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(30)
	},
}

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

ProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

OrdosVehicleTypes = { "raider", "raider", "quad" }

OrdosTankType = { "combat_tank_o" }

ActivateAI = function()
	Defending[OrdosMain] = {}
	Defending[OrdosSmall] = {}
	AttackDelay[OrdosMain] = 8000 * DifficultyModifier[Difficulty]
	AttackDelay[OrdosSmall] = 10000 * DifficultyModifier[Difficulty]
	AttackDelay[Corrino] = 1000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[OrdosMain] = 8000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[OrdosSmall] = 7000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Corrino] = 2000 * DifficultyModifier[Difficulty]
	HarvesterCount[OrdosMain] = 2
	HarvesterCount[OrdosSmall] = 2

	Trigger.OnAllKilledOrCaptured(ProductionBuildingsOrdosSmallBase, function()
			TimeBetweenAttacks[OrdosMain] = 0
	end)

	PatrolPoints[OrdosMain] = {OrdosPatrol1.Location, OrdosPatrol2.Location }
	PatrolPoints[OrdosSmall] = {OrdosRally1.Location, OrdosRally2.Location, OrdosRally4.Location }
	DefencePerimeter[OrdosMain] = GetCellsInRectangle(CPos.New(51,11), CPos.New(67,38))
	DefencePerimeter[OrdosSmall] = GetCellsInRectangle(CPos.New(6,69), CPos.New(22,80))

	IdlingUnits[OrdosMain] = Utils.Concat(Reinforcements.Reinforce(OrdosMain, InitialOrdosReinforcements[1], InitialOrdosPaths[1]), Reinforcements.Reinforce(OrdosMain, InitialOrdosReinforcements[2], InitialOrdosPaths[2]))
	IdlingUnits[OrdosSmall] = Reinforcements.Reinforce(OrdosSmall, InitialOrdosReinforcements[1], InitialOrdosPaths[3])
	IdlingUnits[Corrino] = { CSardaukar1, CSardaukar2, CSardaukar3, CSardaukar4, CSardaukar5 }

	DefendAndRepairBase(OrdosMain, OrdosMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(OrdosSmall, OrdosSmallBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Corrino, CorrinoBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function(player)
		if EmergencyBuildRate[player] and Difficulty ~= "easy" then
			return 1
		end
		return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1)
	end

	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local tanksToBuild = function() return OrdosTankType end
	local attackThresholdSize = {}
	attackThresholdSize[OrdosMain] = AttackGroupSize[Difficulty] * 3
	attackThresholdSize[OrdosSmall] = AttackGroupSize[Difficulty] * 2

	Trigger.AfterDelay(InitialProductionDelay["OrdosMain"][Difficulty], function()
		ProduceUnits(OrdosMain, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
		ProduceUnits(OrdosMain, OLightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
		ProduceUnits(OrdosMain, OHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
	end)

	Trigger.AfterDelay(InitialProductionDelay["OrdosSmall"][Difficulty], function()
		ProduceUnits(OrdosSmall, OBarracks3, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosSmall])
		ProduceUnits(OrdosSmall, OLightFactory2, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosSmall])
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		OrdosMain.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		OrdosMain.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(OrdosMain, function(p, building)
		table.insert(OrdosMainBase, building)
		DefendAndRepairBase(OrdosMain, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(OrdosMain, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
	end)
end
