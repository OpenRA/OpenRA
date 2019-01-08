--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--Boolean Vars
baseDiscovered = false
AtEndGame = false

--Basic Vars
DifficultySetting = Map.LobbyOption("difficulty")
TimerColor = Player.GetPlayer("Spain").Color
InsertionHelicopterType = "tran.insertion"
TimerTicks = DateTime.Minutes(18) -- 18 minutes is roughly 30 mins in the original game
ticks = TimerTicks

--Table Vars
TankPath = { waypoint12.Location, waypoint13.Location }
InsertionPath = { waypoint12.Location, waypoint0.Location }
AlliedBase = { WarFactory, PillBox1, PillBox2, Refinery, PowerPlant1, PowerPlant2, RepairPad, OreSilo, Barracks, RadarDome }
AlliedForces = { "2tnk" , "2tnk", "mcv" }
ChopperTeam = { "e1r1", "e1r1", "e2", "e2", "e1r1" }

SendTanks = function() 
	Media.PlaySpeechNotification(allies, "ReinforcementsArrived")
	Reinforcements.Reinforce(allies, AlliedForces, TankPath, DateTime.Seconds(1))
end

SendInsertionHelicopter = function()
	Media.PlaySpeechNotification(allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(allies, InsertionHelicopterType, ChopperTeam, InsertionPath, { waypoint4.Location })
end

FinishTimer = function()
	for i = 0, 9, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end
		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("Allied forces have arrived!", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() UserInterface.SetMissionText("") end)
end

TimerExpired = function()
	allies.MarkCompletedObjective(SurviveObjective)
end

DiscoveredAlliedBase = function(actor, discoverer)
	if (not baseDiscovered and discoverer.Owner == allies) then
		baseDiscovered = true  
		Media.PlaySpeechNotification(allies, "ObjectiveReached")
		Utils.Do(AlliedBase, function(building)
			building.Owner = allies
		end)

		--Need to delay this so we don't fail mission before obj added
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			SurviveObjective = allies.AddPrimaryObjective("Defend outpost until reinforcements arrive.")
			SetupTimeNotifications()
			Trigger.OnAllRemovedFromWorld(AlliedBase, function()
				allies.MarkFailedObjective(SurviveObjective)
			end)
			Media.PlaySpeechNotification(allies, "TimerStarted")
			Trigger.AfterDelay(DateTime.Seconds(2), function() allies.MarkCompletedObjective(DiscoverObjective) end)
			creeps.GetActorsByType("harv")[1].FindResources()
			creeps.GetActorsByType("harv")[1].Owner = allies
		end)
	end
end

SetupTimeNotifications = function()
		Trigger.AfterDelay(DateTime.Minutes(8), function()
			Media.PlaySpeechNotification(allies, "TenMinutesRemaining")
		end)
		Trigger.AfterDelay(DateTime.Minutes(13), function()
			Media.PlaySpeechNotification(allies, "WarningFiveMinutesRemaining")
		end)

		Trigger.AfterDelay(DateTime.Minutes(14), function()
			Media.PlaySpeechNotification(allies, "WarningFourMinutesRemaining")
		end)

		Trigger.AfterDelay(DateTime.Minutes(15), function()
			Media.PlaySpeechNotification(allies, "WarningThreeMinutesRemaining")
		end)

		Trigger.AfterDelay(DateTime.Minutes(16), function()
			Media.PlaySpeechNotification(allies, "WarningTwoMinutesRemaining")
		end)

		Trigger.AfterDelay(DateTime.Minutes(17), function()
			Media.PlaySpeechNotification(allies, "WarningOneMinuteRemaining")
		end)

		Trigger.AfterDelay(DateTime.Minutes(17) + DateTime.Seconds(40), function()
			Media.PlaySpeechNotification(allies, "AlliedForcesApproaching")
		end)
end

GetTicks = function()
	return ticks
end

Tick = function() 
	if SurviveObjective ~= nil then
		if ticks > 0 then
			if ticks == DateTime.Minutes(17) then
				StartAntAttack()
			elseif ticks == DateTime.Minutes(15) then
				if DifficultySetting ~= "hard" then
					SendInsertionHelicopter()
				end
			elseif ticks == DateTime.Minutes(12) then
				StartAntAttack()
			elseif ticks == DateTime.Minutes(6) then
				StartAntAttack()
			elseif ticks == DateTime.Minutes(1) then
				EndAntAttack()
			end

			ticks = ticks - 1;
			UserInterface.SetMissionText("Reinforcements arrive in " .. Utils.FormatTime(ticks), TimerColor)
		else
			if not AtEndGame then
				Media.PlaySpeechNotification(allies, "SecondObjectiveMet")
				AtEndGame = true
				FinishTimer()
				Camera.Position = waypoint13.CenterPosition
				SendTanks()
				Trigger.AfterDelay(DateTime.Seconds(2), function() TimerExpired() end)
			end
			ticks = ticks - 1
		end
	end
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	DiscoverObjective = allies.AddPrimaryObjective("Find the outpost.")

	Utils.Do(AlliedBase, function(actor)
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(8), function(discoverer, id)
			DiscoveredAlliedBase(actor, discoverer)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		creeps.GetActorsByType("harv")[1].Stop()
	end)

	Trigger.OnObjectiveCompleted(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(allies, function()
		Media.PlaySpeechNotification(allies, "MissionFailed")
	end)

	Trigger.OnPlayerWon(allies, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(allies, "MissionAccomplished")  end)
	end)

	Camera.Position = Ranger.CenterPosition
end

WorldLoaded = function()
	allies = Player.GetPlayer("Spain")
	creeps = Player.GetPlayer("Creeps")
	InitObjectives()
	InitEnemyPlayers()
	Trigger.OnKilled(MoneyDerrick, function()
		Actor.Create("moneycrate", true, { Owner = allies, Location = MoneyDerrick.Location + CVec.New(1,0) })
	end)
end
