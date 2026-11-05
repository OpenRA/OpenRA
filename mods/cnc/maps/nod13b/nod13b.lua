--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EB: Nod starts on the northern island and must establish a second
-- base on the mainland before it can destroy GDI. The original victory and
-- defeat triggers only check the destruction of the opposing house; the
-- Temple objective is a mission briefing request, not a win condition.
StartingCashBonus = { easy = 2000, normal = 1000, hard = 0 }
GDIReinforcementPassengers = {}
MissionFinished = false
OutcomeCheckPending = false
AirstrikeStopped = false
IonCannonStopped = false
WestReinforcementsStopped = false

-- Original times, converted from tenths of a minute: the MCV and two Light
-- Tanks arrive at 14.8 minutes, the transport helicopter at 19.3 minutes.
MCVDelay = { easy = 1066, normal = 888, hard = 755 }
ChinookDelay = { easy = 1390, normal = 1158, hard = 985 }
MCVRetryDelay = 90
MaxMCVResends = 2

local function IsLive(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

local function IsGDIPlayer(player)
	return player == GDI or player == GDIWest
end

TrackGDIReinforcements = function(passengers)
	for i = #GDIReinforcementPassengers, 1, -1 do
		if GDIReinforcementPassengers[i].IsDead then table.remove(GDIReinforcementPassengers, i) end
	end
	for _, passenger in ipairs(passengers or {}) do
		table.insert(GDIReinforcementPassengers, passenger)
	end
end

-- Empty aircraft are ignored by All Destr., but their passengers still count.
-- The check also covers the player's own transport so a loaded helicopter
-- cannot be mistaken for an eliminated house.
HouseEliminated = function(player)
	if not player.HasNoRequiredUnits() then
		return false
	end

	for _, passenger in ipairs(GDIReinforcementPassengers) do
		if passenger.Owner == player and not passenger.IsDead then
			return false
		end
	end

	for _, actorType in ipairs({ "tran", "tran.capture", "tran.in" }) do
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

local function StopAirstrikes()
	if AirstrikeStopped then return end
	AirstrikeStopped = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("airstrikes-stopped"))
	Nod.MarkCompletedObjective(StopAirstrikesObjective)
end

local function StopIonCannon()
	if IonCannonStopped then return end
	IonCannonStopped = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-offline"))
	Nod.MarkCompletedObjective(StopIonCannonObjective)
end

-- The western base calls in its relief force through its Communications
-- Center. Destroying or capturing it permanently stops those reinforcements.
function StopWestReinforcements()
	if WestReinforcementsStopped then return end
	WestReinforcementsStopped = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("west-reinforcements-stopped"))
	Nod.MarkCompletedObjective(StopWestReinforcementsObjective)
end

local function SendSouthernMCV()
	if MissionFinished then return end
	Reinforcements.Reinforce(Nod, { "mcv", "ltnk", "ltnk" },
		{ NodMCVEntry.Location, waypoint4.Location }, 15)
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("reinforcements-south"))
	Media.PlaySpeechNotification(Nod, "Reinforce")
end

-- ReinforceWithTransport queues a real landing instead of only a move order,
-- so the helicopter touches down at the coast and remains available there.
local function SendNodChinook()
	if MissionFinished then return end
	Reinforcements.ReinforceWithTransport(Nod, "tran", nil,
		{ NodChinookEntry.Location, waypoint1.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("reinforcements-chinook"))
end

-- Replace a destroyed reinforcement MCV so the mission cannot become
-- unwinnable, but never flood the map with construction vehicles.
local MCVResends = 0

local function EnsureSouthernAccess()
	if MissionFinished then return end
	if not NodHasSouthernBase() and #Nod.GetActorsByType("mcv") == 0
		and MCVResends < MaxMCVResends then
		MCVResends = MCVResends + 1
		SendSouthernMCV()
	end
	Trigger.AfterDelay(DateTime.Seconds(MCVRetryDelay), EnsureSouthernAccess)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	GDIWest = Player.GetPlayer("GDIWest")

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	BuildTemple = AddSecondaryObjective(Nod, "build-temple")
	StopAirstrikesObjective = AddSecondaryObjective(Nod, "destroy-gdi-communications-center")
	StopWestReinforcementsObjective = AddSecondaryObjective(Nod, "destroy-gdi-west-communications-center")
	StopIonCannonObjective = AddSecondaryObjective(Nod, "destroy-gdi-advanced-communications-center")
	Nod.Cash = Nod.Cash + StartingCashBonus[Difficulty or "normal"]

	-- delx: destroying or capturing the GDI HQ stops the scripted airstrikes.
	if IsLive(GDIHQ) then
		Trigger.OnKilled(GDIHQ, StopAirstrikes)
		Trigger.OnCapture(GDIHQ, function(_, _, oldOwner, newOwner)
			if IsGDIPlayer(oldOwner) and not IsGDIPlayer(newOwner) then StopAirstrikes() end
		end)
	end

	-- delz: destroying or capturing the GDI EYE stops the scripted Ion Cannon.
	if IsLive(GDIEYE) then
		Trigger.OnKilled(GDIEYE, StopIonCannon)
		Trigger.OnCapture(GDIEYE, function(_, _, oldOwner, newOwner)
			if IsGDIPlayer(oldOwner) and not IsGDIPlayer(newOwner) then StopIonCannon() end
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(MCVDelay[Difficulty or "normal"]), function()
		SendSouthernMCV()
		Trigger.AfterDelay(DateTime.Seconds(ChinookDelay[Difficulty or "normal"]
			- MCVDelay[Difficulty or "normal"]), SendNodChinook)
		Trigger.AfterDelay(DateTime.Seconds(120), EnsureSouthernAccess)
	end)

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
	elseif HouseEliminated(GDI) and HouseEliminated(GDIWest) then
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
		or (HouseEliminated(GDI) and HouseEliminated(GDIWest))) then
		OutcomeCheckPending = true
		Trigger.AfterDelay(1, CheckOutcome)
	end

	if not Nod.IsObjectiveCompleted(BuildTemple) and #Nod.GetActorsByType("tmpl") > 0 then
		Nod.MarkCompletedObjective(BuildTemple)
	end
end