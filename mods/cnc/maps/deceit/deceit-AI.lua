--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI economy, production and attacks for the Nod Covert Operations mission
-- "Deceit" (SCB21EA.INI).
--
-- The original GDI house is active from the start: Nod commandos begin on the
-- eastern edge of the map, inside the GDI perimeter, and the `gau`/`gad`
-- autocreate teams and the `auto`/`hunt` triggers keep the base moving. The
-- port follows the Bad Neighborhood pattern: placed units become the initial
-- garrison, the paid army comes from the real PYLE/WEAP/HPAD queues, and the
-- scripted Chinook/engineer teams stay free like in the original engine.

local AI = CampaignAI
local GDIHome

-- Defined in deceit.lua; keep the engine default if this file is loaded alone.
TransportDropRange = TransportDropRange or 3
SignalRelayUntil = SignalRelayUntil or 0
SignalRelayDelay = SignalRelayDelay or 180
SignalRelayActive = SignalRelayActive or function()
	return DateTime.GameTime < SignalRelayUntil
end
local GDI_AI = {
	enemy = nil,
	players = nil,
	base = nil,
	isActive = function() return not MissionFinished end,
	isFinished = function() return MissionFinished end,
	repairThreshold = 0.7,
	-- Orcas stay on local defence until the announced GDI attack; the
	-- `gdi-attack` wave enables their release.
	releaseOrcas = false,
	-- Do not pull AttackAnything units out of a running assault when a GDI
	-- building is damaged; the Defend home guard and ATWR handle the base.
	keepAttackers = true,
	allOut = false,
	canRepair = function(actor)
		return true
	end,
}

local GDI_IDLE_DEFENSE = {
	enemy = nil,
	players = nil,
	-- The original placed units are Guard/Area Guard, not hunters. Only the
	-- `hunt` trigger (half the buildings destroyed) makes them proactive.
	isActive = function() return not MissionFinished and GDIHunting end,
	isFinished = function() return MissionFinished end,
}

local function RegisterDamageResponse(actor)
	AI.RegisterDamageHandler(actor, GDI_AI)
end

-- Original GoodGuy credits: 90 * 100.
local StartingCash = 9000
-- Original `prod` trigger fires after 15 * 6 seconds (90 s).
local ProductionStart = { easy = 90, normal = 75, hard = 60 }
local ProductionInterval = { easy = 30, normal = 22, hard = 15 }
local MaxGDIArmy = { easy = 14, normal = 20, hard = 26 }
local UnitReserve = { easy = 300, normal = 400, hard = 500 }
local RebuildReserve = { easy = 500, normal = 700, hard = 900 }
local HomeGuardSize = { easy = 3, normal = 4, hard = 6 }

-- The original `auto` Autocreate trigger fires whenever something is
-- destroyed. The original timed `gdi1` attack appears at 250 * 6 s =
-- 25 minutes game time, but the port starts its timers after base discovery
-- and already has autocreate pressure. Use a shorter, readable delay.
local AutoWaveCooldown = { easy = 90, normal = 60, hard = 45 }
local GDI1Time = { easy = 720, normal = 600, hard = 480 }

-- After the base is discovered the placed army is committed through the
-- shared campaign-ai pool instead of sitting at home forever. The home guard
-- stays behind; all surplus units are fed into assaults and pressure waves.
local AssaultSize = { easy = 8, normal = 12, hard = 16 }
local PressureSquadSize = { easy = 3, normal = 5, hard = 7 }
local AlertFirstWave = { easy = 45, normal = 30, hard = 20 }
local AlertWaveInterval = { easy = 90, normal = 60, hard = 45 }

-- Original `xxxx` gives GDI the Ion Cannon after 200 * 6 s = 20 minutes.
-- `ion1`/`ion2` do the same when the marked tanks or the factory are attacked.
local IonCannonUnlockTime = { easy = 1200, normal = 1200, hard = 1200 }
local IonCannonFirstDelay = { easy = 180, normal = 150, hard = 120 }
local IonCannonInterval = { easy = 420, normal = 360, hard = 300 }

local UnitCosts = {
	e1 = 100, e2 = 160, e3 = 300,
	jeep = 400, mtnk = 900, apc = 600, msam = 900, htnk = 1800,
	orca = 1200, harv = 1100,
}

local UnitPrerequisites = {
	e1 = { "barracks" }, e2 = { "anyhq", "~techlevel.medium" }, e3 = { "barracks" },
	jeep = { "weap" }, mtnk = { "anyhq", "~techlevel.medium" },
	apc = { "pyle" }, msam = { "anyhq", "~techlevel.medium" },
	htnk = { "eye", "~techlevel.high" },
	harv = { "proc" },
	orca = { "hpad", "anyhq", "~techlevel.medium" },
}

-- The GDI base sits in the north-east. These routes lead from the base gate
-- towards the south-west, where the cut-off Nod base is.
SouthRoute = { waypoint13, waypoint0, waypoint2, waypoint4, waypoint21 }
CenterRoute = { waypoint3, waypoint17, waypoint11, waypoint12, waypoint4 }
-- Waypoint 6 (60,61) is a river tile; the shared orders filter it out anyway,
-- but the route reads better without it.
EastRoute = { waypoint5, waypoint2, waypoint4 }
local Routes = { SouthRoute, CenterRoute, EastRoute }

-- Commit the units closest to the Nod base first, so an assault does not start
-- with the furthest outpost garrison while nearer units idle at home.
local function PreferFront(unit)
	local target = NodFact and NodFact.Location or CPos.New(3, 52)
	if unit.Location == nil then
		return math.huge
	end
	local dx = unit.Location.X - target.X
	local dy = unit.Location.Y - target.Y
	return dx * dx + dy * dy
end

-- Original `gau`/`gad` autocreate teams, reduced to the compositions that
-- actually fit the map's routes.
local AutoWaves = {
	{ units = { "e1", "e1", "e2" }, route = CenterRoute, baseAttack = true },
	{ units = { "e2", "e2", "e3", "e3" }, route = SouthRoute, baseAttack = false },
	{ units = { "mtnk", "jeep" }, route = EastRoute, baseAttack = true },
	{ units = { "htnk" }, route = SouthRoute, baseAttack = false },
	{ units = { "e2", "mtnk", "msam" }, route = CenterRoute, baseAttack = true },
	{ units = { "e2", "e2", "e3" }, route = EastRoute, baseAttack = false },
}

-- Original [Base] economy/production layout. `eye` is deliberately absent:
-- the Ion Cannon is the mission's special threat and must not be rebuilt.
-- The 8 [Base] ATWRs are pre-placed in map.yaml; the seven [Base] GTWR nodes
-- are omitted because the map already has 18 GTWRs at its outposts.
-- Positions decoded from the packed values (`x = byte 1`, `y = byte 3`).
GDIRebuildPlan = {
	{ "fact", 59, 3 },
	{ "nuk2", 56, 3 }, { "nuk2", 48, 9 },
	-- The four [Base] power plants are pre-placed; keep them in the plan so
	-- destroyed ones are rebuilt.
	{ "nuk2", 48, 3 }, { "nuk2", 56, 6 },
	{ "nuk2", 51, 9 }, { "nuk2", 58, 19 }, { "nuk2", 53, 3 },
	{ "proc", 45, 8 }, { "proc", 58, 8 },
	{ "weap", 47, 5 },
	{ "pyle", 54, 8 },
	{ "silo", 45, 3 }, { "silo", 51, 3 }, { "silo", 45, 1 }, { "silo", 47, 1 },
	{ "hpad", 60, 6 }, { "hpad", 53, 4 },
}

-- Defensive towers are taken from the map. The 8 [Base] ATWRs are pre-placed
-- in map.yaml (the base had no close-in/anti-air towers), while the seven
-- [Base] GTWR nodes stay omitted because the map already has 18 GTWRs at its
-- outposts. Only towers that actually existed at mission start are rebuilt
-- when destroyed.
GDITowerPlan = {}

local GDI_SITE_OPTIONS = { owner = nil }
local GDI_ECONOMY_OPTIONS = {
	harvestersPerRefinery = 1,
	strainedFunds = 1500,
	crisisFunds = 500,
}

-- Shared rebuild/maintenance configuration. The plan supplies the economy and
-- production layout, targets keeps both refineries and helipads, and the
-- optional entries are the towers that actually existed on the map.
local GDI_REBUILD_OPTIONS = {
	plan = GDIRebuildPlan,
	targets = { proc = 2, weap = 1, pyle = 1, hpad = 2 },
	reserve = function() return RebuildReserve[Difficulty or "normal"] end,
	siteOptions = GDI_SITE_OPTIONS,
	isActive = function() return not MissionFinished and GDIHasBase() end,
	optionalEntries = function() return GDITowerPlan end,
	onComplete = function(building)
		RegisterDamageResponse(building)
		if building.Type == "gtwr" and PrepareTowerReveal ~= nil then
			PrepareTowerReveal(building)
		end
	end,
}

-- Land bounds shared by the region scan and the safe-entry search.
local GDI_LAND = { minX = 2, maxX = 61, minY = 2, maxY = 61 }

GDIProductionStarted = false
GDIAttackWavesStarted = false
GDIBaseStarted = false
GDIWaveLoopStarted = false
-- GDIIonCannonUnlocked is declared in deceit.lua; the mission script owns it.
IonCannonStrikesStarted = false
GDIHunting = false
EngineersDisabled = false
InitialGDIBuildings = 0
InitialGDIArmy = 0
AgentJeepSent = false
ChinSent = false
GDI2Sent = false
Atk2Sent = false
EchnSent = false
NextWaveIndex = 0
NextAutoWaveTime = 0
AutoWaveRegistered = {}
NextProduction = "vehicle"
-- Cached safe entry cell for the `gdi1` attack (waypoint 6 is a river tile).
GDI1Entry = nil

-- Global (the mission script uses it too).
IsLive = function(actor)
	return CampaignAI.IsLive(actor)
end

GDIHasBase = function()
	return IsLive(GDICYard) and GDICYard.Owner == GDI
end

LocalBuilding = function(kind)
	if GDIHome ~= nil then
		return AI.LocalBuilding(GDIHome, kind)
	end
	for _, actor in ipairs(GDI.GetActorsByType(kind)) do
		if IsLive(actor) and actor.IsInWorld and actor.Owner == GDI then
			return actor
		end
	end
	return nil
end

GetCombatUnits = function()
	return AI.GetCombatUnits(GDIHome, GDI_AI)
end

Queue = function(factory, kind, player)
	return AI.QueueUnit(factory, kind, player)
end

-- ---------------------------------------------------------------- setup

SetupGDI = function()
	GDI.Cash = StartingCash
	local difficulty = Difficulty or "normal"

	AI.ConfigureLand({
		minX = GDI_LAND.minX, maxX = GDI_LAND.maxX,
		minY = GDI_LAND.minY, maxY = GDI_LAND.maxY,
		startCells = {},
	})
	AI.BuildLandRegions()

	local yard = GDICYard
	if yard == nil then
		yard = { Location = CPos.New(59, 3) }
	end

	GDIHome = AI.NewBase({
		player = GDI,
		yard = { yard.Location.X, yard.Location.Y },
		defenderReserve = HomeGuardSize[difficulty],
	})
	AI.InitEconomy(GDIHome, {
		harvesterKind = "harv",
		refineryKind = "proc",
		harvesterProductionKind = "weap",
		incomeWindow = 180,
	})
	GDI_AI.enemy = Nod
	GDI_AI.players = { GDI }
	GDI_AI.base = GDIHome
	GDI_IDLE_DEFENSE.enemy = Nod
	GDI_IDLE_DEFENSE.players = { GDI }
	GDI_SITE_OPTIONS.owner = GDI
	AI.SeedPool(GDIHome, GDI_AI, { nearestFirst = true })
	-- The placed garrison is part of the original army; the production cap
	-- must allow replacements on top of it instead of blocking all production.
	InitialGDIArmy = #GetCombatUnits()

	for _, actor in ipairs(GDI.GetActors()) do
		-- Only the towers around the GDI base are rebuilt. The map also has
		-- old GTWRs near the Nod base and in the south-west; those are map
		-- defenses, not part of the [Base] that GDI maintains.
		if IsLive(actor) and (actor.Type == "gtwr" or actor.Type == "atwr")
			and actor.HasProperty("Location") and AI.IsLocal(GDIHome, actor, 42) then
			table.insert(GDITowerPlan, { actor.Type, actor.Location.X, actor.Location.Y })
		end
	end

	for _, actor in ipairs(GDI.GetActors()) do
		if IsLive(actor) and actor.HasProperty("StartBuildingRepairs") then
			InitialGDIBuildings = InitialGDIBuildings + 1
		end
	end

	Utils.Do(GDI.GetActors(), function(actor)
		if not IsLive(actor) or not actor.IsInWorld then
			return
		end

		if actor.HasProperty("FindResources") then
			AI.TrackHarvester(GDIHome, actor)
			if actor.HasProperty("Stance") then
				actor.Stance = "Defend"
			end
			actor.FindResources()
		elseif actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end

		RegisterDamageResponse(actor)
	end)
	Trigger.OnAnyProduction(OnGDIProduced)

	-- The GDI economy, production and attack schedule start only after a Nod
	-- unit has found the base (nod11a/nod11b pattern). GDI's additional [Base]
	-- NUK2s are pre-placed in the map, so the base already has enough power;
	-- until then the garrison defends itself but does not attack the Nod base.
	SetupAutoWaves()
	AI.StartIdleDefense(GDI_IDLE_DEFENSE, 3)
	AI.StartOrcaWatch({ GDIHome }, GDI_AI, 15)
end

StartGDIAfterBaseFound = function()
	if GDIBaseStarted or MissionFinished or not BaseTakenOver then
		return
	end

	GDIBaseStarted = true
	GDIAttackWavesStarted = true

	local difficulty = Difficulty or "normal"
	Trigger.AfterDelay(DateTime.Seconds(ProductionStart[difficulty]), StartGDIProduction)
	Trigger.AfterDelay(DateTime.Seconds(GDI1Time[difficulty]), SendGDI1)
	-- `zzzz`: the timed engineer drop. `delz` (destroying C8) cancels it, but
	-- must not cancel the `rtal`/`rtl2` reactions.
	Trigger.AfterDelay(DateTime.Seconds(1800), function()
		if not EngineersDisabled then
			SendEngineerTeam()
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonUnlockTime[difficulty]), UnlockIonCannon)
	Trigger.AfterDelay(DateTime.Seconds(20), MaintenanceTick)
	Trigger.AfterDelay(DateTime.Seconds(60), HuntTick)
end

OnGDIProduced = function(producer, unit, productionType)
	if MissionFinished or not IsLive(producer) or not IsLive(unit) or unit.Owner ~= GDI then
		return
	end

	RegisterDamageResponse(unit)

	if unit.Type == "harv" then
		AI.TrackHarvester(GDIHome, unit)
		if unit.HasProperty("Stance") then
			unit.Stance = "Defend"
		end
		unit.FindResources()
		return
	end

	if unit.Type == "orca" then
		-- Keep produced Orcas on local defence until the announced attack.
		AI.GuardOrca(GDIHome, unit, GDI_AI, GDIWaveLoopStarted)
		return
	end

	if unit.Type == "mcv" or not unit.HasProperty("AttackMove") then
		return
	end

	AI.AddToPool(GDIHome, unit, GDI_AI)
end

-- -------------------------------------------------------------- production

StartGDIProduction = function()
	if not BaseTakenOver or GDIProductionStarted or MissionFinished then
		return
	end

	GDIProductionStarted = true
	ProductionTick()
end

ProductionTick = function()
	if not BaseTakenOver or MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	AI.SafeCall("production", function()
		if #GetCombatUnits() < InitialGDIArmy + MaxGDIArmy[Difficulty or "normal"] then
			TryProduceUnit()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty or "normal"]), ProductionTick)
end

TryProduceUnit = function()
	local difficulty = Difficulty or "normal"
	local reserve = UnitReserve[difficulty]
	local economy = AI.ClassifyEconomy(GDIHome, GDI_ECONOMY_OPTIONS)
	if economy.state == "Crisis" or economy.state == "Liquidation" then
		return
	end

	local harvReserve = 0
	if economy.neededHarvesters > 0 and LocalBuilding("weap") ~= nil then
		harvReserve = UnitCosts.harv or 1100
	end
	local funds = AI.AvailableFunds(GDI, RebuildReserve[difficulty] + harvReserve)

	local pad = LocalBuilding("hpad")
	if difficulty ~= "easy" and pad ~= nil and #GDI.GetActorsByType("orca") < 2
		and funds >= (UnitCosts.orca or 1200) + reserve
		and GDI.HasPrerequisites(UnitPrerequisites.orca) then
		if Queue(pad, "orca", GDI) then
			return
		end
	end

	local vehiclePool = { "jeep", "mtnk", "apc", "msam" }
	if difficulty == "hard" then
		table.insert(vehiclePool, "htnk")
	end

	if NextProduction == "vehicle" then
		NextProduction = "infantry"
		if TryProduceVehicle(vehiclePool, reserve, funds) then
			return
		end
		TryProduceInfantry(reserve, funds)
	else
		NextProduction = "vehicle"
		if TryProduceInfantry(reserve, funds) then
			return
		end
		TryProduceVehicle(vehiclePool, reserve, funds)
	end
end

TryProduceVehicle = function(pool, reserve, funds)
	return AI.TryQueueFromPool(LocalBuilding("weap"), pool, GDI, {
		costs = UnitCosts,
		prerequisites = UnitPrerequisites,
		reserve = reserve,
		funds = funds,
		minFunds = 600,
	})
end

TryProduceInfantry = function(reserve, funds)
	return AI.TryQueueFromPool(LocalBuilding("pyle"), { "e1", "e2", "e3" }, GDI, {
		costs = UnitCosts,
		prerequisites = UnitPrerequisites,
		reserve = reserve,
		funds = funds,
		minFunds = 100,
		defaultCost = 100,
	})
end

-- --------------------------------------------------------------- economy

RestartIdleHarvesters = function()
	AI.MaintainHarvesters(GDIHome, GDI_ECONOMY_OPTIONS)
end

ReplaceHarvester = function()
	return AI.ReplaceHarvester(GDIHome, {
		harvesterCost = UnitCosts.harv,
		reserve = function() return UnitReserve[Difficulty or "normal"] end,
		canBuild = function(kind) return GDI.HasPrerequisites(UnitPrerequisites[kind] or {}) end,
	})
end

RebuildOneBuilding = function()
	AI.MaintainBase(GDIHome, GDI_REBUILD_OPTIONS)
end

MaintenanceTick = function()
	if MissionFinished or not BaseTakenOver or not GDIHasBase() then
		return
	end

	AI.SafeCall("maintenance", function()
		RestartIdleHarvesters()
		ReplaceHarvester()
		RebuildOneBuilding()
		-- Pull idle home-guard units back after they chased an attacker.
		AI.RecallReserve(GDIHome)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), MaintenanceTick)
end

-- ------------------------------------------------------------------ attack

-- Original `auto=Destroyed,Autocreate`: any destruction can create an
-- autocreate team. The cooldown approximates the original production delay
-- and prevents a single battle from spawning one team per corpse.
SetupAutoWaves = function()
	local function onKilled()
		if not BaseTakenOver or MissionFinished or not GDIHasBase() or not GDIWaveLoopStarted then
			return
		end

		local now = DateTime.GameTime
		if now < NextAutoWaveTime then
			return
		end

		NextAutoWaveTime = now + DateTime.Seconds(AutoWaveCooldown[Difficulty or "normal"])
		AI.SafeCall("auto-wave", function()
			SendAutoWave()
		end)
	end

	local function watch(actor)
		if not IsLive(actor) or AutoWaveRegistered[actor] then
			return
		end

		-- Only watch player-owned actors: Neutral crates and civilian
		-- buildings do not all carry ScriptTriggers.
		if actor.Owner ~= GDI and actor.Owner ~= Nod then
			return
		end

		local ok = pcall(Trigger.OnKilled, actor, onKilled)
		if ok then
			AutoWaveRegistered[actor] = true
		end
	end

	Utils.Do(GDI.GetActors(), watch)
	Utils.Do(Nod.GetActors(), watch)

	-- Units produced after the initial setup must be watched as well.
	Trigger.OnAnyProduction(function(_, unit)
		watch(unit)
	end)
end

-- Shared campaign-ai attack model: the home guard stays at the yard, all
-- surplus placed/produced units are committed through the pool. The original
-- Autocreate teams keep their shape; paid pool units fill them first and only
-- the missing part is spawned for free.
LaunchAssault = function()
	if MissionFinished or not GDIHasBase() then
		return
	end

	local units = AI.TakeForAttack(GDIHome, AssaultSize[Difficulty or "normal"], true, { prefer = PreferFront })
	if #units == 0 then
		return
	end

	AI.SendTeam(units, GDI, {}, true, true, GDI_AI)
end

WaveTick = function()
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	AI.SafeCall("waves", function()
		SendPressureSquad()
	end)

	Trigger.AfterDelay(DateTime.Seconds(AlertWaveInterval[Difficulty or "normal"]), WaveTick)
end

SendPressureSquad = function()
	local units = AI.TakeForAttack(GDIHome, PressureSquadSize[Difficulty or "normal"], true, { prefer = PreferFront })
	if #units == 0 then
		return
	end

	AI.SendTeam(units, GDI, Utils.Random(Routes), true, true, GDI_AI)
end

SendAutoWave = function()
	if not BaseTakenOver or MissionFinished or not GDIHasBase() then
		return
	end

	-- Do not start Autocreate pressure before the announced attack. Capturing
	-- the Chinook reveals the Nod base, but the first skirmish in the landing
	-- area must not pull tanks out of the base pool. GDI becomes aggressive
	-- with the `gdi-attack` message (SendGDI1 starts GDIWaveLoopStarted).
	if not GDIWaveLoopStarted then
		return
	end

	if SignalRelayActive() then
		NextAutoWaveTime = math.max(NextAutoWaveTime, SignalRelayUntil)
		return
	end

	local def = AutoWaves[(NextWaveIndex % #AutoWaves) + 1]
	NextWaveIndex = NextWaveIndex + 1

	local units = {}
	Utils.Do(def.units, function(unitType)
		table.insert(units, unitType)
	end)

	if Difficulty == "normal" then
		table.insert(units, "e1")
	elseif Difficulty == "hard" then
		table.insert(units, "e3")
		table.insert(units, "jeep")
	end

	-- Use paid pool units first, then create only the missing part of the
	-- original Autocreate team. This keeps the wave shape without making the
	-- offensive independent of the GDI economy.
	local paid = AI.TakeForAttack(GDIHome, #units, true, { prefer = PreferFront })
	if #paid > 0 then
		AI.SendTeam(paid, GDI, def.route or {}, true, def.baseAttack, GDI_AI)
	end

	local missing = #units - #paid
	if missing > 0 then
		local free = {}
		for i = 1, missing do
			free[i] = units[i]
		end
		SpawnTeam(free, def.route, def.baseAttack)
	end
end

SpawnTeam = function(units, route, baseAttack)
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	Reinforcements.Reinforce(GDI, units, { waypoint13.Location }, 12, function(unit)
		if not IsLive(unit) or MissionFinished then
			return
		end

		RegisterDamageResponse(unit)
		AI.SendTeam({ unit }, GDI, route or {}, true, baseAttack, GDI_AI)
	end)
end

-- Original `gdi1`: `gad2` (2 MTNK + 1 MSAM) enters at waypoint 6 and moves
-- via 13, 0, 2, 4 to waypoint 21 near the Nod base. The port uses 12/10/8
-- minutes after base discovery instead of the original 25 minutes.
--
-- Waypoint 6 (60,61) is a river tile on the imported map. Units spawned
-- there have no passable land region, so CA.SendTeam's SameLand filter drops
-- every route waypoint and the tanks idle in the bottom-right corner. Enter
-- at the nearest passable cell inside the land-region bounds from SetupGDI.
GDI1EntryCell = function()
	if GDI1Entry ~= nil then
		return GDI1Entry
	end

	local passable = {
		Clear = true, Road = true, Rough = true, Beach = true,
		Tiberium = true, BlueTiberium = true,
	}

	local target = waypoint6.Location
	if passable[Map.TerrainType(target)] then
		GDI1Entry = target
		return GDI1Entry
	end

	for radius = 1, 6 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				if math.abs(dx) == radius or math.abs(dy) == radius then
					local x, y = target.X + dx, target.Y + dy
					if x >= GDI_LAND.minX and x <= GDI_LAND.maxX
						and y >= GDI_LAND.minY and y <= GDI_LAND.maxY then
						local candidate = CPos.New(x, y)
						if passable[Map.TerrainType(candidate)] then
							GDI1Entry = candidate
							return GDI1Entry
						end
					end
				end
			end
		end
	end

	GDI1Entry = target
	return GDI1Entry
end

SendGDI1 = function()
	if not BaseTakenOver or MissionFinished or not GDIHasBase() then
		return
	end

	if SignalRelayActive() then
		Trigger.AfterDelay(SignalRelayUntil - DateTime.GameTime, SendGDI1)
		return
	end

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-attack"))

	-- The original `gdi1` creation is only three vehicles. Combine it with
	-- the standing army from the shared pool so the announced attack is real
	-- and the base stops hoarding units.
	if not GDIWaveLoopStarted then
		GDIWaveLoopStarted = true

		-- Now that the attack is announced, existing Orcas join the offensive
		-- and damage to GDI buildings may release them.
		GDI_AI.releaseOrcas = true
		for _, orca in ipairs(GDI.GetActorsByType("orca")) do
			if IsLive(orca) then
				AI.GuardOrca(GDIHome, orca, GDI_AI, true)
			end
		end

		AI.SafeCall("launch-assault", LaunchAssault)
		Trigger.AfterDelay(DateTime.Seconds(AlertFirstWave[Difficulty or "normal"]), WaveTick)
	end

	Reinforcements.Reinforce(GDI, { "mtnk", "mtnk", "msam" }, { GDI1EntryCell() }, 12, function(unit)
		if not IsLive(unit) or MissionFinished then
			return
		end

		RegisterDamageResponse(unit)
		AI.SendTeam({ unit }, GDI, { waypoint13, waypoint0, waypoint2, waypoint4, waypoint21 }, true, true, GDI_AI)
	end)
end

-- Original `hunt`: once half of the GDI buildings are gone, every surviving
-- GDI unit hunts Nod instead of holding its position.
HuntTick = function()
	if MissionFinished or not BaseTakenOver or GDIHunting or not GDIHasBase() then
		return
	end

	if SignalRelayActive() then
		Trigger.AfterDelay(SignalRelayUntil - DateTime.GameTime, HuntTick)
		return
	end

	local current = 0
	for _, actor in ipairs(GDI.GetActors()) do
		if IsLive(actor) and actor.HasProperty("StartBuildingRepairs") then
			current = current + 1
		end
	end

	if InitialGDIBuildings > 0 and current <= math.floor(InitialGDIBuildings * 0.5) then
		GDIHunting = true
		GDI_AI.releaseOrcas = true
		for _, unit in ipairs(GDI.GetGroundAttackers()) do
			if IsLive(unit) and unit.HasProperty("Stance") then
				unit.Stance = "AttackAnything"
				IdleHunt(unit)
			end
		end
		for _, orca in ipairs(GDI.GetActorsByType("orca")) do
			if IsLive(orca) then
				AI.GuardOrca(GDIHome, orca, GDI_AI, true)
				IdleHunt(orca)
			end
		end
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(30), HuntTick)
end

-- -------------------------------------------------------- scripted teams

-- GDI engineers are unarmed, so the original `atk1` "Attack Base" order means
-- walking to the Nod base and capturing the nearest capturable structure.
-- Pick the target when the engineer is actually in the world and retry on
-- idle, because the first target may be destroyed or captured en route.
CaptureNearestNodStructure = function(engineer)
	if MissionFinished or not IsLive(engineer) or not engineer.HasProperty("Capture")
		or not engineer.HasProperty("Location") then
		return
	end

	local nearest, distance = nil, nil
	for _, actor in ipairs(Map.ActorsInWorld) do
		if IsLive(actor) and actor.Owner == Nod and actor.HasProperty("Location")
			and actor.HasProperty("StartBuildingRepairs") and engineer.CanCapture(actor) then
			local location = actor.Location
			if location ~= nil then
				local dx, dy = location.X - engineer.Location.X, location.Y - engineer.Location.Y
				local d = dx * dx + dy * dy
				if distance == nil or d < distance then
					nearest, distance = actor, d
				end
			end
		end
	end

	if nearest ~= nil then
		engineer.Capture(nearest)
	end
end

ChinookTeam = function(units, entry, unload, attackBase, attackUnits)
	if MissionFinished or not GDIHasBase() then
		return
	end

	-- `entry` is either one waypoint or a list of waypoints. The original
	-- `atk1` enters at waypoint 5, moves via waypoint 2 and unloads at 7.
	local entryPath = {}
	if entry.Location ~= nil then
		table.insert(entryPath, entry.Location)
	else
		for _, point in ipairs(entry) do
			table.insert(entryPath, point.Location)
		end
	end
	table.insert(entryPath, unload.Location)

	-- Do not pass an actionFunc: ReinforceWithTransport then queues the
	-- aircraft Land + UnloadCargo activities. A custom action runs while the
	-- Chinook is still at flight altitude, which made it hover.
	local result = Reinforcements.ReinforceWithTransport(GDI, "tran", units,
		entryPath, { entryPath[1] }, nil, nil, TransportDropRange)
	local tran = result[1]
	if tran == nil then
		return
	end

	Trigger.OnPassengerExited(tran, function(_, passenger)
		-- OnPassengerExited fires from Cargo.Unload *before* the passenger is
		-- added to the world, so IsLive (which requires IsInWorld) is false
		-- here and must not gate the callback. Only a destroyed actor is
		-- skipped.
		if MissionFinished or passenger == nil or passenger.IsDead then
			return
		end

		RegisterDamageResponse(passenger)
		if passenger.HasProperty("Stance") then
			passenger.Stance = "AttackAnything"
		end

		-- OnPassengerExited fires before Cargo.Unload's frame-end task adds
		-- the passenger to the world. AI.SendTeam skips actors that are not
		-- in world, so defer every order by one tick. Engineers additionally
		-- need a capture order: AttackMove/Attack Base does nothing for them.
		if attackBase and passenger.Type == "e6" then
			Trigger.AfterDelay(0, function()
				if MissionFinished or not IsLive(passenger) then
					return
				end

				CaptureNearestNodStructure(passenger)
				Trigger.OnIdle(passenger, function()
					if not MissionFinished then
						CaptureNearestNodStructure(passenger)
					end
				end)
			end)
			return
		end

		Trigger.AfterDelay(0, function()
			if MissionFinished or not IsLive(passenger) then
				return
			end

			AI.SendTeam({ passenger }, GDI, {}, attackBase or attackUnits, attackBase, GDI_AI)
		end)
	end)
end

-- `supr`: original gdi4. 3x E2 + 2x E3 land at waypoint 8 (13,10), north-west
-- of the empty `echn` transport. This northern landing is original behavior.
SendGDI4 = function()
	ChinookTeam({ "e2", "e2", "e2", "e3", "e3" }, waypoint5, waypoint8, false, true)
end

-- `rchn`: original chin. 1x E1 + 4x E2 land at waypoint 2 (42,58) in the
-- southern village, only after the village patrol has been wiped out.
SendChin = function()
	if ChinSent then
		return
	end
	ChinSent = true
	ChinookTeam({ "e1", "e2", "e2", "e2", "e2" }, waypoint5, waypoint2, false, true)
end

SendGDI2 = function()
	if GDI2Sent then
		return
	end
	GDI2Sent = true
	ChinookTeam({ "e1", "e2", "e2", "e2", "e3" }, waypoint5, waypoint4, false, true)
end

SendEngineerTeam = function()
	if not BaseTakenOver then
		return
	end

	if SignalRelayActive() then
		Trigger.AfterDelay(SignalRelayUntil - DateTime.GameTime, SendEngineerTeam)
		return
	end

	ChinookTeam({ "e6", "e6", "e6" }, { waypoint5, waypoint2 }, waypoint7, true, false)
end

-- `grds`: original atk2. 5x E2 land at waypoint 17 (32,26), immediately north
-- of the empty `echn` transport at waypoint 3 (34,30). This is original.
-- The two nearby MTNKs were moved north in map.yaml so their 10-cell damage
-- response does not join the fight around the landing site.
SendAtk2 = function()
	if Atk2Sent then
		return
	end
	Atk2Sent = true
	ChinookTeam({ "e2", "e2", "e2", "e2", "e2" }, waypoint5, waypoint17, false, true)
end

-- `echn`: an unarmed GDI Chinook crosses the map when Nod enters the eastern
-- drop-zone cells. It is ambient in the original, so it stays unarmed here.
SendEchnChinook = function()
	if EchnSent or MissionFinished then
		return
	end
	EchnSent = true

	-- `echn`/`chn2`: the unarmed GDI Chinook flies to waypoint 3 and lands.
	-- `TRAN.CAPTURE` makes only this empty transport capturable by the Nod
	-- engineer, matching the original `chn2` landing.
	local result = Reinforcements.ReinforceWithTransport(GDI, "tran.capture", nil,
		{ waypoint1.Location, waypoint3.Location }, nil, nil, nil, TransportDropRange)
	local tran = result[1]
	if tran == nil then
		return
	end

	-- Do not gate this on IsLive: the actor may only enter the world later in
	-- the same frame, but the owner poll in Tick must still find it.
	EchnTransport = tran
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("chinook-spotted"))
	Trigger.OnCapture(tran, function(self)
		if IsNodPlayer(self.Owner) then
			SignalCapturedChinook()
		end
	end)
end

-- `nujp`: the agent jeep is created at waypoint 12 (the original team spawn
-- point), drives through the village and attacks the GDI base. It is Neutral,
-- matching `005=Neutral,JEEP` and the `nujp` team's house.
SendAgentJeep = function()
	if AgentJeepSent or MissionFinished then
		return
	end
	AgentJeepSent = true

	local jeep = Actor.Create("jeep", true, { Owner = Neutral, Location = waypoint12.Location })
	if not jeep then
		return
	end

	Utils.Do({ waypoint4, waypoint11, waypoint14, waypoint18, waypoint14, waypoint11,
		waypoint12, waypoint8, waypoint19, waypoint9, waypoint10 }, function(point)
		if IsLive(jeep) then
			jeep.Move(point.Location)
		end
	end)

	if IsLive(jeep) then
		-- Keep the neutral jeep non-hostile while it drives through the Nod
		-- base, then force the original `Attack Base` order on the GDI yard.
		if IsLive(GDICYard) then
			jeep.Attack(GDICYard, true, true)
		elseif jeep.HasProperty("AttackMove") then
			jeep.AttackMove(waypoint10.Location)
		end
	end
end

-- ---------------------------------------------------------------- ambush

SendAmbush = function()
	if MissionFinished or GDIAmbushSent or not GDIHasBase() then
		return
	end

	GDIAmbushSent = true
	Reinforcements.Reinforce(GDI, { "e2", "e2", "e2" }, { waypoint5.Location }, 10, function(unit)
		if not IsLive(unit) or MissionFinished then
			return
		end

		RegisterDamageResponse(unit)
		AI.SendTeam({ unit }, GDI, { waypoint5, waypoint0, waypoint2, waypoint4 }, false, false, GDI_AI)
	end)
end

-- ------------------------------------------------------------ ion cannon

UnlockIonCannon = function()
	if GDIIonCannonUnlocked or MissionFinished or not IsLive(GDIAdvComCenter)
		or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	GDIIonCannonUnlocked = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	if AddIonCannonObjective ~= nil then
		AddIonCannonObjective()
	end
	TryStartIonCannon()
end

TryStartIonCannon = function()
	if not GDIIonCannonUnlocked or IonCannonStrikesStarted or MissionFinished
		or not IsLive(GDIAdvComCenter) or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	IonCannonStrikesStarted = true
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[Difficulty or "normal"]), IonCannonStrike)
end

IonCannonStrike = function()
	if not GDIIonCannonUnlocked or MissionFinished or not IsLive(GDIAdvComCenter)
		or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	if GDI.PowerState ~= "Normal" then
		Trigger.AfterDelay(DateTime.Seconds(30), IonCannonStrike)
		return
	end

	local targets = Utils.Where(Nod.GetGroundAttackers(), function(actor)
		return IsLive(actor)
	end)

	if #targets == 0 then
		targets = Utils.Where(Nod.GetActors(), function(actor)
			return IsLive(actor) and actor.HasProperty("Location")
		end)
	end

	if #targets == 0 then
		Trigger.AfterDelay(DateTime.Seconds(20), IonCannonStrike)
		return
	end

	-- Never aim next to the EYE itself: the blast would destroy the building
	-- the objective is about.
	local eye = GDIAdvComCenter.Location
	targets = Utils.Where(targets, function(actor)
		local dx = actor.Location.X - eye.X
		local dy = actor.Location.Y - eye.Y
		return dx * dx + dy * dy > 100
	end)

	if #targets == 0 then
		Trigger.AfterDelay(DateTime.Seconds(20), IonCannonStrike)
		return
	end

	local target = targets[Utils.RandomInteger(1, #targets + 1)]
	GDIAdvComCenter.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[Difficulty or "normal"]), IonCannonStrike)
end

-- --------------------------------------------------------------- reports

GDIAIDebug = function()
	AI.PruneReserve(GDIHome)
	return {
		state = AI.ClassifyEconomy(GDIHome, GDI_ECONOMY_OPTIONS).state,
		reserveUnits = GDIHome.reserve,
		producedUnits = GDIHome.produced,
		defenderReserve = GDIHome.defenderReserve,
		ionCannon = GDIIonCannonUnlocked,
		waves = GDIAttackWavesStarted,
	}
end