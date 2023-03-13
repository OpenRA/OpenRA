--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

IdlingUnits = { }
Yaks = { }

AttackGroupSizes =
{
	easy = 6,
	normal = 8,
	hard = 10,
	tough = 12
}

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(5) },
	tough = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

AttackRallyPoints =
{
	{ SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally13.Location, PlayerBase.Location },
	{ SovietRally7.Location, SovietRally10.Location, PlayerBase.Location },
	{ SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally12.Location, PlayerBase.Location },
	{ SovietRally7.Location, ParadropPoint1.Location, PlayerBase.Location },
	{ SovietRally8.Location, SovietRally9.Location, ParadropPoint1.Location, PlayerBase.Location, }
}

SovietInfantryTypes = { "e1", "e1", "e2" }
SovietVehicleTypes = { "3tnk", "3tnk", "3tnk", "ftrk", "ftrk", "apc" }
SovietAircraftType = { "yak" }

AttackOnGoing = false
HoldProduction = false
HarvesterKilled = false

Powr1 = { name = "powr", pos = CPos.New(47, 21), prize = 500, exists = true }
Barr = { name = "barr", pos = CPos.New(53, 26), prize = 400, exists = true }
Proc = { name = "proc", pos = CPos.New(54, 21), prize = 1400, exists = true }
Weap = { name = "weap", pos = CPos.New(48, 28), prize = 2000, exists = true }
Powr2 = { name = "powr", pos = CPos.New(51, 21), prize = 500, exists = true }
Powr3 = { name = "powr", pos = CPos.New(46, 25), prize = 500, exists = true }
Powr4 = { name = "powr", pos = CPos.New(49, 21), prize = 500, exists = true }
Ftur1 = { name = "powr", pos = CPos.New(56, 27), prize = 600, exists = true }
Ftur2 = { name = "powr", pos = CPos.New(51, 32), prize = 600, exists = true }
Ftur3 = { name = "powr", pos = CPos.New(54, 30), prize = 600, exists = true }
Afld1 = { name = "afld", pos = CPos.New(43, 23), prize = 500, exists = true }
Afld2 = { name = "afld", pos = CPos.New(43, 21), prize = 500, exists = true }
BaseBuildings = { Powr1, Barr, Proc, Weap, Powr2, Powr3, Powr4, Ftur1, Ftur2, Ftur3, Afld1, Afld2 }
InitialBase = { Barracks, Refinery, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, Warfactory, Flametur1, Flametur2, Flametur3, Airfield1, Airfield2 }

BuildBase = function()
	if Conyard.IsDead or Conyard.Owner ~= USSR then
		return
	elseif Harvester.IsDead and USSR.Resources <= 299 then
		return
	end

	for i,v in ipairs(BaseBuildings) do
		if not v.exists then
			BuildBuilding(v)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBuilding = function(building)
	Trigger.AfterDelay(Actor.BuildTime(building.name), function()
		local actor = Actor.Create(building.name, true, { Owner = USSR, Location = building.pos })
		USSR.Cash = USSR.Cash - building.prize

		building.exists = true
		Trigger.OnKilled(actor, function() building.exists = false end)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
				DefendActor(actor)
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
	end)
end

SetupAttackGroup = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		if #IdlingUnits == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	return units
end

SendAttack = function()
	if Attacking then
		return
	end
	Attacking = true
	HoldProduction = true

	local units = SetupAttackGroup()
	local path = Utils.Random(AttackRallyPoints)
	Utils.Do(units, function(unit)
		unit.Patrol(path)
		IdleHunt(unit)
	end)

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking = false
		HoldProduction = false
	end)
end

ProtectHarvester = function(unit)
	DefendActor(unit)
	Trigger.OnKilled(unit, function() HarvesterKilled = true end)
end

DefendActor = function(unit)
	Trigger.OnDamaged(unit, function(self, attacker)
		if AttackOnGoing then
			return
		end
		AttackOnGoing = true

		local Guards = SetupAttackGroup()

		if #Guards <= 0 then
			AttackOnGoing = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() AttackOnGoing = false end)
	end)
end

InitAIUnits = function()
	IdlingUnits = USSR.GetGroundAttackers()

	DefendActor(Conyard)
	for i,v in ipairs(InitialBase) do
		DefendActor(v)
		Trigger.OnDamaged(v, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.OnKilled(v, function()
			BaseBuildings[i].exists = false
		end)
	end
end

ProduceInfantry = function()
	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		return
	end

	-- See AttackDelay in WorldLoaded
	local delay = Utils.RandomInteger(AttackDelay[1], AttackDelay[2])
	local toBuild = { Utils.Random(SovietInfantryTypes) }
	USSR.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		-- See AttackGroupSize in WorldLoaded
		if #IdlingUnits >= (AttackGroupSize * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceVehicles)
		return
	end

	-- See AttackDelay in WorldLoaded
	local delay = Utils.RandomInteger(AttackDelay[1], AttackDelay[2])
	if HarvesterKilled then
		USSR.Build({ "harv" }, function(harv)
			ProtectHarvester(harv[1])
			HarvesterKilled = false
			Trigger.AfterDelay(delay, ProduceVehicles)
		end)
	else
		local toBuild = { Utils.Random(SovietVehicleTypes) }
		USSR.Build(toBuild, function(unit)
			IdlingUnits[#IdlingUnits + 1] = unit[1]
			Trigger.AfterDelay(delay, ProduceVehicles)

			-- See AttackGroupSize in WorldLoaded
			if #IdlingUnits >= (AttackGroupSize * 2.5) then
				SendAttack()
			end
		end)
	end
end

ProduceAircraft = function()
	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)
		if #Yaks == 1 then
			Trigger.AfterDelay(DateTime.Minutes(1), ProduceAircraft)
		end

		InitializeAttackAircraft(yak, Greece)
	end)
end

ActivateAI = function()
	InitAIUnits()
	ProtectHarvester(Harvester)

	AttackDelay = AttackDelays[Difficulty]
	AttackGroupSize = AttackGroupSizes[Difficulty]
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		ProduceInfantry()
		ProduceVehicles()
		ProduceAircraft()
	end)
end
