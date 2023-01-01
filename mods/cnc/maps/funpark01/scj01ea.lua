--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

RifleReinforcments = { "e1", "e1", "e1", "bike" }
BazookaReinforcments = { "e3", "e3", "e3", "bike" }
BikeReinforcments = { "bike" }

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	Dinosaur = Player.GetPlayer("Dinosaur")
	Civilian = Player.GetPlayer("Civilian")

	InvestigateObj = AddPrimaryObjective(Nod, "investigate-village")

	InitObjectives(Nod)

	ReachVillageObj = AddPrimaryObjective(Nod, "reach-village")

	Trigger.OnPlayerDiscovered(Civilian, function(_, discoverer)
		if discoverer == Nod and not Nod.IsObjectiveCompleted(ReachVillageObj) then
			if not Dinosaur.HasNoRequiredUnits() then
				KillDinos = AddPrimaryObjective(Nod, "kill-creatures")
			end

			Nod.MarkCompletedObjective(ReachVillageObj)
		end
	end)

	DinoTric.Patrol({ WP0.Location, WP1.Location }, true, 3)
	Trigger.OnDamaged(DinoTric, function()
		DinoTric.Stop()
		IdleHunt(DinoTric)
	end)

	DinoTrex.AttackMove(WP2.Location)
	DinoTrex.AttackMove(WP3.Location)
	IdleHunt(DinoTrex)

	ReinforceWithLandingCraft(Nod, RifleReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location)
	Trigger.AfterDelay(DateTime.Seconds(3), function() InitialUnitsArrived = true end)

	Trigger.AfterDelay(DateTime.Seconds(15), function() ReinforceWithLandingCraft(Nod, BazookaReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	if Difficulty == "easy" then
		Trigger.AfterDelay(DateTime.Seconds(25), function() ReinforceWithLandingCraft(Nod, BikeReinforcments, SeaEntryA.Location, BeachReinforceA.Location, BeachReinforceA.Location) end)
		Trigger.AfterDelay(DateTime.Seconds(30), function() ReinforceWithLandingCraft(Nod, BikeReinforcments, SeaEntryB.Location, BeachReinforceB.Location, BeachReinforceB.Location) end)
	end

	Camera.Position = CameraStart.CenterPosition
end

Tick = function()
	if InitialUnitsArrived then
		if Nod.HasNoRequiredUnits() then
			Nod.MarkFailedObjective(InvestigateObj)
		end

		if Dinosaur.HasNoRequiredUnits() then
			if KillDinos then Nod.MarkCompletedObjective(KillDinos) end
			Nod.MarkCompletedObjective(InvestigateObj)
		end
	end
end
