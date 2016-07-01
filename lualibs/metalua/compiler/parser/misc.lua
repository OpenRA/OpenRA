-------------------------------------------------------------------------------
-- Copyright (c) 2006-2013 Fabien Fleutot and others.
--
-- All rights reserved.
--
-- This program and the accompanying materials are made available
-- under the terms of the Eclipse Public License v1.0 which
-- accompanies this distribution, and is available at
-- http://www.eclipse.org/legal/epl-v10.html
--
-- This program and the accompanying materials are also made available
-- under the terms of the MIT public license which accompanies this
-- distribution, and is available at http://www.lua.org/license.html
--
-- Contributors:
--     Fabien Fleutot - API and implementation
--
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--
-- Summary: metalua parser, miscellaneous utility functions.
--
-------------------------------------------------------------------------------

--------------------------------------------------------------------------------
--
-- Exported API:
-- * [mlp.fget()]
-- * [mlp.id()]
-- * [mlp.opt_id()]
-- * [mlp.id_list()]
-- * [mlp.string()]
-- * [mlp.opt_string()]
-- * [mlp.id2string()]
--
--------------------------------------------------------------------------------

local pp       = require 'metalua.pprint'
local gg       = require 'metalua.grammar.generator'

-- TODO: replace splice-aware versions with naive ones, move etensions in ./meta

return function(M)
    local _M = gg.future(M)


    --------------------------------------------------------------------------------
    -- Try to read an identifier (possibly as a splice), or return [false] if no
    -- id is found.
    --------------------------------------------------------------------------------
    function M.opt_id (lx)
        local a = lx:peek();
        if lx:is_keyword (a, "-{") then
            local v = M.meta.splice(lx)
            if v.tag ~= "Id" and v.tag ~= "Splice" then
                gg.parse_error(lx, "Bad id splice")
            end
            return v
        elseif a.tag == "Id" then return lx:next()
        else return false end
    end

    --------------------------------------------------------------------------------
    -- Mandatory reading of an id: causes an error if it can't read one.
    --------------------------------------------------------------------------------
    function M.id (lx)
        return M.opt_id (lx) or gg.parse_error(lx,"Identifier expected")
    end

    --------------------------------------------------------------------------------
    -- Common helper function
    --------------------------------------------------------------------------------
    M.id_list = gg.list { primary = _M.id, separators = "," }

    --------------------------------------------------------------------------------
    -- Converts an identifier into a string. Hopefully one day it'll handle
    -- splices gracefully, but that proves quite tricky.
    --------------------------------------------------------------------------------
    function M.id2string (id)
        --print("id2string:", disp.ast(id))
        if id.tag == "Id" then id.tag = "String"; return id
        elseif id.tag == "Splice" then
            error ("id2string on splice not implemented")
            -- Evaluating id[1] will produce `Id{ xxx },
            -- and we want it to produce `String{ xxx }.
            -- The following is the plain notation of:
            -- +{ `String{ `Index{ `Splice{ -{id[1]} }, `Number 1 } } }
            return { tag="String",  { tag="Index", { tag="Splice", id[1] },
                                      { tag="Number", 1 } } }
        else error ("Identifier expected: "..pp.tostring(id, 'nohash')) end
    end

    --------------------------------------------------------------------------------
    -- Read a string, possibly spliced, or return an error if it can't
    --------------------------------------------------------------------------------
    function M.string (lx)
        local a = lx:peek()
        if lx:is_keyword (a, "-{") then
            local v = M.meta.splice(lx)
            if v.tag ~= "String" and v.tag ~= "Splice" then
                gg.parse_error(lx,"Bad string splice")
            end
            return v
        elseif a.tag == "String" then return lx:next()
        else error "String expected" end
    end

    --------------------------------------------------------------------------------
    -- Try to read a string, or return false if it can't. No splice allowed.
    --------------------------------------------------------------------------------
    function M.opt_string (lx)
        return lx:peek().tag == "String" and lx:next()
    end

    --------------------------------------------------------------------------------
    -- Chunk reader: block + Eof
    --------------------------------------------------------------------------------
    function M.skip_initial_sharp_comment (lx)
        -- Dirty hack: I'm happily fondling lexer's private parts
        -- FIXME: redundant with lexer:newstream()
        lx :sync()
        local i = lx.src:match ("^#.-\n()", lx.i)
        if i then
            lx.i = i
            lx.column_offset = i
            lx.line = lx.line and lx.line + 1 or 1
        end
    end

    local function chunk (lx)
        if lx:peek().tag == 'Eof' then
            return { } -- handle empty files
        else
            M.skip_initial_sharp_comment (lx)
            local chunk = M.block (lx)
            if lx:peek().tag ~= "Eof" then
                gg.parse_error(lx, "End-of-file expected")
            end
            return chunk
        end
    end

    -- chunk is wrapped in a sequence so that it has a "transformer" field.
    M.chunk = gg.sequence { chunk, builder = unpack }

    return M
end