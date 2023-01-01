--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
IdlingUnits = function()
	local lazyUnits = Utils.Where(Map.ActorsInWorld, function(actor)
		return actor.HasProperty("Hunt") and actor.Owner == Greece end)

	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

ProduceInfantry = function()
	if Barr.IsDead then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(AlliedInfantryTypes) }
	Greece.Build(toBuild, function(unit)
		GreeceInfAttack[#GreeceInfAttack + 1] = unit[1]

		if #GreeceInfAttack >= 7 then
			SendUnits(GreeceInfAttack, InfantryWaypoints)
			GreeceInfAttack = { }
			Trigger.AfterDelay(DateTime.Minutes(2), ProduceInfantry)
		else
			Trigger.AfterDelay(delay, ProduceInfantry)
		end
	end)
end

ProduceShips = function()
	if Navalyard.IsDead then
		return
	end

	Greece.Build( {"dd"}, function(unit)
		Ships[#Ships + 1] = unit[1]

		if #Ships >= 2 then
			SendUnits(Ships, ShipWaypoints)
			Ships = { }
			Trigger.AfterDelay(DateTime.Minutes(6), ProduceShips)
		else
			Trigger.AfterDelay(Actor.BuildTime("dd"), ProduceShips)
		end
	end)
end

SendUnits = function(units, waypoints)
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			Utils.Do(waypoints, function(waypoint)
				unit.AttackMove(waypoint.Location)
			end)
			unit.Hunt()
		end
	end)
end
