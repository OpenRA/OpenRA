--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- GDI schedule for "Cloak and Dagger" (SCB32EA.INI).
--
-- The original mission has no working GDI base production and no
-- Autocreate waves: the auto/prod cell triggers and the Autocreate
-- TeamTypes are present in the INI but are not used by this scenario.
-- The GDI base is static; its units keep their Guard stance (Defend in
-- OpenRA). The only timed GDI threats are the two engineer transports and
-- the Ion Cannon.
--
-- Shared with cloak-and-dagger.lua: DisableIonCannon, GDI, InitGDI,
-- IsCloaked, IsLive, MissionFinished, Nod, NodStartingCash.
local AI = CampaignAI

local GDI_AI
local ScheduleEngineers
local SendEngineersAPC
local SendEngineersChinook
local StartIonCannon
local FireIonCannon

IsLive = function(actor)
	return AI.IsLive(actor)
end

IsCloaked = function(actor)
	return actor.HasProperty("IsCloaked") and actor.IsCloaked
end

local StartingGDICash = 8000

-- Original zzzz: TriggerTime 400 in 6 second units = 40:00, persistence 2
-- repeats every 40:00. Original eng2/engs: 380/390 units = 38:00/39:00.
-- Difficulty tuning: Hard brings both threats forward; Easy keeps the
-- engineer transports one-shot.
local IonCannonFirst = { easy = 2400, normal = 2400, hard = 1800 }
local IonCannonInterval = { easy = 2400, normal = 2400, hard = 1800 }
local EngineerFirstAPC = { easy = 2280, normal = 2280, hard = 1560 }
local EngineerFirstChinook = { easy = 2340, normal = 2340, hard = 1620 }
local EngineerIntervalAPC = { easy = nil, normal = 2280, hard = 1560 }
local EngineerIntervalChinook = { easy = nil, normal = 2340, hard = 1620 }
NodStartingCash = { easy = 6000, normal = 5000, hard = 4000 }

GDI_AI = nil
local IonCannonOnline = false
local IonCannonDisabled = false

-- GDI's declared Edge is North. These are the two road entries that lead
-- directly into the base and to the scripted Chinook landing waypoint.
local GDI_NORTH_ENTRY = CPos.New(8, 2)
local GDI_NORTH_EAST_ENTRY = CPos.New(45, 2)

function InitGDI()
	GDI.Cash = StartingGDICash
	GDI_AI = { enemy = Nod }

	Utils.Do(GDI.GetActors(), function(actor)
		if IsLive(actor) and actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end
	end)

	StartIonCannon()
	ScheduleEngineers()
end

ScheduleEngineers = function()
	local difficulty = Difficulty or "normal"
	Trigger.AfterDelay(DateTime.Seconds(EngineerFirstAPC[difficulty]), SendEngineersAPC)
	Trigger.AfterDelay(DateTime.Seconds(EngineerFirstChinook[difficulty]), SendEngineersChinook)
end

SendEngineersAPC = function()
	if MissionFinished or GDI.HasNoRequiredUnits() then return end

	-- Original engapc: E6 x4 + APC, Move 9 -> 0 -> 5, Unload 5, Attack Base.
	Reinforcements.ReinforceWithTransport(GDI, "apc", { "e6", "e6", "e6", "e6" },
		{ GDI_NORTH_ENTRY, waypoint9.Location, waypoint0.Location, waypoint5.Location },
		nil,
		function(apc, cargo)
			if MissionFinished or not IsLive(apc) then return end
			if apc.HasProperty("UnloadPassengers") then apc.UnloadPassengers() end

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Utils.Do(cargo, function(unit)
					if not IsLive(unit) then return end
					if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
					AI.OrderAttack(unit, GDI_AI)
				end)
			end)
		end)

	local interval = EngineerIntervalAPC[Difficulty or "normal"]
	if interval ~= nil then
		Trigger.AfterDelay(DateTime.Seconds(interval), SendEngineersAPC)
	end
end

SendEngineersChinook = function()
	if MissionFinished or GDI.HasNoRequiredUnits() then return end

	-- Original engchin: E6 x5 + TRAN, Move 7, Unload 7, Attack Base.
	Reinforcements.ReinforceWithTransport(GDI, "tran", { "e6", "e6", "e6", "e6", "e6" },
		{ GDI_NORTH_EAST_ENTRY, waypoint7.Location },
		nil,
		function(tran, cargo)
			if MissionFinished or not IsLive(tran) then return end
			if tran.HasProperty("UnloadPassengers") then tran.UnloadPassengers() end

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Utils.Do(cargo, function(unit)
					if not IsLive(unit) then return end
					if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
					AI.OrderAttack(unit, GDI_AI)
				end)
			end)
		end,
		function(tran)
			-- Give UnloadCargo time to finish before the transport leaves;
			-- an exitPath would queue the move before the unload activity.
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not IsLive(tran) then return end
				tran.Move(GDI_NORTH_EAST_ENTRY)
				Trigger.AfterDelay(DateTime.Seconds(20), function()
					if IsLive(tran) then tran.Destroy() end
				end)
			end)
		end)

	local interval = EngineerIntervalChinook[Difficulty or "normal"]
	if interval ~= nil then
		Trigger.AfterDelay(DateTime.Seconds(interval), SendEngineersChinook)
	end
end

function DisableIonCannon()
	if IonCannonDisabled then return end
	IonCannonDisabled = true
	if IonCannonOnline then
		IonCannonOnline = false
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-destroyed"))
	end
end

StartIonCannon = function()
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirst[Difficulty or "normal"]), FireIonCannon)
end

local function FindIonCannonTarget()
	local eye = GDI_EYE
	if not IsLive(eye) then return nil end

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

FireIonCannon = function()
	if MissionFinished or IonCannonDisabled then return end
	if not IsLive(GDI_EYE) or GDI_EYE.Owner ~= GDI then
		DisableIonCannon()
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

	if not IonCannonOnline then
		IonCannonOnline = true
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	end

	GDI_EYE.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[Difficulty or "normal"]), FireIonCannon)
end
