IdlingUnits = { }

AttackGroupSize =
{
	Easy = 6,
	Normal = 8,
	Hard = 10
}
AttackDelays =
{
	Easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	Normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	Hard = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

OrdosInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad" }

AttackOnGoing = false
HoldProduction = false
HarvesterKilled = true

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SetupAttackGroup = function()
	local units = { }

	for i = 0, AttackGroupSize[Map.Difficulty], 1 do
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
	IdlingUnits = Reinforcements.Reinforce(ordos, InitialOrdosReinforcements, OrdosPaths[2])
	IdlingUnits[#IdlingUnits + 1] = OTrooper1
	IdlingUnits[#IdlingUnits + 1] = OTrooper2
	IdlingUnits[#IdlingUnits + 1] = ORaider

	Utils.Do(OrdosBase, function(actor)
		DefendActor(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

ProduceInfantry = function()
	if OBarracks.IsDead then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Map.Difficulty][1], AttackDelays[Map.Difficulty][2] + 1)
	local toBuild = { Utils.Random(OrdosInfantryTypes) }
	ordos.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize[Map.Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if OLightFactory.IsDead then
		return
	end

	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceVehicles)
		return
	end

	local delay = Utils.RandomInteger(AttackDelays[Map.Difficulty][1], AttackDelays[Map.Difficulty][2] + 1)
	local toBuild = { Utils.Random(OrdosVehicleTypes) }
	ordos.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceVehicles)

		if #IdlingUnits >= (AttackGroupSize[Map.Difficulty] * 2.5) then
			SendAttack()
		end
	end)
end

ActivateAI = function()
	Trigger.AfterDelay(0, InitAIUnits)

	OConyard.Produce(AtreidesUpgrades[1])
	OConyard.Produce(AtreidesUpgrades[2])

	-- Finish the upgrades first before trying to build something
	Trigger.AfterDelay(DateTime.Seconds(14), function()
		ProduceInfantry()
		ProduceVehicles()
	end)
end
