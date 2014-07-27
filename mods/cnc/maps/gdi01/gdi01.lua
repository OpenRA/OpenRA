InfantryReinforcements = { "e1", "e1", "e1" }
VehicleReinforcements = { "jeep" }
NodPatrol = { "e1", "e1" }

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, true)
	Media.PlayMovieFullscreen("consyard.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, true)
	Media.PlayMovieFullscreen("gameover.vqa")
end

SendNodPatrol = function()
	local patrol = Reinforcements.Reinforce(enemy, NodPatrol, nod0.Location, nod1.Location, 0)
	Utils.Do(patrol, function(soldier)
		Actor.Move(soldier, nod2.Location)
		Actor.Move(soldier, nod3.Location)
		Actor.Hunt(soldier)
	end)
end

SetGunboatPath = function()
	Actor.AttackMove(Gunboat, gunboatLeft.Location)
	Actor.AttackMove(Gunboat, gunboatRight.Location)
end

ReinforceFromSea = function(passengers)
	local hovercraft, troops = Reinforcements.Insert(player, "oldlst", passengers, { lstStart.Location, lstEnd.Location  }, { lstStart.Location })
	Media.PlaySpeechNotification("Reinforce")
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("GDI")
	enemy = OpenRA.GetPlayer("Nod")

	Media.PlayMovieFullscreen("gdi1.vqa", function() Media.PlayMovieFullscreen("landing.vqa") end)

	SendNodPatrol()

	OpenRA.RunAfterDelay(25 * 5, function() ReinforceFromSea(InfantryReinforcements) end)
	OpenRA.RunAfterDelay(25 * 15, function() ReinforceFromSea(InfantryReinforcements) end)
	OpenRA.RunAfterDelay(25 * 30, function() ReinforceFromSea(VehicleReinforcements) end)
	OpenRA.RunAfterDelay(25 * 60, function() ReinforceFromSea(VehicleReinforcements) end)
end

Tick = function()
	if Actor.IsIdle(Gunboat) then
		SetGunboatPath()
	end

	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end