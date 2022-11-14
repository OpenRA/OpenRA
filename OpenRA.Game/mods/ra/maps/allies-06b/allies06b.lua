--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedReinforcementsA = { "e1", "e1", "e1", "e1", "e1" }
AlliedReinforcementsB = { "e1", "e1", "e3", "e3", "e3" }
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
	{ SubPatrol3_1.Location, SubPatrol3_2.Location },
	{ SubPatrol4_1.Location, SubPatrol4_2.Location },
	{ SubPatrol5_1.Location, SubPatrol5_2.Location }
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
				local bool = Utils.All(units, function(actor) return actor.IsIdle end)
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
	Patrol4Sub.Patrol(SubPaths[4])
	Patrol5Sub.Patrol(SubPaths[5])
end

InitialAlliedReinforcements = function()
	local camera = Actor.Create("Camera", true, { Owner = player, Location = DefaultCameraPosition.Location })
	Trigger.AfterDelay(DateTime.Seconds(30), camera.Destroy)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
	Reinforcements.Reinforce(player, AlliedReinforcementsA, { AlliedEntry3.Location, UnitCStopLocation.Location }, 2)
		Reinforcements.Reinforce(player, AlliedReinforcementsB, { AlliedEntry2.Location, UnitAStopLocation.Location }, 2)
	end)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(player, { "mcv" }, { AlliedEntry1.Location, UnitBStopLocation.Location })
		Reinforcements.Reinforce(player, AlliedBoatReinforcements, { AlliedBoatEntry.Location, AlliedBoatStop.Location })
	end)
end

CaptureRadarDome = function()
	Trigger.OnKilled(RadarDome, function()
		player.MarkFailedObjective(CaptureRadarDomeObj)
	end)

	Trigger.OnCapture(RadarDome, function()
		player.MarkCompletedObjective(CaptureRadarDomeObj)

		Utils.Do(SovietTechLabs, function(a)
			if a.IsDead then
				return
			end

			Beacon.New(player, a.CenterPosition)
			if Difficulty ~= "hard" then
				Actor.Create("TECH.CAM", true, { Owner = player, Location = a.Location + CVec.New(1, 1) })
			end
		end)

		Media.DisplayMessage("Coordinates of the Soviet tech centers discovered.")

		if Difficulty == "easy" then
			Actor.Create("Camera", true, { Owner = player, Location = Weapcam.Location })
		end
	end)
end

InfiltrateTechCenter = function()
	Utils.Do(SovietTechLabs, function(a)
		Trigger.OnInfiltrated(a, function()
			if infiltrated then
				return
			end
			infiltrated = true
			DestroySovietsObj = player.AddObjective("Destroy all Soviet buildings and units in the area.")
			player.MarkCompletedObjective(InfiltrateTechCenterObj)
		end)

		Trigger.OnCapture(a, function()
			if not infiltrated then
				Media.DisplayMessage("Do not capture the tech centers! Infiltrate one with a spy.")
			end
		end)
	end)

	Trigger.OnAllKilledOrCaptured(SovietTechLabs, function()
		if not player.IsObjectiveCompleted(InfiltrateTechCenterObj) then
			player.MarkFailedObjective(InfiltrateTechCenterObj)
		end
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(InfiltrateTechCenterObj)
	end

	if DestroySovietsObj and ussr.HasNoRequiredUnits() then
		player.MarkCompletedObjective(DestroySovietsObj)
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")

	InitObjectives(player)

	InfiltrateTechCenterObj = player.AddObjective("Infiltrate one of the Soviet tech centers with a spy.")
	CaptureRadarDomeObj = player.AddObjective("Capture the Radar Dome at the shore.", "Secondary", false)

	Camera.Position = DefaultCameraPosition.CenterPosition

	if Difficulty == "easy" then
		Trigger.OnEnteredProximityTrigger(SovietDefenseCam.CenterPosition, WDist.New(1024 * 7), function(a, id)
			if a.Owner == player then
				Trigger.RemoveProximityTrigger(id)
				local cam1 = Actor.Create("TECH.CAM", true, { Owner = player, Location = SovietDefenseCam.Location })
				Trigger.AfterDelay(DateTime.Seconds(15), cam1.Destroy)
				if not DefenseFlame1.IsDead then
					local cam2 = Actor.Create("TECH.CAM", true, { Owner = player, Location = DefenseFlame1.Location })
					Trigger.AfterDelay(DateTime.Seconds(15), cam2.Destroy)
				end
				if not DefenseFlame2.IsDead then
					local cam3 = Actor.Create("TECH.CAM", true, { Owner = player, Location = DefenseFlame2.Location })
					Trigger.AfterDelay(DateTime.Seconds(15), cam3.Destroy)
				end
			end
		end)
	end

	if Difficulty ~= "hard" then
		Trigger.OnKilled(DefBrl1, function(a, b)
			if not DefenseFlame1.IsDead then
				DefenseFlame1.Kill()
			end
		end)
		Trigger.OnKilled(DefBrl2, function(a, b)
			if not DefenseFlame2.IsDead then
				DefenseFlame2.Kill()
			end
		end)
	end

	Utils.Do(BadGuys, function(a)
		a.AttackMove(UnitCStopLocation.Location)
	end)

	InitialAlliedReinforcements()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		InitialSovietPatrols()
	end)

	Trigger.OnEnteredProximityTrigger(SovietMiniBaseCam.CenterPosition, WDist.New(1024 * 14), function(a, id)
		if a.Owner == player then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = player, Location = SovietMiniBaseCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
		end
	end)
	CaptureRadarDome()
	InfiltrateTechCenter()
	Trigger.AfterDelay(0, ActivateAI)
end
