Production = { }

Production.BuildWithSharedQueue = function(player, unit, amount)
	Internal.BuildWithSharedQueue(player, unit, amount or 1)
end

Production.BuildWithPerFactoryQueue = function(factory, unit, amount)
	Internal.BuildWithPerFactoryQueue(factory, unit, amount or 1)
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
