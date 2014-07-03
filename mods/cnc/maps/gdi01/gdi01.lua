InfantryReinforcements = { "e1", "e1", "e1" }
VehicleReinforcements = { "jeep" }
NodPatrol = { "e1", "e1" }
MissionIsOver = false

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

	Utils.Do(passengers, function(type)
		local passenger = Actor.Create(type, false, { Owner = player })
		transport.LoadPassenger(passenger)
	end)

	transport.Move(lstEnd.Location)
	transport.UnloadPassengers()
	transport.Wait(50)
	transport.Move(lstStart.Location)
	transport.Destroy()

	Media.PlaySpeechNotification("Reinforce")
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	Media.PlayMovieFullscreen("gdi1.vqa", function() Media.PlayMovieFullscreen("landing.vqa") end)

	Trigger.OnIdle(Gunboat, function() SetGunboatPath(Gunboat) end)

	SendNodPatrol()

	Trigger.AfterDelay(25 * 5, function() ReinforceFromSea(InfantryReinforcements) end)
	Trigger.AfterDelay(25 * 15, function() ReinforceFromSea(InfantryReinforcements) end)
	Trigger.AfterDelay(25 * 30, function() ReinforceFromSea(VehicleReinforcements) end)
	Trigger.AfterDelay(25 * 60, function() ReinforceFromSea(VehicleReinforcements) end)
end

Tick = function()
	if not MissionIsOver then
		if player.Won then
			MissionIsOver = true
			Media.PlayMovieFullscreen("consyard.vqa")
		end

		if player.Lost then
			MissionIsOver = true
			Media.PlayMovieFullscreen("gameover.vqa")
		end
	end
end