--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		Media.PlaySpeechNotification(greece, "ReinforcementsArrived")
		Reinforcements.Reinforce(greece, { "mcv" }, { MCVEntry.Location, MCVStop.Location })
		Reinforcements.Reinforce(greece, AlliedBoatReinforcements, { DDEntry.Location, DDStop.Location })
	end)
end

CreateScientists = function()
	scientists = Reinforcements.Reinforce(greece, ScientistTypes, { ScientistsExit.Location })
	Utils.Do(scientists, function(s)
		s.Move(s.Location + CVec.New(0, 1))
		s.Scatter()
	end)

	local flare = Actor.Create("flare", true, { Owner = greece, Location = DefaultCameraPosition.Location + CVec.New(-1, 0) })
	Trigger.AfterDelay(DateTime.Seconds(2), function() Media.PlaySpeechNotification(player, "SignalFlareNorth") end)

	Trigger.OnAnyKilled(scientists, function()
		Media.PlaySpeechNotification(greece, "ObjectiveNotMet")
		greece.MarkFailedObjective(EvacuateScientists)
	end)

	-- Add the footprint trigger in a frame end task (delay 0) to avoid crashes
	local left = #scientists
	Trigger.AfterDelay(0, function()
		local changeOwnerTrigger = Trigger.OnEnteredFootprint(ScientistEvacuationFootprint, function(a, id)
			if a.Owner == greece and a.Type == "chan" then
				a.Owner = germany
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
			if a.Owner == germany then
				a.Stop()
				a.Destroy()

				left = left - 1
				if left == 0 then
					Trigger.RemoveFootprintTrigger(id)
					Trigger.RemoveFootprintTrigger(changeOwnerTrigger)
					flare.Destroy()

					if not greece.IsObjectiveCompleted(EvacuateScientists) and not greece.IsObjectiveFailed(EvacuateScientists) then
						Media.PlaySpeechNotification(greece, "ObjectiveMet")
						greece.MarkCompletedObjective(EvacuateScientists)
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
		local unit = Actor.Create("2tnk", true, { Owner = greece, Facing = Angle.North })
		units[unit] = cells[i]
	end
	Chronosphere.Chronoshift(units)
	UserInterface.SetMissionText("The experiment is a success!", greece.Color)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		greece.MarkCompletedObjective(DefendChronosphere)
		greece.MarkCompletedObjective(KeepBasePowered)
	end)
end

ticked = TimerTicks
Tick = function()
	ussr.Cash = 5000

	if ussr.HasNoRequiredUnits() then
		greece.MarkCompletedObjective(DefendChronosphere)
		greece.MarkCompletedObjective(KeepBasePowered)
	end

	if greece.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(BeatAllies)
	end

	if ticked > 0 then
		UserInterface.SetMissionText("Chronosphere experiment completes in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 and (greece.PowerState ~= "Normal") then
		greece.MarkFailedObjective(KeepBasePowered)
	elseif ticked == 0 then
		DefendChronosphereCompleted()
		ticked = ticked - 1
	end
end

WorldLoaded = function()
	greece = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")
	germany = Player.GetPlayer("Germany")

	InitObjectives(greece)
	DefendChronosphere = greece.AddObjective("Defend the Chronosphere and the Tech Center\nat all costs.")
	KeepBasePowered = greece.AddObjective("The Chronosphere must have power when the\ntimer runs out.")
	EvacuateScientists = greece.AddObjective("Evacuate all scientists from the island to\nthe west.", "Secondary", false)
	BeatAllies = ussr.AddObjective("Defeat the Allied forces.")

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(greece, "TwentyMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(11), function()
		Media.PlaySpeechNotification(greece, "TenMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(16), function()
		Media.PlaySpeechNotification(greece, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(18), function()
		Media.PlaySpeechNotification(greece, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(20), function()
		Media.PlaySpeechNotification(greece, "WarningOneMinuteRemaining")
	end)

	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = ussr })

	Camera.Position = DefaultCameraPosition.CenterPosition
	TimerColor = greece.Color

	Trigger.OnAnyKilled(ObjectiveBuildings, function()
		greece.MarkFailedObjective(DefendChronosphere)
	end)

	Trigger.OnEnteredFootprint(ScientistDiscoveryFootprint, function(a, id)
		if a.Owner == greece and not scientistsTriggered then
			scientistsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			CreateScientists()
		end
	end)

	InitialAlliedReinforcements()
	ActivateAI()
end
