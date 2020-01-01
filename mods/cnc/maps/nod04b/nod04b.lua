--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitsBuggy = { "bggy", "bggy", "bike", "bike" }
NodUnitsRocket = { "e3", "e3", "e3", "e3", "e3", "e3" }
NodUnitsGunner = { "e1", "e1", "e1", "e1", "e1", "e1" }

Apc3Trigger = { CPos.New(28,58), CPos.New(27,58), CPos.New(28,57), CPos.New(27,57), CPos.New(28,56), CPos.New(27,56), CPos.New(28,55), CPos.New(27,55), CPos.New(28,54), CPos.New(27,54), CPos.New(28,53), CPos.New(27,53) }
Civ1CellTriggerActivator = { CPos.New(24,52), CPos.New(23,52), CPos.New(22,52), CPos.New(23,51), CPos.New(22,51), CPos.New(21,51) }
Civ2CellTriggerActivator = { CPos.New(26,54), CPos.New(25,54), CPos.New(24,54), CPos.New(25,53), CPos.New(24,53), CPos.New(23,53) }

Apc1Units = { "c2", "c3", "c4", "c5" }

TargetActors = { Civilian1, Civilian2, Civilian3, Civilian4, Civilian5, Civilian6, Civilian7, Civilian8, CivBuilding1, CivBuilding2, CivBuilding3, CivBuilding4, CivBuilding5, CivBuilding6, CivBuilding7, CivBuilding8, CivBuilding9, CivBuilding10, CivBuilding11, CivBuilding12, CivBuilding13, CivBuilding14 }
Apc2Trigger = { NodGunner1, NodGunner2, NodGunner3 }

Apc1Waypoints = { waypoint0.Location, waypoint11.Location, waypoint10.Location, waypoint8.Location, waypoint9.Location }
Apc2Waypoints = { waypoint8, waypoint7, waypoint6, waypoint5, waypoint4 }
Apc3Waypoints = { waypoint3, waypoint2, waypoint1, waypoint0, waypoint11, waypoint10, waypoint8, waypoint9 }
Civ1Waypoints = { waypoint3, waypoint2, waypoint3, waypoint1, waypoint2, waypoint11, waypoint10, waypoint8, waypoint9 }
Civ2Waypoints = { waypoint3, waypoint2, waypoint1, waypoint11, waypoint10, waypoint8, waypoint9 }
Hummer1Waypoints = { waypoint8, waypoint7, waypoint6, waypoint5, waypoint4, waypoint3, waypoint2, waypoint1, waypoint0, waypoint11, waypoint10, waypoint8 }

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.ReinforceWithTransport(GDI, "apc", Apc1Units, Apc1Waypoints, nil,
			function(transport, cargo)
				Utils.Do(cargo, IdleHunt)
			end)
	end)

	Trigger.OnEnteredFootprint(Civ2CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			for type, count in pairs({ ["c6"] = 1, ["c7"] = 1, ["c8"] = 1, ["c9"] = 1 }) do
				MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Civ2Waypoints)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Civ1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			for type, count in pairs({ ["c2"] = 1, ["c3"] = 1, ["c4"] = 1, ["c5"] = 1 }) do
				MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Civ1Waypoints)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnDiscovered(Convoi, function()
		MoveAndHunt(Utils.Take(2, GDI.GetActorsByType("jeep")), Hummer1Waypoints)
	end)

	Trigger.OnAllRemovedFromWorld(Apc2Trigger, function()
		MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("apc")), Apc2Waypoints)
	end)

	Trigger.OnEnteredFootprint(Apc3Trigger, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("apc"), Apc3Waypoints))
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllRemovedFromWorld(TargetActors, function()
		Nod.MarkCompletedObjective(KillCivilians)
	end)

	InitObjectives(Nod)

	KillCivilians = Nod.AddObjective("Destroy the village and kill all civilians.")
	KillGDI = Nod.AddObjective("Kill all GDI units in the area.", "Secondary", false)

	Camera.Position = CameraPoint.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsBuggy, { EntryPointVehicle.Location, RallyPointVehicle.Location }, { EntryPointVehicle.Location }, nil, nil)
	end)
	Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsRocket, { EntryPointRocket.Location, RallyPointRocket.Location }, { EntryPointRocket.Location }, nil, nil)
	Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsGunner, { EntryPointGunner.Location, RallyPointGunner.Location }, { EntryPointGunner.Location }, nil, nil)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(KillCivilians)
	end

	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(KillGDI)
	end
end
