--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackPaths = { { waypoint7 }, { waypoint8 } }
NodBase = { handofnod, nodairfield, nodrefinery, NodCYard, nodpower1, nodpower2, nodpower3, nodpower4, nodpower5, gun5, gun6, gun7, gun8, nodsilo1, nodsilo2, nodsilo3, nodsilo4, nodobelisk }

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

BaseRefinery = { type = "proc", pos = CPos.New(12, 25) }
BaseNuke1 = { type = "nuke", pos = CPos.New(5, 24) }
BaseNuke2 = { type = "nuke", pos = CPos.New(3, 24) }
BaseNuke3 = { type = "nuke", pos = CPos.New(16, 30) }
BaseNuke4 = { type = "nuke", pos = CPos.New(14, 30) }
BaseNuke5 = { type = "nuke", pos = CPos.New(12, 30) }
InfantryProduction = { type = "hand", pos = CPos.New(15, 24) }
VehicleProduction = { type = "afld", pos = CPos.New(3, 27) }

NodGuards = { Actor168, Actor169, Actor170, Actor171, Actor172, Actor181, Actor177, Actor188, Actor189, Actor190 }

BaseBuildings = { BaseRefinery, BaseNuke1, BaseNuke2, BaseNuke3, BaseNuke4, InfantryProduction, VehicleProduction }

BuildBuilding = function(building, cyard)
	local buildingCost = Actor.Cost(building.type)
	if CyardIsBuilding or Nod.Cash < buildingCost then
		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBuilding(building, cyard) end)
		return
	end

	CyardIsBuilding = true

	Nod.Cash = Nod.Cash - buildingCost
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		CyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= Nod then
			Nod.Cash = Nod.Cash + buildingCost
			return
		end

		local actor = Actor.Create(building.type, true, { Owner = Nod, Location = building.pos })

		if actor.Type == 'hand' or actor.Type == 'pyle' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(actor) end)
		elseif actor.Type == 'afld' or actor.Type == 'weap' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(actor) end)
		end

		Trigger.OnKilled(actor, function()
			BuildBuilding(building, cyard)
		end)

		RepairBuilding(Nod, actor, 0.75)
	end)
end

HasHarvester = function()
	local harv = Nod.GetActorsByType("harv")
	return #harv > 0
end

GuardBase = function()
	Utils.Do(NodBase, function(building)
		Trigger.OnDamaged(building, function()
			if not building.IsDead then
				Utils.Do(NodGuards, function(guard)
					if not guard.IsDead then
						guard.Stop()
						guard.Guard(building)
					end
				end)
			end
		end)
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
	elseif not HasHarvester() then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(building) end)
		return
	end

	if #PatrolProductionQueue >= 1 then
		local inQueue = PatrolProductionQueue[1]
		local toBuild = { inQueue.unit[1] }
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
			MoveAndHunt(InfantryAttackGroup, Path)
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
	elseif not HasHarvester() then
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
			MoveAndHunt(VehicleAttackGroup, Path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(VehicleProductionCooldown, function() ProduceVehicle(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceVehicle(building) end)
		end
	end)
end

StartAI = function()
	RepairNamedActors(Nod, 0.75)

	Nod.Cash = StartingCash
	GuardBase()
end

Trigger.OnAllKilledOrCaptured(NodBase, function()
	Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
end)

Trigger.OnKilled(nodrefinery, function(building)
	BuildBuilding(BaseRefinery, NodCYard)
end)

Trigger.OnKilled(nodpower1, function(building)
	BuildBuilding(BaseNuke1, NodCYard)
end)

Trigger.OnKilled(nodpower2, function(building)
	BuildBuilding(BaseNuke2, NodCYard)
end)

Trigger.OnKilled(nodpower3, function(building)
	BuildBuilding(BaseNuke3, NodCYard)
end)

Trigger.OnKilled(nodpower4, function(building)
	BuildBuilding(BaseNuke4, NodCYard)
end)

Trigger.OnKilled(nodpower5, function(building)
	BuildBuilding(BaseNuke5, NodCYard)
end)

Trigger.OnKilled(handofnod, function(building)
	BuildBuilding(InfantryProduction, NodCYard)
end)

Trigger.OnKilled(nodairfield, function(building)
	BuildBuilding(VehicleProduction, NodCYard)
end)
