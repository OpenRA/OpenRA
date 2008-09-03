-----------------------------------------------------------------------------
-- LuaSocket helper module
-- Author: Diego Nehab
-- RCS ID: $Id: socket.lua 1418 2006-04-25 09:38:15Z 3rdparty $
-----------------------------------------------------------------------------

-----------------------------------------------------------------------------
-- Declare module and import dependencies
-----------------------------------------------------------------------------
local base = _G
local string = require("string")
local math = require("math")
local socket = require("socket.core")
module("socket")

-----------------------------------------------------------------------------
-- Exported auxiliar functions
-----------------------------------------------------------------------------
function connect(address, port, laddress, lport)
    local sock, err = socket.tcp()
    if not sock then return nil, err end
    if laddress then
        local res, err = sock:bind(laddress, lport, -1)
        if not res then return nil, err end
    end
    local res, err = sock:connect(address, port)
    if not res then return nil, err end
    return sock
end

function bind(host, port, backlog)
    local sock, err = socket.tcp()
    if not sock then return nil, err end
    sock:setoption("reuseaddr", true)
    local res, err = sock:bind(host, port)
    if not res then return nil, err end
    res, err = sock:listen(backlog)
    if not res then return nil, err end
    return sock
end

try = newtry()

function choose(table)
    return function(name, opt1, opt2)
        if base.type(name) ~= "string" then
            name, opt1, opt2 = "default", name, opt1
        end
        local f = table[name or "nil"]
        if not f then base.error("unknown key (".. base.tostring(name) ..")", 3)
        else return f(opt1, opt2) end
    end
end

-----------------------------------------------------------------------------
-- Socket sources and sinks, conforming to LTN12
-----------------------------------------------------------------------------
-- create namespaces inside LuaSocket namespace
sourcet = {}
sinkt = {}

BLOCKSIZE = 2048

sinkt["close-when-done"] = function(sock)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function(self, chunk, err)
            if not chunk then
                sock:close()
                return 1
            else return sock:send(chunk) end
        end
    })
end

sinkt["keep-open"] = function(sock)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function(self, chunk, err)
            if chunk then return sock:send(chunk)
            else return 1 end
        end
    })
end

sinkt["default"] = sinkt["keep-open"]

sink = choose(sinkt)

sourcet["by-length"] = function(sock, length)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function()
            if length <= 0 then return nil end
            local size = math.min(socket.BLOCKSIZE, length)
            local chunk, err = sock:receive(size)
            if err then return nil, err end
            length = length - string.len(chunk)
            return chunk
        end
    })
end

sourcet["until-closed"] = function(sock)
    local done
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function()
            if done then return nil end
            local chunk, err, partial = sock:receive(socket.BLOCKSIZE)
            if not err then return chunk
            elseif err == "closed" then
                sock:close()
                done = 1
                return partial
            else return nil, err end
        end
    })
end


sourcet["default"] = sourcet["until-closed"]

source = choose(sourcet)

