--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnits = { "bggy", "e1", "e1", "e1", "e1", "e1", "bggy", "e1", "e1", "e1", "bggy" }
NodBaseBuildings = { "hand", "fact", "nuke" }

GDIBase = { Refinery, Barracks, Powerplant, Yard }
Guards = { Guard1, Guard2, Guard3, Guard4, Guard5, Guard6, Guard7 }

Atk1CellTriggerActivator = { CPos.New(45,37), CPos.New(44,37), CPos.New(45,36), CPos.New(44,36), CPos.New(45,35), CPos.New(44,35), CPos.New(45,34), CPos.New(44,34) }
Atk4CellTriggerActivator = { CPos.New(50,47), CPos.New(49,47), CPos.New(48,47), CPos.New(47,47), CPos.New(46,47), CPos.New(45,47), CPos.New(44,47), CPos.New(43,47), CPos.New(42,47), CPos.New(41,47), CPos.New(40,47), CPos.New(39,47), CPos.New(38,47), CPos.New(37,47), CPos.New(50,46), CPos.New(49,46), CPos.New(48,46), CPos.New(47,46), CPos.New(46,46), CPos.New(45,46), CPos.New(44,46), CPos.New(43,46), CPos.New(42,46), CPos.New(41,46), CPos.New(40,46), CPos.New(39,46), CPos.New(38,46) }

Atk1Waypoints = { waypoint2, waypoint4, waypoint5, waypoint6 }
Atk2Waypoints = { waypoint2, waypoint5, waypoint7, waypoint6 }
Atk3Waypoints = { waypoint2, waypoint4, waypoint5, waypoint9 }
Atk4Waypoints = { waypoint0, waypoint8, waypoint9 }
Pat1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3 }

GetAttackers = function(amount)
	local units = GDI.GetActorsByType("e1")

	if amount > #units then
		return units
	end

	return Utils.Take(amount, units)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	BuildBase = Nod.AddObjective("Build a base.")
	DestroyGDI = Nod.AddObjective("Destroy the GDI base.")
	GDIObjective = GDI.AddObjective("Kill all enemies.")

	Utils.Do(Guards, function(actor)
		Trigger.OnDamaged(actor, function()
			if Atk3TriggerSwitch then
				return
			end

			Atk3TriggerSwitch = true
			MoveAndHunt(GetAttackers(4), Atk3Waypoints)
		end)
	end)

	Trigger.OnAllRemovedFromWorld(GDIBase, function()
		Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Seconds(40), function()
		MoveAndHunt(GetAttackers(3), Atk2Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(15), function()
		MoveAndHunt(GetAttackers(3), Atk2Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(20), function()
		MoveAndHunt(GetAttackers(3), Atk3Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Seconds(50), function()
		MoveAndHunt(GetAttackers(3), Atk4Waypoints)
	end)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		MoveAndHunt(GetAttackers(3), Pat1Waypoints)
	end)

	Trigger.OnEnteredFootprint(Atk1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(GetAttackers(5), Atk1Waypoints)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Atk4CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MoveAndHunt(GetAttackers(3), Atk2Waypoints)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(0, function()
		Utils.Do(GDI.GetActorsByType("e1"), function(unit)
			RebuildUnit({ unit }, GDI, Barracks)
		end)
	end)

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
	Reinforcements.Reinforce(Nod, { "mcv" }, { McvEntry.Location, McvRally.Location })
end

Tick = function()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(DestroyGDI)
	end

	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(BuildBase) and CheckForBase(Nod, NodBaseBuildings) then
		Nod.MarkCompletedObjective(BuildBase)
	end
end
