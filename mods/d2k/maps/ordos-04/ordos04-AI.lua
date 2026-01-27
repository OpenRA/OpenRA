--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

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

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

HarkonnenVehicleTypes = { "trike", "trike", "quad" }

HarkonnenTankType = { "combat_tank_h" }

SmugglerVehicleTypes = { "raider", "raider", "quad" }

SmugglerTankType = { "combat_tank_o" }

InitAIUnits = function(house)
	LastHarvesterEaten[house] = true
	IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.InternalName], InitialReinforcementsPaths[house.InternalName])
	DefendAndRepairBase(house, Base[house.InternalName], 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	Defending[Harkonnen] = { }
	Defending[Smuggler] = { }
	AttackDelay[Harkonnen] = 7000
	AttackDelay[Smuggler] = 7000
	TimeBetweenAttacks[Harkonnen] = 8000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Smuggler] = 15000 * DifficultyModifier[Difficulty]
	HarvesterCount[Harkonnen] = 2
	HarvesterCount[Smuggler] = 0
	PatrolPoints[Harkonnen] = { HPatrolPoint1.Location, HPatrolPoint2.Location, HarkonnenRally2.Location }
	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(23,3), CPos.New(41,22))
	DefencePerimeter[Smuggler] = Utils.Concat(GetCellsInRectangle(CPos.New(57,15), CPos.New(65, 24)), GetCellsInRectangle(CPos.New(43,4), CPos.New(54, 16)))
	InitAIUnits(Harkonnen)
	InitAIUnits(Smuggler)

	local delay = function(player)
		if EmergencyBuildRate[player] and Difficulty ~= "easy" then
			return 1
		else
			return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1)
		end
	end

	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local hVehiclesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local hTanksToBuild = function() return HarkonnenTankType end
	local sVehiclesToBuild = function() return { Utils.Random(SmugglerVehicleTypes) } end
	local sTanksToBuild = function() return SmugglerTankType end
	local attackTresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Harkonnen, HLightFactory, delay, hVehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Harkonnen, HHeavyFactory, delay, hTanksToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	ProduceUnits(Smuggler, SBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Smuggler, SLightFactory, delay, sVehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(Smuggler, SHeavyFactory, delay, sTanksToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	ActivateCrushLogic()

	AIProductionActivated = true
	if Difficulty == "normal" then
		Harkonnen.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		Harkonnen.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = hVehiclesToBuild,
		heavy_factory = hTanksToBuild
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(Base[Harkonnen], building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
