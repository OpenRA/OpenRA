--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SetDifficulty = function()
    if Difficulty == "easy" then
        StartingCash = 7000

        USSRStartingCash = 75000
        BadGuyStartingCash = 30000

        USSRActivationDelay = DateTime.Minutes(11)
        BadGuyActivationDelay = DateTime.Minutes(4)

        badgerCounterAtk = 0

        nuclearAtk = false

        InfantryAttackGroupSize = 7
        VehicleAttackGroupSize = 4 -- starts in n and it increases to n + 2
        VehicleAttackInterval = DateTime.Minutes(3)
        IronTanks = {"4tnk"}

        ProductionIntervalAir = DateTime.Seconds(15)
    elseif Difficulty == "normal" then
        StartingCash = 6000

        USSRStartingCash = 100000
        BadGuyStartingCash = 40000

        USSRActivationDelay = DateTime.Minutes(9)
        BadGuyActivationDelay = DateTime.Minutes(3)

        badgerCounterAtk = 2

        nuclearAtk = true
        nuclearWaitTime = DateTime.Minutes(40)
        nuclearCountdown = DateTime.Minutes(10)

        InfantryAttackGroupSize = 7
        VehicleAttackGroupSize = 5  -- starts in n and it increases to n + 2
        VehicleAttackInterval = DateTime.Minutes(2)
        IronTanks = {"4tnk"}

        ProductionIntervalAir = DateTime.Seconds(10)

    elseif Difficulty == "hard" then
        StartingCash = 5000

        USSRStartingCash = 125000
        BadGuyStartingCash = 50000

        USSRActivationDelay = DateTime.Minutes(7)
        BadGuyActivationDelay = DateTime.Minutes(2)

        badgerCounterAtk = 3

        nuclearAtk = true
        nuclearWaitTime = DateTime.Minutes(30)
        nuclearCountdown = DateTime.Minutes(20)

        InfantryAttackGroupSize = 10
        VehicleAttackGroupSize = 6  -- starts in n and it increases to n + 2
        VehicleAttackInterval = DateTime.Minutes(2)
        IronTanks = {"4tnk", "4tnk"}

        ProductionIntervalAir = DateTime.Seconds(5)
    end
end

--[[
ProductionInterval =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(5)
}
]]

STeksToCapture = { USSRStek, BadGuyStek }
STeksCaptured = 0

LstReinforcements =
{
	actors =  { { "mcv", "e1", "e1", "e3", "e3" }, { "jeep", "2tnk", "2tnk", "spy" }, { "arty", "1tnk", "1tnk" } },
	entryPath = { { LstMiddleEntry.Location, LstMiddleDst.Location }, { LstLeftEntry.Location, LstLeftDst.Location }, {LstRightEntry.Location, LstRightDst.Location} },
	exitPath = { { LstMiddleEntry.Location },{ LstLeftEntry.Location },  { LstRightEntry.Location } }
}

IntroSequence = function()
    local startingCam =  Actor.Create("Camera", true, { Owner = Greece, Location = LstMiddleDst.Location } )

    EnglishCruiser.Move(cruiserDeletion.Location)
    EnglishCruiser.Destroy()

    Trigger.AfterDelay(DateTime.Seconds(1), function()
        for i = 1, 3, 1 do
            Trigger.AfterDelay(DateTime.Seconds(i*2), function()
                Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", LstReinforcements.actors[i], LstReinforcements.entryPath[i], LstReinforcements.exitPath[i])
            end)
        end
    end)

    Trigger.AfterDelay(DateTime.Seconds(20), function()
        InitialUnitsArrived = true
        startingCam.Destroy()
    end)
end

HostileVillager = function(spawnLoc, targetLoc)
    local activeFlare = false
    local civ = Reinforcements.Reinforce(USSR, {"c1"}, { spawnLoc.Location, spawnLoc.Location + CVec.New(-1, 1) })[1]

    Trigger.AfterDelay(DateTime.Seconds(1), function()
        if not civ.IsDead then
            civ.Move(targetLoc.Location + CVec.New(-3, 0), 5)
        end
    end)

    Trigger.OnDamaged(civ, function(self, attacker)
        if not activeFlare then
            Trigger.ClearAll(self)
            Actor.Create("Flare", true, { Owner = USSR, Location = attacker.Location } )  
            activeFlare = true
        else
            Trigger.ClearAll(self)
        end
    end)

    Trigger.OnEnteredProximityTrigger(targetLoc.CenterPosition, WDist.FromCells(5), function(a, id)
        if a.Type == "c1" and not activeFlare  then
            Trigger.RemoveProximityTrigger(id)
            Actor.Create("Flare", true, { Owner = USSR, Location = targetLoc.Location + CVec.New(0, -1) } )
            activeFlare = true
        elseif activeFlare then
            Trigger.RemoveProximityTrigger(id)
        end
    end)

    Trigger.AfterDelay(DateTime.Seconds(30), function()
        USSR.GetActorsByType("flare")[1].Destroy()
    end)
end

InitTriggers = function()
    Greece.Cash = StartingCash

    IntroSequence()

    Trigger.AfterDelay(DateTime.Seconds(20), function()
        HostileVillager(CivBuilding1, PlayerBaseTarget)
    end)
end

PrepareObjectives = function()
	InitObjectives(Greece)

    DenyAllies = AddPrimaryObjective(USSR, "")
    DestroyIron = AddPrimaryObjective(Greece, "destroy-iron-curtain")
	CaptureTech = AddPrimaryObjective(Greece, "capture-tech-centers")
    ProtectTech = AddSecondaryObjective(Greece, "protect-tech-centers")

    Utils.Do(STeksToCapture, function(b)
        Trigger.OnCapture(b, function()

            STeksCaptured = STeksCaptured + 1
            if STeksCaptured == 2 then
                Media.PlaySpeechNotification(Greece, "FirstObjectiveMet")
            else
                Media.PlaySpeechNotification(Greece, "SecondObjectiveMet")
            end
            Media.DisplayMessage("Soviet Tech Center captured.")

            Trigger.OnKilled(b, function()
                if not STekLost then
                    Media.DisplayMessage("Soviet Tech Center destroyed.")
                    Greece.MarkFailedObjective(ProtectTech)
                    STekLost = true
                end
            end)
        end)

        Trigger.OnKilled(b, function()
            if b.Owner == USSR or b.Owner == BadGuy then
                Greece.MarkFailedObjective(CaptureTech)
                Greece.MarkFailedObjective(ProtectTech)
                --Media.DisplayMessage("Soviet Tech Center destroyed.")
            end
        end)
    end)

    Trigger.OnKilled(USSRIron, function()
        Greece.MarkCompletedObjective(DestroyIron)
        --Media.DisplayMessage("Iron Curtain destroyed.")
    end)

    Trigger.OnPlayerWon(Greece, function()
        local steks = Greece.GetActorsByType("stek")
        if #steks == 2 then
            Greece.MarkCompletedObjective(ProtectTech)
        end
        USSR.MarkFailedObjective(DenyAllies)
    end)
end

Tick = function()
    USSR.Resources = USSR.Resources - (0.01 * USSR.ResourceCapacity / 25)
    BadGuy.Resources = BadGuy.Resources - (0.01 * BadGuy.ResourceCapacity / 25)

    if STeksCaptured == 2 then
        STeksCaptured = -1
        Greece.MarkCompletedObjective(CaptureTech)
    end

    if InitialUnitsArrived then
        if Greece.HasNoRequiredUnits() then
            USSR.MarkCompletedObjective(DenyAllies)
        end
    end
end

WorldLoaded = function()
    Camera.Position = DefaultCameraPosition.CenterPosition

    Greece = Player.GetPlayer("Greece")
    USSR = Player.GetPlayer("USSR")
    BadGuy = Player.GetPlayer("BadGuy")
	England = Player.GetPlayer("England")
	Neutral = Player.GetPlayer("Neutral")

    SetDifficulty()
    InitTriggers()
    PrepareObjectives() --Replaces "InitObjectives()"

    Trigger.AfterDelay(DateTime.Minutes(1), function()
        SetupAIActivities()
    end)
end
