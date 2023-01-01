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
BGAttackGroup = { }
BGAttackGroupSize = 10
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles = { "3tnk", "3tnk", "v2rl" }
Mammoths = { "4tnk", "4tnk" }
SovietAircraftType = { "yak" }
Planes = { }

ProductionInterval =
{
	easy = DateTime.Seconds(33),
	normal = DateTime.Seconds(22),
	hard = DateTime.Seconds(11)
}

ParadropDelays =
{
	easy = { DateTime.Minutes(3), DateTime.Minutes(4) },
	normal = { DateTime.Minutes(2), DateTime.Minutes(3) },
	hard = { DateTime.Minutes(1), DateTime.Minutes(2) }
}

ParadropLZs = { DropZone1.CenterPosition, DropZone2.CenterPosition, DropZone3.CenterPosition, DropZone4.CenterPosition, DropZone5.CenterPosition, DropZone6.CenterPosition, DropZone7.CenterPosition, DropZone8.CenterPosition }

Paradrop = function()
	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelays[1], ParadropDelays[2]), function()
		local aircraft = PowerProxy.TargetParatroopers(Utils.Random(ParadropLZs), Angle.North)
		Utils.Do(aircraft, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				IdleHunt(p)
			end)
		end)

		if not (Airfield1.IsDead or Airfield1.Owner ~= BadGuy) and not (Airfield2.IsDead or Airfield2.Owner ~= BadGuy) then
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
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceBadGuyInfantry)
	end)
end

ProduceBadGuyVehicles = function()
	if BadGuyWarFactory.IsDead or BadGuyWarFactory.Owner ~= BadGuy then
		return
	end

	BadGuy.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(BGAttackGroup, units[1])
		SendBGAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceBadGuyVehicles)
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
	if (Airfield1.IsDead or Airfield1.Owner ~= BadGuy) and (Airfield2.IsDead or Airfield2.Owner ~= BadGuy) then
		return
	end

	BadGuy.Build(SovietAircraftType, function(units)
		local plane = units[1]
		Planes[#Planes + 1] = plane

		Trigger.OnKilled(plane, ProduceAircraft)

		local alive = Utils.Where(Planes, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(plane, Allies)
	end)
end

StartBGMammoths =
{
	easy = 18,
	normal = 15,
	hard = 12
}

StartUSSRMammoths =
{
	easy = 12,
	normal = 10,
	hard = 9
}

MammothDelays =
{
	easy = 5,
	normal = 4,
	hard = 3
}

BGMammoths = function()
	if not ForwardCommandBG.IsDead or ForwardCommandBG.Owner ~= BadGuy then
		local tanks = Reinforcements.Reinforce(BadGuy, Mammoths, { BGMammothEntry.Location }, 5)
		Utils.Do(tanks, IdleHunt)

		Trigger.AfterDelay(DateTime.Minutes(MammothDelays), BGMammoths)
	end
end

USSRMammoths = function()
	if not ForwardCommandUSSR.IsDead or ForwardCommandUSSR.Owner ~= USSR then
		local tanks = Reinforcements.Reinforce(USSR, Mammoths, { USSRMammothEntry.Location }, 5)
		Utils.Do(tanks, IdleHunt)

		Trigger.AfterDelay(DateTime.Minutes(MammothDelays), USSRMammoths)
	end
end

ActivateAI = function()
	ParadropDelays = ParadropDelays[Difficulty]
	MammothDelays = MammothDelays[Difficulty]
	StartBGMammoths = StartBGMammoths[Difficulty]
	StartUSSRMammoths = StartUSSRMammoths[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Paradrop()
	ProduceBadGuyInfantry()
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceUSSRInfantry)
	Trigger.AfterDelay(DateTime.Minutes(4), ProduceUSSRVehicles)
	Trigger.AfterDelay(DateTime.Minutes(8), ProduceBadGuyVehicles)
	Trigger.AfterDelay(DateTime.Minutes(10), ProduceAircraft)
	Trigger.AfterDelay(DateTime.Minutes(StartBGMammoths), BGMammoths)
	Trigger.AfterDelay(DateTime.Minutes(StartUSSRMammoths), USSRMammoths)
end
