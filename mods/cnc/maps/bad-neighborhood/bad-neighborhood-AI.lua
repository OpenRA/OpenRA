--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI economy, production and attacks for the Nod Covert Operations mission
-- "Bad Neighborhood" (SCB20EA.INI).
--
-- The original GDI house is passive until one of the detection trips fires
-- (the `yyyy`/`zzzz` Autocreate triggers). The port keeps that behaviour for
-- the opening: patrols and the garrison run from the start, but the attack
-- teams only begin once Nod has been discovered.
--
-- The economy follows the nod13c port instead of spawning units from thin
-- air: harvesters work the two nearby Tiberium fields, lost harvesters and
-- buildings are rebuilt, and combat units come out of the real PYLE/WEAP/
-- HPAD production queues. The only free units are the original scripted
-- teams (`grd1`/`grd2` patrols, `auto1`..`auto7` autocreate waves, `apc1`,
-- `gdi1` and `orca1`), which the original engine also created without cost.

-- Reusable helpers live in mods/cnc/scripts/campaign-ai.lua. The mission
-- keeps its alert gating, scripted teams and Ion Cannon, but uses the shared
-- garrison/attack pools, damage response and Orca lifecycle.
local AI = CampaignAI
local GDIHome
local GDI_AI = {
	enemy = nil,
	players = nil,
	base = nil,
	isActive = function() return not MissionFinished end,
	repairThreshold = 0.7,
	releaseOrcas = true,
	allOut = false,
	onDamage = function() AlertGDI() end,
	canRepair = function(actor)
		local economy = AI.ClassifyEconomy(GDIHome, GDI_ECONOMY_OPTIONS)
		if economy.state == "Crisis" or economy.state == "Liquidation" then
			return actor.Type == "nuk2" or actor.Type == "proc"
				or actor.Type == "weap" or actor.Type == "pyle" or actor.Type == "fact"
		end
		return true
	end,
}

-- The house is asleep until the first detection trip. Units still guard their
-- own position (Defend), but must not proactively hunt Nod, otherwise the
-- Mammoth north of the Nod start chases as soon as the player moves north.
local GDI_IDLE_DEFENSE = {
	enemy = nil,
	players = nil,
	isActive = function() return not MissionFinished and GDIAttackWavesStarted end,
}

-- Shared damage response for placed, produced and rebuilt GDI actors. Keeping
-- this in one place avoids the old bug where newly produced harvesters/units
-- had no local damage handler at all.
local function RegisterDamageResponse(actor)
	AI.RegisterDamageHandler(actor, GDI_AI)
end

-- Original GoodGuy credits: 20 * 100.
StartingCash = 2000
-- Original `prod` trigger fires after 30 seconds (5 x 6 s). The port keeps
-- the longer 3-5 minute delay on purpose for pacing; the harder difficulties
-- start slightly earlier so the base is awake sooner.
ProductionStart = { easy = 300, normal = 240, hard = 180 }
ProductionInterval = { easy = 30, normal = 22, hard = 15 }
MaxGDIArmy = { easy = 12, normal = 18, hard = 24 }
UnitReserve = { easy = 300, normal = 400, hard = 500 }
RebuildReserve = { easy = 500, normal = 700, hard = 900 }
-- Home guard kept at the construction yard; the rest of the paid army is
-- available for attack waves. PressureSquadSize is the number of surplus
-- units sent with each paid wave.
HomeGuardSize = { easy = 3, normal = 4, hard = 6 }
PressureSquadSize = { easy = 2, normal = 3, hard = 4 }

AlertFirstWave = { easy = 75, normal = 50, hard = 35 }
AlertWaveInterval = { easy = 210, normal = 150, hard = 100 }

-- Original [Triggers] timetable: apc1 at 25, gdi1/orc1 at 45 and apc2 at 50
-- minutes. That is far too long for a ported mission; the schedule starts
-- when GDI is alerted instead and is compressed as documented in the port.
AttackSchedule = {
	easy = { apc1 = 300, engineers = 360, orcas = 420, apc2 = 600 },
	normal = { apc1 = 240, engineers = 300, orcas = 330, apc2 = 480 },
	hard = { apc1 = 180, engineers = 240, orcas = 270, apc2 = 360 },
}

-- `xxxx` gives the Ion Cannon to GDI after 20 minutes (200 tenths of a
-- minute), `ion1` does the same when the marked area is entered. The times
-- are compressed as in the other ports.
IonCannonUnlockTime = { easy = 900, normal = 720, hard = 600 }
IonCannonFirstDelay = { easy = 150, normal = 120, hard = 90 }
IonCannonInterval = { easy = 360, normal = 300, hard = 240 }

UnitCosts = {
	e1 = 100, e2 = 160, e3 = 300,
	jeep = 400, mtnk = 900, apc = 600, msam = 900, htnk = 1800,
	orca = 1200, harv = 1100,
}

UnitPrerequisites = {
	e1 = { "barracks" }, e2 = { "anyhq", "~techlevel.medium" }, e3 = { "barracks" },
	jeep = { "weap" }, mtnk = { "anyhq", "~techlevel.medium" },
	apc = { "pyle" }, msam = { "anyhq", "~techlevel.medium" },
	htnk = { "eye", "~techlevel.high" },
	harv = { "proc" },
	orca = { "hpad", "anyhq", "~techlevel.medium" },
}

-- Route 4/5/8/9 runs from the south gate through the village, 4/5/16/6
-- heads west towards the Nod base and 10/11 stays in the north-west.
VillageRoute = { waypoint4, waypoint5, waypoint8, waypoint9 }
WestRoute = { waypoint4, waypoint5, waypoint16, waypoint6 }
ShortRoute = { waypoint4, waypoint5 }
NorthRoute = { waypoint10, waypoint11 }
Routes = { VillageRoute, WestRoute, ShortRoute, NorthRoute }

-- Original `auto1`..`auto7` attack teams, in their original order and with
-- their original waypoint routes.
AutoWaves = {
	{ units = { "e1", "e1", "e2", "e2" }, route = VillageRoute },
	{ units = { "e3", "e3", "mtnk" }, route = WestRoute },
	{ units = { "htnk" }, route = VillageRoute },
	{ units = { "htnk" }, route = WestRoute },
	{ units = { "e2", "e2", "msam" }, route = ShortRoute },
	{ units = { "e2", "e2", "e3", "e3" }, route = ShortRoute },
	{ units = { "mtnk", "jeep" }, route = NorthRoute },
}

-- Scripted GDI reinforcements enter at the mapped ReinforcementEntry
-- waypoint at the right-hand map edge below the base and drive in from
-- there, instead of popping up inside the base.

-- The shared site check knows the CNC footprints. The owner is set in
-- SetupGDI so friendly units that block a rebuild can be moved out of the
-- footprint instead of blocking it forever.
GDI_SITE_OPTIONS = { owner = nil }
GDI_ECONOMY_OPTIONS = {
	harvestersPerRefinery = 1,
	strainedFunds = 1500,
	crisisFunds = 500,
}

-- Original [Base] layout. `eye` is deliberately absent: the original `delx`
-- trigger disables the Ion Cannon permanently when the Advanced
-- Communications Center is destroyed, so GDI must not rebuild it.
GDIRebuildPlan = {
	{ "nuk2", 48, 12 }, { "nuk2", 49, 9 }, { "nuk2", 47, 9 }, { "nuk2", 45, 9 },
	{ "proc", 42, 11 }, { "proc", 45, 12 },
	{ "weap", 50, 14 }, { "pyle", 55, 14 },
	{ "silo", 59, 16 }, { "silo", 57, 16 }, { "silo", 43, 9 }, { "silo", 41, 9 },
	{ "hpad", 57, 13 }, { "hpad", 59, 13 },
	{ "atwr", 40, 17 }, { "atwr", 57, 18 }, { "atwr", 51, 9 }, { "atwr", 40, 10 }, { "atwr", 47, 18 },
	{ "gtwr", 38, 15 }, { "gtwr", 50, 19 }, { "gtwr", 38, 12 }, { "gtwr", 54, 19 },
}

GDIProductionStarted = false
GDIAttackWavesStarted = false
GDIIonCannonUnlocked = false
IonCannonStrikesStarted = false
IonCannonObjectiveAdded = false
NextWaveIndex = 0
NextProduction = "vehicle"
RebuildInProgress = false

-- Global (the mission script uses it too). Same semantics as the shared
-- helper: an actor must also be in the world, so cargo inside a transport is
-- not treated as a live unit.
IsLive = function(actor)
	return CampaignAI.IsLive(actor)
end

-- The construction yard is the heart of the GDI base. Once it falls the
-- garrison fights on, but no further production, rebuilds, attack waves or
-- off-map reinforcements are created: the original `All Destr.` win
-- condition only works out if the stream of new units actually stops.
GDIHasBase = function()
	return IsLive(GDICYard) and GDICYard.Owner == GDI
end

-- The named actor globals keep pointing at the original, possibly destroyed
-- building. Production and economy always resolve a live building instead.
LocalBuilding = function(kind)
	for _, actor in ipairs(GDI.GetActorsByType(kind)) do
		if IsLive(actor) and actor.IsInWorld and actor.Owner == GDI then
			return actor
		end
	end

	return nil
end

GDIFunds = function(player)
	return AI.Funds(player)
end

SpendFunds = function(player, amount)
	return AI.SpendFunds(player, amount)
end

RefundFunds = function(player, amount)
	AI.RefundFunds(player, amount)
end

CellsOf = function(waypoints)
	local cells = {}
	for i = 1, #waypoints do
		cells[i] = waypoints[i].Location
	end
	return cells
end

GetCombatUnits = function()
	return AI.GetCombatUnits(GDIHome, GDI_AI)
end

Queue = function(factory, kind, player)
	if not IsLive(factory) or not factory.IsInWorld or not factory.HasProperty("Build")
		or not factory.HasProperty("IsProducing") or factory.IsProducing(kind) then
		return false
	end

	if GDIFunds(player) < Actor.Cost(kind) then
		return false
	end

	return factory.Build({ kind })
end

-- ---------------------------------------------------------------- garrison

SetupGDI = function()
	GDI.Cash = StartingCash
	local difficulty = Difficulty or "normal"

	-- The whole map is one landmass, but the shared attack orders still use
	-- the region check so future Covert Ops can reuse the same helpers.
	AI.ConfigureLand({ minX = 2, maxX = 61, minY = 2, maxY = 61, startCells = {} })
	AI.BuildLandRegions()

	GDIHome = AI.NewBase({
		player = GDI,
		yard = { GDICYard.Location.X, GDICYard.Location.Y },
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
	AI.SeedPool(GDIHome, GDI_AI)

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
			-- Area Guard, as in the original: the unit defends its own
			-- position and returns fire, but does not proactively hunt until
			-- the house is alerted (see GDI_IDLE_DEFENSE).
			actor.Stance = "Defend"
		end

		RegisterDamageResponse(actor)
	end)
	Trigger.OnAnyProduction(OnGDIProduced)

	Trigger.AfterDelay(DateTime.Seconds(ProductionStart[difficulty]), StartGDIProduction)

	-- grd1: two E1 patrol waypoints 0/1. grd2: two E1 patrol waypoints 2/3.
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		SpawnPatrol({ "e1", "e1" }, { waypoint0, waypoint1 })
	end)
	Trigger.AfterDelay(DateTime.Seconds(18), function()
		SpawnPatrol({ "e1", "e1" }, { waypoint2, waypoint3 })
	end)

	Trigger.AfterDelay(DateTime.Seconds(IonCannonUnlockTime[difficulty]), UnlockIonCannon)
	Trigger.AfterDelay(DateTime.Seconds(20), MaintenanceTick)
	AI.StartIdleDefense(GDI_IDLE_DEFENSE, 3)
	AI.StartOrcaWatch({ GDIHome }, GDI_AI, 15)
end

SpawnPatrol = function(units, path)
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	Reinforcements.Reinforce(GDI, units, { path[1].Location }, 10, function(unit)
		if not IsLive(unit) then
			return
		end

		if unit.HasProperty("Stance") then
			-- Area Guard, like the placed garrison. IdleDefense (16-cell
			-- proactive hunt) stays gated until the alert.
			unit.Stance = "Defend"
		end
		unit.Patrol(CellsOf(path), true, DateTime.Seconds(20))
		RegisterDamageResponse(unit)
	end)
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
		-- Produced Orcas defend the base until the alert, then join the attack.
		AI.GuardOrca(GDIHome, unit, GDI_AI, GDIAttackWavesStarted)
		return
	end

	if unit.Type == "mcv" or not unit.HasProperty("AttackMove") then
		return
	end

	-- Do not park produced units with Guard: a Guard activity never ends, so
	-- later AttackMove orders would queue behind it forever. The shared pool
	-- keeps a local home guard and sends the surplus with the attack waves.
	AI.AddToPool(GDIHome, unit, GDI_AI)
end

-- -------------------------------------------------------------- production

StartGDIProduction = function()
	if GDIProductionStarted or MissionFinished then
		return
	end

	GDIProductionStarted = true
	ProductionTick()
end

ProductionTick = function()
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	AI.SafeCall("production", function()
		if #GetCombatUnits() < MaxGDIArmy[Difficulty or "normal"] then
			TryProduceUnit()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty or "normal"]), ProductionTick)
end

TryProduceUnit = function()
	local difficulty = Difficulty or "normal"
	local reserve = UnitReserve[difficulty]
	-- In Crisis/Liquidation the maintenance pass must rebuild the income
	-- chain first; do not spend combat money on new tanks.
	local economy = AI.ClassifyEconomy(GDIHome, GDI_ECONOMY_OPTIONS)
	if economy.state == "Crisis" or economy.state == "Liquidation" then
		return
	end
	-- Keep the rebuild reserve intact and, while a harvester is missing,
	-- reserve its replacement cost so combat production cannot consume it.
	local harvReserve = 0
	if economy.neededHarvesters > 0 and LocalBuilding("weap") ~= nil then
		harvReserve = UnitCosts.harv or 1100
	end
	local funds = AI.AvailableFunds(GDI, RebuildReserve[difficulty] + harvReserve)

	-- Normal and hard keep up to two Orcas as long as the helipads stand
	-- (the original AI builds Orcas from its helipads without a difficulty gate).
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

	-- Alternate between infantry and vehicles so neither factory starves.
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
	local factory = LocalBuilding("weap")
	if factory == nil or funds < 600 + reserve then
		return false
	end

	for _ = #pool, 1, -1 do
		local unitType = table.remove(pool, Utils.RandomInteger(1, #pool + 1))
		if funds >= (UnitCosts[unitType] or 800) + reserve
			and GDI.HasPrerequisites(UnitPrerequisites[unitType] or {})
			and Queue(factory, unitType, GDI) then
			return true
		end
	end

	return false
end

TryProduceInfantry = function(reserve, funds)
	local factory = LocalBuilding("pyle")
	if factory == nil or funds < 100 + reserve then
		return false
	end

	local pool = { "e1", "e2", "e3" }
	for _ = #pool, 1, -1 do
		local unitType = table.remove(pool, Utils.RandomInteger(1, #pool + 1))
		if funds >= (UnitCosts[unitType] or 100) + reserve
			and GDI.HasPrerequisites(UnitPrerequisites[unitType] or {})
			and Queue(factory, unitType, GDI) then
			return true
		end
	end

	return false
end

-- --------------------------------------------------------------- economy

RestartIdleHarvesters = function()
	if #GDI.GetActorsByType("proc") == 0 then
		return
	end

	-- Adopt FreeActor harvesters from rebuilt refineries before counting, then
	-- re-task idle collectors and update the income watchdog.
	AI.AdoptFreeHarvesters(GDIHome)
	AI.RefreshHarvesterClaims(GDIHome, GDI_ECONOMY_OPTIONS)
	AI.RestartIdleHarvesters(GDIHome)
	AI.UpdateIncomeWatchdog(GDIHome)
end

ReplaceHarvester = function()
	if AI.NeededHarvesters(GDIHome, GDI_ECONOMY_OPTIONS) <= 0 then
		return
	end

	local factory = LocalBuilding("weap")
	if factory == nil then
		return
	end

	if not GDI.HasPrerequisites(UnitPrerequisites.harv) then
		return
	end

	if factory.HasProperty("IsProducing") and factory.IsProducing("harv") then
		return
	end

	if GDIFunds(GDI) < UnitCosts.harv + UnitReserve[Difficulty or "normal"] then
		return
	end

	Queue(factory, "harv", GDI)
end

BuildingAt = function(kind, x, y)
	return AI.BuildingAt(kind, x, y) ~= nil
end

MissingBuilding = function(kind)
	for _, entry in ipairs(GDIRebuildPlan) do
		if entry[1] == kind and not BuildingAt(entry[1], entry[2], entry[3]) then
			return entry
		end
	end

	return nil
end

TryRebuildEntry = function(entry)
	if RebuildInProgress or not AI.SiteFree(entry, GDI_SITE_OPTIONS) then
		return false
	end

	local cost = Actor.Cost(entry[1])
	if GDIFunds(GDI) < cost + RebuildReserve[Difficulty or "normal"] then
		return false
	end

	if not SpendFunds(GDI, cost) then
		return false
	end
	RebuildInProgress = true

	local queue = (entry[1] == "gtwr" or entry[1] == "atwr" or entry[1] == "silo")
		and "Support.GDI" or "Building.GDI"
	local buildTime = Actor.BuildTime(entry[1], queue)

	Trigger.AfterDelay(buildTime, function()
		RebuildInProgress = false
		if MissionFinished or not GDIHasBase() or not AI.SiteFree(entry, GDI_SITE_OPTIONS) then
			RefundFunds(GDI, cost)
			return
		end

		local building = Actor.Create(entry[1], true, { Owner = GDI, Location = CPos.New(entry[2], entry[3]) })
		if not IsLive(building) then
			RefundFunds(GDI, cost)
			return
		end

		RegisterDamageResponse(building)
	end)

	return true
end

RebuildOneBuilding = function()
	if not GDIHasBase() then
		return
	end

	local difficulty = Difficulty or "normal"
	local power = GDI.PowerState
	local lowPower = power == "Low" or power == "Critical"
	local storageFull = AI.IsStorageNearlyFull(GDI, 500)

	-- 1. Keep the lights on, then the income and production chain.
	local order = {}
	if lowPower then
		for _, entry in ipairs(GDIRebuildPlan) do
			if entry[1] == "nuk2" then
				table.insert(order, entry)
			end
		end
	end

	-- Two refineries is the original target count; rebuild a missing one even
	-- while the other still runs.
	if #GDI.GetActorsByType("proc") < 2 then
		local entry = MissingBuilding("proc")
		if entry then
			table.insert(order, entry)
		end
	end

	for _, kind in ipairs({ "weap", "pyle" }) do
		if #GDI.GetActorsByType(kind) == 0 then
			local entry = MissingBuilding(kind)
			if entry then
				table.insert(order, entry)
			end
		end
	end

	for _, entry in ipairs(GDIRebuildPlan) do
		if (entry[1] == "nuk2" and not lowPower) or (storageFull and entry[1] == "silo") then
			table.insert(order, entry)
		end
	end

	-- The original base has two helipads and two refineries: rebuild both up to
	-- that target count, not only when the last one is gone.
	if #GDI.GetActorsByType("hpad") < 2 then
		local entry = MissingBuilding("hpad")
		if entry then
			table.insert(order, entry)
		end
	end

	-- 2. Optional structures only while the economy is healthy.
	if power == "Normal" and GDIFunds(GDI) >= 1500 then
		for _, entry in ipairs(GDIRebuildPlan) do
			if entry[1] == "silo" or entry[1] == "atwr" or entry[1] == "gtwr" then
				table.insert(order, entry)
			end
		end
	end

	local seen = {}
	for _, entry in ipairs(order) do
		local key = entry[1] .. entry[2] .. "," .. entry[3]
		if not seen[key] and not BuildingAt(entry[1], entry[2], entry[3]) and TryRebuildEntry(entry) then
			return
		end
		seen[key] = true
	end

	return
end

MaintenanceTick = function()
	if MissionFinished or not GDIHasBase() then
		return
	end

	AI.SafeCall("maintenance", function()
		RestartIdleHarvesters()
		ReplaceHarvester()
		RebuildOneBuilding()
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), MaintenanceTick)
end

-- ---------------------------------------------------------------- defense

-- Defence is handled by CampaignAI.DefendAgainst. The mission keeps
-- AlertGDI as the discovery/alert hook; the shared helper performs repairs,
-- local responses, Orca release and optional all-out handling.

-- ------------------------------------------------------------------ attack

AlertGDI = function()
	if GDIAttackWavesStarted or MissionFinished then
		return
	end

	GDIAttackWavesStarted = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-alerted"))

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		AI.SafeCall("launch-assault", LaunchAssault)
	end)
	Trigger.AfterDelay(DateTime.Seconds(AlertFirstWave[Difficulty or "normal"]), WaveTick)
	ScheduleScriptedReinforcements()
	TryStartIonCannon()
end

-- Once the house is alerted, the standing army commits, but the shared
-- pool keeps a home guard at the yard. Every unit attacks its closest Nod
-- building; the shared WatchAttackers loop re-issues orders after a route
-- finishes.
LaunchAssault = function()
	if MissionFinished or not GDIHasBase() then
		return
	end

	local team = AI.TakeForAttack(GDIHome, math.huge, true)
	if #team == 0 then
		return
	end

	AI.SendTeam(team, GDI, {}, true, true, GDI_AI)
end

WaveTick = function()
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	AI.SafeCall("waves", function()
		SendAutoWave()
		SendPressureSquad()
	end)

	Trigger.AfterDelay(DateTime.Seconds(AlertWaveInterval[Difficulty or "normal"]), WaveTick)
end

SendAutoWave = function()
	local def = AutoWaves[(NextWaveIndex % #AutoWaves) + 1]
	NextWaveIndex = NextWaveIndex + 1

	local units = {}
	Utils.Do(def.units, function(unitType)
		table.insert(units, unitType)
	end)

	-- The original Autocreate waves keep coming; scale them a little with
	-- difficulty so Normal/Hard are not just the Easy trickle.
	if Difficulty == "normal" then
		table.insert(units, "e1")
	elseif Difficulty == "hard" then
		table.insert(units, "e3")
		table.insert(units, "jeep")
	end

	-- Use paid units from the pool first; only create the missing part of the
	-- original Autocreate team for free. That keeps the original wave shape
	-- without making the whole offensive independent of the GDI economy.
	local paid = AI.TakeForAttack(GDIHome, #units, true)
	if #paid > 0 then
		AI.SendTeam(paid, GDI, def.route, true, true, GDI_AI)
	end

	local missing = #units - #paid
	if missing > 0 then
		local free = {}
		for i = 1, missing do
			free[i] = units[i]
		end
		SpawnTeam(free, def.route)
	end
end

-- Surplus paid units are pushed out in a squad every wave interval. The
-- home guard stays at the yard; the free original Autocreate teams keep
-- their own scripted spawn path.
SendPressureSquad = function()
	local units = AI.TakeForAttack(GDIHome, PressureSquadSize[Difficulty or "normal"], true)
	if #units == 0 then
		return
	end

	AI.SendTeam(units, GDI, Utils.Random(Routes), true, true, GDI_AI)
end

-- Spawns one of the original scripted Autocreate teams.
SpawnTeam = function(units, route)
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	Reinforcements.Reinforce(GDI, units, { ReinforcementEntry.Location, waypoint4.Location }, 10, function(unit)
		if not IsLive(unit) then
			return
		end

		if unit.HasProperty("Stance") then
			unit.Stance = "AttackAnything"
		end
		RegisterDamageResponse(unit)
		Utils.Do(CellsOf(route), function(cell)
			unit.AttackMove(cell)
		end)
		IdleHunt(unit)
	end)
end

ScheduleScriptedReinforcements = function()
	local schedule = AttackSchedule[Difficulty or "normal"]

	Trigger.AfterDelay(DateTime.Seconds(schedule.apc1), SendAPC1)
	Trigger.AfterDelay(DateTime.Seconds(schedule.engineers), SendEngineers)
	-- orca1 is a single scripted flight (original: once at 45 min). The second
	-- flight is orc2, triggered by Orc2Cells.
	Trigger.AfterDelay(DateTime.Seconds(schedule.orcas), SendOrcas)
	Trigger.AfterDelay(DateTime.Seconds(schedule.apc2), SendAPC2)
end

-- apc1: three engineers and two commandos in an APC. The APC enters from
-- the right-hand map edge below the base and then follows the original
-- 4/5/16/6 route to waypoint 7 where it unloads.
SendAPC1 = function()
	SendAPC({ "e6", "e6", "e6", "rmbo", "rmbo" },
		{ ReinforcementEntry, waypoint3, waypoint4, waypoint5, waypoint16, waypoint6, waypoint7 })
end

SendAPC2 = function()
	SendAPC({ "e6", "e6", "e6", "rmbo", "rmbo" },
		{ ReinforcementEntry, waypoint3, waypoint4, waypoint5, waypoint16, waypoint6, waypoint7 })
end

SendAPC = function(units, path)
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	local cells = CellsOf(path)

	-- The custom action queues UnloadPassengers before the exitPath, so the
	-- APC unloads first and then leaves the map (no empty transport left
	-- behind to block the All-Destroyed check).
	Reinforcements.ReinforceWithTransport(GDI, "apc", units, cells, { waypoint17.Location }, function(apc, cargo)
		if apc.HasProperty("UnloadPassengers") then
			apc.UnloadPassengers()
		end

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Utils.Do(cargo, function(unit)
				if not IsLive(unit) then
					return
				end

				if unit.HasProperty("Stance") then
					unit.Stance = "AttackAnything"
				end

				if unit.HasProperty("AttackMove") then
					unit.AttackMove(path[#path].Location)
					IdleHunt(unit)
				elseif unit.HasProperty("Capture") then
					-- Engineers cannot attack-move; let them sabotage the
					-- closest Nod structure instead of idling.
					unit.Move(path[#path].Location)
					Trigger.OnIdle(unit, function()
						CaptureNodStructure(unit)
					end)
				end
			end)
		end)
	end)
end

-- GDI engineers are unarmed. Send them towards the Nod base and let them
-- sabotage the nearest Nod structure when they arrive.
CaptureNodStructure = function(engineer)
	if not IsLive(engineer) or not engineer.HasProperty("Capture") then
		return
	end

	local nearest, distance = nil, 1000000
	Utils.Do(Nod.GetActors(), function(actor)
		if not IsLive(actor) or actor.Owner ~= Nod or not actor.HasProperty("StartBuildingRepairs") then
			return
		end

		local dx = actor.Location.X - engineer.Location.X
		local dy = actor.Location.Y - engineer.Location.Y
		local d = math.abs(dx) + math.abs(dy)
		if d < distance and engineer.CanCapture(actor) then
			distance = d
			nearest = actor
		end
	end)

	if nearest then
		engineer.Capture(nearest)
	end
end

-- gdi1: five engineers in a Chinook that unloads near the GDI base and
-- then attacks the Nod base.
SendEngineers = function()
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	-- Same as the APC: unload first, then the Chinook leaves via exitPath.
	-- Original gdi1: Move:0, Move:1, Unload:1, Move:2, then Attack Base.
	Reinforcements.ReinforceWithTransport(GDI, "tran", { "e6", "e6", "e6", "e6", "e6" },
		{ ReinforcementEntry.Location, waypoint0.Location, waypoint1.Location },
		{ waypoint2.Location },
		function(tran, cargo)
			if MissionFinished then
				return
			end

			if tran.HasProperty("UnloadPassengers") then
				tran.UnloadPassengers()
			end

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				if MissionFinished then
					return
				end

				Utils.Do(cargo, function(unit)
					if not IsLive(unit) then
						return
					end

					if unit.HasProperty("Stance") then
						unit.Stance = "AttackAnything"
					end

					-- Engineers head straight for the Nod base (original
					-- "Attack Base") instead of a long detour loop.
					if unit.HasProperty("Capture") then
						unit.Move(waypoint14.Location)
						Trigger.OnIdle(unit, function()
							if not MissionFinished then
								CaptureNodStructure(unit)
							end
						end)
					end
				end)
			end)
		end)
end

-- orc1/orc2: two Orcas.
SendOrcas = function()
	if MissionFinished or GDI.HasNoRequiredUnits() or not GDIHasBase() then
		return
	end

	Reinforcements.Reinforce(GDI, { "orca", "orca" }, { ReinforcementEntry.Location }, 25, function(unit)
		if not IsLive(unit) or MissionFinished then
			return
		end

		AI.GuardOrca(GDIHome, unit, GDI_AI, true)
		RegisterDamageResponse(unit)
		Utils.Do({ waypoint3, waypoint4, waypoint5, waypoint8, waypoint9 }, function(point)
			unit.AttackMove(point.Location)
		end)
		IdleHunt(unit)
	end)
end

-- ------------------------------------------------------------- ion cannon

UnlockIonCannon = function()
	if GDIIonCannonUnlocked or MissionFinished or not IsLive(GDIAdvComCenter)
		or GDIAdvComCenter.Owner ~= GDI then
		return
	end

	GDIIonCannonUnlocked = true
	Media.PlaySpeechNotification(Nod, "IonCannonCharging")
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))

	if not IonCannonObjectiveAdded then
		IonCannonObjectiveAdded = true
		StopIonCannon = AddSecondaryObjective(Nod, "destroy-gdi-ion-cannon")
	end

	-- delx: the original removes the Ion Cannon trigger when the Advanced
	-- Communications Center is destroyed or captured.
	Trigger.OnKilled(GDIAdvComCenter, function()
		if MissionFinished then
			return
		end

		GDIIonCannonUnlocked = false
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-destroyed"))

		if StopIonCannon and not Nod.IsObjectiveCompleted(StopIonCannon) and not Nod.IsObjectiveFailed(StopIonCannon) then
			Nod.MarkCompletedObjective(StopIonCannon)
		end
	end)

	Trigger.OnCapture(GDIAdvComCenter, function(_, _, _, newOwner)
		if newOwner == GDI or MissionFinished then
			return
		end

		GDIIonCannonUnlocked = false
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-destroyed"))

		if StopIonCannon and not Nod.IsObjectiveCompleted(StopIonCannon) and not Nod.IsObjectiveFailed(StopIonCannon) then
			Nod.MarkCompletedObjective(StopIonCannon)
		end
	end)

	TryStartIonCannon()
end

TryStartIonCannon = function()
	if not GDIIonCannonUnlocked or not GDIAttackWavesStarted or IonCannonStrikesStarted
		or MissionFinished or not IsLive(GDIAdvComCenter) or GDIAdvComCenter.Owner ~= GDI then
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

	-- Never aim next to the Advanced Communications Center itself: the blast
	-- would destroy the building the objective is about.
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

	local target = Utils.Random(targets)
	GDIAdvComCenter.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[Difficulty or "normal"]), IonCannonStrike)
end

-- Harness/diagnostic hook. Mirrors nod13c's EconomyDebug: reports the shared
-- pool state without exposing the internal base table to gameplay code.
function GDIAIDebug()
	if GDIHome == nil then
		return { reserve = 0, produced = 0, defenderReserve = 0, allOut = false }
	end
	local economy = AI.ClassifyEconomy(GDIHome, GDI_ECONOMY_OPTIONS)
	return {
		reserve = #GDIHome.reserve,
		produced = #GDIHome.produced,
		defenderReserve = GDIHome.defenderReserve,
		allOut = GDIHome.allOut == true,
		defending = GDIHome.defending == true,
		harvesters = economy.harvesters,
		neededHarvesters = economy.neededHarvesters,
		economyState = economy.state,
		incomeSuspect = GDIHome.incomeSuspect == true,
		reserveUnits = GDIHome.reserve,
		producedUnits = GDIHome.produced,
	}
end