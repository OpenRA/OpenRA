--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EC: GDI owns three bases. The original mission has one GDI house; the
-- port splits it into GDI (northern base and central outpost), GDIWest
-- (south-western base with the EYE and helipads) and GDISouth (the small
-- south-eastern construction-yard outpost). GDI and GDIWest send each other
-- relief when a key building is lost; GDISouth stays isolated. Nod starts on
-- the northern peninsula and has to reach the mainland with a captured
-- transport or the second MCV that arrives in the west. The scripted
-- airstrikes are stopped by destroying or capturing the northern base's
-- Communications Center. The south-western base's Advanced Communications
-- Center (EYE) controls the Ion Cannon and the western relief force;
-- destroying or capturing it stops both.
--
-- Design note: the broad base alarm (any repairable building hit) and the
-- broad harvester defense (all nearby combat units react) are deliberate.
-- They are the preferred behaviour of the nod13b backport; see
-- HANDOFF-NOD13B.md and HANDOFF-NOD13C.md.

local difficulty
local land = {}
local bases = {}
AllOutAttackStarted = false
BaseAlarmStarted = false
local gdiPlayers = {}
local guarded = {}
local trackedHarvesters = {}
local retreatingOrcas = {}
local orcaBase = {}
local orcaHandled = {}
local defenseResponses = {}
-- Forward declarations for the economy recovery machinery. Lua resolves these
-- locals as upvalues in functions that are defined before the economy block.
local RebuildHarvester
local Queue
local RequestRelief
local EconomyTick
local ClaimHarvester
local UnclaimHarvester
local LiveHarvesterCount
local HarvesterCount
local AdoptFreeHarvester
local Rebuild
local Produce
local EnsureHelipad
local ProduceOrca
local StartBuilding
local CanRepairBuilding
local prerequisites

-- Each base builds and rebuilds on its own. GDI receives the port's normal
-- starting funds, split between GDI and GDIWest; GDISouth stays at zero.
local MaxProducedUnits = { easy = 5, normal = 7, hard = 9 }
local DefenderReserve = { easy = 2, normal = 3, hard = 4 }
local StartingCash = { easy = 8000, normal = 10000, hard = 12000 }
local ReliefSize = { easy = 2, normal = 3, hard = 4 }
local ReliefCooldown = { easy = 150, normal = 110, hard = 80 }
local BaseReliefBuildings = { fact = true, proc = true, weap = true, pyle = true,
	hq = true, eye = true, nuke = true, nuk2 = true, hpad = true, atwr = true, gtwr = true }
local BaseDefenseBuildings = { fact = true, proc = true, weap = true, pyle = true, hq = true, eye = true }
local BaseDefenseAlarmSeconds = 45
-- All-out is a reaction, not a permanent state: after this long without
-- further building damage the base stands down and units garrison again.
local AllOutStandDownSeconds = 60
-- Sales pause while the base is being hit; towers are defence, not funding.
local UnderAttackSellPauseSeconds = 15
-- After losing the construction yard, give the harvester time to earn the
-- MCV before liquidating anything (the WEAP stays protected regardless).
local HarvesterEarnWindowSeconds = 180
-- If power is low again within this window after adding a plant, the next
-- capacity step uses the bigger NUK2 instead of another small NUKE.
local PowerOscillationSeconds = 300
-- Original SCB13EC trigger times (tenths of a minute, multiplied by six):
-- atk1-atk7 at 26.0-40.0 min, A10 strikes every 8 min from 8 min.
local WaveStart = { easy = 1872, normal = 1560, hard = 1326 }
local AirstrikeFirstDelay = { easy = 576, normal = 480, hard = 408 }
local AirstrikeInterval = { easy = 576, normal = 480, hard = 408 }
local AutoWaveInterval = { easy = 240, normal = 180, hard = 120 }
local AutoWaveStart = { easy = 1800, normal = 1500, hard = 1275 }
-- Ion Cannon timings match nod13b: first strike at 40/32/25 minutes
-- (easy/normal/hard), then every 10/8/6 minutes.
local IonCannonFirstDelay = { easy = 2400, normal = 1920, hard = 1500 }
local IonCannonInterval = { easy = 600, normal = 480, hard = 360 }

local passable = { Clear = true, Rough = true, Road = true, Bridge = true,
	Tiberium = true, BlueTiberium = true, Beach = true }

local function IsLive(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

-- Actor values are not guaranteed to have stable Lua table identity across
-- engine callbacks. Use the stable ActorID for sets/maps. The harness (and
-- other callers without the property) falls back to the table itself.
function EconomyActorKey(actor)
	if actor == nil then return nil end
	local id = actor.ActorID
	if id ~= nil then return id end
	return actor
end

local function IsGDIPlayer(player)
	for _, p in ipairs(gdiPlayers) do
		if p == player then return true end
	end
	return false
end

local function AnyGDIPlayer()
	for _, p in ipairs(gdiPlayers) do
		if p ~= nil and not HouseEliminated(p) then return p end
	end
	return nil
end

local function CellKey(x, y)
	return y * 64 + x
end

local function BuildLandRegions()
	local nextRegion = 0
	for y = 1, 62 do
		for x = 3, 62 do
			local key = CellKey(x, y)
			if land[key] == nil then
				if not passable[Map.TerrainType(CPos.New(x, y))] then
					land[key] = 0
				else
					nextRegion = nextRegion + 1
					land[key] = nextRegion
					local queue, index = { { x, y } }, 1
					while index <= #queue do
						local cell = queue[index]
						index = index + 1
						for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 },
							{ 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }) do
							local nx, ny = cell[1] + d[1], cell[2] + d[2]
							local nk = CellKey(nx, ny)
							if nx >= 3 and nx <= 62 and ny >= 1 and ny <= 62 and land[nk] == nil then
								if passable[Map.TerrainType(CPos.New(nx, ny))] then
									land[nk] = nextRegion
									table.insert(queue, { nx, ny })
								else
									land[nk] = 0
								end
							end
						end
					end
				end
			end
		end
	end
end

local function Region(cell)
	if cell.X < 3 or cell.X > 62 or cell.Y < 1 or cell.Y > 62 then return 0 end
	return land[CellKey(cell.X, cell.Y)] or 0
end

local function SameLand(a, b)
	local region = Region(a)
	return region ~= 0 and region == Region(b)
end

-- The start peninsula and the small church island are separate land regions
-- from the mainland. Mission logic uses this to tell whether a Nod base has
-- reached the mainland at all.
local StartCell = CPos.New(15, 4)
local ChurchIslandCell = CPos.New(6, 18)

function IsStartingLand(cell)
	return SameLand(StartCell, cell) or SameLand(ChurchIslandCell, cell)
end

-- The Communications Center outpost (HQ 22,26) is a small static guard
-- position, not a fourth production base. Keep it out of the global GDI
-- mutual-help/relief/Orca machinery so attacking its towers does not summon
-- the whole map; its own towers and nearby units still defend locally.
local CentralOutpostCell = CPos.New(22, 26)
local CentralOutpostRadius = 14

local function IsCentralOutpostActor(actor)
	if not actor.HasProperty("Location") then return false end
	local dx = actor.Location.X - CentralOutpostCell.X
	local dy = actor.Location.Y - CentralOutpostCell.Y
	return dx * dx + dy * dy <= CentralOutpostRadius * CentralOutpostRadius
end

local function ActorRegion(actor)
	local center = CPos.New(math.floor(actor.CenterPosition.X / 1024), math.floor(actor.CenterPosition.Y / 1024))
	local region = Region(center)
	if region == 0 then region = Region(actor.Location) end
	return region
end

-- GDI waves only start once Nod has a real mainland foothold. The starting
-- peninsula and the church island do not count; four completed Nod
-- buildings on the big island unlock patrols, APC counter-attacks, the
-- scripted atk waves and the Autocreate waves.
local WaveBuildingThreshold = 4

-- The waves come from the north-eastern GDI base and are tied to its
-- Communications Center (GDINorthHQ, 31,7). Destroying or capturing that
-- building stops future waves; already-spawned teams keep fighting.
local WaveSpawnCell = CPos.New(62, 5)
local WaveSpawn = { Location = WaveSpawnCell }

local function WaveSourceAlive()
	return IsLive(GDINorthHQ) and GDINorthHQ.IsInWorld and IsGDIPlayer(GDINorthHQ.Owner)
end

local function WaveOwner()
	if WaveSourceAlive() then return GDI end
	return nil
end

local function MainlandBuildingCount()
	local startRegion, churchRegion = Region(StartCell), Region(ChurchIslandCell)
	local count = 0
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("StartBuildingRepairs") then
			local region = ActorRegion(actor)
			if region ~= 0 and region ~= startRegion and region ~= churchRegion then
				count = count + 1
			end
		end
	end
	return count
end

local function WavesUnlocked()
	return MainlandBuildingCount() >= WaveBuildingThreshold
end

local function RunWhenUnlocked(fn)
	if MissionFinished or not WaveSourceAlive() then return end
	if WavesUnlocked() then
		fn()
		return
	end
	Trigger.AfterDelay(DateTime.Seconds(30), function() RunWhenUnlocked(fn) end)
end

local function TargetOnLand(origin, target)
	if Region(target.Location) ~= 0 then return SameLand(origin, target.Location) end
	for dx = -1, 3 do
		for dy = -1, 3 do
			if SameLand(origin, CPos.New(target.Location.X + dx, target.Location.Y + dy)) then return true end
		end
	end
	return false
end

local function IsTerrainObstacle(actor)
	return actor.Type == "brik" or actor.Type == "cycl" or actor.Type:match("^rock%d") ~= nil
		or actor.Type:match("^t%d%d") ~= nil or actor.Type:lower():match("^split") ~= nil
		or actor.Type:match("%.husk$") ~= nil
end

local function GDIFunds(player)
	return player.Cash + player.Resources
end

function EconomyResourceCapacity(player)
	return player.ResourceCapacity or 0
end

-- Storage can be full while the harvesters keep delivering. In that state
-- Cash + Resources stays flat, which must not be mistaken for no income.
function EconomyIsStorageFull(player)
	local capacity = EconomyResourceCapacity(player)
	return capacity > 0 and (player.Resources or 0) >= capacity
end

function EconomyIsStorageNearlyFull(player)
	local capacity = EconomyResourceCapacity(player)
	return capacity > 0 and (player.Resources or 0) >= math.max(0, capacity - 500)
end

local function SpendGDIFunds(player, amount)
	local resources = math.min(player.Resources, amount)
	player.Resources = player.Resources - resources
	player.Cash = player.Cash - amount + resources
	return resources
end

local function IsLocal(base, actor)
	if not actor.HasProperty("Location") then return false end
	local dx, dy = actor.Location.X - base.yard[1], actor.Location.Y - base.yard[2]
	return dx * dx + dy * dy <= 32 * 32
end

-- All live, in-world actors of a type that belong to a base's economy.
-- The central Communications Center outpost is deliberately excluded: the
-- base-local radius of the northern base also covers it, but it is a static
-- guard position, not part of the base's economy or tech chain.
local function GetLocalActors(base, kind)
	local out = {}
	for _, actor in ipairs(base.player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.Owner == base.player and actor.IsInWorld
			and IsLocal(base, actor) and not IsCentralOutpostActor(actor) then
			out[#out + 1] = actor
		end
	end
	return out
end

local function BaseAt(cell)
	for _, base in ipairs(bases) do
		local dx, dy = cell.X - base.yard[1], cell.Y - base.yard[2]
		if dx * dx + dy * dy <= 24 * 24 then return base end
	end
	return nil
end

local function LocalBuilding(base, kind)
	local best, bestDistance
	for _, actor in ipairs(GetLocalActors(base, kind)) do
		local dx, dy = actor.Location.X - base.yard[1], actor.Location.Y - base.yard[2]
		local distance = dx * dx + dy * dy
		if bestDistance == nil or distance < bestDistance then
			best, bestDistance = actor, distance
		end
	end
	return best
end

local function LocalTech(base, kind)
	if kind == "proc" or kind == "pyle" then
		return LocalBuilding(base, "nuke") ~= nil or LocalBuilding(base, "nuk2") ~= nil
	elseif kind == "weap" or kind == "hq" or kind == "silo" or kind == "harv" then
		return LocalBuilding(base, "proc") ~= nil
	elseif kind == "gtwr" or kind == "e1" or kind == "e3" then
		return LocalBuilding(base, "pyle") ~= nil
	elseif kind == "atwr" or kind == "nuk2" or kind == "eye" or kind == "e2" or kind == "mtnk" or kind == "msam" then
		return LocalBuilding(base, "hq") ~= nil or LocalBuilding(base, "eye") ~= nil
	elseif kind == "htnk" then
		return LocalBuilding(base, "eye") ~= nil or LocalBuilding(base, "hq") ~= nil
	elseif kind == "hpad" then
		return LocalBuilding(base, "proc") ~= nil
	elseif kind == "orca" then
		return LocalBuilding(base, "hpad") ~= nil
			and (LocalBuilding(base, "hq") ~= nil or LocalBuilding(base, "eye") ~= nil)
	end
	return true
end

local function BaseForPlayer(player)
	for _, base in ipairs(bases) do
		if base.player == player then return base end
	end
	return nil
end

-- Multi-base: pick the base of the actor's owner that is closest to the
-- actor. Never fall back to the first base of the player; that silently
-- mixes bases when a player owns more than one.
local function BaseForActor(actor)
	if actor == nil or not actor.HasProperty("Location") then return nil end
	local best, bestDistance
	for _, base in ipairs(bases) do
		if base.player == actor.Owner then
			local dx = actor.Location.X - base.yard[1]
			local dy = actor.Location.Y - base.yard[2]
			local distance = dx * dx + dy * dy
			if best == nil or distance < bestDistance then
				best, bestDistance = base, distance
			end
		end
	end
	return best
end

-- The base that owns the building which produced/placed an actor. Used for
-- harvester claims (WEAP queue / direct PROC placement).
local function BaseForOwnedBuilding(actor)
	if actor == nil then return nil end
	for _, base in ipairs(bases) do
		if base.player == actor.Owner then
			for _, candidate in ipairs(GetLocalActors(base, actor.Type)) do
				if candidate == actor then return base end
			end
		end
	end
	return BaseForActor(actor)
end

local function BaseForHarvester(harv)
	local best, bestDistance
	for _, base in ipairs(bases) do
		-- Only refineries can use a harvester. Never fall back to a bare
		-- construction yard: GDISouth has a FACT but no economy, and its
		-- proximity stole the western base's starting harvester.
		for _, anchor in ipairs(GetLocalActors(base, "proc")) do
			local dx = harv.Location.X - anchor.Location.X
			local dy = harv.Location.Y - anchor.Location.Y
			local distance = dx * dx + dy * dy
			if best == nil or distance < bestDistance then
				best, bestDistance = base, distance
			end
		end
	end
	return best
end

local function PruneReserve(base)
	for i = #base.reserve, 1, -1 do
		local unit = base.reserve[i]
		if not IsLive(unit) or unit.Owner ~= base.player then table.remove(base.reserve, i) end
	end
end

local function ProducedCounts(base)
	for i = #base.produced, 1, -1 do
		local unit = base.produced[i]
		if not IsLive(unit) or unit.Owner ~= base.player then table.remove(base.produced, i) end
	end

	local total, infantryCount, vehicleCount = 0, 0, 0
	for _, unit in ipairs(base.produced) do
		total = total + 1
		if unit.Type == "jeep" or unit.Type == "mtnk" or unit.Type == "msam" or unit.Type == "htnk" then
			vehicleCount = vehicleCount + 1
		else
			infantryCount = infantryCount + 1
		end
	end
	return total, infantryCount, vehicleCount
end

local function RemoveProduced(base, team)
	for _, unit in ipairs(team) do
		for i = #base.produced, 1, -1 do
			if base.produced[i] == unit then table.remove(base.produced, i) end
		end
	end
end

local function ClosestEnemy(unit, maxDistance, capture)
	local target, distance
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land")
			and TargetOnLand(unit.Location, actor)
			and (not capture or unit.CanCapture(actor)) then
			local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if (maxDistance == nil or d <= maxDistance * maxDistance) and (distance == nil or d < distance) then
				target, distance = actor, d
			end
		end
	end
	return target
end

local function OrderAttack(unit)
	if unit.HasProperty("AttackMove") then
		local target = ClosestEnemy(unit)
		if target ~= nil then unit.AttackMove(target.Location, 2) end
	elseif unit.HasProperty("Capture") and unit.HasProperty("CanCapture") then
		local target = ClosestEnemy(unit, nil, true)
		if target ~= nil then
			unit.Capture(target)
		elseif unit.HasProperty("Move") then
			local enemy = ClosestEnemy(unit)
			if enemy ~= nil then unit.Move(enemy.Location) end
		end
	end
end

local function ClosestBuilding(unit)
	local target, distance
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
			and actor.HasProperty("StartBuildingRepairs")
			and TargetOnLand(unit.Location, actor) then
			local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if distance == nil or d < distance then
				target, distance = actor, d
			end
		end
	end
	return target
end

-- Original atk1..atk7 and apc2 use "Attack Base". Prefer Nod buildings and
-- only fall back to the nearest Nod unit when no building is reachable.
local function OrderAttackBase(unit)
	if not unit.HasProperty("AttackMove") then return end
	local target = ClosestBuilding(unit)
	if target ~= nil then unit.AttackMove(target.Location, 2) else OrderAttack(unit) end
end

local function WatchAttackers(team, owner, baseAttack)
	if MissionFinished then return end
	local alive = false
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner then
			alive = true
			if unit.IsInWorld and unit.IsIdle then
				if baseAttack then OrderAttackBase(unit) else OrderAttack(unit) end
			end
		end
	end
	if alive then
		Trigger.AfterDelay(DateTime.Seconds(10), function() WatchAttackers(team, owner, baseAttack) end)
	end
end

local function SendTeam(team, owner, route, attack, baseAttack)
	if MissionFinished then return end
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner and unit.IsInWorld then
			if unit.HasProperty("Stance") then unit.Stance = attack and "AttackAnything" or "Defend" end
			for _, waypoint in ipairs(route) do
				if SameLand(unit.Location, waypoint.Location) then
					if unit.HasProperty("AttackMove") then unit.AttackMove(waypoint.Location)
					elseif unit.HasProperty("Move") then unit.Move(waypoint.Location) end
				end
			end
			if attack and #route == 0 then
				if baseAttack then OrderAttackBase(unit) else OrderAttack(unit) end
			end
		end
	end
	if attack and #team > 0 then WatchAttackers(team, owner, baseAttack) end
end

local function TakeForAttack(base, limit)
	local team = {}
	local taken = {}
	PruneReserve(base)
	for i = #base.reserve, 1, -1 do
		if #team >= limit then break end
		local unit = base.reserve[i]
		if IsLive(unit) and unit.Owner == base.player and unit.IsInWorld and not taken[unit] then
			table.remove(base.reserve, i)
			table.insert(team, unit)
			taken[unit] = true
		end
	end
	for _, unit in ipairs(base.produced) do
		if #team >= limit then break end
		if IsLive(unit) and unit.Owner == base.player and unit.IsInWorld and not taken[unit] then
			table.insert(team, unit)
			taken[unit] = true
		end
	end
	-- Offensive units no longer count against the production cap; the freed
	-- slot is used for the next garrison unit.
	RemoveProduced(base, team)
	return team
end

local function JoinGarrison(base, unit)
	if not IsLive(unit) or unit.Owner ~= base.player then return end
	-- Never trickle newly produced units into the enemy one by one during an
	-- all-out/alarm. The initial AllOutAttack already sent the standing army;
	-- replacements stay home as garrison so production stops at the cap and
	-- the base does not endlessly burn money on suicide attacks.
	PruneReserve(base)
	if #base.reserve < DefenderReserve[difficulty] then table.insert(base.reserve, unit) end
	if unit.HasProperty("Stance") then unit.Stance = "Defend" end
end

local function FindUnitSpot(anchor, used, maxRadius)
	maxRadius = maxRadius or 12
	for radius = 0, maxRadius do
		for dx = -radius, radius do
			for dy = -radius, radius do
				if math.abs(dx) == radius or math.abs(dy) == radius then
					local cell = CPos.New(anchor.X + dx, anchor.Y + dy)
					local key = cell.X * 1000 + cell.Y
					if cell.X >= 3 and cell.X <= 62 and cell.Y >= 1 and cell.Y <= 62
						and SameLand(anchor, cell) and (used == nil or not used[key]) then
						local terrain = Map.TerrainType(cell)
						if passable[terrain] then
							-- Two cells catch building footprints as well as trees and
							-- rocks, so a rebuilt harvester is not placed inside its
							-- own refinery.
							local blockers = Map.ActorsInCircle(Map.CenterOfCell(cell), WDist.New(2048), function(actor)
								return IsLive(actor) and actor.HasProperty("Location")
									and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
							end)
							if #blockers == 0 then
								if used ~= nil then used[key] = true end
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

-- A base that loses a key building or its harvester asks the other base for
-- help. A scratch does not trigger this; relief also leaves a home guard.
RequestRelief = function(base)
	if MissionFinished or base == nil or HouseEliminated(base.player) then return end
	if DateTime.GameTime < (base.reliefCooldown or 0) then return end
	base.reliefCooldown = DateTime.GameTime + DateTime.Seconds(ReliefCooldown[difficulty])
	for _, helper in ipairs(bases) do
		if helper ~= base and not HouseEliminated(helper.player)
			and helper.canRelieve ~= false
			and not (helper.player == GDIWest and WestReinforcementsStopped) then
			local available = #helper.reserve + #helper.produced
			local keep = math.min(DefenderReserve[difficulty], available)
			local takeLimit = math.min(ReliefSize[difficulty], math.max(0, available - keep))
			local team = TakeForAttack(helper, takeLimit)
			local taken = {}
			for _, unit in ipairs(team) do taken[unit] = true end
			local tracked = {}
			for _, unit in ipairs(helper.reserve) do tracked[unit] = true end
			for _, unit in ipairs(helper.produced) do tracked[unit] = true end
			-- Early in the mission most of a base's force is still placed on
			-- the map, so allow idle Defend units to join the relief force.
			-- Units still tracked by the base must stay in its production pool.
			for _, unit in ipairs(helper.player.GetGroundAttackers()) do
				if #team >= ReliefSize[difficulty] then break end
				if not taken[unit] and not tracked[unit] and IsLive(unit) and unit.IsInWorld
					and unit.Type ~= "harv"
					and not IsCentralOutpostActor(unit)
					and unit.HasProperty("Stance") and unit.Stance == "Defend" and IsLocal(helper, unit)
					and SameLand(unit.Location, CPos.New(base.yard[1], base.yard[2])) then
					table.insert(team, unit)
					taken[unit] = true
				end
			end
			if #team > 0 then
				local cell = FindUnitSpot(CPos.New(base.yard[1], base.yard[2]))
				if cell == nil then cell = CPos.New(base.yard[1], base.yard[2]) end
				SendTeam(team, helper.player, { { Location = cell } }, false)
			end
		end
	end
end

-- Easy/Normal: a hit on a key building commits the attacked base. Every
-- ground unit (except harvesters/MCVs and the isolated central outpost) and
-- every ready Orca attacks, and newly produced combat units join in.
-- Hard keeps the broad but limited nod13b alarm instead.
local function AllOutAttack(base, target)
	if MissionFinished then return end
	local anchor = CPos.New(base.yard[1], base.yard[2])
	local team = {}
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if IsLive(unit) and unit.IsInWorld and unit.Type ~= "harv" and unit.Type ~= "mcv"
			and not IsCentralOutpostActor(unit) and SameLand(unit.Location, anchor) then
			table.insert(team, unit)
		end
	end
	base.reserve = {}
	base.produced = {}
	base.allOut = true
	AllOutAttackStarted = true

	local route = {}
	if target ~= nil and IsLive(target) and target.HasProperty("Location")
		and SameLand(anchor, target.Location) then
		route = { { Location = target.Location } }
	end

	for _, orca in ipairs(base.player.GetActorsByType("orca")) do
		local orcaKey = EconomyActorKey(orca)
		if IsLive(orca) and orca.IsInWorld and not retreatingOrcas[orcaKey] then
			if orca.HasProperty("Stance") then orca.Stance = "AttackAnything" end
			if orca.HasProperty("Stop") then orca.Stop() end
			if orca.HasProperty("AttackMove") and #route > 0 then
				orca.AttackMove(target.Location, 2)
			else
				IdleHunt(orca)
			end
		end
	end

	if #team > 0 then
		SendTeam(team, base.player, route, true, #route == 0)
	end
end

local function StartBaseDefense(base, target)
	if MissionFinished or base == nil or base.allOut then return end
	base.defending = true
	base.defenseUntil = DateTime.GameTime + DateTime.Seconds(BaseDefenseAlarmSeconds)
	AllOutAttack(base, target)
end

local function DefendAgainst(actor, attacker, damage)
	if MissionFinished or not IsLive(actor) or not actor.IsInWorld or not actor.HasProperty("Location")
		or damage == nil or damage <= 0 then return end
	-- The small Communications Center outpost defends itself with its own
	-- towers and garrison; it must not pull in the other bases or Orcas.
	if IsCentralOutpostActor(actor) then return end
	-- Same for the south-eastern outpost: it is a small capture target, not
	-- a full base. Its own garrison still fights locally.
	if actor.Owner == GDISouth then return end
	if not IsLive(attacker) or attacker.Owner ~= Nod or not attacker.HasProperty("Location") then return end

	local base = BaseForActor(actor)
	if base ~= nil and not IsLocal(base, actor) then base = nil end
	-- Selling buildings while the base is being attacked leaves it
	-- defenceless, so sales are suspended until 15 s after the last hit.
	if base ~= nil and actor.HasProperty("StartBuildingRepairs") then
		base.underAttackUntil = math.max(base.underAttackUntil or 0,
			DateTime.GameTime + DateTime.Seconds(UnderAttackSellPauseSeconds))
	end

	-- Repairs are gated by the economy state. In Crisis/Liquidation only the
	-- income chain is repaired; the EYE is deliberately not (Option B).
	if actor.HasProperty("StartBuildingRepairs") and actor.Health < actor.MaxHealth * 0.75 then
		if base == nil or CanRepairBuilding(base, actor) then
			actor.StartBuildingRepairs()
		end
	end
	if base ~= nil and actor.HasProperty("StartBuildingRepairs")
		and (actor.Type == "fact" or actor.Type == "proc" or actor.Type == "nuke" or actor.Type == "nuk2") then
		base.hasCriticalDamage = true
	end

	if base ~= nil and actor.HasProperty("StartBuildingRepairs") then
		if difficulty == "hard" then
			-- Broad nod13b alarm on Hard: every repairable building releases
			-- the Orcas and two reserve units instead of committing the base.
			BaseAlarmStarted = true
			base.defending = true
			base.defenseUntil = math.max(base.defenseUntil, DateTime.GameTime + DateTime.Seconds(BaseDefenseAlarmSeconds))
			for _, player in ipairs(gdiPlayers) do
				for _, orca in ipairs(player.GetActorsByType("orca")) do
					if IsLive(orca) and orca.IsInWorld and orca.HasProperty("Stance")
						and orca.Stance ~= "AttackAnything" then
						orca.Stance = "AttackAnything"
						IdleHunt(orca)
					end
				end
			end
		elseif BaseDefenseBuildings[actor.Type] then
			-- Easy/Normal: the attacked base commits everything.
			StartBaseDefense(base, attacker)
		end
	end

	local attackerKey = EconomyActorKey(attacker)
	for i = #defenseResponses, 1, -1 do
		local response = defenseResponses[i]
		if DateTime.GameTime >= response.expires then
			table.remove(defenseResponses, i)
		elseif response.attackerKey == attackerKey then
			return
		end
	end
	table.insert(defenseResponses, { attackerKey = attackerKey, expires = DateTime.GameTime + DateTime.Seconds(2) })

	-- A hit on a base building commits part of that base's reserve on Hard.
	if difficulty == "hard" and base ~= nil and actor.HasProperty("StartBuildingRepairs")
		and DateTime.GameTime >= (base.reserveRelease or 0) then
		base.reserveRelease = DateTime.GameTime + DateTime.Seconds(30)
		local team = TakeForAttack(base, 2)
		if #team > 0 then SendTeam(team, base.player, {}, true) end
	end

	local radius = actor.HasProperty("StartBuildingRepairs") and 18 or 10
	for _, player in ipairs(gdiPlayers) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if IsLive(unit) and unit.IsInWorld and unit.HasProperty("AttackMove") and unit ~= actor
				and unit.Type ~= "harv"
				and not IsCentralOutpostActor(unit) and unit.Owner ~= GDISouth
				and SameLand(unit.Location, attacker.Location) then
				local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
				if dx * dx + dy * dy <= radius * radius then
					if unit.HasProperty("Stop") then unit.Stop() end
					unit.AttackMove(attacker.Location, 2)
				end
			end
		end
	end
end

local function Guard(actor)
	if not IsLive(actor) then return end
	local key = EconomyActorKey(actor)
	if guarded[key] then return end
	guarded[key] = true
	Trigger.OnDamaged(actor, DefendAgainst)
	if actor.HasProperty("StartBuildingRepairs") then
		Trigger.OnCapture(actor, function(_, _, _, newOwner)
			if IsGDIPlayer(newOwner) then Guard(actor) end
		end)
	end
end

-- Orcas are the mobile reserve and stay on their helipad until their base
-- comes under attack. DefendAgainst releases them when a base building is hit;
-- the same pattern is used in nod13a.
local function OrcaDamaged(base, orca, attacker, damage)
	if MissionFinished or base == nil or damage == nil or damage <= 0 or not IsLive(orca) then return end
	local key = EconomyActorKey(orca)
	if retreatingOrcas[key] then return end
	if orca.Health * 4 > orca.MaxHealth or LocalBuilding(base, "hpad") == nil then return end
	retreatingOrcas[key] = true
	orca.Stop()
	orca.ReturnToBase()
end

local function GuardOrca(base, orca)
	if not IsLive(orca) or orca.Type ~= "orca" then return end
	Guard(orca)
	local key = EconomyActorKey(orca)
	if base ~= nil then orcaBase[key] = base end
	if not orcaHandled[key] then
		orcaHandled[key] = true
		Trigger.OnDamaged(orca, function(actor, attacker, damage)
			OrcaDamaged(orcaBase[EconomyActorKey(actor)] or BaseForPlayer(actor.Owner) or BaseAt(actor.Location), actor, attacker, damage)
		end)
	end
	if orca.HasProperty("Stance") then orca.Stance = "Defend" end
	if base ~= nil and base.defending then
		if orca.HasProperty("Stance") then orca.Stance = "AttackAnything" end
		IdleHunt(orca)
	end
end

local function OrcaWatch()
	if MissionFinished then return end
	for _, player in ipairs(gdiPlayers) do
		for _, orca in ipairs(player.GetActorsByType("orca")) do
			local key = EconomyActorKey(orca)
			if IsLive(orca) and retreatingOrcas[key] then
				local base = orcaBase[key] or BaseForPlayer(orca.Owner) or BaseAt(orca.Location)
				if base == nil or LocalBuilding(base, "hpad") == nil or orca.Health * 10 >= orca.MaxHealth * 9 then
					retreatingOrcas[key] = nil
					if base ~= nil and base.defending then
						if orca.HasProperty("Stance") then orca.Stance = "AttackAnything" end
						IdleHunt(orca)
					end
				end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(5), OrcaWatch)
end

local function TrackHarvester(base, harv)
	if harv == nil or base == nil then return end
	local key = EconomyActorKey(harv)
	if trackedHarvesters[key] then return end
	trackedHarvesters[key] = true
	ClaimHarvester(base, harv)
	Trigger.OnKilled(harv, function(_, killer)
		UnclaimHarvester(base, harv)
		base.harvesterLost = true
		-- The economy tick is the single decision point. Do not sell or
		-- start an all-out directly from the kill hook.
		if not MissionFinished then
			Trigger.AfterDelay(1, function()
				if not MissionFinished then EconomyTick() end
			end)
		end
	end)
end

local footprint = { fact = { 3, 3 }, proc = { 3, 4 }, weap = { 3, 3 },
	pyle = { 2, 3 }, nuke = { 2, 3 }, nuk2 = { 2, 3 }, atwr = { 1, 1 }, gtwr = { 1, 1 },
	silo = { 2, 1 }, hq = { 2, 3 }, eye = { 2, 3 }, hpad = { 2, 2 }, fix = { 3, 3 } }
local emptyFootprintCells = { proc = { [0] = true, [2] = true }, hq = { [1] = true }, eye = { [1] = true } }

local function OccupiesFootprint(kind, x, y)
	local empty = emptyFootprintCells[kind]
	return empty == nil or not empty[y * 4 + x]
end

local function BuildingAt(entry)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if IsLive(actor) and actor.Type == entry[1] and actor.HasProperty("Location")
			and actor.Location.X == entry[2] and actor.Location.Y == entry[3] then return actor end
	end
	return nil
end

local function SiteFree(entry, ignore, owner)
	local size = footprint[entry[1]]
	if size == nil then return false end
	for x = entry[2], entry[2] + size[1] - 1 do
		for y = entry[3], entry[3] + size[2] - 1 do
			local terrain = Map.TerrainType(CPos.New(x, y))
			if OccupiesFootprint(entry[1], x - entry[2], y - entry[3])
				and terrain ~= "Clear" and terrain ~= "Road" then return false end
		end
	end
	local topLeft = WPos.New((entry[2] - 4) * 1024, (entry[3] - 4) * 1024, 0)
	local bottomRight = WPos.New((entry[2] + size[1] + 4) * 1024, (entry[3] + size[2] + 4) * 1024, 0)
	local blockers = Map.ActorsInBox(topLeft, bottomRight, function(actor)
		return IsLive(actor) and actor ~= ignore and actor.HasProperty("Location")
			and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
	end)
	for _, actor in ipairs(blockers) do
		if not actor.Type:match("%.husk$") then
			local otherSize = footprint[actor.Type] or (actor.Type:match("^rock%d") and { 4, 2 })
				or (actor.Type:match("^t%d%d") and { 2, 2 })
				or (actor.HasProperty("StartBuildingRepairs") and { 3, 3 } or { 1, 1 })
			local x, y = actor.Location.X, actor.Location.Y
			for cx = math.max(x, entry[2]), math.min(x + otherSize[1], entry[2] + size[1]) - 1 do
				for cy = math.max(y, entry[3]), math.min(y + otherSize[2], entry[3] + size[2]) - 1 do
					if OccupiesFootprint(entry[1], cx - entry[2], cy - entry[3])
						and OccupiesFootprint(actor.Type, cx - x, cy - y) then
						if actor.Owner == owner and actor.HasProperty("Move") and not actor.HasProperty("Land") then
							actor.Move(CPos.New(entry[2], entry[3] + size[2] + 1))
							-- A displaced harvester must resume harvesting; otherwise
							-- rebuilding a building silently parks it forever.
							if actor.HasProperty("FindResources") then
								actor.FindResources()
							end
						end
						return false
					end
				end
			end
		end
	end
	return true
end

local function FindNearbySite(base, kind, anchor, ignore)
	local size = footprint[kind]
	if size == nil then return nil end
	for radius = 0, 8 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				local x, y = base.yard[1] + dx, base.yard[2] + dy
				if (math.abs(dx) == radius or math.abs(dy) == radius)
					and x >= 3 and x + size[1] - 1 <= 62 and y >= 1 and y + size[2] - 1 <= 62 then
					local candidate = { kind, x, y }
					if SameLand(anchor, CPos.New(x + 1, y + 1)) and SiteFree(candidate, ignore, nil) then
						return candidate
					end
				end
			end
		end
	end
	return nil
end

local function WatchMCV(base, mcv, tries)
	if MissionFinished then return end
	if base.mcv ~= mcv then return end
	if not IsLive(mcv) or mcv.Owner ~= base.player then base.mcv = nil return end
	if LocalBuilding(base, "fact") ~= nil then base.mcv = nil return end
	if mcv.IsInWorld then
		local entry = base.mcvSite
		if entry == nil or not SiteFree(entry, mcv, nil) then
			entry = FindNearbySite(base, "fact", mcv.Location, mcv)
			base.mcvSite = entry
		end
		if entry ~= nil then
			local x, y = entry[2] + 1, entry[3] + 1
			if mcv.Location.X == x and mcv.Location.Y == y then
				if mcv.HasProperty("Deploy") then mcv.Deploy() end
			elseif tries % 5 == 0 and mcv.HasProperty("Move") then
				mcv.Move(CPos.New(x, y))
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), function() WatchMCV(base, mcv, tries + 1) end)
end

-- ===========================================================================
-- Economy recovery (nod13c)
--
-- The economy is base-local and the funds are player-wide. A base is only
-- "healthy" while a refinery and a claimed harvester exist; missing power is
-- a strained state, not an income loss. Sales are asynchronous and never
-- fire OnKilled. See harness/ECONOMY-RECOVERY-NOD13C.md.
-- ===========================================================================
local SellMode = { Normal = "normal", Emergency = "emergency", All = "all" }
-- Declaration order matters in Lua: SellOrder must exist before
-- LiquidatableValue/SellForEconomy are defined.
local SellOrder = { "gtwr", "atwr", "hpad", "silo", "pyle", "weap", "hq" }
local CustomSellValue = { proc = 400 }

local EconomySellCooldownSeconds = 20
local SettlementSeconds = 20
local SellTimeoutSeconds = 60
local FreeActorGraceSeconds = 10
local DeadConfirmSeconds = 20
local DeadAllOutGraceSeconds = 60
local IncomeWindowSeconds = 90
local IncomeFailureSeconds = 180
local HealthyReserve = 500
local RepairReserve = 500
local SellBuffer = 0

local harvesterClaims = {}
local reservedFunds = {}
local activeSaleBase = {}
local economyTickScheduled = false

-- Included in lua.log so a log can be matched to the code that produced it.
-- Globals on purpose: the main chunk is at the 200-local limit.
ECONOMY_LOG_VERSION = "v2.15"
function EconomyErrorText(err)
	if debug ~= nil and debug.traceback ~= nil then
		return debug.traceback(tostring(err), 2)
	end
	return tostring(err)
end

-- The harness does not simulate Tiberium income. It disables the cashflow
-- watchdog so a static stub balance is not mistaken for a stuck harvester.
EconomyWatchdogEnabled = EconomyWatchdogEnabled ~= false

local StateRank = { Dead = 0, DeadPending = 1, Liquidation = 2,
	Crisis = 3, Strained = 4, Healthy = 5 }

local function IsEconomyBase(base)
	if base == nil or base.player == nil then return false end
	if base.player == GDISouth then return false end
	if HouseEliminated(base.player) then return false end
	return true
end

local function ReservedFunds(player)
	return reservedFunds[player] or 0
end

local function AvailableFunds(player)
	local funds = GDIFunds(player) - ReservedFunds(player)
	if funds < 0 then return 0 end
	return funds
end

local function AvailableFundsForBase(base)
	local own = base.reservedFunds or 0
	local funds = GDIFunds(base.player) - (ReservedFunds(base.player) - own)
	if funds < 0 then return 0 end
	return funds
end

local function IsNear(actor, other, radius)
	if actor == nil or other == nil or not actor.HasProperty("Location")
		or not other.HasProperty("Location") then return false end
	local dx = actor.Location.X - other.Location.X
	local dy = actor.Location.Y - other.Location.Y
	return dx * dx + dy * dy <= radius * radius
end

ClaimHarvester = function(base, harv)
	if base == nil or harv == nil then return end
	local key = EconomyActorKey(harv)
	if key == nil then return end
	local owner = harvesterClaims[key]
	if owner ~= nil and owner ~= base then
		return -- never steal a harvester from another base
	end
	harvesterClaims[key] = base
	base.harvesters[key] = harv
	if not IsLive(base.harvester) then base.harvester = harv end
	base.harvesterSince = base.harvesterSince or DateTime.GameTime
	base.noHarvesterSince = nil
end

UnclaimHarvester = function(base, harv)
	if base == nil or harv == nil then return end
	local key = EconomyActorKey(harv)
	if key == nil then return end
	if harvesterClaims[key] == base then harvesterClaims[key] = nil end
	if base.harvesters ~= nil then base.harvesters[key] = nil end
	if EconomyActorKey(base.harvester) == key then
		local _, nextHarv = next(base.harvesters or {})
		base.harvester = nextHarv
	end
	if next(base.harvesters or {}) == nil then
		base.harvesterSince = nil
	end
end

LiveHarvesterCount = function(base)
	if base.harvesters == nil then return 0 end
	local count, dead = 0, {}
	for _, harv in pairs(base.harvesters) do
		if IsLive(harv) and harv.IsInWorld and harv.Owner == base.player then
			count = count + 1
		else
			table.insert(dead, harv)
		end
	end
	-- Never mutate the set while pairs() is iterating it.
	for _, harv in ipairs(dead) do UnclaimHarvester(base, harv) end
	return count
end

HarvesterCount = function(base)
	return LiveHarvesterCount(base) + (base.harvesterBuilding and 1 or 0)
end

function EconomyHarvesterTarget(base)
	return math.max(1, (base.harvestersPerProc or 1)
		* math.max(1, #GetLocalActors(base, "proc")))
end

function EconomyHasHarvesterDeficit(base)
	return HarvesterCount(base) < EconomyHarvesterTarget(base)
end

-- Restart every claimed harvester that has no activity. Refinery rebuilds,
-- SiteFree displacements and interrupted harvest cycles can leave a
-- harvester idle; without this it would stand still forever and only the
-- cashflow watchdog (which retasks a single harvester) would notice.
local function RestartIdleHarvesters(base)
	if base.harvesters == nil then return end
	-- Harvesting without a refinery is pointless and would only queue
	-- activities that immediately fail.
	if LocalBuilding(base, "proc") == nil then return end
	for _, harv in pairs(base.harvesters) do
		if IsLive(harv) and harv.IsInWorld and harv.Owner == base.player
			and harv.HasProperty("FindResources") and harv.IsIdle then
			harv.FindResources()
		end
	end
end

AdoptFreeHarvester = function(base)
	if base == nil or not IsEconomyBase(base) then return end
	-- Clear a stale primary pointer first.
	if not (IsLive(base.harvester) and base.harvester.IsInWorld
		and base.harvester.Owner == base.player) then
		base.harvester = nil
	end
	local procs = GetLocalActors(base, "proc")
	local function NearAnyProc(harv)
		if #procs == 0 then return true end
		for _, proc in ipairs(procs) do
			if IsNear(harv, proc, 12) then return true end
		end
		return false
	end
	-- Adopt *all* unclaimed harvesters that belong to one of this base's
	-- refineries. A base can have more than one refinery (mission-specific),
	-- and a free harvester may spawn at the far one.
	for _, harv in ipairs(base.player.GetActorsByType("harv")) do
		local key = EconomyActorKey(harv)
		if IsLive(harv) and harv.Owner == base.player and harv.IsInWorld
			and key ~= nil and harvesterClaims[key] == nil and NearAnyProc(harv) then
			ClaimHarvester(base, harv)
		end
	end
	if base.harvester == nil then
		local _, nextHarv = next(base.harvesters or {})
		base.harvester = nextHarv
	end
	RestartIdleHarvesters(base)
end

local function HasLocalPower(base)
	return LocalBuilding(base, "nuke") ~= nil or LocalBuilding(base, "nuk2") ~= nil
end

local function HasStructuralIncome(base)
	return LocalBuilding(base, "proc") ~= nil and LiveHarvesterCount(base) > 0
end

local function IsIncomeSuspect(base)
	return base.incomeSuspectSince ~= nil and not EconomyIsStorageFull(base.player)
end

local function IsStrained(base)
	local power = base.player.PowerState
	return not HasLocalPower(base)
		or power == "Low" or power == "Critical"
		or AvailableFundsForBase(base) < HealthyReserve
		or base.hasCriticalDamage == true
		or IsIncomeSuspect(base)
end

-- Dynamic anypower policy. StartBuilding bypasses prerequisites, so this is
-- an explicit AI decision, not an engine rule. The decision is cached for
-- the duration of a recovery so a sale cannot flip it and trigger extra
-- selling for a power plant.
local function ResolveRecoveryPowerPolicy(base)
	if LocalBuilding(base, "proc") ~= nil then
		base.recoveryPowerPolicy = nil
		return "respect-anypower"
	end
	if base.recoveryPowerPolicy ~= nil and (base.recovering or (base.reservedFunds or 0) > 0) then
		return base.recoveryPowerPolicy
	end
	local policy = "bypass"
	if AvailableFundsForBase(base) >= Actor.Cost("nuke") + Actor.Cost("proc") then
		policy = "respect-anypower"
	end
	if base.recoveryPowerPolicy ~= policy then
		print(("ECON base=%s t=%d action=power-policy policy=%s funds=%d")
			:format(base.player.Name, DateTime.GameTime, policy, AvailableFundsForBase(base)))
	end
	base.recoveryPowerPolicy = policy
	return policy
end

local function MCVRecoveryNeed(base)
	if base.mcvBuilding then return 0 end
	if LocalBuilding(base, "mcv") ~= nil then return 0 end
	if LocalBuilding(base, "weap") ~= nil or base.rebuildingKind == "weap" then
		return Actor.Cost("mcv")
	end
	return nil -- Dead
end

local function RecoveryNeed(base)
	if LocalBuilding(base, "fact") == nil then
		-- Without a construction yard, income comes first: if a harvester
		-- can still be produced, that is the only need. The MCV is bought
		-- from the money the new harvester earns, not by liquidating the
		-- base up front.
		local harv = HarvesterCount(base)
		local freeWaiting = base.freeActorGraceUntil ~= nil
			and DateTime.GameTime < base.freeActorGraceUntil
		if LocalBuilding(base, "proc") ~= nil and LocalBuilding(base, "weap") ~= nil then
			if freeWaiting or base.harvesterBuilding then
				return 0 -- wait for the harvester before saving for the MCV
			end
			if harv == 0 then
				return Actor.Cost("harv")
			end
		end
		return MCVRecoveryNeed(base)
	end

	local need = 0
	local hasPower = HasLocalPower(base)
	local hasProc = LocalBuilding(base, "proc") ~= nil
	local harv = HarvesterCount(base)
	local powerPending = base.rebuildingKind == "nuke"
		or base.rebuildingKind == "nuk2"
	local procPending = base.rebuildingKind == "proc"

	if not hasProc then
		if ResolveRecoveryPowerPolicy(base) == "respect-anypower"
			and not hasPower and not powerPending then
			need = need + Actor.Cost("nuke")
		end
		if not procPending then
			need = need + Actor.Cost("proc")
		end
	elseif harv == 0 and not base.harvesterBuilding
		and not (base.freeActorGraceUntil ~= nil
			and DateTime.GameTime < base.freeActorGraceUntil) then
		-- Harvesters are only ever produced by the weapons factory, so a
		-- missing factory is part of the replacement cost.
		if LocalBuilding(base, "weap") == nil and base.rebuildingKind ~= "weap" then
			need = need + Actor.Cost("weap")
		end
		need = need + Actor.Cost("harv")
	end
	return need
end

local function TryReserveRecovery(base, need)
	local already = base.reservedFunds or 0
	if already >= need then
		base.recovering = true
		return true
	end
	local extra = need - already
	if AvailableFunds(base.player) < extra then return false end
	base.reservedFunds = already + extra
	reservedFunds[base.player] = (reservedFunds[base.player] or 0) + extra
	base.recovering = true
	return true
end

local function ConsumeRecoveryReservation(base, amount)
	local have = base.reservedFunds or 0
	local used = math.min(have, amount)
	base.reservedFunds = have - used
	reservedFunds[base.player] = math.max(0, (reservedFunds[base.player] or 0) - used)
	return used
end

local function ReleaseRecoveryReservation(base, amount)
	local have = base.reservedFunds or 0
	local released = math.min(have, amount)
	base.reservedFunds = have - released
	reservedFunds[base.player] = math.max(0, (reservedFunds[base.player] or 0) - released)
	if base.reservedFunds == 0
		and not base.rebuilding and not base.harvesterBuilding then
		base.recovering = false
	end
end

local function ReconcileRecoveryReservation(base)
	-- Never release while a build or a queued harvester is in flight.
	if base.rebuilding or base.harvesterBuilding then return end
	local want = RecoveryNeed(base) or 0
	local have = base.reservedFunds or 0
	if have > want then
		ReleaseRecoveryReservation(base, have - want)
	elseif want == 0 then
		ReleaseRecoveryReservation(base, have)
		base.recovering = false
	end
end

local function EstimateRefund(actor)
	if actor == nil or not IsLive(actor) then return 0 end
	local sellValue = CustomSellValue[actor.Type] or Actor.Cost(actor.Type)
	local maxHealth = actor.MaxHealth or 100
	local health = actor.Health or maxHealth
	return math.floor(sellValue * 0.5 * health / maxHealth)
end

local function IsSellCandidate(base, actor, mode)
	if actor == nil or not IsLive(actor) or not actor.IsInWorld then return false end
	if actor.Owner ~= base.player or not actor.HasProperty("Location") then return false end
	if not IsLocal(base, actor) or IsCentralOutpostActor(actor) then return false end
	local kind = actor.Type
	if kind == "fact" or kind == "proc" or kind == "nuke" or kind == "nuk2"
		or kind == "eye" then return false end
	if base.selling ~= nil and base.selling[actor] ~= nil then return false end
	if not actor.HasProperty("Sell") then return false end
	-- A silo with stored resources may never be sold: removing its storage
	-- capacity clips player.Resources. Silos are only sold when empty.
	if kind == "silo" and (base.player.Resources or 0) > 0 then return false end
	if kind == "hq" and mode ~= SellMode.All then return false end
	-- The weapons factory is the only place a harvester can be produced.
	-- Without a construction yard it is also the only place an MCV can be
	-- produced, so it must never be sold while there is no FACT. With a
	-- yard it may be sold to raise funds once a harvester is alive and no
	-- replacement is in production.
	if kind == "weap" then
		if LocalBuilding(base, "fact") == nil then return false end
		if LocalBuilding(base, "proc") ~= nil
			and (LiveHarvesterCount(base) == 0 or base.harvesterBuilding) then
			return false
		end
	end
	return true
end

local function LiquidatableValue(base, mode)
	local total = 0
	for _, kind in ipairs(SellOrder) do
		for _, actor in ipairs(GetLocalActors(base, kind)) do
			if IsSellCandidate(base, actor, mode) then
				total = total + EstimateRefund(actor)
			end
		end
	end
	return total
end

local function TryStartSalePlan(base)
	local active = activeSaleBase[base.player]
	if active ~= nil and active ~= base then return false end
	activeSaleBase[base.player] = base
	return true
end

local function SellForEconomy(base, need, mode)
	if base == nil or base.rebuilding then return false end
	-- Do not strip a base while it is under attack or actively defending:
	-- the towers are its defence, not a funding source in combat.
	if base.defending then return false end
	if base.underAttackUntil ~= nil and DateTime.GameTime < base.underAttackUntil then
		return false
	end
	-- Without a construction yard the harvester should earn the MCV first.
	-- The window starts when the yard is lost, not when the harvester was
	-- claimed, so an old harvester also gets the chance to pay for the MCV.
	if LocalBuilding(base, "fact") == nil and LiveHarvesterCount(base) > 0
		and base.factLostAt ~= nil
		and DateTime.GameTime - base.factLostAt < DateTime.Seconds(HarvesterEarnWindowSeconds) then
		return false
	end
	-- A working income stream is the intended funding source for the MCV.
	-- Do not strip the base (towers, helipad, factory) while the harvester
	-- is actually earning. Only a stuck harvester may trigger sales, and
	-- even then the weapons factory stays protected.
	if LocalBuilding(base, "fact") == nil and HasStructuralIncome(base)
		and not IsIncomeSuspect(base) then
		return false
	end
	-- Selling for a harvester/MCV cannot help while its factory is missing,
	-- still being built, or busy with another item. This was how the west
	-- base stripped its helipads while waiting for a jammed weapons factory.
	local factory = LocalBuilding(base, "weap")
	local factoryUnavailable = base.rebuildingKind == "weap" or factory == nil
		or (factory.HasProperty("IsProducing") and factory.IsProducing("harv"))
	local harvesterNeed = need > 0 and need <= Actor.Cost("harv")
		and LiveHarvesterCount(base) == 0 and not base.harvesterBuilding
	local mcvNeed = need > 0 and need <= Actor.Cost("mcv")
		and LocalBuilding(base, "mcv") == nil and not base.mcvBuilding
	if (harvesterNeed or mcvNeed) and factoryUnavailable then
		if base.lastSaleBlockLog == nil
			or DateTime.GameTime - base.lastSaleBlockLog >= DateTime.Seconds(60) then
			base.lastSaleBlockLog = DateTime.GameTime
			print(("ECON base=%s t=%d action=sell-blocked reason=weap-unavailable need=%d funds=%d")
				:format(base.player.Name, DateTime.GameTime, need, AvailableFundsForBase(base)))
		end
		return false
	end
	mode = mode or SellMode.Emergency
	if mode == SellMode.Normal and base.lastSell ~= nil
		and DateTime.GameTime - base.lastSell < DateTime.Seconds(EconomySellCooldownSeconds) then
		return true
	end
	if not TryStartSalePlan(base) then return false end
	if base.selling == nil then base.selling = {} end

	local funds = AvailableFundsForBase(base)
	if funds >= need then return false end
	local started = false
	local sellCount = 0
	local now = DateTime.GameTime
	for _, kind in ipairs(SellOrder) do
		if funds >= need + SellBuffer then break end
		for _, actor in ipairs(GetLocalActors(base, kind)) do
			if funds >= need + SellBuffer then break end
			if IsSellCandidate(base, actor, mode) then
				local expected = EstimateRefund(actor)
				base.selling[actor] = { started = now, expected = expected, kind = kind }
				funds = funds + expected
				actor.Sell()
				started = true
				sellCount = sellCount + 1
			end
		end
	end

	if started then
		base.lastSell = now
		base.settleUntil = now + DateTime.Seconds(SettlementSeconds)
		base.settling = true
		print(("ECON base=%s t=%d action=sell mode=%s need=%d funds=%d sells=%d")
			:format(base.player.Name, now, mode, need, funds, sellCount))
		return true
	end
	return next(base.selling) ~= nil
end

local function HasRecoverySurplus(base)
	return AvailableFundsForBase(base) >= (RecoveryNeed(base) or 0) + HealthyReserve
end

-- Power plant choice. A missing refinery needs the cheap NUKE to keep the
-- NUKE+PROC chain at 2000. For capacity (refinery exists), the first build is
-- a NUKE; if power is still low afterwards, the bigger NUK2 is cheaper per
-- point of power (800/200 vs 500/100).
function EconomyPowerBuildKind(base)
	if LocalBuilding(base, "proc") == nil then return "nuke" end
	local recent = base.lastPowerKind ~= nil and base.lastPowerAt ~= nil
		and DateTime.GameTime - base.lastPowerAt <= DateTime.Seconds(PowerOscillationSeconds)
	if recent and base.player.HasPrerequisites(prerequisites.nuk2)
		and AvailableFundsForBase(base) >= Actor.Cost("nuk2") then
		return "nuk2"
	end
	return "nuke"
end

function EconomyTryBuildPowerKind(base, kind, yard)
	local cost = Actor.Cost(kind)
	if AvailableFundsForBase(base) < cost then return false end
	for _, entry in ipairs(base.nodes or {}) do
		if entry[1] == kind and BuildingAt(entry) == nil and SiteFree(entry, nil, base.player) then
			return StartBuilding(base, entry, yard)
		end
	end
	local entry = FindNearbySite(base, kind, yard.Location, nil)
	if entry ~= nil then return StartBuilding(base, entry, yard) end
	return false
end

local function TryBuildPower(base)
	if base.rebuilding or not IsEconomyBase(base) then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return false end
	local kind = EconomyPowerBuildKind(base)
	if kind == "nuk2" then
		if EconomyTryBuildPowerKind(base, "nuk2", yard) then return true end
		kind = "nuke"
	end
	return EconomyTryBuildPowerKind(base, kind, yard)
end

local function TryBuildProc(base)
	if base.rebuilding or not IsEconomyBase(base) then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return false end
	local cost = Actor.Cost("proc")
	if AvailableFundsForBase(base) < cost then return false end
	for _, entry in ipairs(base.nodes or {}) do
		if entry[1] == "proc" and BuildingAt(entry) == nil and SiteFree(entry, nil, base.player) then
			return StartBuilding(base, entry, yard)
		end
	end
	local entry = FindNearbySite(base, "proc", yard.Location, nil)
	if entry ~= nil then return StartBuilding(base, entry, yard) end
	return false
end

-- When storage is nearly full, a delivered load is discarded and
-- Cash + Resources stays flat. Adding capacity with an extra silo (not just
-- the fixed map nodes) keeps the harvesters earning.
function EconomyTryBuildSilo(base)
	if base.rebuilding or not IsEconomyBase(base) then return false end
	if not EconomyIsStorageNearlyFull(base.player) then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return false end
	if not base.player.HasPrerequisites(prerequisites.silo) then return false end
	local cost = Actor.Cost("silo")
	if AvailableFundsForBase(base) < cost + HealthyReserve then return false end
	local entry = FindNearbySite(base, "silo", yard.Location, nil)
	if entry ~= nil then return StartBuilding(base, entry, yard) end
	return false
end

local function TryBuildPyle(base)
	if base.rebuilding or not IsEconomyBase(base) then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil or LocalBuilding(base, "proc") == nil then return false end
	local cost = Actor.Cost("pyle")
	if not HasRecoverySurplus(base) or AvailableFundsForBase(base) < cost + HealthyReserve then
		return false
	end
	for _, entry in ipairs(base.nodes or {}) do
		if entry[1] == "pyle" and BuildingAt(entry) == nil and SiteFree(entry, nil, base.player) then
			return StartBuilding(base, entry, yard)
		end
	end
	local entry = FindNearbySite(base, "pyle", yard.Location, nil)
	if entry ~= nil then return StartBuilding(base, entry, yard) end
	return false
end

local function TryBuildWeap(base)
	if base.rebuilding or not IsEconomyBase(base) then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil or LocalBuilding(base, "proc") == nil then return false end
	if not base.player.HasPrerequisites(prerequisites.weap) then return false end
	local cost = Actor.Cost("weap")
	if AvailableFundsForBase(base) < cost then return false end
	for _, entry in ipairs(base.nodes or {}) do
		if entry[1] == "weap" and BuildingAt(entry) == nil and SiteFree(entry, nil, base.player) then
			return StartBuilding(base, entry, yard)
		end
	end
	local entry = FindNearbySite(base, "weap", yard.Location, nil)
	if entry ~= nil then return StartBuilding(base, entry, yard) end
	return false
end

local function TryMCVRecovery(base)
	if LocalBuilding(base, "fact") ~= nil then return end
	if base.mcvBuilding then return end
	local mcv = LocalBuilding(base, "mcv")
	if mcv ~= nil then
		base.mcv = mcv
		WatchMCV(base, mcv, 0)
		return
	end
	local factory = LocalBuilding(base, "weap")
	if factory ~= nil and AvailableFundsForBase(base) >= Actor.Cost("mcv") then
		if Queue(factory, "mcv", base.player) then
			base.mcvBuilding = true
			-- Safety net if OnProduced never fires (e.g. factory lost).
			local timeout = math.ceil(Actor.BuildTime("mcv", "Vehicle.GDI")) + 60
			Trigger.AfterDelay(DateTime.Seconds(timeout), function()
				if base.mcvBuilding then
					base.mcvBuilding = false
					Trigger.AfterDelay(1, function() if not MissionFinished then EconomyTick() end end)
				end
			end)
		end
	end
end

local function Recover(base)
	if base == nil or not base.recovering then return end
	if LocalBuilding(base, "fact") == nil then
		-- No construction yard: restore income first. A harvester is cheap
		-- and earns the MCV; only after it exists (or when there is no
		-- refinery left to earn with) is the MCV ordered.
		local harv = HarvesterCount(base)
		RebuildHarvester(base, nil)
		if harv > 0 or LocalBuilding(base, "proc") == nil then
			TryMCVRecovery(base)
		end
		return
	end

	local hasPower = HasLocalPower(base)
	local hasProc = LocalBuilding(base, "proc") ~= nil
	local powerPending = base.rebuildingKind == "nuke"
		or base.rebuildingKind == "nuk2"
	local freeActorWaiting = base.freeActorGraceUntil ~= nil
		and DateTime.GameTime < base.freeActorGraceUntil

	-- 1) Missing refinery: only build a power plant first when NUKE + PROC
	-- are both affordable (respect-anypower). Otherwise PROC first.
	if not hasProc then
		if ResolveRecoveryPowerPolicy(base) == "respect-anypower"
			and not hasPower and not powerPending then
			if TryBuildPower(base) then return end
		end
		TryBuildProc(base)
		return
	end

	-- 2) Harvesters are produced only by the weapons factory and use its
	-- normal queue/build time. If the factory is gone, rebuild it first;
	-- never spawn a harvester next to the refinery.
	if HarvesterCount(base) == 0 and not base.harvesterBuilding and not freeActorWaiting then
		if LocalBuilding(base, "weap") == nil then
			if base.rebuildingKind == "weap" then return end
			TryBuildWeap(base)
			return
		end
		RebuildHarvester(base, nil)
		return
	end

	-- 3) Optional production building only after the income chain stands.
	if LocalBuilding(base, "pyle") == nil and HasRecoverySurplus(base) then
		TryBuildPyle(base)
	end
end

CanRepairBuilding = function(base, actor)
	local state = base.economyState
	local kind = actor.Type
	if state == "Dead" or state == "DeadPending" then return false end
	local chain = kind == "fact" or kind == "proc" or kind == "nuke" or kind == "nuk2"
	if state == "Crisis" or state == "Liquidation" then
		if not chain then return false end
		return AvailableFundsForBase(base) - (base.recoveryNeed or 0) >= RepairReserve
	end
	if state == "Strained" then
		-- A chain repair protects income and is always allowed. Optional
		-- repairs need the full healthy+repair reserve.
		if chain then return true end
		if kind == "eye" or kind == "hq" or kind == "weap" then
			return AvailableFundsForBase(base) >= HealthyReserve + RepairReserve
		end
		return false
	end
	-- Healthy: chain repairs are always allowed; optional repairs must leave
	-- both the recovery reserve and the repair reserve untouched.
	if chain then return true end
	return AvailableFundsForBase(base) >= HealthyReserve + RepairReserve
end

local function RepairDamagedBuildings(base)
	if base.economyState == "Dead" or base.economyState == "DeadPending" then return end
	for _, actor in ipairs(base.player.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("StartBuildingRepairs")
			and IsLocal(base, actor) and not IsCentralOutpostActor(actor)
			and actor.Health < actor.MaxHealth * 0.75
			and CanRepairBuilding(base, actor) then
			actor.StartBuildingRepairs()
		end
	end
end

local function ClearDead(base)
	base.deadSince = nil
	base.deadAt = nil
end

local function ConfirmDead(base)
	if base.deadSince == nil then
		base.deadSince = DateTime.GameTime
		return false
	end
	return DateTime.GameTime - base.deadSince >= DateTime.Seconds(DeadConfirmSeconds)
end

local function ClassifyEconomy(base)
	if LocalBuilding(base, "fact") == nil then
		local need = RecoveryNeed(base)
		if need == nil then
			if ConfirmDead(base) then return "Dead", nil end
			return "DeadPending", nil
		end
		ClearDead(base)
		return "Crisis", need
	end

	if HasStructuralIncome(base) then
		ClearDead(base)
		if IsStrained(base) then return "Strained", 0 end
		return "Healthy", 0
	end

	local need = RecoveryNeed(base)
	if need == 0 then
		ClearDead(base)
		return "Strained", 0 -- recovery building/queue is pending
	end

	local funds = AvailableFundsForBase(base)
	if funds >= need then
		ClearDead(base)
		return "Crisis", need
	end

	local normal = LiquidatableValue(base, SellMode.Normal)
	if funds + normal >= need then
		ClearDead(base)
		return "Crisis", need
	end

	local all = LiquidatableValue(base, SellMode.All)
	if funds + all >= need then
		ClearDead(base)
		return "Liquidation", need
	end

	if ConfirmDead(base) then return "Dead", need end
	return "DeadPending", need
end

local function SortEconomyBases()
	local scored = {}
	for i, base in ipairs(bases) do
		if IsEconomyBase(base) then
			local state, need = base.economyState, base.recoveryNeed
			table.insert(scored, {
				base = base,
				rank = StateRank[state] or 9,
				need = need or 0,
				index = i,
			})
		end
	end
	table.sort(scored, function(a, b)
		if a.rank ~= b.rank then return a.rank < b.rank end
		if a.need ~= b.need then return a.need < b.need end
		return a.index < b.index
	end)
	local result = {}
	for _, entry in ipairs(scored) do table.insert(result, entry.base) end
	return result
end

local function PruneSelling(base)
	if base.selling == nil then base.selling = {} end
	local now = DateTime.GameTime
	for actor, info in pairs(base.selling) do
		local expired = now - (info.started or now) >= DateTime.Seconds(SellTimeoutSeconds)
		if not IsLive(actor) or not actor.IsInWorld or expired then
			base.selling[actor] = nil
		end
	end
end

local function UpdateSettling(base)
	PruneSelling(base)
	if next(base.selling) ~= nil then
		base.settling = true
		return
	end
	if base.settleUntil ~= nil and DateTime.GameTime < base.settleUntil then
		base.settling = true
		return
	end
	base.settling = false
	base.settleUntil = nil
	local active = activeSaleBase[base.player]
	if active == base then activeSaleBase[base.player] = nil end
end

local function UpdateIncomeWatchdog(base, hasIncome)
	if not EconomyWatchdogEnabled or not hasIncome then
		base.lastFunds = GDIFunds(base.player)
		base.lastFundsAt = DateTime.GameTime
		base.incomeSuspectSince = nil
		base.incomeFailureLogged = nil
		return
	end
	-- A full storage keeps Cash + Resources flat even while the harvester
	-- delivers. That is not a stalled economy.
	if EconomyIsStorageFull(base.player) then
		base.lastFunds = GDIFunds(base.player)
		base.lastFundsAt = DateTime.GameTime
		base.incomeSuspectSince = nil
		base.incomeFailureLogged = nil
		return
	end
	-- Any AI spending in the window masks income; restart the window.
	if base.rebuilding or base.harvesterBuilding or next(base.selling) ~= nil then
		base.lastFunds = GDIFunds(base.player)
		base.lastFundsAt = DateTime.GameTime
		base.incomeSuspectSince = nil
		base.incomeFailureLogged = nil
		return
	end
	local funds = GDIFunds(base.player)
	if base.lastFunds == nil or funds ~= base.lastFunds then
		-- Both income and spending are activity; a spending drain must not
		-- look like a stuck harvester.
		base.lastFunds = funds
		base.lastFundsAt = DateTime.GameTime
		base.incomeSuspectSince = nil
		base.incomeFailureLogged = nil
	elseif base.lastFundsAt ~= nil
		and DateTime.GameTime - base.lastFundsAt >= DateTime.Seconds(IncomeWindowSeconds) then
		if base.incomeSuspectSince == nil then
			base.incomeSuspectSince = DateTime.GameTime
			RestartIdleHarvesters(base)
			print(("ECON base=%s t=%d action=income-suspect lastFunds=%d")
				:format(base.player.Name, DateTime.GameTime, base.lastFunds))
		end
	end
	if base.incomeSuspectSince ~= nil
		and DateTime.GameTime - base.incomeSuspectSince >= DateTime.Seconds(IncomeFailureSeconds)
		and not base.incomeFailureLogged then
		base.incomeFailureLogged = true
		print(("ECON base=%s t=%d action=income-failure lastFunds=%d")
			:format(base.player.Name, DateTime.GameTime, base.lastFunds))
	end
end

local function UpdateEconomyFacts(base)
	if base.selling == nil then base.selling = {} end
	UpdateSettling(base)
	AdoptFreeHarvester(base)
	-- Anchor the "let the harvester earn the MCV" window to the moment the
	-- construction yard is lost, not to the claim time of the harvester.
	if LocalBuilding(base, "fact") == nil then
		base.factLostAt = base.factLostAt or DateTime.GameTime
	else
		base.factLostAt = nil
	end
	-- Clear a cached critical-damage signal once the chain is healthy again,
	-- otherwise the base would stay Strained forever after one hit.
	if base.hasCriticalDamage then
		local damaged = false
		for _, kind in ipairs({ "fact", "proc", "nuke", "nuk2" }) do
			for _, actor in ipairs(GetLocalActors(base, kind)) do
				if actor.Health < actor.MaxHealth * 0.75 then damaged = true end
			end
		end
		base.hasCriticalDamage = damaged
	end
	local hasIncome = HasStructuralIncome(base)
	UpdateIncomeWatchdog(base, hasIncome)
	if not base.rebuilding and not base.harvesterBuilding then
		ReconcileRecoveryReservation(base)
	end
	local state, need = ClassifyEconomy(base)
	base.economyState = state
	-- Keep nil for "no recovery route"; the status log shows it explicitly.
	base.recoveryNeed = need
	-- Forget the last capacity plant only after power has been normal for a
	-- whole oscillation window. Clearing it on every short Normal caused a
	-- new small NUKE after each building that dropped power again.
	if base.player.PowerState == "Normal" and base.lastPowerAt ~= nil
		and DateTime.GameTime - base.lastPowerAt > DateTime.Seconds(PowerOscillationSeconds)
		and base.rebuildingKind ~= "nuke" and base.rebuildingKind ~= "nuk2" then
		base.lastPowerKind = nil
		base.lastPowerAt = nil
	end
	-- Repairs used to be triggered only by a damage event. Nuke/support-power
	-- damage has no Nod attacker and therefore never reached DefendAgainst,
	-- and a denied repair at damage time was never retried. Sweep here so
	-- damaged buildings are repaired as soon as state/funds allow.
	RepairDamagedBuildings(base)
	-- One compact status line per base per minute makes a stalled base
	-- diagnosable from lua.log without spamming the log.
	if base.lastStatusLog == nil
		or DateTime.GameTime - base.lastStatusLog >= DateTime.Seconds(60) then
		base.lastStatusLog = DateTime.GameTime
		local sells = 0
		for _ in pairs(base.selling or {}) do sells = sells + 1 end
		local factory = LocalBuilding(base, "weap")
		local weap = factory == nil and "missing"
			or ((factory.HasProperty("IsProducing")
				and factory.IsProducing("harv")) and "busy" or "idle")
		print(("ECON status base=%s t=%d state=%s need=%s funds=%d procs=%d harv=%d harvTarget=%d silo=%d res=%d/%d weap=%s rebuilding=%s sells=%d lastPower=%s")
			:format(base.player.Name, DateTime.GameTime, tostring(state), tostring(base.recoveryNeed),
				AvailableFundsForBase(base), #GetLocalActors(base, "proc"), LiveHarvesterCount(base),
				EconomyHarvesterTarget(base), #GetLocalActors(base, "silo"),
				base.player.Resources or 0, EconomyResourceCapacity(base.player),
				weap, tostring(base.rebuildingKind), sells, tostring(base.lastPowerKind)))
	end
end

local function NormalEconomy(base)
	-- Economy first: a missing harvester is useless to replace after units
	-- and optional buildings have consumed the funds.
	RebuildHarvester(base)
	Rebuild(base)
	Produce(base)
	EnsureHelipad(base)
	ProduceOrca(base)
end

local function StrainedEconomy(base)
	RebuildHarvester(base)
	Rebuild(base)
	if HasStructuralIncome(base) and AvailableFunds(base.player) >= Actor.Cost("hpad") + 1500 then
		EnsureHelipad(base)
		ProduceOrca(base)
	end
	Produce(base)
end

local function CrisisEconomy(base, need)
	local funds = AvailableFundsForBase(base)
	if funds < need then
		SellForEconomy(base, need, SellMode.Emergency)
		return -- wait for settlement
	end
	if not TryReserveRecovery(base, need) then return end
	Recover(base)
end

local function LiquidationEconomy(base, need)
	local funds = AvailableFundsForBase(base)
	if funds < need then
		SellForEconomy(base, need, SellMode.All)
		return
	end
	if not TryReserveRecovery(base, need) then return end
	Recover(base)
end

local function DeadEconomy(base)
	if base.deadAt == nil then
		ReleaseRecoveryReservation(base, base.reservedFunds or 0)
		base.recovering = false
		base.deadAt = DateTime.GameTime
		return
	end
	if DateTime.GameTime - base.deadAt < DateTime.Seconds(DeadAllOutGraceSeconds) then return end
	if difficulty ~= "easy" and not base.allOut then
		StartBaseDefense(base, nil)
	end
end

local function ScheduleEconomyTick(delay)
	if MissionFinished or economyTickScheduled then return end
	economyTickScheduled = true
	Trigger.AfterDelay(DateTime.Seconds(delay or 10), function()
		economyTickScheduled = false
		EconomyTick()
	end)
end

local function RunEconomyTick()
	for _, base in ipairs(bases) do
		if base.defending and DateTime.GameTime >= (base.defenseUntil or 0) then
			base.defending = false
		end
		-- Stand down from the all-out after the alarm and a quiet period.
		-- Dead bases keep their commit (DeadEconomy re-triggers it).
		if base.allOut and base.deadAt == nil then
			local quietUntil = math.max(base.defenseUntil or 0,
				(base.underAttackUntil or 0) + DateTime.Seconds(AllOutStandDownSeconds))
			if DateTime.GameTime >= quietUntil then
				base.allOut = false
			end
		end
	end
	for _, base in ipairs(bases) do
		if IsEconomyBase(base) then
			local ok, err = xpcall(function() UpdateEconomyFacts(base) end, EconomyErrorText)
			if not ok then
				print(("ECON ERROR t=%d base=%s facts: %s")
					:format(DateTime.GameTime, base.player.Name, tostring(err)))
			end
		end
	end
	for _, base in ipairs(SortEconomyBases()) do
		if IsEconomyBase(base) then
			local ok, err = xpcall(function()
				if base.rebuilding then
					-- The building lock only forbids new buildings/sales. Unit
					-- production continues so a long (cost-based) AI build does
					-- not make the base look dead for 60-80 s.
					if base.economyState == "Healthy" or base.economyState == "Strained" then
						Produce(base)
					end
					return
				end
				if next(base.selling) ~= nil or base.settling then return end
				local state, need = base.economyState, base.recoveryNeed
				if state == "Healthy" then
					NormalEconomy(base)
				elseif state == "Strained" then
					StrainedEconomy(base)
				elseif state == "Crisis" then
					CrisisEconomy(base, need)
				elseif state == "Liquidation" then
					LiquidationEconomy(base, need)
				elseif state == "DeadPending" then
					-- wait for confirmation/settlement
				elseif state == "Dead" then
					DeadEconomy(base)
				end
			end, EconomyErrorText)
			if not ok then
				print(("ECON ERROR t=%d base=%s action: %s")
					:format(DateTime.GameTime, base.player.Name, tostring(err)))
			end
		end
	end
end

EconomyTick = function()
	if MissionFinished then return end
	-- Never let a single Lua error kill the recursive economy timer. Without
	-- this guard the base freezes forever (no repairs, no buildings, no
	-- production) because ScheduleEconomyTick is never reached.
	local ok, err = xpcall(RunEconomyTick, EconomyErrorText)
	if not ok then
		print(("ECON ERROR t=%d tick: %s"):format(DateTime.GameTime, tostring(err)))
	end
	ScheduleEconomyTick(10)
end

-- Original [Base] rebuild plan, split by owner. Positions are the imported
-- TD structure positions so BuildingAt() can match the surviving footprint.
local westNodes = {
	{ "weap", 19, 55 }, { "pyle", 15, 55 }, { "nuk2", 6, 54 },
	{ "nuk2", 4, 56 }, { "nuk2", 4, 59 }, { "silo", 19, 53 },
	{ "silo", 15, 53 }, { "silo", 17, 53 }, { "proc", 12, 53 },
	{ "fact", 22, 54 }, { "atwr", 27, 60 }, { "atwr", 27, 54 },
	{ "gtwr", 27, 59 }, { "gtwr", 27, 55 }, { "gtwr", 11, 51 },
	{ "gtwr", 7, 51 }, { "eye", 4, 53 }, { "atwr", 6, 51 },
	{ "atwr", 12, 51 }, { "atwr", 17, 55 }, { "hpad", 18, 59 },
	{ "hpad", 20, 59 }, { "hpad", 22, 59 },
}
local northNodes = {
	{ "proc", 39, 2 }, { "weap", 31, 3 }, { "pyle", 36, 3 },
	{ "fact", 43, 13 }, { "fix", 29, 9 }, { "nuk2", 29, 6 },
	{ "nuk2", 34, 2 }, { "nuk2", 46, 13 }, { "silo", 42, 2 },
	{ "silo", 42, 4 }, { "hq", 31, 7 }, { "atwr", 37, 2 },
	{ "atwr", 38, 14 }, { "atwr", 42, 17 }, { "atwr", 52, 15 },
	{ "gtwr", 39, 14 }, { "gtwr", 41, 17 }, { "gtwr", 45, 7 },
	{ "gtwr", 48, 12 }, { "nuke", 53, 18 },
	-- The Communications Center (22,26) is a static scripted outpost, not
	-- part of the north base's rebuild plan.
}
local southNodes = {
	{ "fact", 60, 60 }, { "nuk2", 61, 57 },
}

prerequisites = {
	proc = { "anypower" }, weap = { "proc" }, pyle = { "anypower" },
	gtwr = { "barracks" }, atwr = { "anyhq", "~techlevel.medium" },
	nuke = { "fact" }, nuk2 = { "anyhq", "~techlevel.medium" }, hq = { "proc", "~techlevel.medium" },
	eye = { "anyhq", "~techlevel.high" }, silo = { "proc" },
	hpad = { "proc" }, orca = { "hpad", "anyhq", "~techlevel.medium" },
	fix = { "proc" }, fact = { "proc" },
}

StartBuilding = function(base, entry, yard)
	if base.rebuilding then return false end
	local player = yard.Owner
	local kind = entry[1]
	local cost = Actor.Cost(kind)
	local queue = (kind == "gtwr" or kind == "atwr" or kind == "silo") and "Support.GDI" or "Building.GDI"
	local buildTime = Actor.BuildTime(kind, queue)
	if player.PowerState == "Low" or player.PowerState == "Critical" then
		buildTime = math.ceil(buildTime * 1.5)
	end
	base.rebuilding = true
	base.rebuildingKind = kind
	local spentResources = SpendGDIFunds(player, cost)
	ConsumeRecoveryReservation(base, cost)
	print(("ECON base=%s t=%d action=build-start kind=%s ticks=%d")
		:format(player.Name, DateTime.GameTime, kind, buildTime))
	Trigger.AfterDelay(buildTime, function()
		base.rebuilding = false
		base.rebuildingKind = nil
		if MissionFinished then return end
		local ok, err = xpcall(function()
			if not IsLive(yard) or yard.Owner ~= player or not yard.IsInWorld
				or BuildingAt(entry) ~= nil or not SiteFree(entry, nil, player) then
				local reason = "blocked"
				if not IsLive(yard) or yard.Owner ~= player or not yard.IsInWorld then
					reason = "yard"
				elseif BuildingAt(entry) ~= nil then
					reason = "occupied"
				end
				player.Resources = player.Resources + spentResources
				player.Cash = player.Cash + cost - spentResources
				ReleaseRecoveryReservation(base, cost)
				ReconcileRecoveryReservation(base)
				print(("ECON base=%s t=%d action=build-refund kind=%s reason=%s")
					:format(player.Name, DateTime.GameTime, kind, reason))
				return
			end
			local building = Actor.Create(kind, true, { Owner = player, Location = CPos.New(entry[2], entry[3]) })
			Guard(building)
			if kind == "nuke" or kind == "nuk2" then
				base.lastPowerKind = kind
				base.lastPowerAt = DateTime.GameTime
			end
			if kind == "proc" then
				-- FreeActor: HARV spawns without any production event. Wait one
				-- economy tick before buying a second harvester.
				base.freeActorGraceUntil = DateTime.GameTime + DateTime.Seconds(FreeActorGraceSeconds)
				base.recoveryPowerPolicy = nil
				Trigger.AfterDelay(DateTime.Seconds(FreeActorGraceSeconds), function()
					AdoptFreeHarvester(base)
				end)
			end
			ReconcileRecoveryReservation(base)
			print(("ECON base=%s t=%d action=build-done kind=%s")
				:format(player.Name, DateTime.GameTime, kind))
		end, EconomyErrorText)
		if not ok then
			print(("ECON ERROR t=%d base=%s build=%s: %s")
				:format(DateTime.GameTime, player.Name, kind, tostring(err)))
		end
	end)
	return true
end

-- Bring the production buildings back without their tech gates. Power and
-- refinery are handled by Recover (Crisis path); this only adds the barracks
-- after the income chain stands.
local function Bootstrap(base)
	if not IsEconomyBase(base) or base.rebuilding then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return false end
	if LocalBuilding(base, "proc") == nil then return false end
	if LocalBuilding(base, "pyle") == nil then return TryBuildPyle(base) end
	return false
end

Rebuild = function(base)
	if not IsEconomyBase(base) or base.rebuilding then return end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return end
	-- Missing refinery/harvester is a Crisis and handled by Recover.
	if LocalBuilding(base, "proc") == nil then return end
	if LocalBuilding(base, "pyle") ~= nil and HasStructuralIncome(base) == false then return end

	-- The weapons factory is part of the production chain (harvester
	-- replacement and the only vehicle source). Rebuild it in *every* state,
	-- before optional spending, so infantry/orca production cannot starve it.
	if LocalBuilding(base, "weap") == nil then
		if base.rebuildingKind == "weap" then return end
		if TryBuildWeap(base) then return end
	end

	-- Mission-specific expansion: maintain the configured number of
	-- refineries (nod13c north base: two). Only after the existing chain
	-- stands, and only if the base can afford the reserve.
	local procs = #GetLocalActors(base, "proc")
	if procs > 0 and procs < (base.desiredProcs or 1)
		and AvailableFundsForBase(base) >= Actor.Cost("proc") + 1200 then
		if TryBuildProc(base) then return end
	end

	if Bootstrap(base) then return end

	-- Full storage is a different kind of income problem: add capacity
	-- instead of leaving the harvesters with nowhere to unload.
	if EconomyTryBuildSilo(base) then return end

	-- Low power pauses radar, support powers and base defenses. Add capacity
	-- even when local power plants exist: the player-wide network can still
	-- be short (the west base starts at ~675 drain vs 600 supply).
	local power = base.player.PowerState
	if (power == "Low" or power == "Critical")
		and base.rebuildingKind ~= "nuke" and base.rebuildingKind ~= "nuk2"
		and HasRecoverySurplus(base) then
		if TryBuildPower(base) then return end
	end

	-- The south-western base relies on the EYE instead of an HQ. If both are
	-- gone, build a paid HQ near the yard so the tech chain can recover.
	-- Optional buildings are only rebuilt in Healthy.
	local healthy = base.economyState == "Healthy"
	local harvesterDeficit = EconomyHasHarvesterDeficit(base)
	if healthy and not harvesterDeficit
		and LocalBuilding(base, "hq") == nil and LocalBuilding(base, "eye") == nil
		and AvailableFundsForBase(base) >= Actor.Cost("hq") + 1100
		and base.player.HasPrerequisites(prerequisites.hq) then
		local entry = FindNearbySite(base, "hq", yard.Location, nil)
		if entry ~= nil and StartBuilding(base, entry, yard) then
			return
		end
	end
	if not healthy or harvesterDeficit then return end
	for _ = 1, #base.nodes do
		base.nodeIndex = (base.nodeIndex or 1) % #base.nodes + 1
		local entry = base.nodes[base.nodeIndex]
		-- The EYE is not rebuilt: destroying or capturing it permanently stops
		-- the Ion Cannon and the western relief relay.
		if entry[1] ~= "eye" and entry[1] ~= "proc" and entry[1] ~= "nuke"
			and entry[1] ~= "nuk2" and BuildingAt(entry) == nil then
			local cost = Actor.Cost(entry[1])
			local reserve = (entry[1] == "proc" or entry[1] == "weap" or entry[1] == "pyle") and 1200 or 2200
			local required = prerequisites[entry[1]] or {}
			if AvailableFundsForBase(base) >= cost + reserve and base.player.HasPrerequisites(required)
				and SiteFree(entry, nil, base.player) then
				if StartBuilding(base, entry, yard) then return end
			end
		end
	end
end

-- Harvester replacement only. Sales, Deadtimers and all-out decisions live
-- exclusively in EconomyTick; this function never sells and never commits.
-- Harvesters are never spawned directly: they always go through the weapons
-- factory production queue, with the same prerequisites, cost and build time
-- the player has for them.
RebuildHarvester = function(base, killer)
	if MissionFinished or base == nil then return end
	if not IsEconomyBase(base) then return end
	local target = EconomyHarvesterTarget(base)
	local live = LiveHarvesterCount(base)
	if live >= target then
		base.noHarvesterSince = nil
		return
	end
	if base.rebuilding or base.harvesterBuilding then return end
	if base.freeActorGraceUntil ~= nil and DateTime.GameTime < base.freeActorGraceUntil then return end
	if LocalBuilding(base, "proc") == nil then return end
	local factory = LocalBuilding(base, "weap")
	if factory == nil then return end
	if not LocalTech(base, "harv") or not base.player.HasPrerequisites({ "proc" }) then return end
	local cost = Actor.Cost("harv")
	-- Replacements beyond the first are optional; keep the recovery reserve.
	local reserve = live >= 1 and HealthyReserve or 0
	if AvailableFundsForBase(base) < cost + reserve then return end

	base.harvesterBuilding = true
	if Queue(factory, "harv", base.player) then
		print(("ECON base=%s t=%d action=harv-queue target=%d live=%d")
			:format(base.player.Name, DateTime.GameTime, EconomyHarvesterTarget(base), live))
		-- Safety net only: normally OnProduced clears the flag.
		local timeout = math.ceil(Actor.BuildTime("harv", "Vehicle.GDI")) + 60
		Trigger.AfterDelay(DateTime.Seconds(timeout), function()
			if base.harvesterBuilding then
				base.harvesterBuilding = false
				ReconcileRecoveryReservation(base)
				Trigger.AfterDelay(1, function() if not MissionFinished then EconomyTick() end end)
			end
		end)
		return
	end
	base.harvesterBuilding = false
	if base.lastHarvQueueBlockLog == nil
		or DateTime.GameTime - base.lastHarvQueueBlockLog >= DateTime.Seconds(60) then
		base.lastHarvQueueBlockLog = DateTime.GameTime
		print(("ECON base=%s t=%d action=harv-queue-blocked target=%d live=%d funds=%d")
			:format(base.player.Name, DateTime.GameTime,
				EconomyHarvesterTarget(base), live, AvailableFundsForBase(base)))
	end
end

function Queue(factory, kind, player)
	if not IsLive(factory) or factory.Owner ~= player or not factory.IsInWorld or not factory.HasProperty("Build")
		or not factory.HasProperty("IsProducing") or factory.IsProducing(kind) or GDIFunds(player) < Actor.Cost(kind) then
		return false
	end
	return factory.Build({ kind })
end

local westInfantry = { "e1", "e2", "e3", "e1" }
local northInfantry = { "e3", "e1", "e1", "e1" }
local westVehicles = { "mtnk", "jeep", "jeep" }
local hardWestVehicles = { "htnk", "mtnk", "msam", "jeep" }
local northVehicles = { "htnk", "mtnk", "msam", "jeep" }
local unitPrerequisites = {
	e1 = { "barracks" }, e3 = { "barracks" }, e2 = { "anyhq", "~techlevel.medium" },
	jeep = { "weap" }, mtnk = { "anyhq", "~techlevel.medium" },
	msam = { "anyhq", "~techlevel.medium" }, htnk = { "eye", "~techlevel.high" },
}

local function CombatOrderPending(base)
	local pending = base.combatPending
	if pending == nil then return false end
	local factory = pending.factory
	if not IsLive(factory) or factory.Owner ~= base.player or not factory.IsInWorld
		or not factory.HasProperty("IsProducing") then
		base.combatPending = nil
		return false
	end
	if factory.IsProducing(pending.kind) then
		pending.emptySince = nil
	elseif pending.emptySince == nil then
		pending.emptySince = DateTime.GameTime
	elseif DateTime.GameTime > pending.emptySince then
		base.combatPending = nil
		return false
	end
	return true
end

Produce = function(base)
	local factory = LocalBuilding(base, "weap")
	if LocalBuilding(base, "fact") == nil then
		-- A lost construction yard is recovered by an MCV. This branch must
		-- stay reachable even without a refinery (mcvnofact).
		TryMCVRecovery(base)
		return
	end
	if not HasStructuralIncome(base) then return end
	-- Prioritise restoring the weapons factory over new units. Infantry is
	-- cheap enough to otherwise siphon the 2000 the factory needs.
	if LocalBuilding(base, "weap") == nil then return end
	if CombatOrderPending(base) then return end
	local pyle = LocalBuilding(base, "pyle")
	if (pyle ~= nil and pyle.HasProperty("IsProducing") and pyle.IsProducing("e1"))
		or (factory ~= nil and factory.HasProperty("IsProducing") and factory.IsProducing("jeep")) then return end

	local total, infantryCount, vehicleCount = ProducedCounts(base)
	if total >= MaxProducedUnits[difficulty] then return end
	local categoryLimit = math.max(4, math.floor(MaxProducedUnits[difficulty] / 2))
	-- Keep the weapons factory free for harvester replacement while the base
	-- is below its target. Infantry (PYLE) may continue; vehicles wait.
	local needHarvesters = EconomyHasHarvesterDeficit(base)
	local vehicles = needHarvesters and {}
		or ((difficulty == "hard" and base.hardVehicles) or base.vehicles or northVehicles)
	local infantryTypes = base.infantry or northInfantry
	local specs = base.nextProduction == "vehicle"
		and { { "weap", vehicles, vehicleCount }, { "pyle", infantryTypes, infantryCount } }
		or { { "pyle", infantryTypes, infantryCount }, { "weap", vehicles, vehicleCount } }
	-- Strained means the economy is fragile: do not start expensive units
	-- that would block harvester replacement or eat the recovery reserve.
	local strained = base.economyState == "Strained"
	for _, spec in ipairs(specs) do
		local producer = LocalBuilding(base, spec[1])
		if producer ~= nil and spec[3] < categoryLimit then
			for _ = 1, #spec[2] do
				local kind = table.remove(spec[2], 1)
				table.insert(spec[2], kind)
				local affordable = not strained or Actor.Cost(kind) <= 1000
				if affordable and LocalTech(base, kind) and base.player.HasPrerequisites(unitPrerequisites[kind])
					and AvailableFunds(base.player) >= Actor.Cost(kind) + HealthyReserve
					and Queue(producer, kind, base.player) and producer.IsProducing(kind) then
					base.combatPending = { factory = producer, kind = kind }
					base.nextProduction = spec[1] == "pyle" and "vehicle" or "infantry"
					return
				end
			end
		end
	end
end

local function OnProduced(factory, unit, productionType)
	if MissionFinished or not IsLive(factory) or not IsLive(unit)
		or not IsGDIPlayer(factory.Owner) or unit.Owner ~= factory.Owner then return end
	local base = BaseForOwnedBuilding(factory) or BaseForActor(factory) or BaseAt(factory.Location)
	if base == nil then return end
	if base.combatPending ~= nil and base.combatPending.factory == factory
		and base.combatPending.kind == unit.Type then base.combatPending = nil end
	if unit.Type == "mcv" then
		base.mcvBuilding = false
		Guard(unit)
		base.mcv = unit
		base.mcvSite = nil
		WatchMCV(base, unit, 0)
	elseif unit.Type == "harv" then
		base.harvesterBuilding = false
		Guard(unit)
		ClaimHarvester(base, unit)
		TrackHarvester(base, unit)
		ConsumeRecoveryReservation(base, Actor.Cost("harv"))
		ReconcileRecoveryReservation(base)
		print(("ECON base=%s t=%d action=harv-produced live=%d target=%d")
			:format(base.player.Name, DateTime.GameTime,
				LiveHarvesterCount(base), EconomyHarvesterTarget(base)))
		if unit.HasProperty("FindResources") then unit.FindResources() end
	elseif unit.Type == "orca" then
		GuardOrca(base, unit)
	elseif unit.HasProperty("AttackMove") then
		Guard(unit)
		table.insert(base.produced, unit)
		JoinGarrison(base, unit)
	end
end

EnsureHelipad = function(base)
	if MissionFinished or base.rebuilding or LocalBuilding(base, "hpad") ~= nil then return end
	if not HasStructuralIncome(base) then return end
	if LocalBuilding(base, "weap") == nil then return end
	if EconomyHasHarvesterDeficit(base) then return end
	local yard = LocalBuilding(base, "fact")
	if yard == nil or AvailableFunds(base.player) < Actor.Cost("hpad") + 1500 then return end
	if not LocalTech(base, "hpad") or not base.player.HasPrerequisites(prerequisites.hpad) then return end
	local entry = FindNearbySite(base, "hpad", yard.Location, nil)
	if entry ~= nil then StartBuilding(base, entry, yard) end
end

-- Every helipad of a base keeps one Orca. The western base has three pads,
-- so it can maintain three Orcas instead of sharing a global cap. Counting
-- per player/base also removes the global-cap race between bases.
local function LocalBuildingCount(base, kind)
	local count = 0
	for _, actor in ipairs(base.player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.IsInWorld and IsLocal(base, actor) then count = count + 1 end
	end
	return count
end

ProduceOrca = function(base)
	if MissionFinished or base.orcaBusy ~= nil then return end
	if not HasStructuralIncome(base) then return end
	if LocalBuilding(base, "weap") == nil then return end
	if EconomyHasHarvesterDeficit(base) then return end
	local pad = LocalBuilding(base, "hpad")
	local pads = LocalBuildingCount(base, "hpad")
	if pad == nil or pads == 0 then return end
	if #base.player.GetActorsByType("orca") >= pads then return end
	if not LocalTech(base, "orca") or not base.player.HasPrerequisites(prerequisites.orca)
		or AvailableFunds(base.player) < Actor.Cost("orca") + 800 then return end

	local stamp = DateTime.GameTime
	base.orcaBusy = stamp
	if pad.Build({ "orca" }, function(units)
		if base.orcaBusy ~= stamp then return end
		base.orcaBusy = nil
		for _, orca in ipairs(units) do
			if IsLive(orca) and orca.Type == "orca" then
				GuardOrca(base, orca)
			end
		end
	end) then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			if base.orcaBusy == stamp then base.orcaBusy = nil end
		end)
	else
		base.orcaBusy = nil
	end
end

local function CheckGDIEconomy()
	if MissionFinished then return end
	for _, base in ipairs(bases) do
		if base.defending and DateTime.GameTime >= base.defenseUntil then base.defending = false end
	end
	EconomyTick()
end

-- Idle defenders engage nearby enemies instead of waiting for the first hit.
-- Units on the attack have Stance AttackAnything and are excluded here.
local function IdleDefense()
	if MissionFinished then return end
	for _, player in ipairs(gdiPlayers) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if IsLive(unit) and unit.IsInWorld and unit.IsIdle and unit.HasProperty("AttackMove")
				and unit.Type ~= "harv"
				and unit.HasProperty("Stance") and unit.Stance == "Defend"
				and not IsCentralOutpostActor(unit) then
				local target = ClosestEnemy(unit, 16)
				if target ~= nil then unit.AttackMove(target.Location, 2) end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), IdleDefense)
end

-- atk1..atk7 from the original mission. All seven teams enter at WP3 in
-- the western pass and advance to WP4 before attacking the player's
-- mainland base.
local waveUnits = {
	{ "e1", "e1", "e1", "e2", "e2" },
	{ "e2", "e2", "e2", "e3", "e3" },
	{ "e3", "e3", "e3", "e6", "e6" },
	{ "jeep", "jeep" },
	{ "mtnk", "msam" },
	{ "mtnk", "mtnk" },
	{ "htnk" },
}
local waveOffsets = { 0, 120, 240, 420, 540, 600, 840 }

-- Spawn groups get distinct cells and never fall back to the raw anchor:
-- the north-east corner can be partly Rough or blocked, which previously
-- stacked a whole wave on one cell until it was attacked.
local function SpawnCellNear(anchor, used)
	local cell = FindUnitSpot(anchor, used, 24)
	if cell ~= nil then return cell end
	for _, offset in ipairs({ { -4, 6 }, { -7, 8 }, { -10, 6 }, { 0, 16 }, { -6, 14 } }) do
		cell = FindUnitSpot(CPos.New(anchor.X + offset[1], anchor.Y + offset[2]), used, 12)
		if cell ~= nil and SameLand(anchor, cell) then return cell end
	end
	return nil
end

local function SpawnGroupNear(waypoint, kinds, owner)
	owner = owner or AnyGDIPlayer()
	if owner == nil then return {} end
	local team = {}
	local used = {}
	for _, kind in ipairs(kinds) do
		local cell = SpawnCellNear(waypoint.Location, used)
		if cell ~= nil then
			local unit = Actor.Create(kind, true, { Owner = owner, Location = cell })
			if IsLive(unit) then
				Guard(unit)
				if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
				table.insert(team, unit)
			end
		end
	end
	return team
end

local function ScriptedWave(index)
	if MissionFinished then return end
	local owner = WaveOwner()
	if owner == nil then return end
	local team = SpawnGroupNear(WaveSpawn, waveUnits[index], owner)
	SendTeam(team, owner, { waypoint23, waypoint4 }, true, true)
end

-- atk1..atk7 keep their original spacing, but the whole sequence waits
-- until Nod has four buildings on the mainland and the northern base's
-- Communications Center is still in GDI hands.
local function StartWaveSequence(index)
	if MissionFinished or not WaveSourceAlive() then return end
	if not WavesUnlocked() then
		Trigger.AfterDelay(DateTime.Seconds(30), function() StartWaveSequence(index) end)
		return
	end
	ScriptedWave(index)
	if index < #waveOffsets then
		local gap = waveOffsets[index + 1] - waveOffsets[index]
		Trigger.AfterDelay(DateTime.Seconds(gap), function() StartWaveSequence(index + 1) end)
	end
end

-- Autocreate waves come from the northern base only and also wait for the
-- four-building mainland foothold.
local function AutoWave()
	if MissionFinished or not WaveSourceAlive() then return end
	if WavesUnlocked() then
		local sent = false
		for _, base in ipairs(bases) do
			if base.player == GDI and not HouseEliminated(base.player) then
				local team = TakeForAttack(base, 3)
				if #team > 0 then
					SendTeam(team, base.player, {}, true)
					sent = true
				end
				break
			end
		end
		if not sent then
			local owner = WaveOwner()
			if owner ~= nil then
				local team = SpawnGroupNear(WaveSpawn, { "jeep", "e1", "e1" }, owner)
				if #team > 0 then SendTeam(team, owner, { waypoint23, waypoint4 }, true) end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(AutoWaveInterval[difficulty]), AutoWave)
end

local function RandomNodTarget(minDistanceFromEye)
	local eye = nil
	if minDistanceFromEye ~= nil then
		if not IsLive(GDIEYE) then return nil end
		eye = GDIEYE
	end

	local targets = {}
	for _, actor in ipairs(Nod.GetGroundAttackers()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location") then
			local valid = true
			if eye ~= nil then
				local dx = actor.Location.X - eye.Location.X
				local dy = actor.Location.Y - eye.Location.Y
				valid = dx * dx + dy * dy > minDistanceFromEye * minDistanceFromEye
			end
			if valid then table.insert(targets, actor) end
		end
	end
	if #targets == 0 and minDistanceFromEye == nil then
		for _, actor in ipairs(Nod.GetActors()) do
			if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Health")
				and actor.HasProperty("Location") and not actor.HasProperty("Land") then
				table.insert(targets, actor)
			end
		end
	end
	if #targets > 0 then return Utils.Random(targets) end
	return nil
end

-- xxxx: the airstrike trigger is tied to the northern base's Communications
-- Center (GDINorthHQ). Destroying or capturing it stops the strikes
-- permanently through StopAirstrikes() in the mission script.
local function Airstrike()
	if MissionFinished or AirstrikeStopped then return end
	if not IsLive(GDINorthHQ) or not IsGDIPlayer(GDINorthHQ.Owner)
		or not GDINorthHQ.HasProperty("TargetAirstrike") then
		Trigger.AfterDelay(DateTime.Seconds(60), Airstrike)
		return
	end
	local target = RandomNodTarget(nil)
	if target ~= nil then GDINorthHQ.TargetAirstrike(target.CenterPosition) end
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeInterval[difficulty]), Airstrike)
end

-- zzzz/timed Ion Cannon: nod13b timings. The EYE is the source; destroying
-- or capturing it stops the cannon permanently through StopEyePowers().
local function IonCannonStrike()
	if MissionFinished or IonCannonStopped then return end
	if not IsLive(GDIEYE) or not IsGDIPlayer(GDIEYE.Owner)
		or not GDIEYE.HasProperty("ActivateIonCannon") then
		Trigger.AfterDelay(DateTime.Seconds(30), IonCannonStrike)
		return
	end
	local power = GDIEYE.Owner.PowerState
	if power == "Low" or power == "Critical" then
		Trigger.AfterDelay(DateTime.Seconds(60), IonCannonStrike)
		return
	end
	local target = RandomNodTarget(10)
	if target == nil then
		Trigger.AfterDelay(DateTime.Seconds(15), IonCannonStrike)
		return
	end
	GDIEYE.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[difficulty]), IonCannonStrike)
end

-- apc1: Nod enters the western pass. GDI reacts with APCs from the
-- south-western base. Cells decoded from the original [CellTriggers].
local Apc1Cells = {
	CPos.New(12, 23), CPos.New(13, 23), CPos.New(12, 24), CPos.New(13, 24), CPos.New(12, 25),
	CPos.New(13, 25), CPos.New(12, 26), CPos.New(13, 26), CPos.New(12, 27), CPos.New(13, 27),
}

-- apc2: Nod approaches the northern base along the eastern pass.
local Apc2Cells = {
	CPos.New(45, 1), CPos.New(45, 2), CPos.New(45, 3), CPos.New(45, 4), CPos.New(45, 5),
	CPos.New(43, 6), CPos.New(45, 6), CPos.New(43, 7), CPos.New(46, 7), CPos.New(43, 8),
	CPos.New(44, 8), CPos.New(45, 8), CPos.New(43, 9), CPos.New(44, 9), CPos.New(45, 9),
	CPos.New(43, 10), CPos.New(44, 10), CPos.New(38, 11), CPos.New(39, 11), CPos.New(40, 11),
	CPos.New(41, 11), CPos.New(42, 11), CPos.New(43, 11), CPos.New(44, 11), CPos.New(38, 12),
	CPos.New(39, 12), CPos.New(40, 12), CPos.New(41, 12), CPos.New(42, 12), CPos.New(43, 12),
	CPos.New(44, 12), CPos.New(34, 13), CPos.New(35, 13), CPos.New(36, 13), CPos.New(37, 13),
	CPos.New(38, 13), CPos.New(34, 14), CPos.New(35, 14), CPos.New(36, 14), CPos.New(37, 14),
	CPos.New(34, 15),
}

-- map.yaml already assigns the three base regions. This split is kept as a
-- safety net (and for the Lua harness, whose actor list starts as GDI):
-- x <= 28 and y >= 45 belongs to the south-western base, x >= 45 and y >= 45
-- to the south-eastern outpost. Harvesters follow the refinery they are
-- actually working from.
local function SplitGDI()
	local procs = {}
	for _, base in ipairs(bases) do
		local proc = LocalBuilding(base, "proc")
		if proc ~= nil then table.insert(procs, { player = base.player, proc = proc }) end
	end
	for _, actor in ipairs(GDI.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location") then
			local x, y = actor.Location.X, actor.Location.Y
			local newOwner = GDI
			if actor.Type == "harv" then
				local best, bestDistance
				for _, entry in ipairs(procs) do
					local dx = x - entry.proc.Location.X
					local dy = y - entry.proc.Location.Y
					local distance = dx * dx + dy * dy
					if best == nil or distance < bestDistance then
						best, bestDistance = entry.player, distance
					end
				end
				if best ~= nil then newOwner = best end
			elseif y >= 45 and x <= 28 then
				newOwner = GDIWest
			elseif y >= 45 and x >= 45 then
				newOwner = GDISouth
			end
			if newOwner ~= GDI then actor.Owner = newOwner end
		end
	end
end

local function InitializeEconomyBaseFields(base)
	base.defending = false
	base.defenseUntil = 0
	base.reserveRelease = 0
	base.combatPending = nil
	base.rebuilding = false
	base.rebuildingKind = nil
	base.orcaBusy = nil
	base.harvester = nil
	base.harvesters = {}
	base.harvesterSince = nil
	base.harvesterBuilding = false
	base.reliefCooldown = 0
	base.mcv = nil
	base.mcvSite = nil
	base.nodeIndex = 1
	base.economyState = nil
	base.recoveryNeed = 0
	base.selling = {}
	base.settleUntil = nil
	base.settling = false
	base.deadSince = nil
	base.deadAt = nil
	base.harvesterLost = false
	base.freeActorGraceUntil = nil
	base.economyScheduled = false
	base.hasCriticalDamage = false
	base.reservedFunds = 0
	base.recovering = false
	base.recoveryPowerPolicy = nil
	base.lastFunds = nil
	base.lastFundsAt = nil
	base.incomeSuspectSince = nil
	base.lastPowerKind = nil
	base.lastPowerAt = nil
	base.incomeFailureLogged = nil
	base.lastHarvQueueBlockLog = nil
	base.lastStatusLog = nil
	base.lastSaleBlockLog = nil
	base.allOut = false
	base.underAttackUntil = 0
end

-- Harness hook: add a second economy base for a player to exercise the
-- multi-base claim/reservation logic. The mission itself never calls this.
function AddEconomyBase(player, yardX, yardY, nodes)
	local base = { player = player, yard = { yardX, yardY }, nodes = nodes or {},
		reserve = {}, produced = {}, infantry = northInfantry, vehicles = northVehicles,
		nextProduction = "infantry" }
	InitializeEconomyBaseFields(base)
	table.insert(bases, base)
	return base
end

-- Harness/diagnostic hook. Reports the economy facts per base for a player.
function EconomyDebug(player)
	local out = { bases = {} }
	for _, base in ipairs(bases) do
		if base.player == player then
			local sellCount = 0
			for _ in pairs(base.selling or {}) do sellCount = sellCount + 1 end
			out.bases[#out.bases + 1] = {
				state = base.economyState,
				need = base.recoveryNeed,
				procs = #GetLocalActors(base, "proc"),
				silos = #GetLocalActors(base, "silo"),
				harvesters = LiveHarvesterCount(base),
				harvesterCount = HarvesterCount(base),
				target = EconomyHarvesterTarget(base),
				resources = base.player.Resources or 0,
				capacity = EconomyResourceCapacity(base.player),
				reserved = base.reservedFunds or 0,
				recovering = base.recovering == true,
				suspect = IsIncomeSuspect(base),
				allOut = base.allOut == true,
				selling = sellCount,
				activeSale = activeSaleBase[player] == base,
			}
		end
	end
	out.reserved = ReservedFunds(player)
	out.funds = GDIFunds(player)
	return out
end

SetupAI = function()
	difficulty = Difficulty or "normal"
	GDI = GDI or Player.GetPlayer("GDI")
	GDIWest = GDIWest or Player.GetPlayer("GDIWest")
	GDISouth = GDISouth or Player.GetPlayer("GDISouth")
	gdiPlayers = { GDIWest, GDI, GDISouth }
	local cash = math.floor(StartingCash[difficulty] / 2)
	GDI.Cash = cash
	GDIWest.Cash = cash
	GDISouth.Cash = 0
	BuildLandRegions()

	bases = {
		{ player = GDIWest, yard = { 22, 54 }, nodes = westNodes, reserve = {}, produced = {},
			infantry = westInfantry, vehicles = westVehicles, hardVehicles = hardWestVehicles,
			nextProduction = "infantry" },
		{ player = GDI, yard = { 43, 13 }, nodes = northNodes, reserve = {}, produced = {},
			infantry = northInfantry, vehicles = northVehicles, nextProduction = "infantry",
			-- Mission-specific (nod13c only): a richer northern base with two
			-- refineries and two harvesters each. Disabled by default so the
			-- generic recovery behaviour is unchanged everywhere else.
			desiredProcs = MultiRefineryEnabled and 2 or 1,
			harvestersPerProc = MultiRefineryEnabled and 2 or 1 },
		{ player = GDISouth, yard = { 60, 60 }, nodes = southNodes, reserve = {}, produced = {},
			infantry = northInfantry, vehicles = { "mtnk", "jeep" }, canRelieve = false,
			nextProduction = "infantry" },
	}
	for _, base in ipairs(bases) do
		InitializeEconomyBaseFields(base)
	end
	harvesterClaims = {}
	reservedFunds = {}
	activeSaleBase = {}
	SplitGDI()

	for _, player in ipairs(gdiPlayers) do
		for _, harv in ipairs(player.GetActorsByType("harv")) do
			Guard(harv)
			local base = BaseForHarvester(harv)
			if base ~= nil then TrackHarvester(base, harv) end
			if harv.HasProperty("FindResources") then harv.FindResources() end
		end
	end

	for _, player in ipairs(gdiPlayers) do
		for _, actor in ipairs(player.GetActors()) do
			if IsLive(actor) and actor.IsInWorld then
				if actor.HasProperty("StartBuildingRepairs") then
					Guard(actor)
				elseif actor.Type == "orca" then
					GuardOrca(BaseForPlayer(actor.Owner), actor)
				elseif actor.Type ~= "harv" and actor.HasProperty("AttackMove") then
					Guard(actor)
					local localToBase = false
					for _, base in ipairs(bases) do
						if base.player == actor.Owner and IsLocal(base, actor)
							and SameLand(actor.Location, CPos.New(base.yard[1], base.yard[2])) then
							localToBase = true
							break
						end
					end
					-- Original SCB13EC: the two E3 at (4,9) on the starting
					-- peninsula are Area Guard, not Hunt. Keep them on Defend
					-- so they only react when Nod comes close.
					local areaGuard = actor.Type == "e3"
						and actor.Location.X == 4 and actor.Location.Y == 9
					local centralOutpostGuard = IsCentralOutpostActor(actor)
					if actor.HasProperty("Stance") then
						actor.Stance = (localToBase or areaGuard or centralOutpostGuard) and "Defend" or "AttackAnything"
					end
					if not localToBase and not areaGuard and not centralOutpostGuard then IdleHunt(actor) end
				end
			end
		end
	end

	-- Key building losses are serious enough to call the other bases for help.
	for _, base in ipairs(bases) do
		for _, actor in ipairs(base.player.GetActors()) do
			if IsLive(actor) and actor.IsInWorld and BaseReliefBuildings[actor.Type]
				and not IsCentralOutpostActor(actor) and base.player ~= GDISouth then
				Trigger.OnKilled(actor, function(_, killer)
					RequestRelief(base)
					-- Easy/Normal also commit the base if a key building is
					-- destroyed in one Nod hit before the damage alarm could fire.
					-- Ignore self-destruction/selling (no Nod killer).
					if difficulty ~= "hard" and BaseDefenseBuildings[actor.Type]
						and killer ~= nil and IsLive(killer) and killer.Owner == Nod then
						StartBaseDefense(base, killer)
					end
				end)
				Trigger.OnCapture(actor, function(_, _, _, newOwner)
					if not IsGDIPlayer(newOwner) then
						RequestRelief(base)
						if difficulty ~= "hard" and BaseDefenseBuildings[actor.Type] then
							StartBaseDefense(base, nil)
						end
					end
				end)
			end
		end
	end

	Trigger.OnAnyProduction(OnProduced)

	Trigger.AfterDelay(DateTime.Seconds(WaveStart[difficulty]), function() StartWaveSequence(1) end)

	-- trup: three GDI infantry patrol the western approach.
	Trigger.AfterDelay(DateTime.Seconds(60), function()
		RunWhenUnlocked(function()
			for _, unit in ipairs(SpawnGroupNear(WaveSpawn, { "e1", "e2", "e3" }, GDI)) do
				if unit.HasProperty("Patrol") and SameLand(unit.Location, waypoint19.Location) then
					if unit.HasProperty("Stance") then unit.Stance = "Defend" end
					unit.Patrol({ waypoint23.Location, waypoint19.Location, waypoint18.Location }, true, DateTime.Seconds(12))
				else
					SendTeam({ unit }, unit.Owner, {}, true)
				end
			end
		end)
	end)

	-- pat1: a Medium Tank patrols the western pass.
	Trigger.AfterDelay(DateTime.Seconds(90), function()
		RunWhenUnlocked(function()
			for _, unit in ipairs(SpawnGroupNear(WaveSpawn, { "mtnk" }, GDI)) do
				if unit.HasProperty("Patrol") then
					if unit.HasProperty("Stance") then unit.Stance = "Defend" end
					unit.Patrol({ waypoint23.Location, waypoint3.Location, waypoint4.Location, waypoint2.Location },
						true, DateTime.Seconds(15))
				end
			end
		end)
	end)

	-- pat2: two Medium Tanks patrol the eastern and southern roads.
	Trigger.AfterDelay(DateTime.Seconds(120), function()
		RunWhenUnlocked(function()
			for _, unit in ipairs(SpawnGroupNear(WaveSpawn, { "mtnk", "mtnk" }, GDI)) do
				if unit.HasProperty("Patrol") then
					if unit.HasProperty("Stance") then unit.Stance = "Defend" end
					unit.Patrol({ waypoint23.Location, waypoint2.Location, waypoint5.Location,
						waypoint6.Location, waypoint7.Location, waypoint8.Location, waypoint9.Location },
						true, DateTime.Seconds(18))
				end
			end
		end)
	end)

	-- apc1: Nod entering the western pass draws an APC counter-attack.
	Trigger.OnEnteredFootprint(Apc1Cells, function(actor, id)
		if actor.Owner ~= Nod then return end
		Trigger.RemoveFootprintTrigger(id)
		RunWhenUnlocked(function()
			local team = SpawnGroupNear(WaveSpawn, { "e2", "e2", "e2", "e3", "e3", "apc" }, GDI)
			SendTeam(team, GDI, { waypoint23, waypoint4 }, true)
		end)
	end)

	-- apc2: Nod approaching the northern base draws a second APC group.
	-- The original group carried two engineers into an APC to capture a Nod
	-- building. The port cannot model that transport/capture run, and
	-- engineers in an attack-move group would just idle at the north-east
	-- spawn, so the group is limited to combat units.
	Trigger.OnEnteredFootprint(Apc2Cells, function(actor, id)
		if actor.Owner ~= Nod then return end
		Trigger.RemoveFootprintTrigger(id)
		RunWhenUnlocked(function()
			local team = SpawnGroupNear(WaveSpawn, { "e3", "e3", "e3", "e3", "e3", "apc" }, GDI)
			SendTeam(team, GDI, {}, true, true)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(AirstrikeFirstDelay[difficulty]), Airstrike)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[difficulty] - 60), function()
		if IonCannonStopped or not IsLive(GDIEYE) or not IsGDIPlayer(GDIEYE.Owner) then return end
		Media.PlaySpeechNotification(Nod, "IonCannonCharging")
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	end)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[difficulty]), IonCannonStrike)
	Trigger.AfterDelay(DateTime.Seconds(AutoWaveStart[difficulty]), AutoWave)
	print(("ECON session start t=%d difficulty=%s multi=%s version=%s")
		:format(DateTime.GameTime, tostring(difficulty),
			MultiRefineryEnabled and "on" or "off", ECONOMY_LOG_VERSION))
	CheckGDIEconomy()
	IdleDefense()
	OrcaWatch()
end
