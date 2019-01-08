--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

HuntTriggerFunction = function()
	local list = enemy.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

Chn1TriggerFunction = function()
	if not Chn1Switch then
		local cargo = Reinforcements.ReinforceWithTransport(enemy, 'tran', Gdi1Units, Chn1Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, function(actor)
			IdleHunt(actor)
		end)
		Chn1Switch = true
	end
end

Atk1TriggerFunction = function()
	if not Atk1Switch then
		for type, count in pairs({ ['e2'] = 2, ['jeep'] = 1, ['e1'] = 2}) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				IdleHunt(actor)
			end)
		end
		Atk1Switch = true
	end
end

Atk2TriggerFunction = function()
	if not Atk2Switch then
		for type, count in pairs({ ['e2'] = 2, ['e1'] = 2}) do
			MyActors = Utils.Take(count, enemy.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				MoveAndHunt(actor, Gdi5Waypoint)
			end)
		end
		Atk2Switch = true
	end
end

Chn2TriggerFunction = function()
	if not Chn2Switch then
		local cargo = Reinforcements.ReinforceWithTransport(enemy, 'tran', Gdi1Units, Chn2Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, function(actor)
			IdleHunt(actor)
		end)
		Chn2Switch = true
	end
end

Obj2TriggerFunction = function()
	player.MarkCompletedObjective(NodObjective2)
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, Obj2Units, { Obj2UnitsEntry.Location, waypoint13.Location }, 15)
end

MoveAndHunt = function(unit, waypoints)
	if unit ~= nil then
		Utils.Do(waypoints, function(waypoint)
			unit.AttackMove(waypoint.Location)
		end)
		IdleHunt(unit)
	end
end

InsertNodUnits = function()
	local difficulty = Map.LobbyOption("difficulty")
	NodUnitsVehicle1 = NodUnitsVehicle1[difficulty]
	NodUnitsVehicle2 = NodUnitsVehicle2[difficulty]
	NodUnitsGunner = NodUnitsGunner[difficulty]
	NodUnitsRocket = NodUnitsRocket[difficulty]
	
	Media.PlaySpeechNotification(player, "Reinforce")
	Camera.Position = UnitsRallyVehicle2.CenterPosition

	Reinforcements.Reinforce(player, NodUnitsVehicle1, { UnitsEntryVehicle.Location, UnitsRallyVehicle1.Location }, 10)
	Reinforcements.Reinforce(player, NodUnitsVehicle2, { UnitsEntryVehicle.Location, UnitsRallyVehicle2.Location }, 15)
	Reinforcements.Reinforce(player, NodUnitsGunner, { UnitsEntryGunner.Location, UnitsRallyGunner.Location }, 15)
	Reinforcements.Reinforce(player, NodUnitsRocket, { UnitsEntryRocket.Location, UnitsRallyRocket.Location }, 25)
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

	Trigger.OnEnteredFootprint(HuntCellTriggerActivator, function(a, id)
		if a.Owner == player then
			HuntTriggerFunction()
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

	OnAnyDamaged(Chn1ActorTriggerActivator, Chn1TriggerFunction)

	OnAnyDamaged(Atk1ActorTriggerActivator, Atk1TriggerFunction)

	OnAnyDamaged(Atk2ActorTriggerActivator, Atk2TriggerFunction)

	OnAnyDamaged(Chn2ActorTriggerActivator, Chn2TriggerFunction)

	Trigger.OnEnteredFootprint(ChnCellTriggerActivator, function(a, id)
		if a.Owner == player then
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ChnEntry.Location, waypoint17.Location }, nil, nil, nil)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			enemy.MarkCompletedObjective(GDIObjective)
		end
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
