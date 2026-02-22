--Checks if the area (base) has been attacked or captured by the player 
-- nw / NorthWest / TopLeft
-- se / SouthEast / bottomRight
CheckSecuredArea = function(nw, se)
    local actors = Map.ActorsInBox( nw, se, function(actor)
		return actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs")
    end)
    Trigger.AfterDelay(DateTime.Seconds(5), function()
        CheckSecuredArea(nw, se)
    end)
end

StartUSSRAI = false
StartBadGuyAI = false

Trigger.AfterDelay(DateTime.Minutes(2), function()
    CheckSecuredArea(BadGuyBaseAreaTL, BadGuyBaseAreaBR)
    CheckSecuredArea(USSRBaseAreaTL, USSRBaseAreaBR)
end)