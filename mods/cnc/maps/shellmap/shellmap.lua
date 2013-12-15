
ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	OpenRA.SetViewportCenterPosition(WPos.op_Addition(viewportOrigin, MakeVec(-15360 * math.sin(t), 4096 * math.cos(t))))
end

WorldLoaded = function()
	viewportOrigin = OpenRA.GetViewportCenterPosition()
	CreateUnitsInTransport(lst1, { "htnk" });
	CreateUnitsInTransport(lst2, { "mcv" });
	CreateUnitsInTransport(lst3, { "htnk" });

	local units = { boat1, boat2, boat3, boat4, lst1, lst2, lst3}
	for i, unit in ipairs(units) do
		LoopTrack(unit, MakePos(8, unit.Location.Y), MakePos(87, unit.Location.Y))
	end
end

LoopTrack = function(actor, left, right)
	Actor.ScriptedMove(actor, left)
	Actor.Teleport(actor, right)
	Actor.CallFunc(actor, function() LoopTrack(actor, left, right) end)
end

CreateUnitsInTransport = function(transport, passengerNames)
	local cargo = Actor.Trait(transport, "Cargo")
	local owner = Actor.Owner(transport)
	local facing = Actor.Facing(transport)

	for i, passengerName in ipairs(passengerNames) do
		cargo:Load(transport, Actor.Create(passengerName, { AddToWorld = false, Owner = owner, Facing = { facing, "Int32" } }))
	end
end

-- TODO: The standard library should expose CPos.New() etc
MakePos = function(x, y)
	return OpenRA.New("CPos", { {x, "Int32"}, {y, "Int32"} })
end

MakeVec = function(x, y)
	return OpenRA.New("WVec", { {x, "Int32"}, {y, "Int32"}, {0, "Int32"} })
end