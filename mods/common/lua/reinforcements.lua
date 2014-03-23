Reinforcements = { }

Reinforcements.Insert = function(owner, transportName, passengerNames, enterPath, exitPath)
	local facing = { Map.GetFacing(CPos.op_Subtraction(enterPath[2], enterPath[1]), 0), "Int32" }
	local center = WPos.op_Addition(enterPath[1].CenterPosition, WVec.New(0, 0, Rules.InitialAltitude(transportName)))
	local transport = Actor.Create(transportName, { Owner = owner, Location = enterPath[1], CenterPosition = center, Facing = facing })
	local cargo = Actor.Trait(transport, "Cargo")
	local passengers = { }

	for i, passengerName in ipairs(passengerNames) do
		local passenger = Actor.Create(passengerName, { AddToWorld = false, Owner = owner })
		passengers[i] = passenger
		cargo:Load(transport, passenger)
	end

	Utils.Do(Utils.Skip(enterPath, 1), function(l) Actor.ScriptedMove(transport, l) end)
	Actor.AfterMove(transport)
	Actor.UnloadCargo(transport, true)
	Actor.Wait(transport, 25)
	Utils.Do(exitPath, function(l) Actor.ScriptedMove(transport, l) end)
	Actor.RemoveSelf(transport)
	return transport, passengers
end

Reinforcements.Extract = function(owner, transportName, passengerNames, enterPath, exitPath)
	local facing = { Map.GetFacing(CPos.op_Subtraction(enterPath[2], enterPath[1]), 0), "Int32" }
	local center = WPos.op_Addition(enterPath[1].CenterPosition, WVec.New(0, 0, Rules.InitialAltitude(transportName)))
	local transport = Actor.Create(transportName, { Owner = owner, Location = enterPath[1], CenterPosition = center, Facing = facing })
	local cargo = Actor.Trait(transport, "Cargo")

	Utils.Do(Utils.Skip(enterPath, 1), function(l) Actor.ScriptedMove(transport, l) end)
	Actor.AfterMove(transport)
	Actor.WaitFor(transport, function()
		return Utils.All(passengerNames, function(passenger) return cargo.Passengers:Contains(passenger) end)
	end)

	Actor.Wait(transport, 125)
	Utils.Do(exitPath, function(l) Actor.ScriptedMove(transport, l) end)
	Actor.RemoveSelf(transport)
	return transport
end

Reinforcements.Reinforce = function(owner, reinforcementNames, enterLocation, rallyPointLocation, interval, onCreateFunc)
	local facing = { Map.GetFacing(CPos.op_Subtraction(rallyPointLocation, enterLocation), 0), "Int32" }
	local reinforcements = { }
	for i, reinforcementName in ipairs(reinforcementNames) do
		local reinforcement = Actor.Create(reinforcementName, { AddToWorld = false, Owner = owner, Location = enterLocation, Facing = facing })
		reinforcements[i] = reinforcement
		OpenRA.RunAfterDelay((i - 1) * interval, function()
			World:Add(reinforcement)
			Actor.MoveNear(reinforcement, rallyPointLocation, 2)
			if onCreateFunc ~= nil then
				onCreateFunc(reinforcement)
			end
		end)
	end
	return reinforcements
end

Reinforcements.ReinforceWithCargo = function(owner, actorName, path, cargoNames, actionFunc)
	local facing = { Map.GetFacing(CPos.op_Subtraction(path[2].Location, path[1].Location), 0), "Int32" }
	local center = WPos.op_Addition(path[1].CenterPosition, WVec.New(0, 0, Rules.InitialAltitude(actorName)))
	local actor  = Actor.Create(actorName, { Owner = owner, Location = path[1].Location, CenterPosition = center, Facing = facing })
	local cargo  = Actor.TraitOrDefault(actor, "Cargo")
	local team   = Team.New({})
	if cargo ~= nil and cargoNames ~= nil and #cargoNames > 0 then
		local passengers = { }

		for i, cargoName in ipairs(cargoNames) do
			local passenger = Actor.Create(cargoName, { AddToWorld = false, Owner = owner })
			Team.Add(team, passenger)
			passengers[i] = passenger
			cargo:Load(actor, passenger)
		end

	end

	Utils.Do(Utils.Skip(path, 1), function(waypoint) Actor.ScriptedMove(actor, waypoint.Location) end)

	if actionFunc then actionFunc(actor, team) end
	return actor, team
end
