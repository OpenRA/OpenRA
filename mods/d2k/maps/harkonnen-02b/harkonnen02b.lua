
AtreidesBase = { AConyard, APower1, APower2, ABarracks, ALightFactory }

AtreidesReinforcements = { }
AtreidesReinforcements["easy"] =
{
	{ "light_inf", "trike" },
	{ "light_inf", "trike" },
	{ "light_inf", "light_inf", "light_inf", "trike", "trike" }
}

AtreidesReinforcements["normal"] =
{
	{ "light_inf", "trike" },
	{ "light_inf", "trike" },
	{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
	{ "light_inf", "light_inf" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
}

AtreidesReinforcements["hard"] =
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

AtreidesAttackPaths =
{
	{ AtreidesEntry1.Location, AtreidesRally1.Location },
	{ AtreidesEntry1.Location, AtreidesRally4.Location },
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry2.Location, AtreidesRally3.Location }
}

AtreidesAttackDelay = { }
AtreidesAttackDelay["easy"] = DateTime.Minutes(5)
AtreidesAttackDelay["normal"] = DateTime.Minutes(2) + DateTime.Seconds(40)
AtreidesAttackDelay["hard"] = DateTime.Minutes(1) + DateTime.Seconds(20)

AtreidesAttackWaves = { }
AtreidesAttackWaves["easy"] = 3
AtreidesAttackWaves["normal"] = 6
AtreidesAttackWaves["hard"] = 9

wave = 0
SendAtreides = function()
	Trigger.AfterDelay(AtreidesAttackDelay[Map.LobbyOption("difficulty")], function()
		wave = wave + 1
		if wave > AtreidesAttackWaves[Map.LobbyOption("difficulty")] then
			return
		end

		local path = Utils.Random(AtreidesAttackPaths)
		local units = Reinforcements.ReinforceWithTransport(atreides, "carryall.reinforce", AtreidesReinforcements[Map.LobbyOption("difficulty")][wave], path, { path[1] })[2]
		Utils.Do(units, IdleHunt)

		SendAtreides()
	end)
end

IdleHunt = function(unit)
	Trigger.OnIdle(unit, unit.Hunt)
end

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

	InitObjectives()

	Camera.Position = HConyard.CenterPosition

	Trigger.OnAllKilled(AtreidesBase, function()
		Utils.Do(atreides.GetGroundAttackers(), IdleHunt)
	end)

	SendAtreides()
	Trigger.AfterDelay(0, ActivateAI)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillHarkonnen = atreides.AddPrimaryObjective("Kill all Harkonnen units.")
	KillAtreides = player.AddPrimaryObjective("Destroy all Atreides forces.")

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
