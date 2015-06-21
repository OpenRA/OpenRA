NodUnitsVehicle1 = { 'bggy', 'bggy', 'bike', 'bike', 'bike' }
NodUnitsVehicle2 = { 'ltnk', 'ltnk', 'ltnk' }
NodUnitsGunner = { 'e1', 'e1', 'e1', 'e1', 'e1', 'e1' }
NodUnitsRocket = { 'e3', 'e3', 'e3', 'e3', 'e3', 'e3' }
Gdi1Units = { 'e1', 'e1', 'e2', 'e2', 'e2' }

HuntCellTriggerActivator = { CPos.New(61,34), CPos.New(60,34), CPos.New(59,34), CPos.New(58,34), CPos.New(57,34), CPos.New(56,34), CPos.New(55,34), CPos.New(61,33), CPos.New(60,33), CPos.New(59,33), CPos.New(58,33), CPos.New(57,33), CPos.New(56,33) }
DzneCellTriggerActivator = { CPos.New(50,30), CPos.New(49,30), CPos.New(48,30), CPos.New(47,30), CPos.New(46,30), CPos.New(45,30), CPos.New(50,29), CPos.New(49,29), CPos.New(48,29), CPos.New(47,29), CPos.New(46,29), CPos.New(45,29), CPos.New(50,28), CPos.New(49,28), CPos.New(48,28), CPos.New(47,28), CPos.New(46,28), CPos.New(45,28), CPos.New(50,27), CPos.New(49,27), CPos.New(46,27), CPos.New(45,27), CPos.New(50,26), CPos.New(49,26), CPos.New(48,26), CPos.New(47,26), CPos.New(46,26), CPos.New(45,26), CPos.New(50,25), CPos.New(49,25), CPos.New(48,25), CPos.New(47,25), CPos.New(46,25), CPos.New(45,25) }
Win1CellTriggerActivator = { CPos.New(47,27) }
Win2CellTriggerActivator = { CPos.New(57,57), CPos.New(56,57), CPos.New(55,57), CPos.New(57,56), CPos.New(56,56), CPos.New(55,56), CPos.New(57,55), CPos.New(56,55), CPos.New(55,55), CPos.New(57,54), CPos.New(56,54), CPos.New(55,54), CPos.New(57,53), CPos.New(56,53), CPos.New(55,53), CPos.New(57,52), CPos.New(56,52), CPos.New(55,52) }
ChnCellTriggerActivator = { CPos.New(61,52), CPos.New(60,52), CPos.New(59,52), CPos.New(58,52), CPos.New(61,51), CPos.New(60,51), CPos.New(59,51), CPos.New(58,51), CPos.New(61,50), CPos.New(60,50), CPos.New(59,50), CPos.New(58,50) }

Chn1ActorTriggerActivator = { Chn1Actor1, Chn1Actor2 }
Chn2ActorTriggerActivator = { Chn2Actor1 }
Atk1ActorTriggerActivator = { Atk1Actor1, Atk1Actor2 }
Atk2ActorTriggerActivator = { Atk2Actor1, Atk2Actor2 }

Chn1Waypoints = { ChnEntry.Location, waypoint0.Location }
Chn2Waypoints = { ChnEntry.Location, waypoint0.Location }
Gdi5Waypoint = { waypoint1, waypoint2, waypoint3, waypoint4, waypoint5, waypoint6, waypoint7 }

HuntTriggerFunction = function()
	local list = GDI.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

Win1TriggerFunction = function()
	NodObjective2 = Nod.AddPrimaryObjective("Move to the evacuation point.")
	Nod.MarkCompletedObjective(NodObjective1)
end

Chn1TriggerFunction = function()
	if not Chn1Switch then
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Gdi1Units, Chn1Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, function(actor)
			IdleHunt(actor)
		end)
		Chn1Switch = true
	end
end

Atk1TriggerFunction = function()
	if not Atk1Switch then
		for type, count in pairs({ ['e2'] = 2, ['jeep'] = 1, ['e1'] = 2}) do
			MyActors = Utils.Take(count, GDI.GetActorsByType(type))
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
			MyActors = Utils.Take(count, GDI.GetActorsByType(type))
			Utils.Do(MyActors, function(actor)
				MoveAndHunt(actor, Gdi5Waypoint)
			end)
		end
		Atk2Switch = true
	end
end

Chn2TriggerFunction = function()
	if not Chn2Switch then
		local cargo = Reinforcements.ReinforceWithTransport(GDI, 'tran', Gdi1Units, Chn2Waypoints, { ChnEntry.Location })[2]
		Utils.Do(cargo, function(actor)
			IdleHunt(actor)
		end)
		Chn2Switch = true
	end
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
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Camera.Position = UnitsRallyVehicle2.CenterPosition

	Reinforcements.Reinforce(Nod, NodUnitsVehicle1, { UnitsEntryVehicle.Location, UnitsRallyVehicle1.Location }, 10)
	Reinforcements.Reinforce(Nod, NodUnitsVehicle2, { UnitsEntryVehicle.Location, UnitsRallyVehicle2.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsGunner, { UnitsEntryGunner.Location, UnitsRallyGunner.Location }, 15)
	Reinforcements.Reinforce(Nod, NodUnitsRocket, { UnitsEntryRocket.Location, UnitsRallyRocket.Location }, 25)
end

initialSong = "rout"
PlayMusic = function()
	Media.PlayMusic(initialSong, PlayMusic)
	initialSong = nil
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(Nod, function()
		Media.PlaySpeechNotification(Nod, "Win")
	end)

	Trigger.OnPlayerLost(Nod, function()
		Media.PlaySpeechNotification(Nod, "Lose")
	end)

	NodObjective1 = Nod.AddPrimaryObjective("Steal the GDI nuclear detonator.")
	GDIObjective = GDI.AddPrimaryObjective("Stop the Nod taskforce from escaping with the detonator.")

	PlayMusic()

	InsertNodUnits()

	Trigger.OnEnteredFootprint(HuntCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			HuntTriggerFunction()
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(DzneCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Actor.Create('flare', true, { Owner = Nod, Location = waypoint17.Location })
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Win1TriggerFunction()
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Win2CellTriggerActivator, function(a, id)
		if a.Owner == Nod and NodObjective2 then
			Nod.MarkCompletedObjective(NodObjective2)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	OnAnyDamaged(Chn1ActorTriggerActivator, Chn1TriggerFunction)

	OnAnyDamaged(Atk1ActorTriggerActivator, Atk1TriggerFunction)

	OnAnyDamaged(Atk2ActorTriggerActivator, Atk2TriggerFunction)

	OnAnyDamaged(Chn2ActorTriggerActivator, Chn2TriggerFunction)

	Trigger.OnEnteredFootprint(ChnCellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, 'tran', nil, { ChnEntry.Location, waypoint17.Location }, nil, nil, nil)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)
end

Tick = function()
	if Nod.HasNoRequiredUnits()  then
		if DateTime.GameTime > 2 then
			GDI.MarkCompletedObjective(GDIObjective)
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
