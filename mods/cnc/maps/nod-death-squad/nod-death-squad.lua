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
WinObjective = nil

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")

	if Nod == nil or GDI == nil or Neutral == nil then
		return
	end

	InitObjectives(Nod)
	Nod.Cash = 0
	GDI.Cash = 1900

	WinObjective = AddPrimaryObjective(Nod, "destroy-advanced-comm-center")

	-- The Nod squad is a fixed raiding force. Keep the Stealth Tanks on their
	-- HoldFire default so they do not uncloak at the first sighting.
	Utils.Do(Nod.GetGroundAttackers(), function(unit)
		if unit.Type ~= "stnk" and unit.HasProperty("Stance") then
			unit.Stance = "Defend"
		end
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition

	SetupWinCondition()
	InitGDI()
	CheckObjectives()
end

SetupWinCondition = function()
	-- Original `win`: Destroyed on the EYE (structure 019), Persist 0.
	if not AI.IsLive(Eye) then
		return
	end

	Trigger.OnKilled(Eye, function()
		if MissionFinished then
			return
		end

		MissionFinished = true
		Nod.MarkCompletedObjective(WinObjective)
	end)
end

-- Original `lose`: All Destr. for BadGuy. Nod has no buildings, so this is
-- the nine starting units. STNK gets MustBeDestroyed back in rules.yaml.
CheckObjectives = function()
	if MissionFinished then
		return
	end

	if Nod.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkFailedObjective(WinObjective)
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(2), CheckObjectives)
end
