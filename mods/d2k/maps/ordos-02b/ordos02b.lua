--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HConyard, HPower1, HPower2, HBarracks, HOutpost }

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
	{ HarkonnenEntry1.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally3.Location }
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
	if Ordos.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if Harkonnen.HasNoRequiredUnits() and not Ordos.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillHarkonnen)
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Ordos = Player.GetPlayer("Ordos")

	InitObjectives(Ordos)
	KillOrdos = AddPrimaryObjective(Harkonnen, "")
	KillHarkonnen = AddPrimaryObjective(Ordos, "destroy-harkonnen-forces")

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenAttackPaths) end
	SendCarryallReinforcements(Harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty])
	Trigger.AfterDelay(0, ActivateAI)
end
