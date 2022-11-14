--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
SpyType = { "spy" }
SpyEntryPath = { SpyEntry.Location, SpyLoadout.Location }
InsertionTransport = "lst.in"
TrukPath1 = { SpyCamera1, TrukWaypoint1, TrukWaypoint2, TrukWaypoint3, TrukWaypoint4 }
TrukPath2 = { TruckWaypoint5, TruckCrash }

ExtractionHeliType = "tran"
ExtractionPath = { ExtractionEntry.Location, ExtractionLZ.Location }

GreeceReinforcements =
{
	{ types = { "e3", "e3", "e1", "e1", "e1" }, entry = { SpyEntry.Location, SpyLoadout.Location } },
	{ types = { "jeep", "1tnk", "1tnk", "2tnk", "2tnk" }, entry = { GreeceEntry1.Location, GreeceLoadout1.Location } },
	{ types = { "e6", "e6", "e6", "e6", "e6" }, entry = { GreeceEntry2.Location, GreeceLoadout2.Location } }
}

FlameTowerDogs = { FlameTowerDog1, FlameTowerDog2 }

PatrolA = { PatrolA1, PatrolA2, PatrolA3 }
PatrolB = { PatrolB1, PatrolB2, PatrolB3 }
PatrolC = { PatrolC1, PatrolC2, PatrolC3 }

PatrolAPath = { APatrol1.Location, CPatrol1.Location, APatrol2.Location }
PatrolBPath = { BPatrol1.Location, BPatrol2.Location, SpyCamera2.Location }
PatrolCPath = { CPatrol1.Location, CPatrol2.Location, CPatrol3.Location }

CheckpointDogs = { CheckpointDog1, CheckpointDog2 }
CheckpointRifles = { CheckpointRifle1, CheckpointRifle2 }
BridgePatrol = { CheckpointDog1, CheckpointDog2, CheckpointRifle1, CheckpointRifle2 }
BridgePatrolPath = { TrukWaypoint4.Location, BridgePatrolWay.Location }

TanyaVoices = { "tuffguy", "bombit", "laugh", "gotit", "lefty", "keepem" }
SpyVoice = "sking"
CivVoice = "guyokay"
DogBark = "dogy"
SamSites = { Sam1, Sam2, Sam3, Sam4 }

SendSpy = function()
	Camera.Position = SpyEntry.CenterPosition
	Spy = Reinforcements.ReinforceWithTransport(Greece, InsertionTransport, SpyType, SpyEntryPath, { SpyEntryPath[1] })[2][1]

	Trigger.OnKilled(Spy, function() USSR.MarkCompletedObjective(USSRObj) end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Commander! You have to disguise me in order to get through the enemy patrols.", "Spy")
		if SpecialCameras then
			SpyCameraA = Actor.Create("camera", true, { Owner = Greece, Location = SpyCamera1.Location })
			SpyCameraB = Actor.Create("camera", true, { Owner = Greece, Location = SpyCamera2.Location })
			SpyCameraC = Actor.Create("camera", true, { Owner = Greece, Location = BPatrol2.Location })
		else
			SpyCameraHard = Actor.Create("camera.small", true, { Owner = Greece, Location = FlameTowerDogRally.Location + CVec.New(2, 0) })
		end
	end)
end

ChurchFootprint = function()
	Trigger.OnEnteredProximityTrigger(ChurchSpawn.CenterPosition, WDist.FromCells(2), function(actor, id)
		if actor.Type == "spy" and not Greece.IsObjectiveCompleted(MainObj) then
			Trigger.RemoveProximityTrigger(id)
			ChurchSequence()
		end
	end)
end

ChurchSequence = function()
	Media.PlaySoundNotification(Greece, CivVoice)
	Hero = Actor.Create("c1", true, { Owner = GoodGuy, Location = ChurchSpawn.Location })
	Hero.Attack(TargetBarrel)

	Trigger.OnKilled(ResponseBarrel, function()
		if not Hero.IsDead then
			Hero.Stop()
			Hero.Move(SouthVillage.Location)
			BarrelsTower.Kill()
			Utils.Do(FlameTowerDogs, function(dogs)
				if not dogs.IsDead then
					dogs.Stop()
					dogs.AttackMove(SouthVillage.Location)
				end
			end)
			Utils.Do(PatrolA, function(patrol1)
				if not patrol1.IsDead then
					patrol1.Stop()
					patrol1.AttackMove(SouthVillage.Location)
				end
			end)
			Utils.Do(PatrolB, function(patrol2)
				if not patrol2.IsDead then
					patrol2.Stop()
					patrol2.AttackMove(SouthVillage.Location)
				end
			end)
		end
	end)
end

ActivatePatrols = function()
	Utils.Do(FlameTowerDogs, function(dogs)
		dogs.AttackMove(FlameTowerDogRally.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		GroupPatrol(PatrolA, PatrolAPath, DateTime.Seconds(7))
		GroupPatrol(PatrolB, PatrolBPath, DateTime.Seconds(6))
		GroupPatrol(PatrolC, PatrolCPath, DateTime.Seconds(6))
	end)
end

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

WarfactoryInfiltrated = function()
	FollowTruk = true
	Truk.GrantCondition("hijacked")

	Truk.Wait(DateTime.Seconds(1))
	Utils.Do(TrukPath1, function(waypoint)
		Truk.Move(waypoint.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		if SpecialCameras then
			SpyCameraA.Destroy()
			SpyCameraB.Destroy()
			SpyCameraC.Destroy()
		else
			SpyCameraHard.Destroy()
		end
	end)

	Trigger.OnEnteredProximityTrigger(TrukWaypoint4.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Type == "truk.mission" then
			Trigger.RemoveProximityTrigger(id)
			Utils.Do(CheckpointDogs, function(dog)
				dog.Move(TrukInspect.Location)
			end)
		end
	end)
	Trigger.OnEnteredProximityTrigger(TrukWaypoint4.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Type == "dog" then
			Trigger.RemoveProximityTrigger(id)
			Media.PlaySoundNotification(Greece, DogBark)
			Utils.Do(CheckpointRifles, function(guard)
				guard.Move(TrukInspect.Location)
			end)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Utils.Do(TrukPath2, function(waypoint)
					Truk.Move(waypoint.Location)
				end)
			end)
		end
	end)
	Trigger.OnEnteredFootprint({ SpyJumpOut.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)

			Spy = Actor.Create("spy", true, { Owner = Greece, Location = SpyJumpOut.Location })
			Spy.DisguiseAsType("e1", USSR)
			Spy.Move(TruckWaypoint5.Location)
			Spy.Infiltrate(Prison)
			Media.PlaySoundNotification(Greece, SpyVoice)

			FollowTruk = false

			if SpecialCameras then
				PrisonCamera = Actor.Create("camera", true, { Owner = Greece, Location = SpyJumpOut.Location })
			else
				PrisonCamera = Actor.Create("camera.small", true, { Owner = Greece, Location = Prison.Location + CVec.New(1, 1) })
			end

			Trigger.OnKilled(Spy, function() USSR.MarkCompletedObjective(USSRObj) end)
		end
	end)
	Trigger.OnEnteredFootprint({ TruckCrash.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)
			Truk.Stop()
			Truk.Kill()
			CrashTower.Kill()
			CrashBarrel.Kill()
		end
	end)
end

MissInfiltrated = function()
	for i = 0, 5, 1 do
		local sound = Utils.Random(TanyaVoices)
		Trigger.AfterDelay(DateTime.Seconds(i), function()
			Media.PlaySoundNotification(Greece, sound)
		end)
	end
	Prison.Attack(Prison)

	Trigger.AfterDelay(DateTime.Seconds(6), FreeTanya)
end

FreeTanya = function()
	Prison.Stop()
	Tanya = Actor.Create(TanyaType, true, { Owner = Greece, Location = Prison.Location + CVec.New(1, 1) })
	Tanya.Demolish(Prison)
	Tanya.Move(Tanya.Location + CVec.New(Utils.RandomInteger(-1, 2), 1))

	GroupPatrol(BridgePatrol, BridgePatrolPath, DateTime.Seconds(7))

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end

	local escapeResponse1 = Reinforcements.Reinforce(USSR, { "e1", "e2", "e2" }, { RaxSpawn.Location, TrukWaypoint4.Location })
	Utils.Do(escapeResponse1, function(units)
		IdleHunt(units)
	end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		local escapeResponse2 = Reinforcements.Reinforce(USSR, { "e1", "e2", "e2" }, { RaxSpawn.Location, TrukWaypoint4.Location })
		Utils.Do(escapeResponse2, function(units)
			IdleHunt(units)
		end)
	end)

	KillSams = Greece.AddObjective("Destroy all four SAM sites that block\nthe extraction helicopter.")
	Trigger.OnKilled(Tanya, function() USSR.MarkCompletedObjective(USSRObj) end)

	if not SpecialCameras and PrisonCamera and PrisonCamera.IsInWorld then
		PrisonCamera.Destroy()
	end
end

SendReinforcements = function()
	GreeceReinforcementsArrived = true
	Camera.Position = SpyLoadout.CenterPosition
	Greece.Cash = Greece.Cash + ReinforceCash

	Utils.Do(GreeceReinforcements, function(reinforcements)
		Reinforcements.ReinforceWithTransport(Greece, InsertionTransport, reinforcements.types, reinforcements.entry, { SpyEntry.Location })
	end)

	Media.PlaySpeechNotification(Greece, "AlliedReinforcementsArrived")

	ActivateAI()
end

ExtractUnits = function(extractionUnit, pos, after)
	if extractionUnit.IsDead or not extractionUnit.HasPassengers then
		return
	end

	extractionUnit.Move(pos)
	extractionUnit.Destroy()

	Trigger.OnRemovedFromWorld(extractionUnit, after)
end

InitTriggers = function()
	Trigger.OnInfiltrated(Warfactory, function()
		if Greece.IsObjectiveCompleted(InfWarfactory) then
			return
		elseif Truk.IsDead then
			if not Greece.IsObjectiveCompleted(MainObj) then
				USSR.MarkCompletedObjective(USSRObj)
			end

			return
		end

		Trigger.ClearAll(Spy)
		Greece.MarkCompletedObjective(InfWarfactory)
		WarfactoryInfiltrated()
	end)

	Trigger.OnKilled(Truk, function()
		if not Greece.IsObjectiveCompleted(InfWarfactory) then
			Greece.MarkFailedObjective(InfWarfactory)
		elseif FollowTruk then
			USSR.MarkCompletedObjective(USSRObj)
		end
	end)

	Trigger.OnInfiltrated(Prison, function()
		if Greece.IsObjectiveCompleted(MainObj) then
			return
		end

		if not Greece.IsObjectiveCompleted(InfWarfactory) then
			Media.DisplayMessage("Good work! But next time skip the heroics!", "Battlefield Control")
			Greece.MarkCompletedObjective(InfWarfactory)
		end

		if not PrisonCamera then
			if SpecialCameras then
				PrisonCamera = Actor.Create("camera", true, { Owner = Greece, Location = SpyJumpOut.Location })
			else
				PrisonCamera = Actor.Create("camera.small", true, { Owner = Greece, Location = Prison.Location + CVec.New(1, 1) })
			end
		end

		if SpecialCameras and SpyCameraA and not SpyCameraA.IsDead then
			SpyCameraA.Destroy()
			SpyCameraB.Destroy()
			SpyCameraC.Destroy()
		end

		Trigger.ClearAll(Spy)
		Trigger.AfterDelay(DateTime.Seconds(2), MissInfiltrated)
	end)

	Trigger.OnAllKilled(SamSites, function()
		Greece.MarkCompletedObjective(KillSams)

		local flare = Actor.Create("flare", true, { Owner = Greece, Location = ExtractionPath[2] + CVec.New(0, -1) })
		Trigger.AfterDelay(DateTime.Seconds(7), flare.Destroy)
		Media.PlaySpeechNotification(Greece, "SignalFlare")

		ExtractionHeli = Reinforcements.ReinforceWithTransport(Greece, ExtractionHeliType, nil, ExtractionPath)[1]
		local exitPos = CPos.New(ExtractionPath[1].X, ExtractionPath[2].Y)

		Trigger.OnKilled(ExtractionHeli, function() USSR.MarkCompletedObjective(USSRObj) end)
		Trigger.OnRemovedFromWorld(Tanya, function()
			ExtractUnits(ExtractionHeli, exitPos, function()
				Media.PlaySpeechNotification(Greece, "TanyaRescued")
				Greece.MarkCompletedObjective(MainObj)
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					SendReinforcements()
				end)

				if PrisonCamera and PrisonCamera.IsInWorld then
					PrisonCamera.Destroy()
				end
			end)
		end)
	end)
end

Tick = function()
	if FollowTruk and not Truk.IsDead then
		Camera.Position = Truk.CenterPosition
	end

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(KillAll)
	end

	if GreeceReinforcementsArrived and Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRObj)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	GoodGuy = Player.GetPlayer("GoodGuy")

	InitObjectives(Greece)

	USSRObj = USSR.AddObjective("Deny the Allies.")
	MainObj = Greece.AddObjective("Rescue Tanya.")
	KillAll = Greece.AddObjective("Eliminate all Soviet units in this area.")
	InfWarfactory = Greece.AddObjective("Infiltrate the Soviet warfactory.", "Secondary", false)

	InitTriggers()
	SendSpy()
	ChurchFootprint()

	if Difficulty == "easy" then
		TanyaType = "e7"
		ReinforceCash = 5000
		USSR.Cash = 8000
		SpecialCameras = true
	elseif Difficulty == "normal" then
		TanyaType = "e7.noautotarget"
		ReinforceCash = 2250
		USSR.Cash = 15000
		SpecialCameras = true
	else
		TanyaType = "e7.noautotarget"
		ReinforceCash = 1500
		USSR.Cash = 25000
	end

	Trigger.AfterDelay(DateTime.Seconds(3), ActivatePatrols)
end
