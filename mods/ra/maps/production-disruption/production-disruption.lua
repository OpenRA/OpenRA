--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
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
SovietSquad = { "e1", "e1", "e1", "e4", "e4" }
V2Squad = { "v2rl", "v2rl" }
SubEscapePath = { SubPath1, SubPath2, SubPath3 }

MissionStart = function()
	LZCamera = Actor.Create("camera", true, { Owner = Greece, Location = LZ.Location })
	Chalk1.TargetParatroopers(LZ.CenterPosition, Angle.New(740))
	if Difficulty == "normal" then
		Actor.Create("tsla", true, { Owner = USSR, Location = EasyCamera.Location })
		Actor.Create("4tnk", true, { Owner = USSR, Facing = Angle.South, Location = Mammoth.Location })
		Actor.Create("4tnk", true, { Owner = USSR, Facing = Angle.South, Location = Mammoth.Location + CVec.New(1,0) })
		Actor.Create("v2rl", true, { Owner = USSR, Facing = Angle.South, Location = V2.Location })
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Chalk2.TargetParatroopers(LZ.CenterPosition, Angle.New(780))
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		UnitsArrived = true
		TeslaCamera = Actor.Create("camera", true, { Owner = Greece, Location = TeslaCam.Location })
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		LZCamera.Destroy()
		Utils.Do(RifleSquad1, function(actor)
			if not actor.IsDead then
				actor.AttackMove(LZ.Location)
				IdleHunt(actor)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		TeslaCamera.Destroy()
		Utils.Do(RifleSquad2, function(actor)
			if not actor.IsDead then
				actor.AttackMove(LZ.Location)
				IdleHunt(actor)
			end
		end)
	end)
end

SetupTriggers = function()
	Trigger.OnDamaged(SubPen, function()
		Utils.Do(Heavys, function(actor)
			if not actor.IsDead then
				IdleHunt(actor)
			end
		end)
	end)

	Trigger.OnKilled(Church, function()
		Actor.Create("healcrate", true, { Owner = Greece, Location = ChurchCrate.Location })
	end)

	Trigger.OnKilled(ObjectiveDome, function()
		if not DomeCaptured == true then
			Greece.MarkFailedObjective(CaptureDome)
		end
	end)

	Trigger.OnAllKilled(FlameTowerWall, function()
		DomeCam = Actor.Create("camera", true, { Owner = Greece, Location = RadarCam.Location })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			DomeCam.Destroy()
		end)
	end)

	Trigger.OnKilledOrCaptured(SubPen, function()
		Greece.MarkCompletedObjective(StopProduction)
	end)
end

PowerDown = false
PowerDownTeslas = function()
	if not PowerDown then
		CaptureDome = AddSecondaryObjective(Greece, "capture-enemy-radar-dome")
		Greece.MarkCompletedObjective(PowerDownTeslaCoils)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		PowerDown = true

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
			SendChronos()
		end)

		FlameTowersCam = Actor.Create("camera", true, { Owner = Greece, Location = FlameCam.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			FlameTowersCam.Destroy()
		end)
	end
end

SendChronos = function()
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		local sovietWaterSquad1 = Reinforcements.ReinforceWithTransport(USSR, "lst", SovietSquad, { WaterEntry2.Location, Beach2.Location }, { WaterEntry2.Location })[2]
		Utils.Do(sovietWaterSquad1, function(a)
			Trigger.OnAddedToWorld(a, function()
				IdleHunt(a)
			end)
		end)

		Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoSpawn1.Location })
		Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoSpawn2.Location })
		Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoSpawn3.Location })
		Actor.Create("camera", true, { Owner = Greece, Location = EasyCamera.Location })
		Media.PlaySound("chrono2.aud")
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local sovietWaterSquad2 = Reinforcements.ReinforceWithTransport(USSR, "lst", SovietSquad, { WaterEntry2.Location, Beach3.Location }, { WaterEntry2.Location })[2]
		Utils.Do(sovietWaterSquad2, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(FlameCam.Location)
				IdleHunt(a)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(13), function()
		local sovietWaterSquad2 = Reinforcements.ReinforceWithTransport(USSR, "lst", V2Squad, { WaterEntry2.Location, Beach2.Location }, { WaterEntry2.Location })[2]
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

SubmarineEscapes = UserInterface.Translate("submarine-escapes")
FinishTimer = function()
	for i = 0, 5 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(SubmarineEscapes, c) end)
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
			Timer = UserInterface.Translate("submarine-completes-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 and not TimerFinished then
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
	MissionStart()
	SetupTriggers()
end
