RifleReinforcments = { "e1", "e1", "e1", "bike" }
BazookaReinforcments = { "e3", "e3", "e3", "bike" }
BikeReinforcments = { "bike" }


ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = nod, Facing = 0, Location = transportStart })
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
	nod = Player.GetPlayer("Nod")
	dinosaur = Player.GetPlayer("Dinosaur")
	civilian = Player.GetPlayer("Civilian")

	InvestigateObj = nod.AddPrimaryObjective("Investigate the nearby village for reports of \nstrange activity.")

	Trigger.OnObjectiveAdded(nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(nod, function()
		Media.PlaySpeechNotification(nod, "Win")
	end)

	Trigger.OnPlayerLost(nod, function()
		Media.PlaySpeechNotification(nod, "Lose")
	end)

	ReachVillageObj = nod.AddPrimaryObjective("Reach the village.")

	Trigger.OnPlayerDiscovered(civilian, function(_, discoverer)
		if discoverer == nod and not nod.IsObjectiveCompleted(ReachVillageObj) then
			if not dinosaur.HasNoRequiredUnits() then
				KillDinos = nod.AddPrimaryObjective("Kill all creatures in the area.")
			end

			nod.MarkCompletedObjective(ReachVillageObj)
		end
	end)

	DinoTric.Patrol({WP0.Location, WP1.Location}, true, 3)
	DinoTrex.Patrol({WP2.Location, WP3.Location}, false)
	Trigger.OnIdle(DinoTrex, DinoTrex.Hunt)

	ReinforceWithLandingCraft(RifleReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location)
	Trigger.AfterDelay(DateTime.Seconds(1), function() InitialUnitsArrived = true end)

	Trigger.AfterDelay(DateTime.Seconds(15), function() ReinforceWithLandingCraft(BazookaReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	if Map.Difficulty == "Easy" then
		Trigger.AfterDelay(DateTime.Seconds(25), function() ReinforceWithLandingCraft(BikeReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location) end)
		Trigger.AfterDelay(DateTime.Seconds(30), function() ReinforceWithLandingCraft(BikeReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	end

	Camera.Position = CameraStart.CenterPosition
end

Tick = function()
	if InitialUnitsArrived then
		if nod.HasNoRequiredUnits() then
			nod.MarkFailedObjective(InvestigateObj)
		end
		if dinosaur.HasNoRequiredUnits() then
			if KillDinos then nod.MarkCompletedObjective(KillDinos) end
			nod.MarkCompletedObjective(InvestigateObj)
		end
	end
end
