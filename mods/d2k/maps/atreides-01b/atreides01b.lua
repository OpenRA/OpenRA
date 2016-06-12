
HarkonnenReinforcements = { }
HarkonnenReinforcements["easy"] =
{
	{ "light_inf", "light_inf" }
}

HarkonnenReinforcements["normal"] =
{
	{ "light_inf", "light_inf" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
}

HarkonnenReinforcements["hard"] =
{
	{ "light_inf", "light_inf" },
	{ "trike", "trike" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
	{ "trike", "trike" }
}

HarkonnenEntryWaypoints = { HarkonnenWaypoint1.Location, HarkonnenWaypoint2.Location, HarkonnenWaypoint3.Location, HarkonnenWaypoint4.Location }
HarkonnenAttackDelay = DateTime.Seconds(30)

HarkonnenAttackWaves = { }
HarkonnenAttackWaves["easy"] = 1
HarkonnenAttackWaves["normal"] = 5
HarkonnenAttackWaves["hard"] = 12

ToHarvest = { }
ToHarvest["easy"] = 2500
ToHarvest["normal"] = 3000
ToHarvest["hard"] = 3500

AtreidesReinforcements = { "light_inf", "light_inf", "light_inf", "light_inf" }
AtreidesEntryPath = { AtreidesWaypoint.Location, AtreidesRally.Location }

Messages =
{
	"Build a concrete foundation before placing your buildings.",
	"Build a Wind Trap for power.",
	"Build a Refinery to collect Spice.",
	"Build a Silo to store additional Spice."
}


IdleHunt = function(actor)
	if not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

Tick = function()
	if HarkonnenArrived and harkonnen.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillHarkonnen)
	end

	if player.Resources > ToHarvest[Map.LobbyOption("difficulty")] - 1 then
		player.MarkCompletedObjective(GatherSpice)
	end

	-- player has no Wind Trap
	if (player.PowerProvided <= 20 or player.PowerState ~= "Normal") and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		HasPower = false
		Media.DisplayMessage(Messages[2], "Mentat")
	else
		HasPower = true
	end

	-- player has no Refinery and no Silos
	if HasPower and player.ResourceCapacity == 0 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[3], "Mentat")
	end

	if HasPower and player.Resources > player.ResourceCapacity * 0.8 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[4], "Mentat")
	end

	UserInterface.SetMissionText("Harvested resources: " .. player.Resources .. "/" .. ToHarvest[Map.LobbyOption("difficulty")], player.Color)
end

WorldLoaded = function()
	player = Player.GetPlayer("Atreides")
	harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives()

	Trigger.OnRemovedFromWorld(AtreidesConyard, function()
		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" end)

		if #refs == 0 then
			harkonnen.MarkCompletedObjective(KillAtreides)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				harkonnen.MarkCompletedObjective(KillAtreides)
			end)
		end
	end)

	Media.DisplayMessage(Messages[1], "Mentat")

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, AtreidesReinforcements, AtreidesEntryPath)
	end)

	WavesLeft = HarkonnenAttackWaves[Map.LobbyOption("difficulty")]
	SendReinforcements()
end

SendReinforcements = function()
	local units = HarkonnenReinforcements[Map.LobbyOption("difficulty")]
	local delay = Utils.RandomInteger(HarkonnenAttackDelay - DateTime.Seconds(2), HarkonnenAttackDelay)
	HarkonnenAttackDelay = HarkonnenAttackDelay - (#units * 3 - 3 - WavesLeft) * DateTime.Seconds(1)
	if HarkonnenAttackDelay < 0 then HarkonnenAttackDelay = 0 end

	Trigger.AfterDelay(delay, function()
		Reinforcements.Reinforce(harkonnen, Utils.Random(units), { Utils.Random(HarkonnenEntryWaypoints) }, 10, IdleHunt)

		WavesLeft = WavesLeft - 1
		if WavesLeft == 0 then
			Trigger.AfterDelay(DateTime.Seconds(1), function() HarkonnenArrived = true end)
		else
			SendReinforcements()
		end
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillAtreides = harkonnen.AddPrimaryObjective("Kill all Atreides units.")
	GatherSpice = player.AddPrimaryObjective("Harvest " .. tostring(ToHarvest[Map.LobbyOption("difficulty")]) .. " Solaris worth of Spice.")
	KillHarkonnen = player.AddSecondaryObjective("Eliminate all Harkonnen units and reinforcements\nin the area.")

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
