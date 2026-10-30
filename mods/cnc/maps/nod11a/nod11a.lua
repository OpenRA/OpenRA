--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Port of the original Nod mission 11a ("Ezekiel's Wheel", SCB11EA.INI).
--
-- Original win/lose conditions ([Triggers] in SCB11EA.INI):
--   win  = All Destr.,Win, 0,GoodGuy  -> all GDI forces destroyed
--   lose = All Destr.,Lose,0,BadGuy   -> all Nod forces destroyed
--
-- The scripted attacks, cell triggers and GDI production live in nod11a-AI.lua.
-- Original [Triggers] entries for reference:
--   grd1  Time,Create Team,  4,GoodGuy,gdi2   (Mammoth Tank patrol, TeamTypes)
--   chn1  Time,Create Team,  5,GoodGuy,gdi9   (Chinook drop)
--   atk2  Time,Create Team,150,GoodGuy,gdi6
--   atk3  Time,Create Team,160,GoodGuy,gdi7
--   zzzz  Time,Reinforce., 170,GoodGuy,gdi4
--   atk4  Time,Create Team,180,GoodGuy,gdi7
--   atk5  Time,Create Team,210,GoodGuy,gdi5
--   atk6  Time,Create Team,220,GoodGuy,gdi8
--   auto  Time,Autocreate, 270,GoodGuy
--   atk1  Player Enters -> Create Team gdi5  ([CellTriggers] cells)
--   prod  Player Enters -> Production
--   dzne  Player Enters -> destroy trigger 'zzzz' (dead trigger in the original)
--   dely  Player Enters -> destroy trigger 'yyyy'
--   yyyy  Destroyed   -> Lose
--   hunt  Destroyed   -> All to Hunt
-- Team compositions and waypoint paths are in [TeamTypes]; the waypoint
-- numbers there refer to the waypoint0..waypoint15 actors on this map.
-- The original splits the player's forces across a river; both groups are
-- imported separately.

NodBase = { NodConYard }

-- Extra starting cash for Nod on top of the 2000 from rules.yaml.
RefineryGoal = 3
StartingCashBonus = { easy = 1500, normal = 1000, hard = 0 }
BaseTakenOver = false

StartMammothPatrol = function()
	if Mammoth.IsDead then
		return
	end

	Mammoth.Patrol({ waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint2.Location }, true, DateTime.Seconds(1))
end

ConYardDiscovered = function(_, discoverer)
	if not Nod.IsObjectiveCompleted(FindBase) and discoverer == Nod then
		Nod.MarkCompletedObjective(FindBase)

		Utils.Do(NodBase, function(actor)
			if not actor.IsDead then
				actor.Owner = Nod
			end
		end)

		BaseTakenOver = true

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

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)

	FindBase = AddPrimaryObjective(Nod, "find-nod-base")
	CaptureTechCenter = AddSecondaryObjective(Nod, "capture-tech-center")
	BuildRefineries = AddSecondaryObjective(Nod, "build-refineries")
	BuildSAMs = AddSecondaryObjective(Nod, UserInterface.GetFluentMessage("build-sams", { ["sams"] = 3 }))
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")

	Trigger.OnDiscovered(NodConYard, ConYardDiscovered)

	-- Fallback for the discovery trigger: walking a Nod unit up to the yard is
	-- enough even if the shroud-based discovery notification does not fire.
	Trigger.OnEnteredProximityTrigger(NodConYard.CenterPosition, WDist.FromCells(6), function(actor, id)
		if actor.Owner == Nod then
			Trigger.RemoveProximityTrigger(id)
			ConYardDiscovered(NodConYard, Nod)
		end
	end)

	Trigger.OnCapture(TechCenter, TechCenterCaptured)

	Commando.Stance = "Defend"

	-- The harvester next to the Nod base only starts working once the base is
	-- taken over (see ConYardDiscovered), so it does not strip the nearby
	-- Tiberium before the player can use it.
	-- Harvesters search for Tiberium on creation by default, so the west one
	-- has to be stopped explicitly.
	-- All GDI harvesters wait until the base is taken over, so GDI cannot
	-- stockpile money before the player has found the base.
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
		Boat = Actor.Create(boatType, true, { Owner = GDI, Location = BoatLocation.Location, Facing = Angle.West })
		return Boat
	end

	local firstBoat = SpawnBoat()

	-- Hard only: a single replacement gunboat 20 minutes after the first one was destroyed
	-- (independent of the stopped waves; the GDI yard is usually dead by then).
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

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(BuildRefineries) and CheckForRefineries() then
		Nod.MarkCompletedObjective(BuildRefineries)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(BuildSAMs) and CheckForSams() then
		Nod.MarkCompletedObjective(BuildSAMs)
	end
end
