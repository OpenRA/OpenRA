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
--   * base maintenance: rebuild plans, production queues, harvesters
--   * small funds and storage helpers
--   * xpcall-based tick safety
--
-- Status: bad-neighborhood, deceit, eviction-notice, cloak-and-dagger,
-- hostile-takeover and under-siege use this module. The scheduler, unit
-- self-defence, engineer capture, per-pad Orcas, "n-th building" start,
-- one-shot cell triggers, counter-attack helpers (2026-11-17) and the
-- type-filtered team formation / timed special threat (under-siege,
-- 2026-11-17) and cloak-aware scripted targeting (CA.IsCloaked,
-- CA.ClosestEnemy; under-siege playtest 2026-11-18) are covered by
-- harness/campaign-ai-harness.lua.
--
-- Verified by both older ports:
-- land/pool/orders, damage response, idle defence, Orca handling,
-- CA.FindFreeCell/FindFreeSite/SiteFree, CA.MissingBuilding/BuildQueue,
-- CA.TryRebuildEntry/MaintainBase, CA.QueueUnit/TryQueueFromPool,
-- CA.MaintainHarvesters/ReplaceHarvester and CA.RecallReserve.
-- Kept for future missions but not called by any mission yet:
-- CA.SafeTick, CA.SiteOccupied, CA.IsStartingLand, CA.AllOutAttack,
-- CA.StartBaseDefense and CA.Log. Treat those as "not yet verified by a
-- mission". CA.StartBaseDefense is intentionally not used by
-- hostile-takeover, which needs CA.DefendAgainstAttacker instead.
--
-- IsLive requires IsInWorld. Callbacks that fire before an actor enters the
-- world (OnPassengerExited, OnCapture) must use CA.IsAlive instead; using
-- IsLive there silently skips every healthy actor. This exact mistake broke
-- the deceit engineer drop once.
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

-- Repeating timer that survives an error in its body. `isActive` pauses the
-- work but keeps the timer scheduled, so a gate that becomes true later (for
-- example GDIHunting in Deceit) still starts the loop. `isFinished` is the
-- hard stop; a body returning false stops the loop as before.
function CA.RepeatingSafeTick(label, interval, body, isActive, isFinished)
	local function step()
		if isFinished and isFinished() then return end
		if isActive == nil or isActive() then
			local ok, result = xpcall(body, CA.ErrorText)
			if not ok then
				log("ERROR %s: %s", label, tostring(result))
			elseif result == false then
				return
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(interval), step)
	end
	return step
end

-- Schedule a repeating, mission-specific threat. This is the timed sibling of
-- CA.RepeatingSafeTick: the first execution can be delayed independently of
-- the repeat interval, and the body can stop the series by returning false.
--
-- options: label, firstDelay, interval, execute, isActive, isFinished
function CA.StartSpecialThreat(context, options)
	options = options or {}
	context = context or {}
	local step = CA.RepeatingSafeTick(
		options.label or "special-threat",
		options.interval or 300,
		options.execute,
		options.isActive or context.isActive,
		options.isFinished or context.isFinished)
	Trigger.AfterDelay(DateTime.Seconds(options.firstDelay or 0), step)
	return step
end

function CA.IsLive(actor)
	return actor ~= nil and not actor.IsDead and actor.IsInWorld
end

-- Only checks the death flag. Use this inside callbacks that run before the
-- engine adds the actor to the world (OnPassengerExited, OnCapture, spawn
-- callbacks), where CA.IsLive would be false even for a healthy actor.
function CA.IsAlive(actor)
	return actor ~= nil and not actor.IsDead
end

-- Safe wrapper for Player.GetActorsByType. The engine throws for unknown
-- actor types; a future mission migrating to this module may be missing an
-- optional actor (for example a mod without Orcas). Returning an empty list
-- keeps the shared loops alive.
function CA.GetActorsByType(player, kind)
	if player == nil or kind == nil then return {} end
	local ok, list = pcall(function() return player.GetActorsByType(kind) end)
	if ok and list ~= nil then return list end
	return {}
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
		rebuilding = false,
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
	for _, actor in ipairs(CA.GetActorsByType(base.player, kind)) do
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

-- Coarse 3x3 occupancy check. Every live actor with Health or Building that
-- is not an aircraft blocks the cell. Kept for simple scripted spawns; the
-- rebuild plans use CA.SiteFree, which is footprint-aware and can displace
-- friendly mobile units.
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
						-- Move the blocker to a real free cell instead of a fixed
						-- offset that may be off-map, water or occupied.
						local destination = CA.FindFreeCell(x + math.floor(size[1] / 2),
							y + size[2], 8, { ignore = actor, isTerrainObstacle = isObstacle })
						if destination ~= nil then
							actor.Move(destination)
						end
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

-- Find a passable, unoccupied cell near (x, y). Used to displace units that
-- block a rebuild footprint and as a general "park unit somewhere safe" helper.
function CA.FindFreeCell(x, y, radius, options)
	options = options or {}
	radius = radius or 6
	local isObstacle = options.isTerrainObstacle or CA.DefaultTerrainObstacle
	local ignore = options.ignore

	for r = 1, radius do
		for dx = -r, r do
			for dy = -r, r do
				if math.abs(dx) == r or math.abs(dy) == r then
					local cx, cy = x + dx, y + dy
					if cx >= Land.minX and cx <= Land.maxX
						and cy >= Land.minY and cy <= Land.maxY then
						local cell = CPos.New(cx, cy)
						if passable[Map.TerrainType(cell)] then
							local blocked = false
							for _, actor in ipairs(Map.ActorsInWorld) do
								if actor ~= ignore and CA.IsLive(actor)
									and actor.HasProperty("Location")
									and (actor.HasProperty("Health") or isObstacle(actor)) then
									local fp = CA.Footprint(actor.Type, options)
									if cx >= actor.Location.X and cx < actor.Location.X + fp[1]
										and cy >= actor.Location.Y and cy < actor.Location.Y + fp[2] then
										blocked = true
										break
									end
								end
							end
							if not blocked then
								return cell
							end
						end
					end
				end
			end
		end
	end
	return nil
end

-- Try the planned cell and then a small ring around it. Fixed [Base] plans are
-- only roughly aligned with imported maps, so a member of the footprint set is
-- often occupied by a pre-placed unit.
function CA.FindFreeSite(entry, options)
	if entry == nil then return nil end
	if CA.SiteFree(entry, options) then
		return entry
	end

	for radius = 1, 4 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				if math.abs(dx) == radius or math.abs(dy) == radius then
					local candidate = { entry[1], entry[2] + dx, entry[3] + dy }
					if CA.SiteFree(candidate, options) then
						return candidate
					end
				end
			end
		end
	end
	return nil
end

local function optionValue(value)
	if type(value) == "function" then return value() end
	return value
end

local supportQueue = { gtwr = true, atwr = true, silo = true }

function CA.BuildQueue(kind, options)
	options = options or {}
	if options.queueFor ~= nil then return options.queueFor(kind) end
	return supportQueue[kind] and "Support.GDI" or "Building.GDI"
end

function CA.MissingBuilding(plan, kind)
	for _, entry in ipairs(plan or {}) do
		if entry[1] == kind and CA.BuildingAt(entry[1], entry[2], entry[3]) == nil then
			return entry
		end
	end
	return nil
end

-- Spend money and build one plan entry after the engine build time. The entry
-- is relocated to the first free site and the entry itself is updated so a
-- later tick does not build the same structure again next to the old cell.
function CA.TryRebuildEntry(base, entry, options)
	if base == nil or entry == nil then return false end
	options = options or {}
	if base.rebuilding then return false end

	local siteOptions = options.siteOptions or {}
	local site = CA.FindFreeSite(entry, siteOptions)
	if site == nil then return false end
	entry[2], entry[3] = site[2], site[3]

	local player = options.player or base.player
	local cost = Actor.Cost(entry[1])
	local reserve = optionValue(options.reserve) or 0
	if CA.Funds(player) < cost + reserve then return false end
	if not CA.SpendFunds(player, cost) then return false end
	base.rebuilding = true

	local buildTime = Actor.BuildTime(entry[1], CA.BuildQueue(entry[1], options))
	Trigger.AfterDelay(buildTime, function()
		base.rebuilding = false

		if options.isActive ~= nil and not options.isActive() then
			CA.RefundFunds(player, cost)
			return
		end
		if not CA.SiteFree(site, siteOptions) then
			CA.RefundFunds(player, cost)
			return
		end

		local building = Actor.Create(entry[1], true, { Owner = player, Location = CPos.New(site[2], site[3]) })
		if not CA.IsLive(building) then
			CA.RefundFunds(player, cost)
			return
		end
		if options.onComplete ~= nil then
			CA.SafeCall("rebuild-on-complete", function()
				options.onComplete(building)
			end)
		end
	end)

	return true
end

-- One maintenance pass of the shared rebuild plan. Order follows the classic
-- campaign priority: keep the power on, restore the income/production chain,
-- then optional structures while the economy is healthy.
--
-- options: plan, targets, targetOrder, lateTargetOrder, optionalEntries,
-- optionalFunds, reserve, siteOptions, onComplete, isActive, player
function CA.MaintainBase(base, options)
	if base == nil then return end
	options = options or {}
	local player = options.player or base.player
	local plan = options.plan or {}
	local siteOptions = options.siteOptions or {}

	local function count(kind)
		return #CA.GetActorsByType(player, kind)
	end

	local function addMissing(order, kind)
		local entry = CA.MissingBuilding(plan, kind)
		if entry ~= nil then table.insert(order, entry) end
	end

	local lowPower = player.PowerState == "Low" or player.PowerState == "Critical"
	local storageFull = CA.IsStorageNearlyFull(player, 500)

	local order = {}
	if lowPower then
		for _, entry in ipairs(plan) do
			if entry[1] == "nuk2" then table.insert(order, entry) end
		end
	end

	local targets = options.targets or {}
	for _, kind in ipairs(options.targetOrder or { "proc", "weap", "pyle" }) do
		local target = targets[kind]
		if target ~= nil and count(kind) < target then
			addMissing(order, kind)
		end
	end

	for _, entry in ipairs(plan) do
		if (entry[1] == "nuk2" and not lowPower) or (storageFull and entry[1] == "silo") then
			table.insert(order, entry)
		end
	end

	for _, kind in ipairs(options.lateTargetOrder or { "hpad" }) do
		local target = targets[kind]
		if target ~= nil and count(kind) < target then
			addMissing(order, kind)
		end
	end

	if player.PowerState == "Normal" and CA.Funds(player) >= (options.optionalFunds or 1500) then
		local optional = optionValue(options.optionalEntries)
		for _, entry in ipairs(optional or {}) do
			table.insert(order, entry)
		end
	end

	local seen = {}
	for _, entry in ipairs(order) do
		local key = entry[1] .. entry[2] .. "," .. entry[3]
		if not seen[key] and CA.BuildingAt(entry[1], entry[2], entry[3]) == nil then
			seen[key] = true
			if CA.TryRebuildEntry(base, entry, options) then
				return
			end
		else
			seen[key] = true
		end
	end
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
-- With options.nearestFirst the closest units become the home guard instead
-- of whichever units happen to appear first in the map file.
function CA.SeedPool(base, context, options)
	if base == nil then return end
	options = options or {}
	CA.PruneReserve(base)

	local units = {}
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if CA.IsLive(unit) and unit.Type ~= "harv" and unit.Type ~= "mcv"
			and (options.exclude == nil or not options.exclude(unit)) then
			table.insert(units, unit)
		end
	end

	if options.nearestFirst then
		local yard = yardCell(base)
		if yard ~= nil then
			table.sort(units, function(a, b)
				local ax, ay = a.Location.X - yard.X, a.Location.Y - yard.Y
				local bx, by = b.Location.X - yard.X, b.Location.Y - yard.Y
				return ax * ax + ay * ay < bx * bx + by * by
			end)
		end
	end

	for _, unit in ipairs(units) do
		CA.AddToPool(base, unit, context)
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
--
-- options.prefer(unit) may return a numeric score; the lowest score is taken
-- first (for example "closest to the front"). Without it the previous LIFO
-- behaviour is kept: the most recently added units leave first.
function CA.TakeForAttack(base, limit, keepReserve, options)
	local team = {}
	if base == nil then return team end
	options = options or {}
	CA.PruneReserve(base)
	limit = limit or math.huge
	keepReserve = keepReserve ~= false

	local taken = {}
	local function collect(list, count)
		if count <= 0 then return end

		local candidates = {}
		for _, unit in ipairs(list) do
			local key = CA.ActorKey(unit)
			if CA.IsLive(unit) and unit.Owner == base.player and unit.IsInWorld
				and not taken[key] then
				table.insert(candidates, unit)
			end
		end

		if options.prefer ~= nil then
			table.sort(candidates, function(a, b)
				return options.prefer(a) < options.prefer(b)
			end)
		else
			-- Keep the historical LIFO order: newest pool entry first.
			local reversed = {}
			for i = #candidates, 1, -1 do
				table.insert(reversed, candidates[i])
			end
			candidates = reversed
		end

		for i = 1, math.min(count, #candidates) do
			local unit = candidates[i]
			taken[CA.ActorKey(unit)] = true
			table.insert(team, unit)
		end
	end

	local reserveAvailable = #base.reserve
	if keepReserve then
		reserveAvailable = math.max(0, reserveAvailable - base.defenderReserve)
	end

	local fromReserve = math.min(limit, reserveAvailable)
	if fromReserve > 0 then collect(base.reserve, fromReserve) end
	local remaining = limit - #team
	if remaining > 0 then collect(base.produced, remaining) end

	-- Remove the selected units from the pool lists.
	local selected = {}
	for _, unit in ipairs(team) do
		selected[CA.ActorKey(unit)] = true
	end
	for _, list in ipairs({ base.reserve, base.produced }) do
		local i = 1
		while i <= #list do
			if selected[CA.ActorKey(list[i])] then
				table.remove(list, i)
			else
				i = i + 1
			end
		end
	end

	return team
end

-- ---------------------------------------------------------------- targets

-- Script-side enemy scans cannot ask the engine "can my owner see this
-- actor?", so they must not look through cloak on their own. The original
-- TechnoClass::In_Range rejects cloaked objects outright, and OpenRA's own
-- AutoTarget already filters with Actor.CanBeViewedByPlayer (which honours
-- DetectCloaked). A scripted scan that ignores cloak made idle defenders
-- chase a cloaked Stealth Tank (Under Siege playtest). Detected cloaked
-- actors are still engaged by the engine's AutoTarget.
function CA.IsCloaked(actor)
	return actor ~= nil and actor.HasProperty("IsCloaked") and actor.IsCloaked
end

function CA.ClosestEnemy(unit, enemy, maxDistance, capture, context)
	if not CA.IsLive(unit) or enemy == nil or not unit.HasProperty("Location") then return nil end
	context = context or {}

	local target, distance
	for _, actor in ipairs(enemy.GetActors()) do
		if CA.IsLive(actor) and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land")
			and CA.TargetOnLand(unit.Location, actor)
			and (context.includeCloaked or not CA.IsCloaked(actor))
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

	CA.RepeatingSafeTick("watch-attackers", 10, tick, context and context.isActive, context and context.isFinished)()
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
					-- `attack = false` is the original Move mission (for example
					-- SCB35EA's `mtnk` team); do not turn it into AttackMove just
					-- because the unit also has that command.
					if attack and unit.HasProperty("AttackMove") then
						unit.AttackMove(cell)
						ordered = true
					elseif unit.HasProperty("Move") then
						unit.Move(cell)
						ordered = true
					elseif unit.HasProperty("AttackMove") then
						unit.AttackMove(cell)
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

-- Recruit an existing group by exact unit type. The original "Create Team"
-- action does not spawn units; it takes them from the house's existing army.
-- Existing callers hand-rolled this filter (Hostile Takeover's FindGDIUnits);
-- this is the shared, deterministic replacement.
--
-- companions: array of { kind, count }
-- options: routeStart (CPos/actor for nearest-first), exclude(actor),
--          reserve (set of ActorKey values already claimed),
--          keepReserve (do not take base.reserve units)
-- Returns the selected units (possibly understrength) or nil if none.
function CA.FormTeam(base, context, companions, options)
	if base == nil or base.player == nil then return nil end
	options = options or {}
	local claimed = {}
	for key in pairs(options.reserve or {}) do claimed[key] = true end
	if options.keepReserve and base.reserve ~= nil then
		for _, unit in ipairs(base.reserve) do claimed[CA.ActorKey(unit)] = true end
	end

	local start = cellOf(options.routeStart)

	local function distance(unit)
		if start == nil or unit.Location == nil then return 0 end
		local dx = unit.Location.X - start.X
		local dy = unit.Location.Y - start.Y
		return dx * dx + dy * dy
	end

	local function available(unit)
		return CA.IsLive(unit) and unit.Owner == base.player and unit.IsInWorld
			and unit.HasProperty("Location")
			and not claimed[CA.ActorKey(unit)]
			and (options.exclude == nil or not options.exclude(unit))
	end

	local team = {}
	for _, entry in ipairs(companions or {}) do
		local kind, count = entry[1], entry[2] or 1
		local candidates = {}
		for _, unit in ipairs(CA.GetActorsByType(base.player, kind)) do
			if available(unit) then table.insert(candidates, unit) end
		end
		table.sort(candidates, function(a, b)
			local da, db = distance(a), distance(b)
			if da ~= db then return da < db end
			local ka, kb = CA.ActorKey(a), CA.ActorKey(b)
			if type(ka) == "number" and type(kb) == "number" then return ka < kb end
			return tostring(ka) < tostring(kb)
		end)

		for i = 1, math.min(count, #candidates) do
			local unit = candidates[i]
			claimed[CA.ActorKey(unit)] = true
			table.insert(team, unit)
		end
	end
	if #team == 0 then return nil end
	return team
end

-- ---------------------------------------------------------------- production

-- Queue one unit at a factory. Returns false instead of erroring when the
-- factory is missing/busy or the player cannot afford the unit.
function CA.QueueUnit(factory, kind, player)
	if not CA.IsLive(factory) or not factory.IsInWorld or not factory.HasProperty("Build")
		or not factory.HasProperty("IsProducing") or factory.IsProducing(kind) then
		return false
	end
	if CA.Funds(player) < Actor.Cost(kind) then
		return false
	end
	return factory.Build({ kind })
end

-- Try the entries of a shuffled unit pool. Random removal mirrors the original
-- AI, which picks a random buildable unit instead of a fixed order.
function CA.TryQueueFromPool(factory, pool, player, options)
	options = options or {}
	if factory == nil then return false end

	local costs = options.costs or {}
	local prerequisites = options.prerequisites or {}
	local reserve = optionValue(options.reserve) or 0
	local funds = options.funds or CA.AvailableFunds(player, reserve)
	if funds < (options.minFunds or 0) + reserve then return false end

	local kinds = {}
	for _, kind in ipairs(pool) do table.insert(kinds, kind) end
	for _ = #kinds, 1, -1 do
		local kind = table.remove(kinds, Utils.RandomInteger(1, #kinds + 1))
		if funds >= (costs[kind] or options.defaultCost or 800) + reserve
			and (player.HasPrerequisites == nil or player.HasPrerequisites(prerequisites[kind] or {}))
			and CA.QueueUnit(factory, kind, player) then
			return true
		end
	end
	return false
end

-- ---------------------------------------------------------------- triggers

-- Run `onStarted` once the given player has placed `count` real buildings.
-- Wall segments (brik/barb/sbag/cycl/wood) fire OnBuildingPlaced once per
-- segment and would otherwise let three cheap wall pieces start the AI.
-- Captured buildings do not fire the trigger at all, so they never count.
--
-- options: ignoreTypes (extra actor names), fallbackSeconds (start anyway
-- after that many seconds), isFinished
function CA.StartProductionAfterBuildings(player, count, onStarted, options)
	options = options or {}
	local placed = 0
	local started = false
	local fallback = options.fallbackSeconds
	local finished = options.isFinished or function() return false end

	local ignored = {
		brik = true, barb = true, sbag = true, cycl = true, wood = true,
	}
	for _, name in ipairs(options.ignoreTypes or {}) do
		ignored[name] = true
	end

	local function start()
		if started or finished() then return end
		started = true
		CA.SafeCall("production-start", function() onStarted() end)
	end

	Trigger.OnBuildingPlaced(player, function(_, building)
		if started or finished() or building == nil then return end
		if building.Owner ~= player or ignored[building.Type] then return end
		placed = placed + 1
		if placed >= count then start() end
	end)

	if fallback ~= nil and fallback > 0 then
		Trigger.AfterDelay(DateTime.Seconds(fallback), start)
	end

	return function() return started end
end

-- Register a footprint trigger that fires for the first entry of one player
-- and removes itself afterwards. Covers the common mission pattern in one
-- place instead of a hand-written guard in every mission.
--
-- options: isFinished, onEnter(actor)
function CA.OnFirstEntry(cells, owner, onEnter, options)
	options = options or {}
	local finished = options.isFinished or function() return false end
	local done = false

	Trigger.OnEnteredFootprint(cells, function(actor, id)
		if done or finished() or actor == nil or actor.Owner ~= owner then return end
		done = true
		Trigger.RemoveFootprintTrigger(id)
		CA.SafeCall("first-entry", function() onEnter(actor) end)
	end)
end

-- ---------------------------------------------------------------- scheduler

-- Deterministic production loop. Replaces the per-mission ProductionTick
-- copies in Bad Neighborhood, Deceit, Eviction Notice and Hostile Takeover.
--
-- A mission supplies data (pools, caps, interval, factories, reserve) and
-- the loop handles the mechanics that were learned the hard way:
--   * only units produced by the AI count towards the cap, never the
--     starting garrison or scripted drops
--   * the pool is scanned, so one unaffordable entry (a 1500 $ Mammoth next
--     to a 600 $ Jeep) cannot stall the line
--   * one unit per tick, alternating categories
--   * a broken income chain pauses combat production, a missing harvester
--     does NOT (reserving its cost froze every CA mission)
--
-- options:
--   pools        = { infantry = {...}, vehicle = {...}, aircraft = {...} }
--                  or a single array used for every factory
--   factories    = { infantry = "pyle", vehicle = "weap", aircraft = "hpad" }
--   interval     = seconds between ticks (number or function)
--   maxTotal     = produced units total (number or function)
--   maxPerCategory = limit per category (default half of maxTotal, min 4)
--   reserve      = cash kept free (number or function)
--   extraReserve = optional function returning additional reserved cash
--   economy      = options for CA.ClassifyEconomy; if nil the gate is off
--   radius       = factory search radius (default base.baseRadius)
--   produceOrcas = true to also run one Orca per helipad
--   onQueue      = optional function(kind, factory)
--   isActive/isFinished
function CA.StartProduction(base, context, options)
	options = options or {}
	context = context or {}
	local factories = options.factories or { infantry = "pyle", vehicle = "weap" }
	local radius = options.radius or base.baseRadius or 24

	local indices = { infantry = 1, vehicle = 1, aircraft = 1 }
	local turn = false
	local pending = nil

	-- Clear the in-flight guard as soon as the factory actually produces the
	-- unit. Without this the guard only expires on its 60 s safety timeout, so
	-- the loop would build one unit per minute no matter what interval the
	-- mission configured (this regression appeared during the migration).
	Trigger.OnAnyProduction(function(factory, unit)
		if pending ~= nil and unit ~= nil
			and pending.factory == factory and pending.kind == unit.Type then
			pending = nil
		end
	end)

	local function option(value)
		if type(value) == "function" then return value() end
		return value
	end

	local function interval()
		return option(options.interval) or 30
	end

	local function pool(category)
		local pools = options.pools
		if pools == nil then return {} end
		if pools[category] ~= nil then return pools[category] end
		if pools[1] ~= nil then return pools end
		return {}
	end

	-- Exact category lookup by unit type. Deriving it from the mission's pools
	-- avoids the engine limitation that infantry cannot be detected by
	-- HasProperty (there is no "Infantry" script property).
	local typeCategory = {}
	local function buildTypeCategory()
		local pools = options.pools
		if pools == nil then return end
		for category, entries in pairs(pools) do
			if category ~= "1" and type(entries) == "table" then
				for _, kind in ipairs(entries) do
					if typeCategory[kind] == nil then typeCategory[kind] = category end
				end
			end
		end
	end
	buildTypeCategory()

	local function categoryOf(unit)
		if unit == nil then return "vehicle" end
		return typeCategory[unit.Type] or CA.DefaultUnitCategory(unit)
	end

	local countProduced = function()
		return CA.CountProducedUnits(base, categoryOf)
	end

	local function tryCategory(category)
		if pending ~= nil then return false end

		local factoryType = factories[category]
		if factoryType == nil then return false end

		local factory = CA.LocalBuilding(base, factoryType, radius)
		if factory == nil or not CA.IsLive(factory) then return false end

		-- Optional category cap supplied by the mission.
		local categoryCap = options.maxPerCategory
		if type(categoryCap) == "table" then categoryCap = categoryCap[category] end
		if categoryCap ~= nil then
			local _, perCategory = countProduced()
			if (perCategory[category] or 0) >= categoryCap then return false end
		end

		local entries = pool(category)
		if #entries == 0 then return false end

		local reserve = (option(options.reserve) or 0) + (option(options.extraReserve) or 0)
		for offset = 0, #entries - 1 do
			local slot = (indices[category] - 1 + offset) % #entries + 1
			local kind = entries[slot]
			if not factory.IsProducing(kind) and CA.Funds(base.player) >= Actor.Cost(kind) + reserve then
				if CA.QueueUnit(factory, kind, base.player) then
					indices[category] = slot % #entries + 1
					pending = { factory = factory, kind = kind }
					Trigger.AfterDelay(DateTime.Seconds(60), function()
						if pending ~= nil and pending.factory == factory and pending.kind == kind then
							pending = nil
						end
					end)
					if options.onQueue ~= nil then
						CA.SafeCall("production-on-queue", function()
							options.onQueue(kind, factory)
						end)
					end
					return true
				end
			end
		end
		return false
	end

	local function tick()
		if options.isFinished ~= nil and options.isFinished() then return end
		if options.isActive ~= nil and not options.isActive() then
			Trigger.AfterDelay(DateTime.Seconds(interval()), tick)
			return
		end

		CA.SafeCall("production", function()
			-- A structurally broken economy pauses combat production. A
			-- missing harvester does not; that mistake froze whole missions.
			local blocked = false
			if options.economy ~= nil then
				local economy = CA.ClassifyEconomy(base, options.economy)
				blocked = economy.state == "Crisis" or economy.state == "Liquidation"
			end

			if not blocked then
				local maxTotal = option(options.maxTotal)
				local total = countProduced()
				if maxTotal == nil or total < maxTotal then
					if turn then
						if not tryCategory("infantry") then tryCategory("vehicle") end
					else
						if not tryCategory("vehicle") then tryCategory("infantry") end
					end
					turn = not turn
				end

				if options.produceOrcas then
					CA.ProduceOrcaPerPad(base, context, options.orcaOptions)
				end
			end
		end)

		if options.isFinished ~= nil and options.isFinished() then return end
		Trigger.AfterDelay(DateTime.Seconds(interval()), tick)
	end

	Trigger.AfterDelay(DateTime.Seconds(options.firstDelay or 0), tick)
	return tick
end

-- Default unit category. HasProperty checks *member* names of the script
-- command classes, not trait names: there is no "Infantry" or "Aircraft"
-- member. Aircraft are identified by their AircraftProperties members
-- (Land/ReturnToBase), everything else falls back to "vehicle".
-- Missions that need infantry/vehicle caps rely on CA.StartProduction, which
-- derives the category from the mission's pools.
function CA.DefaultUnitCategory(unit)
	if unit == nil then return "vehicle" end
	if unit.HasProperty("Land") or unit.HasProperty("ReturnToBase") then
		return "aircraft"
	end
	return "vehicle"
end

-- Count the units an AI has actually produced (base.produced), pruning dead
-- and captured entries. Missions that implement their own loop use this to
-- keep the cap semantics identical to CA.StartProduction.
--
-- categoryOf: optional function(unit) -> category name
function CA.CountProducedUnits(base, categoryOf)
	if base == nil then return 0, { infantry = 0, vehicle = 0, aircraft = 0 } end
	categoryOf = categoryOf or CA.DefaultUnitCategory

	for i = #base.produced, 1, -1 do
		local unit = base.produced[i]
		if not CA.IsAlive(unit) or unit.Owner ~= base.player or not unit.IsInWorld then
			table.remove(base.produced, i)
		end
	end

	local total, perCategory = 0, { infantry = 0, vehicle = 0, aircraft = 0 }
	for _, unit in ipairs(base.produced) do
		total = total + 1
		local category = categoryOf(unit) or "vehicle"
		perCategory[category] = (perCategory[category] or 0) + 1
	end
	return total, perCategory
end



-- One Orca per helipad, matching the original AI. Nod13c, Deceit and
-- Hostile Takeover each had their own copy of this logic.
--
-- options: reserve (cash kept free, default 800), busySeconds (default 240),
--          pads (explicit list; default: local helipads of the base)
function CA.ProduceOrcaPerPad(base, context, options)
	options = options or {}
	if base == nil or base.orcaBusy ~= nil then return false end

	local radius = options.radius or base.baseRadius or 24
	local pads = options.pads
	local padCount = 0
	local hpad = nil
	if pads ~= nil then
		for _, pad in ipairs(pads) do
			if CA.IsLive(pad) then
				padCount = padCount + 1
				hpad = hpad or pad
			end
		end
	else
		for _, pad in ipairs(CA.GetActorsByType(base.player, "hpad")) do
			if CA.IsLive(pad) and CA.IsLocal(base, pad, radius) then
				padCount = padCount + 1
				hpad = hpad or pad
			end
		end
	end
	if padCount == 0 or hpad == nil then return false end
	if hpad.IsProducing("orca") then return false end

	local orcas = 0
	for _, orca in ipairs(CA.GetActorsByType(base.player, "orca")) do
		if CA.IsLive(orca) then orcas = orcas + 1 end
	end
	if orcas >= padCount then return false end
	if CA.Funds(base.player) < Actor.Cost("orca") + (options.reserve or 800) then return false end

	if not CA.QueueUnit(hpad, "orca", base.player) then return false end

	base.orcaBusy = hpad
	Trigger.AfterDelay(DateTime.Seconds(options.busySeconds or 240), function()
		if base.orcaBusy == hpad then base.orcaBusy = nil end
	end)
	return true
end

local orcaHandled = {}
local retreatingOrcas = {}

function CA.GuardOrca(base, orca, context, attack)
	if base == nil or not CA.IsLive(orca) or orca.Type ~= "orca" then return end
	local key = CA.ActorKey(orca)
	if orcaHandled[key] == nil then
		-- Store the actor, not just a flag, so dead Orcas can be pruned.
		orcaHandled[key] = orca
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

	retreatingOrcas[key] = orca
	if orca.HasProperty("Stop") then orca.Stop() end
	if orca.HasProperty("ReturnToBase") then orca.ReturnToBase() end
end

function CA.OrcaWatch(bases, context, interval)
	interval = interval or 5

	local function tick()
		-- Drop bookkeeping for destroyed Orcas so the tables do not grow for
		-- the whole mission.
		for key, orca in pairs(orcaHandled) do
			if not CA.IsLive(orca) then orcaHandled[key] = nil end
		end
		for key, orca in pairs(retreatingOrcas) do
			if not CA.IsLive(orca) then retreatingOrcas[key] = nil end
		end

		for _, base in ipairs(bases) do
			local pad = CA.LocalBuilding(base, "hpad", base.baseRadius)
			for _, orca in ipairs(CA.GetActorsByType(base.player, "orca")) do
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

	CA.RepeatingSafeTick("orca-watch", interval, tick, context and context.isActive, context and context.isFinished)()
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

	for _, orca in ipairs(CA.GetActorsByType(base.player, "orca")) do
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
		if context.isFinished ~= nil and context.isFinished() then return end
		base.defending = false
		base.allOut = false
		if context.isActive == nil or context.isActive() then
			-- Survivors were removed from reserve/produced when the all-out
			-- started. Re-seed so a later TakeForAttack can commit them again.
			CA.SeedPool(base, context)
		end
	end)
end

-- ---------------------------------------------------------------- unit defence

-- `Defend` does not chase in OpenRA, and CA.DefendAgainst deliberately skips
-- the damaged actor itself. A lone picket (a tank on a bridge, a guard tower
-- post without neighbours) can therefore be outranged forever without ever
-- shooting back. This helper sends the damaged unit at its attacker once.
--
-- Enable per context with `selfDefend = true`; it runs from the damage hook.
function CA.SelfDefend(unit, attacker, context)
	if context ~= nil and context.isActive ~= nil and not context.isActive() then return end
	if not CA.IsLive(unit) or not CA.IsLive(attacker) then return end
	if not unit.HasProperty("Location") or not unit.HasProperty("AttackMove") then return end
	if not attacker.HasProperty("Location") then return end
	if not CA.SameLand(unit.Location, attacker.Location) then return end
	if unit.HasProperty("Stop") then unit.Stop() end
	unit.AttackMove(attacker.Location, 2)
end

-- Convenience context for a unit pool that only needs "shoot back":
-- CA.UnitContext(base, enemy) plus optional overrides.
function CA.UnitContext(base, enemy, overrides)
	local context = {
		enemy = enemy,
		players = { base.player },
		base = base,
		isActive = function() return true end,
		keepAttackers = true,
		repairThreshold = 0.7,
		-- DefendAgainst calls CA.SelfDefend for non-buildings; no onDamage
		-- hook is needed, which avoids issuing the same order twice.
		selfDefend = true,
	}

	for key, value in pairs(overrides or {}) do
		context[key] = value
	end
	return context
end

-- Register a mobile unit for damage response and self-defence. Buildings use
-- CA.RegisterDamageHandler; units need the same handler, and without this a
-- produced or scripted unit never reacts at all.
function CA.RegisterUnit(unit, context)
	if unit == nil or context == nil then return end
	if not unit.HasProperty("Move") or unit.Type == "harv" or unit.Type == "mcv" then return end
	CA.RegisterDamageHandler(unit, context)
end

-- ---------------------------------------------------------------- capture

-- Engineers are consumed by a capture, so a group of engineers must spread
-- across different buildings. Sending them all to the nearest target leaves
-- the first capture to one engineer and the rest standing with a dead order
-- (this happened in Deceit and Hostile Takeover).
--
-- targets: iterable any (towns, Nod.GetActors()) or an explicit list
function CA.CaptureBuildingsFor(unit, enemy, context)
	local candidates = {}
	for _, actor in ipairs(enemy.GetActors()) do
		if CA.IsLive(actor) and actor.HasProperty("Location")
			and actor.HasProperty("StartBuildingRepairs")
			and CA.TargetOnLand(unit.Location, actor) then
			table.insert(candidates, actor)
		end
	end

	table.sort(candidates, function(a, b)
		local da = (a.Location.X - unit.Location.X) ^ 2 + (a.Location.Y - unit.Location.Y) ^ 2
		local db = (b.Location.X - unit.Location.X) ^ 2 + (b.Location.Y - unit.Location.Y) ^ 2
		if da ~= db then return da < db end
		return a.ActorID < b.ActorID
	end)
	return candidates
end

-- Assign engineers to buildings, one claim per engineer. `claims` must be a
-- table shared by the whole drop (mission-owned) so parallel engineers see
-- each other's reservations.
--
-- options: claims, context, retrySeconds, onNoTarget
function CA.CaptureWithEngineers(units, enemy, options)
	options = options or {}
	local claims = options.claims or {}
	local context = options.context

	local function pick(unit)
		for _, actor in ipairs(CA.CaptureBuildingsFor(unit, enemy, context)) do
			local claim = claims[CA.ActorKey(actor)]
			if claim == nil or not CA.IsAlive(claim) then
				return actor
			end
		end
		return nil
	end

	local function order(unit)
		if context ~= nil and context.isActive ~= nil and not context.isActive() then return end
		if not CA.IsAlive(unit) then return end

		local target = pick(unit)
		if target == nil then
			if options.onNoTarget ~= nil then options.onNoTarget(unit) end
			return
		end

		local key = CA.ActorKey(target)
		claims[key] = unit
		unit.Capture(target)

		Trigger.AfterDelay(DateTime.Seconds(options.retrySeconds or 10), function()
			if claims[key] == unit then claims[key] = nil end
			if not CA.IsAlive(unit) or not unit.IsIdle then return end
			if CA.IsLive(target) and target.Owner == enemy then
				claims[key] = unit
				unit.Capture(target)
				return
			end
			order(unit)
		end)
	end

	for _, unit in ipairs(units or {}) do
		order(unit)
	end
end

-- Send the local garrison at the attacker. CA.StartBaseDefense/AllOutAttack
-- order "Attack Base", which marches the units to the nearest enemy building
-- instead of the thing that is actually shooting. Missions that want a real
-- response use this instead.
--
-- options: alarmSeconds (default 45), releaseOrcas (default true),
--          isFinished
function CA.DefendAgainstAttacker(base, attacker, context, options)
	options = options or {}
	if base == nil or base.allOut then return false end
	if not CA.IsLive(attacker) or not attacker.HasProperty("Location") then return false end

	base.allOut = true
	base.underAttackUntil = DateTime.GameTime + DateTime.Seconds(options.alarmSeconds or 45)

	local team = CA.GetCombatUnits(base, context)
	base.reserve = {}
	base.produced = {}

	if #team > 0 then
		CA.SendTeam(team, base.player, { { Location = attacker.Location } }, true, false, context)
	end

	if options.releaseOrcas ~= false then
		for _, orca in ipairs(CA.GetActorsByType(base.player, "orca")) do
			if CA.IsLive(orca) and orca.HasProperty("Stance") then
				orca.Stance = "AttackAnything"
				IdleHunt(orca)
			end
		end
	end

	local finished = options.isFinished or function() return false end
	Trigger.AfterDelay(DateTime.Seconds(options.alarmSeconds or 45), function()
		if base == nil then return end
		base.allOut = false
		if not finished() then CA.SeedPool(base, context) end
	end)
	return true
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
		for _, orca in ipairs(CA.GetActorsByType(context.base.player, "orca")) do
			if CA.IsLive(orca) and orca.IsInWorld and orca.HasProperty("Stance") then
				orca.Stance = "AttackAnything"
				IdleHunt(orca)
			end
		end
	end

	-- A damaged mobile unit shoots back at its attacker (see CA.SelfDefend).
	-- Buildings are handled by the repair/all-out logic above.
	if not building and context.selfDefend then
		CA.SelfDefend(actor, attacker, context)
	end

	-- Ground units cannot engage an aircraft by walking to its cell. Leave
	-- air attackers to the Orcas and anti-air towers instead of pulling the
	-- garrison underneath them.
	local attackerIsAir = attacker.HasProperty("Land")

	local radius = building and 18 or 10
	if attackerIsAir then
		return
	end

	for _, player in ipairs(context.players or {}) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if CA.IsLive(unit) and unit.IsInWorld and unit ~= actor
				and unit.HasProperty("AttackMove") and unit.HasProperty("Location") and unit.Owner == player
				and unit.Type ~= "harv" and unit.Type ~= "mcv"
				and (context.exclude == nil or not context.exclude(unit))
				and (not context.keepAttackers or not unit.HasProperty("Stance") or unit.Stance ~= "AttackAnything")
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

	CA.RepeatingSafeTick("idle-defense", interval, tick, context.isActive, context.isFinished)()
end

function CA.StartIdleDefense(context, interval)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		CA.SafeCall("idle-defense-start", function()
			CA.IdleDefense(context, interval)
		end)
	end)
end

-- Bring idle home-guard units back once they wandered outside the base radius
-- (for example after chasing an attacker). Only units still tracked in
-- reserve are recalled, so an assault group is never pulled back.
function CA.RecallReserve(base, radius)
	if base == nil or base.player == nil then return end
	local yard = yardCell(base)
	if yard == nil then return end

	CA.PruneReserve(base)
	radius = radius or base.baseRadius or 32
	for _, unit in ipairs(base.reserve) do
		if CA.IsLive(unit) and unit.HasProperty("Move") and unit.HasProperty("Location")
			and unit.IsIdle then
			local dx, dy = unit.Location.X - yard.X, unit.Location.Y - yard.Y
			if dx * dx + dy * dy > radius * radius then
				unit.Move(CPos.New(yard.X, yard.Y))
			end
		end
	end
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
	for _, harv in ipairs(CA.GetActorsByType(base.player, base.harvesterKind or "harv")) do
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

-- Shared maintenance pass for the income chain: adopt free harvesters from
-- rebuilt refineries, refresh claims, restart idle collectors and run the
-- watchdog. Returns false when there is no refinery to support harvesters.
function CA.MaintainHarvesters(base, config)
	if base == nil or base.player == nil then return false end
	config = config or {}
	local refineryKind = config.refineryKind or base.refineryKind or "proc"
	if #CA.GetActorsByType(base.player, refineryKind) == 0 then
		return false
	end

	CA.AdoptFreeHarvesters(base)
	CA.RefreshHarvesterClaims(base, config)
	CA.RestartIdleHarvesters(base)
	CA.UpdateIncomeWatchdog(base)
	return true
end

-- Build one replacement harvester when the local refineries are understaffed.
-- config.canBuild(kind) lets the mission apply its tech prerequisites, and
-- config.harvesterCost/reserve keep the spend in line with the mission.
function CA.ReplaceHarvester(base, config)
	if base == nil or base.player == nil then return false end
	config = config or {}
	if CA.NeededHarvesters(base, config) <= 0 then return false end

	local factoryKind = config.harvesterProductionKind or base.harvesterProductionKind or "weap"
	local factory = CA.LocalBuilding(base, factoryKind, base.baseRadius)
	if factory == nil then return false end

	local harvKind = config.harvesterKind or base.harvesterKind or "harv"
	if config.canBuild ~= nil and not config.canBuild(harvKind) then return false end
	if factory.HasProperty("IsProducing") and factory.IsProducing(harvKind) then return false end

	local cost = config.harvesterCost or 1100
	if CA.Funds(base.player) < cost + (optionValue(config.reserve) or 0) then return false end

	return CA.QueueUnit(factory, harvKind, base.player)
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
	for _, proc in ipairs(CA.GetActorsByType(base.player, refineryKind)) do
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
	if #CA.GetActorsByType(base.player, base.refineryKind or "proc") == 0 then
		base.incomeSuspect = false
		return false
	end

	local window = base.incomeWindow or 120
	if DateTime.GameTime - (base.lastIncomeTime or 0) > DateTime.Seconds(window) then
		-- Spending can keep the cash total flat even while harvesters are
		-- delivering. Only flag a stalled economy when a harvester is
		-- actually idle; otherwise the watchdog would fight normal
		-- production and rebuild spending.
		local idle = false
		for _, harv in pairs(base.harvesters) do
			if CA.IsLive(harv) and harv.IsIdle then
				idle = true
				break
			end
		end

		if idle then
			base.incomeSuspect = true
			CA.RestartIdleHarvesters(base)
		end
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
	-- SpendFunds drains Resources first and falls back to Cash. Refunding to
	-- Cash keeps the full amount even when the resource storage is nearly
	-- full; refunding to Resources could be clamped by ResourceCapacity.
	player.Cash = (player.Cash or 0) + amount
end

function CA.IsStorageFull(player)
	if player == nil or (player.ResourceCapacity or 0) <= 0 then return false end
	return (player.Resources or 0) >= player.ResourceCapacity
end

function CA.IsStorageNearlyFull(player, reserve)
	if player == nil or (player.ResourceCapacity or 0) <= 0 then return false end
	return (player.Resources or 0) >= player.ResourceCapacity - (reserve or 500)
end
