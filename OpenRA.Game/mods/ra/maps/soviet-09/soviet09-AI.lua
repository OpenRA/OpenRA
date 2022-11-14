--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
AttackGroupSize = 12
AlliedInfantry = { "e1", "e3" }
AlliedVehicles = { "jeep", "1tnk", "2tnk", "2tnk" }
AlliedAircraftType = { "heli" }
Longbows = { }

ProductionInterval =
{
	easy = DateTime.Seconds(25),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(5)
}
AttackPaths =
{
	{ SouthAttack1.Location, SouthAttack2.Location, DefaultCameraPosition.Location },
	{ TruckStop2.Location, TruckStop1.Location, DefaultCameraPosition.Location }
}

WTransUnits = { { "2tnk", "1tnk", "1tnk", "e3", "e3" }, { "2tnk", "2tnk", "2tnk"  } }
WTransDelays =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(1)
}
WTransWays =
{
	{ SeaEntryEast.Location, SeaEastLZ.Location },
	{ SeaEntryWest1.Location, SeaWestLZ1.Location },
	{ SeaEntryWest1.Location, SeaWestPath1.Location, SeaWestLZ2.Location },
	{ SeaEntryWest2.Location, SeaWestPath2.Location, SeaWestPath3.Location, SeaWestLZ3.Location },
	{ SeaEntryWest2.Location, SeaWestPath2.Location, SeaWestPath3.Location, SeaWestLZ4.Location }
}

ChinookChalk = { "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }
ChinookPaths =
{
	{ SeaEntryEast.Location, LZ1.Location },
	{ TruckEscapeEast.Location, LZ2.Location },
	{ ChinookEntrySouth.Location, LZ3.Location },
	{ SeaEntryWest2.Location, LZ4.Location }
}
ChinookDelay =
{
	easy = { DateTime.Minutes(1), DateTime.Seconds(90) },
	normal = { DateTime.Seconds(45), DateTime.Seconds(75) },
	hard = { DateTime.Seconds(30), DateTime.Minutes(1) }
}
ChinookWaves =
{
	easy = 4,
	normal = 8,
	hard = 12
}
ChinookAttacks = 0

ChinookAttack = function()
	Trigger.AfterDelay(Utils.RandomInteger(ChinookDelay[1], ChinookDelay[2]), function()
		local way = Utils.Random(ChinookPaths)
		local units = ChinookChalk
		local chalk = Reinforcements.ReinforceWithTransport(Greece, "tran", units , way, { way[2], way[1] })[2]
		Utils.Do(chalk, function(unit)
			Trigger.OnAddedToWorld(unit, IdleHunt)
		end)

		ChinookAttacks = ChinookAttacks + 1
		if ChinookAttacks <= ChinookWaves[Difficulty] then
			ChinookAttack()
		end
	end)
end

ProduceInfantry = function()
	if GermanyTent.IsDead or GermanyTent.Owner ~= Germany then
		return
	end

	Germany.Build({ Utils.Random(AlliedInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if GermanyWarFactory.IsDead or GermanyWarFactory.Owner ~= Germany then
		return
	end

	Germany.Build({ Utils.Random(AlliedVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, IdleHunt)

	AttackGroup = { }
end

GreeceAircraft = function()
	if (GreeceHpad1.IsDead or GreeceHpad1.Owner ~= Greece) and (GreeceHpad2.IsDead or GreeceHpad2.Owner ~= Greece) then
		return
	end

	Greece.Build(AlliedAircraftType, function(units)
		local longbow = units[1]
		Longbows[#Longbows + 1] = longbow

		Trigger.OnKilled(longbow, GreeceAircraft)

		local alive = Utils.Where(Longbows, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(90), GreeceAircraft)
		end

		InitializeAttackAircraft(longbow, USSR)
	end)
end

GermanAircraft = function()
	if (GermanyHpad1.IsDead or GermanyHpad1.Owner ~= Germany) and (GermanyHpad2.IsDead or GermanyHpad2.Owner ~= Germany) then
		return
	end

	Germany.Build(AlliedAircraftType, function(units)
		local longbow = units[1]
		Longbows[#Longbows + 1] = longbow

		Trigger.OnKilled(longbow, GermanAircraft)

		local alive = Utils.Where(Longbows, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), GermanAircraft)
		end

		InitializeAttackAircraft(longbow, USSR)
	end)
end

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(Greece, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(DefaultCameraPosition.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(WTransDelays, WTransWaves)
end

ActivateAI = function()
	WTransDelays = WTransDelays[Difficulty]
	ChinookDelay = ChinookDelay[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner ~= USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner ~= USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(3), WTransWaves)
	ChinookAttack()
	ProduceInfantry()
	ProduceVehicles()
	GreeceAircraft()
	GermanAircraft()
end
