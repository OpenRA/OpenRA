--------------------------------------------------------------------------------
-- Copyright (c) 2006-2014 Fabien Fleutot and others.
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

----------------------------------------------------------------------
-- Generate a new lua-specific lexer, derived from the generic lexer.
----------------------------------------------------------------------

local generic_lexer = require 'metalua.grammar.lexer'

return function()
    local lexer = generic_lexer.lexer :clone()

    local keywords = {
        "and", "break", "do", "else", "elseif",
        "end", "false", "for", "function",
        "goto", -- Lua5.2
        "if",
        "in", "local", "nil", "not", "or", "repeat",
        "return", "then", "true", "until", "while",
        "...", "..", "==", ">=", "<=", "~=",
        "<<", ">>", "//", -- Lua5.3
        "::", -- Lua5.2
        "+{", "-{" } -- Metalua

    for _, w in ipairs(keywords) do lexer :add (w) end

    return lexer
end