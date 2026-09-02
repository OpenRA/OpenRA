--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AlliedReinforcementsA = { "e1", "e1", "e1", "e1", "e1" }
AlliedReinforcementsB = { "e3", "e3", "e3", "e3", "e3" }
AlliedBoatReinforcements = { "pt", "pt" }
BadGuys = { BadGuy1, BadGuy2, BadGuy3, BadGuy4 }

SovietDogPatrols =
{
	{ Patrol_1_e1, Patrol_1_dog },
	{ Patrol_2_e1, Patrol_2_dog },
	{ Patrol_3_e1, Patrol_3_dog },
	{ Patrol_4_e1, Patrol_4_dog }
}

SovietDogPatrolPaths =
{
	{ Patrol6.Location, Patrol7.Location, Patrol8.Location, Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location },
	{ Patrol8.Location, Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location },
	{ Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location, Patrol8.Location },
	{ Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location, Patrol8.Location, Patrol1.Location }
}

Mammoths = { Mammoth1, Mammoth2, Mammoth3 }

SovietMammothPaths =
{
	{ TnkPatrol1.Location, TnkPatrol2.Location,TnkPatrol3.Location, TnkPatrol4.Location, TnkPatrol5.Location, TnkPatrol6.Location },
	{ TnkPatrol5.Location, TnkPatrol6.Location, TnkPatrol1.Location, TnkPatrol2.Location, TnkPatrol3.Location, TnkPatrol4.Location },
	{ TnkPatrol6.Location, TnkPatrol1.Location, TnkPatrol2.Location, TnkPatrol3.Location, TnkPatrol4.Location, TnkPatrol5.Location }
}

SubPaths = {
	{ SubPatrol1_1.Location, SubPatrol1_2.Location },
	{ SubPatrol2_1.Location, SubPatrol2_2.Location },
	{ SubPatrol3_1.Location, SubPatrol3_2.Location }
}

ParadropWaypoints =
{
	easy = { UnitBStopLocation },
	normal = { UnitBStopLocation, UnitAStopLocation },
	hard = { UnitBStopLocation, UnitCStopLocation, UnitAStopLocation }
}

SovietTechLabs = { TechLab1, TechLab2 }

GroupPatrol = function(units, waypoints, delay)
	local i = 1
	local stop = false

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end
			if unit.Location == waypoints[i] then
				local bool = Utils.All(units, function(actor) return actor.IsIdle or actor.IsDead end)
				if bool then
					stop = true
					i = i + 1
					if i > #waypoints then
						i = 1
					end
					Trigger.AfterDelay(delay, function() stop = false end)
				end
			else
				unit.AttackMove(waypoints[i])
			end
		end)
	end)
end

InitialSovietPatrols = function()
	-- Dog Patrols
	BeachDog.Patrol({ BeachPatrol1.Location, BeachPatrol2.Location, BeachPatrol3.Location })
	for i = 1, 4 do
		GroupPatrol(SovietDogPatrols[i], SovietDogPatrolPaths[i], DateTime.Seconds(5))
	end

	-- Mammoth Patrols
	for i = 1, 3 do
		Trigger.AfterDelay(DateTime.Seconds(6 * (i - 1)), function()
			Trigger.OnIdle(Mammoths[i], function()
				Mammoths[i].Patrol(SovietMammothPaths[i])
			end)
		end)
	end

	-- Sub Patrols
	Patrol1Sub.Patrol(SubPaths[1])
	Patrol2Sub.Patrol(SubPaths[2])
	Patrol3Sub.Patrol(SubPaths[3])
end

MarkNavalObjective = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		InfiltrateTechCenterObj = InfiltrateTechCenterObj or AddPrimaryObjective(Greece, "infiltrate-tech-center-spy")
		Greece.MarkCompletedObjective(NavalYardObj)
		Media.PlaySpeechNotification(Greece, "FirstObjectiveMet")
	end)
end

--- Once Greece is secure with a ship yard, send subs to investigate the coast.
--- Any sub's death will trigger production of more subs, if not yet started.
CheckNavalObjective = function()
	if Greece.IsObjectiveCompleted(NavalYardObj) or not Greece.HasPrerequisites({ "syrd" }) then
		return
	end

	local eastBase = { EastFlame1, EastFlame2, SovietBarracks }
	local eastBaseDefeated = Utils.All(eastBase, function(building)
		return building.IsDead or building.Owner ~= USSR
	end)

	if not eastBaseDefeated then
		return
	end

	MarkNavalObjective()

	if not ScoutSub1.IsDead then
		-- NE and NW corners of the middle island, then back to NE.
		local path = { SubPatrol3_2.Location, SubPatrol3_1.Location, Harbor.Location }
		ScoutSub1.Patrol(path, false)
	end

	Trigger.AfterDelay(DateTime.Seconds(150), function()
		IdleHunt(ScoutSub2)
	end)
end

InitialAlliedReinforcements = function()
	local camera = Actor.Create("Camera", true, { Owner = Greece, Location = DefaultCameraPosition.Location })
	Trigger.AfterDelay(DateTime.Seconds(30), camera.Destroy)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.Reinforce(Greece, AlliedReinforcementsA, { AlliedEntry3.Location, UnitCStopLocation.Location }, 2)
		Reinforcements.Reinforce(Greece, AlliedReinforcementsB, { AlliedEntry2.Location, UnitAStopLocation.Location }, 2)
	end)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		local mcv = Reinforcements.Reinforce(Greece, { "mcv" }, { AlliedEntry1.Location, UnitBStopLocation.Location })[1]
		Trigger.OnRemovedFromWorld(mcv, ActivateAI)
		Reinforcements.Reinforce(Greece, AlliedBoatReinforcements, { AlliedBoatEntry.Location, AlliedBoatStop.Location })
	end)
end

CaptureRadarDome = function()
	Trigger.OnKilled(RadarDome, function()
		if Greece.IsObjectiveCompleted(CaptureRadarDomeObj) then
			return
		end

		Greece.MarkFailedObjective(CaptureRadarDomeObj)
	end)

	Trigger.OnCapture(RadarDome, function()
		Greece.MarkCompletedObjective(CaptureRadarDomeObj)

		Utils.Do(SovietTechLabs, function(a)
			if a.IsDead then
				return
			end

			Beacon.New(Greece, a.CenterPosition)
			if Difficulty ~= "hard" then
				Actor.Create("TECH.CAM", true, { Owner = Greece, Location = a.Location + CVec.New(1, 1) })
			end
		end)

		Media.DisplayMessage(UserInterface.GetFluentMessage("soviet-tech-centers-discovered"))

		if Difficulty == "easy" then
			Actor.Create("Camera", true, { Owner = Greece, Location = Weapcam.Location })
		end
	end)
end

FailTechCenter = function(killed)
	local speechDelay = 0

	if not killed then
		-- Let the capture speech play first.
		speechDelay = 36
	end

	Trigger.AfterDelay(speechDelay, function()
		Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	end)

	Trigger.AfterDelay(speechDelay + DateTime.Seconds(1), function()
		InfiltrateTechCenterObj = InfiltrateTechCenterObj or AddPrimaryObjective(Greece, "infiltrate-tech-center-spy")
		Greece.MarkFailedObjective(InfiltrateTechCenterObj)
	end)
end

InfiltrateTechCenter = function()
	local infiltrated = false
	local allKilled = false

	Utils.Do(SovietTechLabs, function(a)
		Trigger.OnInfiltrated(a, function()
			if infiltrated then
				return
			end

			infiltrated = true
			InfiltrateTechCenterObj = InfiltrateTechCenterObj or AddPrimaryObjective(Greece, "infiltrate-tech-center-spy")

			-- Let the infiltration speech play first.
			Trigger.AfterDelay(38, function()
				Media.PlaySpeechNotification(Greece, "SecondObjectiveMet")
				DestroySovietsObj = AddPrimaryObjective(Greece, "destroy-soviet-buildings-units")
				Greece.MarkCompletedObjective(InfiltrateTechCenterObj)

				local proxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
				Utils.Do(ParadropWaypoints[Difficulty], function(waypoint)
					local plane = proxy.TargetParatroopers(waypoint.CenterPosition, Angle.South)[1]
					Trigger.OnPassengerExited(plane, function(_, passenger)
						IdleHunt(passenger)
					end)
				end)
				proxy.Destroy()
			end)
		end)

		Trigger.OnCapture(a, function()
			if not infiltrated then
				Media.PlaySoundNotification(Greece, "AlertBleep")
				Media.DisplayMessage(UserInterface.GetFluentMessage("do-not-capture-tech-centers"))
			end
		end)
	end)

	Trigger.OnAllKilled(SovietTechLabs, function()
		allKilled = true
	end)

	Trigger.OnAllKilledOrCaptured(SovietTechLabs, function()
		if infiltrated then
			return
		end

		Trigger.AfterDelay(1, function()
			FailTechCenter(allKilled)
		end)
	end)
end

Tick = function()
	if DestroySovietsObj and USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(DestroySovietsObj)
	end

	if not Greece.HasNoRequiredUnits() then
		return
	end

	Utils.Do({ NavalYardObj, InfiltrateTechCenterObj, DestroySovietsObj }, function(objective)
		if Greece.IsObjectiveCompleted(objective) then
			return
		end

		Greece.MarkFailedObjective(objective)
	end)
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	InitObjectives(Greece)

	NavalYardObj = AddPrimaryObjective(Greece, "build-naval-yard-redeploy-mcv")
	CaptureRadarDomeObj = AddSecondaryObjective(Greece, "capture-radar-shore")

	Camera.Position = DefaultCameraPosition.CenterPosition

	Trigger.OnEnteredProximityTrigger(SovietDefenseCam.CenterPosition, WDist.FromCells(7), function(a, id)
		if a.Owner ~= Greece then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		local revealTargets = { SovietDefenseCam, StartFlame1, StartFlame2 }

		Utils.Do(revealTargets, function(target)
			if not target.IsInWorld then
				return
			end

			local reveal = Actor.Create("TECH.CAM", true, { Owner = Greece, Location = target.Location })
			Trigger.AfterDelay(DateTime.Seconds(20), reveal.Destroy)
		end)
	end)

	Utils.Do(BadGuys, function(bg)
		if bg == BadGuy3 or bg == BadGuy4 then
			bg.AttackMove(UnitCStopLocation.Location)
			IdleHunt(bg)
			return
		end

		Trigger.OnEnteredProximityTrigger(bg.CenterPosition, WDist.FromCells(7), function(a, id)
			if a.Owner ~= Greece or a.Type == "pt" or a.Type == "camera" then
				return
			end

			Trigger.RemoveProximityTrigger(id)
			IdleHunt(bg)
		end)

		Trigger.OnDamaged(bg, function()
			if bg.IsIdle then
				IdleHunt(bg)
			end
		end)
	end)

	Trigger.OnKilled(StartBarrel1, function()
		if not StartFlame1.IsDead then
			StartFlame1.Kill()
		end
	end)
	Trigger.OnKilled(StartBarrel2, function()
		if not StartFlame2.IsDead then
			StartFlame2.Kill()
		end
	end)

	InitialAlliedReinforcements()
	Trigger.AfterDelay(DateTime.Seconds(1), InitialSovietPatrols)

	Trigger.OnEnteredProximityTrigger(SovietMiniBaseCam.CenterPosition, WDist.FromCells(14), function(a, id)
		if a.Owner ~= Greece or a.Type == "pt" or a.Type == "lst" then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		local cam = Actor.Create("Camera", true, { Owner = Greece, Location = SovietMiniBaseCam.Location })
		Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
	end)

	CaptureRadarDome()
	InfiltrateTechCenter()
	Trigger.OnBuildingPlaced(Greece, CheckNavalObjective)
	Trigger.OnAllKilledOrCaptured({ EastFlame1, EastFlame2, SovietBarracks }, CheckNavalObjective)
	-- Prepare Soviet attacks if Greece still has not deployed the MCV.
	Trigger.AfterDelay(DateTime.Seconds(90), ActivateAI)
end
