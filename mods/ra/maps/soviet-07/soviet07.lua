--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
if Difficulty == "easy" then
	RemainingTime = DateTime.Minutes(7)
elseif Difficulty == "normal" then
	RemainingTime = DateTime.Minutes(6)
elseif Difficulty == "hard" then
	RemainingTime = DateTime.Minutes(5)
end

Dogs = { Dog1, Dog2, Dog3, Dog4, Dog5, Dog6, Dog7, Dog8, Dog9, Dog10, Dog11, Dog12, Dog13, Dog14, Dog15, Dog16, Dog17, Dog18, Dog19 }
Engineers = { Prisoner1, Prisoner2, Prisoner3, Prisoner4, Prisoner5 }
PrisonerGuards = { PrisonerGuard1, PrisonerGuard2, PrisonerGuard3 }
EntranceGuards = { EntranceGuard1, EntranceGuard2, EntranceGuard3, EntranceGuard4, EntranceGuard5, EntranceGuard6, EntranceGuard7, EntranceGuard8 }
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
	if not CameraCCTriggered and a.Owner == USSR then
		CameraCCTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraCC.Location })
	end
end)

Trigger.OnEnteredFootprint(CameraGoalCenterTrigger, function(a, id)
	if not CameraGoalCenterTriggered and a.Owner == USSR then
		CameraGoalCenterTriggered = true
		if not ControlCenterEngineerTriggered then
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter1.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter2.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter3.Location })
		end
	end
end)

Trigger.OnEnteredFootprint(CameraGoalLeftTrigger, function(a, id)
	if not CameraGoalLeftTriggered and a.Owner == USSR then
		CameraGoalLeftTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalLeft1.Location })
		Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalLeft2.Location })
	end
end)

Trigger.OnEnteredFootprint(CameraGoalRightTrigger, function(a, id)
	if not CameraGoalRightTriggered and a.Owner == USSR then
		CameraGoalRightTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalRight1.Location })
		Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalRight2.Location })
	end
end)

Trigger.OnEnteredFootprint(ControlCenterTrigger, function(a, id)
	if not ControlCenterTriggered and a.Owner == USSR and a.Type == "e1" then
		ControlCenterTriggered = true
		FTurPrisoners.Kill()
		FTurLeft.Kill()
		FTurRight.Kill()
		FTurBottom.Kill()
		USSR.MarkCompletedObjective(SovietObjective1)
	end
end)

Trigger.OnEnteredFootprint(ControlCenterEngineerTrigger, function(a, id)
	if not ControlCenterEngineerTriggered and a.Owner == USSR and a.Type == "e6" then
		ControlCenterEngineerTriggered = true
		local fturA = Actor.Create("ftur", true, { Owner = USSR, Location = FTur1Goal.Location})
		local fturB = Actor.Create("ftur", true, { Owner = USSR, Location = FTur2Goal.Location})
		Camera.Position = CameraGoalCenter1.CenterPosition

		if not CameraGoalRightTriggered then
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter1.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter2.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraGoalCenter3.Location })
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

		USSR.MarkCompletedObjective(SovietObjective4)
	end
end)

Trigger.OnEnteredFootprint(FTurBottomTrigger, function(a, id)
	if not FTurBottomTriggered and a.Owner == USSR then
		FTurBottomTriggered = true
		if not RSoldierTrapTriggered then
			Actor.Create("camera", true, { Owner = USSR, Location = CameraRSoldier.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraFTurBottom.Location })
		end
	end
end)

Trigger.OnEnteredFootprint(FTurLeftTrigger, function(a, id)
	if not FTurLeftTriggered and a.Owner == USSR then
		FTurLeftTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraFTurLeft.Location })
	end
end)

Trigger.OnEnteredFootprint(FTurRightTrigger, function(a, id)
	if not FTurRightTriggered and a.Owner == USSR then
		FTurRightTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraFTurRight.Location })
	end
end)

Trigger.OnEnteredFootprint(GoalCenterTrigger, function(a, id)
	if not GoalCenterTriggered and a.Owner == USSR and a.Type == "e6" then
		GoalCenterTriggered = true
		USSR.MarkCompletedObjective(SovietObjective5)
	end
end)

Trigger.OnEnteredFootprint(GoalLeft1Trigger, function(a, id)
	if not GoalLeft1Triggered and a.Owner == USSR and a.Type == "e6" then
		GoalLeft1Triggered = true
		Media.PlaySpeechNotification(USSR, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalLeft2Trigger, function(a, id)
	if not GoalLeft2Triggered and a.Owner == USSR and a.Type == "e6" then
		GoalLeft2Triggered = true
		Media.PlaySpeechNotification(USSR, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalRight1Trigger, function(a, id)
	if not GoalRight1Triggered and a.Owner == USSR and a.Type == "e6" then
		GoalRight1Triggered = true
		Media.PlaySpeechNotification(USSR, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(GoalRight2Trigger, function(a, id)
	if not GoalRight2Triggered and a.Owner == USSR and a.Type == "e6" then
		GoalRight2Triggered = true
		Media.PlaySpeechNotification(USSR, "ControlCenterDeactivated")
	end
end)

Trigger.OnEnteredFootprint(RSoldierTrapTrigger, function(a, id)
	if not RSoldierTrapTriggered and a.Owner == USSR then
		RSoldierTrapTriggered = true
		if not FTurBottomTriggered then
			Actor.Create("camera", true, { Owner = USSR, Location = CameraRSoldier.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraFTurBottom.Location })
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
	if not SoldierTrap2Triggered and a.Owner == USSR then
		SoldierTrap2Triggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraSoldierTrap2.Location })
		if not SoldierTrap2.IsDead then
			PrisonEntranceGuard.Attack(SoldierTrap2)
		end
		PrisonEntranceGuard.Move(SoldierTrap2Waypoint.Location)
	end
end)

Trigger.OnAllKilled(Engineers, function()
	Greece.MarkCompletedObjective(AlliedObjective)
end)

Trigger.OnAllKilled(PrisonerGuards, function()
	Utils.Do(Engineers, function(actor)
		actor.Owner = USSR
	end)

	Prisoner6.Owner = USSR
	USSR.MarkCompletedObjective(SovietObjective2)
end)

Trigger.OnKilled(BarlCC, function()
	if not CameraCCTriggered then
		Actor.Create("camera", true, { Owner = USSR, Location = CameraCC.Location })
		CameraCCTriggered = true
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
		actor.Owner = USSR
	end)
	USSR.MarkCompletedObjective(SovietObjective6)
end)

Trigger.OnKilled(PrisonEntranceGuard, function()
	if ControlCenterTriggered then
		Utils.Do(PrisonerGuards, function(actor)
			if not actor.IsDead then
				actor.Hunt()
			end
		end)
	end
end)

IntroSequence = function()
	StartingUnits = Reinforcements.Reinforce(USSR, StartingUnitsReinforcements, { StartingUnitsSpawn.Location, SoldierTrap1Waypoint1.Location }, 0)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(EntranceGuards, function(actor)
			if not SoldierTrap1.IsDead then
				actor.Attack(SoldierTrap1)
			end
			actor.AttackMove(SoldierTrap1Waypoint1.Location)
			actor.AttackMove(SoldierTrap1Waypoint2.Location)
			actor.AttackMove(SoldierTrap1Waypoint3.Location)
		end)
		Media.PlaySpeechNotification(USSR, "TimerStarted")
		TimerStarted = true
	end)

	-- Trigger a game over if the player lost all human units before the security system has been deactivated
	Trigger.OnAllKilled(StartingUnits, function()
		if not ControlCenterTriggered then
			Greece.MarkCompletedObjective(AlliedObjective)
		end
	end)
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	Camera.Position = SoldierTrap1Waypoint1.CenterPosition
	Actor.Create("camera", true, { Owner = USSR, Location = CameraStart1.Location })
	Actor.Create("camera", true, { Owner = USSR, Location = CameraStart2.Location })

	IntroSequence()

	InitObjectives(USSR)
	AlliedObjective = AddPrimaryObjective(Greece, "")
	SovietObjective1 = AddPrimaryObjective(USSR, "deactivate-security-system")
	SovietObjective2 = AddPrimaryObjective(USSR, "rescue-engineers")
	SovietObjective3 = AddPrimaryObjective(USSR, "engineers-coolant-station")
	SovietObjective4 = AddPrimaryObjective(USSR, "engineer-reprogram-security")
	SovietObjective5 = AddPrimaryObjective(USSR, "engineer-reactor-core")
	SovietObjective6 = AddSecondaryObjective(USSR, "free-dogs")
end

Tick = function()
	if USSR.HasNoRequiredUnits() and TimerStarted then
		Greece.MarkCompletedObjective(AlliedObjective)
	end

	if RemainingTime == DateTime.Minutes(5) and Difficulty ~= "hard" then
		Media.PlaySpeechNotification(USSR, "WarningFiveMinutesRemaining")
	elseif RemainingTime == DateTime.Minutes(4) then
		Media.PlaySpeechNotification(USSR, "WarningFourMinutesRemaining")
	elseif RemainingTime == DateTime.Minutes(3) then
		Media.PlaySpeechNotification(USSR, "WarningThreeMinutesRemaining")
	elseif RemainingTime == DateTime.Minutes(2) then
		Media.PlaySpeechNotification(USSR, "WarningTwoMinutesRemaining")
	elseif RemainingTime == DateTime.Minutes(1) then
		Media.PlaySpeechNotification(USSR, "WarningOneMinuteRemaining")
	end

	if GoalLeft1Triggered and GoalLeft2Triggered and GoalRight1Triggered and GoalRight2Triggered then
		USSR.MarkCompletedObjective(SovietObjective3)
	end

	if RemainingTime > 0 and TimerStarted then
		if (RemainingTime % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("time-until-meltdown", { ["time"] = Utils.FormatTime(RemainingTime) })
			UserInterface.SetMissionText(Timer, USSR.Color)
		end
		RemainingTime = RemainingTime - 1
	elseif RemainingTime == 0 then
		UserInterface.SetMissionText("")
		Greece.MarkCompletedObjective(AlliedObjective)
	end
end
