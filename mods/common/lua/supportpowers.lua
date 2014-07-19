SupportPowers = { }

SupportPowers.Airstrike = function(owner, planeName, enterLocation, bombLocation)
	local facing = { Map.GetFacing(CPos.op_Subtraction(bombLocation, enterLocation), 0), "Int32" }
	local center = WPos.op_Addition(Map.CenterOfCell(enterLocation), WVec.New(0, 0, Rules.InitialAltitude(planeName)))
	local plane = Actor.Create(planeName, { Location = enterLocation, Owner = owner, Facing = facing, CenterPosition = center })
	local bombLoc = Map.CenterOfCell(bombLocation)
	Actor.Trait(plane, "AttackBomber"):SetTarget(bombLoc)
	Actor.Fly(plane, bombLoc)
	Actor.FlyOffMap(plane)
	Actor.RemoveSelf(plane)
	return plane
end

SupportPowers.Paradrop = function(owner, planeName, passengerNames, enterLocation, dropLocation)
	local facing = { Map.GetFacing(CPos.op_Subtraction(dropLocation, enterLocation), 0), "Int32" }
	local center = WPos.op_Addition(Map.CenterOfCell(enterLocation), WVec.New(0, 0, Rules.InitialAltitude(planeName)))
	local plane = Actor.Create(planeName, { Location = enterLocation, Owner = owner, Facing = facing, CenterPosition = center })
	Actor.Fly(plane, Map.CenterOfCell(dropLocation))
	Actor.Trait(plane, "ParaDrop"):SetLZ(dropLocation, true)
	Actor.FlyOffMap(plane)
	Actor.RemoveSelf(plane)
	local cargo = Actor.Trait(plane, "Cargo")
	local passengers = { }
	for i, passengerName in ipairs(passengerNames) do
		local passenger = Actor.Create(passengerName, { AddToWorld = false, Owner = owner })
		passengers[i] = passenger
		cargo:Load(plane, passenger)
	end
	return plane, passengers
end

SupportPowers.Chronoshift = function(unitLocationPairs, chronosphere, duration, killCargo)
	duration = duration or -1
	killCargo = killCargo or true
	Utils.Do(unitLocationPairs, function(pair)
		local unit = pair[1]
		local cell = pair[2]
		local cs = Actor.TraitOrDefault(unit, "Chronoshiftable")
		if cs ~= nil and cs:CanChronoshiftTo(unit, cell) then
			cs:Teleport(unit, cell, duration, killCargo, chronosphere)
		end
	end)
end
