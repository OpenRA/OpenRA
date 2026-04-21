--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

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
	easy = 4,
	normal = 3,
	hard = 1
}

BuildDelays =
{
	easy = 90,
	normal = 60,
	hard = 30
}

WaterAttacks =
{
	easy = 1,
	normal = 2,
	hard = 3
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
	{ "e1", "e1", "e1", "e1", "e1"},
	{ "e2", "e2", "e1", "e1", "e1"},
	{ "e4", "e4", "e4", "e1", "e1"}
}

AttackRallyPoints =
{
	{ SovietSideAttack1.Location, SovietBaseAttack.Location },
	{ SovietBaseAttack.Location },
	{ SovietSideAttack2.Location, SovietBaseAttack.Location }
}

ImportantBuildings = { WarFactory, Airfield1, Airfield2, Radar2, Refinery, SovietConyard }
SovietAircraftType = { "yak" }
Yaks = { }
IdlingUnits = { }
IdlingTanks = { tank1, tank2, tank3, tank4, tank5, tank6, tank7 }
IdlingNavalUnits = { }

InitialiseAttack = function()
	Utils.Do(ImportantBuildings, function(a)
		Trigger.OnDamaged(a, function()
			Utils.Do(IdlingTanks, function(unit)
				if not unit.IsDead then
					IdleHunt(unit)
				end
			end)
		end)
		Trigger.OnCapture(a, function()
			Utils.Do(IdlingTanks, function(unit)
				if not unit.IsDead then
					IdleHunt(unit)
				end
			end)
		end)
	end)
end

Attack = 0
ProduceInfantry = function()
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= USSR then
		return
	end

	Attack = Attack + 1
	local toBuild = Utils.Random(InfTypes)
	USSR.Build(toBuild, function(units)
		if Attack == 2 and not AttackTank1.IsDead then
			units[#units + 1] = AttackTank1
		elseif Attack == 4 and not AttackTank2.IsDead then
			units[#units + 1] = AttackTank2
		end

		SendAttack(units, Utils.Random(AttackRallyPoints))
		Trigger.AfterDelay(DateTime.Seconds(BuildDelays), ProduceInfantry)
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
	if SubPen.IsDead or SubPen.Owner ~= USSR then
		return
	end

	if not ShouldProduce and #Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.Type == "syrd" end) < 1 then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceNaval)
		return
	end

	ShouldProduce = true

	USSR.Build(WaterAttackTypes, function(units)
		Utils.Do(units, function(unit)
			IdlingNavalUnits[#IdlingNavalUnits + 1] = unit
		end)

		Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), ProduceNaval)
		if #IdlingNavalUnits >= WaterAttacks then
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				SendAttack(SetupNavalAttackGroup(), { SubPatrol1_2.Location })
			end)
		end
	end)
end

ProduceAircraft = function()
	if (Airfield1.IsDead or Airfield1.Owner ~= USSR) and (Airfield2.IsDead or Airfield2.Owner ~= USSR) then
		return
	end

	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)

		local alive = Utils.Where(Yaks, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(BuildDelays / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(yak, Greece)
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
	for i = 0, 3 do
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
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(USSR, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(SovietBaseAttack.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end

ActivateAI = function()
	WaterAttackTypes = WaterAttackTypes[Difficulty]
	WaterAttacks = WaterAttacks[Difficulty]
	WTransUnits = WTransUnits[Difficulty]
	WTransDelays = WTransDelays[Difficulty]
	BuildDelays = BuildDelays[Difficulty]

	InitialiseAttack()
	Trigger.AfterDelay(DateTime.Seconds(40), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(10), ProduceAircraft)
	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(10), ProduceVehicles)

	WarFactory.RallyPoint = WeaponMeetPoint.Location
	Trigger.AfterDelay(DateTime.Minutes(4) + DateTime.Seconds(10), ProduceNaval)
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays + 1) + DateTime.Seconds(30), WTransWaves)
end
