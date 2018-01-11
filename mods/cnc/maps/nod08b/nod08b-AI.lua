--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackPaths = { { AttackPath1 }, { AttackPath2 }, { AttackPath3 } }
GDIBase = { GDICYard, GDIPyle, GDIWeap, GDIHQ, GDINuke1, GDINuke2, GDINuke3, GDINuke4, GDIProc, GDIBuilding1, GDIBuilding2, GDIBuilding3, GDIBuilding4, GDIBuilding5, GDIBuilding6, GDIBuilding7, GDIBuilding8, GDIBuilding9, GDIBuilding10, GDIBuilding11, GDIBuilding12, GDIBuilding13 }
GDIOrcas = { GDIOrca1, GDIOrca2 }
InfantryAttackGroup = { }
InfantryGroupSize = 4
InfantryProductionCooldown = DateTime.Minutes(3)
InfantryProductionTypes = { "e1", "e1", "e2" }
HarvesterProductionType = { "harv" }
VehicleAttackGroup = { }
VehicleGroupSize = 4
VehicleProductionCooldown = DateTime.Minutes(4)
VehicleProductionTypes = { "jeep", "jeep", "mtnk", "mtnk", "mtnk" }
StartingCash = 4000

BaseProc = { type = "proc", pos = CPos.New(19, 48), cost = 1500, exists = true }
BaseNuke1 = { type = "nuke", pos = CPos.New(9, 53), cost = 500, exists = true }
BaseNuke2 = { type = "nuke", pos = CPos.New(7, 52), cost = 500, exists = true }
BaseNuke3 = { type = "nuke", pos = CPos.New(11, 53), cost = 500, exists = true }
BaseNuke4 = { type = "nuke", pos = CPos.New(13, 52), cost = 500, exists = true }
InfantryProduction = { type = "pyle", pos = CPos.New(15, 52), cost = 500, exists = true }
VehicleProduction = { type = "weap", pos = CPos.New(8, 48), cost = 2000, exists = true }

BaseBuildings = { BaseProc, BaseNuke1, BaseNuke2, BaseNuke3, BaseNuke4, InfantryProduction, VehicleProduction }

AutoGuard = function(guards)
	Utils.Do(guards, function(guard)
		Trigger.OnDamaged(guard, function(guard)
		IdleHunt(guard)
		end)
	end)
end

BuildBase = function(cyard)
	Utils.Do(BaseBuildings, function(building)
		if not building.exists and not cyardIsBuilding then
			BuildBuilding(building, cyard)
			return
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBase(cyard) end)
end

BuildBuilding = function(building, cyard)
	cyardIsBuilding = true

	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		cyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= enemy then
			return
		end

		local actor = Actor.Create(building.type, true, { Owner = enemy, Location = building.pos })
		enemy.Cash = enemy.Cash - building.cost

		building.exists = true

		if actor.Type == 'pyle' or actor.Type == 'hand' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(actor) end)
		elseif actor.Type == 'weap' or actor.Type == 'afld' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(actor) end)
		end

		Trigger.OnKilled(actor, function() building.exists = false end)

		Trigger.OnDamaged(actor, function(building)
			if building.Owner == enemy and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBase(cyard) end)
	end)
end

CheckForHarvester = function()
	local harv = enemy.GetActorsByType("harv")
	return #harv > 0
end

GuardBase = function()
	Utils.Do(GDIBase, function(building)
		Trigger.OnDamaged(building, function(guard)
			Utils.Do(GDIOrcas, function(guard)
				if not guard.IsDead and not building.IsDead then
					guard.Guard(building)
				end
			end)
		end)
	end)
end

IdleHunt = function(unit)
	if not unit.IsDead then 
		Trigger.OnIdle(unit, unit.Hunt) 
	end 
end

IdlingUnits = function(enemy)
	local lazyUnits = enemy.GetGroundAttackers()

	Utils.Do(lazyUnits, function(unit)
		IdleHunt(unit)
	end)
end

ProduceHarvester = function(building)
	if not buildingHarvester then
		buildingHarvester = true
		building.Build(HarvesterProductionType, function()
			buildingHarvester = false
		end)
	end
end

ProduceInfantry = function(building)
	if building.IsDead or building.Owner ~= enemy then
		return
	elseif not CheckForHarvester() then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(building) end)
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(InfantryProductionTypes) }
	local Path = Utils.Random(AttackPaths)
	building.Build(toBuild, function(unit)
		InfantryAttackGroup[#InfantryAttackGroup + 1] = unit[1]

		if #InfantryAttackGroup >= InfantryGroupSize then
			SendUnits(InfantryAttackGroup, Path)
			InfantryAttackGroup = { }
			Trigger.AfterDelay(InfantryProductionCooldown, function() ProduceInfantry(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceInfantry(building) end)
		end
	end)
	
end

ProduceVehicle = function(building)
	if building.IsDead or building.Owner ~= enemy then
		return
	elseif not CheckForHarvester() then
		ProduceHarvester(building)
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(building) end)
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(VehicleProductionTypes) }
	local Path = Utils.Random(AttackPaths)
	building.Build(toBuild, function(unit)
		VehicleAttackGroup[#VehicleAttackGroup + 1] = unit[1]

		if #VehicleAttackGroup >= VehicleGroupSize then
			SendUnits(VehicleAttackGroup, Path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(VehicleProductionCooldown, function() ProduceVehicle(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceVehicle(building) end)
		end
	end)
end

SendUnits = function(units, waypoints)
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			Utils.Do(waypoints, function(waypoint)
				unit.AttackMove(waypoint.Location)
			end)
			IdleHunt(unit)
		end
	end)
end

StartAI = function(cyard)
	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == enemy and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == enemy and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)
	enemy.Cash = StartingCash
	BuildBase(cyard)
	GuardBase()
end

Trigger.OnAllKilledOrCaptured(GDIBase, function()
	IdlingUnits(enemy)
end)

Trigger.OnKilled(GDIProc, function(building)
	BaseProc.exists = false
end)

Trigger.OnKilled(GDINuke1, function(building)
	BaseNuke1.exists = false
end)

Trigger.OnKilled(GDINuke2, function(building)
	BaseNuke2.exists = false
end)

Trigger.OnKilled(GDINuke3, function(building)
	BaseNuke3.exists = false
end)

Trigger.OnKilled(GDINuke4, function(building)
	BaseNuke4.exists = false
end)

Trigger.OnKilled(GDIPyle, function(building)
	InfantryProduction.exists = false
end)

Trigger.OnKilled(GDIWeap, function(building)
	VehicleProduction.exists = false
end)
