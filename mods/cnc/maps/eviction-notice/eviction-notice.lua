--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Nod Covert Operations mission "Eviction Notice" (SCB30EA.INI).
--
-- GDI is asleep until Nod is detected. The placed garrison holds its ground
-- on Defend; the alert can be triggered by the fleeing villagers, by combat
-- damage, by the base perimeter triggers, by the oil derricks, or by the
-- time gate. The AI script owns the alert, production and attack schedule.
--
-- The original `seek` trigger (one HTNK ordered to attack the player) is
-- deliberately not ported: it removed the stealth opening. The HTNK remains
-- as a normal guard on the map.
--
-- Shared with eviction-notice-AI.lua: AlertGDI, GDI, GDI_AI, InitAi,
-- IonCannonOnline, IsLive, MissionFinished, MissionStartTick, Nod,
-- OnGDIProduced, RegisterGDIActor.
local AI = CampaignAI

local RunForHelpCivs = { Actor109, Actor110 }

-- Original `bomb` cells (astk2, one 3-plane A10 strike).
local BombCells = {
	CPos.New(17, 8), CPos.New(16, 8),
	CPos.New(17, 7), CPos.New(16, 7),
	CPos.New(17, 6), CPos.New(16, 6),
	CPos.New(18, 5), CPos.New(17, 5), CPos.New(16, 5),
	CPos.New(19, 4), CPos.New(18, 4), CPos.New(17, 4),
}

-- Original `boo2` cells (grnf1, two HTNKs from WP5).
local InnerBaseCells = {
	CPos.New(7, 5),
	CPos.New(7, 6), CPos.New(8, 6), CPos.New(9, 6),
	CPos.New(7, 7), CPos.New(8, 7), CPos.New(9, 7),
}

-- Original `civv` cells: a civilian reaching any of these warns GDI.
local CivsHelpCells = {
	CPos.New(20, 10), CPos.New(19, 10), CPos.New(18, 10),
	CPos.New(20, 9), CPos.New(19, 9), CPos.New(18, 9),
	CPos.New(20, 8), CPos.New(19, 8), CPos.New(18, 8),
	CPos.New(20, 7), CPos.New(19, 7), CPos.New(18, 7),
	CPos.New(19, 6), CPos.New(18, 6),
}

-- Cross-file state stays global (see header).
IonCannonOnline = false
MissionFinished = false
MissionStartTick = 0

---@type boolean
local CivsRunning = false
---@type boolean
local CivsReachedGDI = false
---@type boolean
local BombCellTriggered = false
---@type boolean
local InnerBaseCellTriggered = false
---@type boolean
local HQAirstrikeSent = false
---@type table<actor, boolean>
local NodDamageRegistered = {}

---@type actor?
local GDIAirstrike3
---@type actor?
local GDIAirstrike1
---@type integer?
local EliminateGDI
---@type player
local Neutral
---@type player
local Civilians
---@type actor?
local Flare
local SendGDIHelpTeam

local function RegisterNodActor(actor)
	if not IsLive(actor) or actor.Owner ~= Nod then return end
	-- Aircraft husks inherit without ScriptTriggers; they cannot carry triggers.
	if actor.Type:match("%.husk$") then return end
	if not actor.HasProperty("Health") then return end
	if NodDamageRegistered[actor] then return end
	NodDamageRegistered[actor] = true

	Trigger.OnDamaged(actor, function(_, attacker)
		if attacker ~= nil and attacker.Owner == GDI then
			AlertGDI("combat")
		end
	end)
	Trigger.OnKilled(actor, function(_, killer)
		if killer ~= nil and killer.Owner == GDI then
			AlertGDI("combat")
		end
	end)
end

local function SendAirstrike(proxy, target)
	if proxy == nil or not IsLive(proxy) or proxy.Owner ~= GDI then return end
	if not proxy.HasProperty("TargetAirstrike") then return end
	proxy.TargetAirstrike(target, Angle.SouthEast)
end

local function RunForHelp(discovered, discoverer)
	if CivsRunning or MissionFinished then return end
	if discoverer ~= Nod then return end
	CivsRunning = true

	Media.DisplayMessage(UserInterface.GetFluentMessage("civilians-running"), UserInterface.GetFluentMessage("nod-soldier"))

	local focus = IsLive(Actor109) and Actor109 or Actor110
	if IsLive(focus) then
		Camera.Position = focus.CenterPosition
	end

	-- Give the player a short reaction window before the runners bolt.
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		if MissionFinished then return end
		Utils.Do(RunForHelpCivs, function(a)
			if not IsLive(a) then return end
			if a.HasProperty("Stance") then a.Stance = "HoldFire" end
			a.Move(waypoint6.Location)
			a.Move(waypoint4.Location)
			a.Move(waypoint9.Location)
			a.Move(waypoint2.Location)
			a.Move(waypoint12.Location)
		end)
	end)

	Trigger.OnEnteredFootprint(CivsHelpCells, function(a, id)
		if CivsReachedGDI or not IsLive(a) then return end
		if a ~= Actor109 and a ~= Actor110 then return end
		CivsReachedGDI = true
		Trigger.RemoveFootprintTrigger(id)
		AlertGDI("civilians")
		SendGDIHelpTeam()
	end)
end

SendGDIHelpTeam = function()
	if MissionFinished then return end
	Reinforcements.Reinforce(GDI, { "mtnk", "mtnk", "msam" }, { waypoint3.Location }, 10, function(unit)
		if not IsLive(unit) then return end
		RegisterGDIActor(unit)
		if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
		AI.SendTeam({ unit }, GDI, { waypoint2, waypoint9, waypoint8 }, true, true, GDI_AI)
	end)
end

local function OnBombCellEntered(actor, id)
	if BombCellTriggered or MissionFinished then return end
	if not IsLive(actor) or actor.Owner ~= Nod then return end
	BombCellTriggered = true
	Trigger.RemoveFootprintTrigger(id)
	AlertGDI("base")
	SendAirstrike(GDIAirstrike3, actor.CenterPosition)
end

local function OnInnerBaseCellEntered(actor, id)
	if InnerBaseCellTriggered or MissionFinished then return end
	if not IsLive(actor) or actor.Owner ~= Nod then return end
	InnerBaseCellTriggered = true
	Trigger.RemoveFootprintTrigger(id)
	AlertGDI("base")

	-- boo2: two HTNKs, but they use the same top-right road entry as the
	-- scripted auto waves instead of popping up next to the GDI base.
	Reinforcements.Reinforce(GDI, { "htnk", "htnk" }, { waypoint3.Location }, 10, function(unit)
		if not IsLive(unit) then return end
		RegisterGDIActor(unit)
		if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
		AI.SendTeam({ unit }, GDI, { waypoint4, waypoint6 }, true, true, GDI_AI)
	end)
end

local function HQCaptured(self, captor, oldOwner, newOwner)
	if HQAirstrikeSent or MissionFinished then return end
	if newOwner ~= Nod then return end
	HQAirstrikeSent = true
	AlertGDI("hq")
	SendAirstrike(GDIAirstrike1, self.CenterPosition)
end

local function EyeDisabled()
	if IonCannonOnline then
		IonCannonOnline = false
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-destroyed"))
	end
end

local function CheckObjectives()
	if MissionFinished then return end

	-- Catch Nod buildings/units that were created after WorldLoaded.
	Utils.Do(Nod.GetActors(), RegisterNodActor)

	if GDI.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkCompletedObjective(EliminateGDI)
		return
	end

	if Nod.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end

function WorldLoaded()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	Civilians = Player.GetPlayer("Civilians")

	MissionStartTick = DateTime.GameTime
	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")

	GDIAirstrike3 = Actor.Create("airstrike.proxy", true, { Owner = GDI })
	GDIAirstrike1 = Actor.Create("airstrike.single", true, { Owner = GDI })

	-- Start view between the Nod landing zone and the village so the player
	-- sees both their own units and the village/flare.
	Camera.Position = Map.CenterOfCell(CPos.New(32, 56))
	Flare = Actor.Create("flare", true, { Owner = Nod, Location = DefaultFlareLocation.Location })
	Trigger.AfterDelay(DateTime.Minutes(1), function()
		if IsLive(Flare) then Flare.Destroy() end
	end)

	InitAi()

	-- OpenRA's civilian actors carry a pistol and default to AttackAnything.
	-- Keep the villagers out of the stealth opening; only the C5/C8 runners
	-- are scripted, and they also run on HoldFire.
	Utils.Do(Civilians.GetActors(), function(a)
		if IsLive(a) and a.HasProperty("Stance") then a.Stance = "HoldFire" end
	end)

	-- The player starts in stealth mode: Nod units hold fire instead of
	-- auto-chasing or auto-engaging wandering GDI guards/harvesters. The player
	-- switches stance manually before starting the assault.
	Utils.Do(Nod.GetActors(), function(a)
		if IsLive(a) and a.HasProperty("Stance") then a.Stance = "HoldFire" end
	end)

	Utils.Do(Nod.GetActors(), RegisterNodActor)
	Trigger.OnAnyProduction(function(producer, unit, productionType)
		if not IsLive(unit) then return end
		if unit.Owner == GDI then
			OnGDIProduced(producer, unit, productionType)
		elseif unit.Owner == Nod then
			RegisterNodActor(unit)
		end
	end)

	Utils.Do(RunForHelpCivs, function(a)
		if IsLive(a) then Trigger.OnDiscovered(a, RunForHelp) end
	end)

	Trigger.OnKilledOrCaptured(GDIAdvComCenter, EyeDisabled)
	Trigger.OnCapture(GDIhq, HQCaptured)

	-- The original WCRATE overlays sit underneath village structures:
	-- (50,54) under V25, (53,56) under V20, (61,41) under V19. Spawn them only
	-- once the covering building is gone, so they are not visible above it.
	Trigger.OnKilled(Actor41, function()
		Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(50, 54) })
	end)
	Trigger.OnKilled(Actor45, function()
		Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(53, 56) })
	end)
	Trigger.OnKilled(Actor53, function()
		Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(61, 41) })
	end)

	Trigger.OnEnteredFootprint(BombCells, OnBombCellEntered)
	Trigger.OnEnteredFootprint(InnerBaseCells, OnInnerBaseCellEntered)

	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end
