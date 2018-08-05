--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

Gdi1Units = { ['e1'] = 3, ['e2'] = 1 }
Gdi2Units = { ['e1'] = 2, ['e2'] = 1 }
Gdi3Units = { ['jeep'] = 1 }
Gdi4Units = { ['mtnk'] = 1 }
Gdi5Units = { ['e1'] = 1, ['e2'] = 2 }
Gdi6Units = { ['e1'] = 3 }
Gdi7Units = { ['e2'] = 2 }
Gdi8Units = { ['e2'] = 5 }

AllUnits = { Gdi1Units, Gdi2Units, Gdi3Units, Gdi4Units, Gdi5Units, Gdi6Units, Gdi7Units, Gdi8Units }

AirstrikeDelay = DateTime.Minutes(1) + DateTime.Seconds(40)
YyyyTriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(30)
ZzzzTriggerFunctionTime = DateTime.Minutes(2) + DateTime.Seconds(30)
Grd1TriggerFunctionTime = DateTime.Seconds(3)
Atk2TriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(10)
Atk3TriggerFunctionTime = DateTime.Minutes(3) + DateTime.Seconds(10)
Atk4TriggerFunctionTime = DateTime.Minutes(4) + DateTime.Seconds(40)
Atk6TriggerFunctionTime = DateTime.Minutes(2) + DateTime.Seconds(30)

DelyCellTriggerActivator = { CPos.New(29,30), CPos.New(28,30), CPos.New(27,30), CPos.New(26,30), CPos.New(25,30), CPos.New(24,30), CPos.New(23,30), CPos.New(22,30), CPos.New(21,30), CPos.New(29,29), CPos.New(28,29), CPos.New(27,29), CPos.New(26,29), CPos.New(25,29), CPos.New(24,29), CPos.New(23,29), CPos.New(22,29) }
DelzCellTriggerActivator = { CPos.New(29,27), CPos.New(28,27), CPos.New(27,27), CPos.New(26,27), CPos.New(25,27), CPos.New(24,27), CPos.New(29,26), CPos.New(28,26), CPos.New(27,26), CPos.New(26,26), CPos.New(25,26), CPos.New(24,26) }
Atk5CellTriggerActivator = { CPos.New(10,33), CPos.New(9,33), CPos.New(8,33), CPos.New(9,32), CPos.New(8,32), CPos.New(7,32), CPos.New(8,31), CPos.New(7,31), CPos.New(6,31) }
Atk1CellTriggerActivator = { CPos.New(10,33), CPos.New(9,33), CPos.New(8,33), CPos.New(9,32), CPos.New(8,32), CPos.New(7,32), CPos.New(8,31), CPos.New(7,31), CPos.New(6,31) }

Gdi1Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4 }
Gdi2Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4, waypoint5 }
Gdi3Waypoints = { waypoint0, waypoint1, waypoint2 }
Gdi5Waypoints = { waypoint0, waypoint1, waypoint3, waypoint1, waypoint6 }
Gdi11Waypoints = { waypoint0, waypoint1, waypoint3, waypoint4, waypoint7, waypoint8 }
Gdi12Waypoints = { waypoint0, waypoint1, waypoint3, waypoint11, waypoint12 }

AllWaypoints = { Gdi1Waypoints, Gdi2Waypoints, Gdi3Waypoints, Gdi5Waypoints, Gdi11Waypoints, Gdi12Waypoints }

PrimaryTargets = { Tower1, Tower2, CommCenter, Silo1, Silo2, Silo3, Refinery, Barracks, Plant1, Plant2, Yard, Factory }

GDIStartUnits = { }

SendGDIAirstrike = function()
	if not CommCenter.IsDead and CommCenter.Owner == enemy then
		local target = getAirstrikeTarget()

		if target then
			CommCenter.SendAirstrike(target, false, Facing.NorthEast + 4)
			Trigger.AfterDelay(AirstrikeDelay, SendGDIAirstrike)
		else
			Trigger.AfterDelay(AirstrikeDelay/4, SendGDIAirstrike)
		end
	end
end

YyyyTriggerFunction = function()
	if not YyyyTriggerSwitch then
		for type, count in pairs(Gdi2Units) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				WaypointMovementAndHunt(actor, Gdi2Waypoints)
			end)
		end
	end
end

ZzzzTriggerFunction = function()
	if not ZzzzTriggerSwitch then
		for type, count in pairs(Gdi1Units) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				WaypointMovementAndHunt(actor, Gdi1Waypoints)
			end)
		end
	end
end

Grd1TriggerFunction = function()
	MyActors = Utils.Take(2, enemy.GetActorsByType('jeep'))
	Utils.Do(MyActors, function(actor)
		WaypointMovementAndHunt(actor, Gdi5Waypoints)
	end)
end

Atk5TriggerFunction = function()
	WaypointMovementAndHunt(enemy.GetActorsByType('mtnk')[1], Gdi12Waypoints)
end

Atk2TriggerFunction = function()
	for type, count in pairs(Gdi1Units) do
		MyActors = Utils.Take(count, enemy.GetActorsByType(type))
		Utils.Do(MyActors, function(actor)
			WaypointMovementAndHunt(actor, Gdi1Waypoints)
		end)
	end
end

Atk3TriggerFunction = function()
	for type, count in pairs(Gdi2Units) do
		MyActors = Utils.Take(count, enemy.GetActorsByType(type))
		Utils.Do(MyActors, function(actor)
			WaypointMovementAndHunt(actor, Gdi2Waypoints)
		end)
	end
end

Atk4TriggerFunction = function()
	WaypointMovementAndHunt(enemy.GetActorsByType('jeep')[1], Gdi3Waypoints)
end

Atk6TriggerFunction = function()
	WaypointMovementAndHunt(enemy.GetActorsByType('mtnk')[1], Gdi2Waypoints)
end

Atk1TriggerFunction = function()
	local cargo = Reinforcements.ReinforceWithTransport(enemy, 'tran', GDIReinforceUnits, { waypoint9.Location, waypoint26.Location }, { waypoint9.Location })[2]
	Utils.Do(cargo, IdleHunt)
end

AutoTriggerFunction = function()
	local units = AllUnits[DateTime.GameTime % #AllUnits + 1]
	local waypoints = AllWaypoints[DateTime.GameTime % #AllWaypoints + 1]

	for type, count in pairs(units) do
		MyActors = Utils.Take(count, enemy.GetActorsByType(type))
		Utils.Do(MyActors, function(actor)
			WaypointMovementAndHunt(actor, waypoints)
		end)
	end
end

HuntTriggerFunction = function()
	local list = enemy.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

WaypointMovementAndHunt = function(unit, waypoints)
	if unit ~= nil then
		Utils.Do(waypoints, function(waypoint)
			unit.AttackMove(waypoint.Location)
		end)
		IdleHunt(unit)
	end
end

InsertNodUnits = function()
	Camera.Position = UnitsEntry.CenterPosition

	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, NodUnitsVehicle, { UnitsEntry.Location, UnitsRallyVehicle.Location }, 1)
	Reinforcements.Reinforce(player, NodUnitsRocket, { UnitsEntry.Location, UnitsRallyRocket.Location }, 50)
	Reinforcements.Reinforce(player, NodUnitsGunner, { UnitsEntry.Location, UnitsRallyGunner.Location }, 50)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Reinforcements.Reinforce(player, { 'mcv' }, { UnitsEntry.Location, UnitsRallyMCV.Location })
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	InsertNodUnits()

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

	NodObjective1 = player.AddPrimaryObjective("Build 3 SAMs.")
	NodObjective2 = player.AddPrimaryObjective("Destroy the GDI base.")
	GDIObjective = enemy.AddPrimaryObjective("Kill all enemies.")

	Trigger.AfterDelay(AirstrikeDelay, SendGDIAirstrike)
	Trigger.AfterDelay(YyyyTriggerFunctionTime, YyyyTriggerFunction)
	Trigger.AfterDelay(ZzzzTriggerFunctionTime, ZzzzTriggerFunction)

	Trigger.OnEnteredFootprint(DelyCellTriggerActivator, function(a, id)
		if a.Owner == player then
			YyyyTriggerSwitch = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DelzCellTriggerActivator, function(a, id)
		if a.Owner == player then
			ZzzzTriggerSwitch = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(Grd1TriggerFunctionTime, Grd1TriggerFunction)

	Trigger.OnEnteredFootprint(Atk5CellTriggerActivator, function(a, id)
		if a.Owner == player then
			Atk5TriggerFunction()
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(Atk2TriggerFunctionTime, Atk2TriggerFunction)
	Trigger.AfterDelay(Atk3TriggerFunctionTime, Atk3TriggerFunction)
	Trigger.AfterDelay(Atk4TriggerFunctionTime, Atk4TriggerFunction)
	Trigger.AfterDelay(Atk6TriggerFunctionTime, Atk6TriggerFunction)

	Trigger.OnEnteredFootprint(Atk1CellTriggerActivator, function(a, id)
		if a.Owner == player then
			Atk1TriggerFunction()
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnDiscovered(Tower1, AutoTriggerFunction)
	Trigger.OnDiscovered(Tower2, AutoTriggerFunction)

	Trigger.OnAllKilledOrCaptured(PrimaryTargets, function()
		player.MarkCompletedObjective(NodObjective2)
		HuntTriggerFunction()
	end)

	Trigger.AfterDelay(0, getStartUnits)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			enemy.MarkCompletedObjective(GDIObjective)
		end
	end

	if not player.IsObjectiveCompleted(NodObjective1) and CheckForSams(player) then
		player.MarkCompletedObjective(NodObjective1)
	end

	if DateTime.GameTime % DateTime.Seconds(3) == 0 then
		checkProduction(enemy)
	end

	if DateTime.GameTime % DateTime.Seconds(45) == 0 then
		AutoTriggerFunction()
	end
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end

CheckForSams = function(player)
	local sams = player.GetActorsByType("sam")
	return #sams >= 3
end

checkProduction = function(player)
	local Units = Utils.Where(Map.ActorsInWorld, function(actor)
		return actor.Owner == enemy
	end)

	local UnitsType = { }
	for type, count in pairs(GDIStartUnits) do
		counter = 0
		Utils.Do(Units, function(unit)
			if unit.Type == type then
				counter = counter + 1
			end
		end)
		if counter < count then
			for i = 1, count - counter, 1 do
				UnitsType[i] = type
			end
		end
		if #UnitsType > 0 then
			if (type == 'jeep' or type == 'mtnk') and not Factory.IsDead and Factory.Owner == enemy then
				Factory.Build(UnitsType)
			elseif (type == 'e1' or type == 'e2') and not Barracks.IsDead and Barracks.Owner == enemy then
				Barracks.Build(UnitsType)
			end
		end
		UnitsType = { }
	end
end

getStartUnits = function()
	local Units = Utils.Where(Map.ActorsInWorld, function(actor)
		return actor.Owner == enemy and ( actor.Type == 'e2' or actor.Type == 'e1' or actor.Type == 'jeep' or actor.Type == 'mtnk')
	end)
	Utils.Do(Units, function(unit)
		if not GDIStartUnits[unit.Type] then
			GDIStartUnits[unit.Type] = 1
		else
			GDIStartUnits[unit.Type] = GDIStartUnits[unit.Type] + 1
		end
	end)
end

searches = 0
getAirstrikeTarget = function()
	local list = player.GetGroundAttackers()

	if #list == 0 then
		return
	end

	local target = list[DateTime.GameTime % #list + 1].CenterPosition

	local sams = Map.ActorsInCircle(target, WDist.New(8 * 1024), function(actor)
		return actor.Type == "sam" end)

	if #sams == 0 then
		searches = 0
		return target
	elseif searches < 6 then
		searches = searches + 1
		return getAirstrikeTarget()
	else
		searches = 0
		return nil
	end
end
