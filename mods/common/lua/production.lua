Production = { }
Production.EventHandlers = { }

Production.BuildWithSharedQueue = function(player, unit, amount)
	Internal.BuildWithSharedQueue(player, unit, amount or 1)
end

Production.BuildWithPerFactoryQueue = function(factory, unit, amount)
	Internal.BuildWithPerFactoryQueue(factory, unit, amount or 1)
end

Production.Build = function(factory, unit, amount)
	if Actor.HasTrait(factory, "ProductionQueue") then
		Production.BuildWithPerFactoryQueue(factory, unit, amount)
	elseif Actor.HasTrait(factory, "Production") then
		Production.SetPrimaryBuilding(factory)
		Production.BuildWithSharedQueue(Actor.Owner(factory), unit, amount)
	else
		error("Production.Build: not a factory")
	end
end

Production.SharedQueueIsBusy = function(player, category)
	return Internal.SharedQueueIsBusy(player, category)
end

Production.PerFactoryQueueIsBusy = function(factory)
	return Internal.PerFactoryQueueIsBusy(factory)
end

Production.SetRallyPoint = function(factory, location)
	local srp = Actor.Trait(factory, "RallyPoint")
	if srp ~= nil then
		srp.rallyPoint = location.Location
	end
end

Production.SetPrimaryBuilding = function(factory)
	local pb = Actor.TraitOrDefault(factory, "PrimaryBuilding")
	if pb ~= nil then
		pb:SetPrimaryProducer(factory, true)
	end
end

Production.BuildTeamFromTemplate = function(player, template, func)
	local factories = { }
	Utils.Do(template, function(t) table.insert(factories, t[1]) end)

	if Utils.Any(factories, Actor.IsDead) then
		return
	end

	if Utils.Any(factories, function(fact) return Production.EventHandlers[fact] end) then
		OpenRA.RunAfterDelay(Utils.Seconds(10), function() Production.BuildTeamFromTemplate(player, template, func) end)
		return
	end

	local team = Team.New({ })
	local teamSize = 0
	Utils.Do(template, function(t) teamSize = teamSize + #t[2] end)

	local eventHandler = function(unit)
		Team.Add(team, unit)

		if #team.Actors >= teamSize then
			func(team)
			Utils.Do(factories, function(factory)
				Production.EventHandlers[factory] = nil
			end)
		end
	end

	Utils.Do(factories, function(factory)
		Production.EventHandlers[factory] = eventHandler
	end)

	Utils.Do(template, function(t)
		Utils.Do(t[2], function(unit)
			Production.Build(t[1], unit)
		end)
	end)
end

Production.EventHandlers.Setup = function(player)
	Utils.Do(Actor.ActorsWithTrait("Production"), function(factory)
		if Actor.Owner(factory) == player then
			Actor.OnProduced(factory, function(fact, unit)
				if Production.EventHandlers[fact] then
					Production.EventHandlers[fact](unit)
				end
			end)
		end
	end)
end
