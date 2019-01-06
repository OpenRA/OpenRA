--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
DestroySubPensTriggerActivator = { Spen1, Spen2, Spen3, Spen4, Spen5 }
ClearSubActivityTriggerActivator = { Sub1, Sub2, Sub3, Sub4, Sub5, Sub6, Sub7, Sub8, Sub9, Sub10, Sub11, Sub12, Sub13, Sub14, Sub15, Sub16, Sub17 }
AlliedGunboats = { "pt", "pt", "pt" }
BeachRifles = { BeachRifle1, BeachRifle2, BeachRifle3, BeachRifle4 }

lstReinforcements =
{
	first =
	{
		actors = { "mcv", "jeep", "2tnk", "2tnk" },
		entryPath = { AlliedMCVEntry.Location, Unload1.Location },
		exitPath = { AlliedMCVEntry.Location }
	},
	second =
	{
		actors = { "jeep", "2tnk", "e1", "e1", "e1" },
		entryPath = { AlliedMCVEntry.Location, Unload1.Location },
		exitPath = { AlliedMCVEntry.Location }
	}
}

if Map.LobbyOption("difficulty") == "easy" then
	ActivateAIDelay = DateTime.Minutes(1)
else
	ActivateAIDelay = DateTime.Seconds(30)
end

RaidingParty = { "3tnk", "3tnk", "v2rl", "e1", "e2"}
BaseRaidDelay1 = { DateTime.Minutes(1), DateTime.Minutes(2) }
BaseRaidDelay2 = { DateTime.Minutes(3), DateTime.Minutes(4) }
RaidOnePath = { RaidOneEntry.Location, RaidOneLanding.Location }
RaidTwoPath = { RaidTwoEntry.Location, RaidTwoLanding.Location }

StartTimer = false
TimerColor = Player.GetPlayer("USSR").Color
TimerTicks = DateTime.Minutes(10)
ticked = TimerTicks
StartTimerDelay = DateTime.Minutes(5)

InitialAlliedReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.Reinforce(greece, AlliedGunboats, { GunboatEntry.Location, waypoint42.Location }, 2)
		Media.PlaySpeechNotification(greece, "ReinforcementsArrived")
		local reinforcement = lstReinforcements.first
		Reinforcements.ReinforceWithTransport(greece, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

BeachRunners = function()
	Trigger.AfterDelay(DateTime.Seconds(7), function()
		Utils.Do(BeachRifles, function(actor)
			actor.Move(BeachRifleDestination.Location)
		end)
	end)
end

SecondAlliedLanding = function()
	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(greece, "ReinforcementsArrived")
		local reinforcement = lstReinforcements.second
		Reinforcements.ReinforceWithTransport(greece, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

CaptureRadarDome = function()
	Trigger.OnKilled(RadarDome, function()
		greece.MarkFailedObjective(CaptureRadarDomeObj)
	end)

	Trigger.OnCapture(RadarDome, function()
		greece.MarkCompletedObjective(CaptureRadarDomeObj)
		BaseRaids()
	end)
end

BaseRaids = function()
	if Map.LobbyOption("difficulty") == "easy" then
		return
	else
		Trigger.AfterDelay(Utils.RandomInteger(BaseRaidDelay1[1], BaseRaidDelay1[2]), function()	
			local raiders = Reinforcements.ReinforceWithTransport(ussr, "lst", RaidingParty, RaidOnePath, { RaidOneEntry.Location })[2]
			Utils.Do(raiders, function(a)
				Trigger.OnAddedToWorld(a, function()
					a.AttackMove(PlayerBase.Location)
					IdleHunt(a)
				end)
			end)
		end)

		Trigger.AfterDelay(Utils.RandomInteger(BaseRaidDelay2[1], BaseRaidDelay2[2]), function()	
			local raiders = Reinforcements.ReinforceWithTransport(ussr, "lst", RaidingParty, RaidTwoPath, { RaidTwoEntry.Location })[2]
			Utils.Do(raiders, function(a)
				Trigger.OnAddedToWorld(a, function()
					a.AttackMove(PlayerBase.Location)
					IdleHunt(a)
				end)
			end)
		end)
	end
end

StartTimerFunction = function()
	if Map.LobbyOption("difficulty") == "hard" then
		StartTimer = true
		Media.PlaySpeechNotification(greece, "TimerStarted")
	end
end

FinishTimer = function()
	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("Enemy approaching", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

BattalionWays = 
{
	{ HardEntry1.Location, HardLanding1.Location },
	{ HardEntry2.Location, HardLanding2.Location },
	{ HardEntry3.Location, HardLanding3.Location },
	{ HardEntry4.Location, HardLanding4.Location },
	{ HardEntry5.Location, HardLanding5.Location },
	{ HardEntry6.Location, HardLanding6.Location }
}

SendArmoredBattalion = function()
	Media.PlaySpeechNotification(greece, "EnemyUnitsApproaching")
	Utils.Do(BattalionWays, function(way) 
		local units = { "3tnk", "3tnk", "3tnk", "4tnk", "4tnk" }
		local armor = Reinforcements.ReinforceWithTransport(ussr, "lst", units , way, { way[2], way[1] })[2]
		Utils.Do(armor, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(PlayerBase.Location)
				IdleHunt(a)
			end)
		end)	
	end)
end

DestroySubPensCompleted = function()
	greece.MarkCompletedObjective(DestroySubPens)
end

ClearSubActivityCompleted = function()
	greece.MarkCompletedObjective(ClearSubActivity)
end

Tick = function()
	ussr.Cash = 5000
	badguy.Cash = 500

	if StartTimer then
		if ticked > 0 then
			UserInterface.SetMissionText("Soviet armored battalion arrives in " .. Utils.FormatTime(ticked), TimerColor)
			ticked = ticked - 1
		elseif ticked == 0 then
			FinishTimer()
			SendArmoredBattalion()
			ticked = ticked - 1
		end
	end

	if greece.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(BeatAllies)
	end
end

WorldLoaded = function()
	greece = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")
	badguy = Player.GetPlayer("BadGuy")

	Camera.Position = DefaultCameraPosition.CenterPosition

	CaptureRadarDomeObj = greece.AddPrimaryObjective("Capture the Radar Dome.")
	DestroySubPens = greece.AddPrimaryObjective("Destroy all Soviet Sub Pens")
	ClearSubActivity = greece.AddSecondaryObjective("Clear the area of all sub activity")
	BeatAllies = ussr.AddPrimaryObjective("Defeat the Allied forces.")

	Trigger.OnObjectiveCompleted(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(greece, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(greece, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(greece, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(greece, "MissionAccomplished")
		end)
	end)

	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = ussr })

	InitialAlliedReinforcements()
	SecondAlliedLanding()
	BeachRunners()
	CaptureRadarDome()
	Trigger.AfterDelay(ActivateAIDelay, ActivateAI)
	Trigger.AfterDelay(StartTimerDelay, StartTimerFunction)

	Trigger.OnAllRemovedFromWorld(DestroySubPensTriggerActivator, DestroySubPensCompleted)
	Trigger.OnAllRemovedFromWorld(ClearSubActivityTriggerActivator, ClearSubActivityCompleted)
end
