Patrol = { "e1", "e2", "e1" }
Infantry = { "e4", "e1", "e1", "e2", "e1", "e2" }
Vehicles = { "arty", "ftrk", "ftrk", "apc", "apc" }
Tank = { "3tnk" }
LongRange = { "v2rl" }
Boss = { "4tnk" }

SovietEntryPoints = { Entry1, Entry2, Entry3, Entry4, Entry5, Entry6, Entry7, Entry8 }
PatrolWaypoints = { Patrol1, Patrol2, Patrol3, Patrol4 }
ParadropWaypoints = { Paradrop1, Paradrop2, Paradrop3, Paradrop4 }
OilDerricks = { OilDerrick1, OilDerrick2, OilDerrick3, OilDerrick4 }
SpawnPoints = { Spawn1, Spawn2, Spawn3, Spawn4 }
Snipers = { Sniper1, Sniper2, Sniper3, Sniper4, Sniper5, Sniper6, Sniper7, Sniper8, Sniper9, Sniper10, Sniper11, Sniper12 }

Wave = 0
Waves =
{
	{ 500, SovietEntryPoints, Infantry, SpawnPoints },

	{ 750, PatrolWaypoints, Patrol, ParadropWaypoints },

	{ 750, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Vehicles, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Vehicles, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },
	{ 1, SovietEntryPoints, Vehicles, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, LongRange, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, LongRange, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },
	{ 1, SovietEntryPoints, LongRange, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, LongRange, SpawnPoints },
	{ 1, SovietEntryPoints, LongRange, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },
	{ 1, SovietEntryPoints, Tank, SpawnPoints },
	{ 1, SovietEntryPoints, Vehicles, SpawnPoints },

	{ 1500, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Infantry, SpawnPoints },
	{ 1, SovietEntryPoints, Boss, SpawnPoints }
}

SendUnits = function(entryCell, unitTypes, interval, targetCell)
	local i = 0
	Utils.Do(unitTypes, function(type)
		local a = Actor.Create(type, false, { Owner = soviets, Location = entryCell })
		Trigger.OnIdle(a, function(a) a.AttackMove(targetCell) end)
		Trigger.AfterDelay(i * interval, function() a.IsInWorld = true end)
		i = i + 1
	end)

	if (Wave < #Waves) then
		SendWave()
	else
		Trigger.AfterDelay(3000, SovietsRetreating)
	end
end

SendWave = function()
	Wave = Wave + 1
	local wave = Waves[Wave]

	local delay = wave[1]
	local entry = Utils.Random(wave[2]).Location
	local units = wave[3]
	local target = Utils.Random(wave[4]).Location

	print(string.format("Sending wave %i in %i.", Wave, delay))
	Trigger.AfterDelay(delay, function() SendUnits(entry, units, 40, target) end)
end

SovietsRetreating = function()
	Utils.Do(Snipers, function(a)
		if not a.IsDead and a.Owner == soviets then
			a.Destroy()
		end
	end)
end

WorldLoaded = function()
	soviets = Player.GetPlayer("Soviets")

	Utils.Do(Snipers, function(a)
		if a.Owner == soviets then
			a.Invulnerable = true
		end
	end)

	SendWave()
end