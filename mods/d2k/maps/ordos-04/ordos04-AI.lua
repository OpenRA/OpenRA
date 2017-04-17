IdlingUnits =
{
	Harkonnen = { },
	Smugglers = { }
}

AttackGroupSize =
{
	easy = 6,
	normal = 8,
	hard = 10
}

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(7) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(5) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(3) }
}

EnemyInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }

HarkonnenVehicleTypes = { "trike", "trike", "quad" }
HarkonnenTankType = { "combat_tank_h" }

SmugglerVehicleTypes = { "raider", "raider", "quad" }
SmugglerTankType = { "combat_tank_o" }

IsAttacking = 
{
	Harkonnen = false,
	Smugglers = false
}

AttackOnGoing = 
{
	Harkonnen = false,
	Smugglers = false
}

HoldProduction =
{
	Harkonnen = false,
	Smugglers = false
}

HarvesterKilled =
{
	Harkonnen = true,
	Smugglers = true
}

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SetupAttackGroup = function(house)
	local units = { }

	for i = 0, AttackGroupSize[Difficulty], 1 do
		if #IdlingUnits[house.Name] == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits[house.Name])

		if IdlingUnits[house.Name][number] and not IdlingUnits[house.Name][number].IsDead then
			units[i] = IdlingUnits[house.Name][number]
			table.remove(IdlingUnits[house.Name], number)
		end
	end

	return units
end

SendAttack = function(house)
	if IsAttacking[house.Name] then
		return
	end
	IsAttacking[house.Name] = true
	HoldProduction[house.Name] = true

	local units = SetupAttackGroup(house)
	Utils.Do(units, function(unit)
		IdleHunt(unit)
	end)

	Trigger.OnAllRemovedFromWorld(units, function()
		IsAttacking[house.Name] = false
		HoldProduction[house.Name] = false
	end)
end

ProtectHarvester = function(unit, house)
	DefendActor(unit, house)
	Trigger.OnKilled(unit, function() HarvesterKilled[house.Name] = true end)
end

DefendActor = function(unit, house)
	Trigger.OnDamaged(unit, function(self, attacker)
		if AttackOnGoing[house.Name] then
			return
		end
		AttackOnGoing[house.Name] = true

		-- Don't try to attack spiceblooms
		if attacker and attacker.Type == "spicebloom" then
			return
		end

		local Guards = SetupAttackGroup(house)

		if #Guards <= 0 then
			AttackOnGoing[house.Name] = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() AttackOnGoing[house.Name] = false end)
	end)
end

InitAIUnits = function(house)
	IdlingUnits[house.Name] = Reinforcements.Reinforce(house, InitialReinforcements[house.Name], InitialReinforcementsPaths[house.Name])

	Utils.Do(Base[house.Name], function(actor)
		DefendActor(actor, house)
		Trigger.OnDamaged(actor, function(building)
			if building.Health < building.MaxHealth * 3/4 and building.Owner.Name == house.Name then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

Produce = function(house, units, factory)
	if factory.IsDead then
		return
	end

	if HoldProduction[house.Name] then
		Trigger.AfterDelay(DateTime.Seconds(30), function() Produce(house, units, factory) end)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	local toBuild = { Utils.Random(units) }
	house.Build(toBuild, function(unit)
		local unitCount = 1
		if IdlingUnits[house.Name] then
			unitCount = 1 + #IdlingUnits[house.Name]
		end
		IdlingUnits[house.Name][unitCount] = unit[1]
		Trigger.AfterDelay(delay, function() Produce(house, units, factory) end)

		if unitCount >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack(house)
		end
	end)
end

ActivateAI = function()
	InitAIUnits(harkonnen)
	InitAIUnits(smuggler)

	-- Finish the upgrades first before trying to build something
	Trigger.AfterDelay(DateTime.Seconds(14), function()
		Produce(harkonnen, EnemyInfantryTypes, HBarracks)
		Produce(harkonnen, HarkonnenVehicleTypes, HLightFactory)
		Produce(harkonnen, HarkonnenTankType, HHeavyFactory)
	
		Produce(smuggler, EnemyInfantryTypes, SBarracks)
		Produce(smuggler, SmugglerVehicleTypes, SLightFactory)
		Produce(smuggler, SmugglerTankType, SHeavyFactory)
	end)
end
