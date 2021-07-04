--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Engineers = { Engi1, Engi2, Engi3 }
TopLeftConsole = { CPos.New(49, 38), CPos.New(50, 38) }
BottomLeftConsole = { CPos.New(48, 93) }
TopRightConsole = { CPos.New(75, 40), CPos.New(76, 40) }
MiddleRightConsole = { CPos.New(81, 72), CPos.New(82, 72) }
TanyaFootprint = { CPos.New(71, 98), CPos.New(72, 98), CPos.New(73, 98), CPos.New(74, 98), CPos.New(87, 101), CPos.New(88, 101) }
GrenTeamFootprint = { CPos.New(53, 69), CPos.New(54, 69), CPos.New(55, 69) }
FlamerTeamFootprint = { CPos.New(74, 61), CPos.New(75, 61), CPos.New(76, 61) }
TimerTicks = DateTime.Minutes(26)
Scientists = { Scientist1, Scientist2, Scientist3, Scientist4, Scientist5, Scientist6, Scientist7, Scientist8, Scientist9, Scientist10 }
StartingRifles = { StartRifle1, StartRifle2, StartRifle3, StartRifle4, StartRifle5 }
AssaultTeamA = { AssaultTeamA1, AssaultTeamA2, AssaultTeamA3 }
AssaultTeamB = { AssaultTeamB1, AssaultTeamB2, AssaultTeamB3 }
AssaultTeamC = { AssaultTeamC1, AssaultTeamC2, AssaultTeamC3 }
PatrolSupport = { PatrolSupport1, PatrolSupport2, PatrolSupport3, PatrolSupport4, PatrolSupport5, PatrolSupport6 }
BarrelSquad = { BarrelSquad1, BarrelSquad2, BarrelSquad3, BarrelSquad4, BarrelSquad5 }
ScientistConsoles = { NWSilo1.Location, NWSilo2.Location, NESilo1.Location, NESilo2.Location, SESilo1.Location, SESilo2.Location, SWSilo1.Location, SWSilo2.Location }
ExplosionCheckTeam = { CheckTeam1, CheckTeam2, CheckTeam3, CheckTeam4, CheckTeam5 }

OpeningMoves = function()
	Utils.Do(StartingRifles, function(a)
		a.AttackMove(DefaultCameraPosition.Location)
	end)

	Utils.Do(Scientists, ScientistPatrol)
	GroupPatrol(PatrolA, PatrolAPath, DateTime.Seconds(5))
	GroupPatrol(PatrolB, PatrolBPath, DateTime.Seconds(5))

	Trigger.OnKilled(StartRifle1, function()
		Utils.Do(AssaultTeamA, function(a)
			IdleHunt(a)
		end)
	end)
	Trigger.OnKilled(StartRifle2, function()
		Utils.Do(AssaultTeamB, function(b)
			IdleHunt(b)
		end)
	end)
	Trigger.OnKilled(StartRifle3, function()
		Utils.Do(AssaultTeamC, function(c)
			IdleHunt(c)
		end)
	end)
end

ScientistPatrol = function(scientist)
	Trigger.OnIdle(scientist, function(sci)
		sci.Move(Utils.Random(ScientistConsoles))
	end)
end

PatrolA = { PatrolA1, PatrolA2, PatrolA3, PatrolA4 }
PatrolB = { PatrolB1, PatrolB2, PatrolB3, PatrolB4 }
PatrolAPath = { PatrolARally1.Location, PatrolARally2.Location, PatrolARally3.Location, PatrolARally4.Location }
PatrolBPath = { PatrolBRally1.Location, PatrolBRally2.Location, PatrolBRally3.Location, PatrolBRally4.Location }

GroupPatrol = function(units, waypoints, delay)
	local i = 1
	local stop = false

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end

			if unit.Location == waypoints[i] then
				local bool = Utils.All(units, function(actor) return actor.IsIdle end)

				if bool then
					stop = true

					i = i + 1
					if i > #waypoints then
						i = 1
					end

					Trigger.AfterDelay(delay, function() stop = false end)
				end
			else
				unit.AttackMove(waypoints[i])
			end
		end)
	end)
end

MiscTriggers = function()
	Trigger.OnAnyKilled(Scientists, function()
		Greece.MarkFailedObjective(Paperclip)
	end)

	Trigger.OnEnteredProximityTrigger(FlameTowerConsole.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower.IsDead then
				Media.PlaySoundNotification(Greece, "AlertBleep")
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTower.Kill()
			end
		end
	end)

	Trigger.OnKilled(TankBarrel, function()
		if not BarrelTank.IsDead then
			BarrelTank.Kill()
		end
	end)

	Trigger.OnKilled(CheckBarrel, function()
		Utils.Do(ExplosionCheckTeam, function(a)
			if not a.IsDead then
				IdleHunt(a)
			end
		end)
	end)

	Trigger.OnKilled(SquadBarrel, function()
		Utils.Do(BarrelSquad, function(b)
			if not b.IsDead then
				IdleHunt(b)
			end
		end)
	end)

	Trigger.OnAnyKilled(PatrolA, function()
		Utils.Do(PatrolSupport, function(sup)
			if not sup.IsDead then
				IdleHunt(sup)
			end
		end)
	end)

	local grensTriggered
	Trigger.OnEnteredFootprint(GrenTeamFootprint, function(actor, id)
		if actor.Owner == Greece and not grensTriggered then
			Trigger.RemoveFootprintTrigger(id)
			grensTriggered = true

			Trigger.AfterDelay(DateTime.Minutes(1), function()
				local grens = Reinforcements.Reinforce(USSR, { "e2", "e2", "e2", "e2", "e2" }, { BunkerEntryWest.Location}, 0)
				Utils.Do(grens, IdleHunt)
			end)
		end
	end)

	local flamersTriggered
	Trigger.OnEnteredFootprint(FlamerTeamFootprint, function(actor, id)
		if actor.Owner == Greece and not flamersTriggered then
			Trigger.RemoveFootprintTrigger(id)
			flamersTriggered = true

			Trigger.AfterDelay(DateTime.Minutes(1), function()
				local flamers = Reinforcements.Reinforce(USSR, { "e1", "e1", "e4", "e4", "e4" }, { BunkerEntryEast.Location}, 0)
				Utils.Do(flamers, IdleHunt)
			end)
		end
	end)

	local dogsTriggered
	Trigger.OnEnteredFootprint(TanyaFootprint, function(actor, id)
		if actor.Type == "e7.noautotarget" and not dogsTriggered then
			Trigger.RemoveFootprintTrigger(id)
			dogsTriggered = true

			local dogs = Reinforcements.Reinforce(USSR, { "dog", "dog", "dog", "dog", "dog" }, { BunkerEntryEast.Location}, 0)
			Utils.Do(dogs, IdleHunt)
		end
	end)
end

TanyaSequence = function()
	local tanyaTriggered
	Trigger.OnEnteredFootprint(TanyaFootprint, function(actor, id)
		if actor.Owner == Greece and not tanyaTriggered then
			Trigger.RemoveFootprintTrigger(id)
			tanyaTriggered = true

			IdleHunt(TankRoomDog)
			local tankCam = Actor.Create("camera", true, { Owner = Greece, Location = TankRoomCam.Location })
			Media.PlaySoundNotification(Greece, "laugh")
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				local victim1 = Reinforcements.Reinforce(USSR, { "e2" }, { TanyaEntry.Location, DiePoint1.Location })
				Trigger.OnIdle(victim1[1], function(one)
					Media.PlaySound("gun5.aud")
					one.Kill("BulletDeath")
				end)
			end)
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				local victim2 = Reinforcements.Reinforce(USSR, { "e2" }, { GrenEntry.Location, DiePoint2.Location })
				Trigger.OnIdle(victim2[1], function(two)
					two.Kill("BulletDeath")
					Media.PlaySound("gun5.aud")
				end)
			end)
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				local tanya = Reinforcements.Reinforce(Greece, { "e7.noautotarget" }, { TanyaEntry.Location, TankRoomCam.Location })
				TanyaArrived = true
				TanyaSurvive = AddPrimaryObjective(Greece, "tanya-survive")
				Media.PlaySoundNotification(Greece, "lefty")
				Trigger.OnKilled(tanya[1], function()
					Greece.MarkFailedObjective(TanyaSurvive)
				end)
			end)
			Trigger.AfterDelay(DateTime.Minutes(1), function()
				tankCam.Destroy()
			end)
		end
	end)
end

TanyaObjectiveCheck = function()
	if TanyaArrived then
		Greece.MarkCompletedObjective(TanyaSurvive)
	end
end

DeactivateMissiles = function()
	Trigger.OnAllKilled(Engineers, function()
		Greece.MarkFailedObjective(StopNukes)
	end)

	Trigger.OnEnteredFootprint(TopLeftConsole, function(actor, id)
		if actor.Type == "e6" and not TopLeftTriggered then
			Trigger.RemoveFootprintTrigger(id)
			TopLeftTriggered = true
			Media.PlaySpeechNotification(Greece, "ControlCenterDeactivated")
			Media.DisplayMessage(UserInterface.Translate("nuclear-missile-deactivated"), UserInterface.Translate("console"))
		end
	end)

	Trigger.OnEnteredFootprint(BottomLeftConsole, function(actor, id)
		if actor.Type == "e6" and not BottomLeftTriggered then
			Trigger.RemoveFootprintTrigger(id)
			BottomLeftTriggered = true
			Media.PlaySpeechNotification(Greece, "ControlCenterDeactivated")
			Media.DisplayMessage(UserInterface.Translate("nuclear-missile-deactivated"), UserInterface.Translate("console"))
		end
	end)

	Trigger.OnEnteredFootprint(TopRightConsole, function(actor, id)
		if actor.Type == "e6" and not TopRightTriggered then
			Trigger.RemoveFootprintTrigger(id)
			TopRightTriggered = true
			Media.PlaySpeechNotification(Greece, "ControlCenterDeactivated")
			Media.DisplayMessage(UserInterface.Translate("nuclear-missile-deactivated"), UserInterface.Translate("console"))
		end
	end)

	Trigger.OnEnteredFootprint(MiddleRightConsole, function(actor, id)
		if actor.Type == "e6" and not MiddleRightTriggered then
			Trigger.RemoveFootprintTrigger(id)
			MiddleRightTriggered = true
			Media.PlaySpeechNotification(Greece, "ControlCenterDeactivated")
			Media.DisplayMessage(UserInterface.Translate("nuclear-missile-deactivated"), UserInterface.Translate("console"))
		end
	end)
end

Ticked = TimerTicks
Tick = function()
	if Greece.HasNoRequiredUnits() then
		Greece.MarkFailedObjective(StopNukes)
	end

	if TopLeftTriggered and BottomLeftTriggered and TopRightTriggered and MiddleRightTriggered then
		Greece.MarkCompletedObjective(StopNukes)
		Greece.MarkCompletedObjective(Paperclip)
		TanyaObjectiveCheck()
	end

	if Ticked > 0 then
		if Ticked % DateTime.Seconds(1) == 0 then
			local timer = UserInterface.Translate("reach-target-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(timer, USSR.Color)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 then
		FinishedTimer = UserInterface.Translate("we-are-too-late")
		UserInterface.SetMissionText(FinishedTimer, USSR.Color)
		Greece.MarkFailedObjective(StopNukes)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	InitObjectives(Greece)	

	KillGreece = USSR.AddObjective("")
	StopNukes = AddPrimaryObjective(Greece, "get-engineers-to-consoles")
	Paperclip = AddSecondaryObjective(Greece, "spare-the-scientists")

	Trigger.AfterDelay(DateTime.Minutes(6), function()
		Media.PlaySpeechNotification(Greece, "TwentyMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(16), function()
		Media.PlaySpeechNotification(Greece, "TenMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(21), function()
		Media.PlaySpeechNotification(Greece, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(23), function()
		Media.PlaySpeechNotification(Greece, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(25), function()
		Media.PlaySpeechNotification(Greece, "WarningOneMinuteRemaining")
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition
	Spy1.DisguiseAsType("e1", USSR)
	Spy2.DisguiseAsType("e1", USSR)
	TimerColor = USSR.Color
	DeactivateMissiles()
	TanyaSequence()
	OpeningMoves()
	MiscTriggers()
end
