--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local USSR = Player.GetPlayer("USSR")
local Spain = Player.GetPlayer("Spain")

--- The objectives below are delayed and only assigned an ID later.

---@type number Destroy hostile towers from the security center.
local DeactivateSecurity
---@type number Free the Soviet dogs.
local FreeDogs
---@type number Rescue the guarded Engineers.
local RescueEngineers
---@type number Reach all coolant stations with an Engineer.
local GetEngineersToCoolant
---@type number Activate friendly towers from the security center.
local ReprogramSecurity
---@type number Use the final reactor console.
local SaveReactor

--- Has either Rocket Soldier reached their fallback position?
local RocketRetreated = false

local TimeLimits =
{
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5)
}
local RemainingTime = TimeLimits[Difficulty]
local TimerColor = USSR.Color
local CountdownEnabled = false

local Dogs = { Dog1, Dog2, Dog3, Dog4, Dog5, Dog6, Dog7, Dog8, Dog9, Dog10, Dog11, Dog12, Dog13, Dog14, Dog15, Dog16, Dog17, Dog18, Dog19 }
local Engineers = { Prisoner1, Prisoner2, Prisoner3, Prisoner4, Prisoner5 }
local PrisonerGuards = { PrisonerGuard1, PrisonerGuard2, PrisonerGuard3 }
local EntranceGuards = { EntranceGuard1, EntranceGuard2, EntranceGuard3, EntranceGuard4, EntranceGuard5, EntranceGuard6, EntranceGuard7, EntranceGuard8 }
local ReactorGuards = { ReactorGuard1, ReactorGuard2, ReactorGuard3, ReactorGuard4 }
local SecurityCenterGuards = { SecurityCenterGuard1, SecurityCenterGuard2, SecurityCenterGuard3, SecurityCenterGuard4 }
local StartingUnitsReinforcements = { "e1", "e1", "e1", "e1" }

---@param actor actor
---@return boolean
local function IsSoviet(actor)
	return actor.Owner == USSR
end

---@param actor actor
---@return boolean
local function IsSovietHuman(actor)
	return actor.Owner == USSR and actor.Type ~= "dog"
end

---@param actor actor
---@return boolean
local function IsEngineer(actor)
	return actor.Type == "e6"
end

local function MarkAlliedVictory()
	DeactivateSecurity = DeactivateSecurity or USSR.AddPrimaryObjective(UserInterface.GetFluentMessage("deactivate-security-system"))
	local primaries = { DeactivateSecurity, RescueEngineers, GetEngineersToCoolant, SaveReactor }

	Utils.Do(primaries, function(p)
		if not USSR.IsObjectiveCompleted(p) then
			USSR.MarkFailedObjective(p)
		end
	end)
end

local function UpdateTimerText()
	local text = UserInterface.GetFluentMessage("time-until-meltdown", { ["time"] = Utils.FormatTime(RemainingTime) })
	UserInterface.SetMissionText(text, TimerColor)
end

---@param cells cpos[]
---@param filter fun(actor: actor):boolean
---@param action fun()
local function SetBasicFootprint(cells, filter, action)
	local activated = false

	Trigger.OnEnteredFootprint(cells, function(actor, id)
		if activated or not filter(actor) then
			return
		end

		activated = true
		action()
		Trigger.RemoveFootprintTrigger(id)
	end)
end

---@param tag string Tag shared by the cameras.
local function RevealArea(tag)
	local cameras = Map.ActorsWithTag(tag)
	Utils.Do(cameras, function(c)
		c.Owner = USSR
	end)
end

local function PrepareBasicReveals()
	local securityCenter = { CPos.New(83, 71), CPos.New(84,71) }
	local reactor = { CPos.New(74, 66), CPos.New(75, 66), CPos.New(76, 66), CPos.New(77, 66) }
	local westCoolant = { CPos.New(62, 59), CPos.New(62, 60), CPos.New(62, 62), CPos.New(62, 63) }
	local eastCoolant = { CPos.New(90, 59), CPos.New(90, 60), CPos.New(90, 62), CPos.New(90, 63) }
	local southFlame = { CPos.New(67, 82), CPos.New(67, 83) }
	local westFlame = { CPos.New(57, 70), CPos.New(58, 70), CPos.New(59, 70), CPos.New(60, 70) }

	local reveals =
	{
		{ cells = securityCenter, tag = "Security Control Center" },
		{ cells = reactor, tag = "Reactor Room" },
		{ cells = westCoolant, tag = "West Coolant Stations" },
		{ cells = eastCoolant, tag = "East Coolant Stations" },
		{ cells = southFlame, tag = "Flame Tower South" },
		{ cells = westFlame, tag = "Flame Tower West" },
	}

	Utils.Do(reveals, function(reveal)
		SetBasicFootprint(reveal.cells, IsSoviet, function()
			RevealArea(reveal.tag)
		end)
	end)
end

local function SpawnFriendlyFlames()
	local fturA = Actor.Create("ftur", true, { Owner = USSR, Location = FriendlyTower1Goal.Location})
	local fturB = Actor.Create("ftur", true, { Owner = USSR, Location = FriendlyTower2Goal.Location})
	Camera.Position = CameraGoalCenter2.CenterPosition
	RevealArea("Reactor Room")

	Utils.Do(ReactorGuards, function(guard)
		if not guard.IsDead then
			guard.AttackMove(FriendlyTower1Goal.Location, 2)
			guard.AttackMove(CameraGoalCenter2.Location, 2)
		end
	end)

	if not Tanya.IsDead then
		Tanya.Demolish(fturA)
		Tanya.Demolish(fturB)
	end

	USSR.MarkCompletedObjective(ReprogramSecurity)
end

local function PrepareSecurityCenter()
	local cells =  { CPos.New(87, 67), CPos.New(88, 67) }

	Trigger.OnKilled(SecurityCenterBarrel, function()
		RevealArea("Security Control Center")
		Utils.Do(SecurityCenterGuards, IdleHunt)
	end)

	SetBasicFootprint(cells, IsSovietHuman, function()
		FlameTowerPrison.Kill()
		FlameTowerWest.Kill()
		FlameTowerEast.Kill()
		FlameTowerSouth.Kill()
		RescueEngineers = RescueEngineers or AddPrimaryObjective(USSR, "rescue-engineers")
		USSR.MarkCompletedObjective(DeactivateSecurity)
	end)

	SetBasicFootprint(cells, IsEngineer, SpawnFriendlyFlames)
end

local function PrepareFinalStation()
	local cells = { CPos.New(73, 51), CPos.New(74, 51), CPos.New(75, 51), CPos.New(76, 51), CPos.New(77, 51), CPos.New(78, 51) }

	SetBasicFootprint(cells, IsEngineer, function()
		CountdownEnabled = false
		TimerColor = HSLColor.LightGreen
		UpdateTimerText()
		DateTime.TimeLimit = 0
		USSR.MarkCompletedObjective(SaveReactor)
	end)
end

local function PrepareEngineerStations()
	local northWestCells = { CPos.New(65, 58), CPos.New(66, 58), CPos.New(67, 58), CPos.New(65, 59), CPos.New(66, 59), CPos.New(67, 59) }
	local southWestCells = { CPos.New(65, 64), CPos.New(66, 64), CPos.New(67, 64), CPos.New(65, 65), CPos.New(66, 65), CPos.New(67, 65) }
	local northEastCells = { CPos.New(86, 57), CPos.New(87, 57), CPos.New(88, 57), CPos.New(86, 58), CPos.New(87, 58), CPos.New(88, 58) }
	local southEastCells = { CPos.New(86, 64), CPos.New(87, 64), CPos.New(88, 64), CPos.New(86, 65), CPos.New(87, 65), CPos.New(88, 65) }
	local stations = { northWestCells, southWestCells, northEastCells, southEastCells }
	local coolantMarks = { CoolantMarkNorthWest, CoolantMarkSouthWest, CoolantMarkNorthEast, CoolantMarkSouthEast }
	local coolantCount = 0

	for i = 1, #stations do
		SetBasicFootprint(stations[i], IsEngineer, function()
			Media.PlaySpeechNotification(USSR, "ControlCenterDeactivated")
			Actor.Create("flare", true, { Owner = Spain, Location = coolantMarks[i].Location })
			coolantCount = coolantCount + 1

			if coolantCount < #stations then
				return
			end

			SaveReactor = AddPrimaryObjective(USSR, "engineer-reactor-core")
			USSR.MarkCompletedObjective(GetEngineersToCoolant)
			Trigger.AfterDelay(DateTime.Seconds(2), PrepareFinalStation)
		end)
	end
end

--- Prepare the Rocket Soldiers and their barrel traps.
--- After the first explosions, they flee to try again at a fallback position.
local function PrepareRocketSoldiers()
	local rockets = { Rocket1, Rocket2 }
	local firstTraps = { RocketTrap1, RocketTrap2 }
	local trapCells = { CPos.New(72, 72), CPos.New(72,73), CPos.New(72,74) }
	local retreatTrapCells = { CPos.New(66, 72), CPos.New(66,73), CPos.New(66,74) }

	SetBasicFootprint(trapCells, IsSoviet, function()
		for i = 1, #rockets do
			if not rockets[i].IsDead and not firstTraps[i].IsDead then
				rockets[i].Attack(firstTraps[i])
			end
		end

		RevealArea("Rocket Soldiers")
	end)

	Trigger.OnAnyKilled(firstTraps, function()
		-- Stay if an attack from the west seems likely.
		if CrateMazeTrap.IsDead then
			return
		end

		Utils.Do(rockets, function(rocket)
			if rocket.IsDead then
				return
			end

			rocket.Move(RocketRetreat.Location)
		end)
	end)

	Trigger.OnEnteredProximityTrigger(RocketRetreat.CenterPosition, WDist.FromCells(1), function(a, id)
		if a.Type == "e3" then
			RocketRetreated = true
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(retreatTrapCells, function(a, id)
		if not RocketRetreated or not IsSoviet(a) then
			return
		end

		Utils.Do(rockets, function(rocket)
			if rocket.IsDead or RocketRetreatTrap.IsDead then
				return
			end

			rocket.Attack(RocketRetreatTrap)
		end)

		Trigger.RemoveFootprintTrigger(id)
	end)

	Trigger.OnKilled(RocketRetreatTrap, function()
		Utils.Do(rockets, IdleHunt)
	end)
end

--- Prepare a soldier at the maze to fire at a barrel trap and flee.
--- If they successfully escape, they will blow up the captive Engineers.
local function PrepareCrateMazeGuard()
	local trapCells = { CPos.New(51, 73), CPos.New(51, 74) }
	local moveGoals = { CrateMazeWaypoint1.Location, CrateMazeWaypoint2.Location, CrateMazeWaypoint3.Location, ExecutionWaypoint.Location }
	local goalCount = 1
	local waitTime = DateTime.Seconds(6)
	if Difficulty == "hard" then
		waitTime = DateTime.Seconds(2)
	end

	SetBasicFootprint(trapCells, IsSoviet, function()
		if not CrateMazeTrap.IsDead then
			CrateMazeGuard.Attack(CrateMazeTrap)
		end

		CrateMazeGuard.Move(moveGoals[1])
		RevealArea("Crate Maze")
	end)

	local proximity = Trigger.OnEnteredProximityTrigger(ExecutionWaypoint.CenterPosition, WDist.New(512), function(a)
		if a == CrateMazeGuard and not ExecutionBarrel.IsDead then
			a.Attack(ExecutionBarrel)
		end
	end)

	local foot = Trigger.OnEnteredFootprint(moveGoals, function(a)
		if a ~= CrateMazeGuard then
			return
		end

		Trigger.AfterDelay(waitTime, function()
			if CrateMazeGuard.IsDead then
				return
			end

			goalCount = goalCount + 1
			CrateMazeGuard.Move(moveGoals[goalCount])
		end)
	end)

	Trigger.OnKilled(CrateMazeGuard, function()
		Trigger.RemoveProximityTrigger(proximity)
		Trigger.RemoveFootprintTrigger(foot)
	end)
end

local function PreparePrisonGuards()
	local towerAndGuards = Utils.Concat({ FlameTowerPrison }, PrisonerGuards)

	Utils.Do(PrisonerGuards, function(guard)
		Trigger.OnKilled(guard, function()
			-- Remain behind the tower if possible.
			if not USSR.IsObjectiveCompleted(DeactivateSecurity) then
				return
			end

			Utils.Do(PrisonerGuards, IdleHunt)
		end)
	end)

	Trigger.OnAllKilled(towerAndGuards, function()
		-- Avoid announcing the Engineers' rescue if they're about to burn.
		if ExecutionBarrel.IsDead then
			return
		end

		Utils.Do(Engineers, function(engineer)
			engineer.Owner = USSR
		end)

		Prisoner6.Owner = USSR
		GetEngineersToCoolant = AddPrimaryObjective(USSR, "engineers-coolant-station")
		ReprogramSecurity = AddSecondaryObjective(USSR, "engineer-reprogram-security")
		-- Unlikely but possible: guards are dead before security deactivation.
		RescueEngineers = RescueEngineers or USSR.AddPrimaryObjective(UserInterface.GetFluentMessage("rescue-engineers"))
		USSR.MarkCompletedObjective(RescueEngineers)
	end)
end

--- Set distant lab structures to self-reveal, crumble, and explode.
--- The original explosions were scheduled for ~291 and ~441 seconds,
--- but that was on a more lenient time limit of ~600 seconds.
local function PrepareLabExplosions()
	-- The time limit changes with difficulty; work backward.
	Trigger.AfterDelay(RemainingTime - DateTime.Seconds(153), function()
		EastLab.Health = EastLab.MaxHealth * 0.2
		EastLab.GrantCondition("unstable")
	end)

	Trigger.AfterDelay(RemainingTime - DateTime.Seconds(63), function()
		WestLab.Health = WestLab.MaxHealth * 0.2
		WestLab.GrantCondition("unstable")
	end)
end

--- Start the countdown and entrance encounter.
local function IntroSequence()
	local StartingUnits = Reinforcements.Reinforce(USSR, StartingUnitsReinforcements, { StartingUnitsSpawn.Location, EntranceTrapWaypoint1.Location }, 0)
	local countdownDelay = DateTime.Seconds(5)
	DateTime.TimeLimit = RemainingTime

	Trigger.AfterDelay(countdownDelay, function()
		Media.PlaySpeechNotification(USSR, "TimerStarted")
		RemainingTime = RemainingTime - countdownDelay
		CountdownEnabled = true
		UpdateTimerText()
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(EntranceGuards, function(actor)
			if not EntranceTrap.IsDead then
				actor.Attack(EntranceTrap)
			end

			actor.AttackMove(EntranceTrapWaypoint1.Location)
			actor.AttackMove(EntranceTrapWaypoint2.Location)
			actor.AttackMove(EntranceTrapWaypoint3.Location)
			actor.Hunt()
		end)
	end)

	Trigger.OnAllKilled(StartingUnits, function()
		DeactivateSecurity = DeactivateSecurity or USSR.AddPrimaryObjective(UserInterface.GetFluentMessage("deactivate-security-system"))

		if not USSR.IsObjectiveCompleted(DeactivateSecurity) then
			MarkAlliedVictory()
		end
	end)

	local eastFlameReveal = { CPos.New(97, 68), CPos.New(97, 69), CPos.New(97, 70) }
	SetBasicFootprint(eastFlameReveal, IsSoviet, function()
		DeactivateSecurity = AddPrimaryObjective(USSR, "deactivate-security-system")
		FreeDogs = AddSecondaryObjective(USSR, "free-dogs")
		RevealArea("Flame Tower East")
	end)
end

WorldLoaded = function()
	Camera.Position = EntranceTrapWaypoint1.CenterPosition
	RevealArea("Entrance")
	InitObjectives(USSR)

	IntroSequence()
	PrepareLabExplosions()
	PrepareBasicReveals()
	PrepareSecurityCenter()
	PrepareRocketSoldiers()
	PrepareCrateMazeGuard()
	PreparePrisonGuards()
	PrepareEngineerStations()

	Trigger.OnKilled(PillboxBarrel, function()
		Pillbox.Kill()
		Utils.Do(Dogs, function(actor)
			actor.Owner = USSR
		end)
		USSR.MarkCompletedObjective(FreeDogs)
	end)

	Trigger.OnAllKilled(Engineers, function()
		-- In case they die before security deactivation.
		RescueEngineers = RescueEngineers or USSR.AddPrimaryObjective(UserInterface.GetFluentMessage("rescue-engineers"))
		MarkAlliedVictory()
	end)

	Trigger.OnTimerExpired(function()
		UserInterface.SetMissionText(UserInterface.GetFluentMessage("too-late"), TimerColor)
		MarkAlliedVictory()
	end)
end

Tick = function()
	if USSR.HasNoRequiredUnits() and CountdownEnabled then
		MarkAlliedVictory()
	end

	if RemainingTime > 0 and CountdownEnabled then
		if (RemainingTime % DateTime.Seconds(1)) == 0 then
			UpdateTimerText()
		end
		RemainingTime = RemainingTime - 1
	end
end
