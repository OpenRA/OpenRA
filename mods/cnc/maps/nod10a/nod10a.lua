--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "easy" then
	Rambo = "rmbo.easy"
elseif Difficulty == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

GDIBuildings = { ConYard, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, Barracks, CommCenter, WeaponsFactory, GuardTower1, GuardTower2, GuardTower3 }

ReinforceWithChinook = function(_, discoverer)
	if not ChinookTrigger and discoverer == Nod then
		ChinookTrigger = true

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Actor.Create("flare", true, { Owner = Nod, Location = DefaultFlareLocation.Location })
			Media.PlaySpeechNotification(Nod, "Reinforce")
			Reinforcements.ReinforceWithTransport(Nod, "tran", nil, { ChinookEntry.Location, ChinookTarget.Location })
		end)
	end
end

CreateScientist = function()
	local scientist = Actor.Create("CHAN", true, { Owner = GDI, Location = ScientistLocation.Location })

	KillScientistObjective = Nod.AddObjective("Kill the GDI scientist.")
	Nod.MarkCompletedObjective(DestroyTechCenterObjective)

	Trigger.OnKilled(scientist, function()
		Nod.MarkCompletedObjective(KillScientistObjective)
	end)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	GDI.Cash = 10000

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)

	Utils.Do(GDIBuildings, function(building)
		RepairBuilding(GDI, building, 0.75)
	end)

	DestroyTechCenterObjective = Nod.AddObjective("Destroy the GDI R&D center.")

	Actor.Create(Rambo, true, { Owner = Nod, Location = RamboLocation.Location })

	Trigger.OnDiscovered(TechCenter, ReinforceWithChinook)
	Trigger.OnKilled(TechCenter, CreateScientist)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(DestroyTechCenterObjective)
	end
end
