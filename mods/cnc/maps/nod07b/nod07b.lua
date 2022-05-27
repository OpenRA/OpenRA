--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

WaypointGroup1 = { waypoint0, waypoint15 }
WaypointGroup2 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5, waypoint8 }
WaypointGroup3 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint9, waypoint10, waypoint11, waypoint6, waypoint7 }
WaypointGroup4 = { waypoint9, waypoint10, waypoint11, waypoint6, waypoint7, waypoint14 }

GDI1 = { units = { "e2", "e2", "e6", "e6", "e6" }, waypoints = WaypointGroup4, delay = 40 }
GDI2 = { units = { "e1", "e2" }, waypoints = WaypointGroup3, delay = 40 }
GDI3 = { units = { "e2", "e3", "jeep" }, waypoints = WaypointGroup2, delay = 40 }
GDI4 = { units = { "mtnk" }, waypoints = WaypointGroup3, delay = 40 }
GDI5 = { units = { "e1", "e2" }, waypoints = WaypointGroup2, delay = 40 }
GDI6 = { units = { "e2", "e2", "e2", "e2", "e2" }, waypoints = WaypointGroup1, delay = 40 }
Auto1 = { units = { "e1", "e1", "e1", "e2", "e2" }, waypoints = WaypointGroup3, delay = 40 }
Auto2 = { units = { "e1", "e2", "e2" }, waypoints = WaypointGroup2, delay = 40 }
Auto3 = { units = { "e1", "e3", "e3" }, waypoints = WaypointGroup2, delay = 40 }
Auto4 = { units = { "e2", "e2", "e3", "e3" }, waypoints = WaypointGroup3, delay = 40 }
Auto5 = { units = { "jeep" }, waypoints = WaypointGroup2, delay = 50 }
Auto6 = { units = { "jeep" }, waypoints = WaypointGroup3, delay = 40 }
Auto7 = { units = { "mtnk" }, waypoints = WaypointGroup2, delay = 50 }
Auto8 = { units = { "mtnk" }, waypoints = WaypointGroup3, delay = 30 }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(10)

AutoAttackWaves = { Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7, Auto8 }
WhitelistedStructures = { "afld", "hand", "hq", "nuke", "silo", "proc", "sam" }

NodUnitsTanks = { "ltnk", "ltnk", "ltnk" }
NodUnitsBikes = { "bike", "bike", "bike" }
NodUnitsBuggys = { "bggy", "bggy", "bggy" }
NodUnitsRockets = { "e3", "e3", "e3" }
NodUnitsGunners = { "e1", "e1", "e1" }

Atk1 = { CPos.New(11, 43), CPos.New(10, 43), CPos.New(9, 43), CPos.New(8, 43), CPos.New(7, 43), CPos.New(6, 43), CPos.New(5, 43), CPos.New(11, 42), CPos.New(10, 42), CPos.New(9, 42), CPos.New(8, 42), CPos.New(7, 42), CPos.New(6, 42), CPos.New(5, 42), CPos.New(23, 38), CPos.New(22, 38), CPos.New(21, 38), CPos.New(20, 38), CPos.New(19, 38), CPos.New(24, 37), CPos.New(23, 37), CPos.New(22, 37), CPos.New(21, 37), CPos.New(20, 37), CPos.New(19, 37) }
Atk2 = { CPos.New(16, 52), CPos.New(15, 52), CPos.New(14, 52), CPos.New(13, 52), CPos.New(12, 52), CPos.New(11, 52), CPos.New(10, 52), CPos.New(9, 52), CPos.New(8, 52), CPos.New(16, 51), CPos.New(15, 51), CPos.New(14, 51), CPos.New(13, 51), CPos.New(12, 51), CPos.New(11, 51), CPos.New(10, 51), CPos.New(9, 51), CPos.New(8, 51), CPos.New(31, 44), CPos.New(30, 44), CPos.New(29, 44), CPos.New(28, 44), CPos.New(27, 44), CPos.New(26, 44), CPos.New(25, 44), CPos.New(24, 44), CPos.New(23, 44), CPos.New(22, 44), CPos.New(21, 44), CPos.New(31, 43), CPos.New(30, 43), CPos.New(29, 43), CPos.New(28, 43), CPos.New(27, 43), CPos.New(26, 43), CPos.New(25, 43), CPos.New(24, 43), CPos.New(23, 43), CPos.New(22, 43), CPos.New(21, 43) }
Atk3 = { CPos.New(53, 58), CPos.New(52, 58), CPos.New(51, 58), CPos.New(53, 57), CPos.New(52, 57), CPos.New(51, 57), CPos.New(53, 56), CPos.New(52, 56), CPos.New(51, 56), CPos.New(53, 55), CPos.New(52, 55), CPos.New(51, 55) }
Atk4 = { CPos.New(54, 47), CPos.New(53, 47), CPos.New(52, 47), CPos.New(51, 47), CPos.New(43, 47), CPos.New(54, 46), CPos.New(53, 46), CPos.New(52, 46), CPos.New(51, 46), CPos.New(50, 46), CPos.New(43, 46), CPos.New(42, 46), CPos.New(41, 46), CPos.New(43, 45), CPos.New(42, 45), CPos.New(41, 45), CPos.New(43, 44), CPos.New(42, 44), CPos.New(41, 44), CPos.New(43, 43), CPos.New(42, 43), CPos.New(41, 43), CPos.New(43, 42) }

CaptureStructures = function(actor)
	for i = 1, #WhitelistedStructures do
		structures = Nod.GetActorsByType(WhitelistedStructures[i])
		if #structures > 0 and not actor.IsDead and not structures[1].IsDead then
			actor.Capture(structures[1])
			return
		end
	end
end

CheckForSams = function()
	local sams = Nod.GetActorsByType("sam")
	return #sams >= 3
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnitsBikes, { ReinforcementsSpawnLeft.Location, ReinforcementsBikesRally.Location }, 1)
	Reinforcements.Reinforce(Nod, NodUnitsBuggys, { ReinforcementsSpawnRight.Location, ReinforcementsBuggyRally.Location }, 50)
	Reinforcements.Reinforce(Nod, NodUnitsGunners, { ReinforcementsSpawnLeft.Location, ReinforcementsGunnersRally.Location }, 50)
	Reinforcements.Reinforce(Nod, NodUnitsRockets, { ReinforcementsSpawnRight.Location, ReinforcementsRocketsRally.Location }, 50)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Reinforcements.Reinforce(Nod, { "mcv" }, { ReinforcementsSpawnCenter.Location, ReinforcementsMCVRally.Location })
		Reinforcements.Reinforce(Nod, NodUnitsTanks, { ReinforcementsSpawnCenter.Location, ReinforcementsTanksRally.Location }, 50)
	end)
end

SendAttackWave = function(team)
	Utils.Do(team.units, function(unitType)
		local actors = Utils.Where(GDI.GetActorsByType(unitType), function(unit) return unit.IsIdle end)
		MoveAndHunt(Utils.Take(1, actors), team.waypoints)
	end)
end

SendGDIAirstrike = function(hq, delay)
	if not hq.IsDead and hq.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)

		if target then
			hq.TargetAirstrike(target, Angle.NorthEast + Angle.New(16))
			Trigger.AfterDelay(delay, function() SendGDIAirstrike(hq, delay) end)
		else
			Trigger.AfterDelay(delay/4, function() SendGDIAirstrike(hq, delay) end)
		end
	end
end

SendWaves = function(counter, Waves)
	if counter <= #Waves then
		local team = Waves[counter]
		SendAttackWave(team)
		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

SendReinforcementsWave = function(team)
	Reinforcements.ReinforceWithTransport(GDI, "apc", team.units, { ReinforcementsGDISpawn.Location, waypoint12.Location}, nil, function(transport, passengers)
		Utils.Do(team.waypoints, function(waypoint)
			transport.Move(waypoint.Location)
		end)

		transport.UnloadPassengers()
		Trigger.OnPassengerExited(transport, function(_, passenger)
			if passenger.Type == "e6" then
				Trigger.OnIdle(passenger, CaptureStructures)
			else
				IdleHunt(passenger)
			end

			if not transport.HasPassengers then
				IdleHunt(transport)
			end
		end)
	end)
end

Trigger.OnEnteredFootprint(Atk1, function(a, id)
	if not atk1Trigger and a.Owner == Nod then
		atk1Trigger = true
		SendAttackWave(GDI5)
		Trigger.RemoveFootprintTrigger(id)
	end
end)

Trigger.OnEnteredFootprint(Atk2, function(a, id)
	if not atk2Trigger and a.Owner == Nod then
		atk2Trigger = true
		SendAttackWave(GDI4)
		Trigger.RemoveFootprintTrigger(id)
	end
end)

Trigger.OnEnteredFootprint(Atk3, function(a, id)
	if not atk3Trigger and a.Owner == Nod then
		atk3Trigger = true
		SendAttackWave(GDI6)
		Trigger.RemoveFootprintTrigger(id)
	end
end)

Trigger.OnEnteredFootprint(Atk4, function(a, id)
	if not atk4Trigger and a.Owner == Nod then
		atk4Trigger = true
		SendReinforcementsWave(GDI1)
		Trigger.RemoveFootprintTrigger(id)
	end
end)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()
	StartAI()

	Trigger.AfterDelay(DateTime.Seconds(10), function() SendAttackWave(GDI2) end)
	Trigger.AfterDelay(DateTime.Seconds(55), function() SendAttackWave(GDI2) end)
	Trigger.AfterDelay(DateTime.Seconds(85), function() SendAttackWave(GDI3) end)

	Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
	Trigger.OnPlayerDiscovered(GDI, function() SendWaves(1, AutoAttackWaves) end)

	InitObjectives(Nod)

	EliminateGDI = Nod.AddObjective("Eliminate all GDI forces in the area.")
	BuildSAMs = Nod.AddObjective("Build 3 SAMs to fend off the GDI bombers.", "Secondary", false)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(EliminateGDI)
	end

	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(EliminateGDI)
	end

	if not Nod.IsObjectiveCompleted(BuildSAMs) and CheckForSams() then
		Nod.MarkCompletedObjective(BuildSAMs)
	end
end
