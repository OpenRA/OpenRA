--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
ArmorAttack = { }
AttackPaths = { { AttackWaypoint1 }, { AttackWaypoint2 } }
BaseAttackers = { BaseAttacker1, BaseAttacker2 }
InfAttack = { }
IntroAttackers = { IntroEnemy1, IntroEnemy2, IntroEnemy3 }
Trucks = { Truck1, Truck2 }

AlliedInfantryTypes = { "e1", "e1", "e3" }
AlliedArmorTypes = { "jeep", "jeep", "1tnk", "1tnk", "2tnk", "2tnk", "arty" }

SovietReinforcements1 = { "e6", "e6", "e6", "e6", "e6" }
SovietReinforcements2 = { "e4", "e4", "e2", "e2", "e2" }
SovietReinforcements1Waypoints = { McvWaypoint.Location, APCWaypoint1.Location }
SovietReinforcements2Waypoints = { McvWaypoint.Location, APCWaypoint2.Location }

TruckGoalTrigger = { CPos.New(83, 7), CPos.New(83, 8), CPos.New(83, 9), CPos.New(83, 10), CPos.New(84, 10), CPos.New(84, 11), CPos.New(84, 12), CPos.New(85, 12), CPos.New(86, 12), CPos.New(87, 12), CPos.New(87, 13), CPos.New(88, 13), CPos.New(89, 13), CPos.New(90, 13), CPos.New(90, 14), CPos.New(90, 15), CPos.New(91, 15), CPos.New(92, 15), CPos.New(93, 15), CPos.New(94, 15) }
CameraBarrierTrigger = { CPos.New(65, 39), CPos.New(65, 40), CPos.New(66, 40), CPos.New(66, 41), CPos.New(67, 41), CPos.New(67, 42), CPos.New(68, 42), CPos.New(68, 43), CPos.New(68, 44) }
CameraBaseTrigger = { CPos.New(53, 42), CPos.New(54, 42), CPos.New(54, 41), CPos.New(55, 41), CPos.New(56, 41), CPos.New(56, 40), CPos.New(57, 40), CPos.New(57, 39), CPos.New(58, 39), CPos.New(59, 39), CPos.New(59, 38), CPos.New(60, 38), CPos.New(61, 38) }

Trigger.OnEnteredFootprint(TruckGoalTrigger, function(a, id)
	if not TruckGoalTriggered and a.Owner == USSR and a.Type == "truk" then
		TruckGoalTriggered = true
		USSR.MarkCompletedObjective(SovietObjective)
		USSR.MarkCompletedObjective(SaveAllTrucks)
	end
end)

Trigger.OnEnteredFootprint(CameraBarrierTrigger, function(a, id)
	if not CameraBarrierTriggered and a.Owner == USSR then
		CameraBarrierTriggered = true
		local cameraBarrier = Actor.Create("camera", true, { Owner = USSR, Location = CameraBarrier.Location })
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			cameraBarrier.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(CameraBaseTrigger, function(a, id)
	if not CameraBaseTriggered and a.Owner == USSR then
		CameraBaseTriggered = true
		local cameraBase1 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase1.Location })
		local cameraBase2 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase2.Location })
		local cameraBase3 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase3.Location })
		local cameraBase4 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase4.Location })
		Trigger.AfterDelay(DateTime.Minutes(1), function()
			cameraBase1.Destroy()
			cameraBase2.Destroy()
			cameraBase3.Destroy()
			cameraBase4.Destroy()
		end)
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

-- Activate the AI once the player deployed the Mcv
Trigger.OnRemovedFromWorld(Mcv, function()
	if not McvDeployed then
		McvDeployed = true
		BuildBase()
		SendEnemies()
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)
		Trigger.AfterDelay(DateTime.Minutes(2), function()
			Utils.Do(BaseAttackers, function(actor)
				IdleHunt(actor)
			end)
		end)
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

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Reinforcements.ReinforceWithTransport(USSR, "apc", SovietReinforcements1, SovietReinforcements1Waypoints)
	Reinforcements.ReinforceWithTransport(USSR, "apc", SovietReinforcements2, SovietReinforcements2Waypoints)

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
