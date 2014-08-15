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

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.ReinforceWithTransport(player, "oldlst", units, { lstStart.Location, lstEnd.Location }, { lstStart.Location })
end

triggerAdded = false
CheckForBase = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
		return actor.Type == "fact" or actor.Type == "pyle" or actor.Type == "nuke"
	end)

	Utils.Do(baseBuildings, function(building)
		if not triggerAdded and building.Type == "fact" then
			Trigger.OnRemovedFromWorld(building, function()
				player.MarkFailedObjective(gdiObjective2)
			end)
			triggerAdded = true
		end
	end)

	return #baseBuildings >= 3
end

WorldLoaded = function()
	Media.PlayMovieFullscreen("gdi1.vqa", function() Media.PlayMovieFullscreen("landing.vqa") end)

	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	nodObjective = enemy.AddPrimaryObjective("Destroy all GDI troops")
	gdiObjective1 = player.AddPrimaryObjective("Eliminate all Nod forces in the area")
	gdiObjective2 = player.AddSecondaryObjective("Establish a beachhead")

	Trigger.OnObjectiveCompleted(player, function() Media.DisplayMessage("Objective completed") end)
	Trigger.OnObjectiveFailed(player, function() Media.DisplayMessage("Objective failed") end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(25, function()
			Media.PlayMovieFullscreen("consyard.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(25, function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	Trigger.OnIdle(Gunboat, function() SetGunboatPath(Gunboat) end)

	SendNodPatrol()

	Trigger.AfterDelay(Utils.Seconds(5), function() Reinforce(InfantryReinforcements) end)
	Trigger.AfterDelay(Utils.Seconds(15), function() Reinforce(InfantryReinforcements) end)
	Trigger.AfterDelay(Utils.Seconds(30), function() Reinforce(VehicleReinforcements) end)
	Trigger.AfterDelay(Utils.Seconds(60), function() Reinforce(VehicleReinforcements) end)
end

tick = 0
baseEstablished = false
Tick = function()
	tick = tick + 1
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(gdiObjective1)
	end

	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(nodObjective)
	end

	if not baseEstablished and tick % Utils.Seconds(1) == 0 and CheckForBase() then
		baseEstablished = true
		player.MarkCompletedObjective(gdiObjective2)
	end
end
