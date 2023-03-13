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

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

AtreidesInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
AtreidesVehicleTypes = { "trike", "trike", "quad" }

InitAIUnits = function()
	IdlingUnits[Atreides] = Reinforcements.Reinforce(Atreides, AtreidesInitialReinforcements, AtreidesInitialPath)

	DefendAndRepairBase(Atreides, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	LastHarvesterEaten[Atreides] = true
	Trigger.AfterDelay(0, InitAIUnits)

	AConyard.Produce(HarkonnenUpgrades[1])
	AConyard.Produce(HarkonnenUpgrades[2])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(AtreidesInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	-- Finish the upgrades first before trying to build something
	Trigger.AfterDelay(DateTime.Seconds(14), function()
		ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Atreides, ALightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
