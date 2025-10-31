--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
FlameWallRevealed = false
TimerTicks = DateTime.Minutes(12)
LSTType = "lst.reinforcement"
RifleSquad1 = { Rifle1, Rifle2, Rifle3 }
RifleSquad2 = { Rifle4, Rifle5, Rifle6 }
Heavys = { Heavy1, Heavy2 }
FlameTowerWall = { FlameTower1, FlameTower2 }
DemoEngiPath = { WaterEntry1.Location, Beach1.Location }
DemoEngiTeam = { "dtrk", "dtrk", "e6", "e6", "e6" }
SovietWaterEntry1 = { WaterEntry2.Location, Beach2.Location }
SovietWaterEntry2 = { WaterEntry2.Location, Beach3.Location }
SovietSquad = { "e1", "e1", "e2", "e4", "e4" }
V2Squad = { "v2rl", "v2rl" }
SubEscapePath = { SubPath1, SubPath2, SubPath3 }

MissionStart = function()
	if Difficulty == "normal" then
		local northCoil = Actor.Create("tsla", true, { Owner = USSR, Location = EasyCamera.Location })
		Actor.Create("4tnk", true, { Owner = USSR, Facing = Angle.South, Location = Mammoth.Location })
		Actor.Create("4tnk", true, { Owner = USSR, Facing = Angle.South, Location = Mammoth.Location + CVec.New(1,0) })
		Actor.Create("v2rl", true, { Owner = USSR, Facing = Angle.South, Location = V2.Location })
		-- Avoid leaving infantry stranded on the island.
		northCoil.GrantCondition("no-actors-on-sell")
	end

	SpawnTemporaryCamera(SouthLZ.Location, DateTime.Seconds(15))
	Chalk1.TargetParatroopers(SouthLZ.CenterPosition, Angle.New(740))

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Chalk2.TargetParatroopers(SouthLZ.CenterPosition, Angle.New(780))
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		UnitsArrived = true
		SpawnTemporaryCamera(TeslaCam.Location, DateTime.Seconds(10))
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Utils.Do(RifleSquad1, function(actor)
			if not actor.IsDead then
				actor.AttackMove(SouthLZ.Location)
				IdleHunt(actor)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Utils.Do(RifleSquad2, function(actor)
			if not actor.IsDead then
				actor.AttackMove(SouthLZ.Location)
				IdleHunt(actor)
			end
		end)
	end)
end

SetupTriggers = function()
	SetupFlameWall()
	SetupNorthBase()

	Trigger.OnKilled(Church, function()
		Actor.Create("healcrate", true, { Owner = Greece, Location = ChurchCrate.Location })
	end)

	Trigger.OnKilled(ObjectiveDome, function()
		if not DomeCaptured then
			Greece.MarkFailedObjective(CaptureDome)
		end
	end)

	-- Avoid notifications unless part of the the northern base is captured.
	Greece.PlayLowPowerNotification = false
	local notifiers = Utils.Where(USSR.GetActors(), function(actor)
		return actor.HasProperty("StartBuildingRepairs")
	end)

	Utils.Do(notifiers, function(n)
		Trigger.OnCapture(n, function()
			Greece.PlayLowPowerNotification = true
		end)
	end)
end

SetupFlameWall = function()
	Trigger.OnAllKilled(FlameTowerWall, function()
		SpawnTemporaryCamera(RadarCam.Location, DateTime.Seconds(5))
	end)

	-- Give some warning if they are not yet revealed by the power cut.
	Trigger.OnEnteredProximityTrigger(FlameCam.CenterPosition, WDist.FromCells(8), function(actor, id)
		if FlameWallRevealed then
			Trigger.RemoveProximityTrigger(id)
			return
		end

		if actor.Owner == Greece then
			FlameWallRevealed = true
			Trigger.RemoveProximityTrigger(id)
			SpawnTemporaryCamera(FlameCam.Location, DateTime.Seconds(10))
		end
	end)
end

SetupNorthBase = function()
	local bombsPrepared = false
	local tanksAlerted = false

	Trigger.OnKilledOrCaptured(SubPen, function()
		Greece.MarkCompletedObjective(StopProduction)
	end)

	Trigger.OnDamaged(SubPen, function(_, attacker)
		if tanksAlerted or attacker.Type == "badr.bomber" then
			return
		end

		tanksAlerted = true
		Utils.Do(Heavys, IdleHunt)
	end)

	Trigger.OnKilledOrCaptured(ForwardCommand, function()
		StartFireSale()
		-- The original mission used four waypoints (7-10) for drops.
		-- For simplicity, a four-Badger team will target the middle.
		Chalk3.TargetParatroopers(NorthLZ.CenterPosition, Angle.New(927))

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		end)

		if not bombsPrepared then
			bombsPrepared = true
			BombNorthBase()
		end
	end)

	Trigger.OnAllKilledOrCaptured(USSR.GetActorsByType("sam"), function()
		if not bombsPrepared then
			bombsPrepared = true
			BombNorthBase()
		end
	end)
end

PowerDown = false
PowerDownTeslas = function()
	if not PowerDown then
		CaptureDome = AddSecondaryObjective(Greece, "capture-enemy-radar-dome")
		Greece.MarkCompletedObjective(PowerDownTeslaCoils)
		Media.PlaySoundNotification(Greece, "RadarDown")
		PowerDown = true

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		end)

		local bridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)[1]
		if not bridge.IsDead then
			bridge.Kill()
		end

		local demoEngis = Reinforcements.ReinforceWithTransport(Greece, LSTType, DemoEngiTeam, DemoEngiPath, { DemoEngiPath[1] })[2]
		Trigger.OnAllRemovedFromWorld(Utils.Where(demoEngis, function(a) return a.Type == "e6" end), function()
			if not DomeCaptured then
				Greece.MarkFailedObjective(CaptureDome)
			end
			if not DomeCaptured and bridge.IsDead then
				Greece.MarkFailedObjective(StopProduction)
			end
		end)

		Trigger.OnCapture(ObjectiveDome, function()
			DomeCaptured = true
			Greece.MarkCompletedObjective(CaptureDome)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				SendChronos()
				SendWaterSquads()
				Actor.Create("camera", true, { Owner = Greece, Location = EasyCamera.Location })
			end)
		end)

		if not FlameWallRevealed then
			FlameWallRevealed = true
			SpawnTemporaryCamera(FlameCam.Location, DateTime.Seconds(10))
		end
	end
end

BombNorthBase = function()
	local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = Greece })
	proxy.TargetAirstrike(BomberTarget1.CenterPosition, Angle.New(970))
	proxy.TargetAirstrike(BomberTarget2.CenterPosition, Angle.New(932))
	proxy.Destroy()
end

SendChronos = function()
	local payload = { }
	local proxy = Actor.Create("powerproxy.chronoshift", false, { Owner = Greece })
	local spawns =
	{
		{ cell = ChronoSpawn2.Location, facing = Angle.NorthWest },
		{ cell = ChronoSpawn1.Location, facing = Angle.NorthEast },
		{ cell = ChronoSpawn3.Location, facing = Angle.South }
	}

	Utils.Do(spawns, function(spawn)
		local tank = Actor.Create("ctnk", true, { Owner = Greece, Facing = spawn.facing })
		payload[tank] = spawn.cell
	end)

	Media.PlaySound("chrono2.aud")
	proxy.Chronoshift(payload)
	proxy.Destroy()
end

SendWaterSquads = function()
	local sovietWaterSquad1 = Reinforcements.ReinforceWithTransport(USSR, LSTType, SovietSquad, { WaterEntry2.Location, Beach2.Location }, { WaterEntry2.Location })[2]
	Utils.Do(sovietWaterSquad1, function(a)
		Trigger.OnAddedToWorld(a, IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local sovietWaterSquad2 = Reinforcements.ReinforceWithTransport(USSR, LSTType, SovietSquad, { WaterEntry2.Location, Beach3.Location }, { WaterEntry2.Location })[2]
		Utils.Do(sovietWaterSquad2, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(FlameCam.Location)
				IdleHunt(a)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(13), function()
		local sovietWaterSquad2 = Reinforcements.ReinforceWithTransport(USSR, LSTType, V2Squad, { WaterEntry2.Location, Beach2.Location }, { WaterEntry2.Location })[2]
		Utils.Do(sovietWaterSquad2, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(FlameCam.Location)
				IdleHunt(a)
			end)
		end)
	end)
end

MissileSubEscape = function()
	local missileSub = Actor.Create("msub", true, { Owner = USSR, Location = MissileSubSpawn.Location })
	Actor.Create("camera", true, { Owner = Greece, Location = SubPath2.Location })
	DestroySub = AddPrimaryObjective(Greece, "destroy-escaping-submarine")

	Utils.Do(SubEscapePath, function(waypoint)
		missileSub.Move(waypoint.Location)
	end)

	Trigger.OnEnteredFootprint({ SubPath3.Location }, function(a, id)
		if a.Owner == USSR and a.Type == "msub" then
			Trigger.RemoveFootprintTrigger(id)
			USSR.MarkCompletedObjective(EscapeWithSub)
		end
	end)

	Trigger.OnKilled(missileSub, function()
		Greece.MarkCompletedObjective(DestroySub)
	end)
end

StartFireSale = function()
	local structures = Utils.Where(USSR.GetActors(), function(actor)
		return actor.HasProperty("StartBuildingRepairs")
	end)

	if #structures == 0 then
		return
	end

	SpawnTemporaryCamera(BomberTarget1.Location, DateTime.Seconds(5))
	SpawnTemporaryCamera(BomberTarget2.Location, DateTime.Seconds(5))

	Utils.Do(structures, function(building)
		building.Sell()
	end)

	Trigger.OnAllRemovedFromWorld(structures, function()
		Utils.Do(USSR.GetGroundAttackers(), IdleHunt)
	end)
end

SpawnTemporaryCamera = function(location, duration)
	local camera = Actor.Create("camera", true, { Owner = Greece, Location = location })

	Trigger.AfterDelay(duration, function()
		if camera.IsInWorld then
			camera.Destroy()
		end
	end)
end

FinishTimer = function()
	local submarineEscapes = UserInterface.GetFluentMessage("submarine-escapes")

	for i = 0, 5 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(submarineEscapes, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

UnitsArrived = false
TimerFinished = false
Ticked = TimerTicks
Tick = function()
	if BadGuy.PowerState ~= "Normal" then
		PowerDownTeslas()
	end

	if Greece.HasNoRequiredUnits() and UnitsArrived then
		USSR.MarkCompletedObjective(EscapeWithSub)
	end

	if Ticked > 0 then
		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.GetFluentMessage("submarine-construction-complete-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 and not TimerFinished then
		FinishTimer()
		MissileSubEscape()
		TimerFinished = true
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	EscapeWithSub = AddPrimaryObjective(USSR, "")
	StopProduction = AddPrimaryObjective(Greece, "destroy-soviet-sub-pen")
	PowerDownTeslaCoils = AddPrimaryObjective(Greece, "power-down-tesla-coils")

	InitObjectives(Greece)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		Media.PlaySpeechNotification(Greece, "TenMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(7), function()
		Media.PlaySpeechNotification(Greece, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(9), function()
		Media.PlaySpeechNotification(Greece, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(11), function()
		Media.PlaySpeechNotification(Greece, "WarningOneMinuteRemaining")
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition
	TimerColor = USSR.Color
	Chalk1 = Actor.Create("chalk1", false, { Owner = Greece })
	Chalk2 = Actor.Create("chalk2", false, { Owner = Greece })
	Chalk3 = Actor.Create("chalk3", false, { Owner = Greece })
	MissionStart()
	SetupTriggers()
end
