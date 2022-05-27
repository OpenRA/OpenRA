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

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

HarkonnenVehicleTypes = { "trike", "trike", "quad" }
HarkonnenTankType = { "combat_tank_h" }

SmugglerVehicleTypes = { "raider", "raider", "quad" }
SmugglerTankType = { "combat_tank_o" }

InitAIUnits = function(house)
	LastHarvesterEaten[house] = true
	IdlingUnits[house] = Reinforcements.Reinforce(house, InitialReinforcements[house.Name], InitialReinforcementsPaths[house.Name])

	DefendAndRepairBase(house, Base[house.Name], 0.75, AttackGroupSize[Difficulty])
end

ActivateAI = function()
	InitAIUnits(harkonnen)
	InitAIUnits(smuggler)

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local hVehiclesToBuild = function() return { Utils.Random(HarkonnenVehicleTypes) } end
	local hTanksToBuild = function() return HarkonnenTankType end
	local sVehiclesToBuild = function() return { Utils.Random(SmugglerVehicleTypes) } end
	local sTanksToBuild = function() return SmugglerTankType end
	local attackTresholdSize = AttackGroupSize[Difficulty] * 2.5

	ProduceUnits(harkonnen, HBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(harkonnen, HLightFactory, delay, hVehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(harkonnen, HHeavyFactory, delay, hTanksToBuild, AttackGroupSize[Difficulty], attackTresholdSize)

	ProduceUnits(smuggler, SBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(smuggler, SLightFactory, delay, sVehiclesToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
	ProduceUnits(smuggler, SHeavyFactory, delay, sTanksToBuild, AttackGroupSize[Difficulty], attackTresholdSize)
end
