--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HConyard, HPower1, HPower2, HBarracks, HOutpost }
HarkonnenBaseAreaTrigger = { CPos.New(31, 37), CPos.New(32, 37), CPos.New(33, 37), CPos.New(34, 37), CPos.New(35, 37), CPos.New(36, 37), CPos.New(37, 37), CPos.New(38, 37), CPos.New(39, 37), CPos.New(40, 37), CPos.New(41, 37), CPos.New(42, 37), CPos.New(42, 38), CPos.New(42, 39), CPos.New(42, 40), CPos.New(42, 41), CPos.New(42, 42), CPos.New(42, 43), CPos.New(42, 44), CPos.New(42, 45), CPos.New(42, 46), CPos.New(42, 47), CPos.New(42, 48), CPos.New(42, 49) }

HarkonnenReinforcements =
{
	easy =
	{
		{ "light_inf", "trike" },
		{ "light_inf", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trike", "trike" }
	},

	normal =
	{
		{ "light_inf", "trike" },
		{ "light_inf", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
		{ "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "trike" }
	},

	hard =
	{
		{ "trike", "trike" },
		{ "light_inf", "trike" },
		{ "light_inf", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
		{ "light_inf", "light_inf" },
		{ "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "trike" },
		{ "trike", "trike" }
	}
}

HarkonnenAttackPaths =
{
	{ HarkonnenEntry3.Location, HarkonnenRally3.Location },
	{ HarkonnenEntry4.Location, HarkonnenRally5.Location },
	{ HarkonnenEntry4.Location, HarkonnenRally6.Location },
	{ HarkonnenEntry5.Location, HarkonnenRally4.Location }
}

InitialHarkonnenReinforcementsPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location }
}

InitialHarkonnenReinforcements =
{
	{ "trike", "trike" },
	{ "light_inf", "light_inf" }
}

HarkonnenAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20)
}

HarkonnenAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

OrdosReinforcements = { "light_inf", "light_inf", "raider" }
OrdosEntryPath = { OrdosEntry.Location, OrdosRally.Location }

Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	player = Player.GetPlayer("Ordos")

	InitObjectives(player)
	KillOrdos = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillHarkonnen = player.AddPrimaryObjective("Destroy all Harkonnen forces.")

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", OrdosReinforcements, OrdosEntryPath, { OrdosEntryPath[1] })
	end)

	TriggerCarryallReinforcements(player, harkonnen, HarkonnenBaseAreaTrigger, InitialHarkonnenReinforcements[1], InitialHarkonnenReinforcementsPaths[1])
	TriggerCarryallReinforcements(player, harkonnen, HarkonnenBaseAreaTrigger, InitialHarkonnenReinforcements[2], InitialHarkonnenReinforcementsPaths[2])

	local path = function() return Utils.Random(HarkonnenAttackPaths) end
	SendCarryallReinforcements(harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty])
	Trigger.AfterDelay(0, ActivateAI)
end
