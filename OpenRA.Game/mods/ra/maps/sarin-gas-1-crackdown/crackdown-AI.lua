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
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles =  { "ttnk", "3tnk", "3tnk", "v2rl" }
ProductionInterval =
{
	easy = DateTime.Seconds(25),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(5)
}

GroundAttackUnits = { { "ttnk", "ttnk", "e2", "e2", "e2" }, { "3tnk", "v2rl", "e4", "e4", "e4" } }
GroundAttackPaths =
{
	{ EscapeSouth5.Location, Patrol1.Location },
	{ EscapeNorth10.Location, EscapeNorth7.Location }
}
GroundWavesDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

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

GroundWaves = function()
	Reinforcements.Reinforce(ussr, Utils.Random(GroundAttackUnits), Utils.Random(GroundAttackPaths), 0, function(unit)
		unit.Hunt()
	end)

	Trigger.AfterDelay(DateTime.Minutes(GroundWavesDelays), GroundWaves)
end

ActivateAI = function()
	GroundWavesDelays = GroundWavesDelays[Difficulty]

	ProduceBadGuyInfantry()
	ProduceUSSRInfantry()
	Trigger.AfterDelay(DateTime.Minutes(1), ProduceVehicles)
	Trigger.AfterDelay(DateTime.Minutes(4), GroundWaves)
end
