--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodStartUnitsRight =
{
	tough = { 'ltnk', 'bike', 'e1', 'e1', 'e3', 'e3' },
	hard = { 'ltnk', 'bike', 'e1', 'e1', 'e3', 'e3', 'e3' },
	normal = { 'ltnk', 'bike', 'bike', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3', 'e3' },
	easy =  { 'ltnk', 'ltnk', 'bike', 'bike', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3', 'e3' }
}

NodStartUnitsLeft =
{
	tough = { 'ltnk', 'ltnk', 'bggy', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3' },
	hard = { 'ltnk', 'ltnk', 'bggy', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3' },
	normal = { 'ltnk', 'ltnk', 'bggy', 'bggy', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3', 'e3', 'e3' },
	easy = { 'ltnk', 'ltnk', 'ltnk', 'bggy', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3', 'e3', 'e3' }
}

Chn1Units = { 'e1', 'e1', 'e1', 'e1', 'e1' }
Chn2Units = { 'e2', 'e2', 'e2', 'e2', 'e2' }
Obj2Units = { 'ltnk', 'bike', 'e1', 'e1', 'e1' }

Chn3CellTriggerActivator = { CPos.New(49,58), CPos.New(48,58), CPos.New(49,57), CPos.New(48,57), CPos.New(49,56), CPos.New(48,56), CPos.New(49,55), CPos.New(48,55) }
DzneCellTriggerActivator = { CPos.New(61,45), CPos.New(60,45), CPos.New(59,45), CPos.New(58,45), CPos.New(57,45), CPos.New(61,44), CPos.New(60,44), CPos.New(59,44), CPos.New(58,44), CPos.New(57,44), CPos.New(61,43), CPos.New(60,43), CPos.New(58,43), CPos.New(57,43), CPos.New(61,42), CPos.New(60,42), CPos.New(59,42), CPos.New(58,42), CPos.New(57,42), CPos.New(61,41), CPos.New(60,41), CPos.New(59,41), CPos.New(58,41), CPos.New(57,41) }
DetonatorArea = { CPos.New(59,43) }
EvacuationArea = { CPos.New(54,58), CPos.New(53,58), CPos.New(52,58), CPos.New(54,57), CPos.New(53,57), CPos.New(52,57), CPos.New(54,56), CPos.New(53,56), CPos.New(52,56), CPos.New(54,55), CPos.New(53,55), CPos.New(52,55) }

Grd2ActorTriggerActivator = { Guard1, Guard2, Guard3 }
Atk1ActorTriggerActivator = { Atk1Activator1, Atk1Activator2 }
Atk2ActorTriggerActivator = { Atk2Activator1, Atk2Activator2 }
Chn1ActorTriggerActivator = { Chn1Activator1, Chn1Activator2, Chn1Activator3, Chn1Activator4, Chn1Activator5 }
Chn2ActorTriggerActivator = { Chn2Activator1, Chn2Activator2, Chn2Activator3 }
GDIVillage = { Chn1Activator1, Chn1Activator2, Chn1Activator3, Chn1Activator4, Chn1Activator5, Chn2Activator1, Chn2Activator2, Chn2Activator3, Atk3Activator }

Chn1Waypoints = { ChnEntry.Location, waypoint5.Location }
Chn2Waypoints = { ChnEntry.Location, waypoint6.Location }
Gdi3Waypoints = { waypoint1, waypoint3, waypoint7, waypoint8, waypoint9 }
Gdi4Waypoints = { waypoint4, waypoint10, waypoint9, waypoint11, waypoint9, waypoint10 }
Gdi5Waypoints = { waypoint1, waypoint4 }
Gdi6Waypoints = { waypoint2, waypoints3 }

Grd2TriggerFunction = function()
	if not Grd2Switch then
		for type, count in pairs({ ['e1'] = 2, ['e2'] = 1, ['jeep'] = 1 }) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Gdi4Waypoints)
		end
		Grd2Swicth = true
	end
end

Atk1TriggerFunction = function()
	if not Atk1Switch then
		for type, count in pairs({ ['e1'] = 3, ['e2'] = 3, ['jeep'] = 1 }) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Gdi5Waypoints)
		end
		Atk1Switch = true
	end
end

Atk2TriggerFunction = function()
	if not Atk2Switch then
		for type, count in pairs({ ['mtnk'] = 1, ['jeep'] = 1 }) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Gdi6Waypoints)
		end
		Atk2Switch = true
	end
end

Atk3TriggerFunction = function()
	if not Atk3Switch then
		Atk3Switch = true
		if not CommCenter.IsDead then
			local targets = Nod.GetGroundAttackers()
			local target = targets[DateTime.GameTime % #targets + 1].CenterPosition

			if target then
				CommCenter.SendAirstrike(target, false, Facing.NorthEast + 4)
			end
		end
	end
end

InsertNodUnits = function()
	NodStartUnitsRight = NodStartUnitsRight[Difficulty]
	NodStartUnitsLeft = NodStartUnitsLeft[Difficulty]

	Camera.Position = UnitsRallyRight.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodStartUnitsLeft, { UnitsEntryLeft.Location, UnitsRallyLeft.Location }, 15)
	Reinforcements.Reinforce(Nod, NodStartUnitsRight, { UnitsEntryRight.Location, UnitsRallyRight.Location }, 15)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	StealDetonator = Nod.AddObjective("Steal the GDI nuclear detonator.")
	DestroyVillage = Nod.AddObjective("Destroy the houses of the GDI supporters\nin the village.", "Secondary", false)

	InsertNodUnits()

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		MoveAndHunt(Utils.Take(2, GDI.GetActorsByType("mtnk")), Gdi3Waypoints)
	end)

	Utils.Do(Grd2ActorTriggerActivator, function(actor)
		Trigger.OnDiscovered(actor, Grd2TriggerFunction)
	end)

	Utils.Do(Atk1ActorTriggerActivator, function(actor)
		Trigger.OnDamaged(actor, Atk1TriggerFunction)
	end)

	Utils.Do(Atk2ActorTriggerActivator, function(actor)
		Trigger.OnDamaged(actor, Atk2TriggerFunction)
	end)

	if Difficulty == "tough" then
		Trigger.OnDamaged(Atk3Activator, Atk3TriggerFunction)
	end

	Trigger.OnAllKilled(Chn1ActorTriggerActivator, function()
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Chn1Units, Chn1Waypoints, { waypoint14.Location })[2]
		Utils.Do(cargo, IdleHunt)
	end)

	Trigger.OnAllKilled(Chn2ActorTriggerActivator, function()
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Chn2Units, Chn2Waypoints, { waypoint14.Location })[2]
		Utils.Do(cargo, IdleHunt)
	end)

	Trigger.OnEnteredFootprint(Chn3CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, 'tran', nil, { ChnEntry.Location, waypoint17.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Actor.Create('flare', true, { Owner = Nod, Location = waypoint17.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllRemovedFromWorld(GDIVillage, function()
		Nod.MarkCompletedObjective(DestroyVillage)
		Media.PlaySpeechNotification(Nod, "Reinforce")
		Reinforcements.Reinforce(Nod, Obj2Units, { Obj2UnitsEntry.Location, waypoint13.Location }, 15)
	end)

	Trigger.OnEnteredFootprint(DetonatorArea, function(a, id)
		if a.Owner == Nod then
			EvacuateObjective = Nod.AddObjective("Move to the evacuation point.")
			Nod.MarkCompletedObjective(StealDetonator)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(EvacuationArea, function(a, id)
		if a.Owner == Nod and EvacuateObjective then
			Nod.MarkCompletedObjective(EvacuateObjective)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(StealDetonator)

		if EvacuateObjective then
			Nod.MarkFailedObjective(EvacuateObjective)
		end
	end
end
