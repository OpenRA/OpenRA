--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI economy, production and attacks for "Eviction Notice" (SCB30EA.INI).
--
-- The original house is trigger-driven and passive until it is discovered.
-- Placed units therefore use the same pattern as Bad Neighborhood: Defend for
-- their own position, no proactive IdleDefense until GDIAttackWavesStarted,
-- and a shared damage handler that calls AlertGDI on the first contact.
-- Production and the fixed Autocreate-style waves only start on the alert.

local AI = CampaignAI

IsLive = function(actor)
	return AI.IsLive(actor)
end

AlertTime = { easy = 1500, normal = 1200, hard = 900 }
AlertFirstWave = { easy = 75, normal = 50, hard = 35 }
AlertWaveInterval = { easy = 210, normal = 150, hard = 100 }
ProductionInterval = { easy = 30, normal = 22, hard = 15 }
EngineerDelay = { easy = 360, normal = 300, hard = 240 }
HomeGuardSize = { easy = 3, normal = 4, hard = 6 }
PressureSquadSize = { easy = 2, normal = 3, hard = 4 }
MaxGDIArmy = { easy = 8, normal = 12, hard = 16 }
StartingCash = 9000
IonCannonInterval = 300

GDIAlerted = false
GDIAttackWavesStarted = false
GDIProductionStarted = false
GDIHome = nil
GDI_AI = nil
GDI_IDLE_DEFENSE = nil
GDIRegistered = {}
GDIProducedUnits = {}
NextUnitIndex = 1
NextWaveIndex = 1
NextPressureRoute = 1

UnitRotation = {
	easy = { "e1", "e2", "e3", "jeep", "mtnk", "apc" },
	normal = { "e1", "e2", "e3", "jeep", "mtnk", "apc", "msam", "e2", "mtnk" },
	hard = { "e1", "e2", "e3", "jeep", "mtnk", "apc", "msam", "e2", "mtnk", "htnk" },
}

-- Keep production moving when a tech building is lost: the EYE gates the
-- Mammoth tank, the HQ gates E2/MTNK/MSAM, and a destroyed factory removes
-- its whole unit class from the rotation for that tick.
UnitPrerequisites = {
	e1 = { "barracks" },
	e2 = { "anyhq", "~techlevel.medium" },
	e3 = { "barracks" },
	jeep = { "weap" },
	mtnk = { "anyhq", "~techlevel.medium" },
	apc = { "pyle" },
	msam = { "anyhq", "~techlevel.medium" },
	htnk = { "eye", "~techlevel.high" },
}

-- Original [Base] layout. GDI rebuilds one entry per maintenance pass.
GDI_PLAN = {
	{ "nuk2", 6, 3 }, { "proc", 8, 12 }, { "pyle", 17, 4 }, { "weap", 10, 8 }, { "proc", 12, 3 },
	{ "nuk2", 4, 11 }, { "nuk2", 3, 14 }, { "atwr", 8, 4 }, { "atwr", 6, 9 }, { "atwr", 6, 15 },
	{ "gtwr", 9, 8 }, { "nuk2", 13, 12 }, { "atwr", 15, 9 }, { "atwr", 13, 15 }, { "nuk2", 11, 14 },
	{ "silo", 3, 6 }, { "silo", 5, 17 }, { "atwr", 19, 4 }, { "atwr", 10, 20 }, { "silo", 17, 2 },
	{ "silo", 9, 17 }, { "atwr", 18, 19 },
}

GDI_REBUILD_OPTIONS = {
	plan = GDI_PLAN,
	targets = { proc = 2, weap = 1, pyle = 1 },
	reserve = 500,
	-- Filled in InitAi once the GDI player lookup is available.
	siteOptions = { },
	isActive = function() return not MissionFinished and GDIHasBase() end,
	optionalEntries = function()
		local list = {}
		for _, entry in ipairs(GDI_PLAN) do
			if entry[1] == "silo" or entry[1] == "atwr" or entry[1] == "gtwr" then
				table.insert(list, entry)
			end
		end
		return list
	end,
	onComplete = function(building) RegisterGDIActor(building) end,
}

-- Original Autocreate-style attack teams. Compositions and routes follow the
-- original [TeamTypes] data, but the entry point is consolidated to the
-- north-east road near the top-right Tiberium field (WP3, 49,5) so waves no
-- longer appear directly in front of the GDI base. The rotation is fixed and
-- has no RNG.
AutoWaves = {
	{ units = { "e2", "mtnk" }, entry = waypoint3, route = { }, baseAttack = false }, -- gatk5
	{ units = { "e2", "e2", "e3", "e3", "mtnk" }, entry = waypoint3, route = { waypoint12 }, baseAttack = false }, -- gatk4
	{ units = { "htnk" }, entry = waypoint3, route = { waypoint2 }, baseAttack = true }, -- gatk1
	{ units = { "mtnk", "mtnk", "msam" }, entry = waypoint3, route = { }, baseAttack = true }, -- gatk2
	{ units = { "jeep", "jeep", "jeep" }, entry = waypoint3, route = { }, baseAttack = false }, -- gatk3
	{ units = { "e2", "e2", "e2", "e3", "e3", "e6" }, entry = waypoint3, route = { waypoint2, waypoint5 }, baseAttack = false }, -- gatk
}

PressureRoutes = {
	{ waypoint2, waypoint9, waypoint8 },
	{ waypoint5, waypoint4, waypoint6 },
	{ waypoint2, waypoint5 },
}

function GDIHasBase()
	return IsLive(GDICYard) and GDICYard.Owner == GDI
end

function RegisterGDIActor(actor)
	if not IsLive(actor) or actor.Owner ~= GDI then return end
	if actor.Type == "airstrike.proxy" or actor.Type == "airstrike.single" then return end
	if not actor.HasProperty("Health") then return end
	if GDIRegistered[actor] then return end
	GDIRegistered[actor] = true

	-- Harvesters are economy units and regularly drive past the Nod start on
	-- their way to the nearby Tiberium field. They must not wake the whole GDI
	-- base just because a player unit auto-targets one.
	if actor.Type == "harv" then
		if actor.HasProperty("Stance") then actor.Stance = "Defend" end
		AI.TrackHarvester(GDIHome, actor)
		if actor.HasProperty("FindResources") then actor.FindResources() end
		return
	end

	if actor.HasProperty("Stance") then actor.Stance = "Defend" end
	AI.RegisterDamageHandler(actor, GDI_AI)
	Trigger.OnKilled(actor, function(_, killer)
		if killer ~= nil and killer.Owner == Nod then
			AlertGDI("combat")
		end
	end)
	Trigger.OnCapture(actor, function(_, _, oldOwner, newOwner)
		if oldOwner == GDI and newOwner == Nod then
			AlertGDI("combat")
		end
	end)

	if actor.Type == "harv" and actor.HasProperty("FindResources") then
		AI.TrackHarvester(GDIHome, actor)
		actor.FindResources()
	end
end

function OnGDIProduced(producer, unit, productionType)
	if MissionFinished or not IsLive(unit) or unit.Owner ~= GDI then return end
	RegisterGDIActor(unit)

	if unit.Type == "harv" or unit.Type == "mcv" or unit.Type == "a10"
		or not unit.HasProperty("AttackMove") then
		return
	end

	table.insert(GDIProducedUnits, unit)
	AI.AddToPool(GDIHome, unit, GDI_AI)
end

function CountProducedUnits()
	local count = 0
	local i = #GDIProducedUnits
	while i >= 1 do
		local unit = GDIProducedUnits[i]
		if IsLive(unit) and unit.Owner == GDI then
			count = count + 1
		else
			table.remove(GDIProducedUnits, i)
		end
		i = i - 1
	end
	return count
end

function InitAi()
	local difficulty = Difficulty or "normal"
	GDI.Cash = StartingCash
	GDI_REBUILD_OPTIONS.siteOptions = { owner = GDI }
	MissionStartTick = MissionStartTick or DateTime.GameTime

	AI.ConfigureLand({ minX = 2, maxX = 61, minY = 2, maxY = 61, startCells = { } })
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

	GDI_AI = {
		enemy = Nod,
		players = { GDI },
		base = GDIHome,
		repairThreshold = 0.7,
		keepAttackers = true,
		isActive = function() return not MissionFinished and GDIAlerted end,
		isFinished = function() return MissionFinished end,
		onDamage = function(self, attacker)
			-- Only actual Nod aggression should wake the base. Nil/environment
			-- damage (docking, crush, terrain, scripted effects) and friendly
			-- fire must not trigger the discovery alarm.
			if attacker ~= nil and attacker.Owner == Nod then
				AlertGDI("combat")
			end
		end,
		canRepair = function(actor)
			return actor.Type == "nuk2" or actor.Type == "proc"
				or actor.Type == "weap" or actor.Type == "pyle" or actor.Type == "fact"
		end,
	}
	GDI_IDLE_DEFENSE = {
		enemy = Nod,
		players = { GDI },
		isActive = function() return not MissionFinished and GDIAlerted end,
		isFinished = function() return MissionFinished end,
	}

	Utils.Do(GDI.GetActors(), RegisterGDIActor)

	Trigger.AfterDelay(DateTime.Seconds(12), StartPatrol)
	Trigger.AfterDelay(DateTime.Seconds(10), MaintenanceTick)
	AI.StartIdleDefense(GDI_IDLE_DEFENSE, 3)

	Trigger.AfterDelay(DateTime.Seconds(AlertTime[difficulty]), function()
		AlertGDI("time")
	end)
end

function StartPatrol()
	if MissionFinished then return end

	local patrol = { Actor103, Actor122, Actor123 }
	local path = { waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location }
	Utils.Do(patrol, function(unit)
		if not IsLive(unit) then return end
		if unit.HasProperty("Stance") then unit.Stance = "Defend" end
		if unit.HasProperty("Patrol") then
			unit.Patrol(path, true, DateTime.Seconds(20))
		end
	end)
end

function MaintenanceTick()
	if MissionFinished or not GDIHasBase() then return end

	AI.SafeCall("maintenance", function()
		Utils.Do(GDI.GetActorsByType("harv"), function(harv)
			if IsLive(harv) then
				if harv.HasProperty("Stance") then harv.Stance = "Defend" end
				AI.TrackHarvester(GDIHome, harv)
			end
		end)
		AI.MaintainHarvesters(GDIHome, { harvestersPerRefinery = 1 })
		AI.ReplaceHarvester(GDIHome, {
			harvesterCost = 1100,
			reserve = 0,
			canBuild = function(kind) return GDI.HasPrerequisites({ "proc" }) end,
		})
		AI.MaintainBase(GDIHome, GDI_REBUILD_OPTIONS)
		AI.RecallReserve(GDIHome)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), MaintenanceTick)
end

function StartGDIProduction()
	if GDIProductionStarted or MissionFinished then return end
	GDIProductionStarted = true
	ProductionTick()
end

function ProductionTick()
	if MissionFinished or not GDIAlerted or not GDIHasBase() or GDI.HasNoRequiredUnits() then return end

	local difficulty = Difficulty or "normal"
	if CountProducedUnits() < MaxGDIArmy[difficulty] then
		local rotation = UnitRotation[difficulty] or UnitRotation.normal
		for _ = 1, #rotation do
			local kind = rotation[NextUnitIndex]
			NextUnitIndex = NextUnitIndex % #rotation + 1

			local prerequisites = UnitPrerequisites[kind]
			if prerequisites == nil or GDI.HasPrerequisites(prerequisites) then
				local factory
				if kind == "e1" or kind == "e2" or kind == "e3" then
					factory = AI.LocalBuilding(GDIHome, "pyle", GDIHome.baseRadius)
				else
					factory = AI.LocalBuilding(GDIHome, "weap", GDIHome.baseRadius)
				end

				if factory ~= nil then
					AI.QueueUnit(factory, kind, GDI)
					break
				end
			end
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[difficulty]), ProductionTick)
end

function AlertGDI(reason)
	if GDIAlerted or MissionFinished then return end

	GDIAlerted = true
	GDIAttackWavesStarted = true

	local messages = {
		civilians = "civilians-reached-gdi",
		time = "gdi-alerted-time",
		combat = "gdi-alerted-combat",
		base = "gdi-alerted-base",
		hq = "gdi-alerted-hq",
	}
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage(messages[reason] or "gdi-alerted"))

	StartGDIProduction()
	Trigger.AfterDelay(DateTime.Seconds(AlertFirstWave[Difficulty or "normal"]), WaveTick)
	ScheduleEngineers()
	StartIonCannon()
end

function WaveTick()
	if MissionFinished or not GDIHasBase() or GDI.HasNoRequiredUnits() then return end

	AI.SafeCall("waves", function()
		SendNextAutoWave()
		SendPressureSquad()
	end)

	Trigger.AfterDelay(DateTime.Seconds(AlertWaveInterval[Difficulty or "normal"]), WaveTick)
end

function SendNextAutoWave()
	local def = AutoWaves[NextWaveIndex]
	NextWaveIndex = NextWaveIndex % #AutoWaves + 1
	if def == nil then return end

	Reinforcements.Reinforce(GDI, def.units, { def.entry.Location }, 10, function(unit)
		if not IsLive(unit) then return end
		RegisterGDIActor(unit)
		if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
		AI.SendTeam({ unit }, GDI, def.route, true, def.baseAttack, GDI_AI)
	end)
end

function SendPressureSquad()
	local units = AI.TakeForAttack(GDIHome, PressureSquadSize[Difficulty or "normal"], true)
	if #units == 0 then return end

	local route = PressureRoutes[NextPressureRoute]
	NextPressureRoute = NextPressureRoute % #PressureRoutes + 1
	AI.SendTeam(units, GDI, route, true, false, GDI_AI)
end

function ScheduleEngineers()
	Trigger.AfterDelay(DateTime.Seconds(EngineerDelay[Difficulty or "normal"]), SendEngineers)
end

function SendEngineers()
	if MissionFinished or not GDIHasBase() or GDI.HasNoRequiredUnits() then return end

	-- Original grnf2: four engineers in a Chinook, unload at WP10 and attack
	-- the Nod base. The mission design uses this as a one-time attack after
	-- the alert, not as a reaction to civilian deaths.
	Reinforcements.ReinforceWithTransport(GDI, "tran", { "e6", "e6", "e6", "e6" },
		{ waypoint3.Location, waypoint10.Location },
		{ waypoint10.Location, CPos.New(61, 60) },
		function(tran, cargo)
			if MissionFinished or not IsLive(tran) then return end
			if tran.HasProperty("UnloadPassengers") then tran.UnloadPassengers() end

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Utils.Do(cargo, function(unit)
					if not IsLive(unit) then return end
					RegisterGDIActor(unit)
					if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
					AI.OrderAttack(unit, GDI_AI)
				end)
			end)

			-- Never let a surviving transport block the All-Destroyed win check.
			Trigger.AfterDelay(DateTime.Seconds(45), function()
				if IsLive(tran) and tran.IsInWorld then tran.Destroy() end
			end)
		end)
end

function StartIonCannon()
	if IonCannonOnline or MissionFinished then return end
	if not IsLive(GDIAdvComCenter) or GDIAdvComCenter.Owner ~= GDI then return end

	IonCannonOnline = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))

	-- Original first shot at 25:00. If GDI is alerted later, give it time to
	-- charge; never fire before 25:00.
	local elapsed = math.max(0, math.floor((DateTime.GameTime - MissionStartTick) / 25))
	local delay = math.max(600, 1500 - elapsed)
	Trigger.AfterDelay(DateTime.Seconds(delay), FireIonCannon)
end

function FindIonCannonTarget()
	local eye = GDIAdvComCenter
	local units = { }
	Utils.Do(Nod.GetGroundAttackers(), function(unit)
		if IsLive(unit) and unit.HasProperty("Location") then
			table.insert(units, unit)
		end
	end)

	if #units == 0 then
		return AI.ClosestBuilding(eye, Nod, GDI_AI)
	end

	-- Deterministic target: the largest cluster of Nod ground units, with the
	-- cluster closest to the EYE as the tie-breaker. No random target choice.
	local best, bestScore, bestDistance
	Utils.Do(units, function(unit)
		local score = 0
		Utils.Do(units, function(other)
			if other ~= unit and AI.SameLand(unit.Location, other.Location) then
				local dx = unit.Location.X - other.Location.X
				local dy = unit.Location.Y - other.Location.Y
				if dx * dx + dy * dy <= 4 then
					score = score + 1
				end
			end
		end)

		local dx = unit.Location.X - eye.Location.X
		local dy = unit.Location.Y - eye.Location.Y
		local distance = dx * dx + dy * dy
		if best == nil or score > bestScore or (score == bestScore and distance < bestDistance) then
			best, bestScore, bestDistance = unit, score, distance
		end
	end)

	return best
end

function FireIonCannon()
	if MissionFinished or not IonCannonOnline then return end
	if not IsLive(GDIAdvComCenter) or GDIAdvComCenter.Owner ~= GDI then
		IonCannonOnline = false
		return
	end

	-- The original superweapon needs a working GDI power grid.
	if GDI.PowerState ~= "Normal" then
		Trigger.AfterDelay(DateTime.Seconds(30), FireIonCannon)
		return
	end

	local target = FindIonCannonTarget()
	if target == nil then
		Trigger.AfterDelay(DateTime.Seconds(30), FireIonCannon)
		return
	end

	GDIAdvComCenter.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval), FireIonCannon)
end
