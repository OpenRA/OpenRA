--------------------------------------------------------------------------------
--
-- Non-Lua syntax extensions
--
--------------------------------------------------------------------------------

module ("mlp", package.seeall)

--------------------------------------------------------------------------------
-- Alebraic Datatypes
--------------------------------------------------------------------------------
local function adt (lx)
   local tagval = id (lx) [1]
   local tagkey = {tag="Pair", {tag="String", "tag"}, {tag="String", tagval} }
   if lx:peek().tag == "String" or lx:peek().tag == "Number" then
      return { tag="Table", tagkey, lx:next() }
   elseif lx:is_keyword (lx:peek(), "{") then
      local x = table (lx)
      _G.table.insert (x, 1, tagkey)
      return x
   else return { tag="Table", tagkey } end
end

expr:add{ "`", adt, builder = fget(1) }

--------------------------------------------------------------------------------
-- Anonymous lambda
--------------------------------------------------------------------------------
local lambda_expr = gg.sequence{ 
   "|", func_params_content, "|", expr,
   builder= function (x) 
      local li = x[2].lineinfo
      return { tag="Function", x[1], 
               { {tag="Return", x[2], lineinfo=li }, lineinfo=li } }
   end }

-- In an earlier version, lambda_expr took an expr_list rather than an expr
-- after the 2nd bar. However, it happened to be much more of a burden than an
-- help, So finally I disabled it. If you want to return several results, 
-- use the long syntax.
--------------------------------------------------------------------------------
-- local lambda_expr = gg.sequence{ 
--    "|", func_params_content, "|", expr_list,
--    builder= function (x) 
--       return {tag="Function", x[1], { {tag="Return", unpack(x[2]) } } } end }

expr:add (lambda_expr)

--------------------------------------------------------------------------------
-- Allows to write "a `f` b" instead of "f(a, b)". Taken from Haskell.
-- This is not part of Lua 5.1 syntax, so it's added to the expression
-- afterwards, so that it's easier to disable.
--------------------------------------------------------------------------------
local function expr_in_backquotes (lx) return expr(lx, 35) end

expr.infix:add{ name = "infix function", 
   "`", expr_in_backquotes, "`", prec = 35, assoc="left", 
   builder = function(a, op, b) return {tag="Call", op[1], a, b} end }


--------------------------------------------------------------------------------
-- table.override assignment
--------------------------------------------------------------------------------

mlp.lexer:add "<-"
stat.assignments["<-"] = function (a, b)
   assert( #a==1 and #b==1, "No multi-args for '<-'")                         
   return { tag="Call", { tag="Index", { tag="Id", "table" },
                                       { tag="String", "override" } },
                        a[1], b[1]} 
end

--------------------------------------------------------------------------------
-- C-style op+assignments
--------------------------------------------------------------------------------
local function op_assign(kw, op) 
   local function rhs(a, b)
      return { tag="Op", op, a, b } 
   end
   local function f(a,b) 
      return { tag="Set", a, _G.table.imap(rhs, a, b) }
   end
   mlp.lexer:add (kw)
   mlp.stat.assignments[kw] = f
end

_G.table.iforeach (op_assign, 
                {"+=", "-=", "*=", "/="},
                {"add", "sub", "mul", "div"})