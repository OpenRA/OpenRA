------------------------------------------------------------------------------
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
-- Summary: metalua parser, statement/block parser. This is part of the
-- definition of module [mlp].
--
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--
-- Exports API:
-- * [mlp.stat()]
-- * [mlp.block()]
-- * [mlp.for_header()]
--
-------------------------------------------------------------------------------

local lexer    = require 'metalua.grammar.lexer'
local gg       = require 'metalua.grammar.generator'

local annot = require 'metalua.compiler.parser.annot.generator'

--------------------------------------------------------------------------------
-- List of all keywords that indicate the end of a statement block. Users are
-- likely to extend this list when designing extensions.
--------------------------------------------------------------------------------


return function(M)
    local _M = gg.future(M)

    M.block_terminators = { "else", "elseif", "end", "until", ")", "}", "]" }

    -- FIXME: this must be handled from within GG!!!
    -- FIXME: there's no :add method in the list anyway. Added by gg.list?!
    function M.block_terminators :add(x)
        if type (x) == "table" then for _, y in ipairs(x) do self :add (y) end
        else table.insert (self, x) end
    end

    ----------------------------------------------------------------------------
    -- list of statements, possibly followed by semicolons
    ----------------------------------------------------------------------------
    M.block = gg.list {
        name        = "statements block",
        terminators = M.block_terminators,
        primary     = function (lx)
            -- FIXME use gg.optkeyword()
            local x = M.stat (lx)
            if lx:is_keyword (lx:peek(), ";") then lx:next() end
            return x
        end }

    ----------------------------------------------------------------------------
    -- Helper function for "return <expr_list>" parsing.
    -- Called when parsing return statements.
    -- The specific test for initial ";" is because it's not a block terminator,
    -- so without it gg.list would choke on "return ;" statements.
    -- We don't make a modified copy of block_terminators because this list
    -- is sometimes modified at runtime, and the return parser would get out of
    -- sync if it was relying on a copy.
    ----------------------------------------------------------------------------
    local return_expr_list_parser = gg.multisequence{
        { ";" , builder = function() return { } end },
        default = gg.list {
            _M.expr, separators = ",", terminators = M.block_terminators } }


    local for_vars_list = gg.list{
        name        = "for variables list",
        primary     = _M.id,
        separators  = ",",
        terminators = "in" }

    ----------------------------------------------------------------------------
    -- for header, between [for] and [do] (exclusive).
    -- Return the `Forxxx{...} AST, without the body element (the last one).
    ----------------------------------------------------------------------------
    function M.for_header (lx)
        local vars = M.id_list(lx)
        if lx :is_keyword (lx:peek(), "=") then
            if #vars ~= 1 then
                gg.parse_error (lx, "numeric for only accepts one variable")
            end
            lx:next() -- skip "="
            local exprs = M.expr_list (lx)
            if #exprs < 2 or #exprs > 3 then
                gg.parse_error (lx, "numeric for requires 2 or 3 boundaries")
            end
            return { tag="Fornum", vars[1], unpack (exprs) }
        else
            if not lx :is_keyword (lx :next(), "in") then
                gg.parse_error (lx, '"=" or "in" expected in for loop')
            end
            local exprs = M.expr_list (lx)
            return { tag="Forin", vars, exprs }
        end
    end

    ----------------------------------------------------------------------------
    -- Function def parser helper: id ( . id ) *
    ----------------------------------------------------------------------------
    local function fn_builder (list)
        local acc = list[1]
        local first = acc.lineinfo.first
        for i = 2, #list do
            local index = M.id2string(list[i])
            local li = lexer.new_lineinfo(first, index.lineinfo.last)
            acc = { tag="Index", acc, index, lineinfo=li }
        end
        return acc
    end
    local func_name = gg.list{ _M.id, separators = ".", builder = fn_builder }

    ----------------------------------------------------------------------------
    -- Function def parser helper: ( : id )?
    ----------------------------------------------------------------------------
    local method_name = gg.onkeyword{ name = "method invocation", ":", _M.id,
        transformers = { function(x) return x and x.tag=='Id' and M.id2string(x) end } }

    ----------------------------------------------------------------------------
    -- Function def builder
    ----------------------------------------------------------------------------
    local function funcdef_builder(x)
        local name, method, func = unpack(x)
        if method then
            name = { tag="Index", name, method,
                     lineinfo = {
                         first = name.lineinfo.first,
                         last  = method.lineinfo.last } }
            table.insert (func[1], 1, {tag="Id", "self"})
        end
        local r = { tag="Set", {name}, {func} }
        r[1].lineinfo = name.lineinfo
        r[2].lineinfo = func.lineinfo
        return r
    end


    ----------------------------------------------------------------------------
    -- if statement builder
    ----------------------------------------------------------------------------
    local function if_builder (x)
        local cond_block_pairs, else_block, r = x[1], x[2], {tag="If"}
        local n_pairs = #cond_block_pairs
        for i = 1, n_pairs do
            local cond, block = unpack(cond_block_pairs[i])
            r[2*i-1], r[2*i] = cond, block
        end
        if else_block then table.insert(r, #r+1, else_block) end
        return r
    end

    --------------------------------------------------------------------------------
    -- produce a list of (expr,block) pairs
    --------------------------------------------------------------------------------
    local elseifs_parser = gg.list {
        gg.sequence { _M.expr, "then", _M.block , name='elseif parser' },
        separators  = "elseif",
        terminators = { "else", "end" }
    }

    local annot_expr = gg.sequence {
        _M.expr,
        gg.onkeyword{ "#", gg.future(M, 'annot').tf },
        builder = function(x)
            local e, a = unpack(x)
            if a then return { tag='Annot', e, a }
            else return e end
        end }

    local annot_expr_list = gg.list {
        primary = annot.opt(M, _M.expr, 'tf'), separators = ',' }

    ------------------------------------------------------------------------
    -- assignments and calls: statements that don't start with a keyword
    ------------------------------------------------------------------------
    local function assign_or_call_stat_parser (lx)
        local e = annot_expr_list (lx)
        local a = lx:is_keyword(lx:peek())
        local op = a and M.assignments[a]
        -- TODO: refactor annotations
        if op then
            --FIXME: check that [e] is a LHS
            lx :next()
            local annots
            e, annots = annot.split(e)
            local v = M.expr_list (lx)
            if type(op)=="string" then return { tag=op, e, v, annots }
            else return op (e, v) end
        else
            assert (#e > 0)
            if #e > 1 then
                gg.parse_error (lx,
                    "comma is not a valid statement separator; statement can be "..
                    "separated by semicolons, or not separated at all")
            elseif e[1].tag ~= "Call" and e[1].tag ~= "Invoke" then
                local typename
                if e[1].tag == 'Id' then
                    typename = '("'..e[1][1]..'") is an identifier'
                elseif e[1].tag == 'Op' then
                    typename = "is an arithmetic operation"
                else typename = "is of type '"..(e[1].tag or "<list>").."'" end
                gg.parse_error (lx,
                     "This expression %s; "..
                     "a statement was expected, and only function and method call "..
                     "expressions can be used as statements", typename);
            end
            return e[1]
        end
    end

    M.local_stat_parser = gg.multisequence{
        -- local function <name> <func_val>
        { "function", _M.id, _M.func_val, builder =
          function(x)
              local vars = { x[1], lineinfo = x[1].lineinfo }
              local vals = { x[2], lineinfo = x[2].lineinfo }
              return { tag="Localrec", vars, vals }
          end },
        -- local <id_list> ( = <expr_list> )?
        default = gg.sequence{
            gg.list{
                primary = annot.opt(M, _M.id, 'tf'),
                separators = ',' },
            gg.onkeyword{ "=", _M.expr_list },
            builder = function(x)
                 local annotated_left, right = unpack(x)
                 local left, annotations = annot.split(annotated_left)
                 return {tag="Local", left, right or { }, annotations }
             end } }

    ------------------------------------------------------------------------
    -- statement
    ------------------------------------------------------------------------
    M.stat = gg.multisequence {
        name = "statement",
        { "do", _M.block, "end", builder =
          function (x) return { tag="Do", unpack (x[1]) } end },
        { "for", _M.for_header, "do", _M.block, "end", builder =
          function (x) x[1][#x[1]+1] = x[2]; return x[1] end },
        { "function", func_name, method_name, _M.func_val, builder=funcdef_builder },
        { "while", _M.expr, "do", _M.block, "end", builder = "While" },
        { "repeat", _M.block, "until", _M.expr, builder = "Repeat" },
        { "local", _M.local_stat_parser, builder = unpack },
        { "return", return_expr_list_parser, builder =
          function(x) x[1].tag='Return'; return x[1] end },
        { "goto", _M.id, builder =
          function(x) x[1].tag='Goto'; return x[1] end },
        { "::", _M.id, "::", builder =
          function(x) x[1].tag='Label'; return x[1] end },
        { "break", builder = function() return { tag="Break" } end },
        { "-{", gg.future(M, 'meta').splice_content, "}", builder = unpack },
        { "if", gg.nonempty(elseifs_parser), gg.onkeyword{ "else", M.block }, "end",
          builder = if_builder },
        default = assign_or_call_stat_parser }

    M.assignments = {
        ["="] = "Set"
    }

    function M.assignments:add(k, v) self[k] = v end

    return M
end