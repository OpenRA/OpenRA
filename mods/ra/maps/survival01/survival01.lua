Difficulty = Map.Difficulty

if Difficulty == "Easy" then
	AttackAtFrameIncrement = DateTime.Seconds(22)
	AttackAtFrameIncrementInf = DateTime.Seconds(16)
	TimerTicks = DateTime.Minutes(15)
	IncrementTurningPoint = TimerTicks / 2
	DamageModifier = 0.5
	LongBowReinforcements = { "heli", "heli" }
	ParadropArtillery = true
elseif Difficulty == "Medium" then
	AttackAtFrameIncrement = DateTime.Seconds(18)
	AttackAtFrameIncrementInf = DateTime.Seconds(12)
	TimerTicks = DateTime.Minutes(20)
	IncrementTurningPoint = TimerTicks / 2
	MoreParas = true
	DamageModifier = 0.75
	LongBowReinforcements = { "heli", "heli" }
else --Difficulty == "Hard"
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

AlliedArtilleryParadrops = { "arty", "arty", "arty" }
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
BadgerPassengers = { "e1", "e1", "e1", "e2", "e2" }
ParadropWaypoints =
{
	{ BadgerEntryPoint1.Location, ParaDrop1.Location },
	{ BadgerEntryPoint2.Location, ParaDrop2.Location },
	{ BadgerEntryPoint1.Location, Alliesbase2.Location },
	{ BadgerEntryPoint2.Location, Alliesbase1.Location }
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
		Trigger.OnIdle(unit, function()
			local bool = Utils.All(units, function(unit) return unit.IsIdle end)
			if bool then
				Utils.Do(units, function(unit)
					if not unit.IsDead then
						Trigger.ClearAll(unit)
						Trigger.AfterDelay(0, function()
							if not unit.IsDead then
								if dest then unit.AttackMove(dest, 3) end
								Trigger.OnIdle(unit, unit.Hunt)
							end
						end)
					end
				end)
			end
		end)
		Trigger.OnDamaged(unit, function()
			Utils.Do(units, function(unit)
				if not unit.IsDead then
					Trigger.ClearAll(unit)
					Trigger.AfterDelay(0, function()
						if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end
					end)
				end
			end)
		end)
	end)
end

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

	if DateTime.Minutes(20) == TimerTicks - DateTime.GameTime then
		Media.PlaySpeechNotification(allies, "TwentyMinutesRemaining")
	elseif DateTime.Minutes(10) == TimerTicks - DateTime.GameTime then
		Media.PlaySpeechNotification(allies, "TenMinutesRemaining")
	elseif DateTime.Minutes(5) == TimerTicks - DateTime.GameTime then
		Media.PlaySpeechNotification(allies, "WarningFiveMinutesRemaining")
		InitTimer()
	end
end

SendSovietParadrops = function(table)
	local plane = Actor.Create("badr", true, { Owner = soviets, Location = table[1] })
	Utils.Do(BadgerPassengers, function(type)
		local unit = Actor.Create(type, false, { Owner = soviets })
		plane.LoadPassenger(unit)
		Trigger.OnIdle(unit, unit.Hunt)
	end)
	plane.Paradrop(table[2])
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

InitTimer = function()
	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(allies, "WarningFourMinutesRemaining")

		Trigger.AfterDelay(ParadropTicks, function()
			SendSovietParadrops(ParadropWaypoints[3])
			SendSovietParadrops(ParadropWaypoints[2])
		end)
		Trigger.AfterDelay(ParadropTicks * 2, function()
			SendSovietParadrops(ParadropWaypoints[4])
			SendSovietParadrops(ParadropWaypoints[1])
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function() Media.PlaySpeechNotification(allies, "WarningThreeMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(3), function()
		Media.PlaySpeechNotification(allies, "WarningTwoMinutesRemaining")

		AttackAtFrameIncrement = DateTime.Seconds(4)
		AttackAtFrameIncrementInf = DateTime.Seconds(4)
	end)

	Trigger.AfterDelay(DateTime.Minutes(4), function() Media.PlaySpeechNotification(allies, "WarningOneMinuteRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(4) + DateTime.Seconds(45), function() Media.PlaySpeechNotification(allies, "AlliedForcesApproaching") end)
	Trigger.AfterDelay(DateTime.Minutes(5), TimerExpired)
end

TimerExpired = function()
	SpawningSovietUnits = false
	SpawningInfantry = false
	SpawnNavalUnits = false

	Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(allies, FrenchReinforcements, { SovietEntryPoint7.Location, Alliesbase.Location })

	if DestroyObj then
		KillObj = allies.AddPrimaryObjective("Take control of French reinforcements and\nkill all remaining soviet forces.")
	else
		DestroyObj = allies.AddPrimaryObjective("Take control of French reinforcements and\ndismantle the nearby Soviet base.")
	end

	allies.MarkCompletedObjective(SurviveObj)
	if not allies.IsObjectiveCompleted(KillSams) then
		allies.MarkFailedObjective(KillSams)
	end
end

DropAlliedArtillery = function(table)
	local plane = Actor.Create("badr", true, { Owner = allies, Location = table[1] })
	Utils.Do(AlliedArtilleryParadrops, function(type)
		local unit = Actor.Create(type, false, { Owner = allies })
		plane.LoadPassenger(unit)
	end)
	plane.Paradrop(table[2])
end

SendLongBowReinforcements = function()
	Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[1])
	Reinforcements.Reinforce(allies, LongBowReinforcements, AlliedAirReinforcementsWaypoints[2])
	if ParadropArtillery then
		DropAlliedArtillery({ Utils.Random(AlliedAirReinforcementsWaypoints)[1], Alliesbase.Location })
	end
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	SurviveObj = allies.AddPrimaryObjective("Enforce your position and hold-out the onslaught\nuntil reinforcements arrive.")
	KillSams = allies.AddSecondaryObjective("Destroy the two SAM Sites before reinforcements\narrive.")
	Media.DisplayMessage("The soviets are blocking our GPS. We need to investigate their new technology.")
	CaptureAirfields = allies.AddSecondaryObjective("Capture and hold the soviet airbase\nin the north east.")
	SovietObj = soviets.AddPrimaryObjective("Eliminate all Allied forces.")

	Trigger.OnObjectiveCompleted(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(allies, function()
		Media.PlaySpeechNotification(allies, "Lose")
	end)
	Trigger.OnPlayerWon(allies, function()
		Media.PlaySpeechNotification(allies, "Win")
		Media.DisplayMessage("The French forces have survived and dismantled the soviet presence in the area!")
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
				KillObj = allies.AddPrimaryObjective("Kill all remaining soviet forces.")
			end
			allies.MarkCompletedObjective(DestroyObj)
		else
			DestroyObj = allies.AddPrimaryObjective("Dismantle the nearby Soviet base.")
			allies.MarkCompletedObjective(DestroyObj)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(allies, "MissionTimerInitialised") end)
end

SetupSoviets = function()
	Barrack1.IsPrimaryBuilding = true
	Barrack1.RallyPoint = SovietInfantryRally1.Location
	Trigger.OnKilledOrCaptured(Barrack1, function()
		SpawningInfantry = false
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(self) return self.Owner == soviets and self.HasProperty("StartBuildingRepairs") end)
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
