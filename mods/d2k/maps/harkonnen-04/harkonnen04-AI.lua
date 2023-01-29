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
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

AtreidesInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
AtreidesVehicleTypes = { "trike", "trike", "quad" }
AtreidesTankType = { "combat_tank_a" }

ActivateAI = function()
	IdlingUnits[Fremen] = { }
	IdlingUnits[Atreides] = Utils.Concat(Reinforcements.Reinforce(Atreides, InitialAtreidesReinforcements[1], AtreidesPaths[2]), Reinforcements.Reinforce(Atreides, InitialAtreidesReinforcements[2], AtreidesPaths[3]))
	FremenProduction()

	DefendAndRepairBase(Atreides, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Fremen, FremenBase, 0.75, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(AtreidesInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(AtreidesVehicleTypes) } end
	local tanksToBuild = function() return AtreidesTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(Atreides, ABarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Atreides, ALightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Atreides, AHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
end
