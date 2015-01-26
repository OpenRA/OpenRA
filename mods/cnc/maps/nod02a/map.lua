-- standard spawn point
SPAWNPOINT = { waypoint27.Location }

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

-- ************************** HUNT Start  - (Function for Create Team) **************
HUNT = function()	
	print("HUNTmode")
	local list = BadGuy.GetGroundAttackers()
	for isx, val in pairs(list) do        
		val.Hunt()
   end
end
-- ************************** HUNT End ********************************************

-- ************************** ATK1 start **********************************************
--teamtype atk1
ATK1_UNITS= {'e1', 'e1', 'e1', 'e1', 'e1'}
ATK1_CELLTRIGGERS = {CPos.New(45,37), CPos.New(44,37), CPos.New(45,36), CPos.New(44,36), CPos.New(45,35), CPos.New(44,35), CPos.New(45,34), CPos.New(44,34)}
ATK1_MOVEMENT = function(unit)
    -- 5,Move:2,Move:4,Move:5,Move:6,Attack Units:50
    unit.Move(waypoint2.Location)
	unit.Move(waypoint4.Location)
	unit.Move(waypoint5.Location)
    unit.AttackMove(waypoint6.Location)	
end
-- ************************** ATK1 end ************************************************

-- ************************** ATK2 start **********************************************
ATK2_MOVEMENT = function(unit)
    -- 5,Move:2,Move:5,Move:7,Move:6,Attack Base:50
    unit.Move(waypoint2.Location)
	unit.Move(waypoint5.Location)
	unit.Move(waypoint7.Location)
    unit.AttackMove(waypoint6.Location)	
end

TRIGGER_ATK2 = function()
		print("trigger atk2 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk2 losgelaufen: "..MyActors[key].Type)
            ATK2_MOVEMENT(MyActors[key])
        end 
end

TRIGGER_ATK2_TIME = 40
-- ************************** ATK2 end ************************************************

-- ************************** ATK3 start **********************************************
ATK3_MOVEMENT = function(unit)
    -- 5,Move:2,Move:7,Move:8,Move:9,Attack Units
    unit.Move(waypoint2.Location)
	unit.Move(waypoint7.Location)
	unit.Move(waypoint8.Location)
    unit.AttackMove(waypoint9.Location)	
end

TRIGGER_ATK3_ACTIVE = false
TRIGGER_ATK3 = function()

    if TRIGGER_ATK3_ACTIVE == false then
    	TRIGGER_ATK3_ACTIVE = true
		print("trigger atk3 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk3 losgelaufen: "..MyActors[key].Type)
            ATK3_MOVEMENT(MyActors[key])
        end 
	end
end

SET_ATK3_ACTIVE = function()
	TRIGGER_ATK3_ACTIVE = false
end
-- ************************** ATK3 end ************************************************

-- ************************** ATK4 start **********************************************
--teamtype atk4
ATK4_MOVEMENT = function(unit)
    -- 4,Move:0,Move:8,Move:9,Attack Units:40
    unit.Move(waypoint0.Location)
	unit.Move(waypoint8.Location)
    unit.AttackMove(waypoint9.Location)	
end
ATK4_UNITS= {'e1', 'e1', 'e1'}
ATK4_CELLTRIGGERS = {CPos.New(50,47), CPos.New(49,47), CPos.New(48,47), CPos.New(47,47), CPos.New(46,47), CPos.New(45,47), CPos.New(44,47), CPos.New(43,47), CPos.New(42,47), CPos.New(41,47), CPos.New(40,47), CPos.New(39,47), CPos.New(38,47), CPos.New(37,47), CPos.New(50,46), CPos.New(49,46), CPos.New(48,46), CPos.New(47,46), CPos.New(46,46), CPos.New(45,46), CPos.New(44,46), CPos.New(43,46), CPos.New(42,46), CPos.New(41,46), CPos.New(40,46), CPos.New(39,46), CPos.New(38,46)}
ATK4_MOVEMENT = function(unit)
    -- 4,Move:0,Move:8,Move:9,Attack Units:40
    unit.Move(waypoint0.Location)
	unit.Move(waypoint8.Location)
    unit.AttackMove(waypoint9.Location)	
end
-- ************************** ATK4 end ************************************************

-- ************************** ATK5 start **********************************************
TRIGGER_ATK5 = function()
		print("trigger atk5 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk5 losgelaufen: "..MyActors[key].Type)
            ATK2_MOVEMENT(MyActors[key])
        end 
end

TRIGGER_ATK5_TIME = 75
-- ************************** ATK5 end ************************************************

-- ************************** ATK6 start **********************************************
TRIGGER_ATK6 = function()
		print("trigger atk6 started")
    	MyActors = GETACTORS(4, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk6 losgelaufen: "..MyActors[key].Type)
            ATK3_MOVEMENT(MyActors[key])
        end 
end

TRIGGER_ATK6_TIME = 80
-- ************************** ATK6 end ************************************************

-- ************************** ATK7 start **********************************************
TRIGGER_ATK7 = function()
		print("trigger atk7 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk7 losgelaufen: "..MyActors[key].Type)
            ATK4_MOVEMENT(MyActors[key])
        end 
end

TRIGGER_ATK7_TIME = 80
-- ************************** ATK7 end ************************************************

-- ************************** PAT1 start **********************************************
PAT1_MOVEMENT = function(unit)
    -- 9,Move:0,Guard:2,Move:1,Guard:1,Move:2,Guard:2,Move:3,Guard:1
    unit.Move(waypoint0.Location)
	unit.Move(waypoint1.Location)
	unit.Move(waypoint2.Location)
    unit.Move(waypoint3.Location)	
end

TRIGGER_PAT1 = function()
		print("trigger pat1 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor pat1 losgelaufen: "..MyActors[key].Type)
            PAT1_MOVEMENT(MyActors[key])
        end 
end

TRIGGER_PAT1_TIME = 30
-- ************************** PAT1 end ************************************************


-- ************************** prod start ************************************************
PRODUCTION = function(type)
	print("PRODUCTION called!")
	if Actor6.IsInWorld == true then
		Actor6.Produce(type, nil)
		print("Einheit erstellt!")
		Trigger.AfterDelay(DateTime.Seconds(30), function() PRODUCTION(type) end)
	end
end
-- ************************** prod end ************************************************


-- ************************** SCAN start ************************************************
SCAN = function()
	if  Actor4.IsInWorld == false and
		Actor5.IsInWorld == false and
		Actor6.IsInWorld == false and
		Actor7.IsInWorld == false and
		Actor8.IsInWorld == false and
		Actor9.IsInWorld == false then
		HUNT()
	end
	print("SCAN started")	
	Trigger.AfterDelay(DateTime.Seconds(15), function() SCAN() end)
end
-- ************************** SCAN start ************************************************

WorldLoaded = function()
	BadGuy = Player.GetPlayer("GoodGuy")
	GoodGuy = Player.GetPlayer("BadGuy")

	-- *********************************************** atk1 start ***************************************************************
    -- trigger atk1
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(ATK1_CELLTRIGGERS, function(a, id)
	print("trigger atk1 started")
        if a.Owner == GoodGuy then
            MyActors = GETACTORS(5, "Player (GoodGuy)", "e1")           
            for key,value in pairs(MyActors) do
				print("Actor atk1 losgelaufen: "..MyActors[key].Type)
                ATK1_MOVEMENT(MyActors[key])
            end 
            -- delete this trigger 
            Trigger.RemoveFootprintTrigger(id)

        end
    end)
    -- *********************************************** atk1 end ***************************************************************	

	-- *********************************************** atk2 start *************************************************************
	-- atk2 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK2_TIME), function() TRIGGER_ATK2() end)
	-- *********************************************** atk2 end ***************************************************************

	-- *********************************************** atk3 start *************************************************************
	Trigger.OnDamaged(Actor4, TRIGGER_ATK3)
	Trigger.OnDamaged(Actor5, TRIGGER_ATK3)
	Trigger.OnDamaged(Actor6, TRIGGER_ATK3)
	Trigger.OnDamaged(Actor7, TRIGGER_ATK3)
	Trigger.OnDamaged(Actor8, TRIGGER_ATK3)
	Trigger.OnDamaged(Actor9, TRIGGER_ATK3)
	Trigger.AfterDelay(DateTime.Seconds(30), function() SET_ATK3_ACTIVE() end)
	-- *********************************************** atk3 end ***************************************************************

	-- *********************************************** atk4 start ***************************************************************
    -- trigger atk4
    -- on cell enters to do something
    Trigger.OnEnteredFootprint(ATK4_CELLTRIGGERS, function(a, id)
	print("trigger atk4 started")
        if a.Owner == GoodGuy then
            MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")           
            for key,value in pairs(MyActors) do
				print("Actor atk4 losgelaufen: "..MyActors[key].Type)
                ATK2_MOVEMENT(MyActors[key])
            end 
            -- delete this trigger 
            Trigger.RemoveFootprintTrigger(id)

        end
    end)
    -- *********************************************** atk4 end ***************************************************************

    -- *********************************************** atk5 start *************************************************************
	-- atk2 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK5_TIME), function() TRIGGER_ATK5() end)
	-- *********************************************** atk5 end ***************************************************************

    -- *********************************************** atk6 start *************************************************************
	-- atk2 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK6_TIME), function() TRIGGER_ATK6() end)
	-- *********************************************** atk6 end ***************************************************************

	-- *********************************************** atk7 start *************************************************************
	-- atk2 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_ATK7_TIME), function() TRIGGER_ATK7() end)
	-- *********************************************** atk7 end ***************************************************************

	-- *********************************************** atk7 start *************************************************************
	-- pat1 start after delay
    Trigger.AfterDelay(DateTime.Seconds(TRIGGER_PAT1_TIME), function() TRIGGER_PAT1() end)
	-- *********************************************** atk7 end ***************************************************************

	-- *********************************************** hunt start *************************************************************	
		Trigger.AfterDelay(DateTime.Seconds(60), function() SCAN() end)	
	-- *********************************************** hunt end ***************************************************************


	-- *********************************************** prod start *************************************************************
	
		Trigger.AfterDelay(DateTime.Seconds(60), function() PRODUCTION("e1") end)
	
	-- *********************************************** prod end ***************************************************************
	
	


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
	GoodGuyObjective = GoodGuy.AddPrimaryObjective("Build a NOD Base and Detroy the GDI!")

	DFND_ACTIVATE = { Actor5, Actor6, Actor7, Actor8 }

	ATK3_ACTIVATE = { Actor20, Actor21, Actor22, Actor23, Actor24, Actor33, Actor34 }
end

Tick = function()
	if GoodGuy.HasNoRequiredUnits()  then
		BadGuy.MarkCompletedObjective(BadGuyObjective)
	end

	if BadGuy.HasNoRequiredUnits() then
		GoodGuy.MarkCompletedObjective(GoodGuyObjective)
	end
end