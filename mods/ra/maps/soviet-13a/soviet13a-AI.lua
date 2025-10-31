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
	{ EastWaterEntry.Location, EastBeach1.Location },
	{ EastWaterEntry.Location, EastBeach2.Location },
	{ EastWaterEntry.Location, EastBeach3.Location },
	{ WestWaterEntry.Location, WestBeach1.Location },
	{ WestWaterEntry.Location, WestBeach2.Location }
}

WTransUnits =
{
	hard = { { "2tnk", "2tnk", "e3", "e3", "e3" }, { "2tnk", "2tnk", "2tnk", "2tnk" } },
	normal = { { "2tnk", "1tnk", "e3", "e3", "jeep"  }, { "2tnk", "2tnk", "1tnk", "jeep" } },
	easy = { { "2tnk", "e1", "e1", "e3", "e3" }, { "1tnk", "1tnk", "jeep", "jeep" } }
}

WTransDelays =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(4),
	hard = DateTime.Minutes(3)
}

AttackGroup = { }
AttackGroupSize = 8

ProductionInterval =
{
	easy = DateTime.Seconds(20),
	normal = DateTime.Seconds(14),
	hard = DateTime.Seconds(8)
}

AlliedInfantry = { "e1", "e3" }
AlliedVehiclesUpgradeDelay = DateTime.Minutes(8)
AlliedVehicleType = "Normal"
AlliedVehicles =
{
	Normal = { "1tnk", "2tnk", "2tnk" },
	Upgraded = { "2tnk", "2tnk", "arty" }
}

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(Greece, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(WTransDelay, WTransWaves)
end

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, IdleHunt)

	AttackGroup = { }
end

ProduceInfantry = function()
	if (GreeceTent1.IsDead or GreeceTent1.Owner ~= Greece) and (GreeceTent2.IsDead or GreeceTent2.Owner ~= Greece) then
		return
	end

	Greece.Build({ Utils.Random(AlliedInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if GreeceWarFactory.IsDead or GreeceWarFactory.Owner ~= Greece then
		return
	end

	Greece.Build({ Utils.Random(AlliedVehicles[AlliedVehicleType]) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

AlliedAircraftType = { "heli" }
Longbows = { }

AlliedAircraft = function()
	if (HPad1.IsDead or HPad1.Owner ~= Greece) and (HPad2.IsDead or HPad2.Owner ~= Greece) and (HPad3.IsDead or HPad3.Owner ~= Greece) and (HPad4.IsDead or HPad4.Owner ~= Greece) then
		return
	end

	Greece.Build(AlliedAircraftType, function(units)
		local longbow = units[1]
		Longbows[#Longbows + 1] = longbow

		Trigger.OnKilled(longbow, AlliedAircraft)

		local alive = Utils.Where(Longbows, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(75), AlliedAircraft)
		end

		InitializeAttackAircraft(longbow, USSR)
	end)
end

SendCruiser = function()
	if GoodguyShipyard.IsDead or GoodguyShipyard.Owner ~= GoodGuy then
		return
	end

	local boat = Reinforcements.Reinforce(Greece, { "ca" }, { WestWaterEntry.Location })
	Utils.Do(boat, function(ca)
		ca.Move(CruiserStop.Location)
		Trigger.OnKilled(ca, function()
			Trigger.AfterDelay(DateTime.Minutes(6), SendCruiser)
		end)
	end)
end

ChinookChalk = { "e1", "e1", "e1", "e3", "e3", "e3", "e3", "e3" }
ChinookPath = { ChinookEntry.Location, ChinookLZ.Location }

SendChinook = function()
	if (HPad1.IsDead or HPad1.Owner ~= Greece) and (HPad2.IsDead or HPad2.Owner ~= Greece) and (HPad3.IsDead or HPad3.Owner ~= Greece) and (HPad4.IsDead or HPad4.Owner ~= Greece) then
		return
	end

	local chalk = Reinforcements.ReinforceWithTransport(Greece, "tran", ChinookChalk , ChinookPath, { ChinookPath[1] })[2]
	Utils.Do(chalk, function(unit)
		Trigger.OnAddedToWorld(unit, IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Minutes(5), SendChinook)
end

ActivateAI = function()
	WTransUnits = WTransUnits[Difficulty]
	WTransDelay = WTransDelays[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == Greece and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Trigger.AfterDelay(AlliedVehiclesUpgradeDelay, function() AlliedVehicleType = "Upgraded" end)
	ProduceInfantry()
	Trigger.AfterDelay(DateTime.Minutes(3), ProduceVehicles)
	Trigger.AfterDelay(DateTime.Minutes(5), AlliedAircraft)
	Trigger.AfterDelay(DateTime.Minutes(6), WTransWaves)
	Trigger.AfterDelay(DateTime.Minutes(10), SendCruiser)
end
