--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOption("difficulty")

InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end

SendCarryallReinforcements = function(player, currentWave, totalWaves, delay, pathFunction, unitTypes, customCondition, customHuntFunction, announcementFunction)
	Trigger.AfterDelay(delay, function()
		if customCondition and customCondition() then
			return
		end

		currentWave = currentWave + 1
		if currentWave > totalWaves then
			return
		end

		if announcementFunction then
			announcementFunction(currentWave)
		end

		local path = pathFunction()
		local units = Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", unitTypes[currentWave], path, { path[1] })[2]

		if not customHuntFunction then
			customHuntFunction = IdleHunt
		end
		Utils.Do(units, customHuntFunction)

		SendCarryallReinforcements(player, currentWave, totalWaves, delay, pathFunction, unitTypes, customCondition, customHuntFunction)
	end)
end

TriggerCarryallReinforcements = function(triggeringPlayer, reinforcingPlayer, area, unitTypes, path, customCondition)
	local fired = false
	Trigger.OnEnteredFootprint(area, function(a, id)
		if customCondition and customCondition() then
			return
		end

		if not fired and a.Owner == triggeringPlayer and a.Type ~= "carryall" then
			fired = true
			Trigger.RemoveFootprintTrigger(id)
			local units = Reinforcements.ReinforceWithTransport(reinforcingPlayer, "carryall.reinforce", unitTypes, path, { path[1] })[2]
			Utils.Do(units, IdleHunt)
		end
	end)
end

DestroyCarryalls = function(player)
	Utils.Do(player.GetActorsByType("carryall"), function(actor) actor.Kill() end)
end

-- Used for the AI:

IdlingUnits = { }
Attacking = { }
HoldProduction = { }
LastHarvesterEaten = { }

SetupAttackGroup = function(owner, size)
	local units = { }

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
		if unit.Owner ~= defendingPlayer then
			return
		end

		-- Don't try to attack spiceblooms
		if attacker and attacker.Type == "spicebloom" then
			return
		end

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

ProtectHarvester = function(unit, owner, defenderCount)
	DefendActor(unit, owner, defenderCount)

	-- Worms don't kill the actor, but dispose it instead.
	-- If a worm kills the last harvester (hence we check for remaining ones),
	-- a new harvester is delivered by the harvester insurance.
	-- Otherwise, there's no need to check for new harvesters.
	local killed = false
	Trigger.OnKilled(unit, function()
		killed = true
	end)
	Trigger.OnRemovedFromWorld(unit, function()
		if not killed and #unit.Owner.GetActorsByType("harvester") == 0 then
			LastHarvesterEaten[owner] = true
		end
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
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
		return
	end

	player.Build(toBuild(), function(unit)
		IdlingUnits[player][#IdlingUnits[player] + 1] = unit[1]
		Trigger.AfterDelay(delay(), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)

		if #IdlingUnits[player] >= attackThresholdSize then
			SendAttack(player, attackSize)
		end
	end)
end
