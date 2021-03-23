--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AttackAircraftTargets = { }
InitializeAttackAircraft = function(aircraft, enemyPlayer)
	Trigger.OnIdle(aircraft, function()
		local actorId = tostring(aircraft)
		local target = AttackAircraftTargets[actorId]

		if not target or not target.IsInWorld then
			target = ChooseRandomTarget(aircraft, enemyPlayer)
		end

		if target then
			AttackAircraftTargets[actorId] = target
			aircraft.Attack(target)
		else
			AttackAircraftTargets[actorId] = nil
			aircraft.ReturnToBase()
		end
	end)
end

ChooseRandomTarget = function(unit, enemyPlayer)
	local target = nil
	local enemies = Utils.Where(enemyPlayer.GetActors(), function(self)
		return self.HasProperty("Health") and unit.CanTarget(self) and not Utils.Any({ "sbag", "fenc", "brik", "cycl", "barb" }, function(type) return self.Type == type end)
	end)
	if #enemies > 0 then
		target = Utils.Random(enemies)
	end
	return target
end
