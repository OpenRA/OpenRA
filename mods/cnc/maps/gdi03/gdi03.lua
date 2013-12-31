MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, false)
	Media.PlayMovieFullscreen("bombaway.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, false)
	Media.PlayMovieFullscreen("gameover.vqa")
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("GDI")
	enemy = OpenRA.GetPlayer("Nod")

	Media.PlayMovieFullscreen("gdi3.vqa", function() Media.PlayMovieFullscreen("samdie.vqa") end)

	samSites = Team.New({ Sam1, Sam2, Sam3, Sam4 })
	Team.AddEventHandler(samSites.OnAllKilled, function() Actor.Create("PowerProxy.AirSupport", { Owner = player }) end)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end