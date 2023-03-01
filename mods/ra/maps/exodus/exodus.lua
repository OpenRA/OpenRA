--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

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

MountainEntry = { CPos.New(25, 45), CPos.New(25, 46), CPos.New(25, 47), CPos.New(25, 48), CPos.New(25, 49) }

BridgeEntry = { CPos.New(25, 29), CPos.New(26, 29), CPos.New(27, 29), CPos.New(28, 29) }

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
	Reinforcements.Reinforce(Allies1, units, { Allies1EntryPoint.Location, Allies1MovePoint.Location })

	if Allies2 then
		Reinforcements.Reinforce(Allies2, units, { Allies2EntryPoint.Location, Allies2MovePoint.Location })
	end

	Utils.Do(Humans, function(player)
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

	local units = SovietUnits1
	if DateTime.GameTime >= SovietUnits2Ticks[Difficulty] then
		units = SovietUnits2
	end

	local unitType = Utils.Random(units)
	local spawnPoint = Utils.Random(SovietEntryPoints)
	local rallyPoint = Utils.Random(SovietRallyPoints)
	local actor = Actor.Create(unitType, true, { Owner = Soviets, Location = spawnPoint.Location })
	actor.AttackMove(rallyPoint.Location)
	IdleHunt(actor)

	local delay = math.max(AttackAtFrame[Difficulty] - 5, MinAttackAtFrame[Difficulty])
	Trigger.AfterDelay(delay, SpawnSovietUnits)
end

SovietParadrop = 0
SendSovietParadrop = function()
	local sovietParadrops = SovietParadrops[Difficulty]

	if (SovietParadrop > sovietParadrops) then
		return
	end

	SovietParadrop = SovietParadrop + 1

	Utils.Do(Humans, function(player)
		Media.PlaySpeechNotification(player, "SovietForcesApproaching")
	end)

	local x = Utils.RandomInteger(ParadropBoxTopLeft.Location.X, ParadropBoxBottomRight.Location.X)
	local y = Utils.RandomInteger(ParadropBoxBottomRight.Location.Y, ParadropBoxTopLeft.Location.Y)

	local randomParadropCell = CPos.New(x, y)
	local lz = Map.CenterOfCell(randomParadropCell)

	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = Soviets })
	powerproxy.TargetParatroopers(lz)
	powerproxy.Destroy()

	Trigger.AfterDelay(SovietParadropTicks[Difficulty], SendSovietParadrop)
end

AircraftTargets = function(yak)
	local targets = Utils.Where(Map.ActorsInWorld, function(a)
		return (a.Owner == Allies1 or a.Owner == Allies2) and a.HasProperty("Health") and yak.CanTarget(a)
	end)

	-- Prefer mobile units
	table.sort(targets, function(a, b) return a.HasProperty("Move") and not b.HasProperty("Move") end)

	return targets
end

YakAttack = function(yak, target)
	if not target or target.IsDead or (not target.IsInWorld) or (not yak.CanTarget(target)) then
		local targets = AircraftTargets(yak)
		if #targets > 0 then
			target = Utils.Random(targets)
		end
	end

	if target and yak.AmmoCount() > 0 and yak.CanTarget(target) then
		yak.Attack(target)
	else
		-- Includes yak.Resupply()
		yak.ReturnToBase()
	end

	yak.CallFunc(function()
		YakAttack(yak, target)
	end)
end

ManageSovietAircraft = function()
	if Allies1.IsObjectiveCompleted(DestroyAirbases) then
		return
	end

	local maxSovietYaks = MaxSovietYaks[Difficulty]
	local sovietYaks = Soviets.GetActorsByType('yak')
	if #sovietYaks < maxSovietYaks then
		Soviets.Build(Yak, function(units)
			local yak = units[1]
			YakAttack(yak)
		end)
	end
end

SetEvacuateMissionText = function()
	local attributes = { ["evacuated"] = UnitsEvacuated, ["threshold"] = UnitsEvacuatedThreshold[Difficulty] }
	local unitsEvacuated = UserInterface.Translate("units-evacuated", attributes)
	UserInterface.SetMissionText(unitsEvacuated, TextColor)
end

UnitsEvacuated = 0
EvacuateAlliedUnit = function(unit)
	if (unit.Owner == Allies1 or unit.Owner == Allies2) and unit.HasProperty("Move") then
		unit.Stop()
		unit.Owner = Allies

		if unit.Type == 'mgg' then
			Utils.Do(Humans, function(player)
				if player then
					player.MarkCompletedObjective(EvacuateMgg)
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

		SetEvacuateMissionText()

		if UnitsEvacuated >= UnitsEvacuatedThreshold[Difficulty] then
			Utils.Do(Humans, function(player)
				if player then
					player.MarkCompletedObjective(EvacuateUnits)
				end
			end)
		end
	end
end

Tick = function()
	if DateTime.GameTime % 100 == 0 then
		ManageSovietAircraft()

		Utils.Do(Humans, function(player)
			if player and player.HasNoRequiredUnits() then
				Soviets.MarkCompletedObjective(SovietObjective)
			end
		end)
	end
end

WorldLoaded = function()
	-- NPC
	Neutral = Player.GetPlayer("Neutral")
	Allies = Player.GetPlayer("Allies")
	Soviets = Player.GetPlayer("Soviets")

	-- Player controlled
	Allies1 = Player.GetPlayer("Allies1")
	Allies2 = Player.GetPlayer("Allies2")

	Humans = { Allies1, Allies2 }
	Utils.Do(Humans, function(player)
		if player and player.IsLocalPlayer then
			InitObjectives(player)
			TextColor = player.Color
		end
	end)

	SetEvacuateMissionText()
	Utils.Do(Humans, function(player)
		if player then
			EvacuateUnits = AddPrimaryObjective(player, UserInterface.Translate("evacuate-units", { ["threshold"] = UnitsEvacuatedThreshold[Difficulty] }))
			DestroyAirbases = AddSecondaryObjective(player, "destroy-nearby-soviet-airbases")
			EvacuateMgg = AddSecondaryObjective(player, "evacuate-at-least-one-gap-generator")
		end
	end)

	Trigger.OnAllKilledOrCaptured(SovietAirfields, function()
		Utils.Do(Humans, function(player)
			if player then
				player.MarkCompletedObjective(DestroyAirbases)
			end
		end)
	end)

	SovietObjective = AddPrimaryObjective(Soviets, "")

	if not Allies2 or Allies1.IsLocalPlayer then
		Camera.Position = Allies1EntryPoint.CenterPosition
	else
		Camera.Position = Allies2EntryPoint.CenterPosition
	end

	if not Allies2 then
		Allies1.Cash = 10000
		Media.DisplayMessage(UserInterface.Translate("transferring-funds"), UserInterface.Translate("co-commander-missing"))
	end

	SpawnAlliedUnit(MobileConstructionVehicle)

	Trigger.AfterDelay(AttackAtFrame[Difficulty], SpawnSovietUnits)
	Trigger.AfterDelay(SovietParadropTicks[Difficulty], SendSovietParadrop)

	Trigger.OnEnteredFootprint(MountainEntry, EvacuateAlliedUnit)
	Trigger.OnEnteredFootprint(BridgeEntry, EvacuateAlliedUnit)
end
