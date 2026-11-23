--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.

   Shared with nod-death-squad.lua: GDI, InitGDI, MissionFinished, Nod.
]]

local AI = CampaignAI

local OnNodUnitKilled
local OnMoveMtnkKilled
local SetupGDIGarrison
local SetupJeepPatrol
local SetupJeep2Patrol
local SetupScout1Patrol
local SetupRespondTrigger
local SetupMoveTrigger
local SetupGredTrigger

-- The imported map is 64 cells wide even though its playable bounds are
-- x = 8..55, y = 7..55.
local LandConfig = { minX = 8, maxX = 55, minY = 7, maxY = 55, stride = 64 }

-- Original [Waypoints], converted with x = n % 64, y = n // 64.
local JeepRoute = {
	CPos.New(19, 31), -- 0
	CPos.New(16, 22), -- 1
	CPos.New(20, 19), -- 2
	CPos.New(36, 18), -- 3
	CPos.New(43, 23), -- 4
	CPos.New(38, 29), -- 5
	CPos.New(43, 23), -- 4
	CPos.New(36, 18), -- 3
	CPos.New(20, 19), -- 2
	CPos.New(16, 22), -- 1
	CPos.New(19, 31), -- 0
}

local ScoutRoute = {
	CPos.New(35, 37), -- 14
	CPos.New(38, 52), -- 15
	CPos.New(20, 36), -- 16
}

local MoveRoute = {
	CPos.New(42, 18), -- 10
	CPos.New(19, 21), -- 11
}

local Jeep2Route = {
	CPos.New(11, 34), -- 6
	CPos.New(39, 33), -- 7
	CPos.New(43, 42), -- 8
}

local Claimed = {}

local GDIHome = nil
local JeepPatrolTeam = {}
local Jeep2Team = {}
local ScoutTeam = {}
local MoveTeam = {}
local RespondTeam = {}
local GDICombatContext = nil
local GDIRespondContext = nil

local BaseAlerted = false
local ResponseDone = false
local MoveMtnkKills = 0
local MoveTeamDone = false

local function AddClaimed(units)
	for _, unit in ipairs(units) do
		Claimed[AI.ActorKey(unit)] = true
	end
end

local function RemoveFromTeam(team, unit)
	for i = #team, 1, -1 do
		if team[i] == unit then
			table.remove(team, i)
		end
	end
end

-- Hard-only base alert. It deliberately excludes the two east-gate MSAMs:
-- their 11c0 range would seal the east corridor, and the mission must stay
-- solvable. Everything else (infantry, tanks, jeeps, Area Guard MSAM) goes
-- to AttackAnything and the patrol loops stop, so the patrols and garrison
-- actively hunt the player.
local function AlertBase()
	if MissionFinished or BaseAlerted then
		return
	end

	BaseAlerted = true

	for _, unit in ipairs(GDI.GetGroundAttackers()) do
		if AI.IsLive(unit) and unit ~= HuntMsam1 and unit ~= HuntMsam2 and unit.HasProperty("Stance") then
			unit.Stance = "AttackAnything"
		end
	end

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-alert"))
end

local function PatrolUnits()
	local units = {}
	for _, team in ipairs({ JeepPatrolTeam, Jeep2Team, ScoutTeam }) do
		for _, unit in ipairs(team) do
			if AI.IsLive(unit) then
				table.insert(units, unit)
			end
		end
	end
	return units
end

-- Hard: an outer patrol that spots a non-cloaked Nod unit (or is attacked)
-- triggers the base alert. Cloaked Stealth Tanks do not trigger it, so the
-- intended stealth route stays open.
local function PatrolSpottedEnemy()
	local enemies = {}
	for _, unit in ipairs(Nod.GetGroundAttackers()) do
		if AI.IsLive(unit) and not AI.IsCloaked(unit) then
			table.insert(enemies, unit)
		end
	end

	for _, patrol in ipairs(PatrolUnits()) do
		for _, enemy in ipairs(enemies) do
			local dx = patrol.Location.X - enemy.Location.X
			local dy = patrol.Location.Y - enemy.Location.Y
			if dx * dx + dy * dy <= 7 * 7 then
				return true
			end
		end
	end

	return false
end

local function StartPatrolAlertWatch()
	local function Check()
		if MissionFinished or BaseAlerted then
			return
		end

		if PatrolSpottedEnemy() then
			AlertBase()
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(2), Check)
	end

	Trigger.AfterDelay(DateTime.Seconds(2), Check)
end

-- Hard: the three original patrol teams (jeep, jeep2, scout1) are the
-- alarm sources. Normal/Easy leave them out.
local function RegisterPatrolAlert(team)
	if Difficulty ~= "hard" then
		return
	end

	for _, unit in ipairs(team) do
		if AI.IsLive(unit) then
			Trigger.OnDamaged(unit, AlertBase)
			Trigger.OnKilled(unit, AlertBase)
		end
	end
end

local function StartPatrolLoop(label, team, route)
	AI.StartSpecialThreat(GDICombatContext, {
		label = label,
		firstDelay = 8,
		interval = 8,
		execute = function()
			if MissionFinished or BaseAlerted then
				return false
			end

			local alive = false
			for _, unit in ipairs(team) do
				if AI.IsLive(unit) and unit.Owner == GDI then
					alive = true
					if unit.IsIdle and unit.HasProperty("Move") then
						AI.SendTeam({ unit }, GDI, route, false, false, GDICombatContext)
					end
				end
			end

			return alive
		end,
	})
end

InitGDI = function()
	AI.ConfigureLand(LandConfig)
	AI.BuildLandRegions()

	GDIHome = AI.NewBase({ player = GDI, yard = { 33, 13 }, defenderReserve = 0, baseRadius = 40 })

	GDICombatContext = AI.UnitContext(GDIHome, Nod, {
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
	})

	GDIRespondContext = AI.UnitContext(GDIHome, Nod, {
		isActive = function() return not MissionFinished end,
		isFinished = function() return MissionFinished end,
	})

	-- Easy-only accessibility change: the ATWR next to the EYE is the last
	-- hard gate of the raid. Normal/Hard keep the original defence.
	if Difficulty == "easy" and EyeAtwr ~= nil and AI.IsLive(EyeAtwr) then
		EyeAtwr.Destroy()
	end

	SetupGDIGarrison()
	SetupJeepPatrol()
	-- The two outer patrol teams are present on Normal and Hard. On Hard
	-- they are also alarm sources; on Normal they only patrol.
	if Difficulty ~= "easy" then
		SetupJeep2Patrol()
		SetupScout1Patrol()
	end
	SetupRespondTrigger()
	SetupMoveTrigger()
	-- The crate alarm exists on every difficulty. On Normal/Easy the base
	-- alert leaves the two east MSAMs on ReturnFire; on Hard they stay on
	-- their normal Defend stance.
	SetupGredTrigger()
	if Difficulty == "hard" then
		StartPatrolAlertWatch()
	end
end

-- GDI starts on Defend. On Normal/Easy the two east-gate MSAMs are
-- permanent ReturnFire sentinels. On Hard they keep the normal Defend
-- stance (the player can attack from the other side). The Hard base alert
-- switches everything except those two MSAMs to AttackAnything.
SetupGDIGarrison = function()
	for _, unit in ipairs(GDI.GetGroundAttackers()) do
		if unit.HasProperty("Stance") then
			unit.Stance = "Defend"
		end
	end

	-- Normal/Easy: passive sentinels. Hard: normal active Defend stance,
	-- because the player can approach the base from the other side.
	if Difficulty ~= "hard" then
		for _, unit in ipairs({ HuntMsam1, HuntMsam2 }) do
			if AI.IsLive(unit) and unit.HasProperty("Stance") then
				unit.Stance = "ReturnFire"
			end
		end
	end
end

-- Original trigger `jeep`: Time 0, Create Team jeep, Persist 0.
-- Create Team recruits the first JEEP in Units order (#010 at 20,29).
SetupJeepPatrol = function()
	local team = AI.FormTeam(GDIHome, GDICombatContext, { { "jeep", 1 } }, {
		routeStart = PatrolJeep,
		reserve = Claimed,
	})
	if team == nil then
		return
	end

	-- Do not reserve the patrol jeep: the original respond team (priority 25)
	-- may pull it out of the low-priority jeep team (priority 7).
	JeepPatrolTeam = team
	RegisterPatrolAlert(team)
	AI.SendTeam(JeepPatrolTeam, GDI, JeepRoute, false, false, GDICombatContext)
	StartPatrolLoop("jeep-patrol", JeepPatrolTeam, JeepRoute)
end

-- `jeep2` is unreferenced in the original mission data. On Hard it is
-- activated as a documented difficulty addition, using the original route
-- (WP6 -> 7 -> 8, Loop 0) and the otherwise idle second jeep. It is also a
-- Hard-only alarm source.
SetupJeep2Patrol = function()
	local team = AI.FormTeam(GDIHome, GDICombatContext, { { "jeep", 1 } }, {
		routeStart = ReserveJeep,
		reserve = Claimed,
	})
	if team == nil then
		return
	end

	Jeep2Team = team
	RegisterPatrolAlert(team)
	AI.SendTeam(Jeep2Team, GDI, Jeep2Route, false, false, GDICombatContext)
	StartPatrolLoop("jeep2-patrol", Jeep2Team, Jeep2Route)
end

-- `scout1` is unreferenced in the original mission data. On Hard it is
-- activated as a documented difficulty addition, using the original
-- TeamType composition and route (3x E2, WP14 -> 15 -> 16, Loop 0). It is
-- also a Hard-only alarm source.
SetupScout1Patrol = function()
	local team = AI.FormTeam(GDIHome, GDICombatContext, { { "e2", 3 } }, {
		routeStart = ScoutRoute[1],
		reserve = Claimed,
	})
	if team == nil then
		return
	end

	AddClaimed(team)
	ScoutTeam = team
	RegisterPatrolAlert(team)
	AI.SendTeam(ScoutTeam, GDI, ScoutRoute, false, false, GDICombatContext)
	StartPatrolLoop("scout1-patrol", ScoutTeam, ScoutRoute)
end

-- Original trigger `rspd`: Discovered, Create Team respond, Persist 0.
-- In Vanilla Conquer this fires from Record_The_Kill for the player's own
-- units, i.e. on the first Nod squad loss. The respond team has
-- RecruitPriority 25, so it can pull the jeep out of the low-priority
-- patrol team; otherwise the reserve jeep is used.
SetupRespondTrigger = function()
	local squad = {
		SquadLtnk, SquadStnk1, SquadStnk2, SquadBuggy, SquadFtnk,
		SquadRmbo, SquadE3a, SquadE3b, SquadE3c,
	}

	for _, unit in ipairs(squad) do
		if AI.IsLive(unit) then
			Trigger.OnKilled(unit, OnNodUnitKilled)
		end
	end
end

OnNodUnitKilled = function()
	if MissionFinished or ResponseDone then
		return
	end

	ResponseDone = true

	local team = AI.FormTeam(GDIHome, GDICombatContext, { { "jeep", 1 } }, {
		routeStart = PatrolJeep,
		reserve = Claimed,
	})
	if team == nil then
		return
	end

	for _, unit in ipairs(team) do
		RemoveFromTeam(JeepPatrolTeam, unit)
		RemoveFromTeam(Jeep2Team, unit)
	end
	AddClaimed(team)
	RespondTeam = team

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-respond"))
	AI.SendTeam(RespondTeam, GDI, {}, true, false, GDIRespondContext)
end

-- Original trigger `move`: Destroyed on MTNK #006 and #005, Persist 1.
-- Semipersistent means it fires only after BOTH are destroyed. The two
-- eastern launchers are the original 2x MSAM team; they move but are reset
-- to ReturnFire immediately, so they never become active sentinels.
SetupMoveTrigger = function()
	if AI.IsLive(MoveMtnk1) then
		Trigger.OnKilled(MoveMtnk1, OnMoveMtnkKilled)
	end
	if AI.IsLive(MoveMtnk2) then
		Trigger.OnKilled(MoveMtnk2, OnMoveMtnkKilled)
	end
end

OnMoveMtnkKilled = function()
	if MissionFinished then
		return
	end

	MoveMtnkKills = MoveMtnkKills + 1
	if MoveMtnkKills < 2 or MoveTeamDone then
		return
	end

	MoveTeamDone = true

	local team = AI.FormTeam(GDIHome, GDICombatContext, { { "msam", 2 } }, {
		routeStart = HuntMsam1,
		reserve = Claimed,
		-- Original `move` TeamType: exactly the two east-gate MSAMs. The
		-- Area Guard MSAM at 28,16 is not recruitable.
		exclude = function(actor) return actor == AreaMsam end,
	})
	if team == nil then
		return
	end

	AddClaimed(team)
	MoveTeam = team

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-redeploy"))
	AI.SendTeam(MoveTeam, GDI, MoveRoute, false, false, GDICombatContext)

	-- Normal/Easy keep the moved launchers passive. Hard keeps the normal
	-- active Defend stance (SendTeam already set it).
	if Difficulty ~= "hard" then
		for _, unit in ipairs(MoveTeam) do
			if AI.IsLive(unit) and unit.HasProperty("Stance") then
				unit.Stance = "ReturnFire"
			end
		end
	end
end

-- Crate alarm on every difficulty: the two original WCRATE cells trigger
-- AlertBase. On Normal/Easy the two east MSAMs stay ReturnFire; on Hard
-- they stay Defend. The rest of the base goes on the attack.
SetupGredTrigger = function()
	AI.OnFirstEntry({ CPos.New(46, 28), CPos.New(29, 28) }, Nod, function()
		AlertBase()
	end, { isFinished = function() return MissionFinished end })
end
