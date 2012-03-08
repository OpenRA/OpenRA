----------------------------------------------------------------------
-- Metalua:  $Id: mlp_meta.lua,v 1.4 2006/11/15 09:07:50 fab13n Exp $
--
-- Summary: Meta-operations: AST quasi-quoting and splicing
--
----------------------------------------------------------------------
--
-- Copyright (c) 2006, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------


--------------------------------------------------------------------------------
--
-- Exported API:
-- * [mlp.splice_content()]
-- * [mlp.quote_content()]
--
--------------------------------------------------------------------------------

module ("mlp", package.seeall)

--------------------------------------------------------------------------------
-- External splicing: compile an AST into a chunk, load and evaluate
-- that chunk, and replace the chunk by its result (which must also be
-- an AST).
--------------------------------------------------------------------------------

function splice (ast)
   local f = mlc.function_of_ast(ast, '=splice')
   local result=f()
   return result
end

--------------------------------------------------------------------------------
-- Going from an AST to an AST representing that AST
-- the only key being lifted in this version is ["tag"]
--------------------------------------------------------------------------------
function quote (t)
   --print("QUOTING:", _G.table.tostring(t, 60))
   local cases = { }
   function cases.table (t)
      local mt = { tag = "Table" }
      --_G.table.insert (mt, { tag = "Pair", quote "quote", { tag = "True" } })
      if t.tag == "Splice" then
         assert (#t==1, "Invalid splice")
         local sp = t[1]
         return sp
      elseif t.tag then
         _G.table.insert (mt, { tag = "Pair", quote "tag", quote (t.tag) })
      end
      for _, v in ipairs (t) do
         _G.table.insert (mt, quote(v))
      end
      return mt
   end
   function cases.number (t) return { tag = "Number", t, quote = true } end
   function cases.string (t) return { tag = "String", t, quote = true } end
   return cases [ type (t) ] (t)
end

--------------------------------------------------------------------------------
-- when this variable is false, code inside [-{...}] is compiled and
-- avaluated immediately. When it's true (supposedly when we're
-- parsing data inside a quasiquote), [-{foo}] is replaced by
-- [`Splice{foo}], which will be unpacked by [quote()].
--------------------------------------------------------------------------------
in_a_quote = false

--------------------------------------------------------------------------------
-- Parse the inside of a "-{ ... }"
--------------------------------------------------------------------------------
function splice_content (lx)
   local parser_name = "expr"
   if lx:is_keyword (lx:peek(2), ":") then
      local a = lx:next()
      lx:next() -- skip ":"
      assert (a.tag=="Id", "Invalid splice parser name")
      parser_name = a[1]
   end
   local ast = mlp[parser_name](lx)
   if in_a_quote then
      --printf("SPLICE_IN_QUOTE:\n%s", _G.table.tostring(ast, "nohash", 60))
      return { tag="Splice", ast }
   else
      if parser_name == "expr" then ast = { { tag="Return", ast } }
      elseif parser_name == "stat"  then ast = { ast }
      elseif parser_name ~= "block" then
         error ("splice content must be an expr, stat or block") end
      --printf("EXEC THIS SPLICE:\n%s", _G.table.tostring(ast, "nohash", 60))
      return splice (ast)
   end
end

--------------------------------------------------------------------------------
-- Parse the inside of a "+{ ... }"
--------------------------------------------------------------------------------
function quote_content (lx)
   local parser 
   if lx:is_keyword (lx:peek(2), ":") then -- +{parser: content }
      parser = mlp[id(lx)[1]]
      lx:next()
   else -- +{ content }
      parser = mlp.expr
   end

   local prev_iq = in_a_quote
   in_a_quote = true
   --print("IN_A_QUOTE")
   local content = parser (lx)
   local q_content = quote (content)
   in_a_quote = prev_iq
   return q_content
end

