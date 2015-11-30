EnemyReinforcements =
{
	Easy =
	{
		{ "e1", "e1", "e3" },
		{ "e1", "e3", "jeep" },
		{ "e1", "jeep", "1tnk" }
	},

	Normal =
	{
		{ "e1", "e1", "e3", "e3" },
		{ "e1", "e3", "jeep", "jeep" },
		{ "e1", "jeep", "1tnk", "2tnk" }
	},

	Hard =
	{
		{ "e1", "e1", "e3", "e3", "e1" },
		{ "e1", "e3", "jeep", "jeep", "1tnk" },
		{ "e1", "jeep", "1tnk", "2tnk", "arty" }
	}
}

EnemyAttackDelay =
{
	Easy = DateTime.Minutes(5),
	Normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	Hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

EnemyPaths =
{
	{ EnemyEntry1.Location, EnemyRally1.Location },
	{ EnemyEntry2.Location, EnemyRally2.Location }
}

wave = 0
SendEnemies = function()
	Trigger.AfterDelay(EnemyAttackDelay[Map.Difficulty], function()

		wave = wave + 1
		if wave > 3 then
			wave = 1
		end

		if wave == 1 then
			local units = Reinforcements.ReinforceWithTransport(enemy, "tran", EnemyReinforcements[Map.Difficulty][wave], EnemyPaths[1], { EnemyPaths[1][1] })[2]
			Utils.Do(units, IdleHunt)
		else
			local units = Reinforcements.ReinforceWithTransport(enemy, "lst", EnemyReinforcements[Map.Difficulty][wave], EnemyPaths[2], { EnemyPaths[2][1] })[2]
			Utils.Do(units, IdleHunt)
		end

		if not Dome.IsDead then
			SendEnemies()
		end
	end)
end
