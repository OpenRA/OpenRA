if Map.Difficulty == "Easy" then
	TanyaType = "e7"
	ReinforceCash = 5000
	HoldAITime = DateTime.Minutes(3)
else
	TanyaType = "e7.noautotarget"
	ChangeStance = true
	ReinforceCash = 2500
	HoldAITime = DateTime.Minutes(2)
end

SpyType = { "spy" }
SpyEntryPath = { SpyEntry.Location, SpyLoadout.Location }
InsertionTransport = "lst"
TrukPath = { TrukWaypoint1, TrukWaypoint2, TrukWaypoint3, TrukWaypoint4, TrukWaypoint5, TrukWaypoint6 }
ExtractionHeliType = "tran"
ExtractionPath = { ExtractionEntry.Location, ExtractionLZ.Location }

GreeceReinforcements =
{
	{ { "2tnk", "2tnk", "2tnk", "arty", "arty" }, { SpyEntry.Location, SpyLoadout.Location } },
	{ { "e3", "e3", "e3", "e6", "e6" }, { SpyEntry.Location, GreeceLoadout1.Location } },
	{ { "jeep", "jeep", "e1", "e1", "2tnk" }, { SpyEntry.Location, GreeceLoadout2.Location } }
}

DogPatrol = { Dog1, Dog2 }
PatrolA = { PatrolA1, PatrolA2, PatrolA3, PatrolA4, PatrolA5 }
PatrolB = { PatrolB1, PatrolB2, PatrolB3 }

DogPatrolPath = { DogPatrolRally1.Location, DogPatrolRally2.Location, DogPatrolRally3.Location }
PatrolAPath = { PatrolRally.Location, PatrolARally1.Location, PatrolARally2.Location, PatrolARally3.Location }
PatrolBPath = { PatrolBRally1.Location, PatrolBRally2.Location, PatrolBRally3.Location, PatrolRally.Location }

TanyaVoices = { "tuffguy", "bombit", "laugh", "gotit", "lefty", "keepem" }
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
		greece.MarkCompletedObjective(KillAll)
	end

	if GreeceReinforcementsArrived and greece.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(ussrObj)
	end
end

SendReinforcements = function()
	GreeceReinforcementsArrived = true
	Camera.Position = ReinforceCamera.CenterPosition
	greece.Cash = greece.Cash + ReinforceCash

	Utils.Do(GreeceReinforcements, function(reinforceTable)
		Reinforcements.ReinforceWithTransport(greece, InsertionTransport, reinforceTable[1], reinforceTable[2], { SpyEntry.Location })
	end)

	Media.PlaySpeechNotification(greece, "AlliedReinforcementsArrived")

	ActivateAI()
end

ExtractTanya = function()
	if ExtractionHeli.IsDead or not ExtractionHeli.HasPassengers then
		return
	end

	ExtractionHeli.Move(CPos.New(ExtractionPath[1].X, ExtractionHeli.Location.Y))
	ExtractionHeli.Destroy()

	Trigger.OnRemovedFromWorld(ExtractionHeli, function()
		greece.MarkCompletedObjective(mainObj)
		SendReinforcements()
		PrisonCamera.Destroy()
	end)
end

WarfactoryInfiltrated = function()
	FollowTruk = true
	TrukCamera = Actor.Create("camera.truk", true, { Owner = greece, Location = Truk.Location })

	Truk.Wait(DateTime.Seconds(1))
	Utils.Do(TrukPath, function(waypoint)
		Truk.Move(waypoint.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		SpyCameraA.Destroy()
		SpyCameraB.Destroy()
	end)
end

MissInfiltrated = function()
	CloakProvider.Destroy()

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
	Prison.Kill()
	Tanya.Scatter()

	if ChangeStance then
		Tanya.Stance = "HoldFire"
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end

	Trigger.OnKilled(Tanya, function() ussr.MarkCompletedObjective(ussrObj) end)

	KillSams = greece.AddPrimaryObjective("Destroy all four SAM sites that block\nthe extraction helicopter.")
end

SendSpy = function()
	Camera.Position = SpyEntry.CenterPosition
	Spy = Reinforcements.ReinforceWithTransport(greece, InsertionTransport, SpyType, SpyEntryPath, { SpyEntryPath[1] })[2][1]

	Trigger.OnKilled(Spy, function() ussr.MarkCompletedObjective(ussrObj) end)

	SpyCameraA = Actor.Create("camera", true, { Owner = greece, Location = SpyCamera1.Location })
	SpyCameraB = Actor.Create("camera", true, { Owner = greece, Location = SpyCamera2.Location })
end

ActivatePatrols = function()
	GroupPatrol(DogPatrol, DogPatrolPath, DateTime.Seconds(2))

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		GroupPatrol(PatrolA, PatrolAPath, DateTime.Seconds(7))
		GroupPatrol(PatrolB, PatrolBPath, DateTime.Seconds(6))
	end)

	local units = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(self) return self.Owner == soviets and self.HasProperty("AutoTarget") end)
	Utils.Do(units, function(unit)
		unit.Stance = "Defend"
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

			CloakProvider = Actor.Create("CloakUpgrade", true, { Owner = greece, Location = Prison.Location })

			Spy = Actor.Create("spy", true, { Owner = greece, Location = TrukWaypoint5.Location })
			Spy.Move(SpyWaypoint.Location)
			Spy.Move(Prison.Location, 3)

			FollowTruk = false
			TrukCamera.Destroy()
			PrisonCamera = Actor.Create("camera", true, { Owner = greece, Location = TrukWaypoint5.Location })

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

	Trigger.OnKilled(Mammoth, function()
		Trigger.AfterDelay(HoldAITime - DateTime.Seconds(45), function() HoldProduction = false end)
		Trigger.AfterDelay(HoldAITime, function() Attacking = true end)
	end)

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
		ExtractionHeli = Reinforcements.ReinforceWithTransport(greece, ExtractionHeliType, nil, ExtractionPath)[1]

		Trigger.OnKilled(ExtractionHeli, function() ussr.MarkCompletedObjective(ussrObj) end)
		Trigger.OnRemovedFromWorld(Tanya, ExtractTanya)
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
