--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.

   Mission Overview:
   Soviet10 features an escort sequence where non-player controlled convoy trucks follow the nearest player controlled ground unit. This is handled by TrucksFollow() and its related functions.
   A timer which was linked to capturing buildings or destroying the first conyard has been changed to starting after a few minutes set by difficulty level.
   The conesquences of having the timer run out have been increased as well, surrounding the player with several enemy units instead of sending just four tanks.
   The starting number of tanks has been altered as well, with hard difficulty having the original amount of four.

   Ommissions from original:
   1. Radar Dome had a sell trigger when 3 rockets north of it were killed. Plan to add when if/when we add sell triggers across all missions.
   2. Turrets at gauntlet rebuild after being destroyed. Plan to add if/when we fix/add base rebuilding across all miissions.
   3. At gauntlet, all four north side turrets in were killed by barrels. Omitted for balance, add when #2 is addressed.
   4. At gauntlet, two of the south side turrets to the west end were killed by barrels.  Omitted for balance, add when #2 is addressed.
]]
ConvoyEscort =
{
	easy = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk" },
	normal = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk" },
	hard = { "3tnk", "3tnk", "3tnk", "3tnk" }
}

StartingPlanes = { Mig1, Mig2, Yak1, Yak2, Yak3 }
Airfields = { Airfield1, Airfield2, Airfield3, Airfield4, Airfield5 }
Migs = { Mig1, Mig2 }
ConvoyTrucks = { "truk", "truk", "truk" }
LightVehicleAttack = { Jeep1, Jeep2, Ltnk }
LightVehicleAttackTrigger = { CPos.New(94, 55), CPos.New(94, 56), CPos.New(94, 57), CPos.New(94, 58), CPos.New(94, 59), CPos.New(94, 70), CPos.New(94, 71), CPos.New(94, 72), CPos.New(94, 73) }
WinTriggerArea = { CPos.New(16, 55), CPos.New(16, 56), CPos.New(16, 57), CPos.New(16, 58), CPos.New(16, 59), CPos.New(16, 60), CPos.New(16, 61), CPos.New(16, 62), CPos.New(16, 63), CPos.New(16, 64), CPos.New(16, 65), CPos.New(16, 66) }
EngiDropTriggerArea = { CPos.New(66, 51), CPos.New(66, 52), CPos.New(66, 53), CPos.New(66, 54), CPos.New(66, 55), CPos.New(66, 67), CPos.New(66, 68), CPos.New(66, 69), CPos.New(66, 70) }
ChinookAmbushTrigger = { CPos.New(54, 70), CPos.New(55, 70), CPos.New(56, 70), CPos.New(57, 70), CPos.New(58, 70), CPos.New(59, 70), CPos.New(60, 70), CPos.New(61, 70) }
ChinookChalk = { "e3", "e3", "e3", "e3", "e3" }
ChinookPath = { ChinookEntry.Location, ChinookLZ.Location }
PatrolA = { "e1", "e1", "e1", "e1", "e1" }
PatrolB = { "e1", "e1", "e1", "e3", "e3" }
PatrolAPath = { PatrolA1.Location, PatrolA2.Location, PatrolA3.Location, PatrolA4.Location }
PatrolBPath = { PatrolB1.Location, PatrolB2.Location, PatrolB3.Location, PatrolB4.Location }
HunterTeam = { "1tnk", "1tnk", "1tnk", "1tnk", "2tnk", "2tnk", "arty" }

StartTimer = false
TimerColor = Player.GetPlayer("USSR").Color
TimerTicks = DateTime.Minutes(9)
ticked = TimerTicks
StartTimerDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(1)
}

MissionStart = function()
	Utils.Do(StartingPlanes, function(a)
		a.ReturnToBase()
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		StartTank.AttackMove(TrucksStop.Location)
		Reinforcements.Reinforce(USSR, ConvoyEscort, { ConvoyEntry.Location, TanksStop.Location })
		Media.PlaySpeechNotification(USSR, "ConvoyApproaching")
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local patrol1 = Reinforcements.Reinforce(Greece, PatrolA, { SouthEntry.Location, PatrolA1.Location })
		Utils.Do(patrol1, function(unit)
			Trigger.OnIdle(unit, function(patrols)
				patrols.Patrol(PatrolAPath, true, 80)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local patrol2 = Reinforcements.Reinforce(Greece, PatrolB, { NorthEntry.Location, PatrolB1.Location })
		Utils.Do(patrol2, function(unit)
			Trigger.OnIdle(unit, function(patrols)
				patrols.Patrol(PatrolBPath, true, 80)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(8), function()
		Trucks = Reinforcements.Reinforce(Turkey, ConvoyTrucks, { ConvoyEntry.Location, TrucksStop.Location })
		TrucksFollow()
		Trigger.OnAnyKilled(Trucks, function()
			USSR.MarkFailedObjective(ProtectEveryTruck)
		end)
	end)
end

MissionSetup = function()
	Trigger.OnEnteredFootprint(WinTriggerArea, function(a, id)
		if a.Type == "truk" then
			-- Stop truck from looping through TrucksFollow
			a.AddTag("Exiting")
			a.Stop()
			Trigger.OnIdle(a, function()
				a.Move(Exit.Location)
			end)
		end
	end)

	Trigger.OnEnteredFootprint({ Exit.Location }, function(a, id)
		if a.Type == "truk" then
			a.Stop()
			a.Destroy()
			if not USSR.IsObjectiveCompleted(EscortTrucks) then
				USSR.MarkCompletedObjective(EscortTrucks)
				if not USSR.IsObjectiveFailed(ProtectEveryTruck) then
					USSR.MarkCompletedObjective(ProtectEveryTruck)
				end
				if not USSR.IsObjectiveFailed(SaveMigs) then
					USSR.MarkCompletedObjective(SaveMigs)
				end
			end
		end
	end)

	Utils.Do(Airfields, function(runway)
		Trigger.OnProduction(runway, function(a, b)
			if b.Type == "mig" then
				Trigger.OnKilled(b, function()
					USSR.MarkFailedObjective(SaveMigs)
				end)
			end
		end)
	end)

	Trigger.OnAnyKilled(Migs, function()
		USSR.MarkFailedObjective(SaveMigs)
	end)

	Trigger.OnKilled(AABarrel, function()
		if not AAGun.IsDead then
			AAGun.Kill()
		end
	end)

	Trigger.OnKilled(HealCrateBarrel, function()
		Actor.Create("healcrate", true, { Owner = USSR, Location = HealCrateBarrel.Location })
	end)

	Trigger.OnEnteredFootprint(LightVehicleAttackTrigger, function(unit, id)
		if not attackTriggered and unit.Type == "3tnk" then
			Trigger.RemoveFootprintTrigger(id)
			attackTriggered = true

			Utils.Do(LightVehicleAttack, function(unit)
				if not unit.IsDead then
					IdleHunt(unit)
				end
			end)
		end
	end)

	Trigger.OnEnteredFootprint(EngiDropTriggerArea, function(unit, id)
		if unit.Owner == USSR and not dropTriggered then
			Trigger.RemoveFootprintTrigger(id)
			dropTriggered = true

			Media.PlaySpeechNotification(USSR, "SignalFlare")
			local engiFlare = Actor.Create("flare", true, { Owner = USSR, Location = EngiDropLZ.Location })
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Engidrop.TargetParatroopers(EngiDropLZ.CenterPosition, Angle.NorthWest)
			end)

			Trigger.AfterDelay(DateTime.Seconds(20), function()
				engiFlare.Destroy()
			end)
		end
	end)

	Trigger.OnEnteredFootprint(ChinookAmbushTrigger, function(unit, id)
		if not chinookTriggered and unit.Owner == USSR then
			Trigger.RemoveFootprintTrigger(id)
			chinookTriggered = true

			local chalk = Reinforcements.ReinforceWithTransport(Greece, "tran", ChinookChalk , ChinookPath, { ChinookPath[1] })[2]
			Utils.Do(chalk, function(unit)
				Trigger.OnAddedToWorld(unit, IdleHunt)
			end)
		end
	end)
end

TrucksFollow = function()
	Utils.Do(Trucks, function(truck)
		if not truck.IsDead and not Running and not truck.HasTag("Exiting") then
			-- Trucks that are next to a guard don't need to move
			local guards = Map.ActorsInCircle(truck.CenterPosition, WDist.New(2048), IsValidConvoyGuard)
			if #guards == 0 then
				-- Search nearby area to find a new guard
				guards = Map.ActorsInCircle(truck.CenterPosition, WDist.New(6144), IsValidConvoyGuard)

				if #guards == 0 then
					-- Search the whole map as a last resort: this is slow!
					guards = Utils.Where(Map.ActorsInWorld, IsValidConvoyGuard)
				end

				if #guards == 0 then
					-- Runs on the first truck; returns to not loop over the rest.
					RunForIt()
					return
				end

				truck.Move(ClosestActorTo(guards, truck).Location, 3)
			end
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), TrucksFollow)
end

IsValidConvoyGuard = function(actor)
	-- Ground units (not aircraft) owned by USSR
	return actor.Owner == USSR and actor.HasProperty("Move") and not actor.HasProperty("Land")
end

ClosestActorTo = function(actors, target)
	local closestDistSq = 0
	local closestActor = nil
	Utils.Do(actors, function(actor)
		local offset = actor.Location - target.Location
		distSq = offset.X * offset.X + offset.Y * offset.Y
		if closestActor == nil or distSq < closestDistSq then
			closestDistSq = distSq
			closestActor = actor
		end
	end)

	return closestActor
end

RunForIt = function()
	Running = true
	Media.PlaySoundNotification(USSR, "AlertBleep")
	Media.DisplayMessage("RUN FOR IT!", "Convoy commander")
	Utils.Do(Trucks, function(truck)
		if not truck.IsDead then
			truck.Stop()
			Trigger.OnIdle(truck, function(a)
				a.Move(Exit.Location)
			end)
		end
	end)
end

SendHunters = function()
	Media.PlaySpeechNotification(USSR, "AlliedForcesApproaching")
	local Hunters1 = Reinforcements.Reinforce(Greece, HunterTeam, { ConvoyEntry.Location, TrucksStop.Location })
	local Hunters2 = Reinforcements.Reinforce(Greece, HunterTeam, { SouthEntry.Location, PatrolA1.Location })
	local Hunters3 = Reinforcements.Reinforce(Greece, HunterTeam, { NorthEntry.Location, PatrolB1.Location })
	local Hunters4 = Reinforcements.Reinforce(Greece, HunterTeam, { Exit.Location, PatrolB2.Location })
	Utils.Do(Hunters1, IdleHunt)
	Utils.Do(Hunters2, IdleHunt)
	Utils.Do(Hunters3, IdleHunt)
	Utils.Do(Hunters4, IdleHunt)
end

StartTimerFunction = function()
	StartTimer = true
	Media.PlaySpeechNotification(USSR, "TimerStarted")
	Trigger.AfterDelay(DateTime.Minutes(5), function()
		Media.PlaySpeechNotification(USSR, "WarningFourMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(7), function()
		Media.PlaySpeechNotification(USSR, "WarningTwoMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(8), function()
		Media.PlaySpeechNotification(USSR, "WarningOneMinuteRemaining")
	end)
end

FinishTimer = function()
	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("We're surrounded!", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

Tick = function()
	if StartTimer then
		if ticked > 0 then
			UserInterface.SetMissionText("Corridor closes in " .. Utils.FormatTime(ticked), TimerColor)
			ticked = ticked - 1
		elseif ticked == 0 then
			FinishTimer()
			SendHunters()
			ticked = ticked - 1
		end
	end

	if Turkey.UnitsLost == 3 then
		USSR.MarkFailedObjective(EscortTrucks)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	Turkey = Player.GetPlayer("Turkey")

	InitObjectives(USSR)

	EscortTrucks = USSR.AddObjective("Escort the convoy through the mountain pass.")
	ProtectEveryTruck = USSR.AddObjective("Do not lose a single truck.", "Secondary", false)
	SaveMigs = USSR.AddObjective("Do not squander any of our new MiG aircraft.", "Secondary", false)
	BeatUSSR = Greece.AddObjective("Defeat the Soviet forces.")

	ConvoyEscort = ConvoyEscort[Difficulty]
	StartTimerDelay = StartTimerDelay[Difficulty]

	MissionStart()
	MissionSetup()

	Trigger.AfterDelay(StartTimerDelay, StartTimerFunction)
	Engidrop = Actor.Create("engidrop", false, { Owner = USSR })
	Camera.Position = DefaultCameraPosition.CenterPosition
end
