--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Port of the original Nod Covert Operations mission "Deceit"
-- (SCB21EA.INI).
--
-- The original starts Nod with two commandos and an engineer on the eastern
-- edge of the map, inside the GDI perimeter. A damaged Nod base waits in the
-- south-west. The player has to find a way into the base, mark the drop zone
-- for the agent, rebuild the cut-off base and destroy the GDI forces.
--
-- Cell triggers in the original use `Player Enters` with the BadGuy house,
-- so they are Nod-triggered here. `dzon` marks the drop zone, `supr` brings a
-- GDI Chinook, `echn` an unarmed GDI Chinook fly-by, and `gren` the ambush
-- teams. The `ion1`/`ion2` triggers grant GDI the Ion Cannon when the marked
-- tanks or the factory are attacked.

-- Original [CellTriggers]
DzonCells = {
	CPos.New(13, 34), CPos.New(13, 35), CPos.New(13, 36), CPos.New(13, 37),
	CPos.New(14, 35), CPos.New(14, 36), CPos.New(14, 37)
}

SuprCells = {
	CPos.New(4, 10), CPos.New(4, 11), CPos.New(4, 12),
	CPos.New(5, 10), CPos.New(5, 11), CPos.New(5, 12), CPos.New(5, 13)
}

EchnCells = {
	CPos.New(37, 34), CPos.New(37, 35), CPos.New(38, 34), CPos.New(38, 35),
	CPos.New(55, 52), CPos.New(55, 53), CPos.New(56, 52), CPos.New(56, 53),
	CPos.New(57, 52), CPos.New(57, 53)
}

-- `gren`: ambush cells around the central outposts, the eastern GDI gate and
-- the north-western village.
GrenCells = {
	CPos.New(33, 32), CPos.New(34, 32), CPos.New(35, 32),
	CPos.New(33, 33), CPos.New(34, 33), CPos.New(35, 33),
	CPos.New(33, 34), CPos.New(34, 34),
	CPos.New(33, 35), CPos.New(34, 35),
	CPos.New(33, 36), CPos.New(34, 36),
	CPos.New(33, 37), CPos.New(34, 37), CPos.New(35, 37), CPos.New(36, 37),
	CPos.New(34, 38), CPos.New(35, 38), CPos.New(36, 38),
	CPos.New(19, 23), CPos.New(20, 23),
	CPos.New(19, 24), CPos.New(20, 24),
	CPos.New(19, 25), CPos.New(20, 25),
	CPos.New(53, 54), CPos.New(54, 54),
	CPos.New(53, 55), CPos.New(54, 55),
	CPos.New(53, 56), CPos.New(54, 56), CPos.New(55, 56), CPos.New(56, 56), CPos.New(57, 56),
	CPos.New(53, 57), CPos.New(54, 57), CPos.New(57, 57)
}

MissionFinished = false
DropZoneMarked = false
GDIAmbushSent = false
NodEstablished = false
BaseTakenOver = false
BaseTransferPending = false
NodBaseDiscovered = nil
EchnTransport = nil
EchnSignalSent = false
VillageJeepTriggered = false
VillageJeepTarget = nil
HeliGranted = false
GDIIonCannonUnlocked = false
StopIonCannon = nil
StartingUnits = {}
NodBaseBuildings = {}
CommsHQs = {}
CommsCaptured = {}
CommsCaptureCount = 0
CommsObjectives = {}
-- Cell, base name and objective suffix for the four GDI Communications
-- Centers. The order matches the original HQ cells used by `rtal`.
CommsSites = {
	{ 2, 9, "northwest" },
	{ 22, 40, "southwest" },
	{ 26, 30, "central" },
	{ 39, 40, "southeast" },
}

-- Capturing a GDI Communications Center lets our agent relay false signals
-- and delays the next GDI attack. Each additional center extends the delay.
SignalRelayUntil = 0
SignalRelayDelay = 180

SignalRelayActive = function()
	return DateTime.GameTime < SignalRelayUntil
end

IsNodPlayer = function(player)
	return player ~= nil and player.InternalName ~= nil and player.InternalName:lower() == "nod"
end

IsCombatantPlayer = function(player)
	if player == nil or player.InternalName == nil then
		return false
	end
	local name = player.InternalName:lower()
	return name == "nod" or name == "gdi"
end

-- The original `Unload` order lands the transport on the waypoint cell. Keep
-- the engine's landing search tight so a blocked target cannot move the
-- landing several cells away from the intended waypoint.
TransportDropRange = 3

-- Safe reinforcement landing south of the Nod helipad (HPAD at 4,45).
HelipadLanding = CPos.New(4, 47)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	Civilians = Player.GetPlayer("Civilians")

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)
	FindNodBase = AddPrimaryObjective(Nod, "find-nod-base")
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")

	-- Original [BadGuy] credits: 30 * 100.
	Nod.Cash = 3000

	SetupNodStart()
	SetupIonTriggers()
	SetupGDI()
	SetupDropZone()
	SetupAmbush()
	SetupSupr()
	SetupEchn()
	SetupAgentTriggers()
	SetupCommsObjective()
	SetupCaptureTriggers()
	PrepareTowerReveals()
	SetupNodBaseDiscovery()
end

Tick = function()
	if MissionFinished or DateTime.GameTime <= DateTime.Seconds(2) then
		return
	end

	if GDI.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkCompletedObjective(EliminateGDI)
		return
	end

	if NodBaseDiscoveryCheck ~= nil then
		NodBaseDiscoveryCheck()
	end

	-- Fallback for engines/timing where OnCapture on the transport is missed.
	if EchnTransport ~= nil and not EchnSignalSent and IsLive(EchnTransport)
		and IsNodPlayer(EchnTransport.Owner) then
		SignalCapturedChinook()
	end

	-- During the infiltration phase the only Nod actors are the starting
	-- units. Passengers in a transport are not "in world", and the captured
	-- Chinook deliberately has no MustBeDestroyed trait, so the engine check
	-- would report a false loss. Use it only once the Nod base is owned; until
	-- then the `yyyy` check below covers the starting team (including cargo).
	-- Transports with passengers also count after the base is established.
	if BaseTakenOver and not BaseTransferPending and not NodBaseStillPending()
		and Nod.HasNoRequiredUnits() and not NodHasCargo() then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
		return
	end

	-- Original `yyyy`: losing the initial infiltration team before the base is
	-- established is a mission failure. `dely` disables it once Nod builds; the
	-- nod11a/nod11b-style base takeover also ends the infiltration phase.
	if #StartingUnits > 0 and not BaseTakenOver and not NodEstablished and #AliveStartingUnits() == 0 then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
	end
end

ActorAt = function(kind, x, y)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if IsLive(actor) and actor.Type == kind and actor.HasProperty("Location")
			and actor.Location.X == x and actor.Location.Y == y then
			return actor
		end
	end
	return nil
end

-- Passengers are removed from the world while they are inside a transport.
-- A consumed engineer is also removed, but with IsDead still false, so plain
-- IsDead would count it as alive forever. Check the cargo list explicitly.
IsInCargo = function(actor)
	for _, transport in ipairs(Nod.GetActors()) do
		if not transport.IsDead and transport.HasProperty("Passengers") then
			local ok, passengers = pcall(function()
				return transport.Passengers
			end)
			if ok and passengers ~= nil then
				for _, passenger in ipairs(passengers) do
					if not passenger.IsDead and passenger.ActorID ~= nil
						and passenger.ActorID == actor.ActorID then
						return true
					end
				end
			end
		end
	end

	return false
end

AliveStartingUnits = function()
	local alive = {}
	Utils.Do(StartingUnits, function(actor)
		-- In-world units count normally; passengers count while they are in a
		-- transport; disposed units (e.g. the consumed engineer) do not.
		if actor ~= nil and not actor.IsDead and (actor.IsInWorld or IsInCargo(actor)) then
			table.insert(alive, actor)
		end
	end)
	return alive
end

-- `Nod.HasNoRequiredUnits` ignores passengers and the capturable Chinook,
-- which has no MustBeDestroyed trait. A loaded transport must not lose the
-- mission for the player.
NodHasCargo = function()
	for _, actor in ipairs(Nod.GetActors()) do
		if not actor.IsDead and actor.HasProperty("HasPassengers") then
			local ok, hasPassengers = pcall(function()
				return actor.HasPassengers
			end)
			if ok and hasPassengers then
				return true
			end
		end
	end

	return false
end

-- The engine applies owner changes at the end of the frame. Until the live
-- base buildings really belong to Nod, the required-unit check would see no
-- Nod base at all.
NodBaseStillPending = function()
	for _, actor in ipairs(NodBaseBuildings) do
		if IsLive(actor) and actor.Owner ~= Nod then
			return true
		end
	end

	return false
end

SetupNodStart = function()
	-- The original mission gives Nod two commandos and one engineer on the
	-- eastern edge. They are the `yyyy` lose condition until the base is
	-- rebuilt.
	Utils.Do(Nod.GetActors(), function(actor)
		if not IsLive(actor) or not actor.HasProperty("Location") then
			return
		end

		if actor.Type == "rmbo" or actor.Type == "e6" then
			table.insert(StartingUnits, actor)
		end

		if actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end
	end)

	Trigger.OnAnyProduction(function(producer, unit, productionType)
		if MissionFinished or producer.Owner ~= Nod or not IsLive(unit) then
			return
		end

		if productionType ~= nil and productionType:match("^Building") then
			NodEstablished = true
		end
	end)
end

-- The agent marks the landing zone with a flare and sends a small Nod
-- reinforcement. Used by the original `dzon` cells and by capturing the
-- `echn` Chinook.
MarkDropZone = function()
	if DropZoneMarked or MissionFinished then
		return
	end
	DropZoneMarked = true

	Actor.Create("flare", true, { Owner = Neutral, Location = DefaultFlareLocation.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("drop-zone-marked"))

	-- The original `dzon` only marks the landing zone. The port makes the
	-- landing concrete: the agent sends two rocket infantry and an engineer
	-- to help rebuild the cut-off base.
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if MissionFinished then
			return
		end

		local result = Reinforcements.ReinforceWithTransport(Nod, "tran",
			{ "e3", "e3", "e6" },
			{ waypoint7.Location, HelipadLanding },
			{ waypoint7.Location }, nil, nil, TransportDropRange)
		local tran = result[1]
		if tran == nil then
			return
		end

		Trigger.OnPassengerExited(tran, function(_, unit)
			-- The engine fires this before the passenger enters the world;
			-- IsLive would be false here even for a healthy unit.
			if MissionFinished or unit == nil or unit.IsDead then
				return
			end

			if unit.HasProperty("Stance") then
				unit.Stance = "Defend"
			end
		end)
	end)
end

SetupDropZone = function()
	Trigger.OnEnteredFootprint(DzonCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		MarkDropZone()
	end)
end

SetupAmbush = function()
	Trigger.OnEnteredFootprint(GrenCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		SendAmbush()
	end)
end

SetupSupr = function()
	Trigger.OnEnteredFootprint(SuprCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-reinforcements"))
		SendGDI4()
	end)
end

SetupEchn = function()
	Trigger.OnEnteredFootprint(EchnCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		SendEchnChinook()
	end)
end

SetupIonTriggers = function()
	GDIAdvComCenter = ActorAt("eye", 51, 5)
	GDICYard = ActorAt("fact", 59, 3)
	NodFact = ActorAt("fact", 3, 52)
	NodNuke = ActorAt("nuk2", 6, 44)
	NodPad = ActorAt("hpad", 4, 45)
	NodSilo = ActorAt("silo", 3, 49)

	-- `ion1`: the two marked Mammoth Tanks.
	for _, cell in ipairs({ { 54, 16 }, { 42, 7 } }) do
		local tank = ActorAt("htnk", cell[1], cell[2])
		if IsLive(tank) then
			Trigger.OnDamaged(tank, function()
				UnlockIonCannon()
			end)
		end
	end

	-- `ion2`: attacking the GDI factory also enables the Ion Cannon.
	if IsLive(GDICYard) then
		Trigger.OnDamaged(GDICYard, function()
			UnlockIonCannon()
		end)
	end

	if IsLive(GDIAdvComCenter) then
		Trigger.OnKilled(GDIAdvComCenter, function()
			DisableIonCannon()
		end)
		Trigger.OnCapture(GDIAdvComCenter, function(_, _, _, newOwner)
			if newOwner ~= GDI then
				DisableIonCannon()
			end
		end)
	end
end

SetupNodBaseDiscovery = function()
	NodBaseBuildings = { NodFact, NodNuke, NodPad, NodSilo }

	local function discovered(_, discoverer)
		if BaseTakenOver or MissionFinished or discoverer ~= Nod then
			return
		end

		BaseTakenOver = true
		-- `actor.Owner = Nod` queues the ownership change as a frame-end task.
		-- Skip the engine's required-unit check until that task has run,
		-- otherwise the NodBase buildings still count as absent at takeover.
		BaseTransferPending = true
		Nod.MarkCompletedObjective(FindNodBase)

		Utils.Do(NodBaseBuildings, function(actor)
			if IsLive(actor) then
				actor.Owner = Nod
			end
		end)

		-- The original base includes a combat helicopter on the helipad.
		if IsLive(NodPad) and not HeliGranted then
			HeliGranted = true
			local heli = Actor.Create("heli", true, { Owner = Nod, CenterPosition = NodPad.CenterPosition })
			if heli ~= nil then
				Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("heli-ready"))
			end
		end

		Trigger.AfterDelay(0, function()
			BaseTransferPending = false
		end)

		StartGDIAfterBaseFound()

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Nod, "NewOptions")
		end)
	end

	-- Capturing any of the base buildings with the engineer also counts as
	-- finding the base, even if the unit is not within the conyard radius.
	Utils.Do(NodBaseBuildings, function(actor)
		if IsLive(actor) then
			Trigger.OnCapture(actor, function(_, _, _, newOwner)
				if newOwner == Nod then
					discovered(actor, Nod)
				end
			end)
		end
	end)

	if not IsLive(NodFact) then
		return
	end

	Trigger.OnDiscovered(NodFact, discovered)

	-- Actor-map triggers (proximity and footprints) only track actors with
	-- IOccupySpace; aircraft do not have it, so a flying Chinook would never
	-- trigger them. Check the Nod actors' cells directly instead. This also
	-- covers ground units if the shroud-based notification does not fire.
	NodBaseDiscoveryCheck = function()
		if BaseTakenOver or MissionFinished or not IsLive(NodFact) then
			return
		end

		local yard = NodFact.Location
		for _, actor in ipairs(Nod.GetActors()) do
			-- Player and world actors have no Location property; accessing it
			-- would yield `false`, which is not nil and fails on indexing.
			if not actor.IsDead and actor.HasProperty("Location") then
				local location = actor.Location
				local dx, dy = location.X - yard.X, location.Y - yard.Y
				if dx * dx + dy * dy <= 100 then
					discovered(NodFact, Nod)
					return
				end
			end
		end
	end

	-- Used by the captured `echn` Chinook: the agent's signal reveals the base.
	NodBaseDiscovered = function()
		if not BaseTakenOver and IsLive(NodFact) then
			discovered(NodFact, Nod)
		end
	end
end

-- Capture of the empty `echn` Chinook triggers the agent's signal. Both the
-- OnCapture callback and the Tick owner check call this idempotent function.
SignalCapturedChinook = function()
	if EchnSignalSent or MissionFinished then
		return
	end
	EchnSignalSent = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("chinook-captured"))
	MarkDropZone()
	if NodBaseDiscovered ~= nil then
		NodBaseDiscovered()
	end
	if IsLive(NodFact) then
		Beacon.New(Nod, NodFact.CenterPosition, DateTime.Seconds(15), true)
	end
end

AddIonCannonObjective = function()
	if StopIonCannon ~= nil or MissionFinished then
		return
	end

	StopIonCannon = AddSecondaryObjective(Nod, "destroy-gdi-ion-cannon")
end

DisableIonCannon = function()
	if MissionFinished then
		return
	end

	GDIIonCannonUnlocked = false
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-destroyed"))

	if StopIonCannon ~= nil and not Nod.IsObjectiveCompleted(StopIonCannon)
		and not Nod.IsObjectiveFailed(StopIonCannon) then
		Nod.MarkCompletedObjective(StopIonCannon)
	end
end

-- C&C1 `Destroyed` triggers are attached to whole object groups: the action
-- only fires once every tagged object is gone (`yyyy`, `jog2`, `rchn`, `grds`).
OnAllDestroyed = function(units, callback)
	local alive = 0
	Utils.Do(units, function(unit)
		if IsLive(unit) then
			alive = alive + 1
			Trigger.OnKilled(unit, function()
				alive = alive - 1
				if alive <= 0 then
					callback()
				end
			end)
		end
	end)
end

ClosestNodUnitTo = function(cell)
	local best, bestDist = nil, math.huge
	for _, unit in ipairs(Nod.GetActors()) do
		if not unit.IsDead and unit.HasProperty("Location") and unit.HasProperty("Attack") then
			local location = unit.Location
			if location ~= nil then
				local dx, dy = location.X - cell.X, location.Y - cell.Y
				local distance = dx * dx + dy * dy
				if distance < bestDist then
					best, bestDist = unit, distance
				end
			end
		end
	end

	return best
end

-- The pre-placed Neutral jeep reacts when anyone destroys a civilian or
-- building in the village: it force-attacks the actual destroyer (Nod or GDI).
-- For a C4 kill without a damage event it uses the last Nod unit adjacent.
SetupVillageJeepReaction = function()
	if not IsLive(AgentJeep) then
		return
	end

	local function React(attacker)
		if MissionFinished or not IsLive(AgentJeep) then
			return
		end
		if attacker == nil or attacker.IsDead then
			return
		end
		if not IsCombatantPlayer(attacker.Owner) then
			return
		end
		-- A dead actor loses its script properties (including ActorID), so
		-- only compare while the stored target is still alive.
		if IsLive(VillageJeepTarget) and VillageJeepTarget.ActorID == attacker.ActorID then
			return
		end

		VillageJeepTarget = attacker
		VillageJeepTriggered = true
		if AgentJeep.HasProperty("Stop") then
			AgentJeep.Stop()
		end
		AgentJeep.Attack(attacker, true, true)
	end

	-- GDI village guards retaliate against the unit that actually destroyed
	-- the building, not against whichever Nod unit happens to be closest.
	local function Retaliate(target, location)
		if MissionFinished or not IsLive(target) or location == nil then
			return
		end

		for _, unit in ipairs(GDI.GetActors()) do
			if IsLive(unit) and unit.HasProperty("Move") and unit.HasProperty("Attack") and unit.HasProperty("Location") then
				local unitLocation = unit.Location
				if unitLocation ~= nil then
					local dx, dy = unitLocation.X - location.X, unitLocation.Y - location.Y
					if dx * dx + dy * dy <= 64 then
						unit.Attack(target, true, true)
					end
				end
			end
		end
	end

	local function Handle(target, location)
		if MissionFinished or target == nil or target.IsDead or not IsCombatantPlayer(target.Owner) then
			return
		end
		React(target)
		-- GDI guards only retaliate against Nod attackers.
		if IsNodPlayer(target.Owner) then
			Retaliate(target, location)
		end
	end

	local function Watch(actor)
		-- Civilians.GetActors() also contains the player actor, which has no
		-- Location; attaching the killed handler to it crashes when the
		-- civilian player is defeated.
		if not IsLive(actor) or not actor.HasProperty("Location") then
			return
		end

		-- Buildings are static; cache the location while the actor is alive.
		-- Dead actors may lose their script properties (Location/ActorID).
		local actorLocation = actor.Location
		local lastAttacker = nil
		local lastNearby = nil

		Trigger.OnDamaged(actor, function(_, attacker)
			lastAttacker = attacker
			Handle(attacker, actorLocation)
		end)

		-- C4 does not always emit a damage event. Remember the last Nod unit
		-- adjacent to the building; that is normally the C4 planter.
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(2), function(unit)
			if IsNodPlayer(unit.Owner) then
				lastNearby = unit
			end
		end)

		Trigger.OnKilled(actor, function()
			local target = lastAttacker
			if target ~= nil then
				-- The destroyer is known. Never blame a nearby Nod unit when
				-- GDI (or anyone else) actually destroyed the building.
				if not target.IsDead and IsCombatantPlayer(target.Owner) then
					Handle(target, actorLocation)
				end
				return
			end

			-- No damage event: C4. Use the last adjacent Nod unit, then the
			-- nearest Nod combat unit.
			target = lastNearby
			if target == nil or target.IsDead or not IsCombatantPlayer(target.Owner) then
				target = ClosestNodUnitTo(actorLocation)
			end
			Handle(target, actorLocation)
		end)
	end

	Utils.Do(Civilians.GetActors(), Watch)

	-- The village's c1/c2/c3/c4/c7 houses are Neutral, not Civilians, so they
	-- need their own guard. The bounding box excludes the C8 near the Nod base.
	local function InVillage(location)
		return location ~= nil and location.X >= 34 and location.X <= 52
			and location.Y >= 53 and location.Y <= 62
	end

	for _, actor in ipairs(Neutral.GetActors()) do
		if IsLive(actor) and actor.Type ~= nil and actor.Type:match("^c%d")
			and actor.HasProperty("Location") then
			local location = actor.Location
			if location ~= nil and InVillage(location) then
				Watch(actor)
			end
		end
	end

	local haystack = ActorAt("v13", 28, 57)
	if IsLive(haystack) then
		Watch(haystack)
	end
end

SetupAgentTriggers = function()
	AgentJeep = ActorAt("jeep", 42, 62)
	if IsLive(AgentJeep) and AgentJeep.HasProperty("Stance") then
		AgentJeep.Stance = "Defend"
	end

	-- `jog2`: the neutral agent jeep starts moving once the GDI patrol near the
	-- village is destroyed. The original `nujp` team spawns a new jeep at
	-- waypoint 12; the pre-placed jeep at 42,62 stays where it is.
	local patrol = {
		ActorAt("e1", 36, 59), ActorAt("e1", 39, 58),
		ActorAt("e3", 41, 55)
	}
	OnAllDestroyed(patrol, SendAgentJeep)

	-- `delz`: destroying the civilian C8 cancels the `zzzz` engineer drop.
	local c8 = ActorAt("c8", 5, 60)
	if IsLive(c8) then
		Trigger.OnKilled(c8, function()
			EngineersDisabled = true
		end)
	end

	-- `rchn`: a GDI Chinook reaction force.
	local watch = { ActorAt("e1", 46, 54), ActorAt("e1", 41, 54), ActorAt("e3", 48, 55) }
	OnAllDestroyed(watch, SendChin)

	-- `crch`: destroying the church brings a GDI Chinook team and reveals the
	-- money crate that was hidden under the building in the original map.
	local village = ActorAt("v01", 44, 58)
	if IsLive(village) then
		Trigger.OnKilled(village, function()
			Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(44, 59) })
			SendGDI2()
		end)
	end

	-- `grds`: the three E2 guarding the central outpost. This is the trigger
	-- that sends atk2 to waypoint 17 (32,26), immediately north of the empty
	-- `echn` transport at waypoint 3 (34,30).
	local grds = { ActorAt("e2", 28, 31), ActorAt("e2", 25, 32), ActorAt("e2", 28, 33) }
	OnAllDestroyed(grds, SendAtk2)

	-- `rtal`/`rtl2`: `rtal` is attached to all four GDI HQs and only fires once
	-- every HQ is gone (persistence 1). `rtl2` fires once on the first attack
	-- on the PYLE (persistence 0). Both launch the original `atk1` engineer
	-- Chinook to waypoint 7 (4,59).
	CommsHQs = {}
	for _, site in ipairs(CommsSites) do
		local hq = ActorAt("hq", site[1], site[2])
		if IsLive(hq) then
			table.insert(CommsHQs, hq)
		end
	end
	OnAllDestroyed(CommsHQs, SendEngineerTeam)

	local pyle = ActorAt("pyle", 54, 8)
	if IsLive(pyle) then
		local pyleAttacked = false
		Trigger.OnDamaged(pyle, function()
			if pyleAttacked then
				return
			end
			pyleAttacked = true
			SendEngineerTeam()
		end)
	end

	SetupVillageJeepReaction()
end

-- Each GDI Communications Center is its own secondary objective. Capturing
-- one completes that objective and extends the attack delay; destroying an
-- uncaptured one fails only that center's objective.
SetupCommsObjective = function()
	CommsObjectives = {}
	local secondaryLabel = UserInterface.GetFluentMessage("secondary")
	for _, site in ipairs(CommsSites) do
		-- Add the four objectives directly and announce them with one message.
		-- Four separate AddSecondaryObjective calls produced four chat lines.
		local description = UserInterface.GetFluentMessage("capture-gdi-comms-" .. site[3])
		local objective = Nod.AddObjective(description, secondaryLabel, false)
		local hq = ActorAt("hq", site[1], site[2])
		if IsLive(hq) then
			-- Actor values are fresh Lua wrappers for the same engine actor, so
			-- actor identity must not be used as a table key. ActorID is stable
			-- across the OnCapture/OnKilled callbacks.
			CommsObjectives[CampaignAI.ActorKey(hq)] = objective
		else
			Nod.MarkFailedObjective(objective)
		end
	end

	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("capture-gdi-comms-all"),
		UserInterface.GetFluentMessage("new-secondary-objective"))

	Utils.Do(CommsHQs, function(hq)
		if not IsLive(hq) then
			return
		end

		Trigger.OnCapture(hq, function(_, _, _, newOwner)
			local key = CampaignAI.ActorKey(hq)
			if newOwner ~= Nod or CommsCaptured[key] or MissionFinished then
				return
			end

			CommsCaptured[key] = true
			CommsCaptureCount = CommsCaptureCount + 1
			SignalRelayUntil = math.max(SignalRelayUntil, DateTime.GameTime)
				+ DateTime.Seconds(SignalRelayDelay)

			-- Do not let a pending autocreate wave slip through the relay.
			if NextAutoWaveTime ~= nil then
				NextAutoWaveTime = math.max(NextAutoWaveTime, SignalRelayUntil)
			end

			local objective = CommsObjectives[key]
			if objective ~= nil and not Nod.IsObjectiveCompleted(objective)
				and not Nod.IsObjectiveFailed(objective) then
				Nod.MarkCompletedObjective(objective)
			end

			Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-comms-captured"))
		end)

		Trigger.OnKilled(hq, function()
			local key = CampaignAI.ActorKey(hq)
			if CommsCaptured[key] or MissionFinished then
				return
			end

			local objective = CommsObjectives[key]
			if objective ~= nil and not Nod.IsObjectiveCompleted(objective)
				and not Nod.IsObjectiveFailed(objective) then
				Nod.MarkFailedObjective(objective)
				Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("gdi-comms-center-destroyed"))
			end
		end)
	end)
end

SetupCaptureTriggers = function()
	-- `engr`: if GDI captures one of the key Nod buildings, the agent sends
	-- engineers to recapture it.
	Utils.Do({ NodFact, NodNuke, NodPad }, function(building)
		if not IsLive(building) then
			return
		end

		Trigger.OnCapture(building, function(_, _, _, newOwner)
			if not BaseTakenOver or newOwner ~= GDI or MissionFinished then
				return
			end

			Reinforcements.Reinforce(Nod, { "e6", "e6", "e6" }, { waypoint24.Location }, 10, function(unit)
				if not IsLive(unit) or MissionFinished then
					return
				end

				-- Send the engineers after the building that was just lost;
				-- otherwise they spawn inside the base and idle there.
				if IsLive(building) and building.Owner == GDI and unit.HasProperty("Capture") then
					unit.Capture(building)
				elseif unit.HasProperty("Stance") then
					unit.Stance = "Defend"
				end
			end)
		end)
	end)
end

-- Guard towers stay hidden by the shroud until a Nod unit approaches, so the
-- player sees them before they open fire (same pattern as nod11a/nod11b).
PrepareTowerReveal = function(tower)
	if not IsLive(tower) or tower.IsDead then
		return
	end

	-- GDI towers guard the blockade; they must not auto-target the neutral
	-- NodBase buildings before the player finds the base.
	if tower.HasProperty("Stance") then
		tower.Stance = "Defend"
	end

	Trigger.OnEnteredProximityTrigger(tower.CenterPosition, WDist.FromCells(9), function(actor, id)
		if actor.Owner ~= Nod or not actor.HasProperty("Move") then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		if not tower.IsDead then
			tower.GrantCondition("revealed")
		end
	end)
end

PrepareTowerReveals = function()
	Utils.Do(GDI.GetActorsByType("gtwr"), function(tower)
		PrepareTowerReveal(tower)
	end)
end