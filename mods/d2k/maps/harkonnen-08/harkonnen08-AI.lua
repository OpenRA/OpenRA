--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

EarlyGameStage = DateTime.Minutes(7)

InitialProductionDelay = {
	Ordos =
	{
		easy = DateTime.Seconds(150),
		normal = DateTime.Seconds(120),
		hard = DateTime.Seconds(60)
	},

	AtreidesEnemy =
	{
		easy = DateTime.Seconds(180),
		normal = DateTime.Seconds(120),
		hard = DateTime.Seconds(60)
	},

	MercenaryEnemy =
	{
		easy = DateTime.Seconds(160),
		normal = DateTime.Seconds(80),
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
	easy = { DateTime.Seconds(10), DateTime.Seconds(13) },
	normal = { DateTime.Seconds(6), DateTime.Seconds(8) },
	hard = { DateTime.Seconds(3), DateTime.Seconds(5) }
}

LateProductionDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

EnemyInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }

OrdosVehicleTypes = { "raider", "raider", "quad" }

OrdosTankTypes =
{
	EarlyGame = { "combat_tank_o", "combat_tank_o", "siege_tank"},
	LateGame = { "combat_tank_o", "combat_tank_o", "siege_tank", "deviator" }
}

OrdosStarportTypes =
{
	EarlyGame = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_o.starport" },
	LateGame = { "trike.starport", "trike.starport", "quad.starport", "combat_tank_o.starport", "combat_tank_o.starport", "siege_tank.starport", "missile_tank.starport" }
}

AtreidesVehicleTypes = { "trike", "trike", "quad" }

AtreidesTankTypes =
{
	EarlyGame = { "combat_tank_a" },
	LateGame = { "combat_tank_a", "combat_tank_a", "siege_tank" }
}

AtreidesStarportTypes =
{
	EarlyGame ={ "trike.starport", "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport" },
	LateGame ={ "trike.starport", "quad.starport", "combat_tank_a.starport", "combat_tank_a.starport", "siege_tank.starport", "missile_tank.starport" }
}

MercenaryTankTypes = { "combat_tank_o", "combat_tank_o", "siege_tank" }

ActivateAI = function()
	Defending[Ordos] = {}
	Defending[AtreidesEnemy] = {}
	Defending[AtreidesNeutral] = {}
	Defending[MercenaryEnemy] = {}
	Defending[MercenaryAlly] = {}
	-- this is also first attack timing
	AttackDelay[Ordos] = 16000
	AttackDelay[AtreidesEnemy] = 10000
	AttackDelay[MercenaryEnemy] = 7000
	TimeBetweenAttacks[Ordos] = 8000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[AtreidesEnemy] = 7000 * DifficultyModifier[Difficulty]
	TimeBetweenAttacks[MercenaryEnemy] = 7000 * DifficultyModifier[Difficulty]
	HarvesterCount[Ordos] = 4
	HarvesterCount[AtreidesEnemy] = 4
	HarvesterCount[AtreidesNeutral] = 0
	HarvesterCount[MercenaryEnemy] = 0
	HarvesterCount[MercenaryAlly] = 0
	PatrolPoints[Ordos] = {OrdosPatrolPoint1.Location, OrdosPatrolPoint2.Location, OrdosPatrolPoint3.Location, OrdosPatrolPoint4.Location }
	PatrolPoints[AtreidesEnemy] = {AtreidesPatrolPoint1.Location, AtreidesPatrolPoint2.Location, OrdosPatrolPoint3.Location }
	DefencePerimeter[Ordos] = GetCellsInRectangle(CPos.New(39,36), CPos.New(71,54))
	DefencePerimeter[AtreidesEnemy] = Utils.Concat(GetCellsInRectangle(CPos.New(3,3), CPos.New(17,20)), GetCellsInRectangle(CPos.New(18,3), CPos.New(31,9)))
	DefencePerimeter[MercenaryEnemy] = GetCellsInRectangle(CPos.New(29,64), CPos.New(40,71))
	DefencePerimeter[MercenaryAlly] = DefencePerimeter[MercenaryEnemy]
	DefencePerimeter[AtreidesNeutral] = DefencePerimeter[AtreidesEnemy]

	IdlingUnits[Ordos] = Utils.Concat(Reinforcements.Reinforce(Ordos, InitialOrdosReinforcements[1], InitialOrdosPaths[1]), Reinforcements.Reinforce(Ordos, InitialOrdosReinforcements[2], InitialOrdosPaths[2]))
	IdlingUnits[AtreidesEnemy] = Reinforcements.Reinforce(AtreidesEnemy, InitialAtreidesReinforcements, InitialAtreidesPath)
	IdlingUnits[AtreidesNeutral] = { }
	IdlingUnits[MercenaryEnemy] = Reinforcements.Reinforce(MercenaryEnemy, InitialMercenaryReinforcements, InitialMercenaryPath)
	IdlingUnits[MercenaryAlly] = { }

	DefendAndRepairBase(Ordos, OrdosBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(AtreidesEnemy, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
	DefendAndRepairBase(MercenaryEnemy, MercenaryBase, 0.75, AttackGroupSize[Difficulty])

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

	local infantryToBuild = function() return { Utils.Random(EnemyInfantryTypes) } end
	local vehiclesToBuildOrdos = function() return { Utils.Random(OrdosVehicleTypes) } end
	local vehiclesToBuildAtreides = function() return { Utils.Random(AtreidesVehicleTypes) } end

	local tanksToBuildOrdos = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(OrdosTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(OrdosTankTypes["LateGame"]) }
		end
	end

	local tanksToBuildAtreides = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(AtreidesTankTypes["EarlyGame"]) }
		else
			return { Utils.Random(AtreidesTankTypes["LateGame"]) }
		end
	end

	local tanksToBuildMercenary = function() return { Utils.Random(MercenaryTankTypes) } end
	local unitsToBuyOrdos = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(OrdosStarportTypes["EarlyGame"]) }
		else
			return { Utils.Random(OrdosStarportTypes["LateGame"]) }
		end
	 end

	local unitsToBuyAtreides = function()
		if EarlyGameStage >= DateTime.GameTime then
			return { Utils.Random(AtreidesStarportTypes["EarlyGame"]) }
		else
			return { Utils.Random(AtreidesStarportTypes["LateGame"]) }
		end
	end

	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay["Ordos"][Difficulty], function ()
		ProduceUnits(Ordos, OBarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OLightFactory, delay, vehiclesToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OHeavyFactory, delay, tanksToBuildOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Ordos, OStarport, delay, unitsToBuyOrdos, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesEnemy"][Difficulty], function ()
		ProduceUnits(AtreidesEnemy, ABarracks1, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesEnemy, ALightFactory, delay, vehiclesToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesEnemy, AHeavyFactory, delay, tanksToBuildAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(AtreidesEnemy, AStarport, delay, unitsToBuyAtreides, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.AfterDelay(InitialProductionDelay["AtreidesEnemy"][Difficulty], function ()
		ProduceUnits(MercenaryEnemy, MHeavyFactory, delay, tanksToBuildMercenary, AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	ActivateCrushLogic()

	if Difficulty == "normal" then
		Ordos.GrantCondition("base-rebuilder")
		AtreidesEnemy.GrantCondition("base-rebuilder2")
	end

	if Difficulty == "hard" then
		Ordos.GrantCondition("defense-rebuilder")
		AtreidesEnemy.GrantCondition("defense-rebuilder2")
	end

	local productionTypesAtreidis =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuildAtreides,
		heavy_factory = tanksToBuildAtreides
	}

	local productionTypesOrdos =
	{
		barracks = infantryToBuild,
		light_factory = vehiclesToBuildOrdos,
		heavy_factory = tanksToBuildOrdos
	}

	Trigger.OnBuildingPlaced(Ordos, function(p, building)
		table.insert(OrdosBase, building)
		DefendAndRepairBase(Ordos, {building}, 0.5, AttackGroupSize[Difficulty] )
		if productionTypesOrdos[building.Type] == nil then return end
		ProduceUnits(Ordos, building, delay, productionTypesOrdos[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)

	Trigger.OnBuildingPlaced(AtreidesEnemy, function(p, building)
		table.insert(AtreidesBase, building)
		DefendAndRepairBase(AtreidesEnemy, {building}, 0.75, AttackGroupSize[Difficulty] )
		if productionTypesAtreidis[building.Type] == nil then return end
		ProduceUnits(AtreidesEnemy, building, delay, productionTypesAtreidis[building.Type], AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end
