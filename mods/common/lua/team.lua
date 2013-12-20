Team = { }

Team.New = function(actors)
	local team = { }
	team.Actors = actors
	team.OnAllKilled = { }
	team.OnAnyKilled = { }
	team.OnAllRemovedFromWorld = { }
	team.OnAnyRemovedFromWorld = { }
	Team.AddActorEventHandlers(team)
	return team
end

Team.AddActorEventHandlers = function(team)
	for i, actor in ipairs(team.Actors) do

		Actor.OnKilled(actor, function()
			Team.InvokeHandlers(team.OnAnyKilled)
			if Team.AllAreDead(team) then Team.InvokeHandlers(team.OnAllKilled) end
		end)
		
		Actor.OnRemovedFromWorld(actor, function()
			Team.InvokeHandlers(team.OnAnyRemovedFromWorld)
			if not Team.AnyAreInWorld(team) then Team.InvokeHandlers(team.OnAllRemovedFromWorld) end
		end)
	end
end

Team.InvokeHandlers = function(event)
	for i, handler in ipairs(event) do
		handler()
	end
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
	Utils.Do(team.Actors, func)
end