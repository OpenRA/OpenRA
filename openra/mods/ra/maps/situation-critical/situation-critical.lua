--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local USSR = Player.GetPlayer("USSR")
local Turkey = Player.GetPlayer("Turkey")
local Ukraine = Player.GetPlayer("Ukraine")

local MissileSubs = { MSub1, MSub2, MSub3, MSub4 }
local VolkovEntryPath = { LSTEntry.Location, LZ.Location }
local VolkovAndFriend = { "delphi", "volk" }
local InsertionTransport = "lst.reinforcement"
local SamSites = { Sam1, Sam2, Sam3, Sam4, Sam5, Sam6, Sam7, Sam8, Sam9, Sam10, Sam11, Sam12 }
local PrimaryTargets = { BioLab, Silo1, Silo2 }
local Shocktroopers = { Shok1, Shok2, Shok3, Shok4 }

local GuardLab = AddPrimaryObjective(Turkey, "")
local KillPower = AddPrimaryObjective(USSR, "kill-power")
local InfiltrateLab = AddPrimaryObjective(USSR, "infiltrate-bio-weapons-lab-scientist")
local DestroyFacility = AddPrimaryObjective(USSR, "destroy-bio-weapons-lab-missile-silos")
local KillSams = AddSecondaryObjective(USSR, "destroy-all-sam-sites-strategic-bombers")
local VolkovSurvive = AddPrimaryObjective(USSR, "volkov-survive")

local LabInfiltrated = false
local VolkovArrived = false
local LaunchStarted = false
local BombersCalled = false

local LowPowerDuration = 0
local LowPowerThreshold = DateTime.Seconds(2)
local TimerTicks = DateTime.Minutes(8)
local TimerColor = Turkey.Color

---@type cpos[][]
local InnerPatrolPaths =
{
	{ InnerPatrol2.Location, InnerPatrol3.Location, InnerPatrol4.Location, InnerPatrol1.Location },
	{ InnerPatrol3.Location, InnerPatrol2.Location, InnerPatrol1.Location, InnerPatrol4.Location },
	{ InnerPatrol4.Location, InnerPatrol1.Location, InnerPatrol2.Location, InnerPatrol3.Location },
	{ InnerPatrol1.Location, InnerPatrol4.Location, InnerPatrol3.Location, InnerPatrol2.Location }
}

---@type actor[][]
local OuterPatrols =
{
	{ TeamOne1, TeamOne2, TeamOne3 },
	{ TeamTwo1, TeamTwo2, TeamTwo3 },
	{ TeamThree1, TeamThree2, TeamThree3 },
	{ TeamFour1, TeamFour2, TeamFour3 },
	{ TeamFive1, TeamFive2, TeamFive3 }
}

---@type cpos[][]
local OuterPatrolPaths =
{
	{ OuterPatrol1.Location, OuterPatrol2.Location, OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location, OuterPatrol6.Location, OuterPatrol7.Location },
	{ OuterPatrol5.Location, OuterPatrol4.Location, OuterPatrol3.Location, OuterPatrol2.Location, OuterPatrol1.Location, OuterPatrol7.Location, OuterPatrol6.Location },
	{ OuterPatrol6.Location, OuterPatrol7.Location, OuterPatrol1.Location, OuterPatrol2.Location, OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location },
	{ OuterPatrol3.Location, OuterPatrol4.Location, OuterPatrol5.Location, OuterPatrol6.Location, OuterPatrol7.Location, OuterPatrol1.Location, OuterPatrol2.Location },
	{ OuterPatrol3.Location, OuterPatrol2.Location, OuterPatrol1.Location, OuterPatrol7.Location, OuterPatrol6.Location, OuterPatrol5.Location, OuterPatrol4.Location }
}

local function IsVolkovNearBlast()
	-- Within this radius, most normal infantry are left at 20% HP or less.
	local cyborgsInDanger = Map.ActorsInCircle(TacticalNukeCenter.CenterPosition, WDist.FromCells(12), function(a)
		return a.Type == "volk"
	end)

	return #cyborgsInDanger == 1
end

local function ClearCountdown()
	UserInterface.SetMissionText("")
	DateTime.TimeLimit = 0
	TimerTicks = 0
end

---@param duration integer
local function SetRunText(duration)
	local text = UserInterface.GetFluentMessage("run-for-it")
	local seconds = math.floor(duration / DateTime.Seconds(1))
	Media.PlaySoundNotification(USSR, "AlertBleep")

	for i = 0, seconds - 1, 1 do
		local color = Ukraine.Color
		if i % 2 ~= 0 then
			color = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(text, color) end)
	end
	Trigger.AfterDelay(duration, function() UserInterface.SetMissionText("") end)
end

---@param units actor[]
---@param path cpos[]
---@param delay integer
local function GroupPatrol(units, path, delay)
	local i = 1
	local stop = false

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end

			if unit.Location ~= path[i] then
				unit.AttackMove(path[i])
				return
			end

			local bool = Utils.All(units, function(actor) return actor.IsIdle or actor.IsDead end)
			if bool then
				stop = true
				i = i + 1
				if i > #path then
					i = 1
				end
				Trigger.AfterDelay(delay, function() stop = false end)
			end
		end)
	end)
end

local function StartPatrols()
	for i = 1, 5 do
		GroupPatrol(OuterPatrols[i], OuterPatrolPaths[i], DateTime.Seconds(3))
	end

	for i = 1, 4 do
		Trigger.AfterDelay(DateTime.Seconds(3 * (i - 1)), function()
			if Shocktroopers[i].IsDead then
				return
			end

			Trigger.OnIdle(Shocktroopers[i], function()
				Shocktroopers[i].Patrol(InnerPatrolPaths[i])
			end)
		end)
	end
end

local function LaunchMissiles()
	if LaunchStarted then
		return
	end

	LaunchStarted = true
	UserInterface.SetMissionText(UserInterface.GetFluentMessage("too-late"), USSR.Color)
	Actor.Create("camera", true, { Owner = USSR, Location = LaunchCameraMark.Location })

	-- Edge case that seems impossible in RA1.
	if Silo1.IsDead and Silo2.IsDead and not BioLab.IsDead then
		Actor.Create("flare", true, { Owner = Ukraine, Location = GasEntry.Location })
		BioLab.GrantCondition("demolish-disabled")
		return
	end

	Camera.Position = LaunchCameraMark.CenterPosition
	local delay = 15

	Utils.Do({ Silo1, Silo2 }, function(silo)
		Trigger.AfterDelay(delay, function()
			if silo.IsDead then
				return
			end

			silo.ActivateNukePower(DefaultCameraPosition.Location)
		end)

		if not silo.IsDead then
			silo.GrantCondition("demolish-disabled")
			delay = delay + 15
		end
	end)
end

local function SendInBombers()
	if LaunchStarted or not LabInfiltrated or not USSR.IsObjectiveCompleted(KillSams) then
		return
	end

	BombersCalled = true
	ClearCountdown()
	local bombDelay = DateTime.Seconds(2)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Actor.Create("camera.bomber", true, { Owner = USSR, Location = TacticalNukeCenter.Location })
	end)

	if IsVolkovNearBlast() then
		bombDelay = DateTime.Seconds(8)
		SetRunText(bombDelay + DateTime.Seconds(1))
	end

	Trigger.AfterDelay(bombDelay, function()
		local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
		proxy.TargetAirstrike(TacticalNuke1.CenterPosition, Angle.SouthWest)
		proxy.TargetAirstrike(TacticalNuke2.CenterPosition, Angle.SouthWest)
		proxy.TargetAirstrike(TacticalNuke3.CenterPosition, Angle.SouthWest)
		proxy.Destroy()
	end)
end

local function SendInVolkov()
	if VolkovArrived or LowPowerDuration < LowPowerThreshold then
		return
	end

	VolkovArrived = true
	USSR.MarkCompletedObjective(KillPower)
	Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
	Beacon.New(USSR, LZ.CenterPosition, DateTime.Seconds(6))
	local teamVolkov = Reinforcements.ReinforceWithTransport(USSR, InsertionTransport, VolkovAndFriend, VolkovEntryPath, { VolkovEntryPath[1] })[2]
	local scientist, volkov = teamVolkov[1], teamVolkov[2]

	Trigger.OnKilled(volkov, function()
		USSR.MarkFailedObjective(VolkovSurvive)
	end)

	Trigger.OnAddedToWorld(volkov, function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("software-update-failed-manual-targets"), UserInterface.GetFluentMessage("volkov"))
	end)

	Trigger.OnAddedToWorld(scientist, function(b)
		Trigger.OnKilled(b, function()
			if not LabInfiltrated then
				USSR.MarkFailedObjective(InfiltrateLab)
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not scientist.IsDead then
				scientist.GrantCondition("can-be-targeted")
			end
		end)
	end)
end

local function SetupStartTriggers()
	Trigger.OnAllKilled(SamSites, function()
		USSR.MarkCompletedObjective(KillSams)
		SendInBombers()
	end)

	Trigger.OnInfiltrated(BioLab, function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("plans-stolen-erase-data"), UserInterface.GetFluentMessage("scientist"))
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
		if LaunchStarted then
			return
		end

		if not BombersCalled then
			ClearCountdown()
		end

		USSR.MarkCompletedObjective(DestroyFacility)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			USSR.MarkCompletedObjective(VolkovSurvive)
		end)
	end)

	Trigger.OnAllKilled(MissileSubs, function()
		if not VolkovArrived then
			USSR.MarkFailedObjective(KillPower)
		end
	end)
end

Tick = function()
	if Turkey.PowerState ~= "Normal" then
		LowPowerDuration = LowPowerDuration + 1
		SendInVolkov()
	else
		LowPowerDuration = 0
	end

	if TimerTicks > 0 then
		if (TimerTicks % DateTime.Seconds(1)) == 0 then
			local text = UserInterface.GetFluentMessage("missiles-launch-in", { ["time"] = Utils.FormatTime(TimerTicks) })
			UserInterface.SetMissionText(text, TimerColor)
		end
		TimerTicks = TimerTicks - 1
	elseif TimerTicks == 0 and not BombersCalled and not USSR.IsObjectiveCompleted(DestroyFacility) then
		Turkey.MarkCompletedObjective(GuardLab)
		LaunchMissiles()
	end
end

WorldLoaded = function()
	InitObjectives(USSR)
	StartPatrols()
	SetupStartTriggers()
	Camera.Position = DefaultCameraPosition.CenterPosition
	DateTime.TimeLimit = TimerTicks
end
