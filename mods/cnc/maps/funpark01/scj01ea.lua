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
	transport.ScriptedMove(transportstart)
	transport.Destroy()

	Media.PlaySpeechNotification(player, "Reinforce")
end

initialSong = "j1"
PlayMusic = function()
	Media.PlayMusic(initialSong, PlayMusic)
	initialSong = nil
end

WorldLoaded = function()
	nod = Player.GetPlayer("Nod")
	dinosaur = Player.GetPlayer("Dinosaur")

	NodObjective1 = nod.AddPrimaryObjective("Investigate the nearby village for reports of /nstrange activity")

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

	DinoTric.Patrol({WP0.Location, WP1.Location}, true, 3)
	DinoTrex.Patrol({WP2.Location, WP3.Location}, false)
	Trigger.OnIdle(DinoTrex, DinoTrex.Hunt)

	function(
		ReinforceWithLandingCraft(RifleReinforcments, EntryA.Location, ReinforceA.Location, ReinforceA.Location)
		InitialUnitsArrived = true
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function() ReinforceWithLandingCraft(BazookaReinforcments, EntryB.Location, ReinforceB.Location, ReinforceB.Location) end)
	Trigger.AfterDelay(DateTime.Seconds(200), function() ReinforceWithLandingCraft(BikeReinforcments, EntryA.Location, ReinforceA.Location, ReinforceA.Location) end)
	Trigger.AfterDelay(DateTime.Seconds(220), function() ReinforceWithLandingCraft(BikeReinforcments, EntryB.Location, ReinforceB.Location, ReinforceB.Location) end)

	Camera.Position = CameraStart.CenterPosition
end

Tick = function()
	if InitialUnitsArrived then
		if nod.HasNoRequiredUnits() then
			nod.MarkFailedObjective(NodObjective1)
		end
		if dinosaur.HasNoRequiredUnits() then
			nod.MarkCompletedObjective(NodObjective1)
		end
	end
end