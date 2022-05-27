--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = { OConYard, OOutpost, ORefinery1, ORefinery2, OHeavyFactory, OLightFactory1, OHiTechFactory, OGunt1, OGunt2, OGunt3, OGunt4, OBarracks1, OBarracks2, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9 }
OrdosSmallBase = { ORefinery3, OBarracks3, OLightFactory2, OGunt5, OGunt6, OPower10, OPower11, OPower12, OPower13, OSilo }
CorrinoBase = { CStarport, CPower1, CPower2 }

BaseAreaTriggers =
{
	{ CPos.New(68, 70), CPos.New(69, 70), CPos.New(70, 70), CPos.New(71, 70) },
	{ CPos.New(39, 78), CPos.New(39, 79), CPos.New(39, 80), CPos.New(39, 81), CPos.New(43, 68), CPos.New(44, 68), CPos.New(45, 68), CPos.New(46, 68), CPos.New(47, 68), CPos.New(48, 68), CPos.New(49, 68), CPos.New(50, 68), CPos.New(51, 68), CPos.New(52, 68), CPos.New(53, 68), CPos.New(54, 68), CPos.New(55, 68), CPos.New(56, 68), CPos.New(57, 68), CPos.New(58, 68), CPos.New(59, 68), CPos.New(60, 68) }
}

OrdosReinforcements =
{
	easy =
	{
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "quad" },
		{ "combat_tank_o", "raider", "light_inf", "light_inf" },
		{ "siege_tank", "combat_tank_o", "quad" }
	},

	normal =
	{
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "combat_tank_o", "raider", "raider", "light_inf" },
		{ "siege_tank", "combat_tank_o", "quad", "quad" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o" }
	},

	hard =
	{
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "quad" },
		{ "combat_tank_o", "raider", "raider", "raider" },
		{ "siege_tank", "combat_tank_o", "combat_tank_o", "quad" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "quad", "quad", "raider" }
	}
}

CorrinoStarportReinforcements =
{
	easy = { "trooper", "trooper", "quad", "quad", "trike", "trike", "missile_tank", "missile_tank" },
	normal = { "trooper", "trooper", "trooper", "quad", "quad", "trike", "trike", "missile_tank", "missile_tank" },
	hard = { "trooper", "trooper", "trooper", "quad", "quad", "quad", "trike", "trike", "trike", "missile_tank", "missile_tank" }
}

OrdosAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

CorrinoStarportDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(2)
}

OrdosAttackWaves =
{
	easy = 6,
	normal = 7,
	hard = 8
}

OrdosHunters =
{
	{ "combat_tank_o", "combat_tank_o" },
	{ "missile_tank" }
}

InitialOrdosReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	{ "combat_tank_o", "combat_tank_o", "quad", "quad", "raider", "raider" }
}

OrdosPaths =
{
	{ OrdosEntry1.Location, OrdosRally1.Location },
	{ OrdosEntry2.Location, OrdosRally2.Location },
	{ OrdosEntry3.Location, OrdosRally3.Location },
	{ OrdosEntry4.Location, OrdosRally4.Location },
	{ OrdosEntry5.Location, OrdosRally5.Location },
	{ OrdosEntry6.Location, OrdosRally6.Location },
	{ OrdosEntry7.Location, OrdosRally7.Location }
}

InitialOrdosPaths =
{
	{ OrdosEntry8.Location, OrdosRally8.Location },
	{ OrdosEntry9.Location, OrdosRally9.Location },
	{ OrdosEntry10.Location, OrdosRally10.Location }
}

OrdosHunterPaths =
{
	{ OrdosEntry11.Location, OrdosEntry11.Location },
	{ OrdosEntry12.Location, OrdosEntry12.Location },
}

HarkonnenReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper" },
	{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h" }
}

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location }
}

SendStarportReinforcements = function()
	Trigger.AfterDelay(CorrinoStarportDelay[Difficulty], function()
		if CStarport.IsDead or CStarport.Owner ~= corrino then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(corrino, "frigate", CorrinoStarportReinforcements[Difficulty], { CorrinoStarportEntry.Location, CStarport.Location + CVec.New(1, 1) }, { CorrinoStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(OrdosAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements()

		if player.IsObjectiveFailed(GuardOutpost) then
			return
		end

		Media.DisplayMessage("Imperial ships penetrating defense grid!", "Mentat")
	end)
end

SendHarkonnenReinforcements = function(delay, number)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", HarkonnenReinforcements[number], HarkonnenPaths[number], { HarkonnenPaths[number][1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(player, "Reinforce")
		end)
	end)
end

OrdosReinforcementNotification = function(currentWave, totalWaves)
	Trigger.AfterDelay(OrdosAttackDelay[Difficulty], function()
		if player.IsObjectiveFailed(GuardOutpost) or player.IsObjectiveCompleted(KillOrdos) then
			return
		end

		currentWave = currentWave + 1
		if currentWave > totalWaves then
			return
		end

		Media.DisplayMessage("Enemy carryall drop detected!", "Mentat")

		OrdosReinforcementNotification(currentWave, totalWaves)
	end)
end


Tick = function()
	if player.HasNoRequiredUnits() then
		ordos_main.MarkCompletedObjective(KillHarkonnen1)
		ordos_small.MarkCompletedObjective(KillHarkonnen2)
		corrino.MarkCompletedObjective(KillHarkonnen3)
	end

	if ordos_main.HasNoRequiredUnits() and ordos_small.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage("The Ordos have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillOrdos)
	end

	if corrino.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage("The Emperor has been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillCorrino)
	end

	if player.IsObjectiveCompleted(KillOrdos) and player.IsObjectiveCompleted(KillCorrino) and not player.IsObjectiveCompleted(GuardOutpost) then
		player.MarkCompletedObjective(GuardOutpost)
	end

	if (HOutpost.IsDead or HOutpost.Owner ~= player) and not player.IsObjectiveFailed(GuardOutpost) then
		player.MarkFailedObjective(GuardOutpost)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos_main] then
		local units = ordos_main.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos_main] = false
			ProtectHarvester(units[1], ordos_main, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos_small] then
		local units = ordos_small.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos_small] = false
			ProtectHarvester(units[1], ordos_small, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	ordos_main = Player.GetPlayer("Ordos Main Base")
	ordos_small = Player.GetPlayer("Ordos Small Base")
	corrino = Player.GetPlayer("Corrino")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	KillOrdos = player.AddPrimaryObjective("Destroy the Ordos.")
	KillCorrino = player.AddPrimaryObjective("Destroy the Imperial Forces.")
	GuardOutpost = player.AddSecondaryObjective("Keep the Modified Outpost intact.")
	KillHarkonnen1 = ordos_main.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen2 = ordos_small.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen3 = corrino.AddPrimaryObjective("Kill all Harkonnen units.")

	HOutpost.GrantCondition("modified")

	Camera.Position = HConYard.CenterPosition
	OrdosAttackLocation = HConYard.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(ordos_main.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Utils.Do(ordos_small.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(corrino.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage("Protect the Outpost from attack.", "Mentat")
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillOrdos) end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(ordos_main, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)
	OrdosReinforcementNotification(0, OrdosAttackWaves[Difficulty])

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = ordos_main })
	Actor.Create("upgrade.light", true, { Owner = ordos_main })
	Actor.Create("upgrade.heavy", true, { Owner = ordos_main })
	Actor.Create("upgrade.barracks", true, { Owner = ordos_small })
	Actor.Create("upgrade.light", true, { Owner = ordos_small })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Seconds(15), 1)
	SendHarkonnenReinforcements(DateTime.Seconds(30), 1)
	SendHarkonnenReinforcements(DateTime.Seconds(35), 2)

	local ordosCondition = function() return player.IsObjectiveCompleted(KillOrdos) end
	TriggerCarryallReinforcements(player, ordos_main, BaseAreaTriggers[1], OrdosHunters[1], OrdosHunterPaths[2], ordosCondition)
	TriggerCarryallReinforcements(player, ordos_main, BaseAreaTriggers[2], OrdosHunters[2], OrdosHunterPaths[1], ordosCondition)
end
