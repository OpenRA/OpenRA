InfantryReinforcements = { "e1", "e1", "e1" }
VehicleReinforcements = { "jeep" }
NodPatrol = { "e1", "e1" }

SendNodPatrol = function()
	Utils.Do(NodPatrol, function(type)
		local soldier = Actor.Create(type, true, { Location = nod0.Location, Owner = enemy })
		soldier.Move(nod1.Location)
		soldier.AttackMove(nod2.Location)
		soldier.Move(nod3.Location)
		soldier.Hunt()
	end)
end

SetGunboatPath = function(gunboat)
	gunboat.AttackMove(gunboatLeft.Location)
	gunboat.AttackMove(gunboatRight.Location)
end

ReinforceFromSea = function(passengers)
	local transport = Actor.Create("oldlst", true, { Location = lstStart.Location, Owner = player })

	Utils.Do(passengers, function(actorType)
		local passenger = Actor.Create(actorType, false, { Owner = player })
		transport.LoadPassenger(passenger)
	end)

	transport.Move(lstEnd.Location)
	transport.UnloadPassengers()
	transport.Wait(50)
	transport.Move(lstStart.Location)
	transport.Destroy()

	Media.PlaySpeechNotification(player, "Reinforce")
end

WorldLoaded = function()
	Media.PlayMovieFullscreen("gdi1.vqa", function() Media.PlayMovieFullscreen("landing.vqa") end)

	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	gdiObjective = player.AddPrimaryObjective("Destroy all Nod forces in the area!")

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

	Trigger.AfterDelay(25 * 5, function() ReinforceFromSea(InfantryReinforcements) end)
	Trigger.AfterDelay(25 * 15, function() ReinforceFromSea(InfantryReinforcements) end)
	Trigger.AfterDelay(25 * 30, function() ReinforceFromSea(VehicleReinforcements) end)
	Trigger.AfterDelay(25 * 60, function() ReinforceFromSea(VehicleReinforcements) end)
end

Tick = function()
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(gdiObjective)
	end

	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(gdiObjective)
	end
end
