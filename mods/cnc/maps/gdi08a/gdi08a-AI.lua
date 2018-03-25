

--------Adapted from the global script for d2k, can/should be moved to a global campaign script at some point 
IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

IdlingUnits = {}
Attacking = {}
HoldProduction = {}

SetupAttackGroup = function(owner, size)
	local units = {}

	for i = 0, size, 1 do
		if #IdlingUnits[owner] == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits[owner] + 1)

		if IdlingUnits[owner][number] and not IdlingUnits[owner][number].IsDead then
			units[i] = IdlingUnits[owner][number]
			table.remove(IdlingUnits[owner], number)
		end
	end

	return units
end

SendAttack = function(owner, size)
	if Attacking[owner] then
		return
	end
	Attacking[owner] = true
	HoldProduction[owner] = true

	local units = SetupAttackGroup(owner, size)
	Utils.Do(units, IdleHunt)

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking[owner] = false
		HoldProduction[owner] = false
	end)
end

DefendActor = function(unit, defendingPlayer, defenderCount)
	Trigger.OnDamaged(unit, function(self, attacker)

		if Attacking[defendingPlayer] then
			return
		end
		Attacking[defendingPlayer] = true

		local Guards = SetupAttackGroup(defendingPlayer, defenderCount)

		if #Guards <= 0 then
			Attacking[defendingPlayer] = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() Attacking[defendingPlayer] = false end)
	end)
end


RepairBuilding = function(owner, actor, modifier)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * modifier then
			building.StartBuildingRepairs()
		end
	end)
end

DefendAndRepairBase = function(owner, baseBuildings, modifier, defenderCount)
	Utils.Do(baseBuildings, function(actor)
		if actor.IsDead then
			return
		end

		DefendActor(actor, owner, defenderCount)
		RepairBuilding(owner, actor, modifier)
	end)
end

ProduceUnits = function(player, factory, delay, toBuild, attackSize, attackThresholdSize)
	if factory.IsDead or factory.Owner ~= player then
		return
	end

	if HoldProduction[player] then
		Trigger.AfterDelay(DateTime.Minutes(1), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
		return
	end

	factory.Build(toBuild(), function(unit)
		IdlingUnits[player][#IdlingUnits[player] + 1] = unit[1]
		Trigger.AfterDelay(delay(), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)

		if #IdlingUnits[player] >= attackThresholdSize then
			SendAttack(player, attackSize)
		end
	end)
end
-------------


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

InitialProductionDelay =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(15),
	hard = 0
}

NodInfantryTypes = { "e1", "e1", "e1","e3", "e3", "e4", "e4"}
NodVehicleTypes = { "bggy", "bggy", "bike", "ltnk"}


ActivateAI = function()
	IdlingUnits[Nod] = {}
	DefendAndRepairBase(Nod, NodBase, 1, AttackGroupSize[Difficulty])
	DefendAndRepairBase(Nod, Samsites, 1, AttackGroupSize[Difficulty])

	local delay = function() return Utils.RandomInteger(AttackDelays[Difficulty][1], AttackDelays[Difficulty][2] + 1) end
	local infantryToBuild = function() return { Utils.Random(NodInfantryTypes) } end
	local vehiclesToBuild = function() return { Utils.Random(NodVehicleTypes) } end
	local attackThresholdSize = AttackGroupSize[Difficulty] * 2.5

	Trigger.AfterDelay(InitialProductionDelay[Difficulty], function()
		ProduceUnits(Nod, NodAirfield, delay, vehiclesToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
		ProduceUnits(Nod, NodHand, delay, infantryToBuild, AttackGroupSize[Difficulty], attackThresholdSize)
	end)
end