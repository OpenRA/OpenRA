--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
NorthNestPath = { NorthPath1, NorthPath2 }
SouthNestPath = { SouthPath1, SouthPath2 }
BaseNestPath = { NorthPath1, SouthPath1 }

AntSquad =
{
	easy = { "warriorant", "warriorant", "warriorant" },
	normal = { "warriorant", "warriorant", "warriorant", "warriorant" },
	hard = { "warriorant", "warriorant", "warriorant", "warriorant", "warriorant" }
}

ActivateHive1 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive1.Location })
	Utils.Do(ants, IdleHunt)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive1)
end

ActivateHive2 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive2.Location })
	local path = Utils.Random(NorthNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive2)
end

ActivateHive3 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive3.Location })
	local path = Utils.Random(NorthNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive3)
end

ActivateHive4 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive4.Location })
	local path = Utils.Random(SouthNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive4)
end

ActivateHive5 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive5.Location })
	local path = Utils.Random(SouthNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive5)
end

ActivateHive6 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive6.Location })
	local path = Utils.Random(BaseNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive6)
end

ActivateHive7 = function()
	local ants = Reinforcements.Reinforce(USSR, AntSquad, { Hive7.Location })
	local path = Utils.Random(SouthNestPath)
	Utils.Do(ants, function(ant)
		ant.AttackMove(path.Location)
		IdleHunt(ant)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive7)
end

ActivateAntHives = function()
	AntSquad = AntSquad[Difficulty]

	Trigger.AfterDelay(DateTime.Minutes(1), ActivateHive1)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), ActivateHive2)
	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(45), ActivateHive3)
	Trigger.AfterDelay(DateTime.Minutes(4), ActivateHive4)
	Trigger.AfterDelay(DateTime.Minutes(5) + DateTime.Seconds(15), ActivateHive5)
	Trigger.AfterDelay(DateTime.Minutes(6) + DateTime.Seconds(30), ActivateHive6)
	Trigger.AfterDelay(DateTime.Minutes(7) + DateTime.Seconds(45), ActivateHive7)
end
