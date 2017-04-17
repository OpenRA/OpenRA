--[[
   Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesReinforcements = { }
AtreidesReinforcements["easy"] =
{
	{ "light_inf", "light_inf" }
}

AtreidesReinforcements["normal"] =
{
	{ "light_inf", "light_inf" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
}

AtreidesReinforcements["hard"] =
{
	{ "light_inf", "light_inf" },
	{ "trike", "trike" },
	{ "light_inf", "light_inf", "light_inf" },
	{ "light_inf", "trike" },
	{ "trike", "trike" }
}

AtreidesEntryWaypoints = { AtreidesWaypoint1.Location, AtreidesWaypoint2.Location, AtreidesWaypoint3.Location, AtreidesWaypoint4.Location }
AtreidesAttackDelay = DateTime.Seconds(30)

AtreidesAttackWaves = { }
AtreidesAttackWaves["easy"] = 1
AtreidesAttackWaves["normal"] = 5
AtreidesAttackWaves["hard"] = 12

ToHarvest = { }
ToHarvest["easy"] = 2500
ToHarvest["normal"] = 3000
ToHarvest["hard"] = 3500

HarkonnenReinforcements = { "light_inf", "light_inf", "light_inf", "trike" }
HarkonnenEntryPath = { HarkonnenWaypoint.Location, HarkonnenRally.Location }

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
	if AtreidesArrived and atreides.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillAtreides)
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
	player = Player.GetPlayer("Harkonnen")
	atreides = Player.GetPlayer("Atreides")

	InitObjectives()

	Trigger.OnRemovedFromWorld(HarkonnenConyard, function()
		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" end)

		if #refs == 0 then
			atreides.MarkCompletedObjective(KillHarkonnen)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				atreides.MarkCompletedObjective(KillHarkonnen)
			end)
		end
	end)

	Media.DisplayMessage(Messages[1], "Mentat")

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, HarkonnenReinforcements, HarkonnenEntryPath)
	end)

	WavesLeft = AtreidesAttackWaves[Map.LobbyOption("difficulty")]
	SendReinforcements()
end

SendReinforcements = function()
	local units = AtreidesReinforcements[Map.LobbyOption("difficulty")]
	local delay = Utils.RandomInteger(AtreidesAttackDelay - DateTime.Seconds(2), AtreidesAttackDelay)
	AtreidesAttackDelay = AtreidesAttackDelay - (#units * 3 - 3 - WavesLeft) * DateTime.Seconds(1)
	if AtreidesAttackDelay < 0 then AtreidesAttackDelay = 0 end

	Trigger.AfterDelay(delay, function()
		Reinforcements.Reinforce(atreides, Utils.Random(units), { Utils.Random(AtreidesEntryWaypoints) }, 10, IdleHunt)

		WavesLeft = WavesLeft - 1
		if WavesLeft == 0 then
			Trigger.AfterDelay(DateTime.Seconds(1), function() AtreidesArrived = true end)
		else
			SendReinforcements()
		end
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillHarkonnen = atreides.AddPrimaryObjective("Kill all Harkonnen units.")
	GatherSpice = player.AddPrimaryObjective("Harvest " .. tostring(ToHarvest[Map.LobbyOption("difficulty")]) .. " Solaris worth of Spice.")
	KillAtreides = player.AddSecondaryObjective("Eliminate all Atreides units and reinforcements\nin the area.")

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
