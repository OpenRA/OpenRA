-- STANARD SWANPOINT
SPAWNPOINT = { waypoint27.Location }

-- Cell triggers arrays
win2_celltriggers = {CPos.New(20,55), CPos.New(19,55), CPos.New(20,54), CPos.New(19,54), CPos.New(20,53), CPos.New(19,53), CPos.New(20,52), CPos.New(19,52)}
chin_celltriggers = {CPos.New(31,49), CPos.New(30,49), CPos.New(29,49), CPos.New(28,49), CPos.New(27,49), CPos.New(26,49), CPos.New(25,49), CPos.New(24,49), CPos.New(23,49), CPos.New(22,49), CPos.New(21,49), CPos.New(20,49), CPos.New(31,48), CPos.New(30,48), CPos.New(29,48), CPos.New(28,48), CPos.New(27,48), CPos.New(26,48), CPos.New(25,48), CPos.New(24,48), CPos.New(23,48), CPos.New(22,48), CPos.New(21,48), CPos.New(20,48), CPos.New(31,47), CPos.New(30,47), CPos.New(29,47), CPos.New(28,47), CPos.New(27,47), CPos.New(26,47), CPos.New(25,47), CPos.New(24,47), CPos.New(23,47), CPos.New(22,47), CPos.New(21,47), CPos.New(20,47)}
-- dzon_celltriggers = {CPos.New(26,24), CPos.New(25,24), CPos.New(24,24), CPos.New(23,24), CPos.New(22,24), CPos.New(26,23), CPos.New(25,23), CPos.New(24,23), CPos.New(23,23), CPos.New(22,23), CPos.New(26,22), CPos.New(25,22), CPos.New(23,22), CPos.New(22,22), CPos.New(25,21), CPos.New(24,21), CPos.New(23,21), CPos.New(22,21), CPos.New(25,20), CPos.New(24,20), CPos.New(23,20), CPos.New(22,20)}
win_celltriggers = {CPos.New(24,22)}

-- TEAMTYPES ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


-- UNITS


-- gdi2=GoodGuy,1,0,0,0,0,2,0,0,0,1,JEEP:1,0,0,0
GDI2_UNITS = {'jeep'}
-- gdi1=GoodGuy,1,0,0,0,0,2,0,0,0,2,E1:2,E2:3,0,0,1
GDI1_UNITS = {'e1', 'e1', 'e2', 'e2', 'e2'}
-- chin=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:10,0,0
CHIN_UNITS = {'tran'}
-- grd1=GoodGuy,1,0,0,0,0,15,0,0,0,3,E2:2,E3:2,MTNK:1,12,Move:0,Move:1,Guard:3,Move:2,Guard:3,Move:3,Guard:3,Move:4,Guard:3,Move:5,Guard:3,Loop:0,0,0
GRD1_UNITS = {'e2', 'e2', 'e3', 'e3', 'mtnk'}
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:3,JEEP:1,1,Attack Units:40,0,0
GDI3_UNITS = {'e1', 'e1', 'e2', 'e2', 'e2', 'jeep'}


-- MOVEMENTS


-- chin=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:10,0,0
CHIN_MOVEMENT = function(unit)
	unit.Move(waypoint10.Location)
end

-- grd1=GoodGuy,1,0,0,0,0,15,0,0,0,3,E2:2,E3:2,MTNK:1,12,Move:0,Move:1,Guard:3,Move:2,Guard:3,Move:3,Guard:3,Move:4,Guard:3,Move:5,Guard:3,Loop:0,0,0
GRD1_MOVEMENT = function(unit)
	unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint4.Location)
	unit.Move(waypoint5.Location)
	-- ToDo Loop (Loop: 0,0,0)
end
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:3,JEEP:1,1,Attack Units:40,0,0
GDI3_MOVEMENT = function(unit)
	unit.AttackMove(waypoint6.Location)
end

-- *******************GETACTOR START  - (FUNCTION FOR CREATE TEAM) **************
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
-- *********************GETACTOR End**************************************

-- TRIGGERS ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

-- prod=Time,Production,5,GoodGuy,None,0
-- ************************** PROD START ************************************************
	    PRODUCTION = function(type)
		 print("PRODUCTION called!")
		 if Actor71.IsInWorld == true then
		  Actor71.Produce(type, nil)
			 print("Einheit erstellt!")
			 Trigger.AfterDelay(DateTime.Seconds(5), function() PRODUCTION(type) end)
		end
	end
-- ************************** PROD END ************************************************

-- win2=Player Enters,Win,0,BadGuy,None,0

-- -- ************************** WIN2 START ************************************************
	 TRIGGER_WIN2 = function()
			  NOD.MarkCompletedObjective(WIN2)
  	end
		
-- -- ************************** WIN2 END ************************************************

-- -- win=Player Enters,Allow Win,0,BadGuy,None,0

-- -- ************************** WIN START ************************************************
	 TRIGGER_WIN = function()
		NOD.MarkCompletedObjective(WIN)
	end

-- -- ************************** WIN END ************************************************

-- -- chin=Player Enters,Reinforce.,0,BadGuy,chin,0
-- -- chin=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:10,0,0

-- -- ************************** CHIN START ************************************************
		
TRIGGER_CHIN = function()
     Reinforcements.Reinforce(NOD, CHIN_UNITS, SPAWNPOINT, 7, function(unit)        
        unit.Move(waypoint10.Location)
     end)
end

-- -- ************************** CHIN END ************************************************

-- -- dzon=Player Enters,DZ at 'Z',0,BadGuy,None,0

-- -- ************************** DZON START ************************************************
	-- TRIGGER_DZON = function()
		-- N.I.
		-- end
	-- end
-- -- ************************** DZON END ************************************************

-- -- NOD=All Destr.,Lose,0,BadGuy,None,0
		
-- -- ************************** NOD START ************************************************
	-- TRIGGER_NOD = function()
			-- S.U.
		-- end
	-- end
-- -- ************************** NOD END ************************************************

-- -- atk1=Time,Create Team,3,GoodGuy,grd1,0
-- -- grd1=GoodGuy,1,0,0,0,0,15,0,0,0,3,E2:2,E3:2,MTNK:1,12,Move:0,Move:1,Guard:3,Move:2,Guard:3,Move:3,Guard:3,Move:4,Guard:3,Move:5,Guard:3,Loop:0,0,0


-- -- ************************** ATK1 START ************************************************
	   TRIGGER_ATK1 = function()
				print("Trigger ATK1 started")
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e2")
				for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GRD1_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e3")
				for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GRD1_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(1, "Player (GoodGuy)", "mtnk")
			for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GRD1_MOVEMENT(MyActors[key])
		    end
	    end
-- -- ************************** ATK1 END ************************************************

-- -- atk2=Destroyed,Create Team,0,None,gdi3,0
-- -- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:3,JEEP:1,1,Attack Units:40,0,0


-- -- ************************** ATK2 START ************************************************
	   TRIGGER_ATK2 = function()
				print("Trigger ATK2 started")
				MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")
			for key in pairs(MyActors) do
				print("Actor ATK2 losgelaufen: "..MyActors[key].Type)
				GDI3_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(3, "Player (GoodGuy)", "e2")
			for key in pairs(MyActors) do
				print("Actor ATK2 losgelaufen: "..MyActors[key].Type)
				GDI3_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(1, "Player (GoodGuy)", "jeep")
			for key in pairs(MyActors) do
				print("Actor ATK2 losgelaufen: "..MyActors[key].Type)
				GDI3_MOVEMENT(MyActors[key])
		    end
	    end
-- -- ************************** ATK2 END ************************************************

-- -- ************************** WORLD LOADED START *************************************
WorldLoaded = function()

	GDI = Player.GetPlayer("GoodGuy")
	NOD = Player.GetPlayer("BadGuy")
	
	-- ************************** ACTIVATE_ACTORS START *************************************
	WIN2_ACTIVATE = { Actor8, Actor16, Actor58, Actor66, Actor9, Actor43, Actor57 }
	WIN_ACTIVATE = { Actor39 } 
	ATK2_ACTIVATE = { Actor87, Actor88, Actor89, Actor97, Actor98, Actor99, Actor100, Actor103, Actor106, Actor107, Actor108, Actor109, Actor116, Actor92, Actor93, Actor104, Actor105, Actor114, Actor115, Actor119, Actor120, Actor121, Actor122, Actor72, Actor73, Actor85 }
	-- ************************** ACTIVATE_ACTORS END ***************************************
	
	-- ************************** PROD START ************************************************
	Trigger.AfterDelay(DateTime.Seconds(5), function() PRODUCTION("e1") end)
	-- ************************** PROD END **************************************************
	-- ************************** CHIN START ************************************************
	 Trigger.OnEnteredFootprint(chin_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_CHIN()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)
	-- ************************** CHIN END **************************************************
		
	-- ************************** ATK1 START ************************************************
	Trigger.AfterDelay(DateTime.Seconds(3), function() TRIGGER_ATK1() end)
	-- ************************** ATK1 END **************************************************

	-- ************************** ATK2 START ************************************************
	 Trigger.OnAllKilled(ATK2_ACTIVATE, function() TRIGGER_ATK2() end)
	-- ************************** ATK2 END **************************************************
	
	-- ************************** WIN2 START ************************************************
	Trigger.OnEnteredFootprint(win2_celltriggers, function(a, id)        
        if a.Owner == NOD then
            TRIGGER_WIN2()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)
	
	--Trigger.OnAllKilled(WIN2_ACTIVATE,  function() TRIGGER_WIN2() end)
    --Trigger.OnObjectiveCompleted(NOD,  function() TRIGGER_WIN2() end)
	-- ************************** WIN2 END **************************************************

	
	-- ************************** WIN START ************************************************
	Trigger.OnEnteredFootprint(win_celltriggers, function(a, id)        
        if a.Owner == NOD then
			TRIGGER_WIN()
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

	--Trigger.OnAllKilled(WIN_ACTIVATE,  function() TRIGGER_WIN() end)
    --Trigger.OnObjectiveCompleted(NOD,  function() TRIGGER_WIN() end)
	-- ************************** WIN END **************************************************


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
	WIN = NOD.AddPrimaryObjective("Steal the GDI Nuclear detonator")
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