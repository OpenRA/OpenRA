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

-- Export all public APIs from sub-modules, squashed into a flat spacename

local MT = { __type='metalua.compiler.parser' }

local MODULE_REL_NAMES = { "annot.grammar", "expr", "meta", "misc",
                           "stat", "table", "ext" }

local function new()
    local M = {
        lexer = require "metalua.compiler.parser.lexer" ();
        extensions = { } }
    for _, rel_name in ipairs(MODULE_REL_NAMES) do
        local abs_name = "metalua.compiler.parser."..rel_name
        local extender = require (abs_name)
        if not M.extensions[abs_name] then
            if type (extender) == 'function' then extender(M) end
            M.extensions[abs_name] = extender
        end
    end
    return setmetatable(M, MT)
end

return { new = new }
