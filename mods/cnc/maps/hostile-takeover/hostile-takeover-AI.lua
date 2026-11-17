--[[
   Hostile Takeover (SCB33EA) - GDI AI.

   The original GDI house is a garrison with a complete north-eastern base.
   It has no Communications Center, so it cannot build Grenadiers, Medium
   Tanks, Rocket Soldiers or Mammoth Tanks; those only exist as the starting
   garrison and as the fixed scripted teams. `prod` starts base production
   (including the missing Weapons Factory), and `auto` starts the original
   Autocreate teams. All teams are formed from existing GDI units, never
   spawned from thin air, and every route is the original waypoint list.
   No randomness is used anywhere.
]]

local AI = CampaignAI

GDIProductionStarted = false
GDIAIAlerted = false
IdleDefenseStarted = false
GIOffensiveContext = nil
GIBaseDefenseContext = nil
GIUnitContext = nil

-- Active scripted teams. A key can only have one live team at a time, which
-- mirrors `Max Allowed` for the Autocreate types and prevents two triggers
-- from recruiting the same unit.
local ActiveTeams = {}
local TriggerDone = {}
local RegisteredBuildings = {}
local RegisteredGDIUnits = {}

local WP = {
	CPos.New(12, 51), CPos.New(16, 51), CPos.New(14, 53), CPos.New(49, 52),
	CPos.New(40, 25), CPos.New(39, 41), CPos.New(55, 16), CPos.New(55, 7),
	CPos.New(45, 2), CPos.New(32, 6), CPos.New(9, 2), CPos.New(23, 12),
	CPos.New(31, 25), CPos.New(36, 15), CPos.New(34, 34), CPos.New(19, 39),
	CPos.New(11, 33), CPos.New(4, 40), CPos.New(42, 53), CPos.New(35, 44),
}

local GDI_ENTRY = CPos.New(4, 61)
local CHN4_ENTRY = CPos.New(49, 61)
-- East of the three Chinooks in the cyclone pen, on the clear strip
-- between the pen towers and the river. The LST waits in the river at
-- (22,51), unloads at (21,51) and the Jeep drives to (20,51).
local HOV_ENTRY = CPos.New(22, 51)
local HOV_LANDING = CPos.New(21, 51)
local HOV_RALLY = CPos.New(20, 51)

-- [Base] plan in OpenRA map coordinates. ATWR and WEAP were imported one
-- cell lower than the packed original coordinate. The two extra NUK2 from
-- [Base] are not in this list: the original AI builds power only when the
-- base is short, and the three placed reactors plus these replacement sites
-- already cover that need.
local GDIRebuildPlan = {
	{ "fact", 59, 22 }, { "proc", 56, 22 },
	{ "pyle", 55, 27 }, { "weap", 58, 18 },
	-- Alternate sites for the two production buildings. If Nod captures the
	-- original PYLE/WEAP, the first plan node is occupied by a foreign actor
	-- and the base rebuilds the lost line at these nearby free cells.
	{ "pyle", 55, 24 }, { "weap", 55, 19 },
	{ "hpad", 53, 21 }, { "hpad", 53, 23 },
	{ "silo", 52, 19 }, { "silo", 50, 17 }, { "silo", 50, 19 }, { "silo", 52, 17 },
	{ "nuk2", 57, 26 }, { "nuk2", 59, 27 }, { "nuk2", 60, 25 },
	{ "atwr", 59, 16 }, { "atwr", 52, 16 }, { "atwr", 48, 28 }, { "atwr", 47, 30 },
	{ "atwr", 50, 22 }, { "atwr", 51, 24 },
	{ "gtwr", 49, 25 }, { "gtwr", 57, 14 }, { "gtwr", 54, 14 }, { "gtwr", 46, 27 },
}

local GDI_TowerEntries = {}
local GDI_REBUILD_OPTIONS = nil
-- One refinery with one harvester, matching the original GDI base. The
-- income chain is maintained best-effort: a missing harvester must never
-- block combat production or an attack (that stalled the whole AI when the
-- second refinery/harvester could not be afforded).
local GDI_ECONOMY_OPTIONS = {
	harvestersPerRefinery = 1,
	strainedFunds = 1500,
	crisisFunds = 500,
}
local AssaultIndex = 0
local PressureIndex = 0

local AutoDefs = {
	auto7 = { units = { { "htnk", 1 } }, route = { WP[5], WP[15], WP[16], WP[17] }, attack = true, baseAttack = true },
	auto6 = { units = { { "mtnk", 1 }, { "jeep", 1 } }, route = { WP[5], WP[15], WP[16], WP[17] }, attack = true, baseAttack = false },
	auto5 = { units = { { "htnk", 1 } }, route = { WP[5], WP[13], WP[14] }, attack = false, baseAttack = false },
	auto4 = { units = { { "e2", 3 }, { "e6", 2 } }, route = { WP[5], WP[13] }, attack = true, baseAttack = true },
	auto3 = { units = { { "e2", 3 }, { "e3", 3 } }, route = { WP[5], WP[13] }, attack = true, baseAttack = false },
	auto2 = { units = { { "msam", 2 } }, route = { WP[7], WP[8], WP[9], WP[10], WP[11] }, attack = true, baseAttack = true },
	auto1 = { units = { { "mtnk", 1 }, { "jeep", 2 } }, route = { WP[7], WP[8], WP[9], WP[10], WP[12] }, attack = true, baseAttack = false },
}

-- RecruitPriority order from the original team types: 20, 15, 7, 7, 7, 7, 7.
local AutoOrder = { "auto7", "auto6", "auto5", "auto4", "auto3", "auto2", "auto1" }
local AutoIndex = 1

local function IsGDIUnit(actor)
	return IsLive(actor) and actor.Owner == GDI and actor.HasProperty("Location")
end

-- Units use the shared self-defence context from CampaignAI: a damaged
-- unit shoots back at its attacker instead of being outranged forever.
function RegisterGDIUnit(actor)
	if GIUnitContext == nil or not IsGDIUnit(actor) then return end
	local key = AI.ActorKey(actor)
	if RegisteredGDIUnits[key] then return end
	RegisteredGDIUnits[key] = true
	AI.RegisterUnit(actor, GIUnitContext)
end

local function GDIHasBase()
	return IsLive(GDICYard) and GDICYard.Owner == GDI
end

local function DistanceSquared(a, b)
	local dx = a.X - b.X
	local dy = a.Y - b.Y
	return dx * dx + dy * dy
end

local function PruneActiveTeams()
	for key, team in pairs(ActiveTeams) do
		local alive = {}
		for _, unit in ipairs(team) do
			if IsGDIUnit(unit) then
				table.insert(alive, unit)
			end
		end

		if #alive == 0 then
			ActiveTeams[key] = nil
		else
			ActiveTeams[key] = alive
		end
	end
end

local function ReservedKeys()
	local keys = {}
	for _, team in pairs(ActiveTeams) do
		for _, unit in ipairs(team) do
			if IsLive(unit) then
				keys[AI.ActorKey(unit)] = true
			end
		end
	end
	return keys
end

-- Deterministic recruitment: choose the nearest free units to the first
-- waypoint, breaking ties by ActorID. The original TeamClass may act with
-- an understrength team (IsReinforcable=0), so take up to the requested
-- count instead of failing when one unit is missing. Returns nil only when
-- no matching unit is available at all.
local function FindGDIUnits(classes, routeStart)
	local reserved = ReservedKeys()
	local selected = {}

	for _, class in ipairs(classes) do
		local kind, count = class[1], class[2]
		local candidates = {}
		for _, actor in ipairs(AI.GetActorsByType(GDI, kind)) do
			if IsGDIUnit(actor) and not reserved[AI.ActorKey(actor)] then
				table.insert(candidates, actor)
			end
		end

		table.sort(candidates, function(a, b)
			if routeStart ~= nil then
				local da = DistanceSquared(a.Location, routeStart)
				local db = DistanceSquared(b.Location, routeStart)
				if da ~= db then
					return da < db
				end
			end
			return a.ActorID < b.ActorID
		end)

		local take = math.min(#candidates, count)
		for i = 1, take do
			table.insert(selected, candidates[i])
			reserved[AI.ActorKey(candidates[i])] = true
		end
	end

	if #selected == 0 then
		return nil
	end

	return selected
end

local function DispatchTeam(key, units, route, attack, baseAttack, context)
	if #units == 0 then
		return false
	end

	ActiveTeams[key] = units
	AI.SendTeam(units, GDI, route, attack, baseAttack, context or GIContext)
	return true
end

local function TryCreateTeam(key, classes, route, attack, baseAttack, context)
	if MissionFinished or TriggerDone[key] then
		return false
	end

	PruneActiveTeams()
	if ActiveTeams[key] ~= nil then
		return false
	end

	local start = route and route[1] or nil
	local units = FindGDIUnits(classes, start)
	if units == nil then
		return false
	end

	return DispatchTeam(key, units, route or {}, attack, baseAttack, context)
end

-- Engineers are consumed by the capture, so every engineer in a drop needs
-- its own building. CA.CaptureWithEngineers assigns one claim per engineer
-- and retries with the next free building.
local CaptureClaims = {}

function OrderEngineerCapture(unit)
	AI.CaptureWithEngineers({ unit }, Nod, {
		claims = CaptureClaims,
		context = GIContext,
		retrySeconds = 10,
		onNoTarget = function(engineer)
			AI.OrderAttackBase(engineer, GIContext)
		end,
	})
end

local function OrderDropUnit(unit)
	if MissionFinished or not IsLive(unit) then
		return
	end

	if unit.HasProperty("Capture") and unit.HasProperty("CanCapture") then
		OrderEngineerCapture(unit)
		return
	end

	AI.OrderAttackBase(unit, GIContext)
end

local function TransportDrop(units, entry, landing, onPassenger)
	local result = Reinforcements.ReinforceWithTransport(GDI, "tran", units,
		{ entry, landing }, { entry }, nil, nil, 3)
	local tran = result and result[1]
	if tran == nil then
		return nil
	end

	Trigger.OnPassengerExited(tran, function(_, passenger)
		-- The callback fires before the passenger is added to the world;
		-- only a destroyed actor is skipped here.
		if MissionFinished or passenger == nil or passenger.IsDead then
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			if MissionFinished or not IsLive(passenger) then
				return
			end
			onPassenger(passenger)
			RegisterGDIUnit(passenger)
		end)
	end)

	return tran
end

local function EngineerDrop(units)
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("engineer-drop"))
	TransportDrop(units, GDI_ENTRY, WP[18], OrderDropUnit)
end

local function SpawnCapturableChinook(entry, landing)
	local result = Reinforcements.ReinforceWithTransport(GDI, "tran.capture", nil,
		{ entry, landing }, nil, nil, nil, 3)
	local tran = result and result[1]
	if tran == nil then
		return
	end

	if tran.HasProperty("Stance") then
		tran.Stance = "Defend"
	end

	table.insert(GDIChinooks, tran)
	Trigger.OnCapture(tran, function(self)
		if self.Owner == Nod then
			OnTransportCaptured()
		end
	end)
end

-- Deceit-style front preference: units closest to the old Nod base go first.
local function PreferFront(unit)
	local target = CPos.New(7, 26)
	if NodFact ~= nil and IsLive(NodFact) and NodFact.HasProperty("Location") then
		target = NodFact.Location
	elseif NodProc ~= nil and IsLive(NodProc) and NodProc.HasProperty("Location") then
		target = NodProc.Location
	end
	if unit.Location == nil then return math.huge end
	return DistanceSquared(unit.Location, target)
end

-- Surplus units for a general assault: free combat units that are not
-- already reserved for an original scripted/Autocreate team. The base keeps
-- its `defenderReserve` home guard, matching CA.TakeForAttack(keepReserve).
-- Deterministic order (Deceit's PreferFront), no randomness.
local function FindSurplusUnits(limit)
	local reserved = ReservedKeys()
	local candidates = {}
	for _, unit in ipairs(GDI.GetGroundAttackers()) do
		if IsGDIUnit(unit) and unit.HasProperty("AttackMove")
			and unit.Type ~= "harv" and unit.Type ~= "mcv"
			and unit.Type ~= "tran" and unit.Type ~= "tran.capture"
			and not reserved[AI.ActorKey(unit)] then
			table.insert(candidates, unit)
		end
	end

	table.sort(candidates, function(a, b)
		local da, db = PreferFront(a), PreferFront(b)
		if da ~= db then return da < db end
		return a.ActorID < b.ActorID
	end)

	local function IsHome(unit)
		return AI.IsLocal(GDIHome, unit, GDIHome.baseRadius or 24)
	end

	-- Count the home guard before taking anything. Units already outside the
	-- base may always leave; only the local garrison is limited.
	local homeGuard = GDIHome.defenderReserve or 4
	local homeCount = 0
	for _, unit in ipairs(candidates) do
		if IsHome(unit) then homeCount = homeCount + 1 end
	end

	local selected = {}
	for _, unit in ipairs(candidates) do
		if #selected >= limit then break end
		if not IsHome(unit) then
			table.insert(selected, unit)
		elseif homeCount > homeGuard then
			table.insert(selected, unit)
			homeCount = homeCount - 1
		end
	end
	return selected
end

-- One announced counter-attack after `auto`, capped like Deceit's
-- AssaultSize so the whole garrison is not thrown away at once.
function LaunchAssault()
	if MissionFinished or not GDIHasBase() then return end
	local units = FindSurplusUnits(CurrentDifficulty().assaultSize)
	if #units == 0 then return end
	AssaultIndex = AssaultIndex + 1
	DispatchTeam("assault" .. AssaultIndex, units, {}, true, true, GIOffensiveContext)
end

-- Regular pressure squad from surplus units (Deceit/Bad Neighborhood). The
-- home guard is not touched because only unreserved surplus is taken.
function SendPressureSquad()
	if MissionFinished or not GDIHasBase() then return end
	local units = FindSurplusUnits(CurrentDifficulty().pressureSquadSize)
	if #units == 0 then return end
	PressureIndex = PressureIndex + 1
	DispatchTeam("pressure" .. PressureIndex, units, {}, true, true, GIOffensiveContext)
end

local function SetupAttackedTriggers()
	if IsLive(GDI_NW_ATWR) and not TriggerDone.gdi4 then
		Trigger.OnDamaged(GDI_NW_ATWR, function(_, attacker)
			if MissionFinished or TriggerDone.gdi4 then
				return
			end
			if attacker == nil or attacker.Owner ~= Nod then
				return
			end

			TriggerDone.gdi4 = true
			EngineerDrop({ "e6", "e6", "e6" })
		end)
	end

	if IsLive(GDI_N_GTWR) and not TriggerDone.gdi5 then
		Trigger.OnDamaged(GDI_N_GTWR, function(_, attacker)
			if MissionFinished or TriggerDone.gdi5 then
				return
			end
			if attacker == nil or attacker.Owner ~= Nod then
				return
			end

			TriggerDone.gdi5 = true
			TryCreateTeam("gdi10Scripted", { { "htnk", 1 } }, { WP[7], WP[8], WP[9] }, false, false)
		end)
	end
end

-- gdi2/gdi3 are the "enter the old base" responses. They must not hit
-- immediately after the base is reclaimed; each waits baseAttackDelay
-- seconds (Easy 240 / Normal 300 / Hard 360) after TransferNodBase.
local BaseAttackPending = {}
local NodBaseReclaimedAt = nil

function OnNodBaseReclaimed()
	NodBaseReclaimedAt = DateTime.GameTime
	for key, fn in pairs(BaseAttackPending) do
		Trigger.AfterDelay(DateTime.Seconds(CurrentDifficulty().baseAttackDelay), fn)
		BaseAttackPending[key] = nil
	end
end

local function QueueBaseAttack(key, fn)
	if NodBaseReclaimedAt == nil then
		BaseAttackPending[key] = fn
		return
	end

	local target = NodBaseReclaimedAt + DateTime.Seconds(CurrentDifficulty().baseAttackDelay)
	Trigger.AfterDelay(math.max(0, target - DateTime.GameTime), fn)
end

local function AddResponseTrigger(cells, key, response)
	AI.OnFirstEntry(cells, Nod, function(actor)
		-- Aircraft do not occupy cells on the ground; the captured Chinook
		-- must not trip the ground-entry triggers while flying over the base.
		if actor.Type == "tran.capture" or TriggerDone[key] then
			return
		end

		TriggerDone[key] = true
		response()
	end, { isFinished = function() return MissionFinished end })
end

local function SetupResponseTriggers()
	AddResponseTrigger(GDI9Cells, "gdi9", function()
		EngineerDrop({ "e3", "e3", "e6", "e6", "e6" })
	end)

	AddResponseTrigger(GDI2Cells, "gdi2", function()
		QueueBaseAttack("gdi2", function()
			TryCreateTeam("gdi7Scripted", { { "msam", 1 } }, { WP[5], WP[6] }, true, true, GIOffensiveContext)
		end)
	end)

	AddResponseTrigger(GDI3Cells, "gdi3", function()
		QueueBaseAttack("gdi3", function()
			TryCreateTeam("gdi8Scripted", { { "htnk", 1 } }, {}, true, true, GIOffensiveContext)
		end)
	end)

	AddResponseTrigger(Grd1Cells, "grd1", function()
		TryCreateTeam("grd1Scripted", { { "mtnk", 2 } }, { WP[7], WP[8] }, false, false)
	end)

	AddResponseTrigger(Grd2Cells, "grd2", function()
		TryCreateTeam("grd2Scripted", { { "msam", 2 } }, { WP[5], WP[15] }, false, false)
	end)
end

-- Send the base garrison at the attacker (the shared helper does not march
-- them to the nearest enemy building like AllOutAttack would).
function StartGDIBaseDefense(attacker)
	if MissionFinished or GDIHome == nil then return end
	AI.DefendAgainstAttacker(GDIHome, attacker, GIBaseDefenseContext, {
		alarmSeconds = 45,
		releaseOrcas = true,
		isFinished = function() return MissionFinished end,
	})
end

-- The GDI garrison defends its base from the start; this does not make the
-- outer pickets chase across the map because the context is base-local.
function StartBaseDefenseOnce()
	if IdleDefenseStarted or MissionFinished then return end
	IdleDefenseStarted = true
	AI.StartIdleDefense(GIBaseDefenseContext, 10)
end

-- GDI production starts as soon as the player has placed three real
-- buildings. Captured buildings never fire the trigger and wall segments are
-- ignored by the shared helper.
function StartGDIProduction()
	if MissionFinished or GDIProductionStarted then
		return
	end

	GDIProductionStarted = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-production"))
	ProductionTick()
	StartProductionLoop()
end

local function SetupTimedTriggers()
	AI.StartProductionAfterBuildings(Nod, 3, StartGDIProduction,
		{ isFinished = function() return MissionFinished end })

	-- chn1/chn2/chn3: three empty Chinooks arrive in the cyclone pen.
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		SpawnCapturableChinook(CPos.New(12, 61), WP[1])
	end)
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		SpawnCapturableChinook(CPos.New(16, 61), WP[2])
	end)
	Trigger.AfterDelay(DateTime.Seconds(18), function()
		SpawnCapturableChinook(CPos.New(14, 61), WP[3])
	end)

	-- chn4: GDI drop at WP3. The survivors are the recruits for gdi8 at 6:00.
	Trigger.AfterDelay(DateTime.Seconds(300), function()
		TransportDrop({ "e2", "e2", "e2", "e3", "e3" }, CHN4_ENTRY, WP[4], function(passenger)
			if passenger.HasProperty("Stance") then
				passenger.Stance = "Defend"
			end
		end)
	end)

	-- gdi8: Create Team gdi12 (3xE2 + 2xE3) from the chn4 survivors.
	Trigger.AfterDelay(DateTime.Seconds(360), function()
		TryCreateTeam("gdi12Scripted", { { "e2", 3 }, { "e3", 2 } }, { WP[19], WP[20] }, false, false)
	end)

	-- hov1: one Jeep carried by a landing craft. The craft starts in the
	-- water at the south edge, drives to the beach, unloads the Jeep and
	-- leaves again. gdi1 recruits the Jeep one minute later.
	Trigger.AfterDelay(DateTime.Seconds(600), function()
		ReinforceWithLandingCraft(GDI, { "jeep" }, HOV_ENTRY, HOV_LANDING, HOV_RALLY)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Utils.Do(GDI.GetGroundAttackers(), RegisterGDIUnit)
		end)
	end)

	-- gdi1: Create Team gdi6 (one Jeep) from the hov1 Jeep.
	Trigger.AfterDelay(DateTime.Seconds(660), function()
		TryCreateTeam("gdi6Scripted", { { "jeep", 1 } }, { WP[20] }, false, false)
	end)

	-- prod fallback: if the player never places three buildings, production
	-- still starts at the difficulty time. The normal trigger is the third
	-- Nod building placed (OnNodBuildingPlaced above).
	Trigger.AfterDelay(DateTime.Seconds(CurrentDifficulty().prodDelay), StartGDIProduction)

	-- auto: the original Autocreate action alerts the house and starts the
	-- fixed attack teams after the original AlertTime interval.
	Trigger.AfterDelay(DateTime.Seconds(CurrentDifficulty().autoDelay), function()
		if MissionFinished or GDIAIAlerted then
			return
		end

		GDIAIAlerted = true
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-alerted"))
		-- First wave at the alert time; later waves follow waveInterval.
		AutoWaveTick()
		-- Deceit-style announced counter-attack from the surplus garrison.
		Trigger.AfterDelay(DateTime.Seconds(30), LaunchAssault)
	end)

	-- gdi7: repeating engineer drop every 80 minutes (original persistence 2).
	Trigger.AfterDelay(DateTime.Seconds(4800), ScheduleGDI7)
end

function ScheduleGDI7()
	if MissionFinished then
		return
	end

	EngineerDrop({ "e6", "e6", "e6" })
	Trigger.AfterDelay(DateTime.Seconds(4800), ScheduleGDI7)
end

-- Production via the shared scheduler: caps count only units the AI has
-- produced (never the starting garrison or scripted drops), one unit per
-- tick, alternating infantry/vehicles, and the pool is scanned so a 1500 $
-- Mammoth cannot stall the line behind a 600 $ Jeep.
local InfantryPool = { "e3", "e1", "e3" }

function StartProductionLoop()
	AI.StartProduction(GDIHome, GIContext, {
		pools = {
			infantry = InfantryPool,
			vehicle = CurrentDifficulty().vehiclePool or { "jeep" },
		},
		factories = { infantry = "pyle", vehicle = "weap" },
		interval = function() return CurrentDifficulty().prodInterval end,
		maxTotal = function() return CurrentDifficulty().maxProducedUnits end,
		maxPerCategory = 4,
		reserve = function() return CurrentDifficulty().prodReserve end,
		economy = GDI_ECONOMY_OPTIONS,
		produceOrcas = true,
		firstDelay = 0,
		isActive = function() return not MissionFinished and GDIHasBase() end,
		isFinished = function() return MissionFinished end,
	})
end

-- Kept as the maintenance half of the old ProductionTick: rebuild,
-- harvesters and harvester replacement still run on their own cadence.
function ProductionTick()
	if MissionFinished then
		return
	end

	PruneActiveTeams()
	AI.SafeCall("production-maintenance", function()
		if GDIHasBase() and (GDIHome.underAttackUntil == nil
			or DateTime.GameTime >= GDIHome.underAttackUntil) then
			AI.MaintainBase(GDIHome, GDI_REBUILD_OPTIONS)
		end

		AI.MaintainHarvesters(GDIHome, GDI_ECONOMY_OPTIONS)
		local weap = AI.LocalBuilding(GDIHome, "weap", 30)
		AI.ReplaceHarvester(GDIHome, {
			harvesterProductionKind = "weap",
			harvestersPerRefinery = 1,
			reserve = 0,
			canBuild = function()
				return weap ~= nil and IsLive(weap)
			end,
		})
	end)

	Trigger.AfterDelay(DateTime.Seconds(CurrentDifficulty().prodInterval), ProductionTick)
end

function DoAllToHunt()
	for _, actor in ipairs(GDI.GetActors()) do
		if IsLive(actor) and actor.HasProperty("Hunt") then
			if actor.HasProperty("Stance") then
				actor.Stance = "AttackAnything"
			end
			actor.Hunt()
		end
	end
end

function OnGDIBuildingLost()
	if MissionFinished then
		return
	end

	GDIBuildingsLost = GDIBuildingsLost + 1
	if GDIBuildingsLost < 31 or GDIBuildingsHunt then
		return
	end

	GDIBuildingsHunt = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-all-hunt"))
	DoAllToHunt()
end

function RegisterGDIBuilding(building)
	if not IsLive(building) or building.Owner ~= GDI or not building.HasProperty("StartBuildingRepairs") then
		return
	end

	local key = AI.ActorKey(building)
	if RegisteredBuildings[key] then
		return
	end
	RegisteredBuildings[key] = true

	Trigger.OnKilled(building, function(self)
		-- Captured buildings no longer count as GDI losses in the original
		-- BuildingsLost counter.
		if self ~= nil and self.Owner == GDI then
			OnGDIBuildingLost()
		end
		RegisteredBuildings[key] = nil
	end)

	if GIBaseDefenseContext ~= nil and AI.IsLocal(GDIHome, building, 24) then
		AI.RegisterDamageHandler(building, GIBaseDefenseContext)
	end
end

function AutoWaveTick()
	if MissionFinished or not GDIAIAlerted then
		return
	end

	PruneActiveTeams()

	local sent = 0
	local attempts = 0
	while sent < CurrentDifficulty().waveCount and attempts < #AutoOrder do
		local key = AutoOrder[AutoIndex]
		AutoIndex = AutoIndex % #AutoOrder + 1
		attempts = attempts + 1

		local def = AutoDefs[key]
		if def ~= nil and ActiveTeams[key] == nil then
			local units = FindGDIUnits(def.units, def.route[1])
			if units ~= nil then
				DispatchTeam(key, units, def.route, def.attack, def.baseAttack)
				sent = sent + 1
			end
		end
	end

	-- Deceit/Bad Neighborhood pressure: surplus units that were not needed
	-- for an original Autocreate team advance every wave interval.
	SendPressureSquad()

	Trigger.AfterDelay(DateTime.Seconds(CurrentDifficulty().waveInterval), AutoWaveTick)
end

function InitGDI()
	AI.ConfigureLand({ minX = 2, maxX = 61, minY = 1, maxY = 61, stride = 64 })
	AI.BuildLandRegions()

	GDIHome = AI.NewBase({
		player = GDI,
		yard = { 59, 22 },
		defenderReserve = 4,
		baseRadius = 24,
	})
	GDIHome.produced = GDIHome.produced or {}
	GDIHome.orcaBusy = nil

	GIContext = {
		player = GDI,
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
		keepAttackers = true,
		repairThreshold = 0.7,
	}

	-- Units additionally shoot back at whoever damaged them (CampaignAI
	-- handles this through context.selfDefend).
	GIUnitContext = AI.UnitContext(GDIHome, Nod, {
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
	})

	-- The gdi2/gdi3 responses are "attack base" reactions to entering the
	-- reconquered Nod base. They must hit the main base (FACT/PROC), not the
	-- much closer Hand of Nod / Communications Center outpost, which is a
	-- separate discovery. Engineer drops keep the unrestricted GIContext so
	-- they can capture any Nod building.
	GIOffensiveContext = {
		player = GDI,
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
		keepAttackers = true,
		repairThreshold = 0.7,
		exclude = function(actor)
			return actor.Type == "hand" or actor.Type == "hq"
		end,
	}

	-- The generic damage/idle response only covers the main base. The outer
	-- picket towers and the cyclone-pen towers are isolated guards in the
	-- original; attacking them must not pull the three tanks near the pen.
	GIBaseDefenseContext = {
		player = GDI,
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
		keepAttackers = true,
		repairThreshold = 0.7,
		defenseAlarmSeconds = 45,
		exclude = function(actor)
			if not IsLive(actor) or not actor.HasProperty("Location") then
				return true
			end
			return not AI.IsLocal(GDIHome, actor, 24)
		end,
		-- Any registered base building (including the inner towers) pulls the
		-- base garrison. Outer pickets are not registered, so they do not
		-- drag the pen tanks across the map.
		releaseOrcas = true,
		onDamage = function(self, attacker)
			if GDIHome == nil then return end
			StartGDIBaseDefense(attacker)
		end,
	}

	-- Only towers that actually existed at mission start are eligible for
	-- optional rebuilding; the unbuilt [Base] tower nodes must not be built.
	for _, entry in ipairs(GDIRebuildPlan) do
		if (entry[1] == "atwr" or entry[1] == "gtwr")
			and AI.BuildingAt(entry[1], entry[2], entry[3], GDI) ~= nil then
			table.insert(GDI_TowerEntries, entry)
		end
	end

	GDI_REBUILD_OPTIONS = {
		plan = GDIRebuildPlan,
		targets = { fact = 1, proc = 1, pyle = 1, weap = 1, hpad = 2, silo = 4, nuk2 = 3, atwr = 8, gtwr = 10 },
		targetOrder = { "weap", "nuk2", "proc", "pyle", "hpad", "silo", "fact" },
		optionalEntries = GDI_TowerEntries,
		optionalFunds = 2500,
		reserve = function() return CurrentDifficulty().rebuildReserve end,
		siteOptions = { owner = GDI },
		isActive = function()
			return not MissionFinished and GDIHasBase()
				and (GDIHome.underAttackUntil == nil or DateTime.GameTime >= GDIHome.underAttackUntil)
		end,
		onComplete = function(building) RegisterGDIBuilding(building) end,
	}

	AI.InitEconomy(GDIHome, {
		harvesterKind = "harv",
		refineryKind = "proc",
		harvesterProductionKind = "weap",
		incomeWindow = 180,
	})

	if IsLive(GDIHarv) then
		AI.TrackHarvester(GDIHome, GDIHarv)
		if GDIHarv.HasProperty("Stance") then
			GDIHarv.Stance = "Defend"
		end
		GDIHarv.FindResources()
	end

	Utils.Do(GDI.GetActors(), function(actor)
		if IsLive(actor) and actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end
	end)

	Utils.Do(GDI.GetActors(), function(actor)
		if IsLive(actor) and actor.HasProperty("StartBuildingRepairs") then
			RegisterGDIBuilding(actor)
		end
	end)

	Utils.Do(GDI.GetGroundAttackers(), RegisterGDIUnit)
	Trigger.OnAnyProduction(function(factory, unit, productionType)
		if unit == nil or unit.Owner ~= GDI then return end
		RegisterGDIUnit(unit)
		if unit.Type == "orca" then
			GDIHome.orcaBusy = nil
		elseif unit.HasProperty("AttackMove") and unit.Type ~= "harv" and unit.Type ~= "mcv" then
			table.insert(GDIHome.produced, unit)
		end
	end)

	StartBaseDefenseOnce()
	SetupAttackedTriggers()
	SetupResponseTriggers()
	SetupTimedTriggers()
end