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
-- Exported API:
-- * [mlp.expr()]
-- * [mlp.expr_list()]
-- * [mlp.func_val()]
--
-------------------------------------------------------------------------------

local pp    = require 'metalua.pprint'
local gg    = require 'metalua.grammar.generator'
local annot = require 'metalua.compiler.parser.annot.generator'

return function(M)
    local _M = gg.future(M)
    local _table = gg.future(M, 'table')
    local _meta  = gg.future(M, 'meta') -- TODO move to ext?
    local _annot = gg.future(M, 'annot') -- TODO move to annot

    --------------------------------------------------------------------------------
    -- Non-empty expression list. Actually, this isn't used here, but that's
    -- handy to give to users.
    --------------------------------------------------------------------------------
    M.expr_list = gg.list{ primary=_M.expr, separators="," }

    --------------------------------------------------------------------------------
    -- Helpers for function applications / method applications
    --------------------------------------------------------------------------------
    M.func_args_content = gg.list{
        name        = "function arguments",
        primary     = _M.expr,
        separators  = ",",
        terminators = ")" }

    -- Used to parse methods
    M.method_args = gg.multisequence{
        name = "function argument(s)",
        { "{",  _table.content, "}" },
        { "(",  _M.func_args_content, ")", builder = unpack },
        { "+{", _meta.quote_content, "}" },
        -- TODO lineinfo?
        function(lx) local r = M.opt_string(lx); return r and {r} or { } end }

    --------------------------------------------------------------------------------
    -- [func_val] parses a function, from opening parameters parenthese to
    -- "end" keyword included. Used for anonymous functions as well as
    -- function declaration statements (both local and global).
    --
    -- It's wrapped in a [_func_val] eta expansion, so that when expr
    -- parser uses the latter, they will notice updates of [func_val]
    -- definitions.
    --------------------------------------------------------------------------------
    M.func_params_content = gg.list{
        name="function parameters",
        gg.multisequence{ { "...", builder = "Dots" }, annot.opt(M, _M.id, 'te') },
        separators  = ",", terminators = {")", "|"} }

    -- TODO move to annot
    M.func_val = gg.sequence{
        name = "function body",
        "(", _M.func_params_content, ")", _M.block, "end",
        builder = function(x)
             local params, body = unpack(x)
             local annots, some = { }, false
             for i, p in ipairs(params) do
                 if p.tag=='Annot' then
                     params[i], annots[i], some = p[1], p[2], true
                 else annots[i] = false end
             end
             if some then return { tag='Function', params, body, annots }
             else  return { tag='Function', params, body } end
         end }

    local func_val = function(lx) return M.func_val(lx) end

    --------------------------------------------------------------------------------
    -- Default parser for primary expressions
    --------------------------------------------------------------------------------
    function M.id_or_literal (lx)
        local a = lx:next()
        if a.tag~="Id" and a.tag~="String" and a.tag~="Number" then
            local msg
            if a.tag=='Eof' then
                msg = "End of file reached when an expression was expected"
            elseif a.tag=='Keyword' then
                msg = "An expression was expected, and `"..a[1]..
                    "' can't start an expression"
            else
                msg = "Unexpected expr token " .. pp.tostring (a)
            end
            gg.parse_error (lx, msg)
        end
        return a
    end


    --------------------------------------------------------------------------------
    -- Builder generator for operators. Wouldn't be worth it if "|x|" notation
    -- were allowed, but then lua 5.1 wouldn't compile it
    --------------------------------------------------------------------------------

    -- opf1 = |op| |_,a| `Op{ op, a }
    local function opf1 (op) return
        function (_,a) return { tag="Op", op, a } end end

    -- opf2 = |op| |a,_,b| `Op{ op, a, b }
    local function opf2 (op) return
        function (a,_,b) return { tag="Op", op, a, b } end end

    -- opf2r = |op| |a,_,b| `Op{ op, b, a } -- (args reversed)
    local function opf2r (op) return
        function (a,_,b) return { tag="Op", op, b, a } end end

    local function op_ne(a, _, b)
        -- This version allows to remove the "ne" operator from the AST definition.
        -- However, it doesn't always produce the exact same bytecode as Lua 5.1.
        return { tag="Op", "not",
                 { tag="Op", "eq", a, b, lineinfo= {
                       first = a.lineinfo.first, last = b.lineinfo.last } } }
    end


    --------------------------------------------------------------------------------
    --
    -- complete expression
    --
    --------------------------------------------------------------------------------

    -- FIXME: set line number. In [expr] transformers probably
    M.expr = gg.expr {
        name = "expression",
        primary = gg.multisequence{
            name = "expr primary",
            { "(", _M.expr, ")",               builder = "Paren" },
            { "function", _M.func_val,         builder = unpack },
            { "-{", _meta.splice_content, "}", builder = unpack },
            { "+{", _meta.quote_content, "}",  builder = unpack },
            { "nil",                           builder = "Nil" },
            { "true",                          builder = "True" },
            { "false",                         builder = "False" },
            { "...",                           builder = "Dots" },
            { "{", _table.content, "}",        builder = unpack },
            _M.id_or_literal },

        infix = {
            name = "expr infix op",
            { "+",  prec = 60, builder = opf2 "add"  },
            { "-",  prec = 60, builder = opf2 "sub"  },
            { "*",  prec = 70, builder = opf2 "mul"  },
            { "/",  prec = 70, builder = opf2 "div"  },
            { "%",  prec = 70, builder = opf2 "mod"  },
            { "^",  prec = 90, builder = opf2 "pow",    assoc = "right" },
            { "//", prec = 70, builder = opf2 "idiv" },
            { "&",  prec = 36, builder = opf2 "band" },
            { "|",  prec = 32, builder = opf2 "bor"  },
            { "~",  prec = 34, builder = opf2 "bxor" },
            { "<<", prec = 38, builder = opf2 "shl"  },
            { ">>", prec = 38, builder = opf2 "shr"  },
            { "..", prec = 40, builder = opf2 "concat", assoc = "right" },
            { "==", prec = 30, builder = opf2 "eq"  },
            { "~=", prec = 30, builder = op_ne  },
            { "<",  prec = 30, builder = opf2 "lt"  },
            { "<=", prec = 30, builder = opf2 "le"  },
            { ">",  prec = 30, builder = opf2r "lt"  },
            { ">=", prec = 30, builder = opf2r "le"  },
            { "and",prec = 20, builder = opf2 "and" },
            { "or", prec = 10, builder = opf2 "or"  } },

        prefix = {
            name = "expr prefix op",
            { "not", prec = 80, builder = opf1 "not" },
            { "#",   prec = 80, builder = opf1 "len" },
            { "~",   prec = 80, builder = opf2 "bnot"  },
            { "-",   prec = 80, builder = opf1 "unm" } },

        suffix = {
            name = "expr suffix op",
            { "[", _M.expr, "]", builder = function (tab, idx)
              return {tag="Index", tab, idx[1]} end},
            { ".", _M.id, builder = function (tab, field)
              return {tag="Index", tab, _M.id2string(field[1])} end },
            { "(", _M.func_args_content, ")", builder = function(f, args)
              return {tag="Call", f, unpack(args[1])} end },
            { "{", _table.content, "}", builder = function (f, arg)
              return {tag="Call", f, arg[1]} end},
            { ":", _M.id, _M.method_args, builder = function (obj, post)
              local m_name, args = unpack(post)
              return {tag="Invoke", obj, _M.id2string(m_name), unpack(args)} end},
            { "+{", _meta.quote_content, "}", builder = function (f, arg)
              return {tag="Call", f,  arg[1] } end },
            default = { name="opt_string_arg", parse = _M.opt_string, builder = function(f, arg)
              return {tag="Call", f, arg } end } } }
    return M
end