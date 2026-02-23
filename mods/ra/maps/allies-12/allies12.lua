--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

STeksToCapture = { USSRStek, BadGuyStek }
STeksCaptured = 0
STekLost = false

InitialUnitsArrived = false
TimerColor = HSLColor.OrangeRed
TimerTicks = -1

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
            Trigger.AfterDelay(DateTime.Seconds(i * 2), function()
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

FinishTimer = function()
   	DateTime.TimeLimit = 0
	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end
        Trigger.AfterDelay(DateTime.Seconds(i), function()
           if not USSRMslo.IsDead then
              UserInterface.SetMissionText(UserInterface.GetFluentMessage("nuke-incoming"), c)
            else
              UserInterface.SetMissionText(UserInterface.GetFluentMessage("nuke-averted"), c)
            end
        end)
    end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

ToBeRemoved = function()
    local toBeRemoved = Utils.Where(Neutral.GetActors(), function(a) return a.Type == "hpad" or a.Type == "hind" end)

    Utils.Do(toBeRemoved, function(a)
        a.Destroy()
    end)
end

InitTriggers = function()
    Greece.Cash = StartingCash

    IntroSequence()

    Trigger.AfterDelay(DateTime.Seconds(20), function()
        HostileVillager(CivBuilding1, PlayerBaseTarget)
    end)

    Trigger.OnCapture(BadGuyStek, function()
        --Move to when right stek is captured
        PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
        Trigger.AfterDelay(DateTime.Seconds(5), function()
            HarassingParadrop()
        end)
    end)

    TimerTicks = nuclearCountdown

    Trigger.AfterDelay(DateTime.Seconds(5), PrepareNuclearLaunch)
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
    
    --Debug function. Remove later
    ToBeRemoved()

    Trigger.AfterDelay(DateTime.Seconds(30), function()
        SetupAIActivities()
    end)

    TimerColor = USSR.Color
end
