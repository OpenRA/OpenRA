--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "easy" then
	AttackAtFrameIncrement = DateTime.Seconds(22)
	AttackAtFrameIncrementInf = DateTime.Seconds(16)
	TimerTicks = DateTime.Minutes(15)
	IncrementTurningPoint = TimerTicks / 2
	DamageModifier = 0.5
	LongBowReinforcements = { "heli", "heli" }
	ParadropArtillery = true
elseif Difficulty == "normal" then
	AttackAtFrameIncrement = DateTime.Seconds(18)
	AttackAtFrameIncrementInf = DateTime.Seconds(12)
	TimerTicks = DateTime.Minutes(20)
	IncrementTurningPoint = TimerTicks / 2
	MoreParas = true
	DamageModifier = 0.75
	LongBowReinforcements = { "heli", "heli" }
else --Difficulty == "hard"
	AttackAtFrameIncrement = DateTime.Seconds(14)
	AttackAtFrameIncrementInf = DateTime.Seconds(8)
	TimerTicks = DateTime.Minutes(25)
	IncrementTurningPoint = DateTime.Minutes(10)
	MoreParas = true
	AttackAtFrameNaval = DateTime.Minutes(3) + DateTime.Seconds(45)
	SpawnNavalUnits = true
	DamageModifier = 1
	LongBowReinforcements = { "heli" }
end

AlliedAirReinforcementsWaypoints =
{
	{ AirReinforcementsEntry1.Location, AirReinforcementsRally1.Location },
	{ AirReinforcementsEntry2.Location, AirReinforcementsRally2.Location }
}
FrenchReinforcements = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk", "1tnk", "arty", "arty", "arty", "jeep", "jeep" }

SpawningSovietUnits = true
SpawningInfantry = true
AttackAtFrameInf = DateTime.Seconds(12)
AttackAtFrame = DateTime.Seconds(18)
SovietAttackGroupSize = 5
SovietInfantryGroupSize = 7
FactoryClearRange = 10
ParadropTicks = DateTime.Seconds(30)
ParadropWaypoints =
{
	{ Angle.East + Angle.New(16), ParaDrop1},
	{ Angle.East - Angle.New(16), ParaDrop2},
	{ Angle.East + Angle.New(16), Alliesbase2},
	{ Angle.East - Angle.New(16), Alliesbase1}
}
NavalTransportPassengers = { "e1", "e1", "e2", "e4", "e4" }
NavalReinforcementsWaypoints = { NavalWaypoint1, NavalWaypoint2, NavalWaypoint2, NavalWaypoint3 }
Squad1 = { "e1", "e1" }
Squad2 = { "e2", "e2" }
SovietVehicles = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "ftrk", "ftrk", "apc", "apc" }
SovietInfantry = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e4", "e4", "e3" }
SovietEntryPoints = { SovietEntryPoint1, SovietEntryPoint2, SovietEntryPoint3, SovietEntryPoint4, SovietEntryPoint5 }
SovietRallyPoints = { SovietRallyPoint1, SovietRallyPoint2, SovietRallyPoint3, SovietRallyPoint4, SovietRallyPoint5 }
SovietGateRallyPoints = { AlliesBaseGate2, AlliesBaseGate2, AlliesBaseGate1, AlliesBaseGate1, AlliesBaseGate1 }

Airfields = { SovietAirfield1, SovietAirfield2, SovietAirfield3 }
SovietBuildings = { Barrack1, SubPen, RadarDome, AdvancedPowerPlant1, AdvancedPowerPlant2, AdvancedPowerPlant3, WarFactory, Refinery, Silo1, Silo2, FlameTower1, FlameTower2, FlameTower3, Sam1, Sam2, Sam3, Sam4, SovietAirfield1, SovietAirfield2, SovietAirfield3 }

IdleTrigger = function(units, _)
	Utils.Do(units, function(unit)

		if not unit.IsDead then
			Trigger.OnIdle(unit, function()
				local bool = Utils.All(units, function(unit) return unit.IsIdle end)
				if bool then
					SetupHuntTrigger(units)
				end
			end)

			Trigger.OnDamaged(unit, function()
				SetupHuntTrigger(units)
			end)

			Trigger.OnCapture(unit, function()
				Trigger.ClearAll(unit)
			end)
		end
	end)
end

SetupHuntTrigger = function(units)
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function()
				if not unit.IsDead then
					Trigger.OnIdle(unit, unit.Hunt)
					Trigger.OnCapture(unit, function()
						Trigger.ClearAll(unit)
					end)
				end
			end)
		end
	end)
end

Ticked = TimerTicks
Tick = function()
	if KillObj and Soviets.HasNoRequiredUnits() then
		Allies.MarkCompletedObjective(KillObj)
	end

	if Allies.HasNoRequiredUnits() then
		Soviets.MarkCompletedObjective(SovietObj)
	end

	if Soviets.Resources > Soviets.ResourceCapacity / 2 then
		Soviets.Resources = Soviets.ResourceCapacity / 2
	end

	if Ticked > 0 then
		if DateTime.Minutes(20) == Ticked then
			Media.PlaySpeechNotification(Allies, "TwentyMinutesRemaining")

		elseif DateTime.Minutes(10) == Ticked then
			Media.PlaySpeechNotification(Allies, "TenMinutesRemaining")

		elseif DateTime.Minutes(5) == Ticked then
			Media.PlaySpeechNotification(Allies, "WarningFiveMinutesRemaining")

		elseif DateTime.Minutes(4) == Ticked then
			Media.PlaySpeechNotification(Allies, "WarningFourMinutesRemaining")

			Trigger.AfterDelay(ParadropTicks, function()
				SendSovietParadrops(ParadropWaypoints[3])
				SendSovietParadrops(ParadropWaypoints[2])
			end)
			Trigger.AfterDelay(ParadropTicks * 2, function()
				SendSovietParadrops(ParadropWaypoints[4])
				SendSovietParadrops(ParadropWaypoints[1])
			end)

		elseif DateTime.Minutes(3) == Ticked then
			Media.PlaySpeechNotification(Allies, "WarningThreeMinutesRemaining")

		elseif DateTime.Minutes(2) == Ticked then
			Media.PlaySpeechNotification(Allies, "WarningTwoMinutesRemaining")

			AttackAtFrameIncrement = DateTime.Seconds(4)
			AttackAtFrameIncrementInf = DateTime.Seconds(4)

		elseif DateTime.Minutes(1) == Ticked then
			Media.PlaySpeechNotification(Allies, "WarningOneMinuteRemaining")

		elseif DateTime.Seconds(45) == Ticked then
			Media.PlaySpeechNotification(Allies, "AlliedForcesApproaching")
		end

		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("french-reinforcements-arrive-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 then
		FinishTimer()
		TimerExpired()
		Ticked = Ticked - 1
	end
end

FrenchAlliesArrived = UserInterface.Translate("french-allies-arrived")
FinishTimer = function()
	for i = 0, 9, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(FrenchAlliesArrived, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() UserInterface.SetMissionText("") end)
end

SendSovietParadrops = function(table)
	local aircraft = ParaTroopersPowerProxy.TargetParatroopers(table[2].CenterPosition, table[1])
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
end

SendSovietNavalReinforcements = function()
	if SpawnNavalUnits then
		local entry = NavalEntryPoint.Location
		local units = Reinforcements.ReinforceWithTransport(Soviets, "lst", NavalTransportPassengers, { entry, Utils.Random(NavalReinforcementsWaypoints).Location }, { entry })[2]
		Utils.Do(units, function(unit)
			Trigger.OnIdle(unit, unit.Hunt)
		end)

		local delay = Utils.RandomInteger(AttackAtFrameNaval, AttackAtFrameNaval + DateTime.Minutes(2))

		Trigger.AfterDelay(delay, SendSovietNavalReinforcements)
	end
end

SpawnSovietInfantry = function()
	local units = { }
	for i = 0, SovietInfantryGroupSize - 1, 1 do
		local type = Utils.Random(SovietInfantry)
		units[i] = type
	end

	Soviets.Build(units, function(soldiers)
		Trigger.AfterDelay(25, function() IdleTrigger(soldiers) end)
	end)
end

SpawnSovietUnits = function()
	local units = { }
	for i = 0, SovietAttackGroupSize - 1, 1 do
		local type = Utils.Random(SovietVehicles)
		units[i] = type
	end

	local route = Utils.RandomInteger(1, #SovietEntryPoints + 1)
	local attackers = Reinforcements.Reinforce(Soviets, units, { SovietEntryPoints[route].Location, SovietRallyPoints[route].Location })
	Trigger.AfterDelay(25, function()
		IdleTrigger(attackers, SovietGateRallyPoints[route].Location)
	end)
end

SendInfantryWave = function()
	if SpawningInfantry then
		SpawnSovietInfantry()

		if DateTime.GameTime < IncrementTurningPoint then
			AttackAtFrameIncrementInf = AttackAtFrameIncrementInf + Utils.RandomInteger(DateTime.Seconds(2), DateTime.Seconds(3))
		elseif not (AttackAtFrameIncrementInf <= DateTime.Seconds(4)) then
			AttackAtFrameIncrementInf = AttackAtFrameIncrementInf - Utils.RandomInteger(DateTime.Seconds(2), DateTime.Seconds(3))
		end

		Trigger.AfterDelay(AttackAtFrameInf + AttackAtFrameIncrementInf, SendInfantryWave)
	end
end

SendVehicleWave = function()
	if SpawningSovietUnits then
		SpawnSovietUnits()

		if DateTime.GameTime < IncrementTurningPoint then
			AttackAtFrameIncrement = AttackAtFrameIncrement + Utils.RandomInteger(DateTime.Seconds(4), DateTime.Seconds(6))
		elseif not (AttackAtFrameIncrement <= DateTime.Seconds(4)) then
			AttackAtFrameIncrement = AttackAtFrameIncrement - Utils.RandomInteger(DateTime.Seconds(4), DateTime.Seconds(6))
		end

		Trigger.AfterDelay(AttackAtFrame + AttackAtFrameIncrement, SendVehicleWave)
	end
end

TimerExpired = function()
	SpawningSovietUnits = false
	SpawningInfantry = false
	SpawnNavalUnits = false

	Beacon.New(Allies, SovietEntryPoint7.CenterPosition - WVec.New(3 * 1024, 0, 0))
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(Allies, FrenchReinforcements, { SovietEntryPoint7.Location, Alliesbase.Location })

	if DestroyObj then
		KillObj = AddPrimaryObjective(Allies, "control-reinforcements-kill-remaining-soviet-forces")
	else
		DestroyObj = AddPrimaryObjective(Allies, "takeover-reinforcements-dismantle-soviet-base")
	end

	Allies.MarkCompletedObjective(SurviveObj)
	if not Allies.IsObjectiveCompleted(KillSams) then
		Allies.MarkFailedObjective(KillSams)
	end
end

DropAlliedArtillery = function(facing, dropzone)
	local proxy = Actor.Create("powerproxy.allied", true, { Owner = Allies })
	proxy.TargetParatroopers(dropzone, facing)
	proxy.Destroy()
end

SendLongBowReinforcements = function()
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(Allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[1])
	Reinforcements.Reinforce(Allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[2])

	if ParadropArtillery then
		local facing = Angle.New(Utils.RandomInteger(128, 384))
		DropAlliedArtillery(facing, Alliesbase.CenterPosition)
	end
end

AddObjectives = function()
	InitObjectives(Allies)

	SurviveObj = AddPrimaryObjective(Allies, "enforce-position-hold-until-reinforcements")
	KillSams = AddSecondaryObjective(Allies, "destroy-two-sames-before-reinforcements")
	Media.DisplayMessage(UserInterface.Translate("soviets-blocking-gps"))
	CaptureAirfields = AddSecondaryObjective(Allies, "capture-hold-soviet-airbase-northeast")
	SovietObj = AddPrimaryObjective(Soviets, "")

	Trigger.OnPlayerWon(Allies, function()
		Media.DisplayMessage(UserInterface.Translate("french-survived-dismantled-soviet-presence"))
	end)
end

InitMission = function()
	Camera.Position = Alliesbase.CenterPosition
	local camera1 = Actor.Create("camera.sam", true, { Owner = Allies, Location = Sam1.Location })
	local camera2 = Actor.Create("camera.sam", true, { Owner = Allies, Location = Sam2.Location })
	Trigger.OnKilled(Sam1, function()
		if camera1.IsInWorld then camera1.Destroy() end
	end)
	Trigger.OnKilled(Sam2, function()
		if camera2.IsInWorld then camera2.Destroy() end
	end)
	Trigger.OnAllKilledOrCaptured({ Sam1, Sam2 }, function()
		if not Allies.IsObjectiveFailed(KillSams) then
			Allies.MarkCompletedObjective(KillSams)
			SendLongBowReinforcements()
		end
	end)

	local count = 0
	Utils.Do(Airfields, function(field)
		Trigger.OnCapture(field, function()
			count = count + 1
			if count == #Airfields then
				Allies.MarkCompletedObjective(CaptureAirfields)
				local atek = Actor.Create("atek.mission", true, { Owner = Allies, Location = HiddenATEK.Location })
				Trigger.AfterDelay(DateTime.Seconds(5), atek.Destroy)
			end
		end)
		Trigger.OnKilled(field, function()
			Allies.MarkFailedObjective(CaptureAirfields)
		end)
	end)

	Trigger.OnAllKilledOrCaptured(SovietBuildings, function()
		if DestroyObj then
			if not Soviets.HasNoRequiredUnits() then
				KillObj = AddPrimaryObjective(Allies, "kill-remaining-soviet-forces")
			end
			Allies.MarkCompletedObjective(DestroyObj)
		else
			DestroyObj = AddPrimaryObjective(Allies, "dismantle-nearby-soviet-base")
			Allies.MarkCompletedObjective(DestroyObj)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Allies, "MissionTimerInitialised") end)
	TimerColor = Allies.Color
end

SetupSoviets = function()
	Barrack1.IsPrimaryBuilding = true
	Barrack1.RallyPoint = SovietInfantryRally1.Location
	Trigger.OnKilledOrCaptured(Barrack1, function()
		SpawningInfantry = false
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Soviets and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Soviets and building.Health < building.MaxHealth * DamageModifier then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)

	Reinforcements.Reinforce(Soviets, Squad1, { AlliesBaseGate1.Location, Alliesbase1.Location })
	Reinforcements.Reinforce(Soviets, Squad2, { AlliesBaseGate2.Location, Alliesbase2.Location })

	ParaTroopersPowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = Soviets })
	Trigger.AfterDelay(ParadropTicks, function()
		SendSovietParadrops(ParadropWaypoints[1])
		SendSovietParadrops(ParadropWaypoints[2])
	end)
	Trigger.AfterDelay(ParadropTicks * 2, function()
		SendSovietParadrops(ParadropWaypoints[3])
		SendSovietParadrops(ParadropWaypoints[4])
	end)

	Trigger.AfterDelay(AttackAtFrame, SendVehicleWave)
	Trigger.AfterDelay(AttackAtFrameInf, SendInfantryWave)

	if MoreParas then
		local delay = Utils.RandomInteger(TimerTicks/3, TimerTicks*2/3)
		Trigger.AfterDelay(delay, function()
			SendSovietParadrops(ParadropWaypoints[Utils.RandomInteger(1,3)])
			SendSovietParadrops(ParadropWaypoints[Utils.RandomInteger(3,5)])
		end)
	end
	if SpawnNavalUnits then
		Trigger.AfterDelay(AttackAtFrameNaval, SendSovietNavalReinforcements)
	end
end

WorldLoaded = function()

	Allies = Player.GetPlayer("Allies")
	Soviets = Player.GetPlayer("Soviets")

	AddObjectives()
	InitMission()
	SetupSoviets()
end
