local ants = OpenRA.GetRandomInteger(0, 51) == 0

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
ParadropPlaneType = "badr"
ParadropWaypointCount = 8

SendSovietUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(soviets, unitTypes, entryCell, entryCell, interval)
	local team = Team.New(units)
	Team.AddEventHandler(team.OnAllKilled, function()
		SendSovietUnits(entryCell, unitTypes, interval)
	end)
	Team.Do(team, function(a)
		Actor.OnDamaged(a, function()
			if not Actor.CargoIsEmpty(a) then
				Actor.Stop(a)
				Actor.UnloadCargo(a, true)
			end
		end)
		Actor.OnIdle(a, function() Actor.AttackMove(a, AlliedTechnologyCenter.Location) end)
	end)
end

ShipAlliedUnits = function()
	local transport, reinforcements = Reinforcements.Insert(allies, "lst", { "1tnk", "1tnk", "jeep", "2tnk", "2tnk" }, { LstEntry.Location, LstUnload.Location }, { LstEntry.Location })
	Utils.Do(reinforcements, function(a) Actor.OnIdle(a, Actor.Hunt) end)
	OpenRA.RunAfterDelay(60 * 25, ShipAlliedUnits)
end

ParadropSovietUnits = function()
	local lz = Map.GetNamedActor("Paradrop" .. OpenRA.GetRandomInteger(1, ParadropWaypointCount - 1)).Location
	local plane, passengers = SupportPowers.Paradrop(soviets, ParadropPlaneType, ParadropUnitTypes, Map.GetRandomEdgeCell(), lz)
	Utils.Do(passengers, function(a) Actor.OnIdle(a, Actor.Hunt) end)
	OpenRA.RunAfterDelay(35 * 25, ParadropSovietUnits)
end

ProduceUnits = function()
	Utils.Do(ProducedUnitTypes, function(t)
		local factory = t[1]
		if not Actor.IsDead(factory) and not Production.PerFactoryQueueIsBusy(factory) then
			local unitType = t[2][OpenRA.GetRandomInteger(1, #t[2] + 1)]
			Production.BuildWithPerFactoryQueue(factory, unitType)
		end
	end)
	OpenRA.RunAfterDelay(15, ProduceUnits)
end

SetupAlliedUnits = function()
	for a in Utils.Enumerate(Map.GetNamedActors()) do
		if Actor.Owner(a) == allies then
			if Actor.HasTrait(a, "LuaScriptEvents") then
				a:AddTrait(OpenRA.New("Invulnerable")) -- todo: replace
			end
			Actor.SetStance(a, "Defend")
		end
	end
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(pair)
		Actor.OnProduced(pair[1], function(self, other, ex)
			Actor.Hunt(other)
			Actor.OnDamaged(other, function()
				if not Actor.CargoIsEmpty(other) then
					Actor.Stop(other)
					Actor.UnloadCargo(other, true)
				end
			end)
		end)
	end)
end

ChronoshiftAlliedUnits = function()
	local cells = Map.ExpandFootprint({ ChronoshiftLocation.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("2tnk", { Owner = allies, Facing = { 0, "Int32" } })
		Actor.OnIdle(unit, Actor.Hunt)
		table.insert(units, { unit, cells[i] })
	end
	SupportPowers.Chronoshift(units, Chronosphere)
	OpenRA.RunAfterDelay(60 * 25, ChronoshiftAlliedUnits)
end

ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	OpenRA.SetViewportCenterPosition(WPos.op_Addition(viewportOrigin, WVec.New(19200 * math.sin(t), 20480 * math.cos(t))))
	
	if ticks % 150 == 0 then
		Utils.Do(Actor.ActorsWithTrait("AttackBase"), function(a)
			if Actor.IsIdle(a) and not Map.IsNamedActor(a) and not Actor.IsDead(a) and Actor.IsInWorld(a) and (Actor.Owner(a) == soviets or Actor.Owner(a) == allies) then
				Actor.Hunt(a)
			end
		end)
	end
end

WorldLoaded = function()
	allies = OpenRA.GetPlayer("Allies")
	soviets = OpenRA.GetPlayer("Soviets")
	
	viewportOrigin = OpenRA.GetViewportCenterPosition()
	
	SetupAlliedUnits()
	SetupFactories()
	ProduceUnits()
	ShipAlliedUnits()
	ParadropSovietUnits()
	OpenRA.RunAfterDelay(5 * 25, ChronoshiftAlliedUnits)
	
	OpenRA.GiveCash(allies, 1000000)
	OpenRA.GiveCash(soviets, 1000000)
	
	SendSovietUnits(Entry1.Location, UnitTypes, 50)
	SendSovietUnits(Entry2.Location, UnitTypes, 50)
	SendSovietUnits(Entry3.Location, UnitTypes, 50)
	SendSovietUnits(Entry4.Location, UnitTypes, 50)
	SendSovietUnits(Entry5.Location, UnitTypes, 50)
	SendSovietUnits(Entry6.Location, UnitTypes, 50)
	SendSovietUnits(Entry7.Location, BeachUnitTypes, 15)
end