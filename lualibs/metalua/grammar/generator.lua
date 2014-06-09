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

--------------------------------------------------------------------------------
--
-- Summary: parser generator. Collection of higher order functors,
--   which allow to build and combine parsers. Relies on a lexer
--   that supports the same API as the one exposed in mll.lua.
--
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
--
-- Exported API:
--
-- Parser generators:
-- * [gg.sequence()]
-- * [gg.multisequence()]
-- * [gg.expr()]
-- * [gg.list()]
-- * [gg.onkeyword()]
-- * [gg.optkeyword()]
--
-- Other functions:
-- * [gg.parse_error()]
-- * [gg.make_parser()]
-- * [gg.is_parser()]
--
--------------------------------------------------------------------------------

local M = { }

local lexer = require 'metalua.grammar.lexer'

--------------------------------------------------------------------------------
-- Symbol generator: [gensym()] returns a guaranteed-to-be-unique identifier.
-- The main purpose is to avoid variable capture in macros.
--
-- If a string is passed as an argument, theis string will be part of the
-- id name (helpful for macro debugging)
--------------------------------------------------------------------------------
local gensymidx = 0

function M.gensym (arg)
   gensymidx = gensymidx + 1
   return { tag="Id", string.format(".%i.%s", gensymidx, arg or "")}
end


-------------------------------------------------------------------------------
-- parser metatable, which maps __call to method parse, and adds some
-- error tracing boilerplate.
-------------------------------------------------------------------------------
local parser_metatable = { }

function parser_metatable :__call (lx, ...)
    return self :parse (lx, ...)
end

-------------------------------------------------------------------------------
-- Turn a table into a parser, mainly by setting the metatable.
-------------------------------------------------------------------------------
function M.make_parser(kind, p)
   p.kind = kind
   if not p.transformers then p.transformers = { } end
   function p.transformers:add (x)
      table.insert (self, x)
   end
   setmetatable (p, parser_metatable)
   return p
end

-------------------------------------------------------------------------------
-- Return true iff [x] is a parser.
-- If it's a gg-generated parser, return the name of its kind.
-------------------------------------------------------------------------------
function M.is_parser (x)
   return type(x)=="function" or getmetatable(x)==parser_metatable and x.kind
end

-------------------------------------------------------------------------------
-- Parse a sequence, without applying builder nor transformers.
-------------------------------------------------------------------------------
local function raw_parse_sequence (lx, p)
    local r = { }
    for i=1, #p do
        local e=p[i]
        if type(e) == "string" then
            local kw = lx :next()
            if not lx :is_keyword (kw, e) then
                M.parse_error(
                    lx, "A keyword was expected, probably `%s'.", e)
            end
        elseif M.is_parser (e) then
            table.insert (r, e(lx))
        else -- Invalid parser definition, this is *not* a parsing error
            error(string.format(
                      "Sequence `%s': element #%i is neither a string nor a parser: %s",
                      p.name, i, table.tostring(e)))
        end
    end
    return r
end

-------------------------------------------------------------------------------
-- Parse a multisequence, without applying multisequence transformers.
-- The sequences are completely parsed.
-------------------------------------------------------------------------------
local function raw_parse_multisequence (lx, sequence_table, default)
   local seq_parser = sequence_table[lx:is_keyword(lx:peek())]
   if seq_parser  then return seq_parser (lx)
   elseif default then return default (lx)
   else return false end
end

-------------------------------------------------------------------------------
-- Applies all transformers listed in parser on ast.
-------------------------------------------------------------------------------
local function transform (ast, parser, fli, lli)
   if parser.transformers then
      for _, t in ipairs (parser.transformers) do ast = t(ast) or ast end
   end
   if type(ast) == 'table' then
      local ali = ast.lineinfo
      if not ali or ali.first~=fli or ali.last~=lli then
         ast.lineinfo = lexer.new_lineinfo(fli, lli)
      end
   end
   return ast
end

-------------------------------------------------------------------------------
-- Generate a tracable parsing error (not implemented yet)
-------------------------------------------------------------------------------
function M.parse_error(lx, fmt, ...)
   local li = lx:lineinfo_left()
   local file, line, column, offset, positions
   if li then
      file, line, column, offset = li.source, li.line, li.column, li.offset
      positions = { first = li, last = li }
   else
      line, column, offset = -1, -1, -1
   end

   local msg  = string.format("line %i, char %i: "..fmt, line, column, ...)
   if file and file~='?' then msg = "file "..file..", "..msg end

   local src = lx.src
   if offset>0 and src then
      local i, j = offset, offset
      while src:sub(i,i) ~= '\n' and i>=0    do i=i-1 end
      while src:sub(j,j) ~= '\n' and j<=#src do j=j+1 end
      local srcline = src:sub (i+1, j-1)
      local idx  = string.rep (" ", column).."^"
      msg = string.format("%s\n>>> %s\n>>> %s", msg, srcline, idx)
   end
   --lx :kill()
   error(msg)
end

-------------------------------------------------------------------------------
--
-- Sequence parser generator
--
-------------------------------------------------------------------------------
-- Input fields:
--
-- * [builder]: how to build an AST out of sequence parts. let [x] be the list
--   of subparser results (keywords are simply omitted). [builder] can be:
--    - [nil], in which case the result of parsing is simply [x]
--    - a string, which is then put as a tag on [x]
--    - a function, which takes [x] as a parameter and returns an AST.
--
-- * [name]: the name of the parser. Used for debug messages
--
-- * [transformers]: a list of AST->AST functions, applied in order on ASTs
--   returned by the parser.
--
-- * Table-part entries corresponds to keywords (strings) and subparsers
--   (function and callable objects).
--
-- After creation, the following fields are added:
-- * [parse] the parsing function lexer->AST
-- * [kind] == "sequence"
-- * [name] is set, if it wasn't in the input.
--
-------------------------------------------------------------------------------
function M.sequence (p)
   M.make_parser ("sequence", p)

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p:parse (lx)

      -- Raw parsing:
      local fli = lx:lineinfo_right()
      local seq = raw_parse_sequence (lx, self)
      local lli = lx:lineinfo_left()

      -- Builder application:
      local builder, tb = self.builder, type (self.builder)
      if tb == "string" then seq.tag = builder
      elseif tb == "function" or builder and builder.__call then seq = builder(seq)
      elseif builder == nil then -- nothing
      else error ("Invalid builder of type "..tb.." in sequence") end
      seq = transform (seq, self, fli, lli)
      assert (not seq or seq.lineinfo)
      return seq
   end

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   -- Try to build a proper name
   if p.name then
      -- don't touch existing name
   elseif type(p[1])=="string" then -- find name based on 1st keyword
      if #p==1 then p.name=p[1]
      elseif type(p[#p])=="string" then
         p.name = p[1] .. " ... " .. p[#p]
      else p.name = p[1] .. " ..." end
   else -- can't find a decent name
      p.name = "unnamed_sequence"
   end

   return p
end --</sequence>


-------------------------------------------------------------------------------
--
-- Multiple, keyword-driven, sequence parser generator
--
-------------------------------------------------------------------------------
-- in [p], useful fields are:
--
-- * [transformers]: as usual
--
-- * [name]: as usual
--
-- * Table-part entries must be sequence parsers, or tables which can
--   be turned into a sequence parser by [gg.sequence]. These
--   sequences must start with a keyword, and this initial keyword
--   must be different for each sequence.  The table-part entries will
--   be removed after [gg.multisequence] returns.
--
-- * [default]: the parser to run if the next keyword in the lexer is
--   none of the registered initial keywords. If there's no default
--   parser and no suitable initial keyword, the multisequence parser
--   simply returns [false].
--
-- After creation, the following fields are added:
--
-- * [parse] the parsing function lexer->AST
--
-- * [sequences] the table of sequences, indexed by initial keywords.
--
-- * [add] method takes a sequence parser or a config table for
--   [gg.sequence], and adds/replaces the corresponding sequence
--   parser. If the keyword was already used, the former sequence is
--   removed and a warning is issued.
--
-- * [get] method returns a sequence by its initial keyword
--
-- * [kind] == "multisequence"
--
-------------------------------------------------------------------------------
function M.multisequence (p)
   M.make_parser ("multisequence", p)

   -------------------------------------------------------------------
   -- Add a sequence (might be just a config table for [gg.sequence])
   -------------------------------------------------------------------
   function p :add (s)
      -- compile if necessary:
      local keyword = type(s)=='table' and s[1]
      if type(s)=='table' and not M.is_parser(s) then M.sequence(s) end
      if M.is_parser(s)~='sequence' or type(keyword)~='string' then
         if self.default then -- two defaults
            error ("In a multisequence parser, all but one sequences "..
                   "must start with a keyword")
         else self.default = s end -- first default
     else
         if self.sequences[keyword] then -- duplicate keyword
             -- TODO: warn that initial keyword `keyword` is overloaded in multiseq
         end
         self.sequences[keyword] = s
     end
   end -- </multisequence.add>

   -------------------------------------------------------------------
   -- Get the sequence starting with this keyword. [kw :: string]
   -------------------------------------------------------------------
   function p :get (kw) return self.sequences [kw] end

   -------------------------------------------------------------------
   -- Remove the sequence starting with keyword [kw :: string]
   -------------------------------------------------------------------
   function p :del (kw)
      if not self.sequences[kw] then
          -- TODO: warn that we try to delete a non-existent entry
      end
      local removed = self.sequences[kw]
      self.sequences[kw] = nil
      return removed
   end

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p :parse (lx)
      local fli = lx:lineinfo_right()
      local x = raw_parse_multisequence (lx, self.sequences, self.default)
      local lli = lx:lineinfo_left()
      return transform (x, self, fli, lli)
   end

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   -- Register the sequences passed to the constructor. They're going
   -- from the array part of the parser to the hash part of field
   -- [sequences]
   p.sequences = { }
   for i=1, #p do p :add (p[i]); p[i] = nil end

   -- FIXME: why is this commented out?
   --if p.default and not is_parser(p.default) then sequence(p.default) end
   return p
end --</multisequence>


-------------------------------------------------------------------------------
--
-- Expression parser generator
--
-------------------------------------------------------------------------------
--
-- Expression configuration relies on three tables: [prefix], [infix]
-- and [suffix]. Moreover, the primary parser can be replaced by a
-- table: in this case the [primary] table will be passed to
-- [gg.multisequence] to create a parser.
--
-- Each of these tables is a modified multisequence parser: the
-- differences with respect to regular multisequence config tables are:
--
-- * the builder takes specific parameters:
--   - for [prefix], it takes the result of the prefix sequence parser,
--     and the prefixed expression
--   - for [infix], it takes the left-hand-side expression, the results
--     of the infix sequence parser, and the right-hand-side expression.
--   - for [suffix], it takes the suffixed expression, and the result
--     of the suffix sequence parser.
--
-- * the default field is a list, with parameters:
--   - [parser] the raw parsing function
--   - [transformers], as usual
--   - [prec], the operator's precedence
--   - [assoc] for [infix] table, the operator's associativity, which
--     can be "left", "right" or "flat" (default to left)
--
-- In [p], useful fields are:
-- * [transformers]: as usual
-- * [name]: as usual
-- * [primary]: the atomic expression parser, or a multisequence config
--   table (mandatory)
-- * [prefix]:  prefix  operators config table, see above.
-- * [infix]:   infix   operators config table, see above.
-- * [suffix]: suffix operators config table, see above.
--
-- After creation, these fields are added:
-- * [kind] == "expr"
-- * [parse] as usual
-- * each table is turned into a multisequence, and therefore has an
--   [add] method
--
-------------------------------------------------------------------------------
function M.expr (p)
   M.make_parser ("expr", p)

   -------------------------------------------------------------------
   -- parser method.
   -- In addition to the lexer, it takes an optional precedence:
   -- it won't read expressions whose precedence is lower or equal
   -- to [prec].
   -------------------------------------------------------------------
   function p :parse (lx, prec)
      prec = prec or 0

      ------------------------------------------------------
      -- Extract the right parser and the corresponding
      -- options table, for (pre|in|suff)fix operators.
      -- Options include prec, assoc, transformers.
      ------------------------------------------------------
      local function get_parser_info (tab)
         local p2 = tab :get (lx :is_keyword (lx :peek()))
         if p2 then -- keyword-based sequence found
            local function parser(lx) return raw_parse_sequence(lx, p2) end
            return parser, p2
         else -- Got to use the default parser
            local d = tab.default
            if d then return d.parse or d.parser, d
            else return false, false end
         end
      end

      ------------------------------------------------------
      -- Look for a prefix sequence. Multiple prefixes are
      -- handled through the recursive [p.parse] call.
      -- Notice the double-transform: one for the primary
      -- expr, and one for the one with the prefix op.
      ------------------------------------------------------
      local function handle_prefix ()
         local fli = lx :lineinfo_right()
         local p2_func, p2 = get_parser_info (self.prefix)
         local op = p2_func and p2_func (lx)
         if op then -- Keyword-based sequence found
            local ili = lx :lineinfo_right() -- Intermediate LineInfo
            local e = p2.builder (op, self :parse (lx, p2.prec))
            local lli = lx :lineinfo_left()
            return transform (transform (e, p2, ili, lli), self, fli, lli)
         else -- No prefix found, get a primary expression
            local e = self.primary(lx)
            local lli = lx :lineinfo_left()
            return transform (e, self, fli, lli)
         end
      end --</expr.parse.handle_prefix>

      ------------------------------------------------------
      -- Look for an infix sequence+right-hand-side operand.
      -- Return the whole binary expression result,
      -- or false if no operator was found.
      ------------------------------------------------------
      local function handle_infix (e)
         local p2_func, p2 = get_parser_info (self.infix)
         if not p2 then return false end

         -----------------------------------------
         -- Handle flattening operators: gather all operands
         -- of the series in [list]; when a different operator
         -- is found, stop, build from [list], [transform] and
         -- return.
         -----------------------------------------
         if (not p2.prec or p2.prec>prec) and p2.assoc=="flat" then
            local fli = lx:lineinfo_right()
            local pflat, list = p2, { e }
            repeat
               local op = p2_func(lx)
               if not op then break end
               table.insert (list, self:parse (lx, p2.prec))
               local _ -- We only care about checking that p2==pflat
               _, p2 = get_parser_info (self.infix)
            until p2 ~= pflat
            local e2 = pflat.builder (list)
            local lli = lx:lineinfo_left()
            return transform (transform (e2, pflat, fli, lli), self, fli, lli)

         -----------------------------------------
         -- Handle regular infix operators: [e] the LHS is known,
         -- just gather the operator and [e2] the RHS.
         -- Result goes in [e3].
         -----------------------------------------
         elseif p2.prec and p2.prec>prec or
                p2.prec==prec and p2.assoc=="right" then
            local fli = e.lineinfo.first -- lx:lineinfo_right()
            local op = p2_func(lx)
            if not op then return false end
            local e2 = self:parse (lx, p2.prec)
            local e3 = p2.builder (e, op, e2)
            local lli = lx:lineinfo_left()
            return transform (transform (e3, p2, fli, lli), self, fli, lli)

         -----------------------------------------
         -- Check for non-associative operators, and complain if applicable.
         -----------------------------------------
         elseif p2.assoc=="none" and p2.prec==prec then
            M.parse_error (lx, "non-associative operator!")

         -----------------------------------------
         -- No infix operator suitable at that precedence
         -----------------------------------------
         else return false end

      end --</expr.parse.handle_infix>

      ------------------------------------------------------
      -- Look for a suffix sequence.
      -- Return the result of suffix operator on [e],
      -- or false if no operator was found.
      ------------------------------------------------------
      local function handle_suffix (e)
         -- FIXME bad fli, must take e.lineinfo.first
         local p2_func, p2 = get_parser_info (self.suffix)
         if not p2 then return false end
         if not p2.prec or p2.prec>=prec then
            --local fli = lx:lineinfo_right()
            local fli = e.lineinfo.first
            local op = p2_func(lx)
            if not op then return false end
            local lli = lx:lineinfo_left()
            e = p2.builder (e, op)
            e = transform (transform (e, p2, fli, lli), self, fli, lli)
            return e
         end
         return false
      end --</expr.parse.handle_suffix>

      ------------------------------------------------------
      -- Parser body: read suffix and (infix+operand)
      -- extensions as long as we're able to fetch more at
      -- this precedence level.
      ------------------------------------------------------
      local e = handle_prefix()
      repeat
         local x = handle_suffix (e); e = x or e
         local y = handle_infix   (e); e = y or e
      until not (x or y)

      -- No transform: it already happened in operators handling
      return e
   end --</expr.parse>

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   if not p.primary then p.primary=p[1]; p[1]=nil end
   for _, t in ipairs{ "primary", "prefix", "infix", "suffix" } do
      if not p[t] then p[t] = { } end
      if not M.is_parser(p[t]) then M.multisequence(p[t]) end
   end
   function p:add(...) return self.primary:add(...) end
   return p
end --</expr>


-------------------------------------------------------------------------------
--
-- List parser generator
--
-------------------------------------------------------------------------------
-- In [p], the following fields can be provided in input:
--
-- * [builder]: takes list of subparser results, returns AST
-- * [transformers]: as usual
-- * [name]: as usual
--
-- * [terminators]: list of strings representing the keywords which
--   might mark the end of the list. When non-empty, the list is
--   allowed to be empty. A string is treated as a single-element
--   table, whose element is that string, e.g. ["do"] is the same as
--   [{"do"}].
--
-- * [separators]: list of strings representing the keywords which can
--   separate elements of the list. When non-empty, one of these
--   keyword has to be found between each element. Lack of a separator
--   indicates the end of the list. A string is treated as a
--   single-element table, whose element is that string, e.g. ["do"]
--   is the same as [{"do"}]. If [terminators] is empty/nil, then
--   [separators] has to be non-empty.
--
-- After creation, the following fields are added:
-- * [parse] the parsing function lexer->AST
-- * [kind] == "list"
--
-------------------------------------------------------------------------------
function M.list (p)
   M.make_parser ("list", p)

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p :parse (lx)

      ------------------------------------------------------
      -- Used to quickly check whether there's a terminator
      -- or a separator immediately ahead
      ------------------------------------------------------
      local function peek_is_in (keywords)
         return keywords and lx:is_keyword(lx:peek(), unpack(keywords)) end

      local x = { }
      local fli = lx :lineinfo_right()

      -- if there's a terminator to start with, don't bother trying
      local is_empty_list = self.terminators and (peek_is_in (self.terminators) or lx:peek().tag=="Eof")
      if not is_empty_list then
         repeat
             local item = self.primary(lx)
             table.insert (x, item) -- read one element
         until
            -- There's a separator list specified, and next token isn't in it.
            -- Otherwise, consume it with [lx:next()]
            self.separators and not(peek_is_in (self.separators) and lx:next()) or
            -- Terminator token ahead
            peek_is_in (self.terminators) or
            -- Last reason: end of file reached
            lx:peek().tag=="Eof"
      end

      local lli = lx:lineinfo_left()

      -- Apply the builder. It can be a string, or a callable value,
      -- or simply nothing.
      local b = self.builder
      if b then
         if type(b)=="string" then x.tag = b -- b is a string, use it as a tag
         elseif type(b)=="function" then x=b(x)
         else
            local bmt = getmetatable(b)
            if bmt and bmt.__call then x=b(x) end
         end
      end
      return transform (x, self, fli, lli)
   end --</list.parse>

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   if not p.primary then p.primary = p[1]; p[1] = nil end
   if type(p.terminators) == "string" then p.terminators = { p.terminators }
   elseif p.terminators and #p.terminators == 0 then p.terminators = nil end
   if type(p.separators) == "string" then p.separators = { p.separators }
   elseif p.separators and #p.separators == 0 then p.separators = nil end

   return p
end --</list>


-------------------------------------------------------------------------------
--
-- Keyword-conditioned parser generator
--
-------------------------------------------------------------------------------
--
-- Only apply a parser if a given keyword is found. The result of
-- [gg.onkeyword] parser is the result of the subparser (modulo
-- [transformers] applications).
--
-- lineinfo: the keyword is *not* included in the boundaries of the
-- resulting lineinfo. A review of all usages of gg.onkeyword() in the
-- implementation of metalua has shown that it was the appropriate choice
-- in every case.
--
-- Input fields:
--
-- * [name]: as usual
--
-- * [transformers]: as usual
--
-- * [peek]: if non-nil, the conditioning keyword is left in the lexeme
--   stream instead of being consumed.
--
-- * [primary]: the subparser.
--
-- * [keywords]: list of strings representing triggering keywords.
--
-- * Table-part entries can contain strings, and/or exactly one parser.
--   Strings are put in [keywords], and the parser is put in [primary].
--
-- After the call, the following fields will be set:
--
-- * [parse] the parsing method
-- * [kind] == "onkeyword"
-- * [primary]
-- * [keywords]
--
-------------------------------------------------------------------------------
function M.onkeyword (p)
   M.make_parser ("onkeyword", p)

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p :parse (lx)
      if lx :is_keyword (lx:peek(), unpack(self.keywords)) then
         local fli = lx:lineinfo_right()
         if not self.peek then lx:next() end
         local content = self.primary (lx)
         local lli = lx:lineinfo_left()
         local li = content.lineinfo or { }
         fli, lli = li.first or fli, li.last or lli
         return transform (content, p, fli, lli)
      else return false end
   end

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   if not p.keywords then p.keywords = { } end
   for _, x in ipairs(p) do
      if type(x)=="string" then table.insert (p.keywords, x)
      else assert (not p.primary and M.is_parser (x)); p.primary = x end
   end
   assert (next (p.keywords), "Missing trigger keyword in gg.onkeyword")
   assert (p.primary, 'no primary parser in gg.onkeyword')
   return p
end --</onkeyword>


-------------------------------------------------------------------------------
--
-- Optional keyword consummer pseudo-parser generator
--
-------------------------------------------------------------------------------
--
-- This doesn't return a real parser, just a function. That function parses
-- one of the keywords passed as parameters, and returns it. It returns
-- [false] if no matching keyword is found.
--
-- Notice that tokens returned by lexer already carry lineinfo, therefore
-- there's no need to add them, as done usually through transform() calls.
-------------------------------------------------------------------------------
function M.optkeyword (...)
   local args = {...}
   if type (args[1]) == "table" then
      assert (#args == 1)
      args = args[1]
   end
   for _, v in ipairs(args) do assert (type(v)=="string") end
   return function (lx)
      local x = lx:is_keyword (lx:peek(), unpack (args))
      if x then lx:next(); return x
      else return false end
   end
end


-------------------------------------------------------------------------------
--
-- Run a parser with a special lexer
--
-------------------------------------------------------------------------------
--
-- This doesn't return a real parser, just a function.
-- First argument is the lexer class to be used with the parser,
-- 2nd is the parser itself.
-- The resulting parser returns whatever the argument parser does.
--
-------------------------------------------------------------------------------
function M.with_lexer(new_lexer, parser)

   -------------------------------------------------------------------
   -- Most gg functions take their parameters in a table, so it's
   -- better to silently accept when with_lexer{ } is called with
   -- its arguments in a list:
   -------------------------------------------------------------------
   if not parser and #new_lexer==2 and type(new_lexer[1])=='table' then
      return M.with_lexer(unpack(new_lexer))
   end

   -------------------------------------------------------------------
   -- Save the current lexer, switch it for the new one, run the parser,
   -- restore the previous lexer, even if the parser caused an error.
   -------------------------------------------------------------------
   return function (lx)
      local old_lexer = getmetatable(lx)
      lx:sync()
      setmetatable(lx, new_lexer)
      local status, result = pcall(parser, lx)
      lx:sync()
      setmetatable(lx, old_lexer)
      if status then return result else error(result) end
   end
end

--------------------------------------------------------------------------------
--
-- Make sure a parser is used and returns successfully.
--
--------------------------------------------------------------------------------
function M.nonempty(primary)
    local p = M.make_parser('non-empty list', { primary = primary, name=primary.name })
    function p :parse (lx)
         local fli = lx:lineinfo_right()
         local content = self.primary (lx)
         local lli = lx:lineinfo_left()
         local li = content.lineinfo or { }
         fli, lli = li.first or fli, li.last or lli
         if #content == 0 then
           M.parse_error (lx, "`%s' must not be empty.", self.name or "list")
       else
           return transform (content, self, fli, lli)
       end
    end
    return p
end

local FUTURE_MT = { }
function FUTURE_MT:__tostring() return "<Proxy parser module>" end
function FUTURE_MT:__newindex(key, value) error "don't write in futures" end
function FUTURE_MT :__index (parser_name)
    return function(...)
        local p, m = rawget(self, '__path'), self.__module
        if p then for _, name in ipairs(p) do
            m=rawget(m, name)
            if not m then error ("Submodule '"..name.."' undefined") end
        end end
        local f = rawget(m, parser_name)
        if not f then error ("Parser '"..parser_name.."' undefined") end
        return f(...)
    end
end

function M.future(module, ...)
    checks('table')
    local path = ... and {...}
    if path then for _, x in ipairs(path) do 
        assert(type(x)=='string', "Bad future arg")
    end end
    local self = { __module = module,
                   __path   = path }
    return setmetatable(self, FUTURE_MT)
end

return M
