--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackPaths = { { waypoint7 }, { waypoint8 } }
NodBase = { handofnod, nodairfield, nodrefinery, nodconyard, nodpower1, nodpower2, nodpower3, nodpower4, nodpower5, gun5, gun6, gun7, gun8, nodsilo1, nodsilo2, nodsilo3, nodsilo4, nodobelisk}

PatrolProductionQueue = { }
InfantryAttackGroup = { }
InfantryGroupSize = 5
InfantryProductionCooldown = DateTime.Minutes(3)
InfantryProductionTypes = { "e1", "e1", "e1", "e3", "e3", "e4" }
HarvesterProductionType = { "harv" }
VehicleAttackGroup = { }
VehicleGroupSize = 5
VehicleProductionCooldown = DateTime.Minutes(3)
VehicleProductionTypes = { "bggy", "bggy", "bggy", "ltnk", "ltnk", "arty" }
StartingCash = 14000

BaseRefinery = { type = "proc", pos = CPos.New(12, 25), cost = 1500, exists = true }
BaseNuke1 = { type = "nuke", pos = CPos.New( 5, 24), cost = 500, exists = true }
BaseNuke2 = { type = "nuke", pos = CPos.New( 3, 24), cost = 500, exists = true }
BaseNuke3 = { type = "nuke", pos = CPos.New(16, 30), cost = 500, exists = true }
BaseNuke4 = { type = "nuke", pos = CPos.New(14, 30), cost = 500, exists = true }
BaseNuke5 = { type = "nuke", pos = CPos.New(12, 30), cost = 500, exists = true }
InfantryProduction = { type = "hand", pos = CPos.New(15, 24), cost = 500, exists = true }
VehicleProduction = { type = "afld", pos = CPos.New(3, 27), cost = 2000, exists = true }

NodGuards = {Actor168, Actor169, Actor170, Actor171, Actor172, Actor181, Actor177, Actor188, Actor189, Actor190 }

BaseBuildings = { BaseRefinery, BaseNuke1, BaseNuke2, BaseNuke3, BaseNuke4, InfantryProduction, VehicleProduction }

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

		if cyard.IsDead or cyard.Owner ~= Nod then
			return
		end

		local actor = Actor.Create(building.type, true, { Owner = Nod, Location = building.pos })
		Nod.Cash = Nod.Cash - building.cost

		building.exists = true

		if actor.Type == 'hand' or actor.Type == 'pyle' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(actor) end)
		elseif actor.Type == 'afld' or actor.Type == 'weap' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(actor) end)
		end

		Trigger.OnKilled(actor, function() building.exists = false end)

		Trigger.OnDamaged(actor, function(building)
			if building.Owner == Nod and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBase(cyard) end)
	end)
end

CheckForHarvester = function()
	local harv = Nod.GetActorsByType("harv")
	return #harv > 1
end

GuardBase = function()
	Utils.Do(NodBase, function(building)
		Trigger.OnDamaged(building, function(building) --?
			Utils.Do(NodGuards, function(guard)
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

IdlingUnits = function(Nod)
	local lazyUnits = Nod.GetGroundAttackers()

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
	if building.IsDead or building.Owner ~= Nod then
		return
	elseif not CheckForHarvester() then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(building) end)
		return
	end

	if #PatrolProductionQueue >= 1 then
		local inQueue = PatrolProductionQueue[1]
		local toBuild =  { inQueue.unit[1] }  
		local patrolPath = inQueue.waypoints
		building.Build(toBuild, function(unit)
			ReplenishPatrolUnit(unit[1], handofnod, patrolPath, 40)
			table.remove(PatrolProductionQueue, 1)
		end)
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(building) end)
		return
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
	if building.IsDead or building.Owner ~= Nod then
		return
	elseif not CheckForHarvester() then
		ProduceHarvester(building)
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(building) end)
		return
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
		if actor.Owner == Nod and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Nod and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)
	Nod.Cash = StartingCash
	BuildBase(cyard)
	GuardBase()
end

Trigger.OnAllKilledOrCaptured(NodBase, function()
	IdlingUnits(Nod)
end)

Trigger.OnKilled(nodrefinery, function(building)
	BaseRefinery.exists = false
end)

Trigger.OnKilled(nodpower1, function(building)
	BaseNuke1.exists = false
end)

Trigger.OnKilled(nodpower2, function(building)
	BaseNuke2.exists = false
end)

Trigger.OnKilled(nodpower3, function(building)
	BaseNuke3.exists = false
end)

Trigger.OnKilled(nodpower4, function(building)
	BaseNuke4.exists = false
end)

Trigger.OnKilled(nodpower5, function(building)
	BaseNuke5.exists = false
end)

Trigger.OnKilled(handofnod, function(building)
	InfantryProduction.exists = false
end)

Trigger.OnKilled(nodairfield, function(building)
	VehicleProduction.exists = false
end)
