--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
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

Trigger.OnKilledOrCaptured(Dome, function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		player.MarkCompletedObjective(sovietObjective2)
		Media.PlaySpeechNotification(player, "ObjectiveMet")
	end)
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

	InitObjectives(player)
	alliedObjective = enemy.AddObjective("Destroy all Soviet troops.")
	sovietObjective = player.AddObjective("Escort the Convoy.")
	sovietObjective2 = player.AddObjective("Destroy or capture the Allied radar dome to stop\nenemy reinforcements.", "Secondary", false)
	SaveAllTrucks = player.AddObjective("Keep all trucks alive.", "Secondary", false)
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
