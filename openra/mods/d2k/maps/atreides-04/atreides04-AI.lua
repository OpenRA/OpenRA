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

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenTankType = { "combat_tank_h" }

-- Overwrite the template function because of the message
SendAttack = function(owner, size)
	if Attacking[owner] then return end

	Attacking[owner] = true
	HoldProduction[owner] = true

	local units = SetupAttackGroup(owner, size)
	Utils.Do(units, Trigger.ClearAll)

	if #units > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-units-approaching"), UserInterface.GetFluentMessage("fremen-leader"))
	end

	Trigger.AfterDelay(1, function()
		Utils.Do(units, function(u)
			u.Stop()
			IdleHunt(u)
		end)

		Trigger.OnAllRemovedFromWorld(units, function()
			Attacking[owner] = false
			HoldProduction[owner] = false
		end)
	end)
end

InitAIUnits = function()
	IdlingUnits[Harkonnen] = Reinforcements.Reinforce(Harkonnen, InitialHarkonnenReinforcements, HarkonnenPaths[1])

	DefendAndRepairBase(Harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	Defending[Harkonnen] = {}
	HarvesterCount[Harkonnen] = 2
	AttackDelay[Harkonnen] = 9000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Harkonnen] = 9000 * DifficultyModifier[Difficulty]
	LastHarvesterEaten[Harkonnen] = true
	InitAIUnits()
	FremenProduction()

	local delay = function() return Utils.RandomInteger(ProductionDelays[Difficulty][1], ProductionDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(HarkonnenInfantryTypes) } end
	local tanksToBuild = function() return HarkonnenTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(Harkonnen, HarkonnenBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HarkonnenHeavyFact, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)

	ActivateCrushLogic()

	local productionTypes =
	{
		barracks = infantryToBuild,
		heavy_factory = tanksToBuild
	}

	Trigger.OnBuildingPlaced(Harkonnen, function(p, building)
		table.insert(HarkonnenBase, building)
		DefendAndRepairBase(Harkonnen, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Harkonnen, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
