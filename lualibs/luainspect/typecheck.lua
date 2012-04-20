-- luainspect.typecheck - Type definitions used to check LuaInspect itself.
--
-- (c) 2010 David Manura, MIT License.

local T = require "luainspect.types"

local ast_mt = {__tostring = function(s) return 'AST' end}

return function(context)
  -- AST type.
  local ast = T.table {
    tag = T.string,
    lineinfo=T.table{first=T.table{comments=T.table{T.table{T.string,T.number,T.number}},T.number,T.number,T.number,T.string},
                 ast=T.table{comments=T.table{T.table{T.string,T.number,T.number}},T.number,T.number,T.number,T.string}},
    isfield=T.boolean, tag2=T.string,
    value=T.universal, valueself=T.number, valuelist=T.table{n=T.number, isvaluepegged=T.boolean},
    resolvedname=T.string, definedglobal=T.boolean, id=T.number, isparam=T.boolean, isset=T.boolean, isused=T.boolean,
    isignore=T.boolean,
    functionlevel=T.number, localmasked=T.boolean, note=T.string, nocollect=T.table{}, isdead=T.boolean}
     -- FIX: some of these are "boolean or nil" actually
  ast.localdefinition=ast; ast.localmasking = ast
  ast.previous = ast; ast.parent = ast
  ast.seevalue = ast; ast.seenote=ast
  setmetatable(ast, ast_mt)
  
  ast[1] = ast; ast[2] = ast
  context.apply_value('ast$', ast)

  -- Token type.
  context.apply_value('token$', T.table{
    tag=T.string, fpos=T.number, lpos=T.number, keywordid=T.number, ast=ast, [1]=T.string
  })
  
  -- Lua source code string type.
  context.apply_value('src$', '')

  -- SciTE syler object type.
  local nf = function()end
  context.apply_value('^styler$', T.table{SetState=nf, More=nf, Current=nf, Forward=nf, StartStyling=nf, EndStyling=nf, language=T.string})
end
