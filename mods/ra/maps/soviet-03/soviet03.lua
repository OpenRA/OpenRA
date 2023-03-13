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

USSRReinforcements1 = { "dog", "dog", "dog", "dog", "dog" }
USSRReinforcements2 = { "e1", "e2", "e2" }
USSRReinforcementsFarm = { "e1", "e1", "e1", "e2", "e2" }
EnemyReinforcements1SpyHideout2 = { "e1", "e3" }
EnemyReinforcements2SpyHideout2 = { "e3", "e3" }
ExtractionHeliType = "tran"
ExtractionPath = { HelicopterSpawn.Location, HelicopterGoal.Location }
Farmers = { Farmer1, Farmer2, Farmer3 }
BarrierSoldiers = { BarrierSoldier1, BarrierSoldier2, BarrierSoldier3, BarrierSoldier4, BarrierSoldier5, BarrierSoldier6 }
RedBuildings = { RedBuilding1, RedBuilding2, RedBuilding3 }
BaseBuildings1 = { BaseBarrel1, BaseBarrel2, BaseBuilding1, BaseBuilding2 }
BaseBuildings2 = { BaseBuilding3, BaseBuilding4, BaseBuilding5, BaseBuilding6 }

SpyHideout1Trigger = { CPos.New(84, 45), CPos.New(85, 45), CPos.New(86, 45), CPos.New(87, 45), CPos.New(88, 45), CPos.New(89, 45), CPos.New(90, 45) }
SpyHideout2PathTrigger = { CPos.New(70, 61), CPos.New(70, 62), CPos.New(70, 63), CPos.New(70, 64), CPos.New(70, 65) }
SpyHideout2Trigger = { CPos.New(50, 63), CPos.New(50, 64), CPos.New(50, 65), CPos.New(50, 66), CPos.New(50, 67), CPos.New(50, 68), CPos.New(50, 69) }
SpyTransport1CheckpointTrigger = { CPos.New(31, 65) }
SpyTransport2CheckpointTrigger = { CPos.New(47, 51) }
Barrier1Trigger = { CPos.New(59,57), CPos.New(60,57), CPos.New(61,57), CPos.New(62,57), CPos.New(63,57), CPos.New(64,57), CPos.New(65,57), CPos.New(66,57), CPos.New(67,57), CPos.New(68,57) }
Barrier2Trigger = { CPos.New(63, 47), CPos.New(64, 47), CPos.New(65, 47), CPos.New(66, 47), CPos.New(67, 47), CPos.New(68, 47) }
SpyHideout3Trigger = { CPos.New(58, 45), CPos.New(58, 46), CPos.New(58, 47) }
RTrapTrigger = { CPos.New(46, 34), CPos.New(47, 35), CPos.New(48, 36), CPos.New(48, 37), CPos.New(48, 38), CPos.New(48, 39) }
SpyHideout4Trigger = { CPos.New(41, 34), CPos.New(41, 35), CPos.New(41, 36), CPos.New(41, 37), CPos.New(41, 38) }

IntroSequence = function()
	TheSpy.DisguiseAsType("e1", USSR)
	Actor.Create("camera", true, { Owner = USSR, Location = Playerbase.Location })
	Actor.Create("camera", true, { Owner = USSR, Location = IntroCamera.Location })
	Actor.Create("camera", true, { Owner = USSR, Location = FarmArea.Location })
	if not TheSpy.IsDead then
		TheSpy.Move(SpyWaypoint1.Location)
		TheSpy.Move(SpyWaypoint2.Location)
	end
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySoundNotification(USSR, "sking")
	end)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(USSR, "ExplosiveChargePlaced")
	end)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		if not RSoldier1.IsDead and not BaseBarrel1.IsDead then
			RSoldier1.Attack(BaseBarrel1)
		end
		if not RSoldier2.IsDead and not BaseBarrel2.IsDead then
			RSoldier2.Attack(BaseBarrel2)
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Utils.Do(BaseBuildings1, function(actor)
			if not actor.IsDead then
				actor.Kill()
			end
		end)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Utils.Do(BaseBuildings2, function(actor)
			if not actor.IsDead then
				actor.Kill()
			end
		end)
		if not RSoldier1.IsDead then
			RSoldier1.Move(SpyWaypoint2.Location)
		end
		if not RSoldier2.IsDead then
			RSoldier2.Move(SpyWaypoint2.Location)
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(8), function()
		Dogs = Reinforcements.Reinforce(USSR, USSRReinforcements1, { ReinforcementSpawn.Location, ReinforcementGoal.Location }, 0)
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		TimerStarted = true
	end)
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.PlaySoundNotification(USSR, "AlertBleep")
	end)
		Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.PlaySpeechNotification(USSR, "TimerStarted")
	end)
end

SendUSSRParadrops = function()
	local paraproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
	paraproxy.TargetParatroopers(ReinforcementDropOff.CenterPosition, Angle.North)
	paraproxy.Destroy()
end

SpyFinalSequency = function()
	if not SpyHideout4.IsDead then
		SpyHideout4.UnloadPassengers()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if not TheSpy.IsDead then
				TheSpy.Move(SpyGoal.Location)
			end
		end)
	end
end

SpyHelicopterEscape = function()
	if not SpyHelicopterEscaped then
		SpyHelicopterEscaped = true
		SpyFinalSequency()
		Actor.Create("camera", true, { Owner = USSR, Location = CameraFinalArea.Location })
		ExtractionHeli = Reinforcements.ReinforceWithTransport(Greece, ExtractionHeliType, nil, ExtractionPath)[1]
		local exitPos = CPos.New(ExtractionPath[1].X, ExtractionPath[2].Y)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not TheSpy.IsDead and not ExtractionHeli.IsDead then
				TheSpy.EnterTransport(ExtractionHeli)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			if not ExtractionHeli.IsDead then
				ExtractionHeli.Move(HelicopterEscape.Location)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			England.MarkCompletedObjective(AlliedObjective)
		end)
	end
end

Trigger.OnAllKilled(Farmers, function()
	Reinforcements.Reinforce(USSR, USSRReinforcementsFarm, { FarmSpawn.Location, FarmArea.Location }, 0)
	USSR.MarkCompletedObjective(SovietObjective2)
end)

Trigger.OnAllKilled(RedBuildings, function()
	USSR.MarkCompletedObjective(SovietObjective3)
end)

Trigger.OnAnyKilled(BarrierSoldiers, function()
	if Barrier1Triggered then
		Utils.Do(BarrierSoldiers, function(actor)
			if not actor.IsDead then
				Trigger.OnIdle(actor, actor.Hunt)
			end
		end)
	end
end)

Trigger.OnEnteredFootprint(SpyHideout1Trigger, function(a, id)
	if not SpyHideout1Triggered and a.Owner == USSR then
		SpyHideout1Triggered = true
		Trigger.RemoveFootprintTrigger(id)
		Actor.Create("camera", true, { Owner = USSR, Location = SpyHideout1.Location })
		if not TheSpy.IsDead and not SpyHideout1.IsDead then
			TheSpy.EnterTransport(SpyHideout1)
		end
	end
end)

Trigger.OnEnteredFootprint(SpyHideout2PathTrigger, function(a, id)
	if not SpyHideout2PathTriggered and a.Owner == USSR then
		SpyHideout2PathTriggered = true
		Trigger.RemoveFootprintTrigger(id)
		Actor.Create("camera", true, { Owner = USSR, Location = CameraSpyVillage.Location })
		Actor.Create("camera", true, { Owner = USSR, Location = CameraVillage.Location })
		if not TheSpy.IsDead and not SpyHideout2.IsDead then
			TheSpy.EnterTransport(SpyHideout2)
		end
	end
end)

Trigger.OnEnteredFootprint(SpyHideout2Trigger, function(a, id)
	if not SpyHideout2Triggered and a.Owner == USSR then
		SpyHideout2Triggered = true
		SpyGuards1 = Reinforcements.Reinforce(Greece, EnemyReinforcements1SpyHideout2, { EnemyReinforcements1.Location, EnemyReinforcements1Goal.Location }, 0)
		SpyGuards2 = Reinforcements.Reinforce(Greece, EnemyReinforcements2SpyHideout2, { EnemyReinforcements2.Location, EnemyReinforcements2Goal.Location }, 0)
		Utils.Do(SpyGuards1, function(actor)
			if not actor.IsDead then
				Trigger.OnIdle(actor, actor.Hunt)
			end
		end)
		Utils.Do(SpyGuards2, function(actor)
			if not actor.IsDead then
				Trigger.OnIdle(actor, actor.Hunt)
			end
		end)
		if not SpyHideout2.IsDead and not Transport.IsDead then
			SpyHideout2.UnloadPassengers()
			Transport.Move(TransportPath1Water.Location)
		end
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if not TheSpy.IsDead then
				TheSpy.Move(TransportPath1.Location)
				TheSpy.EnterTransport(Transport)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(7), function()
			SendUSSRParadrops()
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		end)
	end
end)

Trigger.OnEnteredFootprint(SpyTransport1CheckpointTrigger, function(a, id)
	if not SpyTransport1CheckpointTriggered and a.Owner == England then
		SpyTransport1CheckpointTriggered = true
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater1.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater2.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater3.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater4.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater5.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = CameraWater6.Location })
			Actor.Create("camera", true, { Owner = USSR, Location = TransportPath2.Location })
			if not Transport.IsDead then
				Transport.Wait(25)
				Transport.Move(TransportPath2Water.Location)
			end
		end)
	end
end)

Trigger.OnEnteredFootprint(SpyTransport2CheckpointTrigger, function(a, id)
	if not SpyTransport2CheckpointTriggered and a.Owner == Greece then
		SpyTransport2CheckpointTriggered = true
		Transport.UnloadPassengers()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if not TheSpy.IsDead then
				if not Hideout3Barrel.IsDead then
					TheSpy.EnterTransport(SpyHideout3)
				elseif not SpyHideout4.IsDead then
					TheSpy.EnterTransport(SpyHideout4)
				else
					TheSpy.Move(SpyGoal.Location)
				end
			end
		end)
	end
end)

Trigger.OnEnteredFootprint(Barrier1Trigger, function(a, id)
	if not Barrier1Triggered and a.Owner == USSR then
		Barrier1Triggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraBarrier.Location })
	end
end)

Trigger.OnEnteredFootprint(Barrier2Trigger, function(a, id)
	if not Barrier2Triggered and a.Owner == USSR then
		Barrier2Triggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraSpyHideout31.Location })
		Actor.Create("camera", true, { Owner = USSR, Location = CameraSpyHideout32.Location })
		Actor.Create("camera", true, { Owner = USSR, Location = CameraSpyHideout33.Location })
	end
end)

Trigger.OnEnteredFootprint(SpyHideout3Trigger, function(a, id)
	if not SpyHideout3Triggered and a.Owner == USSR then
		SpyHideout3Triggered = true
		if Difficulty ~= "hard" then
			Reinforcements.Reinforce(USSR, USSRReinforcements2, { ReinforcementSpawn.Location, CameraSpyHideout33.Location }, 0)
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		end
	end
end)

Trigger.OnEnteredFootprint(RTrapTrigger, function(a, id)
	if not RTrapTriggered and a.Owner == USSR then
		RTrapTriggered = true
		Actor.Create("camera", true, { Owner = USSR, Location = CameraFinalArea.Location })
		if not RSoldier3.IsDead and not RSoldierTrap.IsDead then
			RSoldier3.Attack(RSoldierTrap)
		end
		if not RSoldier4.IsDead and not RSoldierTrap.IsDead then
			RSoldier4.Attack(RSoldierTrap)
		end
	end
end)

Trigger.OnEnteredFootprint(SpyHideout4Trigger, function(a, id)
	if not SpyHideout4Triggered and a.Owner == USSR then
		SpyHideout4Triggered = true
		SpyFinalSequency()
		Actor.Create("camera", true, { Owner = USSR, Location = HelicopterGoal.Location })
	end
end)

Trigger.OnKilled(Hideout1Barrel, function()
		if not Hideout1PBox.IsDead then
			Hideout1PBox.Kill()
		end
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if not SpyHideout1.IsDead then
				SpyHideout1.UnloadPassengers()
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			if not TheSpy.IsDead then
				TheSpy.Move(SpyWaypoint3.Location)
				TheSpy.Move(SpyWaypoint4.Location)
			end
		end)
		if not RSoldier1.IsDead then
			RSoldier1.Move(RStandoff.Location)
		end
		if not RSoldier2.IsDead then
			RSoldier2.Move(RStandoff.Location)
		end
end)

Trigger.OnKilled(Hideout3Barrel, function()
	if not SpyHideout3.IsDead then
		SpyHideout3.UnloadPassengers()
	end
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if not TheSpy.IsDead then
			TheSpy.Move(SpyGoal.Location)
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(7), function()
		if not TheSpy.IsDead and not SpyHideout4.IsDead then
			TheSpy.EnterTransport(SpyHideout4)
		end
	end)
end)

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	England = Player.GetPlayer("England")
	Greece = Player.GetPlayer("Greece")

	Camera.Position = Playerbase.CenterPosition
	IntroSequence()

	InitObjectives(USSR)
	AlliedObjective = AddPrimaryObjective(England, "")
	SovietObjective1 = AddPrimaryObjective(USSR, "kill-enemy-spy")
	SovietObjective2 = AddSecondaryObjective(USSR, "clear-farm-reinforcements")
	SovietObjective3 = AddSecondaryObjective(USSR, "scavenge-civilian-buildings")
end

Trigger.OnKilled(TheSpy, function()
	USSR.MarkCompletedObjective(SovietObjective1)
end)

Tick = function()
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		if USSR.HasNoRequiredUnits() then
			England.MarkCompletedObjective(AlliedObjective)
		end
	end)
	if not SpyHideout4.IsDead and SpyHideout4.HasPassengers then
		SpyReachedHideout4 = true
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
	if RemainingTime > 0 and TimerStarted then
		if (RemainingTime % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("time-remaining", { ["time"] = Utils.FormatTime(RemainingTime) })
			UserInterface.SetMissionText(Timer, USSR.Color)
		end
		RemainingTime = RemainingTime - 1
	elseif RemainingTime == 0 and not SpyReachedHideout4 then
		UserInterface.SetMissionText("")
		England.MarkCompletedObjective(AlliedObjective)
	elseif RemainingTime == 0 and SpyReachedHideout4 then
		UserInterface.SetMissionText("")
		SpyHelicopterEscape()
	end
end
