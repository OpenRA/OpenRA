IslandSamSites = { SAM01, SAM02 }
NodBase = { PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, PowerPlant5, Refinery, HandOfNod, Silo1, Silo2, Silo3, Silo4, ConYard, CommCenter }

FlameSquad = { FlameGuy1, FlameGuy2, FlameGuy3 }
FlameSquadRoute = { waypoint4.Location, waypoint12.Location, waypoint4.Location, waypoint6.Location }

FootPatrol1Squad = { MiniGunner1, MiniGunner2, RocketSoldier1 }
FootPatrol1Route = {
	waypoint4.Location,
	waypoint12.Location,
	waypoint13.Location,
	waypoint3.Location,
	waypoint2.Location,
	waypoint7.Location,
	waypoint6.Location
}

FootPatrol2Squad = { MiniGunner3, MiniGunner4 }
FootPatrol2Route = {
	waypoint14.Location,
	waypoint16.Location
}

FootPatrol3Squad = { MiniGunner5, MiniGunner6 }
FootPatrol3Route = {
	waypoint15.Location,
	waypoint17.Location
}

FootPatrol4Route = {
	waypoint4.Location,
	waypoint5.Location
}

FootPatrol5Squad = { RocketSoldier2, RocketSoldier3, RocketSoldier4 }
FootPatrol5Route = {
	waypoint4.Location,
	waypoint12.Location,
	waypoint13.Location,
	waypoint8.Location,
	waypoint9.Location,
}

Buggy1Route = {
	waypoint6.Location,
	waypoint7.Location,
	waypoint2.Location,
	waypoint8.Location,
	waypoint9.Location,
	waypoint8.Location,
	waypoint2.Location,
	waypoint7.Location
}

Buggy2Route = {
	waypoint6.Location,
	waypoint10.Location,
	waypoint11.Location,
	waypoint10.Location
}

HuntTriggerActivator = { SAM03, SAM04, SAM05, SAM06, LightTank1, LightTank2, LightTank3, Buggy1, Buggy2, Turret1, Turret2 }

AttackCellTriggerActivator = { CPos.New(57,26), CPos.New(56,26), CPos.New(57,25), CPos.New(56,25), CPos.New(57,24), CPos.New(56,24), CPos.New(57,23), CPos.New(56,23), CPos.New(57,22), CPos.New(56,22), CPos.New(57,21), CPos.New(56,21) }
AttackUnits = { LightTank2, LightTank3 }

KillCounter = 0

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")
	civilian = Player.GetPlayer("Neutral")

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

	if Map.LobbyOption("difficulty") == "easy" then
		CommandoType = "rmbo.easy"
		KillCounterHuntThreshold = 30
	elseif Map.LobbyOption("difficulty") == "hard" then
		CommandoType = "rmbo.hard"
		KillCounterHuntThreshold = 15
	else
		CommandoType = "rmbo"
		KillCounterHuntThreshold = 20
	end

	destroyObjective = player.AddPrimaryObjective("Destroy the Nod ********.")

	Trigger.OnKilled(Airfield, function()
		player.MarkCompletedObjective(destroyObjective)
	end)

	Utils.Do(NodBase, function(structure)
		Trigger.OnKilled(structure, function()
			player.MarkCompletedObjective(destroyObjective)
		end)
	end)

	Trigger.OnAllKilled(IslandSamSites, function()
		TransportFlare = Actor.Create('flare', true, { Owner = player, Location = Flare.Location })
		Reinforcements.ReinforceWithTransport(player, 'tran', nil, { lstStart.Location, TransportRally.Location })
	end)

	Trigger.OnKilled(CivFleeTrigger, function()
		if not Civilian.IsDead then
			Civilian.Move(CivHideOut.Location)
		end
	end)

	Trigger.OnKilled(AttackTrigger2, function()
		Utils.Do(FlameSquad, function(unit)
			if not unit.IsDead then
				unit.Patrol(FlameSquadRoute, false)
			end
		end)
	end)

	Trigger.OnEnteredFootprint(AttackCellTriggerActivator, function(a, id)
		if a.Owner == player then
			Utils.Do(AttackUnits, function(unit)
				if not unit.IsDead then
					unit.AttackMove(waypoint10.Location)
				end
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Utils.Do(HuntTriggerActivator, function(unit)
		Trigger.OnDamaged(unit, HuntTriggerFunction)
	end)

	Trigger.AfterDelay(5, NodKillCounter)

	Utils.Do(FootPatrol1Squad, function(unit)
		unit.Patrol(FootPatrol1Route, true)
	end)

	Utils.Do(FootPatrol2Squad, function(unit)
		unit.Patrol(FootPatrol2Route, true, 50)
	end)

	Utils.Do(FootPatrol3Squad, function(unit)
		unit.Patrol(FootPatrol3Route, true, 50)
	end)

	Utils.Do(FootPatrol5Squad, function(unit)
		unit.Patrol(FootPatrol5Route, true, 50)
	end)

	AttackTrigger2.Patrol(FootPatrol4Route, true, 25)
	LightTank1.Move(waypoint6.Location)
	Buggy1.Patrol(Buggy1Route, true, 25)
	Buggy2.Patrol(Buggy2Route, true, 25)

	Camera.Position = UnitsRally.CenterPosition
	Reinforce({ CommandoType })
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) and player.HasNoRequiredUnits() then
		player.MarkFailedObjective(destroyObjective)
	end
end

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	ReinforceWithLandingCraft(units, lstStart.Location, lstEnd.Location, UnitsRally.Location)
end

ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = player, Facing = 0, Location = transportStart })
	local subcell = 0
	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint ~= nil then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()
end

NodKillCounter = function()
	local enemyUnits = enemy.GetGroundAttackers()
	Utils.Do(enemyUnits, function(unit)
		Trigger.OnKilled(unit, function()
			KillCounter = KillCounter + 1
			if KillCounter >= KillCounterHuntThreshold then
				HuntTriggerFunction()
			end
		end)
	end)
end

HuntTriggerFunction = function()
	local list = enemy.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end
