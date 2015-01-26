-- standard spawn point
SPAWNPOINT = { waypoint27.Location }

APC1_UNITS = { 'c2', 'c3', 'c4', 'c5', 'apc' }
TRIGGER_APC1_TIME = 3
TRIGGER_APC1 = function()
	Reinforcements.Reinforce(enemy, APC1_UNITS, SPAWNPOINT, 15, function(unit)
		unit.Move(waypoint0.Location)
		unit.Move(waypoint11.Location)
		unit.Move(waypoint10.Location)
		unit.Move(waypoint8.Location)
		unit.Move(waypoint9.Location)
		-- TODO: Loop
	end)
end

APC3_CELLTRIGGERS = {CPos.New(28,58), CPos.New(27,58), CPos.New(28,57), CPos.New(27,57), CPos.New(28,56), CPos.New(27,56), CPos.New(28,55), CPos.New(27,55), CPos.New(28,54), CPos.New(27,54), CPos.New(28,53), CPos.New(27,53)}
TRIGGER_APC3 = function()
	if (units.IsDead == false) then
		unit.Move(waypoint3.Location)
		unit.Move(waypoint2.Location)
		unit.Move(waypoint1.Location)
		unit.Move(waypoint0.Location)
		unit.Move(waypoint11.Location)
		unit.Move(waypoint10.Location)
		unit.Move(waypoint8.Location)
		unit.Move(waypoint9.Location)
		-- TODO: Guard
		-- TODO: Loop
		end
end

CIV1_CELLTRIGGERS = {CPos.New(24,52), CPos.New(23,52), CPos.New(22,52), CPos.New(23,51), CPos.New(22,51), CPos.New(21,51)}
TRIGGER_CIV1 = function(units) 
    if units.IsDead == false then
        units.Move(waypoint3.Location)
		units.Move(waypoint2.Location)
		units.Move(waypoint3.Location)
		units.Move(waypoint1.Location)
		units.Move(waypoint2.Location)
		-- TODO: Guard
		units.Move(waypoint11.Location)
		-- TODO: Guard
		units.Move(waypoint10.Location)
		-- TODO: Guard
		units.Move(waypoint8.Location)
		-- TODO: Guard
		units.Move(waypoint9.Location)
    end
end

CIV2_CELLTRIGGERS = {CPos.New(26,54), CPos.New(25,54), CPos.New(24,54), CPos.New(25,53), CPos.New(24,53), CPos.New(23,53)}
TRIGGER_CIV2 = function(units) 
    if units.IsDead == false then
        units.Move(waypoint3.Location)
		units.Move(waypoint2.Location)
		-- units.Move(waypoint0.Location)	-- -14?
		units.Move(waypoint1.Location)
		-- units.Move(waypoint0.Location)	-- -15?
		units.Move(waypoint0.Location)
		-- units.Move(waypoint0.Location)	-- -14?
		units.Move(waypoint11.Location)
		-- units.Move(waypoint0.Location)	-- -15?
		units.Move(waypoint10.Location)
		-- units.Move(waypoint0.Location)	-- -14?
		units.Move(waypoint8.Location)
		-- units.Move(waypoint0.Location)	-- -15?
		units.Move(waypoint9.Location)
    end
end

-- TODO: Unknown reason for 'los1'
LOS1_CELLTRIGGERS = {CPos.New(49,26), CPos.New(48,26), CPos.New(49,25), CPos.New(48,25), CPos.New(49,24), CPos.New(48,24), CPos.New(49,23), CPos.New(48,23), CPos.New(49,22), CPos.New(48,22), CPos.New(49,21), CPos.New(48,21), CPos.New(49,20), CPos.New(48,20)}

WorldLoaded = function()
	enemy = Player.GetPlayer("GoodGuy")
	player = Player.GetPlayer("BadGuy")
	
	Trigger.AfterDelay(DateTime.Seconds(TRIGGER_APC1_TIME), function() TRIGGER_APC1() end)

	Trigger.OnEnteredFootprint(APC3_CELLTRIGGERS, function(player, id)
		if (player.Owner == Player.GetPlayer("Neutral")) then
			NeutralActors = { Actor127 }
            for key,value in pairs(NeutralActors) do
                TRIGGER_APC3(NeutralActors[key])
            end
            Trigger.RemoveFootprintTrigger(id)
        end
    end)

	Trigger.OnEnteredFootprint(CIV1_CELLTRIGGERS, function(player, id)
		if (player.Owner == Player.GetPlayer("GoodGuy")) then
			NeutralActors = {Actor99, Actor106, Actor105, Actor104}
            for key,value in pairs(NeutralActors) do
                TRIGGER_CIV1(NeutralActors[key])
            end           
            Trigger.RemoveFootprintTrigger(id)
        end
    end)
	
	Trigger.OnEnteredFootprint(CIV2_CELLTRIGGERS, function(player, id)
		if (player.Owner == Player.GetPlayer("GoodGuy")) then
			NeutralActors = {Actor103, Actor102, Actor101, Actor100}
            for key,value in pairs(NeutralActors) do
                TRIGGER_CIV2(NeutralActors[key])
            end
			Trigger.RemoveFootprintTrigger(id)
        end
    end)
	
	Trigger.OnEnteredFootprint(LOS1_CELLTRIGGERS, function(player, id)
		if (player.Owner == Player.GetPlayer("Neutral")) then
			-- enemy.MarkCompletedObjective(enemyObjective) -- TODO: LOS1_CELLTRIGGERS
        end
    end)

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	playerObjective = enemy.AddPrimaryObjective("Kill all enemies!")
	enemyObjective = player.AddPrimaryObjective("Kill all enemies!")

	WIN2_ACTIVATE = { Actor71, Actor78, Actor79, Actor80, Actor81, Actor82, Actor83, Actor84, Actor85, Actor86, Actor88, Actor89, Actor90, Actor91 }
	WIN1_ACTIVATE = { Actor99, Actor100, Actor101, Actor102, Actor103, Actor104, Actor105, Actor106 }
	HUM1_ACTIVATE = { Actor94 }
	APC2_ACTIVATE = { Actor107, Actor108, Actor109 }

end

Tick = function()
	if enemy.HasNoRequiredUnits()  then
		player.MarkCompletedObjective(playerObjective)
	end

	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(enemyObjective)
	end
end
