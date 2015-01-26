-- standard spawn point
SPAWNPOINT = { waypoint27.Location }
-- Cell triggers arrays
WIN2_CELLTRIGGERS = {CPos.New(54,58), CPos.New(53,58), CPos.New(52,58), CPos.New(54,57), CPos.New(53,57), CPos.New(52,57), CPos.New(54,56), CPos.New(53,56), CPos.New(52,56), CPos.New(54,55), CPos.New(53,55), CPos.New(52,55)}
CHIN3_CELLTRIGGERS = {CPos.New(49,58), CPos.New(48,58), CPos.New(49,57), CPos.New(48,57), CPos.New(49,56), CPos.New(48,56), CPos.New(49,55), CPos.New(48,55)}
-- DZNE_CELLTRIGGERS = {CPos.New(61,45), CPos.New(60,45), CPos.New(59,45), CPos.New(58,45), CPos.New(57,45), CPos.New(61,44), CPos.New(60,44), CPos.New(59,44), CPos.New(58,44), CPos.New(57,44), CPos.New(61,43), CPos.New(60,43), CPos.New(58,43), CPos.New(57,43), CPos.New(61,42), CPos.New(60,42), CPos.New(59,42), CPos.New(58,42), CPos.New(57,42), CPos.New(61,41), CPos.New(60,41), CPos.New(59,41), CPos.New(58,41), CPos.New(57,41)}
WIN1_CELLTRIGGERS = {CPos.New(59,43)}

-- TEAMTYPES :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
-- a10s=GoodGuy,1,0,0,0,0,7,0,0,0,1,A10:1,0,0,0
A10S_UNITS ={"a10"}
-- nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
NOD1_UNITS = {"tran"}
-- gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:1,JEEP:1,7,Move:4,Move:10,Move:9,Move:11,Move:9,Move:10,Loop:0,0,0
GDI4_UNITS = {"e1", "e1", "e2", "jeep"}
-- gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,2,E1:5,TRAN:1,3,Move:5,Unload:5,Attack Units:20,0,0
GDI1_UNITS = {"e1", "e1", "e1", "e1", "e1", "tran"}
-- gdi2=GoodGuy,1,0,0,0,0,7,0,0,0,2,E2:5,TRAN:1,3,Move:6,Unload:6,Attack Units:20,0,0
GDI2_UNITS = {"e2", "e2", "e2", "e2", "e2", "tran"}
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:2,12,Move:1,Guard:1,Move:3,Guard:1,Move:7,Guard:1,Move:8,Guard:1,Move:9,Guard:1,Move:10,Loop:1,0,0
GDI3_UNIT = {"mtnk", "mtnk"}
-- gdi5=GoodGuy,0,0,1,0,0,7,0,0,0,2,E1:3,E2:3,4,Move:0,Move:1,Move:4,Attack Units:30,0,0
GDI5_UNITS = {"e1", "e1", "e1", "e2", "e2", "e2"}
-- gdi6=GoodGuy,0,0,1,0,0,7,0,0,0,2,MTNK:1,JEEP:1,3,Move:2,Move:3,Attack Units:30,0,0
GDI6_UNITS = {"mtnk", "jeep"}

-- nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
NOD1_MOVEMENT = function(unit)
	unit.Move(waypoint17.Location)
end
-- gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:1,JEEP:1,7,Move:4,Move:10,Move:9,Move:11,Move:9,Move:10,Loop:0,0,0
GDI4_MOVEMENT = function(unit)
	unit.Move(waypoint9.Location)
	unit.Move(waypoint11.Location)
	unit.Move(waypoint9.Location)
	unit.Move(waypoint10.Location)
	-- Loop doesn´t Work!
end
-- gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,2,E1:5,TRAN:1,3,Move:5,Unload:5,Attack Units:20,0,0
GDI1_MOVMENT = function(unit)
	unit.Move(waypoint5.Location)
	unit.AttackMove(waypoint5.Location)
	-- Can´t use Unload
end
-- gdi2=GoodGuy,1,0,0,0,0,7,0,0,0,2,E2:5,TRAN:1,3,Move:6,Unload:6,Attack Units:20,0,0
GDI2_MOVEMENT = function(unit)
	unit.Move(waypoint6.Location)
	-- Can´t use Unload
	unit.AttackMove(waypoint6.Location)
end
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:2,12,Move:1,Guard:1,Move:3,Guard:1,Move:7,Guard:1,Move:8,Guard:1,Move:9,Guard:1,Move:10,Loop:1,0,0
GDI3_MOVEMENT = function(unit)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint7.Location)
	unit.Move(waypoint8.Location)
	unit.Move(waypoint9.Location)
	unit.Move(waypoint10.Location)
	-- Loop doesn´t Work
end
-- gdi5=GoodGuy,0,0,1,0,0,7,0,0,0,2,E1:3,E2:3,4,Move:0,Move:1,Move:4,Attack Units:30,0,0
GDI5_MOVEMENT = function(unit)
	unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint4.Location)
	unit.AttackMove(waypoint4.Location)
end
-- gdi6=GoodGuy,0,0,1,0,0,7,0,0,0,2,MTNK:1,JEEP:1,3,Move:2,Move:3,Attack Units:30,0,0
GDI6_MOVEMENT = function(unit)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.AttackMove(waypoint3.Location)
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
--prod=Time,Production,3,GoodGuy,None,0 - Copy FROM NOD 6A
-- ************************** PROD START ************************************************
--	    PRODUCTION = function(type)
--		 print("PRODUCTION called!")
--		 if Actor110.IsInWorld == true then
--		  Actor110.Produce(type, nil)
--			 print("Einheit erstellt!")
--			 Trigger.AfterDelay(DateTime.Seconds(50), function() PRODUCTION(type) end)
--		end
--	end
-- ************************** PROD END **************************************************
-- TRIGGERS :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

-- atk3=Attacked,Reinforce.,0,None,a10s,0 - Airstrike --> Unit A10 --> not implemented
-- -- ************************** ATK3 START ************************************************
	--ATK3_SWITCH = true
	--TRIGGER_ATK3 = function()
		--if ATK3_SWITCH == true then
			--Reinforcements.Reinforce(A10,A10S_UNITS, SPAWNPOINT, 15, function(unit)
			--end)
			--ATK3_SWITCH= false
		--end
	--end
-- -- ************************** ATK3 END ************************************************
-- chn3=Player Enters,Reinforce.,0,BadGuy,nod1,0
-- nod1=BadGuy,1,0,0,0,0,7,0,0,0,1,TRAN:1,1,Move:17,0,0
-- -- ************************** CHIN3 START ************************************************
TRIGGER_CHIN3 = function()
     Reinforcements.Reinforce(NOD, NOD1_UNITS, SPAWNPOINT, 7, function(unit)        
        unit.Move(waypoint17.Location)
     end)
end
-- -- ************************** CHIN3 END ************************************************
-- dzne=Player Enters,DZ at 'Z',0,BadGuy,None,0
-- -- ************************** DZNE START ************************************************
	-- TRIGGER_DZNE = function()
		-- N.I.
		-- end
	-- end
-- -- ************************** DZNE END *************************************************
-- grd2=Discovered,Create Team,0,None,gdi4,0 --> Discovered --> not implemented
-- gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,3,E1:2,E2:1,JEEP:1,7,Move:4,Move:10,Move:9,Move:11,Move:9,Move:10,Loop:0,0,0
-- -- ************************** GRD2 START ***********************************************
	   -- TRIGGER_GRD2_ACTIVE = false
	   --TRIGGER_GRD2 = function()
		--if TRIGGER_GRD2_ACTIVE == false then
			--TRIGGER_GRD2_ACTIVE = true
			--	print("Trigger GRD2 started")
			--	MyActors = GETACTORS(2, "Player (GDI)", "e1")
			--	MyActors = GETACTORS(1, "Player (GDI)", "e2")
			--	MyActors = GETACTORS(1, "Player (GDI)", "jeep")
			--	for key in pairs(MyActors) do
			--	print("Actor GRD2 losgelaufen: "..MyActors[key].Type)
			--	GDI4_MOVEMENT(MyActors[key])
		    --end
	    --end
	--end
--TRIGGER_GRD1_TIME = 30
--SET_GRD2_ACTIVE = function()
		--TRIGGER_GRD2_ACTIVE = false
--end	
-- -- ************************** GRD2 END *************************************************
-- gdi=All Destr.,Win,0,GoodGuy,None,0
-- -- ************************** GDI START ************************************************
	-- TRIGGER_GDI = function()
			-- S.U.
		-- end
	-- end
-- -- ************************** GDI END ************************************************
-- nod=All Destr.,Lose,0,BadGuy,None,0
-- -- ************************** NOD START ************************************************
	-- TRIGGER_NOD = function()
			-- S.U.
		-- end
	-- end
-- -- ************************** NOD END ************************************************
-- chn1=Destroyed,Reinforce.,0,None,gdi1,0
-- gdi1=GoodGuy,1,0,0,0,0,7,0,0,0,2,E1:5,TRAN:1,3,Move:5,Unload:5,Attack Units:20,0,0
-- -- ************************** CHIN1 START ************************************************
 --TRIGGER_CHIN1 = function()
--end
-- -- ************************** CHIN1 END ************************************************
-- chn2=Destroyed,Reinforce.,0,None,gdi2,0
-- gdi2=GoodGuy,1,0,0,0,0,7,0,0,0,2,E2:5,TRAN:1,3,Move:6,Unload:6,Attack Units:20,0,0
-- -- ************************** CHIN2 START ***********************************************
--TRIGGER_CHIN2 = function()
--end
-- -- ************************** CHIN2 END ************************************************
-- grd1=Time,Create Team,3,GoodGuy,gdi3,0
-- gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:2,12,Move:1,Guard:1,Move:3,Guard:1,Move:7,Guard:1,Move:8,Guard:1,Move:9,Guard:1,Move:10,Loop:1,0,0
	   TRIGGER_GRD1_ACTIVE = false
	   TRIGGER_GRD1 = function()
		if TRIGGER_GRD1_ACTIVE == false then
			TRIGGER_GRD1_ACTIVE = true
				print("Trigger GRD1 started")
				MyActors = GETACTORS(2, "Player (GoodGuy)", "mtnk")
				for key in pairs(MyActors) do
				print("Actor GRD1 losgelaufen: "..MyActors[key].Type)
				GDI3_MOVEMENT(MyActors[key])
		    end
	    end
	end
--TRIGGER_GRD1_TIME = 30
SET_GRD1_ACTIVE = function()
		TRIGGER_GRD1_ACTIVE = false
end	
-- -- ************************** GRD1 END ************************************************

-- -- ************************** GRD1 END ************************************************
-- atk1=Attacked,Create Team,0,None,gdi5,0
-- gdi5=GoodGuy,0,0,1,0,0,7,0,0,0,2,E1:3,E2:3,4,Move:0,Move:1,Move:4,Attack Units:30,0,0
-- -- ************************** ATK1 START ************************************************
	     TRIGGER_ATK1_ACTIVE = false
	   TRIGGER_ATK1 = function()
		if TRIGGER_ATK1_ACTIVE == false then
			TRIGGER_ATK1_ACTIVE = true
				print("Trigger ATK1 started")
				MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")
			for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GDI5_MOVEMENT(MyActors[key])
		    end	
				MyActors = GETACTORS(3, "Player (GoodGuy)", "e2")
				for key in pairs(MyActors) do
				print("Actor ATK1 losgelaufen: "..MyActors[key].Type)
				GDI5_MOVEMENT(MyActors[key])
		    end
	    end
	end
--TRIGGER_ATK1_TIME = 30
SET_ATK1_ACTIVE = function()
		TRIGGER_ATK1_ACTIVE = false
end	
-- -- ************************** ATK1 END ************************************************
-- atk2=Attacked,Create Team,0,None,gdi6,0
-- gdi6=GoodGuy,0,0,1,0,0,7,0,0,0,2,MTNK:1,JEEP:1,3,Move:2,Move:3,Attack Units:30,0,0
-- -- ************************** ATK2 START ************************************************
	     TRIGGER_ATK2_ACTIVE = false
	   TRIGGER_ATK2 = function()
		if TRIGGER_ATK2_ACTIVE == false then
			TRIGGER_ATK2_ACTIVE = true
				print("Trigger ATK1 started")
				MyActors = GETACTORS(1, "Player (GoodGuy)", "mtnk")
			for key in pairs(MyActors) do
				print("Actor AKT2 losgelaufen: "..MyActors[key].Type)
				GDI6_MOVEMENT(MyActors[key])
		    end
				MyActors = GETACTORS(1, "Player (GoodGuy)", "jeep")
			for key in pairs(MyActors) do
				print("Actor AKT2 losgelaufen: "..MyActors[key].Type)
				GDI6_MOVEMENT(MyActors[key])
		    end
	    end
	end
--TRIGGER_ATK2_TIME = 30
SET_ATK2_ACTIVE = function()
		TRIGGER_ATK2_ACTIVE = false
end	
-- -- ************************** ATK1 END ************************************************
--win1=Player Enters,Allow Win,0,BadGuy,None,0 (-- win=Player Enters,Allow Win,0,BadGuy,None,0)
-- -- ************************** WIN1 START ************************************************
	 TRIGGER_WIN1 = function()
		 NOD.MarkCompletedObjective(WIN1)
  	end
-- -- ************************** WIN1 END **************************************************
-- lose=All Destr.,Lose,0,BadGuy,None,0
-- -- ************************** LOSE START ************************************************
	-- TRIGGER_LOSE = function()
			-- S.U.
		-- end
	-- end
-- -- ************************** LOSE END **************************************************
-- win2=Player Enters,Win,0,BadGuy,None,0
-- -- ************************** WIN2 START ************************************************
	 TRIGGER_WIN2 = function()
		NOD.MarkCompletedObjective(WIN2)
	end
	--WIN2_SWITCH = false
-- -- ************************** WIN2 END **************************************************
WorldLoaded = function()
	GDI = Player.GetPlayer("GoodGuy")
	NOD = Player.GetPlayer("BadGuy")
	
	ATK1_ACTIVATE = { Actor116, Actor145 }
	ATK2_ACTIVATE = { Actor115, Actor161 }
	ATK3_ACTIVATE = { Actor98 }
	
	WIN1_ACTIVATE = { Actor13, Actor14, Actor15, Actor16, Actor17, Actor18, Actor19  }
	WIN2_ACTIVATE = { Actor61 }
	
	GRD2_ACTIVATE = { Actor170, Actor171, Actor172 }
	
	CHN1_ACTIVATE = { Actor105, Actor107, Actor109, Actor110, Actor112 }
	CHN2_ACTIVATE = { Actor104, Actor106, Actor108 }

	
	
-- ************************** ATK1 START ************************************************
	Trigger.OnDamaged(Actor116, function() TRIGGER_ATK1() end)
	Trigger.OnDamaged(Actor145, function() TRIGGER_ATK1() end)
-- ************************** ATK1 END **************************************************
-- ************************** ATK2 START ************************************************
	Trigger.OnDamaged(Actor115, function() TRIGGER_ATK2() end)
	Trigger.OnDamaged(Actor161, function() TRIGGER_ATK2() end)
-- ************************** ATK2 END **************************************************

-- ************************** ATK3 START ************************************************
	--Trigger.OnAllKilled(ATK3_ACTIVATE, function() TRIGGER_ATK3() end)
-- ************************** ATK3 END **************************************************

-- -- ************************** CHIN1 START ************************************************
-- -- CHN1_ACTIVATE = { Actor105, Actor107, Actor109, Actor110, Actor112 }
CHN1_TeamPath = {SPAWNPOINT.Location, waypoint1.Location}
	--CHN1_UNITS = { "e1", "e1", "e1", "e1", "e1", "tran" }
	Trigger.OnAnyKilled(CHN1_ACTIVATE, function()
		Reinforcements.Reinforce(GDI,GDI1_UNITS, CHN1_TeamPath, 15, function(unit)
			unit.Move(waypoint5.Location)
			-- Move Unload is not found
			unit.AttackMove(waypoint5.Location)
		end)
	end)
-- -- ************************** CHIN1 END ************************************************

-- -- ************************** CHIN2 START ************************************************
-- -- CHN2_ACTIVATE = { Actor104, Actor106, Actor108 }
	CHN2_TeamPath = {SPAWNPOINT.Location, waypoint1.Location}
	--CHN2_UNITS = { "e2", "e2", "e2", "e2", "e2", "tran" }
	Trigger.OnAnyKilled(CHN2_ACTIVATE, function()
		Reinforcements.Reinforce(GDI,GDI2_UNITS, CHN2_TeamPath, 15, function(unit)
			unit.Move(waypoint6.Location)
			-- Move Unload is not found
			unit.AttackMove(waypoint6.Location)
		end)
	end)
-- -- ************************** CHIN2 END ************************************************
-- ************************** CHIN3 START ************************************************
   Trigger.OnEnteredFootprint(CHIN3_CELLTRIGGERS, function(a, id)
     if a.Owner == NOD then
        TRIGGER_CHIN3()
	 end
        Trigger.RemoveFootprintTrigger(id)
	end)
-- ************************** CHIN3 END **************************************************
-- ************************** GRD2 END **************************************************
-- GRD2_ACTIVATE = { Actor170, Actor171, Actor172 }
--Trigger.OnDamaged(Actor170, TRIGGER_GRD2)
--Trigger.OnDamaged(Actor171, TRIGGER_GRD2)
--Trigger.OnDamaged(Actor172, TRIGGER_GRD2)
-- ************************** GRD2 END **************************************************

-- ************************** GRD1 END **************************************************
 Trigger.AfterDelay(DateTime.Seconds(3), function() TRIGGER_GRD1() end)
-- ************************** GRD1 END **************************************************

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
	WIN1 = NOD.AddPrimaryObjective("First: Steal the GDI Nuclear detonator")
	WIN2 = NOD.AddPrimaryObjective("Second: Move to the transportplace")
	--GDIObjective = GDI.AddPrimaryObjective("Kill all enemies!")
	GDIObjective = GDI.AddPrimaryObjective("Stop the NOD taskforce from escaping with the detonator")
end
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