--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Port of the original Nod mission 11b ("Ezekiel's Wheel", SCB11EB.INI).
--
-- Original win/lose conditions ([Triggers] in SCB11EB.INI):
--   win  = All Destr.,Win, 0,GoodGuy  -> all GDI forces destroyed
--   lose = All Destr.,Lose,0,BadGuy   -> all Nod forces destroyed
--   xxxx = Destroyed,Lose,0,None,None,1
--          attached to the north-west strike team; disabled by delx
--   delx = Player Enters,Dstry Trig 'XXXX',0,BadGuy
--          cell trigger at the river crossing south of the Nod base
--
-- The scripted attacks, cell triggers and GDI production live in
-- nod11b-AI.lua. Original [Triggers] entries for reference:
--   apc1  Time,Reinforce.,  2,GoodGuy,apc1
--   apc2  Time,Reinforce., 10,GoodGuy,apc1
--   yyyy  Time,Reinforce.,140,GoodGuy,a10s,2   (A10 strike)
--   dely  Destroyed,Dstry Trig 'YYYY',0,None   (GDI HQ destruction stops it)
--   atk1  Player Enters -> Create Team gdi1    ([CellTriggers] west crossing)
--   atk2  Time,Create Team,120,GoodGuy,gdi2
--   atk3  Time,Create Team,150,GoodGuy,gdi3
--   atk4  Time,Create Team,160,GoodGuy,gdi4
--   atk5  Time,Create Team,160,GoodGuy,gdi4
--   atk6  Time,Create Team,750,GoodGuy,heavy1
--   atk7  Time,Create Team,745,GoodGuy,heavy2
--   auto  Time,Autocreate,240,GoodGuy
--   grd1  Player Enters -> Create Team gdi5    ([CellTriggers] east flank)
--   prod  Player Enters -> Production          ([CellTriggers] central area)
--   apc3  Destroyed -> Reinforce apc1          (all four base defenses)
--
-- Team compositions and waypoint paths are in [TeamTypes]; the waypoint
-- numbers there refer to the waypoint0..waypoint18 actors on this map.
--
-- As in 11a, the Nod base is owned by the non-combatant NodBase player until
-- the strike team reaches it. Only then does it transfer to the player and
-- does the GDI AI (harvesters, production, waves, A10) start.

-- The north-west strike team. The original `xxxx` trigger is attached to 15
-- of the 16 units; the E3 at (9,4) with subcell 2 is missing it. All 16 are
-- tracked here so the lose condition cannot be dodged (documented deviation).
StrikeTeam = {
	Commando,
	Engineer1, Engineer2, Engineer3,
	Rifleman1, Rifleman2, Rifleman3, Rifleman4, Rifleman5, Rifleman6,
	RocketSoldier1, RocketSoldier2, RocketSoldier3, RocketSoldier4, RocketSoldier5, RocketSoldier6,
}

NodBase = { NodConYard, NodNuke, NodHQ }

StrikeTeamSafe = false
RefineryGoal = 3
StartingCashBonus = { easy = 1500, normal = 1000, hard = 0 }

NodBaseDiscovered = function(_, discoverer)
	if not Nod.IsObjectiveCompleted(FindBase) and discoverer == Nod then
		Nod.MarkCompletedObjective(FindBase)

		Utils.Do(NodBase, function(actor)
			if not actor.IsDead then
				actor.Owner = Nod
			end
		end)

		BaseTakenOver = true
		StrikeTeamSafe = true

		-- GDI harvesters were stopped in WorldLoaded so they cannot wander
		-- into the strike team's route before the base is found.
		Utils.Do(GDI.GetActorsByType("harv"), function(harv)
			harv.FindResources()
		end)

		TryStartGDIProduction()
		StartAttackSchedule()

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Nod, "NewOptions")
		end)
	end
end

TechCenterCaptured = function(_, _, _, newOwner)
	if not Nod.IsObjectiveCompleted(CaptureTechCenter) and newOwner == Nod then
		Nod.MarkCompletedObjective(CaptureTechCenter)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Media.PlaySpeechNotification(Nod, "NewOptions")
		end)
	end
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	-- Start at the southern artillery group: that force has to clear its
	-- side of the river before the strike team in the north moves in.
	Camera.Position = Map.CenterOfCell(CPos.New(57, 57))

	-- The original mission marks the strike team rendezvous with a flare at
	-- (13,40). It is Neutral so that the flare itself cannot trigger the
	-- rendezvous proximity check.
	Actor.Create("flare", true, { Owner = Player.GetPlayer("Neutral"), Location = DefaultFlareLocation.Location })

	InitObjectives(Nod)

	FindBase = AddPrimaryObjective(Nod, "find-nod-base")
	CaptureTechCenter = AddSecondaryObjective(Nod, "capture-tech-center")
	BuildRefineries = AddSecondaryObjective(Nod, "build-refineries")
	BuildSAMs = AddSecondaryObjective(Nod, UserInterface.GetFluentMessage("build-sams", { ["sams"] = 3 }))
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")

	Trigger.OnDiscovered(NodConYard, NodBaseDiscovered)

	-- Fallback for the discovery trigger: walking a Nod unit up to the yard is
	-- enough even if the shroud-based discovery notification does not fire.
	Trigger.OnEnteredProximityTrigger(NodConYard.CenterPosition, WDist.FromCells(6), function(actor, id)
		if actor.Owner == Nod and actor.HasProperty("Move") then
			Trigger.RemoveProximityTrigger(id)
			NodBaseDiscovered(NodConYard, Nod)
		end
	end)

	Trigger.OnCapture(TechCenter, TechCenterCaptured)

	-- The strike team and the southern artillery group hold position until the
	-- player takes control, so they do not wander off or reveal themselves.
	Utils.Do(StrikeTeam, function(unit)
		if not unit.IsDead then
			unit.Stance = "Defend"
		end
	end)
	Utils.Do(Nod.GetActorsByType("arty"), function(arty)
		arty.Stance = "Defend"
	end)

	-- GDI harvesters wait until the Nod base is found (11a behaviour). This
	-- also keeps them from walking into the strike team before the player has
	-- reached the base.
	Utils.Do(GDI.GetActorsByType("harv"), function(harv)
		harv.Stop()
	end)

	local difficulty = Difficulty or "normal"

	Nod.Cash = Nod.Cash + StartingCashBonus[difficulty]

	Utils.Do(Nod.GetActorsByType("arty"), function(arty)
		arty.GrantCondition("difficulty-" .. difficulty)
	end)

	local boatType = "BOAT"
	if difficulty == "easy" then
		boatType = "BOAT.easy"
	end

	SpawnBoat = function()
		Boat = Actor.Create(boatType, true, { Owner = GDI, Location = BoatLocation.Location, Facing = Angle.North })
		return Boat
	end

	local firstBoat = SpawnBoat()

	-- Hard only: a single replacement gunboat 20 minutes after the first one
	-- was destroyed (independent of the stopped waves).
	if difficulty == "hard" then
		Trigger.OnKilled(firstBoat, function()
			Trigger.AfterDelay(DateTime.Minutes(20), function()
				if not GDI.HasNoRequiredUnits() then
					SpawnBoat()
				end
			end)
		end)
	end

	SetupAI()
end

CheckForRefineries = function()
	return #Nod.GetActorsByType("proc") >= RefineryGoal
end

Tick = function()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(EliminateGDI)
	end

	if DateTime.GameTime > DateTime.Seconds(2) and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(EliminateGDI)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 then
		if not Nod.IsObjectiveCompleted(BuildRefineries) and CheckForRefineries() then
			Nod.MarkCompletedObjective(BuildRefineries)
		end

		if not Nod.IsObjectiveCompleted(BuildSAMs) and CheckForSams() then
			Nod.MarkCompletedObjective(BuildSAMs)
		end
	end
end
