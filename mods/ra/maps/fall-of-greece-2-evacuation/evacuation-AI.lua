--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
SovietInfantry = { "e1", "e2" }
SovietVehicles = { "3tnk", "3tnk", "v2rl" }
SovietAircraftType = { "yak" }
Yaks = { }
AttackPaths = { AttackRight, AttackLeft }

AttackGroupSizes =
{
	easy = 8,
	normal = 9,
	hard = 10
}

ProductionInterval =
{
	easy = DateTime.Seconds(20),
	normal = DateTime.Seconds(10),
	hard = DateTime.Seconds(5)
}

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	local path = Utils.Random(AttackPaths)
	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			unit.AttackMove(path.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceInfantry = function()
	if USSRRax.IsDead or USSRRax.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

ProduceAircraft = function()
	if Airfield.IsDead or Airfield.Owner ~= USSR then
		return
	end

	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)

		local alive = Utils.Where(Yaks, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(yak, Allies)
	end)
end

ParadropDelays =
{
	easy = { DateTime.Minutes(1), DateTime.Minutes(2) },
	normal = { DateTime.Seconds(45), DateTime.Seconds(105) },
	hard = { DateTime.Seconds(30), DateTime.Seconds(90) }
}

ParadropLZs = { ParaLZ1.CenterPosition, ParaLZ2.CenterPosition, ParaLZ3.CenterPosition, ParaLZ4.CenterPosition }

Paradrop = function()
	if Airfield.IsDead or Airfield.Owner ~= USSR then
		return
	end

	local aircraft = StandardDrop.TargetParatroopers(Utils.Random(ParadropLZs))
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelay[1], ParadropDelay[2]), Paradrop)
end

ActivateAI = function()
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	ParadropDelay = ParadropDelays[Difficulty]
	AttackGroupSize = AttackGroupSizes[Difficulty]
	Trigger.AfterDelay(DateTime.Seconds(30), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(3), ProduceVehicles)
	Trigger.AfterDelay(DateTime.Minutes(4), Paradrop)
	Trigger.AfterDelay(DateTime.Minutes(5), ProduceAircraft)
end
