--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--- This mission's human player.
local Greece = Player.GetPlayer("Greece")
--- Owner of the main Soviet base and most submarines.
local USSR = Player.GetPlayer("USSR")
--- Owner of the radar base, some scattered units, and the Hard battalion.
local BadGuy = Player.GetPlayer("BadGuy")

local CaptureRadarDome = AddPrimaryObjective(Greece, "capture-radar-dome")
local DestroySubPens = AddPrimaryObjective(Greece, "destroy-all-soviet-sub-pens")
local ClearSubActivity = AddSecondaryObjective(Greece, "clear-area-all-subs")
local PlayerVictoryStarted = false
local RadarCaptured = false
local PensDestroyed = false
local BoatRaidsStarted = false

local DestroySubPensTriggerActivator = { Spen1, Spen2, Spen3, Spen4, Spen5 }
local ClearSubActivityTriggerActivator = { Sub1, Sub2, Sub3, Sub4, Sub5, Sub6, Sub7, Sub8, Sub9, Sub10, Sub11, Sub12, Sub13, Sub14, Sub15, Sub16, Sub17 }
local BeachRifles = { BeachRifle1, BeachRifle2, BeachRifle3, BeachRifle4 }

--- Greece's reinforcements, based on RA1 teams "mcvlst" and "renf".
local LstReinforcements =
{
	first =
	{
		actors = { "mcv", "jeep", "2tnk", "2tnk" },
		entryPath = { AlliedMCVEntry.Location, waypoint1.Location, AlliedLanding.Location },
		exitPath = { AlliedMCVEntry.Location }
	},
	second =
	{
		actors = { "jeep", "2tnk", "e1", "e1", "e1" },
		entryPath = { AlliedMCVEntry.Location, waypoint1.Location, AlliedLanding.Location },
		exitPath = { AlliedMCVEntry.Location }
	}
}

local RaidingParties =
{
	easy = { "e1", "e1", "e2", "e2", "e2" },
	normal = { "3tnk", "e2", "e2", "e2", "e2" },
	hard = { "3tnk", "3tnk", "v2rl", "e1", "e2" }
}
local BoatRaidDelay1 = { DateTime.Minutes(1), DateTime.Minutes(2) }
local BoatRaidDelay2 = { DateTime.Minutes(3), DateTime.Minutes(4) }
local RaidOnePath = { RaidOneEntry.Location, RaidOneLanding.Location }
local RaidTwoPath = { RaidTwoEntry.Location, RaidTwoLanding.Location }

local TimerEnabled = false
local TimerColor = USSR.Color
local TimerTicks = DateTime.Minutes(10)
local StartTimerDelay = DateTime.Minutes(5)

---@param objective integer
---@param speech string
---@param markDelay? integer
---@param speechDelay? integer
local function CompleteObjectiveWithSpeech(objective, speech, markDelay, speechDelay)
	Trigger.AfterDelay(markDelay or 0, function()
		Greece.MarkCompletedObjective(objective)
	end)

	Trigger.AfterDelay(speechDelay or 0, function()
		Media.PlaySpeechNotification(Greece, speech)
	end)
end

local function MarkDefeat()
	local mainObjectives = { CaptureRadarDome, DestroySubPens }

	Utils.Do(mainObjectives, function(mo)
		if not Greece.IsObjectiveCompleted(mo) then
			Greece.MarkFailedObjective(mo)
		end
	end)
end

--- Send the MCV and its escort, based on RA1 trigger "set1".
local function InitialAlliedReinforcements()
	local gunboats = { "pt", "pt", "pt" }

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Reinforcements.Reinforce(Greece, gunboats, { GunboatEntry.Location, waypoint1.Location, waypoint42.Location }, 15)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		local reinforcement = LstReinforcements.first
		Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

--- Order some infantry to flee for help as others investigate.
--- The RA1 triggers were "flee" and "chek".
local function BeachRunners()
	local grenadiers = { BeachScout1, BeachScout2 }

	Trigger.AfterDelay(DateTime.Seconds(7), function()
		Utils.Do(BeachRifles, function(actor)
			if actor.IsDead then
				return
			end

			actor.Move(MainBaseGate.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(52), function()
		Utils.Do(grenadiers, function(actor)
			if actor.IsDead then
				return
			end

			actor.Move(AlliedLanding.Location, 2)
			actor.Hunt()
		end)
	end)

	Utils.Do(grenadiers, function(actor)
		Trigger.OnDamaged(actor, function(_, attacker)
			if actor.IsDead or attacker.IsDead then
				return
			end

			Trigger.Clear(actor, "OnDamaged")
			actor.Stop()
			actor.Attack(attacker)
			IdleHunt(actor)
		end)
	end)
end

--- Prepare a V2 to follow runners on their return hunt.
--- The RA1 trigger was "attk", where the V2 was ordered to attack on its own.
local function PrepareRunnerLauncher()
	local path = { RadarDefenseLanding.Location, waypoint29.Location, ParaLZ4.Location, ParaLZ3.Location }

	local foot = Trigger.OnEnteredFootprint({ MainBaseGate.Location }, function(a, id)
		if a.Owner ~= BadGuy then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		local liveRifles = Utils.Where(BeachRifles, function(br)
			return not br.IsDead
		end)

		Utils.Do(liveRifles, function(rifle)
			rifle.Patrol(path)
			IdleHunt(rifle)

			if not BadGuyLauncherEast.IsDead then
				BadGuyLauncherEast.Guard(rifle)
			end
		end)

		Trigger.OnAllKilled(liveRifles, function()
			IdleHunt(BadGuyLauncherEast)
		end)
	end)

	Trigger.OnAllKilled(BeachRifles, function()
		if foot then
			Trigger.RemoveFootprintTrigger(foot)
		end
	end)
end

--- Send Greece's follow-up reinforcement LST.
--- The RA1 trigger was "renf" and activated by Greece's first refinery.
local function SecondAlliedLanding()
	Trigger.AfterDelay(DateTime.Seconds(90), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		local reinforcement = LstReinforcements.second
		Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

--- Set up the radar objective, based on RA1 triggers "win" and "xp".
local function PrepareRadarDome()
	Trigger.OnKilled(RadarDome, function()
		if RadarCaptured then
			return
		end

		Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")

		Trigger.AfterDelay(50, function()
			Greece.MarkFailedObjective(CaptureRadarDome)
		end)
	end)

	Trigger.OnCapture(RadarDome, function()
		RadarCaptured = true
		PlayerVictoryStarted = RadarCaptured and PensDestroyed
		CompleteObjectiveWithSpeech(CaptureRadarDome, "FirstObjectiveMet", 50, 36)
	end)
end

--- Set up defensive paratroopers for BadGuy, based on the RA1 trigger "atkd".
local function PrepareRadarDefenseDrop()
	local done = false
	local buildings = { BadGuyFlameTower, BadGuyRax, RadarDome }

	Utils.Do(buildings, function(wsb)
		Trigger.OnDamaged(wsb, function(_, attacker)
			if done or attacker.Owner ~= Greece then
				return
			end

			done = true
			local proxy = Actor.Create("powerproxy.badguydrop", false, { Owner = BadGuy })
			local plane = proxy.TargetParatroopers(RadarDefenseLanding.CenterPosition, Angle.West)[1]
			proxy.Destroy()

			Trigger.OnPassengerExited(plane, function(_, passenger)
				IdleHunt(passenger)
			end)
		end)
	end)
end

--- Schedule LSTs to unload attackers near the Allied start area.
--- This is loosely based on the RA1 triggers "navl" and "tm09".
local function StartBoatRaids()
	Trigger.AfterDelay(Utils.RandomInteger(BoatRaidDelay1[1], BoatRaidDelay1[2]), function()
		local raiders = Reinforcements.ReinforceWithTransport(USSR, "lst", RaidingParties[Difficulty], RaidOnePath, { RaidOneEntry.Location })[2]
		Utils.Do(raiders, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(PlayerBase.Location)
				IdleHunt(a)
			end)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(BoatRaidDelay2[1], BoatRaidDelay2[2]), function()
		local raiders = Reinforcements.ReinforceWithTransport(USSR, "lst", RaidingParties[Difficulty], RaidTwoPath, { RaidTwoEntry.Location })[2]
		Utils.Do(raiders, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(PlayerBase.Location)
				IdleHunt(a)
			end)
		end)
	end)
end

--- Check if the Allied base has progressed enough to trigger LST raids.
--- Based on the RA1 trigger "navl", which only checks for a Naval Yard.
local function PrepareBoatRaids()
	Trigger.OnBuildingPlaced(Greece, function()
		if BoatRaidsStarted or not Greece.HasPrerequisites({ "proc", "syrd", "weap" }) then
			return
		end

		BoatRaidsStarted = true
		StartBoatRaids()
	end)
end

local function StartTimer()
	if Difficulty == "hard" then
		TimerEnabled = true
		Media.PlaySpeechNotification(Greece, "TimerStarted")
	end
end

local function FinishTimer()
	local enemyApproaching = UserInterface.GetFluentMessage("enemy-approaching")

	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(enemyApproaching, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

local function SendArmoredBattalion()
	local boatPaths =
	{
		{ HardEntry1.Location, HardLanding1.Location },
		{ HardEntry2.Location, HardLanding2.Location },
		{ HardEntry3.Location, HardLanding3.Location },
		{ HardEntry4.Location, HardLanding4.Location },
		{ HardEntry5.Location, HardLanding5.Location },
		{ HardEntry6.Location, HardLanding6.Location }
	}

	Media.PlaySpeechNotification(Greece, "EnemyUnitsApproaching")
	Utils.Do(boatPaths, function(way)
		local units = { "3tnk", "3tnk", "3tnk", "4tnk", "4tnk" }
		local armor = Reinforcements.ReinforceWithTransport(BadGuy, "lst", units , way, { way[2], way[1] })[2]

		Utils.Do(armor, function(a)
			Trigger.OnAddedToWorld(a, function()
				a.AttackMove(PlayerBase.Location)
				IdleHunt(a)
			end)
		end)
	end)
end

local function DestroySubPensCompleted()
	PensDestroyed = true
	PlayerVictoryStarted = RadarCaptured and PensDestroyed
	CompleteObjectiveWithSpeech(DestroySubPens, "SecondObjectiveMet", 50)
end

local function ClearSubActivityCompleted()
	CompleteObjectiveWithSpeech(ClearSubActivity, "ObjectiveMet")
end

Tick = function()
	USSR.Cash = 5000
	BadGuy.Cash = 500

	if Greece.HasNoRequiredUnits() and not PlayerVictoryStarted then
		MarkDefeat()
	end

	if not TimerEnabled then
		return
	end

	if TimerTicks > 0 then
		if (TimerTicks % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.GetFluentMessage("soviet-armored-battalion-arrives-in", { ["time"] = Utils.FormatTime(TimerTicks) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		TimerTicks = TimerTicks - 1
	else
		FinishTimer()
		SendArmoredBattalion()
		TimerEnabled = false
	end
end

WorldLoaded = function()
	local englandAttackers = { EnglandRifle1, EnglandRifle2, EnglandRifle3, EnglandRanger }

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives(Greece)

	InitialAlliedReinforcements()
	SecondAlliedLanding()
	BeachRunners()
	PrepareRunnerLauncher()
	PrepareRadarDome()
	PrepareRadarDefenseDrop()
	PrepareBoatRaids()
	ActivateAI()
	Trigger.AfterDelay(StartTimerDelay, StartTimer)

	Trigger.OnAnyKilled(englandAttackers, function()
		Utils.Do(englandAttackers, IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(DestroySubPensTriggerActivator, DestroySubPensCompleted)
	Trigger.OnAllRemovedFromWorld(ClearSubActivityTriggerActivator, ClearSubActivityCompleted)
end
