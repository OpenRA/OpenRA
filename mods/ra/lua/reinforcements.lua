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

Reinforcements.Reinforce = function(owner, reinforcementNames, enterLocation, rallyPointLocation, interval, onCreateFunc)
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