Difficulty = Map.LobbyOption("difficulty")

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
	{ AirReinforcementsEntry1.Location, AirReinforcementsEntry2.Location },
	{ AirReinforcementsRally1.Location, AirReinforcementsRally2.Location }
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
	{ 192 + 4, ParaDrop1},
	{ 192 - 4, ParaDrop2},
	{ 192 + 4, Alliesbase2},
	{ 192 - 4, Alliesbase1}
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

IdleTrigger = function(units, dest)
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

ticked = TimerTicks
Tick = function()
	if KillObj and soviets.HasNoRequiredUnits() then
		allies.MarkCompletedObjective(KillObj)
	end

	if allies.HasNoRequiredUnits() then
		soviets.MarkCompletedObjective(SovietObj)
	end

	if soviets.Resources > soviets.ResourceCapacity / 2 then
		soviets.Resources = soviets.ResourceCapacity / 2
	end

	if ticked > 0 then
		if DateTime.Minutes(20) == ticked then
			Media.PlaySpeechNotification(allies, "TwentyMinutesRemaining")

		elseif DateTime.Minutes(10) == ticked then
			Media.PlaySpeechNotification(allies, "TenMinutesRemaining")

		elseif DateTime.Minutes(5) == ticked then
			Media.PlaySpeechNotification(allies, "WarningFiveMinutesRemaining")

		elseif DateTime.Minutes(4) == ticked then
			Media.PlaySpeechNotification(allies, "WarningFourMinutesRemaining")

			Trigger.AfterDelay(ParadropTicks, function()
				SendSovietParadrops(ParadropWaypoints[3])
				SendSovietParadrops(ParadropWaypoints[2])
			end)
			Trigger.AfterDelay(ParadropTicks * 2, function()
				SendSovietParadrops(ParadropWaypoints[4])
				SendSovietParadrops(ParadropWaypoints[1])
			end)

		elseif DateTime.Minutes(3) == ticked then
			Media.PlaySpeechNotification(allies, "WarningThreeMinutesRemaining")

		elseif DateTime.Minutes(2) == ticked then
			Media.PlaySpeechNotification(allies, "WarningTwoMinutesRemaining")

			AttackAtFrameIncrement = DateTime.Seconds(4)
			AttackAtFrameIncrementInf = DateTime.Seconds(4)

		elseif DateTime.Minutes(1) == ticked then
			Media.PlaySpeechNotification(allies, "WarningOneMinuteRemaining")

		elseif DateTime.Seconds(45) == ticked then
			Media.PlaySpeechNotification(allies, "AlliedForcesApproaching")
		end

		UserInterface.SetMissionText("French reinforcements arrive in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 then
		FinishTimer()
		TimerExpired()
		ticked = ticked - 1
	end
end

FinishTimer = function()
	for i = 0, 9, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("Our french allies have arrived!", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() UserInterface.SetMissionText("") end)
end

SendSovietParadrops = function(table)
	local units = powerproxy.SendParatroopers(table[2].CenterPosition, false, table[1])

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(a)
			if a.IsInWorld then
				a.Hunt()
			end
		end)
	end)
end

SendSovietNavalReinforcements = function()
	if SpawnNavalUnits then
		local entry = NavalEntryPoint.Location
		local units = Reinforcements.ReinforceWithTransport(soviets, "lst", NavalTransportPassengers, { entry, Utils.Random(NavalReinforcementsWaypoints).Location }, { entry })[2]
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

	soviets.Build(units, function(soldiers)
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
	local attackers = Reinforcements.Reinforce(soviets, units, { SovietEntryPoints[route].Location, SovietRallyPoints[route].Location })
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

	Beacon.New(allies, SovietEntryPoint7.CenterPosition - WVec.New(3 * 1024, 0, 0))
	Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(allies, FrenchReinforcements, { SovietEntryPoint7.Location, Alliesbase.Location })

	if DestroyObj then
		KillObj = allies.AddPrimaryObjective("Take control of French reinforcements and\nkill all remaining Soviet forces.")
	else
		DestroyObj = allies.AddPrimaryObjective("Take control of French reinforcements and\ndismantle the nearby Soviet base.")
	end

	allies.MarkCompletedObjective(SurviveObj)
	if not allies.IsObjectiveCompleted(KillSams) then
		allies.MarkFailedObjective(KillSams)
	end
end

DropAlliedArtillery = function(facing, dropzone)
	local proxy = Actor.Create("powerproxy.allied", true, { Owner = allies })
	proxy.SendParatroopers(dropzone, false, facing)
	proxy.Destroy()
end

SendLongBowReinforcements = function()
	Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[1])
	Reinforcements.Reinforce(allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[2])

	if ParadropArtillery then
		local facing = Utils.RandomInteger(Facing.NorthWest, Facing.SouthWest)
		DropAlliedArtillery(facing, Alliesbase.CenterPosition)
	end
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	SurviveObj = allies.AddPrimaryObjective("Enforce your position and hold-out the onslaught\nuntil reinforcements arrive.")
	KillSams = allies.AddSecondaryObjective("Destroy the two SAM sites before reinforcements\narrive.")
	Media.DisplayMessage("The Soviets are blocking our GPS. We need to investigate their new technology.")
	CaptureAirfields = allies.AddSecondaryObjective("Capture and hold the Soviet airbase\nin the northeast.")
	SovietObj = soviets.AddPrimaryObjective("Eliminate all Allied forces.")

	Trigger.OnObjectiveCompleted(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(allies, function()
		Media.PlaySpeechNotification(allies, "MissionFailed")
	end)
	Trigger.OnPlayerWon(allies, function()
		Media.PlaySpeechNotification(allies, "MissionAccomplished")
		Media.DisplayMessage("The French forces have survived and dismantled the Soviet presence in the area!")
	end)
end

InitMission = function()
	Camera.Position = Alliesbase.CenterPosition
	camera1 = Actor.Create("camera.sam", true, { Owner = allies, Location = Sam1.Location })
	camera2 = Actor.Create("camera.sam", true, { Owner = allies, Location = Sam2.Location })
	Trigger.OnKilled(Sam1, function()
		if camera1.IsInWorld then camera1.Destroy() end
	end)
	Trigger.OnKilled(Sam2, function()
		if camera2.IsInWorld then camera2.Destroy() end
	end)
	Trigger.OnAllKilledOrCaptured({ Sam1, Sam2 }, function()
		if not allies.IsObjectiveFailed(KillSams) then
			allies.MarkCompletedObjective(KillSams)
			SendLongBowReinforcements()
		end
	end)

	local count = 0
	Utils.Do(Airfields, function(field)
		Trigger.OnCapture(field, function()
			count = count + 1
			if count == #Airfields then
				allies.MarkCompletedObjective(CaptureAirfields)
				local atek = Actor.Create("atek.mission", true, { Owner = allies, Location = HiddenATEK.Location })
				Trigger.AfterDelay(DateTime.Seconds(5), atek.Destroy)
			end
		end)
		Trigger.OnKilled(field, function()
			allies.MarkFailedObjective(CaptureAirfields)
		end)
	end)

	Trigger.OnAllKilledOrCaptured(SovietBuildings, function()
		if DestroyObj then
			if not soviets.HasNoRequiredUnits() then
				KillObj = allies.AddPrimaryObjective("Kill all remaining Soviet forces.")
			end
			allies.MarkCompletedObjective(DestroyObj)
		else
			DestroyObj = allies.AddPrimaryObjective("Dismantle the nearby Soviet base.")
			allies.MarkCompletedObjective(DestroyObj)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(allies, "MissionTimerInitialised") end)
	TimerColor = allies.Color
end

SetupSoviets = function()
	Barrack1.IsPrimaryBuilding = true
	Barrack1.RallyPoint = SovietInfantryRally1.Location
	Trigger.OnKilledOrCaptured(Barrack1, function()
		SpawningInfantry = false
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == soviets and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == soviets and building.Health < building.MaxHealth * DamageModifier then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)

	Reinforcements.Reinforce(soviets, Squad1, { AlliesBaseGate1.Location, Alliesbase1.Location })
	Reinforcements.Reinforce(soviets, Squad2, { AlliesBaseGate2.Location, Alliesbase2.Location })

	powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = soviets })
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

	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")

	InitObjectives()
	InitMission()
	SetupSoviets()
end
