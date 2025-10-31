--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOptionOrDefault("difficulty", "normal")

--- Prepare basic messages for a player's win, loss, or objective updates.
---@param player player
InitObjectives = function(player)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-completed"))
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-failed"))
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

---The Dune stand-in for the usual "Battlefield Control" announcer.
Mentat = UserInterface.GetFluentMessage("mentat")

--- Prepare waves of reinforcements to be deployed from Carryalls.
---@param player player Owner of the reinforcements and Carryalls.
---@param currentWave integer Current wave count. A typical starting value is 0.
---@param totalWaves integer Total number of waves to be reinforced.
---@param delay integer Ticks between each reinforcement.
---@param pathFunction fun():cpos[] Returns a path for each Carryall's entry flight.
---@param unitTypes table<integer, string[]> Collection of unit types that will be reinforced. Each group within this collection is keyed to a different wave number.
---@param haltCondition? fun():boolean Returns true if reinforcements should stop. If this function is absent, assume false.
---@param customHuntFunction? fun(actors: actor[]) Function called by each unit within a group upon creation. This defaults to IdleHunt. Note that reinforced units will not be in the world until unloaded.
---@param announcementFunction? fun(currentWave: integer) Function called when a new Carryall is created.
SendCarryallReinforcements = function(player, currentWave, totalWaves, delay, pathFunction, unitTypes, haltCondition, customHuntFunction, announcementFunction)
	Trigger.AfterDelay(delay, function()
		if haltCondition and haltCondition() then
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

		SendCarryallReinforcements(player, currentWave, totalWaves, delay, pathFunction, unitTypes, haltCondition, customHuntFunction)
	end)
end

--- Create a one-time area trigger that reinforces attackers with a Carryall.
---@param triggeringPlayer player
---@param reinforcingPlayer player
---@param area cpos[]
---@param unitTypes string[]
---@param path cpos[] Returns a path for the Carryall's entry flight.
---@param pauseCondition? fun():boolean Returns true if this trigger's activation should be paused. If this function is absent, assume false.
TriggerCarryallReinforcements = function(triggeringPlayer, reinforcingPlayer, area, unitTypes, path, pauseCondition)
	local fired = false
	Trigger.OnEnteredFootprint(area, function(a, id)
		if pauseCondition and pauseCondition() then
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

--- Destroy all non-reinforcement Carryalls owned by a player.
---@param player player
DestroyCarryalls = function(player)
	Utils.Do(player.GetActorsByType("carryall"), function(actor) actor.Kill() end)
end

--- The following tables are used by the campaign AI,
--- with each value keyed to a different AI player.

--- Collection of a bot's spare units from production and reinforcement.
--- These units are used for periodic attacks or base/harvester defense.
---@type table<player, actor[]>
IdlingUnits = { }

--- Is a bot currently using idle units to attack or defend?
---@type table<player, boolean>
Attacking = { }

--- Is a bot's production routine on hold?
---@type table<player, boolean>
HoldProduction = { }

--- Was a bot's last harvester eaten by a sandworm?
---@type table<player, boolean>
LastHarvesterEaten = { }

--- Gather units from a bot's idle unit pool, up to a certain group size.
---@param owner player
---@param size integer
---@return actor[]
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

--- Order an attack from this bot if one is not already started.
---@param owner player
---@param size integer
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

--- Prepare a unit to call for help if attacked.
---@param unit actor
---@param defendingPlayer player
---@param defenderCount integer
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

--- Prepare a harvester to call for help when attacked.
---@param unit actor
---@param owner player
---@param defenderCount integer
ProtectHarvester = function(unit, owner, defenderCount)
	-- Note that worm attacks will not trigger the OnDamaged event for this.
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

--- Schedule repairs for this building once it takes enough damage.
---@param owner player Owner of the building.
---@param actor actor Building to be repaired.
---@param modifier number The repair threshold. Below this health percentage, repairs are started. 1 is full health, while 0.5 is half.
RepairBuilding = function(owner, actor, modifier)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * modifier then
			building.StartBuildingRepairs()
		end
	end)
end

--- Prepare buildings to call for help and begin repairs if attacked.
---@param owner player Owner of the base.
---@param baseBuildings actor[] Buildings to defend and repair.
---@param modifier number The repair threshold. Below this health percentage, repairs are started. 1 is full health, while 0.5 is half.
---@param defenderCount integer Maximum number of defenders to use per counterattack.
DefendAndRepairBase = function(owner, baseBuildings, modifier, defenderCount)
	Utils.Do(baseBuildings, function(actor)
		if actor.IsDead then
			return
		end

		DefendActor(actor, owner, defenderCount)
		RepairBuilding(owner, actor, modifier)
	end)
end

--- Schedule production and attacks for a factory or other unit producer.
---@param player player Owner of the factory.
---@param factory actor The factory itself.
---@param delay fun():integer Function that returns a delay until production repeats.
---@param toBuild fun():string[] Function that returns a list of unit types to be produced.
---@param attackSize integer Number of units that will form the next attack wave.
---@param attackThresholdSize integer Number of idle units that will trigger an attack wave.
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
