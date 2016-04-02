
AIPlayers = { }
AIBarracks = { }
AIDerricks = { }
AIBaseLocation = { }
IdlingAIActors = { }
IdlingAISupportActors = { }
AIOnDefense = { }

-- It's good to start with 10 rifle man, one medic and 5 rocket soldiers
InitialUnitsToBuild = { "e1", "e1", "e1", "e1", "e1", "medi", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3", "e3" }
UnitsToBuild = { "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "medi" }

ActivateAI = function(player, id)
	AIPlayers[id] = player

	Trigger.AfterDelay(0, function()
		local barracks = player.GetActorsByType("tent")
		if #barracks > 0 then
			AIBarracks[id] = barracks[1]
			AIBaseLocation[id] = barracks[1].Location + CVec.New(2, 1)
			IdlingAIActors[id] = { }
			IdlingAISupportActors[id] = { }
			InitialInfantryProduction(id, InitialUnitsToBuild)
			DefendActor(id, barracks[1])
			RepairBarracks(id)
			SellWalls(id)

			Trigger.AfterDelay(DateTime.Seconds(10), function() LookOutForCrates(id) end)
		end

		local derricks = player.GetActorsByType("oilb")
		if #derricks > 0 then
			AIDerricks[id] = derricks[1]
			DefendActor(id, derricks[1])
		end
	end)
end

InitialInfantryProduction = function(id, units)
	local productionComplete = AIPlayers[id].Build(units, function(actors)
		InfantryProduction(id)
	end)

	Trigger.OnProduction(AIBarracks[id], function(producer, produced)
		BuildComplete(id, produced)
	end)
end

InfantryProduction = function(id)
	local productionComplete = AIPlayers[id].Build({ Utils.Random(UnitsToBuild) }, function(actors)
		Trigger.AfterDelay(0, function() InfantryProduction(id) end)
	end)

	if not productionComplete then
		Trigger.AfterDelay(0, function() InfantryProduction(id) end)
	end
end

BuildComplete = function(id, actor)
	if actor.Type == "medi" then
		local number = #IdlingAISupportActors[id] + 1
		IdlingAISupportActors[id][number] = actor

		Trigger.OnKilled(actor, function()
			table.remove(IdlingAISupportActors[id], number)
		end)
	else
		local number = #IdlingAIActors[id] + 1
		IdlingAIActors[id][number] = actor

		Trigger.OnKilled(actor, function()
			table.remove(IdlingAIActors[id], number)
		end)
	end

	Trigger.AfterDelay(0, function() DefendActor(id, actor) end)
end

AttackGroupSize = 5
SetupAttackGroup = function(id)
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		if (#IdlingAIActors[id] == 0) then
			return units
		end

		local number = Utils.RandomInteger(0, #IdlingAIActors[id]) + 1
		units[#units + 1] = IdlingAIActors[id][number]
		table.remove(IdlingAIActors[id], number)
	end

	return units
end

DefendActor = function(id, actorToDefend)
	if not actorToDefend or actorToDefend.IsDead or not actorToDefend.IsInWorld then
		return
	end

	Trigger.OnDamaged(actorToDefend, function(self, attacker)
		if AIOnDefense[id] or not attacker or attacker.IsDead then
			return
		end

		-- Don't try to kill something you can't kill
		if attacker.Type == "sniper.soviets" then
			return
		end

		AIOnDefense[id] = true
		local attackLoc = attacker.Location

		local defenders = SetupAttackGroup(id)
		if not defenders or #defenders == 0 then
			Trigger.AfterDelay(DateTime.Seconds(30), function() AIOnDefense[id] = false end)
			return
		end

		Utils.Do(defenders, function(unit)
			if unit.IsDead then
				return
			end

			unit.AttackMove(attackLoc)

			local home = AIBaseLocation[id]
			Trigger.OnIdle(unit, function()
				if unit.Location == home then
					IdlingAIActors[id][#IdlingAIActors[id] + 1] = unit
					Trigger.Clear(unit, "OnIdle")
					AIOnDefense[id] = false
				else
					unit.AttackMove(home)
				end
			end)
		end)
	end)
end

RepairBarracks = function(id)
	Trigger.OnDamaged(AIBarracks[id], function(self, attacker)
		self.StartBuildingRepairs(AIPlayers[id])
	end)
end

SellWalls = function(id)
	Media.DisplayMessage("Lonestar AI " .. id .. " sold its walls for better combat experience.")

	local walls = AIPlayers[id].GetActorsByType("brik")
	Utils.Do(walls, function(wall)
		wall.Destroy()
	end)
end

LookOutForCrates = function(id)
	Trigger.OnEnteredProximityTrigger(AIBarracks[id].CenterPosition, WDist.New(12 * 1024), function(actor)
		if actor.Type ~= "fortcrate" or #IdlingAIActors[id] == 0 then
			return
		end

		local unit = Utils.Random(IdlingAIActors[id])
		local home = AIBaseLocation[id]
		local aim = actor.Location
		if unit.IsDead then
			return
		end

		unit.AttackMove(aim)
		Trigger.OnIdle(unit, function()
			if unit.Location == aim or not actor.IsInWorld then
				unit.AttackMove(home)
				Trigger.Clear(unit, "OnIdle")
			else
				unit.AttackMove(aim)
			end
		end)
	end)
end
