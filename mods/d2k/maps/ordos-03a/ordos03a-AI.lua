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
	easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

HarkonnenVehicleTypes = { "trike", "trike", "quad" }

InitAIUnits = function()
	Defending[Harkonnen] = { }
	AttackDelay[Harkonnen] = 6000  * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 5000 * DifficultyModifier[Difficulty]
	LastHarvesterEaten[Harkonnen] = true
	PatrolPoints[Harkonnen] = { HarkonnenRally3.Location, HPatrolPoint1.Location, Actor6.Location }
	DefencePerimeter[Harkonnen] = GetCellsInRectangle(CPos.New(3,42), CPos.New(13,63))
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, HarkonnenInitialReinforcements, HarkonnenInitialPath)

	DefendAndRepairBase(Harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	LastHarvesterEaten[Harkonnen] = true
	Trigger.AfterDelay(0, InitAIUnits)

	HConyard.Produce(OrdosUpgrades[1])
	HConyard.Produce(OrdosUpgrades[2])

	local delay = function() return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(HarkonnenInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	-- Finish the upgrades first before trying to build something
	Trigger.AfterDelay(DateTime.Seconds(14), function()
		ProduceUnits(Harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Harkonnen, HLightFactory, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	if Difficulty ~= "easy" then
		Harkonnen.GrantCondition("base-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuild,
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
