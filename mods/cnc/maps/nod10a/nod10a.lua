--[[
	Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
	This file is part of OpenRA, which is free software. It is made
	available to you under the terms of the GNU General Public License
	as published by the Free Software Foundation, either version 3 of
	the License, or (at your option) any later version. For more
	information, see COPYING.
]]
if Map.LobbyOption("difficulty") == "easy" then
	Rambo = "rmbo.easy"
elseif Map.LobbyOption("difficulty") == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

GDIBuildings = { ConYard, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4,
Barracks, CommCenter, WeaponsFactory, GuardTower1, GuardTower2, GuardTower3 }


function RepairBuilding(building, attacker)
	if not building.IsDead and building.Owner == enemy then
		building.StartBuildingRepairs(enemy)
	end
end


ChinookTrigger = false


function ReinforceWithChinook(_, discoverer)
	if not ChinookTrigger and discoverer == player then
		ChinookTrigger = true

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			TransportFlare = Actor.Create('flare', true, { Owner = player, Location = DefaultFlareLocation.Location })
			Media.PlaySpeechNotification(player, "Reinforce")
			Reinforcements.ReinforceWithTransport(player, 'tran', nil, { ChinookEntry.Location, ChinookTarget.Location })
		end)
	end
end


function CreateScientist()
	scientist = Actor.Create('CHAN', true, { Owner = enemy, Location = ScientistLocation.Location })
	killScientistObjective = player.AddPrimaryObjective("Kill the GDI scientist.")
	Trigger.OnKilled(scientist, function()
		player.MarkCompletedObjective(killScientistObjective)
	end)
	player.MarkCompletedObjective(destroyTechCenterObjective)
end


function WorldLoaded()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	enemy.Cash = 10000

	Camera.Position = DefaultCameraPosition.CenterPosition

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

	Utils.Do(GDIBuildings, function(building)
		Trigger.OnDamaged(building, RepairBuilding)
	end)

	gdiObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	destroyTechCenterObjective = player.AddPrimaryObjective("Destroy the GDI R&D center.")

	Actor.Create(Rambo, true, { Owner = player, Location = RamboLocation.Location })

	Trigger.OnDiscovered(TechCenter, ReinforceWithChinook)

	Trigger.OnKilled(TechCenter, CreateScientist)
end


function Tick()
	if DateTime.GameTime > 2 then
		if player.HasNoRequiredUnits() then
			enemy.MarkCompletedObjective(gdiObjective)
		end
	end
end
