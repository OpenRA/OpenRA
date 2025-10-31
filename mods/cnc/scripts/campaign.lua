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
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end

--- Send reinforcements carried by a naval landing craft.
---@param player player Owner of the landing craft and its passengers.
---@param units string[] Unit types to spawn and unload.
---@param transportStart cpos Cell at which the craft spawns and removes itself.
---@param transportUnload cpos Cell at which passengers unload.
---@param rallypoint? cpos Cell to which unloaded passengers will move.
ReinforceWithLandingCraft = function(player, units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("lst", true, { Owner = player, Facing = Angle.North, Location = transportStart })
	local subcell = 1

	if #units == 1 then
		subcell = 0
	end

	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()
end

--- Schedule repairs for this building once it takes enough damage.
---@param owner player Owner of the building.
---@param actor actor Building to be repaired.
---@param modifier number The repair threshold. Below this health percentage, repairs are started. 1 is full health, while 0.5 is half.
RepairBuilding = function(owner, actor, modifier)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * modifier then
			building.StartBuildingRepairs()
		end
	end)
end

--- Schedule repairs for a player's starting buildings.
---@param owner player Owner of the buildings.
---@param modifier number The repair threshold. Below this health percentage, repairs are started. 1 is full health, while 0.5 is half.
RepairNamedActors = function(owner, modifier)
	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == owner and actor.HasProperty("StartBuildingRepairs") then
			RepairBuilding(owner, actor, modifier)
		end
	end)
end

--- Schedule production tasks for a factory or other unit producer.
---@param player player Owner of the factory.
---@param factory actor The factory itself.
---@param delay? fun():integer Function that returns a delay until production repeats. If this is absent, production will not repeat.
---@param toBuild fun():string[] Function that returns a list of unit types to be produced.
---@param after? fun(actors: actor[]) Function called by each group of built units after their production is finished. Only alive actors are included.
ProduceUnits = function(player, factory, delay, toBuild, after)
	if factory.IsDead or factory.Owner ~= player then
		return
	end

	factory.Build(toBuild(), function(units)
		if delay and delay() > 0 then
			Trigger.AfterDelay(delay(), function() ProduceUnits(player, factory, delay, toBuild, after) end)
		end

		if after then
			after(units)
		end
	end)
end

--- Check if a player currently owns one of each building type in a list.
---@param player player
---@param buildingTypes string[]
---@return boolean
CheckForBase = function(player, buildingTypes)
	local count = 0

	Utils.Do(buildingTypes, function(name)
		if #player.GetActorsByType(name) > 0 then
			count = count + 1
		end
	end)

	return count == #buildingTypes
end

--- Schedule production of a unit's replacement after its death.
---@param unit { [1]: actor }
---@param player player
---@param factory actor
RebuildUnit = function(unit, player, factory)
	Trigger.OnKilled(unit[1], function()
		ProduceUnits(player, factory, nil, function() return { unit[1].Type } end, function(actors)
			RebuildUnit(actors, player, factory)
		end)
	end)
end

--- Order a group to attack-move toward each waypoint in a list, then hunt.
---@param actors actor[]
---@param waypoints actor[]
MoveAndHunt = function(actors, waypoints)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		Utils.Do(waypoints, function(point)
			actor.AttackMove(point.Location)
		end)

		IdleHunt(actor)
	end)
end

--- Order a group to move toward each waypoint in a list.
---@param actors any
---@param waypoints actor[]
MoveAndIdle = function(actors, waypoints)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		Utils.Do(waypoints, function(point)
			actor.Move(point.Location, 0)
		end)
	end)
end

Searches = 0
--- Get the position of a hostile, mobile actor that is distant from anti-air sites.
---@param player player Player to be targeted.
---@return wpos|nil target The chosen airstrike position, if any.
GetAirstrikeTarget = function(player)
	local list = player.GetGroundAttackers()

	if #list == 0 then
		return
	end

	local target = list[DateTime.GameTime % #list + 1].CenterPosition

	local sams = Map.ActorsInCircle(target, WDist.New(8 * 1024), function(actor)
		return actor.Type == "sam" end)

	if #sams == 0 then
		Searches = 0
		return target
	elseif Searches < 6 then
		Searches = Searches + 1
		return GetAirstrikeTarget(player)
	else
		Searches = 0
		return nil
	end
end
