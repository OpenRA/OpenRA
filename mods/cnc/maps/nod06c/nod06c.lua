--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

Gdi1Units = { 'e1', 'e1', 'e1', 'e2', 'e2' }
Gdi2Units = { 'e1', 'e1', 'e3', 'e3', 'e3' }
Gdi3Units = { 'jeep', 'jeep', 'e3', 'e3' }
Gdi4Units = { 'mtnk', 'e2', 'e2', 'e2', 'e2' }
Gdi5Units = { 'e1', 'e2', 'e2', 'e3', 'e3' }

AllUnits = { Gdi1Units, Gdi2Units, Gdi3Units, Gdi4Units, Gdi5Units }
Grd1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5 }

Atk1TriggerFunctionTime = DateTime.Seconds(3)
ProdTriggerFunctionTime = DateTime.Minutes(5)

Atk1TriggerFunction = function()
	for type, count in pairs({ ['e1'] = 2, ['e2'] = 3 }) do
		MyActors = Utils.Take(count, enemy.GetActorsByType(type))
		Utils.Do(MyActors, function(actor)
			MovementAndHunt(actor, Grd1Waypoints)
		end)
	end
end

Atk2TriggerFunction = function()
	for type, count in pairs({ ['e1'] = 2, ['e2'] = 3 , ['jeep'] = 1}) do
		MyActors = Utils.Take(count, enemy.GetActorsByType(type))
		Utils.Do(MyActors, function(actor)
			IdleHunt(actor)
		end)
	end
end

ProdTriggerFunction = function()
	local Units = AllUnits[DateTime.GameTime % #AllUnits + 1]

	Utils.Do(Units, function(UnitType)
		if (UnitType == 'jeep' or UnitType == 'mtnk') and not Factory.IsDead and Factory.Owner == enemy then
			Factory.Build({UnitType})
		elseif (UnitType == 'e1' or UnitType == 'e2' or UnitType == 'e3') and not Barracks.IsDead and Barracks.Owner == enemy then
			Barracks.Build({UnitType})
		end
	end)

	local list = enemy.GetGroundAttackers()
	local counter = 1
	while counter <= 5 do
		counter = counter + 1
		if counter <= #list then
			IdleHunt(list[counter])
		end
	end

	Trigger.AfterDelay(ProdTriggerFunctionTime, ProdTriggerFunction)
end

MovementAndHunt = function(unit, waypoints)
	if unit ~= nil then
		Utils.Do(waypoints, function(waypoint)
			unit.AttackMove(waypoint.Location)
		end)
		IdleHunt(unit)
	end
end

InsertNodUnits = function()
	Camera.Position = UnitsRallyRight.CenterPosition
	
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, NodStartUnitsVehicle, { UnitsEntryMiddle.Location, UnitsRallyMiddle.Location }, 30)
	Reinforcements.Reinforce(player, NodStartUnitsMiddle, { UnitsEntryMiddle.Location, UnitsRallyMiddle.Location }, 15)
	Reinforcements.Reinforce(player, NodStartUnitsLeft, { UnitsEntryLeft.Location, UnitsRallyLeft.Location }, 15)
	Reinforcements.Reinforce(player, NodStartUnitsRight, { UnitsEntryRight.Location, UnitsRallyRight.Location }, 15)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	NodObjective1 = player.AddPrimaryObjective("Steal the GDI nuclear detonator.")
	NodObjective3 = player.AddSecondaryObjective("Infiltrate the barracks, weapon factory and \nthe construction yard.")
	GDIObjective = enemy.AddPrimaryObjective("Stop the Nod taskforce from escaping with the detonator.")

	InsertNodUnits()

	Trigger.AfterDelay(Atk1TriggerFunctionTime, Atk1TriggerFunction)

	Trigger.OnAllKilled(Atk2ActorTriggerActivator, Atk2TriggerFunction)

	Trigger.OnEnteredFootprint(ChinCellTriggerActivator, function(a, id)
		if a.Owner == player then
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ChnEntry.Location, waypoint10.Location }, nil, nil, nil)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == player then
			Actor.Create('flare', true, { Owner = player, Location = waypoint10.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win1CellTriggerActivator, function(a, id)
		if a.Owner == player then
			NodObjective2 = player.AddPrimaryObjective("Move to the evacuation point.")
			player.MarkCompletedObjective(NodObjective1)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win2CellTriggerActivator, function(a, id)
		if a.Owner == player and NodObjective2 then
			player.MarkCompletedObjective(NodObjective2)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(ProdTriggerFunctionTime, ProdTriggerFunction)
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime % 5 == 0 and Barracks.Owner == player and Factory.Owner == player and Yard.Owner == player then
		player.MarkCompletedObjective(NodObjective3)
	end

	if DateTime.GameTime % 7 == 0 and not player.IsObjectiveCompleted(NodObjective3) and (Barracks.IsDead or Factory.IsDead or Yard.IsDead) then
		player.MarkFailedObjective(NodObjective3)
	end
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end
