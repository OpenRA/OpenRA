--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
NorthernBridgeTrigger = { CPos.New(13,41), CPos.New(14,41), CPos.New(15,41), CPos.New(14,42), CPos.New(15,42), CPos.New(16,42) }
SouthernBridgeTrigger = { CPos.New(26,54), CPos.New(25,54), CPos.New(24,54), CPos.New(25,53), CPos.New(24,53), CPos.New(23,53) }

Apc1Units = { "c2", "c3", "c4", "c5" }

Civilians = { Civilian1, Civilian2, Civilian3, Civilian4, Civilian5, Civilian6, Civilian7, Civilian8 }
TargetActors = { Civilian1, Civilian2, Civilian3, Civilian4, Civilian5, Civilian6, Civilian7, Civilian8, CivBuilding1, CivBuilding2, CivBuilding3, CivBuilding4, CivBuilding5, CivBuilding6, CivBuilding7, CivBuilding8, CivBuilding9, CivBuilding10, CivBuilding11, CivBuilding12, CivBuilding13, CivBuilding14 }
Apc2Trigger = { GDIGunner1, GDIGunner2, GDIGunner3 }

Apc1Waypoints = { waypoint0.Location, waypoint11.Location, waypoint10.Location, waypoint8.Location, GDIBase.Location }
Apc2Waypoints = { waypoint8, waypoint7, waypoint6, waypoint5, waypoint4 }
Apc3Waypoints = { waypoint3, waypoint2, waypoint1, waypoint0, waypoint11, waypoint10, waypoint8, GDIBase }
FlightRouteTop = { waypoint4, waypoint5, waypoint6, waypoint7, waypoint8, GDIBase }
FlightRouteBottom = { waypoint3, waypoint2, waypoint1, waypoint11, waypoint10, waypoint8, GDIBase }
Hummer1Waypoints = { waypoint8, waypoint7, waypoint6, waypoint5, waypoint4, waypoint3, waypoint2, waypoint1, waypoint0, waypoint11, waypoint10, waypoint8 }

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		local apc = Actor.Create("apc", true, { Owner = GDI, Location = Apc1Waypoints[1], Cargo = Apc1Units })
		Utils.Do(Apc1Waypoints, function(waypoint)
			apc.AttackMove(waypoint)
		end)

		Trigger.OnEnteredFootprint(Apc3Trigger, function(a, id)
			if a.Owner == Nod then
				MoveAndHunt({ apc }, Apc3Waypoints)
				Trigger.RemoveFootprintTrigger(id)
			end
		end)
	end)

	Trigger.OnEnteredFootprint(NorthernBridgeTrigger, function(a, id)
		if a.Owner == Nod then
			if not CiviliansEvacuated then
				CiviliansEvacuated = true
				Utils.Do(Civilians, function(civ)
					Utils.Do(FlightRouteBottom, function(waypoint)
						civ.Move(waypoint.Location)
					end)

					Trigger.OnIdle(civ, function()
						if civ.Location == GDIBase.Location then
							Trigger.Clear(civ, "OnIdle")
						else
							civ.Move(GDIBase.Location)
						end
					end)
				end)
			end

			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(SouthernBridgeTrigger, function(a, id)
		if a.Owner == Nod then
			if not CiviliansEvacuated then
				CiviliansEvacuated = true
				Utils.Do(Civilians, function(civ)
					Utils.Do(FlightRouteTop, function(waypoint)
						civ.Move(waypoint.Location)
					end)

					Trigger.OnIdle(civ, function()
						if civ.Location == GDIBase.Location then
							Trigger.Clear(civ, "OnIdle")
						else
							civ.Move(GDIBase.Location)
						end
					end)
				end)
			end

			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnDiscovered(Convoi, function()
		MoveAndHunt({ Jeep1, Jeep2 }, Hummer1Waypoints)
	end)

	Trigger.OnAllRemovedFromWorld(Apc2Trigger, function()
		MoveAndHunt({ Convoi }, Apc2Waypoints)
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
		Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsBuggy, { EntryPointVehicle.Location, RallyPointVehicle.Location }, { EntryPointVehicle.Location })
	end)
	Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsRocket, { EntryPointRocket.Location, RallyPointRocket.Location }, { EntryPointRocket.Location })
	Reinforcements.ReinforceWithTransport(Nod, "tran", NodUnitsGunner, { EntryPointGunner.Location, RallyPointGunner.Location }, { EntryPointGunner.Location })
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(KillCivilians)
	end

	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(KillGDI)
	end
end
