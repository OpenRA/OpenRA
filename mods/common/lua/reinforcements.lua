Reinforcements = { }

Reinforcements.PerformHelicopterInsertion = function(owner, helicopterName, passengerNames, enterPosition, unloadPosition, exitPosition)
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

Reinforcements.PerformHelicopterExtraction = function(owner, helicopterName, passengers, enterPosition, loadPosition, exitPosition)
	local facing = { Map.GetFacing(WPos.op_Subtraction(loadPosition, enterPosition), 0), "Int32" }
	local altitude = { Actor.TraitInfo(helicopterName, "AircraftInfo").CruiseAltitude, "Int32" }
	local heli = Actor.Create(helicopterName, { Owner = owner, CenterPosition = enterPosition, Facing = facing, Altitude = altitude })
	local cargo = Actor.Trait(heli, "Cargo")
	Actor.HeliFly(heli, loadPosition)
	Actor.Turn(heli, 0)
	Actor.HeliLand(heli, true)
	Actor.WaitFor(heli, function()
		return Utils.All(passengers, function(passenger) return cargo.Passengers:Contains(passenger) end)
	end)
	Actor.Wait(heli, 125)
	Actor.HeliFly(heli, exitPosition)
	Actor.RemoveSelf(heli)
	return heli
end

Reinforcements.PerformInsertion = function(owner, vehicleName, passengerNames, enterPath, exitPath)
	local facing = { 0, "Int32" }
	if #enterPath > 1 then
		facing = { Map.GetFacing(CPos.op_Subtraction(enterPath[2], enterPath[1]), 0), "Int32" }
	end
	local vehicle = Actor.Create(vehicleName, { Owner = owner, Location = enterPath[1], Facing = facing })
	local cargo = Actor.Trait(vehicle, "Cargo")
	local passengers = { }
	for i, passengerName in ipairs(passengerNames) do
		local passenger = Actor.Create(passengerName, { AddToWorld = false, Owner = owner })
		passengers[i] = passenger
		cargo:Load(vehicle, passenger)
	end
	Utils.Do(Utils.Skip(enterPath, 1), function(l) Actor.ScriptedMove(vehicle, l) end)
	Actor.UnloadCargo(vehicle, true)
	Actor.Wait(vehicle, 25)
	Utils.Do(exitPath, function(l) Actor.ScriptedMove(vehicle, l) end)
	Actor.RemoveSelf(vehicle)
	return vehicle, passengers
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