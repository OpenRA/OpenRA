InitialForcesA = { "bggy", "e1", "e1", "e1", "e1" }
InitialForcesB = { "e1", "e1", "bggy", "e1", "e1"  }

RifleInfantryReinforcements = { "e1", "e1", "e1", "e1" }
RifleInfantryReinforcementsGDI = { "e1", "e1", "e1" }

GDIPatrolA = {"e1"}
GDIPatrolAPath = { GDIPatrolASpanPoint.Location, GDIPatrolARallyPoint.Location}

SendPatrolA = function()
    Reinforcements.Reinforce(gdi, GDIPatrolA, GDIPatrolAPath, 15, function(unit)
        unit.AttackMove(GDIPatrolARallyPoint.Location)
        unit.AttackMove(GDIPatrolARallyPointA.Location)
    end )
end

SendPatrolB = function()
    Actor56.AttackMove(GDIPatrolARallyPointA.Location)
end

SendPatrolC = function()
    Actor51.AttackMove(GDIPatrolARallyPointA.Location)
    Actor52.AttackMove(GDIPatrolARallyPointA.Location)
    Actor53.AttackMove(GDIPatrolARallyPointA.Location)
end

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

FirstRifleInfantryReinforcementsGDI = function()
    Reinforcements.Reinforce(gdi, RifleInfantryReinforcementsGDI, { GDISpawnPoint.Location, GDIRallyPoint.Location}, 15, function(unit) 
        unit.AttackMove(GDIPatrolARallyPointA.Location)
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
	GDIObjective1 = gdi.AddPrimaryObjective("Eliminate all Nod forces")

	Trigger.OnKilled(Nikoomba, function()
		nod.MarkCompletedObjective(NodObjective1)
	end)

	Camera.Position = StartRallyPoint.CenterPosition

	SendInitialForces()
    SendPatrolA()
    SendPatrolB()
    SendPatrolC()
	Trigger.AfterDelay(DateTime.Seconds(30), SendFirstInfantryReinforcements)
	Trigger.AfterDelay(DateTime.Seconds(60), SendSecondInfantryReinforcements)
    Trigger.AfterDelay(DateTime.Seconds(20), FirstRifleInfantryReinforcementsGDI)
end

Tick = function()
	if nod.HasNoRequiredUnits() then
		gdi.MarkCompletedObjective(GDIObjective1)
	end
end
