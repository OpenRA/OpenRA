--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenTankType = { "combat_tank_h" }

-- Overwrite the template function because of the message
SendAttack = function(owner, size)
	if Attacking[owner] then
		return
	end
	Attacking[owner] = true
	HoldProduction[owner] = true

	local units = SetupAttackGroup(owner, size)
	Utils.Do(units, IdleHunt)

	if #units > 0 then
		Media.DisplayMessage("Harkonnen units approaching!", "Fremen Leader")
	end

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking[owner] = false
		HoldProduction[owner] = false
	end)
end

InitAIUnits = function()
	IdlingUnits[harkonnen] = Reinforcements.Reinforce(harkonnen, InitialHarkonnenReinforcements, HarkonnenPaths[1])

	DefendAndRepairBase(harkonnen, HarkonnenBase, 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	LastHarvesterEaten[harkonnen] = true
	InitAIUnits()
	FremenProduction()

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(HarkonnenInfantryTypes) } end
	local tanksToBuild = function() return HarkonnenTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(harkonnen, HarkonnenBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(harkonnen, HarkonnenHeavyFact, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
end
