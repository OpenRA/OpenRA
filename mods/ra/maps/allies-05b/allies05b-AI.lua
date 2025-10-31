--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
AttackGroupSize = 10

SovietInfantry = { "e1", "e1", "e2", "e4" }
SovietVehicles = { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "apc" }
SovietAircraftType = { "yak" }
Planes = { }
SovietProduction = { Conyard, USSRBarracks, USSRWarFactory }

ProductionInterval =
{
	easy = DateTime.Seconds(25),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(5)
}

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceUSSRInfantry = function()
	if USSRBarracks.IsDead or USSRBarracks.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceUSSRInfantry)
	end)
end

ProduceUSSRVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceUSSRVehicles)
	end)
end

ProduceAircraft = function()
	if (Airfield1.IsDead or Airfield1.Owner ~= USSR) and (Airfield2.IsDead or Airfield2.Owner ~= USSR) then
		return
	end

	USSR.Build(SovietAircraftType, function(units)
		local plane = units[1]
		Planes[#Planes + 1] = plane

		Trigger.OnKilled(plane, ProduceAircraft)

		local alive = Utils.Where(Planes, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(plane, Greece)
	end)
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

	ProduceUSSRInfantry()
	Trigger.AfterDelay(DateTime.Minutes(1), ProduceUSSRVehicles)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceAircraft)

	local intactProduction = Utils.Where(SovietProduction, function(self) return not self.IsDead end)
	Trigger.OnAllKilled(intactProduction, function()
		Utils.Do(USSR.GetGroundAttackers(), IdleHunt)
	end)
end
