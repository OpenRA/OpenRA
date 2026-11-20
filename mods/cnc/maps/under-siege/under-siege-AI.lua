--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.

   Shared with under-siege.lua: GDI, InitGDI, MissionFinished, Nod.
]]

local AI = CampaignAI

local AllToHunt
local MaintenanceTick

local GDIProductionStarted = false
local GDIAIAlerted = false
local GDIHome = nil
local GDIStartingUnits
local GDI_REBUILD_OPTIONS = nil
local GIBaseDefenseContext = nil
local GIContext = nil
local GICounterContext = nil
local GIUnitContext = nil
local GDIAirstrikeProxy = nil
local GDINukeRetaliation = false
local GDIPendingNukes = {}
local HuntMessageSent = false
local NukeMessageSent = false

-- Original times: `Time` is measured in 6 s ticks (HouseClass::AI).
--   astk 80  -> 480 s = 8:00, repeating every 480 s (persist 2)
--            (easy waits two more minutes, see Profiles)
--   prod 100 -> 600 s = 10:00
--   auto 160 -> 960 s = 16:00
local AIRSTRIKE_INTERVAL = 480
local PRODUCTION_DELAY = 600
local ALERT_DELAY = 960
local MAINTENANCE_INTERVAL = 20

local GDI_ECONOMY_OPTIONS = {
	harvestersPerRefinery = 1,
	strainedFunds = 1500,
	crisisFunds = 500,
}

-- The original scenario has no difficulty levels, so the per-grade
-- differences are a documented design addition:
--   easy   - two fronts can be opened without the global hunt, and the
--            first A10 arrives two minutes later than the original 8:00.
--   normal - original behaviour.
--   hard   - original escalation, plus pre-placed reinforcements.
local Profiles = {
	easy = {
		productionInterval = 50,
		maxProduced = 4,
		maxPerCategory = 4,
		reserve = 600,
		vehiclePool = { "jeep", "mtnk" },
		airstrikeFirstDelay = 600,
		sectorHuntThreshold = 3,
	},
	normal = {
		productionInterval = 38,
		maxProduced = 6,
		maxPerCategory = 6,
		reserve = 400,
		vehiclePool = { "mtnk", "jeep", "msam" },
		airstrikeFirstDelay = 480,
		sectorHuntThreshold = 2,
	},
	hard = {
		productionInterval = 28,
		maxProduced = 8,
		maxPerCategory = 8,
		reserve = 250,
		vehiclePool = { "mtnk", "msam", "htnk" },
		airstrikeFirstDelay = 480,
		sectorHuntThreshold = 2,
	},
}

local RebuildPlan = {
	{ "fact", 9, 6 },
	{ "proc", 13, 7 },
	{ "weap", 19, 7 },
	{ "silo", 12, 6 },
	{ "silo", 15, 6 },
	{ "eye", 9, 11 },
}

-- Original TeamTypes that the broken MaxAllowed=0 data cannot create.
-- Routes are the original waypoints: 2 = (55,32), 1 = (6,32), 0 = (33,18).
local AutoTeams = {
	{ units = { { "mtnk", 2 } }, route = { CPos.New(55, 32) }, attack = true, baseAttack = true },
	{ units = { { "mtnk", 2 } }, route = { CPos.New(33, 18) }, attack = false },
	{ units = { { "htnk", 1 } }, route = { CPos.New(6, 32) }, attack = true, baseAttack = true },
	{ units = { { "htnk", 1 } }, route = { CPos.New(33, 18) }, attack = true, baseAttack = true },
}

local function Profile()
	return Profiles[Difficulty] or Profiles.normal
end

-- Hard-only reinforcements. The original scenario has no difficulty
-- levels, so these are an addition; the cells are ordered preference
-- lists and the first passable, unoccupied cell wins (deterministic, no
-- randomness). Placement keeps the bypass corridors around the sectors
-- open: north group y16-17 and x26-27, east group x52-55, west group
-- x6-7 and y36-38.
--
-- The east/west groups get their extras on the side facing the Nod base,
-- the north group gets them inside its own band. The base group prefers
-- cells inside the GDI base (row 13, north of the wall) and falls back to
-- the open ground left of and below the Advanced Communications Center at
-- (9,11), outside the wall, if those cells are blocked.
local BaseReinforcementCells = {
	{ 11, 13 }, { 12, 13 }, { 13, 13 }, { 14, 13 },
	{ 8, 16 }, { 9, 16 }, { 10, 16 }, { 8, 17 },
}

-- MSAMs are the only GDI unit whose OpenRA range (227mm, 11c0) exceeds
-- the original (6 cells), so they must start outside 11 cells of every Nod
-- actor or they open fire on the base at mission start. The *_Msam lists
-- are the furthest cells of each group; the tank lists may sit closer
-- because their guns reach ~5 cells.
local EastTankCells = { { 49, 30 }, { 49, 31 }, { 50, 31 }, { 48, 30 } }
local EastMsamCells = { { 54, 34 }, { 54, 35 }, { 54, 36 } }
local WestTankCells = { { 11, 28 }, { 12, 28 }, { 11, 29 } }
local WestMsamCells = { { 8, 29 }, { 8, 30 }, { 9, 29 }, { 9, 30 } }
-- A single row directly below the northern MSAM pair (32,18)/(34,18) and
-- above the original group at y22, so the four Mammoths form a visible
-- line between them.
local NorthCells = { { 31, 19 }, { 32, 19 }, { 33, 19 }, { 34, 19 } }

local HardReinforcements = {
	-- hnt2 (north): four additional Mammoth tanks
	{ kind = "htnk", sector = "hnt2", count = 4, cells = NorthCells, facing = Angle.South },
	-- hnt1 (east): two Medium tanks and one MSAM
	{ kind = "mtnk", sector = "hnt1", count = 2, cells = EastTankCells, facing = Angle.West },
	{ kind = "msam", sector = "hnt1", count = 1, cells = EastMsamCells, facing = Angle.West },
	-- hnt3 (west): two Medium tanks and one MSAM
	{ kind = "mtnk", sector = "hnt3", count = 2, cells = WestTankCells, facing = Angle.East },
	{ kind = "msam", sector = "hnt3", count = 1, cells = WestMsamCells, facing = Angle.East },
	-- GDI base: two Mammoth tanks and two Medium tanks
	{ kind = "htnk", sector = nil, count = 2, cells = BaseReinforcementCells, facing = Angle.South },
	{ kind = "mtnk", sector = nil, count = 2, cells = BaseReinforcementCells, facing = Angle.South },
}

-- Units spawned by the reinforcement pass, grouped per sector so the
-- semipersistent sector counters (hnt1-hnt3) include them.
local SectorExtras = { hnt1 = {}, hnt2 = {}, hnt3 = {} }

local function IsCellBuildable(cell)
	local terrain = Map.TerrainType(cell)
	-- Mirrors the tracked locomotor in mods/cnc/rules/world.yaml.
	return terrain == "Clear" or terrain == "Road" or terrain == "Rough"
		or terrain == "Bridge" or terrain == "Tiberium"
		or terrain == "BlueTiberium" or terrain == "Beach"
end

local function IsCellFree(cell)
	if not IsCellBuildable(cell) then
		return false
	end

	local center = Map.CenterOfCell(cell)
	local reach = 1536
	local nearby = Map.ActorsInBox(
		WPos.New(center.X - reach, center.Y - reach, 0),
		WPos.New(center.X + reach, center.Y + reach, 0),
		function(actor) return actor.HasProperty("Location") end)

	for _, actor in ipairs(nearby) do
		-- A building within one cell of the candidate would overlap its
		-- footprint; the terrain layer does not know about buildings.
		if actor.HasProperty("StartBuildingRepairs") then
			return false
		end
		if actor.Location.X == cell.X and actor.Location.Y == cell.Y then
			return false
		end
	end
	return true
end

local function CellKey(cell)
	return cell.X .. "," .. cell.Y
end

local function SpawnReinforcements(entries)
	local spawned = {}
	-- Actor.Create only places the actor at the end of the tick, so the
	-- engine cannot tell us about the cells we already handed out.
	local claimed = {}

	for _, entry in ipairs(entries) do
		for _ = 1, entry.count or 1 do
			local cell
			for _, candidate in ipairs(entry.cells) do
				local pos = CPos.New(candidate[1], candidate[2])
				if claimed[CellKey(pos)] == nil and IsCellFree(pos) then
					cell = pos
					break
				end
			end

			if cell == nil then
				-- All preferred cells are blocked; skip instead of stacking a
				-- vehicle on top of a building.
				AI.Log("under-siege: no free cell for hard reinforcement %s", entry.kind)
			else
				claimed[CellKey(cell)] = true
				-- Angle.South/West/East are WAngle objects; the init table
				-- only accepts CLR values, not raw numbers.
				local unit = Actor.Create(entry.kind, true, {
					Owner = GDI,
					Location = cell,
					Facing = entry.facing,
				})
				if entry.sector ~= nil and SectorExtras[entry.sector] ~= nil then
					table.insert(SectorExtras[entry.sector], unit)
				end
				table.insert(spawned, { unit = unit, sector = entry.sector })
			end
		end
	end

	return spawned
end

local function GDIHasBase()
	return #AI.GetActorsByType(GDI, "fact") > 0
end

local GDIHasWeaponsFactory = function()
	return #AI.GetActorsByType(GDI, "weap") > 0
end

local function Active()
	return not MissionFinished
end

local function Finished()
	return MissionFinished
end

-- ------------------------------------------------ superweapon retaliation

-- OpenRA allows repeated nukes (and, with a captured EYE, an ion strike).
-- A superweapon therefore counts as a declaration of total war. The trigger
-- must fire on the DETONATION, not on the launch: NukeLaunch applies its
-- warhead with SourceActor = the firing player's PlayerActor, so a damage
-- event on a GDI actor with a Nod PlayerActor as attacker identifies the
-- impact. A fallback timer (nuke flight is ~16 s) covers a strike that hits
-- nothing important.
local NUKE_FLIGHT_FALLBACK = 20

local NukeImpactWatchers = {}

local function PendingNukeCount()
	local count = 0
	for _, token in ipairs(GDIPendingNukes) do
		if not token.resolved then
			count = count + 1
		end
	end
	return count
end

local function ResolveOldestNuke()
	for _, token in ipairs(GDIPendingNukes) do
		if not token.resolved then
			token.resolved = true
			return true
		end
	end
	return false
end

local function IsSuperweaponAttacker(attacker)
	if attacker == nil or attacker.Owner ~= Nod then
		return false
	end
	-- The superweapon warhead uses the owner's PlayerActor as SourceActor.
	-- The Property fallback also covers engine versions that expose the
	-- system actor under a different name.
	return attacker.Type == "Player" or not attacker.HasProperty("Location")
end

local function RetaliateToSuperweapon()
	if MissionFinished then
		return
	end

	-- Suppress the generic hunt text; the nuclear message is more specific.
	if not GDINukeRetaliation then
		GDINukeRetaliation = true
		HuntMessageSent = true
	end
	AllToHunt()

	if not NukeMessageSent then
		NukeMessageSent = true
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-nuke-retaliation"))
	end
end

local function OnSuperweaponImpact()
	if not ResolveOldestNuke() then
		return
	end
	RetaliateToSuperweapon()
end

local function RegisterSuperweaponWatcher(actor)
	if not AI.IsLive(actor) or not actor.HasProperty("Health") then
		return
	end

	local key = AI.ActorKey(actor)
	if NukeImpactWatchers[key] then
		return
	end
	NukeImpactWatchers[key] = true

	Trigger.OnDamaged(actor, function(self, attacker)
		if PendingNukeCount() > 0 and IsSuperweaponAttacker(attacker) then
			OnSuperweaponImpact()
		end
	end)
end

local function OnNukeLaunched()
	if MissionFinished then
		return
	end

	local token = { resolved = false }
	table.insert(GDIPendingNukes, token)

	Trigger.AfterDelay(DateTime.Seconds(NUKE_FLIGHT_FALLBACK), function()
		if MissionFinished or token.resolved then
			return
		end
		token.resolved = true
		RetaliateToSuperweapon()
	end)
end

-- --------------------------------------------------------------- unit handling

local function RegisterGDIUnit(unit)
	if not AI.IsLive(unit) or unit.Owner ~= GDI or unit.Type == "harv" then
		return
	end

	AI.RegisterUnit(unit, GIUnitContext)
	RegisterSuperweaponWatcher(unit)
	if unit.HasProperty("Stance") then
		unit.Stance = "Defend"
	end
end

local GDIRegisteredBuildings = {}

local function RegisterGDIBuilding(building)
	if not AI.IsLive(building) or building.Owner ~= GDI or not building.HasProperty("StartBuildingRepairs") then
		return
	end

	local key = AI.ActorKey(building)
	if GDIRegisteredBuildings[key] then
		return
	end
	GDIRegisteredBuildings[key] = true
	AI.RegisterDamageHandler(building, GIBaseDefenseContext)
	RegisterSuperweaponWatcher(building)
end

local function StartGDIBaseDefense(attacker)
	if not AI.IsLive(attacker) or MissionFinished then
		return
	end
	AI.DefendAgainstAttacker(GDIHome, attacker, GICounterContext, {
		alarmSeconds = 45,
		isFinished = Finished,
		releaseOrcas = false,
	})
end

-- --------------------------------------------------------------- hunt triggers

-- Original trigger data (SCB35EA.INI):
--   hnt1 = Destroyed, All to Hunt, Persist 1  -> East  (x 50-53, y 29-36)
--   hnt2 = Destroyed, All to Hunt, Persist 1  -> North (x 28-37, y 20-23)
--   hnt3 = Destroyed, All to Hunt, Persist 1  -> West  (x 8-10,  y 28-35)
-- Persist 1 is SEMIPERSISTANT (trigger.h): the trigger detaches from each
-- object and only fires when AttachCount reaches zero, i.e. after the last
-- of the seven vehicles. All 46 GDI infantry carry Trigger=None and never
-- count towards a sector.
--
-- Playtest finding: firing "All to Hunt" on the first kill removed the
-- stealth approach entirely. The port therefore keeps the original
-- semipersistent rule (S0) and adds sector-wise escalation (S1):
--   * first kill in a sector -> only that sector hunts;
--   * first kill in a second sector -> the remaining sectors hunt too;
--   * full clear (7/7) of any sector -> global hunt (original S0).
local Sectors = {}

local SectorUnits = {
	{ name = "hnt1", units = function() return {
		Hnt1Jeep1, Hnt1Jeep2, Hnt1Msam1, Hnt1Msam2, Hnt1Mtnk, Hnt1Htnk1, Hnt1Htnk2 } end },
	{ name = "hnt2", units = function() return {
		Hnt2Jeep1, Hnt2Jeep2, Hnt2Msam1, Hnt2Msam2, Hnt2Mtnk, Hnt2Htnk1, Hnt2Htnk2 } end },
	{ name = "hnt3", units = function() return {
		Hnt3Jeep1, Hnt3Jeep2, Hnt3Msam1, Hnt3Msam2, Hnt3Mtnk, Hnt3Htnk1, Hnt3Htnk2 } end },
}

local function HuntUnit(unit)
	if not AI.IsLive(unit) or not unit.HasProperty("Hunt") then
		return
	end

	-- Original Do_All_To_Hunt only walks the Units and Infantry lists, never
	-- Aircraft. Scripted A10s have AttackBomber + Aircraft, so they expose
	-- Hunt too; Stop+Hunt would cancel their Fly/RemoveSelf queue and leave
	-- them loitering over the map. "Land" is the AircraftProperties member.
	if unit.HasProperty("Land") then
		return
	end

	if unit.HasProperty("Stance") then
		unit.Stance = "AttackAnything"
	end
	-- Original Assign_Mission(MISSION_HUNT) replaces the current mission;
	-- Stop+Hunt does the same in OpenRA and avoids stacking one Hunt
	-- activity per trigger death.
	if unit.HasProperty("Stop") then unit.Stop() end
	unit.Hunt()
end

local function ShowHuntMessage()
	if HuntMessageSent then
		return
	end
	HuntMessageSent = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-hunt"))
end

AllToHunt = function()
	if MissionFinished then
		return
	end

	-- Ground attackers only (vehicles + infantry), matching the original
	-- Units/Infantry scan; this also keeps scripted aircraft out.
	for _, unit in ipairs(GDI.GetGroundAttackers()) do
		HuntUnit(unit)
	end
	for _, sector in ipairs(Sectors) do
		sector.hunting = true
	end
	ShowHuntMessage()
end

local function HuntSector(sector)
	if sector.hunting then
		return
	end
	sector.hunting = true
	for _, unit in ipairs(sector.units) do
		HuntUnit(unit)
	end
end

local function HuntingSectors()
	local count = 0
	for _, sector in ipairs(Sectors) do
		if sector.hunting then
			count = count + 1
		end
	end
	return count
end

local function OnSectorLoss(sector)
	if MissionFinished then
		return
	end

	sector.killed = sector.killed + 1

	-- S0: the semipersistent trigger fires with the last attached vehicle.
	if sector.killed >= sector.size then
		AllToHunt()
		return
	end

	-- S1: the attacked sector reacts immediately; a second front wakes the
	-- rest. This reproduces the original "one side, then the others" feel.
	HuntSector(sector)
	-- Easy tolerates two fronts before the rest of GDI joins in; Normal and
	-- Hard keep the original two-front escalation.
	if HuntingSectors() >= Profile().sectorHuntThreshold then
		AllToHunt()
	end
end

-- Every Temple of Nod (the pre-placed one and any rebuilt replacement) is
-- watched for support-power launches; the retaliation itself happens on
-- detonation, see RetaliateToSuperweapon above.
local NukeTemples = {}

local function RegisterNukeTemple(temple)
	if not AI.IsLive(temple) or temple.Owner ~= Nod then
		return
	end
	local key = AI.ActorKey(temple)
	if NukeTemples[key] then
		return
	end
	NukeTemples[key] = true

	Trigger.OnSupportPowerActivated(temple, function(self, orderName)
		OnNukeLaunched()
	end)
end

local function SetupNukeRetaliation()
	for _, temple in ipairs(Nod.GetActorsByType("tmpl")) do
		RegisterNukeTemple(temple)
	end

	-- A sold/destroyed temple can be rebuilt; hook the replacement too.
	Trigger.OnBuildingPlaced(Nod, function(_, placed)
		if placed ~= nil and placed.Type == "tmpl" then
			RegisterNukeTemple(placed)
		end
	end)
end

local function SetupHuntTriggers()
	Sectors = {}
	for _, definition in ipairs(SectorUnits) do
		local sector = { name = definition.name, killed = 0, size = 0, hunting = false, units = {} }
		local members = {}
		for _, unit in ipairs(definition.units()) do table.insert(members, unit) end
		for _, unit in ipairs(SectorExtras[definition.name] or {}) do table.insert(members, unit) end

		for _, unit in ipairs(members) do
			if AI.IsLive(unit) then
				-- size grows with Hard reinforcements, so "sector cleared"
				-- always means every vehicle of that side is gone.
				sector.size = sector.size + 1
				table.insert(sector.units, unit)
				Trigger.OnKilled(unit, function() OnSectorLoss(sector) end)
			end
		end
		table.insert(Sectors, sector)
	end
end

-- --------------------------------------------------------------- production

local function StartGDIProduction()
	if GDIProductionStarted or MissionFinished then
		return
	end

	GDIProductionStarted = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-production"))

	local profile = Profile()
	AI.StartProduction(GDIHome, GIContext, {
		pools = { vehicle = profile.vehiclePool },
		factories = { vehicle = "weap" },
		interval = profile.productionInterval,
		maxTotal = profile.maxProduced,
		maxPerCategory = profile.maxPerCategory,
		reserve = profile.reserve,
		economy = GDI_ECONOMY_OPTIONS,
		produceOrcas = false,
		firstDelay = 0,
		isActive = function() return Active() and GDIHasBase() and GDIHasWeaponsFactory() and not GDIHome.allOut end,
		isFinished = Finished,
	})

	Trigger.AfterDelay(DateTime.Seconds(MAINTENANCE_INTERVAL), MaintenanceTick)
end

MaintenanceTick = function()
	if MissionFinished then
		return
	end

	AI.SafeCall("gdi-maintenance", function()
		if not GDIHasBase() then
			return
		end

		AI.MaintainBase(GDIHome, GDI_REBUILD_OPTIONS)

		-- A rebuilt refinery spawns its free harvester (original TD rule),
		-- so adopt it before counting the harvester fleet.
		AI.AdoptFreeHarvesters(GDIHome)

		if AI.MaintainHarvesters(GDIHome, GDI_ECONOMY_OPTIONS) then
			AI.ReplaceHarvester(GDIHome, {
				harvesterKind = "harv",
				harvesterProductionKind = "weap",
				harvesterCost = 1100,
				reserve = 0,
			})
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(MAINTENANCE_INTERVAL), MaintenanceTick)
end

-- --------------------------------------------------------------- alert wave

local function AlertGDI()
	if GDIAIAlerted or MissionFinished then
		return
	end

	GDIAIAlerted = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-alerted"))

	local claimed = {}
	for _, definition in ipairs(AutoTeams) do
		local team = AI.FormTeam(GDIHome, GIContext, definition.units, {
			routeStart = definition.route[1],
			reserve = claimed,
		})
		if team ~= nil then
			for _, unit in ipairs(team) do
				claimed[AI.ActorKey(unit)] = true
			end
			AI.SendTeam(team, GDI, definition.route, definition.attack, definition.baseAttack, GIContext)
		end
	end
end

-- --------------------------------------------------------------- A10 airstrikes

-- Original Greatest_Threat rejects cloaked objects (TechnoClass::In_Range),
-- so a hidden Stealth Tank neither becomes an A10 target nor inflates the
-- cluster score of its neighbours. Aircraft are excluded as well: the A10's
-- Vulcan and Napalm are ground-only, so aiming a strike at a helicopter
-- cluster would only make the jet overfly it and bomb the ground beneath.
-- "Land" is the AircraftProperties member.
local function IsNodActor(actor)
	return actor ~= nil and actor.Owner == Nod and not actor.IsDead
		and actor.HasProperty("Location") and not AI.IsCloaked(actor)
		and not actor.HasProperty("Land")
end

local function ClusterScore(actor, mobileOnly)
	local score = 0
	local nearby = Map.ActorsInCircle(actor.CenterPosition, WDist.New(2 * 1024), function(other)
		if not IsNodActor(other) then
			return false
		end
		if mobileOnly and not (other.HasProperty("Move") and other.HasProperty("Attack")) then
			return false
		end
		return true
	end)

	for _, other in ipairs(nearby) do
		local cost = Actor.Cost(other.Type) or 0
		-- Structures are valid targets, but the original Greatest_Threat
		-- prefers mobile threats. Weighting buildings lower keeps the A10
		-- aimed at the army unless only buildings remain.
		if other.HasProperty("StartBuildingRepairs") then
			cost = cost * 0.6
		end
		score = score + cost
	end
	return score
end

local function FindAirstrikeTarget()
	local best, bestScore

	local function consider(actor, mobileOnly)
		-- The candidate itself must be a valid strike target: no aircraft
		-- (ground-only weapons) and no cloaked actors, even if it happens to
		-- sit next to a fat ground cluster.
		if not IsNodActor(actor) then
			return
		end
		local score = ClusterScore(actor, mobileOnly)
		if best == nil or score > bestScore
			or (score == bestScore and actor.ActorID < best.ActorID) then
			best, bestScore = actor, score
		end
	end

	for _, actor in ipairs(Nod.GetActors()) do
		if actor.Type ~= "harv" and actor.HasProperty("Move") and actor.HasProperty("Attack") then
			consider(actor, true)
		end
	end

	if best == nil then
		for _, actor in ipairs(Nod.GetActors()) do
			if actor.HasProperty("StartBuildingRepairs") then
				consider(actor, false)
			end
		end
	end

	return best and best.CenterPosition
end

local function Airstrike()
	if MissionFinished then
		return false
	end

	if GDIAirstrikeProxy == nil or not AI.IsLive(GDIAirstrikeProxy)
		or GDIAirstrikeProxy.Owner ~= GDI
		or not GDIAirstrikeProxy.HasProperty("TargetAirstrike") then
		return false
	end

	local target = FindAirstrikeTarget()
	if target ~= nil then
		GDIAirstrikeProxy.TargetAirstrike(target)
	end
end

-- The reinforcements created above are only added to the world at the end
-- of the tick, so they cannot be registered or counted in the same pass.
-- One tick later they are part of the world; then they join the starting
-- garrison and the sector counters are built from the full roster.
local function CompleteGDIStartup(reinforcements, startingUnits)
	for _, entry in ipairs(reinforcements) do
		local unit = entry.unit
		startingUnits[AI.ActorKey(unit)] = true
		RegisterGDIUnit(unit)
	end
	SetupHuntTriggers()
end

-- --------------------------------------------------------------- setup

InitGDI = function()
	AI.ConfigureLand({ minX = 6, maxX = 55, minY = 6, maxY = 58, stride = 64 })
	AI.BuildLandRegions()

	GDIHome = AI.NewBase({
		player = GDI,
		yard = { 9, 6 },
		defenderReserve = 0,
		baseRadius = 20,
	})

	-- Spawned before the starting-unit scan so they are treated as part of
	-- the initial garrison (never re-seeded into the attack pool).
	SectorExtras = { hnt1 = {}, hnt2 = {}, hnt3 = {} }
	local hardReinforcements = {}
	if Difficulty == "hard" then
		hardReinforcements = SpawnReinforcements(HardReinforcements)
	end

	-- Starting units are not counted against the production caps and are
	-- never re-seeded into the pool by DefendAgainstAttacker.
	GDIStartingUnits = {}
	for _, actor in ipairs(GDI.GetActors()) do
		if actor.HasProperty("Move") then
			GDIStartingUnits[AI.ActorKey(actor)] = true
		end
	end

	local function IsStartUnit(actor)
		return GDIStartingUnits[AI.ActorKey(actor)] == true
	end

	GIContext = {
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		isActive = Active,
		isFinished = Finished,
		exclude = function(actor)
			return IsStartUnit(actor) or not AI.IsLocal(GDIHome, actor, 26)
		end,
	}

	GICounterContext = {
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		keepAttackers = true,
		isActive = Active,
		isFinished = Finished,
		exclude = function(actor)
			return IsStartUnit(actor) or not AI.IsLocal(GDIHome, actor, 26)
				or (actor.HasProperty("Stance") and actor.Stance == "AttackAnything")
		end,
	}

	GIUnitContext = AI.UnitContext(GDIHome, Nod, {
		isActive = Active,
		isFinished = Finished,
	})

	GIBaseDefenseContext = {
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		keepAttackers = true,
		repairThreshold = 0.7,
		isActive = Active,
		isFinished = Finished,
		exclude = function(actor)
			return not AI.IsLocal(GDIHome, actor, 24)
		end,
		onDamage = function(self, attacker)
			StartGDIBaseDefense(attacker)
		end,
	}

	GDI_REBUILD_OPTIONS = {
		plan = RebuildPlan,
		targets = {
			fact = 1,
			proc = 1,
			weap = 1,
			silo = 2,
			eye = 1,
		},
		-- FACT is the gate: if the Construction Yard is gone, GDI loses the
		-- base and does not rebuild the other entries. NUK2 and towers are
		-- deliberately absent because the original [Base] has 6 entries.
		targetOrder = { "weap", "proc", "silo", "eye" },
		lateTargetOrder = {},
		reserve = 0,
		siteOptions = { owner = GDI },
		isActive = function()
			return Active() and GDIHasBase()
				and (GDIHome.underAttackUntil == nil or DateTime.GameTime >= GDIHome.underAttackUntil)
		end,
		onComplete = RegisterGDIBuilding,
	}

	AI.InitEconomy(GDIHome, {
		harvesterKind = "harv",
		refineryKind = "proc",
		harvesterProductionKind = "weap",
	})

	if AI.IsLive(GDIHarv) and GDIHarv.Owner == GDI then
		AI.TrackHarvester(GDIHome, GDIHarv)
		if GDIHarv.HasProperty("FindResources") then
			GDIHarv.FindResources()
		end
	end

	for _, unit in ipairs(GDI.GetGroundAttackers()) do
		if AI.IsLive(unit) and unit.Type ~= "harv" then
			RegisterGDIUnit(unit)
		end
	end

	for _, building in ipairs(GDI.GetActors()) do
		RegisterGDIBuilding(building)
	end

	Trigger.OnAnyProduction(function(factory, unit)
		if unit == nil or unit.Owner ~= GDI then
			return
		end
		if unit.Type == "harv" then
			AI.TrackHarvester(GDIHome, unit)
			if unit.HasProperty("FindResources") then
				unit.FindResources()
			end
			return
		end
		RegisterGDIUnit(unit)
		AI.AddToPool(GDIHome, unit, GIContext)
	end)

	SetupNukeRetaliation()
	AI.StartIdleDefense(GIBaseDefenseContext, 10)

	-- Actor.Create only adds the actors at the end of the tick, so the
	-- sector counters (and the reinforcement registration) run one tick
	-- later, when every unit is part of the world.
	if #hardReinforcements > 0 then
		Trigger.AfterDelay(1, function() CompleteGDIStartup(hardReinforcements, GDIStartingUnits) end)
	else
		SetupHuntTriggers()
	end

	GDIAirstrikeProxy = Actor.Create("airstrike.proxy", true, { Owner = GDI })
	local profile = Profile()
	AI.StartSpecialThreat(GIContext, {
		label = "gdi-airstrike",
		firstDelay = profile.airstrikeFirstDelay,
		interval = AIRSTRIKE_INTERVAL,
		execute = Airstrike,
		isFinished = Finished,
	})

	Trigger.AfterDelay(DateTime.Seconds(PRODUCTION_DELAY), StartGDIProduction)
	Trigger.AfterDelay(DateTime.Seconds(ALERT_DELAY), AlertGDI)
end
