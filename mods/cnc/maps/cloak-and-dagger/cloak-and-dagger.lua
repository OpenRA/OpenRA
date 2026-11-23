--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Nod Covert Operations mission "Cloak and Dagger" (SCB32EA.INI).
--
-- Nod starts with an MCV hidden in a pocket at the western edge of the GDI
-- base and a stealth tank in the south-east. The GDI house is passive until a
-- Nod unit trips one of the original cell triggers. Cloaked units do not
-- trip cell triggers, so the stealth tank can scout; the uncloaked MCV
-- starts the alarm when it leaves the pocket.
--
-- Trigger semantics used here:
--   apch  Player Enters on a building = captured; capturing the Tech Center
--         sends two Apaches to Nod.
--   desx  Nod enters the pocket exit cells -> STNK loss trigger removed.
--   cnst  Built It with parameter 6 = FACT; deploying the MCV removes the
--         MCV loss trigger.
--   auto  Player Enters -> all houses alerted, GDI Autocreate teams start.
--   prod  Player Enters -> GDI production and [Base] rebuilding (no alert).
--   gohp  Attacked on V01-V08/V10 -> Create Team civhlp (existing C1/C5/C6/C8).
--   hlp2  Neutral enters the base-approach cells -> Create Team gdihlp2
--         (the two existing HTNKs drive to the village and back).
--
-- Shared with cloak-and-dagger-AI.lua: DisableIonCannon, GDI, InitGDI,
-- IsCloaked, IsLive, MissionFinished, Nod, NodStartingCash.
local AI = CampaignAI

-- Cross-file state stays global (see header).
MissionFinished = false

---@type boolean
local MCVRequired = true
---@type boolean
local STNKRequired = true
---@type boolean
local DesxDone = false
---@type boolean
local Hlp2Done = false
---@type boolean
local VillageAttacked = false
---@type boolean
local ApachesSent = false
---@type boolean
local DiversionCompleted = false
---@type boolean
local PowerDownCompleted = false
---@type integer
local SouthernTowersDestroyed = 0

---@type table[]
local VillageBuildings = { }
---@type table[]
local VillageGuards = { }

-- The seven southern guards exist only on Hard. Normal/Easy remove them
-- before the mission starts.
local function ApplyDifficultyGuards()
	if (Difficulty or "normal") == "hard" then return end
	Utils.Do(VillageGuards, function(unit)
		if IsLive(unit) then unit.Destroy() end
	end)
	VillageGuards = { }
end

-- Easy: remove the outpost ATWR and trade one base NUK2 for a basic NUKE
-- (100 instead of 200 power). Normal/Hard keep the outpost ATWR.
local function ApplyEasyMapChanges()
	if (Difficulty or "normal") ~= "easy" then return end

	if IsLive(Actor173) then Actor173.Destroy() end
	if IsLive(Actor162) then
		local location = Actor162.Location
		Actor162.Destroy()
		Actor.Create("nuke", true, { Owner = GDI, Location = location })
	end
end
local CounterAttackUnits = { }
local SouthernTowers = { }

-- Original desx cells (18). (3,11) is not part of the zone; the MCV sits in
-- the pocket and trips it as soon as it moves east to (4,11).
local DesxCells = {
	CPos.New(2, 9), CPos.New(3, 9), CPos.New(4, 9), CPos.New(5, 9),
	CPos.New(2, 10), CPos.New(3, 10), CPos.New(4, 10), CPos.New(5, 10),
	CPos.New(4, 11), CPos.New(5, 11),
	CPos.New(2, 12), CPos.New(3, 12), CPos.New(4, 12), CPos.New(5, 12),
	CPos.New(2, 13), CPos.New(3, 13), CPos.New(4, 13), CPos.New(5, 13),
}

-- Original hlp2 cells (8): the villager route between WP0 and WP2.
local Hlp2Cells = {
	CPos.New(6, 25), CPos.New(7, 25), CPos.New(8, 25), CPos.New(9, 25),
	CPos.New(7, 26), CPos.New(8, 26), CPos.New(9, 26), CPos.New(10, 26),
}

-- Original Nod Edge is South; (54,61) is the passable edge cell next to the
-- stealth tank's start.
local NodSouthEntry = CPos.New(54, 61)

---@type player
local Neutral
---@type integer?
local RecoverMCV
---@type integer?
local VillageDiversion
---@type integer?
local PowerDown
local CheckObjectives

local function FailMission()
	if MissionFinished then return end
	MissionFinished = true
	Nod.MarkFailedObjective(RecoverMCV)
end

local function WinMission()
	if MissionFinished then return end
	MissionFinished = true
	Nod.MarkCompletedObjective(RecoverMCV)
end

-- Move every live member of a team to a waypoint and call back once they are
-- all idle. Used for the two "Create Team" routes that contain Guard waits.
local function MoveTeamThen(team, waypoint, done)
	local any = false
	Utils.Do(team, function(unit)
		if IsLive(unit) then
			any = true
			if unit.HasProperty("Stop") then unit.Stop() end
			if unit.HasProperty("Move") then unit.Move(waypoint.Location) end
		end
	end)

	if not any then return end

	local function WaitForArrival()
		if MissionFinished then return end
		local moving = false
		local alive = false
		Utils.Do(team, function(unit)
			if IsLive(unit) then
				alive = true
				if not unit.IsIdle then moving = true end
			end
		end)
		if not alive then return end
		if moving then
			Trigger.AfterDelay(DateTime.Seconds(2), WaitForArrival)
		elseif done ~= nil then
			done()
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(2), WaitForArrival)
end

local function StartCivilianHelp()
	if VillageAttacked or MissionFinished then return end
	VillageAttacked = true

	-- Original civhlp: C1, C5, C6 and C8. Create Team recruits existing units.
	local team = { }
	local wanted = { c1 = true, c5 = true, c6 = true, c8 = true }
	Utils.Do(Neutral.GetActors(), function(actor)
		if #team < 4 and wanted[actor.Type] and IsLive(actor) then
			wanted[actor.Type] = nil
			table.insert(team, actor)
		end
	end)
	if #team == 0 then return end

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("villagers-running"))
	Utils.Do(team, function(unit)
		if unit.HasProperty("Stance") then unit.Stance = "HoldFire" end
	end)

	-- Move:0, Move:2, Guard:5, Move:0, Move:1.
	MoveTeamThen(team, waypoint0, function()
		MoveTeamThen(team, waypoint2, function()
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				MoveTeamThen(team, waypoint0, function()
					MoveTeamThen(team, waypoint1, function() end)
				end)
			end)
		end)
	end)
end

local function CreateGDIHelpTeam()
	if MissionFinished then return end

	-- Original gdihlp2: the two existing Mammoth tanks at the base entrance.
	-- The seven southern-entrance guards accompany them to the village and
	-- stay there; only the Mammoth tanks return.
	local mammoths = { }
	Utils.Do(GDI.GetActorsByType("htnk"), function(actor)
		if #mammoths < 2 and IsLive(actor) and actor.Owner == GDI then
			table.insert(mammoths, actor)
		end
	end)

	local team = { }
	Utils.Do(mammoths, function(actor) table.insert(team, actor) end)
	Utils.Do(VillageGuards, function(actor)
		if IsLive(actor) and actor.Owner == GDI then table.insert(team, actor) end
	end)
	if #team == 0 then return end

	if not DiversionCompleted then
		DiversionCompleted = true
		Nod.MarkCompletedObjective(VillageDiversion)
	end

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("mammoths-responding"))
	Utils.Do(team, function(unit)
		if unit.HasProperty("Stance") then unit.Stance = "Defend" end
	end)

	-- Move:1, Guard:40, Move:0 for the Mammoth tanks only; the infantry
	-- remain in the village.
	MoveTeamThen(team, waypoint1, function()
		Trigger.AfterDelay(DateTime.Seconds(40), function()
			MoveTeamThen(mammoths, waypoint0, function() end)
		end)
	end)
end

local function OrderCounterAttack(target)
	if target == nil or not IsLive(target) then return end
	Utils.Do(CounterAttackUnits, function(unit)
		if not IsLive(unit) then return end
		if unit.HasProperty("Attack") then
			unit.Attack(target)
		elseif unit.HasProperty("Hunt") then
			unit.Hunt()
		end
	end)
end

local function OnCounterAttack(self, attacker, damage)
	if MissionFinished or damage == nil or damage <= 0 then return end
	if attacker == nil or attacker.Owner ~= Nod then return end
	OrderCounterAttack(attacker)
end

local function MammothRetaliation(killer)
	if MissionFinished then return end
	Utils.Do({ Actor207, Actor208 }, function(unit)
		if not IsLive(unit) then return end
		if killer ~= nil and IsLive(killer) and killer.Owner == Nod and unit.HasProperty("Attack") then
			unit.Attack(killer)
		elseif unit.HasProperty("Hunt") then
			unit.Hunt()
		end
	end)
end

local function OnSouthernTowerKilled(self, killer)
	if MissionFinished then return end
	SouthernTowersDestroyed = SouthernTowersDestroyed + 1
	if SouthernTowersDestroyed >= 2 then MammothRetaliation(killer) end
end

local function OnVillageAttacked(self, attacker, damage)
	if VillageAttacked or MissionFinished then return end
	if damage == nil or damage <= 0 then return end
	StartCivilianHelp()
end

local function OnDesxCellEntered(actor, id)
	if DesxDone or MissionFinished then return end
	if not IsLive(actor) or actor.Owner ~= Nod or IsCloaked(actor) then return end

	DesxDone = true
	Trigger.RemoveFootprintTrigger(id)
	STNKRequired = false
end

local function OnHlp2CellEntered(actor, id)
	if Hlp2Done or MissionFinished then return end
	if not IsLive(actor) or actor.Owner ~= Neutral or IsCloaked(actor) then return end

	Hlp2Done = true
	Trigger.RemoveFootprintTrigger(id)
	CreateGDIHelpTeam()
end

-- Original apch is attached to the Tech Center with a Player Enters event,
-- which means "the linked object was captured" for either house.
local function OnTechCenterCaptured(self, captor, oldOwner, newOwner)
	if ApachesSent or MissionFinished then return end

	ApachesSent = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("tech-center-captured"))
	Reinforcements.Reinforce(Nod, { "heli", "heli" }, { NodSouthEntry }, 10, function(unit)
		if not IsLive(unit) then return end
		if unit.HasProperty("Stance") then unit.Stance = "Defend" end
	end)
end

CheckObjectives = function()
	if MissionFinished then return end

	-- power-down: the outpost reactor is the critical NUK2; when the GDI
	-- base drops to low power the ATWR/EYE sensors go offline.
	if not PowerDownCompleted and GDI.PowerState ~= "Normal" then
		PowerDownCompleted = true
		Nod.MarkCompletedObjective(PowerDown)
	end

	-- cnst fires on the Construction Yard created by deploying the MCV.
	if MCVRequired and not IsLive(NodMCV) and #AI.GetActorsByType(Nod, "fact") > 0 then
		MCVRequired = false
	end

	if GDI.HasNoRequiredUnits() then
		WinMission()
		return
	end

	if Nod.HasNoRequiredUnits() then
		FailMission()
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end

function WorldLoaded()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")

	Nod.Cash = NodStartingCash[Difficulty or "normal"]

	InitObjectives(Nod)
	RecoverMCV = AddPrimaryObjective(Nod, "recover-mcv-and-eliminate-gdi")
	VillageDiversion = AddSecondaryObjective(Nod, "village-diversion")
	PowerDown = AddSecondaryObjective(Nod, "power-down")

	VillageGuards = { SouthGuard1, SouthGuard2, SouthGuard3, SouthGuard4,
		SouthGuard5, SouthGuard6, SouthGuard7 }
	ApplyDifficultyGuards()
	-- The tank above the construction yard, the eastern tank and the jeep
	-- retaliate together. The Mammoth tanks are not part of this group;
	-- they only react when both southern guard towers are destroyed.
	CounterAttackUnits = { Actor202, Actor205, Actor206 }
	SouthernTowers = { Actor192, Actor193 }

	Utils.Do(CounterAttackUnits, function(unit)
		Trigger.OnDamaged(unit, OnCounterAttack)
	end)
	Utils.Do(SouthernTowers, function(tower)
		Trigger.OnKilled(tower, OnSouthernTowerKilled)
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitGDI()
	ApplyEasyMapChanges()

	-- The stealth opening depends on the player choosing when to fire. Keep
	-- both Nod and the neutral villagers on HoldFire until the player acts.
	Utils.Do(Nod.GetActors(), function(actor)
		if IsLive(actor) and actor.HasProperty("Stance") then actor.Stance = "HoldFire" end
	end)
	Utils.Do(Neutral.GetActors(), function(actor)
		if IsLive(actor) and actor.HasProperty("Stance") then actor.Stance = "HoldFire" end
	end)

	Utils.Do({ V01, V02, V03, V04, V05, V06, V07, V08, V10 }, function(building)
		if building ~= nil then
			table.insert(VillageBuildings, building)
			Trigger.OnDamaged(building, OnVillageAttacked)
		end
	end)

	Trigger.OnKilled(NodSTNK, function()
		if STNKRequired and not MissionFinished then FailMission() end
	end)
	Trigger.OnKilled(NodMCV, function()
		if MCVRequired and not MissionFinished then FailMission() end
	end)

	Trigger.OnKilledOrCaptured(GDI_EYE, DisableIonCannon)
	Trigger.OnCapture(TechCenter, OnTechCenterCaptured)

	Trigger.OnEnteredFootprint(DesxCells, OnDesxCellEntered)
	Trigger.OnEnteredFootprint(Hlp2Cells, OnHlp2CellEntered)


	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end
