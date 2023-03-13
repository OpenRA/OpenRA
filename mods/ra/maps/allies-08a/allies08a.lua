--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedBoatReinforcements = { "dd", "dd" }
TimerTicks = DateTime.Minutes(21)
ObjectiveBuildings = { Chronosphere, AlliedTechCenter }
ScientistTypes = { "chan", "chan", "chan", "chan" }
ScientistDiscoveryFootprint = { CPos.New(28, 83), CPos.New(29, 83) }
ScientistEvacuationFootprint = { CPos.New(29, 60), CPos.New(29, 61), CPos.New(29, 62), CPos.New(29, 63), CPos.New(29, 64), CPos.New(29, 65), CPos.New(29, 66) }

InitialAlliedReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.Reinforce(Greece, { "mcv" }, { MCVEntry.Location, MCVStop.Location })
		Reinforcements.Reinforce(Greece, AlliedBoatReinforcements, { DDEntry.Location, DDStop.Location })
	end)
end

CreateScientists = function()
	local scientists = Reinforcements.Reinforce(Greece, ScientistTypes, { ScientistsExit.Location })
	Utils.Do(scientists, function(s)
		s.Move(s.Location + CVec.New(0, 1))
		s.Scatter()
	end)

	local flare = Actor.Create("flare", true, { Owner = Greece, Location = DefaultCameraPosition.Location + CVec.New(-1, 0) })
	Trigger.AfterDelay(DateTime.Seconds(2), function() Media.PlaySpeechNotification(Greece, "SignalFlareNorth") end)

	Trigger.OnAnyKilled(scientists, function()
		Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
		Greece.MarkFailedObjective(EvacuateScientists)
	end)

	-- Add the footprint trigger in a frame end task (delay 0) to avoid crashes
	local left = #scientists
	Trigger.AfterDelay(0, function()
		local changeOwnerTrigger = Trigger.OnEnteredFootprint(ScientistEvacuationFootprint, function(a, id)
			if a.Owner == Greece and a.Type == "chan" then
				a.Owner = Germany
				a.Stop()
				a.Move(MCVEntry.Location)

				-- Constantly try to reach the exit (and thus avoid getting stuck if the path was blocked)
				Trigger.OnIdle(a, function()
					a.Move(MCVEntry.Location)
				end)
			end
		end)

		-- Use a cell trigger to destroy the scientists preventing the player from causing glitchs by blocking the path
		Trigger.OnEnteredFootprint({ MCVEntry.Location }, function(a, id)
			if a.Owner == Germany then
				a.Stop()
				a.Destroy()

				left = left - 1
				if left == 0 then
					Trigger.RemoveFootprintTrigger(id)
					Trigger.RemoveFootprintTrigger(changeOwnerTrigger)
					flare.Destroy()

					if not Greece.IsObjectiveCompleted(EvacuateScientists) and not Greece.IsObjectiveFailed(EvacuateScientists) then
						Media.PlaySpeechNotification(Greece, "ObjectiveMet")
						Greece.MarkCompletedObjective(EvacuateScientists)
					end
				end
			end
		end)
	end)
end

DefendChronosphereCompleted = function()
	local cells = Utils.ExpandFootprint({ ChronoshiftLocation.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("2tnk", true, { Owner = Greece, Facing = Angle.North })
		units[unit] = cells[i]
	end
	Chronosphere.Chronoshift(units)
	UserInterface.SetMissionText(UserInterface.Translate("experiment-successful"), Greece.Color)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Greece.MarkCompletedObjective(DefendChronosphere)
		Greece.MarkCompletedObjective(KeepBasePowered)
	end)
end

Ticked = TimerTicks
Tick = function()
	USSR.Cash = 5000

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(DefendChronosphere)
		Greece.MarkCompletedObjective(KeepBasePowered)
	end

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(BeatAllies)
	end

	if Ticked > 0 then
		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("chronosphere-experiments-completes-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 and (Greece.PowerState ~= "Normal") then
		Greece.MarkFailedObjective(KeepBasePowered)
	elseif Ticked == 0 then
		DefendChronosphereCompleted()
		Ticked = Ticked - 1
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Germany = Player.GetPlayer("Germany")

	InitObjectives(Greece)
	DefendChronosphere = AddPrimaryObjective(Greece, "defend-chronosphere-tech-center")
	KeepBasePowered = AddPrimaryObjective(Greece, "chronosphere-needs-power")
	EvacuateScientists = AddSecondaryObjective(Greece, "evacuate-scientists-from-island")
	BeatAllies = AddPrimaryObjective(USSR, "")

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(Greece, "TwentyMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(11), function()
		Media.PlaySpeechNotification(Greece, "TenMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(16), function()
		Media.PlaySpeechNotification(Greece, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(18), function()
		Media.PlaySpeechNotification(Greece, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(20), function()
		Media.PlaySpeechNotification(Greece, "WarningOneMinuteRemaining")
	end)

	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })

	Camera.Position = DefaultCameraPosition.CenterPosition
	TimerColor = Greece.Color

	Trigger.OnAnyKilled(ObjectiveBuildings, function()
		Greece.MarkFailedObjective(DefendChronosphere)
	end)

	Trigger.OnEnteredFootprint(ScientistDiscoveryFootprint, function(a, id)
		if a.Owner == Greece and not ScientistsTriggered then
			ScientistsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			CreateScientists()
		end
	end)

	InitialAlliedReinforcements()
	ActivateAI()
end
