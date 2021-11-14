--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = hard

WorldLoaded = function()
	DestroyAll = GDI.AddObjective("Destroy all Nod units and buildings.")
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(DestroyAll)
	end
end

AttackPaths =
{
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location, waypoint6.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint2.Location, waypoint13.Location, waypoint14.Location, waypoint15.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint7.Location, waypoint8.Location, waypoint9.Location, waypoint10.Location, waypoint17.Location }
}

AttackDelayMin = { easy = DateTime.Minutes(1), normal = DateTime.Seconds(45), hard = DateTime.Seconds(30) }
AttackDelayMax = { easy = DateTime.Minutes(2), normal = DateTime.Seconds(90), hard = DateTime.Minutes(1) }
AttackUnitTypes = 
{
	easy =
	{
		{ factory = HandOfNod, types = { "e1", "e1" } },
		{ factory = HandOfNod, types = { "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e3", "e3" } },
	},
	normal =
	{
		{ factory = HandOfNod, types = { "e1", "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3" } },
		{ factory = Airfield, types = { "bggy" } },
	},
	hard =
	{
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3", "e3" } },
		{ factory = Airfield, types = { "bggy" } },
		{ factory = Airfield, types = { "ltnk" } },
	}
}

Attack = function()
	local production = Utils.Random(AttackUnitTypes[Difficulty])
	local path = Utils.Random(AttackPaths)
	local toBuild = function() return production.types end
	ProduceUnits(Nod, production.factory, nil, toBuild, function(units)
		Utils.Do(units, function(unit)
			unit.Patrol(path, false)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(AttackDelayMin[Difficulty], AttackDelayMax[Difficulty]), Attack)
end