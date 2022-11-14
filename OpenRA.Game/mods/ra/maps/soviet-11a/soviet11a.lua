--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
StartingUnits = { "mcv", "e4", "e4", "e2", "e2" }
SovietTransportWay = { TransportEntry.Location, TransportStop.Location }

InitialSovietReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Reinforcements.ReinforceWithTransport(USSR, "lst", StartingUnits, SovietTransportWay)
	end)
end

Tick = function()
	Greece.Cash = 10000

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(DestroyNavalBase)
	end

	if USSR.HasNoRequiredUnits() and DateTime.GameTime > DateTime.Seconds(5) then
		Greece.MarkCompletedObjective(BeatSoviets)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	InitObjectives(USSR)

	DestroyNavalBase = USSR.AddPrimaryObjective("Destroy all Allied units and structures.")
	BeatSoviets = Greece.AddPrimaryObjective("Destroy all Soviet troops.")

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitialSovietReinforcements()
	Trigger.AfterDelay(DateTime.Seconds(5), ActivateAI)
end
