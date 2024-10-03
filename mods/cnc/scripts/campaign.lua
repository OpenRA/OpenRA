--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOptionOrDefault("difficulty", "normal")

InitObjectives = function(player)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.Translate("objective-completed"))
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.Translate("objective-failed"))
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

RepairBuilding = function(owner, actor, modifier)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * modifier then
			building.StartBuildingRepairs()
		end
	end)
end

RepairNamedActors = function(owner, modifier)
	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == owner and actor.HasProperty("StartBuildingRepairs") then
			RepairBuilding(owner, actor, modifier)
		end
	end)
end

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

CheckForBase = function(player, buildingTypes)
	local count = 0

	Utils.Do(buildingTypes, function(name)
		if #player.GetActorsByType(name) > 0 then
			count = count + 1
		end
	end)

	return count == #buildingTypes
end

RebuildUnit = function(unit, player, factory)
	Trigger.OnKilled(unit[1], function()
		ProduceUnits(player, factory, nil, function() return { unit[1].Type } end, function(actors)
			RebuildUnit(actors, player, factory)
		end)
	end)
end

MoveAndHunt = function(actors, path)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		Utils.Do(path, function(point)
			actor.AttackMove(point.Location)
		end)

		IdleHunt(actor)
	end)
end

MoveAndIdle = function(actors, path)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		Utils.Do(path, function(point)
			actor.Move(point.Location, 0)
		end)
	end)
end

Patrol = function(actors, path, waitTicks)
	Utils.Do(actors, function(actor)
		if not actor or actor.IsDead then
			return
		end

		local locations = {}
		for idx, point in ipairs(path) do
			table.insert(locations, point.Location)
		end
		actor.Patrol(locations, true, waitTicks)
	end)
end

Searches = 0
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

-- Convert a list of cellnumbers to cartesian (X,Y) positions
CellsToPositions = function(list)
    for i = 1, #list do
        local cell = list[i]
        list[i] = CPos.New(cell % 64, math.floor(cell / 64))
    end
    return list
end
