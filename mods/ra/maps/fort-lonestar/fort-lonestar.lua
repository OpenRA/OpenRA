Patrol = { "e1", "e2", "e1" }
Infantry = { "e4", "e1", "e1", "e2", "e1", "e2" }
Vehicles = { "arty", "ftrk", "ftrk", "apc", "apc" }
Tank = { "3tnk" }
LongRange = { "v2rl" }
Boss = { "4tnk" }

SovietEntryPoints = { Entry1, Entry2, Entry3, Entry4, Entry5, Entry6, Entry7, Entry8 }
PatrolWaypoints = { Entry2, Entry4, Entry6, Entry8 }
ParadropWaypoints = { Paradrop1, Paradrop2, Paradrop3, Paradrop4 }
SpawnPoints = { Spawn1, Spawn2, Spawn3, Spawn4 }
Snipers = { Sniper1, Sniper2, Sniper3, Sniper4, Sniper5, Sniper6, Sniper7, Sniper8, Sniper9, Sniper10, Sniper11, Sniper12 }

Wave = 0
Waves =
{
	{ delay = 500, units = { Infantry } },
	{ delay = 750, units = { Patrol } },
	{ delay = 750, units = { Infantry, Infantry, Vehicles }, },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Vehicles } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Tank, Vehicles } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Tank, Tank } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, LongRange } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, LongRange, Tank, LongRange } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, LongRange, LongRange, Tank, Tank, Vehicles } },
	{ delay = 1500, units = { Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry, Boss } }
}

SendUnits = function(entryCell, unitTypes, targetCell)
	Reinforcements.Reinforce(soviets, unitTypes, { entryCell }, 40, function(a)
		Trigger.OnIdle(a, function(a)
			if a.Location ~= targetCell then
				a.AttackMove(targetCell)
			else
				a.Hunt()
			end
		end)
	end)
end

SendWave = function()
	Wave = Wave + 1
	local wave = Waves[Wave]

	Trigger.AfterDelay(wave.delay, function()
		Utils.Do(wave.units, function(units)
			local entry = Utils.Random(SovietEntryPoints).Location
			local target = Utils.Random(SpawnPoints).Location

			SendUnits(entry, units, target)
		end)

		Utils.Do(players, function(player)
			Media.PlaySpeechNotification(player, "EnemyUnitsApproaching")
		end)

		if (Wave < #Waves) then
			SendWave()
		else
			Trigger.AfterDelay(DateTime.Minutes(2), SovietsRetreating)
			Media.DisplayMessage("You almost survived the onslaught! No more waves incoming.")
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

Tick = function()
	if (Utils.RandomInteger(1, 200) == 10) then
		local delay = Utils.RandomInteger(1, 10)
		Lighting.Flash("LightningStrike", delay)
		Trigger.AfterDelay(delay, function()
			Media.PlaySound("thunder" .. Utils.RandomInteger(1,6) .. ".aud")
		end)
	end
	if (Utils.RandomInteger(1, 200) == 10) then
		Media.PlaySound("thunder-ambient.aud")
	end
end

WorldLoaded = function()
	soviets = Player.GetPlayer("Soviets")
	players = { }
	for i = 0, 4, 1 do
		local player = Player.GetPlayer("Multi" ..i)
		players[i] = player
	end

	Media.DisplayMessage("Defend Fort Lonestar at all costs!")

	SendWave()
end
