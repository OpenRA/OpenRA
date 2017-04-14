--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HConyard, HPower1, HPower2, HBarracks, HOutpost }
HarkonnenBaseAreaTrigger = { CPos.New(31, 37), CPos.New(32, 37), CPos.New(33, 37), CPos.New(34, 37), CPos.New(35, 37), CPos.New(36, 37), CPos.New(37, 37), CPos.New(38, 37), CPos.New(39, 37), CPos.New(40, 37), CPos.New(41, 37), CPos.New(42, 37), CPos.New(42, 38), CPos.New(42, 39), CPos.New(42, 40), CPos.New(42, 41), CPos.New(42, 42), CPos.New(42, 43), CPos.New(42, 44), CPos.New(42, 45), CPos.New(42, 46), CPos.New(42, 47), CPos.New(42, 48), CPos.New(42, 49) }

HarkonnenReinforcements = { }
HarkonnenReinforcements["easy"] =
{
	{ "light_inf", "trike" },
	{ "light_inf", "trike" },
	{ "light_inf", "light_inf", "light_inf", "trike", "trike" }
}

HarkonnenReinforcements["normal"] =
{
	{ "light_inf", "trike" },
	{ "light_inf", "trike" },
	{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
	{ "light_inf", "light_inf" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
}

HarkonnenReinforcements["hard"] =
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

HarkonnenAttackDelay = { }
HarkonnenAttackDelay["easy"] = DateTime.Minutes(5)
HarkonnenAttackDelay["normal"] = DateTime.Minutes(2) + DateTime.Seconds(40)
HarkonnenAttackDelay["hard"] = DateTime.Minutes(1) + DateTime.Seconds(20)

HarkonnenAttackWaves = { }
HarkonnenAttackWaves["easy"] = 3
HarkonnenAttackWaves["normal"] = 6
HarkonnenAttackWaves["hard"] = 9

OrdosReinforcements = { "light_inf", "light_inf", "raider" }
OrdosEntryPath = { OrdosEntry.Location, OrdosRally.Location }

wave = 0
SendHarkonnen = function()
	Trigger.AfterDelay(HarkonnenAttackDelay[Map.LobbyOption("difficulty")], function()
		wave = wave + 1
		if wave > HarkonnenAttackWaves[Map.LobbyOption("difficulty")] then
			return
		end

		local path = Utils.Random(HarkonnenAttackPaths)
		local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", HarkonnenReinforcements[Map.LobbyOption("difficulty")][wave], path, { path[1] })[2]
		Utils.Do(units, IdleHunt)

		SendHarkonnen()
	end)
end

IdleHunt = function(unit)
	Trigger.OnIdle(unit, unit.Hunt)
end

SendInitialUnits = function(areaTrigger, unit, path, check)
	Trigger.OnEnteredFootprint(areaTrigger, function(a, id)
		if not check and a.Owner == player then
			local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", unit, path, { path[1] })[2]
			Utils.Do(units, IdleHunt)
			check = true
		end
	end)
end

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

	InitObjectives()

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", OrdosReinforcements, OrdosEntryPath, { OrdosEntryPath[1] })
	end)

	SendInitialUnits(HarkonnenBaseAreaTrigger, InitialHarkonnenReinforcements[1], InitialHarkonnenReinforcementsPaths[1], InitialReinforcementsSent1)
	SendInitialUnits(HarkonnenBaseAreaTrigger, InitialHarkonnenReinforcements[2], InitialHarkonnenReinforcementsPaths[2], InitialReinforcementsSent2)

	SendHarkonnen()
	Trigger.AfterDelay(0, ActivateAI)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillOrdos = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillHarkonnen = player.AddPrimaryObjective("Destroy all Harkonnen forces.")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end
