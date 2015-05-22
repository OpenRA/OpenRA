local ants = Utils.RandomInteger(0, 51) == 0

if ants then
	UnitTypes = { "rifle", "rifle", "rifle" }
	--BeachUnitTypes = { "ant", "ant" }
	--ParadropUnitTypes = { "ant", "ant", "ant", "ant", "ant" }
	ProducedUnitTypes =
	{
		{ AtreidesBarracks1, { "rifle", "bazooka" } },
		{ AtreidesBarracks2, { "rifle", "bazooka" } },
		{ HarkonnenBarracks1, { "rifle" } },
		{ HarkonnenBarracks2, { "rifle" } },
		{ HarkonnenBarracks3, { "rifle" } },
		{ AtreidesLight1, { "trike", "quad" } },
		{ AtreidesHeavy1, { "combata", "siegetank", "sonictank" } },
		{ HarkonnenLight1, { "quad", "trike" } },
		{ HarkonnenHeavy1, { "combath", "devast", "missiletank" } }
	}
else
	UnitTypes = { "combath", "quad", "quad", "trike" }
	--BeachUnitTypes = { "rifle", "bazooka", "bazooka", "sardaukar", "rifle", "bazooka", "bazooka", "sardaukar", "rifle", "bazooka", "bazooka", "sardaukar", "rifle", "bazooka", "bazooka", "sardaukar" }
	--ParadropUnitTypes = { "rifle", "rifle", "bazooka", "bazooka", "sardaukar" }
	ProducedUnitTypes =
	{
		{ AtreidesBarracks1, { "rifle", "bazooka" } },
		{ AtreidesBarracks2, { "rifle", "bazooka" } },
		{ HarkonnenBarracks1, { "rifle", "rifle", "bazooka", "bazooka", "sardaukar", "sardaukar" } },
		{ HarkonnenBarracks2, { "rifle", "rifle", "bazooka", "bazooka", "sardaukar", "sardaukar" } },
		{ HarkonnenBarracks3, { "rifle", "rifle", "bazooka", "bazooka", "sardaukar", "sardaukar" } },
		{ AtreidesLight1, { "trike", "quad" } },
		{ AtreidesHeavy1, { "combata", "siegetank", "sonictank" } },
		{ HarkonnenLight1, { "quad", "trike" } },
		{ HarkonnenHeavy1, { "combath", "devast", "missiletank" } }
	}
end

--ParadropWaypoints = { Paradrop1, Paradrop2, Paradrop3, Paradrop4, Paradrop5, Paradrop6, Paradrop7, Paradrop8 }

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner == atreides then
			Trigger.OnIdle(a, a.Hunt)
		else
			Trigger.OnIdle(a, function(a) a.AttackMove(AtreidesWeakspot.Location) end)
		end
	end

	-- if a.HasProperty("HasPassengers") then
		-- Trigger.OnDamaged(a, function()
			-- if a.HasPassengers then
				-- a.Stop()
				-- a.UnloadPassengers()
			-- end
		-- end)
	-- end
end

SendHarkonnenUnits = function(entryCell, unitTypes, interval)
	local i = 0
	team = {}

	Utils.Do(unitTypes, function(type)
		local a = Actor.Create(type, false, { Owner = harkonnen, Location = entryCell })
		BindActorTriggers(a)
		Trigger.AfterDelay(i * interval, function() a.IsInWorld = true end)
		table.insert(team, a)
		i = i + 1
	end)

	Trigger.OnAllKilled(team, function() SendHarkonnenUnits(entryCell, unitTypes, interval) end)
end

-- ShipAtreidesUnits = function()
	-- local transport = Actor.Create("carryall", true, { Location = LstEntry.Location, Owner = atreides })

	-- Utils.Do({ "quad", "quad", "trike", "combata", "combata" }, function(type)
		-- local a = Actor.Create(type, false, { Owner = atreides })
		-- BindActorTriggers(a)
		-- transport.LoadPassenger(a)
	-- end)

	-- transport.Move(LstUnload.Location)
	-- transport.UnloadPassengers()
	-- transport.Wait(50)
	-- transport.Move(LstEntry.Location)
	-- transport.Destroy()
 	-- Trigger.AfterDelay(60 * 25, ShipAtreidesUnits)
-- end

-- ParadropHarkonnenUnits = function()
 	-- local lz = Utils.Random(ParadropWaypoints).Location
	-- local start = Utils.CenterOfCell(Map.RandomEdgeCell()) + WVec.New(0, 0, Actor.CruiseAltitude("carryall"))
	-- local transport = Actor.Create("carryall", true, { CenterPosition = start, Owner = harkonnen, Facing = (Utils.CenterOfCell(lz) - start).Facing })

	-- Utils.Do(ParadropUnitTypes, function(type)
		-- local a = Actor.Create(type, false, { Owner = harkonnen })
		-- BindActorTriggers(a)
		-- transport.LoadPassenger(a)
	-- end)

	-- transport.Paradrop(lz)
 	-- Trigger.AfterDelay(35 * 25, ParadropHarkonnenUnits)
-- end

ProduceUnits = function(t)
	local factory = t[1]
	if not factory.IsDead then
		local unitType = t[2][Utils.RandomInteger(1, #t[2] + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

SetupAtreidesUnits = function()
	Utils.Do(Map.NamedActors, function(a)
		if a.Owner == atreides and a.HasProperty("Invulnerable") then
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

ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(19200 * math.sin(t), 20480 * math.cos(t), 0)
end

WorldLoaded = function()
	atreides = Player.GetPlayer("Atreides")
	harkonnen = Player.GetPlayer("Harkonnen")
	viewportOrigin = Camera.Position

	SetupAtreidesUnits()
	SetupFactories()
	--ShipAtreidesUnits()
	--ParadropHarkonnenUnits()
	--Trigger.AfterDelay(5 * 25, ChronoshiftAtreidesUnits)
 	Utils.Do(ProducedUnitTypes, ProduceUnits)

	SendHarkonnenUnits(Entry1.Location, UnitTypes, 50)
	SendHarkonnenUnits(Entry2.Location, UnitTypes, 50)
	SendHarkonnenUnits(Entry3.Location, UnitTypes, 50)
	SendHarkonnenUnits(Entry4.Location, UnitTypes, 50)
	SendHarkonnenUnits(Entry5.Location, UnitTypes, 50)
	SendHarkonnenUnits(Entry6.Location, UnitTypes, 50)
	--SendHarkonnenUnits(Entry7.Location, BeachUnitTypes, 15)
	
	Media.PlayMusic("score")
end