--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { AConyard, APower1, APower2, ABarracks, ALightFactory }

AtreidesReinforcements =
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

AtreidesAttackPaths =
{
	{ AtreidesEntry1.Location, AtreidesRally1.Location },
	{ AtreidesEntry1.Location, AtreidesRally4.Location },
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry2.Location, AtreidesRally3.Location }
}

AtreidesAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20)
}

AtreidesAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

Tick = function()
	if player.HasNoRequiredUnits() then
		atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if atreides.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end
end

WorldLoaded = function()
	atreides = Player.GetPlayer("Atreides")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	KillHarkonnen = atreides.AddPrimaryObjective("Kill all Harkonnen units.")
	KillAtreides = player.AddPrimaryObjective("Destroy all Atreides forces.")

	Camera.Position = HConyard.CenterPosition

	Trigger.OnAllKilled(AtreidesBase, function()
		Utils.Do(atreides.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(AtreidesAttackPaths) end
	SendCarryallReinforcements(atreides, 0, AtreidesAttackWaves[Difficulty], AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty])
	Trigger.AfterDelay(0, ActivateAI)
end
