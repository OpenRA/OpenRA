local ants = Utils.RandomInteger(0, 51) == 0

if ants then
	UnitTypes = { "ant", "ant", "ant" }
	BeachUnitTypes = { "ant", "ant" }
	ParadropUnitTypes = { "ant", "ant", "ant", "ant", "ant" }
	ProducedUnitTypes =
	{
		{ AlliedBarracks1, { "e1", "e3" } },
		{ AlliedBarracks2, { "e1", "e3" } },
		{ SovietBarracks1, { "ant" } },
		{ SovietBarracks2, { "ant" } },
		{ SovietBarracks3, { "ant" } },
		{ AlliedWarFactory1, { "jeep", "1tnk", "2tnk", "arty", "ctnk" } },
		{ SovietWarFactory1, { "3tnk", "4tnk", "v2rl", "ttnk", "apc" } }
	}
else
	UnitTypes = { "3tnk", "ftrk", "ttnk", "apc" }
	BeachUnitTypes = { "e1", "e2", "e3", "e4", "e1", "e2", "e3", "e4", "e1", "e2", "e3", "e4", "e1", "e2", "e3", "e4" }
	ParadropUnitTypes = { "e1", "e1", "e2", "e3", "e4" }
	ProducedUnitTypes =
	{
		{ AlliedBarracks1, { "e1", "e3" } },
		{ AlliedBarracks2, { "e1", "e3" } },
		{ SovietBarracks1, { "dog", "e1", "e2", "e3", "e4", "shok" } },
		{ SovietBarracks2, { "dog", "e1", "e2", "e3", "e4", "shok" } },
		{ SovietBarracks3, { "dog", "e1", "e2", "e3", "e4", "shok" } },
		{ AlliedWarFactory1, { "jeep", "1tnk", "2tnk", "arty", "ctnk" } },
		{ SovietWarFactory1, { "3tnk", "4tnk", "v2rl", "ttnk", "apc" } }
	}
end

ParadropWaypoints = { Paradrop1, Paradrop2, Paradrop3, Paradrop4, Paradrop5, Paradrop6, Paradrop7, Paradrop8 }

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner == allies then
			Trigger.OnIdle(a, a.Hunt)
		else
			Trigger.OnIdle(a, function(a) a.AttackMove(AlliedTechnologyCenter.Location) end)
		end
	end

	if a.HasProperty("HasPassengers") then
		Trigger.OnDamaged(a, function()
			if a.HasPassengers then
				a.Stop()
				a.UnloadPassengers()
			end
		end)
	end
end

SendSovietUnits = function(entryCell, unitTypes, interval)
	local i = 0
	team = {}

	Utils.Do(unitTypes, function(type)
		local a = Actor.Create(type, false, { Owner = soviets, Location = entryCell })
		BindActorTriggers(a)
		Trigger.AfterDelay(i * interval, function() a.IsInWorld = true end)
		table.insert(team, a)
		i = i + 1
	end)

	Trigger.OnAllKilled(team, function() SendSovietUnits(entryCell, unitTypes, interval) end)
end

ShipAlliedUnits = function()
	local transport = Actor.Create("lst", true, { Location = LstEntry.Location, Owner = allies })

	Utils.Do({ "1tnk", "1tnk", "jeep", "2tnk", "2tnk" }, function(type)
		local a = Actor.Create(type, false, { Owner = allies })
		BindActorTriggers(a)
		transport.LoadPassenger(a)
	end)

	transport.Move(LstUnload.Location)
	transport.UnloadPassengers()
	transport.Wait(50)
	transport.Move(LstEntry.Location)
	transport.Destroy()
	Trigger.AfterDelay(60 * 25, ShipAlliedUnits)
end

ParadropSovietUnits = function()
	local lz = Utils.Random(ParadropWaypoints).Location
	local start = Utils.CenterOfCell(Map.RandomEdgeCell()) + WVec.New(0, 0, Actor.CruiseAltitude("badr"))
	local transport = Actor.Create("badr", true, { CenterPosition = start, Owner = soviets, Facing = (Utils.CenterOfCell(lz) - start).Facing })

	Utils.Do(ParadropUnitTypes, function(type)
		local a = Actor.Create(type, false, { Owner = soviets })
		BindActorTriggers(a)
		transport.LoadPassenger(a)
	end)

	transport.Paradrop(lz)
	Trigger.AfterDelay(35 * 25, ParadropSovietUnits)
end

ProduceUnits = function(t)
	local factory = t[1]
	if not factory.IsDead then
		local unitType = t[2][Utils.RandomInteger(1, #t[2] + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

SetupAlliedUnits = function()
	Utils.Do(Map.NamedActors, function(a)
		if a.Owner == allies and a.HasProperty("Invulnerable") then
			a.Invulnerable = true
			a.Stance = "Defend"
		end
	end)
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(pair)
		Trigger.OnProduction(pair[1], function(_, a) BindActorTriggers(a) end)
	end)
end

ChronoshiftAlliedUnits = function()
	local cells = Utils.ExpandFootprint({ ChronoshiftLocation.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("2tnk", true, { Owner = allies, Facing = 0 })
		BindActorTriggers(unit)
		units[unit] = cells[i]
	end
	Chronosphere.Chronoshift(units)
	Trigger.AfterDelay(60 * 25, ChronoshiftAlliedUnits)
end

ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(19200 * math.sin(t), 20480 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")
	viewportOrigin = Camera.Position

	SetupAlliedUnits()
	SetupFactories()
	ShipAlliedUnits()
	ParadropSovietUnits()
	Trigger.AfterDelay(5 * 25, ChronoshiftAlliedUnits)
	Utils.Do(ProducedUnitTypes, ProduceUnits)

	SendSovietUnits(Entry1.Location, UnitTypes, 50)
	SendSovietUnits(Entry2.Location, UnitTypes, 50)
	SendSovietUnits(Entry3.Location, UnitTypes, 50)
	SendSovietUnits(Entry4.Location, UnitTypes, 50)
	SendSovietUnits(Entry5.Location, UnitTypes, 50)
	SendSovietUnits(Entry6.Location, UnitTypes, 50)
	SendSovietUnits(Entry7.Location, BeachUnitTypes, 15)
end