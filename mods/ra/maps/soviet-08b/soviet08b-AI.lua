--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
IdlingUnits = { }

DDPatrol = { "dd", "dd" }
DDPatrol1Path = { DDPatrol1Point1.Location, DDPatrol1Point2.Location, DDPatrol1Point3.Location, DDPatrol1Point4.Location }
DDPatrol2Path = { DDPatrol2Point1.Location, DDPatrol2Point2.Location, DDPatrol2Point3.Location, DDPatrol2Point4.Location }
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
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(1)
}

AttackGroup = { }
AttackGroupSize = 10

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

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(Greece, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(SovietBase.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(WTransDelays, WTransWaves)
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

BringDDPatrol = function(patrolPath)
	local units = Reinforcements.Reinforce(Greece, DDPatrol, ShipArrivePath)
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(patrolPath, true, 200)
		end)
	end)

	Trigger.OnAllKilled(units, function()
		if GreeceNavalYard.IsDead then
			return
		else
			if Difficulty == "easy" then
				Trigger.AfterDelay(DateTime.Minutes(7), function() BringDDPatrol(patrolPath) end)
			else
				Trigger.AfterDelay(DateTime.Minutes(4), function() BringDDPatrol(patrolPath) end)
			end
		end
	end)
end

ActivateAI = function()
	WTransUnits = WTransUnits[Difficulty]
	WTransDelays = WTransDelays[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == Greece and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(3), WTransWaves)

	Trigger.AfterDelay(AlliedVehiclesUpgradeDelay, function() AlliedVehicleType = "Upgraded" end)

	ProduceInfantry()
	ProduceVehicles()
	BringDDPatrol(DDPatrol1Path)
	Trigger.AfterDelay(DateTime.Minutes(1), function() BringDDPatrol(DDPatrol2Path) end)
end
