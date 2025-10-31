--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOptionOrDefault("difficulty", "normal")

--- Prepare basic messages for a player's win, loss, or objective updates.
---@param player player
InitObjectives = function(player)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-completed"))
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-failed"))
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionFailed")
		end)
	end)

	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionAccomplished")
		end)
	end)
end

AttackAircraftTargets = { }
--- Order an aircraft to seek and attack an enemy player's units whenever idle.
--- Each target is focused until it can no longer be attacked.
---@param aircraft actor
---@param enemyPlayer player
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

--- Return a random enemy target for an actor.
---@param unit actor Actor to be given a target.
---@param enemyPlayer player Player to be targeted.
---@return actor|nil
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

--- Call a function when one of the actors in a group is damaged. The callback function will be called as func(self: actor, attacker: actor, damage: integer).
---@param actors actor[]
---@param func fun(self: actor, attacker: actor, damage: integer)
OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end
