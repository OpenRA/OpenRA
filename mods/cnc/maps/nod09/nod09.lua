--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
if Map.LobbyOption("difficulty") == "easy" then
	Rambo = "rmbo.easy"
elseif Map.LobbyOption("difficulty") == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

WaypointGroup1 = { waypoint0, waypoint3, waypoint2, waypoint4, waypoint5, waypoint7 }
WaypointGroup2 = { waypoint0, waypoint3, waypoint2, waypoint4, waypoint5, waypoint6 }
WaypointGroup3 = { waypoint0, waypoint8, waypoint9, waypoint10, waypoint11, waypoint12, waypoint6, waypoint13 }
Patrol1Waypoints = { waypoint0.Location, waypoint8.Location, waypoint9.Location, waypoint10.Location }
Patrol2Waypoints = { waypoint0.Location, waypoint3.Location, waypoint2.Location, waypoint4.Location }

GDI1 = { units = { ['e2'] = 3, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 40 }
GDI2 = { units = { ['e1'] = 2, ['e2'] = 4 }, waypoints = WaypointGroup2, delay = 50 }
GDI4 = { units = { ['jeep'] = 2 }, waypoints = WaypointGroup1, delay = 50 }
GDI5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 40 }
Auto1 = { units = { ['e1'] = 2, ['e3'] = 3 }, waypoints = WaypointGroup3, delay = 40 }
Auto2 = { units = { ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup3, delay = 50 }
Auto3 = { units = { ['e1'] = 3, ['e2'] = 2 }, waypoints = WaypointGroup2, delay = 60 }
Auto4 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 50 }
Auto5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 50 }
Auto6 = { units = { ['e1'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup3, delay = 50 }
Auto7 = { units = { ['msam'] = 1 }, waypoints = WaypointGroup1, delay = 40 }
Auto8 = { units = { ['msam'] = 1 }, waypoints = WaypointGroup3, delay = 50 }

RmboReinforcements = { Rambo }
EngineerReinforcements = { "e6", "e6" }
RocketReinforcements = { "e3", "e3", "e3", "e3" }

AutoAttackWaves = { GDI1, GDI2, GDI4, GDI5, Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7, Auto8 }

NodBaseTrigger = { CPos.New(9, 52), CPos.New(9, 51), CPos.New(9, 50), CPos.New(9, 49), CPos.New(9, 48), CPos.New(9, 47), CPos.New(9, 46), CPos.New(10, 46), CPos.New(11, 46), CPos.New(12, 46), CPos.New(13, 46), CPos.New(14, 46), CPos.New(15, 46), CPos.New(16, 46), CPos.New(17, 46), CPos.New(18, 46), CPos.New(19, 46), CPos.New(20, 46), CPos.New(21, 46), CPos.New(22, 46), CPos.New(23, 46), CPos.New(24, 46), CPos.New(25, 46), CPos.New(25, 47), CPos.New(25, 48), CPos.New(25, 49), CPos.New(25, 50), CPos.New(25, 51), CPos.New(25, 52) }
EngineerTrigger = { CPos.New(5, 13), CPos.New(6, 13), CPos.New(7, 13), CPos.New(8, 13), CPos.New(9, 13), CPos.New(10, 13), CPos.New(16, 7), CPos.New(16, 6), CPos.New(16, 5), CPos.New(16, 4), CPos.New(16, 3)}
RocketTrigger = { CPos.New(20, 15), CPos.New(21, 15), CPos.New(22, 15), CPos.New(23, 15), CPos.New(24, 15), CPos.New(25, 15), CPos.New(26, 15), CPos.New(32, 15), CPos.New(32, 14), CPos.New(32, 13), CPos.New(32, 12), CPos.New(32, 11)}

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(30)

CheckForSams = function(player)
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

SendAttackWave = function(team)
	for type, amount in pairs(team.units) do
		count = 0
		local actors = enemy.GetActorsByType(type)
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

StartPatrols = function()
	local mtnks = enemy.GetActorsByType("mtnk")
	local msams = enemy.GetActorsByType("msam")

	if #mtnks >= 1 then
		mtnks[1].Patrol(Patrol1Waypoints, true, 20)
	end

	if #msams >= 1 then
		msams[1].Patrol(Patrol2Waypoints, true, 20)
	end
end

StartWaves = function()
	SendWaves(1, AutoAttackWaves)
end

Trigger.OnEnteredFootprint(NodBaseTrigger, function(a, id)
	if not nodBaseTrigger and a.Owner == player then
		nodBaseTrigger = true
		player.MarkCompletedObjective(NodObjective3)
		NodCYard.Owner = player

		local walls = nodBase.GetActorsByType("brik")
		Utils.Do(walls, function(actor)
			actor.Owner = player
		end)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(player, "NewOptions")
		end)
	end
end)

Trigger.OnEnteredFootprint(RocketTrigger, function(a, id)
	if not rocketTrigger and a.Owner == player then
		rocketTrigger = true
		player.MarkCompletedObjective(NodObjective1)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran.in', RocketReinforcements, { HelicopterEntryRocket.Location, HelicopterGoalRocket.Location }, { HelicopterEntryRocket.Location }, nil, nil)
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			FlareEngineerCamera1 = Actor.Create("camera", true, { Owner = player, Location = FlareEngineer.Location })
			FlareEngineerCamera2 = Actor.Create("camera", true, { Owner = player, Location = CameraEngineerPath.Location })
			FlareEngineer = Actor.Create("flare", true, { Owner = player, Location = FlareEngineer.Location })
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			FlareRocketCamera.Destroy()
			FlareRocket.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(EngineerTrigger, function(a, id)
	if not engineerTrigger and a.Owner == player then
		engineerTrigger = true
		player.MarkCompletedObjective(NodObjective2)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran.in', EngineerReinforcements, { HelicopterEntryEngineer.Location, HelicopterGoalEngineer.Location }, { HelicopterEntryEngineer.Location }, nil, nil)
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			FlareEngineerCamera1.Destroy()
			FlareEngineerCamera2.Destroy()
			FlareEngineer.Destroy()
		end)
	end
end)

Trigger.OnKilledOrCaptured(OutpostProc, function()
	if not outpostCaptured then
		outpostCaptured = true

		if OutpostProc.IsDead then
			player.MarkFailedObjective(NodObjective4)
		else
			player.MarkCompletedObjective(NodObjective4)
			player.Cash = 1000
		end

		StartPatrols()

		Trigger.AfterDelay(DateTime.Minutes(1), function() StartWaves() end)
		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceInfantry(GDIPyle) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)
	end
end)

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	nodBase = Player.GetPlayer("NodBase")

	Camera.Position = CameraIntro.CenterPosition
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.ReinforceWithTransport(player, 'tran.in', RmboReinforcements, { HelicopterEntryRmbo.Location, HelicopterGoalRmbo.Location }, { HelicopterEntryRmbo.Location }, nil, nil)
	FlareRocketCamera = Actor.Create("camera", true, { Owner = player, Location = FlareRocket.Location })
	FlareRocket = Actor.Create("flare", true, { Owner = player, Location = FlareRocket.Location })

	StartAI(GDICYard)
	AutoGuard(enemy.GetGroundAttackers())

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

	NodObjective1 = player.AddPrimaryObjective("Secure the first landing zone.")
	NodObjective2 = player.AddPrimaryObjective("Secure the second landing zone.")
	NodObjective3 = player.AddPrimaryObjective("Locate the Nod base.")
	NodObjective4 = player.AddPrimaryObjective("Capture the refinery.")
	NodObjective5 = player.AddPrimaryObjective("Eliminate all GDI forces in the area.")
	NodObjective6 = player.AddSecondaryObjective("Build 3 SAMs to fend off the GDI bombers.")
	GDIObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective5)
	end

	if not player.IsObjectiveCompleted(NodObjective6) and CheckForSams(player) then
		player.MarkCompletedObjective(NodObjective6)
	end
end
