--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

InsertionHelicopterType = "tran.insertion"
GDIHeliReinfUnits = { "e2", "e2", "e2", "e3", "e3" }

SamSites = { sam1, sam2, sam3, sam4 }
NodBunkersNorth = { gun3, gun4 }
NodBunkersSouth = { gun1, gun2 }

BoatEscapeTrigger = { CPos.New(2,37) }

WaypointGroup1 = { waypoint1, waypoint2, waypoint8 }
WaypointGroup2 = { waypoint1, waypoint2, waypoint3, waypoint9 }
WaypointGroup3 = { waypoint1, waypoint2, waypoint3, waypoint10, waypoint11, waypoint12, waypoint6, waypoint13 }
WaypointGroup4 = { waypoint1, waypoint2, waypoint3, waypoint4 }
Patrol1Waypoints = { waypoint11.Location, waypoint10.Location }
Patrol2Waypoints = { waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location, waypoint4.Location, waypoint3.Location, waypoint2.Location, waypoint1.Location, waypoint6.Location }

Nod1 = { units = { ['e1'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 40 }
Nod2 = { units = { ['e3'] = 2, ['e4'] = 2 }, waypoints = WaypointGroup2, delay = 50 }
Nod3 = { units = { ['e1'] = 2, ['e3'] = 3, ['e4'] = 2 }, waypoints = WaypointGroup1, delay = 50 }
Nod4 = { units = { ['bggy'] = 2 }, waypoints = WaypointGroup2, delay = 50 }
Nod5 = { units = { ['e4'] = 2, ['ltnk'] = 1 }, waypoints = WaypointGroup1, delay = 50 }
Auto1 = { units = { ['e4'] = 2, ['arty'] = 1 }, waypoints = WaypointGroup1, delay = 50 }
Auto2 = { units = { ['e1'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup2, delay = 50 }
Auto3 = { units = { ['e3'] = 2, ['e4'] = 2 }, waypoints = WaypointGroup1, delay = 50 }
Auto4 = { units = { ['e1'] = 3, ['e4'] = 1 }, waypoints = WaypointGroup1, delay = 50 }
Auto5 = { units = { ['ltnk'] = 1, ['bggy'] = 1 }, waypoints = WaypointGroup1, delay = 60 }
Auto6 = { units = { ['bggy'] = 1 }, waypoints = WaypointGroup2, delay = 50 }
Auto7 = { units = { ['ltnk'] = 1 }, waypoints = WaypointGroup2, delay = 50 }
Auto8 = { units = { ['e4'] = 2, ['bggy'] = 1 }, waypoints = WaypointGroup4, delay = 0 }

Patrols = {
	grd1 = { units = { ['e3'] = 3 }, waypoints = Patrol1Waypoints, wait = 40, initialWaypointPlacement = { 1 } },
	grd2 = { units = { ['e1'] = 2, ['e3'] = 2, ['e4'] = 2 }, waypoints = Patrol2Waypoints, wait = 20, initialWaypointPlacement = { 4, 10, 1 } }
}

AutoAttackWaves = { Nod1, Nod2, Nod3, Nod4, Nod5, Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7, Auto8 }

StationaryGuards = { Actor174, Actor173, Actor182, Actor183, Actor184, Actor185, Actor186, Actor187 , Actor199, Actor200, Actor201, Actor202, Actor203, Actor204 }

StartStationaryGuards = function()
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

StartPatrols = function()
	for k, team in pairs(Patrols) do
		local group = 1
		for type, amount in pairs(team.units) do
			for i = 1, amount do
				Reinforcements.Reinforce(Nod, { type }, { team.waypoints[team.initialWaypointPlacement[group]] }, 0, function(unit)
					ReplenishPatrolUnit(unit, handofnod, team.waypoints, team.wait)
				end)
			end
			group = group + 1
		end
	end
	Patrols = nil
end

ReplenishPatrolUnit = function(unit, building, waypoints, waitatwaypoint)
	unit.Patrol(waypoints, true, waitatwaypoint)
	Trigger.OnKilled(unit, function()
		local queueUnit = { unit = { unit.Type }, atbuilding = { building }, waypoints = waypoints }
		PatrolProductionQueue[#PatrolProductionQueue + 1] = queueUnit
	end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.ReinforceWithTransport(GDI, InsertionHelicopterType, GDIHeliReinfUnits, { GDIHeliEntryNorth.Location, GDIHeliLZ.Location }, { GDIHeliLZ.Location + CVec.New(20, 0) })
end

SendGDIReinforcementChinook = function()
	Reinforcements.ReinforceWithTransport(GDI, 'tran', nil, { GDIHeliEntryNorth.Location, GDIHeliLZ.Location })
end

SpawnGunboat = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Actor.Create("boat", true, { Owner = GDI, Facing = 0, Location = CPos.New(62,37) })
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Camera.Position = DefaultCameraPosition.CenterPosition

	DestroyBunkers = GDI.AddObjective("Destroy the Nod bunkers to allow Carter's\nconvoy to pass through safely.")
	Trigger.OnAllKilled(NodBunkersNorth, function()
		GDI.MarkCompletedObjective(DestroyBunkers)
		Trigger.AfterDelay(DateTime.Seconds(1), SpawnGunboat)
	end)
	Trigger.OnAllKilled(NodBunkersSouth, function()
		GDI.MarkCompletedObjective(DestroyBunkers)
		SendGDIReinforcementChinook()
		Trigger.AfterDelay(DateTime.Seconds(1), SpawnGunboat)
	end)
	Trigger.OnEnteredFootprint(BoatEscapeTrigger, function(a, id)
		if a.Type == "boat" then
			a.Destroy()
			Media.DisplayMessage("Part of Carter's convoy passed through!")
			Media.PlaySoundNotification(GDI, "AlertBleep")
		end
	end)

	SecureArea = GDI.AddObjective("Destroy the Nod strike force.")
	KillGDI = Nod.AddObjective("Kill all enemies!")

	Trigger.AfterDelay(DateTime.Seconds(5), SendGDIReinforcements)

	AirSupport = GDI.AddObjective("Destroy the SAM sites to receive air support.", "Secondary", false)
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(AirSupport)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Actor.Create("flare", true, { Owner = GDI, Location = DefaultFlareLocation.Location })

	StartStationaryGuards()

	StartAI()

	StartPatrols()

	InitObjectives(GDI)

	Trigger.AfterDelay(DateTime.Minutes(1), function() SendWaves(1, AutoAttackWaves) end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceInfantry(handofnod) end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(nodairfield) end)

	local initialArrivingUnits = { Actor175, Actor191, Actor192, Actor193, Actor194, Actor195, Actor196, Actor197, Actor198 }
	Utils.Do(initialArrivingUnits, function(unit)
		unit.Move(unit.Location + CVec.New(0, 1), 0)
	end)
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
