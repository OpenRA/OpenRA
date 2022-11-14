--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	Media.PlaySpeechNotification(Allies, "ReinforcementsArrived")
	Reinforcements.Reinforce(Allies, AlliedForces, TankPath, DateTime.Seconds(1))
end

SendInsertionHelicopter = function()
	Media.PlaySpeechNotification(Allies, "AlliedReinforcementsSouth")
	Reinforcements.ReinforceWithTransport(Allies, InsertionHelicopterType, ChopperTeam, InsertionPath, { waypoint4.Location })
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
	Allies.MarkCompletedObjective(SurviveObjective)
end

DiscoveredAlliedBase = function(actor, discoverer)
	if (not baseDiscovered and discoverer.Owner == Allies) then
		baseDiscovered = true
		Media.PlaySpeechNotification(Allies, "ObjectiveReached")
		Utils.Do(AlliedBase, function(building)
			building.Owner = Allies
		end)

		--Need to delay this so we don't fail mission before obj added
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			SurviveObjective = Allies.AddObjective("Defend outpost until reinforcements arrive.")
			SetupTimeNotifications()
			Trigger.OnAllRemovedFromWorld(AlliedBase, function()
				Allies.MarkFailedObjective(SurviveObjective)
			end)
			Media.PlaySpeechNotification(Allies, "TimerStarted")
			Trigger.AfterDelay(DateTime.Seconds(2), function() Allies.MarkCompletedObjective(DiscoverObjective) end)
			Creeps.GetActorsByType("harv")[1].FindResources()
			Creeps.GetActorsByType("harv")[1].Owner = Allies
		end)
	end
end

SetupTimeNotifications = function()
	Trigger.AfterDelay(DateTime.Minutes(8), function()
		Media.PlaySpeechNotification(Allies, "TenMinutesRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(13), function()
		Media.PlaySpeechNotification(Allies, "WarningFiveMinutesRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(14), function()
		Media.PlaySpeechNotification(Allies, "WarningFourMinutesRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(15), function()
		Media.PlaySpeechNotification(Allies, "WarningThreeMinutesRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(16), function()
		Media.PlaySpeechNotification(Allies, "WarningTwoMinutesRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(17), function()
		Media.PlaySpeechNotification(Allies, "WarningOneMinuteRemaining")
	end)

	Trigger.AfterDelay(DateTime.Minutes(17) + DateTime.Seconds(40), function()
		Media.PlaySpeechNotification(Allies, "AlliedForcesApproaching")
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
				SendInsertionHelicopter()
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
				Media.PlaySpeechNotification(Allies, "SecondObjectiveMet")
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

AddObjectives = function()
	InitObjectives(Allies)

	DiscoverObjective = Allies.AddObjective("Find the outpost.")

	Utils.Do(AlliedBase, function(actor)
		Trigger.OnEnteredProximityTrigger(actor.CenterPosition, WDist.FromCells(8), function(discoverer, id)
			DiscoveredAlliedBase(actor, discoverer)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Creeps.GetActorsByType("harv")[1].Stop()
	end)

	Camera.Position = Ranger.CenterPosition
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Spain")
	AntMan = Player.GetPlayer("AntMan")
	Creeps = Player.GetPlayer("Creeps")
	AddObjectives()
	Trigger.OnKilled(MoneyDerrick, function()
		Actor.Create("moneycrate", true, { Owner = Allies, Location = MoneyDerrick.Location + CVec.New(1,0) })
	end)
	Trigger.OnKilled(MoneyBarrel, function()
		Actor.Create("moneycrate", true, { Owner = Allies, Location = MoneyBarrel.Location})
	end)
end
