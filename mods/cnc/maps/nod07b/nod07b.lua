WaypointGroup1 = { waypoint0, waypoint15 }
WaypointGroup2 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5, waypoint8 }
WaypointGroup3 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint9, waypoint10, waypoint11, waypoint6, waypoint7 }
WaypointGroup4 = { waypoint9, waypoint10, waypoint11, waypoint6, waypoint7, waypoint14 }

GDI1 = { units = { ['e2'] = 2, ['e6'] = 1 }, waypoints = WaypointGroup4, delay = 40 }
GDI2 = { units = { ['e1'] = 1, ['e2'] = 1}, waypoints = WaypointGroup3, delay = 40 }
GDI3 = { units = { ['e2'] = 1, ['e3'] = 1, ['jeep'] = 1 }, waypoints = WaypointGroup2, delay = 40 }
GDI4 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
GDI5 = { units = { ['e1'] = 1, ['e2'] = 2 }, waypoints = WaypointGroup2, delay = 40 }
GDI6 = { units = { ['e2'] = 2, ['e2'] = 3 }, waypoints = WaypointGroup1, delay = 40 }
Auto1 = { units = { ['e1'] = 3, ['e2'] = 2 }, waypoints = WaypointGroup3, delay = 40 }
Auto2 = { units = { ['e1'] = 1, ['e2'] = 2 }, waypoints = WaypointGroup2, delay = 40 }
Auto3 = { units = { ['e1'] = 1, ['e3'] = 2 }, waypoints = WaypointGroup2, delay = 40 }
Auto4 = { units = { ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup3, delay = 40 }
Auto5 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup2, delay = 50 }
Auto6 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto7 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup2, delay = 50 }
Auto8 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 30 }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(10)

AutoAttackWaves = { Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7, Auto8 }
WhitelistedStructures = { 'afld', 'hand', 'hq', 'nuke', 'silo', 'proc', 'sam' }

NodUnitsTanks = { 'ltnk', 'ltnk', 'ltnk' }
NodUnitsBikes = { 'bike', 'bike', 'bike' }
NodUnitsBuggys = { 'bggy', 'bggy', 'bggy' }
NodUnitsRockets = { 'e3', 'e3', 'e3' }
NodUnitsGunners = { 'e1', 'e1', 'e1' }

Atk1 = { CPos.New(11, 43), CPos.New(10, 43), CPos.New(9, 43), CPos.New(8, 43), CPos.New(7, 43), CPos.New(6, 43), CPos.New(5, 43), CPos.New(11, 42), CPos.New(10, 42), CPos.New(9, 42), CPos.New(8, 42), CPos.New(7, 42), CPos.New(6, 42), CPos.New(5, 42), CPos.New(23, 38), CPos.New(22, 38), CPos.New(21, 38), CPos.New(20, 38), CPos.New(19, 38), CPos.New(24, 37), CPos.New(23, 37), CPos.New(22, 37), CPos.New(21, 37), CPos.New(20, 37), CPos.New(19, 37) }
Atk2 = { CPos.New(16, 52), CPos.New(15, 52), CPos.New(14, 52), CPos.New(13, 52), CPos.New(12, 52), CPos.New(11, 52), CPos.New(10, 52), CPos.New(9, 52), CPos.New(8, 52), CPos.New(16, 51), CPos.New(15, 51), CPos.New(14, 51), CPos.New(13, 51), CPos.New(12, 51), CPos.New(11, 51), CPos.New(10, 51), CPos.New(9, 51), CPos.New(8, 51), CPos.New(31, 44), CPos.New(30, 44), CPos.New(29, 44), CPos.New(28, 44), CPos.New(27, 44), CPos.New(26, 44), CPos.New(25, 44), CPos.New(24, 44), CPos.New(23, 44), CPos.New(22, 44), CPos.New(21, 44), CPos.New(31, 43), CPos.New(30, 43), CPos.New(29, 43), CPos.New(28, 43), CPos.New(27, 43), CPos.New(26, 43), CPos.New(25, 43), CPos.New(24, 43), CPos.New(23, 43), CPos.New(22, 43), CPos.New(21, 43) }
Atk3 = { CPos.New(53, 58), CPos.New(52, 58), CPos.New(51, 58), CPos.New(53, 57), CPos.New(52, 57), CPos.New(51, 57), CPos.New(53, 56), CPos.New(52, 56), CPos.New(51, 56), CPos.New(53, 55), CPos.New(52, 55), CPos.New(51, 55) }
Atk4 = { CPos.New(54, 47), CPos.New(53, 47), CPos.New(52, 47), CPos.New(51, 47), CPos.New(43, 47), CPos.New(54, 46), CPos.New(53, 46), CPos.New(52, 46), CPos.New(51, 46), CPos.New(50, 46), CPos.New(43, 46), CPos.New(42, 46), CPos.New(41, 46), CPos.New(43, 45), CPos.New(42, 45), CPos.New(41, 45), CPos.New(43, 44), CPos.New(42, 44), CPos.New(41, 44), CPos.New(43, 43), CPos.New(42, 43), CPos.New(41, 43), CPos.New(43, 42) }

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
	Reinforcements.Reinforce(player, NodUnitsBikes, { ReinforcementsSpawnLeft.Location, ReinforcementsBikesRally.Location }, 1)
	Reinforcements.Reinforce(player, NodUnitsBuggys, { ReinforcementsSpawnRight.Location, ReinforcementsBuggyRally.Location }, 50)
	Reinforcements.Reinforce(player, NodUnitsGunners, { ReinforcementsSpawnLeft.Location, ReinforcementsGunnersRally.Location }, 50)
	Reinforcements.Reinforce(player, NodUnitsRockets, { ReinforcementsSpawnRight.Location, ReinforcementsRocketsRally.Location }, 50)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Reinforcements.Reinforce(player, { 'mcv' }, { ReinforcementsSpawnCenter.Location, ReinforcementsMCVRally.Location })
		Reinforcements.Reinforce(player, NodUnitsTanks, { ReinforcementsSpawnCenter.Location, ReinforcementsTanksRally.Location }, 50)
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
		SendAttackWave(team)
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
		IdleHunt(actor)
	end
end

StartWaves = function()
	SendWaves(1, AutoAttackWaves)
end



Trigger.OnEnteredFootprint(Atk1, function(a, id)
	if not atk1Trigger and a.Owner == player then
		atk1Trigger = true
		SendAttackWave(GDI5)
	end
end)

Trigger.OnEnteredFootprint(Atk2, function(a, id)
	if not atk2Trigger and a.Owner == player then
		atk2Trigger = true
		SendAttackWave(GDI4)
	end
end)

Trigger.OnEnteredFootprint(Atk3, function(a, id)
	if not atk3Trigger and a.Owner == player then
		atk3Trigger = true
		SendAttackWave(GDI6)
	end
end)

Trigger.OnEnteredFootprint(Atk4, function(a, id)
	if not atk4Trigger and a.Owner == player then
		atk4Trigger = true
		SendReinforcementsWave(GDI1)
	end
end)

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()
	StartAI(GDICYard)

	Trigger.AfterDelay(DateTime.Seconds(10), function() SendAttackWave(GDI2) end)
	Trigger.AfterDelay(DateTime.Seconds(55), function() SendAttackWave(GDI2) end)
	Trigger.AfterDelay(DateTime.Seconds(85), function() SendAttackWave(GDI3) end)

	Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(GDIPyle) end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)

	Trigger.OnPlayerDiscovered(player, StartWaves)

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

	NodObjective1 = player.AddPrimaryObjective("Eliminate all GDI forces in the area.")
	NodObjective2 = player.AddSecondaryObjective("Build 3 SAMs to fend off the GDI bombers.")
	GDIObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective1)
	end

	if not player.IsObjectiveCompleted(NodObjective2) and CheckForSams() then
		player.MarkCompletedObjective(NodObjective2)
	end
end
