--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
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

Wave = 0
SendEnemies = function()
	Trigger.AfterDelay(EnemyAttackDelay[Difficulty], function()

		Wave = Wave + 1
		if Wave > 3 then
			Wave = 1
		end

		if Wave == 1 then
			local units = Reinforcements.ReinforceWithTransport(Greece, "tran", EnemyReinforcements[Difficulty][Wave], EnemyPaths[1], { EnemyPaths[1][1] })[2]
			Utils.Do(units, IdleHunt)
		else
			local units = Reinforcements.ReinforceWithTransport(Greece, "lst", EnemyReinforcements[Difficulty][Wave], EnemyPaths[2], { EnemyPaths[2][1] })[2]
			Utils.Do(units, IdleHunt)
		end

		if not Dome.IsDead then
			SendEnemies()
		end
	end)
end
