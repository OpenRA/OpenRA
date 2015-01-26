-- standard spawn point
SPAWNPOINT = { waypoint0.Location }
-- Cell triggers arrays
dely_celltriggers = {CPos.New(57,54), CPos.New(56,54)}
norc_celltriggers = {CPos.New(56,50), CPos.New(55,50), CPos.New(54,50), CPos.New(53,50), CPos.New(52,50), CPos.New(51,50), CPos.New(50,50), CPos.New(49,50), CPos.New(56,49), CPos.New(55,49), CPos.New(54,49), CPos.New(53,49), CPos.New(52,49), CPos.New(51,49), CPos.New(50,49), CPos.New(49,49)}

WIN_ACTIVATE = { }
YYYY_ACTIVATE = {  }
--DELY_ACTIVATE = { Actor72 }
GRD1_ACTIVATE = { Actor73, Actor74 }

 --************************************* grd4 start ***********************************************
--grd4=Time,Create Team,8,GoodGuy,gdi1,0
-- gdi1=GoodGuy,1,0,0,0,0,20,0,0,0,1,E1:2,8,Move:0,Move:1,Move:2,Move:8,Guard:2,Move:9,Guard:2,Loop:3,0,0
Trigger_Time_grd4 = 8
Trigger_Units_grd4 = {'e1', 'e1'}
TRIGGER_grd4 = function()
    Reinforcements.Reinforce(GDI, Trigger_Units_grd4, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)                  
        unit.Move(waypoint1.Location)
        unit.Move(waypoint8.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint9.Location)
        unit.Move(waypoint2.Location)
    end)
end

--************************************** grd4 end ************************************************

 --************************************* grd3 start ***********************************************
--grd3=Time,Create Team,2,GoodGuy,gdi3,0
--gdi3=GoodGuy,1,0,0,0,0,25,0,0,0,1,JEEP:1,14,Move:0,Move:1,Guard:2,Move:2,Move:3,Move:4,Move:3,Guard:2,Move:2,Move:5,Move:6,Guard:2,Move:7,Loop:0,0,0
Trigger_Time_grd3 = 2
Trigger_Units_grd3 = {'jeep'}
TRIGGER_grd3 = function()
    Reinforcements.Reinforce(GDI, Trigger_Units_grd3, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint0.Location)                  
        unit.Move(waypoint1.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint4.Location)
        unit.Move(waypoint3.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint5.Location)
        unit.Move(waypoint6.Location)
        unit.Move(waypoint2.Location)
        unit.Move(waypoint7.Location)
    end)
end
--************************************** grd3 end ************************************************

--************************************* grd2 start ***********************************************
-- grd2=Destroyed,Create Team,0,None,mtank,0
-- mtank=GoodGuy,1,0,0,0,0,4,0,0,0,1,MTNK:1,3,Move:14,Guard:5,Loop:1,0,0
-- it is Actor75
-- test: actor90
Trigger_Units_grd2 = {'mtnk'}
TRIGGER_Destroyed_Units_grd2 = {Actor75}
TRIGGER_grd2 = function()
    Reinforcements.Reinforce(GDI, Trigger_Units_grd2, SPAWNPOINT, 15, function(unit)        
        unit.Move(waypoint15.Location)
        unit.Move(waypoint5.Location)
    end)
end

--************************************* grd2 end ***********************************************

--************************************* win start ***********************************************
TRIGGER_Destroyed_Units_win = {Actor86, Actor87, Actor89, Actor93, Actor95 }
TRIGGER_win = function()
    -- you won
    NOD.MarkCompletedObjective(NODObjective)
end
--************************************* win end ***********************************************

--************************************* yyyy start ***********************************************
TRIGGER_Destroyed_Units_yyyy = {Actor138, Actor174, Actor139, Actor140, Actor144, Actor173, Actor139, Actor140, Actor144, Actor173, Actor138, Actor174}
TRIGGER_yyyy = function()    
    -- you lose
    -- !!!! This trigger can be deleted
    if DeactiveTriggerYYYY == false then
        GDI.MarkCompletedObjective(GDIObjective)
    end    
end
--************************************* yyyy end ***********************************************

--************************************* grd1 start **********************************************
-- grd1=Attacked,Create Team,0,None,gdi2,0
-- gdi2=GoodGuy,1,0,0,0,0,15,0,0,0,4,E1:10,E2:8,MTNK:1,JEEP:1,5,Move:12,Move:15,Move:0,Guard:60,Attack Units:50,0,0
Trigger_Units_grd1 = {'e1','e1','e1','e1','e1','e1','e1','e1','e1','e1','e2','e2','e2','e2','e2','e2','e2','e2', 'mtnk', 'jeep'}
grd1_SWITCH = true
TRIGGER_grd1 = function()
    if grd1_SWITCH == true then
        Reinforcements.Reinforce(GDI, Trigger_Units_grd1, SPAWNPOINT, 15, function(unit)        
            unit.Move(waypoint12.Location)
            unit.Move(waypoint15.Location)
            unit.AttackMove(waypoint0.Location)
        end)
        grd1_SWITCH = false
    end
end
--************************************* grd1 end ***********************************************

--************************************* dely start ***********************************************
DeactiveTriggerYYYY = false
--************************************* dely end ***********************************************

WorldLoaded = function()
	GDI = Player.GetPlayer("GoodGuy")
	NOD = Player.GetPlayer("BadGuy")

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
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("flag.vqa")
		end)
	end)

	GDIObjective = GDI.AddPrimaryObjective("Kill all enemies!")
	NODObjective = NOD.AddPrimaryObjective("Destroy the village in the north!")

    -- grd4
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_grd4), function() TRIGGER_grd4() end)

    --grd3
    Trigger.AfterDelay(DateTime.Seconds(Trigger_Time_grd3), function() TRIGGER_grd3() end)

    -- grd2
    Trigger.OnAnyKilled(TRIGGER_Destroyed_Units_grd2, function() TRIGGER_grd2() end)

    --norc
    Trigger.OnEnteredFootprint(norc_celltriggers, function(a, id)        
        if a.Owner == NOD then
            -- light something up in map not found in openra
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- win
    Trigger.OnAllKilled(TRIGGER_Destroyed_Units_win,  function() TRIGGER_win() end)

    -- yyyy
    Trigger.OnAllKilled(TRIGGER_Destroyed_Units_yyyy,  function() TRIGGER_yyyy() end)

    --dely
    Trigger.OnEnteredFootprint(dely_celltriggers, function(a, id)        
        if a.Owner == NOD then
            -- destroy trigger yyyy
            DeactiveTriggerYYYY = true
        end
        Trigger.RemoveFootprintTrigger(id)
    end)

    -- grd1    
    Trigger.OnDamaged(Actor73, TRIGGER_grd1)
    Trigger.OnDamaged(Actor74, TRIGGER_grd1)
end

Tick = function()
    --lose
    if #NOD.GetGroundAttackers() == 0 then
        GDI.MarkCompletedObjective(GDIObjective)
    end
end
