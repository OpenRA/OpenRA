SouthNotDiscovered = true
NorthNotDiscovered = true
MCVMovedPast = false
BridgesLeft = 3
ShouldLaunchAttack = true
NVillageWPs = {waypoint29, NEBridge, waypoint30}
SVillageWPs = {waypoint37, waypoint8, waypoint33, waypoint21} 
NVillageBlds = {Actor192, Actor193, Actor188, Actor185, Actor187, Actor191, Actor189, Actor190}
SVillageBlds = {Actor204, Actor197, Actor195, Actor201, Actor198, Actor199, Actor200, Actor202, Actor196, Actor203}
ticks = DateTime.Minutes(1)
AttackCounter = 0
VillagerList = {"c1","c2","c3","c4","c5","c6","c7","c8","c9","c10","c1","c2","e1r1","e1r1","gnrl"}

WorldLoaded = function()
	Allies = Player.GetPlayer("Spain")
	Germany = Player.GetPlayer("Germany")
	GoodGuy = Player.GetPlayer("GoodGuy")
	Turkey = Player.GetPlayer("Turkey")
	Ukraine = Player.GetPlayer("Ukraine")
	BadGuy = Player.GetPlayer("BadGuy")
	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives()
	InitTriggers()
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
		Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Allies, "MissionAccomplished")  end)
	end)
	
	ReachSouthObj = Allies.AddPrimaryObjective("Send forces to secure Southern village.")
	ReachNorthObj = Allies.AddPrimaryObjective("Find and secure Northern village.")
	DestroyBridgesObj = Allies.AddPrimaryObjective("Destroy Bridges to reduce ant attacks.")
	FindColonyObj = Allies.AddSecondaryObjective("Find second ant colony.")
end

BridgeDestroyed = function()
	BridgesLeft =- 1
	Media.PlaySpeechNotification(Allies,"ObjectiveReached")
end

InitTriggers = function()
	Trigger.OnKilled(Actor192, function()
		if (not Allies.IsObjectiveCompleted(ReachNorthObj)) then
			Allies.MarkFailedObjective(ReachNorthObj)
		end
	end)

	Trigger.OnKilled(Actor204, function() 
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
	
	Trigger.OnAllKilled({Actor211,Actor212,Actor213}, function()
		Allies.MarkCompletedObjective(GasColonyObj)
		ShouldLaunchAttack = false
		SendChemTroops()
	end)
	
	Utils.Do(NVillageWPs, function(actor)
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(8), function(discoverer, id)
			if (NorthNotDiscovered and discoverer.Owner == Allies) then
				RescueNorth =  Allies.AddPrimaryObjective("Protect and extract villagers from Northern village!")
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Allies.MarkCompletedObjective(ReachNorthObj) 
					SpawnNorthVillagers()
					StartNorthAttack()
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
					StartSouthAttack()
				end)
				SouthNotDiscovered = false
			end
			if not SouthNotDiscovered then 
				Trigger.RemoveProximityTrigger(id)
			end
		end)
	end)
	
	MCV1Trigger = Trigger.OnEnteredProximityTrigger(waypoint40.CenterPosition,WDist.FromCells(5), function(actor, id)
		if actor.Type == "mcv" then
			Trigger.RemoveProximityTrigger(MCV1Trigger)
			Trigger.RemoveProximityTrigger(MCV2Trigger)
			MCVMovedPast = true
			SendSniperTeam()
		end
	end)

	MCV2Trigger = Trigger.OnEnteredProximityTrigger(waypoint37.CenterPosition,WDist.FromCells(5), function(actor, id)
		if actor.Type == "mcv" then
			Trigger.RemoveProximityTrigger(MCV1Trigger)
			Trigger.RemoveProximityTrigger(MCV2Trigger)
			MCVMovedPast = true
		end
	end)
	
end

SpawnNorthVillagers = function()
	NorthernVillagers = Reinforcements.Reinforce(Allies, VillagerList, {Actor192.Location, waypoint29.Location}, DateTime.Seconds(1))
	Trigger.OnAllKilled(NorthernVillagers, function() Allies.MarkFailedObjective(RescueNorth) end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(8), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(NorthernVillagers, function(villager)
				if actor == villager then
					Trigger.RemoveProximityTrigger(id)
					Allies.MarkCompletedObjective(RescueNorth)
				end
			end)
		end
	end)
end

SpawnSouthVillagers = function()
	SouthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, {Actor204.Location, waypoint38.Location}, DateTime.Seconds(1))
	Trigger.OnAllKilled(SouthernVillagers, function() Allies.MarkFailedObjective(RescueSouth) end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(8), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(SouthernVillagers, function(villager)
				if actor == villager then
					Trigger.RemoveProximityTrigger(id)
					Allies.MarkCompletedObjective(RescueSouth)
				end
			end)
		end
	end)
end

StartNorthAttack = function()
	if (not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then
		local AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
		NorthernStrikeForce = StartAttack(AntList, {waypoint0.Location, waypoint12.Location}, Actor192.Location, Ukraine)
	end
end

StartSouthAttack = function()
	if (GasColonyObj == nil) then
		local AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
		SouthernStrikeForce = StartAttack(AntList, {waypoint2.Location, waypoint8.Location}, Actor204.Location, Ukraine)
	end
end

StartAttack = function(ActorList, MoveRoute, AttackPoint, Faction)

	return Reinforcements.Reinforce(Faction,ActorList, MoveRoute, DateTime.Seconds(3), function(actor)
		actor.AttackMove(AttackPoint)
		actor.Hunt()
	end)
	
end

SendAntAttack = function(colony)
	if (colony == 0 and not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then

		StartAttack({"ant","scoutant","scoutant","scoutant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, BadGuy)

	elseif (colony == 1 and not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then
		StartAttack({"fireant","scoutant","scoutant","scoutant"},{waypoint1.Location,waypoint11.Location}, waypoint35.Location, BadGuy)
	elseif (ShouldLaunchAttack) then
		StartAttack({"scoutant","fireant","ant","ant"},{waypoint2.Location,waypoint95.Location}, waypoint20.Location, BadGuy)
	end
end

SendChemTroops = function()
	local MovePath = {waypoint36.Location, CPos.New(65,65)}
	local Forces = {"extrm","extrm","extrm","extrm","extrm"}
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(Allies, "tran", Forces, MovePath, {CPos.New(65,65), waypoint36.Location})
end

SendChemTroops = function()
	local MovePath = {waypoint36.Location, CPos.New(65,65)}
	local Forces = {"sniper","sniper"}
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(Allies, "tran", Forces, MovePath, {CPos.New(65,65), waypoint36.Location})
end


SetIdleToHunt = function()
	Utils.Do(BadGuy.GetActors(), function(actor) 
		if (actor.Type == "ant" or actor.Type == "fireant" or actor.Type == "scoutant") then
			if actor.IsIdle then
				actor.Hunt()
			end
		end
	end)
	
	Utils.Do(Ukraine.GetActors(), function(actor) 
		if (actor.Type == "ant" or actor.Type == "fireant" or actor.Type == "scoutant") then
			if actor.IsIdle then
				actor.Hunt()
			end
		end
	end)
end

Tick = function() 
	if (DateTime.GameTime > DateTime.Seconds(1)) then
		if (not Allies.IsObjectiveCompleted(DestroyBridgesObj) and BridgesLeft < 1) then Allies.MarkCompletedObjective(DestroyBridgesObj) end
	end
	if (DateTime.GameTime > DateTime.Minutes(3) or MCVMovedPast) then
		ticks = ticks + 1
		if (ticks == DateTime.Minutes(2)) then
			SetIdleToHunt()
			
			AttackCounter = AttackCounter + 1
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				SendAntAttack(AttackCounter % 3)
			end)
			ticks = DateTime.Minutes(1)
		end
	end
end
