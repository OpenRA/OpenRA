--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
InitialForcesA = { "bggy", "e1", "e1", "e1", "e1" }
InitialForcesB = { "e1", "e1", "bggy", "e1", "e1" }

RifleInfantryReinforcements = { "e1", "e1" }
RocketInfantryReinforcements = { "e3", "e3", "e3", "e3", "e3" }

SendInitialForces = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, InitialForcesA, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 5)
	Reinforcements.Reinforce(player, InitialForcesB, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 10)
end

SendFirstInfantryReinforcements = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, RifleInfantryReinforcements, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 15)
end

SendSecondInfantryReinforcements = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, RifleInfantryReinforcements, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 15)
end

SendLastInfantryReinforcements = function()
	Media.PlaySpeechNotification(player, "Reinforce")

	-- Move the units properly into the map before they start attacking
	local forces = Reinforcements.Reinforce(player, RocketInfantryReinforcements, { VillageSpawnPoint.Location, VillageRallyPoint.Location }, 8)
	Utils.Do(forces, function(a)
		a.Stance = "Defend"
		a.CallFunc(function() a.Stance = "AttackAnything" end)
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")
	villagers = Player.GetPlayer("Villagers")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	NodObjective1 = player.AddPrimaryObjective("Kill Nikoomba.")
	NodObjective2 = player.AddPrimaryObjective("Destroy the village.")
	NodObjective3 = player.AddSecondaryObjective("Destroy all GDI troops in the area.")
	GDIObjective1 = enemy.AddPrimaryObjective("Eliminate all Nod forces.")

	Trigger.OnKilled(Nikoomba, function()
		player.MarkCompletedObjective(NodObjective1)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			SendLastInfantryReinforcements()
		end)
	end)

	Camera.Position = StartRallyPoint.CenterPosition

	SendInitialForces()
	Trigger.AfterDelay(DateTime.Seconds(30), SendFirstInfantryReinforcements)
	Trigger.AfterDelay(DateTime.Seconds(60), SendSecondInfantryReinforcements)
end

Tick = function()
	if DateTime.GameTime > 2 then
		if player.HasNoRequiredUnits() then
			enemy.MarkCompletedObjective(GDIObjective1)
		end
		if villagers.HasNoRequiredUnits() then
			player.MarkCompletedObjective(NodObjective2)
		end
		if enemy.HasNoRequiredUnits() then
			player.MarkCompletedObjective(NodObjective3)
		end
	end
end
