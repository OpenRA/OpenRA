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

HarkonnenInfantryTypes = { "light_inf", "light_inf", "trooper", "trooper", "trooper" }
HarkonnenVehicleTypes = { "trike", "trike", "trike", "quad", "quad" }
HarkonnenTankType = { "combat_tank_h" }

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
	Utils.Do(units, function(unit)
		unit.AttackMove(HarkonnenAttackLocation)
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

RepairBuilding = function(owner, actor)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * 3/4 then
			building.StartBuildingRepairs()
		end
	end)
end

InitAIUnits = function()
	IdlingUnits = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", InitialHarkonnenReinforcements, HarkonnenPaths[1], { HarkonnenPaths[1][1] })[2]

	Utils.Do(HarkonnenBase, function(actor)
		DefendActor(actor)
		RepairBuilding(harkonnen, actor)
	end)

	DefendActor(HarkonnenBarracks)
	RepairBuilding(harkonnen, HarkonnenBarracks)

	Utils.Do(SmugglerBase, function(actor)
		RepairBuilding(smuggler, actor)
	end)
	RepairBuilding(smuggler, Starport)
end

ProduceInfantry = function()
	if StopInfantryProduction or HarkonnenBarracks.IsDead or HarkonnenBarracks.Owner ~= harkonnen then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceInfantry)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	local toBuild = { Utils.Random(HarkonnenInfantryTypes) }
	harkonnen.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if HarkonnenLightFactory.IsDead or HarkonnenLightFactory.Owner ~= harkonnen  then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceVehicles)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	local toBuild = { Utils.Random(HarkonnenVehicleTypes) }
	harkonnen.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceVehicles)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ProduceTanks = function()
	if HarkonnenHeavyFactory.IsDead or HarkonnenHeavyFactory.Owner ~= harkonnen then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Seconds(30), ProduceTanks)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1)
	harkonnen.Build(HarkonnenTankType, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceTanks)

		if #IdlingUnits >= (AttackGroupSize[Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ActivateAI = function()
	harkonnen.Cash = 15000
	InitAIUnits()

	ProduceInfantry()
	ProduceVehicles()
	ProduceTanks()
end
