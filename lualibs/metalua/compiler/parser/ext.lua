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

--------------------------------------------------------------------------------
--
-- Non-Lua syntax extensions
--
--------------------------------------------------------------------------------

local gg        = require 'metalua.grammar.generator'

return function(M)

    local _M = gg.future(M)

    ---------------------------------------------------------------------------
    -- Algebraic Datatypes
    ----------------------------------------------------------------------------
    local function adt (lx)
        local node = _M.id (lx)
        local tagval = node[1]
        -- tagkey = `Pair{ `String "key", `String{ -{tagval} } }
        local tagkey = { tag="Pair", {tag="String", "tag"}, {tag="String", tagval} }
        if lx:peek().tag == "String" or lx:peek().tag == "Number" then
            -- TODO support boolean litterals
            return { tag="Table", tagkey, lx:next() }
        elseif lx:is_keyword (lx:peek(), "{") then
            local x = M.table.table (lx)
            table.insert (x, 1, tagkey)
            return x
        else return { tag="Table", tagkey } end
    end

    M.adt = gg.sequence{ "`", adt, builder = unpack }

    M.expr.primary :add(M.adt)

    ----------------------------------------------------------------------------
    -- Anonymous lambda
    ----------------------------------------------------------------------------
    M.lambda_expr = gg.sequence{
        "|", _M.func_params_content, "|", _M.expr,
        builder = function (x)
            local li = x[2].lineinfo
            return { tag="Function", x[1],
                     { {tag="Return", x[2], lineinfo=li }, lineinfo=li } }
        end }

    M.expr.primary :add (M.lambda_expr)

    --------------------------------------------------------------------------------
    -- Allows to write "a `f` b" instead of "f(a, b)". Taken from Haskell.
    --------------------------------------------------------------------------------
    function M.expr_in_backquotes (lx) return M.expr(lx, 35) end -- 35=limited precedence
    M.expr.infix :add{ name = "infix function",
        "`", _M.expr_in_backquotes, "`", prec = 35, assoc="left",
        builder = function(a, op, b) return {tag="Call", op[1], a, b} end }

    --------------------------------------------------------------------------------
    -- C-style op+assignments
    -- TODO: no protection against side-effects in LHS vars.
    --------------------------------------------------------------------------------
    local function op_assign(kw, op)
        local function rhs(a, b) return { tag="Op", op, a, b } end
        local function f(a,b)
            if #a ~= #b then gg.parse_error "assymetric operator+assignment" end
            local right = { }
            local r = { tag="Set", a, right }
            for i=1, #a do right[i] = { tag="Op", op, a[i], b[i] } end
            return r
        end
        M.lexer :add (kw)
        M.assignments[kw] = f
    end

    local ops = { add='+='; sub='-='; mul='*='; div='/=' }
    for ast_op_name, keyword in pairs(ops) do op_assign(keyword, ast_op_name) end

    return M
end