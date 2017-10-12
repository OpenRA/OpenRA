--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = { OOutpost, ORefinery1, ORefinery2, OHeavyFactory1, OHeavyFactory2, OLightFactory1, OHiTechFactory, OResearch, ORepair, OStarport, OGunt1, OGunt2, OGunt3, OGunt4, OGunt5, OGunt6, OGunt7, OGunt8, OGunt9, OGunt10, OGunt11, OGunt12, OBarracks1, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10 }
OrdosSmallBase = { OConyard, ORefinery3, OBarracks2, OLightFactory2, OGunt13, OGunt14, OGunt15, OGunt16, OPower11, OPower12, OPower13, OPower14 }

OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "quad", "raider", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "quad", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "quad", "quad", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "siege_tank" }
	},

	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "raider" },
		{ "quad", "quad", "raider", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "siege_tank", "siege_tank" },
		{ "missile_tank", "quad", "quad", "raider", "raider" }
	}
}

OrdosStarportReinforcements =
{
	easy = { "raider", "missile_tank", "combat_tank_o", "quad", "deviator", "deviator" },
	normal = { "raider", "missile_tank", "missile_tank", "quad", "deviator", "deviator" },
	hard = { "raider", "raider", "missile_tank", "missile_tank", "quad", "quad", "deviator", "deviator" }
}

OrdosAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

OrdosStarportDelay =
{
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5)
}

OrdosAttackWaves =
{
	easy = 7,
	normal = 8,
	hard = 9
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

HarkonnenReinforcements = 
{
	{ "combat_tank_h", "combat_tank_h" },
	{ "missile_tank", "missile_tank" }
}

HarkonnenPaths = 
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location }
}

OrdosInterval =
{
	easy = { DateTime.Minutes(1) + DateTime.Seconds(30), DateTime.Minutes(2) },
	normal = { DateTime.Minutes(2) + DateTime.Seconds(20), DateTime.Minutes(2) + DateTime.Seconds(40) },
	hard = { DateTime.Minutes(3) + DateTime.Seconds(40), DateTime.Minutes(4) }
}

SendStarportReinforcements = function()
	Trigger.AfterDelay(OrdosStarportDelay[Difficulty], function()
		if OStarport.IsDead or OStarport.Owner ~= ordos_main then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(ordos_main, "frigate", OrdosStarportReinforcements[Difficulty], { OrdosStarportEntry.Location, OStarport.Location + CVec.New(1, 1) }, { OrdosStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(OrdosAttackLocation)
			IdleHunt(unit)
		end)

		Media.DisplayMessage("Ixian transports detected.", "Mentat")

		SendStarportReinforcements()
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

Tick = function()
	if player.HasNoRequiredUnits() then
		ordos_main.MarkCompletedObjective(KillHarkonnen1)
		ordos_small.MarkCompletedObjective(KillHarkonnen2)
	end

	if ordos_main.HasNoRequiredUnits() and ordos_small.HasNoRequiredUnits() and not OrdosKilled then
		Media.DisplayMessage("The Ordos have been annihilated!", "Mentat")
		OrdosKilled = true
	end

	if (OStarport.IsDead or OStarport.Owner == player) and not player.IsObjectiveCompleted(DestroyStarport) then
		player.MarkCompletedObjective(DestroyStarport)
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
	smuggler = Player.GetPlayer("Smugglers")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	DestroyStarport = player.AddPrimaryObjective("Capture or Destroy the Ordos Starport.")
	KillHarkonnen1 = ordos_main.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen2 = ordos_small.AddPrimaryObjective("Kill all Harkonnen units.")

	Camera.Position = HMCV.CenterPosition
	OrdosAttackLocation = HMCV.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(ordos_main.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Utils.Do(ordos_small.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return OrdosKilled end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(ordos_main, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = ordos_main })
	Actor.Create("upgrade.light", true, { Owner = ordos_main })
	Actor.Create("upgrade.heavy", true, { Owner = ordos_main })
	Actor.Create("upgrade.barracks", true, { Owner = ordos_small })
	Actor.Create("upgrade.light", true, { Owner = ordos_small })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Minutes(2) + DateTime.Seconds(15), 2)
	SendHarkonnenReinforcements(DateTime.Minutes(2) + DateTime.Seconds(45), 1)
	SendHarkonnenReinforcements(DateTime.Minutes(4) + DateTime.Seconds(30), 2)
end
