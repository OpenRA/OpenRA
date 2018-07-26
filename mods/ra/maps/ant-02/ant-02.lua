SouthNotDiscovered = true
NorthNotDiscovered = true
NVillage = {waypoint29, NEBridge, waypoint30, waypoint12}
SVillage = {waypoint37, waypoint8, waypoint35, waypoint21}

VillagerList = {"c1","c2","c3","c4","c5","c6","c7","c8","c9","c10","c1","c2","e1","e1","e1r1"}

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
	
	ReachSouthObj = Allies.AddPrimaryObjective("Find Southern village.")
	ReachNorthObj = Allies.AddPrimaryObjective("Find Northern village.")
	DestroyBridgesObj = Allies.AddPrimaryObjective("Destroy Bridges to reduce ant attacks.")
	FindColonyObj = Allies.AddSecondaryObjective("Investigate Southern ant colony.")
	ResearchCenterObj = Allies.AddSecondaryObjective("Capture bio research center on island.")
end

InitTriggers = function()
	Trigger.OnCapture(Actor194, function() Allies.MarkCompletedObjective(ResearchCenterObj) end)
	Trigger.OnPlayerDiscovered(Turkey,function() 
		Allies.MarkCompletedObjective(FindColonyObj)
	end)
	
	Utils.Do({Actor211,Actor212,Actor213}, function(actor)
		Trigger.OnKilled(actor, function()
			local count = #Turkey.GetActorsByType("truk")
			if count == 0 then
				Allies.MarkCompletedObjective(Allies.AddSecondaryObjective("Gas the Southern hive!"))
			end
		end)
	end)
	
	Utils.Do(NVillage, function(nactor)
		Trigger.OnEnteredProximityTrigger(nactor.CenterPosition, WDist.FromCells(8), function(discoverer, nid)
			if (NorthNotDiscovered and discoverer.Owner == Allies) then		
				RescueNorth =  Allies.AddPrimaryObjective("Rescue villagers!")
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
	
	Utils.Do(SVillage, function(sactor)
		Trigger.OnEnteredProximityTrigger(sactor.CenterPosition, WDist.FromCells(8), function(discoverer2, sid)
			if (SouthNotDiscovered and discoverer2.Owner == Allies) then
				Trigger.AfterDelay(DateTime.Seconds(1), function() Allies.MarkCompletedObjective(ReachSouthObj) end)
				RescueSouth =  Allies.AddPrimaryObjective("Rescue villagers!")
				SpawnSouthVillagers()
				StartSouthAttack()
				SouthNotDiscovered = false
			end
			if not SouthNotDiscovered then 
				Trigger.RemoveProximityTrigger(nid)
			end
		end)
	end)
	
end

SpawnNorthVillagers = function()
	LivingNorthernVillagers = 2
	local sml = {waypoint31.Location, waypoint38.Location}
	NorthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, sml, DateTime.Seconds(1),function(actor)
		Trigger.OnKilled(actor, function() LivingNorthernVillagers = LivingNorthernVillagers - 1 end)
		if (LivingNorthernVillagers == 0) then 
			Allies.MarkFailedObjective(RescueNorth)
		end
	end)
	
	Trigger.AfterDelay(DateTime.Seconds(3), function()
	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(8), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(NorthernVillagers, function(villager)
				if actor == villager then
					Trigger.RemoveProximityTrigger(id)
					Allies.MarkCompletedObjective(RescueNorth)
				end
			end)
		end
	end)end)
end
SpawnSouthVillagers = function()
	LivingSouthernVillagers = 2
	local sml = {waypoint28.Location, waypoint29.Location}
	SouthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, sml, DateTime.Seconds(1),function(actor)
		Trigger.OnKilled(actor, function() LivingSouthernVillagers = LivingSouthernVillagers - 1 end)
		if (LivingSouthernVillagers == 0) then 
			Allies.MarkFailedObjective(RescueSouth)
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(8), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(SouthernVillagers, function(villager)
				if actor == villager then
					Trigger.RemoveProximityTrigger(id)
					Allies.MarkCompletedObjective(RescueSouth)
				end
			end)
		end
	end) end)
end

StartNorthAttack = function()
	local AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
	NorthernStrikeForce = StartAttack(AntList, {waypoint0.Location, waypoint12.Location, NEBridge.Location}, CPos.New(101,58))
end

StartSouthAttack = function()
	AntList = {"ant","ant","ant","ant","ant","fireant","fireant"}
	SouthernStrikeForce = StartAttack(AntList, {waypoint2.Location, waypoint8.Location}, CPos.New(105,13))
end

StartAttack = function(ActorList, MoveRoute, AttackPoint)

	return Reinforcements.Reinforce(Ukraine,ActorList, MoveRoute, DateTime.Seconds(3), function(actor)
		actor.AttackMove(AttackPoint)
	end)
	
end
