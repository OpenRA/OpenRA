--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- AI will try to replace certain buildings when lost
-- @param structures A list of structures that the AI should
--        repair or rebuild as needed
Base_init = function(player, structures, rallyPoint)

    Base_Player = player
    Base_rallyPoint = rallyPoint
    Base_state = "idle"
    Base_queue = {}
    Base_Blueprints = {}
	for i = 1, #structures do
		local structure = structures[i]
		local blueprint = {
			Type = structure.Type,
			Location = structure.Location,
		}
		table.insert(Base_Blueprints, blueprint)
		Base_SetupBldg(blueprint, structure, true)
	end
end

Base_SetupBldg = function(blueprint, structure, autoRepair)
	Media.Debug("Setup " .. tostring(structure) .. " as " .. ActorString(blueprint) .. ' Bal$' .. BankBalance(Base_Player))
	if autoRepair then
		RepairBuilding(Base_Player, structure, 0.75)
	end
	-- NOTE If newly built, it enters the world on the next tick
    local type = structure.Type
	if type == 'hand' or type == 'pyle' then
		Trigger.AfterDelay(AttackCooldown[Difficulty], function()
			ProduceUnit(structure, InfProducer)
		end)
	elseif type == 'afld' or type == 'weap' then
		Trigger.AfterDelay(AttackCooldown[Difficulty], function()
			ProduceUnit(structure, ArmorProducer)
		end)
	end
	-- New units' first move
	if Base_rallyPoint and (type == 'hand' or type == 'afld' or type == 'weap'
        or type == 'pyle')
    then
		structure.RallyPoint = Base_rallyPoint
	end
    -- What to do if this structure is destroyed
	Trigger.OnKilled(structure, function()
		-- Add to build queue
		Media.Debug(string.format('%s killed: Base_state=%s  Queue_len=%d',
            structure.Type, tostring(Base_state), #Base_queue))
		-- Enqueue
        table.insert(Base_queue, blueprint)
		if Base_state == "idle" then
			-- Build queue was empty; start building now
			-- This ensures we only build one structure at a time
			Base_StartWork()
		end
	end)
end

Base_StartWork = function()
    -- Continue processing the queue.
    Trigger.AfterDelay(DateTime.Seconds(3), function()
        Base_DoWork(Base_queue)
    end)
end

-- Replace structures when destroyed
Base_DoWork = function(queue)
	if Base_state ~= "idle" then
		local s='Base_DoWork: should not happen while build in progress! Queue_len='..#queue
		Media.Debug(s)
		print(s)
	end
	while #queue > 0 do
		local rc = Base_Rebuild(queue)
		Media.Debug('Base_Rebuild: ' .. rc)
		if rc == 'cancelled' then
			table.remove(queue, 1)
		elseif rc == 'insufficient_funds' then
			-- Continue processing later
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Base_DoWork(queue)
			end)
			return
		elseif rc == 'in_progress' then
			-- Stop processing the queue while it builds
			table.remove(queue, 1)
			return
		else
			Media.Debug("Unknown return from Base_Rebuild: " .. tostring(rc))
		end
	end
    Base_state = 'idle'
end

-- Helper for Base_DoWork
-- Return 'cancelled' when we give up trying to rebuild this structure.
-- Return 'insufficient_funds' when we are waiting for funds
-- Return 'in_progress' when we start to build the structure
Base_Rebuild = function(queue)
	if #queue == 0 then
		local s = 'Base_Rebuild: queue empty!'
		Media.Debug(s)
		print(s)
		return 'queue-empty'
	end
	local blueprint = queue[1]
	local cyard = Base_GetCyard()
	if not cyard then
		Media.Debug("Can't rebuild " .. ActorString(blueprint) .. ": No Cyard")
		return 'cancelled' -- Lost cyard; can never build again
	end

	local cost = Actor.Cost(blueprint.Type)
	if BankBalance(Base_Player) < cost then
		-- Can't build now
		Media.Debug("Can't rebuild " .. ActorString(blueprint) .. " yet. Cash$" .. BankBalance(Base_Player) .. ", Cost$" .. cost)
		return 'insufficient_funds'
	end

	-- Start building now
	Base_state = "working"
	BankDeduct(Base_Player, cost)
	Trigger.AfterDelay(Actor.BuildTime(blueprint.Type), function()
		-- Construction complete
		RebuildingInProgress = false
		--[[TODO Check for obstacles
		if IsBuildAreaBlocked(Base_Player, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				BuildBlueprint(blueprint)  -- this function???
			end)
			return
		end
		]]--
		if cyard.IsDead or cyard.Owner ~= Base_Player then
			-- Lost our cyard; can't build anymore
			Media.Debug("Lost cyard; refunding $" .. cost .. " for " .. ActorString(blueprint))
			-- Credit
			Base_Player.Cash = Base_Player.Cash + cost
		else
			local structure = Actor.Create(blueprint.Type, true, { Owner = Base_Player, Location = blueprint.Location })
			Base_SetupBldg(blueprint, structure, true)
		end
		-- Continue processing the queue.
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Base_DoWork(queue)
		end)
	end)
	Media.Debug(string.format("Rebuilding %s: %d sec, $%d", ActorString(blueprint),
		Actor.BuildTime(blueprint.Type), cost))
	return 'in_progress'
end

Base_GetCyard = function()
	local cyards = Base_Player.GetActorsByType("fact")
	if (#cyards < 1) then
		return nil -- Lost cyard; can never build again
	else
		return cyards[1]
	end
end
