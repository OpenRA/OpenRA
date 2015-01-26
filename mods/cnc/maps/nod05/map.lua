-- standard spawn point
SPAWNPOINT = { waypoint26.Location }
YYYY_START = { waypoint0.Location }
TransReinforcementsPath = { waypoint0.Location, waypoint9.Location}

-- Cell triggers arrays
dely_celltriggers = {CPos.New(29,30), CPos.New(28,30), CPos.New(27,30), CPos.New(26,30), CPos.New(25,30), CPos.New(24,30), CPos.New(23,30), CPos.New(22,30), CPos.New(21,30), CPos.New(29,29), CPos.New(28,29), CPos.New(27,29), CPos.New(26,29), CPos.New(25,29), CPos.New(24,29), CPos.New(23,29), CPos.New(22,29)}
delz_celltriggers = {CPos.New(29,27), CPos.New(28,27), CPos.New(27,27), CPos.New(26,27), CPos.New(25,27), CPos.New(24,27), CPos.New(29,26), CPos.New(28,26), CPos.New(27,26), CPos.New(26,26), CPos.New(25,26), CPos.New(24,26)}
atk1_celltriggers = {CPos.New(54,16), CPos.New(53,16), CPos.New(52,16), CPos.New(51,16), CPos.New(50,16), CPos.New(54,15), CPos.New(53,15), CPos.New(52,15), CPos.New(51,15), CPos.New(50,15)}
atk5_celltriggers = {CPos.New(10,33), CPos.New(9,33), CPos.New(8,33), CPos.New(9,32), CPos.New(8,32), CPos.New(7,32), CPos.New(8,31), CPos.New(7,31), CPos.New(6,31)}

AUTO_ACTIVATE = { Actor54, Actor55 }
HUNT_ACTIVATE = { Actor60, Actor61, Actor62, Actor63, Actor64, Actor19, Actor73 }

-- ************************** ATK1 start **********************************************
-- atk1=Player Enters,Reinforce.,0,BadGuy,gdi10,0
-- gdi10=GoodGuy,1,0,0,0,0,15,0,0,0,2,E2:5,TRAN:1,4,Move:9,Unload:9,Move:10,Attack Units:50,0,0
ATK1_UNITS= {'e2', 'e2', 'e2', 'e2', 'e2'}
ATK1_TRIGGER = function()
    print("ATK1_TRIGGER")
    local units = Reinforcements.ReinforceWithTransport(enemy, "tran", ATK1_UNITS, TransReinforcementsPath, {waypoint0.Location})[2]
    Utils.Do(units, function(unit) 
        Trigger.OnAddedToWorld(unit, function() 
            unit.AttackMove(waypoint10.Location)
            Trigger.ClearAll(unit) 
        end)
    end)
end
-- ************************** ATK1 end ************************************************

-- ************************** ATK2 start **********************************************
-- atk2=Time,Create Team,70,GoodGuy,gdi1,0
-- gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:1,6,Move:0,Move:1,Move:3,Move:4,Move:5,Attack Base:50,0,1
ATK2_UNITS= {'e1', 'e1', 'e2'}
ATK2_TRIGGER_TIME = 70
ATK2_MOVEMENT = function()
    print('ATK2_MOVEMENT')
    Reinforcements.Reinforce(enemy, ATK2_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.AttackMove(waypoint5.Location)
    end)
end
-- ************************** ATK2 end ************************************************

-- ************************** ATK3 start **********************************************
-- atk3=Time,Create Team,190,GoodGuy,gdi2,0
-- gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:1,6,Move:0,Move:1,Move:3,Move:4,Move:5,Attack Base:50,0,1
ATK3_UNITS= {'e1', 'e1', 'e2'}
ATK3_TRIGGER_TIME = 190
ATK3_MOVEMENT = function()
    print('ATK3_MOVEMENT')
    Reinforcements.Reinforce(enemy, ATK3_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.AttackMove(waypoint5.Location)
    end)
end
-- ************************** ATK3 end ************************************************

-- ************************** ATK4 start **********************************************
-- atk4=Time,Create Team,260,GoodGuy,gdi3,0
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,1,JEEP:1,4,Move:0,Move:1,Move:2,Attack Base:50,0,0
ATK4_UNITS= {'jeep'}
ATK4_TRIGGER_TIME = 260
ATK4_MOVEMENT = function()
    print('ATK4_MOVEMENT')
    Reinforcements.Reinforce(enemy, ATK4_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.AttackMove(waypoint2.Location)
    end)
end
-- ************************** ATK4 end ************************************************

-- ************************** ATK5 start **********************************************
-- atk5=Player Enters,Create Team,0,BadGuy,gdi12,0
-- gdi12=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:1,6,Move:0,Move:1,Move:3,Move:11,Move:12,Attack Units:50,0,0
ATK5_UNITS= {'mtnk'}
ATK5_MOVEMENT = function()
    print('ATK5_MOVEMENT')
    Reinforcements.Reinforce(enemy, ATK5_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint11.Location)
        unit.AttackMove(waypoint12.Location)
    end)
end
-- ************************** ATK5 end ************************************************

-- ************************** ATK6 start **********************************************
-- atk6=Time,Create Team,150,GoodGuy,gdi4,0
-- gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:1,6,Move:0,Move:1,Move:3,Move:4,Move:5,Attack Base:50,0,0
ATK6_UNITS= {'mtnk'}
ATK6_TRIGGER_TIME = 150
ATK6_MOVEMENT = function()
    print('ATK6_MOVEMENT')
    Reinforcements.Reinforce(enemy, ATK6_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.AttackMove(waypoint5.Location)
    end)
end
-- ************************** ATK6 end ************************************************

-- ************************** GRD start **********************************************
-- grd1=Time,Create Team,2,GoodGuy,gdi5,0
-- gdi5=GoodGuy,1,0,0,0,0,15,0,0,0,1,JEEP:2,11,Move:0,Guard:2,Move:1,Guard:2,Move:3,Guard:2,Move:1,Guard:2,Move:6,Guard:2,Loop:1,0,0
GRD_UNITS= {'jeep', 'jeep'}
GRD_TRIGGER_TIME = 2
GRD_MOVEMENT = function()
    print('GRD_MOVEMENT')
    Reinforcements.Reinforce(enemy, GRD_UNITS, YYYY_START, 15, function(unit)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint1.Location)
        unit.AttackMove(waypoint6.Location)
    end)   
end
-- ************************** GRD end ************************************************

-- ************************** XXXX start **********************************************
-- xxxx=Time,Reinforce.,100,GoodGuy,gdi13,2
-- DELX_ACTIVATE = { Actor56 }
-- gdi13=GoodGuy,1,0,0,0,0,7,0,0,0,1,A10:1,0,0,0
-- XXXX_UNITS = {'a10'}
-- TRIGGER_XXXX_TIME = 100
-- DELX_TRIGGER_BOOL = false
-- TRIGGER_XXXX = function()
--     if DELX_TRIGGER_BOOL == false then
--         print('XXXX_Trigger im IF')
--         Reinforcements.Reinforce(enemy, XXXX_UNITS, SPAWNPOINT, 15, function(unit)
--             unit.AttackMove(waypoint2.Location)
--         end)
--     end
-- end
-- ************************** XXXX end ************************************************

-- ************************** YYYY start **********************************************
-- yyyy=Time,Create Team,90,GoodGuy,gdi2,2
-- gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:1,6,Move:0,Move:1,Move:3,Move:4,Move:5,Attack Base:50,0,1
YYYY_UNITS= {'e1', 'e1', 'e2'}
TRIGGER_YYYY_TIME = 90
DELY_TRIGGER_BOOL = false
TRIGGER_YYYY = function()
    if DELY_TRIGGER_BOOL == false then
        print('YYYY_Trigger im IF')
        Reinforcements.Reinforce(enemy, YYYY_UNITS, YYYY_START, 15, function(unit)
            unit.Move(waypoint1.Location)
            unit.Move(waypoint3.Location)
            unit.Move(waypoint4.Location)
            unit.Move(waypoint5.Location)
            unit.AttackMove(waypoint26.Location)
        end)
    end
end
-- ************************** YYYY end ************************************************

-- ************************** ZZZZ start **********************************************
-- zzzz=Time,Create Team,150,GoodGuy,gdi1,2
-- gdi1=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:3,E2:1,4,Move:0,Move:1,Move:2,Attack Units:40,0,1
ZZZZ_UNITS= {'e1', 'e1', 'e1', 'e2'}
TRIGGER_ZZZZ_TIME = 150
DELZ_TRIGGER_BOOL = false
TRIGGER_ZZZZ = function()
    if DELZ_TRIGGER_BOOL == false then
        print('ZZZZ_Trigger im IF')
        Reinforcements.Reinforce(enemy, ZZZZ_UNITS, YYYY_START, 15, function(unit)
            unit.Move(waypoint1.Location)
            unit.AttackMove(waypoint2.Location)
        end)
    end
end
-- ************************** ZZZZ end ************************************************


-- ************************** AUTO start **********************************************
-- auto=Discovered,Autocreate,0,None,None,0
-- ************************** AUTO end ************************************************


-- ************************** PROD start **********************************************
-- prod=Time,Production,0,GoodGuy,None,0
-- ************************** PROD end ************************************************


-- ************************** HUNT start **********************************************
-- hunt=Destroyed,All to Hunt,0,None,None,1
-- ************************** HUNT end ************************************************


WorldLoaded = function()
	enemy = Player.GetPlayer("GoodGuy")
	player = Player.GetPlayer("BadGuy")    

    Trigger.OnEnteredFootprint(dely_celltriggers, function(a, id)
	        print('YYYY_Trigger OnEnteredFootprint')
        if a.Owner == player then
            print('YYYY_Trigger OnEnteredFootprint Player')
            DELY_TRIGGER_BOOL = true
            Trigger.RemoveFootprintTrigger(id) 
        end       
    end)    

    Trigger.OnEnteredFootprint(delz_celltriggers, function(a, id)
        print('ZZZZ_Trigger OnEnteredFootprint')
        if a.Owner == player then
            print('ZZZZ_Trigger OnEnteredFootprint Player')
            DELZ_TRIGGER_BOOL = true
            Trigger.RemoveFootprintTrigger(id) 
        end       
    end)

    Trigger.OnEnteredFootprint(atk1_celltriggers, function(a, id)
        print('ATK1_Trigger OnEnteredFootprint')
        if a.Owner == player then
            print('ATK1_Trigger OnEnteredFootprint Player')
            ATK1_TRIGGER()
            Trigger.RemoveFootprintTrigger(id) 
        end       
    end)

    Trigger.OnEnteredFootprint(atk5_celltriggers, function(a, id)
        print('ATK5_Trigger OnEnteredFootprint')
        if a.Owner == player then
            print('ATK5_Trigger OnEnteredFootprint Player')
            ATK5_MOVEMENT()
            Trigger.RemoveFootprintTrigger(id) 
        end       
    end)

--     Trigger.AfterDelay(DateTime.Seconds(TRIGGER_XXXX_TIME), function() TRIGGER_XXXX() end)
    
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_YYYY_TIME), function() TRIGGER_YYYY() end)

    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ZZZZ_TIME), function() TRIGGER_ZZZZ() end)

    Trigger.AfterDelay(DateTime.Seconds(GRD_TRIGGER_TIME), function() GRD_MOVEMENT() end)
        
    Trigger.AfterDelay(DateTime.Seconds(ATK2_TRIGGER_TIME), function() ATK2_MOVEMENT() end)

    Trigger.AfterDelay(DateTime.Seconds(ATK3_TRIGGER_TIME), function() ATK3_MOVEMENT() end)
    
    Trigger.AfterDelay(DateTime.Seconds(ATK4_TRIGGER_TIME), function() ATK4_MOVEMENT() end)
    
    Trigger.AfterDelay(DateTime.Seconds(ATK6_TRIGGER_TIME), function() ATK6_MOVEMENT() end)

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
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("insites.vqa")
		end)
	end)
    
    Media.PlayMovieFullscreen("dessweep.vqa", function()
        -- ************************** LOSE start **********************************************
        -- lose=All Destr.,Lose,0,BadGuy,None,0
        gdiObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area")
        -- ************************** LOSE end ************************************************
		
        -- ************************** WIN2 start **********************************************
        -- win2=Built It,Allow Win,10,BadGuy,None,0
        nodObjective1 = player.AddPrimaryObjective("Destroy the airbase")
        -- ************************** WIN2 end ************************************************

		-- ************************** WIN start ***********************************************
        -- win=All Destr.,Win,0,GoodGuy,None,0
        nodObjective2 = player.AddSecondaryObjective("Destroy all GDI forces")
        -- ************************** WIN end *************************************************		
	end)

    --Airbase = {'HPAD'}

    Trigger.OnKilled(Actor73, function()

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			player.MarkCompletedObjective(nodObjective1)
		end)
	end)
	

	Trigger.OnCapture(Actor73, function()
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			player.MarkCompletedObjective(nodObjective1)
		end)
	end)

	--enemyObjective = enemy.AddPrimaryObjective("Kill all enemies!")
	--playerObjective = player.AddPrimaryObjective("Kill all enemies!")
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(gdiObjective)
	end    

	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(nodObjective2)
	end
end