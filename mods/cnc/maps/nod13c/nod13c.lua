--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EC: Nod starts with a single MCV on the northern peninsula. The
-- original `lse2` trigger attached to that MCV loses the mission if it is
-- destroyed. GDI owns three bases: a large northern base, a south-western
-- base with the EYE and helipads, and a small south-eastern outpost whose
-- construction yard is meant to be captured. A second MCV arrives in the
-- west at 20 minutes. The original `yyyy` trigger gives an early MCV when
-- the central GDI outpost HQ is destroyed; `dely` disables that fallback
-- when Nod reaches the western landing area. The scripted airstrikes repeat forever
-- because the original `delx` trigger is not attached to any building.
StartingCashBonus = { easy = 2000, normal = 1000, hard = 0 }
MissionFinished = false
OutcomeCheckPending = false
SecondMCVSent = false
MCVFallbackEnabled = true
MCVInFlight = false
MCVResends = 0
MaxMCVResends = 2

-- Original times, converted from tenths of a minute: the empty GDI
-- transport lands at 0.1 minutes (6 s), the Nod commando and its transport
-- arrive at 0.8 minutes (48 s), the second MCV at 20 minutes (1200 s).
FirstTransportDelay = 6
CommandoDelay = 48
SecondMCVDelay = { easy = 1440, normal = 1200, hard = 1020 }
MCVRetryDelay = 90

-- dely: entering this western strip cancels the early MCV that the
-- destroyed central GDI outpost HQ would otherwise provide.
DelyCells = {
	CPos.New(3, 30), CPos.New(3, 31), CPos.New(3, 32), CPos.New(3, 33),
	CPos.New(3, 34), CPos.New(3, 35), CPos.New(3, 36), CPos.New(3, 37),
	CPos.New(3, 38), CPos.New(3, 39), CPos.New(3, 40),
}

local function IsLive(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

local function IsGDIPlayer(player)
	return player == GDI or player == GDIWest or player == GDISouth
end

-- Empty aircraft are ignored by All Destr., but passengers still count.
-- Scan transports as well so a loaded helicopter cannot be mistaken for an
-- eliminated house.
HouseEliminated = function(player)
	if not player.HasNoRequiredUnits() then
		return false
	end

	for _, actorType in ipairs({ "tran", "tran.in", "tran.capture" }) do
		for _, transport in ipairs(player.GetActorsByType(actorType)) do
			if not transport.IsDead and transport.IsInWorld and transport.HasProperty("Passengers") then
				for _, passenger in ipairs(transport.Passengers) do
					if not passenger.IsDead and passenger.Owner == player then
						return false
					end
				end
			end
		end
	end

	return true
end

-- The AI computes the map's land regions. Anything not connected to the
-- starting peninsula counts as the mainland, so a base next to the northern
-- GDI base is recognised just like one in the west.
local function IsMainland(cell)
	-- The AI script defines IsStartingLand; fall back to a coordinate test
	-- if the mission runs without it.
	if IsStartingLand == nil then return cell.Y >= 25 end
	return not IsStartingLand(cell)
end

local function MainlandBaseExists()
	for _, kind in ipairs({ "fact", "mcv" }) do
		for _, actor in ipairs(Nod.GetActorsByType(kind)) do
			if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
				and IsMainland(actor.Location) then return true end
		end
	end
	return false
end

local function MainlandMCVCount()
	local count = 0
	for _, mcv in ipairs(Nod.GetActorsByType("mcv")) do
		if IsLive(mcv) and mcv.IsInWorld and mcv.HasProperty("Location")
			and IsMainland(mcv.Location) then count = count + 1 end
	end
	return count
end

local function SendMCV(messageKey)
	MCVInFlight = true
	Trigger.AfterDelay(DateTime.Seconds(30), function() MCVInFlight = false end)
	Reinforcements.Reinforce(Nod, { "mcv" }, { NodMCVEntry.Location, waypoint13.Location }, 15)
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage(messageKey))
	Media.PlaySpeechNotification(Nod, "Reinforce")
end

local function SendSecondMCV()
	if MissionFinished or SecondMCVSent then return end
	SecondMCVSent = true
	SendMCV("mcv-arrived")
end

-- Replace a destroyed reinforcement MCV so the mission cannot become
-- unwinnable, but never flood the map with construction vehicles.
local function EnsureMainlandAccess()
	if MissionFinished then return end
	if SecondMCVSent and not MCVInFlight and not MainlandBaseExists()
		and MainlandMCVCount() == 0 and MCVResends < MaxMCVResends then
		MCVResends = MCVResends + 1
		SendMCV("mcv-lost")
	end
	Trigger.AfterDelay(DateTime.Seconds(MCVRetryDelay), EnsureMainlandAccess)
end

-- chn1: an empty GDI transport lands west of the starting area. It is
-- capturable by an engineer and is the intended way to reach the mainland.
local function SendGDIChinook()
	if MissionFinished then return end
	Reinforcements.ReinforceWithTransport(GDI, "tran.capture", nil,
		{ GDIChinookEntry.Location, waypoint0.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("reinforcements-chinook"))
end

-- nchn: a Nod commando and transport helicopter arrive near the church.
local function SendNodCommando()
	if MissionFinished then return end
	Reinforcements.ReinforceWithTransport(Nod, "tran", { "rmbo" },
		{ NodChinookEntry.Location, waypoint1.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("commando-arrived"))
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	GDIWest = Player.GetPlayer("GDIWest")
	GDISouth = Player.GetPlayer("GDISouth")

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	ProtectMCV = AddSecondaryObjective(Nod, "protect-the-mcv")
	BuildTemple = AddSecondaryObjective(Nod, "build-temple")
	Nod.Cash = Nod.Cash + StartingCashBonus[Difficulty or "normal"]

	-- lse2: the starting MCV is the only construction vehicle. Losing it
	-- before it can deploy ends the mission.
	if IsLive(NodStartMCV) then
		Trigger.OnKilled(NodStartMCV, function()
			if MissionFinished then return end
			MissionFinished = true
			Nod.MarkFailedObjective(ProtectMCV)
			Nod.MarkFailedObjective(EliminateGDI)
		end)
	end

	-- yyyy: destroying or capturing the central GDI outpost HQ (22,26) grants
	-- an early MCV, unless Nod has already entered the western landing area
	-- (dely). The original attaches the trigger to this HQ, not to the
	-- northern base's HQ.
	if IsLive(GDIHQ) then
		Trigger.OnKilled(GDIHQ, function()
			if MCVFallbackEnabled and IsGDIPlayer(GDIHQ.Owner) then
				SendSecondMCV()
				Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-hq-destroyed"))
			end
		end)
		Trigger.OnCapture(GDIHQ, function(_, _, oldOwner, newOwner)
			if MCVFallbackEnabled and IsGDIPlayer(oldOwner) and not IsGDIPlayer(newOwner) then
				SendSecondMCV()
				Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-hq-destroyed"))
			end
		end)
	end

	Trigger.OnEnteredFootprint(DelyCells, function(actor, id)
		if actor.Owner ~= Nod then return end
		MCVFallbackEnabled = false
		Trigger.RemoveFootprintTrigger(id)
	end)

	-- The original church spawns a money crate when demolished. The crate
	-- itself is created by the CHURCH SpawnActorOnDeath rules.
	if IsLive(Church) then
		Trigger.OnKilled(Church, function()
			Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("church-destroyed"))
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(FirstTransportDelay), SendGDIChinook)
	Trigger.AfterDelay(DateTime.Seconds(CommandoDelay), SendNodCommando)
	Trigger.AfterDelay(DateTime.Seconds(SecondMCVDelay[Difficulty or "normal"]), SendSecondMCV)
	Trigger.AfterDelay(DateTime.Seconds(120), EnsureMainlandAccess)

	SetupAI()
end

CheckOutcome = function()
	OutcomeCheckPending = false
	if MissionFinished then
		return
	end

	if DateTime.GameTime > DateTime.Seconds(2) and HouseEliminated(Nod) then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
	elseif HouseEliminated(GDI) and HouseEliminated(GDIWest) and HouseEliminated(GDISouth) then
		MissionFinished = true
		Nod.MarkCompletedObjective(EliminateGDI)
	end
end

Tick = function()
	if MissionFinished or DateTime.GameTime % DateTime.Seconds(1) ~= 0 then
		return
	end

	-- Cargo is removed from its transport before it is added back to the world
	-- at frame end. Confirm elimination on the next tick to avoid a false result.
	if not OutcomeCheckPending and ((DateTime.GameTime > DateTime.Seconds(2) and HouseEliminated(Nod))
		or (HouseEliminated(GDI) and HouseEliminated(GDIWest) and HouseEliminated(GDISouth))) then
		OutcomeCheckPending = true
		Trigger.AfterDelay(1, CheckOutcome)
	end

	if not Nod.IsObjectiveCompleted(ProtectMCV)
		and (not IsLive(NodStartMCV) or #Nod.GetActorsByType("fact") > 0) then
		Nod.MarkCompletedObjective(ProtectMCV)
	end

	if not Nod.IsObjectiveCompleted(BuildTemple) and #Nod.GetActorsByType("tmpl") > 0 then
		Nod.MarkCompletedObjective(BuildTemple)
	end
end
