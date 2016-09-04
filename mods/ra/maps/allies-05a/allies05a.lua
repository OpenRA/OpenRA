if Map.LobbyOption("difficulty") == "easy" then
	TanyaType = "e7"
	ReinforceCash = 5000
	HoldAITime = DateTime.Minutes(3)
	SpecialCameras = true
elseif Map.LobbyOption("difficulty") == "normal" then
	TanyaType = "e7.noautotarget"
	ReinforceCash = 2250
	HoldAITime = DateTime.Minutes(2)
	SpecialCameras = true
else
	TanyaType = "e7.noautotarget"
	ReinforceCash = 1500
	HoldAITime = DateTime.Minutes(1) + DateTime.Seconds(30)
	SendWaterTransports = true
end

SpyType = { "spy" }
SpyEntryPath = { SpyEntry.Location, SpyLoadout.Location }
InsertionTransport = "lst.in"
ExtractionTransport = "lst"
TrukPath = { TrukWaypoint1, TrukWaypoint2, TrukWaypoint3, TrukWaypoint4, TrukWaypoint5, TrukWaypoint6 }
ExtractionHeliType = "tran"
InsertionHeliType = "tran.in"
ExtractionPath = { ExtractionEntry.Location, ExtractionLZ.Location }
HeliReinforcements = { "medi", "mech", "mech" }

GreeceReinforcements =
{
	{ types = { "2tnk", "2tnk", "2tnk", "arty", "arty" }, entry = { SpyEntry.Location, SpyLoadout.Location } },
	{ types = { "e3", "e3", "e3", "e6", "e6" }, entry = { SpyEntry.Location, GreeceLoadout1.Location } },
	{ types = { "jeep", "jeep", "e1", "e1", "2tnk" }, entry = { SpyEntry.Location, GreeceLoadout2.Location } }
}

DogPatrol = { Dog1, Dog2 }
PatrolA = { PatrolA1, PatrolA2, PatrolA3, PatrolA4, PatrolA5 }
PatrolB = { PatrolB1, PatrolB2, PatrolB3 }

DogPatrolPath = { DogPatrolRally1.Location, DogPatrolRally2.Location, DogPatrolRally3.Location }
PatrolAPath = { PatrolRally.Location, PatrolARally1.Location, PatrolARally2.Location, PatrolARally3.Location }
PatrolBPath = { PatrolBRally1.Location, PatrolBRally2.Location, PatrolBRally3.Location, PatrolRally.Location }

TanyaVoices = { "tuffguy", "bombit", "laugh", "gotit", "lefty", "keepem" }
SpyVoice = "sking"
SamSites = { Sam1, Sam2, Sam3, Sam4 }

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

Tick = function()
	if FollowTruk then
		TrukCamera.Teleport(Truk.Location)
		Camera.Position = Truk.CenterPosition
	end

	if ussr.HasNoRequiredUnits() then
		if not greece.IsObjectiveCompleted(KillAll) and Map.LobbyOption("difficulty") == "tough" then
			SendWaterExtraction()
		end
		greece.MarkCompletedObjective(KillAll)
	end

	if GreeceReinforcementsArrived and greece.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(ussrObj)
	end

	if ussr.Resources >= ussr.ResourceCapacity * 0.75 then
		ussr.Cash = ussr.Cash + ussr.Resources - ussr.ResourceCapacity * 0.25
		ussr.Resources = ussr.ResourceCapacity * 0.25
	end
end

SendReinforcements = function()
	GreeceReinforcementsArrived = true
	Camera.Position = ReinforceCamera.CenterPosition
	greece.Cash = greece.Cash + ReinforceCash

	Utils.Do(GreeceReinforcements, function(reinforcements)
		Reinforcements.ReinforceWithTransport(greece, InsertionTransport, reinforcements.types, reinforcements.entry, { SpyEntry.Location })
	end)

	Media.PlaySpeechNotification(greece, "AlliedReinforcementsArrived")

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

SendWaterExtraction = function()
	local flare = Actor.Create("flare", true, { Owner = greece, Location = SpyEntryPath[2] + CVec.New(2, 0) })
	Trigger.AfterDelay(DateTime.Seconds(5), flare.Destroy)
	Media.PlaySpeechNotification(greece, "SignalFlareNorth")
	Camera.Position = flare.CenterPosition

	WaterExtractionTran = Reinforcements.ReinforceWithTransport(greece, ExtractionTransport, nil, SpyEntryPath)[1]
	ExtractObj = greece.AddPrimaryObjective("Get all your forces into the transport.")

	Trigger.OnKilled(WaterExtractionTran, function() ussr.MarkCompletedObjective(ussrObj) end)
	Trigger.OnAllRemovedFromWorld(greece.GetGroundAttackers(), function()
		ExtractUnits(WaterExtractionTran, SpyEntryPath[1], function()
			greece.MarkCompletedObjective(ExtractObj)
			greece.MarkCompletedObjective(surviveObj)
		end)
	end)
end

WarfactoryInfiltrated = function()
	FollowTruk = true
	TrukCamera = Actor.Create("camera.truk", true, { Owner = greece, Location = Truk.Location })

	Truk.Wait(DateTime.Seconds(1))
	Utils.Do(TrukPath, function(waypoint)
		Truk.Move(waypoint.Location)
	end)

	if SpecialCameras then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			SpyCameraA.Destroy()
			SpyCameraB.Destroy()
		end)
	end
end

MissInfiltrated = function()
	for i = 0, 5, 1 do
		local sound = Utils.Random(TanyaVoices)
		Trigger.AfterDelay(DateTime.Seconds(i), function()
			Media.PlaySoundNotification(greece, sound)
		end)
	end
	TanyasColt = Actor.Create("Colt", true, { Owner = greece, Location = Prison.Location + CVec.New(1, 6) })

	Trigger.AfterDelay(DateTime.Seconds(6), FreeTanya)
end

FreeTanya = function()
	TanyasColt.Destroy()
	Tanya = Actor.Create(TanyaType, true, { Owner = greece, Location = Prison.Location + CVec.New(1, 1) })
	Tanya.Demolish(Prison)
	Tanya.Move(Tanya.Location + CVec.New(Utils.RandomInteger(-1, 2), 1))

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end

	Trigger.OnKilled(Tanya, function() ussr.MarkCompletedObjective(ussrObj) end)

	if Map.LobbyOption("difficulty") == "tough" then
		KillSams = greece.AddPrimaryObjective("Destroy all four SAM Sites that block\nour reinforcements' helicopter.")

		greece.MarkCompletedObjective(mainObj)
		surviveObj = greece.AddPrimaryObjective("Tanya must not die!")
		Media.PlaySpeechNotification(greece, "TanyaRescued")
	else
		KillSams = greece.AddPrimaryObjective("Destroy all four SAM sites that block\nthe extraction helicopter.")

		Media.PlaySpeechNotification(greece, "TargetFreed")
	end

	if not SpecialCameras then
		PrisonCamera.Destroy()
	end
end

SendSpy = function()
	Camera.Position = SpyEntry.CenterPosition
	Spy = Reinforcements.ReinforceWithTransport(greece, InsertionTransport, SpyType, SpyEntryPath, { SpyEntryPath[1] })[2][1]

	Trigger.OnKilled(Spy, function() ussr.MarkCompletedObjective(ussrObj) end)

	if SpecialCameras then
		SpyCameraA = Actor.Create("camera", true, { Owner = greece, Location = SpyCamera1.Location })
		SpyCameraB = Actor.Create("camera", true, { Owner = greece, Location = SpyCamera2.Location })
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Commander! You have to disguise me in order to get through the enemy patrols.", "Spy")
	end)
end

ActivatePatrols = function()
	GroupPatrol(DogPatrol, DogPatrolPath, DateTime.Seconds(2))

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		GroupPatrol(PatrolA, PatrolAPath, DateTime.Seconds(7))
		GroupPatrol(PatrolB, PatrolBPath, DateTime.Seconds(6))
	end)
end

InitTriggers = function()
	Trigger.OnInfiltrated(Warfactory, function()
		Trigger.ClearAll(Spy)
		greece.MarkCompletedObjective(infWarfactory)
		WarfactoryInfiltrated()
	end)

	Trigger.OnInfiltrated(Prison, function()
		Trigger.ClearAll(Spy)
		Trigger.AfterDelay(DateTime.Seconds(2), MissInfiltrated)
	end)

	Trigger.OnEnteredFootprint({ TrukWaypoint5.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)

			Spy = Actor.Create("spy", true, { Owner = greece, Location = TrukWaypoint5.Location })
			Spy.DisguiseAsType("e1", ussr)
			Spy.Move(SpyWaypoint.Location)
			Spy.Infiltrate(Prison)
			Media.PlaySoundNotification(greece, SpyVoice)

			FollowTruk = false
			TrukCamera.Destroy()

			if SpecialCameras then
				PrisonCamera = Actor.Create("camera", true, { Owner = greece, Location = TrukWaypoint5.Location })
			else
				PrisonCamera = Actor.Create("camera.truk", true, { Owner = greece, Location = Prison.Location + CVec.New(1, 1) })
			end

			Trigger.OnKilled(Spy, function() ussr.MarkCompletedObjective(ussrObj) end)
		end
	end)

	Trigger.OnEnteredFootprint({ TrukWaypoint6.Location }, function(a, id)
		if a == Truk then
			Trigger.RemoveFootprintTrigger(id)
			Truk.Stop()
			Truk.Kill()
			ExplosiveBarrel.Kill()
		end
	end)

	if Map.LobbyOption("difficulty") ~= "tough" then
		Trigger.OnKilled(Mammoth, function()
			Trigger.AfterDelay(HoldAITime - DateTime.Seconds(45), function() HoldProduction = false end)
			Trigger.AfterDelay(HoldAITime, function() Attacking = true end)
		end)
	end

	Trigger.OnKilled(FlameBarrel, function()
		if not FlameTower.IsDead then
			FlameTower.Kill()
		end
	end)

	Trigger.OnKilled(SamBarrel, Sam1.Kill)

	Trigger.OnAllKilled(SamSites, function()
		greece.MarkCompletedObjective(KillSams)

		local flare = Actor.Create("flare", true, { Owner = greece, Location = ExtractionPath[2] + CVec.New(0, -1) })
		Trigger.AfterDelay(DateTime.Seconds(7), flare.Destroy)
		Media.PlaySpeechNotification(greece, "SignalFlare")

		if Map.LobbyOption("difficulty") == "tough" then
			Reinforcements.ReinforceWithTransport(greece, InsertionHeliType, HeliReinforcements, ExtractionPath, { ExtractionPath[1] })
			if not Harvester.IsDead then
				Harvester.FindResources()
			end

		else
			ExtractionHeli = Reinforcements.ReinforceWithTransport(greece, ExtractionHeliType, nil, ExtractionPath)[1]
			local exitPos = CPos.New(ExtractionPath[1].X, ExtractionPath[2].Y)

			Trigger.OnKilled(ExtractionHeli, function() ussr.MarkCompletedObjective(ussrObj) end)
			Trigger.OnRemovedFromWorld(Tanya, function()
				ExtractUnits(ExtractionHeli, exitPos, function()

					Media.PlaySpeechNotification(greece, "TanyaRescued")
					greece.MarkCompletedObjective(mainObj)
					SendReinforcements()

					if SpecialCameras then
						PrisonCamera.Destroy()
					end
				end)
			end)
		end
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	ussrObj = ussr.AddPrimaryObjective("Deny the Allies.")
	mainObj = greece.AddPrimaryObjective("Rescue Tanya.")
	KillAll = greece.AddPrimaryObjective("Eliminate all Soviet units in this area.")
	infWarfactory = greece.AddPrimaryObjective("Infiltrate the Soviet warfactory.")

	Trigger.OnObjectiveCompleted(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(greece, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)
	Trigger.OnPlayerWon(greece, function()
		Media.PlaySpeechNotification(player, "Win")
	end)
end

WorldLoaded = function()
	greece = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")

	InitObjectives()
	InitTriggers()
	SendSpy()

	Trigger.AfterDelay(DateTime.Seconds(3), ActivatePatrols)
end
