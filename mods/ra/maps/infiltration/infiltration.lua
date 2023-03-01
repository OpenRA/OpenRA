--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	Utils.Do(Humans, function(player)
		if player then
			player.MarkFailedObjective(SecureLab)
		end
	end)
end

TimerStarted = false
StartTimer = function()
	Utils.Do(Humans, function(player)
		if player.IsLocalPlayer then
			TimerColor = player.Color
		end
	end)
	CountDownTimerAnnouncements()
	Ticked = TimerTicks
	TimerStarted = true
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Utils.Do(Humans, function(player)
			Media.PlaySpeechNotification(player, "TimerStarted")
		end)
	end)
end

CountDownTimerAnnouncements = function()
	for i = #Announcements, 1, -1 do
		local delay = TimerTicks - Announcements[i].delay
		Trigger.AfterDelay(delay, function()
			if not LabSecured then
				Utils.Do(Humans, function(player)
					Media.PlaySpeechNotification(player, Announcements[i].speech)
				end)
			end
		end)
	end
end

ReinforcementsHaveArrived = false
LabInfiltrated = function()
	Utils.Do(Humans, function(player)
		if player then
			SecureLab = AddPrimaryObjective(player, "secure-laboratory-guards")
			DestroyBase = AddPrimaryObjective(player, "destroy-remaining-soviet-presence")
			player.MarkCompletedObjective(InfiltrateLab)
			Trigger.ClearAll(Lab)
			Trigger.AfterDelay(0, function()
				Trigger.OnKilled(Lab, SecureLabFailed)
			end)
		end
	end)

	Camera.Position = ReinforcementsUnloadPoint.CenterPosition
	local entryPath = { ReinforcementsEntryPoint.Location, ReinforcementsUnloadPoint.Location }
	local exit = { ReinforcementsEntryPoint.Location }

	local mcvActors = { "mcv" }
	if Allies2 then
		mcvActors = { "mcv", "mcv" }
	end

	local reinforcements = Reinforcements.ReinforceWithTransport(Allies, TransportType, mcvActors, entryPath, exit)
	local mcvs = reinforcements[2]

	Trigger.OnAddedToWorld(mcvs[1], function(mcvUnloaded)

		-- Don't call this twice (because of the owner change)
		if mcvUnloaded.Owner == Allies1 then
			return
		end

		mcvUnloaded.Owner = Allies1
		if not Allies2 then
			Allies1.Cash = 5000
		end
		Media.PlaySpeechNotification(Allies1, "AlliedReinforcementsSouth")
		StartTimer()
		HijackTruck.Destroy()
		ReinforcementsHaveArrived = true
	end)

	if Allies2 then
		Trigger.OnAddedToWorld(mcvs[2], function(mcvUnloaded)

			-- Don't call this twice (because of the owner change)
			if mcvUnloaded.Owner == Allies2 then
				return
			end

			mcvUnloaded.Owner = Allies2
			Allies1.Cash = 2500
			Allies2.Cash = 2500
		end)
	end

	Utils.Do(Humans, function(player)
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
	Utils.Do(Humans, function(player)
		if player then
			player.MarkFailedObjective(InfiltrateLab)
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
	Utils.Do(Humans, function(player)
		if player then
			InfiltrateLab = AddPrimaryObjective(player, "infiltrate-laboratory")
		end
	end)

	Trigger.OnKilled(Lab, function()
		if not Allies1.IsObjectiveCompleted(InfiltrateLab) then
			InfiltrateLabFailed()
		end
	end)

	-- The delay isn't purely cosmetic, but also prevents a System.InvalidOperationException
	-- "Collection was modified after the enumerator was instantiated." in tick_activities
	local infiltrationCount = 0
	Trigger.OnInfiltrated(Lab, function()
		infiltrationCount = infiltrationCount + 1

		if (Allies2 and infiltrationCount == 2) or not Allies2 then
			Trigger.AfterDelay(DateTime.Seconds(3), LabInfiltrated)
		end
	end)

	local spyActors = { "spy.strong" }
	if Allies2 then
		spyActors = { "spy.strong", "spy.strong" }
	end

	local entryPath = { SpyReinforcementsEntryPoint.Location, SpyReinforcementsUnloadPoint.Location }
	local exit = { SpyReinforcementsExitPoint.Location }
	local reinforcements = Reinforcements.ReinforceWithTransport(Allies, TransportType, spyActors, entryPath, exit)

	local transport = reinforcements[1]
	Camera.Position = transport.CenterPosition

	local spies = reinforcements[2]
	Trigger.OnAnyKilled(spies, InfiltrateLabFailed)

	ChangeOwnerOnAddedToWorld(spies[1], Allies1)

	if Allies2 then
		ChangeOwnerOnAddedToWorld(spies[2], Allies2)
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
	local guard = Actor.Create(actorType, true, { Owner = Soviets, Location = start })
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

Ticked = 0
SecureLabTimer = function()
	if not TimerStarted or LabSecured then
		return
	end

	if Ticked > 0 then
		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("secure-lab-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked <= 0 then
		TimerColor = Soviets.Color
		UserInterface.SetMissionText(UserInterface.Translate("soviet-research-lab-not-secured-in-time"), TimerColor)
		SecureLabFailed()
	end
end

SovietBaseMaintenanceSetup = function()
	local sovietbuildings = Utils.Where(Map.NamedActors, function(a)
		return a.Owner == Soviets and a.HasProperty("StartBuildingRepairs")
	end)

	Trigger.OnAllKilledOrCaptured(sovietbuildings, function()
		Utils.Do(Humans, function(player)
			player.MarkCompletedObjective(DestroyBase)
		end)
	end)

	Utils.Do(sovietbuildings, function(sovietbuilding)
		Trigger.OnDamaged(sovietbuilding, function(building)
			if building.Owner == Soviets and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

CheckPlayerDefeat = function()
	if not ReinforcementsHaveArrived then
		return
	end

	Utils.Do(Humans, function(player)
		if player.HasNoRequiredUnits() then
			player.MarkFailedObjective(DestroyBase)
		end
	end)
end

LabSecured = false
CheckLabSecured = function()
	if not ReinforcementsHaveArrived or LabSecured then
		return
	end

	if Allies1.HasNoRequiredUnits() or (Allies2 and Allies2.HasNoRequiredUnits()) then
		Utils.Do(Humans, function(player)
			player.MarkFailedObjective(SecureLab)
		end)
	end

	local radius = WDist.FromCells(10)
	local labGuards = Utils.Where(Map.ActorsInCircle(LabWaypoint.CenterPosition, radius), function(a)
		return a.Owner == Soviets and a.HasProperty("Move")
	end)

	if #labGuards < 1 then
		LabSecured = true
		Utils.Do(Humans, function(player)
			player.MarkCompletedObjective(SecureLab)
		end)
		UserInterface.SetMissionText("")
	end
end

Tick = function()
	CapOre(Soviets)
	SecureLabTimer()
	CheckLabSecured()
	CheckPlayerDefeat()
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	Neutral = Player.GetPlayer("Neutral")
	Creeps = Player.GetPlayer("Creeps")
	Soviets = Player.GetPlayer("Soviets")

	Allies1 = Player.GetPlayer("Allies1")
	Allies2 = Player.GetPlayer("Allies2")
	Humans = { Allies1, Allies2 }

	Utils.Do(Humans, function(player)
		if player and player.IsLocalPlayer then
			InitObjectives(player)
		end
	end)

	InsertSpies()
	AttackTown()
	SetupPatrols()
	SovietBaseMaintenanceSetup()
end
