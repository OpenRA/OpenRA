--[[
   Copyright (c) The OpenRA Developers and Contributors
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
		if CStarport.IsDead or CStarport.Owner ~= Corrino then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(Corrino, "frigate", CorrinoStarportReinforcements[Difficulty], { CorrinoStarportEntry.Location, CStarport.Location + CVec.New(1, 1) }, { CorrinoStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(OrdosAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements()

		if Harkonnen.IsObjectiveFailed(GuardOutpost) then
			return
		end

		Media.DisplayMessage(UserInterface.Translate("imperial-ships-penetrating-defense-grid"), Mentat)
	end)
end

SendHarkonnenReinforcements = function(delay, number)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenReinforcements[number], HarkonnenPaths[number], { HarkonnenPaths[number][1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Harkonnen, "Reinforce")
		end)
	end)
end

OrdosReinforcementNotification = function(currentWave, totalWaves)
	Trigger.AfterDelay(OrdosAttackDelay[Difficulty], function()
		if Harkonnen.IsObjectiveFailed(GuardOutpost) or Harkonnen.IsObjectiveCompleted(KillOrdos) then
			return
		end

		currentWave = currentWave + 1
		if currentWave > totalWaves then
			return
		end

		Media.DisplayMessage(UserInterface.Translate("enemy-carryall-drop-detected"), Mentat)

		OrdosReinforcementNotification(currentWave, totalWaves)
	end)
end


Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		OrdosMain.MarkCompletedObjective(KillHarkonnen1)
		OrdosSmall.MarkCompletedObjective(KillHarkonnen2)
		Corrino.MarkCompletedObjective(KillHarkonnen3)
	end

	if OrdosMain.HasNoRequiredUnits() and OrdosSmall.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage(UserInterface.Translate("ordos-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if Corrino.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage(UserInterface.Translate("emperor-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillCorrino)
	end

	if Harkonnen.IsObjectiveCompleted(KillOrdos) and Harkonnen.IsObjectiveCompleted(KillCorrino) and not Harkonnen.IsObjectiveCompleted(GuardOutpost) then
		Harkonnen.MarkCompletedObjective(GuardOutpost)
	end

	if (HOutpost.IsDead or HOutpost.Owner ~= Harkonnen) and not Harkonnen.IsObjectiveFailed(GuardOutpost) then
		Harkonnen.MarkFailedObjective(GuardOutpost)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[OrdosMain] then
		local units = OrdosMain.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[OrdosMain] = false
			ProtectHarvester(units[1], OrdosMain, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[OrdosSmall] then
		local units = OrdosSmall.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[OrdosSmall] = false
			ProtectHarvester(units[1], OrdosSmall, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	OrdosMain = Player.GetPlayer("Ordos Main Base")
	OrdosSmall = Player.GetPlayer("Ordos Small Base")
	Corrino = Player.GetPlayer("Corrino")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	KillOrdos = AddPrimaryObjective(Harkonnen, "destroy-ordos")
	KillCorrino = AddPrimaryObjective(Harkonnen, "destroy-imperial-forces")
	GuardOutpost = AddSecondaryObjective(Harkonnen, "keep-modified-outpost-intact")
	KillHarkonnen1 = AddPrimaryObjective(OrdosMain, "")
	KillHarkonnen2 = AddPrimaryObjective(OrdosSmall, "")
	KillHarkonnen3 = AddPrimaryObjective(Corrino, "")

	HOutpost.GrantCondition("modified")

	Camera.Position = HConYard.CenterPosition
	OrdosAttackLocation = HConYard.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(OrdosMain.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(Corrino.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage(UserInterface.Translate("protect-outpost"), Mentat)
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return Harkonnen.IsObjectiveCompleted(KillOrdos) end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(OrdosMain, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)
	OrdosReinforcementNotification(0, OrdosAttackWaves[Difficulty])

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = OrdosMain })
	Actor.Create("upgrade.light", true, { Owner = OrdosMain })
	Actor.Create("upgrade.heavy", true, { Owner = OrdosMain })
	Actor.Create("upgrade.barracks", true, { Owner = OrdosSmall })
	Actor.Create("upgrade.light", true, { Owner = OrdosSmall })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Seconds(15), 1)
	SendHarkonnenReinforcements(DateTime.Seconds(30), 1)
	SendHarkonnenReinforcements(DateTime.Seconds(35), 2)

	local ordosCondition = function() return Harkonnen.IsObjectiveCompleted(KillOrdos) end
	TriggerCarryallReinforcements(Harkonnen, OrdosMain, BaseAreaTriggers[1], OrdosHunters[1], OrdosHunterPaths[2], ordosCondition)
	TriggerCarryallReinforcements(Harkonnen, OrdosMain, BaseAreaTriggers[2], OrdosHunters[2], OrdosHunterPaths[1], ordosCondition)
end
