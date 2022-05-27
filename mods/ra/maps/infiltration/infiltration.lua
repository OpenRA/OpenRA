--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "hard" then
	TimerTicks = DateTime.Minutes(25)
elseif Difficulty == "normal" then
	TimerTicks = DateTime.Minutes(28)
else
	TimerTicks = DateTime.Minutes(31)
end

Announcements =
{
	{ speech = "TwentyMinutesRemaining", delay = DateTime.Minutes(20) },
	{ speech = "TenMinutesRemaining", delay = DateTime.Minutes(10) },
	{ speech = "WarningFiveMinutesRemaining", delay = DateTime.Minutes(5) },
	{ speech = "WarningFourMinutesRemaining", delay = DateTime.Minutes(4) },
	{ speech = "WarningThreeMinutesRemaining", delay = DateTime.Minutes(3) },
	{ speech = "WarningTwoMinutesRemaining", delay = DateTime.Minutes(2) },
	{ speech = "WarningOneMinuteRemaining", delay = DateTime.Minutes(1) }
}

TownAttackers = { TownAttacker1, TownAttacker2, TownAttacker3, TownAttacker4, TownAttacker5, TownAttacker6, TownAttacker7 }

PatrolPoints1 = { PatrolPoint11.Location, PatrolPoint12.Location, PatrolPoint13.Location, PatrolPoint14.Location, PatrolPoint15.Location }
PatrolPoints2 = { PatrolPoint21.Location, PatrolPoint22.Location, PatrolPoint23.Location, PatrolPoint24.Location, PatrolPoint25.Location }
PatrolPoints3 = { PatrolPoint31.Location, PatrolPoint32.Location, PatrolPoint33.Location, PatrolPoint34.Location }
PatrolPoints4 = { PatrolPoint41.Location, PatrolPoint42.Location, PatrolPoint43.Location, PatrolPoint44.Location, PatrolPoint45.Location }

Patrol1 = { "e1", "e1", "e1", "e1", "e1" }
Patrol2 = { "e1", "dog.patrol", "dog.patrol" }
Patrol3 = { "e1", "e1", "dog.patrol" }

TransportType = "lst.unselectable.unloadonly"

SecureLabFailed = function()
	Utils.Do(humans, function(player)
		if player then
			player.MarkFailedObjective(secureLab)
		end
	end)
end

timerStarted = false
StartTimer = function()
	Utils.Do(humans, function(player)
		if player.IsLocalPlayer then
			TimerColor = player.Color
		end
	end)
	CountDownTimerAnnouncements()
	ticked = TimerTicks
	timerStarted = true
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(humans, function(player)
			Media.PlaySpeechNotification(player, "TimerStarted")
		end)
	end)
end

CountDownTimerAnnouncements = function()
	for i = #Announcements, 1, -1 do
		local delay = TimerTicks - Announcements[i].delay
		Trigger.AfterDelay(delay, function()
			if not labSecured then
				Utils.Do(humans, function(player)
					Media.PlaySpeechNotification(player, Announcements[i].speech)
				end)
			end
		end)
	end
end

reinforcementsHaveArrived = false
LabInfiltrated = function()
	Utils.Do(humans, function(player)
		if player then
			secureLab = player.AddObjective("Secure the laboratory by eliminating its guards.")
			destroyBase = player.AddObjective("Destroy the remaining Soviet presence.")
			player.MarkCompletedObjective(infiltrateLab)
			Trigger.ClearAll(Lab)
			Trigger.AfterDelay(0, function()
				Trigger.OnKilled(Lab, SecureLabFailed)
			end)
		end
	end)

	Camera.Position = ReinforcementsUnloadPoint.CenterPosition
	local entryPath = { ReinforcementsEntryPoint.Location, ReinforcementsUnloadPoint.Location }
	local exit = { ReinforcementsEntryPoint.Location }

	mcvActors = { "mcv" }
	if player2 then
		mcvActors = { "mcv", "mcv" }
	end

	local reinforcements = Reinforcements.ReinforceWithTransport(allies, TransportType, mcvActors, entryPath, exit)
	local mcvs = reinforcements[2]

	Trigger.OnAddedToWorld(mcvs[1], function(mcvUnloaded)

		-- Don't call this twice (because of the owner change)
		if mcvUnloaded.Owner == player1 then
			return
		end

		mcvUnloaded.Owner = player1
		if not player2 then
			player1.Cash = 5000
		end
		Media.PlaySpeechNotification(player, "AlliedReinforcementsSouth")
		StartTimer()
		HijackTruck.Destroy()
		reinforcementsHaveArrived = true
	end)

	if player2 then
		Trigger.OnAddedToWorld(mcvs[2], function(mcvUnloaded)

			-- Don't call this twice (because of the owner change)
			if mcvUnloaded.Owner == player2 then
				return
			end

			mcvUnloaded.Owner = player2
			player1.Cash = 2500
			player2.Cash = 2500
		end)
	end

	Utils.Do(humans, function(player)
		for i = 0, 2 do
			Trigger.AfterDelay(DateTime.Seconds(i), function()
				Media.PlaySoundNotification(player, "AlertBuzzer")
			end)
		end
	end)

	if BridgeTank.IsDead then
		return
	end

	local attackPoint = BridgeAttackPoint.CenterPosition
	local radius = WDist.FromCells(5)
	local bridge = Map.ActorsInCircle(attackPoint, radius, function(actor)
		return actor.Type == "br3"
	end)[1]
	BridgeTank.Attack(bridge, true, true)
end

InfiltrateLabFailed = function()
	Utils.Do(humans, function(player)
		if player then
			player.MarkFailedObjective(infiltrateLab)
		end
	end)
end

ChangeOwnerOnAddedToWorld = function(actor, newOwner)
	Trigger.OnAddedToWorld(actor, function(unloadedActor)
		unloadedActor.Owner = newOwner
		Trigger.Clear(unloadedActor, "OnAddedToWorld")
	end)
end

InsertSpies = function()
	Utils.Do(humans, function(player)
		if player then
			infiltrateLab = player.AddObjective("Get our spy into the laboratory undetected.")
		end
	end)

	Trigger.OnKilled(Lab, function()
		if not player1.IsObjectiveCompleted(infiltrateLab) then
			InfiltrateLabFailed()
		end
	end)

	-- The delay isn't purely cosmetic, but also prevents a System.InvalidOperationException
	-- "Collection was modified after the enumerator was instantiated." in tick_activities
	local infiltrationCount = 0
	Trigger.OnInfiltrated(Lab, function()
		infiltrationCount = infiltrationCount + 1

		if (player2 and infiltrationCount == 2) or not player2 then
			Trigger.AfterDelay(DateTime.Seconds(3), LabInfiltrated)
		end
	end)

	spyActors = { "spy.strong" }
	if player2 then
		spyActors = { "spy.strong", "spy.strong" }
	end

	local entryPath = { SpyReinforcementsEntryPoint.Location, SpyReinforcementsUnloadPoint.Location }
	local exit = { SpyReinforcementsExitPoint.Location }
	local reinforcements = Reinforcements.ReinforceWithTransport(allies, TransportType, spyActors, entryPath, exit)

	local transport = reinforcements[1]
	Camera.Position = transport.CenterPosition

	spies = reinforcements[2]
	Trigger.OnAnyKilled(spies, InfiltrateLabFailed)

	ChangeOwnerOnAddedToWorld(spies[1], player1)

	if player2 then
		ChangeOwnerOnAddedToWorld(spies[2], player2)
	end
end

StopHunt = function(unit)
	if not unit.IsDead then
		unit.Stop()
		Trigger.Clear(unit, "OnIdle")
	end
end

AttackTown = function()
	Utils.Do(TownAttackers, IdleHunt)

	Trigger.OnRemovedFromWorld(Hospital, function()
		Utils.Do(TownAttackers, StopHunt)
	end)
end

CapOre = function(player)
	if player.Resources > player.ResourceCapacity * 0.9 then
		player.Resources = player.ResourceCapacity * 0.8
	end
end

NewPatrol = function(actorType, start, waypoints)
	local guard = Actor.Create(actorType, true, { Owner = soviets, Location = start })
	guard.Patrol(waypoints, true, Utils.RandomInteger(50, 75))
end

SetupPatrols = function()
	Utils.Do(Patrol1, function(patrol1) NewPatrol(patrol1, PatrolPoints1[1], PatrolPoints1) end)
	Utils.Do(Patrol2, function(patrol2) NewPatrol(patrol2, PatrolPoints1[3], PatrolPoints1) end)
	Utils.Do(Patrol2, function(patrol3) NewPatrol(patrol3, PatrolPoints3[1], PatrolPoints3) end)
	Utils.Do(Patrol2, function(patrol4) NewPatrol(patrol4, PatrolPoints4[1], PatrolPoints4) end)

	if Difficulty == "hard" then
		Utils.Do(Patrol3, function(patrol5) NewPatrol(patrol5, PatrolPoints2[1], PatrolPoints2) end)
	end

	local checkpoint = { BaseGuardTruckPos.Location }
	Trigger.OnEnteredFootprint(checkpoint, function(a, id)
		Trigger.RemoveFootprintTrigger(id)
		if not BaseGuard.IsDead then
			BaseGuard.ScriptedMove(BaseGuardMovePos.Location)
		end
	end)
end

ticked = 0
SecureLabTimer = function()
	if not timerStarted or labSecured then
		return
	end

	if ticked > 0 then
		UserInterface.SetMissionText("Secure lab in: " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked <= 0 then
		TimerColor = soviets.Color
		UserInterface.SetMissionText("The Soviet research laboratory was not secured in time.", TimerColor)
		SecureLabFailed()
	end
end

SovietBaseMaintenanceSetup = function()
	local sovietbuildings = Utils.Where(Map.NamedActors, function(a)
		return a.Owner == soviets and a.HasProperty("StartBuildingRepairs")
	end)

	Trigger.OnAllKilledOrCaptured(sovietbuildings, function()
		Utils.Do(humans, function(player)
			player.MarkCompletedObjective(destroyBase)
		end)
	end)

	Utils.Do(sovietbuildings, function(sovietbuilding)
		Trigger.OnDamaged(sovietbuilding, function(building)
			if building.Owner == soviets and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

CheckPlayerDefeat = function()
	if not reinforcementsHaveArrived then
		return
	end

	Utils.Do(humans, function(player)
		if player.HasNoRequiredUnits() then
			player.MarkFailedObjective(destroyBase)
		end
	end)
end

labSecured = false
CheckLabSecured = function()
	if not reinforcementsHaveArrived or labSecured then
		return
	end

	if player1.HasNoRequiredUnits() or (player2 and player2.HasNoRequiredUnits()) then
		Utils.Do(humans, function(player)
			player.MarkFailedObjective(secureLab)
		end)
	end

	local radius = WDist.FromCells(10)
	local labGuards = Utils.Where(Map.ActorsInCircle(LabWaypoint.CenterPosition, radius), function(a)
		return a.Owner == soviets and a.HasProperty("Move")
	end)

	if #labGuards < 1 then
		labSecured = true
		Utils.Do(humans, function(player)
			player.MarkCompletedObjective(secureLab)
		end)
		UserInterface.SetMissionText("")
	end
end

Tick = function()
	CapOre(soviets)
	SecureLabTimer()
	CheckLabSecured()
	CheckPlayerDefeat()
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	neutral = Player.GetPlayer("Neutral")
	creeps = Player.GetPlayer("Creeps")
	soviets = Player.GetPlayer("Soviets")

	player1 = Player.GetPlayer("Allies1")
	player2 = Player.GetPlayer("Allies2")
	humans = { player1, player2 }

	Utils.Do(humans, function(player)
		if player and player.IsLocalPlayer then
			InitObjectives(player)
		end
	end)

	InsertSpies()
	AttackTown()
	SetupPatrols()
	SovietBaseMaintenanceSetup()
end
