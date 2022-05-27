--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

StartingCash = 15000

InfantryAttackPaths = { WaypointGroupBaseFrontal, WaypointGroupRightFlankInf, WaypointGroupVillageRight }
VehicleAttackPaths = { WaypointGroupBaseFrontal, WaypointGroupRightFlank, WaypointGroupVillageRight }
ArtyAttackPaths = { WaypointGroupBaseFrontal, ArtyWaypoints1, ArtyWaypoints2 }

InfantryProductionTypes = { "e1", "e3", "e3", "e3", "e4", "e4", "e4" }
VehicleProductionTypes = { "bggy", "ltnk" }
ArtyProductionType = { "arty" }
VehicleAutoProductionTypes = { "bggy", "bggy", "ltnk", "ltnk", "arty" }
HarvesterProductionType = { "harv" }

InfantryGroupSize = { hard = 8, normal = 6, easy = 4 }
InfantryAttackGroup = { }
InfantryProductionCooldown = { hard = DateTime.Seconds(40), normal = DateTime.Seconds(60), easy = DateTime.Seconds(80) }

VehicleGroupSize = { hard = 4, normal = 3, easy = 2 }
VehicleAttackGroup = { }
VehicleProductionCooldown = { hard = DateTime.Seconds(60), normal = DateTime.Seconds(90), easy = DateTime.Seconds(120) }

ArtyGroupSize = { hard = 2, normal = 1, easy = 1 }
ArtyAttackGroup = { }
ArtyProductionCooldown = { hard = DateTime.Seconds(60), normal = DateTime.Seconds(75), easy = DateTime.Seconds(100) }

NodBase = { handofnod, nodairfield, nodrefinery, nodradar, NodYard, nodpower1, nodpower2, nodpower3, nodpower4, nodpower5, gun1, gun2, gun3, gun4, nodsilo1, nodsilo2, nodsilo3}

BaseRadar = { type = "hq", pos = CPos.New(5, 6), cost = 1000 }
BaseRefinery = { type = "proc", pos = CPos.New(8, 7), cost = 1500 }
BaseSilo1 = { type = "silo", pos = CPos.New(6, 5), cost = 100 }
BaseSilo2 = { type = "silo", pos = CPos.New(8, 5), cost = 100 }
BaseSilo3 = { type = "silo", pos = CPos.New(10, 5), cost = 100 }
BaseGun1 = { type = "gun", pos = CPos.New( 7, 18), cost = 600 }
BaseGun2 = { type = "gun", pos = CPos.New( 11, 18), cost = 600 }
BaseGun3 = { type = "gun", pos = CPos.New( 45, 16), cost = 600 }
BaseGun4 = { type = "gun", pos = CPos.New( 50, 16), cost = 600 }
BaseNuke1 = { type = "nuke", pos = CPos.New( 13, 13), cost = 500 }
BaseNuke2 = { type = "nuke", pos = CPos.New( 50, 5), cost = 500 }
BaseNuke3 = { type = "nuke", pos = CPos.New( 5, 9), cost = 500 }
BaseNuke4 = { type = "nuke", pos = CPos.New( 52, 5), cost = 500 }
BaseNuke5 = { type = "nuke", pos = CPos.New( 48, 5), cost = 500 }
InfantryProduction = { type = "hand", pos = CPos.New(5, 12), cost = 500 }
VehicleProduction = { type = "afld", pos = CPos.New(52, 8), cost = 2000 }

BaseBuildings = { BaseRadar, BaseRefinery, BaseNuke1, BaseNuke2, BaseNuke3, BaseNuke4, BaseNuke5, InfantryProduction, VehicleProduction, BaseGun1, BaseGun2, BaseGun3, BaseGun4, BaseSilo1, BaseSilo2, BaseSilo3 }

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

ProduceHarvester = function(building)
	if building.IsDead or building.Owner ~= Nod then
		return
	end
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
	local Path = Utils.Random(InfantryAttackPaths)
	building.Build(toBuild, function(unit)
		InfantryAttackGroup[#InfantryAttackGroup + 1] = unit[1]

		if #InfantryAttackGroup >= InfantryGroupSize[Difficulty] then
			MoveAndHunt(InfantryAttackGroup, Path)
			InfantryAttackGroup = { }
			Trigger.AfterDelay(InfantryProductionCooldown[Difficulty], function() ProduceInfantry(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceAutocreateInfantry(building) end)
		end
	end)
end

-- We want every 2nd produced infantry to be free for the Auto attack teams
ProduceAutocreateInfantry = function(building)
	if building.IsDead or building.Owner ~= Nod then
		return
	elseif not CheckForHarvester() then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceInfantry(building) end)
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(InfantryProductionTypes) }
	building.Build(toBuild, function(unit)
		Trigger.AfterDelay(delay, function() ProduceInfantry(building) end)
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
	local Path = Utils.Random(VehicleAttackPaths)
	building.Build(toBuild, function(unit)
		VehicleAttackGroup[#VehicleAttackGroup + 1] = unit[1]

		if #VehicleAttackGroup >= VehicleGroupSize[Difficulty] then
			
			MoveAndHunt(VehicleAttackGroup, Path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(VehicleProductionCooldown[Difficulty], function() ProduceArty(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceAutocreateVehicle(building) end)
		end
	end)
end

-- We want every 2nd produced vehicle to be free for the Auto attack teams
ProduceAutocreateVehicle = function(building)
	if building.IsDead or building.Owner ~= Nod then
		return
	elseif not CheckForHarvester() then
		ProduceHarvester(building)
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceVehicle(building) end)
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(VehicleProductionTypes) }
	building.Build(toBuild, function(unit)
		Trigger.AfterDelay(delay, function() ProduceVehicle(building) end)
	end)
end

ProduceArty = function(building)
	if building.IsDead or building.Owner ~= Nod then
		return
	elseif not CheckForHarvester() then
		ProduceHarvester(building)
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceArty(building) end)
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(ArtyProductionType) }
	local Path = Utils.Random(ArtyAttackPaths)
	building.Build(toBuild, function(unit)
		ArtyAttackGroup[#ArtyAttackGroup + 1] = unit[1]

		if #ArtyAttackGroup >= ArtyGroupSize[Difficulty] then
			MoveAndHunt(ArtyAttackGroup, Path)
			ArtyAttackGroup = { }
			Trigger.AfterDelay(ArtyProductionCooldown[Difficulty], function() ProduceVehicle(building) end)
		else
			Trigger.AfterDelay(delay, function() ProduceArty(building) end)
		end
	end)
end

StartAI = function()
	Nod.Cash = StartingCash
end

Trigger.OnAllKilledOrCaptured(NodBase, function()
	Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
end)

Trigger.OnKilled(nodrefinery, function()
	BuildBuilding(BaseRefinery, NodYard)
end)

Trigger.OnKilled(nodradar, function()
	BuildBuilding(BaseRadar, NodYard)
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

Trigger.OnKilled(nodpower5, function()
	BuildBuilding(BaseNuke5, NodYard)
end)

Trigger.OnKilled(gun1, function()
	BuildBuilding(BaseGun1, NodYard)
end)

Trigger.OnKilled(gun2, function()
	BuildBuilding(BaseGun2, NodYard)
end)

Trigger.OnKilled(gun3, function()
	BuildBuilding(BaseGun3, NodYard)
end)

Trigger.OnKilled(gun4, function()
	BuildBuilding(BaseGun4, NodYard)
end)

Trigger.OnKilled(nodsilo1, function()
	BuildBuilding(BaseSilo1, NodYard)
end)

Trigger.OnKilled(nodsilo2, function()
	BuildBuilding(BaseSilo2, NodYard)
end)

Trigger.OnKilled(nodsilo3, function()
	BuildBuilding(BaseSilo3, NodYard)
end)

Trigger.OnKilled(handofnod, function()
	BuildBuilding(InfantryProduction, NodYard)
end)

Trigger.OnKilled(nodairfield, function()
	BuildBuilding(VehicleProduction, NodYard)
end)
