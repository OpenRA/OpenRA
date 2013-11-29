Mission = { }

Mission.PerformHelicopterInsertion = function(owner, helicopterName, passengerNames, enterPosition, unloadPosition, exitPosition)
	local facing = { Map.GetFacing(WPos.op_Subtraction(unloadPosition, enterPosition), 0), "Int32" }
	local altitude = { Actor.TraitInfo(helicopterName, "AircraftInfo").CruiseAltitude, "Int32" }
	local heli = Actor.Create(helicopterName, { Owner = owner, CenterPosition = enterPosition, Facing = facing, Altitude = altitude })
	local cargo = Actor.Trait(heli, "Cargo")
	local passengers = { }
	for i, passengerName in ipairs(passengerNames) do
		local passenger = Actor.Create(passengerName, { AddToWorld = false, Owner = owner })
		cargo:Load(heli, passenger)
		passengers[i] = passenger
	end
	Actor.HeliFly(heli, unloadPosition)
	Actor.Turn(heli, 0)
	Actor.HeliLand(heli, true)
	Actor.UnloadCargo(heli, true)
	Actor.Wait(heli, 125)
	Actor.HeliFly(heli, exitPosition)
	Actor.RemoveSelf(heli)
	return heli, passengers
end

Mission.PerformHelicopterExtraction = function(owner, helicopterName, passengers, enterPosition, loadPosition, exitPosition)
	local facing = { Map.GetFacing(WPos.op_Subtraction(loadPosition, enterPosition), 0), "Int32" }
	local altitude = { Actor.TraitInfo(helicopterName, "AircraftInfo").CruiseAltitude, "Int32" }
	local heli = Actor.Create(helicopterName, { Owner = owner, CenterPosition = enterPosition, Facing = facing, Altitude = altitude })
	local cargo = Actor.Trait(heli, "Cargo")
	Actor.HeliFly(heli, loadPosition)
	Actor.Turn(heli, 0)
	Actor.HeliLand(heli, true)
	Actor.WaitFor(heli, function()
		for i, passenger in ipairs(passengers) do
			if not cargo.Passengers:Contains(passenger) then
				return false
			end
		end
		return true
	end)
	Actor.Wait(heli, 125)
	Actor.HeliFly(heli, exitPosition)
	Actor.RemoveSelf(heli)
	return heli
end

Mission.Reinforce = function(owner, reinforcementNames, enterLocation, rallyPointLocation, interval, onCreateFunc)
	local facing = { Map.GetFacing(CPos.op_Subtraction(rallyPointLocation, enterLocation), 0), "Int32" }
	local ret = { }
	for i = 1, #reinforcementNames do
		local reinforcement = Actor.Create(reinforcementNames[i], { AddToWorld = false, Owner = owner, Location = enterLocation, Facing = facing })
		table.insert(ret, reinforcement)
		OpenRA.RunAfterDelay((i - 1) * interval, function()
			World:Add(reinforcement)
			Actor.MoveNear(reinforcement, rallyPointLocation, 2)
			if onCreateFunc ~= nil then
				onCreateFunc(reinforcement)
			end
		end)
	end
	return ret
end

Mission.Parabomb = function(owner, planeName, enterLocation, bombLocation)
	local facing = { Map.GetFacing(CPos.op_Subtraction(bombLocation, enterLocation), 0), "Int32" }
	local altitude = { Actor.TraitInfo(planeName, "AircraftInfo").CruiseAltitude, "Int32" }
	local plane = Actor.Create(planeName, { Location = enterLocation, Owner = owner, Facing = facing, Altitude = altitude })
	Actor.Trait(plane, "AttackBomber"):SetTarget(bombLocation.CenterPosition)
	Actor.Fly(plane, bombLocation.CenterPosition)
	Actor.FlyOffMap(plane)
	Actor.RemoveSelf(plane)
end

Mission.Paradrop = function(owner, planeName, passengerNames, enterLocation, dropLocation)
	local facing = { Map.GetFacing(CPos.op_Subtraction(dropLocation, enterLocation), 0), "Int32" }
	local altitude = { Actor.TraitInfo(planeName, "AircraftInfo").CruiseAltitude, "Int32" }
	local plane = Actor.Create(planeName, { Location = enterLocation, Owner = owner, Facing = facing, Altitude = altitude })
	Actor.FlyAttackCell(plane, dropLocation)
	Actor.Trait(plane, "ParaDrop"):SetLZ(dropLocation)
	local cargo = Actor.Trait(plane, "Cargo")
	for i, passengerName in ipairs(passengerNames) do
		cargo:Load(plane, Actor.Create(passengerName, { AddToWorld = false, Owner = owner }))
	end
end

Mission.MissionOver = function(winners, losers, setWinStates)
	World:SetLocalPauseState(true)
	World:set_PauseStateLocked(true)
	if winners then
		for i, player in ipairs(winners) do
			Media.PlaySpeechNotification("Win", player)
			if setWinStates then
				OpenRA.SetWinState(player, "Won")
			end
		end
	end
	if losers then
		for i, player in ipairs(losers) do
			Media.PlaySpeechNotification("Lose", player)
			if setWinStates then
				OpenRA.SetWinState(player, "Lost")
			end
		end
	end
	Mission.MissionIsOver = true
end

Mission.GetGroundAttackersOf = function(player)
	return Utils.EnumerableWhere(World.Actors, function(actor)
		return not Actor.IsDead(actor) and Actor.IsInWorld(actor) and Actor.Owner(actor) == player and Actor.HasTrait(actor, "AttackBase") and Actor.HasTrait(actor, "Mobile")
	end)
end

Mission.TickTakeOre = function(player)
	OpenRA.TakeOre(player, 0.01 * OpenRA.GetOreCapacity(player) / 25)
end

Mission.RequiredUnitsAreDestroyed = function(player)
	return Internal.RequiredUnitsAreDestroyed(player)
end