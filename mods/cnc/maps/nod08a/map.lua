-- standard spawn point
SPAWNPOINT = { waypoint27.Location }
-- Cell triggers arrays
DELY_CELLTRIGGERS = {CPos.New(56,9), CPos.New(56,8), CPos.New(55,8), CPos.New(54,8), CPos.New(56,7), CPos.New(55,7), CPos.New(54,7), CPos.New(56,6), CPos.New(55,6), CPos.New(54,6), CPos.New(55,5), CPos.New(54,5), CPos.New(55,4), CPos.New(54,4), CPos.New(56,3), CPos.New(55,3), CPos.New(54,3), CPos.New(56,2), CPos.New(55,2), CPos.New(54,2)}
DZNE_CELLTRIGGERS = {CPos.New(53,8), CPos.New(52,8), CPos.New(53,7), CPos.New(52,7), CPos.New(53,6), CPos.New(52,6), CPos.New(53,5), CPos.New(52,5), CPos.New(53,4), CPos.New(52,4), CPos.New(53,3), CPos.New(52,3), CPos.New(53,2), CPos.New(52,2)}

CHIN_ACTIVATE = { Actor159, Actor160, Actor161, Actor145 }
DELX_ACTIVATE = { Actor138 }
YYYY_ACTIVATE = { Actor164, Actor175, Actor165, Actor174, Actor166, Actor167, Actor168, Actor170, Actor169, Actor189, Actor168, Actor170, Actor165, Actor174, Actor164, Actor175, Actor169, Actor189 }
HUNT_ACTIVATE = { Actor114, Actor74, Actor123, Actor124, Actor125, Actor126, Actor127, Actor129 }

--/************************************ atk5 *********************************/
--atk5=Time,Create Team,175,GoodGuy,gdi2,0
ATK5_UNITS = {'mtnk'}
Trigger_Time_ATK5 = 175
TRIGGER_ATK5 = function()
	print("ATK5 started")
	--gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,1,MTNK:1,5,Move:5,Move:6,Move:7,Move:8,Attack Base:40,0,0
	Reinforcements.Reinforce(GoodGuy, ATK5_UNITS, SPAWNPOINT, 15, function(unit)        
		unit.Move(waypoint5.Location)
		unit.Move(waypoint6.Location)
		unit.Move(waypoint7.Location)
		unit.Move(waypoint8.Location)
	end)
end

--/************************************ atk4 *********************************/
--atk4=Time,Create Team,160,GoodGuy,gdi4,0
ATK4_UNITS = {'jeep'}
Trigger_Time_ATK4 = 160
TRIGGER_ATK4 = function()
	print("ATK4 started")
	--gdi4=GoodGuy,1,0,0,0,0,15,0,0,0,1,JEEP:2,4,Move:5,Move:6,Move:7,Move:8,0,0
	Reinforcements.Reinforce(GoodGuy, ATK4_UNITS, SPAWNPOINT, 15, function(unit)        
		unit.Move(waypoint5.Location)
		unit.Move(waypoint6.Location)
		unit.Move(waypoint7.Location)
		unit.Move(waypoint8.Location)
	end)
end

--/************************************ atk3 *********************************/
--atk3=Time,Create Team,120,GoodGuy,gdi1,0
ATK3_UNITS = {'e2', 'e2', 'e3', 'e3'}
Trigger_Time_ATK3 = 120
TRIGGER_ATK3 = function()
	print("ATK3 started")
	--gdi1=GoodGuy,1,0,0,0,0,15,0,0,0,2,E2:2,E3:2,6,Move:1,Move:2,Move:3,Move:9,Move:10,Attack Units:30,0,1
	Reinforcements.Reinforce(GoodGuy, ATK3_UNITS, SPAWNPOINT, 15, function(unit)        
		unit.Move(waypoint1.Location)
		unit.Move(waypoint2.Location)
		unit.Move(waypoint3.Location)
		unit.Move(waypoint9.Location)
		unit.Move(waypoint10.Location)
	end)
end

--/************************************ atk2 *********************************/
--atk2=Time,Create Team,110,GoodGuy,gdi3,0
ATK2_UNITS = {'e1', 'e1', 'e1', 'e2', 'e2', 'e2'}
Trigger_Time_ATK2 = 110
TRIGGER_ATK2 = function()
	print("ATK2 started")
	--gdi3=GoodGuy,1,0,0,0,0,15,0,0,0,2,E1:3,E2:3,5,Move:1,Move:2,Move:4,Move:11,Attack Units:40,0,1
	Reinforcements.Reinforce(GoodGuy, ATK2_UNITS, SPAWNPOINT, 15, function(unit)        
		unit.Move(waypoint1.Location)
		unit.Move(waypoint2.Location)
		unit.Move(waypoint4.Location)
		unit.Move(waypoint11.Location)
	end)
end

--/************************************ atk1 *********************************/
--atk1=Time,Create Team,90,GoodGuy,gdi1,0
ATK1_UNITS = {'e2', 'e2', 'e3', 'e3'}
Trigger_Time_ATK1 = 90
TRIGGER_ATK1 = function()
	print("ATK1 started")
	--gdi1=GoodGuy,1,0,0,0,0,15,0,0,0,2,E2:2,E3:2,6,Move:1,Move:2,Move:3,Move:9,Move:10,Attack Units:30,0,1
	Reinforcements.Reinforce(GoodGuy, ATK1_UNITS, SPAWNPOINT, 15, function(unit)        
		unit.Move(waypoint1.Location)
		unit.Move(waypoint2.Location)
		unit.Move(waypoint3.Location)
		unit.Move(waypoint9.Location)
		unit.Move(waypoint10.Location)
	end)
end

-- ************************** HUNT Start **********************************************
HUNT = function()	
	local list = BadGuy.GetGroundAttackers()
	for isx, val in pairs(list) do        
		val.Hunt()
   end
end
-- ************************** HUNT End ***********************************************

-- ************************** SCAN start **************************
SCAN = function()
	if  HUNT_ACTIVATE.IsInWorld == false then
		HUNT()
	end
	Trigger.AfterDelay(DateTime.Seconds(15), function() SCAN() end)
end
-- ************************** SCAN end ************************************************


WorldLoaded = function()
	BadGuy = Player.GetPlayer("GoodGuy")
	GoodGuy = Player.GetPlayer("BadGuy")

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

	BadGuyObjective = BadGuy.AddPrimaryObjective("Destroy NOD!")
	GoodGuyObjective = GoodGuy.AddPrimaryObjective("Locate the abandoned GDI base,\nuse GDI's own weapons against them \nand be sure that no GDI forces remain alive!")

	--/************************************ chin *********************************/
	CHIN_TeamPath = {waypoint26.Location, waypoint0.Location}
	CHIN_UNITS = { "tran" }
	Trigger.OnAnyKilled(CHIN_ACTIVATE, function()
		Reinforcements.Reinforce(GoodGuy,CHIN_UNITS, CHIN_TeamPath, 15, function(unit)
			print("CHIN started")
			unit.Move(waypoint0.Location)
		end)
	end)

	--/************************************ yyyy *********************************/
	-- Trigger.OnAllKilled(YYYY_ACTIVATE, function()
	-- 	BadGuy.MarkCompletedObjective(GoodGuyObjective)
	-- end)

	--/************************************ xxxx *********************************/
	-- XXXX_UNITS = { "a10", "a10" }
	-- TRIGGER_XXXX_TIME = 150
	-- Trigger.AfterDelay(DateTime.Seconds(TRIGGER_XXXX_TIME), function() TRIGGER_XXXX() end)

	-- TRIGGER_XXXX = function()
	-- 	print("XXXX started")
	-- 	if DstryTrigXXXX == false then
	-- 		Reinforcements.Reinforce(BadGuy, XXXX_UNITS, SPAWNPOINT, 15, function(unit)
	-- 		end)
	-- 	end
	-- end
	
	--/************************************ delx *********************************/
    Trigger.OnAllKilled(DELX_ACTIVATE,  function() TRIGGER_DELX() end)
    
	DstryTrigXXXX = false
	TRIGGER_DELX = function()
		print("DELX started")
	    DstryTrigXXXX = true
	end

	--/************************************ dely *********************************/
	Trigger.OnEnteredFootprint(DELY_CELLTRIGGERS, function(a, id)
		print("DELY started")
		if a.Owner == GoodGuy then
			Trigger.RemoveFootprintTrigger(id)
			TRIGGER_YYYY = function() end
		end
	end)

	--/************************************ dzne *********************************/
	Trigger.OnEnteredFootprint(DZNE_CELLTRIGGERS, function(a, id)
		print("DZNE started")
		if a.Owner == GoodGuy then
			Trigger.RemoveFootprintTrigger(id)
			TRIGGER_DZNE = function() end
		end
	end)

	-- *********************************************** hunt start ***************************
	--Trigger.AfterDelay(DateTime.Seconds(60), function() SCAN() end)	
	-- *********************************************** hunt end *****************************
end

Tick = function()
	if GoodGuy.HasNoRequiredUnits()  then
		BadGuy.MarkCompletedObjective(BadGuyObjective)
	end

	if BadGuy.HasNoRequiredUnits() then
		GoodGuy.MarkCompletedObjective(GoodGuyObjective)
	end
end