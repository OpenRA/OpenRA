--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitsVehicle = { 'bike', 'bike', 'bggy', 'ltnk', 'bike', 'bike' }
NodUnitsRocket = { 'e1', 'e1', 'e1', 'e1' }
NodUnitsGunner = { 'e3', 'e3', 'e3', 'e3' }
GDIReinforceUnits = { 'e2', 'e2', 'e2', 'e2', 'e2' }

GDI1Units = { ['e1'] = 3, ['e2'] = 1 }
GDI2Units = { ['e1'] = 2, ['e2'] = 1 }
GDI3Units = { ['jeep'] = 1 }
GDI4Units = { ['mtnk'] = 1 }
GDI5Units = { ['e1'] = 1, ['e2'] = 2 }
GDI6Units = { ['e1'] = 3 }
GDI7Units = { ['e2'] = 2 }
GDI8Units = { ['e2'] = 5 }

AllUnits = { GDI1Units, GDI2Units, GDI3Units, GDI4Units, GDI5Units, GDI6Units, GDI7Units, GDI8Units }

AirstrikeDelay = DateTime.Minutes(1) + DateTime.Seconds(40)

DelyCellTriggerActivator = { CPos.New(29,30), CPos.New(28,30), CPos.New(27,30), CPos.New(26,30), CPos.New(25,30), CPos.New(24,30), CPos.New(23,30), CPos.New(22,30), CPos.New(21,30), CPos.New(29,29), CPos.New(28,29), CPos.New(27,29), CPos.New(26,29), CPos.New(25,29), CPos.New(24,29), CPos.New(23,29), CPos.New(22,29) }
DelzCellTriggerActivator = { CPos.New(29,27), CPos.New(28,27), CPos.New(27,27), CPos.New(26,27), CPos.New(25,27), CPos.New(24,27), CPos.New(29,26), CPos.New(28,26), CPos.New(27,26), CPos.New(26,26), CPos.New(25,26), CPos.New(24,26) }
Atk5CellTriggerActivator = { CPos.New(10,33), CPos.New(9,33), CPos.New(8,33), CPos.New(9,32), CPos.New(8,32), CPos.New(7,32), CPos.New(8,31), CPos.New(7,31), CPos.New(6,31) }
Atk1CellTriggerActivator = { CPos.New(10,33), CPos.New(9,33), CPos.New(8,33), CPos.New(9,32), CPos.New(8,32), CPos.New(7,32), CPos.New(8,31), CPos.New(7,31), CPos.New(6,31) }

GDI1Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4 }
GDI2Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4, waypoint5 }
GDI3Waypoints = { waypoint0, waypoint1, waypoint2 }
GDI5Waypoints = { waypoint0, waypoint1, waypoint3, waypoint1, waypoint6 }
GDI11Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4, waypoint7, waypoint8 }
GDI12Waypoints = { waypoint0, waypoint1, waypoint3, waypoint11, waypoint12 }

AllWaypoints = { GDI1Waypoints, GDI2Waypoints, GDI3Waypoints, GDI5Waypoints, GDI11Waypoints, GDI12Waypoints }

PrimaryTargets = { Tower1, Tower2, CommCenter, Silo1, Silo2, Silo3, Refinery, Barracks, Plant1, Plant2, Yard, Factory }

SendGDIAirstrike = function()
	if not CommCenter.IsDead and CommCenter.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)

		if target then
			CommCenter.TargetAirstrike(target, Angle.NorthEast + Angle.New(16))
			Trigger.AfterDelay(AirstrikeDelay, SendGDIAirstrike)
		else
			Trigger.AfterDelay(AirstrikeDelay/4, SendGDIAirstrike)
		end
	end
end

SendGDI2Units = function()
	if DontSendGDI2 then
		return
	end

	for type, count in pairs(GDI2Units) do
		MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), GDI2Waypoints)
	end
end

SendGDI1Units = function()
	if DontSendGDI1 then
		return
	end

	for type, count in pairs(GDI1Units) do
		MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), GDI1Waypoints)
	end
end

AutoPatrol = function()
	local units = AllUnits[DateTime.GameTime % #AllUnits + 1]
	local waypoints = AllWaypoints[DateTime.GameTime % #AllWaypoints + 1]

	for type, count in pairs(units) do
		MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), waypoints)
	end

	Trigger.AfterDelay(DateTime.Seconds(45), AutoPatrol)
end

RebuildStartUnits = function()
	local types = { "e1", "e2", "jeep", "mtnk" }
	local factories = { Barracks, Barracks, Factory, Factory }

	for i = 1, 4 do
		Utils.Do(GDI.GetActorsByType(types[i]), function(actor)
			RebuildUnit({ actor }, GDI, factories[i])
		end)
	end
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	Camera.Position = UnitsEntry.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnitsVehicle, { UnitsEntry.Location, UnitsRallyVehicle.Location }, 1)
	Reinforcements.Reinforce(Nod, NodUnitsRocket, { UnitsEntry.Location, UnitsRallyRocket.Location }, 50)
	Reinforcements.Reinforce(Nod, NodUnitsGunner, { UnitsEntry.Location, UnitsRallyGunner.Location }, 50)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Reinforcements.Reinforce(Nod, { 'mcv' }, { UnitsEntry.Location, UnitsRallyMCV.Location })
	end)

	InitObjectives(Nod)

	BuildSAMObjective = Nod.AddObjective("Build 3 SAMs.")
	DestroyGDI = Nod.AddObjective("Destroy the GDI base.")
	GDIObjective = GDI.AddObjective("Kill all enemies.")

	Trigger.AfterDelay(AirstrikeDelay, SendGDIAirstrike)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), SendGDI2Units)
	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), SendGDI1Units)

	Trigger.OnEnteredFootprint(DelyCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			DontSendGDI2 = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DelzCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			DontSendGDI1 = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Atk5CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("mtnk")), GDI12Waypoints)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		MoveAndHunt(Utils.Take(2, GDI.GetActorsByType("jeep")), GDI5Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(10), function()
		for type, count in pairs(GDI1Units) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), GDI1Waypoints)
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(3) + DateTime.Seconds(10), function()
		for type, count in pairs(GDI2Units) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), GDI2Waypoints)
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(4) + DateTime.Seconds(40), function()
		MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("jeep")), GDI3Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("mtnk")), GDI1Waypoints)
	end)

	Trigger.OnEnteredFootprint(Atk1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			local cargo = Reinforcements.ReinforceWithTransport(GDI, "tran", GDIReinforceUnits, { waypoint9.Location, waypoint26.Location }, { waypoint9.Location })[2]
			Utils.Do(cargo, IdleHunt)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllKilledOrCaptured(PrimaryTargets, function()
		Nod.MarkCompletedObjective(DestroyGDI)
		Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(0, RebuildStartUnits)

	AutoPatrol()
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if not Nod.IsObjectiveCompleted(BuildSAMObjective) and CheckForSams(Nod) then
		Nod.MarkCompletedObjective(BuildSAMObjective)
	end
end

CheckForSams = function(Nod)
	local sams = Nod.GetActorsByType("sam")
	return #sams >= 3
end
