--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
FrenchSquad = { "2tnk", "2tnk", "mcv" }

TimerTicks = DateTime.Minutes(10)
AttackTicks = DateTime.Seconds(52)
AttackAtFrame = DateTime.Seconds(18)
AttackAtFrameIncrement = DateTime.Seconds(18)
Producing = true
SpawningInfantry = true
ProduceAtFrame = DateTime.Seconds(12)
ProduceAtFrameIncrement = DateTime.Seconds(12)
SovietGroupSize = 4
SovietAttackGroupSize = 7

InfantryGuards = { }
HarvGuards = { HarvGuard1, HarvGuard2, HarvGuard3 }
SovietPlatoonUnits = { "e1", "e1", "e2", "e4", "e4", "e1", "e1", "e2", "e4", "e4" }
SovietTanks = { "3tnk", "3tnk", "3tnk" }
SovietVehicles = { "3tnk", "3tnk", "v2rl" }
SovietInfantry = { "e1", "e4", "e2" }
SovietEntryPoints = { SovietEntry1, SovietEntry2, SovietEntry3 }
SovietRallyPoints = { SovietRally2, SovietRally4, SovietRally5, SovietRally6 }
NewSovietEntryPoints = { SovietParaDropEntry, SovietEntry3 }
NewSovietRallyPoints = { SovietRally3, SovietRally4, SovietRally8 }

ParaWaves =
{
	{ delay = AttackTicks, type = "SovietSquad", target = SovietRally5  },
	{ delay = 0, type = "SovietSquad", target = SovietRally6 },
	{ delay = AttackTicks * 2, type = "SovietSquad", target = SovietParaDrop3 },
	{ delay = 0, type = "SovietPlatoonUnits", target = SovietRally5 },
	{ delay = 0, type = "SovietPlatoonUnits", target = SovietRally6 },
	{ delay = 0, type = "SovietSquad", target = SovietRally2 },
	{ delay = AttackTicks * 2, type = "SovietSquad", target = SovietParaDrop2 },
	{ delay = AttackTicks * 2, type = "SovietSquad", target = SovietParaDrop1 },
	{ delay = AttackTicks * 3, type = "SovietSquad", target = SovietParaDrop1 }
}

GuardHarvester = function(unit, harvester)
	if not unit.IsDead then
		unit.Stop()

		local start = unit.Location
		if not harvester.IsDead then
			unit.AttackMove(harvester.Location)
		else
			unit.Hunt()
		end

		Trigger.OnIdle(unit, function()
			if unit.Location == start then
				Trigger.ClearAll(unit)
			else
				unit.AttackMove(start)
			end
		end)

		Trigger.OnCapture(unit, function()
			Trigger.ClearAll(unit)
		end)
	end
end

Ticked = TimerTicks
Tick = function()
	if Soviets.HasNoRequiredUnits() then
		if DestroyObj then
			Allies.MarkCompletedObjective(DestroyObj)
		else
			DestroyObj = AddPrimaryObjective(Allies, "destroy-all-soviet-forces")
			Allies.MarkCompletedObjective(DestroyObj)
		end
	end

	if Allies.HasNoRequiredUnits() then
		Soviets.MarkCompletedObjective(SovietObj)
	end

	if Soviets.Resources > Soviets.ResourceCapacity / 2 then
		Soviets.Resources = Soviets.ResourceCapacity / 2
	end

	if DateTime.GameTime == ProduceAtFrame then
		if SpawningInfantry then
			ProduceAtFrame = ProduceAtFrame + ProduceAtFrameIncrement
			ProduceAtFrameIncrement = ProduceAtFrameIncrement * 2 - 5
			SpawnSovietInfantry()
		end
	end

	if DateTime.GameTime == AttackAtFrame then
		AttackAtFrame = AttackAtFrame + AttackAtFrameIncrement
		AttackAtFrameIncrement = AttackAtFrameIncrement * 2 - 5
		if Producing then
			SpawnSovietVehicle(SovietEntryPoints, SovietRallyPoints)
		else
			SpawnSovietVehicle(NewSovietEntryPoints, NewSovietRallyPoints)
		end
	end

	if DateTime.Minutes(5) == Ticked then
		Media.PlaySpeechNotification(Allies, "WarningFiveMinutesRemaining")
		InitCountDown()
	end

	if Ticked > 0 then
		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("soviet-reinforcements-arrive-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 then
		FinishTimer()
		Ticked = Ticked - 1
	end
end

SendSovietParadrops = function(table)
	local paraproxy = Actor.Create(table.type, false, { Owner = Soviets })
	local aircraft = paraproxy.TargetParatroopers(table.target.CenterPosition)
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
	paraproxy.Destroy()
end

SpawnSovietInfantry = function()
	Soviets.Build({ Utils.Random(SovietInfantry) }, function(units)
		IdleHunt(units[1])
	end)
end

SpawnSovietVehicle = function(spawnpoints, rallypoints)
	local route = Utils.RandomInteger(1, #spawnpoints + 1)
	local rally = Utils.RandomInteger(1, #rallypoints + 1)
	local unit = Reinforcements.Reinforce(Soviets, { Utils.Random(SovietVehicles) }, { spawnpoints[route].Location })[1]
	unit.AttackMove(rallypoints[rally].Location)
	IdleHunt(unit)

	Trigger.OnCapture(unit, function()
		Trigger.ClearAll(unit)
	end)
end

SpawnAndAttack = function(types, entry)
	local units = Reinforcements.Reinforce(Soviets, types, { entry })
	Utils.Do(units, function(unit)
		IdleHunt(unit)

		Trigger.OnCapture(unit, function()
			Trigger.ClearAll(unit)
		end)
	end)
	return units
end

SendFrenchReinforcements = function()
	local camera = Actor.Create("camera", true, { Owner = Allies, Location = SovietRally1.Location })
	Beacon.New(Allies, FranceEntry.CenterPosition - WVec.New(0, 3 * 1024, 0))
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(Allies, FrenchSquad, { FranceEntry.Location, FranceRally.Location })
	Trigger.AfterDelay(DateTime.Seconds(3), function() camera.Destroy() end)
end

FrenchReinforcements = function()
	Camera.Position = SovietRally1.CenterPosition

	if drum1.IsDead or drum2.IsDead or drum3.IsDead then
		SendFrenchReinforcements()
		return
	end

	local powerproxy = Actor.Create("powerproxy.parabombs", false, { Owner = Allies })
	powerproxy.TargetAirstrike(drum1.CenterPosition, Angle.NorthEast + Angle.New(16))
	powerproxy.TargetAirstrike(drum2.CenterPosition, Angle.NorthEast)
	powerproxy.TargetAirstrike(drum3.CenterPosition, Angle.NorthEast - Angle.New(16))
	powerproxy.Destroy()

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		SendFrenchReinforcements()
	end)
end

FinalAttack = function()
	local units1 = SpawnAndAttack(SovietTanks, SovietEntry1.Location)
	local units2 = SpawnAndAttack(SovietTanks, SovietEntry1.Location)
	local units3 = SpawnAndAttack(SovietTanks, SovietEntry2.Location)
	local units4 = SpawnAndAttack(SovietPlatoonUnits, SovietEntry1.Location)
	local units5 = SpawnAndAttack(SovietPlatoonUnits, SovietEntry2.Location)

	local units = { }
	local insert = function(table)
		local count = #units
		Utils.Do(table, function(unit)
			units[count] = unit
			count = count + 1
		end)
	end

	insert(units1)
	insert(units2)
	insert(units3)
	insert(units4)
	insert(units5)

	Trigger.OnAllKilledOrCaptured(units, function()
		if not DestroyObj then
			Media.DisplayMessage(UserInterface.Translate("reinforced-position-initiate-counter-attack"), UserInterface.Translate("incoming-report"))
			DestroyObj = AddPrimaryObjective(Allies, "destroy-remaining-soviet-forces-area")
		end
		Allies.MarkCompletedObjective(SurviveObj)
	end)
end

SovietReinforcementsArrived = UserInterface.Translate("soviet-reinforcements-arrived")
FinishTimer = function()
	for i = 0, 9, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(SovietReinforcementsArrived, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() UserInterface.SetMissionText("") end)
end

Wave = 1
SendParadrops = function()
	SendSovietParadrops(ParaWaves[Wave])

	Wave = Wave + 1
	if Wave > #ParaWaves then
		Trigger.AfterDelay(AttackTicks, FrenchReinforcements)
	else
		Trigger.AfterDelay(ParaWaves[Wave].delay, SendParadrops)
	end
end

SetupBridges = function()
	local count = 0
	local counter = function()
		count = count + 1
		if count == 2 then
			Allies.MarkCompletedObjective(RepairBridges)
		end
	end

	Media.DisplayMessage(UserInterface.Translate("repair-bridges-for-reinforcement"), UserInterface.Translate("incoming-report"))
	RepairBridges = AddSecondaryObjective(Allies, "repair-two-southern-bridges")

	local bridgeA = Map.ActorsInCircle(BrokenBridge1.CenterPosition, WDist.FromCells(1), function(self) return self.Type == "bridge1" end)
	local bridgeB = Map.ActorsInCircle(BrokenBridge2.CenterPosition, WDist.FromCells(1), function(self) return self.Type == "bridge1" end)

	Utils.Do(bridgeA, function(bridge)
		Trigger.OnDamaged(bridge, function()
			Utils.Do(bridgeA, function(self) Trigger.ClearAll(self) end)
			Media.PlaySpeechNotification(Allies, "AlliedReinforcementsArrived")
			Reinforcements.Reinforce(Allies, { "1tnk", "2tnk", "2tnk" }, { ReinforcementsEntry1.Location, ReinforcementsRally1.Location })
			counter()
		end)
	end)
	Utils.Do(bridgeB, function(bridge)
		Trigger.OnDamaged(bridge, function()
			Utils.Do(bridgeB, function(self) Trigger.ClearAll(self) end)
			Media.PlaySpeechNotification(Allies, "AlliedReinforcementsArrived")
			Reinforcements.Reinforce(Allies, { "jeep", "1tnk", "1tnk" }, { ReinforcementsEntry2.Location, ReinforcementsRally2.Location })
			counter()
		end)
	end)
end

InitCountDown = function()
	Trigger.AfterDelay(DateTime.Minutes(1), function() Media.PlaySpeechNotification(Allies, "WarningFourMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() Media.PlaySpeechNotification(Allies, "WarningThreeMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() Media.PlaySpeechNotification(Allies, "WarningTwoMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(4), function() Media.PlaySpeechNotification(Allies, "WarningOneMinuteRemaining") end)
end

AddObjectives = function()
	InitObjectives(Allies)

	SurviveObj = AddPrimaryObjective(Allies, "enforce-position-hold-out-onslaught")
	SovietObj = AddPrimaryObjective(Soviets, "")

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		SetupBridges()
	end)

	Trigger.OnPlayerWon(Allies, function()
		Media.DisplayMessage(UserInterface.Translate("remaining-soviet-presence-destroyed"), UserInterface.Translate("incoming-report"))
	end)
end

InitMission = function()
	Camera.Position = AlliesBase.CenterPosition
	TimerColor = HSLColor.Red

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Allies, "MissionTimerInitialised") end)

	Trigger.AfterDelay(TimerTicks, function()
		Media.DisplayMessage(UserInterface.Translate("soviet-reinforcements-approaching"), UserInterface.Translate("incoming-report"))
		Media.PlaySpeechNotification(Allies, "SovietReinforcementsArrived")
		SpawnSovietVehicle(NewSovietEntryPoints, NewSovietRallyPoints)
		FinalAttack()
		Producing = false
	end)

	Trigger.AfterDelay(AttackTicks, SendParadrops)

	Trigger.OnKilled(drum1, function() --Kill the remaining stuff from FrenchReinforcements
		if not boom2.IsDead then boom2.Kill() end
		if not boom4.IsDead then boom4.Kill() end
		if not drum2.IsDead then drum2.Kill() end
		if not drum3.IsDead then drum3.Kill() end
	end)
	Trigger.OnKilled(drum2, function()
		if not boom1.IsDead then boom1.Kill() end
		if not boom5.IsDead then boom5.Kill() end
		Trigger.AfterDelay(DateTime.Seconds(1), function() if not drum1.IsDead then drum1.Kill() end end)
	end)
	Trigger.OnKilled(drum3, function()
		if not boom1.IsDead then boom1.Kill() end
		if not boom3.IsDead then boom3.Kill() end
		Trigger.AfterDelay(DateTime.Seconds(1), function() if not drum1.IsDead then drum1.Kill() end end)
	end)
end

SetupSoviets = function()
	Barrack1.IsPrimaryBuilding = true
	Barrack1.RallyPoint = SovietRally.Location
	Trigger.OnKilledOrCaptured(Barrack1, function()
		SpawningInfantry = false
	end)

	Harvester1.FindResources()
	Trigger.OnDamaged(Harvester1, function()
		Utils.Do(HarvGuards, function(unit)
			GuardHarvester(unit, Harvester1)
		end)
	end)
	Trigger.OnCapture(Harvester1, function()
		Trigger.ClearAll(Harvester1)
	end)

	Harvester2.FindResources()
	Trigger.OnDamaged(Harvester2, function()
		Utils.Do(InfantryGuards, function(unit) GuardHarvester(unit, Harvester2) end)

		local toBuild = { }
		for i = 1, 6, 1 do
			toBuild[i] = Utils.Random(SovietInfantry)
		end

		Soviets.Build(toBuild, function(units)
			Utils.Do(units, function(unit)
				InfantryGuards[#InfantryGuards + 1] = unit
				GuardHarvester(unit, Harvester2)
			end)
		end)
	end)
	Trigger.OnCapture(Harvester2, function()
		Trigger.ClearAll(Harvester2)
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Soviets and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Soviets and building.Health < building.MaxHealth * 3/4 then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	Soviets = Player.GetPlayer("Soviets")

	AddObjectives()
	InitMission()
	SetupSoviets()
end
