Team = { }

Team.New = function(actors)
	local team = { }
	team.Actors = actors
	team.OnAllKilled = { }
	team.OnAnyKilled = { }
	team.OnAllRemovedFromWorld = { }
	team.OnAnyRemovedFromWorld = { }
	Team.Do(team, function(actor) Team.AddActorEventHandlers(team, actor) end)
	return team
end

Team.Add = function(team, actor)
	table.insert(team.Actors, actor)
	Team.AddActorEventHandlers(team, actor)
end

Team.AddActorEventHandlers = function(team, actor)
	Actor.OnKilled(actor, function()
		Team.InvokeHandlers(team.OnAnyKilled)
		if Team.AllAreDead(team) then Team.InvokeHandlers(team.OnAllKilled) end
	end)

	Actor.OnRemovedFromWorld(actor, function()
		Team.InvokeHandlers(team.OnAnyRemovedFromWorld)
		if not Team.AnyAreInWorld(team) then Team.InvokeHandlers(team.OnAllRemovedFromWorld) end
	end)
end

Team.InvokeHandlers = function(event)
	Utils.Do(event, function(handler) handler() end)
end

Team.AllAreDead = function(team)
	return Utils.All(team.Actors, Actor.IsDead)
end

Team.AnyAreDead = function(team)
	return Utils.Any(team.Actors, Actor.IsDead)
end

Team.AllAreInWorld = function(team)
	return Utils.All(team.Actors, Actor.IsInWorld)
end

Team.AnyAreInWorld = function(team)
	return Utils.Any(team.Actors, Actor.IsInWorld)
end

Team.AddEventHandler = function(event, func)
	table.insert(event, func)
end

Team.Contains = function(team, actor)
	return Utils.Any(team.Actors, function(a) return a == actor end)
end

Team.Do = function(team, func)
	Utils.Do(team.Actors, function(actor)
		if not Actor.IsDead(actor) then
			func(actor)
		end
	end)
end

Team.Patrol = function(team, waypoints, wait, loop)
	Team.Do(team, function(a) Actor.Patrol(a, waypoints, wait, loop) end)
end

Team.PatrolUntil = function(team, waypoints, wait, func)
	Team.Do(team, function(a) Actor.PatrolUntil(a, waypoints, wait, func) end)
end
