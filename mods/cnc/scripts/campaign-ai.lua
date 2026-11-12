--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Shared, mission-agnostic building blocks for campaign AI scripts.
--
-- Mission scripts keep their scenario-specific triggers, wave tables and
-- scripted reinforcements. This file only provides the reusable parts that
-- would otherwise be copied between missions:
--
--   * land-region and reachability helpers
--   * a home-guard / attack-pool model (reserve vs. produced)
--   * target selection and attack orders with land checks
--   * local damage response and proactive idle defence
--   * Orca retreat / return-to-base handling
--   * small funds and storage helpers
--   * xpcall-based tick safety
--
-- Status: only bad-neighborhood uses this module so far. CA.SafeTick,
-- CA.SiteOccupied, CA.IsStartingLand, CA.AllOutAttack, CA.StartBaseDefense and
-- CA.Log are kept for future missions and are not yet verified by a mission.
--
-- The module exposes one global table, CampaignAI, so mission scripts can
-- use CampaignAI.TakeForAttack(...) etc. without polluting the global
-- namespace with generic short names.
--
-- Minimal usage sketch for a single-base mission:
--
--   CampaignAI.ConfigureLand({ minX = 2, maxX = 61, minY = 2, maxY = 61 })
--   CampaignAI.BuildLandRegions()
--   local base = CampaignAI.NewBase({ player = GDI, yard = { x, y },
--       defenderReserve = 4 })
--   CampaignAI.InitEconomy(base, { harvesterKind = "harv", refineryKind = "proc" })
--   CampaignAI.TrackHarvester(base, harvester)
--   CampaignAI.UpdateIncomeWatchdog(base)
--   local context = { enemy = Nod, players = { GDI }, base = base,
--       isActive = function() return not MissionFinished end }
--   CampaignAI.SeedPool(base, context)
--   CampaignAI.RegisterDamageHandler(actor, context)
--   CampaignAI.StartIdleDefense(context, 3)
--   CampaignAI.StartOrcaWatch({ base }, context, 5)
--   local team = CampaignAI.TakeForAttack(base, 3, true)
--   CampaignAI.SendTeam(team, GDI, route, true, true, context)

CampaignAI = CampaignAI or {}
local CA = CampaignAI

local function log(fmt, ...)
	print(("[CampaignAI] " .. fmt):format(...))
end

CA.Log = log

function CA.ErrorText(err)
	if debug and debug.traceback then
		return debug.traceback(tostring(err), 2)
	end
	return tostring(err)
end

-- Run fn through xpcall and log the traceback. Callers that own a repeating
-- timer should schedule the next tick outside this function or use a
-- recursive wrapper that always re-schedules after the call.
function CA.SafeCall(label, fn)
	local ok, err = xpcall(fn, CA.ErrorText)
	if not ok then
		log("ERROR %s: %s", label, tostring(err))
	end
	return ok
end

-- Convenience wrapper for Trigger.AfterDelay(..., CA.SafeTick("name", fn)).
function CA.SafeTick(label, fn)
	return function()
		CA.SafeCall(label, fn)
	end
end

-- Repeating timer that survives an error in its body. body returns false to
-- stop the loop, anything else (including nil) keeps it running.
function CA.RepeatingSafeTick(label, interval, body, isActive)
	local function step()
		if isActive and not isActive() then return end
		local ok, result = xpcall(body, CA.ErrorText)
		if not ok then
			log("ERROR %s: %s", label, tostring(result))
		elseif result == false then
			return
		end
		if isActive and not isActive() then return end
		Trigger.AfterDelay(DateTime.Seconds(interval), step)
	end
	return step
end

function CA.IsLive(actor)
	return actor ~= nil and not actor.IsDead and actor.IsInWorld
end

function CA.IsOwnedBy(actor, players)
	if players == nil then return true end
	if actor == nil or actor.Owner == nil then return false end
	for _, player in ipairs(players) do
		if actor.Owner == player then return true end
	end
	return false
end

function CA.ActorKey(actor)
	if actor == nil then return nil end
	local ok, id = pcall(function() return actor.ActorID end)
	if ok and id ~= nil then return id end
	return actor
end

local function cellOf(value)
	if value == nil then return nil end
	-- CPos and Actor are both userdata. Probing a property that only one of
	-- them defines throws, so use pcall instead of value.X/value.Location.
	local ok, location = pcall(function() return value.Location end)
	if ok and location ~= nil then return location end
	return value
end

local function yardCell(base)
	if base == nil or base.yard == nil then return nil end
	if base.yard.X ~= nil and base.yard.Y ~= nil then return base.yard end
	return CPos.New(base.yard[1], base.yard[2])
end

-- ---------------------------------------------------------------- land

local Land = {
	configured = false,
	cells = {},
	stride = 64,
	minX = 3,
	maxX = 62,
	minY = 1,
	maxY = 62,
	startCells = {},
}

local passable = {
	Clear = true,
	Rough = true,
	Road = true,
	Bridge = true,
	Tiberium = true,
	BlueTiberium = true,
	Beach = true,
}

local function cellKey(x, y)
	return y * Land.stride + x
end

function CA.ConfigureLand(config)
	config = config or {}
	local maxX = config.maxX or 62
	local stride = config.stride or 64
	-- cellKey packs (x, y) as y * stride + x; x must stay below the stride or
	-- two different cells would share a key. Grow the stride instead of
	-- asserting, because the campaign Lua sandbox has no assert().
	if maxX >= stride then
		log("WARNING maxX %d >= stride %d, growing stride to %d", maxX, stride, maxX + 1)
		stride = maxX + 1
	end
	Land.stride = stride
	Land.minX = config.minX or 3
	Land.maxX = config.maxX or 62
	Land.minY = config.minY or 1
	Land.maxY = config.maxY or 62
	Land.startCells = config.startCells or {}
	Land.cells = {}
	Land.configured = false
end

-- Build connected passable regions once after the map is loaded. The
-- implementation mirrors the proven nod13c region scan: 4-neighbour BFS over
-- the configured map bounds.
function CA.BuildLandRegions()
	if Map == nil or Map.TerrainType == nil then
		Land.configured = false
		return
	end

	Land.cells = {}
	local region = 0
	for y = Land.minY, Land.maxY do
		for x = Land.minX, Land.maxX do
			local key = cellKey(x, y)
			if Land.cells[key] == nil and passable[Map.TerrainType(CPos.New(x, y))] then
				region = region + 1
				Land.cells[key] = region
				local queue = { CPos.New(x, y) }
				local index = 1
				while index <= #queue do
					local cell = queue[index]
					index = index + 1
					for _, offset in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
						local nx, ny = cell.X + offset[1], cell.Y + offset[2]
						if nx >= Land.minX and nx <= Land.maxX and ny >= Land.minY and ny <= Land.maxY then
							local nkey = cellKey(nx, ny)
							if Land.cells[nkey] == nil and passable[Map.TerrainType(CPos.New(nx, ny))] then
								Land.cells[nkey] = region
								table.insert(queue, CPos.New(nx, ny))
							end
						end
					end
				end
			end
		end
	end

	Land.configured = region > 0
end

function CA.Region(cell)
	cell = cellOf(cell)
	if not Land.configured or cell == nil then return 0 end
	return Land.cells[cellKey(cell.X, cell.Y)] or 0
end

function CA.SameLand(a, b)
	if not Land.configured then return true end
	local ca, cb = cellOf(a), cellOf(b)
	if ca == nil or cb == nil then return false end
	local ra = CA.Region(ca)
	return ra ~= 0 and ra == CA.Region(cb)
end

function CA.IsStartingLand(cell)
	if not Land.configured or #Land.startCells == 0 then return false end
	local region = CA.Region(cell)
	if region == 0 then return false end
	for _, startCell in ipairs(Land.startCells) do
		if CA.Region(startCell) == region then return true end
	end
	return false
end

function CA.TargetOnLand(origin, target)
	if not Land.configured then return true end
	local targetCell = cellOf(target)
	if targetCell == nil then return false end
	local region = CA.Region(targetCell)
	return region ~= 0 and CA.SameLand(origin, targetCell)
end

-- ---------------------------------------------------------------- bases

function CA.NewBase(config)
	return {
		player = config.player,
		yard = config.yard or { 0, 0 },
		nodes = config.nodes or {},
		reserve = {},
		produced = {},
		defenderReserve = config.defenderReserve or 3,
		baseRadius = config.baseRadius or 32,
		defending = false,
		defenseUntil = 0,
		allOut = false,
	}
end

function CA.IsLocal(base, actor, radius)
	local yard = yardCell(base)
	if yard == nil or actor == nil or not actor.HasProperty("Location") or actor.Location == nil then return false end
	local dx = actor.Location.X - yard.X
	local dy = actor.Location.Y - yard.Y
	local limit = radius or base.baseRadius or 32
	return dx * dx + dy * dy <= limit * limit
end

function CA.GetLocalActors(base, kind, radius)
	local actors = {}
	if base == nil or base.player == nil then return actors end
	for _, actor in ipairs(base.player.GetActorsByType(kind)) do
		if CA.IsLive(actor) and actor.HasProperty("Location") and CA.IsLocal(base, actor, radius) then
			table.insert(actors, actor)
		end
	end
	return actors
end

function CA.LocalBuilding(base, kind, radius)
	local actors = CA.GetLocalActors(base, kind, radius)
	return actors[1]
end

-- Find a building of kind at exactly x/y. Unlike a player-local lookup this
-- also sees captured buildings, so a captured refinery/factory is treated as
-- present instead of causing an endless rebuild attempt at the occupied site.
function CA.BuildingAt(kind, x, y, owner)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if CA.IsLive(actor) and actor.Type == kind and actor.HasProperty("Location")
			and actor.Location.X == x and actor.Location.Y == y
			and (owner == nil or actor.Owner == owner) then
			return actor
		end
	end
	return nil
end

-- Approximate site check used by fixed rebuild plans. Terrain, player actors
-- and husks never block a site; any other health-bearing actor does.
function CA.SiteOccupied(x, y)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if CA.IsLive(actor) and actor.HasProperty("Location")
			and (actor.HasProperty("Health") or actor.HasProperty("Building"))
			and not actor.HasProperty("Land")
			and not actor.Type:match("%.husk$") then
			local dx = math.abs(actor.Location.X - x)
			local dy = math.abs(actor.Location.Y - y)
			if dx <= 1 and dy <= 1 then
				return true
			end
		end
	end
	return false
end

-- CNC building footprints. Missions can override any of these through the
-- options table passed to SiteFree; the defaults match the campaign ports.
CA.Footprints = {
	fact = { 3, 3 }, proc = { 3, 4 }, weap = { 3, 3 }, pyle = { 2, 3 },
	nuke = { 2, 3 }, nuk2 = { 2, 3 }, atwr = { 1, 1 }, gtwr = { 1, 1 },
	silo = { 2, 1 }, hq = { 2, 3 }, eye = { 2, 3 }, hpad = { 2, 2 },
	fix = { 3, 3 },
}
CA.EmptyFootprintCells = {
	proc = { [0] = true, [2] = true },
	hq = { [1] = true },
	eye = { [1] = true },
}

function CA.DefaultTerrainObstacle(actor)
	local actorType = actor.Type
	return actorType == "brik" or actorType == "cycl"
		or actorType:match("^rock%d") ~= nil
		or actorType:match("^t%d%d") ~= nil
		or actorType:lower():match("^split") ~= nil
		or actorType:match("%.husk$") ~= nil
end

function CA.Footprint(kind, options)
	if kind == nil then return { 1, 1 } end
	local footprints = (options and options.footprints) or CA.Footprints
	local size = footprints[kind]
	if size ~= nil then return size end
	if kind:match("^rock%d") then return { 4, 2 } end
	if kind:match("^t%d%d") then return { 2, 2 } end
	return { 1, 1 }
end

function CA.OccupiesFootprint(kind, dx, dy, options)
	local empty = (options and options.emptyFootprintCells) or CA.EmptyFootprintCells
	local cells = empty[kind]
	return cells == nil or not cells[dy * 4 + dx]
end

-- Footprint-aware rebuild-site check. Besides checking terrain it moves
-- friendly mobile units that overlap the requested footprint out of the way,
-- so a parked unit cannot block a rebuild indefinitely. Returns true only
-- when the whole footprint is free.
function CA.SiteFree(entry, options)
	options = options or {}
	local kind, x, y = entry[1], entry[2], entry[3]
	local size = CA.Footprint(kind, options)
	if size == nil then return false end

	for cx = x, x + size[1] - 1 do
		for cy = y, y + size[2] - 1 do
			if CA.OccupiesFootprint(kind, cx - x, cy - y, options) then
				local terrain = Map.TerrainType(CPos.New(cx, cy))
				if terrain ~= "Clear" and terrain ~= "Road" then
					return false
				end
			end
		end
	end

	local ignore = options.ignore
	local owner = options.owner
	local isObstacle = options.isTerrainObstacle or CA.DefaultTerrainObstacle
	for _, actor in ipairs(Map.ActorsInWorld) do
		if CA.IsLive(actor) and actor ~= ignore and actor.HasProperty("Location")
			and (actor.HasProperty("Health") or isObstacle(actor))
			and not actor.Type:match("%.husk$") then
			local ax, ay = actor.Location.X, actor.Location.Y
			local other = CA.Footprint(actor.Type, options)
			if ax < x + size[1] and ax + other[1] > x
				and ay < y + size[2] and ay + other[2] > y then
				local overlaps = false
				for cx = math.max(x, ax), math.min(ax + other[1], x + size[1]) - 1 do
					for cy = math.max(y, ay), math.min(ay + other[2], y + size[2]) - 1 do
						if CA.OccupiesFootprint(kind, cx - x, cy - y, options)
							and CA.OccupiesFootprint(actor.Type, cx - ax, cy - ay, options) then
							overlaps = true
						end
					end
				end

				if overlaps then
					if owner ~= nil and actor.Owner == owner
						and actor.HasProperty("Move") and not actor.HasProperty("Land") then
						actor.Move(CPos.New(x, y + size[2] + 1))
						-- A displaced harvester must resume harvesting; otherwise
						-- rebuilding silently parks it forever.
						if actor.HasProperty("FindResources") then
							actor.FindResources()
						end
					end
					return false
				end
			end
		end
	end
	return true
end

local function pruneList(list, owner)
	local i = 1
	while i <= #list do
		local actor = list[i]
		if not CA.IsLive(actor) or (owner ~= nil and actor.Owner ~= owner) then
			table.remove(list, i)
		else
			i = i + 1
		end
	end
end

function CA.PruneReserve(base)
	if base == nil then return end
	pruneList(base.reserve, base.player)
	pruneList(base.produced, base.player)
end

local function inList(list, actor)
	for _, entry in ipairs(list) do
		if entry == actor then return true end
	end
	return false
end

function CA.AddToPool(base, unit, context)
	if base == nil or not CA.IsLive(unit) or unit.Owner ~= base.player then return end
	if unit.Type == "harv" or unit.Type == "mcv" then return end
	CA.PruneReserve(base)
	if inList(base.reserve, unit) or inList(base.produced, unit) then return end

	if unit.HasProperty("Stance") then
		unit.Stance = "Defend"
	end

	local localUnit = CA.IsLocal(base, unit, base.baseRadius)
	-- During an all-out the base keeps replacements at home instead of
	-- trickling them into the attacker one by one.
	if base.allOut then
		if localUnit and #base.reserve < base.defenderReserve then
			table.insert(base.reserve, unit)
		end
		return
	end

	if localUnit and #base.reserve < base.defenderReserve then
		table.insert(base.reserve, unit)
	else
		table.insert(base.produced, unit)
	end
end

-- Seed the pool with the already placed units at mission start. Far-away
-- units become attack-pool units; local units become the initial garrison.
function CA.SeedPool(base, context, options)
	if base == nil then return end
	options = options or {}
	CA.PruneReserve(base)
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if CA.IsLive(unit) and unit.Type ~= "harv" and unit.Type ~= "mcv"
			and (options.exclude == nil or not options.exclude(unit)) then
			CA.AddToPool(base, unit, context)
		end
	end
end

function CA.GetCombatUnits(base, context)
	local units = {}
	if base == nil or base.player == nil then return units end
	context = context or {}
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if CA.IsLive(unit) and unit.Type ~= "harv" and unit.Type ~= "mcv"
			and (context.exclude == nil or not context.exclude(unit)) then
			table.insert(units, unit)
		end
	end
	return units
end

-- Take up to limit units for an attack. With keepReserve (default true) the
-- home garrison is left intact and only surplus reserve units plus the
-- produced pool are committed.
function CA.TakeForAttack(base, limit, keepReserve)
	local team = {}
	if base == nil then return team end
	CA.PruneReserve(base)
	limit = limit or math.huge
	keepReserve = keepReserve ~= false

	local reserveAvailable = #base.reserve
	if keepReserve then
		reserveAvailable = math.max(0, reserveAvailable - base.defenderReserve)
	end

	local taken = {}
	local function takeFrom(list, count)
		local index = #list
		while index >= 1 and count > 0 do
			local unit = table.remove(list, index)
			index = index - 1
			if CA.IsLive(unit) and unit.Owner == base.player and unit.IsInWorld
				and not taken[CA.ActorKey(unit)] then
				taken[CA.ActorKey(unit)] = true
				table.insert(team, unit)
				count = count - 1
			end
		end
	end

	local fromReserve = math.min(limit, reserveAvailable)
	if fromReserve > 0 then takeFrom(base.reserve, fromReserve) end
	local remaining = limit - #team
	if remaining > 0 then takeFrom(base.produced, remaining) end
	return team
end

-- ---------------------------------------------------------------- targets

function CA.ClosestEnemy(unit, enemy, maxDistance, capture, context)
	if not CA.IsLive(unit) or enemy == nil or not unit.HasProperty("Location") then return nil end
	context = context or {}

	local target, distance
	for _, actor in ipairs(enemy.GetActors()) do
		if CA.IsLive(actor) and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land")
			and CA.TargetOnLand(unit.Location, actor)
			and (context.exclude == nil or not context.exclude(actor))
			and (not capture or (unit.HasProperty("CanCapture") and unit.CanCapture(actor))) then
			local dx = unit.Location.X - actor.Location.X
			local dy = unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if (maxDistance == nil or d <= maxDistance * maxDistance)
				and (distance == nil or d < distance) then
				target, distance = actor, d
			end
		end
	end
	return target
end

function CA.ClosestBuilding(unit, enemy, context)
	if not CA.IsLive(unit) or enemy == nil or not unit.HasProperty("Location") then return nil end
	context = context or {}

	local target, distance
	for _, actor in ipairs(enemy.GetActors()) do
		if CA.IsLive(actor) and actor.HasProperty("Location")
			and actor.HasProperty("StartBuildingRepairs")
			and CA.TargetOnLand(unit.Location, actor)
			and (context.exclude == nil or not context.exclude(actor)) then
			local dx = unit.Location.X - actor.Location.X
			local dy = unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if distance == nil or d < distance then
				target, distance = actor, d
			end
		end
	end
	return target
end

function CA.OrderAttack(unit, context)
	if not CA.IsLive(unit) or context == nil or context.enemy == nil then return end
	if unit.HasProperty("AttackMove") then
		local target = CA.ClosestEnemy(unit, context.enemy, nil, false, context)
		if target ~= nil then unit.AttackMove(target.Location, 2) end
	elseif unit.HasProperty("Capture") and unit.HasProperty("CanCapture") then
		local target = CA.ClosestEnemy(unit, context.enemy, nil, true, context)
		if target ~= nil then
			unit.Capture(target)
		elseif unit.HasProperty("Move") then
			local enemy = CA.ClosestEnemy(unit, context.enemy, nil, false, context)
			if enemy ~= nil then unit.Move(enemy.Location) end
		end
	end
end

function CA.OrderAttackBase(unit, context)
	if not CA.IsLive(unit) or not unit.HasProperty("AttackMove") or context == nil or context.enemy == nil then return end
	local target = CA.ClosestBuilding(unit, context.enemy, context)
	if target ~= nil then
		unit.AttackMove(target.Location, 2)
	else
		CA.OrderAttack(unit, context)
	end
end

function CA.WatchAttackers(team, owner, baseAttack, context)
	local function tick()
		local alive = false
		for _, unit in ipairs(team) do
			if CA.IsLive(unit) and unit.Owner == owner then
				alive = true
				if unit.IsInWorld and unit.IsIdle then
					if baseAttack then
						CA.OrderAttackBase(unit, context)
					else
						CA.OrderAttack(unit, context)
					end
				end
			end
		end
		return alive
	end

	CA.RepeatingSafeTick("watch-attackers", 10, tick, context and context.isActive)()
end

-- Send an existing group along optional waypoints. Unlike a plain sequence
-- of AttackMove calls this cancels a running Guard/Patrol activity first, so
-- the new orders actually start. SameLand filtering keeps waypoints from
-- other islands out of the queue.
function CA.SendTeam(team, owner, route, attack, baseAttack, context)
	if context and context.isActive and not context.isActive() then return end
	route = route or {}
	for _, unit in ipairs(team) do
		if CA.IsLive(unit) and unit.Owner == owner and unit.IsInWorld and unit.HasProperty("Location") then
			if unit.HasProperty("Stop") then unit.Stop() end
			if unit.HasProperty("Stance") then
				unit.Stance = attack and "AttackAnything" or "Defend"
			end

			local ordered = false
			for _, waypoint in ipairs(route) do
				local cell = cellOf(waypoint)
				if cell ~= nil and CA.SameLand(unit.Location, cell) then
					if unit.HasProperty("AttackMove") then
						unit.AttackMove(cell)
						ordered = true
					elseif unit.HasProperty("Move") then
						unit.Move(cell)
						ordered = true
					end
				end
			end

			if attack and not ordered then
				if baseAttack then
					CA.OrderAttackBase(unit, context)
				else
					CA.OrderAttack(unit, context)
				end
			end
		end
	end
	if attack and #team > 0 then
		CA.WatchAttackers(team, owner, baseAttack, context)
	end
end

-- ---------------------------------------------------------------- orca

local orcaHandled = {}
local retreatingOrcas = {}

function CA.GuardOrca(base, orca, context, attack)
	if base == nil or not CA.IsLive(orca) or orca.Type ~= "orca" then return end
	local key = CA.ActorKey(orca)
	if orcaHandled[key] == nil then
		orcaHandled[key] = true
		Trigger.OnDamaged(orca, function(actor, attacker, damage)
			CA.OrcaDamaged(base, actor, attacker, damage, context)
		end)
	end

	if orca.HasProperty("Stance") then
		orca.Stance = attack and "AttackAnything" or "Defend"
	end
	if base.defending and orca.HasProperty("Stance") then
		orca.Stance = "AttackAnything"
		IdleHunt(orca)
	end
end

function CA.OrcaDamaged(base, orca, attacker, damage, context)
	if base == nil or not CA.IsLive(orca) or orca.Type ~= "orca" then return end
	if not orca.Health or not orca.MaxHealth or orca.Health * 4 > orca.MaxHealth then return end
	local key = CA.ActorKey(orca)
	if retreatingOrcas[key] then return end
	if #CA.GetLocalActors(base, "hpad", base.baseRadius) == 0 then return end

	retreatingOrcas[key] = true
	if orca.HasProperty("Stop") then orca.Stop() end
	if orca.HasProperty("ReturnToBase") then orca.ReturnToBase() end
end

function CA.OrcaWatch(bases, context, interval)
	interval = interval or 5

	local function tick()
		for _, base in ipairs(bases) do
			local pad = CA.LocalBuilding(base, "hpad", base.baseRadius)
			for _, orca in ipairs(base.player.GetActorsByType("orca")) do
				if CA.IsLive(orca) and orca.IsInWorld then
					local key = CA.ActorKey(orca)
					if retreatingOrcas[key] then
						if pad == nil or (orca.Health and orca.MaxHealth and orca.Health * 10 >= orca.MaxHealth * 9) then
							retreatingOrcas[key] = nil
							if base.defending and orca.HasProperty("Stance") then
								orca.Stance = "AttackAnything"
								IdleHunt(orca)
							end
						end
					end
				end
			end
		end
		return true
	end

	CA.RepeatingSafeTick("orca-watch", interval, tick, context and context.isActive)()
end

function CA.StartOrcaWatch(bases, context, interval)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		CA.SafeCall("orca-watch-start", function()
			CA.OrcaWatch(bases, context, interval)
		end)
	end)
end

-- ---------------------------------------------------------------- all-out

function CA.AllOutAttack(base, target, context)
	if base == nil then return end
	base.allOut = true

	local team = CA.GetCombatUnits(base, context)
	base.reserve = {}
	base.produced = {}
	if #team > 0 then
		CA.SendTeam(team, base.player, {}, true, true, context)
	end

	for _, orca in ipairs(base.player.GetActorsByType("orca")) do
		if CA.IsLive(orca) and orca.IsInWorld then
			CA.GuardOrca(base, orca, context, true)
			if target ~= nil and orca.HasProperty("AttackMove")
				and CA.SameLand(orca.Location, target.Location) then
				orca.AttackMove(target.Location, 2)
			end
			IdleHunt(orca)
		end
	end
end

function CA.StartBaseDefense(base, target, context)
	if base == nil or base.allOut then return end
	context = context or {}
	local alarmSeconds = context.defenseAlarmSeconds or 30
	base.defending = true
	base.defenseUntil = DateTime.GameTime + DateTime.Seconds(alarmSeconds)
	CA.AllOutAttack(base, target, context)

	-- All-out is temporary: after the alarm window new replacements return to
	-- the normal pool/garrison behaviour and Orcas stop being force-released.
	Trigger.AfterDelay(DateTime.Seconds(alarmSeconds), function()
		base.defending = false
		base.allOut = false
	end)
end

-- ---------------------------------------------------------------- defence

-- Register the standard damage response for placed, produced or rebuilt
-- actors. Missions can hook scenario-specific alerting through context.onDamage
-- while the shared helper performs repairs and local defence.
function CA.RegisterDamageHandler(actor, context)
	if actor == nil or context == nil then return end
	Trigger.OnDamaged(actor, function(self, attacker, damage)
		-- A captured building keeps its old handler; do not alert the former
		-- owner after ownership changed.
		if not CA.IsOwnedBy(self, context.players) then return end
		if context.onDamage ~= nil then
			CA.SafeCall("damage-hook", function()
				context.onDamage(self, attacker, damage)
			end)
		end
		CA.DefendAgainst(self, attacker, damage, context)
	end)
end

function CA.DefendAgainst(actor, attacker, damage, context)
	if context == nil or (context.isActive and not context.isActive()) then return end
	if not CA.IsLive(actor) or not actor.HasProperty("Location") or damage == nil or damage <= 0 then return end
	if not CA.IsOwnedBy(actor, context.players) then return end
	if not CA.IsLive(attacker) or attacker.Owner ~= context.enemy or not attacker.HasProperty("Location") then return end
	if context.exclude ~= nil and context.exclude(actor) then return end

	local building = actor.HasProperty("StartBuildingRepairs")
	if building and actor.Health and actor.MaxHealth
		and actor.Health < actor.MaxHealth * (context.repairThreshold or 0.7) then
		if context.canRepair == nil or context.canRepair(actor) then
			actor.StartBuildingRepairs()
		end
	end

	if building and context.allOutBuildings ~= nil and context.allOutBuildings[actor.Type] then
		CA.StartBaseDefense(context.base, attacker, context)
	end

	if building and context.releaseOrcas and context.base ~= nil then
		for _, orca in ipairs(context.base.player.GetActorsByType("orca")) do
			if CA.IsLive(orca) and orca.IsInWorld and orca.HasProperty("Stance") then
				orca.Stance = "AttackAnything"
				IdleHunt(orca)
			end
		end
	end

	local radius = building and 18 or 10
	for _, player in ipairs(context.players or {}) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if CA.IsLive(unit) and unit.IsInWorld and unit ~= actor
				and unit.HasProperty("AttackMove") and unit.HasProperty("Location") and unit.Owner == player
				and unit.Type ~= "harv" and unit.Type ~= "mcv"
				and (context.exclude == nil or not context.exclude(unit))
				and CA.SameLand(unit.Location, attacker.Location) then
				local dx = unit.Location.X - actor.Location.X
				local dy = unit.Location.Y - actor.Location.Y
				if dx * dx + dy * dy <= radius * radius then
					if unit.HasProperty("Stop") then unit.Stop() end
					unit.AttackMove(attacker.Location, 2)
				end
			end
		end
	end
end

-- Idle defenders proactively engage nearby enemies instead of waiting for
-- the first hit. Only units on Defend are considered, so units already on
-- the attack are not pulled back.
function CA.IdleDefense(context, interval)
	if context == nil or context.enemy == nil then return end
	interval = interval or 3

	local function tick()
		for _, player in ipairs(context.players or {}) do
			for _, unit in ipairs(player.GetGroundAttackers()) do
				if CA.IsLive(unit) and unit.IsInWorld and unit.IsIdle
					and unit.HasProperty("AttackMove") and unit.HasProperty("Location") and unit.HasProperty("Stance")
					and unit.Stance == "Defend"
					and unit.Type ~= "harv" and unit.Type ~= "mcv"
					and (context.exclude == nil or not context.exclude(unit)) then
					local target = CA.ClosestEnemy(unit, context.enemy, 16, false, context)
					if target ~= nil and CA.SameLand(unit.Location, target.Location) then
						if unit.HasProperty("Stop") then unit.Stop() end
						unit.AttackMove(target.Location, 2)
					end
				end
			end
		end
		return true
	end

	CA.RepeatingSafeTick("idle-defense", interval, tick, context.isActive)()
end

function CA.StartIdleDefense(context, interval)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		CA.SafeCall("idle-defense-start", function()
			CA.IdleDefense(context, interval)
		end)
	end)
end

-- ---------------------------------------------------------------- economy

function CA.InitEconomy(base, config)
	config = config or {}
	base.harvesters = {}
	base.harvesterKind = config.harvesterKind or "harv"
	base.refineryKind = config.refineryKind or "proc"
	base.harvesterProductionKind = config.harvesterProductionKind or "weap"
	base.incomeWindow = config.incomeWindow or 120
	base.lastFunds = CA.Funds(base.player)
	base.lastIncomeTime = DateTime.GameTime
	base.incomeSuspect = false
end

function CA.TrackHarvester(base, harv)
	if base == nil or not CA.IsLive(harv) or harv.Owner ~= base.player then return end
	if base.harvesterKind ~= nil and harv.Type ~= base.harvesterKind then return end
	base.harvesters = base.harvesters or {}
	local key = CA.ActorKey(harv)
	if base.harvesters[key] ~= nil then return end
	base.harvesters[key] = harv
	Trigger.OnKilled(harv, function()
		base.harvesters[key] = nil
	end)
end

function CA.AdoptFreeHarvesters(base)
	if base == nil or base.player == nil then return end
	base.harvesters = base.harvesters or {}
	for _, harv in ipairs(base.player.GetActorsByType(base.harvesterKind or "harv")) do
		if CA.IsLive(harv) then CA.TrackHarvester(base, harv) end
	end
end

function CA.LiveHarvesterCount(base)
	local count = 0
	if base == nil or base.harvesters == nil then return 0 end
	for key, harv in pairs(base.harvesters) do
		if CA.IsLive(harv) and harv.Owner == base.player then
			count = count + 1
		else
			base.harvesters[key] = nil
		end
	end
	return count
end

function CA.RestartIdleHarvesters(base)
	if base == nil or base.harvesters == nil then return end
	for key, harv in pairs(base.harvesters) do
		if CA.IsLive(harv) and harv.Owner == base.player
			and harv.IsIdle and harv.HasProperty("FindResources") then
			harv.FindResources()
		elseif not CA.IsLive(harv) then
			base.harvesters[key] = nil
		end
	end
end

-- Assign each tracked harvester to the nearest local refinery that still has
-- capacity. The claim tables are bookkeeping only; harvesters keep their
-- engine resource-finding behaviour.
function CA.RefreshHarvesterClaims(base, config)
	config = config or {}
	if base == nil or base.player == nil then return {} end
	local refineryKind = config.refineryKind or base.refineryKind or "proc"
	local harvesterKind = config.harvesterKind or base.harvesterKind or "harv"
	local perRefinery = config.harvestersPerRefinery or 1

	base.refineryClaims = {}
	base.harvesterClaims = {}
	local refineries = {}
	for _, proc in ipairs(base.player.GetActorsByType(refineryKind)) do
		if CA.IsLive(proc) and CA.IsLocal(base, proc, base.baseRadius) then
			local entry = { actor = proc, harvesters = {} }
			base.refineryClaims[CA.ActorKey(proc)] = entry
			table.insert(refineries, entry)
		end
	end

	base.harvesters = base.harvesters or {}
	for key, harv in pairs(base.harvesters) do
		if CA.IsLive(harv) and harv.Owner == base.player and harv.Type == harvesterKind then
			local best, bestDistance
			for _, entry in ipairs(refineries) do
				if #entry.harvesters < perRefinery then
					local dx = harv.Location.X - entry.actor.Location.X
					local dy = harv.Location.Y - entry.actor.Location.Y
					local d = dx * dx + dy * dy
					if bestDistance == nil or d < bestDistance then
						best, bestDistance = entry, d
					end
				end
			end
			-- If every refinery is full, still attach the surplus harvester to
			-- the nearest one for diagnostics; it is not a build target.
			if best == nil then
				for _, entry in ipairs(refineries) do
					local dx = harv.Location.X - entry.actor.Location.X
					local dy = harv.Location.Y - entry.actor.Location.Y
					local d = dx * dx + dy * dy
					if bestDistance == nil or d < bestDistance then
						best, bestDistance = entry, d
					end
				end
			end
			if best ~= nil then
				table.insert(best.harvesters, harv)
				base.harvesterClaims[key] = CA.ActorKey(best.actor)
			end
		else
			base.harvesters[key] = nil
		end
	end
	return refineries
end

-- Number of additional harvesters needed to staff the local refineries at
-- harvestersPerRefinery each. Returns 0 when no local refinery exists.
function CA.NeededHarvesters(base, config)
	config = config or {}
	local refineries = CA.RefreshHarvesterClaims(base, config)
	local perRefinery = config.harvestersPerRefinery or 1
	return math.max(0, #refineries * perRefinery - CA.LiveHarvesterCount(base))
end

-- Coarse, reusable economy state. Healthy means the income chain is staffed
-- and funded; Strained means income exists but is damaged or underfunded;
-- Crisis means the income chain is structurally broken; Liquidation is a
-- crisis without enough cash to rebuild the chain.
function CA.ClassifyEconomy(base, config)
	config = config or {}
	if base == nil or base.player == nil then
		return {
			state = "Crisis",
			refineries = 0,
			harvesters = 0,
			neededHarvesters = 0,
			funds = 0,
			lowPower = false,
			storageFull = false,
		}
	end

	local refineries = CA.RefreshHarvesterClaims(base, config)
	local perRefinery = config.harvestersPerRefinery or 1
	local harvesters = CA.LiveHarvesterCount(base)
	local needed = math.max(0, #refineries * perRefinery - harvesters)
	local funds = CA.Funds(base.player)
	local lowPower = base.player.PowerState ~= nil and base.player.PowerState ~= "Normal"
	local storageFull = CA.IsStorageFull(base.player)

	local state = "Healthy"
	if #refineries == 0 or harvesters == 0 then
		state = funds < (config.crisisFunds or 500) and "Liquidation" or "Crisis"
	elseif needed > 0 or lowPower or funds < (config.strainedFunds or 1500) or base.incomeSuspect then
		state = "Strained"
	end

	return {
		state = state,
		refineries = #refineries,
		harvesters = harvesters,
		neededHarvesters = needed,
		funds = funds,
		lowPower = lowPower,
		storageFull = storageFull,
	}
end

-- Detect an economy that is not producing income despite live harvesters and
-- refineries. The watchdog only re-tasks idle harvesters; it does not sell or
-- rebuild anything, so it is safe to use as a low-frequency maintenance step.
function CA.UpdateIncomeWatchdog(base)
	if base == nil or base.player == nil then return false end
	base.harvesters = base.harvesters or {}
	local funds = CA.Funds(base.player)
	if base.lastFunds == nil then
		base.lastFunds = funds
		base.lastIncomeTime = DateTime.GameTime
	end
	if funds > base.lastFunds then
		base.lastIncomeTime = DateTime.GameTime
		base.incomeSuspect = false
	end
	base.lastFunds = funds

	if CA.IsStorageFull(base.player) then
		base.incomeSuspect = false
		return false
	end
	if CA.LiveHarvesterCount(base) == 0 then
		base.incomeSuspect = false
		return false
	end
	if #base.player.GetActorsByType(base.refineryKind or "proc") == 0 then
		base.incomeSuspect = false
		return false
	end

	local window = base.incomeWindow or 120
	if DateTime.GameTime - (base.lastIncomeTime or 0) > DateTime.Seconds(window) then
		base.incomeSuspect = true
		CA.RestartIdleHarvesters(base)
	end
	return base.incomeSuspect
end

function CA.Funds(player)
	if player == nil then return 0 end
	return (player.Cash or 0) + (player.Resources or 0)
end

function CA.AvailableFunds(player, reserved)
	return math.max(0, CA.Funds(player) - (reserved or 0))
end

function CA.SpendFunds(player, amount)
	if player == nil or amount <= 0 then return true end
	if CA.Funds(player) < amount then return false end
	local resources = math.min(player.Resources or 0, amount)
	player.Resources = (player.Resources or 0) - resources
	player.Cash = (player.Cash or 0) - (amount - resources)
	return true
end

function CA.RefundFunds(player, amount)
	if player == nil or amount <= 0 then return end
	player.Resources = (player.Resources or 0) + amount
end

function CA.IsStorageFull(player)
	if player == nil or (player.ResourceCapacity or 0) <= 0 then return false end
	return (player.Resources or 0) >= player.ResourceCapacity
end

function CA.IsStorageNearlyFull(player, reserve)
	if player == nil or (player.ResourceCapacity or 0) <= 0 then return false end
	return (player.Resources or 0) >= player.ResourceCapacity - (reserve or 500)
end
