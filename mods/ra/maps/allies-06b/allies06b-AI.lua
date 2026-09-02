--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SovietsActivated = false

WTransWays =
{
	{ WaterUnloadEntry1.Location, WaterUnload1.Location },
	{ WaterUnloadEntry2.Location, WaterUnload2.Location },
	{ WaterUnloadEntry3.Location, WaterUnload3.Location }
}

WTransUnits =
{
	hard = { { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl" }, { "v2rl", "v2rl", "e4", "e4", "3tnk" } },
	normal = { { "e1", "e1", "3tnk", "3tnk", "v2rl" }, { "e4", "e4", "e4", "e4", "v2rl" } },
	easy = { { "e1", "e1", "e1", "e2", "e2" }, { "e2", "e2", "3tnk" } }
}

WTransDelays =
{
	easy = DateTime.Seconds(240),
	normal = DateTime.Seconds(180),
	hard = DateTime.Seconds(140)
}

FirstAirDelays =
{
	easy = { DateTime.Seconds(180), DateTime.Seconds(270) },
	normal = { DateTime.Seconds(120), DateTime.Seconds(180) },
	hard = { DateTime.Seconds(90), DateTime.Seconds(135) }
}

BuildDelays =
{
	easy = DateTime.Seconds(90),
	normal = DateTime.Seconds(60),
	hard = DateTime.Seconds(30)
}

WaterAttackTypes =
{
	easy = { "ss" },
	normal = { "ss", "ss" },
	hard = { "ss", "ss", "ss" }
}

VehicleTypes = { "v2rl", "3tnk", "3tnk", "3tnk", "3tnk", "harv" }

InfTypes =
{
	{ "e1", "e1", "e1" },
	{ "e2", "e2", "e1" },
	{ "e4", "e4", "e1" }
}

AttackRallyPoints =
{
	{ SovietSideAttack1.Location, SovietBaseAttack.Location },
	{ SovietBaseAttack.Location },
	{ SovietSideAttack2.Location, SovietBaseAttack.Location }
}

ImportantBuildings = { WarFactory, Airfield1, Airfield2, NorthRadarDome, Refinery, SovietConyard }
SovietAircraftType = { "yak" }
IdlingTanks = { tank1, tank2, tank3, tank4, tank5, tank6, tank7 }
IdlingNavalUnits = { }

InitialiseAttack = function()
	Utils.Do(ImportantBuildings, function(a)
		Trigger.OnDamaged(a, function()
			Utils.Do(IdlingTanks, IdleHunt)
		end)
		Trigger.OnCapture(a, function()
			Utils.Do(IdlingTanks, IdleHunt)
		end)
	end)
end

InfantryWave = 0
ProduceInfantry = function()
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= USSR then
		return
	end

	InfantryWave = InfantryWave + 1
	local toBuild = Utils.Random(InfTypes)
	USSR.Build(toBuild, function(units)
		if InfantryWave == 2 and not AttackTank1.IsDead then
			units[#units + 1] = AttackTank1
		elseif InfantryWave == 4 and not AttackTank2.IsDead then
			units[#units + 1] = AttackTank2
		end

		SendAttack(units, Utils.Random(AttackRallyPoints))
		Trigger.AfterDelay(BuildDelays[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if WarFactory.IsDead or WarFactory.Owner ~= USSR then
		return
	end
	USSR.Build(VehicleTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Type ~= "harv" then
				IdlingTanks[#IdlingTanks + 1] = unit
			end
		end)
	end)
end

ProduceNaval = function()
	if not Greece.HasPrerequisites({ "syrd" }) then
		Trigger.AfterDelay(DateTime.Seconds(60), ProduceNaval)
		return
	end

	if SubPen.IsDead or SubPen.Owner ~= USSR then
		return
	end

	USSR.Build(WaterAttackTypes[Difficulty], function(units)
		Utils.Do(units, function(unit)
			IdlingNavalUnits[#IdlingNavalUnits + 1] = unit
		end)

		Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), ProduceNaval)
		if #IdlingNavalUnits >= #WaterAttackTypes[Difficulty] then
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				SendAttack(SetupNavalAttackGroup(), { Harbor.Location })
			end)
		end
	end)
end

ProduceAircraft = function()
	if not USSR.HasPrerequisites({ "afld" }) then
		return
	end

	USSR.Build(SovietAircraftType, function(units)
		Utils.Do(units, function(yak)
			InitializeAttackAircraft(yak, Greece)

			Trigger.OnKilled(yak, function()
				Trigger.AfterDelay(BuildDelays[Difficulty], ProduceAircraft)
			end)
		end)
	end)
end

SendAttack = function(units, path)
	Utils.Do(units, function(unit)
		unit.Patrol(path, false)
		IdleHunt(unit)
	end)
end

SetupNavalAttackGroup = function()
	local units = { }
	for i = 1, 3 do
		if #IdlingNavalUnits == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingNavalUnits + 1)
		if IdlingNavalUnits[number] and not IdlingNavalUnits[number].IsDead then
			units[i] = IdlingNavalUnits[number]
			table.remove(IdlingNavalUnits, number)
		end
	end

	return units
end

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits[Difficulty])
	local attackUnits = Reinforcements.ReinforceWithTransport(USSR, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(SovietBaseAttack.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(WTransDelays[Difficulty], WTransWaves)
end

ActivateAI = function()
	if SovietsActivated then
		return
	end
	SovietsActivated = true

	InitialiseAttack()
	Trigger.AfterDelay(DateTime.Seconds(40), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(10), ProduceVehicles)

	Utils.Do(FirstAirDelays[Difficulty], function(delay)
		Trigger.AfterDelay(delay, ProduceAircraft)
	end)

	WarFactory.RallyPoint = WeaponMeetPoint.Location
	Trigger.AfterDelay(WTransDelays[Difficulty] + DateTime.Seconds(150), WTransWaves)
	Trigger.OnAnyKilled(USSR.GetActorsByType("ss"), ProduceNaval)
end
