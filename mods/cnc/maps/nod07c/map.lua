-- standard spawn point
SPAWNPOINT = { waypoint5.Location }
-- Cell triggers arrays
PROD_CELLTRIGGERS = {CPos.New(21,48), CPos.New(20,48), CPos.New(21,47), CPos.New(20,47), CPos.New(21,46), CPos.New(20,46), CPos.New(21,45), CPos.New(20,45), CPos.New(21,44), CPos.New(20,44), CPos.New(21,43), CPos.New(20,43)}
HLP1_CELLTRIGGERS = {CPos.New(12,42), CPos.New(11,42), CPos.New(10,42), CPos.New(13,41), CPos.New(12,41), CPos.New(11,41), CPos.New(14,40), CPos.New(13,40), CPos.New(12,40), CPos.New(6,40), CPos.New(5,40), CPos.New(4,40), CPos.New(6,39), CPos.New(5,39), CPos.New(4,39), CPos.New(6,38), CPos.New(5,38), CPos.New(4,38)}
HLP2_CELLTRIGGERS = {CPos.New(35,23), CPos.New(34,23), CPos.New(35,22), CPos.New(34,22), CPos.New(35,21), CPos.New(34,21), CPos.New(35,20), CPos.New(34,20), CPos.New(35,19), CPos.New(34,19), CPos.New(35,18), CPos.New(34,18), CPos.New(35,17), CPos.New(34,17), CPos.New(35,16), CPos.New(34,16), CPos.New(35,15), CPos.New(34,15), CPos.New(35,14), CPos.New(34,14), CPos.New(35,13), CPos.New(34,13), CPos.New(35,12), CPos.New(34,12)}
ATK1_CELLTRIGGERS = {CPos.New(57,19), CPos.New(56,19), CPos.New(55,19), CPos.New(54,19), CPos.New(53,19), CPos.New(52,19), CPos.New(51,19), CPos.New(50,19), CPos.New(49,19), CPos.New(48,19), CPos.New(47,19), CPos.New(46,19), CPos.New(57,18), CPos.New(56,18), CPos.New(55,18), CPos.New(54,18), CPos.New(53,18), CPos.New(52,18), CPos.New(51,18), CPos.New(50,18), CPos.New(49,18), CPos.New(48,18), CPos.New(47,18), CPos.New(46,18), CPos.New(47,17), CPos.New(46,17), CPos.New(47,16), CPos.New(46,16), CPos.New(47,15), CPos.New(46,15), CPos.New(47,14), CPos.New(46,14), CPos.New(47,13), CPos.New(46,13), CPos.New(47,12), CPos.New(46,12), CPos.New(47,11), CPos.New(46,11)}

-- trigger activater
HUNT_ACTIVATE = { Actor69, Actor70, Actor73, Actor74, Actor75, Actor79 }
WIN2_ACTIVATE = { Actor82, Actor83, Actor84, Actor85, Actor86, Actor87, Actor88, Actor89, Actor90, Actor92, Actor93, Actor106 }
--WIN2_ACTIVATE = { Actor95 }
DELX_ACTIVATE = { Actor71 }


-- teamunits 
APC1_UNITS = { "e2", "e2", "e2", "e6", "e6", "apc" }
-- what is tran (unit from c&c95?
NOD1_UNITS = {"e3", "e3","e3", "e3","e3"}
GDI1_UNITS = {"e2", "e2","e2"}
GDI2_UNITS = {"mtnk", "mtnk"}
GDI3_UNITS = {"e2", "e2","e2", "e2"}
GDI4_UNITS = {"e1"}
GDI5_UNITS = {"mtnk"}
GDI6_UNITS = {"mtnk"}
GDI7_UNITS = {"jeep"}
GDI8_UNITS = {"e2", "e2", "e2", "e6", "e6", "apc"}
GDI9_UNITS = {"e2", "e2","e2", "e2"}
GDI10_UNITS = {"mtnk", "mtnk"}

GDI1_UNITS_WAY = function(units)  
    units.Move(waypoint0.Location)
    units.Move(waypoint1.Location)
    units.Move(waypoint2.Location)
    units.AttackMove(waypoint14.Location)    
end

GDI2_UNITS_WAY = function(units)   
    units.Move(waypoint0.Location)
    units.Move(waypoint1.Location)
    units.Move(waypoint2.Location)
    units.Move(waypoint3.Location)
    units.Move(waypoint4.Location)
    units.AttackMove(waypoint9.Location)    
end

GDI3_UNITS_WAY = function(units)     
    units.Move(waypoint0.Location)
    units.Move(waypoint4.Location)
    units.Move(waypoint5.Location)
    units.Move(waypoint6.Location)
    units.Move(waypoint7.Location)
    units.AttackMove(waypoint8.Location)    
end

GDI4_UNITS_WAY = function(units)     
    units.Move(waypoint0.Location)
    units.Move(waypoint4.Location)
    units.AttackMove(waypoint9.Location)    
end

GDI5_UNITS_WAY = function(units)      
    units.Move(waypoint0.Location)
    units.Move(waypoint4.Location)
    units.Move(waypoint10.Location)
    units.Move(waypoint11.Location)
    units.Move(waypoint12.Location)
    units.AttackMove(waypoint13.Location)    
end

GDI6_UNITS_WAY = function(units)      
    units.Move(waypoint0.Location)
    units.Move(waypoint4.Location)
    units.AttackMove(waypoint9.Location)    
end

GDI7_UNITS_WAY = function(units)       
    units.Move(waypoint0.Location)
    units.Move(waypoint4.Location)
    units.Move(waypoint5.Location)
    units.Move(waypoint6.Location)
    units.Move(waypoint7.Location)
    units.AttackMove(waypoint8.Location)    
end

GDI8_UNITS_WAY = function(units)
    units.Move(waypoint12.Location)      
    units.Move(waypoint11.Location)
    units.Move(waypoint10.Location)
    units.Move(waypoint4.Location)
    units.Move(waypoint5.Location)
    units.Move(waypoint6.Location)
    units.Move(waypoint7.Location)
    units.AttackMove(waypoint8.Location)    
end

GDI9_UNITS_WAY = function(units)
    units.AttackMove(waypoint1.Location)    
end

GDI10_UNITS_WAY = function(units)
    units.AttackMove(waypoint14.Location)    
end

--/************************************ hunt *********************************/
TRIGGER_hunt = function()
    local list = GoodGuy.GetGroundAttackers()
    for idx, val in pairs(list) do        
        val.Hunt()
    end
end

--/************************************ atk1 *********************************/
ATK1_MOVEMENT = function(units)
    -- 2,Move:0,Attack Units:50    
    if units.IsDead == false then
        units.Move(waypoint0.Location)
        units.Move(waypoint1.Location)
        units.Move(waypoint2.Location)
        units.Move(waypoint3.Location)
        units.Move(waypoint4.Location)
        units.AttackMove(waypoint9.Location)
    end
end


--/************************************ win2 *********************************/
WIN2_SWITCH = false


--/************************************ auto *********************************/
TEAM_TYPE_ARRAY = {GDI1_UNITS, GDI2_UNITS, GDI3_UNITS, GDI4_UNITS, GDI5_UNITS, GDI6_UNITS, GDI7_UNITS, GDI8_UNITS, GDI9_UNITS, GDI10_UNITS}
TEAM_TYPE_ARRAY_WAY = {GDI1_UNITS_WAY, GDI2_UNITS_WAY, GDI3_UNITS_WAY, GDI4_UNITS_WAY, GDI5_UNITS_WAY, GDI6_UNITS_WAY, GDI7_UNITS_WAY, GDI8_UNITS_WAY, GDI9_UNITS_WAY, GDI10_UNITS_WAY}
TRIGGER_AUTO = function()    
    --num = math.random(10)
    -- this should be a random number
    num = 6
    Reinforcements.Reinforce(GoodGuy, TEAM_TYPE_ARRAY[num], SPAWNPOINT, 15, function(unit)
        TEAM_TYPE_ARRAY_WAY[num](unit)
    end)
end

--/************************************ apc1 *********************************/
TRIGGER_APC1 = function()    
		Reinforcements.Reinforce(BadGuy, APC1_UNITS, SPAWNPOINT, 15, function(unit)
			unit.Move(waypoint12.Location)
			unit.Move(waypoint11.Location)
			unit.Move(waypoint10.Location)
			unit.Move(waypoint4.Location)
			unit.Move(waypoint5.Location)
			unit.Move(waypoint6.Location)
			unit.Move(waypoint7.Location)
			unit.Move(waypoint8.Location)
			-- Move Unload is not found
		end)
	end

--/************************************ WorldLoaded *********************************/
WorldLoaded = function()
	GoodGuy = Player.GetPlayer("GoodGuy")
	BadGuy = Player.GetPlayer("BadGuy")
    
    -- there is a need for random numbers later
    --math.randomseed(os.time())

	Trigger.OnObjectiveAdded(BadGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(BadGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(BadGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(BadGuy, function()
		Media.PlaySpeechNotification(GoodGuy, "Win")
	end)

	Trigger.OnPlayerLost(BadGuy, function()
		Media.PlaySpeechNotification(GoodGuy, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("visor.vqa")
		end)
	end)

	BadGuyObjective = BadGuy.AddPrimaryObjective("Destory the base of GDI and all units. Destory the village near the tiberium field.")
	GoodGuyObjective = GoodGuy.AddPrimaryObjective("Kill all enemies!")

	-- hunt trigger    
    Trigger.OnAllKilled(HUNT_ACTIVATE,  function() TRIGGER_hunt() end)

    -- prod 
    -- not possible


    --atk1
    Trigger.OnEnteredFootprint(ATK1_CELLTRIGGERS, function(a, id)
        print("trigger atk1")
        -- be sure that it will activate by palyer, because [Triggers] parameter is badguy for this
        if a.Owner == BadGuy then
            MyActors = {Actor109,Actor110}
            for key,value in pairs(MyActors) do
                ATK1_MOVEMENT(MyActors[key])
            end  
        end
        Trigger.RemoveFootprintTrigger(id)
    end)


    --hlp2
    Trigger.OnEnteredFootprint(HLP2_CELLTRIGGERS, function(a, id)
        print("trigger hlp2")
        if a.Owner == GoodGuy then
            Reinforcements.Reinforce(BadGuy, NOD1_UNITS, SPAWNPOINT, 15, function(unit)        
                unit.Move(waypoint24.Location)      
            end)
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    --hlp2
    -- DZ at 'Z' not possible in openra


    -- win2
    Trigger.OnAllKilled(WIN2_ACTIVATE,  function() 
        print("trigger win2")
        WIN2_SWITCH = true 
    end)


    -- xxxx
    -- no airstike possible
	--XXXX_UNITS = { "a10" }
	TRIGGER_XXXX_TIME = 140
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_XXXX_TIME), function() TRIGGER_XXXX() end)

	TRIGGER_XXXX = function()
		--Reinforcements.Reinforce(BadGuy, XXXX_UNITS, SPAWNPOINT, 15, function(unit)
		--end)
	end

    -- delx
    -- I cant make trigger xxxx so I dont need to do trigger delx

    -- grd1
    -- can't be activated because there are no cell triggers to activate

    -- auto
    Trigger.AfterDelay(DateTime.Seconds(180), function() TRIGGER_AUTO() end)


	-- apc1
	TRIGGER_APC1_TIME = 350
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_APC1_TIME), function() TRIGGER_APC1() end)
	
end


tick = 0
Tick = function()
    tick = tick + 1
    --win
    if #GoodGuy.GetGroundAttackers() == 0 then
        --print("no ground attack")
        if tick % DateTime.Seconds(2) == 0 and CheckForBaseGDI() <= 1  then
            --print(CheckForBaseGDI())
            --print("no base")
            if WIN2_SWITCH == true then  
                BadGuy.MarkCompletedObjective(BadGuyObjective)
            end
        end
    end
    -- lose
    if #BadGuy.GetGroundAttackers() == 0 then
        if tick % DateTime.Seconds(1) == 0 and CheckForBaseNOD() <= 0 then
            GoodGuy.MarkCompletedObjective(GoodGuyObjective)
        end
    end
end

CheckForBaseNOD = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)        		
        return actor.Owner == BadGuy
	end)    
	return #baseBuildings
end

CheckForBaseGDI = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)        		
        return actor.Owner == GoodGuy
	end)    
	return #baseBuildings
end