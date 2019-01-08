--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
MammothPath = { Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location }
ConvoyTrucks = { Truck1, Truck2, Truck3, Truck4, Truck5, IntroTruck1, IntroTruck2 }
IntroTrucks = {  IntroTruck1, IntroTruck2 }
TruckEscapeNorth = { EscapeNorth1, EscapeNorth2, EscapeNorth3, EscapeNorth4, EscapeNorth5, EscapeNorth6, EscapeNorth7, EscapeNorth8, EscapeNorth9, EscapeNorth10 }
TruckEscapeSouth = { EscapeSouth1, EscapeSouth2, EscapeSouth3, EscapeSouth4, EscapeSouth5 }
SovAttackStart = { StartTank, StartRifle1, StartRifle2, StartRifle3, StartRifle4 }
SovAttackStart2 = { StartRifle5, StartRifle6, StartGren }
RunAway = { IntroTruck2, StartRifle3, StartRifle4 }
GreeceRifles = { GreeceRifle1, GreeceRifle2, GreeceRifle3, GreeceRifle4, GreeceRifle5 }
CombatTeam1 = { "mnly", "spy", "spy", "mcv" }
CombatTeam2 = 
{ 
	easy = { "2tnk", "2tnk", "2tnk", "e3", "e3", "e3" },
	normal = { "e3", "e3", "e3", "2tnk", "1tnk" },
	hard = { "e3", "e3", "e3", "1tnk" }
}

SetupTriggers = function()
	Trigger.OnInfiltrated(RadarDome, function()
		greece.MarkCompletedObjective(objRadarSpy)
		Actor.Create("camera", true, { Owner = greece, Location = Cam1.Location })
		Actor.Create("camera", true, { Owner = greece, Location = Cam2.Location })
		Actor.Create("camera", true, { Owner = greece, Location = Cam3.Location })
		Actor.Create("camera", true, { Owner = greece, Location = Cam4.Location })
	end)

	Trigger.OnKilled(RadarDome, function()
		if not greece.IsObjectiveCompleted(objRadarSpy) then
			greece.MarkFailedObjective(objRadarSpy)
		end
	end)

	Trigger.OnAllKilled(ConvoyTrucks, function()
		greece.MarkCompletedObjective(objDestroyAllTrucks)
	end)

	Trigger.OnEnteredFootprint({ TruckEscapeCenter.Location }, function(actor, triggerlose1)
		if actor.Owner == ussr and actor.Type == "truk" then
			Trigger.RemoveProximityTrigger(triggerlose1)
			actor.Destroy()
			greece.MarkFailedObjective(objDestroyAllTrucks)
		end
	end)

	Trigger.OnEnteredFootprint({ EscapeNorth10.Location }, function(actor, triggerlose2)
		if actor.Owner == ussr and actor.Type == "truk" then
			Trigger.RemoveProximityTrigger(triggerlose2)
			actor.Destroy()
			greece.MarkFailedObjective(objDestroyAllTrucks)
		end
	end)

	Trigger.OnEnteredFootprint({ EscapeSouth5.Location }, function(actor, triggerlose3)
		if actor.Owner == ussr and actor.Type == "truk" then
			Trigger.RemoveProximityTrigger(triggerlose3)
			actor.Destroy()
			greece.MarkFailedObjective(objDestroyAllTrucks)
		end
	end)
end

MissionStart = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.Reinforce(greece, CombatTeam1, { TruckEscapeCenter.Location, DefaultCameraPosition.Location })
		local StartCamera = Actor.Create("camera", true, { Owner = greece, Location = DefaultCameraPosition.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			StartCamera.Destroy()
		end)
	end)

	Utils.Do(GreeceRifles, function(actor)
		actor.Move(DefaultCameraPosition.Location)
	end)

	Utils.Do(SovAttackStart, function(actor)
		actor.AttackMove(DefaultCameraPosition.Location)
	end)

	Utils.Do(IntroTrucks, function(truck)
		truck.Move(TruckEscapeCenter.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(RunAway, function(actor)
			if actor.IsDead then
				return
			else
				actor.Stop()
				actor.Move(Cam4.Location)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Utils.Do(SovAttackStart2, function(actor)
			if actor.IsDead then
				return
			else
				actor.AttackMove(DefaultCameraPosition.Location)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		local difficulty = Map.LobbyOption("difficulty")
		CombatTeam2 = CombatTeam2[difficulty]
		Reinforcements.Reinforce(greece, CombatTeam2, { TruckEscapeCenter.Location, DefaultCameraPosition.Location })
		Media.PlaySpeechNotification(greece, "ReinforcementsArrived")
	end)
end

SendPatrol = function(mammoth)
	if not mammoth.IsDead then
		mammoth.Patrol(MammothPath, true, 20)
	end
end

MoveTruckNorth = function(truck)
	if truck.IsDead then
		return
	else
		Media.DisplayMessage("Convoy truck attempting to escape!")
		Media.PlaySoundNotification(greece, "AlertBleep")
		Utils.Do(TruckEscapeNorth, function(waypoint)
			truck.Move(waypoint.Location)
		end)
	end
end

MoveTruckSouth = function(truck)
	if truck.IsDead then
		return
	else
		Media.DisplayMessage("Convoy truck attempting to escape!")
		Media.PlaySoundNotification(greece, "AlertBleep")
		Utils.Do(TruckEscapeSouth, function(waypoint)
			truck.Move(waypoint.Location)
		end)
	end
end

Tick = function()
	ussr.Cash = 5000
	badguy.Cash = 5000

	if ussr.HasNoRequiredUnits() and badguy.HasNoRequiredUnits() then
		greece.MarkCompletedObjective(objKillAll)
	end
end

WorldLoaded = function()
	greece = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")
	badguy = Player.GetPlayer("BadGuy")
	
	Trigger.OnObjectiveAdded(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	objDestroyAllTrucks = greece.AddPrimaryObjective("Prevent Soviet convoy trucks from escaping.")
	objKillAll = greece.AddPrimaryObjective("Clear the sector of all Soviet presence.")
	objRadarSpy = greece.AddSecondaryObjective("Infiltrate the Soviet Radar Dome to reveal truck \necape routes.")
	ussrObj = ussr.AddPrimaryObjective("Deny the Allies.")

	Trigger.OnObjectiveCompleted(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(greece, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)
	Trigger.OnPlayerWon(greece, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	ActivateAI()
	SetupTriggers()
	MissionStart()

	Camera.Position = DefaultCameraPosition.CenterPosition

	Trigger.AfterDelay(DateTime.Minutes(5), function() SendPatrol(PatrolMammoth) end)
	Trigger.AfterDelay(DateTime.Minutes(5), function() MoveTruckNorth(Truck1) end)
	Trigger.AfterDelay(DateTime.Minutes(9), function() MoveTruckNorth(Truck2) end)
	Trigger.AfterDelay(DateTime.Minutes(12), function() MoveTruckSouth(Truck3) end)
	Trigger.AfterDelay(DateTime.Minutes(15), function() MoveTruckNorth(Truck4) end)
	Trigger.AfterDelay(DateTime.Minutes(17), function() MoveTruckSouth(Truck5) end)
	Trigger.AfterDelay(DateTime.Minutes(18), function() MoveTruckSouth(IntroTruck2) end)
end
