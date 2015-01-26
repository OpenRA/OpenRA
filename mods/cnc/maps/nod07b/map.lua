-- standard spawn point
SPAWNPOINT = { waypoint15.Location }
-- Cell triggers arrays
atk3_celltriggers = {CPos.New(53,58), CPos.New(52,58), CPos.New(51,58), CPos.New(53,57), CPos.New(52,57), CPos.New(51,57), CPos.New(53,56), CPos.New(52,56), CPos.New(51,56), CPos.New(53,55), CPos.New(52,55), CPos.New(51,55)}
atk2_celltriggers = {CPos.New(16,52), CPos.New(15,52), CPos.New(14,52), CPos.New(13,52), CPos.New(12,52), CPos.New(11,52), CPos.New(10,52), CPos.New(9,52), CPos.New(8,52), CPos.New(16,51), CPos.New(15,51), CPos.New(14,51), CPos.New(13,51), CPos.New(12,51), CPos.New(11,51), CPos.New(10,51), CPos.New(9,51), CPos.New(8,51), CPos.New(31,44), CPos.New(30,44), CPos.New(29,44), CPos.New(28,44), CPos.New(27,44), CPos.New(26,44), CPos.New(25,44), CPos.New(24,44), CPos.New(23,44), CPos.New(22,44), CPos.New(21,44), CPos.New(31,43), CPos.New(30,43), CPos.New(29,43), CPos.New(28,43), CPos.New(27,43), CPos.New(26,43), CPos.New(25,43), CPos.New(24,43), CPos.New(23,43), CPos.New(22,43), CPos.New(21,43)}
delz_celltriggers = {CPos.New(35,50), CPos.New(34,50), CPos.New(35,49), CPos.New(34,49), CPos.New(35,48), CPos.New(34,48), CPos.New(35,47), CPos.New(34,47), CPos.New(30,36), CPos.New(29,36), CPos.New(28,36), CPos.New(27,36), CPos.New(26,36), CPos.New(30,35), CPos.New(29,35), CPos.New(28,35), CPos.New(27,35), CPos.New(26,35)}
dely_celltriggers = {CPos.New(33,50), CPos.New(32,50), CPos.New(33,49), CPos.New(32,49), CPos.New(33,48), CPos.New(32,48), CPos.New(33,47), CPos.New(32,47), CPos.New(29,38), CPos.New(28,38), CPos.New(27,38), CPos.New(29,37), CPos.New(28,37), CPos.New(27,37)}
atk4_celltriggers = {CPos.New(54,47), CPos.New(53,47), CPos.New(52,47), CPos.New(51,47), CPos.New(43,47), CPos.New(54,46), CPos.New(53,46), CPos.New(52,46), CPos.New(51,46), CPos.New(50,46), CPos.New(43,46), CPos.New(42,46), CPos.New(41,46), CPos.New(43,45), CPos.New(42,45), CPos.New(41,45), CPos.New(43,44), CPos.New(42,44), CPos.New(41,44), CPos.New(43,43), CPos.New(42,43), CPos.New(41,43), CPos.New(43,42)}
atk1_celltriggers = {CPos.New(11,43), CPos.New(10,43), CPos.New(9,43), CPos.New(8,43), CPos.New(7,43), CPos.New(6,43), CPos.New(5,43), CPos.New(11,42), CPos.New(10,42), CPos.New(9,42), CPos.New(8,42), CPos.New(7,42), CPos.New(6,42), CPos.New(5,42), CPos.New(23,38), CPos.New(22,38), CPos.New(21,38), CPos.New(20,38), CPos.New(19,38), CPos.New(24,37), CPos.New(23,37), CPos.New(22,37), CPos.New(21,37), CPos.New(20,37), CPos.New(19,37)}
hunt_celltriggers = {CPos.New(48,33), CPos.New(47,33), CPos.New(46,33), CPos.New(49,32), CPos.New(48,32), CPos.New(47,32), CPos.New(50,31), CPos.New(49,31), CPos.New(48,31)}

-- near spawn {CPos.New(12,10)}
-- left jeep for test: Actor193
HUNT_ACTIVATE = { Actor127, Actor128, Actor129, Actor130, Actor131, Actor132 }
WIN_ACTIVATE = { Actor221, Actor222, Actor223, Actor224, Actor226, Actor227, Actor228, Actor229, Actor231, Actor232, Actor233, Actor234, Actor235, Actor236, Actor238 }
DELX_ACTIVATE = { Actor126 }
AUTO_ACTIVATE = { Actor118, Actor119, Actor120, Actor133 }

DeactiveTriggerYYYY = false

--/************************************ atk5 *********************************/
Trigger_Time_atk5 = 10
ATK5_UNITS = {'e1', 'e1', 'e2'}
TRIGGER_atk5 = function()
     Reinforcements.Reinforce(GDI, ATK5_UNITS, SPAWNPOINT, 15, function(unit)
        --Move:0,Move:1,Move:2,Move:3,Move:9,Move:10,Move:11,Move:6,Move:7,Attack Units:40
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint9.Location)
        unit.Move(waypoint10.Location)
        unit.Move(waypoint11.Location)
        unit.Move(waypoint6.Location)
        unit.AttackMove(waypoint7.Location)
    end)
end


--/************************************ hunt *********************************/
TRIGGER_hunt = function()
    local list = GDI.GetGroundAttackers()
    for idx, val in pairs(list) do        
        val.Hunt()
    end
end


--/************************************ atk3 *********************************/
--atk3=Player Enters,Create Team,0,BadGuy,gdi6,0
-- gdi6=GoodGuy,1,0,0,0,0,15,0,0,0,2,E2:2,E3:2,3,Move:0,Move:15,Attack Units:40,0,1
ATK3_UNITS = {'e2', 'e2', 'e3', 'e3'}
TRIGGER_atk3 = function()
     Reinforcements.Reinforce(GDI, ATK3_UNITS, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)
        unit.AttackMove(waypoint15.Location)
    end)
end

--/************************************ atk2 *********************************/
--atk2=Player Enters,Create Team,0,BadGuy,gdi4,0
--gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:1,10,Move:0,Move:1,Move:2,Move:3,Move:9,Move:10,Move:11,Move:6,Move:7,Attack Base:40,0,0
ATK2_UNITS = {'mtnk'}
TRIGGER_atk2 = function()
     Reinforcements.Reinforce(GDI, ATK2_UNITS, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint9.Location)
        unit.Move(waypoint10.Location)
        unit.Move(waypoint11.Location)
        unit.Move(waypoint6.Location)
        unit.AttackMove(waypoint7.Location)
    end)
end

--/************************************ atk1 *********************************/
--atk1=Player Enters,Create Team,0,BadGuy,gdi5,0
--gdi5=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:1,E2:2,E3:1,8,Move:0,Move:1,Move:2,Move:3,Move:4,Move:5,Move:8,Attack Units:40,0,1
ATK1_UNITS = {'e1','e2', 'e2', 'e3'}
TRIGGER_atk1 = function()
     Reinforcements.Reinforce(GDI, ATK1_UNITS, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.Move(waypoint5.Location)
        unit.AttackMove(waypoint8.Location)
    end)
end

--/************************************ yyyy *********************************/
--yyyy=Time,Create Team,55,GoodGuy,gdi2,2
YYYY_UNITS = {'e1', 'e1', 'e2'}
Trigger_Time_yyyy  = 55
TRIGGER_yyyy = function()
    Reinforcements.Reinforce(GDI, YYYY_UNITS, SPAWNPOINT, 15, function(unit)
        --gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:1,10,Move:0,Move:1,Move:2,Move:3,Move:9,Move:10,Move:11,Move:6,Move:7,Attack Units:40,0,1
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint9.Location)
        unit.Move(waypoint10.Location)
        unit.Move(waypoint11.Location)
        unit.Move(waypoint6.Location)
       unit.AttackMove(waypoint7.Location)
    end)
end

--/************************************ zzzz *********************************/
--zzzz=Time,Create Team,85,GoodGuy,gdi3,2
ZZZZ_UNITS = {'e3', 'jeep', 'e2'}
Trigger_Time_zzzz = 85
TRIGGER_zzzz = function()
    --gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,3,E2:1,E3:1,JEEP:1,8,Move:0,Move:1,Move:2,Move:3,Move:4,Move:5,Move:8,Attack Units:40,0,0
    Reinforcements.Reinforce(GDI, ZZZZ_UNITS, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.Move(waypoint5.Location)
        unit.AttackMove(waypoint8.Location)
    end)
end

--/************************************ win *********************************/
TRIGGER_win = function()
    NOD.MarkCompletedObjective(NODObjective)
end

--/************************************ delx *********************************/
DstryTrigXXXX = false
TRIGGER_delx = function()
    DstryTrigXXXX = true
end

--/************************************ Trigger_Time_prod *********************************/
Trigger_Time_prod = 2
TRIGGER_prod = function() 
    -- TODO
    --prod=Time,Production,3,GoodGuy,None,0
    -- reproduce buildings
    --GDI.StartBuildingRepairs()    
end

--/************************************ Trigger_atk4 *********************************/
-- there should be a e6 unit, but there is no
ATK4_UNITS = {'e2', 'e2', 'apc'}
TRIGGER_atk4 = function()
    --atk4=Player Enters,Reinforce.,0,BadGuy,gdi1,0
    --gdi1=GoodGuy,1,0,0,0,0,15,0,0,0,3,E2:2,E6:1,APC:1,8,Move:9,Move:10,Move:11,Move:6,Move:7,Move:14,Unload:14,Attack Base:40,0,0
    Reinforcements.Reinforce(GDI, ATK4_UNITS, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint9.Location)
        unit.Move(waypoint10.Location)
        unit.Move(waypoint11.Location)
        unit.Move(waypoint6.Location)
        unit.Move(waypoint7.Location)        
        unit.AttackMove(waypoint14.Location)
    end)
end

--/************************************ WorldLoaded *********************************/
WorldLoaded = function()
	GDI = Player.GetPlayer("GoodGuy")
	NOD = Player.GetPlayer("BadGuy")

	Trigger.OnObjectiveAdded(GDI, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(GDI, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(GDI, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(GDI, function()
		Media.PlaySpeechNotification(GDI, "Win")
	end)

	Trigger.OnPlayerLost(GDI, function()
		Media.PlaySpeechNotification(GDI, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("visor.vqa")
		end)
	end)

	NODObjective = NOD.AddPrimaryObjective("Destory the base of GDI and all units. \nDestory the village near the tiberium field.")
	GDIObjective = GDI.AddPrimaryObjective("Kill all enemies!")
	
	--XXXX_UNITS = { "a10" }
	TRIGGER_XXXX_TIME = 130
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_XXXX_TIME), function() TRIGGER_XXXX() end)    
	TRIGGER_XXXX = function()
        if DstryTrigXXXX == false then
            --TODO
            -- here must be a airstrike, so no reinforce. but not found in openra
            -- unit a10 is unkown
		    --Reinforcements.Reinforce(GDI, XXXX_UNITS, SPAWNPOINT, 15, function(unit)
		    --end)
        end
	end	

    --delz
    Trigger.OnEnteredFootprint(delz_celltriggers, function(a, id)        
        if a.Owner == NOD then
            -- destroy trigger yyyy
            DeactiveTriggerYYYY = true
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    --dely
    Trigger.OnEnteredFootprint(dely_celltriggers, function(a, id)        
        if a.Owner == NOD then
            -- destroy trigger yyyy
            DeactiveTriggerYYYY = true
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- atk5
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_atk5), function() TRIGGER_atk5() end)   

    -- hunt    
    Trigger.OnAnyKilled(HUNT_ACTIVATE,  function() TRIGGER_hunt() end)

    -- atk3
     Trigger.OnEnteredFootprint(atk3_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_atk3()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- atk2
     Trigger.OnEnteredFootprint(atk2_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_atk2()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- atk1
     Trigger.OnEnteredFootprint(atk1_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_atk1()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- zzzz
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_zzzz), function() TRIGGER_zzzz() end)  
    
    -- yyyy
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_yyyy), function() TRIGGER_yyyy() end)  

    -- win
    Trigger.OnAllKilled(WIN_ACTIVATE,  function() TRIGGER_win() end)

    --delx
    Trigger.OnAllKilled(DELX_ACTIVATE,  function() TRIGGER_delx() end)

    -- prod
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_prod), function() TRIGGER_prod() end)

    -- auto
    -- TODO
    -- How activated trigger Discovered
    -- auto=Discovered,Autocreate,0,None,None,0

     -- atk4
     Trigger.OnEnteredFootprint(atk4_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_atk4()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)
end

tick = 0
Tick = function()
    tick = tick + 1
    if #NOD.GetGroundAttackers() == 0 then
        if tick % DateTime.Seconds(1) == 0 and CheckForBase() <= 0 then
            GDI.MarkCompletedObjective(GDIObjective)
        end
    end
end

CheckForBase = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)        
        return actor.Owner == NOD
	end)
	return #baseBuildings
end
