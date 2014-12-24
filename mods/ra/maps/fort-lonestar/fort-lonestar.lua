Patrol = { "e1", "e2", "e1" }
Infantry = { "e4", "e1", "e1", "e2", "e1", "e2" }
Vehicles = { "arty", "ftrk", "ftrk", "apc", "apc" }
Tank = { "3tnk" }
LongRange = { "v2rl" }
Boss = { "4tnk" }

SovietEntryPoints = { Entry1, Entry2, Entry3, Entry4, Entry5, Entry6, Entry7, Entry8 }
PatrolWaypoints = { Entry2, Entry4, Entry6, Entry8 }
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
	Reinforcements.Reinforce(soviets, unitTypes, { entryCell }, interval, function(a)
		Trigger.OnIdle(a, function(a)
			if a.Location ~= targetCell then
				a.AttackMove(targetCell)
			else
				a.Hunt()
			end
		end)
	end)

	if (Wave < #Waves) then
		SendWave()
	else
		Trigger.AfterDelay(DateTime.Minutes(2), SovietsRetreating)
		Media.DisplayMessage("You survived the onslaught! No more waves incoming.")
	end
end

SendWave = function()
	Wave = Wave + 1
	local wave = Waves[Wave]

	local delay = wave[1]
	local entry = Utils.Random(wave[2]).Location
	local units = wave[3]
	local target = Utils.Random(wave[4]).Location

	Trigger.AfterDelay(delay, function()
		SendUnits(entry, units, 40, target)

		if not played then
			played = true
			Utils.Do(players, function(player)
				Media.PlaySpeechNotification(player, "EnemyUnitsApproaching")
			end)
			Trigger.AfterDelay(DateTime.Seconds(1), function() played = false end)
		end
	end)
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
	players = { }
	for i = 0, 4, 1 do
		local player = Player.GetPlayer("Multi" ..i)
		players[i] = player
	end

	Utils.Do(Snipers, function(a)
		if a.Owner == soviets then
			a.GrantUpgrade("unkillable")
		end
	end)

	Media.DisplayMessage("Defend Fort Lonestar at all costs!")

	SendWave()
end
