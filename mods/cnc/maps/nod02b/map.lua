-- standard spawn point
SPAWNPOINT = { waypoint27.Location }
-- Cell triggers arrays



-- TEAMTYPES ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

-- Units
	GDI5_UNITS= {'e1', 'e1', 'e1', 'e1', 'e1'}
	GDI4_UNITS= {'e1', 'e1', 'e1'}
	GDI3_UNITS= {'e1', 'e1', 'e1'}
	GDI2_UNITS= {'e1', 'e1'}
	GDI1_UNITS= {'e1', 'e1'}


--Movements
	GDI5_MOVEMENT = function(unit)
	--1,Move:8,
	    unit.AttackMove(waypoint9.Location)	
	end

	GDI3_MOVEMENT = function(unit)
	--8,Move:0,Move:1,Move:4,Move:5,Move:6,Move:7,Move:9,Attack Base:30
	    unit.Move(waypoint0.Location)
		unit.Move(waypoint1.Location)
		unit.Move(waypoint4.Location)
		unit.Move(waypoint5.Location)
		unit.Move(waypoint6.Location)
		unit.Move(waypoint7.Location)
	    unit.AttackMove(waypoint9.Location)	
	end

	GDI2_MOVEMENT = function(unit)
	--1,Move:8,
	    unit.Move(waypoint8.Location)	
	end

	GDI1_MOVEMENT = function(unit)
	--5,Move:0,Move:1,Move:2,Move:3,Attack Units:30
		unit.Move(waypoint0.Location)
		unit.Move(waypoint1.Location)
		unit.Move(waypoint2.Location)
	    unit.AttackMove(waypoint3.Location)	
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

-- ************************** prd1 start ************************************************
	PRODUCTION = function(type)
		print("PRODUCTION called!")
		if Actor19.IsInWorld == true then
			Actor19.Produce(type, nil)
			print("Einheit erstellt!")
			Trigger.AfterDelay(DateTime.Seconds(50), function() PRODUCTION(type) end)
		end
	end
-- ************************** prd1 end ************************************************


-- ************************** grd2 start **********************************************
TRIGGER_GRD2_ACTIVE = false
TRIGGER_GRD2 = function()

    if TRIGGER_GRD2_ACTIVE == false then
    	TRIGGER_GRD2_ACTIVE = true
		print("trigger grd2 started")
    	MyActors = GETACTORS(5, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor grd2 losgelaufen: "..MyActors[key].Type)
            GDI5_MOVEMENT(MyActors[key])
        end 
	end
end

SET_GRD2_ACTIVE = function()
	TRIGGER_GRD2_ACTIVE = false
end
-- ************************** grd2 end ************************************************

-- ************************** ATK3 start **********************************************
TRIGGER_ATK3 = function()
		print("trigger atk3 started")
    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk3 losgelaufen: "..MyActors[key].Type)
            GDI1_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK3 end ************************************************

-- ************************** ATK4 start **********************************************
TRIGGER_ATK4 = function()
		print("trigger atk4 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk4 losgelaufen: "..MyActors[key].Type)
            GDI3_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK4 end ************************************************

-- ************************** ATK5 start **********************************************
TRIGGER_ATK5 = function()
		print("trigger atk5 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk5 losgelaufen: "..MyActors[key].Type)
            GDI3_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK5 end ************************************************

-- ************************** ATK6 start **********************************************
TRIGGER_ATK6 = function()
		print("trigger atk6 started")
    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk6 losgelaufen: "..MyActors[key].Type)
            GDI1_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK6 end ************************************************

-- ************************** ATK7 start **********************************************
TRIGGER_ATK7 = function()
		print("trigger atk7 started")
    	MyActors = GETACTORS(3, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk7 losgelaufen: "..MyActors[key].Type)
            GDI3_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK7 end ************************************************

-- ************************** ATK8 start **********************************************
TRIGGER_ATK8 = function()
		print("trigger atk8 started")
    	MyActors = GETACTORS(2, "Player (GoodGuy)", "e1")   

        for key in pairs(MyActors) do
			print("Actor atk8 losgelaufen: "..MyActors[key].Type)
            GDI1_MOVEMENT(MyActors[key])
        end 
end
-- ************************** ATK8 end ************************************************


WorldLoaded = function()
	BadGuy = Player.GetPlayer("GoodGuy")
	GoodGuy = Player.GetPlayer("BadGuy")

	GRD2_ACTIVATE = { Actor14, Actor15 }

	ATK5_ACTIVATE = { Actor27 }

	ATK4_ACTIVATE = { Actor36 }

	ATK3_ACTIVATE = { Actor40 }

	ATK6_ACTIVATE = { Actor38, Actor39 }


	-- ************************** prd1 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(10), function() PRODUCTION("e1") end)
	-- ************************** prd1 end **************************************************

	-- ************************** grd2 start ************************************************
	Trigger.OnDamaged(Actor14, TRIGGER_GRD2)
	Trigger.OnDamaged(Actor15, TRIGGER_GRD2)
	Trigger.AfterDelay(DateTime.Seconds(30), function() SET_GRD2_ACTIVE() end)
	-- ************************** grd2 end **************************************************

	-- ************************** atk3 start ************************************************
	Trigger.OnAllKilled(ATK3_ACTIVATE, function() TRIGGER_ATK3() end)
	-- ************************** atk3 end **************************************************

	-- ************************** atk4 start ************************************************
	Trigger.OnAllKilled(ATK4_ACTIVATE, function() TRIGGER_ATK4() end)
	-- ************************** atk4 end **************************************************

	-- ************************** atk5 start ************************************************
	--Discovered Trigger wird nicht umgesetzt, weil keine passende Function vorhanden ist
	--Trigger.*******(ATK5_ACTIVATE, function() TRIGGER_ATK5() end)
	-- ************************** atk5 end **************************************************

	-- ************************** atk6 start ************************************************
	Trigger.OnAllKilled(ATK6_ACTIVATE, function() TRIGGER_ATK6() end)
	-- ************************** atk6 end **************************************************

	-- ************************** atk7 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(80), function() TRIGGER_ATK7() end)
	-- ************************** atk7 end **************************************************

	-- ************************** atk8 start ************************************************
	Trigger.AfterDelay(DateTime.Seconds(85), function() TRIGGER_ATK8() end)
	-- ************************** atk8 end **************************************************



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


end

Tick = function()
	if GoodGuy.HasNoRequiredUnits()  then
		BadGuy.MarkCompletedObjective(BadGuyObjective)
	end

	if BadGuy.HasNoRequiredUnits() then
		GoodGuy.MarkCompletedObjective(GoodGuyObjective)
	end
end