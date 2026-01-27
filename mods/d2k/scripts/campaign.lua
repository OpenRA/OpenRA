--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOptionOrDefault("difficulty", "normal")

DifficultyModifier =
{
	easy = 1,
	normal = 0.8,
	hard = 0.7
}

CrushChance =
{
	easy = 20,
	normal = 40,
	hard = 60
}

CrusherTypes = { "combat_tank_a", "combat_tank_a.starport", "combat_tank_h", "combat_tank_h.starport, combat_tank_o, combat_tank_o.starport"}

ActivateCrushLogic = function ()
	Trigger.OnAnyProduction( function(producer, produced)
		if not producer.Owner.IsBot then return end
		if Utils.Any(CrusherTypes, function(crusherType)
				return crusherType == produced.Type
			end)
		then
			AICrushLogic(produced, producer.Owner)
		end
	end)
end

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

--- Is a bot currently using idle units to attack?
---@type table<player, boolean>
Attacking = { }

--- return true if actor is already defended
---@type table<player, boolean[]>
Defending = { }

--- Collection of cells around player base
--- @type table<player, cpos[]>
DefencePerimeter = {}

--- Collection of patrol point where player should patrol
---@type table<player, cpos[]>
PatrolPoints = {}

--- Time in ticks for next attack. AI cant attack until AttackDelay < GameTime
--- Serve also as initial attack delay.
--- @type table<player, integer>
AttackDelay = {}

--- Minimum delays between attacks
--- @type table<player, integer>
TimeBetweenAttacks = {}

--- How much harvesters AI should maintain
--- @type table<player, integer>
HarvesterCount = {}

--- List of all unit combat types.
AllAttackUnitTypes = { "light_inf", "trooper", "trike", "quad", "siege_tank", "missile_tank", "sonic_tank", "devastator", "raider", "stealth_raider", "deviator", "combat_tank_a", "combat_tank_h", "combat_tank_o" }

--- When true ProductionDelays is 1 and HoldProduction is always false
---@type table<player, boolean>
EmergencyBuildRate = {}

--- Activates when EmergencyBuildRate is True.
--- Override this function for mission specific behaviour
---@param player player
---@param target cpos -- position where emergency event take place
EmergencyBehaviour = function(player, target)
	HoldProduction[player] = false
	if Difficulty == "hard" then
		player.Cash = player.Cash + 2000
	end
end

--- Is a bot's production routine on hold?
---@type table<player, boolean>
HoldProduction = { }

IgnoreDamageFromTypes = {"spicebloom", "SpiceExplosion", "Debris", "Debris2", "Debris3", "Debris4"}

--- Was a bot's last harvester eaten by a sandworm?
---@type table<player, boolean>
LastHarvesterEaten = { }

--- Gather units from a bot's idle unit pool, up to a certain group size.
---@param owner player
---@param size integer
---@return actor[]
SetupAttackGroup = function(owner, size)
	local units = { }

	RemoveDeadActors(IdlingUnits[owner])
	for i = 0, size, 1 do
		if #IdlingUnits[owner] == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits[owner] + 1)

		if IdlingUnits[owner][number] then
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
	if Attacking[owner] then return end

	Attacking[owner] = true
	HoldProduction[owner] = true

	local units = SetupAttackGroup(owner, size)
	Utils.Do(units, Trigger.ClearAll)
	Trigger.AfterDelay(1, function()
		Utils.Do(units, function(u)
			u.Stop()
			IdleHunt(u)
		end)
	end)

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking[owner] = false
		HoldProduction[owner] = false
	end)
end

--- Order patrol routine along side predefined patrol points. When Patrol routine ends, squad return into IdlingUnits pool.
--- If no patrol points set - ignore request
---@param owner player
---@param size integer
SendPatrol = function(owner, size)
	if PatrolPoints[owner] == nil then return end

	local units = SetupAttackGroup(owner, size)
	local cells = Utils.Shuffle(PatrolPoints[owner])

	Utils.Do(units, function(unit)
		Trigger.ClearAll(unit)
		Trigger.AfterDelay(1, function()
			if unit.IsDead then return end
			unit.Patrol(cells, false, 1000)
			unit.CallFunc(function()
					table.insert(IdlingUnits[owner], unit)
					SelectRoutine(owner, unit)
			end)
			Trigger.OnDamaged(unit, function(self,  attacker)
				if Defending[owner][self] or owner.IsAlliedWith(attacker.Owner)  then
					return
				end
				if Utils.Any(IgnoreDamageFromTypes, function(a) return a == attacker.Type end) then
					return
				end
				Defending[owner][unit] = true
				Utils.Do(units, function(u)
					if not u.IsDead then
						u.Stop()
						u.AttackMove(self.Location)
						u.CallFunc(function()
							FindTargetsInArea(owner, self)
						end)
					end
				end)
			end)
		end)
	end)
end

--- Prepare a unit to call for help if attacked.
---@param unit actor
---@param defendingPlayer player
---@param defenderCount integer
DefendActor = function(unit, defendingPlayer, defenderCount)
	Trigger.OnDamaged(unit, function(self, attacker)
		if unit.Owner ~= defendingPlayer or attacker.Owner == defendingPlayer then
			return
		end

		if Defending[defendingPlayer][unit] then
			return
		end

		-- Don't try to attack spiceblooms, debris, etc
		if Utils.Any(IgnoreDamageFromTypes,
			function(a) return a == attacker.Type end) then
			return
		end

		Defending[defendingPlayer][unit] = true
		Trigger.AfterDelay(1000, function()
			if unit.IsDead then return end
			Defending[defendingPlayer][unit] = false
		end)

		-- if all units are busy or dead, activate emergency mode
		if #IdlingUnits[defendingPlayer] == 0 then
			if EmergencyBuildRate[defendingPlayer] then return end
			EmergencyBuildRate[defendingPlayer] = true
			EmergencyBehaviour(defendingPlayer, unit.Location)
			Trigger.AfterDelay(1500, function() EmergencyBuildRate[defendingPlayer] = false end)
		end

		CheckArea(defendingPlayer, unit.Location)
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
		if Difficulty == "hard" then
			Trigger.OnKilled(actor, function(killer)
				if EmergencyBuildRate[owner] then return end
				EmergencyBuildRate[owner] = true
				EmergencyBehaviour(owner, killer.Location)
				Trigger.AfterDelay(1500, function() EmergencyBuildRate[owner] = false end)
			end)
		end
	end)
end

--- Schedule production and attacks for a factory or other unit producer.
---@param player player Owner of the factory.
---@param factory actor The factory itself.
---@param delay fun(player):integer Function that returns a delay until production repeats.
---@param toBuild fun():string[] Function that returns a list of unit types to be produced.
---@param attackSize integer Number of units that will form the next attack wave.
---@param attackThresholdSize integer Number of idle units that will trigger an attack wave.
ProduceUnits = function(player, factory, delay, toBuild, attackSize, attackThresholdSize)
	if factory.IsDead or factory.Owner ~= player then return end

	if HoldProduction[player] then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
		return
	end

	--- Check harvester count
	if factory.Type == "heavy_factory" and #player.GetActorsByType("harvester") < HarvesterCount[player] then
		player.Build({"harvester"}, function()
			Trigger.AfterDelay(delay(player), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
		end)
		return
	end

	player.Build(toBuild(), function(unit)
		IdlingUnits[player][#IdlingUnits[player] + 1] = unit[1]
		SelectRoutine(player, unit[1])

		if #IdlingUnits[player] >= attackThresholdSize then
			if DateTime.GameTime < AttackDelay[player] then
				SendPatrol(player, attackSize / 2)
				HoldProduction[player] = true
				Trigger.AfterDelay((AttackDelay[player] - DateTime.GameTime) / 2, function()
					HoldProduction[player] = false
				end)
			else
				AttackDelay[player] = DateTime.GameTime + TimeBetweenAttacks[player]
				SendAttack(player, attackSize)
			end
		end

		Trigger.AfterDelay(delay(player), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
	end)
end

--- Periodically checks for nearby infantry units to crush.
---@param unit actor
---@param bot player
function AICrushLogic(unit, bot)
	if unit.IsDead then return end

	if Utils.RandomInteger(1,101) >= CrushChance[Difficulty] then
		Trigger.AfterDelay(200, function ()
			AICrushLogic(unit, bot)
		end)
		return
	end

	local targets = Map.ActorsInCircle(unit.CenterPosition, WDist.FromCells(5), function (a)
        return a.Owner.IsAlliedWith(bot) == false and
			a.Type == "light_inf" or
			a.Type == "trooper" or
			a.Type == "engineer" and
			Map.TerrainType(a.Location) ~= "Rough"
    end)

	if targets[1] ~= nil then
		unit.Stop()
		unit.Move(Utils.Random(targets).Location)
		Trigger.AfterDelay(55, function ()
			AICrushLogic(unit, bot)
		end)
		unit.Hunt()
	else
		Trigger.AfterDelay(200, function ()
			AICrushLogic(unit, bot)
		end)
	end
end

function IdleHuntOnBaseDestroyed(player, base)
	local checkedBase  = Utils.Where(base, function(building) return not building.IsDead and building.Owner == player end)
	Trigger.OnAllKilledOrCaptured(checkedBase , function()
		if Utils.Any(base, function(building) return not building.IsDead and building.Owner == player end) then
			IdleHuntOnBaseDestroyed(player, base)
		else
			Utils.Do(player.GetGroundAttackers(), function(a)
				a.Stop()
				IdleHunt(a)
			end)
		end
	end)
end

--- Take  number of units from IdlingUnits pool and check target area for enemy activity
--- When nothing found return to base and IdlingUnits pool/
--- @param owner player
--- @param targetArea cpos
--- @param size? integer (optional). When nil random integer is selected
CheckArea = function(owner, targetArea, size)
	if #IdlingUnits[owner] == 0 then return end

	size = size or Utils.RandomInteger(1 , #IdlingUnits[owner])
	local squad =  SetupAttackGroup(owner, size)
	Utils.Do(squad, function(unit)
		if unit.IsDead then return end
		Trigger.ClearAll(unit)
		unit.Stop()
		unit.AttackMove(targetArea)
		unit.CallFunc(function()
			Trigger.AfterDelay(1, function()
				FindTargetsInArea(owner, unit)
			end)
		end)
	end)
end

FindTargetsInArea = function(owner, unit)
	if unit.IsDead then return end

	Trigger.OnIdle(unit, function ()
		local enemies = Map.ActorsInCircle(unit.CenterPosition, WDist.FromCells(8), function(a)
			return a.IsInWorld
				and not a.IsDead
				and not a.Owner.IsAlliedWith(unit.Owner)
				and a.Owner.InternalName ~= "Neutral"
				and a.Owner.InternalName ~= "Creeps"
		end)

		if #enemies > 0 then
			unit.Hunt()
			unit.Wait(10)
		else
			unit.Wait(Utils.RandomInteger(200 , 500))
			if DefencePerimeter[owner] ~= nil then
				unit.AttackMove(Utils.Random(DefencePerimeter[owner]))
			end
			unit.CallFunc(function()
				if not unit.IsDead then
					table.insert(IdlingUnits[owner], unit)
					SelectRoutine(owner, unit)
				end
			end)
		end
	end)
end

SelectRoutine = function(owner, unit)
	Trigger.ClearAll(unit)
	Trigger.AfterDelay(1, function()
		if unit.IsDead then return end
		if DefencePerimeter[owner] == nil then return end

		-- ensure unit return into defence perimeter before apply new triggers
		unit.Move(Utils.Random(DefencePerimeter[owner]))
		unit.CallFunc(function()
			PatrolPerimeter(owner, unit)
			Trigger.OnKilled(unit, function(killer)
				RemoveActor(IdlingUnits[owner], unit)
				CheckArea(owner, killer.Location)
			end)
		end)
	end)
end

PatrolPerimeter = function(owner, unit)
	if unit.IsDead then return end

	local targetCell = Utils.Random(DefencePerimeter[owner])
	unit.AttackMove(targetCell, 2)
	unit.CallFunc(function ()
		Trigger.AfterDelay(Utils.RandomInteger(300, 500), function()
			if unit.IsDead then return end
				PatrolPerimeter(owner, unit)
		end)
	end)
end

GetCellsInRectangle = function (topLeft, bottomRight)
	local cells = {}
	local index = 1
	for x = topLeft.X, bottomRight.X, 1 do
		for y = topLeft.Y, bottomRight.Y, 1 do
			local cell = CPos.New(x, y)
			if Map.TerrainType(cell) ~= "Cliff" and Map.TerrainType(cell) ~= "Rough" then
				cells[index] = cell
				index = index + 1
			end
		end
	end
	return cells
end

RemoveDeadActors = function (actors)
	for i = #actors, 1, -1 do
		if actors[i].IsDead or not actors[i].IsInWorld then
			table.remove(actors, i)
		end
	end
end

RemoveActor = function (actors, actor)
	for i = #actors, 1, -1 do
		if actors[i] == actor then
			table.remove(actors, i)
			return
		end
	end
end
