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
BadGuys = { BadGuy1, BadGuy2, BadGuy3 }

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
	{ TnkPatrol1.Location, TnkPatrol2.Location,TnkPatrol3.Location, TnkPatrol4.Location, TnkPatrol5.Location, TnkPatrol6.Location, TnkPatrol7.Location, TnkPatrol8.Location },
	{ TnkPatrol5.Location, TnkPatrol6.Location, TnkPatrol7.Location, TnkPatrol8.Location,  TnkPatrol1.Location, TnkPatrol2.Location, TnkPatrol3.Location, TnkPatrol4.Location },
	{ TnkPatrol8.Location, TnkPatrol1.Location, TnkPatrol2.Location, TnkPatrol3.Location, TnkPatrol4.Location, TnkPatrol5.Location,  TnkPatrol6.Location, TnkPatrol7.Location }
}

SovietSubPath = { SubPatrol3_1.Location, SubPatrol3_2.Location, SubPatrol3_3.Location }

ParadropWaypoints =
{
	easy = { UnitBStopLocation },
	normal = { UnitBStopLocation, UnitAStopLocation },
	hard = { UnitBStopLocation, MCVStopLocation, UnitAStopLocation }
}

SovietTechLabs = { TechLab1, TechLab2, TechLab3 }

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
	Patrol1Sub.Patrol({ SubPatrol1_1.Location, SubPatrol1_2.Location })
	Patrol2Sub.Patrol({ SubPatrol2_1.Location, SubPatrol2_2.Location })
	Patrol3Sub.Patrol(SovietSubPath)
end

InitialAlliedReinforcements = function()
	local camera = Actor.Create("Camera", true, { Owner = Greece, Location = DefaultCameraPosition.Location })
	Trigger.AfterDelay(DateTime.Seconds(30), camera.Destroy)

	Reinforcements.Reinforce(Greece, AlliedReinforcementsA, { AlliedEntry1.Location, UnitBStopLocation.Location }, 2)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Reinforcements.Reinforce(Greece, AlliedReinforcementsB, { AlliedEntry2.Location, UnitAStopLocation.Location }, 2)
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local mcv = Reinforcements.Reinforce(Greece, { "mcv" }, { AlliedEntry3.Location, MCVStopLocation.Location })[1]
		Trigger.OnRemovedFromWorld(mcv, ActivateAI)
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

-- Check progress on the Naval Yard and smaller Soviet base.
-- If a Naval Yard is built, send a sub to investigate the coast.
-- Its death will trigger production of more subs, if that's not yet started.
CheckNavalObjective = function()
	if not Greece.HasPrerequisites({ "syrd" } ) then
		Trigger.AfterDelay(DateTime.Seconds(3), CheckNavalObjective)
		return
	end

	local intact = IntactMiniBaseStructures()
	if #intact == 0 then
		MarkNavalObjective()
	else
		Trigger.OnAllKilledOrCaptured(intact, MarkNavalObjective)
	end

	if ScoutSub.IsDead then
		return
	end

	local path = { SubPatrol1_1.Location, SubMeetPoint.Location, Harbor.Location }
	ScoutSub.Patrol(path, false)
	IdleHunt(ScoutSub)
end

MarkNavalObjective = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		InfiltrateTechCenterObj = InfiltrateTechCenterObj or AddPrimaryObjective(Greece, "infiltrate-tech-center-spy")
		Greece.MarkCompletedObjective(NavalYardObj)
		Media.PlaySpeechNotification(Greece, "FirstObjectiveMet")
	end)
end

IntactMiniBaseStructures = function()
	local base = { MiniBaseTower1, MiniBaseTower2, SovietBarracks }
	return Utils.Where(base, function(structure)
		return not structure.IsDead and structure.Owner == USSR
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

	Utils.Do(BadGuys, function(a)
		a.AttackMove(MCVStopLocation.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		InitialAlliedReinforcements()
		InitialSovietPatrols()
	end)

	Trigger.OnEnteredProximityTrigger(SovietMiniBaseCam.CenterPosition, WDist.New(1024 * 6), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = Greece, Location = SovietMiniBaseCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
		end
	end)

	CaptureRadarDome()
	InfiltrateTechCenter()
	Trigger.AfterDelay(DateTime.Minutes(2), CheckNavalObjective)
	-- Prepare Soviet attacks if Greece still has an undeployed MCV.
	Trigger.AfterDelay(DateTime.Seconds(30), ActivateAI)
end
