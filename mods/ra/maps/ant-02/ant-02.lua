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
	Special = Player.GetPlayer("Special")
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
	Trigger.OnAllKilled(NorthernVillagers, function() 
		if  not Allies.IsObjectiveCompleted(RescueNorth) then
			Allies.MarkFailedObjective(RescueNorth)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(10), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(NorthernVillagers, function(villager)
				if actor == villager then
					if (not Allies.IsObjectiveCompleted(RescueNorth)) then
						Allies.MarkCompletedObjective(RescueNorth)
					end
					actor.Owner = Special
					actor.Move(CPos.New(46,10))
					if actor.Type == "gnrl" then Media.DisplayMessage("Christ, did you see the size of those things? Good luck Commander!","Field Marshal",HSLColor.DarkGreen) end
					Trigger.OnIdle(actor, actor.Destroy)
				end
			end)
		end
	end)
end

SpawnSouthVillagers = function()
	SouthernVillagers = Reinforcements.Reinforce(Allies,VillagerList, {Actor204.Location, waypoint38.Location}, DateTime.Seconds(1))
	Trigger.OnAllKilled(SouthernVillagers, function()		
		if not Allies.IsObjectiveCompleted(RescueSouth) then
			Allies.MarkFailedObjective(RescueSouth)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Actor194.CenterPosition, WDist.FromCells(10), function(actor, id)
		if (actor.Owner == Allies) then
			Utils.Do(SouthernVillagers, function(villager)
				if actor == villager then
					if (not Allies.IsObjectiveCompleted(RescueSouth)) then
						Allies.MarkCompletedObjective(RescueSouth)
					end
					if actor.Type == "gnrl" then Media.DisplayMessage("Thank you for rescuing us Commander.","Commandant",HSLColor.LightBlue) end
					actor.Owner = GoodGuy
					actor.Move(CPos.New(46,10))
					Trigger.OnIdle(actor, actor.Destroy)
				end
			end)
		end
	end)
end

StartNorthAttack = function()
	if (not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then
		local AntList = {"ant","ant","ant","ant","ant","fireant","fireant","fireant","fireant","scoutant","scoutant"}
		NorthernStrikeForce = StartAttack(AntList, {waypoint0.Location, waypoint12.Location}, Actor192.Location, Ukraine)
	end
end

StartSouthAttack = function()
	if (GasColonyObj == nil) then
		local AntList = {"ant","ant","ant","ant","ant","fireant","fireant","fireant","fireant","scoutant","scoutant"}
		SouthernStrikeForce = StartAttack(AntList, {waypoint2.Location, waypoint8.Location}, Actor204.Location, Ukraine)
	end
end

StartAttack = function(ActorList, MoveRoute, AttackPoint, Faction)

	return Reinforcements.Reinforce(Faction,ActorList, MoveRoute, DateTime.Seconds(3), function(actor)
		actor.AttackMove(AttackPoint)
		actor.Hunt()
		Trigger.OnIdle(actor, function(actor) actor.Hunt() end)
	end)
	
end

SendAntAttack = function(colony)
	if (colony == 0 and not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then

		StartAttack({"ant","scoutant","ant","ant", "fireant"},{waypoint0.Location,waypoint12.Location}, waypoint18.Location, BadGuy)

	elseif (colony == 1 and not Allies.IsObjectiveCompleted(DestroyBridgesObj)) then
		if #Allies.GetActorsByType("fact") > 0 then
			local conyard = Allies.GetActorsByType("fact")[0]
			StartAttack({"fireant","ant","ant","fireant","scoutant"},{waypoint1.Location,waypoint11.Location}, conyard.Location, BadGuy)
		else
			StartAttack({"fireant","ant","ant","fireant","scoutant"},{waypoint1.Location,waypoint11.Location}, waypoint35.Location, BadGuy)
		end
	elseif (ShouldLaunchAttack) then
		StartAttack({"scoutant","fireant","ant","fireant","scoutant"},{waypoint2.Location,waypoint95.Location}, waypoint20.Location, BadGuy)
	end
end

SendChemTroops = function()
	local MovePath = {CPos.New(114,83), CPos.New(107,86)}
	local Forces = {"extrm","e1r1"}
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsEast")
	Media.DisplayMessage("We've brought a experimental gas canister launcher recovered from Soviet plans!","Trooper",Allies.Color)
	Reinforcements.ReinforceWithTransport(Allies, "jeep", Forces, MovePath, {CPos.New(107,86), CPos.New(114,83)})
end

SendSniperTeam = function()
	local MovePath = {waypoint36.Location, CPos.New(65,65)}
	local Forces = {"sniper","e1r1"}
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(Allies, "tran", Forces, MovePath, {CPos.New(65,65), waypoint36.Location})
end

Tick = function() 
	if (DateTime.GameTime > DateTime.Seconds(1)) then
		if (not Allies.IsObjectiveCompleted(DestroyBridgesObj) and BridgesLeft < 1) then Allies.MarkCompletedObjective(DestroyBridgesObj) end
	end
	if (DateTime.GameTime > DateTime.Minutes(3) or MCVMovedPast) then
		ticks = ticks + 1
		local eventTime = DateTime.Minutes(1) + (DateTime.Seconds(15) * (AttackCounter % 4))
		if (ticks == eventTime) then
			AttackCounter = AttackCounter + 1
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				SendAntAttack(AttackCounter % 3)
			end)
			ticks = DateTime.Minutes(1)
		end
	end
end
