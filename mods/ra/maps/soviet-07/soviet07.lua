if Map.LobbyOption("difficulty") == "easy" then
	remainingTime = DateTime.Minutes(7)
elseif Map.LobbyOption("difficulty") == "normal" then
	remainingTime = DateTime.Minutes(6)
elseif Map.LobbyOption("difficulty") == "hard" then
	remainingTime = DateTime.Minutes(5)
end

Dogs = { Dog1, Dog2, Dog3, Dog4, Dog5, Dog6, Dog7, Dog8, Dog9, Dog10, Dog11, Dog12, Dog13, Dog14, Dog15, Dog16, Dog17, Dog18, Dog19 }
Engineers = { Prisoner1, Prisoner2, Prisoner3, Prisoner4, Prisoner5 }
PrisonerGuards = { PrisonerGuard1, PrisonerGuard2, PrisonerGuard3 }
EntranceGuards = { EntranceGuard1, EntranceGuard2, EntranceGuard3, EntranceGuard4, EntranceGuard5, EntranceGuard6, EntranceGuard7, EntranceGuard8, EntranceGuard9, EntranceGuard10 }
GoalGuards = { GoalGuard1, GoalGuard2, GoalGuard3, GoalGuard4 }
CCGuards = { CCGuard1, CCGuard2, CCGuard3, CCGuard4 }
StartingUnitsReinforcements = { "e1", "e1", "e1", "e1" }

CameraCCTrigger = { CPos.New(83, 71), CPos.New(84,71) }
CameraGoalCenterTrigger = { CPos.New(74, 66), CPos.New(75, 66), CPos.New(76, 66), CPos.New(77, 66) }
CameraGoalLeftTrigger = { CPos.New(62, 59), CPos.New(62, 60), CPos.New(62, 62), CPos.New(62, 63) }
CameraGoalRightTrigger = { CPos.New(90, 59), CPos.New(90, 60), CPos.New(90, 62), CPos.New(90, 63) }
ControlCenterTrigger = { CPos.New(87, 67), CPos.New(88, 67) }
ControlCenterEngineerTrigger = { CPos.New(87, 67), CPos.New(88, 67) }
FTurBottomTrigger = { CPos.New(67, 82), CPos.New(67, 83) }
FTurLeftTrigger = { CPos.New(57, 70), CPos.New(58, 70), CPos.New(59, 70), CPos.New(60, 70) }
FTurRightTrigger = { CPos.New(97, 68), CPos.New(97, 69), CPos.New(97, 70) }
GoalCenterTrigger = { CPos.New(73, 52), CPos.New(74, 52), CPos.New(75, 52), CPos.New(76, 52), CPos.New(77, 52), CPos.New(78, 52) }
GoalLeft1Trigger = { CPos.New(65, 58), CPos.New(66, 58), CPos.New(67, 58), CPos.New(65, 59), CPos.New(66, 59), CPos.New(67, 59) }
GoalLeft2Trigger = { CPos.New(65, 64), CPos.New(66, 64), CPos.New(67, 64), CPos.New(65, 65), CPos.New(66, 65), CPos.New(67, 65) }
GoalRight1Trigger = { CPos.New(86, 57), CPos.New(87, 57), CPos.New(88, 57), CPos.New(86, 58), CPos.New(87, 58), CPos.New(88, 58) }
GoalRight2Trigger = { CPos.New(86, 64), CPos.New(87, 64), CPos.New(88, 64), CPos.New(86, 65), CPos.New(87, 65), CPos.New(88, 65) }
RSoldierTrapTrigger = { CPos.New(72, 72), CPos.New(72,73), CPos.New(72,74) }
SoldierTrap2Trigger = { CPos.New(51, 73), CPos.New(51, 74) }

Trigger.OnEnteredFootprint(CameraCCTrigger, function(a, id)
	if not cameraCCTrigger and a.Owner == player then
		cameraCCTrigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraCC.Location })
	end
end)

Trigger.OnEnteredFootprint(CameraGoalCenterTrigger, function(a, id)
	if not cameraGoalCenterTrigger and a.Owner == player then
		cameraGoalCenterTrigger = true
		if not controlCenterEngineerTrigger then
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter1.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter2.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter3.Location })
		end
	end
end)

Trigger.OnEnteredFootprint(CameraGoalLeftTrigger, function(a, id)
	if not cameraGoalLeftTrigger and a.Owner == player then
		cameraGoalLeftTrigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraGoalLeft1.Location })
		Actor.Create("camera", true, { Owner = player, Location = CameraGoalLeft2.Location })
	end
end)

Trigger.OnEnteredFootprint(CameraGoalRightTrigger, function(a, id)
	if not cameraGoalRightTrigger and a.Owner == player then
		cameraGoalRightTrigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraGoalRight1.Location })
		Actor.Create("camera", true, { Owner = player, Location = CameraGoalRight2.Location })
	end
end)

Trigger.OnEnteredFootprint(ControlCenterTrigger, function(a, id)
	if not controlCenterTrigger and a.Owner == player and a.Type == "e1" then
		controlCenterTrigger = true
		FTurPrisoners.Kill()
		FTurLeft.Kill()
		FTurRight.Kill()
		FTurBottom.Kill()
		player.MarkCompletedObjective(sovietObjective1)
	end
end)

Trigger.OnEnteredFootprint(ControlCenterEngineerTrigger, function(a, id)
	if not controlCenterEngineerTrigger and a.Owner == player and a.Type == "e6" then
		controlCenterEngineerTrigger = true
		local fturA = Actor.Create("ftur", true, { Owner = player, Location = FTur1Goal.Location})
		local fturB = Actor.Create("ftur", true, { Owner = player, Location = FTur2Goal.Location})
		Camera.Position = CameraGoalCenter1.CenterPosition

		if not cameraGoalRightTrigger then
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter1.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter2.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraGoalCenter3.Location })
		end

		Utils.Do(GoalGuards, function(actor)
			if not actor.IsDead then
				actor.AttackMove(FTur1Goal.Location)
			end
		end)

		if not Tanya.IsDead then
			Tanya.Demolish(fturA)
			Tanya.Demolish(fturB)
		end

		player.MarkCompletedObjective(sovietObjective4)
	end
end)

Trigger.OnEnteredFootprint(FTurBottomTrigger, function(a, id)
	if not fTurBottomTrigger and a.Owner == player then
		fTurBottomTrigger = true
		if not rSoldierTrapTrigger then
			Actor.Create("camera", true, { Owner = player, Location = CameraRSoldier.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraFTurBottom.Location })
		end
	end
end)

Trigger.OnEnteredFootprint(FTurLeftTrigger, function(a, id)
	if not fTurLeftTrigger and a.Owner == player then
		fTurLeftTrigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraFTurLeft.Location })
	end
end)

Trigger.OnEnteredFootprint(FTurRightTrigger, function(a, id)
	if not fTurRightTrigger and a.Owner == player then
		fTurRightTrigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraFTurRight.Location })
	end
end)

Trigger.OnEnteredFootprint(GoalCenterTrigger, function(a, id)
	if not goalCenterTrigger and a.Owner == player and a.Type == "e6" then
		goalCenterTrigger = true
		player.MarkCompletedObjective(sovietObjective5)
	end
end)

Trigger.OnEnteredFootprint(GoalLeft1Trigger, function(a, id)
	if not goalLeft1Trigger and a.Owner == player and a.Type == "e6" then
		goalLeft1Trigger = true
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalLeft2Trigger, function(a, id)
	if not goalLeft2Trigger and a.Owner == player and a.Type == "e6" then
		goalLeft2Trigger = true
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalRight1Trigger, function(a, id)
	if not goalRight1Trigger and a.Owner == player and a.Type == "e6" then
		goalRight1Trigger = true
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalRight2Trigger, function(a, id)
	if not goalRight2Trigger and a.Owner == player and a.Type == "e6" then
		goalRight2Trigger = true
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(RSoldierTrapTrigger, function(a, id)
	if not rSoldierTrapTrigger and a.Owner == player then
		rSoldierTrapTrigger = true
		if not fTurBottomTrigger then
			Actor.Create("camera", true, { Owner = player, Location = CameraRSoldier.Location })
			Actor.Create("camera", true, { Owner = player, Location = CameraFTurBottom.Location })
		end

		if not RSoldier1.IsDead and not RSoldierTrap1.IsDead then
			RSoldier1.Attack(RSoldierTrap1)
		end

		if not RSoldier2.IsDead and not RSoldierTrap2.IsDead then
			RSoldier2.Attack(RSoldierTrap2)
		end
	end
end)

Trigger.OnEnteredFootprint(SoldierTrap2Trigger, function(a, id)
	if not soldierTrap2Trigger and a.Owner == player then
		soldierTrap2Trigger = true
		Actor.Create("camera", true, { Owner = player, Location = CameraSoldierTrap2.Location })
		if not SoldierTrap2.IsDead then
			PrisonEntranceGuard.Attack(SoldierTrap2)
		end
		PrisonEntranceGuard.Move(SoldierTrap2Waypoint.Location)
	end
end)

Trigger.OnAllKilled(Engineers, function()
	enemy.MarkCompletedObjective(alliedObjective)
end)

Trigger.OnAllKilled(PrisonerGuards, function()
	Utils.Do(Engineers, function(actor)
		actor.Owner = player
	end)

	Prisoner6.Owner = player
	player.MarkCompletedObjective(sovietObjective2)
end)

Trigger.OnKilled(BarlCC, function()
	if not cameraCCTrigger then
		Actor.Create("camera", true, { Owner = player, Location = CameraCC.Location })
		cameraCCTrigger = true
	end

	Utils.Do(CCGuards, function(actor)
		if not actor.IsDead then
			actor.Hunt()
		end
	end)
end)

Trigger.OnKilled(PBoxBrl, function()
	PBox.Kill()
	Utils.Do(Dogs, function(actor)
		actor.Owner = player
	end)
	player.MarkCompletedObjective(sovietObjective6)
end)

Trigger.OnKilled(PrisonEntranceGuard, function()
	if controlCenterTrigger then
		Utils.Do(PrisonerGuards, function(actor)
			if not actor.IsDead then
				actor.Hunt()
			end
		end)
	end
end)

IntroSequence = function()
	StartingUnits = Reinforcements.Reinforce(player, StartingUnitsReinforcements, { StartingUnitsSpawn.Location, SoldierTrap1Waypoint1.Location }, 0)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(EntranceGuards, function(actor)
			if not SoldierTrap1.IsDead then
				actor.Attack(SoldierTrap1)
			end
			actor.AttackMove(SoldierTrap1Waypoint1.Location)
			actor.AttackMove(SoldierTrap1Waypoint2.Location)
			actor.AttackMove(SoldierTrap1Waypoint3.Location)
		end)
		Media.PlaySpeechNotification(player, "TimerStarted")
		timerStarted = true
	end)

	-- Trigger a game over if the player lost all human units before the security system has been deactivated
	Trigger.OnAllKilled(StartingUnits, function()
		if not controlCenterTrigger then
			enemy.MarkCompletedObjective(alliedObjective)
		end
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	enemy = Player.GetPlayer("Greece")

	Camera.Position = SoldierTrap1Waypoint1.CenterPosition
	Actor.Create("camera", true, { Owner = player, Location = CameraStart1.Location })
	Actor.Create("camera", true, { Owner = player, Location = CameraStart2.Location })

	IntroSequence()

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	alliedObjective = enemy.AddPrimaryObjective("Destroy all Soviet troops.")
	sovietObjective1 = player.AddPrimaryObjective("Deactivate the security system.")
	sovietObjective2 = player.AddPrimaryObjective("Rescue the engineers.")
	sovietObjective3 = player.AddPrimaryObjective("Get the engineers to the coolant stations.")
	sovietObjective4 = player.AddPrimaryObjective("Use an Engineer to reprogram the security system.")
	sovietObjective5 = player.AddPrimaryObjective("Get an Engineer to the reactor core.")
	sovietObjective6 = player.AddSecondaryObjective("Free the dogs.")
end

Tick = function()
	if player.HasNoRequiredUnits() and timerStarted then
		enemy.MarkCompletedObjective(alliedObjective)
	end

	if remainingTime == DateTime.Minutes(5) and Map.LobbyOption("difficulty") ~= "hard" then
		Media.PlaySpeechNotification(player, "WarningFiveMinutesRemaining")
	elseif remainingTime == DateTime.Minutes(4) then
		Media.PlaySpeechNotification(player, "WarningFourMinutesRemaining")
	elseif remainingTime == DateTime.Minutes(3) then
		Media.PlaySpeechNotification(player, "WarningThreeMinutesRemaining")
	elseif remainingTime == DateTime.Minutes(2) then
		Media.PlaySpeechNotification(player, "WarningTwoMinutesRemaining")
	elseif remainingTime == DateTime.Minutes(1) then
		Media.PlaySpeechNotification(player, "WarningOneMinuteRemaining")
	end

	if goalLeft1Trigger and goalLeft2Trigger and goalRight1Trigger and goalRight2Trigger then
		player.MarkCompletedObjective(sovietObjective3)
	end

	if remainingTime > 0 and timerStarted then
		UserInterface.SetMissionText("Time until Meltdown: " .. Utils.FormatTime(remainingTime), player.Color)
		remainingTime = remainingTime - 1
	elseif remainingTime == 0 then
		UserInterface.SetMissionText("")
		enemy.MarkCompletedObjective(alliedObjective)
	end
end
