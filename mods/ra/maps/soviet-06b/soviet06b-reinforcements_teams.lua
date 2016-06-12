EnemyReinforcements =
{
	easy =
	{
		{ "e1", "e1", "e3" },
		{ "e1", "e3", "jeep" },
		{ "e1", "jeep", "1tnk" }
	},

	normal =
	{
		{ "e1", "e1", "e3", "e3" },
		{ "e1", "e3", "jeep", "jeep" },
		{ "e1", "jeep", "1tnk", "2tnk" }
	},

	hard =
	{
		{ "e1", "e1", "e3", "e3", "e1" },
		{ "e1", "e3", "jeep", "jeep", "1tnk" },
		{ "e1", "jeep", "1tnk", "2tnk", "arty" }
	}
}

EnemyAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

EnemyPaths =
{
	{ EnemyEntry1.Location, EnemyRally1.Location },
	{ EnemyEntry2.Location, EnemyRally2.Location }
}

wave = 0
SendEnemies = function()
	Trigger.AfterDelay(EnemyAttackDelay[Map.LobbyOption("difficulty")], function()

		wave = wave + 1
		if wave > 3 then
			wave = 1
		end

		if wave == 1 then
			local units = Reinforcements.ReinforceWithTransport(enemy, "tran", EnemyReinforcements[Map.LobbyOption("difficulty")][wave], EnemyPaths[1], { EnemyPaths[1][1] })[2]
			Utils.Do(units, IdleHunt)
		else
			local units = Reinforcements.ReinforceWithTransport(enemy, "lst", EnemyReinforcements[Map.LobbyOption("difficulty")][wave], EnemyPaths[2], { EnemyPaths[2][1] })[2]
			Utils.Do(units, IdleHunt)
		end

		if not Dome.IsDead then
			SendEnemies()
		end
	end)
end
