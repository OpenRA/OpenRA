--[[
   Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
	
	Trigger.OnObjectiveAdded(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	DestroyNavalBase = USSR.AddPrimaryObjective("Destroy all Allied units and structures.")
	BeatSoviets = Greece.AddPrimaryObjective("Destroy all Soviet troops.")
	
	Trigger.OnObjectiveCompleted(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(USSR, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(USSR, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(USSR, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(USSR, "MissionAccomplished")
		end)
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitialSovietReinforcements()
	Trigger.AfterDelay(DateTime.Seconds(5), ActivateAI)
end
