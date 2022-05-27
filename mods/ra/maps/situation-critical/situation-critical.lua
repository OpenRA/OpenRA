--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
MissleSubs = { MSub1, MSub2, MSub3, MSub4 }
VolkovEntryPath = { LSTEntry.Location, LZ.Location }
VolkovandFriend = { "volk", "delphi" }
InsertionTransport = "lst.reinforcement"
SamSites = { Sam1, Sam2, Sam3, Sam4, Sam5, Sam6, Sam7, Sam8, Sam9, Sam10, Sam11, Sam12 }
PrimaryTargets = { BioLab, Silo1, Silo2 }
TimerTicks = DateTime.Minutes(8)

Shocktroopers = { Shok1, Shok2, Shok3, Shok4 }

InnerPatrolPaths =
{
	{ InnerPatrol2.Location, InnerPatrol3.Location, InnerPatrol4.Location, InnerPatrol1.Location },
	{ InnerPatrol3.Location, InnerPatrol2.Location, InnerPatrol1.Location, InnerPatrol4.Location },
	{ InnerPatrol4.Location, InnerPatrol1.Location, InnerPatrol2.Location, InnerPatrol3.Location },
	{ InnerPatrol1.Location, InnerPatrol4.Location, InnerPatrol3.Location, InnerPatrol2.Location }
}

OuterPatrols =
{
	{ TeamOne1, TeamOne2, TeamOne3 },
	{ TeamTwo1, TeamTwo2, TeamTwo3 },
	{ TeamThree1, TeamThree2, TeamThree3 },
	{ TeamFour1, TeamFour2, TeamFour3 },
	{ TeamFive1, TeamFive2, TeamFive3 }
}

OuterPatrolPaths =
{
	{ OuterPatrol1.Location, OuterPatrol2.Location, OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location, OuterPatrol6.Location, OuterPatrol7.Location },
	{ OuterPatrol5.Location, OuterPatrol4.Location, OuterPatrol3.Location, OuterPatrol2.Location, OuterPatrol1.Location, OuterPatrol7.Location, OuterPatrol6.Location },
	{ OuterPatrol6.Location, OuterPatrol7.Location, OuterPatrol1.Location, OuterPatrol2.Location, OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location },
	{ OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location, OuterPatrol6.Location, OuterPatrol7.Location, OuterPatrol1.Location, OuterPatrol2.Location },
	{ OuterPatrol3.Location, OuterPatrol2.Location, OuterPatrol1.Location, OuterPatrol7.Location, OuterPatrol6.Location, OuterPatrol5.Location, OuterPatrol4.Location }
}

GroupPatrol = function(units, waypoints, delay)
	local i = 1
	local stop = false

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end
			if unit.Location == waypoints[i] then
				local bool = Utils.All(units, function(actor) return actor.IsIdle end)
				if bool then
					stop = true
					i = i + 1
					if i > #waypoints then
						i = 1
					end
					Trigger.AfterDelay(delay, function() stop = false end)
				end
			else
				unit.AttackMove(waypoints[i])
			end
		end)
	end)
end

StartPatrols = function()
	for i = 1, 5 do
		GroupPatrol(OuterPatrols[i], OuterPatrolPaths[i], DateTime.Seconds(3))
	end

	for i = 1, 4 do
		Trigger.AfterDelay(DateTime.Seconds(3* (i - 1)), function()
			Trigger.OnIdle(Shocktroopers[i], function()
				Shocktroopers[i].Patrol(InnerPatrolPaths[i])
			end)
		end)
	end
end

LabInfiltrated = false
SetupTriggers = function()
	Trigger.OnAllKilled(SamSites, function()
		USSR.MarkCompletedObjective(KillSams)
		SendInBombers()
	end)

	Trigger.OnInfiltrated(BioLab, function()
		Media.DisplayMessage("Plans stolen; erasing all data.", "Scientist")
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			USSR.MarkCompletedObjective(InfiltrateLab)
			LabInfiltrated = true
			SendInBombers()
		end)
	end)

	Trigger.OnKilled(BioLab, function()
		if not LabInfiltrated then
			USSR.MarkFailedObjective(InfiltrateLab)
		end
	end)

	Trigger.OnAllKilled(PrimaryTargets, function()
		USSR.MarkCompletedObjective(DestroyFacility)
		USSR.MarkCompletedObjective(VolkovSurvive)
	end)

	Trigger.OnAllKilled(MissleSubs, function()
		if not VolkovArrived then
			USSR.MarkFailedObjective(KillPower)
		end
	end)
end

SendInBombers = function()
	if LabInfiltrated and USSR.IsObjectiveCompleted(KillSams) then
		local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
		proxy.TargetAirstrike(TacticalNuke1.CenterPosition, Angle.SouthWest)
		proxy.TargetAirstrike(TacticalNuke2.CenterPosition, Angle.SouthWest)
		proxy.TargetAirstrike(TacticalNuke3.CenterPosition, Angle.SouthWest)
		proxy.Destroy()
	end
end


SendInVolkov = function()
	if not VolkovArrived then
		USSR.MarkCompletedObjective(KillPower)
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local teamVolkov = Reinforcements.ReinforceWithTransport(USSR, InsertionTransport, VolkovandFriend, VolkovEntryPath, { VolkovEntryPath[1] })[2]
		VolkovArrived = true
		Trigger.OnKilled(teamVolkov[1], function()
			USSR.MarkFailedObjective(VolkovSurvive)
		end)
		Trigger.OnAddedToWorld(teamVolkov[1], function(a)
			Media.DisplayMessage("IFF software update failed. Require manual target input.", "Volkov")
		end)

		Trigger.OnAddedToWorld(teamVolkov[2], function(b)
			Trigger.OnKilled(b, function()
				if not LabInfiltrated then
					USSR.MarkFailedObjective(InfiltrateLab)
				end
			end)
		end)
	end
end

ticked = TimerTicks
Tick = function()
	if Turkey.PowerState ~= "Normal" then
		SendInVolkov()
	end

	if ticked > 0 then
		UserInterface.SetMissionText("Missiles launch in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 then
		UserInterface.SetMissionText("We're too late!", USSR.Color)
		Turkey.MarkCompletedObjective(LaunchMissles)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Turkey = Player.GetPlayer("Turkey")

	InitObjectives(USSR)

	LaunchMissles = Turkey.AddObjective("Survive until time expires.")
	KillPower = USSR.AddObjective("Bring the base to low power. Volkov will arrive\nonce the defenses are down.")
	InfiltrateLab = USSR.AddObjective("Infiltrate the bio-weapons lab with the scientist.")
	DestroyFacility = USSR.AddObjective("Destroy the bio-weapons lab and missile silos.")
	KillSams = USSR.AddObjective("Destroy all sam sites on the island.\nOur strategic bombers will finish the rest.", "Secondary", false)
	VolkovSurvive = USSR.AddObjective("Volkov must survive.")

	Trigger.AfterDelay(DateTime.Minutes(3), function()
		Media.PlaySpeechNotification(USSR, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(5), function()
		Media.PlaySpeechNotification(USSR, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(7), function()
		Media.PlaySpeechNotification(USSR, "WarningOneMinuteRemaining")
	end)

	StartPatrols()
	SetupTriggers()
	Camera.Position = DefaultCameraPosition.CenterPosition
	TimerColor = Turkey.Color
end
