-------------------------------------------------------------------------------
-- Coroutine safe xpcall and pcall versions
--
-- Encapsulates the protected calls with a coroutine based loop, so errors can
-- be dealed without the usual Lua 5.x pcall/xpcall issues with coroutines
-- yielding inside the call to pcall or xpcall.
--
-- Authors: Roberto Ierusalimschy and Andre Carregal 
-- Contributors: Thomas Harning Jr., Ignacio Burgueño, Fábio Mascarenhas
--
-- Copyright 2005 - Kepler Project (www.keplerproject.org)
--
-- $Id: coxpcall.lua,v 1.13 2008/05/19 19:20:02 mascarenhas Exp $
-------------------------------------------------------------------------------

-- This version has been modified to handle the case where a child coroutine
-- (created by copcall or coxpcall) is directly resumed, instead of resuming
-- its parent coroutine, because the coroutine reference was obtained by
-- calling coroutine.running from the child coroutine. The modification
-- implemented here is to override coroutine.running so that it returns the
-- parent coroutine instead of the child one.

local oldrunning = coroutine.running
local parents = {}
setmetatable( parents, { __mode = "kv" } )

local function getRoot( co )
	return parents[co] or co
end

coroutine.running = function()
	return getRoot( oldrunning() )
end

-------------------------------------------------------------------------------
-- Implements xpcall with coroutines
-------------------------------------------------------------------------------
local performResume, handleReturnValue
local oldpcall, oldxpcall = pcall, xpcall

function handleReturnValue(err, co, status, ...)
    if not status then
        return false, err(debug.traceback(co, (...)), ...)
    end
    if coroutine.status(co) == 'suspended' then
        return performResume(err, co, coroutine.yield(...))
    else
        return true, ...
    end
end

function performResume(err, co, ...)
    return handleReturnValue(err, co, coroutine.resume(co, ...))
end    

function coxpcall(f, err, ...)
    local res, co = oldpcall(coroutine.create, f)
    if not res then
        local params = {...}
        local newf = function() return f(unpack(params)) end
        co = coroutine.create(newf)
    end
	parents[co] = getRoot( oldrunning() )
    return performResume(err, co, ...)
end

-------------------------------------------------------------------------------
-- Implements pcall with coroutines
-------------------------------------------------------------------------------

local function id(trace, ...)
  return ...
end

function copcall(f, ...)
    return coxpcall(f, id, ...)
end

-------------------------------------------------------------------------------
-- Override coroutine.running
-------------------------------------------------------------------------------