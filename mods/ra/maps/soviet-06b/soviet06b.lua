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
end)

PrepareResponseCruiser = function()
	local responseBuildings = { Apwr, Tent, Weap }
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

	ResponseCruiser.AttackMove(waypoint94.Location, 2)
	ResponseCruiser.Wait(DateTime.Seconds(90))

	Trigger.OnIdle(ResponseCruiser, function()
		ResponseCruiser.AttackMove(waypoint69.Location, 2)
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

	local breakers = { BridgeBreaker, BridgeBreaker2 }
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
	local goalCells = { CPos.New(85, 10), CPos.New(85, 11), CPos.New(85, 12), CPos.New(86, 13), CPos.New(87, 13), CPos.New(88, 13), CPos.New(88, 14), CPos.New(89, 14), CPos.New(90, 14), CPos.New(90, 15), CPos.New(91, 15), CPos.New(91, 16), CPos.New(91, 17), CPos.New(92, 17), CPos.New(93, 17), CPos.New(94, 17), CPos.New(94, 18), CPos.New(95, 18), CPos.New(96, 18), CPos.New(96, 19), CPos.New(97, 19), CPos.New(98, 19)}

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
	Neutral = Player.GetPlayer("Neutral")

	PrepareObjectives()
	Camera.Position = CameraStart.CenterPosition
	Harvester.FindResources()
	BeginBaseMaintenance()

	if Difficulty ~= "easy" then
		PrepareResponseCruiser()
		Trigger.AfterDelay(1, PrepareBridgeBreakers)
	end

	PrepareTrucks()
	BeginIntro()
	PrepareIdleGuards()
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

