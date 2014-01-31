Rules = { }

Rules.HasTraitInfo = function(actorType, className)
	return Internal.HasTraitInfo(actorType, className)
end

Rules.TraitInfoOrDefault = function(actorType, className)
	return Internal.TraitInfoOrDefault(actorType, className)
end

Rules.TraitInfo = function(actorType, className)
	return Internal.TraitInfo(actorType, className)
end

Rules.InitialAltitude = function(actorType)
	local ai = Rules.TraitInfoOrDefault(actorType, "AircraftInfo")
	if ai ~= nil then
		return ai.CruiseAltitude.Range
	end
	return 0
end