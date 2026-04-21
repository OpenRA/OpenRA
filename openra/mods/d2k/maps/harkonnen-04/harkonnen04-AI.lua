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
	easy = DateTime.Seconds(150),
	normal = DateTime.Seconds(100),
	hard = DateTime.Seconds(50)
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

AtreidesInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }

AtreidesVehicleTypes = { "trike", "trike", "quad" }

AtreidesTankType = { "combat_tank_a" }

local attackThresholdSize = AttackGroupSize[Difficulty] * 2

ActivateAI = function()
	Defending[Atreides] = { }
	Defending[Fremen] = { }
	AttackDelay[Atreides] = 5000 * DifficultyModifier[Difficulty]
	AttackDelay[Fremen] = 5000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Atreides] = 6000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Fremen] = 5000 * DifficultyModifier[Difficulty]
	HarvesterCount[Atreides] = 2
	HarvesterCount[Fremen] = 2
	IdlingUnits[Fremen] = { }
	IdlingUnits[Atreides] = Utils.Concat(Reinforcements.Reinforce(Atreides, InitialAtreidesReinforcements[1], AtreidesPaths[2]), Reinforcements.Reinforce(Atreides, InitialAtreidesReinforcements[2], AtreidesPaths[3]))
	FremenProduction()

	DefencePerimeter[Atreides] = GetCellsInRectangle(CPos.New(4,68), CPos.New(44,82))
	Utils.Do(IdlingUnits[Atreides], function(a) SelectRoutine(Atreides, a) end)
	PatrolPoints[Atreides] = { APatrolPoint1.Location, APatrolPoint2.Location, APatrolPoint3.Location, APatrolPoint4.Location }

	DefendAndRepairBase(Atreides, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Fremen, FremenBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function(player)
		if EmergencyBuildRate[player] and Difficulty ~= "easy" then
			return 1
		end
		return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1)
	end

	local infantryToBuild = function() return { Utils.Random(AtreidesInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local tanksToBuild = function() return AtreidesTankType end

	ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	Trigger.AfterDelay(InitialProductionDelay[Difficulty], function()
		ProduceUnits(Atreides, ALightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Atreides.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		Atreides.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(Atreides, function(p, building)
		table.insert(AtreidesBase, building)
		DefendAndRepairBase(Atreides, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Atreides, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
