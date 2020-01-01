--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
NodStartUnitsVehicle = { 'bggy', 'bggy', 'ltnk', 'ltnk', 'ltnk', 'bike', 'bike'}
NodStartUnitsRight = { 'e1', 'e1', 'e1', 'e1' }
NodStartUnitsMiddle = { 'e6', 'e6', 'e6', 'e6', 'e3', 'e3' }
NodStartUnitsLeft = { 'e4', 'e4', 'e4', 'e4' }

Win1CellTriggerActivator = { CPos.New(24,22) }
Win2CellTriggerActivator = { CPos.New(20,55), CPos.New(19,55), CPos.New(20,54), CPos.New(19,54), CPos.New(20,53), CPos.New(19,53), CPos.New(20,52), CPos.New(19,52) }
DzneCellTriggerActivator = { CPos.New(26,24), CPos.New(25,24), CPos.New(24,24), CPos.New(23,24), CPos.New(22,24), CPos.New(26,23), CPos.New(25,23), CPos.New(24,23), CPos.New(23,23), CPos.New(22,23), CPos.New(26,22), CPos.New(25,22), CPos.New(23,22), CPos.New(22,22), CPos.New(25,21), CPos.New(24,21), CPos.New(23,21), CPos.New(22,21), CPos.New(25,20), CPos.New(24,20), CPos.New(23,20), CPos.New(22,20) }
ChinCellTriggerActivator = { CPos.New(31,49), CPos.New(30,49), CPos.New(29,49), CPos.New(28,49), CPos.New(27,49), CPos.New(26,49), CPos.New(25,49), CPos.New(24,49), CPos.New(23,49), CPos.New(22,49), CPos.New(21,49), CPos.New(20,49), CPos.New(31,48), CPos.New(30,48), CPos.New(29,48), CPos.New(28,48), CPos.New(27,48), CPos.New(26,48), CPos.New(25,48), CPos.New(24,48), CPos.New(23,48), CPos.New(22,48), CPos.New(21,48), CPos.New(20,48), CPos.New(31,47), CPos.New(30,47), CPos.New(29,47), CPos.New(28,47), CPos.New(27,47), CPos.New(26,47), CPos.New(25,47), CPos.New(24,47), CPos.New(23,47), CPos.New(22,47), CPos.New(21,47), CPos.New(20,47) }

Atk2ActorTriggerActivator = { Atk2Actor1, Atk2Actor2, Atk2Actor3, Atk2Actor4, Atk2Actor5, Atk2Actor6 }
BuildingsToCapture = { Barracks, Factory, Yard }

Gdi1Units = { 'e1', 'e1', 'e1', 'e2', 'e2' }
Gdi2Units = { 'e1', 'e1', 'e3', 'e3', 'e3' }
Gdi3Units = { 'jeep', 'jeep', 'e3', 'e3' }
Gdi4Units = { 'mtnk', 'e2', 'e2', 'e2', 'e2' }
Gdi5Units = { 'e1', 'e2', 'e2', 'e3', 'e3' }

AllUnits = { Gdi1Units, Gdi2Units, Gdi3Units, Gdi4Units, Gdi5Units }
Grd1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5 }

ProductionDelay = DateTime.Minutes(5)

ProdTriggerFunction = function()
	local units = AllUnits[DateTime.GameTime % #AllUnits + 1]
	Utils.Do(units, function(unitType)
		if (unitType == 'jeep' or unitType == 'mtnk') and not Factory.IsDead and Factory.Owner == GDI then
			Factory.Build({ unitType })
		elseif (unitType == 'e1' or unitType == 'e2' or unitType == 'e3') and not Barracks.IsDead and Barracks.Owner == GDI then
			Barracks.Build({ unitType })
		end
	end)

	Utils.Do(Utils.Take(5, GDI.GetGroundAttackers()), IdleHunt)

	Trigger.AfterDelay(ProductionDelay, ProdTriggerFunction)
end

InsertNodUnits = function()
	Camera.Position = UnitsRallyRight.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodStartUnitsVehicle, { UnitsEntryMiddle.Location, UnitsRallyMiddle.Location }, 30)
	Reinforcements.Reinforce(Nod, NodStartUnitsMiddle, { UnitsEntryMiddle.Location, UnitsRallyMiddle.Location }, 15)
	Reinforcements.Reinforce(Nod, NodStartUnitsLeft, { UnitsEntryLeft.Location, UnitsRallyLeft.Location }, 15)
	Reinforcements.Reinforce(Nod, NodStartUnitsRight, { UnitsEntryRight.Location, UnitsRallyRight.Location }, 15)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	NodObjective1 = Nod.AddObjective("Steal the GDI nuclear detonator.")
	InfiltrateObjective = Nod.AddObjective("Infiltrate the barracks, weapon factory and\nthe construction yard.", "Secondary", false)

	InsertNodUnits()

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		for type, count in pairs({ ['e1'] = 2, ['e2'] = 3 }) do
			MoveAndHunt(Utils.Take(count, GDI.GetActorsByType(type)), Grd1Waypoints)
		end
	end)

	Trigger.OnAllKilled(Atk2ActorTriggerActivator, function()
		for type, count in pairs({ ['e1'] = 2, ['e2'] = 3 , ['jeep'] = 1}) do
			Utils.Do(Utils.Take(count, GDI.GetActorsByType(type)), IdleHunt)
		end
	end)

	Trigger.OnEnteredFootprint(ChinCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, 'tran', nil, { ChnEntry.Location, waypoint10.Location }, nil, nil, nil)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Actor.Create('flare', true, { Owner = Nod, Location = waypoint10.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			EvacuateObjective = Nod.AddObjective("Move to the evacuation point.")
			Nod.MarkCompletedObjective(NodObjective1)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win2CellTriggerActivator, function(a, id)
		if a.Owner == Nod and EvacuateObjective then
			Nod.MarkCompletedObjective(EvacuateObjective)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(ProductionDelay, ProdTriggerFunction)

	Trigger.OnAnyKilled(BuildingsToCapture, function()
		if not Nod.IsObjectiveCompleted(InfiltrateObjective) then
			Nod.MarkFailedObjective(InfiltrateObjective)
		end
	end)

	Utils.Do(BuildingsToCapture, function(building)
		local captured = 0
		Trigger.OnCapture(building, function()
			captured = captured + 1

			if captured == 3 then
				Nod.MarkCompletedObjective(InfiltrateObjective)
			end
		end)
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
