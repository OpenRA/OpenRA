--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackPaths = { { AttackPath1 }, { AttackPath2 } }
GDIBase = { GDICYard, GDIPyle, GDIWeap, GDIHQ, GDIProc, GDINuke1, GDINuke2, GDINuke3, GDIBuilding1, GDIBuilding2, GDIBuilding3, GDIBuilding4, GDIBuilding5, GDIBuilding6, GDIBuilding7, GDIBuilding8 }
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

BaseProc = { type = "proc", pos = CPos.New(22, 51), cost = 1500 }
BaseNuke1 = { type = "nuke", pos = CPos.New(16, 56), cost = 500 }
BaseNuke2 = { type = "nuke", pos = CPos.New(18, 57), cost = 500 }
BaseNuke3 = { type = "nuke", pos = CPos.New(27, 51), cost = 500 }
InfantryProduction = { type = "pyle", pos = CPos.New(18, 54), cost = 500 }
VehicleProduction = { type = "weap", pos = CPos.New(27, 55), cost = 2000 }

BaseBuildings = { BaseProc, BaseNuke1, BaseNuke2, BaseNuke3, InfantryProduction, VehicleProduction }

BuildBuilding = function(building, cyard)
	if CyardIsBuilding or GDI.Cash < building.cost then
		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBuilding(building, cyard) end)
		return
	end

	CyardIsBuilding = true

	GDI.Cash = GDI.Cash - building.cost
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		CyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= GDI then
			GDI.Cash = GDI.Cash + building.cost
			return
		end

		local actor = Actor.Create(building.type, true, { Owner = GDI, Location = building.pos })

		if actor.Type == 'pyle' or actor.Type == 'hand' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(actor) end)
		elseif actor.Type == 'weap' or actor.Type == 'afld' then
			Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(actor) end)
		end

		Trigger.OnKilled(actor, function()
			BuildBuilding(building, cyard)
		end)

		RepairBuilding(GDI, actor, 0.75)
	end)
end

CheckForHarvester = function()
	local harv = GDI.GetActorsByType("harv")
	return #harv > 0
end

ProduceHarvester = function(building)
	if not BuildingHarvester then
		BuildingHarvester = true
		building.Build(HarvesterProductionType, function()
			BuildingHarvester = false
		end)
	end
end

ProduceInfantry = function(building)
	if building.IsDead or building.Owner ~= GDI then
		return
	elseif not CheckForHarvester() then
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
	if building.IsDead or building.Owner ~= GDI then
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
			MoveAndHunt(VehicleAttackGroup, Path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(VehicleProductionCooldown, function() ProduceVehicle(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceVehicle(building) end)
		end
	end)
end

StartAI = function()
	RepairNamedActors(GDI, 0.75)

	GDI.Cash = StartingCash

	Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(GDIPyle) end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)
end

Trigger.OnKilled(GDIProc, function()
	BuildBuilding(BaseProc, GDICYard)
end)

Trigger.OnKilled(GDINuke1, function()
	BuildBuilding(BaseNuke1, GDICYard)
end)

Trigger.OnKilled(GDINuke2, function()
	BuildBuilding(BaseNuke2, GDICYard)
end)

Trigger.OnKilled(GDINuke3, function()
	BuildBuilding(BaseNuke3, GDICYard)
end)

Trigger.OnKilled(GDIPyle, function()
	BuildBuilding(InfantryProduction, GDICYard)
end)

Trigger.OnKilled(GDIWeap, function()
	BuildBuilding(VehicleProduction, GDICYard)
end)
