--[[
   Copyright (c) The OpenRA Developers and Contributors
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
		Greece.MarkCompletedObjective(RadarSpyObjective)
		Actor.Create("camera", true, { Owner = Greece, Location = Cam1.Location })
		Actor.Create("camera", true, { Owner = Greece, Location = Cam2.Location })
		Actor.Create("camera", true, { Owner = Greece, Location = Cam3.Location })
		Actor.Create("camera", true, { Owner = Greece, Location = Cam4.Location })
	end)

	Trigger.OnKilled(RadarDome, function()
		if not Greece.IsObjectiveCompleted(RadarSpyObjective) then
			Greece.MarkFailedObjective(RadarSpyObjective)
		end
	end)

	Trigger.OnAllKilled(ConvoyTrucks, function()
		Greece.MarkCompletedObjective(DestroyAllTrucksObjective)
	end)
end

MissionStart = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.Reinforce(Greece, CombatTeam1, { TruckEscapeCenter.Location, DefaultCameraPosition.Location })
		local StartCamera = Actor.Create("camera", true, { Owner = Greece, Location = DefaultCameraPosition.Location })
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
		CombatTeam2 = CombatTeam2[Difficulty]
		Reinforcements.Reinforce(Greece, CombatTeam2, { TruckEscapeCenter.Location, DefaultCameraPosition.Location })
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	end)
end

SendPatrol = function(mammoth)
	if not mammoth.IsDead then
		mammoth.Patrol(MammothPath, true, 20)
	end
end

MoveTruckEscapeRoute = function(truck, route)
	if truck.IsDead then
		return
	else
		Media.DisplayMessage(UserInterface.Translate("convoy-truck-escaping"))
		Media.PlaySoundNotification(Greece, "AlertBleep")
		Utils.Do(route, function(waypoint)
			truck.Move(waypoint.Location)
		end)

		Trigger.OnIdle(truck, function()
			if truck.Location == route[#route].Location then
				truck.Destroy()
				Greece.MarkFailedObjective(DestroyAllTrucksObjective)
			else
				truck.Move(route[#route].Location)
			end
		end)
	end
end

Tick = function()
	USSR.Cash = 5000
	BadGuy.Cash = 5000

	if USSR.HasNoRequiredUnits() and BadGuy.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(KillAllObjective)
	end

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRobjective)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	InitObjectives(Greece)

	DestroyAllTrucksObjective = AddPrimaryObjective(Greece, "prevent-soviet-trucks-escaping")
	KillAllObjective = AddPrimaryObjective(Greece, "clear-sector-soviet-presence")
	RadarSpyObjective = AddSecondaryObjective(Greece, "infiltrate-radar-reveal-escape-routes")
	USSRobjective = USSR.AddObjective("")

	ActivateAI()
	SetupTriggers()
	MissionStart()

	Camera.Position = DefaultCameraPosition.CenterPosition

	Trigger.AfterDelay(DateTime.Minutes(5), function() SendPatrol(PatrolMammoth) end)
	Trigger.AfterDelay(DateTime.Minutes(5), function() MoveTruckEscapeRoute(Truck1, TruckEscapeNorth) end)
	Trigger.AfterDelay(DateTime.Minutes(9), function() MoveTruckEscapeRoute(Truck2, TruckEscapeNorth) end)
	Trigger.AfterDelay(DateTime.Minutes(12), function() MoveTruckEscapeRoute(Truck3, TruckEscapeSouth) end)
	Trigger.AfterDelay(DateTime.Minutes(15), function() MoveTruckEscapeRoute(Truck4, TruckEscapeNorth) end)
	Trigger.AfterDelay(DateTime.Minutes(17), function() MoveTruckEscapeRoute(Truck5, TruckEscapeSouth) end)
	Trigger.AfterDelay(DateTime.Minutes(18), function() MoveTruckEscapeRoute(IntroTruck2, TruckEscapeSouth) end)
end
