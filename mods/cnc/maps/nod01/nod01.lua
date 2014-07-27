RifleInfantryReinforcements = { "e1", "e1", }
RocketInfantryReinforcements = { "e3", "e3", "e3" }

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, true)
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, true)
	Media.PlayMovieFullscreen("nodlose.vqa")
end

SendFirstInfantryReinforcements = function()
	Media.PlaySpeechNotification("Reinforce")
	Reinforcements.Reinforce(player, RifleInfantryReinforcements, StartSpawnPointRight.Location, StartRallyPoint.Location, 15)
end

SendSecondInfantryReinforcements = function()
	Media.PlaySpeechNotification("Reinforce")
	Reinforcements.Reinforce(player, RifleInfantryReinforcements, StartSpawnPointLeft.Location, StartRallyPoint.Location, 15)
end

SendLastInfantryReinforcements = function()
	Media.PlaySpeechNotification("Reinforce")
	Reinforcements.Reinforce(player, RocketInfantryReinforcements, VillageSpawnPoint.Location, VillageRallyPoint.Location, 15)
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Nod")
	enemy = OpenRA.GetPlayer("Villagers")

	Media.PlayMovieFullscreen("nod1pre.vqa", function() Media.PlayMovieFullscreen("nod1.vqa") end)

	Actor.OnKilled(Nikoomba, SendLastInfantryReinforcements)

	OpenRA.RunAfterDelay(25 * 30, SendFirstInfantryReinforcements)
	OpenRA.RunAfterDelay(25 * 60, SendSecondInfantryReinforcements)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end