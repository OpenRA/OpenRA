
Teams = {

    Init = function(player)
        Teams.Player = player
    end;

    CreateTeam = function(team)
        print('+ CreateTeam')
        local members = {}
        for type, amount in pairs(team.Units) do
            print('  Gathering ' .. type .. ' Quantity ' .. amount)
            local some = Utils.Take(amount, Teams.Player.GetActorsByType(type))
            for idx, actor in ipairs(some) do
                table.insert(members, actor)
            end
        end
        print(string.format(' Recruited %d members', #members))
        if team.Patrol then
            local path = {}
            for idx, wpNum in ipairs(team.Patrol.Waypoints) do
                local waypoint = Waypoints['waypoint'..tostring(wpNum)]
                print(string.format('Patrol (%d,%d)', waypoint.Location.X, waypoint.Location.Y))
                table.insert(path, waypoint)
            end
            Patrol(members, path, team.Patrol.Wait)
        else
            for idx, orderElt in ipairs(team.Orders) do
                -- Grab the first key in the table
                local order = pairs(orderElt)(orderElt)
                local arg = orderElt[order]
                print(string.format("  [%d] %s = %s", idx, order, arg))
                order = string.upper(order)
                if order == 'MOVE' then
                    ---_G not defined??
                    local waypoint = Waypoints['waypoint'..tostring(arg)]
                    print(string.format('Waypoint %d: (%d,%d)', arg,
                        waypoint.Location.X, waypoint.Location.Y))
                    --[[for idx, member in ipairs(members) do
                        print(string.format('    Move member %s', ActorString(member)))
                        MoveAndIdle(member, waypoint)
                    end]]
                    MoveAndIdle(members, {waypoint})
                elseif order == 'ATTACK UNITS' then
                    print('TODO ATTACK UNITS')
                elseif order == 'ATTACK BASE' then
                    print('TODO ATTACK BASE')
                else
                    print(string.format('%s TODO Unknown order ',order))
                end
            end
        end
        print('- CreateTeam')
    end;

    SendWaves = function(counter, Waves)
        if counter <= #Waves then
            local team = Waves[counter]

            for type, amount in pairs(team.units) do
                MoveAndHunt(Utils.Take(amount, Teams.Player.GetActorsByType(type)), team.waypoints)
            end

            Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
        end
    end;
}

Triggers = {

    Init = function(triggers)
        for idx, trigger in pairs(triggers) do
            print(string.format('Initializing trigger %s: %s', idx, trigger.Action))
            if trigger.Action == 'Create Team' then
                trigger.Trigger = Trigger.AfterDelay(trigger.Interval,
                    function()
                        print(string.format('Creating %s team after %d', idx, trigger.Interval))
                        Teams.CreateTeam(trigger.Team)
                    end)
            else
                print(string.format('ERROR Trigger_Init: %s Unknown action %s' .. idx, trigger.Action))
            end
        end
    end;
}
