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

local gg    = require 'metalua.grammar.generator'

return function(M)
    local _M = gg.future(M)
    M.lexer :add '->'
    local A = { }
    local _A = gg.future(A)
    M.annot = A

    -- Type identifier: Lua keywords such as `"nil"` allowed.
    function M.annot.tid(lx)
        local w = lx :next()
        local t = w.tag
        if t=='Keyword' and w[1] :match '^[%a_][%w_]*$' or w.tag=='Id'
        then return {tag='TId'; lineinfo=w.lineinfo; w[1]}
        else return gg.parse_error (lx, 'tid expected') end
    end

    local field_types = { var='TVar'; const='TConst';
                          currently='TCurrently'; field='TField' }

    -- TODO check lineinfo
    function M.annot.tf(lx)
        local tk = lx:next()
        local w = tk[1]
        local tag = field_types[w]
        if not tag then error ('Invalid field type '..w)
        elseif tag=='TField' then return {tag='TField'} else
            local te = M.te(lx)
            return {tag=tag; te}
        end
    end

    M.annot.tebar_content = gg.list{
        name        = 'tebar content',
        primary     = _A.te,
        separators  = { ",", ";" },
        terminators = ")" }

    M.annot.tebar = gg.multisequence{
        name = 'annot.tebar',
        --{ '*', builder = 'TDynbar' }, -- maybe not user-available
        { '(', _A.tebar_content, ')',
          builder = function(x) return x[1] end },
        { _A.te }
    }

    M.annot.te = gg.multisequence{
        name = 'annot.te',
        { _A.tid, builder=function(x) return x[1] end },
        { '*', builder = 'TDyn' },
        { "[",
          gg.list{
              primary = gg.sequence{
                  _M.expr, "=", _A.tf,
                  builder = 'TPair'
              },
              separators  = { ",", ";" },
              terminators = { "]", "|" } },
          gg.onkeyword{ "|", _A.tf },
          "]",
          builder = function(x)
              local fields, other = unpack(x)
              return { tag='TTable', other or {tag='TField'}, fields }
          end }, -- "[ ... ]"
        { '(', _A.tebar_content, ')', '->', '(', _A.tebar_content, ')',
          builder = function(x)
               local p, r = unpack(x)
               return {tag='TFunction', p, r }
           end } }

    M.annot.ts = gg.multisequence{
        name = 'annot.ts',
        { 'return', _A.tebar_content, builder='TReturn' },
        { _A.tid, builder = function(x)
              if x[1][1]=='pass' then return {tag='TPass'}
              else error "Bad statement type" end
          end } }

-- TODO: add parsers for statements:
-- #return tebar
-- #alias = te
-- #ell = tf
--[[
    M.annot.stat_annot = gg.sequence{
        gg.list{ primary=_A.tid, separators='.' },
        '=',
        XXX??,
        builder = 'Annot' }
--]]

    return M.annot
end