InitialForcesA = { "bggy", "e1", "e1", "e1", "e1" }
InitialForcesB = { "e1", "e1", "bggy", "e1", "e1"  }

RifleInfantryReinforcements = { "e1", "e1" }
RocketInfantryReinforcements = { "e3", "e3", "e3", "e3", "e3" }

SendInitialForces = function()
	Media.PlaySpeechNotification(nod, "Reinforce")
	Reinforcements.Reinforce(nod, InitialForcesA, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 5)
	Reinforcements.Reinforce(nod, InitialForcesB, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 10)
end

SendFirstInfantryReinforcements = function()
	Media.PlaySpeechNotification(nod, "Reinforce")
	Reinforcements.Reinforce(nod, RifleInfantryReinforcements, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 15)
end

SendSecondInfantryReinforcements = function()
	Media.PlaySpeechNotification(nod, "Reinforce")
	Reinforcements.Reinforce(nod, RifleInfantryReinforcements, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 15)
end

SendLastInfantryReinforcements = function()
	Media.PlaySpeechNotification(nod, "Reinforce")

	-- Move the units properly into the map before they start attacking
	local forces = Reinforcements.Reinforce(nod, RocketInfantryReinforcements, { VillageSpawnPoint.Location, VillageRallyPoint.Location }, 8)
	Utils.Do(forces, function(a)
		a.Stance = "Defend"
		a.CallFunc(function() a.Stance = "AttackAnything" end)
	end)
end

WorldLoaded = function()
	nod = Player.GetPlayer("Nod")
	gdi = Player.GetPlayer("GDI")
	villagers = Player.GetPlayer("Villagers")

	Trigger.OnObjectiveAdded(nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
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
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("nodlose.vqa")
		end)
	end)

	NodObjective1 = nod.AddPrimaryObjective("Kill Nikoomba")
	NodObjective2 = nod.AddPrimaryObjective("Destroy the village")
	NodObjective3 = nod.AddSecondaryObjective("Destroy all GDI troops in the area")
	GDIObjective1 = gdi.AddPrimaryObjective("Eliminate all Nod forces")

	Trigger.OnKilled(Nikoomba, function()
		nod.MarkCompletedObjective(NodObjective1)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			SendLastInfantryReinforcements()
		end)
	end)

	Camera.Position = StartRallyPoint.CenterPosition

	SendInitialForces()
	Trigger.AfterDelay(DateTime.Seconds(30), SendFirstInfantryReinforcements)
	Trigger.AfterDelay(DateTime.Seconds(60), SendSecondInfantryReinforcements)
end

Tick = function()
	if nod.HasNoRequiredUnits() then
		gdi.MarkCompletedObjective(GDIObjective1)
	end
	if villagers.HasNoRequiredUnits() then
		nod.MarkCompletedObjective(NodObjective2)
	end
	if gdi.HasNoRequiredUnits() then
		nod.MarkCompletedObjective(NodObjective3)
	end
end
