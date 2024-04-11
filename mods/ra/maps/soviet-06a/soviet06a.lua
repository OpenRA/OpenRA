--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Trigger.OnRemovedFromWorld(Mcv, function()
	if McvDeployed or Mcv.IsDead then
		return
	end

	McvDeployed = true
	BuildBase()
	SendReinforcements()

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		ProduceInfantry(Tent)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		ProduceArmor(Weap)
	end)

	local baseAttackers = { BaseAttacker1, BaseAttacker2 }
	Trigger.AfterDelay(DateTime.Minutes(2), function()
		Utils.Do(baseAttackers, IdleHunt)
	end)
end)

PrepareReveals = function()
	local cameraBarrierCells = { CPos.New(65, 39), CPos.New(65, 40), CPos.New(66, 40), CPos.New(66, 41), CPos.New(67, 41), CPos.New(67, 42), CPos.New(68, 42), CPos.New(68, 43), CPos.New(68, 44) }
	local cameraBaseCells = { CPos.New(53, 42), CPos.New(54, 42), CPos.New(54, 41), CPos.New(55, 41), CPos.New(56, 41), CPos.New(56, 40), CPos.New(57, 40), CPos.New(57, 39), CPos.New(58, 39), CPos.New(59, 39), CPos.New(59, 38), CPos.New(60, 38), CPos.New(61, 38) }	
	local cameraBarrierTriggered = false
	local cameraBaseTriggered = false

	Trigger.OnEnteredFootprint(cameraBarrierCells, function(a, id)
		if cameraBarrierTriggered or a.Owner ~= USSR then
			return
		end

		cameraBarrierTriggered = true
		Trigger.RemoveFootprintTrigger(id)
		local cameraBarrier = Actor.Create("camera", true, { Owner = USSR, Location = CameraBarrier.Location })
		Trigger.AfterDelay(DateTime.Seconds(12), cameraBarrier.Destroy)
	end)

	Trigger.OnEnteredFootprint(cameraBaseCells, function(a, id)
		if cameraBaseTriggered or a.Owner ~= USSR then
			return
		end

		cameraBaseTriggered = true
		Trigger.RemoveFootprintTrigger(id)
		local cameraBase1 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase1.Location })
		local cameraBase2 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase2.Location })
		local cameraBase3 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase3.Location })
		local cameraBase4 = Actor.Create("camera", true, { Owner = USSR, Location = CameraBase4.Location })

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			cameraBase1.Destroy()
			cameraBase2.Destroy()
			cameraBase3.Destroy()
			cameraBase4.Destroy()
		end)
	end)
end

PrepareResponseCruiser = function()
	local responseBuildings = { Apwr1, Apwr2, Powr1, Powr2, Weap, Tent }
	local responseOrdered = false

	Utils.Do(responseBuildings, function(building)
		Trigger.OnDamaged(building, function()
			if responseOrdered or USSR.IsObjectiveCompleted(DisruptDome) then
				return
			end

			responseOrdered = true
			OrderResponseCruiser()
		end)
	end)
end

OrderResponseCruiser = function()
	if ResponseCruiser.IsDead then
		return
	end

	Trigger.OnIdle(ResponseCruiser, function()
		ResponseCruiser.AttackMove(waypoint0.Location, 2)
	end)

	Trigger.OnDamaged(ResponseCruiser, function(_, attacker)
		if attacker.IsDead or not ResponseCruiser.CanTarget(attacker) then
			return
		end

		ResponseCruiser.Attack(attacker)
		ResponseCruiser.Scatter()
	end)
end

PrepareBridgeBreakers = function()
	local target = Map.ActorsInCircle(waypoint78.CenterPosition, WDist.New(1536), function(actor)
		return actor.Type == "br3"
	end)[1]

	if not target then
		Media.Debug("No bridge segment found.")
		return
	end

	local orderSent = false
	Trigger.AfterDelay(DateTime.Seconds(30), function()
		orderSent = true
		OrderBridgeBreakers(target)
	end)

	local bridgeEntryCells = { CPos.New(75, 30), CPos.New(76, 30), CPos.New(77, 30) }
	Trigger.OnEnteredFootprint(bridgeEntryCells, function(a, id)
		if a.Owner ~= USSR then
			return
		end

		Trigger.RemoveFootprintTrigger(id)

		if not orderSent then
			OrderBridgeBreakers(target, "with bridge reveal")
		end
	end)
end

OrderBridgeBreakers = function(target, reveal)
	if target.IsDead then
		return
	end

	local breakers = { BridgeBreaker1, BridgeBreaker2 }
	Utils.Do(breakers, function(breaker)
		if breaker.IsDead then
			return
		end

		breaker.Stop()
		breaker.Attack(target, true, true)
	end)

	if not reveal then
		return
	end

	local camera = Actor.Create("camera", true, { Owner = USSR, Location = target.Location })
	Trigger.OnKilled(target, function()
		Trigger.AfterDelay(DateTime.Seconds(2), camera.Destroy)
	end)
end

PrepareObjectives = function()
	InitObjectives(USSR)
	KillTrucks = AddPrimaryObjective(Greece, "")
	EscortConvoy = AddPrimaryObjective(USSR, "escort-convoy")
	DisruptDome = AddSecondaryObjective(USSR, "destroy-capture-radar-dome-reinforcements")
	SaveAllTrucks = AddSecondaryObjective(USSR, "keep-trucks-alive")

	Trigger.OnKilledOrCaptured(Dome, function()
		-- Let the capture notification play first.
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			USSR.MarkCompletedObjective(DisruptDome)
			Media.PlaySpeechNotification(USSR, "ObjectiveMet")
		end)
	end)
end

PrepareTrucks = function()
	local trucks = { Truck1, Truck2 }
	local goalCells = { CPos.New(83, 7), CPos.New(83, 8), CPos.New(83, 9), CPos.New(83, 10), CPos.New(84, 10), CPos.New(84, 11), CPos.New(84, 12), CPos.New(85, 12), CPos.New(86, 12), CPos.New(87, 12), CPos.New(87, 13), CPos.New(88, 13), CPos.New(89, 13), CPos.New(90, 13), CPos.New(90, 14), CPos.New(90, 15), CPos.New(91, 15), CPos.New(92, 15), CPos.New(93, 15), CPos.New(94, 15) }

	local goalTriggered = false
	Trigger.OnEnteredFootprint(goalCells, function(a)
		if not goalTriggered and a.Owner == USSR and a.Type == "truk" then
			goalTriggered = true
			USSR.MarkCompletedObjective(EscortConvoy)
			USSR.MarkCompletedObjective(SaveAllTrucks)
		end
	end)

	Trigger.OnAllKilled(trucks, function()
		Greece.MarkCompletedObjective(KillTrucks)
	end)

	Trigger.OnAnyKilled(trucks, function()
		USSR.MarkFailedObjective(SaveAllTrucks)
	end)
end

BeginIntro = function()
	local introAttackers = { IntroEnemy1, IntroEnemy2, IntroEnemy3 }
	local sovietReinforcements1 = { "e6", "e6", "e6", "e6", "e6" }
	local sovietReinforcements2 = { "e4", "e4", "e2", "e2", "e2" }
	local sovietReinforcements1Path = { McvWaypoint.Location, APCWaypoint1.Location }
	local sovietReinforcements2Path = { McvWaypoint.Location, APCWaypoint2.Location }

	Mcv.Move(McvWaypoint.Location)
	Utils.Do(introAttackers, IdleHunt)
	Reinforcements.ReinforceWithTransport(USSR, "apc", sovietReinforcements1, sovietReinforcements1Path)
	Reinforcements.ReinforceWithTransport(USSR, "apc", sovietReinforcements2, sovietReinforcements2Path)
end

PrepareIdleGuards = function()
	local lazyUnits = Utils.Where(Greece.GetGroundAttackers(), function(unit)
		return unit.Type ~= "ca" and unit.Type ~= "arty"
	end)

	Utils.Do(lazyUnits, function(unit)
		local triggered = false

		Trigger.OnDamaged(unit, function()
			if triggered then
				return
			end

			triggered = true
			IdleHunt(unit)
		end)
	end)
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	PrepareReveals()
	PrepareObjectives()
	Camera.Position = CameraStart.CenterPosition
	Harvester.FindResources()
	BeginBaseMaintenance()

	if Difficulty ~= "easy" then
		PrepareResponseCruiser()
		Trigger.AfterDelay(1, PrepareBridgeBreakers)
	end

	if Difficulty == "hard" then
		BuildNavyPatrol()
	end

	PrepareTrucks()
	BeginIntro()
	PrepareIdleGuards()
end

BuildNavyPatrol = function()
	local types = { "dd", "dd" }
	local patrolPath = { NavyPatrol1.Location, NavyPatrol2.Location, NavyPatrol3.Location, NavyPatrol4.Location }

	Greece.Build(types, function(units)
		Utils.Do(units, function(u)
			u.Patrol(patrolPath, true, 100)
		end)

		Trigger.OnAllKilled(units, function()
			if not Greece.HasPrerequisites({ "syrd", "dome" }) then
				return
			end

			BuildNavyPatrol()
		end)
	end)
end

Tick = function()
	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(KillTrucks)
	end

	if Greece.Resources >= Greece.ResourceCapacity * 0.75 then
		Greece.Cash = Greece.Cash + Greece.Resources - Greece.ResourceCapacity * 0.25
		Greece.Resources = Greece.ResourceCapacity * 0.25
	end
end
