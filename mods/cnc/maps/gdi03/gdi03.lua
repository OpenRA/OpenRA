MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, true)
	Media.PlayMovieFullscreen("bombaway.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, true)
	Media.PlayMovieFullscreen("gameover.vqa")
end

AttackPlayer = function()
	if not Actor.IsDead(NodBarracks) then
		Production.BuildWithPerFactoryQueue(NodBarracks, "e1", 5)
		attackSquad = Team.New(Map.FindUnitsInCircle(enemy, NodBarracks, 3))
		Team.Do(attackSquad, function(unit)
			Actor.AttackMove(unit, waypoint9.location)
			Actor.Hunt(unit)
		end)
		Team.AddEventHandler(attackSquad.OnAllKilled, OpenRA.RunAfterDelay(Utils.Seconds(15), AttackPlayer))
	end
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("GDI")
	enemy = OpenRA.GetPlayer("Nod")

	Media.PlayMovieFullscreen("gdi3.vqa", function() Media.PlayMovieFullscreen("samdie.vqa") end)

	samSites = Team.New({ Sam1, Sam2, Sam3, Sam4 })
	Team.AddEventHandler(samSites.OnAllKilled, function() Actor.Create("PowerProxy.AirSupport", { Owner = player }) end)
	OpenRA.RunAfterDelay(Utils.Seconds(15), AttackPlayer)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end
