--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackPaths = { WaypointGroup3, WaypointGroup4, WaypointGroup5 }
NodBase = { handofnod, nodairfield, nodrefinery, NodYard, nodpower1, nodpower2, nodpower3, nodpower4, gun1, gun2, nodsilo1, nodsilo2, nodsilo3, nodsilo4}

PatrolProductionQueue = { }

InfantryAttackGroup = { }
InfantryGroupSize = 5
InfantryProductionCooldown = DateTime.Minutes(3)
InfantryProductionTypes = { "e1", "e1", "e1", "e3", "e3", "e4" }
HarvesterProductionType = { "harv" }

VehicleAttackGroup = { }
VehicleGroupSize = 5
VehicleProductionCooldown = DateTime.Minutes(3)
VehicleProductionTypes = { "bggy", "bggy", "bike", "ltnk", "ltnk" }

StartingCash = 14000

BaseRefinery = { type = "proc", pos = CPos.New(24, 16), cost = 1500 }
BaseGun1 = { type = "gun", pos = CPos.New( 21, 19), cost = 600 }
BaseGun2 = { type = "gun", pos = CPos.New( 26, 21), cost = 600 }
BaseNuke1 = { type = "nuke", pos = CPos.New( 23, 14), cost = 500 }
BaseNuke2 = { type = "nuke", pos = CPos.New( 10, 9), cost = 500 }
BaseNuke3 = { type = "nuke", pos = CPos.New( 6, 8), cost = 500 }
BaseNuke4 = { type = "nuke", pos = CPos.New( 8, 8), cost = 500 }
InfantryProduction = { type = "hand", pos = CPos.New(27, 17), cost = 500 }
VehicleProduction = { type = "afld", pos = CPos.New(27, 14), cost = 2000 }

NodGuards = { Actor154, Actor155, Actor218, Actor219 }

BaseBuildings = { BaseRefinery, BaseNuke1, BaseNuke2, BaseNuke3, BaseNuke4, InfantryProduction, VehicleProduction, BaseGun1, BaseGun2 }

BuildBuilding = function(building, cyard)
	if CyardIsBuilding or Nod.Cash < building.cost then
		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBuilding(building, cyard) end)
		return
	end

	CyardIsBuilding = true

	Nod.Cash = Nod.Cash - building.cost
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		CyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= Nod then
			Nod.Cash = Nod.Cash + building.cost
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

		RepairBuilding(GDI, actor, 0.75)
	end)
end

CheckForHarvester = function()
	local harv = Nod.GetActorsByType("harv")
	return #harv > 0
end

GuardBase = function()
	Utils.Do(NodBase, function(building)
		Trigger.OnDamaged(building, function()
			Utils.Do(NodGuards, function(guard)
				if not guard.IsDead and not building.IsDead then
					guard.Stop()
					guard.Guard(building)
				end
			end)
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
			MoveAndHunt(VehicleAttackGroup, Path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(VehicleProductionCooldown, function() ProduceVehicle(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceVehicle(building) end)
		end
	end)
end

StartAI = function()
	Nod.Cash = StartingCash
	GuardBase()
end

Trigger.OnAllKilledOrCaptured(NodBase, function()
	Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
end)

Trigger.OnKilled(nodrefinery, function()
	BuildBuilding(BaseRefinery, NodYard)
end)

Trigger.OnKilled(nodpower1, function()
	BuildBuilding(BaseNuke1, NodYard)
end)

Trigger.OnKilled(nodpower2, function()
	BuildBuilding(BaseNuke2, NodYard)
end)

Trigger.OnKilled(nodpower3, function()
	BuildBuilding(BaseNuke3, NodYard)
end)

Trigger.OnKilled(nodpower4, function()
	BuildBuilding(BaseNuke4, NodYard)
end)

Trigger.OnKilled(gun1, function()
	BuildBuilding(BaseGun1, NodYard)
end)

Trigger.OnKilled(gun2, function()
	BuildBuilding(BaseGun2, NodYard)
end)

Trigger.OnKilled(handofnod, function()
	BuildBuilding(InfantryProduction, NodYard)
end)

Trigger.OnKilled(nodairfield, function()
	BuildBuilding(VehicleProduction, NodYard)
end)
