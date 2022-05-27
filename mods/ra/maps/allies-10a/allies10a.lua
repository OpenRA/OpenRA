--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
StartingUnits = { "mcv", "2tnk", "2tnk", "2tnk", "2tnk" }
MammothWays = { MammothWay1.Location, MammothWay2.Location, MammothWay3.Location, MammothWay4.Location, ParaLZ5.Location }
PeekersA = { Peekaboo1, Peekaboo2, Peekaboo3 }
PeekersB = { Peekaboo4, Peekaboo5 }
AmbushTeam = { "3tnk", "v2rl", "e2", "e2", "e4" }
NorthHarassFootprint = { CPos.New(24, 75), CPos.New(25, 75), CPos.New(26, 75), CPos.New(27, 75), CPos.New(36, 72), CPos.New(37, 72), CPos.New(38, 72), CPos.New(39, 72) }
NorthHarassTeam = { "e2", "e2", "e2", "3tnk" }
MissileSilos = { MissileSilo1, MissileSilo2, MissileSilo3, MissileSilo4 }

TimerColor = Player.GetPlayer("USSR").Color
TimerTicks = DateTime.Minutes(59) + DateTime.Seconds(42)

MissionStart = function()
	Utils.Do(USSR.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Reinforcements.Reinforce(Greece, StartingUnits, { MCVEntry.Location, MCVStop.Location })

	PatrolMammoth.Patrol(MammothWays, true, 20)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Utils.Do(PeekersA, function(unit)
			if not unit.IsDead then
				unit.AttackMove(MCVStop.Location)
				IdleHunt(unit)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(45), function()
		Utils.Do(PeekersB, function(unit)
			if not unit.IsDead then
				unit.AttackMove(AttackWaypoint1.Location)
				IdleHunt(unit)
			end
		end)
	end)
end

MissionTriggers = function()
	Trigger.OnKilled(CommandCenter, function()
		USSR.MarkCompletedObjective(HoldOut)
	end)

	Trigger.OnEnteredProximityTrigger(FCom.CenterPosition, WDist.FromCells(15), function(actor, id)
		if actor.Owner == Greece and not MissilesLaunched then
			Trigger.RemoveProximityTrigger(id)
			LaunchMissiles()
		end
	end)

	Trigger.OnTimerExpired(function()
		DateTime.TimeLimit = 0
		Trigger.AfterDelay(1, function() UserInterface.SetMissionText("We're too late!", USSR.Color) end)
		USSR.MarkCompletedObjective(HoldOut)
	end)

	Trigger.OnKilled(BridgeBarrel, function()
		local bridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)[1]
		if not bridge.IsDead then
			bridge.Kill()
		end
	end)

	Trigger.OnEnteredProximityTrigger(OreAmbushTrigger.CenterPosition, WDist.FromCells(5), function(actor, id)
		if actor.Owner == Greece and actor.Type == "harv" and not Map.LobbyOption("difficulty") == "easy" then
			Trigger.RemoveProximityTrigger(id)
			local ambush = Reinforcements.Reinforce(USSR, AmbushTeam, { OreAmbushEntry.Location})
			Utils.Do(ambush, IdleHunt)
		end
	end)

	local northFootTriggered
	Trigger.OnEnteredFootprint(NorthHarassFootprint, function(actor, id)
		if actor.Owner == Greece and not northFootTriggered then
			Trigger.RemoveFootprintTrigger(id)
			northFootTriggered = true

			local northHarass = Reinforcements.Reinforce(USSR, NorthHarassTeam, { OreAmbushEntry.Location})
			Utils.Do(northHarass, IdleHunt)
		end
	end)
end

LaunchMissiles = function()
	MissilesLaunched = true
	local missileCam = Actor.Create("camera", true, { Owner = Greece, Location = FCom.Location })
	Camera.Position = FCom.CenterPosition
	Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
	MissileSilo1.ActivateNukePower(CPos.New(127, 127))

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("INCOMING TRANSMISSION", "LANDCOM 16")
		Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
		MissileSilo2.ActivateNukePower(CPos.New(127, 127))
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
		MissileSilo3.ActivateNukePower(CPos.New(127, 127))
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
		MissileSilo4.ActivateNukePower(CPos.New(127, 127))
	end)

	Trigger.AfterDelay(DateTime.Seconds(8), function()
		local fmvStart = DateTime.GameTime
		Media.PlayMovieFullscreen("ally10b.vqa", function()
			-- Completing immediately indicates that the FMV is not available
			-- Fall back to a text message
			if fmvStart == DateTime.GameTime then
				Media.DisplayMessage("Commander, we're tracking four missiles. They must be deactivated! We are scrambling a team to assult the missile control bunker. Clear the way and capture the enemy command center. Hurry!", "LANDCOM 16")
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(9), function()
		CaptureFCom = Greece.AddObjective("Capture the enemy Command Center. Hurry!")
		DateTime.TimeLimit = TimerTicks
		Media.PlaySpeechNotification(Greece, "TimerStarted")
		Greece.MarkCompletedObjective(ApproachBase)
	end)

	Trigger.OnCapture(CommandCenter, function()
		Greece.MarkCompletedObjective(CaptureFCom)
	end)

	Paradrop()
	Trigger.AfterDelay(DateTime.Minutes(2), SendParabombs)
end

SilosDamaged = function()
	if not MissilesLaunched then
		LaunchMissiles()
	end
end

Tick = function()
	USSR.Cash = 50000
	BadGuy.Cash = 50000

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(HoldOut)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	InitObjectives(Greece)

	HoldOut = USSR.AddObjective("Hold out until missiles reach their destination")
	ApproachBase = Greece.AddObjective("Find a way to take the atomic weapons off-line.")

	Camera.Position = DefaultCameraPosition.CenterPosition
	StandardDrop = Actor.Create("paradrop", false, { Owner = USSR })
	MissionStart()
	MissionTriggers()
	ActivateAI()
	OnAnyDamaged(MissileSilos, SilosDamaged)
end
