MCVReinforcements = { "mcv" }
InfantryReinforcements = { "e1", "e1", "e1" }
VehicleReinforcements = { "jeep" }
NodPatrol = { "e1", "e1" }

SendNodPatrol = function()
	Reinforcements.Reinforce(enemy, NodPatrol, { nod0.Location, nod1.Location }, 15, function(soldier)
		soldier.AttackMove(nod2.Location)
		soldier.Move(nod3.Location)
		soldier.Hunt()
	end)
end

SetGunboatPath = function(gunboat)
	gunboat.AttackMove(gunboatLeft.Location)
	gunboat.AttackMove(gunboatRight.Location)
end

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
end

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	ReinforceWithLandingCraft(units, lstStart.Location, lstEnd.Location, reinforcementsTarget.Location)
end

CheckForBase = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
		return actor.Type == "fact" or actor.Type == "pyle" or actor.Type == "nuke"
	end)

	return #baseBuildings >= 3
end

initialSong = "aoi"
PlayMusic = function()
	Media.PlayMusic(initialSong, PlayMusic)
	initialSong = nil
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

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
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMusic("win1")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	secureAreaObjective = player.AddPrimaryObjective("Eliminate all Nod forces in the area")
	beachheadObjective = player.AddSecondaryObjective("Establish a beachhead")

	ReinforceWithLandingCraft(MCVReinforcements, lstStart.Location + CVec.New(2, 0), lstEnd.Location + CVec.New(2, 0), mcvTarget.Location)
	Reinforce(InfantryReinforcements)

	PlayMusic()

	Trigger.OnIdle(Gunboat, function() SetGunboatPath(Gunboat) end)

	SendNodPatrol()

	Trigger.AfterDelay(DateTime.Seconds(10), function() Reinforce(InfantryReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(60), function() Reinforce(VehicleReinforcements) end)
end

Tick = function()
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(secureAreaObjective)
	end

	if DateTime.GameTime > DateTime.Seconds(5) and player.HasNoRequiredUnits() then
		player.MarkFailedObjective(beachheadObjective)
		player.MarkFailedObjective(secureAreaObjective)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not player.IsObjectiveCompleted(beachheadObjective) and CheckForBase() then
		player.MarkCompletedObjective(beachheadObjective)
	end
end
