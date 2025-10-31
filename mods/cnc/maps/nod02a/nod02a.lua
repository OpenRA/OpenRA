--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnits = { "bggy", "e1", "e1", "e1", "e1", "e1", "bggy", "e1", "e1", "e1", "bggy" }
NodBaseBuildings = { "hand", "fact", "nuke" }

GDIBase = { Refinery, Yard, Barracks, Plant, Silo1, Silo2 }

GDIWaypoints1 = { waypoint0, waypoint1, waypoint2, waypoint3 }
GDIWaypoints2 = { waypoint0, waypoint1, waypoint4, waypoint5, waypoint6, waypoint7, waypoint9 }

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

	GDIObjective = AddPrimaryObjective(GDI, "")
	BuildBaseObjective = AddPrimaryObjective(Nod, "build-base")
	DestroyGDI = AddPrimaryObjective(Nod, "destroy-gdi-units")

	Utils.Do({ Refinery, Yard }, function(actor)
		Trigger.OnDamaged(actor, function()
			if not Grd2TriggerSwitch then
				Grd2TriggerSwitch = true
				Utils.Do(GetAttackers(5), IdleHunt)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(25), function()
		MoveAndHunt(GetAttackers(2), GDIWaypoints1)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(20), function()
		MoveAndHunt(GetAttackers(3), GDIWaypoints2)
	end)

	Trigger.OnKilled(Guard1, function()
		MoveAndHunt(GetAttackers(3), GDIWaypoints2)
	end)

	Trigger.OnKilled(Guard4, function()
		MoveAndHunt(GetAttackers(2), GDIWaypoints1)
	end)

	Trigger.OnAllKilled({ Guard2, Guard3 }, function()
		MoveAndHunt(GetAttackers(2), GDIWaypoints1)
	end)

	Trigger.OnDamaged(Harvester, function()
		if Atk5TriggerSwitch then
			return
		end

		Atk5TriggerSwitch = true
		MoveAndHunt(GetAttackers(3), GDIWaypoints2)
	end)

	Trigger.OnAllRemovedFromWorld(GDIBase, function()
		Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
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
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(DestroyGDI)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(BuildBaseObjective) and CheckForBase(Nod, NodBaseBuildings) then
		Nod.MarkCompletedObjective(BuildBaseObjective)
	end
end
