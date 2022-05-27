--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
if Difficulty == "easy" then
	TanyaType = "e7"
	ReinforceCash = 5000
	HoldAITime = DateTime.Minutes(3)
	SpecialCameras = true
elseif Difficulty == "normal" then
	TanyaType = "e7.noautotarget"
	ReinforceCash = 3500
	HoldAITime = DateTime.Minutes(2)
	SpecialCameras = true
else
	TanyaType = "e7.noautotarget"
	ReinforceCash = 2250
	HoldAITime = DateTime.Minutes(1) + DateTime.Seconds(30)
end

SpyType = { "spy" }
SpyEntryPath = { SpyEntry.Location, SpyLoadout.Location }
InsertionTransport = "lst.in"

TrukPath = { TrukWaypoint1, TrukWaypoint2, TrukWaypoint3, TrukWaypoint4, TrukWaypoint5, TrukWaypoint6, TrukWaypoint7, TrukWaypoint8, TrukWaypoint9, TrukWaypoint10 }

ExtractionHeliType = "tran"
ExtractionPath = { ExtractionEntry.Location, ExtractionLZ.Location }
HeliReinforcements = { "medi", "mech", "mech" }

GreeceReinforcements1 =
{
	{ types = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk" }, entry = { SpyEntry.Location, LSTLanding1.Location } },
	{ types = { "e3", "e3", "e3", "e3", "e1" }, entry = { LSTEntry2.Location, LSTLanding2.Location } }
}

GreeceReinforcements2 =
{
	{ types = { "arty", "arty", "jeep", "jeep" }, entry = { SpyEntry.Location, LSTLanding1.Location } },
	{ types = { "e1", "e1", "e6", "e6", "e6" }, entry = { LSTEntry2.Location, LSTLanding2.Location } }
}

DogPatrol = { Dog1, Dog2 }
RiflePatrol = { RiflePatrol1, RiflePatrol2, RiflePatrol3, RiflePatrol4, RiflePatrol5 }
BasePatrol = { BasePatrol1, BasePatrol2, BasePatrol3 }

DogPatrolPath = { DogPatrolRally1.Location, SpyCamera2.Location, DogPatrolRally3.Location }
RiflePath = { RiflePath1.Location, RiflePath2.Location, RiflePath3.Location }
BasePatrolPath = { BasePatrolPath1.Location, BasePatrolPath2.Location, BasePatrolPath3.Location }

TanyaVoices = { "tuffguy", "bombit", "laugh", "gotit", "lefty", "keepem" }
SpyVoice = "sking"

SamSites = { Sam1, Sam2, Sam3, Sam4, Sam5, Sam6 }

SendSpy = function()
	Camera.Position = SpyEntry.CenterPosition
	Spy = Reinforcements.ReinforceWithTransport(Greece, InsertionTransport, SpyType, SpyEntryPath, { SpyEntryPath[1] })[2][1]

	Trigger.OnKilled(Spy, function() USSR.MarkCompletedObjective(USSRObj) end)

	if SpecialCameras then
		SpyCameraA = Actor.Create("camera", true, { Owner = Greece, Location = SpyCamera1.Location })
		SpyCameraB = Actor.Create("camera", true, { Owner = Greece, Location = SpyCamera2.Location })
		SpyCameraC = Actor.Create("camera", true, { Owner = Greece, Location = SpyCamera3.Location })
	else
		SpyCameraHard = Actor.Create("camera.small", true, { Owner = Greece, Location = RiflePath1.Location + CVec.New(0, 3) })
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Commander! You have to disguise me in order to get through the enemy patrols.", "Spy")
	end)
end

ActivatePatrols = function()
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		GroupPatrol(DogPatrol, DogPatrolPath, DateTime.Seconds(6))
		GroupPatrol(RiflePatrol, RiflePath, DateTime.Seconds(7))
		GroupPatrol(BasePatrol, BasePatrolPath, DateTime.Seconds(6))
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

SendReinforcements = function()
	GreeceReinforcementsArrived = true
	Camera.Position = SpyLoadout.CenterPosition
	Greece.Cash = Greece.Cash + ReinforceCash
	Media.PlaySpeechNotification(Greece, "AlliedReinforcementsArrived")
	Utils.Do(GreeceReinforcements1, function(reinforcements)
		Reinforcements.ReinforceWithTransport(Greece, InsertionTransport, reinforcements.types, reinforcements.entry, { SpyEntry.Location })
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.PlaySpeechNotification(Greece, "AlliedReinforcementsArrived")
		Utils.Do(GreeceReinforcements2, function(reinforcements)
			Reinforcements.ReinforceWithTransport(Greece, InsertionTransport, reinforcements.types, reinforcements.entry, { SpyEntry.Location })
		end)
	end)

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

WarfactoryInfiltrated = function()
	FollowTruk = true
	Truk.GrantCondition("hijacked")

	Truk.Wait(DateTime.Seconds(1))
	Utils.Do(TrukPath, function(waypoint)
		Truk.Move(waypoint.Location)
	end)

	if SpecialCameras then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			SpyCameraA.Destroy()
			SpyCameraB.Destroy()
			SpyCameraC.Destroy()
		end)
	else
		SpyCameraHard.Destroy()
	end
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

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end

	Trigger.OnKilled(Tanya, function() USSR.MarkCompletedObjective(USSRObj) end)

	if Difficulty == "tough" then
		KillSams = Greece.AddObjective("Destroy all six SAM Sites that block\nour reinforcements' helicopter.")

		Greece.MarkCompletedObjective(MainObj)
		SurviveObj = Greece.AddObjective("Tanya must not die!")
		Media.PlaySpeechNotification(Greece, "TanyaRescued")
	else
		KillSams = Greece.AddObjective("Destroy all six SAM sites that block\nthe extraction helicopter.")

		Media.PlaySpeechNotification(Greece, "TargetFreed")
	end

	if not SpecialCameras and PrisonCamera and PrisonCamera.IsInWorld then
		PrisonCamera.Destroy()
	end
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
				PrisonCamera = Actor.Create("camera", true, { Owner = Greece, Location = SpyJump.Location })
			else
				PrisonCamera = Actor.Create("camera.small", true, { Owner = Greece, Location = Prison.Location + CVec.New(1, 1) })
			end
		end

		if SpecialCameras and SpyCameraA and not SpyCameraA.IsDead then
			SpyCameraA.Destroy()
			SpyCameraB.Destroy()
		end

		Trigger.ClearAll(Spy)
		Trigger.AfterDelay(DateTime.Seconds(2), MissInfiltrated)
	end)

	Trigger.OnEnteredFootprint({ SpyJump.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)

			Spy = Actor.Create("spy", true, { Owner = Greece, Location = SpyJump.Location })
			Spy.DisguiseAsType("e1", USSR)
			Spy.Move(SpyWaypoint.Location)
			Spy.Infiltrate(Prison)
			Media.PlaySoundNotification(Greece, SpyVoice)

			FollowTruk = false

			if SpecialCameras then
				PrisonCamera = Actor.Create("camera", true, { Owner = Greece, Location = SpyJump.Location })
			else
				PrisonCamera = Actor.Create("camera.small", true, { Owner = Greece, Location = Prison.Location + CVec.New(1, 1) })
			end

			Trigger.OnKilled(Spy, function() USSR.MarkCompletedObjective(USSRObj) end)
		end
	end)

	Trigger.OnEnteredFootprint({ TrukWaypoint10.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)
			Truk.Stop()
			Truk.Kill()
			ExplosiveBarrel.Kill()
		end
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
		if not Greece.IsObjectiveCompleted(KillAll) and Difficulty == "tough" then
			SendWaterExtraction()
		end
		Greece.MarkCompletedObjective(KillAll)
	end

	if GreeceReinforcementsArrived and Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRObj)
	end

	if USSR.Resources >= USSR.ResourceCapacity * 0.75 then
		USSR.Cash = USSR.Cash + USSR.Resources - USSR.ResourceCapacity * 0.25
		USSR.Resources = USSR.ResourceCapacity * 0.25
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	InitObjectives(Greece)

	USSRObj = USSR.AddObjective("Deny the Allies.")
	MainObj = Greece.AddObjective("Rescue Tanya.")
	KillAll = Greece.AddObjective("Eliminate all Soviet units in this area.")
	InfWarfactory = Greece.AddObjective("Infiltrate the Soviet warfactory.", "Secondary", false)

	InitTriggers()
	SendSpy()
	Trigger.AfterDelay(DateTime.Seconds(3), ActivatePatrols)
end
