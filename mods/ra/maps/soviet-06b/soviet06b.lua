--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	if not TruckGoalTriggered and a.Owner == USSR and a.Type == "truk" then
		TruckGoalTriggered = true
		USSR.MarkCompletedObjective(SovietObjective)
		USSR.MarkCompletedObjective(SaveAllTrucks)
	end
end)

Trigger.OnAllKilled(Trucks, function()
	Greece.MarkCompletedObjective(AlliedObjective)
end)

Trigger.OnAnyKilled(Trucks, function()
	USSR.MarkFailedObjective(SaveAllTrucks)
end)

Trigger.OnKilled(Apwr, function()
	BaseApwr.exists = false
end)

Trigger.OnKilled(Barr, function()
	BaseTent.exists = false
end)

Trigger.OnKilled(Proc, function()
	BaseProc.exists = false
end)

Trigger.OnKilled(Weap, function()
	BaseWeap.exists = false
end)

Trigger.OnKilled(Apwr2, function()
	BaseApwr2.exists = false
end)

Trigger.OnKilledOrCaptured(Dome, function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		USSR.MarkCompletedObjective(SovietObjective2)
		Media.PlaySpeechNotification(USSR, "ObjectiveMet")
	end)
end)

Trigger.OnRemovedFromWorld(Mcv, function()
	if not McvDeployed then
		McvDeployed = true
		BuildBase()
		SendEnemies()
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)
	end
end)

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	Camera.Position = CameraStart.CenterPosition

	Mcv.Move(McvWaypoint.Location)
	Harvester.FindResources()
	Utils.Do(IntroAttackers, function(actor)
		IdleHunt(actor)
	end)

	Reinforcements.ReinforceWithTransport(USSR, "apc", SovietReinforcements1, SovietReinforcements1Waypoints)
	Reinforcements.ReinforceWithTransport(USSR, "apc", SovietReinforcements2, SovietReinforcements2Waypoints)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	InitObjectives(USSR)
	AlliedObjective = AddPrimaryObjective(Greece, "")
	SovietObjective = AddPrimaryObjective(USSR, "escort-convoy")
	SovietObjective2 = AddSecondaryObjective(USSR, "destroy-capture-radar-dome-reinforcements")
	SaveAllTrucks = AddSecondaryObjective(USSR, "Keep all trucks alive.")
end

Tick = function()
	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(AlliedObjective)
	end

	if Greece.Resources >= Greece.ResourceCapacity * 0.75 then
		Greece.Cash = Greece.Cash + Greece.Resources - Greece.ResourceCapacity * 0.25
		Greece.Resources = Greece.ResourceCapacity * 0.25
	end
end
