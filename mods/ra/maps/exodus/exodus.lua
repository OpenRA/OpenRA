Difficulty = Map.LobbyOption("difficulty")

UnitsEvacuatedThreshold =
{
	hard = 200,
	normal = 100,
	easy = 50
}

AttackAtFrame =
{
	hard = 500,
	normal = 500,
	easy = 600
}

MinAttackAtFrame =
{
	hard = 100,
	normal = 100,
	easy = 150
}

MaxSovietYaks =
{
	hard = 4,
	normal = 2,
	easy = 0
}

SovietParadrops =
{
	hard = 40,
	normal = 20,
	easy = 0
}

SovietParadropTicks =
{
	hard = DateTime.Minutes(17),
	normal = DateTime.Minutes(20),
	easy = DateTime.Minutes(20)
}

SovietUnits2Ticks =
{
	hard = DateTime.Minutes(12),
	normal = DateTime.Minutes(15),
	easy = DateTime.Minutes(15)
}

SovietEntryPoints =
{
	SovietEntryPoint1, SovietEntryPoint2, SovietEntryPoint3, SovietEntryPoint4, SovietEntryPoint5, SovietEntryPoint6
}

SovietRallyPoints =
{
	SovietRallyPoint1, SovietRallyPoint2, SovietRallyPoint3, SovietRallyPoint4, SovietRallyPoint5, SovietRallyPoint6
}

SovietAirfields =
{
	SovietAirfield1, SovietAirfield2, SovietAirfield3, SovietAirfield4,
	SovietAirfield5, SovietAirfield6, SovietAirfield7, SovietAirfield8
}

MountainEntry =
{
	MountainEntry1.Location, MountainEntry2.Location, MountainEntry3.Location, MountainEntry4.Location,
	MountainEntry5.Location, MountainEntry6.Location, MountainEntry7.Location, MountainEntry8.Location
}

BridgeEntry =
{
	BridgeEntry1.Location, BridgeEntry2.Location, BridgeEntry3.Location, BridgeEntry4.Location
}

MobileConstructionVehicle = { "mcv" }
Yak = { "yak" }

ReinforcementsTicks1 = DateTime.Minutes(5)
Reinforcements1 =
{
	"mgg", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk",
	"jeep", "jeep", "e1", "e1", "e1", "e1", "e3", "e3"
}

ReinforcementsTicks2 = DateTime.Minutes(10)
Reinforcements2 =
{
	"mgg", "2tnk", "2tnk", "2tnk", "2tnk", "truk", "truk", "truk",
	"truk",	"truk", "truk", "1tnk", "1tnk", "jeep", "jeep"
}

SovietUnits1 =
{
	"3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk",
	"apc", "e1", "e1", "e2", "e3", "e3", "e4"
}

SovietUnits2 =
{
	"4tnk", "4tnk", "4tnk", "4tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl",
	"v2rl", "ftrk", "apc", "e1", "e1", "e2", "e3", "e3", "e4"
}

CurrentReinforcement1 = 0
CurrentReinforcement2 = 0
SpawnAlliedUnit = function(units)
	Reinforcements.Reinforce(allies1, units, { Allies1EntryPoint.Location, Allies1MovePoint.Location })

	if allies2 then
		Reinforcements.Reinforce(allies2, units, { Allies2EntryPoint.Location, Allies2MovePoint.Location })
	end

	Utils.Do(humans, function(player)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(player, "AlliedReinforcementsNorth")
		end)
	end)

	if CurrentReinforcement1 < #Reinforcements1 then
		CurrentReinforcement1 = CurrentReinforcement1 + 1
		Trigger.AfterDelay(ReinforcementsTicks1, function()
			reinforcements1 = { Reinforcements1[CurrentReinforcement1] }
			SpawnAlliedUnit(reinforcements1)
		end)
	end

	if CurrentReinforcement2 < #Reinforcements2 then
		CurrentReinforcement2 = CurrentReinforcement2 + 1
		Trigger.AfterDelay(ReinforcementsTicks2, function()
			reinforcements2 = { Reinforcements2[CurrentReinforcement2] }
			SpawnAlliedUnit(reinforcements2)
		end)
	end
end

SovietGroupSize = 5
SpawnSovietUnits = function()
	local spawnPoint = Utils.Random(SovietEntryPoints)
	local rallyPoint = Utils.Random(SovietRallyPoints)
	local route = { spawnPoint.Location, rallyPoint.Location }

	local units = SovietUnits1
	if DateTime.GameTime >= SovietUnits2Ticks[Difficulty] then
		units = SovietUnits2
	end

	local unit = { Utils.Random(units) }
	Reinforcements.Reinforce(soviets, unit, route)

	local delay = math.max(attackAtFrame - 5, minAttackAtFrame)
	Trigger.AfterDelay(delay, SpawnSovietUnits)
end

SovietParadrop = 0
SendSovietParadrop = function()
	local sovietParadrops = SovietParadrops[Difficulty]

	if (SovietParadrop > sovietParadrops) then
		return
	end

	SovietParadrop = SovietParadrop + 1

	Utils.Do(humans, function(player)
		Media.PlaySpeechNotification(player, "SovietForcesApproaching")
	end)

	local x = Utils.RandomInteger(ParadropBoxTopLeft.Location.X, ParadropBoxBottomRight.Location.X)
	local y = Utils.RandomInteger(ParadropBoxBottomRight.Location.Y, ParadropBoxTopLeft.Location.Y)

	local randomParadropCell = CPos.New(x, y)
	local lz = Map.CenterOfCell(randomParadropCell)

	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = soviets })
	powerproxy.SendParatroopers(lz)
	powerproxy.Destroy()

	Trigger.AfterDelay(sovietParadropTicks, SendSovietParadrop)
end

IdleHunt = function(unit)
	Trigger.OnIdle(unit, unit.Hunt)
end

ManageSovietUnits = function()
	Utils.Do(SovietRallyPoints, function(rallyPoint)
		local radius = WDist.FromCells(8)
		local units = Map.ActorsInCircle(rallyPoint.CenterPosition, radius, function(actor)
			return actor.Owner == soviets and actor.HasProperty("Hunt")
		end)

		if #units > SovietGroupSize then
			Utils.Do(units, IdleHunt)
		end
	end)

	local scatteredUnits = Utils.Where(soviets.GetGroundAttackers(), function(unit)
		return not Map.IsNamedActor(unit) and unit.IsIdle and unit.HasProperty("Hunt")
	end)
	Utils.Do(scatteredUnits, IdleHunt)
end

AircraftTargets = function()
	local targets = Utils.Where(Map.ActorsInWorld, function(a)
		return (a.Owner == allies1 or a.Owner == allies2) and a.HasProperty("Health")
	end)

	-- prefer mobile units
	table.sort(targets, function(a, b) return a.HasProperty("Move") and not b.HasProperty("Move") end)

	return targets
end

YakAttack = function(yak, target)
	if not target or target.IsDead or (not target.IsInWorld) then
		local targets = AircraftTargets()
		if #targets > 0 then
			target = Utils.Random(targets)
		else
			yak.Wait(DateTime.Seconds(5))
		end
	end

	if target and yak.AmmoCount() > 0 then
		yak.Attack(target)
	else
		yak.ReturnToBase() -- includes yak.Resupply()
	end

	yak.CallFunc(function()
		YakAttack(yak, target)
	end)
end

ManageSovietAircraft = function()
	if allies1.IsObjectiveCompleted(destroyAirbases) then
		return
	end

	local maxSovietYaks = MaxSovietYaks[Difficulty]
	local sovietYaks = soviets.GetActorsByType('yak')
	if #sovietYaks < maxSovietYaks then
		soviets.Build(Yak, function(units)
			local yak = units[1]
			YakAttack(yak)
		end)
	end
end

UnitsEvacuated = 0
EvacuateAlliedUnit = function(unit)
	if (unit.Owner == allies1 or unit.Owner == allies2) and unit.HasProperty("Move") then
		unit.Stop()
		unit.Owner = allies

		if unit.Type == 'mgg' then
			Utils.Do(humans, function(player)
				if player then
					player.MarkCompletedObjective(evacuateMgg)
				end
			end)
		end

		UnitsEvacuated = UnitsEvacuated + 1
		if unit.HasProperty("HasPassengers") then
			UnitsEvacuated = UnitsEvacuated + unit.PassengerCount
		end

		local exitCell = Map.ClosestEdgeCell(unit.Location)
		Trigger.OnIdle(unit, function()
			unit.ScriptedMove(exitCell)
		end)

		local exit = Map.CenterOfCell(exitCell)
		Trigger.OnEnteredProximityTrigger(exit, WDist.FromCells(1), function(a)
			a.Destroy()
		end)

		UserInterface.SetMissionText(UnitsEvacuated .. "/" .. unitsEvacuatedThreshold .. " units evacuated.", TextColor)

		if UnitsEvacuated >= unitsEvacuatedThreshold then
			Utils.Do(humans, function(player)
				if player then
					player.MarkCompletedObjective(evacuateUnits)
				end
			end)
		end
	end
end

Tick = function()
	if DateTime.GameTime % 100 == 0 then
		ManageSovietUnits()
		ManageSovietAircraft()

		Utils.Do(humans, function(player)
			if player and player.HasNoRequiredUnits() then
				soviets.MarkCompletedObjective(sovietObjective)
			end
		end)
	end
end

WorldLoaded = function()
	-- NPC
	neutral = Player.GetPlayer("Neutral")
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")

	-- Player controlled
	allies1 = Player.GetPlayer("Allies1")
	allies2 = Player.GetPlayer("Allies2")

	humans = { allies1, allies2 }
	Utils.Do(humans, function(player)
		if player and player.IsLocalPlayer then
			Trigger.OnObjectiveAdded(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
			end)

			Trigger.OnObjectiveCompleted(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
			end)

			Trigger.OnObjectiveFailed(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
			end)

			Trigger.OnPlayerWon(player, function()
				Media.PlaySpeechNotification(player, "MissionAccomplished")
			end)

			Trigger.OnPlayerLost(player, function()
				Media.PlaySpeechNotification(player, "MissionFailed")
			end)

			TextColor = player.Color
		end
	end)

	unitsEvacuatedThreshold = UnitsEvacuatedThreshold[Difficulty]
	UserInterface.SetMissionText(UnitsEvacuated .. "/" .. unitsEvacuatedThreshold .. " units evacuated.", TextColor)
	Utils.Do(humans, function(player)
		if player then
			evacuateUnits = player.AddPrimaryObjective("Evacuate " .. unitsEvacuatedThreshold .. " units.")
			destroyAirbases = player.AddSecondaryObjective("Destroy the nearby Soviet airbases.")
			evacuateMgg = player.AddSecondaryObjective("Evacuate at least one mobile gap generator.")
		end
	end)

	Trigger.OnAllKilledOrCaptured(SovietAirfields, function()
		Utils.Do(humans, function(player)
			if player then
				player.MarkCompletedObjective(destroyAirbases)
			end
		end)
	end)

	sovietObjective = soviets.AddPrimaryObjective("Eradicate all allied troops.")

	if not allies2 or allies1.IsLocalPlayer then
		Camera.Position = Allies1EntryPoint.CenterPosition
	else
		Camera.Position = Allies2EntryPoint.CenterPosition
	end

	if not allies2 then
		allies1.Cash = 10000
		Media.DisplayMessage("Transferring funds.", "Co-Commander is missing")
	end

	SpawnAlliedUnit(MobileConstructionVehicle)

	minAttackAtFrame = MinAttackAtFrame[Difficulty]
	attackAtFrame = AttackAtFrame[Difficulty]
	Trigger.AfterDelay(attackAtFrame, SpawnSovietUnits)

	sovietParadropTicks = SovietParadropTicks[Difficulty]
	Trigger.AfterDelay(sovietParadropTicks, SendSovietParadrop)

	Trigger.OnEnteredFootprint(MountainEntry, EvacuateAlliedUnit)
	Trigger.OnEnteredFootprint(BridgeEntry, EvacuateAlliedUnit)
end
