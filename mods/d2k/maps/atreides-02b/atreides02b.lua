--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	if Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if Harkonnen.HasNoRequiredUnits() and not Atreides.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Atreides = Player.GetPlayer("Atreides")

	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Harkonnen, "")
	KillHarkonnen = AddPrimaryObjective(Atreides, "destroy-harkonnen-forces")

	Camera.Position = AConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenAttackPaths) end
	SendCarryallReinforcements(Harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty])
	Trigger.AfterDelay(0, ActivateAI)
end
