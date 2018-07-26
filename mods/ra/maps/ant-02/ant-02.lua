SouthNotDiscovered = true
NorthNotDiscovered = true
MCVMovedPast = false
BridgesLeft = 3
ShouldLaunchAttack = true
NVillageWPs = {waypoint29, NEBridge, waypoint30}
SVillageWPs = {waypoint37, waypoint8, waypoint33, waypoint21} 
NVillageBlds = {Actor192, Actor193, Actor188, Actor185, Actor187, Actor191, Actor189, Actor190}
SVillageBlds = {Actor204, Actor197, Actor195, Actor201, Actor198, Actor199, Actor200, Actor202, Actor196, Actor203}
ticks = DateTime.Minutes(0)

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

InitTriggers = function()
	Utils.Do(Player.GetPlayer("Neutral").GetActors(), function(actor)	
		local at = actor.Type
		if (at == "bridge1" and actor.CenterPosition == NEBridge.CenterPosition) then
			Trigger.OnKilled(actor, function(actor, destroyer)
				if BridgesLeft > 0 then
					BridgesLeft = BridgesLeft - 1
				else
					Allies.MarkCompletedObjective(DestroyBridgesObj)
				end
			end)
		elseif (at == "bridge2" and (actor.CenterPosition == SEBridge.CenterPosition or actor.CenterPosition == WBridge.CenterPosition)) then
			Trigger.OnKilled(actor, function(actor, destroyer)
				if BridgesLeft > 0 then
					BridgesLeft = BridgesLeft - 1
				else
					Allies.MarkCompletedObjective(DestroyBridgesObj)
				end
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
	
	Utils.Do({Actor211,Actor212,Actor213}, function(actor)
		Trigger.OnKilled(actor, function()
			local count = #Turkey.GetActorsByType("truk")
			if (count == 0) then
				Allies.MarkCompletedObjective(GasColonyObj)
				ShouldLaunchAttack = false
				SendChemTroops()
			end
		end)
	end)
	
	Utils.Do(NVillageWPs, function(nactor)
		Trigger.OnEnteredProximityTrigger(nactor.CenterPosition, WDist.FromCells(8), function(discoverer, nid)
			if (NorthNotDiscovered and discoverer.Owner == Allies) then		
				RescueNorth =  Allies.AddPrimaryObjective("Protect and extract villagers from Northern village!")
				SpawnNorthVillagers()
				StartNorthAttack()
				Trigger.AfterDelay(DateTime.Seconds(1), function() Allies.MarkCompletedObjective(ReachNorthObj) end)
				NorthNotDiscovered = false
			end	
			
			if not NorthNotDiscovered then 
				Trigger.RemoveProximityTrigger(nid)
			end
		end)
	end)
	
	Utils.Do(SVillageWPs, function(sactor)
		Trigger.OnEnteredProximityTrigger(sactor.CenterPosition, WDist.FromCells(8), function(discoverer2, sid)
			if (SouthNotDiscovered and discoverer2.Owner == Allies) then
				RescueSouth =  Allies.AddPrimaryObjective("Protect and extract villagers from Southern village!")
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Allies.MarkCompletedObjective(ReachSouthObj) 
					SpawnSouthVillagers()
					StartSouthAttack()
				end)
				SouthNotDiscovered = false
			end
			if not SouthNotDiscovered then 
				Trigger.RemoveProximityTrigger(sid)
			end
		end)
	end)
	
	MCV1Trigger = Trigger.OnEnteredProximityTrigger(waypoint40.CenterPosition,WDist.FromCells(5), function(actor, id)
		if actor.Type == "mcv" then
			Trigger.RemoveProximityTrigger(MCV1Trigger)
			Trigger.RemoveProximityTrigger(MCV2Trigger)
			MCVMovedPast = true
			SendChemTroops()
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
	LivingNorthernVillagers = 15
	NorthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, {Actor192.Location, waypoint29.Location}, DateTime.Seconds(1),function(actor)
		Trigger.OnKilled(actor, function() LivingNorthernVillagers = LivingNorthernVillagers - 1 end)
		if (LivingNorthernVillagers == 0) then 
			Allies.MarkFailedObjective(RescueNorth)
		end
	end)
	
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
	LivingSouthernVillagers = 2
	SouthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, {Actor204.Location, waypoint38.Location}, DateTime.Seconds(1),function(actor)
		Trigger.OnKilled(actor, function() LivingSouthernVillagers = LivingSouthernVillagers - 1 end)
		if (LivingSouthernVillagers == 0) then 
			Allies.MarkFailedObjective(RescueSouth)
		end
	end)

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
	if not Allies.IsObjectiveCompleted(DestroyBridgesObj) then
		local AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
		NorthernStrikeForce = StartAttack(AntList, {waypoint0.Location, waypoint12.Location, NEBridge.Location}, Actor192.Location, BadGuy)
	end
end

StartSouthAttack = function()
	if (GasColonyObj == nil) then
		AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
		SouthernStrikeForce = StartAttack(AntList, {waypoint2.Location, waypoint8.Location}, Actor204.Location, BadGuy)
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

		StartAttack({"ant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, BadGuy)
		StartAttack({"scoutant","scoutant","scoutant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, Ukraine)

	elseif (colony == 1 and not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then
		StartAttack({"scoutant","scoutant","scoutant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, Ukraine)
		StartAttack({"fireant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, BadGuy)
	elseif (colony == 2) then
		if ShouldLaunchAttack then
			StartAttack({"scoutant"},{waypoint2.Location, waypoint20.Location}, waypoint20.Location, Ukraine)
			StartAttack({"fireant","ant","ant"},{waypoint2.Location,waypoint95.Location}, waypoint20.Location, BadGuy)
		end
	end
end

SendChemTroops = function()
	local MovePath = {waypoint36.Location, CPos.New(65,65)}
	local Forces = {"extrm","extrm","extrm","extrm","extrm"}
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(Allies, "tran", Forces, MovePath, {CPos.New(65,65), waypoint36.Location})
end

Tick = function() 
	if (DateTime.GameTime > DateTime.Minutes(3) or MCVMovedPast) then
		ticks = ticks + 1
		if (ticks == DateTime.Minutes(2)) then
			SendAntAttack(DateTime.GameTime % 3)
			ticks = DateTime.Minutes(1)
		end
	end
end
