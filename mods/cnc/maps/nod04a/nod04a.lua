-- According to ini ref this is default spawn point. But looks to me like a bad choice because it is in the middle of the map, but maybe just on this amp
SPAWNPOINT = { waypoint27.Location }



-- ************************** ATK6 start **********************************************
-- variable trigger atk6
ATK6_ACTIVATE = {Actor34, Actor40}
--- the type of units can be read from Team Types
--- but apc is a transporter and the other units are in it. WHY?
ATK6_Team = {'c1', 'c2', 'c3', 'apc'}
-- Waypoints for this ???
-- just take first waypoint for entry (i do not know if it works every time)
-- Waypoint 0 to 7 (both included) are missing after converting (maybe renamed? or really lost?) [way points 0 - 7 added by hand]
ATK6_TeamPath = {SPAWNPOINT.Location, waypoint1.Location}
-- ************************** ATK6 end ************************************************


-- ************************** ATK5 start **********************************************
--variables trigger atk5
-- ATK5 Trigger
ATK5_TRIGGER = function()
    if ATK5_SWITCH == true then
        Reinforcements.Reinforce(enemy, ATK5_UNITS, SPAWNPOINT, 15, function(unit)
            --Move:0,
            unit.Move(waypoint0.Location)
            --Move:9,Attack Units:50
            unit.AttackMove(waypoint9.Location)
        end)
        ATK5_SWITCH= false
    end
end
-- units atk5
ATK5_UNITS = { 'e1', 'e1', 'e2', 'e2', 'apc' }
-- we need a trigger switch that the trigger will just executed once
ATK5_SWITCH = true
-- ************************** ATK5 end **********************************************


-- ************************** ATK3 start **********************************************
-- ATK3
-- this are all location from [CellTriggers]. calculated into openra coordinates
ATK3_CELLTRIGGERS = {CPos.New(18,18), CPos.New(17,18), CPos.New(16,18), CPos.New(15,18), CPos.New(14,18), CPos.New(13,18), CPos.New(12,18), CPos.New(11,18), CPos.New(24,17), CPos.New(23,17), CPos.New(22,17), CPos.New(21,17), CPos.New(20,17), CPos.New(19,17), CPos.New(17,17), CPos.New(16,17), CPos.New(15,17), CPos.New(14,17), CPos.New(13,17), CPos.New(12,17), CPos.New(11,17)}
ATK3_TRIGGERCOUNTER = 0
ATK3_MAXTRIGGER = 2

ATK3_MOVEMENT = function(units)
    -- 2,Move:0,Attack Units:50    
    if units.IsDead == false then
        units.AttackMove(waypoint0.Location)
    end
end
-- ************************** ATK3 end ************************************************


-- ************************** ATK2 start **********************************************
-- ATK2 Units
-- see TeamTypes
ATK2_UNITS = { "jeep" }
--ATK2_WAYPOINTS = { SPAWNPOINT.Location }
ATK2_CELLTRIGGERS = {CPos.New(41,22), CPos.New(40,22), CPos.New(39,22), CPos.New(41,21), CPos.New(40,21), CPos.New(39,21)}

ATK2_MOVEMENT = function(units)
    -- 2,Move:6,Attack Units:50
    if units.IsDead == false then
        units.AttackMove(waypoint2.Location)
        end
end

-- ************************** ATK2 end ************************************************



-- ************************** ATK1 start **********************************************
--atk1
-- time /= 3
TRIGGER_ATK1_TIME = 6
TRIGGER_ATK1 = function()
    Reinforcements.Reinforce(enemy, ATK1_UNITS, SPAWNPOINT, 15, function(unit)
        --8,Move:0,Move:1,Move:2,Move:3,Move:4,Move:5,Move:6,Attack Units:50
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.Move(waypoint5.Location)
        unit.AttackMove(waypoint6.Location)
    end)
end
-- units of teamtypes
ATK1_UNITS = {'e1', 'e1'}
-- ************************** ATK1 end ************************************************



-- ************************** ATK4 start **********************************************
--unit teamtype atk4
--2,E1:2,E2:1
ATK4_UNITS= {'e1', 'e1', 'e2'}
ATK4_CELLTRIGGERS = {CPos.New(29,28), CPos.New(28,28), CPos.New(29,27), CPos.New(28,27), CPos.New(29,26), CPos.New(28,26), CPos.New(29,25), CPos.New(28,25), CPos.New(29,24), CPos.New(28,24), CPos.New(29,23), CPos.New(28,23), CPos.New(29,22), CPos.New(28,22)}
ATK4_MOVEMENT = function(unit)
    -- 3,Move:0,Move:9,Attack Units:0
    unit.Move(waypoint0.Location)
    unit.Move(waypoint9.Location)
end
-- ************************** ATK4 end ************************************************

-- ************************** gciv start **********************************************
GCIV_CELLTRIGGERS = {CPos.New(51,17), CPos.New(50,17), CPos.New(49,17), CPos.New(48,17), CPos.New(47,17), CPos.New(46,17), CPos.New(45,17), CPos.New(44,17), CPos.New(43,17), CPos.New(42,17), CPos.New(41,17), CPos.New(40,17), CPos.New(39,17), CPos.New(38,17), CPos.New(37,17), CPos.New(36,17), CPos.New(35,17), CPos.New(52,16), CPos.New(51,16), CPos.New(50,16), CPos.New(49,16), CPos.New(48,16), CPos.New(47,16), CPos.New(46,16), CPos.New(45,16), CPos.New(44,16), CPos.New(43,16), CPos.New(42,16), CPos.New(41,16), CPos.New(40,16), CPos.New(39,16), CPos.New(38,16), CPos.New(37,16), CPos.New(36,16), CPos.New(35,16)}
GCIV_MOVEMENT = function(unit)
    -- 10,Move:0,Move:1,Move:2,Move:3,Move:4,Move:7,Attack Civil.:30,Move:8,Attack Civil.:30,Loop:5    
    --print(tostring(mt))
    if unit.IsDead == false then
        unit.AttackMove(waypoint6.Location)
    end
end


-- ************************** xxxx end ************************************************


-- ************************** xxxx start **********************************************
--- Trigger xxxx
TRIGGER_XXXX = function()
    -- the trigger can be deleted
    if DELX_TRIGGER_BOOL == false then
        --- Triger xxxx sends atk2
        -- i do not know why 15 and what i should choose
        Reinforcements.Reinforce(enemy, ATK2_UNITS, SPAWNPOINT, 15, function(unit)
            unit.AttackMove(waypoint6.Location)
        end)
    end
end
TRIGGER_XXXX_TIME = 50
-- ************************** xxxx end ************************************************



-- ************************** Delx start **********************************************
-- Tigger delx
DELX_TRIGGERCELLS = {CPos.New(42,20), CPos.New(41,20), CPos.New(40,20), CPos.New(39,20), CPos.New(38,20)}
DELX_TRIGGER_BOOL = false
-- ************************** delx end ************************************************


-- ************************** yyyy start **********************************************
-- trigger yyyy
TRIGGER_YYYY_TIME = 100
TRIGGER_YYYY = function ()
    if DELY_TRIGGER_BOOL == false then
        -- team atk4
        Reinforcements.Reinforce(enemy, ATK4_UNITS, SPAWNPOINT, 15, function(unit)
            --3,Move:0,Move:9,Attack Units:0
            unit.Move(waypoint0.Location)
            unit.AttackMove(waypoint9.Location)
        end)
    end
end
-- ************************** yyyy end ************************************************


-- ************************** dely start **********************************************
-- Trigger dely
DELY_TRIGGERCELLS = {CPos.New(31,28), CPos.New(30,28), CPos.New(31,27), CPos.New(30,27), CPos.New(31,26), CPos.New(30,26), CPos.New(31,25), CPos.New(30,25), CPos.New(31,24), CPos.New(30,24)}
DELY_TRIGGER_BOOL = false
-- ************************** dely end ************************************************


-- ************************** zzzz start **********************************************
-- Trigger zzzz
TRIGGER_ZZZZ_TIME = 160
TRIGGER_ZZZZ = function()
    -- so the trigger can be delted by trigger delz
    -- end also can be delete if ATK5 ist already used (maybe yes, maybe no, maybe f....) so working with bool variables may not the best solution
    if DELZ_TRIGGER_BOOL == false and ATK5_SWITCH==true then
        -- team atk5
        Reinforcements.Reinforce(enemy, ATK5_UNITS, SPAWNPOINT, 15, function(unit)
            --3,Move:0,Move:9,Attack Units:50
            unit.Move(waypoint0.Location)
            unit.AttackMove(waypoint9.Location)
        end)
    end
end
-- ************************** zzzz end ************************************************


-- ************************** delz start **********************************************
-- triger delz
DELZ_TRIGGERCELLS = {CPos.New(18,20), CPos.New(17,20), CPos.New(16,20), CPos.New(15,20), CPos.New(14,20), CPos.New(13,20), CPos.New(12,20), CPos.New(11,20), CPos.New(25,19), CPos.New(24,19), CPos.New(23,19), CPos.New(22,19), CPos.New(21,19), CPos.New(20,19), CPos.New(19,19), CPos.New(18,19), CPos.New(17,19), CPos.New(16,19), CPos.New(15,19), CPos.New(14,19), CPos.New(13,19), CPos.New(12,19), CPos.New(11,19), CPos.New(25,18), CPos.New(24,18), CPos.New(23,18), CPos.New(22,18), CPos.New(21,18), CPos.New(20,18), CPos.New(19,18)}
DELZ_TRIGGER_BOOL = false
-- ************************** delz end **********************************************

WorldLoaded = function()
	player = Player.GetPlayer("BadGuy")
	enemy = Player.GetPlayer("GoodGuy")
	

    -- *********************************************** ATK6 start ***************************************************************
    -- Trigger atk6: because Destroyed == OnAnyKilled, but can also maybe OnAllKilled
    -- actor list is STRUCTURES 25 and 20 from ini (structur 25 === Actor34; 20 == Actor40)
    Trigger.OnAnyKilled(ATK6_ACTIVATE, function()
        -- Trigger activates Reinforce
        -- enemy: beacuse TeamTypes will activate
        -- ATK6_Team: this team type belongs to the triger
        -- ATK6_TeamPath: is there a team path 
        -- 15: i do not know why and what for
        Reinforcements.Reinforce(enemy, ATK6_Team, ATK6_TeamPath, 15, function(unit)
            -- 11,Move:1,Move:2,Move:3,Move:4,Move:7,Unload:7,Attack Civil.:30,Move:8,Attack Civil.:30,Move:7,Loop:6
            -- maybe the units has to do something, but i do not know
            unit.Move(waypoint1.Location)
            unit.Move(waypoint2.Location)
            unit.Move(waypoint3.Location)
            unit.Move(waypoint4.Location)
            -- why need unload when nothing is in it nad how to?
            --UnloadPassengers()
            -- Attack Civil. is skiped, maybe there is something for it. so it is attack an move 
            unit.AttackMove(waypoint7.Location)
            unit.AttackMove(waypoint8.Location)

            -- todo loop
        end)        
    end)
    -- *********************************************** ATK6 end ***************************************************************


    -- *********************************************** ATK5 start ***************************************************************
    -- trigger atk5
    -- the trigger attack is ondamaged in openra 
    -- for every unit which can be attacked according to trigger there must be an ondamaged trigger. also there must be a switch that trigger is just call once
    -- unit 16 is actor 116
    -- unit 15 is actor 117
    -- unit 14 is actor 118
    -- for test 105
    Trigger.OnDamaged(Actor116, ATK5_TRIGGER)
    Trigger.OnDamaged(Actor117, ATK5_TRIGGER)
    Trigger.OnDamaged(Actor118, ATK5_TRIGGER)
    -- *********************************************** ATK5 end ***************************************************************



    -- *********************************************** ATK3 start ***************************************************************
    -- trigger atk3
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(ATK3_CELLTRIGGERS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- there should be a team created but I do not know which units. Seems to be no Reinforcements

            -- find 3,E1:3,E2:2,MTNK:1 this units an do this:
            --local units = getTeam({'e1', 'e1', 'e1', 'e2', 'e2', 'mtnk'}, enemy)
            
            ----print(type(ATK6_ACTIVATE))
            ----print(type(units))
            ----print(table.maxn(units))
           

            -- randomly choose the units:
            -- e1: 106, 107, 132
            -- e2: Actor127, Actor121
            -- mtnk: Actor 65
            -- and outsurced in function
            MyActors = {Actor106, Actor107, Actor132, Actor127, Actor121, Actor65}
            --MyActors = {Map.NamedActor('Actor106'),Map.NamedActor('Actor107')}
            for key,value in pairs(MyActors) do --actualcode
                ATK3_MOVEMENT(MyActors[key])
            end           
            -- this trigger can activated 2 times. so there should be a condition before removing
            ATK3_TRIGGERCOUNTER = ATK3_TRIGGERCOUNTER + 1
            if ATK3_TRIGGERCOUNTER > ATK3_MAXTRIGGER then
                Trigger.RemoveFootprintTrigger(id)
            end
        end
    end)
    -- *********************************************** ATK3 end *****************************************************************


    -- *********************************************** ATK2 start ***************************************************************
    -- trigger atk2
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(ATK2_CELLTRIGGERS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- there should be a team created but I do not know which units. Seems to be no Reinforcements

            -- find JEEP:1 this units an do this:
            MyActors = {Actor62}
            -- 2,Move:6,Attack Units:50
            for key,value in pairs(MyActors) do
                ATK2_MOVEMENT(MyActors[key])
            end 

            -- delete this trigger 
            Trigger.RemoveFootprintTrigger(id)
        end
    end)
    -- *********************************************** ATK2 end ***************************************************************


    -- *********************************************** gciv start ***************************************************************
    -- trigger gciv
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(GCIV_CELLTRIGGERS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- there should be a team created but I do not know which units. Seems to be no Reinforcements

            -- find 5,E1:2,C1:1,C2:1,C3:1,C4:1, this units an do this:
            -- e1: 87, 107
            -- c1: 85,
            -- c2 82
            -- c3:  81
            -- c4: 98
            MyActors = {Actor87, Actor107, Actor85, Actor82, Actor81, Actor98}            
            for key,value in pairs(MyActors) do
            -- 10,Move:0,Move:1,Move:2,Move:3,Move:4,Move:7,Attack Civil.:30,Move:8,Attack Civil.:30,Loop:5
                GCIV_MOVEMENT(MyActors[key])
            end 
            -- delete this trigger 
            Trigger.RemoveFootprintTrigger(id)

        end
    end)
    -- *********************************************** gciv end ***************************************************************

    -- *********************************************** atk4 start ***************************************************************
    -- trigger atk4
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(ATK4_CELLTRIGGERS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- there should be a team created but I do not know which units. Seems to be no Reinforcements

            -- find 2,E1:2,E2:1, this units an do this:
            -- e1: 132
            -- e2: 123
            MyActors = {Actor132, Actor123}            
            for key,value in pairs(MyActors) do
            -- 10,Move:0,Move:1,Move:2,Move:3,Move:4,Move:7,Attack Civil.:30,Move:8,Attack Civil.:30,Loop:5
                ATK4_MOVEMENT(MyActors[key])
            end 
            -- delete this trigger 
            Trigger.RemoveFootprintTrigger(id)

        end
    end)
    -- *********************************************** atk4 end ***************************************************************



    -- atk1 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK1_TIME), function() TRIGGER_ATK1() end)


    -- xxxx start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_XXXX_TIME), function() TRIGGER_XXXX() end)

    -- delx cell enter trigger
    Trigger.OnEnteredFootprint(DELX_TRIGGERCELLS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- no Reinforcements are sent: delete trigger xxxx
            DELX_TRIGGER_BOOL = true
            Trigger.RemoveFootprintTrigger(id)
        end
    end)


    -- yyyy start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_YYYY_TIME), function() TRIGGER_YYYY() end)

    -- dely cell enter trigger
    Trigger.OnEnteredFootprint(DELY_TRIGGERCELLS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- no Reinforcements are sent: delete trigger xxxx
            DELY_TRIGGER_BOOL = true
            Trigger.RemoveFootprintTrigger(id)
        end
    end)

    -- zzzz start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ZZZZ_TIME), function() TRIGGER_ZZZZ() end)

    -- delz cell enter trigger
    Trigger.OnEnteredFootprint(DELZ_TRIGGERCELLS, function(a, id)
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == player then
            -- no Reinforcements are sent: delete trigger xxxx
            DELZ_TRIGGER_BOOL = true
            Trigger.RemoveFootprintTrigger(id)
        end
    end)

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)
	
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("deskill.vqa")
		end)
	end)	
	
	playerObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	enemyObjective = player.AddPrimaryObjective("Kill all civilian which support the GDI.")	   
    
end

--function getTeam(units, user)
    ----print('getTeam')
    --
    --local myActors = user.GetGroundAttackers()
    --local myTeam = {}
    ----local map = Map.NamedActors()
    ----print(inspect(Map))
    ----print(type(Map))
    ----print(player.PlayerName )
    ----for var, actor in myActors(Actors) do
     ----   enemy.AddPrimaryObjective(actor)
    ----end
    
    --for i, valA in pairs(myActors) do
        
        ----print(tostring(index).."="..tostring(wert)..", ")
        --for j, valU in pairs(units) do
            --for w in string.gmatch(tostring(valA),tostring(valU)) do
                --table.remove(units, j)                
                ----local open = string.find(tostring(valA), "(")
                ----local length = string.find(tostring(valA), " ", open)
                ----local index = string.sub(tostring(valA), open, open+length)
                ----print(index)
                ----print(w..": Actor="..tostring(valA).." /indx:"..j.."|| unit: "..tostring(valU))
                --table.insert(myTeam, Map.NamedActor("Actor"..i))
                ----print(table.getn(units))
                --break
            --end
            ----print(tostring(index).."="..tostring(wert)..", ")
        --end
    --end    
    --return myTeam
--end

Tick = function()
	if player.HasNoRequiredUnits()  then
		enemy.MarkCompletedObjective(playerObjective)
	end

	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(enemyObjective)
	end
end