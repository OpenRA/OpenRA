-- LuaInspect.globals - identifier scope analysis
-- Locates locals, globals, and their definitions.
--
-- (c) D.Manura, 2008-2010, MIT license.

-- based on http://lua-users.org/wiki/DetectingUndefinedVariables

local M = {}

--! require 'luainspect.typecheck' (context)

local LA = require "luainspect.ast"

local function definelocal(scope, name, ast)
  if scope[name] then
    scope[name].localmasked = true
    ast.localmasking = scope[name]
  end
  scope[name] = ast
  if name == '_' then ast.isignore = true end
end

-- Resolves scoping and usages of variable in AST.
-- Data Notes:
--   ast.localdefinition refers to lexically scoped definition of `Id node `ast`.
--     If ast.localdefinition == ast then ast is a "lexical definition".
--     If ast.localdefinition == nil, then variable is global.
--   ast.functionlevel is the number of functions the AST is contained in.
--     ast.functionlevel is defined iff ast is a lexical definition.
--   ast.isparam is true iff ast is a lexical definition and a function parameter.
--   ast.isset is true iff ast is a lexical definition and exists an assignment on it.
--   ast.isused is true iff ast is a lexical definition and has been referred to.
--   ast.isignore is true if local variable should be ignored (e.g. typically "_")
--   ast.localmasking - for a lexical definition, this is set to the lexical definition
--     this is masking (i.e. same name).  nil if not masking.
--   ast.localmasked - true iff lexical definition masked by another lexical definition.
--   ast.isfield is true iff `String node ast is used for field access on object,
--      e.g. x.y or x['y'].z
--   ast.previous - For `Index{o,s} or `Invoke{o,s,...}, s.previous == o
local function traverse(ast, scope, globals, level, functionlevel)
  scope = scope or {}

  local blockrecurse
  ast.level = level

  -- operations on walking down the AST
  if ast.tag == 'Local' then
    blockrecurse = 1
    -- note: apply new scope after processing values
  elseif ast.tag == 'Localrec' then
    local namelist_ast, valuelist_ast = ast[1], ast[2]
    for _,value_ast in ipairs(namelist_ast) do
      assert(value_ast.tag == 'Id')
      local name = value_ast[1]
      local parentscope = getmetatable(scope).__index
      definelocal(parentscope, name, value_ast)
      value_ast.localdefinition = value_ast
      value_ast.functionlevel = functionlevel
      value_ast.level = level+1
    end
    blockrecurse = 1
  elseif ast.tag == 'Id' then
    local name = ast[1]
    if scope[name] then
      ast.localdefinition = scope[name]
      ast.functionlevel = functionlevel
      scope[name].isused = true
    else -- global, do nothing
    end
  elseif ast.tag == 'Function' then
    local paramlist_ast, body_ast = ast[1], ast[2]
    functionlevel = functionlevel + 1
    for _,param_ast in ipairs(paramlist_ast) do
      local name = param_ast[1]
      assert(param_ast.tag == 'Id' or param_ast.tag == 'Dots')
      if param_ast.tag == 'Id' then
        definelocal(scope, name, param_ast)
        param_ast.localdefinition = param_ast
        param_ast.functionlevel = functionlevel
        param_ast.isparam = true
      end
      param_ast.level = level+1
    end
    blockrecurse = 1
  elseif ast.tag == 'Set' then
    local reflist_ast, valuelist_ast = ast[1], ast[2]
    for _,ref_ast in ipairs(reflist_ast) do
      if ref_ast.tag == 'Id' then
        local name = ref_ast[1]
        if scope[name] then
          scope[name].isset = true
        else
          if not globals[name] then
            globals[name] = {set=ref_ast}
          end
        end
      end
      ref_ast.level = level+1
    end
    --ENHANCE? We could differentiate assignments to x (which indicates that
    --  x is not const) and assignments to a member of x (which indicates that
    --  x is not a pointer to const) and assignments to any nested member of x
    --  (which indicates that x it not a transitive const).
  elseif ast.tag == 'Fornum' then
    blockrecurse = 1
  elseif ast.tag == 'Forin' then
    blockrecurse = 1
  end

  -- recurse (depth-first search down the AST)
  if ast.tag == 'Repeat' then
    local block_ast, cond_ast = ast[1], ast[2]
    local scope = scope
    for _,stat_ast in ipairs(block_ast) do
      scope = setmetatable({}, {__index = scope})
      traverse(stat_ast, scope, globals, level+1, functionlevel)
    end
    scope = setmetatable({}, {__index = scope})
    traverse(cond_ast, scope, globals, level+1, functionlevel)
  elseif ast.tag == 'Fornum' then
    local name_ast, block_ast = ast[1], ast[#ast]
    -- eval value list in current scope
    for i=2, #ast-1 do traverse(ast[i], scope, globals, level+1, functionlevel) end
    -- eval body in next scope
    local name = name_ast[1]
    definelocal(scope, name, name_ast)
    name_ast.localdefinition = name_ast
    name_ast.functionlevel = functionlevel
    traverse(block_ast, scope, globals, level+1, functionlevel)
  elseif ast.tag == 'Forin' then
    local namelist_ast, vallist_ast, block_ast = ast[1], ast[2], ast[3]
    -- eval value list in current scope
    traverse(vallist_ast, scope, globals, level+1, functionlevel)
    -- eval body in next scope
    for _,name_ast in ipairs(namelist_ast) do
      local name = name_ast[1]
      definelocal(scope, name, name_ast)
      name_ast.localdefinition = name_ast
      name_ast.functionlevel = functionlevel
      name_ast.level = level+1
    end
    traverse(block_ast, scope, globals, level+1, functionlevel)
  else -- normal
    for i,v in ipairs(ast) do
      if i ~= blockrecurse and type(v) == 'table' then
        local scope = setmetatable({}, {__index = scope})
        traverse(v, scope, globals, level+1, functionlevel)
      end
    end
  end

  -- operations on walking up the AST
  if ast.tag == 'Local' then
    -- Unlike Localrec, variables come into scope after evaluating values.
    local namelist_ast, valuelist_ast = ast[1], ast[2]
    for _,name_ast in ipairs(namelist_ast) do
      assert(name_ast.tag == 'Id')
      local name = name_ast[1]
      local parentscope = getmetatable(scope).__index
      definelocal(parentscope, name, name_ast)
      name_ast.localdefinition = name_ast
      name_ast.functionlevel = functionlevel
      name_ast.level = level+1
    end
  elseif ast.tag == 'Index' then
    if ast[2].tag == 'String' then
      ast[2].isfield = true
      ast[2].previous = ast[1]
    end
  elseif ast.tag == 'Invoke' then
    assert(ast[2].tag == 'String')
    ast[2].isfield = true
    ast[2].previous = ast[1]
  end
end

function M.globals(ast)
  -- Default list of defined variables.
  local scope = setmetatable({}, {})
  local globals = {}
  traverse(ast, scope, globals, 1, 1) -- Start check.

  return globals
end


-- Gets locals in scope of statement of block ast.  If isafter is true and ast is statement,
-- uses scope just after statement ast.
-- Assumes 'parent' attributes on ast are marked.
-- Returns table mapping name -> AST local definition.
function M.variables_in_scope(ast, isafter)
  local scope = {}
  local cast = ast
  while cast.parent do
    local midx = LA.ast_idx(cast.parent, cast)
    for idx=1,midx do
      local bast = cast.parent[idx]
      if bast.tag == 'Localrec' or bast.tag == 'Local' and (idx < midx or isafter) then
        local names_ast = bast[1]
        for bidx=1,#names_ast do
          local name_ast = names_ast[bidx]
          local name = name_ast[1]
          scope[name] = name_ast
        end
      elseif cast ~= ast and (bast.tag == 'For' or bast.tag == 'Forin' or bast.tag == 'Function') then
        local names_ast = bast[1]
        for bidx=1,#names_ast do
          local name_ast = names_ast[bidx]
          if name_ast.tag == 'Id' then  --Q: or maybe `Dots should be included
            local name = name_ast[1]
            scope[name] = name_ast
          end
        end
      end
    end
    cast = cast.parent
  end
  return scope
end


return M
