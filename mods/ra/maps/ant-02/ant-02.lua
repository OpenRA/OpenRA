DifficultySetting = Map.LobbyOption("difficulty")
SouthNotDiscovered = true
NorthNotDiscovered = true
MCVMovedPast = false
BridgesLeft = 3
ShouldLaunchAttack = true
NVillageWPs = { waypoint29, NEBridge, waypoint30 }
SVillageWPs = { waypoint37, waypoint8, waypoint33, waypoint21 }
NVillageBlds = { Actor192, Actor193, Actor188, Actor185, Actor187, Actor191, Actor189, Actor190 }
SVillageBlds = { Church, Actor197, Actor195, Actor201, Actor198, Actor199, Actor200, Actor202, Actor196, Actor203 }
ticks = DateTime.Minutes(1)
AttackCounter = 0
VillagerList = { "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "c10", "c1", "c2", "e1r1", "e1r1", "gnrl" }
EventTime = DateTime.Minutes(1)

BridgeDestroyed = function()
	BridgesLeft =- 1
end

SpawnNorthVillagers = function()
	NorthernVillagers = Reinforcements.Reinforce(Allies, VillagerList, { Actor192.Location, waypoint29.Location }, DateTime.Seconds(1))
	Trigger.OnAllKilled(NorthernVillagers, function() 
		if  not Allies.IsObjectiveCompleted(RescueNorth) then
			Allies.MarkFailedObjective(RescueNorth)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(12), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(NorthernVillagers, function(villager)
				if actor == villager then
					if (not Allies.IsObjectiveCompleted(RescueNorth)) then
						Allies.MarkCompletedObjective(RescueNorth)
					end
					actor.Owner = Special
					actor.Move(CPos.New(46, 10))
					if actor.Type == "gnrl" then Media.DisplayMessage("Christ, did you see the size of those things? Good luck Commander!", "Field Marshal", HSLColor.DarkGreen) end
					Trigger.OnIdle(actor, actor.Destroy)
				end
			end)
		end
	end)
end

SpawnSouthVillagers = function()
	SouthernVillagers = Reinforcements.Reinforce(Allies, VillagerList, { Church.Location, waypoint38.Location }, DateTime.Seconds(1))
	Trigger.OnAllKilled(SouthernVillagers, function()		
		if not Allies.IsObjectiveCompleted(RescueSouth) then
			Allies.MarkFailedObjective(RescueSouth)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(12), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(SouthernVillagers, function(villager)
				if actor == villager then
					if (not Allies.IsObjectiveCompleted(RescueSouth)) then
						Allies.MarkCompletedObjective(RescueSouth)
					end
					if actor.Type == "gnrl" then Media.DisplayMessage("Thank you for rescuing us Commander.", "Commandant", HSLColor.LightBlue) end
					actor.Owner = GoodGuy
					actor.Move(CPos.New(46, 10))
					Trigger.OnIdle(actor, actor.Destroy)
				end
			end)
		end
	end)
end

Tick = function() 
	if (DateTime.GameTime > DateTime.Seconds(1)) then
		if (not Allies.IsObjectiveCompleted(DestroyBridgesObj) and BridgesLeft < 1) then
			Allies.MarkCompletedObjective(DestroyBridgesObj) 
			StopIslandAttacks()
		end
	end

	if (DateTime.GameTime > DateTime.Minutes(2) or MCVMovedPast) then
		ticks = ticks + 1
		if (ticks > EventTime) then
			AttackCounter = AttackCounter + 1
			EventTime = DateTime.Minutes(1) + (DateTime.Seconds(15) * Utils.RandomInteger(0,3))
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				StartAttack()
			end)
			ticks = DateTime.Minutes(1)
		end
	end
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(Allies, function()
		Media.PlaySpeechNotification(Allies, "MissionFailed")
	end)

	Trigger.OnPlayerWon(Allies, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Allies, "MissionAccomplished")
		end)
	end)

	ReachSouthObj = Allies.AddPrimaryObjective("Send forces to secure Southern village.")
	ReachNorthObj = Allies.AddPrimaryObjective("Find and secure Northern village.")
	DestroyBridgesObj = Allies.AddPrimaryObjective("Destroy Bridges to reduce ant attacks.")
	FindColonyObj = Allies.AddSecondaryObjective("Find second ant colony.")
end

InitTriggers = function()
	Trigger.OnKilled(Actor192, function()
		if (not Allies.IsObjectiveCompleted(ReachNorthObj)) then
			Allies.MarkFailedObjective(ReachNorthObj)
		end
	end)

	Trigger.OnKilled(Church, function() 
		if (not Allies.IsObjectiveCompleted(ReachSouthObj)) then
			Allies.MarkFailedObjective(ReachSouthObj)
		end
	end)

	Utils.Do(Player.GetPlayer("Neutral").GetActorsByType("bridge1"), function(actor)	
		local at = actor.Type
		if ((at == "bridge1" and actor.CenterPosition == NEBridge.CenterPosition) or (at == "bridge2")) then
			Trigger.OnKilled(actor, function(actor, destroyer)
				BridgeDestroyed()
			end)
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(waypoint95.CenterPosition, WDist.FromCells(5), function(exterminator, id) 
		if (exterminator.Owner == Allies) then
			Allies.MarkCompletedObjective(FindColonyObj)
			GasColonyObj = Allies.AddSecondaryObjective("Destroy Southern ant colony!")
			Trigger.RemoveProximityTrigger(id)
		end
	end)
	
	Trigger.OnAllKilled( { SupplyTruck1, SupplyTruck2, SupplyTruck3 }, function()
		ShouldLaunchAttack = false
		Allies.MarkCompletedObjective(GasColonyObj)
		StopSouthernAttacks()
		PoisonGas = Actor.Create("flare", true, { Owner = Special, Location = waypoint2.Location })
		Trigger.OnEnteredProximityTrigger(waypoint2.CenterPosition, WDist.FromCells(3), function(actor, id)
			if (actor.Type == "ant" or actor.Type == "fireant" or actor.Type == "scoutant") then
				Trigger.AfterDelay(DateTime.Seconds(1), function() 
					actor.Kill()
				end)
			end
		end)
			
	end)
	
	Utils.Do(NVillageWPs, function(actor)
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(8), function(discoverer, id)
			if (NorthNotDiscovered and discoverer.Owner == Allies) then
				RescueNorth =  Allies.AddPrimaryObjective("Protect and extract villagers from Northern village!")
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Allies.MarkCompletedObjective(ReachNorthObj) 
					SpawnNorthVillagers()
					NorthernStrikeForce = StartNorthAttack()
				end)
				NorthNotDiscovered = false
			end
			if not NorthNotDiscovered then 
		
				Trigger.RemoveProximityTrigger(id)
			end
		end)
	end)
	
	Utils.Do(SVillageWPs, function(actor)
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(8), function(discoverer, id)
			if (SouthNotDiscovered and discoverer.Owner == Allies) then
				RescueSouth =  Allies.AddPrimaryObjective("Protect and extract villagers from Southern village!")
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Allies.MarkCompletedObjective(ReachSouthObj) 
					SpawnSouthVillagers()
					Trigger.AfterDelay(100, function() SouthernStrikeForce = StartSouthAttack() end)
				end)
				SouthNotDiscovered = false
			end

			if not SouthNotDiscovered then 
				Trigger.RemoveProximityTrigger(id)
			end
		end)
	end)
	
	MCV1Trigger = Trigger.OnEnteredProximityTrigger(waypoint40.CenterPosition, WDist.FromCells(5), function(actor, id)
		if actor.Type == "mcv" then
			Trigger.RemoveProximityTrigger(MCV1Trigger)
			Trigger.RemoveProximityTrigger(MCV2Trigger)
			MCVMovedPast = true
		end
	end)

	MCV2Trigger = Trigger.OnEnteredProximityTrigger(waypoint37.CenterPosition, WDist.FromCells(5), function(actor, id)
		if actor.Type == "mcv" then
			Trigger.RemoveProximityTrigger(MCV1Trigger)
			Trigger.RemoveProximityTrigger(MCV2Trigger)
			MCVMovedPast = true
		end
	end)
	
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Spain")
	GoodGuy = Player.GetPlayer("GoodGuy")
	Special = Player.GetPlayer("Special")
	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives()
	InitTriggers()
	InitAntPlayers()
end
