
IdlingUnits = { }
AttackGroupSize = 6

Barracks = { Barracks2, Barracks3 }

Rallypoints = { VehicleRallypoint1, VehicleRallypoint2, VehicleRallypoint3, VehicleRallypoint4, VehicleRallypoint5 }

Airfields = { Airfield1, Airfield2 }
Yaks = { }

SovietInfantryTypes = { "e1", "e1", "e2", "e4" }
SovietVehicleTypes = { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "apc" }
SovietAircraftType = { "yak" }

HoldProduction = true
BuildVehicles = true
TrainInfantry = true

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SetupAttackGroup = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	return units
end

SendAttack = function()
	if not Attacking then
		return
	end
	Attacking = false
	HoldProduction = false

	local units = SetupAttackGroup()
	Utils.Do(units, function(unit)
		IdleHunt(unit)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function() Attacking = true end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() HoldProduction = true end)
end

ProtectHarvester = function(unit)
	Trigger.OnDamaged(unit, function(self, attacker)
		-- TODO: Send the Harvester to the service depo

		if AttackOnGoing then
			return
		end
		AttackOnGoing = true

		local Guards = SetupAttackGroup()
		Utils.Do(Guards, function(unit)
			if attacker.Location then
				unit.AttackMove(attacker.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() AttackOnGoing = false end)
	end)

	Trigger.OnKilled(unit, function() HarvesterKilled = true end)
end

InitAIUnits = function()
	IdlingUnits = Map.ActorsInBox(MainBaseTopLeft.CenterPosition, Map.BottomRight, function(self) return self.Owner == ussr and self.HasProperty("Hunt") end)

	local buildings = Map.ActorsInBox(MainBaseTopLeft.CenterPosition, Map.BottomRight, function(self) return self.Owner == ussr and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == ussr and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

InitAIEconomy = function()
	ussr.Cash = 6000

	Harvester.FindResources()
	ProtectHarvester(Harvester)
end

InitProductionBuildings = function()
	if not Warfactory2.IsDead then
		Warfactory2.IsPrimaryBuilding = true
	end
	Trigger.OnKilled(Warfactory2, function() BuildVehicles = false end)

	if not Barracks2.IsDead then
		Barracks2.IsPrimaryBuilding = true

		Trigger.OnKilled(Barracks2, function()
			if not Barracks3.IsDead then
				Barracks3.IsPrimaryBuilding = true
			else
				TrainInfantry = false
			end
		end)
	end

	if not Barracks3.IsDead then
		Trigger.OnKilled(Barracks3, function() if Barracks2.IsDead then TrainInfantry = false end end)
	end

	if Map.Difficulty == "Normal" then

		if not Airfield1.IsDead then
			Trigger.OnKilled(Airfield1, function()
				if Airfield2.IsDead then
					AirAttacks = false
				else
					Airfield2.IsPrimaryBuilding = true
					Trigger.OnKilled(Airfield2, function() AirAttacks = false end)
				end
			end)

			Airfield1.IsPrimaryBuilding = true
			AirAttacks = true

		elseif not Airfield2.IsDead then
			Trigger.OnKilled(Airfield2, function() AirAttacks = false end)

			Airfield2.IsPrimaryBuilding = true
			AirAttacks = true
		end
	end
end

ProduceInfantry = function()
	if not TrainInfantry then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(SovietInfantryTypes) }
	ussr.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if not BuildVehicles then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceVehicles)
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(5), DateTime.Seconds(9))
	if HarvesterKilled then
		HarvesterKilled = false
		ussr.Build({ "harv" }, function(harv)
			harv[1].FindResources()
			ProtectHarvester(harv[1])
			Trigger.AfterDelay(delay, ProduceVehicles)
		end)
		return
	end

	Warfactory2.RallyPoint = Utils.Random(Rallypoints).Location
	local toBuild = { Utils.Random(SovietVehicleTypes) }
	ussr.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceVehicles)

		if #IdlingUnits >= (AttackGroupSize * 2.5) then
			SendAttack()
		end
	end)
end

ProduceAircraft = function()
	if not AirAttacks then
		return
	end

	ussr.Build(SovietAircraftType, function(units)
		Yaks[#Yaks + 1] = units[1]

		if #Yaks == 2 then
			Trigger.OnKilled(units[1], ProduceAircraft)
		else
			Trigger.AfterDelay(DateTime.Minutes(1), ProduceAircraft)
		end

		local target = nil
		Trigger.OnIdle(units[1], function()
			if not target or target.IsDead or (not target.IsInWorld) then

				local enemies = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(self) return self.Owner == greece and self.HasProperty("Health") end)
				if #enemies > 0 then
					target = Utils.Random(enemies)
					units[1].Attack(target)
				end
			else
				units[1].Attack(target)
			end
		end)
	end)
end

ActivateAI = function()

	InitAIUnits()
	InitAIEconomy()
	InitProductionBuildings()

	Trigger.AfterDelay(DateTime.Minutes(5), function()
		ProduceInfantry()
		ProduceVehicles()
		if AirAttacks then
			Trigger.AfterDelay(DateTime.Minutes(3), ProduceAircraft)
		end
	end)
end
