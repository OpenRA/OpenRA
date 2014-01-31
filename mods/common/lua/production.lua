Production = { }

Production.BuildWithSharedQueue = function(player, unit, amount)
	Internal.BuildWithSharedQueue(player, unit, amount or 1)
end

Production.BuildWithPerFactoryQueue = function(factory, unit, amount)
	Internal.BuildWithPerFactoryQueue(factory, unit, amount or 1)
end

Production.SetRallyPoint = function(factory, location)
	Actor.Trait(factory, "RallyPoint").rallyPoint = location.Location
end

Production.SetPrimaryBuilding = function(factory)
	Actor.Trait(factory, "PrimaryBuilding"):SetPrimaryProducer(factory, true)
end
