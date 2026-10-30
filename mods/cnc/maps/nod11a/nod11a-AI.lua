--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI attacks, cell triggers and production for Nod mission 11a.

-- Seconds after the Nod base was taken over. Hard is the original schedule
-- plus 20 s; Normal and Easy are stretched further.
WaveTimes = {
	easy = { chn1 = 15, atk2 = 290, atk3 = 305, zzzz = 325, atk4 = 340, atk5 = 390, atk6 = 410, auto = 495 },
	normal = { chn1 = 10, atk2 = 230, atk3 = 245, zzzz = 255, atk4 = 270, atk5 = 310, atk6 = 325, auto = 390 },
	hard = { chn1 = 5, atk2 = 170, atk3 = 180, zzzz = 190, atk4 = 200, atk5 = 230, atk6 = 240, auto = 290 },
}

-- Seconds between repeated A10 strikes (HQ alive, fewer than 3 powered SAMs).
AirstrikeInterval = { easy = 160, normal = 140, hard = 120 }

-- Seconds after the base takeover until GDI starts producing units.
ProductionStartDelay = { easy = 420, normal = 300, hard = 240 }
-- Produced units wait in a pool. When the pool is full (reserve + group size)
-- GDI sends a group of the oldest units on an attack route and keeps the
-- reserve as defenders. MaxProducedUnits = reserve + group size.
AttackGroupSize = { easy = 4, normal = 5, hard = 6 }
DefenderReserve = { easy = 2, normal = 3, hard = 4 }
MaxProducedUnits = { easy = 6, normal = 8, hard = 10 }
-- Minimum seconds between two produced attack groups.
AttackGroupCooldown = { easy = 150, normal = 110, hard = 80 }
-- Seconds during which damage reactions of the defenders are not repeated.
DefenseLockoutSeconds = 10

-- Extra strong assault this many seconds after the last scripted wave (atk6).
-- Not in the original mission.
FinalWaveDelay = 420
-- Hard only: every further final wave gets FinalWaveExtraStep more units
-- (up to FinalWaveMaxUnits), and once the first one has come every auto wave
-- gets extra units (see AutoWaveExtras).
FinalWaveExtraStep = { "e2", "e3" }
FinalWaveMaxUnits = 25
FinalWaveCount = 0
FinalWaveUnits = {
	easy = { "e2", "e2", "e2", "e2", "e3", "e3", "e3", "mtnk", "mtnk", "htnk" },
	normal = { "e1", "e1", "e2", "e2", "e2", "e2", "e3", "e3", "e3", "mtnk", "mtnk", "jeep", "htnk" },
	hard = { "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e3", "e3", "e3", "e3", "mtnk", "mtnk", "mtnk", "jeep", "htnk", "htnk" },
}

-- The original "Autocreate" trigger keeps producing teams from the auto1..auto8
-- pool instead of spawning a single team.
AutoWaveDelays = {
	-- Stretched (~1.5x) because produced groups now add pressure.
	easy = { min = 210, max = 285 },
	normal = { min = 150, max = 210 },
	hard = { min = 120, max = 165 },
}

-- TD [Triggers] "Time" values are seconds (e.g. SCG05EB atk1=40 is
-- 40 seconds in the gdi05b port), not 1/10 minute.
local function WaveDelay(seconds)
	return DateTime.Seconds(seconds)
end

InitialUnits = {
	Engineer1, Engineer2, Engineer3,
	Rifleman1, Rifleman2, Rifleman3, Rifleman4,
	RocketSoldier1, RocketSoldier2, RocketSoldier3, RocketSoldier4,
	Commando,
}

HuntBuildings = {
	GDINuke1, GDINuke2, GDINuke3,
	GDIBarracks, GDICYard, GDIProc, GDIWeaponsFactory,
}

-- Waves enter on the top map edge (y = 3) instead of inside the playable
-- area, so they walk in rather than pop up.
WaveSpawn = CPos.New(waypoint6.Location.X, 3)

TeamDefs = {
	atk1 = { units = { "e1", "e1", "e2", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	atk2 = { units = { "e2", "e2", "e3", "e3", "e3" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	atk3 = { units = { "e1", "e1", "e1", "e1", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	atk4 = { units = { "e1", "e1", "e1", "e1", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	atk5 = { units = { "e1", "e1", "e2", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	atk6 = { units = { "mtnk", "jeep" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	auto1 = { units = { "e2", "e2", "e2", "e3", "e3", "e3" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	auto2 = { units = { "e1", "e1", "e1", "e1", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	auto3 = { units = { "jeep", "jeep" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	auto4 = { units = { "e1", "e1", "mtnk" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	auto5 = { units = { "msam", "e2", "e2", "e2" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location } },
	auto6 = { units = { "msam", "e3", "e3" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint10.Location, waypoint12.Location, waypoint13.Location } },
	auto7 = { units = { "e1", "e1", "htnk" }, start = WaveSpawn, moves = { waypoint1.Location, waypoint7.Location, waypoint11.Location, waypoint9.Location, waypoint8.Location } },
	auto8 = { units = { "e1", "e1", "mtnk" }, start = WaveSpawn, moves = { waypoint1.Location } },
}

-- The first AutoInfantryOnlyWaves auto waves are drawn from infantry-only teams.
AutoInfantryOnlyWaves = 4
AutoInfantryTeamNames = { "auto1", "auto2" }
AutoWaveCount = 0

AutoTeamNames = { "auto1", "auto2", "auto3", "auto4", "auto5", "auto6", "auto7", "auto8" }

GDIBaseBuildings = {
	{ type = "nuke", pos = CPos.New(54, 4) },
	{ type = "nuke", pos = CPos.New(53, 6) },
	{ type = "nuke", pos = CPos.New(42, 4) },
	{ type = "nuke", pos = CPos.New(56, 4) },
	{ type = "atwr", pos = CPos.New(47, 12) },
	{ type = "atwr", pos = CPos.New(56, 12) },
	{ type = "gtwr", pos = CPos.New(54, 14) },
	{ type = "gtwr", pos = CPos.New(49, 14) },
	{ type = "silo", pos = CPos.New(46, 4) },
	{ type = "silo", pos = CPos.New(52, 4) },
	{ type = "silo", pos = CPos.New(48, 4) },
	{ type = "silo", pos = CPos.New(50, 4) },
	{ type = "pyle", pos = CPos.New(51, 6) },
	{ type = "fact", pos = CPos.New(41, 7) },
	{ type = "proc", pos = CPos.New(45, 6) },
	{ type = "hq", pos = CPos.New(48, 6) },
	{ type = "weap", pos = CPos.New(56, 8) },
}

GDIProductionTypes = {
	pyle = { "e1", "e2", "e3" },
	weap = { "jeep", "mtnk", "htnk" },
}

YyyyDisabled = false
ProdTriggered = false
GDIProductionStarted = false
AirstrikeDisabled = false
BaseDefenseTriggered = false
AttackScheduleStarted = false
-- Set for good once the GDI Construction Yard is destroyed or captured: no more waves.
WavesStopped = false
Atk1Sent = false
ProducedGDIUnits = {}
ProductionBusy = {}
OrcaBusy = nil
NextGroupTime = 0
DefenseLockUntil = 0

local function Cells(indices)
	local cells = {}
	for _, cell in ipairs(indices) do
		table.insert(cells, CPos.New(cell % 64, math.floor(cell / 64)))
	end
	return cells
end

Atk1Cells = Cells({
	2831, 2830, 2829, 2767, 2766, 2765,
	2703, 2702, 2701, 2639, 2638, 2637,
})

DelyCells = Cells({
	2574, 2573, 2572, 2571, 2570, 2569, 2568, 2567,
	2510, 2509, 2508, 2507, 2506, 2505, 2504, 2503,
	2446, 2445, 2444, 2443, 2442, 2441, 2440, 2439,
})

-- Units spawn at the start cell and then attack-move along the path, so they
-- engage everything on the way instead of walking past it.
SendTeam = function(units, start, moves)
	Reinforcements.Reinforce(GDI, units, { start }, DateTime.Seconds(0.25), function(unit)
		unit.Stance = "AttackAnything"
		Utils.Do(moves, function(cell)
			unit.AttackMove(cell)
		end)
		IdleHunt(unit)
	end)
end

AttackWave = function(def)
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	SendTeam(def.units, def.start, def.moves)
end

FinalWave = function()
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	local units = {}
	Utils.Do(FinalWaveUnits[Difficulty or "normal"], function(unit) table.insert(units, unit) end)

	-- Hard: the 2nd, 3rd, ... final wave is slightly bigger.
	if Difficulty == "hard" then
		for _ = 1, FinalWaveCount do
			Utils.Do(FinalWaveExtraStep, function(unit)
				if #units < FinalWaveMaxUnits then
					table.insert(units, unit)
				end
			end)
		end
	end
	FinalWaveCount = FinalWaveCount + 1

	Media.PlaySpeechNotification(Nod, "EnemyUnitsApproaching")
	SendTeam(units, WaveSpawn,
		{ waypoint1.Location, waypoint4.Location, waypoint5.Location, waypoint9.Location, waypoint8.Location })

	-- On Hard the final wave keeps coming.
	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Seconds(FinalWaveDelay), FinalWave)
	end
end

ChinookDrop = function()
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	local reinforcements = Reinforcements.ReinforceWithTransport(GDI, "tran.in", { "e2", "e2", "e2", "e2", "e2" },
		{ waypoint3.Location, waypoint0.Location }, { waypoint3.Location })

	Utils.Do(reinforcements[2], function(unit)
		unit.Stance = "AttackAnything"
		IdleHunt(unit)
	end)
end

CheckForSams = function()
	return #Nod.GetActorsByType("sam") >= 3 and Nod.PowerState == "Normal"
end

-- Until minute 20 (Hard: 15) of the mission the southern force (4 ARTY, 6 E1,
-- 6 E3, found by position at start) is protected from the A10s, as if it had
-- not been found yet. Not only those units but also everything within
-- SouthSafeRadius of a living member is skipped, so napalm cannot splash onto
-- them, and the protection follows the units wherever the player moves them.
-- Afterwards they are normal targets.
SouthSafeUntil = { easy = DateTime.Minutes(20), normal = DateTime.Minutes(20), hard = DateTime.Minutes(15) }
SouthSafeRadius = 10 * 1024
SouthGroup = {}

InitSouthGroup = function()
	Utils.Do(Nod.GetActors(), function(actor)
		if actor.HasProperty("Location") and actor.Location.X >= 44 and actor.Location.Y >= 18 then
			table.insert(SouthGroup, actor)
		end
	end)
end

-- Distance in world units (1024 per cell) between two positions.
DistanceBetween = function(a, b)
	local dx = a.X - b.X
	local dy = a.Y - b.Y
	return math.sqrt(dx * dx + dy * dy)
end

IsInAirstrikeSafeZone = function(actor)
	if DateTime.GameTime >= SouthSafeUntil[Difficulty or "normal"] then
		return false
	end

	for _, member in ipairs(SouthGroup) do
		if not member.IsDead and (member == actor or DistanceBetween(actor.CenterPosition, member.CenterPosition) <= SouthSafeRadius) then
			return true
		end
	end

	return false
end

-- Like GetAirstrikeTarget from campaign.lua (ground attackers, away from
-- SAM sites), but skipping the safe zone.
GetNodAirstrikeTarget = function()
	local candidates = Utils.Where(Nod.GetGroundAttackers(), function(actor)
		return not actor.IsDead and not IsInAirstrikeSafeZone(actor)
	end)

	if #candidates == 0 then
		return nil
	end

	-- Group the candidates into clusters (within 8 cells of a cluster's first
	-- unit) and pick a random CLUSTER, so the biggest army is not the
	-- most likely target.
	local clusters = {}
	Utils.Do(candidates, function(actor)
		for _, cluster in ipairs(clusters) do
			if DistanceBetween(actor.CenterPosition, cluster[1].CenterPosition) <= 8 * 1024 then
				table.insert(cluster, actor)
				return
			end
		end
		table.insert(clusters, { actor })
	end)

	for _ = 1, 6 do
		local cluster = Utils.Random(clusters)
		local target = Utils.Random(cluster).CenterPosition
		local sams = Map.ActorsInCircle(target, WDist.New(8 * 1024), function(actor)
			return actor.Type == "sam"
		end)

		if #sams == 0 then
			return target
		end
	end

	return nil
end

Airstrike = function()
	if not BaseTakenOver or AirstrikeDisabled or GDIHQ.IsDead or GDIHQ.Owner ~= GDI
		or not GDIHQ.HasProperty("TargetAirstrike") then
		return
	end

	-- Three powered SAMs stop the strikes (see the "Build 3 SAMs" objective).
	-- Keep checking in case they are lost or the power drops.
	if CheckForSams() then
		Trigger.AfterDelay(DateTime.Seconds(10), Airstrike)
		return
	end

	local target = GetNodAirstrikeTarget()
	if target then
		GDIHQ.TargetAirstrike(target)
		Trigger.AfterDelay(DateTime.Seconds(AirstrikeInterval[Difficulty or "normal"]), Airstrike)
	else
		-- No valid target (none or all under SAM cover): try again shortly.
		Trigger.AfterDelay(DateTime.Seconds(5), Airstrike)
	end
end

-- Hard only, after the first final wave: 2, 3, then 4 extra infantry units.
AutoWaveExtras = function()
	local extras = {}
	if Difficulty ~= "hard" or FinalWaveCount < 1 then
		return extras
	end

	local pool = { "e1", "e2", "e3", "e1" }
	for i = 1, math.min(1 + FinalWaveCount, 4) do
		table.insert(extras, pool[i])
	end
	return extras
end

AutoWave = function()
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	local names = AutoTeamNames
	if AutoWaveCount < AutoInfantryOnlyWaves then
		names = AutoInfantryTeamNames
	end
	AutoWaveCount = AutoWaveCount + 1

	local def = TeamDefs[Utils.Random(names)]
	local extras = AutoWaveExtras()
	if #extras > 0 then
		local units = {}
		Utils.Do(def.units, function(unit) table.insert(units, unit) end)
		Utils.Do(extras, function(unit) table.insert(units, unit) end)
		def = { units = units, start = def.start, moves = def.moves }
	end
	AttackWave(def)

	local delays = AutoWaveDelays[Difficulty or "normal"]
	Trigger.AfterDelay(Utils.RandomInteger(DateTime.Seconds(delays.min), DateTime.Seconds(delays.max)), AutoWave)
end

-- Rebuilding of the GDI main base (positions = GDIBaseBuildings).
--  * At the start of production every missing building is queued (all difficulties).
--  * Hard only: production-critical buildings are queued again every time they
--    are killed (OnKilled) and buildings GDI sold for money come back once the
--    economy runs again.
--  * One building at a time, paid up front, placed after its normal build time.
--  * Order by importance (Construction Yard first); an unaffordable important
--    building does not block cheap ones (<= RebuildCheapCost).
--  * Everything except the Construction Yard needs a living Construction Yard.
--  * The site must be free: GDI units are sent away, enemy units are waited out.
RebuildQueue = {}
RebuildWorkerRunning = false
RebuildBusy = false
RebuildPending = {}
RebuildPollSeconds = 5
RebuildCheapCost = 500
RebuildSiteTimeoutAttempts = 30
-- Seconds a sold building needs before it counts as gone (sell animation).
RebuildSoldDelaySeconds = 15

GDIBuildingPriority = { fact = 1, proc = 2, weap = 3, pyle = 4, hq = 5, nuke = 6, atwr = 7, gtwr = 8, hpad = 9, silo = 10 }
-- Occupied cells (width, height) of each building, used for the free-site check.
GDIBuildingFootprint = {
	fact = { 3, 2 }, proc = { 3, 3 }, weap = { 3, 2 }, pyle = { 2, 2 }, hq = { 2, 2 },
	nuke = { 2, 2 }, atwr = { 1, 2 }, gtwr = { 1, 1 }, hpad = { 2, 2 }, silo = { 2, 1 },
}
-- Hard: restored every time they are killed.
HardRebuildTypes = { fact = true, proc = true, weap = true, pyle = true }
-- Hard, while GDI has no harvester: only these are rebuilt (money is needed for the harvester).
EconomyRebuildTypes = { fact = true, proc = true, weap = true }

BuildingKey = function(building)
	return building.type .. ":" .. building.pos.X .. "," .. building.pos.Y
end

GDIBuildingExists = function(building)
	local exists = false
	Utils.Do(GDI.GetActorsByType(building.type), function(actor)
		if not actor.IsDead and actor.Location.X == building.pos.X and actor.Location.Y == building.pos.Y then
			exists = true
		end
	end)
	return exists
end

IsMainBaseBuilding = function(buildingType, pos)
	for _, building in ipairs(GDIBaseBuildings) do
		if building.type == buildingType and building.pos.X == pos.X and building.pos.Y == pos.Y then
			return true
		end
	end
	return false
end

-- Ground units standing on the building site (aircraft do not block).
BlockingUnits = function(building)
	local size = GDIBuildingFootprint[building.type] or { 2, 2 }
	local topLeft = WPos.New(building.pos.X * 1024, building.pos.Y * 1024, 0)
	local bottomRight = WPos.New((building.pos.X + size[1]) * 1024, (building.pos.Y + size[2]) * 1024, 0)
	return Map.ActorsInBox(topLeft, bottomRight, function(actor)
		return not actor.IsDead and actor.HasProperty("Move") and not actor.HasProperty("Land")
	end)
end

-- Sends GDI units off the site; returns true if the site is free. Units of
-- other players cannot be moved, the rebuild simply waits for them to leave.
ClearBuildingSite = function(building)
	local size = GDIBuildingFootprint[building.type] or { 2, 2 }
	local free = true
	Utils.Do(BlockingUnits(building), function(unit)
		free = false
		if unit.Owner == GDI then
			unit.Move(CPos.New(building.pos.X, building.pos.Y + size[2] + 2))
		end
	end)
	return free
end

RegisterGDIBuilding = function(actor)
	Trigger.OnDamaged(actor, PullDefenders)

	if Difficulty == "hard" then
		local info = { type = actor.Type, pos = actor.Location }
		if HardRebuildTypes[info.type] and IsMainBaseBuilding(info.type, info.pos) then
			Trigger.OnKilled(actor, function()
				QueueGDIRebuild(info, false)
			end)
		end
	end
end

StartRebuildWorker = function()
	if RebuildWorkerRunning or not GDIProductionStarted then
		return
	end

	RebuildWorkerRunning = true
	Trigger.AfterDelay(DateTime.Seconds(1), ProcessGDIRebuild)
end

QueueGDIRebuild = function(building, sold)
	local key = BuildingKey(building)
	for _, entry in ipairs(RebuildQueue) do
		if BuildingKey(entry.building) == key then
			return
		end
	end

	local due = nil
	if sold then
		due = DateTime.GameTime + DateTime.Seconds(RebuildSoldDelaySeconds)
	end

	table.insert(RebuildQueue, { building = building, sold = sold, due = due })
	StartRebuildWorker()
end

-- Called once when GDI production starts: queue everything that is missing.
StartGDIRebuild = function()
	for _, building in ipairs(GDIBaseBuildings) do
		if not GDIBuildingExists(building) then
			QueueGDIRebuild(building, false)
		end
	end
end

-- Pays now, places the building after its build time (retrying while the site
-- is blocked; after RebuildSiteTimeoutAttempts tries the money is refunded and
-- the building queued again).
PlaceGDIBuilding = function(building)
	RebuildPending[BuildingKey(building)] = true
	RebuildBusy = true
	GDI.Cash = GDI.Cash - Actor.Cost(building.type)

	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		FinishGDIBuilding(building, 0)
	end)
end

FinishGDIBuilding = function(building, attempt)
	local key = BuildingKey(building)

	if GDIBuildingExists(building) then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	if not ClearBuildingSite(building) then
		if attempt >= RebuildSiteTimeoutAttempts then
			GDI.Cash = GDI.Cash + Actor.Cost(building.type)
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building, false)
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			FinishGDIBuilding(building, attempt + 1)
		end)
		return
	end

	RegisterGDIBuilding(Actor.Create(building.type, true, { Owner = GDI, Location = building.pos }))
	RebuildPending[key] = nil
	RebuildBusy = false
end

PickRebuildCandidate = function()
	local now = DateTime.GameTime
	local hard = Difficulty == "hard"
	local yardAlive = #GDI.GetActorsByType("fact") > 0
	local eligible = {}

	for _, entry in ipairs(RebuildQueue) do
		local buildingType = entry.building.type
		local ok = (entry.due == nil or now >= entry.due) and not GDIBuildingExists(entry.building)

		if ok and buildingType ~= "fact" and not yardAlive then
			ok = false
		end
		if ok and hard and NoHarvesterSince ~= nil and (entry.sold or not EconomyRebuildTypes[buildingType]) then
			ok = false
		end
		if ok and buildingType == "proc" and RefineryBusy then
			ok = false
		end

		if ok then
			table.insert(eligible, entry)
		end
	end

	table.sort(eligible, function(a, b)
		return GDIBuildingPriority[a.building.type] < GDIBuildingPriority[b.building.type]
	end)

	for index, entry in ipairs(eligible) do
		local cost = Actor.Cost(entry.building.type)
		if GDI.Cash >= cost and (index == 1 or cost <= RebuildCheapCost) then
			return entry
		end
	end

	return nil
end

ProcessGDIRebuild = function()
	if GDI.HasNoRequiredUnits() then
		RebuildWorkerRunning = false
		return
	end

	if RebuildBusy then
		Trigger.AfterDelay(DateTime.Seconds(2), ProcessGDIRebuild)
		return
	end

	-- Drop entries whose building exists again (keep sold ones until due).
	local now = DateTime.GameTime
	local remaining = {}
	for _, entry in ipairs(RebuildQueue) do
		if (entry.due ~= nil and now < entry.due) or not GDIBuildingExists(entry.building) then
			table.insert(remaining, entry)
		end
	end
	RebuildQueue = remaining

	if #RebuildQueue == 0 then
		RebuildWorkerRunning = false
		return
	end

	local candidate = PickRebuildCandidate()
	if candidate ~= nil then
		local rest = {}
		for _, entry in ipairs(RebuildQueue) do
			if entry ~= candidate then
				table.insert(rest, entry)
			end
		end
		RebuildQueue = rest
		PlaceGDIBuilding(candidate.building)
	end

	Trigger.AfterDelay(DateTime.Seconds(RebuildPollSeconds), ProcessGDIRebuild)
end

CountProducedUnits = function()
	local alive = {}
	for _, actor in ipairs(ProducedGDIUnits) do
		if not actor.IsDead then
			table.insert(alive, actor)
		end
	end
	ProducedGDIUnits = alive
	return #alive
end

-- Sends the oldest produced units as one attack group once the pool holds the
-- defender reserve plus a full group. Routes are the scripted wave routes.
GroupRoutes = { "atk2", "atk5" }

TrySendGroup = function()
	if BaseDefenseTriggered or DateTime.GameTime < NextGroupTime then
		return
	end

	local difficulty = Difficulty or "normal"
	local size = AttackGroupSize[difficulty]
	if CountProducedUnits() < size + DefenderReserve[difficulty] then
		return
	end

	NextGroupTime = DateTime.GameTime + DateTime.Seconds(AttackGroupCooldown[difficulty])

	local moves = TeamDefs[Utils.Random(GroupRoutes)].moves
	local remaining = {}
	for i, unit in ipairs(ProducedGDIUnits) do
		if i <= size then
			unit.Stance = "AttackAnything"
			Utils.Do(moves, function(cell)
				unit.AttackMove(cell)
			end)
			IdleHunt(unit)
		else
			table.insert(remaining, unit)
		end
	end
	ProducedGDIUnits = remaining
end

-- A GDI building or harvester was hit: the waiting defenders attack-move to the
-- attacker and return to where they stood. Not repeated for a few seconds.
PullDefenders = function(_, attacker)
	if BaseDefenseTriggered or DateTime.GameTime < DefenseLockUntil then
		return
	end

	if attacker == nil or attacker.IsDead or not attacker.HasProperty("Location") then
		return
	end

	DefenseLockUntil = DateTime.GameTime + DateTime.Seconds(DefenseLockoutSeconds)

	local target = attacker.Location

	Utils.Do(ProducedGDIUnits, function(unit)
		if not unit.IsDead then
			local home = unit.Location
			unit.Stop()
			unit.AttackMove(target)
			unit.Move(home)
		end
	end)

	-- Orcas hover where they stop and never go back by themselves, so they are
	-- sent home to the helipad (landing there also repairs them) after the attack.
	Utils.Do(GDI.GetActorsByType("orca"), function(orca)
		if not orca.IsDead and not IsOrcaRetreating(orca) then
			orca.Stop()
			orca.AttackMove(target)
			orca.ReturnToBase()
		end
	end)
end

-- An Orca that lost more than 75 % of its health flies home to the helipad to
-- be repaired and is left out of further attacks until it is repaired
-- (OrcaWatch). Without a helipad it keeps fighting.
OrcaRetreating = {}

IsOrcaRetreating = function(orca)
	for _, other in ipairs(OrcaRetreating) do
		if other == orca then
			return true
		end
	end
	return false
end

SetOrcaRetreating = function(orca, retreating)
	local list = {}
	for _, other in ipairs(OrcaRetreating) do
		if other ~= orca and not other.IsDead then
			table.insert(list, other)
		end
	end
	if retreating then
		table.insert(list, orca)
	end
	OrcaRetreating = list
end

OrcaDamaged = function(orca)
	if orca.IsDead or IsOrcaRetreating(orca) then
		return
	end

	if orca.Health * 4 > orca.MaxHealth or #GDI.GetActorsByType("hpad") == 0 then
		return
	end

	SetOrcaRetreating(orca, true)
	orca.Stop()
	orca.ReturnToBase()
end

OrcaWatch = function()
	if GDI.HasNoRequiredUnits() then
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(5), OrcaWatch)

	Utils.Do(GDI.GetActorsByType("orca"), function(orca)
		if IsOrcaRetreating(orca) and orca.Health * 10 >= orca.MaxHealth * 9 then
			SetOrcaRetreating(orca, false)
			if BaseDefenseTriggered then
				IdleHunt(orca)
			end
		end
	end)
end

-- Orcas from the helipad defend the base: they wait on the pad and join
-- PullDefenders / the main-base alarm. SAMs are the counter.
MaxOrcas = { easy = 1, normal = 2, hard = 3 }

ProduceOrca = function()
	if GDI.HasNoRequiredUnits() then
		return
	end

	local retry = function() ProduceOrca() end
	local pads = GDI.GetActorsByType("hpad")

	if #pads == 0 or #GDI.GetActorsByType("harv") == 0
		or #GDI.GetActorsByType("orca") >= MaxOrcas[Difficulty or "normal"] then
		Trigger.AfterDelay(DateTime.Seconds(15), retry)
		return
	end

	local stamp = DateTime.GameTime
	OrcaBusy = stamp
	local queued = pads[1].Build({ "orca" }, function(units)
		if OrcaBusy ~= stamp then
			return
		end

		OrcaBusy = nil
		Utils.Do(units, function(orca)
			orca.Stance = "AttackAnything"
			Trigger.OnDamaged(orca, OrcaDamaged)
			if BaseDefenseTriggered then
				IdleHunt(orca)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(10), retry)
	end)

	if queued then
		-- Watchdog for a pad destroyed mid-build.
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			if OrcaBusy == stamp then
				OrcaBusy = nil
				retry()
			end
		end)
	else
		OrcaBusy = nil
		Trigger.AfterDelay(DateTime.Seconds(15), retry)
	end
end

-- Real production through the GDI Barracks / Weapons Factory. Produced units
-- stay in the base as defenders; once the main base is attacked they hunt.
ProduceGDIUnit = function(buildingType)
	if GDI.HasNoRequiredUnits() then
		return
	end

	local retry = function() ProduceGDIUnit(buildingType) end
	local factories = GDI.GetActorsByType(buildingType)

	-- Production depends on the economy: no harvester, no new units.
	if #factories == 0 or #GDI.GetActorsByType("harv") == 0
		or CountProducedUnits() >= MaxProducedUnits[Difficulty or "normal"] then
		TrySendGroup()
		Trigger.AfterDelay(DateTime.Seconds(10), retry)
		return
	end

	local types = GDIProductionTypes[buildingType]
	local unitType = table.remove(types, 1)
	table.insert(types, unitType)

	local stamp = DateTime.GameTime
	local queued = factories[1].Build({ unitType }, function(units)
		-- Ignore a late callback after the watchdog below already moved on.
		if ProductionBusy[buildingType] ~= stamp then
			return
		end

		ProductionBusy[buildingType] = nil
		Utils.Do(units, function(unit)
			table.insert(ProducedGDIUnits, unit)
			if BaseDefenseTriggered then
				IdleHunt(unit)
			end
		end)
		TrySendGroup()
		Trigger.AfterDelay(DateTime.Seconds(5), retry)
	end)

	if queued then
		ProductionBusy[buildingType] = stamp

		-- Watchdog: a factory destroyed mid-build never calls back.
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			if ProductionBusy[buildingType] == stamp then
				ProductionBusy[buildingType] = nil
				retry()
			end
		end)
	else
		Trigger.AfterDelay(DateTime.Seconds(5), retry)
	end
end

-- Hard only: if GDI has lost its last harvester it (1) builds a new one at the
-- Weapons Factory, (2) without a factory re-creates the refinery (a new
-- refinery brings a free harvester), (3) sells buildings for money (never the
-- Construction Yard, HQ, Weapons Factory or Refinery; power plants only last
-- and only while power stays sufficient), (4) as a last resort all
-- GDI ground units and Orcas attack.
EconomySellOrder = { "hpad", "silo", "gtwr", "atwr", "pyle" }
-- Power plants are sold last and only while the remaining power still covers
-- the drain (never the HQ: it is not in any sell list).
PowerPlantType = "nuke"
PowerPlantOutput = 100
EconomyGraceSeconds = 30
EconomySellCooldownSeconds = 20
GDIRefineryPosition = CPos.New(45, 6)
NoHarvesterSince = nil
HarvesterBuilding = false
RefineryBusy = false
LastEconomySale = -100000
AllOutAttackStarted = false

GDIFunds = function()
	return GDI.Cash + GDI.Resources
end

BuildGDIHarvester = function(factory)
	HarvesterBuilding = true
	local stamp = DateTime.GameTime
	HarvesterStamp = stamp

	local queued = factory.Build({ "harv" }, function(units)
		if HarvesterStamp ~= stamp then
			return
		end

		HarvesterBuilding = false
		Utils.Do(units, function(harv)
			harv.FindResources()
		end)
	end)

	if queued then
		Trigger.AfterDelay(DateTime.Seconds(120), function()
			if HarvesterStamp == stamp then
				HarvesterBuilding = false
			end
		end)
	else
		HarvesterBuilding = false
	end
end

-- Creates the refinery at `position` (paid, after its build time). A fresh
-- refinery spawns a free harvester.
CreateGDIRefinery = function(position, delay)
	RefineryBusy = true
	Trigger.AfterDelay(delay, function()
		local cost = Actor.Cost("proc")
		if GDI.Cash >= cost then
			GDI.Cash = GDI.Cash - cost
			RegisterGDIBuilding(Actor.Create("proc", true, { Owner = GDI, Location = position }))
		end
		RefineryBusy = false
	end)
end

ReplaceGDIRefinery = function(proc)
	local position = proc.Location
	proc.Sell()
	CreateGDIRefinery(position, DateTime.Seconds(6) + Actor.BuildTime("proc"))
end

-- Returns true while a sale was made or is still cooling down, false if there
-- is nothing left to sell.
SellForEconomy = function()
	local candidate = nil
	for _, buildingType in ipairs(EconomySellOrder) do
		local list = GDI.GetActorsByType(buildingType)
		if #list > 0 then
			candidate = list[1]
			break
		end
	end

	if candidate == nil then
		local plants = GDI.GetActorsByType(PowerPlantType)
		if #plants > 0 and GDI.PowerProvided - PowerPlantOutput >= GDI.PowerDrained then
			candidate = plants[1]
		end
	end

	if candidate == nil then
		return false
	end

	if DateTime.GameTime - LastEconomySale >= DateTime.Seconds(EconomySellCooldownSeconds) then
		LastEconomySale = DateTime.GameTime
		QueueGDIRebuild({ type = candidate.Type, pos = candidate.Location }, true)
		candidate.Sell()
	end

	return true
end

CheckGDIEconomy = function()
	if GDI.HasNoRequiredUnits() or AllOutAttackStarted then
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(10), CheckGDIEconomy)

	if #GDI.GetActorsByType("harv") > 0 then
		NoHarvesterSince = nil
		return
	end

	if NoHarvesterSince == nil then
		NoHarvesterSince = DateTime.GameTime
	end

	if HarvesterBuilding or RefineryBusy or RebuildPending[BuildingKey({ type = "proc", pos = GDIRefineryPosition })] then
		return
	end

	local factories = GDI.GetActorsByType("weap")
	local refineries = GDI.GetActorsByType("proc")

	if #factories > 0 and #refineries > 0 then
		if GDIFunds() >= Actor.Cost("harv") then
			BuildGDIHarvester(factories[1])
			return
		end
	elseif GDI.Cash >= Actor.Cost("proc") then
		-- No factory (or no refinery): a new refinery brings a free harvester.
		if #refineries > 0 then
			ReplaceGDIRefinery(refineries[1])
		else
			CreateGDIRefinery(GDIRefineryPosition, Actor.BuildTime("proc"))
		end
		return
	end

	-- Not enough money: sell something after a short grace period.
	if DateTime.GameTime - NoHarvesterSince < DateTime.Seconds(EconomyGraceSeconds) then
		return
	end

	if SellForEconomy() then
		return
	end

	-- Nothing left to do: all-out attack.
	AllOutAttackStarted = true
	StartBaseDefense()
end

TryStartGDIProduction = function()
	if not BaseTakenOver or not ProdTriggered or GDIProductionStarted then
		return
	end

	GDIProductionStarted = true
	StartGDIRebuild()
	ProduceGDIUnit("pyle")
	ProduceGDIUnit("weap")
	ProduceOrca()
	OrcaWatch()

	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Seconds(10), CheckGDIEconomy)
	end
end

StartBaseDefense = function()
	if BaseDefenseTriggered then
		return
	end

	BaseDefenseTriggered = true
	Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
	Utils.Do(GDI.GetActorsByType("orca"), IdleHunt)
end

StartAttackSchedule = function()
	if AttackScheduleStarted then
		return
	end

	AttackScheduleStarted = true
	local times = WaveTimes[Difficulty or "normal"]

	Trigger.AfterDelay(DateTime.Seconds(4), StartMammothPatrol)
	Trigger.AfterDelay(WaveDelay(times.chn1), ChinookDrop)
	Trigger.AfterDelay(WaveDelay(times.atk2), function() AttackWave(TeamDefs.atk2) end)
	Trigger.AfterDelay(WaveDelay(times.atk3), function() AttackWave(TeamDefs.atk3) end)
	Trigger.AfterDelay(WaveDelay(times.atk4), function() AttackWave(TeamDefs.atk4) end)
	Trigger.AfterDelay(WaveDelay(times.atk5), function() AttackWave(TeamDefs.atk5) end)
	Trigger.AfterDelay(WaveDelay(times.atk6), function() AttackWave(TeamDefs.atk6) end)
	Trigger.AfterDelay(WaveDelay(times.zzzz), Airstrike)
	Trigger.AfterDelay(WaveDelay(times.auto), AutoWave)
	-- Final wave: Easy never, Normal once, Hard repeatedly (see FinalWave).
	if Difficulty ~= "easy" then
		Trigger.AfterDelay(WaveDelay(times.atk6) + DateTime.Seconds(FinalWaveDelay), FinalWave)
	end

	-- GDI starts producing units only after this fixed delay following the
	-- takeover (the original `prod` cell trigger no longer starts it early).
	Trigger.AfterDelay(DateTime.Seconds(ProductionStartDelay[Difficulty or "normal"]), function()
		ProdTriggered = true
		TryStartGDIProduction()
	end)
end

SetupAI = function()
	InitSouthGroup()

	Trigger.OnEnteredFootprint(Atk1Cells, function(actor, id)
		if actor.Owner == Nod and BaseTakenOver and not Atk1Sent then
			-- Several units can enter in the same tick; send the team only once.
			Atk1Sent = true
			Trigger.RemoveFootprintTrigger(id)
			AttackWave(TeamDefs.atk1)
		end
	end)

	Trigger.OnEnteredFootprint(DelyCells, function(actor, id)
		if actor.Owner == Nod then
			Trigger.RemoveFootprintTrigger(id)
			YyyyDisabled = true
		end
	end)

	Trigger.OnAllRemovedFromWorld(InitialUnits, function()
		-- Once the Nod base is taken over the player is no longer dependent on
		-- the starting units; the regular HasNoRequiredUnits check applies.
		if not YyyyDisabled and not BaseTakenOver then
			Nod.MarkFailedObjective(EliminateGDI)
		end
	end)

	Trigger.OnAllKilled(HuntBuildings, function()
		Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
	end)

	-- React to attacks on the main GDI base (x >= 40, y <= 15): all GDI ground
	-- units start hunting and the base begins rebuilding/producing.
	Utils.Do(Map.ActorsInWorld, function(actor)
		if actor.Owner == GDI and actor.HasProperty("StartBuildingRepairs")
			and actor.Location.X >= 40 and actor.Location.Y <= 15 then
			Trigger.OnDamaged(actor, StartBaseDefense)
		end
	end)

	-- Any damaged GDI building or harvester pulls the waiting defenders in.
	Utils.Do(GDI.GetActors(), function(actor)
		if actor.HasProperty("Location") then
			if actor.HasProperty("StartBuildingRepairs") then
				RegisterGDIBuilding(actor)
			elseif actor.Type == "harv" then
				Trigger.OnDamaged(actor, PullDefenders)
			end
		end
	end)

	-- No more waves once the GDI Construction Yard is gone (it may still be
	-- rebuilt, the waves stay stopped).
	local stopWaves = function() WavesStopped = true end
	Trigger.OnKilled(GDICYard, stopWaves)
	Trigger.OnCapture(GDICYard, stopWaves)

	Trigger.OnKilled(GDIHQ, function()
		AirstrikeDisabled = true
	end)

	TryStartGDIProduction()
end
