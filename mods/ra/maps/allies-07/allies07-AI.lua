--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
IdlingUnits = { }
AttackGroup = { }
AttackGroupSize = 10
BGAttackGroup = { }
BGAttackGroupSize = 8
SovietAircraftType = { "yak" }
Yaks = { }
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles =
{
	hard = { "3tnk", "3tnk", "v2rl" },
	normal = { "3tnk" },
	easy = { "3tnk", "apc" }
}

ProductionInterval =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(5)
}

ParadropDelay = { DateTime.Seconds(30), DateTime.Minutes(1) }
ParadropWaves = 6
ParadropLZs = { ParaLZ1.CenterPosition, ParaLZ2.CenterPosition, ParaLZ3.CenterPosition, ParaLZ4.CenterPosition }
Paradropped = 0

Paradrop = function()
	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelay[1], ParadropDelay[2]), function()
		local aircraft = PowerProxy.TargetParatroopers(Utils.Random(ParadropLZs))
		Utils.Do(aircraft, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				IdleHunt(p)
			end)
		end)

		Paradropped = Paradropped + 1
		if Paradropped <= ParadropWaves then
			Paradrop()
		end
	end)
end

SendBGAttackGroup = function()
	if #BGAttackGroup < BGAttackGroupSize then
		return
	end

	Utils.Do(BGAttackGroup, function(unit)
		if not unit.IsDead then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	BGAttackGroup = { }
end

ProduceBadGuyInfantry = function()
	if BadGuyRax.IsDead or BadGuyRax.Owner ~= badguy then
		return
	end

	badguy.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(BGAttackGroup, units[1])
		SendBGAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceBadGuyInfantry)
	end)
end

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
	if USSRRax.IsDead or USSRRax.Owner ~= ussr then
		return
	end

	ussr.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceUSSRInfantry)
	end)
end

ProduceVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= ussr then
		return
	end

	ussr.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

ProduceAircraft = function()
	if (Airfield1.IsDead or Airfield1.Owner ~= ussr) and (Airfield2.IsDead or Airfield2.Owner ~= ussr) and (Airfield3.IsDead or Airfield3.Owner ~= ussr) and (Airfield4.IsDead or Airfield4.Owner ~= ussr) then
		return
	end

	ussr.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)

		local alive = Utils.Where(Yaks, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(yak, greece)
	end)
end

ActivateAI = function()
	SovietVehicles = SovietVehicles[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == ussr and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == ussr and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Paradrop()
	ProduceBadGuyInfantry()
	ProduceUSSRInfantry()
	ProduceVehicles()
	ProduceAircraft()
end
