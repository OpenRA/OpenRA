--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitsVehicle1 =
{
	tough = { 'bggy', 'bike', 'bike' },
	hard = { 'bggy', 'bggy', 'bike', 'bike' },
	normal = { 'bggy', 'bggy', 'bike', 'bike', 'bike' },
	easy = { 'bggy', 'bggy', 'bggy', 'bike', 'bike', 'bike', 'bike' }
}

NodUnitsVehicle2 =
{
	tough = { 'ltnk', 'ltnk' },
	hard = { 'ltnk', 'ltnk', 'ltnk' },
	normal = { 'ltnk', 'ltnk', 'ltnk', 'ltnk' },
	easy = { 'ltnk', 'ltnk', 'ltnk', 'ltnk', 'ltnk' }
}

NodUnitsGunner =
{
	tough = { 'e1', 'e1', 'e1', 'e1' },
	hard = { 'e1', 'e1', 'e1', 'e1', 'e1' },
	normal = { 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1' },
	easy = { 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1' }
}

NodUnitsRocket =
{
	tough = { 'e3', 'e3', 'e3', 'e3' },
	hard = { 'e3', 'e3', 'e3', 'e3', 'e3' },
	normal = { 'e3', 'e3', 'e3', 'e3', 'e3', 'e3', 'e3' },
	easy = { 'e3', 'e3', 'e3', 'e3', 'e3', 'e3', 'e3', 'e3', 'e3', 'e3' }
}

Gdi1Units = { 'e1', 'e1', 'e2', 'e2', 'e2' }
Obj2Units = { 'ftnk', 'e4', 'e4' }

HuntCellTriggerActivator = { CPos.New(61,34), CPos.New(60,34), CPos.New(59,34), CPos.New(58,34), CPos.New(57,34), CPos.New(56,34), CPos.New(55,34), CPos.New(61,33), CPos.New(60,33), CPos.New(59,33), CPos.New(58,33), CPos.New(57,33), CPos.New(56,33) }
DzneCellTriggerActivator = { CPos.New(50,30), CPos.New(49,30), CPos.New(48,30), CPos.New(47,30), CPos.New(46,30), CPos.New(45,30), CPos.New(50,29), CPos.New(49,29), CPos.New(48,29), CPos.New(47,29), CPos.New(46,29), CPos.New(45,29), CPos.New(50,28), CPos.New(49,28), CPos.New(48,28), CPos.New(47,28), CPos.New(46,28), CPos.New(45,28), CPos.New(50,27), CPos.New(49,27), CPos.New(46,27), CPos.New(45,27), CPos.New(50,26), CPos.New(49,26), CPos.New(48,26), CPos.New(47,26), CPos.New(46,26), CPos.New(45,26), CPos.New(50,25), CPos.New(49,25), CPos.New(48,25), CPos.New(47,25), CPos.New(46,25), CPos.New(45,25) }
Win1CellTriggerActivator = { CPos.New(47,27) }
Win2CellTriggerActivator = { CPos.New(57,57), CPos.New(56,57), CPos.New(55,57), CPos.New(57,56), CPos.New(56,56), CPos.New(55,56), CPos.New(57,55), CPos.New(56,55), CPos.New(55,55), CPos.New(57,54), CPos.New(56,54), CPos.New(55,54), CPos.New(57,53), CPos.New(56,53), CPos.New(55,53), CPos.New(57,52), CPos.New(56,52), CPos.New(55,52) }
ChnCellTriggerActivator = { CPos.New(61,52), CPos.New(60,52), CPos.New(59,52), CPos.New(58,52), CPos.New(61,51), CPos.New(60,51), CPos.New(59,51), CPos.New(58,51), CPos.New(61,50), CPos.New(60,50), CPos.New(59,50), CPos.New(58,50) }

Chn1ActorTriggerActivator = { Chn1Actor1, Chn1Actor2 }
Chn2ActorTriggerActivator = { Chn2Actor1 }
Atk1ActorTriggerActivator = { Atk1Actor1, Atk1Actor2 }
Atk2ActorTriggerActivator = { Atk2Actor1, Atk2Actor2 }
Obj2ActorTriggerActivator = { Obj2Actor0, Obj2Actor1, Obj2Actor2, Obj2Actor3, Obj2Actor4, Obj2Actor5, Obj2Actor6, Obj2Actor7, Obj2Actor8, Obj2Actor9, Obj2Actor10, Obj2Actor11, Obj2Actor12, Obj2Actor13, Obj2Actor14 }

Chn1Waypoints = { ChnEntry.Location, waypoint0.Location }
Chn2Waypoints = { ChnEntry.Location, waypoint0.Location }
Gdi5Waypoint = { waypoint1, waypoint2, waypoint3, waypoint4, waypoint5, waypoint6, waypoint7 }

OnAnyDamaged = function(actors, func)
	local triggered
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, function()
			if triggered then
				return
			end

			triggered = true
			func()
		end)
	end)
end

InsertNodUnits = function()
	NodUnitsVehicle1 = NodUnitsVehicle1[Difficulty]
	NodUnitsVehicle2 = NodUnitsVehicle2[Difficulty]
	NodUnitsGunner = NodUnitsGunner[Difficulty]
	NodUnitsRocket = NodUnitsRocket[Difficulty]

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Camera.Position = UnitsRallyVehicle2.CenterPosition

	Reinforcements.Reinforce(Nod, NodUnitsVehicle1, { UnitsEntryVehicle.Location, UnitsRallyVehicle1.Location }, 10)
	Reinforcements.Reinforce(Nod, NodUnitsVehicle2, { UnitsEntryVehicle.Location, UnitsRallyVehicle2.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsGunner, { UnitsEntryGunner.Location, UnitsRallyGunner.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsRocket, { UnitsEntryRocket.Location, UnitsRallyRocket.Location }, 25)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	StealDetonator = Nod.AddObjective("Steal the GDI nuclear detonator.")
	DestroyVillage = Nod.AddObjective("Destroy the houses of the GDI supporters\nin the village.", "Secondary", false)

	InsertNodUnits()

	Trigger.OnEnteredFootprint(HuntCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Actor.Create('flare', true, { Owner = Nod, Location = waypoint17.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllRemovedFromWorld(Obj2ActorTriggerActivator, function()
		Nod.MarkCompletedObjective(DestroyVillage)
		Media.PlaySpeechNotification(Nod, "Reinforce")
		Reinforcements.Reinforce(Nod, Obj2Units, { Obj2UnitsEntry.Location, waypoint13.Location }, 15)
	end)

	Trigger.OnEnteredFootprint(Win1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			EvacuateObjective = Nod.AddObjective("Move to the evacuation point.")
			Nod.MarkCompletedObjective(StealDetonator)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win2CellTriggerActivator, function(a, id)
		if a.Owner == Nod and EvacuateObjective then
			Nod.MarkCompletedObjective(EvacuateObjective)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	OnAnyDamaged(Chn1ActorTriggerActivator, function()
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Gdi1Units, Chn1Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, IdleHunt)
	end)

	OnAnyDamaged(Atk1ActorTriggerActivator, function()
		for type, count in pairs({ ['e2'] = 2, ['jeep'] = 1, ['e1'] = 2}) do
			Utils.Do(Utils.Take(count, GDI.GetActorsByType(type)), IdleHunt)
		end
	end)

	OnAnyDamaged(Atk2ActorTriggerActivator, function()
		for type, count in pairs({ ['e2'] = 2, ['e1'] = 2}) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Gdi5Waypoint)
		end
	end)

	OnAnyDamaged(Chn2ActorTriggerActivator, function()
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Gdi1Units, Chn2Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, IdleHunt)
	end)

	Trigger.OnEnteredFootprint(ChnCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, 'tran', nil, { ChnEntry.Location, waypoint17.Location }, nil, nil, nil)
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
