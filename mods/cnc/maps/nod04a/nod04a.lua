--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitsBuggy = { "bggy", "bggy", "bggy", "bggy", "bggy" }
NodUnitsBikes = { "bike", "bike", "bike" }
NodUnitsGunner = { "e1", "e1", "e1", "e1", "e1", "e1" }
NodUnitsRocket = { "e3", "e3", "e3", "e3", "e3", "e3" }

Atk6Units = { "c1", "c2", "c3" }
Atk5Units = { "e1", "e1", "e2", "e2" }
Atk1Units = { "e1", "e1" }
JeepReinforcements = { "jeep" }
GDIUnits = { "e1", "e1", "e2" }
ApcUnits = { "e1", "e1", "e2", "e2" }

Spawnpoint = { waypoint0.Location }
Atk6WaypointsPart1 = { waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint4.Location }
Atk6WaypointsPart2 = { waypoint7.Location, waypoint8.Location, waypoint7.Location }
Atk5Waypoints = { waypoint0.Location, waypoint9.Location}
Atk3Waypoints = { waypoint0 }
Atk2Waypoints = { waypoint6 }
GcivWaypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4 }
Atk1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5, waypoint6 }
Atk4Waypoints = { waypoint0, waypoint9 }

Atk6ActorTriggerActivator = { Civilian1, Civilian2 }
Atk5ActorTriggerActivator = { Soldier1, Soldier2, Soldier3, Actor105 }
WinActorTriggerActivator = { GDICiv1, GDICiv2, GDICiv3, GDICiv4, GDICiv5, GDICiv6, GDICiv7, GDICiv8, GDICiv9, GDICiv11, GDICiv12, GDICiv13 }
GcivActors = { Gcvi1, Gciv2, GDICiv2, GDICiv9, GDICiv10, GDICiv11 }

Atk2CellTriggerActivator = { CPos.New(41,22), CPos.New(40,22), CPos.New(39,22), CPos.New(41,21), CPos.New(40,21), CPos.New(39,21) }
Atk3CellTriggerActivator = { CPos.New(18,18), CPos.New(17,18), CPos.New(16,18), CPos.New(15,18), CPos.New(14,18), CPos.New(13,18), CPos.New(12,18), CPos.New(11,18), CPos.New(24,17), CPos.New(23,17), CPos.New(22,17), CPos.New(21,17), CPos.New(20,17), CPos.New(19,17), CPos.New(17,17), CPos.New(16,17), CPos.New(15,17), CPos.New(14,17), CPos.New(13,17), CPos.New(12,17), CPos.New(11,17) }
Atk4CellTriggerActivator = { CPos.New(29,28), CPos.New(28,28), CPos.New(29,27), CPos.New(28,27), CPos.New(29,26), CPos.New(28,26), CPos.New(29,25), CPos.New(28,25), CPos.New(29,24), CPos.New(28,24), CPos.New(29,23), CPos.New(28,23), CPos.New(29,22), CPos.New(28,22) }
GcivCellTriggerActivator = { CPos.New(51,17), CPos.New(50,17), CPos.New(49,17), CPos.New(48,17), CPos.New(47,17), CPos.New(46,17), CPos.New(45,17), CPos.New(44,17), CPos.New(43,17), CPos.New(42,17), CPos.New(41,17), CPos.New(40,17), CPos.New(39,17), CPos.New(38,17), CPos.New(37,17), CPos.New(36,17), CPos.New(35,17), CPos.New(52,16), CPos.New(51,16), CPos.New(50,16), CPos.New(49,16), CPos.New(48,16), CPos.New(47,16), CPos.New(46,16), CPos.New(45,16), CPos.New(44,16), CPos.New(43,16), CPos.New(42,16), CPos.New(41,16), CPos.New(40,16), CPos.New(39,16), CPos.New(38,16), CPos.New(37,16), CPos.New(36,16), CPos.New(35,16) }
DelxCellTriggerActivator = { CPos.New(42,20), CPos.New(41,20), CPos.New(40,20), CPos.New(39,20), CPos.New(38,20) }
DelyCellTriggerActivator = { CPos.New(31,28), CPos.New(30,28), CPos.New(31,27), CPos.New(30,27), CPos.New(31,26), CPos.New(30,26), CPos.New(31,25), CPos.New(30,25), CPos.New(31,24), CPos.New(30,24) }
DelzCellTriggerActivator = { CPos.New(18,20), CPos.New(17,20), CPos.New(16,20), CPos.New(15,20), CPos.New(14,20), CPos.New(13,20), CPos.New(12,20), CPos.New(11,20), CPos.New(25,19), CPos.New(24,19), CPos.New(23,19), CPos.New(22,19), CPos.New(21,19), CPos.New(20,19), CPos.New(19,19), CPos.New(18,19), CPos.New(17,19), CPos.New(16,19), CPos.New(15,19), CPos.New(14,19), CPos.New(13,19), CPos.New(12,19), CPos.New(11,19), CPos.New(25,18), CPos.New(24,18), CPos.New(23,18), CPos.New(22,18), CPos.New(21,18), CPos.New(20,18), CPos.New(19,18) }

NodCiviliansActors = { NodCiv1, NodCiv2, NodCiv3, NodCiv4, NodCiv5, NodCiv6, NodCiv7, NodCiv8, NodCiv9 }

SendJeepReinforcements = function()
	if not PreventJeepReinforcements then
		local units = Reinforcements.Reinforce(GDI, JeepReinforcements, Spawnpoint, 15)
		MoveAndHunt(units, Atk2Waypoints)
	end
end

SendGDIReinforcements = function()
	if not PreventGDIReinforcements then
		local units = Reinforcements.Reinforce(GDI, GDIUnits, Spawnpoint, 15)
		MoveAndHunt(units, Atk4Waypoints)
	end
end

SendApcReinforcements = function()
	if not PreventApcReinforcements then
		Reinforcements.ReinforceWithTransport(GDI, "apc", ApcUnits, Atk5Waypoints, nil,
		function(transport, cargo)
			transport.UnloadPassengers()
			Utils.Do(cargo, IdleHunt)
		end, IdleHunt)
	end
end

CreateCivilians = function(actor, discoverer)
	Utils.Do(NodCiviliansActors, function(actor)
		actor.Owner = Nod
	end)

	ProtectCivilians = Nod.AddPrimaryObjective("Protect the civilians that support Nod.")
	Trigger.OnAllKilled(NodCiviliansActors, function()
		Nod.MarkFailedObjective(ProtectCivilians)
	end)

	Utils.Do(GcivActors, function(actor)
		if not actor.IsDead then
			actor.AttackMove(waypoint7.Location)
			actor.AttackMove(waypoint8.Location)
			IdleHunt(actor)
		end
	end)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	NodSupporter = Player.GetPlayer("NodSupporter")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	Trigger.OnAnyKilled(Atk6ActorTriggerActivator, function()
		Reinforcements.ReinforceWithTransport(GDI, "apc", Atk6Units, Atk6WaypointsPart1, Atk6WaypointsPart2,
			function(transport, cargo)
				Utils.Do(cargo, IdleHunt)
			end, IdleHunt)
	end)

	Utils.Do(Atk5ActorTriggerActivator, function(actor)
		Trigger.OnDamaged(actor, function()
			if Atk5TriggerSwitch then
				return
			end

			Atk5TriggerSwitch = true
			Reinforcements.ReinforceWithTransport(GDI, "apc", Atk5Units, Atk5Waypoints, nil,
				function(transport, cargo)
					transport.UnloadPassengers()
					Utils.Do(cargo, IdleHunt)
				end, IdleHunt)
		end)
	end)

	Trigger.OnEnteredFootprint(Atk3CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Trigger.RemoveFootprintTrigger(id)

			for type, count in pairs({ ["e1"] = 3, ["e2"] = 2, ["mtnk"] = 1 }) do
				MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Atk3Waypoints)
			end
		end
	end)

	Trigger.OnEnteredFootprint(Atk2CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(Utils.Take(1, GDI.GetActorsByType("jeep")), Atk2Waypoints)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(GcivCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(GcivActors, GcivWaypoints)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Atk4CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			for type, count in pairs({ ["e1"] = 2,["e2"] = 1 }) do
				MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Atk4Waypoints)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		local units = Reinforcements.Reinforce(GDI, Atk1Units, Spawnpoint, 15)
		MoveAndHunt(units, Atk1Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Seconds(50), SendJeepReinforcements)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), SendGDIReinforcements)
	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), SendApcReinforcements)

	Trigger.OnEnteredFootprint(DelxCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			PreventJeepReinforcements = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DelyCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			PreventGDIReinforcements = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DelzCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			PreventApcReinforcements = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnPlayerDiscovered(NodSupporter, CreateCivilians)

	Trigger.OnAllKilled(WinActorTriggerActivator, function()
		Nod.MarkCompletedObjective(KillGDI)

		if ProtectCivilians then
			Nod.MarkCompletedObjective(ProtectCivilians)
		end
	end)

	KillGDI = Nod.AddObjective("Kill all civilian GDI supporters.")

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnitsBuggy, { UnitsEntryBuggy.Location, UnitsRallyBuggy.Location }, 11)
	Reinforcements.Reinforce(Nod, NodUnitsBikes, { UnitsEntryBikes.Location, UnitsRallyBikes.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsGunner, { UnitsEntryGunner.Location, UnitsRallyGunner.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsRocket, { UnitsEntryRocket.Location, UnitsRallyRocket.Location }, 15)

	Camera.Position = waypoint6.CenterPosition
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(KillGDI)
	end
end
