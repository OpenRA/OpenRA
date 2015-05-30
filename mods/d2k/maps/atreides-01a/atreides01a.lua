
HarkonnenReinforcements = { }
HarkonnenReinforcements["Easy"] =
{
	{ "rifle", "rifle" }
}

HarkonnenReinforcements["Normal"] =
{
	{ "rifle", "rifle" },
	{ "rifle", "rifle", "rifle" },
	{ "rifle", "trike" },
}

HarkonnenReinforcements["Hard"] =
{
	{ "rifle", "rifle" },
	{ "trike", "trike" },
	{ "rifle", "rifle", "rifle" },
	{ "rifle", "trike" },
	{ "trike", "trike" }
}

HarkonnenEntryWaypoints = { HarkonnenWaypoint1.Location, HarkonnenWaypoint2.Location, HarkonnenWaypoint3.Location, HarkonnenWaypoint4.Location }
HarkonnenAttackDelay = DateTime.Seconds(30)
HarkonnenAttackWaves = 1

AtreidesReinforcements = { "rifle", "rifle", "rifle" }
AtreidesEntryPath = { AtreidesWaypoint.Location, AtreidesRally.Location }

Messages =
{
	"Build a concrete foundation before placing your buildings.",
	"Build a Wind Trap for power.",
	"Build a Refinery to collect Spice.",
	"Build a Silo to store additional Spice."
}

ToHarvest = 2500

IdleHunt = function(actor)
	if not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

Tick = function()
	if HarkonnenArrived and harkonnen.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillHarkonnen)
	end

	if player.Resources > ToHarvest - 1 then
		player.MarkCompletedObjective(GatherSpice)
	end

	-- player has no Wind Trap
	if (player.PowerProvided <= 20 or player.PowerState ~= "Normal") and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		HasPower = false
		Media.DisplayMessage(Messages[2], "")
	else
		HasPower = true
	end

	-- player has no Refinery and no Silos
	if HasPower and player.ResourceCapacity == 0 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[3], "")
	end

	if HasPower and player.Resources > player.ResourceCapacity * 0.8 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[4], "")
	end

	UserInterface.SetMissionText("Harvested resources: " .. player.Resources .. "/" .. ToHarvest, player.Color)
end

WorldLoaded = function()
	player = Player.GetPlayer("Atreides")
	harkonnen = Player.GetPlayer("Harkonnen")

	if Map.Difficulty == "Normal" then
		HarkonnenAttackWaves = 5
		ToHarvest = 3000
	elseif Map.Difficulty == "Hard" then
		HarkonnenAttackWaves = 12
		ToHarvest = 3500
	end

	InitObjectives()

	Trigger.OnRemovedFromWorld(AtreidesConyard, function()
		local refs = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
			return actor.Type == "refinery"
		end)

		if #refs == 0 then
			harkonnen.MarkCompletedObjective(KillAtreides)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				harkonnen.MarkCompletedObjective(KillAtreides)
			end)
		end
	end)

	Media.DisplayMessage(Messages[1], "")

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, AtreidesReinforcements, AtreidesEntryPath)
	end)

	SendReinforcements()
end

SendReinforcements = function()
	local units = HarkonnenReinforcements[Map.Difficulty]
	local delay = Utils.RandomInteger(HarkonnenAttackDelay - DateTime.Seconds(2), HarkonnenAttackDelay)
	HarkonnenAttackDelay = HarkonnenAttackDelay - (#units * 3 - 3 - HarkonnenAttackWaves) * DateTime.Seconds(1)
	if HarkonnenAttackDelay < 0 then HarkonnenAttackDelay = 0 end

	Trigger.AfterDelay(delay, function()
		Reinforcements.Reinforce(harkonnen, Utils.Random(units), { Utils.Random(HarkonnenEntryWaypoints) }, 10, IdleHunt)

		HarkonnenAttackWaves = HarkonnenAttackWaves - 1
		if HarkonnenAttackWaves == 0 then
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
	GatherSpice = player.AddPrimaryObjective("Harvest " .. tostring(ToHarvest) .. " Solaris worth of Spice.")
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
