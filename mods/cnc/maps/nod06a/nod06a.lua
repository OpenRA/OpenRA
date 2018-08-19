--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
Win1CellTriggerActivator = { CPos.New(59,43) }
Win2CellTriggerActivator = { CPos.New(54,58), CPos.New(53,58), CPos.New(52,58), CPos.New(54,57), CPos.New(53,57), CPos.New(52,57), CPos.New(54,56), CPos.New(53,56), CPos.New(52,56), CPos.New(54,55), CPos.New(53,55), CPos.New(52,55) }

Grd2ActorTriggerActivator = { Guard1, Guard2, Guard3 }
Atk1ActorTriggerActivator = { Atk1Activator1, Atk1Activator2 }
Atk2ActorTriggerActivator = { Atk2Activator1, Atk2Activator2 }
Chn1ActorTriggerActivator = { Chn1Activator1, Chn1Activator2, Chn1Activator3, Chn1Activator4, Chn1Activator5 }
Chn2ActorTriggerActivator = { Chn2Activator1, Chn2Activator2, Chn2Activator3 }
Obj2ActorTriggerActivator = { Chn1Activator1, Chn1Activator2, Chn1Activator3, Chn1Activator4, Chn1Activator5, Chn2Activator1, Chn2Activator2, Chn2Activator3, Atk3Activator }

Chn1Waypoints = { ChnEntry.Location, waypoint5.Location }
Chn2Waypoints = { ChnEntry.Location, waypoint6.Location }
Gdi3Waypoints = { waypoint1, waypoint3, waypoint7, waypoint8, waypoint9 }
Gdi4Waypoints = { waypoint4, waypoint10, waypoint9, waypoint11, waypoint9, waypoint10 }
Gdi5Waypoints = { waypoint1, waypoint4 }
Gdi6Waypoints = { waypoint2, waypoints3 }

Grd1TriggerFunctionTime = DateTime.Seconds(3)

Grd1TriggerFunction = function()
	MyActors = Utils.Take(2, enemy.GetActorsByType('mtnk'))
	Utils.Do(MyActors, function(actor)
		MovementAndHunt(actor, Gdi3Waypoints)
	end)
end

Grd2TriggerFunction = function()
	if not Grd2Switch then
		for type, count in pairs({ ['e1'] = 2, ['e2'] = 1, ['jeep'] = 1 }) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				MovementAndHunt(actor, Gdi4Waypoints)
			end)
		end
		Grd2Swicth = true
	end
end

Atk1TriggerFunction = function()
	if not Atk1Switch then
		for type, count in pairs({ ['e1'] = 3, ['e2'] = 3, ['jeep'] = 1 }) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				MovementAndHunt(actor, Gdi5Waypoints)
			end)
		end
		Atk1Switch = true
	end
end

Atk2TriggerFunction = function()
	if not Atk2Switch then
		for type, count in pairs({ ['mtnk'] = 1, ['jeep'] = 1 }) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				MovementAndHunt(actor, Gdi6Waypoints)
			end)
		end
		Atk2Switch = true
	end
end

Atk3TriggerFunction = function()
	if not Atk3Switch then
		Atk3Switch = true
		if not CommCenter.IsDead then
			local targets = player.GetGroundAttackers()
			local target = targets[DateTime.GameTime % #targets + 1].CenterPosition

			if target then
				CommCenter.SendAirstrike(target, false, Facing.NorthEast + 4)
			end
		end
	end
end

Chn1TriggerFunction = function()
	local cargo = Reinforcements.ReinforceWithTransport(enemy, 'tran', Chn1Units, Chn1Waypoints, { waypoint14.Location })[2]
	Utils.Do(cargo, function(actor)
		IdleHunt(actor)
	end)
end

Chn2TriggerFunction = function()
	local cargo = Reinforcements.ReinforceWithTransport(enemy, 'tran', Chn2Units, Chn2Waypoints, { waypoint14.Location })[2]
	Utils.Do(cargo, function(actor)
		IdleHunt(actor)
	end)
end

Obj2TriggerFunction = function()
	player.MarkCompletedObjective(NodObjective2)
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, Obj2Units, { Obj2UnitsEntry.Location, waypoint13.Location }, 15)
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
	local difficulty = Map.LobbyOption("difficulty")
	NodStartUnitsRight = NodStartUnitsRight[difficulty]
	NodStartUnitsLeft = NodStartUnitsLeft[difficulty]

	Camera.Position = UnitsRallyRight.CenterPosition

	Media.PlaySpeechNotification(player, "Reinforce")
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
	NodObjective2 = player.AddSecondaryObjective("Destroy the houses of the GDI supporters\nin the village.")

	GDIObjective = enemy.AddPrimaryObjective("Stop the Nod taskforce from escaping with the detonator.")

	InsertNodUnits()

	Trigger.AfterDelay(Grd1TriggerFunctionTime, Grd1TriggerFunction)

	Utils.Do(Grd2ActorTriggerActivator, function(actor)
		Trigger.OnDiscovered(actor, Grd2TriggerFunction)
	end)

	OnAnyDamaged(Atk1ActorTriggerActivator, Atk1TriggerFunction)

	OnAnyDamaged(Atk2ActorTriggerActivator, Atk2TriggerFunction)

	if Map.LobbyOption("difficulty") == "tough" then
		Trigger.OnDamaged(Atk3Activator, Atk3TriggerFunction)
	end

	Trigger.OnAllKilled(Chn1ActorTriggerActivator, Chn1TriggerFunction)

	Trigger.OnAllKilled(Chn2ActorTriggerActivator, Chn2TriggerFunction)

	Trigger.OnEnteredFootprint(Chn3CellTriggerActivator, function(a, id)
		if a.Owner == player then
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ChnEntry.Location, waypoint17.Location }, nil, nil, nil)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == player then
			Actor.Create('flare', true, { Owner = player, Location = waypoint17.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllRemovedFromWorld(Obj2ActorTriggerActivator, Obj2TriggerFunction)

	Trigger.OnEnteredFootprint(Win1CellTriggerActivator, function(a, id)
		if a.Owner == player then
			NodObjective3 = player.AddPrimaryObjective("Move to the evacuation point.")
			player.MarkCompletedObjective(NodObjective1)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win2CellTriggerActivator, function(a, id)
		if a.Owner == player and NodObjective3 then
			player.MarkCompletedObjective(NodObjective3)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end
