-- standard spawn point
SPAWNPOINT = { waypoint27.Location }
-- Cell triggers arrays
DESX_CELLTRIGGERS = {CPos.New(58,60), CPos.New(57,60), CPos.New(58,59), CPos.New(57,59), CPos.New(58,58), CPos.New(57,58), CPos.New(58,57), CPos.New(57,57), CPos.New(58,56), CPos.New(57,56), CPos.New(58,55), CPos.New(57,55), CPos.New(58,54), CPos.New(57,54), CPos.New(58,53), CPos.New(57,53), CPos.New(58,52), CPos.New(57,52), CPos.New(58,51), CPos.New(57,51)}
DELX_CELLTRIGGERS = {CPos.New(56,60), CPos.New(55,60), CPos.New(56,59), CPos.New(55,59), CPos.New(56,58), CPos.New(55,58), CPos.New(56,57), CPos.New(55,57), CPos.New(56,56), CPos.New(55,56), CPos.New(56,55), CPos.New(55,55), CPos.New(56,54), CPos.New(55,54), CPos.New(56,53), CPos.New(55,53), CPos.New(56,52), CPos.New(55,52), CPos.New(56,51)}
DZNE_CELLTRIGGERS = {CPos.New(54,60), CPos.New(53,60), CPos.New(54,59), CPos.New(53,59), CPos.New(54,58), CPos.New(53,58), CPos.New(54,57), CPos.New(53,57), CPos.New(54,56), CPos.New(53,56), CPos.New(54,55), CPos.New(53,55), CPos.New(54,54), CPos.New(53,54), CPos.New(54,53), CPos.New(53,53), CPos.New(54,52), CPos.New(53,52)}
PROD_CELLTRIGGERS = {CPos.New(24,60), CPos.New(23,60), CPos.New(24,59), CPos.New(23,59), CPos.New(24,58), CPos.New(23,58), CPos.New(24,57), CPos.New(23,57), CPos.New(24,56), CPos.New(23,56), CPos.New(24,55), CPos.New(23,55), CPos.New(24,54), CPos.New(23,54), CPos.New(24,53), CPos.New(23,53), CPos.New(24,52), CPos.New(23,52), CPos.New(22,52), CPos.New(21,52), CPos.New(24,51), CPos.New(23,51), CPos.New(22,51), CPos.New(21,51), CPos.New(20,51)}

--UNITS and Activators
HUNT_ACTIVATE = { Actor73 }
DELY_ACTIVATE = { Actor79 }
YYYY_UNITS = { "a10" }

-- Triggers Times
TRIGGER_ATK1_TIME = 80
TRIGGER_ATK2_TIME = 90
TRIGGER_ATK3_TIME = 120
TRIGGER_ATK4_TIME = 165
TRIGGER_ATK5_TIME = 175

--Movements
GDI1_MOVEMENT = function(unit)
--7,Move:0,Move:1,Move:2,Move:3,Move:4,Move:5,Attack Units:30
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint4.Location)
    unit.AttackMove(waypoint5.Location)	
end

GDI2_MOVEMENT = function(unit)
--7,Move:0,Move:1,Move:2,Move:3,Move:6,Move:10,Attack Base:30
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint6.Location)
    unit.AttackMove(waypoint10.Location)	
end

GDI3_MOVEMENT = function(unit)
--10,Move:0,Move:1,Move:2,Move:3,Move:6,Move:7,Move:8,Move:9,Move:11,Attack Units:40
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint6.Location)
	unit.Move(waypoint7.Location)
	unit.Move(waypoint8.Location)
	unit.Move(waypoint9.Location)
    unit.AttackMove(waypoint11.Location)	
end

GDI4_MOVEMENT = function(unit)
--7,Move:0,Move:1,Move:2,Move:3,Move:6,Move:10,Attack Units:40
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint6.Location)
    unit.AttackMove(waypoint10.Location)	
end

GDI5_MOVEMENT = function(unit)
--7,Move:0,Move:1,Move:2,Move:3,Move:4,Move:5,Attack Base:40
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
	unit.Move(waypoint3.Location)
	unit.Move(waypoint4.Location)
    unit.AttackMove(waypoint5.Location)	
end


-- ************************** GETACTOR Start  - (Function for Create Team) **************
-- returns a table with filtered actors whoe are alive and from the specified type and owner
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
-- ************************** GETACTOR End ********************************************

-- ************************** ATK1 start **********************************************
TRIGGER_ATK1 = function()
		print("trigger atk1 started")
    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e1") 
    	for key in pairs(MyActors) do
            GDI1_MOVEMENT(MyActors[key])
        end

    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e2")   
        for key in pairs(MyActors) do
            GDI1_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK1 end ************************************************

-- ************************** ATK2 start **********************************************
TRIGGER_ATK2 = function()
		print("trigger atk2 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e2") 
    	for key in pairs(MyActors) do
            GDI2_MOVEMENT(MyActors[key])
        end

    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e3")   
        for key in pairs(MyActors) do
            GDI2_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK2 end ************************************************

-- ************************** ATK3 start **********************************************
TRIGGER_ATK3 = function()
		print("trigger atk3 started")
    	MyActors = GETACTORS(1, "Player (GoodGuy)", "e1") 
    	for key in pairs(MyActors) do
            GDI3_MOVEMENT(MyActors[key])
        end

    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e3")   
        for key in pairs(MyActors) do
            GDI3_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK3 end ************************************************

-- ************************** ATK4 start **********************************************
TRIGGER_ATK4 = function()
		print("trigger atk4 started")
    	MyActors = GETACTORS(2, "Player (GoodGuy)", "jeep") 
    	for key in pairs(MyActors) do
            GDI4_MOVEMENT(MyActors[key])
        end
end
-- ************************** ATK4 end ************************************************

-- ************************** ATK5 start **********************************************
TRIGGER_ATK5 = function()
		print("trigger atk5 started")
    	MyActors = GETACTORS(1, "Player (GoodGuy)", "tank") 
    	for key in pairs(MyActors) do
            GDI5_MOVEMENT(MyActors[key])
        end
end
-- ************************** ATK5 end ************************************************

-- ************************** PROD start ************************************************
	PRODUCTION = function(type)
		print("PRODUCTION called!")
		if Actor75.IsInWorld == true then
			Actor75.Produce(type, nil)
			print("Einheit erstellt!")
		end
	end
-- ************************** PROD end ************************************************

-- ************************** HUNT Start **********************************************
HUNT = function()	
	print("HUNTmode")
	local list = BadGuy.GetGroundAttackers()
	for isx, val in pairs(list) do        
		val.Hunt()
   end
end
-- ************************** HUNT End ***********************************************

-- ************************** SCAN start (scan for Actor 73)**************************
SCAN = function()
	if  HUNT_ACTIVATE.IsInWorld == false then
		HUNT()
	end
	if  DELY_ACTIVATE.IsInWorld == false then

			if a.Owner == GoodGuy then
				Trigger.RemoveFootprintTrigger(id)
				TRIGGER_YYYY = function() end
			end
	end
	print("SCAN started")	
	Trigger.AfterDelay(DateTime.Seconds(15), function() SCAN() end)
end
-- ************************** SCAN start ************************************************


WorldLoaded = function()
	BadGuy = Player.GetPlayer("GoodGuy")
	GoodGuy = Player.GetPlayer("BadGuy")


	-- ************************** atk1 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK1_TIME), function() TRIGGER_ATK1() end)
	-- ************************** atk1 end **************************************************

	-- ************************** atk2 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK2_TIME), function() TRIGGER_ATK2() end)
	-- ************************** atk2 end **************************************************

	-- ************************** atk3 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK3_TIME), function() TRIGGER_ATK3() end)
	-- ************************** atk3 end **************************************************

	-- ************************** atk4 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK4_TIME), function() TRIGGER_ATK4() end)
	-- ************************** atk4 end **************************************************

	-- ************************** atk5 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK5_TIME), function() TRIGGER_ATK5() end)
	-- ************************** atk5 end **************************************************

	-- ************************** PROD start ************************************************
	Trigger.OnEnteredFootprint(PROD_CELLTRIGGERS, function() PRODUCTION("e1") end)
	-- ************************** PROD end **************************************************

	-- *********************************************** hunt start ***************************
	Trigger.AfterDelay(DateTime.Seconds(60), function() SCAN() end)	
	-- *********************************************** hunt end *****************************


	Trigger.OnObjectiveAdded(GoodGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(GoodGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(GoodGuy, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(GoodGuy, function()
		Media.PlaySpeechNotification(GoodGuy, "Win")
	end)

	Trigger.OnPlayerLost(GoodGuy, function()
		Media.PlaySpeechNotification(GoodGuy, "Lose")
	end)

	BadGuyObjective = BadGuy.AddPrimaryObjective("Kill all enemies!")
	GoodGuyObjective = GoodGuy.AddPrimaryObjective("Build up a infantry and capture the GDI Base!")


	TRIGGER_YYYY_TIME = 140
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_YYYY_TIME), function() TRIGGER_YYYY() end)

	TRIGGER_YYYY = function()
		-- Reinforcements.Reinforce(BadGuy, YYYY_UNITS, SPAWNPOINT, 15, function(unit)
		-- end)
	end

	Trigger.OnEnteredFootprint(DESX_CELLTRIGGERS, function(a, id)
		if a.Owner == GoodGuy then
			Trigger.RemoveFootprintTrigger(id)
			TRIGGER_XXXX = function() end
		end
	end)

	XXXX_ACTIVATE = { Actor125, Actor128, Actor126, Actor127, Actor126, Actor127, Actor125, Actor128, Actor129, Actor130, Actor131, Actor132, Actor131, Actor132, Actor133, Actor134, Actor133, Actor134 }

	Trigger.OnEnteredFootprint(DELX_CELLTRIGGERS, function(a, id)
		if a.Owner == GoodGuy then
			Trigger.RemoveFootprintTrigger(id)
			TRIGGER_XXXX = function() end
		end
	end)

	CHN1_ACTIVATE = { Actor124, Actor135, Actor136, Actor89 }
	CHN1_TeamPath = {SPAWNPOINT.Location, waypoint1.Location}
	CHN1_UNITS = { "tran" }
	Trigger.OnAnyKilled(CHN1_ACTIVATE, function()
		Reinforcements.Reinforce(GoodGuy,CHN1_UNITS, CHN1_TeamPath, 15, function(unit)
			unit.Move(waypoint15.Location)
		end)
	end)
end

Tick = function()
	if GoodGuy.HasNoRequiredUnits()  then
		BadGuy.MarkCompletedObjective(BadGuyObjective)
	end

	if BadGuy.HasNoRequiredUnits() then
		GoodGuy.MarkCompletedObjective(GoodGuyObjective)
	end
end