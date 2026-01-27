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
		easy = DateTime.Seconds(140),
		normal = DateTime.Seconds(80),
		hard = DateTime.Seconds(40)
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

EarlyProductionDelays =
{
	easy = { DateTime.Seconds(10), DateTime.Seconds(13) },
	normal = { DateTime.Seconds(6), DateTime.Seconds(8) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }

OrdosVehicleTypes = { "raider", "raider", "quad" }

OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }

OrdosStarportTypes = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_o.starport", "combat_tank_o.starport", "siege_tank.starport", "missile_tank.starport" }

ActivateAI = function()
	Defending[OrdosMain] = {}
	Defending[OrdosSmall] = {}
	AttackDelay[OrdosMain] = 14000 * DifficultyModifier[Difficulty]
	AttackDelay[OrdosSmall] = 14000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[OrdosMain] = 10000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[OrdosSmall] = 8000 * DifficultyModifier[Difficulty]
	HarvesterCount[OrdosMain] = 4
	HarvesterCount[OrdosSmall] = 2

	Trigger.OnAllKilledOrCaptured(ProductionBuildingsOrdosSmallBase, function()
			TimeBetweenAttacks[OrdosMain] = 0
	end)

	PatrolPoints[OrdosMain] = {OrdosRally5.Location, OrdosRally11.Location, OrdosRally12.Location }
	PatrolPoints[OrdosSmall] = {OrdosRally5.Location, OrdosRally7.Location, OrdosRally4.Location, OrdosRally5.Location }
	DefencePerimeter[OrdosMain] = Utils.Concat( GetCellsInRectangle(CPos.New(37,30), CPos.New(69,50)), GetCellsInRectangle(CPos.New(54,13), CPos.New(70,30)))
	DefencePerimeter[OrdosSmall] = GetCellsInRectangle(CPos.New(4,20), CPos.New(15,40))

	IdlingUnits[OrdosMain] = Utils.Concat(Reinforcements.Reinforce(OrdosMain, InitialOrdosReinforcements[1], InitialOrdosPaths[1]), Reinforcements.Reinforce(OrdosMain, InitialOrdosReinforcements[2], InitialOrdosPaths[2]))
	IdlingUnits[OrdosSmall] = Reinforcements.Reinforce(OrdosSmall, InitialOrdosReinforcements[1], InitialOrdosPaths[3])

	DefendAndRepairBase(OrdosMain, OrdosMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(OrdosSmall, OrdosSmallBase, 0.75, AttackGroupSize[Difficulty])

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

	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local tanksToBuild = function() return { Utils.Random(OrdosTankTypes) } end
	local unitsToBuy = function() return { Utils.Random(OrdosStarportTypes) } end
	local attackThresholdSize = {}
	attackThresholdSize[OrdosMain] = AttackGroupSize[Difficulty] * 3
	attackThresholdSize[OrdosSmall] = AttackGroupSize[Difficulty] * 2

	Trigger.AfterDelay(InitialProductionDelay["OrdosMain"][Difficulty], function()
		ProduceUnits(OrdosMain, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
		ProduceUnits(OrdosMain, OLightFactory1, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
		ProduceUnits(OrdosMain, OHeavyFactory1, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
		ProduceUnits(OrdosMain, OStarport, delay, unitsToBuy, AttackGroupSize[Difficulty], attackThresholdSize[OrdosMain])
	end)

	Trigger.AfterDelay(InitialProductionDelay["OrdosSmall"][Difficulty], function()
		ProduceUnits(OrdosSmall, OBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosSmall] )
		ProduceUnits(OrdosSmall, OLightFactory2, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize[OrdosSmall] )
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		OrdosSmall.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		OrdosSmall.GrantCondition("defense-rebuilder2")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(OrdosSmall, function(p, building)
		table.insert(OrdosSmallBase, building)
		DefendAndRepairBase(OrdosSmall, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(OrdosSmall, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize[OrdosSmall])
	end)
end
