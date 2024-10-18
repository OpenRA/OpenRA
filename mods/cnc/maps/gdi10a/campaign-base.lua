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
Base = {
	Init = function(player, structures, rallyPoint)

		Base.Player = player
		Base.RallyPoint = rallyPoint
		-- 'working' is when we've started building a structure
		-- 'idle' is when we're not building even if there are lost structures
		Base.State = "idle"
		Base.Queue = {}
		Base.Blueprints = {}
		for i = 1, #structures do
			local structure = structures[i]
			local blueprint = {
				Type = structure.Type,
				Location = structure.Location,
			}
			table.insert(Base.Blueprints, blueprint)
			Base.SetupBldg(blueprint, structure, true)
		end
	end;

	SetupBldg = function(blueprint, structure, autoRepair)
		Media.Debug("Setup " .. tostring(structure) .. " as " .. ActorString(blueprint) .. ' Bal$' .. BankBalance(Base.Player))
		if autoRepair then
			RepairBuilding(Base.Player, structure, 0.75)
		end
		-- NOTE If newly built, it enters the world on the next tick
		local type = structure.Type
		-- New units' first move
		if Base.RallyPoint and (type == 'hand' or type == 'afld' or type == 'weap'
			or type == 'pyle') then
			structure.RallyPoint = Base.RallyPoint
		end
		-- What to do if this structure is destroyed
		Trigger.OnKilled(structure, function()
			-- Add to build queue
			Media.Debug(string.format('%s killed: Base.State=%s  Queue_len=%d',
				structure.Type, tostring(Base.State), #Base.Queue))
			-- Enqueue
			table.insert(Base.Queue, blueprint)
			if #Base.Queue == 1 and Base.State == "idle" then
				-- Build queue was empty; start building now
				-- This ensures we only build one structure at a time
				Base.StartWork()
			end
		end)
	end;

	StartWork = function()
		-- Continue processing the queue.
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Base.DoWork()
		end)
	end;

	-- Replace structures when destroyed
	DoWork = function()
		if Base.State ~= "idle" then
			local s='ERR: Base.DoWork: should not happen while build in progress! Queue_len='..#Base.Queue
			Media.Debug(s)
			print(s)
		end
		while #Base.Queue > 0 do
			local rc = Base.Rebuild()
			Media.Debug('Base.Rebuild: ' .. rc)
			if rc == 'cancelled' then
				table.remove(Base.Queue, 1)
			elseif rc == 'insufficient_funds' then
				-- Continue processing later
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Base.DoWork()
				end)
				return
			elseif rc == 'in_progress' then
				-- Stop processing the queue while it builds
				table.remove(Base.Queue, 1)
				return
			else
				Media.Debug("Unknown return from Base.Rebuild: " .. tostring(rc))
			end
		end
		Base.State = 'idle'
	end;

	-- Helper for Base.DoWork
	-- Return 'cancelled' when we give up trying to rebuild this structure.
	-- Return 'insufficient_funds' when we are waiting for funds
	-- Return 'in_progress' when we start to build the structure
	Rebuild = function()
		if #Base.Queue == 0 then
			local s = 'Base.Rebuild: queue empty!'
			Media.Debug(s)
			print(s)
			return 'queue-empty'
		end
		local blueprint = Base.Queue[1]
		local cyard = Base.GetCyard()
		if not cyard then
			Media.Debug("Can't rebuild " .. ActorString(blueprint) .. ": No Cyard")
			return 'cancelled' -- Lost cyard; can never build again
		end

		local cost = Actor.Cost(blueprint.Type)
		if BankBalance(Base.Player) < cost then
			-- Can't build now
			Media.Debug("Can't rebuild " .. ActorString(blueprint) .. " yet. Cash$" .. BankBalance(Base.Player) .. ", Cost$" .. cost)
			return 'insufficient_funds'
		end

		-- Start building now
		Base.State = "working"
		BankDeduct(Base.Player, cost)
		Trigger.AfterDelay(Actor.BuildTime(blueprint.Type), function()
			-- Construction complete
			--[[TODO Check for obstacles
			if IsBuildAreaBlocked(Base.Player, blueprint) then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					BuildBlueprint(blueprint)  -- this function???
				end)
				return
			end
			]]--
			if cyard.IsDead or cyard.Owner ~= Base.Player then
				-- Lost our cyard; can't build anymore
				Media.Debug("Lost cyard; refunding $" .. cost .. " for " .. ActorString(blueprint))
				-- Credit
				Base.Player.Cash = Base.Player.Cash + cost
			else
				local structure = Actor.Create(blueprint.Type, true, { Owner = Base.Player, Location = blueprint.Location })
				Base.SetupBldg(blueprint, structure, true)
			end
			Base.State = 'idle'
			-- Continue processing the queue.
			if #Base.Queue > 0 then
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Base.DoWork()
				end)
			else
			end
		end)
		Media.Debug(string.format("Rebuilding %s: %d sec, $%d", ActorString(blueprint),
			Actor.BuildTime(blueprint.Type), cost))
		return 'in_progress'
	end;

	GetCyard = function()
		local cyards = Base.Player.GetActorsByType("fact")
		if (#cyards < 1) then
			return nil -- Lost cyard; can never build again
		else
			return cyards[1]
		end
	end;
}

ActorString = function(actor)
	return string.format('%s (%d,%d)', actor.Type, actor.Location.X, actor.Location.Y)
end
