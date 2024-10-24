-- Mock actor normally provided by the game scripting engine.
Actor = {
    NextId = 0;

    BuildTime = function(type)
        local buildTime = {
            nuke = 45
        }
        assert(buildTime[type], 'No such type: ' .. tostring(type))
        return buildTime[type]
    end;

    Cost = function(type)
        local costs = {
            nuke = 300
        }
        assert(costs[type], 'No such type: ' .. tostring(type))
        return costs[type]
    end;

    Create = function(type, addToWorld, attribs)
        print(string.format("Actor.Create %s", type))
        assert(attribs.Owner, 'Missing attribute: Owner')
        local actor = {
            Location = {X = 0, Y = 0},
            Type = type,
        }
        -- Shallow copy
        for k,v in pairs(attribs) do
            --print(string.format('attrib %s %s', tostring(k), tostring(v)))
            actor[k] = v
        end
        actor.Id = Actor.NextId
        Actor.NextId = Actor.NextId + 1
        table.insert(attribs.Owner.Actors, actor)
        return actor --return Utils.Concat(actor, attribs)
    end;
}

-- Mock CPos normally provided by the game scripting engine.
CPos = {
    New = function(x, y)
        return { X = x, Y = y }
    end
}

-- Mock DateTime
DateTime = {
    _MockTickRate = 25;
    Seconds = function(s)
        return _MockTickRate * s
    end;
    Minutes = function(m)
        return MockTickRate * 60 * m
    end;
    Hours = function(h)
        return MockTickRate * 3600 * h
    end;
}

-- Mock Media normally provided by the game scripting engine.
Media = {
    Debug = function(msg)
        print('DBG ' .. msg)
    end;
}

-- Mock Map
Map = {
    LobbyOptionOrDefault = function(difficulty, defaultDifficulty)
        return defaultDifficulty
    end;
}

---Mock Utils normally provided by the game scripting engine.
Utils = {
	--- Concatenates two Lua tables into a single table.
    --- WARNING THIS IS SHALLOW COPY??
	---@param t1 table
	---@param t2 table
	---@return table
	Concat = function(t1, t2)
        assert(type(t1) == 'table', 't1 is not a table')
        assert(type(t2) == 'table', 't2 is not a table')
		local t3 = {}
		for k,v in pairs(t1) do
			t3[k] = v
		end
		for k,v in pairs(t2) do
			t3[k] = v
		end
		return t3
	end;
}

-- Mock Trigger
Trigger = {
    QueueKilled = {};

    AfterDelay = function(interval, callback)
        table.insert(Trigger.TimerEvts, {Interval = interval, Callback = callback})
    end;

    OnDamaged = function(actor, callback)
        print('Trigger.OnDamaged register '..ActorString(actor))
        if not actor.OnDamaged then
            actor.OnDamaged = {}
        end
        table.insert(actor.OnDamaged, callback)
    end;

    OnKilled = function(actor, callback)
        print('Trigger.OnKilled register '..ActorString(actor))
        if not actor.OnKilled then
            actor.OnKilled = {}
        end
        table.insert(actor.OnKilled, callback)
    end;

    TimerEvts = {};
}


package.path = package.path .. ";../../scripts/campaign.lua"
require "campaign"
require "campaign-base"
require "campaign-finances"

Simulate = {
    Kill = function(actor)
        for k,callback in pairs(actor.OnKilled) do
            callback(actor)
        end
    end;
}

BaseTest = function()

	Nod = {
        Actors = {},
        Cash = 5000,
        Resources = 1000,
        GetActorsByType = function(type)
            local actors = {}
            for k,v in pairs(Nod.Actors) do
                if v.Type == type then
                    table.insert(actors, v)
                end
            end
            return actors
        end
    }
    local cyard = Actor.Create('fact', true, {Owner = Nod})
    local bldg1 = Actor.Create('nuke', true, {Owner = Nod})

	local structures = { bldg1 }
	local rallyPoint = CPos.New(10, 20) --{ X = 10, Y = 20 }
	Base.Init(Nod, structures, rallyPoint)

    Simulate.Kill(bldg1)

    while #Trigger.TimerEvts > 0 do
        -- Process head of the queue
        local evt = Trigger.TimerEvts[1]
        table.remove(Trigger.TimerEvts, 1)
        print(string.format('\nAfter %d seconds', evt.Interval))
        evt.Callback()
    end
end

BaseTest()
