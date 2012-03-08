----------------------------------------------------------------------
-- Metalua:  $Id: mlp_misc.lua,v 1.6 2006/11/15 09:07:50 fab13n Exp $
--
-- Summary: metalua parser, miscellaneous utility functions.
--
----------------------------------------------------------------------
--
-- Copyright (c) 2006, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------
-- History:
-- $Log: mlp_misc.lua,v $
-- Revision 1.6  2006/11/15 09:07:50  fab13n
-- debugged meta operators.
-- Added command line options handling.
--
-- Revision 1.5  2006/11/10 02:11:17  fab13n
-- compiler faithfulness to 5.1 improved
-- gg.expr extended
-- mlp.expr refactored
--
-- Revision 1.4  2006/11/09 09:39:57  fab13n
-- some cleanup
--
-- Revision 1.3  2006/11/07 04:38:00  fab13n
-- first bootstrapping version.
--
-- Revision 1.2  2006/11/05 15:08:34  fab13n
-- updated code generation, to be compliant with 5.1
--
----------------------------------------------------------------------

--------------------------------------------------------------------------------
--
-- Exported API:
-- * [mlp.fget()]
-- * [mlp.id()]
-- * [mlp.opt_id()]
-- * [mlp.id_list()]
-- * [mlp.gensym()]
-- * [mlp.string()]
-- * [mlp.opt_string()]
-- * [mlp.id2string()]
--
--------------------------------------------------------------------------------

--require "gg"
--require "mll"

module ("mlp", package.seeall)

--------------------------------------------------------------------------------
-- returns a function that takes the [n]th element of a table.
-- if [tag] is provided, then this element is expected to be a
-- table, and this table receives a "tag" field whose value is
-- set to [tag].
--
-- The primary purpose of this is to generate builders for
-- grammar generators. It has little purpose in metalua, as lambda has
-- a lightweight syntax.
--------------------------------------------------------------------------------

function fget (n, tag) 
   assert (type (n) == "number")
   if tag then
      assert (type (tag) == "string")
      return function (x) 
         assert (type (x[n]) == "table")       
         return {tag=tag, unpack(x[n])} end 
   else
      return function (x) return x[n] end 
   end
end


--------------------------------------------------------------------------------
-- Try to read an identifier (possibly as a splice), or return [false] if no
-- id is found.
--------------------------------------------------------------------------------
function opt_id (lx)
   local a = lx:peek();
   if lx:is_keyword (a, "-{") then
      local v = gg.sequence{ "-{", splice_content, "}" } (lx) [1]
      if v.tag ~= "Id" and v.tag ~= "Splice" then
         gg.parse_error(lx,"Bad id splice")
      end
      return v
   elseif a.tag == "Id" then return lx:next()
   else return false end
end

--------------------------------------------------------------------------------
-- Mandatory reading of an id: causes an error if it can't read one.
--------------------------------------------------------------------------------
function id (lx)
   return opt_id (lx) or gg.parse_error(lx,"Identifier expected")
end

--------------------------------------------------------------------------------
-- Common helper function
--------------------------------------------------------------------------------
id_list = gg.list { primary = mlp.id, separators = "," }

--------------------------------------------------------------------------------
-- Symbol generator: [gensym()] returns a guaranteed-to-be-unique identifier.
-- The main purpose is to avoid variable capture in macros.
--
-- If a string is passed as an argument, theis string will be part of the
-- id name (helpful for macro debugging)
--------------------------------------------------------------------------------
local gensymidx = 0

function gensym (arg)
   gensymidx = gensymidx + 1
   return { tag="Id", _G.string.format(".%i.%s", gensymidx, arg or "")}
end

--------------------------------------------------------------------------------
-- Converts an identifier into a string. Hopefully one day it'll handle
-- splices gracefully, but that proves quite tricky.
--------------------------------------------------------------------------------
function id2string (id)
   --print("id2string:", disp.ast(id))
   if id.tag == "Id" then id.tag = "String"; return id
   elseif id.tag == "Splice" then
      assert (in_a_quote, "can't do id2string on an outermost splice")
      error ("id2string on splice not implemented")
      -- Evaluating id[1] will produce `Id{ xxx },
      -- and we want it to produce `String{ xxx }
      -- Morally, this is what I want:
      -- return `String{ `Index{ `Splice{ id[1] }, `Number 1 } }
      -- That is, without sugar:
      return {tag="String",  {tag="Index", {tag="Splice", id[1] }, 
                                           {tag="Number", 1 } } }
   else error ("Identifier expected: ".._G.table.tostring(id, 'nohash')) end
end

--------------------------------------------------------------------------------
-- Read a string, possibly spliced, or return an error if it can't
--------------------------------------------------------------------------------
function string (lx)
   local a = lx:peek()
   if lx:is_keyword (a, "-{") then
      local v = gg.sequence{ "-{", splice_content, "}" } (lx) [1]
      if v.tag ~= "" and v.tag ~= "Splice" then
         gg.parse_error(lx,"Bad string splice")
      end
      return v
   elseif a.tag == "String" then return lx:next()
   else error "String expected" end
end

--------------------------------------------------------------------------------
-- Try to read a string, or return false if it can't. No splice allowed.
--------------------------------------------------------------------------------
function opt_string (lx)
   return lx:peek().tag == "String" and lx:next()
end
   
--------------------------------------------------------------------------------
-- Chunk reader: block + Eof
--------------------------------------------------------------------------------
function skip_initial_sharp_comment (lx)
   -- Dirty hack: I'm happily fondling lexer's private parts
   -- FIXME: redundant with lexer:newstream()
   lx :sync()
   local i = lx.src:match ("^#.-\n()", lx.i)
   if i then lx.i, lx.column_offset, lx.line = i, i, lx.line+1 end
end

local function _chunk (lx)
   if lx:peek().tag == 'Eof' then return { } -- handle empty files
   else 
      skip_initial_sharp_comment (lx)
      local chunk = block (lx)
      if lx:peek().tag ~= "Eof" then error "End-of-file expected" end
      return chunk
   end
end

-- chunk is wrapped in a sequence so that it has a "transformer" field.
chunk = gg.sequence { _chunk, builder = unpack }