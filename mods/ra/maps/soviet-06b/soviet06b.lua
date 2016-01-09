IntroAttackers = { IntroEnemy1, IntroEnemy2, IntroEnemy3 }
Trucks = { Truck1, Truck2 }
InfAttack = { }
ArmorAttack = { }
AttackPaths = { { AttackWaypoint1 }, { AttackWaypoint2 } }

AlliedInfantryTypes = { "e1", "e1", "e3" }
AlliedArmorTypes = { "jeep", "jeep", "1tnk", "1tnk", "2tnk", "2tnk", "arty" }

SovietReinforcements1 = { "e6", "e6", "e6", "e6", "e6" }
SovietReinforcements2 = { "e4", "e4", "e2", "e2", "e2" }
SovietReinforcements1Waypoints = { McvWaypoint.Location, APCWaypoint1.Location }
SovietReinforcements2Waypoints = { McvWaypoint.Location, APCWaypoint2.Location }

TruckGoalTrigger = { CPos.New(85, 10), CPos.New(85, 11), CPos.New(85, 12), CPos.New(86, 13), CPos.New(87, 13), CPos.New(88, 13), CPos.New(88, 14), CPos.New(89, 14), CPos.New(90, 14), CPos.New(90, 15), CPos.New(91, 15), CPos.New(91, 16), CPos.New(91, 17), CPos.New(92, 17), CPos.New(93, 17), CPos.New(94, 17), CPos.New(94, 18), CPos.New(95, 18), CPos.New(96, 18), CPos.New(96, 19), CPos.New(97, 19), CPos.New(98, 19)}

Trigger.OnEnteredFootprint(TruckGoalTrigger, function(a, id)
	if not truckGoalTrigger and a.Owner == player and a.Type == "truk" then
		truckGoalTrigger = true
		player.MarkCompletedObjective(sovietObjective)
		player.MarkCompletedObjective(SaveAllTrucks)
	end
end)

Trigger.OnAllKilled(Trucks, function()
	enemy.MarkCompletedObjective(alliedObjective)
end)

Trigger.OnAnyKilled(Trucks, function()
	player.MarkFailedObjective(SaveAllTrucks)
end)

Trigger.OnKilled(Apwr, function(building)
	BaseApwr.exists = false
end)

Trigger.OnKilled(Barr, function(building)
	BaseTent.exists = false
end)

Trigger.OnKilled(Proc, function(building)
	BaseProc.exists = false
end)

Trigger.OnKilled(Weap, function(building)
	BaseWeap.exists = false
end)

Trigger.OnKilled(Apwr2, function(building)
	BaseApwr2.exists = false
end)

Trigger.OnKilled(Dome, function()
	player.MarkCompletedObjective(sovietObjective2)
	Media.PlaySpeechNotification(player, "ObjectiveMet")
end)

Trigger.OnRemovedFromWorld(Mcv, function()
	if not mcvDeployed then
		mcvDeployed = true
		BuildBase()
		SendEnemies()
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)
	end
end)

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	enemy = Player.GetPlayer("Greece")
	Camera.Position = CameraStart.CenterPosition
	Mcv.Move(McvWaypoint.Location)
	Harvester.FindResources()
	Utils.Do(IntroAttackers, function(actor)
		IdleHunt(actor)
	end)
	Reinforcements.ReinforceWithTransport(player, "apc", SovietReinforcements1, SovietReinforcements1Waypoints)
	Reinforcements.ReinforceWithTransport(player, "apc", SovietReinforcements2, SovietReinforcements2Waypoints)
	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == enemy and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == enemy and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)
	alliedObjective = enemy.AddPrimaryObjective("Destroy all Soviet troops.")
	sovietObjective = player.AddPrimaryObjective("Escort the Convoy.")
	sovietObjective2 = player.AddSecondaryObjective("Destroy the Allied radar dome to stop enemy\nreinforcements.")
	SaveAllTrucks = player.AddSecondaryObjective("Keep all trucks alive.")
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(alliedObjective)
	end

	if enemy.Resources >= enemy.ResourceCapacity * 0.75 then
		enemy.Cash = enemy.Cash + enemy.Resources - enemy.ResourceCapacity * 0.25
		enemy.Resources = enemy.ResourceCapacity * 0.25
	end
end
