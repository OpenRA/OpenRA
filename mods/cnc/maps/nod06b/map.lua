-- standard spawn point
SPAWNPOINT = { waypoint27.Location }
-- Cell triggers arrays
WIN2_CELLTRIGGERS = {CPos.New(57,57), CPos.New(56,57), CPos.New(55,57), CPos.New(57,56), CPos.New(56,56), CPos.New(55,56), CPos.New(57,55), CPos.New(56,55), CPos.New(55,55), CPos.New(57,54), CPos.New(56,54), CPos.New(55,54), CPos.New(57,53), CPos.New(56,53), CPos.New(55,53), CPos.New(57,52), CPos.New(56,52), CPos.New(55,52)}
CHIN_CELLTRIGGERS = {CPos.New(61,52), CPos.New(60,52), CPos.New(59,52), CPos.New(58,52), CPos.New(61,51), CPos.New(60,51), CPos.New(59,51), CPos.New(58,51), CPos.New(61,50), CPos.New(60,50), CPos.New(59,50), CPos.New(58,50)}
HUNT_CELLTRIGGERS = {CPos.New(61,34), CPos.New(60,34), CPos.New(59,34), CPos.New(58,34), CPos.New(57,34), CPos.New(56,34), CPos.New(55,34), CPos.New(61,33), CPos.New(60,33), CPos.New(59,33), CPos.New(58,33), CPos.New(57,33), CPos.New(56,33)}
--DZNE_CELLTRIGGERS = {CPos.New(50,30), CPos.New(49,30), CPos.New(48,30), CPos.New(47,30), CPos.New(46,30), CPos.New(45,30), CPos.New(50,29), CPos.New(49,29), CPos.New(48,29), CPos.New(47,29), CPos.New(46,29), CPos.New(45,29), CPos.New(50,28), CPos.New(49,28), CPos.New(48,28), CPos.New(47,28), CPos.New(46,28), CPos.New(45,28), CPos.New(50,27), CPos.New(49,27), CPos.New(46,27), CPos.New(45,27), CPos.New(50,26), CPos.New(49,26), CPos.New(48,26), CPos.New(47,26), CPos.New(46,26), CPos.New(45,26), CPos.New(50,25), CPos.New(49,25), CPos.New(48,25), CPos.New(47,25), CPos.New(46,25), CPos.New(45,25)}
WIN1_CELLTRIGGERS = {CPos.New(47,27)}

-- TEAMTYPES :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

-- Units
--gdi6=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,4,Move:0,Unload:0,Move:10,Attack Units:40,0,0
GDI6_UNITS = {'e1', 'e1', 'e2', 'e2', 'e2', 'tran'}
--gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,5,Move:0,Unload:0,Move:8,Move:9,Attack Units:40,0,0
GDI1_UNITS = {'e1', 'e1', 'e2', 'e2', 'e2', 'tran'}
--gdi2=GoodGuy,1,0,0,0,0,7,0,0,0,2,E1:2,E2:2,0,0,1
GDI2_UNITS = {'e1', 'e1', 'e2', 'e2'}
--gdi3=GoodGuy,1,0,0,0,0,7,0,0,0,1,JEEP:1,0,0,0
GDI3_UNITS = {'jeep'}
--gdi4=GoodGuy,1,0,0,0,0,20,0,0,0,3,E1:2,E2:2,JEEP:1,1,Attack Units:30,0,0
GDI4_UNITS = {'e1', 'e1', 'e2', 'e2', 'jeep'}
--gdi5=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:2,8,Move:1,Move:2,Move:3,Move:4,Move:5,Move:6,Move:7,Attack Units:40,0,0
GDI5_UNITS = {'e1', 'e1', 'e2', 'e2'}
--nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
NOD1_UNITS = {'tran'}

-- GENERATED

CHN2_UNITS = { "e1", "e1", "e2", "e2", "e2", "tran" }
CHN1_UNITS = { "e1", "e1", "e2", "e2", "e2", "tran" }

-- MOVEMENTS

--gdi6=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,4,Move:0,Unload:0,Move:10,Attack Units:40,0,0
GDI6_MOVEMENT = function(unit)
	unit.Move(waypoint0.Location)
	unit.AttackMove(waypoint10.Location)
end

GDI4_MOVEMENT = function(unit)
	unit.AttackMove(waypoint5.Location)
end

--gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,5,Move:0,Unload:0,Move:8,Move:9,Attack Units:40,0,0
GDI1_MOVEMENT = function(unit)
	unit.Move(waypoint0.Location)
	unit.Move(waypoint8.Location)
	unit.AttackMove(waypoint9.Location)
end

--gdi5=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:2,8,Move:1,Move:2,Move:3,Move:4,Move:5,Move:6,Move:7,Attack Units:40,0,0
GDI5_MOVEMENT = function(unit)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint4.Location)
	unit.Move(waypoint5.Location)
	unit.Move(waypoint6.Location)
	unit.AttackMove(waypoint7.Location)
end

--nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
NOD1_MOVEMENT = function(unit)
	unit.Move(waypoint17.Location)
end

-- TRIGGERS ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

-- *******************GETACTOR START  - (FUNCTION FOR CREATE TEAM) ***********************
GETACTORS = function(num, owner, type)	

	Actors = {}		
    local gactors = Map.ActorsInBox(Map.TopLeft, Map.BottomRight,	function(actor)
         return tostring(actor.Owner) == owner and tostring(actor.Type) == type and actor.IsDead == false
    end)
	
	if #gactors > num then
		for i = 1, num, 1 do	
				Actors[i] = gactors[i]	
		end
	end

	return Actors
end
-- *********************GETACTOR End****************************************************

--prod=Time,Production,3,GoodGuy,None,0
-- ************************** PROD START ************************************************
	    PRODUCTION = function(type)
		 print("PRODUCTION called!")
		 if Actor110.IsInWorld == true then
		  Actor110.Produce(type, nil)
			 print("Einheit erstellt!")
			 Trigger.AfterDelay(DateTime.Seconds(3), function() PRODUCTION(type) end)
		end
	end
-- ************************** PROD END **************************************************

-- TRIGGERS :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

--hunt=Player Enters,All to Hunt,0,BadGuy,None,0
-- ************************** HUNT START ************************************************
TRIGGER_HUNT = function()
    local list = NOD.GetGroundAttackers()
    for idx, val in pairs(list) do        
        val.Hunt()
    end
end
-- ************************** HUNT END ****************************************************

--dzne=Player Enters,DZ at 'Z',0,BadGuy,None,0
-- -- ************************** DZNE START ************************************************
	-- TRIGGER_DZNE = function()
		-- N.I.
		-- end
	-- end
-- -- ************************** DZNE END **************************************************

--win1=Player Enters,Allow Win,0,BadGuy,None,0
-- -- ************************** WIN1 START ************************************************
	 TRIGGER_WIN1 = function()
			  NOD.MarkCompletedObjective(WIN1)
  	end
		
-- -- ************************** WIN1 END **************************************************

--win2=Player Enters,Win,0,BadGuy,None,0
-- -- ************************** WIN2 START ************************************************
	 TRIGGER_WIN2 = function()
		NOD.MarkCompletedObjective(WIN2)
	end

-- -- ************************** WIN2 END **************************************************

--lose=All Destr.,Lose,0,BadGuy,None,0
-- -- ************************** LOSE START ************************************************
	-- TRIGGER_LOSE = function()
			-- S.U.
		-- end
	-- end
-- -- ************************** LOSE END **************************************************

--chn1=Attacked,Reinforce.,0,None,gdi1,0
--gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,5,Move:0,Unload:0,Move:8,Move:9,Attack Units:40,0,0
-- -- ************************** CHIN1 START ***********************************************
		
	CHN1_SWITCH = true
	CHN1_TRIGGER = function()
		if CHN1_SWITCH == true then
			Reinforcements.Reinforce(GDI,CHN1_UNITS, SPAWNPOINT, 15, function(unit)
			unit.Move(waypoint0.Location)
			-- Move Unload is not found
			unit.Move(waypoint8.Location)
			unit.Move(waypoint9.Location)
			unit.AttackMove(waypoint9.Location)
			end)
			CHN1_SWITCH= false
		end
	end

-- -- ************************** CHIN1 END *************************************************

--atk1=Attacked,Create Team,0,None,gdi4,0
--gdi4=GoodGuy,1,0,0,0,0,20,0,0,0,3,E1:2,E2:2,JEEP:1,1,Attack Units:30,0,0
-- -- ************************** ATK1 START ************************************************
	   TRIGGER_ATK1_ACTIVE = false
	   TRIGGER_ATK1 = function()
		if TRIGGER_ATK1_ACTIVE == false then
			TRIGGER_ATK1_ACTIVE = true
				print("Trigger ATK1 started")
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")
			for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GDI4_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e2")
			for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GDI4_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(1, "Player (GoodGuy)", "jeep")
			for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GDI4_MOVEMENT(MyActors[key])
		    end
	    end
	end
	
SET_ATK1_ACTIVE = function()
		TRIGGER_ATK1_ACTIVE = false
end
-- -- ************************** ATK1 END ************************************************

--atk2=Attacked,Create Team,0,None,gdi5,0
--gdi5=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:2,E2:2,8,Move:1,Move:2,Move:3,Move:4,Move:5,Move:6,Move:7,Attack Units:40,0,0
-- -- ************************** ATK2 START ************************************************
	   TRIGGER_ATK2_ACTIVE = false
	   TRIGGER_ATK2 = function()
		if TRIGGER_ATK2_ACTIVE == false then
			TRIGGER_ATK2_ACTIVE = true
				print("Trigger ATK2 started")
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")
			for key in pairs(MyActors) do
				print("Actor ATK2 losgelaufen: "..MyActors[key].Type)
				GDI5_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e2")
			for key in pairs(MyActors) do
				print("Actor ATK2 losgelaufen: "..MyActors[key].Type)
				GDI5_MOVEMENT(MyActors[key])
		    end
	    end
	end
	
SET_ATK2_ACTIVE = function()
		TRIGGER_ATK2_ACTIVE = false
end	
-- -- ************************** ATK2 END ************************************************

--chin=Player Enters,Reinforce.,0,BadGuy,nod1,0
--nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
-- -- ************************** CHIN START ************************************************
TRIGGER_CHIN = function()
     Reinforcements.Reinforce(NOD, CHIN_UNITS, SPAWNPOINT, 7, function(unit)        
        unit.Move(waypoint17.Location)
     end)
end
-- -- ************************** CHIN END **************************************************

--chn2=Attacked,Reinforce.,0,None,gdi6,0
--gdi6=GoodGuy,1,0,0,0,0,7,0,0,0,3,E1:2,E2:3,TRAN:1,4,Move:0,Unload:0,Move:10,Attack Units:40,0,0
-- -- ************************** CHIN2 START ***********************************************
		
	CHN2_SWITCH = true
	CHN2_TRIGGER = function()
		if CHN2_SWITCH == true then
			Reinforcements.Reinforce(GDI,CHN2_UNITS, SPAWNPOINT, 15, function(unit)
			unit.Move(waypoint0.Location)
			-- Move Unload is not found
			unit.Move(waypoint10.Location)
			unit.AttackMove(waypoint10.Location)
			end)
			CHN2_SWITCH= false
		end
	end


-- -- ************************** CHIN2 END ************************************************

-- -- ************************** WORLD LOADED START **************************************
WorldLoaded = function()
	GDI = Player.GetPlayer("GoodGuy")
	NOD = Player.GetPlayer("BadGuy")
	
	HUNT_ACTIVATE = { Actor112 }
	WIN1_ACTIVATE = { Actor13, Actor14, Actor15, Actor16, Actor17, Actor18, Actor19  }
	WIN2_ACTIVATE = { Actor61 }
	CHN1_ACTIVATE = { Actor84, Actor105 }
	ATK1_ACTIVATE = { Actor102, Actor103 }
	ATK2_ACTIVATE = { Actor159, Actor163 }
	CHN2_ACTIVATE = { Actor112 }
	

	
-- ************************** HUNT START ************************************************
	Trigger.OnAnyKilled(HUNT_ACTIVATE,  function() TRIGGER_HUNT() end)
-- ************************** HUNT START ************************************************
	
-- ************************** PROD START ************************************************
	Trigger.AfterDelay(DateTime.Seconds(3), function() PRODUCTION("e1") end)
-- ************************** PROD END **************************************************

-- ************************** WIN1 START ************************************************
	Trigger.OnEnteredFootprint(WIN1_CELLTRIGGERS, function(a, id)        
        if a.Owner == NOD then
           TRIGGER_WIN1()
       end
        Trigger.RemoveFootprintTrigger(id)
    end)
	
	--Trigger.OnAllKilled(WIN1_ACTIVATE,  function() TRIGGER_WIN1() end)
    --Trigger.OnObjectiveCompleted(NOD,  function() TRIGGER_WIN1() end)
-- ************************** WIN1 END **************************************************

	
-- ************************** WIN2 START ************************************************
	Trigger.OnEnteredFootprint(WIN2_CELLTRIGGERS, function(a, id)        
        if a.Owner == NOD then
			TRIGGER_WIN2()
       end
        Trigger.RemoveFootprintTrigger(id)
    end)

	--Trigger.OnAllKilled(WIN2_ACTIVATE,  function() TRIGGER_WIN2() end)
    --Trigger.OnObjectiveCompleted(NOD,  function() TRIGGER_WIN2() end)
-- ************************** WIN2 END **************************************************
	
-- ************************** CHIN1 START ***********************************************
	Trigger.OnDamaged(Actor84, function() CHN1_TRIGGER() end)
	Trigger.OnDamaged(Actor105, function() CHN1_TRIGGER() end)
-- ************************** CHIN1 END *************************************************

-- ************************** ATK1 START ************************************************
	Trigger.OnDamaged(Actor102, function() TRIGGER_ATK1() end)
	Trigger.OnDamaged(Actor103, function() TRIGGER_ATK1() end)
-- ************************** ATK1 END **************************************************

-- ************************** ATK2 START ************************************************
	Trigger.OnDamaged(Actor159, function() TRIGGER_ATK2() end)
	Trigger.OnDamaged(Actor163, function() TRIGGER_ATK2() end)
-- ************************** ATK2 END **************************************************

-- ************************** CHIN START ************************************************
   Trigger.OnEnteredFootprint(CHIN_CELLTRIGGERS, function(a, id)        
     if a.Owner == NOD then
        TRIGGER_CHIN()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)
-- ************************** CHIN END **************************************************

-- ************************** CHIN2 START ***********************************************
	Trigger.OnDamaged(Actor112, function() CHN2_TRIGGER() end)
-- ************************** CHIN2 END *************************************************
	
	Trigger.OnObjectiveAdded(NOD, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(NOD, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(NOD, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(NOD, function()
		Media.PlaySpeechNotification(NOD, "Win")
	end)

	Trigger.OnPlayerLost(NOD, function()
		Media.PlaySpeechNotification(NOD, "Lose")
	end)

	--NODObjective = NOD.AddSecondaryObjective("Kill all enemies!")
	WIN1 = NOD.AddPrimaryObjective("Steal the GDI Nuclear detonator")
	WIN2 = NOD.AddPrimaryObjective("Go to the Transport-Place")
	--GDIObjective = GDI.AddPrimaryObjective("Kill all enemies!")
	GDIObjective = GDI.AddPrimaryObjective("Stop the NOD taskforce from escaping with the detonator")
	
end
-- -- ************************** WORLD LOADED END *************************************

-- -- ************************** TICK START *********************************************
tick = 0
Tick = function()
    tick = tick + 1
    if #NOD.GetGroundAttackers() == 0 then
        if tick % DateTime.Seconds(1) == 0 and CheckForBase() <= 0 then
            GDI.MarkCompletedObjective(GDIObjective)
        end
    end
end
-- -- ************************** TICK END ***********************************************

-- -- ************************** CHECKFORBASE START ************************************
CheckForBase = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)        
		--return tostring(actor.Owner) == "Player (Newbie)"
        return actor.Owner == NOD
	end)
   -- print(tostring(baseBuildings[1]))
    --print(#baseBuildings)
	return #baseBuildings
end
-- -- ************************** CHECKFORBASE END **************************************