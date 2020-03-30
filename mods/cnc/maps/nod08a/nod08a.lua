--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
WaypointGroup1 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint10 }
WaypointGroup2 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint7, waypoint8, waypoint9, waypoint10 }
WaypointGroup3 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5 }
WaypointGroup4 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4 }
WaypointGroup5 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint7, waypoint8, waypoint9, waypoint11 }

GDI1 = { units = { ['e1'] = 2, ['e2'] = 2 }, waypoints = WaypointGroup3, delay = 80 }
GDI2 = { units = { ['e2'] = 3, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 10 }
GDI3 = { units = { ['e1'] = 2, ['e3'] = 3 }, waypoints = WaypointGroup1, delay = 30 }
GDI4 = { units = { ['jeep'] = 2 }, waypoints = WaypointGroup3, delay = 45 }
GDI5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 10 }
Auto1 = { units = { ['e1'] = 2, ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup4, delay = 25 }
Auto2 = { units = { ['e2'] = 2, ['jeep'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto3 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 30 }
Auto4 = { units = { ['e1'] = 2, ['mtnk'] = 1 }, waypoints = WaypointGroup2, delay = 30 }
Auto5 = { units = { ['e3'] = 2, ['jeep'] = 1 }, waypoints = WaypointGroup1, delay = 30 }

AutoAttackWaves = { GDI1, GDI2, GDI3, GDI4, GDI5, Auto1, Auto2, Auto3, Auto4, Auto5 }

NodBase = { NodCYard, NodNuke, NodHand }
Outpost = { OutpostCYard, OutpostProc }

IntroReinforcements = { "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }

IntroGuards = { Actor89, Actor137, Actor123, Actor124, Actor135, Actor136 }
OutpostGuards = { Actor91, Actor108, Actor109, Actor110, Actor111, Actor112, Actor113, Actor122 }

NodBaseTrigger = { CPos.New(52, 52), CPos.New(52, 53), CPos.New(52, 54), CPos.New(52, 55), CPos.New(52, 56), CPos.New(52, 57), CPos.New(52, 58), CPos.New(52, 59), CPos.New(52, 60), CPos.New(55, 54) }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(20)

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
			TransportHelicopter = Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ReinforcementsHelicopterSpawn.Location, waypoint15.Location })[1]
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
	FlareCamera = Actor.Create("camera", true, { Owner = player, Location = waypoint25.Location })
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
			FlareCamera.Destroy()
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

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.ReinforceWithTransport(player, 'tran.in', IntroReinforcements, { ReinforcementsHelicopterSpawn.Location, ReinforcementsHelicopterRally.Location }, { ReinforcementsHelicopterSpawn.Location }, nil, nil)

	StartAI(GDICYard)
	AutoGuard(IntroGuards)
	AutoGuard(OutpostGuards)

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
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective3)
	end
end
