--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI attacks, cell triggers and production for Nod mission 11b.

-- Seconds after mission start. The 11a port interprets the original TD
-- trigger time values as seconds; 11b uses the same convention so both
-- variants play at a comparable pace. The original values are in tenths of
-- a minute (6 s), so the schedule below is compressed by roughly the same
-- factor as 11a.
WaveTimes = {
	easy = { atk2 = 330, atk3 = 360, atk4 = 380, atk5 = 400, atk7 = 700, atk6 = 710, auto = 480 },
	normal = { atk2 = 270, atk3 = 300, atk4 = 320, atk5 = 340, atk7 = 600, atk6 = 610, auto = 400 },
	hard = { atk2 = 210, atk3 = 240, atk4 = 255, atk5 = 270, atk7 = 510, atk6 = 520, auto = 330 },
}

-- The original A10 strike is a single reinforcement at 140 (14 min). The
-- 11a port turned it into a repeating strike; 11b keeps that deviation and
-- uses the same timings, targeting and SAM defence as 11a. Destroying the
-- GDI HQ stops the strikes (dely in the original).
AirstrikeFirstDelay = { easy = 325, normal = 255, hard = 190 }
AirstrikeInterval = { easy = 160, normal = 140, hard = 120 }

-- Seconds after mission start until GDI starts producing units if the player
-- has not entered the original `prod` cell trigger yet.
ProductionStartDelay = { easy = 420, normal = 300, hard = 240 }
AttackGroupSize = { easy = 4, normal = 5, hard = 6 }
DefenderReserve = { easy = 2, normal = 3, hard = 4 }
MaxProducedUnits = { easy = 6, normal = 8, hard = 10 }
AttackGroupCooldown = { easy = 150, normal = 110, hard = 80 }
DefenseLockoutSeconds = 10

AutoWaveDelays = {
	easy = { min = 210, max = 285 },
	normal = { min = 150, max = 210 },
	hard = { min = 120, max = 165 },
}

-- Extra strong assault this many seconds after the last scripted wave (atk6),
-- as in 11a. Not in the original mission. Easy never, Normal once, Hard
-- repeatedly; on Hard every further final wave gets FinalWaveExtraStep more
-- units (up to FinalWaveMaxUnits) and every auto wave gets extras.
FinalWaveDelay = 420
FinalWaveExtraStep = { "e2", "e3" }
FinalWaveMaxUnits = 25
FinalWaveCount = 0
FinalWaveUnits = {
	easy = { "e2", "e2", "e2", "e2", "e3", "e3", "e3", "mtnk", "mtnk", "htnk" },
	normal = { "e1", "e1", "e2", "e2", "e2", "e2", "e3", "e3", "e3", "mtnk", "mtnk", "jeep", "htnk" },
	hard = { "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e3", "e3", "e3", "e3", "mtnk", "mtnk", "mtnk", "jeep", "htnk", "htnk" },
}

local function WaveDelay(seconds)
	return DateTime.Seconds(seconds)
end

HuntBuildings = {
	GDINuke1, GDINuke2, GDINuke3,
	GDIBarracks, GDICYard, GDIProc, GDIWeaponsFactory,
}

-- West waves enter at waypoint0 (41,15), the southern gate of the GDI main
-- base. East waves enter at waypoint7 (53,4).
WestSpawn = CPos.New(41, 15)
EastSpawn = CPos.New(53, 2)
APCStart = CPos.New(60, 4)

WestRoute = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location }
EastRoute = { waypoint7.Location, waypoint8.Location, waypoint9.Location, waypoint10.Location, waypoint11.Location, waypoint4.Location, waypoint5.Location }

TeamDefs = {
	gdi1 = { units = { "e1", "e1", "e1", "e2", "e2" }, start = WestSpawn, moves = WestRoute },
	gdi2 = { units = { "e2", "e2", "e2", "e3", "e3" }, start = WestSpawn, moves = WestRoute },
	gdi3 = { units = { "e1", "e1", "jeep" }, start = WestSpawn, moves = WestRoute },
	gdi4 = { units = { "mtnk", "jeep" }, start = EastSpawn, moves = EastRoute },
	gdi5 = { units = { "e2", "e2", "e3", "e3" }, start = EastSpawn, moves = { waypoint7.Location, waypoint8.Location } },
	auto1 = { units = { "e1", "e1", "e2", "e2" }, start = WestSpawn, moves = WestRoute },
	auto2 = { units = { "e2", "e2", "e2", "e3", "e3" }, start = WestSpawn, moves = WestRoute },
	auto3 = { units = { "msam" }, start = WestSpawn, moves = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint6.Location } },
	auto4 = { units = { "htnk" }, start = EastSpawn, moves = EastRoute },
	auto5 = { units = { "e1", "e1", "e2", "e2", "e3", "e3" }, start = WestSpawn, moves = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location } },
	auto6 = { units = { "e3", "e3", "jeep" }, start = WestSpawn, moves = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location } },
	auto7 = { units = { "apc" }, start = WestSpawn, moves = WestRoute },
	heavy1 = { units = { "apc" }, start = WestSpawn, moves = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location, waypoint13.Location } },
	heavy2 = { units = { "mtnk", "mtnk" }, start = WestSpawn, moves = WestRoute },
}

-- The first two AutoInfantryOnlyWaves auto waves are drawn from infantry-only teams.
AutoInfantryOnlyWaves = 2
AutoInfantryTeamNames = { "auto1", "auto2", "auto5" }
AutoWaveCount = 0
AutoTeamNames = { "auto1", "auto2", "auto3", "auto4", "auto5", "auto6", "auto7" }

GDIBaseBuildings = {
	{ type = "nuke", pos = CPos.New(41, 3) },
	{ type = "nuke", pos = CPos.New(36, 6) },
	{ type = "nuke", pos = CPos.New(38, 6) },
	{ type = "atwr", pos = CPos.New(45, 10) },
	{ type = "atwr", pos = CPos.New(39, 10) },
	{ type = "gtwr", pos = CPos.New(40, 13) },
	{ type = "gtwr", pos = CPos.New(44, 13) },
	{ type = "silo", pos = CPos.New(47, 3) },
	{ type = "silo", pos = CPos.New(46, 5) },
	{ type = "silo", pos = CPos.New(45, 3) },
	{ type = "pyle", pos = CPos.New(40, 6) },
	{ type = "fact", pos = CPos.New(36, 3) },
	{ type = "proc", pos = CPos.New(43, 4) },
	{ type = "weap", pos = CPos.New(47, 7) },
}

GDIProductionTypes = {
	pyle = { "e1", "e2", "e3" },
	weap = { "jeep", "mtnk", "htnk" },
}

-- GDI production, waves and airstrikes are gated on the base takeover.
BaseTakenOver = false
ProdTriggered = false
GDIProductionStarted = false
AirstrikeDisabled = false
BaseDefenseTriggered = false
AttackScheduleStarted = false
-- Set for good once the GDI Construction Yard is destroyed or captured: no more waves.
WavesStopped = false
Atk1Sent = false
Grd1Sent = false
Atk1Pending = false
Grd1Pending = false
ProducedGDIUnits = {}
ProductionBusy = {}
OrcaBusy = nil
NextGroupTime = 0
DefenseLockUntil = 0

local function Cells(coords)
	local cells = {}
	for _, c in ipairs(coords) do
		table.insert(cells, CPos.New(c[1], c[2]))
	end
	return cells
end

-- Original [CellTriggers] for atk1 (west river crossing), prod (central
-- approach), delx (strike team rendezvous) and grd1 (east flank).
Atk1Cells = Cells({
	{19,42}, {20,42}, {21,42}, {19,43}, {20,43}, {21,43},
	{18,44}, {19,44}, {20,44}, {21,44},
	{19,45}, {20,45}, {21,45}, {19,46}, {20,46}, {21,46},
})

ProdCells = Cells({
	{23,31}, {24,31}, {25,31}, {26,31}, {27,31}, {28,31}, {29,31}, {30,31}, {31,31}, {32,31},
	{23,32}, {24,32}, {25,32}, {26,32}, {27,32}, {28,32}, {29,32}, {30,32}, {31,32}, {32,32},
	{23,33}, {24,33}, {25,33}, {26,33}, {27,33}, {28,33}, {29,33}, {30,33}, {32,33},
	{33,40}, {34,40}, {35,40}, {33,41}, {34,41}, {35,41}, {33,42}, {34,42}, {35,42},
	{33,43}, {34,43}, {35,43}, {33,44}, {34,44}, {35,44}, {33,45}, {34,45}, {35,45},
})

DelxCells = Cells({
	{10,42}, {11,42}, {12,42}, {13,42}, {14,42}, {15,42}, {16,42}, {17,42}, {18,42},
	{10,43}, {11,43}, {12,43}, {13,43}, {14,43}, {15,43}, {16,43}, {17,43}, {18,43},
	{10,44}, {11,44}, {10,45}, {11,45},
})

Grd1Cells = Cells({
	{59,20}, {60,20}, {61,20}, {59,21}, {60,21}, {61,21}, {59,22}, {60,22}, {61,22},
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
	SendTeam(units, WestSpawn, WestRoute)

	-- On Hard the final wave keeps coming.
	if Difficulty == "hard" then
		Trigger.AfterDelay(DateTime.Seconds(FinalWaveDelay), FinalWave)
	end
end

-- gdi5 is a guard team: it walks to the given cells and stays there.
SpawnGuardTeam = function(def)
	if GDI.HasNoRequiredUnits() then
		return
	end

	Reinforcements.Reinforce(GDI, def.units, { def.start }, DateTime.Seconds(0.25), function(unit)
		unit.Stance = "Defend"
		Utils.Do(def.moves, function(cell)
			unit.Move(cell)
		end)
	end)
end

-- The original apc1 team (2x E2, 3x E6, APC) guards the GDI base. It is
-- triggered by the two early Time triggers and by apc3 (all four base
-- defenses destroyed).
SpawnGuardAPC = function()
	if GDI.HasNoRequiredUnits() then
		return
	end

	local apc = Actor.Create("apc", true, { Owner = GDI, Location = APCStart, Facing = Angle.South })
	Utils.Do({ "e2", "e2", "e6", "e6", "e6" }, function(unitType)
		local passenger = Actor.Create(unitType, false, { Owner = GDI, Location = APCStart })
		apc.LoadPassenger(passenger)
	end)
	apc.Move(waypoint7.Location)
	apc.Move(waypoint18.Location)
	apc.Stance = "Defend"
end

-- The Nod force in the south-east (3 ARTY, 6 E1, 6 E3, found by position at
-- start) is protected from the A10s until minute 20 (Hard: 15), as in 11a,
-- as if it had not been found yet. Everything within SouthSafeRadius of a
-- living member is skipped, so napalm cannot splash onto them, and the
-- protection follows the units wherever the player moves them.
SouthSafeUntil = { easy = DateTime.Minutes(20), normal = DateTime.Minutes(20), hard = DateTime.Minutes(15) }
SouthSafeRadius = 10 * 1024
SouthGroup = {}

CheckForSams = function()
	return #Nod.GetActorsByType("sam") >= 3 and Nod.PowerState == "Normal"
end

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
GroupRoutes = { "gdi2", "gdi4" }

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
GDIRefineryPosition = CPos.New(43, 4)
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

	-- If the player crossed atk1/grd1 before finding the base, the cell
	-- triggers could not fire. Send those teams now.
	if Atk1Pending and not Atk1Sent then
		Atk1Sent = true
		AttackWave(TeamDefs.gdi1)
	end
	if Grd1Pending and not Grd1Sent then
		Grd1Sent = true
		SpawnGuardTeam(TeamDefs.gdi5)
	end

	-- apc1 and apc2: the two early guard APCs from the original Time triggers.
	Trigger.AfterDelay(DateTime.Seconds(12), SpawnGuardAPC)
	Trigger.AfterDelay(DateTime.Seconds(60), SpawnGuardAPC)

	Trigger.AfterDelay(WaveDelay(times.atk2), function() AttackWave(TeamDefs.gdi2) end)
	Trigger.AfterDelay(WaveDelay(times.atk3), function() AttackWave(TeamDefs.gdi3) end)
	Trigger.AfterDelay(WaveDelay(times.atk4), function() AttackWave(TeamDefs.gdi4) end)
	Trigger.AfterDelay(WaveDelay(times.atk5), function() AttackWave(TeamDefs.gdi4) end)
	Trigger.AfterDelay(WaveDelay(times.atk7), function() AttackWave(TeamDefs.heavy2) end)
	Trigger.AfterDelay(WaveDelay(times.atk6), function() AttackWave(TeamDefs.heavy1) end)
	-- Final wave: Easy never, Normal once, Hard repeatedly (see FinalWave).
	if Difficulty ~= "easy" then
		Trigger.AfterDelay(WaveDelay(times.atk6) + DateTime.Seconds(FinalWaveDelay), FinalWave)
	end
	Trigger.AfterDelay(WaveDelay(times.auto), AutoWave)
	Trigger.AfterDelay(WaveDelay(AirstrikeFirstDelay[Difficulty or "normal"]), Airstrike)

	-- Fallback if the player never enters the original `prod` cell trigger.
	Trigger.AfterDelay(DateTime.Seconds(ProductionStartDelay[Difficulty or "normal"]), function()
		ProdTriggered = true
		TryStartGDIProduction()
	end)
end

SetupAI = function()
	InitSouthGroup()

	Trigger.OnEnteredFootprint(Atk1Cells, function(actor, id)
		if actor.Owner == Nod and not Atk1Sent then
			if BaseTakenOver then
				-- Several units can enter in the same tick; send the team only once.
				Atk1Sent = true
				Trigger.RemoveFootprintTrigger(id)
				AttackWave(TeamDefs.gdi1)
			else
				-- Remember it; StartAttackSchedule fires the wave once the base
				-- has been taken over.
				Atk1Pending = true
			end
		end
	end)

	Trigger.OnEnteredFootprint(Grd1Cells, function(actor, id)
		if actor.Owner == Nod and not Grd1Sent then
			if BaseTakenOver then
				Grd1Sent = true
				Trigger.RemoveFootprintTrigger(id)
				SpawnGuardTeam(TeamDefs.gdi5)
			else
				Grd1Pending = true
			end
		end
	end)

	Trigger.OnEnteredFootprint(ProdCells, function(actor, id)
		if actor.Owner == Nod and not ProdTriggered then
			ProdTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			TryStartGDIProduction()
		end
	end)

	Trigger.OnEnteredFootprint(DelxCells, function(actor, id)
		if actor.Owner == Nod and actor.HasProperty("Move") and not StrikeTeamSafe then
			StrikeTeamSafe = true
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	-- Fallback for the rendezvous: reaching the flare itself is enough, even
	-- if the unit stops two cells short of the original delx cells.
	Trigger.OnEnteredProximityTrigger(DefaultFlareLocation.CenterPosition, WDist.FromCells(4), function(actor, id)
		if actor.Owner == Nod and actor.HasProperty("Move") and not StrikeTeamSafe then
			StrikeTeamSafe = true
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnAllRemovedFromWorld(StrikeTeam, function()
		if not StrikeTeamSafe then
			Nod.MarkFailedObjective(EliminateGDI)
		end
	end)

	-- apc3: all four GDI base defenses destroyed.
	Trigger.OnAllKilled({ GDIATWR1, GDIATWR2, GDIGTWR1, GDIGTWR2 }, SpawnGuardAPC)

	Trigger.OnAllKilled(HuntBuildings, function()
		Utils.Do(GDI.GetGroundAttackers(), IdleHunt)
	end)

	-- React to attacks on the main GDI base (x >= 36, y <= 15): all GDI ground
	-- units start hunting and the base begins rebuilding/producing.
	Utils.Do(Map.ActorsInWorld, function(actor)
		if actor.Owner == GDI and actor.HasProperty("StartBuildingRepairs")
			and actor.Location.X >= 36 and actor.Location.Y <= 15 then
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
