reinforce1 = { "e1", "e1", "e1", "bike", }
reinforce2 = { "bike" }

MissionAccomplished =  function()
	Mission.MissionOver({ player }, nil, false)
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, false)
end

Dino1Patrol = function()
	Actor.Patrol(DinoTric, { WP1, WP0 }, 25, true)
end

Dino2Patrol = function()
	Actor.Patrol(DinoTrex, { WP2, WP3 }, 0, false)
end

ReinforceFromSea = function(passengers, entry, dropoff)
	local hovercraft, troops = Reinforcements.Insert(player, "oldlst", passengers, { entry, dropoff  }, { entry })
	Media.PlaySpeechNotification("Reinforce")
end

SetCameraPos = function()
	OpenRA.SetViewportCenterPosition(CameraStart.CenterPosition)
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Nod")
	enemy = OpenRA.GetPlayer("Dinosaur")

	Media.PlayMovieFullscreen("generic.vqa", function() Media.PlayMovieFullscreen("dino.vqa") end)

	Dino1Patrol()
	Dino2Patrol()

	SetCameraPos()

	ReinforceFromSea(reinforce1, EntryA.Location, ReinforceA.Location)

	OpenRA.RunAfterDelay(25 * 15, function() ReinforceFromSea(reinforce1, EntryB.Location, ReinforceB.Location) end)
	OpenRA.RunAfterDelay(25 * 300, function() ReinforceFromSea(reinforce2, EntryA.Location, ReinforceA.Location) end)
	OpenRA.RunAfterDelay(25 * 320, function() ReinforceFromSea(reinforce2, EntryB.Location, ReinforceB.Location) end)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end
