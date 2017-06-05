IdlingUnits = { }

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

AtreidesInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
AtreidesVehicleTypes = { "trike", "trike", "quad" }
AtreidesTankType = { "combat_tank_a" }

HarvesterKilled = true

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SetupAttackGroup = function()
	local units = { }

	for i = 0, AttackGroupSize[Difficulty] do
		if #IdlingUnits == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits + 1)

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
	Utils.Do(units, IdleHunt)

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
		if Defending then
			return
		end
		Defending = true

		-- Don't try to attack spiceblooms
		if attacker and attacker.Type == "spicebloom" then
			return
		end

		local Guards = SetupAttackGroup()

		if #Guards <= 0 then
			Defending = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() Defending = false end)
	end)
end

InitAIUnits = function()
	IdlingUnits = Reinforcements.Reinforce(atreides, InitialAtreidesReinforcements[1], AtreidesPaths[2]), Reinforcements.Reinforce(atreides, InitialAtreidesReinforcements[2], AtreidesPaths[3])
end

RepairBase = function(house)
	Utils.Do(Base[house.Name], function(actor)
		DefendActor(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == house and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

ProduceInfantry = function()
	if ABarracks.IsDead then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceInfantry)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	local toBuild = { Utils.Random(AtreidesInfantryTypes) }
	atreides.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if ALightFactory.IsDead then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceVehicles)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	local toBuild = { Utils.Random(AtreidesVehicleTypes) }
	atreides.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceVehicles)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ProduceTanks = function()
	if AHeavyFactory.IsDead then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceTanks)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	atreides.Build(AtreidesTankType, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceTanks)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ActivateAI = function()
	InitAIUnits()
	FremenProduction()
	
	RepairBase(atreides)
	RepairBase(fremen)

	ProduceInfantry()
	ProduceVehicles()
	ProduceTanks()
end
