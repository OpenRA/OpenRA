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
	HarkonnenMain =
	{
		easy = DateTime.Seconds(100),
		normal = DateTime.Seconds(60),
		hard = DateTime.Seconds(30)
	}
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
	normal = { DateTime.Seconds(5), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }

HarkonnenVehicleTypes = { "trike", "trike", "trike", "quad", "quad" }

HarkonnenTankType = { "combat_tank_h" }

InitAIUnits = function()
	IdlingUnits[Harkonnen] = Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", InitialHarkonnenReinforcements, HarkonnenPaths[1], { HarkonnenPaths[1][1] })[2]

	DefendAndRepairBase(Harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
	DefendActor(HarkonnenBarracks, Harkonnen, AttackGroupSize[Difficulty])
	RepairBuilding(Harkonnen, HarkonnenBarracks, 0.75)

	Utils.Do(SmugglerBase, function(actor)
		RepairBuilding(Smuggler, actor, 0.75)
	end)
	RepairBuilding(Smuggler, Starport, 0.75)
end

-- Not using ProduceUnits because of the custom StopInfantryProduction condition
ProduceInfantry = function()
	if StopInfantryProduction or HarkonnenBarracks.IsDead or HarkonnenBarracks.Owner ~= Harkonnen then
		return
	end

	if HoldProduction[Harkonnen] then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceInfantry)
		return
	end

	local delay = 0
	if EarlyGameStage >= DateTime.GameTime then
		delay =  Utils.RandomInteger(EarlyProductionDelays[Difficulty][1], EarlyProductionDelays[Difficulty][2] + 1)
	else
		delay =  Utils.RandomInteger(LateProductionDelays[Difficulty][1], LateProductionDelays[Difficulty][2] + 1)
	end

	local toBuild = { Utils.Random(HarkonnenInfantryTypes) }
	Harkonnen.Build(toBuild, function(unit)
		IdlingUnits[Harkonnen][#IdlingUnits[Harkonnen] + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		if #IdlingUnits[Harkonnen] >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack(Harkonnen, AttackGroupSize[Difficulty])
		end
	end)
end

ActivateAI = function()
	Harkonnen.Cash = 15000
	Defending[Harkonnen] = {}
	AttackDelay[Harkonnen] = 6000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 5000 * DifficultyModifier[Difficulty]
	HarvesterCount[Harkonnen] = 1
	PatrolPoints[Harkonnen] = { HPatrolPoint1.Location, HPatrolPoint2.Location,HPatrolPoint3.Location }
	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(13,13), CPos.New(36,25))
	LastHarvesterEaten[Harkonnen] = true
	InitAIUnits()

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

	local vehiclesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local tanksToBuild = function() return HarkonnenTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["HarkonnenMain"][Difficulty], function()
		ProduceInfantry()
		ProduceUnits(Harkonnen, HarkonnenLightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HarkonnenHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
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
		light_factory = vehiclesToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
