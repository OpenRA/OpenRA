--Checks if the area (base) has been attacked or captured by the player 
-- nw / NorthWest / TopLeft
-- se / SouthEast / bottomRight


USSRGuardHelicopters =
{
    {actor = USSRGuardingHind1, guardPoint = USSRHpad1 },
    {actor = USSRGuardingHind2, guardPoint = USSRHpad2 }
}

BadGuyGuardHelicopters =
{
    {actor = BadGuyGuardingHind1, guardPoint = BadGuyHpad1 },
    {actor = BadGuyGuardingHind2, guardPoint = BadGuyHpad2 }
}

GuardRadius = WDist.FromCells(12)

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

InitHeliGuard = function(collection)
    for _, guard in ipairs(collection) do
        SetupGuardBehavior(guard)
    end
end

SetupGuardBehavior = function(guard)
    local heli = guard.actor
    local hpad = guard.guardPoint
    local loc = hpad.CenterPosition

    local function GuardLoop()
        if not heli.IsDead then

            if not heli.AmmoCount() then
                if not hpad.IsDead then
                    heli.ReturnToBase(hpad)
                end

                Trigger.OnIdle(heli, function()
                    GuardLoop()
                end)

                return
            end

            local enemies = Map.ActorsInCircle(loc, GuardRadius, function(e)
                return e.Owner == Greece and not e.IsDead and e.Type ~= "spy" and e.Type ~= "heli" and e.Type ~= "mh60" and e.Type ~= "tran" and e.Type ~= "hind"  and e.Type ~= "mig" and e.Type ~= "yak"
            end)

            if #enemies > 0 then
                heli.Attack(enemies[1])
            else
                if not hpad.IsDead then
                    heli.ReturnToBase(hpad)
                end
            end
            Trigger.AfterDelay(DateTime.Seconds(2), (GuardLoop))
        end
    end
    Trigger.AfterDelay(DateTime.Seconds(2), (GuardLoop))
end

InitHeliGuard(USSRGuardHelicopters)
InitHeliGuard(BadGuyGuardHelicopters)
