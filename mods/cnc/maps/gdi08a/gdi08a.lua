--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SamSites = { sam1, sam2, sam3 }

WaypointGroup1 = { waypoint4, waypoint5, waypoint6, waypoint7, waypoint10, waypoint11, waypoint12 }
WaypointGroup2 = { waypoint4, waypoint5, waypoint13, waypoint16 }
WaypointGroup3 = { waypoint4, waypoint5, waypoint6, waypoint8 }
WaypointGroup4 = { waypoint4, waypoint5, waypoint6, waypoint7, waypoint9 }
WaypointGroup5 = { waypoint4, waypoint5, waypoint6 }
WaypointGroup6 = { waypoint4, waypoint5, waypoint6, waypoint7, waypoint14, waypoint15 }
WaypointGroup7 = { waypoint4, waypoint5 }
WaypointGroupCiv = { waypoint0, waypoint1, waypoint2, waypoint3 }

Atk1 = { units = { ['ltnk'] = 1 }, waypoints = WaypointGroup1, delay = 0 }
Atk2 = { units = { ['ltnk'] = 1 }, waypoints = WaypointGroup2, delay = 0 }
Civ1 = { units = { ['c3'] = 1 }, waypoints = WaypointGroupCiv, delay = 0 }
Nod1 = { units = { ['e1'] = 2, ['e2'] = 2, ['e4'] = 2 }, waypoints = WaypointGroup3, delay = 90 }
Nod2 = { units = { ['e3'] = 2, ['e4'] = 2 }, waypoints = WaypointGroup3, delay = 130 }
Nod3 = { units = { ['e1'] = 2, ['e3'] = 3 }, waypoints = WaypointGroup3, delay = 50 }
Nod4 = { units = { ['bggy'] = 2 }, waypoints = WaypointGroup3, delay = 200 }
Nod5 = { units = { ['e4'] = 2, ['ltnk'] = 1 }, waypoints = WaypointGroup1, delay = 250 }
Nod6 = { units = { ['arty'] = 1 }, waypoints = WaypointGroup4, delay = 40 }
Nod7 = { units = { ['e3'] = 2, ['e4'] = 2 }, waypoints = WaypointGroup4, delay = 40 }
Nod8 = { units = { ['ltnk'] = 1, ['bggy'] = 1 }, waypoints = WaypointGroup3, delay = 170 }
Auto1 = { units = { ['e1'] = 2, ['e2'] = 2 }, waypoints = WaypointGroup5, delay = 50 }
Auto2 = { units = { ['e3'] = 3, ['e4'] = 2 }, waypoints = WaypointGroup3, delay = 50 }
Auto3 = { units = { ['ltnk'] = 1, ['bggy'] = 1 }, waypoints = WaypointGroup3, delay = 50 }
Auto4 = { units = { ['bggy'] = 2 }, waypoints = WaypointGroup7, delay = 50 }
Auto5 = { units = { ['ltnk'] = 1 }, waypoints = WaypointGroup6, delay = 50 } 
Auto6 = { units = { ['arty'] = 1 }, waypoints = WaypointGroup4, delay = 50 }
Auto7 = { units = { ['e3'] = 3, ['e4'] = 2 }, waypoints = WaypointGroup6, delay = 50 }

AutoAttackWaves = { Atk1, Atk2, Nod1, Nod2, Nod3, Nod4, Nod5, Nod6, Nod7, Nod8, Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7 }

StationaryGuardUnits = { Actor181, Actor182, Actor183, Actor184, Actor198, Actor199, Actor157, Actor175, Actor176, Actor173, Actor174, Actor158, Actor200, Actor159, Actor179, Actor180, Actor184, Actor185, Actor216, Actor217, Actor153, Actor215, Actor214, Actor213}

DamagedGDIAssets = { Actor126, Actor127, Actor128, Actor129, Actor130,Actor131, Actor132, Actor133, Actor134, Actor135, Actor136, Actor137, Actor138, Actor160, Actor161, Actor162, Actor163, Actor164, Actor165, Actor166, Actor168, Actor169, Actor170}

StartStationaryGuards = function(StationaryGuards)
	Utils.Do(StationaryGuards, function(unit)
		if not unit.IsDead then
			unit.Patrol( { unit.Location } , true, 20)
		end
	end)
end

SendWaves = function(counter, Waves)
	if counter <= #Waves then
		local team = Waves[counter]

		for type, amount in pairs(team.units) do
			MoveAndHunt(Utils.Take(amount, Nod.GetActorsByType(type)), team.waypoints)
		end

		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

SendAttackWave = function(team)
	for type, amount in pairs(team.units) do
		count = 0
		local actors = Nod.GetActorsByType(type)
		Utils.Do(actors, function(actor)
			if actor.IsIdle and count < amount then
				SetAttackWaypoints(actor, team.waypoints)
				IdleHunt(actor)
				count = count + 1
			end
		end)
	end
end

SetAttackWaypoints = function(actor, waypoints)
	if not actor.IsDead then
		Utils.Do(waypoints, function(waypoint)
			actor.AttackMove(waypoint.Location)
		end)
	end
end

CeckRepairGDIAssetsObjective = function()
	local failed = false
	local repaired = true
	Utils.Do(DamagedGDIAssets, function(actor)
		if actor.IsDead then
			failed = true
		elseif actor.Health < actor.MaxHealth then
			repaired = false
		end
	end)

	if failed then
		GDI.MarkFailedObjective(RepairAssets)
		return
	elseif repaired then
		GDI.MarkCompletedObjective(RepairAssets)
		return
	end
	Trigger.AfterDelay(DateTime.Seconds(3), function() CeckRepairGDIAssetsObjective() end)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Camera.Position = DefaultCameraPosition.CenterPosition

	StartStationaryGuards(StationaryGuardUnits)

	StartAI()

	InitObjectives(GDI)

	SecureArea = GDI.AddObjective("Destroy the Nod strike force.")
	KillGDI = Nod.AddObjective("Kill all enemies!")

	RepairAssets = GDI.AddObjective("Repair GDI base and vehicles.", "Secondary", false)
	Trigger.AfterDelay(DateTime.Seconds(5), function() CeckRepairGDIAssetsObjective() end)

	AirSupport = GDI.AddObjective("Destroy the SAM sites to receive air support.", "Secondary", false)
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(AirSupport)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	local InitialArrivingUnits = { Actor166 }
	Utils.Do(InitialArrivingUnits, function(unit)
		unit.Move(unit.Location + CVec.New(1, 1), 0)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function() SendWaves(1, AutoAttackWaves) end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(handofnod) end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceVehicle(nodairfield) end)
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) then
		if GDI.HasNoRequiredUnits()  then
			Nod.MarkCompletedObjective(KillGDI)
		end
		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(SecureArea)
		end
	end
end
