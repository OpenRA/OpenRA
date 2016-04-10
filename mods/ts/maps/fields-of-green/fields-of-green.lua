NForce = { "e1", "e1", "e1", "e3", "cyborg", "cyborg" }
NForcePath = { NodW.Location, GDIBase.Location }
NForceInterval = 5

VNForce = { "bike", "bike", "bggy", "bggy", "e1", "e1", "e3" }
VNForcePath = { South.Location, GDIBase.Location }
VNForceInterval = 15

GForce = { "e1", "e1", "e1", "e1", "e2", "e1", "e2" }
GForcePath = { GDIW.Location, NodBase.Location }
GForceInterval = 5

VGForce = { "e2", "smech", "smech", "e1", "e1", "apc" }
VGForcePath = { North.Location, NodBase.Location }
VGForceInterval = 15

ProducedUnitTypes =
{
    { nodhand1, { "e1", "e3" } },
    { gdibar1, { "e1", "e2" } }
}

ProduceUnits = function(t)
    local factory = t[1]
    if not factory.IsDead then
        local unitType = t[2][Utils.RandomInteger(1, #t[2] + 1)]
        factory.Wait(Actor.BuildTime(unitType))
        factory.Produce(unitType)
        factory.CallFunc(function() ProduceUnits(t) end)
    end
end

SetupFactories = function()
    Utils.Do(ProducedUnitTypes, function(pair)
        Trigger.OnProduction(pair[1], function(_, a) BindActorTriggers(a) end)
    end)
end

SetupInvulnerability = function()
   Utils.Do(Map.NamedActors, function(actor)
        if actor.HasProperty("AcceptsUpgrade") and actor.AcceptsUpgrade("unkillable") then
            actor.GrantUpgrade("unkillable")
        end
   end)
end

SendNodInfantry = function()
    local units = Reinforcements.Reinforce(nod, NForce, NForcePath, NForceInterval)
    Utils.Do(units, function(unit)
        BindActorTriggers(unit)
    end)
    Trigger.AfterDelay(DateTime.Seconds(60), SendNodInfantry)
end

SendNodVehicles = function()
    local units = Reinforcements.Reinforce(nod, VNForce, VNForcePath, VNForceInterval)
    Utils.Do(units, function(unit)
        BindActorTriggers(unit)
    end)
    Trigger.AfterDelay(DateTime.Seconds(110), SendNodVehicles)
end

SendGDIInfantry = function()
    local units = Reinforcements.Reinforce(gdi, GForce, GForcePath, GForceInterval)
    Utils.Do(units, function(unit)
        BindActorTriggers(unit)
    end)
    Trigger.AfterDelay(DateTime.Seconds(60), SendGDIInfantry)
end

SendGDIVehicles = function()
    local units = Reinforcements.Reinforce(gdi, VGForce, VGForcePath, VGForceInterval)
    Utils.Do(units, function(unit)
        BindActorTriggers(unit)
    end)
    Trigger.AfterDelay(DateTime.Seconds(110), SendGDIVehicles)
end

BindActorTriggers = function(a)
    if a.HasProperty("Hunt") then
        Trigger.OnIdle(a, a.Hunt)
    end

    if a.HasProperty("HasPassengers") then
        Trigger.OnDamaged(a, function()
            if a.HasPassengers then
                a.Stop()
                a.UnloadPassengers()
            end
        end)
    end
end

WorldLoaded = function()
    nod = Player.GetPlayer("Nod")
    gdi = Player.GetPlayer("GDI")
    
    SetupFactories()
    SetupInvulnerability()

    Utils.Do(ProducedUnitTypes, ProduceUnits)
    SendNodInfantry()
    Trigger.AfterDelay(DateTime.Seconds(50), SendNodVehicles)
    SendGDIInfantry()
    Trigger.AfterDelay(DateTime.Seconds(70), SendGDIVehicles)
end
