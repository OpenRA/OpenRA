--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
IdlingUnits = { }

DDPatrol1 = { "dd", "dd" }
DDPatrol2 = { "dd", "dd" }
DDPatrol1Path = { DDPatrol1Point1.Location, DDPatrol1Point2.Location, DDPatrol1Point3.Location, DDPatrol1Point4.Location, DDPatrol1Point5.Location, DDPatrol1Point6.Location }
DDPatrol2Path = { DDPatrol2Point1.Location, DDPatrol2Point2.Location, DDPatrol2Point3.Location, DDPatrol2Point4.Location, DDPatrol2Point5.Location, DDPatrol2Point6.Location, DDPatrol2Point7.Location }
ShipArrivePath = { DDEntry.Location, DDEntryStop.Location }

WTransWays =
{
	{ WaterUnloadEntry1.Location, WaterUnload1.Location },
	{ WaterUnloadEntry2.Location, WaterUnload2.Location },
	{ WaterUnloadEntry3.Location, WaterUnload3.Location },
	{ WaterUnloadEntry4.Location, WaterUnload4.Location },
	{ WaterUnloadEntry5.Location, WaterUnload5.Location },
	{ WaterUnloadEntry6.Location, WaterUnload6.Location }
}

WTransUnits =
{
	hard = { { "2tnk", "1tnk", "e1", "e3", "e3" }, { "2tnk", "2tnk", "2tnk"  } },
	normal = { { "1tnk", "1tnk", "e3", "e3", "jeep"  }, { "2tnk", "e3", "e3", "jeep" } },
	easy = { { "1tnk", "e1", "e1", "e3", "e3" }, { "e3", "e3", "jeep", "jeep" } }
}

WTransDelays =
{
	easy = 5,
	normal = 3,
	hard = 2
}

AttackGroup = { }
AttackGroupSize = 8

ProductionInterval =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(20),
	hard = DateTime.Seconds(10)
}

AlliedInfantry = { "e1", "e3" }
AlliedVehiclesUpgradeDelay = DateTime.Minutes(15)
AlliedVehicleType = "Normal"
AlliedVehicles =
{
	Normal = { "jeep", "1tnk", "1tnk" },
	Upgraded = { "2tnk", "arty" }
}

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(greece, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(SovietStart.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
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

ProduceInfantry = function()
	if (GreeceTent1.IsDead or GreeceTent1.Owner ~= greece) and (GreeceTent2.IsDead or GreeceTent2.Owner ~= greece) then
		return
	end

	greece.Build({ Utils.Random(AlliedInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if GreeceWarFactory.IsDead or GreeceWarFactory.Owner ~= greece then
		return
	end

	greece.Build({ Utils.Random(AlliedVehicles[AlliedVehicleType]) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceVehicles)
	end)
end

BringDDPatrol1 = function()
	local units = Reinforcements.Reinforce(greece, DDPatrol1, ShipArrivePath, 0)
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(DDPatrol1Path, true, 200)
		end)
	end)
	if GreeceNavalYard.IsDead then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Map.LobbyOption("difficulty") == "easy" then
				Trigger.AfterDelay(DateTime.Minutes(7), BringDDPatrol1)
			else
				Trigger.AfterDelay(DateTime.Minutes(4), BringDDPatrol1)
			end
		end)
	end
end

BringDDPatrol2 = function()
	local units = Reinforcements.Reinforce(greece, DDPatrol2, ShipArrivePath, 0)
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(DDPatrol2Path, true, 200)
		end)
	end)
	if GreeceNavalYard.IsDead then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Map.LobbyOption("difficulty") == "easy" then
				Trigger.AfterDelay(DateTime.Minutes(7), BringDDPatrol2)
			else
				Trigger.AfterDelay(DateTime.Minutes(4), BringDDPatrol2)
			end
		end)
	end
end

ActivateAI = function()
	local difficulty = Map.LobbyOption("difficulty")
	WTransUnits = WTransUnits[difficulty]
	WTransDelays = WTransDelays[difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == greece and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == greece and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(3), WTransWaves)

	Trigger.AfterDelay(AlliedVehiclesUpgradeDelay, function() AlliedVehicleType = "Upgraded" end)	

	ProduceInfantry()
	ProduceVehicles()
	BringDDPatrol1()
	Trigger.AfterDelay(DateTime.Minutes(1), BringDDPatrol2)
end
