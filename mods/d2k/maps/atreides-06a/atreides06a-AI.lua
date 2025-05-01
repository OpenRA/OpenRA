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
	OrdosMain =
	{
		easy = DateTime.Seconds(60),
		normal = DateTime.Seconds(30),
		hard = DateTime.Seconds(0)
	},
	Smugglers =
	{
		easy = DateTime.Seconds(60),
		normal = DateTime.Seconds(30),
		hard = DateTime.Seconds(0)
	},
}
AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

EarlyAttackDelays =
{
	easy = { DateTime.Seconds(7), DateTime.Seconds(10) },
	normal = { DateTime.Seconds(4), DateTime.Seconds(6) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}
LateAttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }
OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }


ActivateAI = function()
	Ordos.Cash = 12000
	Smugglers.Cash = 3000
	Defending[Ordos] = { }
	Defending[Smugglers] = { }
	AttackDelay[Ordos] = 8000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Ordos] = 8000 * DifficultyModifier[Difficulty]
	AttackDelay[Smugglers] = 5600 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[Smugglers] = 5000 * DifficultyModifier[Difficulty]
	HarvesterCount[Ordos] = 2
	PatrolPoints[Ordos] = { OPatrolPoint1.Location, OPatrolPoint2.Location,OPatrolPoint3.Location }
	DefencePerimeter[Ordos] = GetCellsInRectangle(CPos.New(22,8), CPos.New(71,23))
	DefencePerimeter[Smugglers] = GetCellsInRectangle(CPos.New(6,3), CPos.New(14,15))
	LastHarvesterEaten[Ordos] = true
	IdlingUnits[Ordos] = Reinforcements.Reinforce(Ordos, InitialReinforcementsSquads[1], InitialPaths[1])

	IdlingUnits[Smugglers] = Reinforcements.Reinforce(Smugglers, InitialReinforcementsSquads[2], InitialPaths[2])
	IdlingUnits[OrdosSmall] = Reinforcements.Reinforce(OrdosSmall, InitialReinforcementsSquads[3], InitialPaths[3])

	DefendAndRepairBase(Ordos, OrdosMainBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Smugglers, SmugglersBase, 0.75, AttackGroupSize[Difficulty])
	RepairBuilding(OrdosSmall, OStarport, 0.75)

	local delay = function(player)
		if EmergencyBuildRate[player] and Difficulty ~= "easy" then
			return 1
		end
		if EarlyGameStage >= DateTime.GameTime then
			return Utils.RandomInteger(EarlyAttackDelays[Difficulty][1], EarlyAttackDelays[Difficulty][2] + 1)
		else
			return Utils.RandomInteger(LateAttackDelays[Difficulty][1], LateAttackDelays[Difficulty][2] + 1)
		end
	end
	local infantryToBuild = function() return { Utils.Random(OrdosInfantryTypes) } end
	local vehilcesToBuild = function() return { Utils.Random(OrdosVehicleTypes) } end
	local tanksToBuild = function() return { Utils.Random(OrdosTankTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["OrdosMain"][Difficulty], function()
		ProduceUnits(Ordos, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OBarracks2, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OLightFactory1, delay, vehilcesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OHeavyFactory, delay, tanksToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
	Trigger.AfterDelay(InitialProductionDelay["Smugglers"][Difficulty], function()
		ProduceUnits(Smugglers, SBarracks, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Ordos.GrantCondition("base-rebuilder")
	end

	if Difficulty == "hard" then
		Ordos.GrantCondition("defense-rebuilder")
	end

	local productionTypes =
	{
		barracks = infantryToBuild,
		light_factory = vehilcesToBuild,
		heavy_factory = tanksToBuild
	}
	Trigger.OnBuildingPlaced(Ordos, function(p, building)
		table.insert(OrdosMainBase, building)
		DefendAndRepairBase(Ordos, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypes[building.Type] == nil then return end
		ProduceUnits(Ordos, building, delay, productionTypes[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

end
