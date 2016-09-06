WaypointGroup1 = { waypoint1, waypoint2, waypoint3, waypoint9, waypoint10 }
WaypointGroup2 = { waypoint5, waypoint6, waypoint7, waypoint8 }
WaypointGroup3 = { waypoint1, waypoint2, waypoint4, waypoint11 }

GDI1 = { units = { ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 30 }
GDI2 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup2, delay = 40 }
GDI3 = { units = { ['e1'] = 3, ['e2'] = 3 }, waypoints = WaypointGroup3, delay = 40 }
GDI4 = { units = { ['jeep'] = 2 }, waypoints = WaypointGroup2, delay = 20 }
Auto1 = { units = { ['e1'] = 3, ['e2'] = 1 }, waypoints = WaypointGroup2, delay = 30 }
Auto2 = { units = { ['e1'] = 2, ['e2'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto3 = { units = { ['e1'] = 2, ['e2'] = 2 }, waypoints = WaypointGroup1, delay = 30 }
Auto4 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 30 }
Auto5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto6 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto7 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup1, delay = 50 }

AutoAttackWaves = { GDI1, GDI2, GDI3, GDI4, Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7 }

NodBase = { NodCYard, NodNuke, NodHand }
Outpost = { OutpostCYard, OutpostProc }

IntroGuards = { Actor171, Actor172, Actor173, Actor145, Actor159, Actor160, Actor161 }
OutpostGuards = { Actor177, Actor178, Actor180, Actor187, Actor188, Actor185, Actor186, Actor184, Actor148, Actor179, Actor176, Actor183, Actor182 }
IntroReinforcements = { "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }

NodBaseTrigger = { CPos.New(52, 2), CPos.New(52, 3), CPos.New(52, 4), CPos.New(52, 5), CPos.New(52, 6), CPos.New(52, 7), CPos.New(52, 8) }

Gunboat1PatrolPath = { GunboatLeft1.Location, GunboatRight1.Location }
Gunboat2PatrolPath = { GunboatLeft2.Location, GunboatRight2.Location }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(30)

NodBaseCapture = function()
	nodBaseTrigger = true
	player.MarkCompletedObjective(NodObjective1)
	Utils.Do(NodBase, function(actor)
		actor.Owner = player
	end)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(player, "NewOptions")
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

--Provide the player with a helicopter until the outpost got captured
SendHelicopter = function()
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if not outpostCaptured then
			Media.PlaySpeechNotification(player, "Reinforce")
			TransportHelicopter = Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ReinforcementsHelicopterSpawn.Location, waypoint0.Location })[1]
			Trigger.OnKilled(TransportHelicopter, function()
				SendHelicopter()
			end)
		end
	end)
end

SendAttackWave = function(team)
	for type, amount in pairs(team.units) do
		count = 0
		actors = enemy.GetActorsByType(type)
		Utils.Do(actors, function(actor)
			if actor.IsIdle and count < amount then
				SetAttackWaypoints(actor, team.waypoints)
				IdleHunt(actor)
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

StartWaves = function()
	SendWaves(1, AutoAttackWaves)
end

Trigger.OnAllKilled(IntroGuards, function()
	FlareCamera1 = Actor.Create("camera", true, { Owner = player, Location = waypoint25.Location })
	FlareCamera2 = Actor.Create("camera", true, { Owner = player, Location = FlareExtraCamera.Location })
	Flare = Actor.Create("flare", true, { Owner = player, Location = waypoint25.Location })
	SendHelicopter()
	player.MarkCompletedObjective(NodObjective1)
	NodBaseCapture()
end)

Trigger.OnAllKilledOrCaptured(Outpost, function()
	if not outpostCaptured then
		outpostCaptured = true

		Trigger.AfterDelay(DateTime.Minutes(1), function()

			if not GDIHQ.IsDead and (not NodHand.IsDead or not NodNuke.IsDead) then
				local airstrikeproxy = Actor.Create("airstrike.proxy", false, { Owner = enemy })
				airstrikeproxy.SendAirstrike(AirstrikeTarget.CenterPosition, false, Facing.NorthEast + 4)
				airstrikeproxy.Destroy()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(15), function()
			Utils.Do(OutpostGuards, function(unit)
				IdleHunt(unit)
			end)
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			FlareCamera1.Kill()
			FlareCamera2.Kill()
			Flare.Destroy()
		end)

		player.MarkCompletedObjective(NodObjective2)
		Trigger.AfterDelay(DateTime.Minutes(1), function() StartWaves() end)
		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
		Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(GDIPyle) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)
	end
end)

Trigger.OnCapture(OutpostCYard, function()
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(player, "NewOptions")
	end)
end)

Trigger.OnAnyKilled(Outpost, function()
	if not outpostCaptured then
		player.MarkFailedObjective(NodObjective2)
	end
end)

Trigger.OnEnteredFootprint(NodBaseTrigger, function(a, id)
	if not nodBaseTrigger and a.Owner == player then
		NodBaseCapture()
	end
end)

Trigger.OnKilled(Gunboat1, function()
	Gunboat1Camera.Destroy()
end)
Trigger.OnKilled(Gunboat2, function()
	Gunboat2Camera.Destroy()
end)

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.ReinforceWithTransport(player, 'tran.in', IntroReinforcements, { ReinforcementsHelicopterSpawn.Location, ReinforcementsHelicopterRally.Location }, { ReinforcementsHelicopterSpawn.Location }, nil, nil)

	StartAI(GDICYard)
	AutoGuard(enemy.GetGroundAttackers())

	Gunboat1Camera = Actor.Create("camera.boat", true, { Owner = player, Location = Gunboat1.Location })
	Gunboat2Camera = Actor.Create("camera.boat", true, { Owner = player, Location = Gunboat2.Location })
	Trigger.OnIdle(Gunboat1, function() Gunboat1.Patrol(Gunboat1PatrolPath) end)
	Trigger.OnIdle(Gunboat2, function() Gunboat2.Patrol(Gunboat2PatrolPath) end)

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

	NodObjective1 = player.AddPrimaryObjective("Locate the Nod base.")
	NodObjective2 = player.AddPrimaryObjective("Capture the GDI outpost.")
	NodObjective3 = player.AddPrimaryObjective("Eliminate all GDI forces in the area.")
	GDIObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if not Gunboat1.IsDead then
		Gunboat1Camera.Teleport(Gunboat1.Location)
	end

	if not Gunboat2.IsDead then
		Gunboat2Camera.Teleport(Gunboat2.Location)
	end

	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective3)
	end
end
