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
----------------------------------------------------------------------

----------------------------------------------------------------------
----------------------------------------------------------------------
--
-- Lua objects pretty-printer
--
----------------------------------------------------------------------
----------------------------------------------------------------------

local M = { }

M.DEFAULT_CFG = {
    hide_hash      = false; -- Print the non-array part of tables?
    metalua_tag    = true;  -- Use Metalua's backtick syntax sugar?
    fix_indent     = nil;   -- If a number, number of indentation spaces;
                            -- If false, indent to the previous brace.
    line_max       = nil;   -- If a number, tries to avoid making lines with
                            -- more than this number of chars.
    initial_indent = 0;     -- If a number, starts at this level of indentation
    keywords       = { };   -- Set of keywords which must not use Lua's field
                            -- shortcuts {["foo"]=...} -> {foo=...}
}

local function valid_id(cfg, x)
    if type(x) ~= "string" then return false end
    if not x:match "^[a-zA-Z_][a-zA-Z0-9_]*$" then return false end
    if cfg.keywords and cfg.keywords[x] then return false end
    return true
end

local __tostring_cache = setmetatable({ }, {__mode='k'})

-- Retrieve the string produced by `__tostring` metamethod if present,
-- return `false` otherwise. Cached in `__tostring_cache`.
local function __tostring(x)
    local the_string = __tostring_cache[x]
    if the_string~=nil then return the_string end
    local mt = getmetatable(x)
    if mt then
        local __tostring = mt.__tostring
        if __tostring then
            the_string = __tostring(x)
            __tostring_cache[x] = the_string
            return the_string
        end
    end
    if x~=nil then __tostring_cache[x] = false end -- nil is an illegal key
    return false
end

local xlen -- mutually recursive with `xlen_type`

local xlen_cache = setmetatable({ }, {__mode='k'})

-- Helpers for the `xlen` function
local xlen_type = {
    ["nil"] = function ( ) return 3 end;
    number  = function (x) return #tostring(x) end;
    boolean = function (x) return x and 4 or 5 end;
    string  = function (x) return #string.format("%q",x) end;
}

function xlen_type.table (adt, cfg, nested)
    local custom_string = __tostring(adt)
    if custom_string then return #custom_string end

    -- Circular referenced objects are printed with the plain
    -- `tostring` function in nested positions.
    if nested [adt] then return #tostring(adt) end
    nested [adt] = true

    local has_tag  = cfg.metalua_tag and valid_id(cfg, adt.tag)
    local alen     = #adt
    local has_arr  = alen>0
    local has_hash = false
    local x = 0

    if not cfg.hide_hash then
        -- first pass: count hash-part
        for k, v in pairs(adt) do
            if k=="tag" and has_tag then
                -- this is the tag -> do nothing!
            elseif type(k)=="number" and k<=alen and math.fmod(k,1)==0 and k>0 then
                -- array-part pair -> do nothing!
            else
                has_hash = true
                if valid_id(cfg, k) then x=x+#k
                else x = x + xlen (k, cfg, nested) + 2 end -- count surrounding brackets
                x = x + xlen (v, cfg, nested) + 5          -- count " = " and ", "
            end
        end
    end

    for i = 1, alen do x = x + xlen (adt[i], nested) + 2 end -- count ", "

    nested[adt] = false -- No more nested calls

    if not (has_tag or has_arr or has_hash) then return 3 end
    if has_tag then x=x+#adt.tag+1 end
    if not (has_arr or has_hash) then return x end
    if not has_hash and alen==1 and type(adt[1])~="table" then
        return x-2 -- substract extraneous ", "
    end
    return x+2 -- count "{ " and " }", substract extraneous ", "
end


-- Compute the number of chars it would require to display the table
-- on a single line. Helps to decide whether some carriage returns are
-- required. Since the size of each sub-table is required many times,
-- it's cached in [xlen_cache].
xlen = function (x, cfg, nested)
    -- no need to compute length for 1-line prints
    if not cfg.line_max then return 0 end
    nested = nested or { }
    if x==nil then return #"nil" end
    local len = xlen_cache[x]
    if len then return len end
    local f = xlen_type[type(x)]
    if not f then return #tostring(x) end
    len = f (x, cfg, nested)
    xlen_cache[x] = len
    return len
end

local function consider_newline(p, len)
    if not p.cfg.line_max then return end
    if p.current_offset + len <= p.cfg.line_max then return end
    if p.indent < p.current_offset then
        p:acc "\n"; p:acc ((" "):rep(p.indent))
        p.current_offset = p.indent
    end
end

local acc_value

local acc_type = {
    ["nil"] = function(p) p:acc("nil") end;
    number  = function(p, adt) p:acc (tostring (adt)) end;
    string  = function(p, adt) p:acc ((string.format ("%q", adt):gsub("\\\n", "\\n"))) end;
    boolean = function(p, adt) p:acc (adt and "true" or "false") end }

-- Indentation:
-- * if `cfg.fix_indent` is set to a number:
--   * add this number of space for each level of depth
--   * return to the line as soon as it flushes things further left
-- * if not, tabulate to one space after the opening brace.
--   * as a result, it never saves right-space to return before first element

function acc_type.table(p, adt)
    if p.nested[adt] then p:acc(tostring(adt)); return end
    p.nested[adt]  = true

    local has_tag  = p.cfg.metalua_tag and valid_id(p.cfg, adt.tag)
    local alen     = #adt
    local has_arr  = alen>0
    local has_hash = false

    local previous_indent = p.indent

    if has_tag then p:acc("`"); p:acc(adt.tag) end

    local function indent(p)
        if not p.cfg.fix_indent then p.indent = p.current_offset
        else p.indent = p.indent + p.cfg.fix_indent end
    end

    -- First pass: handle hash-part
    if not p.cfg.hide_hash then
        for k, v in pairs(adt) do

            if has_tag and k=='tag' then  -- pass the 'tag' field
            elseif type(k)=="number" and k<=alen and k>0 and math.fmod(k,1)==0 then
                -- pass array-part keys (consecutive ints less than `#adt`)
            else -- hash-part keys
                if has_hash then p:acc ", " else -- 1st hash-part pair ever found
                    p:acc "{ "; indent(p)
                end

                -- Determine whether a newline is required
                local is_id, expected_len=valid_id(p.cfg, k)
                if is_id then expected_len=#k+xlen(v, p.cfg, p.nested)+#" = , "
                else expected_len = xlen(k, p.cfg, p.nested)+xlen(v, p.cfg, p.nested)+#"[] = , " end
                consider_newline(p, expected_len)

                -- Print the key
                if is_id then p:acc(k); p:acc " = " else
                    p:acc "["; acc_value (p, k); p:acc "] = "
                end

                acc_value (p, v) -- Print the value
                has_hash = true
            end
        end
    end

    -- Now we know whether there's a hash-part, an array-part, and a tag.
    -- Tag and hash-part are already printed if they're present.
    if not has_tag and not has_hash and not has_arr then p:acc "{ }";
    elseif has_tag and not has_hash and not has_arr then -- nothing, tag already in acc
    else
        assert (has_hash or has_arr) -- special case { } already handled
        local no_brace = false
        if has_hash and has_arr then p:acc ", "
        elseif has_tag and not has_hash and alen==1 and type(adt[1])~="table" then
            -- No brace required; don't print "{", remember not to print "}"
            p:acc (" "); acc_value (p, adt[1]) -- indent= indent+(cfg.fix_indent or 0))
            no_brace = true
        elseif not has_hash then
            -- Braces required, but not opened by hash-part handler yet
            p:acc "{ "; indent(p)
        end

        -- 2nd pass: array-part
        if not no_brace and has_arr then
            local expected_len = xlen(adt[1], p.cfg, p.nested)
            consider_newline(p, expected_len)
            acc_value(p, adt[1]) -- indent+(cfg.fix_indent or 0)
            for i=2, alen do
                p:acc ", ";
                consider_newline(p, xlen(adt[i], p.cfg, p.nested))
                acc_value (p, adt[i]) --indent+(cfg.fix_indent or 0)
            end
        end
        if not no_brace then p:acc " }" end
    end
    p.nested[adt] = false -- No more nested calls
    p.indent = previous_indent
end


function acc_value(p, v)
    local custom_string = __tostring(v)
    if custom_string then p:acc(custom_string) else
        local f = acc_type[type(v)]
        if f then f(p, v) else p:acc(tostring(v)) end
    end
end


-- FIXME: new_indent seems to be always nil?!s detection
-- FIXME: accumulator function should be configurable,
-- so that print() doesn't need to bufferize the whole string
-- before starting to print.
function M.tostring(t, cfg)

    cfg = cfg or M.DEFAULT_CFG or { }

    local p = {
        cfg = cfg;
        indent = 0;
        current_offset = cfg.initial_indent or 0;
        buffer = { };
        nested = { };
        acc = function(self, str)
                  table.insert(self.buffer, str)
                  self.current_offset = self.current_offset + #str
              end;
    }
    acc_value(p, t)
    return table.concat(p.buffer)
end

function M.print(...) return print(M.tostring(...)) end
function M.sprintf(fmt, ...)
    local args={...}
    for i, v in pairs(args) do
        local t=type(v)
        if t=='table' then args[i]=M.tostring(v)
        elseif t=='nil' then args[i]='nil' end
    end
    return string.format(fmt, unpack(args))
end

function M.printf(...) print(M.sprintf(...)) end

return M