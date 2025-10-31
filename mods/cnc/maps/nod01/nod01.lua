--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, InitialForcesA, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 5)
	Reinforcements.Reinforce(Nod, InitialForcesB, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 10)
end

SendFirstInfantryReinforcements = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, RifleInfantryReinforcements, { StartSpawnPointRight.Location, StartRallyPoint.Location }, 15)
end

SendSecondInfantryReinforcements = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, RifleInfantryReinforcements, { StartSpawnPointLeft.Location, StartRallyPoint.Location }, 15)
end

SendLastInfantryReinforcements = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, RocketInfantryReinforcements, { VillageSpawnPoint.Location, VillageRallyPoint.Location }, 8)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Villagers = Player.GetPlayer("Villagers")

	InitObjectives(Nod)

	KillNikoomba = AddPrimaryObjective(Nod, "kill-nikoomba")
	DestroyVillage = AddPrimaryObjective(Nod, "destroy-village")
	DestroyGDI = AddSecondaryObjective(Nod, "destroy-gdi-troops-area")
	GDIObjective = AddPrimaryObjective(GDI, "")

	Trigger.OnKilled(Nikoomba, function()
		Nod.MarkCompletedObjective(KillNikoomba)
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
		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(GDIObjective)
		end

		if Villagers.HasNoRequiredUnits() then
			Nod.MarkCompletedObjective(DestroyVillage)
		end

		if GDI.HasNoRequiredUnits() then
			Nod.MarkCompletedObjective(DestroyGDI)
		end
	end
end
