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

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
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
	LastHarvesterEaten[Harkonnen] = true
	InitAIUnits()

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local vehilcesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local tanksToBuild = function() return HarkonnenTankType end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceInfantry()
	ProduceUnits(Harkonnen, HarkonnenLightFactory, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	ProduceUnits(Harkonnen, HarkonnenHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
end
