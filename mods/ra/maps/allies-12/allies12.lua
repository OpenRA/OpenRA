--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--------------------------------------------------------------------
----------------    SUMMARY OF LEVEL IMPLEMENTATION	----------------
--------------------------------------------------------------------
--[[
APPROACH
First of all I tried to stick to the original level spirit more than copying verbatim triggers/events.

ORIGINAL VS ORA VERSION
The staple element of the mission IMO is the frequent "iron-curtained" mammoth tanks that come from the left (USSR player) 
so I wanted to keep that as close as possible to original intent. The deviation here, to make iron mammoth tanks more of a threat,
was to activate iron curtain only once half hp is reached instead of arrival to x coordinate. Also added a function to allow these to switch 
targets after x seconds to reduce the chance for these to be cheesed.
The village to the left of player units arrival is also another element that I would like to change a little to have more relevance overall.
Since turreted vehicles can attack while moving and most of original triggers are barely noticeable I thought about giving player a neutral
stance towards civilians, so the player will have to "force attack" them. Civilians will have a few instances where they can trigger
attacks made by USSR if the player disturbs the village. There is also the idea of adding and extra secondary objective, something around
the line of: "keep civilian casualties to a bare minimum".
Pointed by JovialFeline, in vanilla ra allies-12, FCom, if captured/destroy, selfs-destruct power plants from BadGuy base. I think this a bit of a strong effect to
keep. Maybe this could happen only if FCom is captured so it requires a bit more thought for the player if he/she wants to take that route.
Last but not least, I created a cruiser variant for England player to be able to destroy sov. beachhead as close as possible to how this
occurs in the og. ra level

BALANCE
Balance-wise since this is the penultimate (/w base) mission for the allies campaign, and ORA allows better unit control and
a smoother gameplay experience overall I wanted to make it way more difficult than the original level. Some of the intentional 
and most noticeable changes are:
- Added extra dogs for both USSR and BadGuy to protect base entrances against spies.
- Dogs, if killed, are "rebuilt" by AI (through "Reinforcments" trigger; no actual usage of inf queue).
- Capped the spy money steal amount upon refinery infiltration to 3000.
- Hard dfficulty involves 2 "iron-curtained" mammoth tanks. Only 1 in both easy and normal difficulties.
- Both AIs rebuild structures.

OTHER
Secondary objective is to protect captured Soviet Tech Centers. Added occasional paratrooper attacks as a way to threat 
secondary objective failure. With the "expanded" logic for civilians I'm thinking about adding a 
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

BadGuyPowerLine = { BadGuyPower1, BadGuyPower2, BadGuyPower3, BadGuyPower4, BadGuyPower5, BadGuyPower6 }

Villagers =
{
    { Civ1, Civ2, Civ3, Civ4, Civ5, Civ6 }, --right side
    { Civ7, Civ8, Civ9, Civ10, Civ11, Civ12 }, -- mid
    { Civ13, Civ14, Civ15 } -- left side
}

VillageHouses = --This may be removed
{
    { CivBuild1, CivBuild2, CivBuild3, CivBuild4, CivBuild5, CivBuild6 }, --right side
    { CivBuild7, CivBuild8, CivBuild9, CivBuild10 }, -- mid
    { CivBuild11, CivBuild12, CivBuild13 } -- left side
} 

AggressiveVillagers = { Civ1, Civ3, Civ4, Civ11, Civ12 }

VillagerAlert = 0
VillageGuardTypes = { "e2", "e2", "e2", "e4", "e4" }

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
            SendAlertedParatroopers()
            activeFlare = true
        elseif activeFlare then
            Trigger.RemoveProximityTrigger(id)
        end
    end)

    Trigger.AfterDelay(DateTime.Seconds(30), function()
        USSR.GetActorsByType("flare")[1].Destroy()
    end)
end

SendAlertedParatroopers = function()
    local angles = { Angle.New( -60 ), Angle.New( 60 ) }

    Utils.Do(angles, function(angle)
        SendParadrop(PlayerBaseTarget.CenterPosition, Angle.South, angle)
    end)
end

ProtectedVillage = function()
    Utils.Do(Villagers, function(a)
        Trigger.OnDamaged(a, function(actor, attacker)
            if attacker.Owner == Greece and not VillagerAlert then
                VillagerAlert = true
                VillagerCallsForHelp()
            end
        end)
    end)
end

VillagerCallsForHelp = function()
    local alertingCiv = Utils.Random( {Civ1, Civ2, Civ4, Civ6} )
    local alert = USSRLeftAtkPath3

    Utils.Do(Villagers, function(actors)
        Utils.Do(actors, function()

        end)
    end)
    Trigger.OnDamaged( function()

    end)

    alertingCiv.Move(rescue.Location)
end

SpawnVillageGuards = function()
    if not CivBuild13.IsDead then
        local guards = Reinforcements.Reinforce(USSR, VillageGuardTypes, { CivBuilding2.Location, CivBuilding2.Location + CVec.New(-1, -2) })
    end

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

    Trigger.AfterDelay(DateTime.Minutes(2), function()
        if not CivBuild1.IsDead then
            HostileVillager(CivBuilding1, PlayerBaseTarget)
        end
    end)

    Trigger.OnCapture(BadGuyStek, function()
        Trigger.AfterDelay(DateTime.Seconds(5), function()
            HarassingParadrop()
        end)
    end)

    TimerTicks = nuclearCountdown

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
            if STeksCaptured == 1 then
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
            end
        end)
    end)

    Trigger.OnKilled(USSRIron, function()
        Greece.MarkCompletedObjective(DestroyIron)
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
        --Small wait to allow speech notification (ObjectiveMet)
        Trigger.AfterDelay(DateTime.Seconds(1), function() 
            Greece.MarkCompletedObjective(CaptureTech)
        end)
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
    
    ToBeRemoved() --Debug function. Remove later

    Trigger.AfterDelay(DateTime.Seconds(30), function()
        SetupAIActivities()
    end)

    PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
    TimerColor = USSR.Color
end
