--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
RifleReinforcments = { "e1", "e1", "e1", "bike" }
BazookaReinforcments = { "e3", "e3", "e3", "bike" }
BikeReinforcments = { "bike" }


ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = player, Facing = 0, Location = transportStart })
	local subcell = 0
	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint ~= nil then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()

	Media.PlaySpeechNotification(player, "Reinforce")
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	dinosaur = Player.GetPlayer("Dinosaur")
	civilian = Player.GetPlayer("Civilian")

	InvestigateObj = player.AddPrimaryObjective("Investigate the nearby village for reports of \nstrange activity.")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	ReachVillageObj = player.AddPrimaryObjective("Reach the village.")

	Trigger.OnPlayerDiscovered(civilian, function(_, discoverer)
		if discoverer == player and not player.IsObjectiveCompleted(ReachVillageObj) then
			if not dinosaur.HasNoRequiredUnits() then
				KillDinos = player.AddPrimaryObjective("Kill all creatures in the area.")
			end

			player.MarkCompletedObjective(ReachVillageObj)
		end
	end)

	DinoTric.Patrol({WP0.Location, WP1.Location}, true, 3)
	DinoTrex.Patrol({WP2.Location, WP3.Location}, false)
	Trigger.OnIdle(DinoTrex, DinoTrex.Hunt)

	ReinforceWithLandingCraft(RifleReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location)
	Trigger.AfterDelay(DateTime.Seconds(3), function() InitialUnitsArrived = true end)

	Trigger.AfterDelay(DateTime.Seconds(15), function() ReinforceWithLandingCraft(BazookaReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	if Map.LobbyOption("difficulty") == "easy" then
		Trigger.AfterDelay(DateTime.Seconds(25), function() ReinforceWithLandingCraft(BikeReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location) end)
		Trigger.AfterDelay(DateTime.Seconds(30), function() ReinforceWithLandingCraft(BikeReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	end

	Camera.Position = CameraStart.CenterPosition
end

Tick = function()
	if InitialUnitsArrived then
		if player.HasNoRequiredUnits() then
			player.MarkFailedObjective(InvestigateObj)
		end
		if dinosaur.HasNoRequiredUnits() then
			if KillDinos then player.MarkCompletedObjective(KillDinos) end
			player.MarkCompletedObjective(InvestigateObj)
		end
	end
end
