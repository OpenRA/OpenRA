-- construct proper path to load metalua modules to build
-- an abstract syntax tree (AST)
local file = debug.getinfo(1, "S").source
if string.find(file, "@") == 1 then file = string.sub(file, 2) end
package.path = string.gsub(file, "metalua%.lua$", "?.lua") .. ';' .. package.path

-- these modules are sufficient to build an AST from a source file/string
require "lexer"
require "gg"
require "mlp_lexer"
require "mlp_misc"
require "mlp_table"
require "mlp_meta"
require "mlp_expr"
require "mlp_stat"

-- these modules are needed to convert an AST into bytecode to execute
require "lcode"
require "ldump"
require "lopcodes"
require "compile"

-- this is the compiler module that builds bytecode from an AST
local mlc = { } 

function mlc.function_of_ast (ast)
   local  proto = bytecode.metalua_compile(ast)
   local  dump  = bytecode.dump_string(proto)
   local  func  = string.undump(dump) 
   return func
end

function mlc.ast_of_luastring (src, file)
   local  lx  = mlp.lexer:newstream(src, file or "(string)")
   local  ast = mlp.chunk(lx)
   return ast
end

function mlc.function_of_luastring (src, file)
   local  ast  = mlc.ast_of_luastring(src, file)
   local  func = mlc.function_of_ast(ast)
   return func
end

function mlc.function_of_luafile (name)
   local f   = io.open(name, 'r')
   local src = f:read('*a')
   f:close()
   return mlc.function_of_luastring(src, "@"..name)
end

_G.mlc = mlc

--[[ 
  -- Can be used with the following code:
  require "metalua"
  local ast = mlc.ast_of_luastring(src)
  local f = mlc.function_of_ast(ast)
  f()
]]
