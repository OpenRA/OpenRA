
ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(-15360 * math.sin(t), 4096 * math.cos(t), 0)
end

WorldLoaded = function()
	viewportOrigin = Camera.Position
	LoadTransport(lst1, "htnk")
	LoadTransport(lst2, "mcv.gdi")
	LoadTransport(lst3, "htnk")
	local units = { boat1, boat2, boat3, boat4, lst1, lst2, lst3}
	for i, unit in ipairs(units) do
		LoopTrack(unit, CPos.New(8, unit.Location.Y), CPos.New(87, unit.Location.Y))
	end
end

LoopTrack = function(actor, left, right)
	actor.ScriptedMove(left)
	actor.Teleport(right)
	actor.CallFunc(function() LoopTrack(actor, left, right) end)
end

LoadTransport = function(transport, passenger)
	transport.LoadPassenger(Actor.Create(passenger, false, { Owner = transport.Owner, Facing = transport.Facing }))
end