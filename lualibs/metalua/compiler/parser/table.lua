--------------------------------------------------------------------------------
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
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
--
-- Exported API:
-- * [M.table_bracket_field()]
-- * [M.table_field()]
-- * [M.table_content()]
-- * [M.table()]
--
-- KNOWN BUG: doesn't handle final ";" or "," before final "}"
--
--------------------------------------------------------------------------------

local gg  = require 'metalua.grammar.generator'

return function(M)

    M.table = { }
    local _table = gg.future(M.table)
    local _expr  = gg.future(M).expr

    --------------------------------------------------------------------------------
    -- `[key] = value` table field definition
    --------------------------------------------------------------------------------
    M.table.bracket_pair = gg.sequence{ "[", _expr, "]", "=", _expr, builder = "Pair" }

    --------------------------------------------------------------------------------
    -- table element parser: list value, `id = value` pair or `[value] = value` pair.
    --------------------------------------------------------------------------------
    function M.table.element (lx)
        if lx :is_keyword (lx :peek(), "[") then return M.table.bracket_pair(lx) end
        local e = M.expr (lx)
        if not lx :is_keyword (lx :peek(), "=") then return e end
        lx :next(); -- skip the "="
        local ok, key = pcall(M.id2string, e) -- will fail on non-identifiers
        if not ok then return gg.parse_error (lx, "Identifier expected.") end
        local val = M.expr(lx)
        local r = { tag="Pair", key, val }
        r.lineinfo = { first = key.lineinfo.first, last = val.lineinfo.last }
        return r
    end

    -----------------------------------------------------------------------------
    -- table constructor, without enclosing braces; returns a full table object
    -----------------------------------------------------------------------------
    M.table.content  = gg.list {
        -- eta expansion to allow patching the element definition
        primary     =  _table.element,
        separators  = { ",", ";" },
        terminators = "}",
        builder     = "Table" }

    --------------------------------------------------------------------------------
    -- complete table constructor including [{...}]
    --------------------------------------------------------------------------------
    -- TODO beware, stat and expr use only table.content, this can't be patched.
    M.table.table = gg.sequence{ "{", _table.content, "}", builder = unpack }

    return M
end