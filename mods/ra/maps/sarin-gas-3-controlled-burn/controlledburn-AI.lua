--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
AttackGroupSize = 10
BGAttackGroup = { }
BGAttackGroupSize = 8
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles = { "3tnk", "3tnk", "v2rl" }
SovietAircraftType = { "mig", "yak" }
Planes = { }

ProductionInterval =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(20),
	hard = DateTime.Seconds(10)
}

SendBGAttackGroup = function()
	if #BGAttackGroup < BGAttackGroupSize then
		return
	end

	Utils.Do(BGAttackGroup, function(unit)
		if not unit.IsDead then
			IdleHunt(unit)
		end
	end)

	BGAttackGroup = { }
end

ProduceBadGuyInfantry = function()
	if BadGuyRax.IsDead or BadGuyRax.Owner ~= BadGuy then
		return
	end

	BadGuy.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(BGAttackGroup, units[1])
		SendBGAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceBadGuyInfantry)
	end)
end

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			IdleHunt(unit)
		end
	end)

	AttackGroup = { }
end

ProduceUSSRInfantry = function()
	if (USSRRax1.IsDead or USSRRax1.Owner ~= USSR) and (USSRRax2.IsDead or USSRRax2.Owner ~= USSR) then
		return
	end

	USSR.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceUSSRInfantry)
	end)
end

ProduceVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceVehicles)
	end)
end

GroundAttackUnits = { {"4tnk", "3tnk", "e2", "e2", "e2", "e2" }, { "3tnk", "3tnk", "v2rl", "e4", "e4", "e4" }, {"ttnk", "ttnk", "ttnk", "shok", "shok", "shok" } }

GroundAttackPaths = 
{ 
	{ SovietGroundEntry1.Location },
	{ SovietGroundEntry2.Location },
	{ SovietGroundEntry3.Location }
}
GroundWavesDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

GroundWaves = function()
	if not ForwardCommand.IsDead then
		local path = Utils.Random(GroundAttackPaths)
		local units = Reinforcements.Reinforce(BadGuy, Utils.Random(GroundAttackUnits), path)
		Utils.Do(units, IdleHunt)

		Trigger.AfterDelay(DateTime.Minutes(GroundWavesDelays), GroundWaves)
	end
end

ProduceAircraft = function()
	if Airfield.IsDead or Airfield.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietAircraftType) }, function(units)
		local plane = units[1]
		Planes[#Planes + 1] = plane

		Trigger.OnKilled(plane, ProduceAircraft)

		local alive = Utils.Where(Planes, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Map.LobbyOption("difficulty")] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(plane, Greece)
	end)
end

ActivateAI = function()
	local difficulty = Map.LobbyOption("difficulty")
	GroundWavesDelays = GroundWavesDelays[difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	ProduceBadGuyInfantry()
	ProduceUSSRInfantry()
	ProduceVehicles()
	Trigger.AfterDelay(DateTime.Minutes(GroundWavesDelays), GroundWaves)
	Trigger.AfterDelay(DateTime.Minutes(5), ProduceAircraft)
end
