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

require 'checks'
local gg = require 'metalua.grammar.generator'
local M  = { }

function M.opt(mlc, primary, a_type)
    checks('table', 'table|function', 'string')
    return gg.sequence{
        primary,
        gg.onkeyword{ "#", function() return assert(mlc.annot[a_type]) end },
        builder = function(x)
            local t, annot = unpack(x)
            return annot and { tag='Annot', t, annot } or t
        end }
end

-- split a list of "foo" and "`Annot{foo, annot}" into a list of "foo"
-- and a list of "annot".
-- No annot list is returned if none of the elements were annotated.
function M.split(lst)
    local x, a, some = { }, { }, false
    for i, p in ipairs(lst) do
        if p.tag=='Annot' then
            some, x[i], a[i] = true, unpack(p)
        else x[i] = p end
    end
    if some then return x, a else return lst end
end

return M
