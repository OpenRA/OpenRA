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

-- Mentat speaker
Mentat = "Mentat"

-- Tick function - runs every game tick
Tick = function()
	CheckObjectiveCompletion()
end

-- Called when map loads
WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")

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
		Objective6_BuildPower()
	elseif objectiveNum == 7 then
		Objective7_BuildRefinery()
	elseif objectiveNum == 8 then
		Objective8_Harvesting()
	elseif objectiveNum == 9 then
		Objective9_BuildBarracks()
	elseif objectiveNum == 10 then
		Objective10_TrainInfantry()
	elseif objectiveNum == 11 then
		Objective11_BuildLightFactory()
	elseif objectiveNum == 12 then
		Objective12_BuildVehicles()
	elseif objectiveNum == 13 then
		Objective13_Combat()
	elseif objectiveNum == 14 then
		Objective14_Victory()
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
		Check6_BuildPower()
	elseif CurrentObjective == 7 then
		Check7_BuildRefinery()
	elseif CurrentObjective == 8 then
		Check8_Harvesting()
	elseif CurrentObjective == 9 then
		Check9_BuildBarracks()
	elseif CurrentObjective == 10 then
		Check10_TrainInfantry()
	elseif CurrentObjective == 11 then
		Check11_BuildLightFactory()
	elseif CurrentObjective == 12 then
		Check12_BuildVehicles()
	elseif CurrentObjective == 13 then
		Check13_Combat()
	end
end

--============================================================================
-- OBJECTIVE 1: Camera Movement
--============================================================================
Objective1_CameraMovement = function()
	Media.DisplayMessage("Welcome to Arrakis, Commander!", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Use the ARROW KEYS to scroll around the map.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Press H to quickly jump back to your base.", Mentat)
		UserInterface.SetMissionText("Scroll around with Arrow Keys, then press H")
	end)
end

Check1_CameraMovement = function()
	-- Complete after player has had time to practice (15 seconds)
	if DateTime.GameTime - ObjectiveStartTime > DateTime.Seconds(15) then
		Media.DisplayMessage("Good! You've learned to navigate the battlefield.", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 2: Unit Selection
--============================================================================
Objective2_UnitSelection = function()
	Media.DisplayMessage("Now let's learn to select units.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.DisplayMessage("LEFT-CLICK on a unit to select it.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.DisplayMessage("CLICK and DRAG to select multiple units.", Mentat)
		UserInterface.SetMissionText("Select your Light Infantry units")
	end)
end

Check2_UnitSelection = function()
	-- Complete after player has had time to practice (12 seconds)
	if DateTime.GameTime - ObjectiveStartTime > DateTime.Seconds(12) then
		Media.DisplayMessage("Excellent! You can now select your troops.", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 3: Unit Movement
--============================================================================
Objective3_UnitMovement = function()
	Media.DisplayMessage("With units selected, RIGHT-CLICK to move them.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Move your infantry to the marked rally point.", Mentat)
		UserInterface.SetMissionText("Move your units to the rally point (near 15,20)")
	end)
end

Check3_UnitMovement = function()
	-- Check if any player infantry is near rally point (15,20)
	local infantry = Atreides.GetActorsByType("light_inf")
	local rallyPoint = CPos.New(15, 20)

	for _, unit in ipairs(infantry) do
		if not unit.IsDead then
			local dist = (unit.Location - rallyPoint).Length
			if dist < 6 then
				Media.DisplayMessage("Well done! Your units followed orders.", Mentat)
				CompleteObjective()
				return
			end
		end
	end
end

--============================================================================
-- OBJECTIVE 4: Control Groups
--============================================================================
Objective4_ControlGroups = function()
	Media.DisplayMessage("Control groups let you quickly select units.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Select units, then press CTRL+1 to assign group 1.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Press 1 to reselect that group anytime!", Mentat)
		UserInterface.SetMissionText("Create control group 1 with your infantry (Ctrl+1)")
	end)

	-- Auto-complete after time since we can't detect control group creation
	Trigger.AfterDelay(DateTime.Seconds(12), function()
		if not ObjectiveCompleted[4] then
			Media.DisplayMessage("Control groups will be essential in battle.", Mentat)
			CompleteObjective()
		end
	end)
end

--============================================================================
-- OBJECTIVE 5: Deploy MCV
--============================================================================
Objective5_DeployMCV = function()
	Media.DisplayMessage("Your MCV (Mobile Construction Vehicle) is your mobile base.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Select the MCV and press D to deploy it.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("This creates your Construction Yard!", Mentat)
		UserInterface.SetMissionText("Deploy your MCV (select it, press D)")
	end)
end

Check5_DeployMCV = function()
	local conyards = Atreides.GetActorsByType("construction_yard")
	if #conyards > 0 then
		Media.DisplayMessage("Your Construction Yard is ready!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 6: Build Wind Trap (Power)
--============================================================================
Objective6_BuildPower = function()
	Media.DisplayMessage("Excellent! Now you can construct buildings.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("TIP: Place concrete slabs before buildings to prevent damage!", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Click the Wind Trap icon in the sidebar to build power.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.DisplayMessage("Then left-click on the map to place it near your base.", Mentat)
		UserInterface.SetMissionText("Build a Wind Trap for power")
	end)
end

Check6_BuildPower = function()
	local windtraps = Atreides.GetActorsByType("wind_trap")
	if #windtraps > 0 then
		Media.DisplayMessage("Power is flowing to your base!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 7: Build Refinery
--============================================================================
Objective7_BuildRefinery = function()
	Media.DisplayMessage("Wind Traps provide power for your base.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Now build a Refinery to collect Spice!", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Spice is the source of all credits on Arrakis.", Mentat)
		UserInterface.SetMissionText("Build a Refinery")
	end)
end

Check7_BuildRefinery = function()
	local refineries = Atreides.GetActorsByType("refinery")
	if #refineries > 0 then
		Media.DisplayMessage("Your Refinery is operational!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 8: Harvesting
--============================================================================
Objective8_Harvesting = function()
	Media.DisplayMessage("Your Harvester will automatically collect Spice.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("The orange Spice fields are your source of income.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Wait for your Harvester to deliver a load of Spice.", Mentat)
		UserInterface.SetMissionText("Wait for Harvester to collect Spice")

		-- Record starting resources
		HarvestingStartResources = Atreides.Resources
	end)
end

Check8_Harvesting = function()
	local gained = Atreides.Resources - HarvestingStartResources
	if gained >= HarvestingGoal then
		Media.DisplayMessage("Credits are flowing in! You can now build more.", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 9: Build Barracks
--============================================================================
Objective9_BuildBarracks = function()
	Media.DisplayMessage("To train soldiers, you need a Barracks.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Build a Barracks from the sidebar.", Mentat)
		UserInterface.SetMissionText("Build a Barracks")
	end)
end

Check9_BuildBarracks = function()
	local barracks = Atreides.GetActorsByType("barracks")
	if #barracks > 0 then
		Media.DisplayMessage("Your Barracks is ready to train infantry!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 10: Train Infantry
--============================================================================
Objective10_TrainInfantry = function()
	Media.DisplayMessage("Click the Light Infantry icon to start training.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Train at least 4 Light Infantry soldiers.", Mentat)
		UserInterface.SetMissionText("Train 4 Light Infantry")
	end)
end

Check10_TrainInfantry = function()
	local infantry = Atreides.GetActorsByType("light_inf")
	-- Player started with 3, need 4 more = 7 total
	if #infantry >= 7 then
		Media.DisplayMessage("Your squad is growing stronger!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 11: Build Light Factory
--============================================================================
Objective11_BuildLightFactory = function()
	Media.DisplayMessage("Infantry alone won't win battles.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Build a Light Factory for vehicles!", Mentat)
		UserInterface.SetMissionText("Build a Light Factory")
	end)
end

Check11_BuildLightFactory = function()
	local factories = Atreides.GetActorsByType("light_factory")
	if #factories > 0 then
		Media.DisplayMessage("Vehicle production is now available!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 12: Build Vehicles
--============================================================================
Objective12_BuildVehicles = function()
	Media.DisplayMessage("Trikes are fast scout vehicles.", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("Build at least 3 Trikes for your army.", Mentat)
		UserInterface.SetMissionText("Build 3 Trikes")
	end)
end

Check12_BuildVehicles = function()
	local trikes = Atreides.GetActorsByType("trike")
	if #trikes >= 3 then
		Media.DisplayMessage("Your mechanized force is ready!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 13: Combat
--============================================================================
Objective13_Combat = function()
	Media.DisplayMessage("Your army is ready, Commander!", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("The Harkonnen have forces to the southeast.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Select your combat units and attack!", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.DisplayMessage("Right-click on enemies to attack them.", Mentat)
		UserInterface.SetMissionText("Destroy the Harkonnen forces!")
	end)
end

Check13_Combat = function()
	-- Check if all enemy combat units are destroyed
	local enemyUnits = Harkonnen.GetActorsByTypes({ "light_inf", "trike", "combat_tank_h" })

	if #enemyUnits == 0 then
		Media.DisplayMessage("The Harkonnen forces have been destroyed!", Mentat)
		CompleteObjective()
	end
end

--============================================================================
-- OBJECTIVE 14: Victory
--============================================================================
TutorialObjective = nil

Objective14_Victory = function()
	-- Add the mission objective for win condition
	TutorialObjective = Atreides.AddObjective("Complete the tutorial")

	Media.DisplayMessage("VICTORY! You have completed the tutorial!", Mentat)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.DisplayMessage("You now know the basics of Dune 2000.", Mentat)
	end)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Media.DisplayMessage("Try a Skirmish or Campaign mission next!", Mentat)
		UserInterface.SetMissionText("Tutorial Complete!")
	end)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Atreides.MarkCompletedObjective(TutorialObjective)
	end)
end
