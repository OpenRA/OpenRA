--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

GDI1 = { teamType = "atk", units = { "e2", "e2", "e2" }, waypoints = { waypoint0, waypoint1, waypoint2, waypoint14 }, delay = 40 }
GDI2 = { teamType = "atk", units = { "mtnk", "mtnk" }, waypoints = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint9 }, delay = 30 }
GDI3 = { teamType = "atk", units = { "e2", "e2", "e2", "e2" }, waypoints = { waypoint0, waypoint4, waypoint5, waypoint6, waypoint7, waypoint8 }, delay = 40 }
GDI4 = { teamType = "atk", units = { "e1", "e2", "e2" }, waypoints = { waypoint0, waypoint4, waypoint9 }, delay = 30 }
GDI5 = { teamType = "atk", units = { "mtnk" }, waypoints = { waypoint0, waypoint4, waypoint10, waypoint11, waypoint12, waypoint13 }, delay = 80 }
GDI6 = { teamType = "atk", units = { "mtnk" }, waypoints = { waypoint0, waypoint4, waypoint9 }, delay = 50 }
GDI7 = { teamType = "atk", units = { "jeep" }, waypoints = { waypoint0, waypoint4, waypoint5, waypoint6, waypoint7, waypoint8 }, delay = 40 }
GDI8 = { teamType = "rei", units = { "e2", "e2", "e2", "e6", "e6" }, waypoints = { waypoint12, waypoint11, waypoint10, waypoint4, waypoint5, waypoint8 }, delay = 8 }
GDI9 = { teamType = "atk", units = { "e2", "e2", "e2", "e2" }, waypoints = { waypoint8 }, delay = 80 }
GDI10 = { teamType = "atk", units = { "e2", "e2", "e2", "e2" }, waypoints = { waypoint14 }, delay = 0 }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(20)

AutoAttackWaves = { GDI3, GDI4, GDI5, GDI6, GDI7, GDI8, GDI9, GDI10 }
IntroAttackWaves = { GDI1, GDI2 }
WhitelistedStructures = { "afld", "hand", "hq", "nuke", "silo", "proc", "sam" }

NodUnitsBikes = { "bike", "bike", "bike" }
NodUnitsEngineers = { "e6", "e6" }
NodUnitsRockets = { "e3", "e3", "e3", "e3" }
NodUnitsGunners = { "e1", "e1", "e1", "e1" }
NodUnitsFlamers = { "e4", "e4", "e4", "e4" }
ReinforcementsRockets = { "e3", "e3", "e3", "e3", "e3" }

NodBase = { NodBuilding1, NodBuilding2, NodBuilding3, NodHarvester }

AbandonedBaseTrigger = { CPos.New(12, 42), CPos.New(11, 42), CPos.New(10, 42), CPos.New(13, 41), CPos.New(12, 41), CPos.New(11, 41), CPos.New(14, 40), CPos.New(13, 40), CPos.New(12, 40), CPos.New(6, 40), CPos.New(5, 40), CPos.New(4, 40), CPos.New(6, 39), CPos.New(5, 39), CPos.New(4, 39), CPos.New(6, 38), CPos.New(5, 38), CPos.New(4, 38) }
ReinforcementsTrigger = { CPos.New(35, 23), CPos.New(34, 23), CPos.New(35, 22), CPos.New(34, 22), CPos.New(35, 21), CPos.New(34, 21), CPos.New(35, 20), CPos.New(34, 20), CPos.New(35, 19), CPos.New(34, 19), CPos.New(35, 18), CPos.New(34, 18), CPos.New(35, 17), CPos.New(34, 17), CPos.New(35, 16), CPos.New(34, 16), CPos.New(35, 15), CPos.New(34, 15), CPos.New(35, 14), CPos.New(34, 14), CPos.New(35, 13), CPos.New(34, 13), CPos.New(35, 12), CPos.New(34, 12), CPos.New(47, 11), CPos.New(46, 11), CPos.New(57, 19), CPos.New(56, 19), CPos.New(55, 19), CPos.New(54, 19), CPos.New(53, 19), CPos.New(52, 19), CPos.New(51, 19), CPos.New(50, 19), CPos.New(49, 19), CPos.New(48, 19), CPos.New(47, 19), CPos.New(46, 19), CPos.New(57, 18), CPos.New(56, 18), CPos.New(55, 18), CPos.New(54, 18), CPos.New(53, 18), CPos.New(52, 18), CPos.New(51, 18), CPos.New(50, 18), CPos.New(49, 18), CPos.New(48, 18), CPos.New(47, 18), CPos.New(46, 18), CPos.New(47, 17), CPos.New(46, 17), CPos.New(47, 16), CPos.New(46, 16), CPos.New(47, 15), CPos.New(46, 15), CPos.New(47, 14), CPos.New(46, 14), CPos.New(47, 13), CPos.New(46, 13), CPos.New(47, 12), CPos.New(46, 12) }

SamSiteGoal = 3

CaptureStructures = function(actor)
	for i = 1, #WhitelistedStructures do
		local structures = Nod.GetActorsByType(WhitelistedStructures[i])
		if #structures > 0 and not actor.IsDead and not structures[1].IsDead then
			actor.Capture(structures[1])
			return
		end
	end
end

CheckForSams = function()
	local sams = Nod.GetActorsByType("sam")
	return #sams >= SamSiteGoal
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, { "ltnk" }, { ReinforcementsTopSpawn.Location, ReinforcementsTank1Rally.Location }, 1)
	Reinforcements.Reinforce(Nod, NodUnitsEngineers, { ReinforcementsTopSpawn.Location, ReinforcementsEngineersRally.Location }, 10)
	Reinforcements.Reinforce(Nod, NodUnitsRockets, { ReinforcementsTopSpawn.Location, ReinforcementsRocketsRally.Location }, 10)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(Nod, NodUnitsGunners, { ReinforcementsBottomSpawn.Location, ReinforcementsGunnersRally.Location }, 10)
		Reinforcements.Reinforce(Nod, NodUnitsFlamers, { ReinforcementsTopSpawn.Location, ReinforcementsFlamersRally.Location }, 10)
		Reinforcements.Reinforce(Nod, { "ltnk" }, { ReinforcementsBottomSpawn.Location, ReinforcementsTank2Rally.Location }, 10)
	end)
end

SendAttackWave = function(team)
	Utils.Do(team.units, function(unitType)
		local actors = Utils.Where(GDI.GetActorsByType(unitType), function(unit) return unit.IsIdle end)
		MoveAndHunt(Utils.Take(1, actors), team.waypoints)
	end)
end

SendGDIAirstrike = function(hq, delay)
	if not hq.IsDead and hq.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)

		if target then
			hq.TargetAirstrike(target, Angle.NorthEast + Angle.New(16))
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
	Reinforcements.ReinforceWithTransport(GDI, "apc", team.units, { ReinforcementsGDISpawn.Location, waypoint12.Location }, nil, function(transport, passengers)
		Utils.Do(team.waypoints, function(waypoint)
			transport.Move(waypoint.Location)
		end)

		transport.UnloadPassengers()
		Trigger.OnPassengerExited(transport, function(_, passenger)
			if passenger.Type == "e6" then
				Trigger.OnIdle(passenger, CaptureStructures)
			else
				IdleHunt(passenger)
			end

			if not transport.HasPassengers then
				IdleHunt(transport)
			end
		end)
	end)
end

Trigger.OnEnteredFootprint(AbandonedBaseTrigger, function(a, id)
	if not AbandonedBaseTriggered and a.Owner == Nod then
		AbandonedBaseTriggered = true
		Trigger.RemoveFootprintTrigger(id)

		FlareCamera = Actor.Create("camera", true, { Owner = Nod, Location = waypoint25.Location })
		Flare = Actor.Create("flare", true, { Owner = Nod, Location = waypoint25.Location })

		Utils.Do(NodBase, function(actor)
			if not actor.IsDead then
				actor.Owner = Nod
			end
		end)

		Nod.MarkCompletedObjective(FindBase)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Media.PlaySpeechNotification(Nod, "NewOptions")
		end)
	end
end)

Trigger.OnEnteredFootprint(ReinforcementsTrigger, function(a, id)
	if not ReinforcementsTriggered and a.Owner == Nod and a.Type ~= "harv" then
		ReinforcementsTriggered = true
		Trigger.RemoveFootprintTrigger(id)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, "tran.in", ReinforcementsRockets, { ReinforcementsHelicopterSpawn.Location, waypoint24.Location }, { ReinforcementsHelicopterSpawn.Location })
		end)

		SendWaves(1, IntroAttackWaves)

		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() SendWaves(1, AutoAttackWaves) end)
		Trigger.AfterDelay(DateTime.Minutes(2), function()
			Flare.Destroy()
			FlareCamera.Destroy()
		end)
	end
end)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()
	StartAI()

	InitObjectives(Nod)

	FindBase = AddPrimaryObjective(Nod, "find-nod-base")
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	local buildSAMs = UserInterface.Translate("build-sams", { ["sams"] = SamSiteGoal })
	BuildSAMs = AddPrimaryObjective(Nod, buildSAMs)
	GDIObjective = AddPrimaryObjective(GDI, "")

	Trigger.OnKilled(GDIProc, function()
		Actor.Create("moneycrate", true, { Owner = GDI, Location = CPos.New(24, 54) })
	end)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(EliminateGDI)
	end

	if not Nod.IsObjectiveCompleted(BuildSAMs) and CheckForSams() then
		Nod.MarkCompletedObjective(BuildSAMs)
	end
end
