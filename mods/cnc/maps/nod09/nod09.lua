--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "easy" then
	Rambo = "rmbo.easy"
elseif Difficulty == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

SamSiteGoal = 3

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

CheckForSams = function(Nod)
	local sams = Nod.GetActorsByType("sam")
	return #sams >= SamSiteGoal
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

		for type, amount in pairs(team.units) do
			MoveAndHunt(Utils.Take(amount, GDI.GetActorsByType(type)), team.waypoints)
		end

		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

StartPatrols = function()
	local mtnks = GDI.GetActorsByType("mtnk")
	local msams = GDI.GetActorsByType("msam")

	if #mtnks >= 1 then
		mtnks[1].Patrol(Patrol1Waypoints, true, 20)
	end

	if #msams >= 1 then
		msams[1].Patrol(Patrol2Waypoints, true, 20)
	end
end

Trigger.OnEnteredFootprint(NodBaseTrigger, function(a, id)
	if not Nod.IsObjectiveCompleted(LocateNodBase) and a.Owner == Nod then
		Trigger.RemoveFootprintTrigger(id)

		Nod.MarkCompletedObjective(LocateNodBase)
		NodCYard.Owner = Nod

		local walls = NodBase.GetActorsByType("brik")
		Utils.Do(walls, function(actor)
			actor.Owner = Nod
		end)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(Nod, "NewOptions")
		end)
	end
end)

Trigger.OnEnteredFootprint(RocketTrigger, function(a, id)
	if not Nod.IsObjectiveCompleted(SecureFirstLanding) and a.Owner == Nod then
		Trigger.RemoveFootprintTrigger(id)

		Nod.MarkCompletedObjective(SecureFirstLanding)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, "tran.in", RocketReinforcements, { HelicopterEntryRocket.Location, HelicopterGoalRocket.Location }, { HelicopterEntryRocket.Location }, nil, nil)
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			EngineerFlareCamera1 = Actor.Create("camera", true, { Owner = Nod, Location = FlareEngineer.Location })
			EngineerFlareCamera2 = Actor.Create("camera", true, { Owner = Nod, Location = CameraEngineerPath.Location })
			EngineerFlare = Actor.Create("flare", true, { Owner = Nod, Location = FlareEngineer.Location })
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			RocketFlareCamera.Destroy()
			RocketFlare.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(EngineerTrigger, function(a, id)
	if not Nod.IsObjectiveCompleted(SecureSecondLanding) and a.Owner == Nod then
		Trigger.RemoveFootprintTrigger(id)

		Nod.MarkCompletedObjective(SecureSecondLanding)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, "tran.in", EngineerReinforcements, { HelicopterEntryEngineer.Location, HelicopterGoalEngineer.Location }, { HelicopterEntryEngineer.Location }, nil, nil)
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			if EngineerFlareCamera1 then
				EngineerFlareCamera1.Destroy()
				EngineerFlareCamera2.Destroy()
				EngineerFlare.Destroy()
			end
		end)
	end
end)

Trigger.OnKilledOrCaptured(OutpostProc, function()
	if not Nod.IsObjectiveCompleted(CaptureRefinery) then

		if OutpostProc.IsDead then
			Nod.MarkFailedObjective(CaptureRefinery)
		else
			Nod.MarkCompletedObjective(CaptureRefinery)
			Nod.Cash = 1000
		end

		StartPatrols()

		Trigger.AfterDelay(DateTime.Minutes(1), function() SendWaves(1, AutoAttackWaves) end)
		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceInfantry(GDIPyle) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)
	end
end)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	NodBase = Player.GetPlayer("NodBase")

	Camera.Position = CameraIntro.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.ReinforceWithTransport(Nod, "tran.in", RmboReinforcements, { HelicopterEntryRmbo.Location, HelicopterGoalRmbo.Location }, { HelicopterEntryRmbo.Location })

	RocketFlareCamera = Actor.Create("camera", true, { Owner = Nod, Location = FlareRocket.Location })
	RocketFlare = Actor.Create("flare", true, { Owner = Nod, Location = FlareRocket.Location })

	StartAI()
	AutoGuard(GDI.GetGroundAttackers())

	InitObjectives(Nod)

	SecureFirstLanding = AddPrimaryObjective(Nod, "secure-first-landing-zone")
	SecureSecondLanding = AddPrimaryObjective(Nod, "secure-second-landing-zone")
	LocateNodBase = AddPrimaryObjective(Nod, "locate-nod-base")
	CaptureRefinery = AddPrimaryObjective(Nod, "capture-refinery")
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	local buildSAMs = UserInterface.Translate("build-sams", { ["sams"] = SamSiteGoal })
	BuildSAMs = AddSecondaryObjective(Nod, buildSAMs)
	GDIObjective = AddPrimaryObjective(GDI, "")
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(EliminateGDI)
	end

	if not Nod.IsObjectiveCompleted(BuildSAMs) and CheckForSams(Nod) then
		Nod.MarkCompletedObjective(BuildSAMs)
	end
end
