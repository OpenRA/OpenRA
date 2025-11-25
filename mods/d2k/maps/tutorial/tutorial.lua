--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--[[
   Dune 2000 Tutorial
   Teaches basic RTS mechanics step by step
]]

-- Objective tracking
CurrentObjective = 0
ObjectiveCompleted = {}

-- Resource tracking for harvesting objective
HarvestingStartResources = 0
HarvestingGoal = 700

-- Time tracking for timed objectives
ObjectiveStartTime = 0

-- Player and enemy references
Atreides = nil
Harkonnen = nil

-- Enemy units (for passive behavior)
EnemyUnits = {}

-- Named actors from map.yaml
PlayerMCVActor = nil
PlayerInfantry = {}
EnemyActors = {}

-- Mentat speaker (from fluent)
Mentat = nil

-- Tick function - runs every game tick
Tick = function()
	CheckObjectiveCompletion()
end

-- Called when map loads
WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")

	-- Get localized Mentat string
	Mentat = UserInterface.GetFluentMessage("mentat")

	-- Center camera on player's MCV (named actor from map.yaml)
	Camera.Position = PlayerMCV.CenterPosition

	-- Store enemy units for passive behavior
	EnemyUnits = Harkonnen.GetActorsByTypes({ "light_inf", "trike", "combat_tank_h" })

	-- Make enemy units hold position (passive) and set up damage triggers
	for _, unit in ipairs(EnemyUnits) do
		if unit.HasProperty("Stop") then
			unit.Stop()
		end
		if unit.HasProperty("Stance") then
			unit.Stance = "HoldFire"
		end
		-- Set up trigger for when this enemy unit is attacked
		Trigger.OnDamaged(unit, EnemyAttacked)
	end

	-- Set up objective completion handlers
	Trigger.OnObjectiveCompleted(Atreides, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective Completed")
	end)

	Trigger.OnPlayerWon(Atreides, function()
		Media.PlaySpeechNotification(Atreides, "Win")
	end)

	Trigger.OnPlayerLost(Atreides, function()
		Media.PlaySpeechNotification(Atreides, "Lose")
	end)

	-- Start first objective after brief delay
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		StartObjective(1)
	end)
end

-- Called when any enemy unit takes damage
EnemyAttacked = function(self, attacker)
	-- Wake up all enemy units - they now fight back
	for _, unit in ipairs(EnemyUnits) do
		if not unit.IsDead then
			if unit.HasProperty("Stance") then
				unit.Stance = "AttackAnything"
			end
			if unit.HasProperty("Hunt") then
				unit.Hunt()
			end
		end
	end
end

-- Start a new objective
StartObjective = function(objectiveNum)
	CurrentObjective = objectiveNum
	ObjectiveStartTime = DateTime.GameTime

	if objectiveNum == 1 then
		Objective1_CameraMovement()
	elseif objectiveNum == 2 then
		Objective2_UnitSelection()
	elseif objectiveNum == 3 then
		Objective3_UnitMovement()
	elseif objectiveNum == 4 then
		Objective4_ControlGroups()
	elseif objectiveNum == 5 then
		Objective5_DeployMCV()
	elseif objectiveNum == 6 then
		Objective6_PlaceConcrete()
	elseif objectiveNum == 7 then
		Objective7_BuildPower()
	elseif objectiveNum == 8 then
		Objective8_BuildRefinery()
	elseif objectiveNum == 9 then
		Objective9_Harvesting()
	elseif objectiveNum == 10 then
		Objective10_BuildBarracks()
	elseif objectiveNum == 11 then
		Objective11_TrainInfantry()
	elseif objectiveNum == 12 then
		Objective12_BuildLightFactory()
	elseif objectiveNum == 13 then
		Objective13_BuildVehicles()
	elseif objectiveNum == 14 then
		Objective14_Combat()
	elseif objectiveNum == 15 then
		Objective15_Victory()
	end
end

-- Complete current objective and move to next
CompleteObjective = function()
	ObjectiveCompleted[CurrentObjective] = true

	-- Brief delay before next objective
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		StartObjective(CurrentObjective + 1)
	end)
end

-- Check if current objective conditions are met
CheckObjectiveCompletion = function()
	if ObjectiveCompleted[CurrentObjective] then
		return
	end

	if CurrentObjective == 1 then
		Check1_CameraMovement()
	elseif CurrentObjective == 2 then
		Check2_UnitSelection()
	elseif CurrentObjective == 3 then
		Check3_UnitMovement()
	elseif CurrentObjective == 5 then
		Check5_DeployMCV()
	elseif CurrentObjective == 6 then
		Check6_PlaceConcrete()
	elseif CurrentObjective == 7 then
		Check7_BuildPower()
	elseif CurrentObjective == 8 then
		Check8_BuildRefinery()
	elseif CurrentObjective == 9 then
		Check9_Harvesting()
	elseif CurrentObjective == 10 then
		Check10_BuildBarracks()
	elseif CurrentObjective == 11 then
		Check11_TrainInfantry()
	elseif CurrentObjective == 12 then
		Check12_BuildLightFactory()
	elseif CurrentObjective == 13 then
		Check13_BuildVehicles()
	elseif CurrentObjective == 14 then
		Check14_Combat()
	end
end

--============================================================================
-- OBJECTIVE 1: Camera Movement
--============================================================================
Objective1_CameraMovement = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-welcome"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-camera-scroll"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-camera-home"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-camera"))
	end)
end

Check1_CameraMovement = function()
	-- Complete after player has had time to practice (10 seconds)
	if DateTime.GameTime - ObjectiveStartTime > DateTime.Seconds(10) then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-camera-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 2: Unit Selection
--============================================================================
Objective2_UnitSelection = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-select-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-select-click"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-select-drag"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-select"))
	end)
end

Check2_UnitSelection = function()
	-- Complete after player has had time to practice (12 seconds)
	if DateTime.GameTime - ObjectiveStartTime > DateTime.Seconds(12) then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-select-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 3: Unit Movement
--============================================================================
Objective3_UnitMovement = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-move-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-move-practice"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-move"))
	end)

	-- Auto-complete after time since movement detection is complex
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		if not ObjectiveCompleted[3] then
			Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-move-complete"), Mentat)
			CompleteObjective()
		end
	end)
end

Check3_UnitMovement = function()
	-- Time-based completion handled in objective function
end

--============================================================================
-- OBJECTIVE 4: Control Groups
--============================================================================
Objective4_ControlGroups = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-control-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-control-create"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-control-select"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-control"))
	end)

	-- Auto-complete after time since we can't detect control group creation
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		if not ObjectiveCompleted[4] then
			Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-control-complete"), Mentat)
			CompleteObjective()
		end
	end)
end

--============================================================================
-- OBJECTIVE 5: Deploy MCV
--============================================================================
Objective5_DeployMCV = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-deploy"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-creates"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-mcv"))
	end)
end

Check5_DeployMCV = function()
	local conyards = Atreides.GetActorsByType("construction_yard")
	if #conyards > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-mcv-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 6: Place Concrete Slabs
--============================================================================
Objective6_PlaceConcrete = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-concrete-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-concrete-protect"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-concrete-click"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-concrete-place"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-concrete"))
	end)

	-- Auto-complete after time since concrete actors self-destruct into terrain
	Trigger.AfterDelay(DateTime.Seconds(18), function()
		if not ObjectiveCompleted[6] then
			Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-concrete-complete"), Mentat)
			CompleteObjective()
		end
	end)
end

Check6_PlaceConcrete = function()
	-- Time-based completion handled in objective function
	-- (Concrete actors self-destruct and become terrain, so we can't count them)
end

--============================================================================
-- OBJECTIVE 7: Build Wind Trap (Power)
--============================================================================
Objective7_BuildPower = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-power-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-power-click"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-power-place"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-power"))
	end)
end

Check7_BuildPower = function()
	local windtraps = Atreides.GetActorsByType("wind_trap")
	if #windtraps > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-power-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 8: Build Refinery
--============================================================================
Objective8_BuildRefinery = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-refinery-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-refinery-build"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-refinery-spice"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-refinery"))
	end)
end

Check8_BuildRefinery = function()
	local refineries = Atreides.GetActorsByType("refinery")
	if #refineries > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-refinery-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 9: Harvesting
--============================================================================
Objective9_Harvesting = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-harvest-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-harvest-fields"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-harvest-wait"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-harvest"))

		-- Record starting resources
		HarvestingStartResources = Atreides.Resources
	end)
end

Check9_Harvesting = function()
	local gained = Atreides.Resources - HarvestingStartResources
	if gained >= HarvestingGoal then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-harvest-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 10: Build Barracks
--============================================================================
Objective10_BuildBarracks = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-barracks-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-barracks-build"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-barracks"))
	end)
end

Check10_BuildBarracks = function()
	local barracks = Atreides.GetActorsByType("barracks")
	if #barracks > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-barracks-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 11: Train Infantry
--============================================================================
Objective11_TrainInfantry = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-infantry-click"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-infantry-train"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-infantry"))
	end)
end

Check11_TrainInfantry = function()
	local infantry = Atreides.GetActorsByType("light_inf")
	-- Player started with 3, need 4 more = 7 total
	if #infantry >= 7 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-infantry-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 12: Build Light Factory
--============================================================================
Objective12_BuildLightFactory = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-factory-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-factory-build"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-factory"))
	end)
end

Check12_BuildLightFactory = function()
	local factories = Atreides.GetActorsByType("light_factory")
	if #factories > 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-factory-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 13: Build Vehicles
--============================================================================
Objective13_BuildVehicles = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-vehicle-intro"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-vehicle-build"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-vehicle"))
	end)
end

Check13_BuildVehicles = function()
	local trikes = Atreides.GetActorsByType("trike")
	if #trikes >= 3 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-vehicle-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 14: Combat
--============================================================================
Objective14_Combat = function()
	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-combat-ready"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-combat-enemy"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-combat-select"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-combat-attack"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-combat"))
	end)
end

Check14_Combat = function()
	-- Check if all enemy combat units are destroyed
	local enemyUnits = Harkonnen.GetActorsByTypes({ "light_inf", "trike", "combat_tank_h" })

	if #enemyUnits == 0 then
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-combat-complete"), Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 15: Victory
--============================================================================
TutorialObjective = nil

Objective15_Victory = function()
	-- Add the mission objective for win condition
	TutorialObjective = Atreides.AddObjective(UserInterface.GetFluentMessage("tutorial-complete-objective"))

	Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-victory"), Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-victory-basics"), Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("tutorial-victory-next"), Mentat)
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("tutorial-mission-text-victory"))
	end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Atreides.MarkCompletedObjective(TutorialObjective)
	end)
end
