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

Map.GetNamedActor = function(actorName)
	return Internal.GetNamedActor(actorName)
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
