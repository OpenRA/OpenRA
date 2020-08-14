--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

TruckStops = { TruckStop1, TruckStop2, TruckStop3, TruckStop4, TruckStop5, TruckStop6, TruckStop7, TruckStop8 }
MissionStartAttackUnits = { StartAttack1tnk1, StartAttack1tnk2, StartAttackArty1, StartAttackArty2, StartAttackArty3 }
TruckEscape = { TruckEscape1, TruckEscape2, TruckEscape3, TruckEscape4, TruckEscape5, TruckEscapeWest }
BackupRoute = { TruckEscape2, TruckEscape1, TruckEscapeEast }

MissionStart = function()
	Utils.Do(TruckStops, function(waypoint)
		StolenTruck.Move(waypoint.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Utils.Do(MissionStartAttackUnits, function(actor)
			actor.AttackMove(DefaultCameraPosition.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(45), function()
		Media.DisplayMessage("Commander, the truck has stopped at a nearby allied base.\nAllied radio intercepts say the truck has orders to flee the battlefield\nif any Soviet units approach the base.")
	end)

	Trigger.OnKilled(StolenTruck, function()
		USSR.MarkCompletedObjective(DestroyTruck)
		USSR.MarkCompletedObjective(DefendCommand)
	end)

	Trigger.OnKilled(CommandCenter, function()
		USSR.MarkFailedObjective(DefendCommand)
	end)
end

Trigger.OnEnteredProximityTrigger(TruckAlarm.CenterPosition, WDist.FromCells(11), function(actor, triggerflee)
	if actor.Owner == USSR and actor.Type ~= "badr" and actor.Type ~= "u2" and actor.Type ~= "camera.spyplane" then
		Trigger.RemoveProximityTrigger(triggerflee)
		Media.DisplayMessage("The convoy truck is attempting to escape!")
		EscapeCamera = Actor.Create("camera", true, { Owner = USSR, Location = TruckAlarm.Location })
		Media.PlaySoundNotification(USSR, "AlertBleep")
		Utils.Do(TruckEscape, function(waypoint)
			StolenTruck.Move(waypoint.Location)
		end)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			EscapeCamera.Destroy()
		end)

		Trigger.OnIdle(StolenTruck, function()
			Utils.Do(BackupRoute, function(waypoint)
				StolenTruck.Move(waypoint.Location)
			end)
		end)
	end
end)

Trigger.OnEnteredFootprint(({ TruckEscapeWest.Location } or { TruckEscapeEast.Location }), function(actor, triggerlose)
	if actor.Owner == Greece and actor.Type == "truk" then
		Trigger.RemoveFootprintTrigger(triggerlose)
		actor.Destroy()
		USSR.MarkFailedObjective(DestroyTruck)
	end
end)

Tick = function()
	Greece.Cash = 50000
	Germany.Cash = 50000
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Germany = Player.GetPlayer("Germany")
	Greece = Player.GetPlayer("Greece")

	Trigger.OnObjectiveAdded(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	DestroyTruck = USSR.AddObjective("Destroy the stolen convoy truck.\nDo not let it escape.")
	DefendCommand = USSR.AddObjective("Defend our forward command center.")

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

	MissionStart()
	ActivateAI()
end
