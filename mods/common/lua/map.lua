Map = { }

Map.GetFacing = function(vec, currentFacing)
	return Internal.GetFacing(vec, currentFacing)
end

Map.GetRandomCell = function()
	return Internal.GetRandomCell()
end

Map.GetRandomEdgeCell = function()
	return Internal.GetRandomEdgeCell()
end

Map.IsNamedActor = function(actor)
	return Internal.IsNamedActor(actor)
end

Map.GetNamedActor = function(actorName)
	return Internal.GetNamedActor(actorName)
end

Map.GetNamedActors = function()
	return Internal.GetNamedActors()
end

Map.FindActorsInCircle = function(location, radius, func)
	local actors = Internal.FindActorsInCircle(location.CenterPosition, WRange.FromCells(radius))
	return Utils.EnumerableWhere(actors, func)
end

Map.FindActorsInBox = function(topLeft, bottomRight, func)
	local actors = Internal.FindActorsInBox(topLeft.CenterPosition, bottomRight.CenterPosition)
	return Utils.EnumerableWhere(actors, func)
end

Map.__FilterByTrait = function(a, player, trait)
	return Actor.Owner(a) == player and Actor.HasTrait(a, trait)
end

Map.__FilterByTraitAndIdle = function(a, player, trait)
	return Map.__FilterByTrait(a, player, trait) and Actor.IsIdle(a)
end

Map.FindUnitsInCircle = function(player, location, radius)
	return Map.FindActorsInCircle(location, radius, function(a) return Map.__FilterByTrait(a, player, "Mobile") end)
end

Map.FindUnitsInBox = function(player, topLeft, bottomRight)
	return Map.FindActorsInBox(topLeft, bottomRight, function(a) return Map.__FilterByTrait(a, player, "Mobile") end)
end

Map.FindStructuresInCircle = function(player, location, radius)
	return Map.FindActorsInCircle(location, radius, function(a) return Map.__FilterByTrait(a, player, "Building") end)
end

Map.FindStructuresInBox = function(player, topLeft, bottomRight)
	return Map.FindActorsInBox(topLeft, bottomRight, function(a) return Map.__FilterByTrait(a, player, "Building") end)
end

Map.FindIdleUnitsInCircle = function(player, location, radius)
	return Map.FindActorsInCircle(location, radius, function(a) return Map.__FilterByTraitAndIdle(a, player, "Mobile") end)
end

Map.FindIdleUnitsInBox = function(player, topLeft, bottomRight)
	return Map.FindActorsInBox(topLeft, bottomRight, function(a) return Map.__FilterByTraitAndIdle(a, player, "Mobile") end)
end

Map.ExpandFootprint = function(cells, allowDiagonal)
	return Utils.EnumerableToTable(Internal.ExpandFootprint(cells, allowDiagonal))
end

Map.CenterOfCell = function(position)
	return Internal.CenterOfCell(position)
end

CPos.New = function(x, y)
	return OpenRA.New("CPos", { { x, "Int32" }, { y, "Int32" } })
end

WPos.New = function(x, y, z)
	if z == nil then
		z = 0
	end
	return OpenRA.New("WPos", { { x, "Int32" }, { y, "Int32" }, { z, "Int32" } })
end

WPos.FromCPos = function(location)
	return WPos.New(location.X * 1024, location.Y * 1024, 0)
end

CVec.New = function(x, y)
	return OpenRA.New("CVec", { { x, "Int32" }, { y, "Int32" } })
end

WVec.New = function(x, y, z)
	if z == nil then
		z = 0
	end
	return OpenRA.New("WVec", { { x, "Int32" }, { y, "Int32" }, { z, "Int32" } })
end

WRange.New = function(r)
	return OpenRA.New("WRange", { { r, "Int32" } })
end

WRange.FromCells = function(cells)
	return WRange.New(cells * 1024)
end
