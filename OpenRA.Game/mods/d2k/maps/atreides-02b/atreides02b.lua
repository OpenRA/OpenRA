--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HConyard, HOutpost, HBarracks }

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
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry1.Location, HarkonnenRally3.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally4.Location }
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

Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	player = Player.GetPlayer("Atreides")

	InitObjectives(player)
	KillAtreides = harkonnen.AddPrimaryObjective("Kill all Atreides units.")
	KillHarkonnen = player.AddPrimaryObjective("Destroy all Harkonnen forces.")

	Camera.Position = AConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenAttackPaths) end
	SendCarryallReinforcements(harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty])
	Trigger.AfterDelay(0, ActivateAI)
end
