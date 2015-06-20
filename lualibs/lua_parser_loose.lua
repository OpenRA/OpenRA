--[[
 lua_parser_loose.lua.
 Loose parsing of Lua code.  See README.
 (c) 2013 David Manura. MIT License.
--]]

local PARSE = {}

local unpack = table.unpack or unpack
local LEX = require 'lua_lexer_loose'

--[[
 Loose parser.

 lx - lexer stream of Lua tokens.
 f(event...) - callback function to send events to.

 Events generated:
   'Var', name, lineinfo - variable declaration that immediately comes into scope.
   'VarSelf', name, lineinfo - same as 'Var' but for implicit 'self' parameter
     in method definitions.  lineinfo is zero-width space after '('
   'VarNext', name, lineinfo - variable definition that comes into scope
     upon next statement.
   'VarInside', name, lineinfo - variable definition that comes into scope
     inside following block.  Used for control variables in 'for' statements.
   'Id', name, lineinfo - reference to variable.
   'String', name - string or table field.
   'Scope', opt - beginning of scope block.
   'EndScope', nil, lineinfo - end of scope block.
   'FunctionCall', name, lineinfo - function call (in addition to other events).
   'Function', name, lineinfo - function definition.
--]]
function PARSE.parse_scope(lx, f, level)
  local cprev = {tag='Eof'}
  
  -- stack of scopes.
  local scopes = {{}}
  for l = 2, (level or 1) do scopes[l] = {} end
  
  local function scope_begin(opt, lineinfo, nobreak)
    scopes[#scopes+1] = {}
    f('Scope', opt, lineinfo, nobreak)
  end
  local function scope_end(opt, lineinfo)
    local scope = #scopes
    if scope > 1 then table.remove(scopes) end
    local inside_local = false
    for scope = scope-1, 1, -1 do
      if scopes[scope].inside_local then inside_local = true; break end
    end
    f('EndScope', opt, lineinfo, inside_local)
  end
  
  local function parse_function_list(has_self, name, pos)
    local c = lx:next(); assert(c[1] == '(')
    f('Statement', c[1], c.lineinfo, true) -- generate Statement for function definition
    scope_begin(c[1], c.lineinfo, true)

    local vars = {} -- accumulate vars (if any) to send after 'Function'
    if has_self then
      local lineinfo = c.lineinfo+1 -- zero size
      table.insert(vars, {'VarSelf', 'self', lineinfo, true})
    end
    while true do
      local n = lx:peek()
      if not (n.tag == 'Id' or n.tag == 'Keyword' and n[1] == '...') then break end
      local c = lx:next()
      if c.tag == 'Id' then table.insert(vars, {'Var', c[1], c.lineinfo, true}) end
      -- ignore '...' in this case
      if lx:peek()[1] == ',' then lx:next() end
    end
    if lx:peek()[1] == ')' then
      lx:next()
      f('Function', name, pos or c.lineinfo, true)
    end
    for _, var in ipairs(vars) do f(unpack(var)) end
  end
  
  while true do
    local c = lx:next()

    -- Detect end of previous statement
    if c.tag == 'Eof' -- trigger 'Statement' at the end of file
    or c.tag == 'Keyword' and (
       c[1] == 'break' or c[1] == 'goto' or c[1] == 'do' or c[1] == 'while' or
       c[1] == 'repeat' or c[1] == 'if' or c[1] == 'for' or c[1] == 'function' and lx:peek().tag == 'Id' or
       c[1] == 'local' or c[1] == ';' or c[1] == 'until' or c[1] == 'return' or c[1] == 'end') or
       c.tag == 'Id' and
           (cprev.tag == 'Id' or
            cprev.tag == 'Keyword' and
               (cprev[1] == ']' or cprev[1] == ')' or cprev[1] == '}' or
                cprev[1] == '...' or cprev[1] == 'end' or
                cprev[1] == 'true' or cprev[1] == 'false' or
                cprev[1] == 'nil') or
            cprev.tag == 'Number' or cprev.tag == 'String')
    then
      if scopes[#scopes].inside_until then scope_end(nil, c.lineinfo) end
      local scope = #scopes
      if not scopes[scope].inside_table then scopes[scope].inside_local = nil end
      f('Statement', c[1], c.lineinfo,
        scopes[scope].inside_local or c[1] == 'local' or c[1] == 'function' or c[1] == 'end')
    end

    if c.tag == 'Eof' then break end
    
    -- Process token(s)
    if c.tag == 'Keyword' then
    
      if c[1] == 'local' and lx:peek().tag == 'Keyword' and lx:peek()[1] == 'function' then
        -- local function
        local c = lx:next(); assert(c[1] == 'function')
        if lx:peek().tag == 'Id' then
          c = lx:next()
          f('Var', c[1], c.lineinfo, true)
          if lx:peek()[1] == '(' then parse_function_list(nil, c[1], c.lineinfo) end
        end
      elseif c[1] == 'function' then
        if lx:peek()[1] == '(' then -- inline function
          parse_function_list()
        elseif lx:peek().tag == 'Id' then -- function definition statement
          c = lx:next(); assert(c.tag == 'Id')
          local name = c[1]
          local pos = c.lineinfo
          f('Id', name, pos, true)
          local has_self
          while lx:peek()[1] ~= '(' and lx:peek().tag ~= 'Eof' do
            c = lx:next()
            name = name .. c[1]
            if c.tag == 'Id' then
              f('String', c[1], c.lineinfo, true)
            elseif c.tag == 'Keyword' and c[1] == ':' then
              has_self = true
            end
          end
          if lx:peek()[1] == '(' then parse_function_list(has_self, name, pos) end
        end
      elseif c[1] == 'local' and lx:peek().tag == 'Id' then
        scopes[#scopes].inside_local = true
        c = lx:next()
        f('VarNext', c[1], c.lineinfo, true)
        while lx:peek().tag == 'Keyword' and lx:peek()[1] == ',' do
          c = lx:next(); if lx:peek().tag ~= 'Id' then break end
          c = lx:next()
          f('VarNext', c[1], c.lineinfo, true)
        end
      elseif c[1] == 'for' and lx:peek().tag == 'Id' then
        c = lx:next()
        f('VarInside', c[1], c.lineinfo, true)
        while lx:peek().tag == 'Keyword' and lx:peek()[1] == ',' do
          c = lx:next(); if lx:peek().tag ~= 'Id' then break end
          c = lx:next()
          f('VarInside', c[1], c.lineinfo, true)
        end
      elseif c[1] == 'goto' and lx:peek().tag == 'Id' then
        lx:next()
      elseif c[1] == 'do' then
        scope_begin('do', c.lineinfo)
        -- note: do/while/for statement scopes all begin at 'do'.
      elseif c[1] == 'repeat' or c[1] == 'then' then
        scope_begin(c[1], c.lineinfo)
      elseif c[1] == 'end' or c[1] == 'elseif' then
        scope_end(c[1], c.lineinfo)
      elseif c[1] == 'else' then
        scope_end(nil, c.lineinfo)
        scope_begin(c[1], c.lineinfo)
      elseif c[1] == 'until' then
        scopes[#scopes].inside_until = true
      elseif c[1] == '{' then
        scopes[#scopes].inside_table = (scopes[#scopes].inside_table or 0) + 1
      elseif c[1] == '}' then
        local newval = (scopes[#scopes].inside_table or 0) - 1
        newval = newval >= 1 and newval or nil
        scopes[#scopes].inside_table = newval
      end
    elseif c.tag == 'Id' then
      local scope = #scopes
      local inside_local = scopes[scope].inside_local ~= nil
      local inside_table = scopes[scope].inside_table
      local cnext = lx:peek()
      if cnext.tag == 'Keyword' and (cnext[1] == '(' or cnext[1] == '{')
      or cnext.tag == 'String' then
        f('FunctionCall', c[1], c.lineinfo, inside_local)
      end
      -- either this is inside a table or it continues from a comma,
      -- which may be a field assignment, so assume it's in a table
      if (inside_table or cprev[1] == ',') and cnext.tag == 'Keyword' and cnext[1] == '=' then
        -- table field; table fields are tricky to handle during incremental
        -- processing as "a = 1" may be either an assignment (in which case
        -- 'a' is Id) or a field initialization (in which case it's a String).
        -- Since it's not possible to decide between two cases in isolation,
        -- this is not a good place to insert a break; instead, the break is
        -- inserted at the location of the previous keyword, which allows
        -- to properly handle those cases. The desired location of
        -- the restart point is returned as the `nobreak` value.
        f('String', c[1], c.lineinfo,
          inside_local or cprev and cprev.tag == 'Keyword' and cprev.lineinfo)
      elseif cprev.tag == 'Keyword' and (cprev[1] == ':' or cprev[1] == '.') then
        f('String', c[1], c.lineinfo, true)
      else
        f('Id', c[1], c.lineinfo, true)
        -- this looks like the left side of (multi-variable) assignment
        -- unless it's a part of `= var, field = value`, so skip if inside a table
        if not inside_table and not (cprev and cprev.tag == 'Keyword' and cprev[1] == '=') then
          while lx:peek().tag == 'Keyword' and lx:peek()[1] == ',' do
            local c = lx:next(); if lx:peek().tag ~= 'Id' then break end
            c = lx:next()
            f('Id', c[1], c.lineinfo, true)
          end
        end
      end
    end
    
    if c.tag ~= 'Comment' then cprev = c end
  end
end

--[[
  This is similar to parse_scope but determines if variables are local or global.

  lx - lexer stream of Lua tokens.
  f(event...) - callback function to send events to.
  
  Events generated:
    'Id', name, lineinfo, 'local'|'global'
     (plus all events in parse_scope)
--]]
function PARSE.parse_scope_resolve(lx, f, vars)
  local NEXT = {}   -- unique key
  local INSIDE = {} -- unique key
  local function newscope(vars, opt, lineinfo)
    local newvars = opt=='do' and vars[INSIDE] or {}
    if newvars == vars[INSIDE] then vars[INSIDE] = false end
    newvars[INSIDE]=false
    newvars[NEXT]=false
    local level = (vars[0] or 0) + 1
    newvars[0] = level -- keep the current level
    newvars[-1] = lineinfo -- keep the start of the scope
    newvars[level] = newvars -- reference the current vars table
    return setmetatable(newvars, {__index=vars})
  end
  
  vars = vars or newscope({[0] = 0}, nil, 1)
  vars[NEXT] = false -- vars that come into scope upon next statement
  vars[INSIDE] = false -- vars that come into scope upon entering block
  PARSE.parse_scope(lx, function(op, name, lineinfo, nobreak)
    -- in some (rare) cases VarNext can follow Statement event (which copies
    -- vars[NEXT]). This may cause vars[0] to be `nil`, so default to 1.
    local var = op:find("^Var") and
      {fpos = lineinfo, at = (vars[0] or 1) + (op == 'VarInside' and 1 or 0),
       masked = vars[name], self = (op == 'VarSelf') or nil } or nil
    if op == 'Var' or op == 'VarSelf' then
      vars[name] = var
    elseif op == 'VarNext' then
      vars[NEXT] = vars[NEXT] or {}
      vars[NEXT][name] = var
    elseif op == 'VarInside' then
      vars[INSIDE] = vars[INSIDE] or {}
      vars[INSIDE][name] = var
    elseif op == 'Scope' then
      vars = newscope(vars, name, lineinfo)
    elseif op == 'EndScope' then
      local mt = getmetatable(vars)
      if mt ~= nil then vars = mt.__index end
    elseif op == 'Id'
    or op == 'String' or op == 'FunctionCall' or op == 'Function' then
      -- Just make callback
    elseif op == 'Statement' then -- beginning of statement
      -- Apply vars that come into scope upon beginning of statement.
      if vars[NEXT] then
        for k,v in pairs(vars[NEXT]) do
          vars[k] = v; vars[NEXT][k] = nil
        end
      end
    else
      assert(false)
    end
    f(op, name, lineinfo, vars, nobreak)
  end, vars[0])
end

function PARSE.extract_vars(code, f)
  local lx = LEX.lexc(code)
  
  local char0 = 1  -- next char offset to write
  local function gen(char1, nextchar0)
    char0 = nextchar0
  end
  
  PARSE.parse_scope_resolve(lx, function(op, name, lineinfo, other)
    if op == 'Id' then
      f('Id', name, other, lineinfo)
    elseif op == 'Var' or op == 'VarNext' or op == 'VarInside' then
      gen(lineinfo, lineinfo+#name)
      f('Var', name, "local", lineinfo)
    end  -- ignore 'VarSelf' and others
  end)
  gen(#code+1, nil)
end

--[[
  Converts 5.2 code to 5.1 style code with explicit _ENV variables.
  Example: "function f(_ENV, x) print(x, y)" -->
            "function _ENV.f(_ENV, x) _ENV.print(x, _ENV.y) end"

  code - string of Lua code.  Assumed to be valid Lua (FIX: 5.1 or 5.2?)
  f(s) - call back function to send chunks of Lua code output to.  Example: io.stdout.
--]]
function PARSE.replace_env(code, f)
  if not f then return PARSE.accumulate(PARSE.replace_env, code) end
  PARSE.extract_vars(code, function(op, name, other)
    if op == 'Id' then
      f(other == 'global' and '_ENV.' .. name or name)
    elseif op == 'Var' or op == 'Other' then
      f(name)
    end
  end)
end

-- helper function.  Can be passed as argument `f` to functions
-- like `replace_env` above to accumulate fragments into a single string.
function PARSE.accumulator()
  local ts = {}
  local mt = {}
  mt.__index = mt
  function mt:__call(s) ts[#ts+1] = s end
  function mt:result() return table.concat(ts) end
  return setmetatable({}, mt)
end

-- helper function
function PARSE.accumulate(g, code)
  local accum = PARSE.accumulator()
  g(code, accum)
  return accum:result()
end

return PARSE
