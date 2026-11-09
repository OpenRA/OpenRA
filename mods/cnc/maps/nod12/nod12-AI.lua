--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI production, scripted attacks, base rebuilding, A10 strikes and the
-- Ion Cannon for Nod mission 12.

-- TD house triggers tick every sixth of a minute, so the [Triggers] Time
-- values are tenths of a minute: Data * 6 seconds. Keeping the original
-- schedule is important here because Nod starts with only an MCV and a few
-- vehicles; compressing it would attack the player before they can build.
-- After playtests the scripted and automatic waves were moved up: the
-- original plan was still far too passive (nothing happened for a quarter of
-- an hour).
WaveTimes = {
	easy = { grd1 = 90, grd2 = 6, apc2 = 12, apc3 = 30, apc1 = 3300, atk1 = 450, atk2 = 600, atk3 = 660, atk4 = 900, atk5 = 960, auto = 780 },
	normal = { grd1 = 90, grd2 = 6, apc2 = 12, apc3 = 30, apc1 = 3300, atk1 = 300, atk2 = 420, atk3 = 480, atk4 = 660, atk5 = 720, auto = 540 },
	hard = { grd1 = 90, grd2 = 6, apc2 = 12, apc3 = 30, apc1 = 3300, atk1 = 240, atk2 = 330, atk3 = 390, atk4 = 540, atk5 = 600, auto = 420 },
}

AutoWaveDelays = {
	easy = { min = 210, max = 285 },
	normal = { min = 150, max = 210 },
	hard = { min = 120, max = 165 },
}

-- Original `xxxx` is 300 tenths of a minute (30 min) and persistent, i.e.
-- one strike every 30 minutes. Too rare for the port: first strike after
-- 10/8/7 minutes, then every 10/8/7 minutes. The GDI HQ's `delx` trigger
-- stops the strikes when the Communications Center is destroyed.
AirstrikeFirstDelay = { easy = 600, normal = 480, hard = 420 }
AirstrikeInterval = { easy = 600, normal = 480, hard = 420 }

-- The Ion Cannon is handed over in the original when Nod enters one of the
-- ion1 areas or destroys the Mammoth Tank carrying the firing codes. The
-- charge interval is shortened in rules.yaml (3000 ticks), so the first
-- scripted strike follows shortly after the warning.
IonCannonFirstDelay = { easy = 180, normal = 120, hard = 90 }
IonCannonInterval = { easy = 360, normal = 300, hard = 240 }

-- Fallback if the player never crosses the original `prod` cell trigger.
-- Earlier than the original schedule: GDI needs an army before the player
-- reaches its base, otherwise the mission feels empty.
ProductionStartDelay = { easy = 480, normal = 360, hard = 300 }
StartingCash = { easy = 6000, normal = 8000, hard = 10000 }

AttackGroupSize = { easy = 4, normal = 5, hard = 6 }
DefenderReserve = { easy = 2, normal = 3, hard = 4 }
MaxProducedUnits = { easy = 6, normal = 8, hard = 10 }
AttackGroupCooldown = { easy = 150, normal = 110, hard = 80 }
BaseDefenseWarningSeconds = 45
MainBaseMinX = 40
MainBaseMaxY = 15

-- Ground units placed on the map (everything that is not a building). Only
-- these get a Trigger.OnDamaged; runtime units are handled by their stance.
MapGroundUnits = {
	GDIBaseMSAM1, GDIBaseMSAM2, GDIBaseMSAM3, GDIBaseMSAM4,
	GDIJeep1, GDIJeep2,
	GDIMammoth1, GDIMammoth2, GDIMammoth3, GDIMammoth4, GDIMammoth5,
	BridgeMammoth1, BridgeMammoth2,
	IonCannonMammoth,
}

-- Teams enter at the map border instead of appearing inside the GDI base
-- (the original spawned them at a waypoint in the middle of the map, which
-- looked like units popping up). EdgeSpawn picks the closest border cell to
-- the first waypoint of the route that a ground vehicle can actually use.
-- APC reinforcements arrive at the east edge.
APCStart = CPos.New(61, 32)

EdgeTerrain = { Clear = true, Road = true, Rough = true, Beach = true, Tiberium = true, BlueTiberium = true }

EdgeSpawn = function(target)
	local function usable(cell)
		if not EdgeTerrain[Map.TerrainType(cell)] then
			return false
		end

		-- Walls and buildings on the border cell would trap the units.
		local blocked = Map.ActorsInCircle(Map.CenterOfCell(cell), WDist.FromCells(1), function(actor)
			return actor.Type == "brik" or actor.HasProperty("StartBuildingRepairs")
		end)

		return #blocked == 0
	end

	local cell = Map.ClosestMatchingEdgeCell(target, usable)
	if cell == nil then
		return target
	end

	-- Driving in from the border also passes through waypoint 4 (the base
	-- gate) on the original route, so units still walk the intended path.
	return cell
end

-- Row of the GDI south gate (waypoint 4): airstrike targets must be south
-- of it, so the bridge tanks keep guarding the crossing.
LayoutSouthGateY = 13

-- Routes keep the waypoint actors; `CellsOf` resolves their cell when the
-- order is actually given. The original routes ended in the empty west, so
-- the teams never reached the player's base (bottom right). They now finish
-- at waypoint 26, the cell of the original mission camera and the Nod base.
Route16 = { waypoint4, waypoint5, waypoint10, waypoint14, waypoint15, waypoint16, DefaultCameraPosition }
Route17 = { waypoint4, waypoint5, waypoint10, waypoint14, waypoint15, waypoint17, DefaultCameraPosition }
NorthRoute16 = { waypoint12, waypoint13, waypoint7, waypoint14, waypoint15, waypoint16, DefaultCameraPosition }
NorthRoute17 = { waypoint12, waypoint13, waypoint7, waypoint14, waypoint15, waypoint17, DefaultCameraPosition }
JeepPatrol = { waypoint4, waypoint5, waypoint10, waypoint7, waypoint8, waypoint7, waypoint10, waypoint9 }
RocketPatrol = { waypoint19, waypoint20 }
APCPatrol = { waypoint9, waypoint6, waypoint5, waypoint4, waypoint18 }

TeamDefs = {
	gdi2 = { units = { "e1", "e1", "e1", "e2", "e2" }, moves = Route16 },
	gdi3 = { units = { "e2", "e2", "e2", "e3", "e3" }, moves = Route17 },
	gdi5 = { units = { "e3", "e3", "jeep" }, moves = Route16 },
	gdi6 = { units = { "mtnk" }, moves = Route17 },
	auto1 = { units = { "e1", "e1", "e1", "e2", "e2" }, moves = Route16 },
	auto2 = { units = { "e2", "e2", "e2", "e3", "e3" }, moves = Route17 },
	auto3 = { units = { "e1", "e1", "e2", "e2", "e3", "e3" }, moves = NorthRoute16 },
	auto4 = { units = { "e3", "e3", "jeep" }, moves = NorthRoute17 },
	auto5 = { units = { "e1", "e1", "e1", "mtnk" }, moves = Route16 },
	auto6 = { units = { "e3", "e3", "mtnk" }, moves = NorthRoute17 },
	auto7 = { units = { "htnk" }, moves = NorthRoute16 },
	auto8 = { units = { "htnk" }, moves = Route17 },
	auto9 = { units = { "jeep", "jeep" }, moves = Route17 },
}

AutoTeamNames = { "auto1", "auto2", "auto3", "auto4", "auto5", "auto6", "auto7", "auto8", "auto9" }
GroupRoutes = AutoTeamNames

-- Original [Base] list. These buildings are rebuilt when destroyed.
GDIBaseBuildings = {
	{ type = "fact", pos = CPos.New(44, 3) },
	{ type = "proc", pos = CPos.New(46, 5) },
	{ type = "weap", pos = CPos.New(55, 6) },
	{ type = "pyle", pos = CPos.New(51, 6) },
	{ type = "nuk2", pos = CPos.New(55, 3) },
	{ type = "nuk2", pos = CPos.New(57, 3) },
	{ type = "nuk2", pos = CPos.New(58, 6) },
	-- Silos are not rebuilt: the money is better spent on units and the
	-- harvester while the base is under attack.
	{ type = "atwr", pos = CPos.New(45, 12) },
	{ type = "atwr", pos = CPos.New(53, 12) },
	{ type = "gtwr", pos = CPos.New(47, 14) },
	{ type = "gtwr", pos = CPos.New(51, 14) },
}

GDIProductionTypes = {
	pyle = { "e1", "e2", "e3" },
	weap = { "jeep", "mtnk", "htnk", "msam", "apc" },
}

-- Skip rotations whose prerequisites are currently missing. Build() reports
-- true even when the queue rejects the order, so ordering blind would block
-- the production slot until the watchdog fires.
GDIUnitPrerequisites = {
	e1 = { "barracks" },
	e2 = { "anyhq" },
	e3 = { "barracks" },
	jeep = { "weap" },
	mtnk = { "anyhq" },
	htnk = { "eye" },
	msam = { "anyhq" },
	apc = { "pyle" },
	orca = { "hpad", "anyhq" },
}

-- Original `grd1` cell trigger south of the river.
Grd1Cells = {
	CPos.New(35, 54), CPos.New(36, 54), CPos.New(37, 54),
	CPos.New(24, 54), CPos.New(25, 54), CPos.New(26, 54),
	CPos.New(35, 53), CPos.New(36, 53), CPos.New(37, 53),
	CPos.New(23, 53), CPos.New(24, 53), CPos.New(25, 53),
	CPos.New(35, 52), CPos.New(36, 52), CPos.New(37, 52),
	CPos.New(22, 52), CPos.New(23, 52), CPos.New(24, 52),
	CPos.New(29, 51), CPos.New(30, 51), CPos.New(31, 51), CPos.New(32, 51),
	CPos.New(33, 51), CPos.New(34, 51), CPos.New(35, 51), CPos.New(36, 51), CPos.New(37, 51),
	CPos.New(29, 50), CPos.New(30, 50), CPos.New(31, 50), CPos.New(32, 50),
	CPos.New(33, 50), CPos.New(34, 50), CPos.New(35, 50), CPos.New(36, 50), CPos.New(37, 50),
	CPos.New(29, 49), CPos.New(30, 49), CPos.New(31, 49), CPos.New(32, 49),
	CPos.New(33, 49), CPos.New(34, 49), CPos.New(35, 49), CPos.New(36, 49), CPos.New(37, 49),
}

-- Original `prod` cell trigger at the river crossing.
ProdCells = {
	CPos.New(24, 54), CPos.New(25, 54), CPos.New(26, 54),
	CPos.New(23, 53), CPos.New(24, 53), CPos.New(25, 53),
	CPos.New(22, 52), CPos.New(23, 52), CPos.New(24, 52),
}

-- Buildings carrying the original `hunt` trigger: WEAP, FACT, two NUK2,
-- PYLE and PROC.
HuntBuildings = { GDICYard, GDIProc, GDIWeaponsFactory, GDIBarracks, GDINuke2, GDINuke3 }

GDIProductionStarted = false
ProdTriggered = false
AutoAttackStarted = false
BaseDefenseTriggered = false
AllToHuntTriggered = false
BridgePost = {}
BridgeChasing = { false, false }
BridgeChaseSeconds = 60
RocketPatrolStarted = false
IonCannonOnline = false
GDIHarvestingStarted = false
NextGroupTime = 0
ProducedGDIUnits = {}
ProductionBusy = {}
HarvesterBuilding = false
HarvesterStamp = nil
EconomyWatchdogRunning = false
NoHarvesterSince = nil
LastEconomySale = -100000
AllOutAttackStarted = false
BaseUnderAttackUntil = 0
WavesStopped = false
AirstrikeStopped = false
ActivePatrols = {}
MCVBuildStamp = nil

-- Walk an actor list by its sequence length. `Utils.Do` works as well, but
-- this helper is used where a null entry has to be skipped explicitly.
ForEach = function(list, fn)
	for i = 1, #list do
		fn(list[i])
	end
end

-- True only for a live actor reference. Actors that do not exist (map
-- entries that were removed) and actors created at runtime (production,
-- reinforcements) both arrive as "null actor" objects: comparing them
-- against nil or calling .IsDead on them throws.
IsLive = function(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

-- GDI starts harvesting once Nod has deployed a Construction Yard.
NodHasConstructionYard = function()
	local found = false
	Utils.Do(Nod.GetActorsByType("fact"), function(actor)
		if not actor.IsDead and actor.Owner == Nod then
			found = true
		end
	end)
	return found
end

StartGDIHarvesting = function()
	if GDIHarvestingStarted then
		return
	end

	GDIHarvestingStarted = true
	ForEach(GDI.GetActorsByType("harv"), function(harvester)
		if IsLive(harvester) then
			harvester.FindResources()
		end
	end)

	StartEconomyWatchdog()
end

-- Turn a list of waypoint actors into the cell list that AttackMove and
-- Patrol expect. The cell is read when the order is given, not at load time.
CellsOf = function(waypoints)
	local cells = {}
	for i = 1, #waypoints do
		cells[i] = waypoints[i].Location
	end
	return cells
end

SendTeam = function(units, moves)
	if GDI.HasNoRequiredUnits() then
		return
	end

	local spawn = EdgeSpawn(moves[1].Location)
	Reinforcements.Reinforce(GDI, units, { spawn }, math.floor(DateTime.Seconds(1) / 4), function(unit)
		unit.Stance = "AttackAnything"
		Utils.Do(CellsOf(moves), function(cell)
			unit.AttackMove(cell)
		end)
		IdleHunt(unit)
	end)
end

SpawnPatrol = function(units, path, wait)
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	local spawn = EdgeSpawn(path[1].Location)
	Reinforcements.Reinforce(GDI, units, { spawn }, math.floor(DateTime.Seconds(1) / 4), function(unit)
		unit.Stance = "AttackAnything"
		unit.Patrol(CellsOf(path), true, DateTime.Seconds(wait or 20))
		table.insert(ActivePatrols, unit)
	end)
end

SpawnGuardAPC = function(passengers)
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	local apc = Actor.Create("apc", true, { Owner = GDI, Location = APCStart, Facing = Angle.South })
	Utils.Do(passengers, function(unitType)
		local passenger = Actor.Create(unitType, false, { Owner = GDI, Location = APCStart })
		apc.LoadPassenger(passenger)
	end)
	apc.Stance = "AttackAnything"
	apc.Patrol(CellsOf(APCPatrol), true, DateTime.Seconds(20))
	table.insert(ActivePatrols, apc)
end

AttackWave = function(def)
	if WavesStopped then
		return
	end

	SendTeam(def.units, def.moves)
end

-- The two Mammoths guard the bridge at the start of the mission. When they
-- are shot at they chase the attacker; once that unit is gone (or the chase
-- took too long) they drive back to their post. A full base alarm overrides
-- this and sends them with the rest of the army.
SendBridgeResponse = function(attacker)
	if BaseDefenseTriggered or GDI.HasNoRequiredUnits() then
		return
	end

	local target = attacker
	if target ~= nil and (target.IsDead or not target.IsInWorld) then
		target = nil
	end

	Media.PlaySpeechNotification(Nod, "EnemyUnitsApproaching")

	for i = 1, 2 do
		local unit = (i == 1) and BridgeMammoth1 or BridgeMammoth2
		local post = BridgePost[i]
		if IsLive(unit) and unit.Owner == GDI and post ~= nil and not BridgeChasing[i] then
			BridgeChasing[i] = true
			unit.Stance = "AttackAnything"
			if target ~= nil and unit.CanTarget(target) then
				unit.Attack(target)
			end
			WatchBridgeChase(i, unit, target, post.x, post.y, 0)
		end
	end
end

WatchBridgeChase = function(index, unit, target, postX, postY, elapsed)
	if not IsLive(unit) then
		BridgeChasing[index] = false
		return
	end

	-- The base alarm takes over; the tanks stay on the offensive.
	if BaseDefenseTriggered then
		BridgeChasing[index] = false
		return
	end

	local attackerGone = target == nil or target.IsDead or not target.IsInWorld
	if attackerGone or elapsed >= BridgeChaseSeconds then
		BridgeChasing[index] = false
		unit.Stance = "Defend"
		unit.Stop()
		unit.AttackMove(CPos.New(postX, postY))
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		WatchBridgeChase(index, unit, target, postX, postY, elapsed + 2)
	end)
end

StartJeepPatrol = function()
	SpawnPatrol({ "jeep", "jeep" }, JeepPatrol)
end

StartRocketPatrol = function()
	if RocketPatrolStarted then
		return
	end

	RocketPatrolStarted = true
	SpawnPatrol({ "e2", "e2", "e2", "e2" }, RocketPatrol)
end

AutoWave = function()
	if WavesStopped or GDI.HasNoRequiredUnits() then
		return
	end

	AttackWave(TeamDefs[Utils.Random(AutoTeamNames)])

	local delays = AutoWaveDelays[Difficulty or "normal"]
	Trigger.AfterDelay(Utils.RandomInteger(DateTime.Seconds(delays.min), DateTime.Seconds(delays.max)), AutoWave)
end

CheckForHunt = function()
	if AllToHuntTriggered then
		return
	end

	for _, building in ipairs(HuntBuildings) do
		if building and not building.IsDead then
			return
		end
	end

	AllToHuntTriggered = true
	BaseDefenseTriggered = true
	StopPatrols()
	ForEach(GDI.GetGroundAttackers(), function(actor)
		if IsLive(actor) then
			actor.Stance = "AttackAnything"
			IdleHunt(actor)
		end
	end)
end

-- Rebuild support. One building at a time, paid up front, placed after its
-- normal build time. GDI keeps the main base alive so the mission cannot be
-- won by simply flattening the Production Yard.
RebuildQueue = {}
RebuildWorkerRunning = false
RebuildBusy = false
RebuildPending = {}
RebuildPollSeconds = 5
RebuildSiteTimeoutAttempts = 20
RebuildSoldDelaySeconds = 15

-- `Actor.BuildTime` needs an explicit queue because `fact` is not listed in
-- any public queue; the actors themselves are created directly, like 11a/11b.
GDIRebuildQueueType = "Building.GDI"
GDIBuildingPriority = { fact = 1, nuk2 = 2, proc = 3, pyle = 4, weap = 5, atwr = 6, gtwr = 7, hpad = 8 }
GDIBuildingFootprint = {
	fact = { 3, 3 }, proc = { 3, 4 }, weap = { 3, 3 }, pyle = { 2, 3 },
	nuk2 = { 2, 3 }, atwr = { 1, 1 }, gtwr = { 1, 1 }, hpad = { 2, 2 },
}

-- Direct Actor.Create rebuilds must not bypass the tech gates that normal GDI
-- queues enforce.
GDIBuildingPrerequisites = {
	proc = { "anypower" },
	weap = { "proc" },
	pyle = { "anypower" },
	nuk2 = { "anyhq" },
	atwr = { "anyhq" },
	gtwr = { "barracks" },
	hpad = { "proc" },
}

CanBuildGDIBuilding = function(buildingType)
	local prerequisites = GDIBuildingPrerequisites[buildingType]
	return prerequisites == nil or GDI.HasPrerequisites(prerequisites)
end

-- Script rebuilds need a duration without a real production queue. Low power
-- applies the normal 50 % penalty at the start of the build.
ScriptBuildTime = function(buildingType)
	local time = Actor.BuildTime(buildingType, GDIRebuildQueueType)
	local power = GDI.PowerState
	if power == "Low" or power == "Critical" then
		time = math.floor(time * 1.5)
	end
	return time
end

-- Hard, while GDI has no harvester: only these are rebuilt (money is needed
-- for the harvester). On Easy/Normal sold buildings may come back after the
-- usual delay.
EconomyRebuildTypes = { fact = true, proc = true, weap = true }

BuildingKey = function(building)
	return building.type .. ":" .. building.pos.X .. "," .. building.pos.Y
end

-- Returns the GDI building of that type standing on the requested spot.
FindGDIBuildingAt = function(building)
	local found = nil
	Utils.Do(GDI.GetActorsByType(building.type), function(actor)
		if IsLive(actor) and actor.Location.X == building.pos.X and actor.Location.Y == building.pos.Y then
			found = actor
		end
	end)
	return found
end

GDIBuildingExists = function(building)
	local exists = false
	Utils.Do(GDI.GetActorsByType(building.type), function(actor)
		if actor ~= nil and not actor.IsDead and actor.Location.X == building.pos.X and actor.Location.Y == building.pos.Y then
			exists = true
		end
	end)
	return exists
end

BlockingUnits = function(building)
	local size = GDIBuildingFootprint[building.type] or { 2, 2 }
	local topLeft = WPos.New(building.pos.X * 1024, building.pos.Y * 1024, 0)
	local bottomRight = WPos.New((building.pos.X + size[1]) * 1024, (building.pos.Y + size[2]) * 1024, 0)
	return Map.ActorsInBox(topLeft, bottomRight, function(actor)
		return actor ~= nil and not actor.IsDead and actor.HasProperty("Move")
			and not actor.HasProperty("Land")
	end)
end

-- Immobile actors (buildings, walls, wrecks) permanently occupy a plot.
-- Movable actors, including aircraft, are handled by BlockingUnits.
FixedSiteBlocker = function(building)
	local size = GDIBuildingFootprint[building.type] or { 2, 2 }
	local topLeft = WPos.New(building.pos.X * 1024, building.pos.Y * 1024, 0)
	local bottomRight = WPos.New((building.pos.X + size[1]) * 1024, (building.pos.Y + size[2]) * 1024, 0)
	local blocker = nil
	Utils.Do(Map.ActorsInBox(topLeft, bottomRight), function(actor)
		if actor == nil or actor.IsDead or not actor.IsInWorld then
			return
		end
		if actor.HasProperty("Move") then
			return
		end
		blocker = actor
	end)
	return blocker
end

PermanentSiteBlocker = function(building)
	return FixedSiteBlocker(building) ~= nil
end

ClearBuildingSite = function(building, ignoreCell)
	if PermanentSiteBlocker(building) then
		return false
	end

	local size = GDIBuildingFootprint[building.type] or { 2, 2 }
	local free = true
	Utils.Do(BlockingUnits(building), function(unit)
		-- The MCV that is about to deploy stands on the plot itself.
		if ignoreCell ~= nil and unit.Location.X == ignoreCell.X and unit.Location.Y == ignoreCell.Y then
			return
		end

		-- Harvesters and APCs are sent away but do not postpone the rebuild:
		-- they cannot leave the footprint fast enough and would block the
		-- site forever.
		local ignorable = unit.Type == "harv" or unit.Type == "apc"
		if unit.Owner ~= GDI then
			free = false
		else
			unit.Move(CPos.New(building.pos.X, building.pos.Y + size[2] + 2))
			if not ignorable then
				free = false
			end
		end
	end)
	return free
end

-- A captured (or otherwise permanently blocked) plot must not keep GDI from
-- restoring a building. Search the nearest free footprint instead of
-- overbuilding the blocker and keep the build plan in sync.
AlternativeSiteSearchRadius = 10

local function FootprintFor(buildingType)
	return GDIBuildingFootprint[buildingType] or { 2, 2 }
end

local function FootprintsOverlap(aPos, aSize, bPos, bSize)
	return aPos.X < bPos.X + bSize[1] and bPos.X < aPos.X + aSize[1]
		and aPos.Y < bPos.Y + bSize[2] and bPos.Y < aPos.Y + aSize[2]
end

local function IsTerrainBuildable(cell)
	local ok, terrain = pcall(Map.TerrainType, cell)
	return ok and (terrain == "Clear" or terrain == "Road")
end

local function IsPlannedFootprintFree(site, ignoreType, ignorePos)
	local size = FootprintFor(site.type)
	for _, planned in ipairs(GDIBaseBuildings) do
		local isIgnored = planned.type == ignoreType
			and planned.pos.X == ignorePos.X and planned.pos.Y == ignorePos.Y
		if not isIgnored and FootprintsOverlap(site.pos, size, planned.pos, FootprintFor(planned.type)) then
			return false
		end
	end
	return true
end

local function IsVacantSite(site, ignoreType, ignorePos)
	if not IsPlannedFootprintFree(site, ignoreType, ignorePos) then
		return false
	end

	local size = FootprintFor(site.type)
	for x = site.pos.X, site.pos.X + size[1] - 1 do
		for y = site.pos.Y, site.pos.Y + size[2] - 1 do
			if not IsTerrainBuildable(CPos.New(x, y)) then
				return false
			end
		end
	end

	return FixedSiteBlocker(site) == nil and #BlockingUnits(site) == 0
end

FindAlternativeSite = function(building)
	local origin = building.pos
	local candidates = {}
	for dx = -AlternativeSiteSearchRadius, AlternativeSiteSearchRadius do
		for dy = -AlternativeSiteSearchRadius, AlternativeSiteSearchRadius do
			if dx ~= 0 or dy ~= 0 then
				table.insert(candidates, {
					dx = dx,
					dy = dy,
					ring = math.max(math.abs(dx), math.abs(dy)),
					order = math.abs(dx) + math.abs(dy),
				})
			end
		end
	end

	table.sort(candidates, function(a, b)
		if a.ring ~= b.ring then
			return a.ring < b.ring
		end
		if a.order ~= b.order then
			return a.order < b.order
		end
		if a.dy ~= b.dy then
			return a.dy < b.dy
		end
		return a.dx < b.dx
	end)

	for _, candidate in ipairs(candidates) do
		local site = { type = building.type, pos = CPos.New(origin.X + candidate.dx, origin.Y + candidate.dy) }
		if IsVacantSite(site, building.type, origin) then
			return site.pos
		end
	end

	return nil
end

RelocateRebuildSite = function(building, newPos)
	local oldPos = building.pos
	local exactMatch = nil
	local planEntries = 0
	for _, planned in ipairs(GDIBaseBuildings) do
		if planned.type == building.type then
			planEntries = planEntries + 1
			if planned.pos.X == oldPos.X and planned.pos.Y == oldPos.Y then
				exactMatch = planned
			end
		end
	end

	if exactMatch ~= nil then
		exactMatch.pos = newPos
	elseif planEntries == 1 then
		-- Do not move a plan whose current site still has a live building.
		for _, planned in ipairs(GDIBaseBuildings) do
			if planned.type == building.type then
				local occupied = false
				for _, actor in ipairs(GDI.GetActorsByType(planned.type)) do
					if not actor.IsDead and actor.Location.X == planned.pos.X
						and actor.Location.Y == planned.pos.Y then
						occupied = true
						break
					end
				end
				if not occupied then
					planned.pos = newPos
				end
				break
			end
		end
	end

	building.pos = newPos
end

RegisterGDIBuilding = function(actor, rebuild)
	if actor.Owner == nil or actor.IsDead then
		return
	end

	Trigger.OnDamaged(actor, BaseBuildingDamaged)

	if rebuild then
		local info = { type = actor.Type, pos = CPos.New(actor.Location.X, actor.Location.Y) }
		Trigger.OnKilled(actor, function()
			QueueGDIRebuild(info)
		end)
		Trigger.OnCapture(actor, function(_, _, oldOwner, newOwner)
			if oldOwner == GDI and newOwner ~= GDI then
				QueueGDIRebuild(info)
			end
		end)
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
	local entry = { type = building.type, pos = building.pos, sold = sold }
	if sold then
		entry.due = DateTime.GameTime + DateTime.Seconds(RebuildSoldDelaySeconds)
	end

	local key = BuildingKey(entry)
	for _, queued in ipairs(RebuildQueue) do
		if BuildingKey(queued) == key then
			return
		end
	end

	table.insert(RebuildQueue, entry)
	StartRebuildWorker()
end

StartGDIRebuild = function()
	Utils.Do(GDIBaseBuildings, function(building)
		if not GDIBuildingExists(building) then
			QueueGDIRebuild(building)
		end
	end)
	StartRebuildWorker()
end

GDIFunds = function()
	return GDI.Cash + GDI.Resources
end

-- Spend stored resources first, then cash, matching PlayerResources.TakeCash.
SpendGDIFunds = function(amount)
	local fromResources = math.min(GDI.Resources, amount)
	GDI.Resources = GDI.Resources - fromResources
	GDI.Cash = GDI.Cash - (amount - fromResources)
end

PlaceGDIBuilding = function(building)
	if building.type == "fact" then
		StartMCVRebuild(building)
		return
	end

	RebuildPending[BuildingKey(building)] = true
	RebuildBusy = true
	SpendGDIFunds(Actor.Cost(building.type))

	Trigger.AfterDelay(ScriptBuildTime(building.type), function()
		FinishGDIBuilding(building, 0)
	end)
end

-- A Construction Yard is not created out of thin air: GDI buys an MCV in the
-- weapons factory, drives it to the (possibly relocated) plot and deploys it
-- there. Without a weapons factory the order stays queued until one exists.
MCVWatchdogChecks = 4

WatchMCVRebuild = function(building, stamp, factory, checks)
	checks = checks or 0
	if MCVBuildStamp ~= stamp then
		return
	end

	local factoryAlive = factory ~= nil and not factory.IsDead and factory.Owner == GDI and factory.IsInWorld
	local stillProducing = factoryAlive and factory.HasProperty("IsProducing") and factory.IsProducing("mcv")
	if stillProducing and checks < MCVWatchdogChecks then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			WatchMCVRebuild(building, stamp, factory, checks + 1)
		end)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if MCVBuildStamp ~= stamp then
			return
		end

		MCVBuildStamp = nil
		local key = BuildingKey(building)
		if RebuildPending[key] and not GDIBuildingExists(building) then
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
		end
	end)
end

StartMCVRebuild = function(building)
	local key = BuildingKey(building)

	if GDI.HasNoRequiredUnits() or #GDI.GetActorsByType("fact") > 0 then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	-- A captured yard can occupy the old plot. Move the plan before ordering.
	if PermanentSiteBlocker(building) then
		local newPos = FindAlternativeSite(building)
		if newPos == nil then
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
			return
		end

		local oldKey = key
		RelocateRebuildSite(building, newPos)
		key = BuildingKey(building)
		if RebuildPending[oldKey] then
			RebuildPending[oldKey] = nil
			RebuildPending[key] = true
		end
	end

	local factory = GDI.GetActorsByType("weap")[1]
	local existingMcv = GDI.GetActorsByType("mcv")[1]
	if existingMcv ~= nil then
		-- Reuse a spare MCV instead of paying for another one.
		RebuildPending[key] = true
		RebuildBusy = true
		DeployRebuiltMCV(existingMcv, building, 0)
		return
	end

	if factory == nil then
		-- No yard can be produced without a weapons factory. Keep the entry
		-- in the queue; a later factory will pick it up.
		RebuildPending[key] = nil
		RebuildBusy = false
		QueueGDIRebuild(building)
		return
	end

	RebuildPending[key] = true
	RebuildBusy = true

	local stamp = DateTime.GameTime
	MCVBuildStamp = stamp
	local queued = factory.Build({ "mcv" }, function(units)
		if MCVBuildStamp ~= stamp then
			return
		end

		MCVBuildStamp = nil
		local mcv = units[1]
		if mcv == nil or mcv.IsDead or mcv.Type ~= "mcv" then
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
			return
		end

		if GDI.HasNoRequiredUnits() or #GDI.GetActorsByType("fact") > 0 then
			RebuildPending[key] = nil
			RebuildBusy = false
			return
		end

		DeployRebuiltMCV(mcv, building, 0)
	end)

	if not queued then
		MCVBuildStamp = nil
		RebuildPending[key] = nil
		RebuildBusy = false
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			QueueGDIRebuild(building)
		end)
		return
	end

	-- Watchdog: a destroyed weapons factory never calls back.
	Trigger.AfterDelay(DateTime.Seconds(240), function()
		WatchMCVRebuild(building, stamp, factory, 0)
	end)
end

DeployRebuiltMCV = function(mcv, building, attempt)
	local key = BuildingKey(building)

	if not IsLive(mcv) or attempt > 150 then
		RebuildPending[key] = nil
		RebuildBusy = false
		QueueGDIRebuild(building)
		return
	end

	if GDI.HasNoRequiredUnits() then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	-- Another path may have rebuilt the yard in the meantime.
	if #GDI.GetActorsByType("fact") > 0 then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	-- The plot can become permanently blocked while the MCV is on its way.
	if PermanentSiteBlocker(building) then
		local newPos = FindAlternativeSite(building)
		if newPos == nil then
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
			return
		end

		local oldKey = key
		RelocateRebuildSite(building, newPos)
		if RebuildPending[oldKey] then
			RebuildPending[oldKey] = nil
			key = BuildingKey(building)
			RebuildPending[key] = true
		end
		attempt = 0
	end

	-- The MCV deploys with Offset -1,-1, so it stands one cell south-east of
	-- the plot's top-left corner to make the yard land on the plot.
	local mcvCell = CPos.New(building.pos.X + 1, building.pos.Y + 1)
	if attempt == 0 then
		-- Make sure the plot is free before the MCV drives there.
		if not ClearBuildingSite(building) then
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				DeployRebuiltMCV(mcv, building, attempt + 1)
			end)
			return
		end

		mcv.Stance = "ReturnFire"
		mcv.Move(mcvCell)
	end

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		if not IsLive(mcv) then
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
			return
		end

		if GDI.HasNoRequiredUnits() or #GDI.GetActorsByType("fact") > 0 then
			RebuildPending[key] = nil
			RebuildBusy = false
			return
		end

		local location = mcv.Location
		if location.X == building.pos.X + 1 and location.Y == building.pos.Y + 1
			and ClearBuildingSite(building, location) then
			mcv.Deploy()
			FinishMCVRebuild(building, mcv)
		else
			DeployRebuiltMCV(mcv, building, attempt + 1)
		end
	end)
end

-- `Transform.Tick` refuses to deploy when `CanDeploy()` fails (terrain or a
-- blocking building). Verify the yard actually appears and retry a few times
-- instead of leaving an MCV standing around forever. Never create the yard
-- directly: a destroyed or blocked MCV must not produce a free FACT.
FinishMCVRebuild = function(building, mcv, attempt)
	local key = BuildingKey(building)
	attempt = attempt or 0

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		local yard = FindGDIBuildingAt(building)
		if yard ~= nil then
			RebuildPending[key] = nil
			RebuildBusy = false
			RegisterGDIBuilding(yard, true)
			return
		end

		if GDI.HasNoRequiredUnits() or #GDI.GetActorsByType("fact") > 0 then
			RebuildPending[key] = nil
			RebuildBusy = false
			return
		end

		if attempt < 3 and IsLive(mcv) and not PermanentSiteBlocker(building) then
			mcv.Deploy()
			FinishMCVRebuild(building, mcv, attempt + 1)
			return
		end

		-- Give up on this MCV, move it off the plot and keep the order queued
		-- for a later attempt.
		if IsLive(mcv) then
			mcv.Stance = "ReturnFire"
			mcv.Move(CPos.New(building.pos.X, building.pos.Y + 4))
		end

		RebuildPending[key] = nil
		RebuildBusy = false
		QueueGDIRebuild(building)
	end)
end

FinishGDIBuilding = function(building, attempt)
	local key = BuildingKey(building)

	if GDI.HasNoRequiredUnits() then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	if GDIBuildingExists(building) then
		RebuildPending[key] = nil
		RebuildBusy = false
		return
	end

	-- The yard or the tech requirement may disappear while the paid rebuild
	-- is in flight. Refund and queue the entry again instead of placing it.
	if building.type ~= "fact" and #GDI.GetActorsByType("fact") == 0 then
		GDI.Cash = GDI.Cash + Actor.Cost(building.type)
		RebuildPending[key] = nil
		RebuildBusy = false
		QueueGDIRebuild(building)
		return
	end

	if not CanBuildGDIBuilding(building.type) then
		GDI.Cash = GDI.Cash + Actor.Cost(building.type)
		RebuildPending[key] = nil
		RebuildBusy = false
		QueueGDIRebuild(building)
		return
	end

	if PermanentSiteBlocker(building) then
		local newPos = FindAlternativeSite(building)
		if newPos ~= nil then
			local oldKey = key
			RelocateRebuildSite(building, newPos)
			if RebuildPending[oldKey] then
				RebuildPending[oldKey] = nil
				key = BuildingKey(building)
				RebuildPending[key] = true
			end
		end
	end

	if not ClearBuildingSite(building) then
		if attempt >= RebuildSiteTimeoutAttempts then
			GDI.Cash = GDI.Cash + Actor.Cost(building.type)
			RebuildPending[key] = nil
			RebuildBusy = false
			QueueGDIRebuild(building)
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			FinishGDIBuilding(building, attempt + 1)
		end)
		return
	end

	RegisterGDIBuilding(Actor.Create(building.type, true, { Owner = GDI, Location = building.pos }), true)
	RebuildPending[key] = nil
	RebuildBusy = false
end

PickRebuildCandidate = function()
	local hard = Difficulty == "hard"
	local now = DateTime.GameTime
	local yardAlive = #GDI.GetActorsByType("fact") > 0
	local noHarvester = #GDI.GetActorsByType("harv") == 0
	local eligible = {}

	for _, entry in ipairs(RebuildQueue) do
		local ok = (entry.due == nil or now >= entry.due) and not GDIBuildingExists(entry)
		if ok and entry.type ~= "fact" and not yardAlive then
			ok = false
		end
		if ok and not CanBuildGDIBuilding(entry.type) then
			ok = false
		end
		-- The yard needs an MCV from a weapons factory (or an existing MCV);
		-- otherwise the order cannot be fulfilled and must stay queued.
		if ok and entry.type == "fact" and #GDI.GetActorsByType("weap") == 0
			and #GDI.GetActorsByType("mcv") == 0 then
			ok = false
		end
		if ok and hard and (NoHarvesterSince ~= nil or noHarvester)
			and (entry.sold or not EconomyRebuildTypes[entry.type]) then
			ok = false
		end
		-- While the base is under attack nothing is rebuilt: the credits are
		-- better spent on units defending it.
		if ok and DateTime.GameTime < BaseUnderAttackUntil then
			ok = false
		end
		-- A permanently blocked plot is still rebuildable if a nearby free
		-- footprint exists. Relocate once here, not on every poll.
		if ok and PermanentSiteBlocker(entry) then
			local newPos = FindAlternativeSite(entry)
			if newPos == nil then
				ok = false
			else
				RelocateRebuildSite(entry, newPos)
			end
		end
		if ok then
			table.insert(eligible, entry)
		end
	end

	table.sort(eligible, function(a, b)
		return GDIBuildingPriority[a.type] < GDIBuildingPriority[b.type]
	end)

	-- An important building that is too expensive must not hold up cheap
	-- ones (artillery towers, ground weapons, silos).
	for _, entry in ipairs(eligible) do
		if GDIFunds() >= Actor.Cost(entry.type) then
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

	local now = DateTime.GameTime
	local remaining = {}
	for _, entry in ipairs(RebuildQueue) do
		-- Keep sold entries until their rebuild delay has passed.
		if (entry.due ~= nil and now < entry.due) or not GDIBuildingExists(entry) then
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
		PlaceGDIBuilding(candidate)
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

TrySendGroup = function()
	if not AutoAttackStarted or BaseDefenseTriggered or DateTime.GameTime < NextGroupTime then
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
			Utils.Do(CellsOf(moves), function(cell)
				unit.AttackMove(cell)
			end)
			IdleHunt(unit)
		else
			table.insert(remaining, unit)
		end
	end
	ProducedGDIUnits = remaining
end

-- A damaged GDI unit opens fire on its attacker instead of waiting to be
-- destroyed from beyond its own range. Units that are busy chasing or
-- patrolling keep their orders.
ReplyToFire = function(unit, attacker)
	if BaseDefenseTriggered or unit.IsDead or attacker == nil or attacker.IsDead or not attacker.HasProperty("Location") then
		return
	end

	-- Harvesters keep working; buildings trigger the base defence.
	if unit.HasProperty("FindResources") then
		return
	end

	if not unit.HasProperty("Attack") or not unit.CanTarget(attacker) then
		return
	end

	if unit.IsIdle then
		unit.Attack(attacker)
	end
end

-- Only map actors get Trigger.OnDamaged (see MapGroundUnits); runtime
-- actors are handled by their stance. A nil reference would abort the
-- script with a NullReferenceException.
ReplyWhenHit = function(actor)
	if actor.Type ~= "harv" and not actor.HasProperty("TargetAirstrike") then
		Trigger.OnDamaged(actor, ReplyToFire)
	end
end

-- A looping Patrol is not interrupted by IdleHunt: the activity keeps the
-- unit busy. Cancel the patrol explicitly when the base alarm or the global
-- hunt starts.
StopPatrols = function()
	local patrols = ActivePatrols
	ActivePatrols = {}
	ForEach(patrols, function(unit)
		if IsLive(unit) then
			unit.Stop()
			unit.Stance = "AttackAnything"
			IdleHunt(unit)
		end
	end)
end

-- The whole GDI army reacts when its main base is attacked, like the
-- original house-level `Attacked` trigger: every ground unit hunts, the
-- bridge Mammoths join in and produced units stop waiting in the base.
StartBaseDefense = function()
	if BaseDefenseTriggered then
		return
	end

	BaseDefenseTriggered = true
	AutoAttackStarted = true
	BaseUnderAttackUntil = DateTime.GameTime + DateTime.Seconds(BaseDefenseWarningSeconds)
	Media.PlaySpeechNotification(Nod, "EnemyUnitsApproaching")

	StopPatrols()

	ForEach(GDI.GetGroundAttackers(), function(actor)
		if IsLive(actor) then
			actor.Stance = "AttackAnything"
			IdleHunt(actor)
		end
	end)

	ForEach(GDI.GetActorsByType("orca"), function(orca)
		if IsLive(orca) and not IsOrcaRetreating(orca) then
			IdleHunt(orca)
		end
	end)

	ForEach(ProducedGDIUnits, function(unit)
		if IsLive(unit) then
			IdleHunt(unit)
		end
	end)

	-- The bridge Mammoths are in GetGroundAttackers and hunt with the rest.
	ProducedGDIUnits = {}
end

-- Called when any GDI building is damaged. Buildings in the main base and
-- the Advanced Communications Center trigger the full base defence; damage
-- to the two west towers only pauses the rebuilds.
BaseBuildingDamaged = function(actor, attacker, damage)
	if not IsLive(actor) or actor.Owner ~= GDI then
		return
	end

	-- Repair steps arrive as negative damage with the building itself as the
	-- attacker. They must not extend the alert or start the base defence.
	if damage == nil or damage <= 0 or attacker == nil or attacker == actor then
		return
	end

	BaseUnderAttackUntil = DateTime.GameTime + DateTime.Seconds(BaseDefenseWarningSeconds)
	if BaseDefenseTriggered then
		return
	end

	if actor.Location.X >= MainBaseMinX and actor.Location.Y <= MainBaseMaxY then
		StartBaseDefense()
	end
end

local function CanProduceGDIUnit(unitType)
	local prerequisites = GDIUnitPrerequisites[unitType]
	return prerequisites == nil or GDI.HasPrerequisites(prerequisites)
end

local ProductionWatchdogMaxChecks = 4

local function WatchGDIProduction(buildingType, stamp, factory, unitType, checks)
	checks = checks or 0
	if ProductionBusy[buildingType] ~= stamp then
		return
	end

	local stillProducing = factory ~= nil and not factory.IsDead and factory.Owner == GDI and factory.IsInWorld
		and factory.HasProperty("IsProducing") and factory.IsProducing(unitType)
	if stillProducing and checks < ProductionWatchdogMaxChecks then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			WatchGDIProduction(buildingType, stamp, factory, unitType, checks + 1)
		end)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if ProductionBusy[buildingType] == stamp then
			ProductionBusy[buildingType] = nil
			ProduceGDIUnit(buildingType)
		end
	end)
end

WatchOrcaProduction = function(stamp, pad, checks)
	checks = checks or 0
	if OrcaBusy ~= stamp then
		return
	end

	local stillProducing = pad ~= nil and not pad.IsDead and pad.Owner == GDI and pad.IsInWorld
		and pad.HasProperty("IsProducing") and pad.IsProducing("orca")
	if stillProducing and checks < ProductionWatchdogMaxChecks then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			WatchOrcaProduction(stamp, pad, checks + 1)
		end)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if OrcaBusy == stamp then
			OrcaBusy = nil
			ProduceOrca()
		end
	end)
end

ProduceGDIUnit = function(buildingType)
	if not GDIProductionStarted then
		return
	end

	local retry = function()
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			ProduceGDIUnit(buildingType)
		end)
	end

	if ProductionBusy[buildingType] ~= nil then
		return
	end

	local factories = GDI.GetActorsByType(buildingType)
	if #factories == 0 then
		retry()
		return
	end

	-- Production depends on the economy: no harvester, no new units.
	if #GDI.GetActorsByType("harv") == 0 then
		retry()
		return
	end

	if CountProducedUnits() >= MaxProducedUnits[Difficulty or "normal"] then
		TrySendGroup()
		retry()
		return
	end

	local types = GDIProductionTypes[buildingType]
	local unitType
	for _ = 1, #types do
		local candidate = table.remove(types, 1)
		table.insert(types, candidate)
		if CanProduceGDIUnit(candidate) then
			unitType = candidate
			break
		end
	end
	if unitType == nil then
		retry()
		return
	end

	local stamp = DateTime.GameTime
	local queued = factories[1].Build({ unitType }, function(units)
		if ProductionBusy[buildingType] ~= stamp then
			return
		end

		ProductionBusy[buildingType] = nil
		-- `units` holds fresh actors; only the actually requested product may
		-- be treated as a combat unit (a stale callback can deliver a HARV).
		ForEach(units, function(unit)
			if not IsLive(unit) or unit.Type ~= unitType or not unit.HasProperty("AttackMove") then
				return
			end

			if BaseDefenseTriggered then
				unit.Stance = "AttackAnything"
				IdleHunt(unit)
			else
				table.insert(ProducedGDIUnits, unit)
			end
		end)
		TrySendGroup()
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			ProduceGDIUnit(buildingType)
		end)
	end)

	if queued then
		ProductionBusy[buildingType] = stamp
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			WatchGDIProduction(buildingType, stamp, factories[1], unitType)
		end)
	else
		retry()
	end
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
		if not IsOrcaRetreating(orca) then
			return
		end

		if #GDI.GetActorsByType("hpad") == 0 then
			-- Without a pad the retreat can never finish. Clear the flag; the
			-- base alarm may put the Orca back into the fight.
			SetOrcaRetreating(orca, false)
			if BaseDefenseTriggered then
				IdleHunt(orca)
			end
		elseif orca.Health * 10 >= orca.MaxHealth * 9 then
			SetOrcaRetreating(orca, false)
			if BaseDefenseTriggered then
				IdleHunt(orca)
			end
		end
	end)
end

-- Orcas: the two helipads on the map may launch aircraft. They defend the
-- base and join the hunt once the main base is attacked. A10 targets under a
-- Nod SAM's local 8-cell cover are avoided; this mission has no global
-- three-SAM stop.
MaxOrcas = { easy = 1, normal = 2, hard = 2 }
OrcaBusy = nil

ProduceOrca = function()
	if not GDIProductionStarted then
		return
	end

	local retry = function()
		Trigger.AfterDelay(DateTime.Seconds(15), ProduceOrca)
	end

	if OrcaBusy ~= nil then
		return
	end

	local pads = GDI.GetActorsByType("hpad")
	-- Same economy rule as ProduceGDIUnit: no harvester, no new units. ORCA
	-- also needs anyhq, so a rejected order must not occupy the Orca slot.
	if #pads == 0 or #GDI.GetActorsByType("harv") == 0
		or not CanProduceGDIUnit("orca")
		or #GDI.GetActorsByType("orca") >= MaxOrcas[Difficulty or "normal"] then
		retry()
		return
	end

	local stamp = DateTime.GameTime
	OrcaBusy = stamp
	local queued = pads[1].Build({ "orca" }, function(units)
		if OrcaBusy ~= stamp then
			return
		end

		OrcaBusy = nil
		ForEach(units, function(orca)
			if not IsLive(orca) or orca.Type ~= "orca" then
				return
			end

			orca.Stance = "AttackAnything"
			Trigger.OnDamaged(orca, OrcaDamaged)
			if BaseDefenseTriggered then
				IdleHunt(orca)
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(10), retry)
	end)

	if queued then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			WatchOrcaProduction(stamp, pads[1], 0)
		end)
	else
		OrcaBusy = nil
		Trigger.AfterDelay(DateTime.Seconds(15), retry)
	end
end

WatchGDIHarvester = function(factory, stamp, checks)
	checks = checks or 0
	if HarvesterStamp ~= stamp then
		return
	end

	local stillProducing = factory ~= nil and not factory.IsDead and factory.Owner == GDI and factory.IsInWorld
		and factory.HasProperty("IsProducing") and factory.IsProducing("harv")
	if stillProducing and checks < 10 then
		Trigger.AfterDelay(DateTime.Seconds(120), function()
			WatchGDIHarvester(factory, stamp, checks + 1)
		end)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if HarvesterStamp == stamp then
			HarvesterStamp = nil
			HarvesterBuilding = false
		end
	end)
end

BuildGDIHarvester = function(factory)
	if HarvesterBuilding then
		return
	end

	HarvesterBuilding = true
	local stamp = DateTime.GameTime
	HarvesterStamp = stamp

	local queued = factory.Build({ "harv" }, function(units)
		if HarvesterStamp ~= stamp then
			return
		end

		HarvesterStamp = nil
		HarvesterBuilding = false
		if not GDIHarvestingStarted then
			return
		end

		ForEach(units, function(harvester)
			-- A stale HARV callback must never call FindResources on another
			-- vehicle type.
			if IsLive(harvester) and harvester.Type == "harv" then
				harvester.FindResources()
			end
		end)
	end)

	if not queued then
		HarvesterStamp = nil
		HarvesterBuilding = false
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(120), function()
		WatchGDIHarvester(factory, stamp)
	end)
end

-- If GDI has lost its last harvester and cannot afford a new one, sell
-- non-essential buildings to recover. Silos are never sold: the mission's
-- storage capacity depends on them.
EconomySellOrder = { "gtwr", "atwr", "pyle", "hpad" }
PowerPlantType = "nuk2"
PowerPlantOutput = 200
EconomyGraceSeconds = 30
EconomySellCooldownSeconds = 20

-- Returns true while a sale was made or is still cooling down, false if there
-- is nothing left to sell.
SellForEconomy = function()
	local candidate = nil
	for _, buildingType in ipairs(EconomySellOrder) do
		local list = GDI.GetActorsByType(buildingType)
		-- Orcas need the helipad to repair and return: sold last.
		local keepPad = buildingType == "hpad" and #GDI.GetActorsByType("orca") > 0
		if #list > 0 and not keepPad then
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

-- GDI economy watchdog: keep one harvester alive so the base does not run
-- dry. The normal path is the weapons factory (the production queue pays).
-- Without a factory the harvester is placed at the refinery directly.
StartEconomyWatchdog = function()
	if EconomyWatchdogRunning then
		return
	end

	EconomyWatchdogRunning = true
	CheckGDIEconomy()
end

CheckGDIEconomy = function()
	if GDI.HasNoRequiredUnits() or AllOutAttackStarted or not GDIHarvestingStarted then
		EconomyWatchdogRunning = false
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

	if HarvesterBuilding then
		return
	end

	local factories = GDI.GetActorsByType("weap")
	local refineries = GDI.GetActorsByType("proc")

	if #factories > 0 and #refineries > 0 and GDIFunds() >= Actor.Cost("harv") then
		BuildGDIHarvester(factories[1])
		return
	end

	-- No weapons factory: do not bypass the normal production queue with a
	-- scripted Actor.Create("harv"). The rebuild worker restores the WEAP (or
	-- a refinery whose FreeActor provides the harvester); wait for that path.
	-- If it cannot be restored, the grace/sell/all-out logic below handles it.

	-- Not enough money: sell something after a short grace period.
	if DateTime.GameTime - NoHarvesterSince < DateTime.Seconds(EconomyGraceSeconds) then
		return
	end

	if SellForEconomy() then
		return
	end

	-- Nothing left to do: all-out attack (not on Easy).
	if Difficulty ~= "easy" then
		AllOutAttackStarted = true
		StartBaseDefense()
	end
end

-- When only unarmed mobile units or buildings are left, GetGroundAttackers
-- returns nothing. A10 and Ion must still have a target; otherwise they
-- silently skip every strike in the late game.
NodTargets = function()
	local list = {}
	for _, actor in ipairs(Nod.GetActors()) do
		if actor ~= nil and not actor.IsDead and actor.HasProperty("Location") then
			table.insert(list, actor)
		end
	end
	return list
end

FindGDIAirstrikeTarget = function()
	if GDIAdvComCenter.IsDead then
		return nil
	end

	-- Never aim at anything standing near the Advanced Communications
	-- Center: the bombs would destroy the mission objective. Another cluster
	-- to the south is preferred so the bridge tanks keep their guard job.
	local center = GDIAdvComCenter.Location
	local function targetable(actor)
		if actor == nil or actor.IsDead then
			return false
		end

		local dx = actor.Location.X - center.X
		local dy = actor.Location.Y - center.Y
		if dx * dx + dy * dy <= 100 then
			return false
		end

		-- Units under Nod's SAM cover (and the SAMs themselves) are safe.
		-- Unpowered SAMs cannot fire and therefore do not shield a target.
		local covered = {}
		if Nod.PowerState == "Normal" then
			covered = Map.ActorsInCircle(actor.CenterPosition, WDist.New(8 * 1024), function(other)
				return other.Owner == Nod and other.Type == "sam" and not other.IsDead
			end)
		end
		return #covered == 0 and actor.Type ~= "sam"
	end

	local candidates = Utils.Where(Nod.GetGroundAttackers(), targetable)
	if #candidates == 0 then
		candidates = Utils.Where(NodTargets(), targetable)
	end

	if #candidates == 0 then
		return nil
	end

	local south = Utils.Where(candidates, function(actor)
		return actor.Location.Y >= LayoutSouthGateY
	end)

	-- `AirstrikePower.TargetAirstrike` expects a position (WPos).
	if #south > 0 then
		return Utils.Random(south).CenterPosition
	end

	return Utils.Random(candidates).CenterPosition
end

Airstrike = function()
	if AirstrikeStopped or not IsLive(GDIHQ) or GDIHQ.Owner ~= GDI
		or GDIAirstrikeProxy == nil or not IsLive(GDIAirstrikeProxy)
		or GDIAirstrikeProxy.Owner ~= GDI
		or not GDIAirstrikeProxy.HasProperty("TargetAirstrike") then
		return
	end

	local target = FindGDIAirstrikeTarget()
	if target then
		GDIAirstrikeProxy.TargetAirstrike(target)
		Trigger.AfterDelay(DateTime.Seconds(AirstrikeInterval[Difficulty or "normal"]), Airstrike)
	else
		Trigger.AfterDelay(DateTime.Seconds(10), Airstrike)
	end
end

EnableIonCannon = function()
	if IonCannonOnline or GDIAdvComCenter.IsDead or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	IonCannonOnline = true
	Media.PlaySpeechNotification(Nod, "IonCannonCharging")
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[Difficulty or "normal"]), IonCannonStrike)
end

IonCannonStrike = function()
	if not IonCannonOnline or GDIAdvComCenter.IsDead or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	-- Do not fire next to the Advanced Communications Center: the blast
	-- would destroy the very building the mission is about.
	local eye = GDIAdvComCenter.Location
	local function targetable(actor)
		if actor == nil or actor.IsDead or actor.Type == "sam" then
			return false
		end

		local dx = actor.Location.X - eye.X
		local dy = actor.Location.Y - eye.Y
		return dx * dx + dy * dy > 100
	end

	local targets = Utils.Where(Nod.GetGroundAttackers(), targetable)
	if #targets == 0 then
		targets = Utils.Where(NodTargets(), targetable)
	end

	if #targets == 0 then
		Trigger.AfterDelay(DateTime.Seconds(15), IonCannonStrike)
		return
	end

	local target = Utils.Random(targets)
	GDIAdvComCenter.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[Difficulty or "normal"]), IonCannonStrike)
end

-- GDI production begins a fixed time after the mission starts (the
-- original `prod` cells only exist as a memory in the port), so it does not
-- depend on the player crossing the bridge.
ScheduleGDIProduction = function()
	Trigger.AfterDelay(DateTime.Seconds(ProductionStartDelay[Difficulty or "normal"]), function()
		ProdTriggered = true
		TryStartGDIProduction()
	end)
end

TryStartGDIProduction = function()
	if GDIProductionStarted or not ProdTriggered then
		return
	end

	-- A little reserve, otherwise GDI would start broke and instantly dry.
	if GDIFunds() < 1000 then
		Trigger.AfterDelay(DateTime.Seconds(30), TryStartGDIProduction)
		return
	end

	GDIProductionStarted = true
	StartGDIRebuild()
	ProduceGDIUnit("pyle")
	ProduceGDIUnit("weap")
	ProduceOrca()
	StartEconomyWatchdog()
	OrcaWatch()
end

StartAttackSchedule = function()
	local times = WaveTimes[Difficulty or "normal"]

	Trigger.AfterDelay(DateTime.Seconds(times.grd2), StartJeepPatrol)
	Trigger.AfterDelay(DateTime.Seconds(times.grd1), StartRocketPatrol)

	Trigger.AfterDelay(DateTime.Seconds(times.apc2), function()
		SpawnGuardAPC({ "e1", "e1", "e1", "e6", "e6" })
	end)
	Trigger.AfterDelay(DateTime.Seconds(times.apc3), function()
		SpawnGuardAPC({ "e1", "e1", "e1", "e6", "e6" })
	end)
	Trigger.AfterDelay(DateTime.Seconds(times.apc1), function()
		SpawnGuardAPC({ "e6", "e6", "rmbo" })
	end)

	Trigger.AfterDelay(DateTime.Seconds(times.atk1), function() AttackWave(TeamDefs.gdi2) end)
	Trigger.AfterDelay(DateTime.Seconds(times.atk2), function() AttackWave(TeamDefs.gdi3) end)
	Trigger.AfterDelay(DateTime.Seconds(times.atk3), function() AttackWave(TeamDefs.gdi5) end)
	Trigger.AfterDelay(DateTime.Seconds(times.atk4), function() AttackWave(TeamDefs.gdi6) end)
	Trigger.AfterDelay(DateTime.Seconds(times.atk5), function() AttackWave(TeamDefs.gdi2) end)

	Trigger.AfterDelay(DateTime.Seconds(times.auto), function()
		-- `auto` (Autocreate) alerts the house; only from this point on do
		-- produced units leave the base to attack.
		AutoAttackStarted = true
		TrySendGroup()
		AutoWave()
	end)

	Trigger.AfterDelay(DateTime.Seconds(AirstrikeFirstDelay[Difficulty or "normal"]), Airstrike)
end

SetupAI = function()
	GDI.Cash = StartingCash[Difficulty or "normal"]
	RepairNamedActors(GDI, 0.75)

	-- Silos are deliberately not registered: a destroyed silo is not rebuilt
	-- (see GDIBaseBuildings), the credits go into units instead.
	Utils.Do({
		GDICYard, GDIProc, GDIWeaponsFactory, GDIBarracks,
		GDINuke1, GDINuke2, GDINuke3,
		GDIATWR1, GDIATWR2, GDIGTWR1, GDIGTWR2,
	}, function(actor)
		if IsLive(actor) and actor.Owner == GDI then
			RegisterGDIBuilding(actor, true)
		end
	end)

	Utils.Do({ GDIHQ, GDIAdvComCenter, GDIHelipad1, GDIHelipad2, GDIWestGTWR1, GDIWestGTWR2 }, function(actor)
		if IsLive(actor) and actor.Owner == GDI then
			RegisterGDIBuilding(actor, false)
		end
	end)

	-- Every GDI ground unit placed on the map reacts to the first hit instead
	-- of being destroyed from beyond its own range. The list is explicit
	-- (MapGroundUnits) so no runtime actor reaches Trigger.OnDamaged; the
	-- bridge guards are handled in nod12.lua (brdg).
	Utils.Do(MapGroundUnits, function(actor)
		if IsLive(actor) and actor ~= BridgeMammoth1 and actor ~= BridgeMammoth2 then
			ReplyWhenHit(actor)
		end
	end)

	-- The bridge guards hold their post until they are attacked or the base
	-- alarm goes off.
	Utils.Do({ BridgeMammoth1, BridgeMammoth2 }, function(unit)
		if IsLive(unit) and unit.Owner == GDI then
			unit.Stance = "Defend"
		end
	end)

	BridgePost = {}
	for i = 1, 2 do
		local unit = (i == 1) and BridgeMammoth1 or BridgeMammoth2
		if IsLive(unit) then
			BridgePost[i] = { x = unit.Location.X, y = unit.Location.Y }
		end
	end

	Utils.Do(HuntBuildings, function(actor)
		if IsLive(actor) and actor.Owner == GDI then
			Trigger.OnKilled(actor, CheckForHunt)
		end
	end)


	Trigger.OnEnteredFootprint(Grd1Cells, function(actor, id)
		if actor.Owner ~= Nod or RocketPatrolStarted then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		StartRocketPatrol()
	end)

	-- `prod` starts GDI production early; the timer above covers it anyway.
	Trigger.OnEnteredFootprint(ProdCells, function(actor, id)
		if actor.Owner ~= Nod or ProdTriggered then
			return
		end

		ProdTriggered = true
		Trigger.RemoveFootprintTrigger(id)
		TryStartGDIProduction()
	end)

	StartAttackSchedule()
	ScheduleGDIProduction()
end
