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
	{ AttackTicks, { "SovietSquad", SovietRally5 } },
	{ 0, { "SovietSquad", SovietRally6 } },
	{ AttackTicks * 2, { "SovietSquad", SovietParaDrop3 } },
	{ 0, { "SovietPlatoonUnits", SovietRally5 } },
	{ 0, { "SovietPlatoonUnits", SovietRally6 } },
	{ 0, { "SovietSquad", SovietRally2 } },
	{ AttackTicks * 2, { "SovietSquad", SovietParaDrop2 } },
	{ AttackTicks * 2, { "SovietSquad", SovietParaDrop1 } },
	{ AttackTicks * 3, { "SovietSquad", SovietParaDrop1 } }
}

IdleHunt = function(unit) Trigger.OnIdle(unit, unit.Hunt) end

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
	end
end

Tick = function()
	if soviets.HasNoRequiredUnits() then
		if DestroyObj then
			allies.MarkCompletedObjective(DestroyObj)
		else
			DestroyObj = allies.AddPrimaryObjective("Destroy all Soviet forces in the area!")
			allies.MarkCompletedObjective(DestroyObj)
		end
	end

	if allies.HasNoRequiredUnits() then
		soviets.MarkCompletedObjective(SovietObj)
	end

	if soviets.Resources > soviets.ResourceCapacity / 2 then
		soviets.Resources = soviets.ResourceCapacity / 2
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

	if DateTime.Minutes(5) == TimerTicks - DateTime.GameTime then
		Media.PlaySpeechNotification(allies, "WarningFiveMinutesRemaining")
		InitCountDown()
	end
end

SendSovietParadrops = function(table)
	local paraproxy = Actor.Create(table[1], false, { Owner = soviets })
	units = paraproxy.SendParatroopers(table[2].CenterPosition)
	Utils.Do(units, function(unit) IdleHunt(unit) end)
	paraproxy.Destroy()
end

SpawnSovietInfantry = function()
	soviets.Build({ Utils.Random(SovietInfantry) }, function(units)
		IdleHunt(units[1])
	end)
end

SpawnSovietVehicle = function(spawnpoints, rallypoints)
	local route = Utils.RandomInteger(1, #spawnpoints + 1)
	local rally = Utils.RandomInteger(1, #rallypoints + 1)
	local unit = Reinforcements.Reinforce(soviets, { Utils.Random(SovietVehicles) }, { spawnpoints[route].Location, rallypoints[rally].Location })[1]
	IdleHunt(unit)
end

SpawnAndAttack = function(types, entry)
	local units = Reinforcements.Reinforce(soviets, types, { entry })
	Utils.Do(units, function(unit)
		IdleHunt(unit)
	end)
	return units
end

FrenchReinforcements = function()
	Camera.Position = SovietRally1.CenterPosition
	local camera = Actor.Create("camera", true, { Owner = allies, Location = SovietRally1.Location })

	if drum1.IsDead or drum2.IsDead or drum3.IsDead then
		Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
		Reinforcements.Reinforce(allies, FrenchSquad, { FranceEntry.Location, FranceRally.Location })
		Trigger.AfterDelay(DateTime.Seconds(3), function() camera.Destroy() end)
		return
	end

	powerproxy = Actor.Create("powerproxy.parabombs", false, { Owner = allies })
	powerproxy.SendAirstrike(drum1.CenterPosition, false, 256 - 28)
	powerproxy.SendAirstrike(drum2.CenterPosition, false, 256 - 32)
	powerproxy.SendAirstrike(drum3.CenterPosition, false, 256 - 36)
	powerproxy.Destroy()

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
		Reinforcements.Reinforce(allies, FrenchSquad, { FranceEntry.Location, FranceRally.Location })
		Trigger.AfterDelay(DateTime.Seconds(3), function() camera.Destroy() end)
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

	Trigger.OnAllKilled(units, function()
		if not DestroyObj then
			Media.DisplayMessage("Excellent work Commander! We have reinforced our position enough to initiate a counter-attack.", "Incoming Report")
			DestroyObj = allies.AddPrimaryObjective("Destroy the remaining Soviet forces in the area!")
		end
		allies.MarkCompletedObjective(SurviveObj)
	end)
end

wave = 1
SendParadrops = function()
	SendSovietParadrops(ParaWaves[wave][2])

	wave = wave + 1
	if wave > #ParaWaves then
		Trigger.AfterDelay(AttackTicks, FrenchReinforcements)
	else
		Trigger.AfterDelay(ParaWaves[wave][1], SendParadrops)
	end
end

SetupBridges = function()
	local count = 0
	local counter = function()
		count = count + 1
		if count == 2 then
			allies.MarkCompletedObjective(RepairBridges)
		end
	end

	Media.DisplayMessage("Commander! The Soviets destroyed the brigdes to disable our reinforcements. Repair them for additional reinforcements.", "Incoming Report")
	RepairBridges = allies.AddSecondaryObjective("Repair the two southern brigdes.")

	local bridgeA = Map.ActorsInCircle(BrokenBridge1.CenterPosition, WRange.FromCells(1), function(self) return self.Type == "bridge1" end)
	local bridgeB = Map.ActorsInCircle(BrokenBridge2.CenterPosition, WRange.FromCells(1), function(self) return self.Type == "bridge1" end)

	Utils.Do(bridgeA, function(bridge)
		Trigger.OnDamaged(bridge, function()
			Utils.Do(bridgeA, function(self) Trigger.ClearAll(self) end)
			Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
			Reinforcements.Reinforce(allies, { "1tnk", "2tnk", "2tnk" }, { ReinforcementsEntry1.Location, ReinforcementsRally1.Location })
			counter()
		end)
	end)
	Utils.Do(bridgeB, function(bridge)
		Trigger.OnDamaged(bridge, function()
			Utils.Do(bridgeB, function(self) Trigger.ClearAll(self) end)
			Media.PlaySpeechNotification(allies, "AlliedReinforcementsArrived")
			Reinforcements.Reinforce(allies, { "jeep", "1tnk", "1tnk" }, { ReinforcementsEntry2.Location, ReinforcementsRally2.Location })
			counter()
		end)
	end)
end

InitCountDown = function()
	Trigger.AfterDelay(DateTime.Minutes(1), function() Media.PlaySpeechNotification(allies, "WarningFourMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() Media.PlaySpeechNotification(allies, "WarningThreeMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() Media.PlaySpeechNotification(allies, "WarningTwoMinutesRemaining") end)
	Trigger.AfterDelay(DateTime.Minutes(4), function() Media.PlaySpeechNotification(allies, "WarningOneMinuteRemaining") end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	SurviveObj = allies.AddPrimaryObjective("Enforce your position and hold-out the onslaught.")
	SovietObj = soviets.AddPrimaryObjective("Eliminate all Allied forces.")

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		SetupBridges()
	end)

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
		Media.DisplayMessage("We have destroyed the remaining Soviet presence!", "Incoming Report")
	end)
end

InitMission = function()
	Camera.Position = AlliesBase.CenterPosition

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(allies, "MissionTimerInitialised") end)

	Trigger.AfterDelay(TimerTicks, function()
		Media.DisplayMessage("The Soviet reinforcements are approaching!", "Incoming Report")
		Media.PlaySpeechNotification(allies, "SovietReinforcementsArrived")
		SpawnSovietVehicle(NewSovietEntryPoints, NewSovietRallyPoints)
		FinalAttack()
		Producing = false
		Timer.Destroy()
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

	Harvester2.FindResources()
	Trigger.OnDamaged(Harvester2, function()
		Utils.Do(InfantryGuards, function(unit) GuardHarvester(unit, Harvester2) end)

		local toBuild = { }
		for i = 1, 6, 1 do
			toBuild[i] = Utils.Random(SovietInfantry)
		end

		soviets.Build(toBuild, function(units)
			Utils.Do(units, function(unit)
				InfantryGuards[#InfantryGuards + 1] = unit
				GuardHarvester(unit, Harvester2)
			end)
		end)
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(self) return self.Owner == soviets and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == soviets and building.Health < building.MaxHealth * 3/4 then
					building.StartBuildingRepairs()
				end
			end)
		end)

		local units = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(self) return self.Owner == soviets and self.HasProperty("AutoTarget") end)
		Utils.Do(units, function(unit)
			unit.Stance = "Defend"
		end)
	end)
end

WorldLoaded = function()

	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")

	InitObjectives()
	InitMission()
	SetupSoviets()
end
