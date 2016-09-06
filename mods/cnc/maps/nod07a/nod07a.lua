GDI1 = { teamType = "atk", units = { ['e2'] = 3 }, waypoints = { waypoint0, waypoint1, waypoint2, waypoint14 }, delay = 40 }
GDI2 = { teamType = "atk", units = { ['mtnk'] = 2 }, waypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint9 }, delay = 30 }
GDI3 = { teamType = "atk", units = { ['e2'] = 4 }, waypoints = { waypoint0, waypoint4, waypoint5, waypoint6, waypoint7, waypoint8 }, delay = 40 }
GDI4 = { teamType = "atk", units = { ['e1'] = 1, ['e2'] = 2 }, waypoints = { waypoint0, waypoint4, waypoint9 }, delay = 30 }
GDI5 = { teamType = "atk", units = { ['mtnk'] = 1 }, waypoints = { waypoint0, waypoint4, waypoint10, waypoint11, waypoint12, waypoint13 }, delay = 80 }
GDI6 = { teamType = "atk", units = { ['mtnk'] = 1 }, waypoints = { waypoint0, waypoint4, waypoint9 }, delay = 50 }
GDI7 = { teamType = "atk", units = { ['jeep'] = 1 }, waypoints = { waypoint0, waypoint4, waypoint5, waypoint6, waypoint7, waypoint8 }, delay = 40 }
GDI8 = { teamType = "rei", units = { ['e2'] = 3, ['e6'] = 2 }, waypoints = { waypoint12, waypoint11, waypoint10, waypoint4, waypoint5, waypoint8 },	delay = 8 }
GDI9 = { teamType = "atk", units = { ['e2'] = 4 }, waypoints = { waypoint8 }, delay = 80 }
GDI10 = { teamType = "atk", units = { ['e2'] = 4 }, waypoints = { waypoint14 }, delay = 0 }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(20)

AutoAttackWaves = { GDI3, GDI4, GDI5, GDI6, GDI7, GDI8, GDI9, GDI10 }
IntroAttackWaves = { GDI1, GDI2 }
WhitelistedStructures = { 'afld', 'hand', 'hq', 'nuke', 'silo', 'proc', 'sam' }

NodUnitsBikes = { 'bike', 'bike', 'bike' }
NodUnitsEngineers = { 'e6', 'e6' }
NodUnitsRockets = { 'e3', 'e3', 'e3', 'e3' }
NodUnitsGunners = { 'e1', 'e1', 'e1', 'e1' }
NodUnitsFlamers = { 'e4', 'e4', 'e4', 'e4' }
ReinforcementsRockets = { 'e3', 'e3', 'e3', 'e3', 'e3' }

NodBase = { NodBuilding1, NodBuilding2, NodBuilding3, NodHarvester }

AbandonedBaseTrigger = { CPos.New(12, 42), CPos.New(11, 42), CPos.New(10, 42), CPos.New(13, 41), CPos.New(12, 41), CPos.New(11, 41), CPos.New(14, 40), CPos.New(13, 40), CPos.New(12, 40), CPos.New(6, 40), CPos.New(5, 40), CPos.New(4, 40), CPos.New(6, 39), CPos.New(5, 39), CPos.New(4, 39), CPos.New(6, 38), CPos.New(5, 38), CPos.New(4, 38) }
ReinforcementsTrigger = { CPos.New(35, 23), CPos.New(34, 23), CPos.New(35, 22), CPos.New(34, 22), CPos.New(35, 21), CPos.New(34, 21), CPos.New(35, 20), CPos.New(34, 20), CPos.New(35, 19), CPos.New(34, 19), CPos.New(35, 18), CPos.New(34, 18), CPos.New(35, 17), CPos.New(34, 17), CPos.New(35, 16), CPos.New(34, 16), CPos.New(35, 15), CPos.New(34, 15), CPos.New(35, 14), CPos.New(34, 14), CPos.New(35, 13), CPos.New(34, 13), CPos.New(35, 12), CPos.New(34, 12), CPos.New(47, 11), CPos.New(46, 11), CPos.New(57, 19), CPos.New(56, 19), CPos.New(55, 19), CPos.New(54, 19), CPos.New(53, 19), CPos.New(52, 19), CPos.New(51, 19), CPos.New(50, 19), CPos.New(49, 19), CPos.New(48, 19), CPos.New(47, 19), CPos.New(46, 19), CPos.New(57, 18), CPos.New(56, 18), CPos.New(55, 18), CPos.New(54, 18), CPos.New(53, 18), CPos.New(52, 18), CPos.New(51, 18), CPos.New(50, 18), CPos.New(49, 18), CPos.New(48, 18), CPos.New(47, 18), CPos.New(46, 18), CPos.New(47, 17), CPos.New(46, 17), CPos.New(47, 16), CPos.New(46, 16), CPos.New(47, 15), CPos.New(46, 15), CPos.New(47, 14), CPos.New(46, 14), CPos.New(47, 13), CPos.New(46, 13), CPos.New(47, 12), CPos.New(46, 12) }

CaptureStructures = function(actor)
	for i = 1, #WhitelistedStructures do
		structures = player.GetActorsByType(WhitelistedStructures[i])
		if #structures > 0 then
			if not actor.IsDead and not structures[1].IsDead then
				actor.Capture(structures[1])
				return
			end
		end
	end
end

CheckForSams = function()
	local sams = player.GetActorsByType("sam")
	return #sams >= 3
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

GetCargo = function(team)
	cargo = { }
	for type, count in pairs(team.units) do
		for i = 1, count, 1 do
			cargo[#cargo + 1] = type
		end
	end
	return cargo
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, { 'ltnk'}, { ReinforcementsTopSpawn.Location, ReinforcementsTank1Rally.Location }, 1)
	Reinforcements.Reinforce(player, NodUnitsEngineers, { ReinforcementsTopSpawn.Location, ReinforcementsEngineersRally.Location }, 10)
	Reinforcements.Reinforce(player, NodUnitsRockets, { ReinforcementsTopSpawn.Location, ReinforcementsRocketsRally.Location }, 10)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(player, NodUnitsGunners, { ReinforcementsBottomSpawn.Location, ReinforcementsGunnersRally.Location }, 10)
		Reinforcements.Reinforce(player, NodUnitsFlamers, { ReinforcementsTopSpawn.Location, ReinforcementsFlamersRally.Location }, 10)
		Reinforcements.Reinforce(player, { 'ltnk'}, { ReinforcementsBottomSpawn.Location, ReinforcementsTank2Rally.Location }, 10)
	end)
end

SendAttackWave = function(team)
	for type, amount in pairs(team.units) do
		count = 0
		actors = enemy.GetActorsByType(type)
		Utils.Do(actors, function(actor)
			if actor.IsIdle and count < amount then
				SetAttackWaypoints(actor, team.waypoints)
				if actor.Type == "e6" then 
					CaptureStructures(actor)
				else
					IdleHunt(actor)
				end
				count = count + 1
			end
		end)
	end
end

SetAttackWaypoints = function(actor, waypoints)
	if not actor.IsDead then
		Utils.Do(waypoints, function(waypoint)
			actor.AttackMove(waypoint.Location)
		end)
	end
end

SendGDIAirstrike = function(hq, delay)
	if not hq.IsDead and hq.Owner == enemy then
		local target = getAirstrikeTarget()

		if target then
			hq.SendAirstrike(target, false, Facing.NorthEast + 4)
			Trigger.AfterDelay(delay, function() SendGDIAirstrike(hq, delay) end)
		else
			Trigger.AfterDelay(delay/4, function() SendGDIAirstrike(hq, delay) end)
		end
	end
end

SendWaves = function(counter, Waves)
	if counter <= #Waves then
		local team = Waves[counter]
		if team.teamType == "atk" then
			SendAttackWave(team)
		elseif team.teamType == "rei" then
			SendReinforcementsWave(team)
		end
		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

SendReinforcementsWave = function(team)
	Reinforcements.ReinforceWithTransport(enemy, "apc", GetCargo(team), { ReinforcementsGDISpawn.Location, waypoint12.Location}, nil, function(transport, passengers)
		SetReinforcementsWaypoints(transport, team.waypoints)
		transport.UnloadPassengers()
		Trigger.OnPassengerExited(transport, function(_, passenger)
			Utils.Do(passengers, function(actor)
				if actor.Type == "e6" then 
					CaptureStructures(actor)
				else
					IdleHunt(actor)
				end
			end)
			if not transport.HasPassengers then
				IdleHunt(transport)
			end
		end)
	end)
end

SetReinforcementsWaypoints = function(actor, waypoints)
	if not actor.IsDead then
		Utils.Do(waypoints, function(waypoint)
			actor.Move(waypoint.Location)
		end)
	end
end

StartWaves = function(Waves)
	SendWaves(1, Waves)
end

Trigger.OnEnteredFootprint(AbandonedBaseTrigger, function(a, id)
	if not abandonedBaseTrigger and a.Owner == player then
		abandonedBaseTrigger = true

		FlareCamera = Actor.Create("camera", true, { Owner = player, Location = waypoint25.Location })
		Flare = Actor.Create("flare", true, { Owner = player, Location = waypoint25.Location })

		Utils.Do(NodBase, function(actor)
			if not actor.IsDead then
				actor.Owner = player
			end
		end)

		player.MarkCompletedObjective(NodObjective1)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Media.PlaySpeechNotification(player, "NewOptions")
		end)
	end
end)

Trigger.OnEnteredFootprint(ReinforcementsTrigger, function(a, id)
	if not reinforcementsTrigger and a.Owner == player and a.Type ~= 'harv' then
		reinforcementsTrigger = true

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran.in', ReinforcementsRockets, { ReinforcementsHelicopterSpawn.Location, waypoint24.Location }, { ReinforcementsHelicopterSpawn.Location }, nil, nil)
		end)

		StartWaves(IntroAttackWaves)

		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)

		Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(GDIPyle) end)

		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)

		Trigger.AfterDelay(DateTime.Minutes(3), function()StartWaves(AutoAttackWaves) end)

		Trigger.AfterDelay(DateTime.Minutes(2), function()
			Flare.Destroy()
			FlareCamera.Destroy()
		end)
	end
end)

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()
	StartAI(GDICYard)

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

	NodObjective1 = player.AddPrimaryObjective("Find the Nod base.")
	NodObjective2 = player.AddPrimaryObjective("Eliminate all GDI forces in the area.")
	NodObjective3 = player.AddSecondaryObjective("Build 3 SAMs to fend off the GDI bombers.")
	GDIObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective2)
	end

	if not player.IsObjectiveCompleted(NodObjective3) and CheckForSams() then
		player.MarkCompletedObjective(NodObjective3)
	end
end
