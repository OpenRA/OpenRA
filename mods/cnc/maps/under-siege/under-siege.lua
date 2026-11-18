--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local AI = CampaignAI

MissionFinished = false

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")

	if Nod == nil or GDI == nil or Neutral == nil then
		return
	end

	InitObjectives(Nod)
	Nod.Cash = 0
	GDI.Cash = 1000

	DestroyGDI = AddPrimaryObjective(Nod, "destroy-all-gdi")

	Utils.Do(Nod.GetGroundAttackers(), function(unit)
		if unit.HasProperty("Stance") then
			unit.Stance = "Defend"
		end
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition

	SetupChurchCrate()
	InitGDI()
	CheckObjectives()
end

-- The original WCRATE sits inside V01's 2x2 footprint and WCRATE is inert in
-- OpenRA. Follow Hostile Takeover / nod11a-b: remove the overlay crate and
-- spawn a real money crate at the original cell when the church dies.
SetupChurchCrate = function()
	if not AI.IsLive(Church) then
		return
	end

	Trigger.OnKilled(Church, function()
		if MissionFinished or ChurchCrateSpawned then
			return
		end

		ChurchCrateSpawned = true
		Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(37, 56) })
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("crate-found"))
	end)
end

CheckObjectives = function()
	if MissionFinished then
		return
	end

	if GDI.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkCompletedObjective(DestroyGDI)
		return
	end

	if Nod.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkFailedObjective(DestroyGDI)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end
