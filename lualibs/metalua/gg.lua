----------------------------------------------------------------------
-- Metalua.
--
-- Summary: parser generator. Collection of higher order functors,
--   which allow to build and combine parsers. Relies on a lexer
--   that supports the same API as the one exposed in mll.lua.
--
----------------------------------------------------------------------
--
-- Copyright (c) 2006-2008, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------

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

module("gg", package.seeall)

-------------------------------------------------------------------------------
-- parser metatable, which maps __call to method parse, and adds some
-- error tracing boilerplate.
-------------------------------------------------------------------------------
local parser_metatable = { }
function parser_metatable.__call (parser, lx, ...)
   --printf ("Call parser %q of type %q", parser.name or "?", parser.kind)
   if mlc.metabugs then 
      return parser:parse (lx, ...) 
      --local x = parser:parse (lx, ...) 
      --printf ("Result of parser %q: %s", 
      --        parser.name or "?",
      --        _G.table.tostring(x, "nohash", 80))
      --return x
   else
      local li = lx:lineinfo_right() or { "?", "?", "?", "?" }
      local status, ast = pcall (parser.parse, parser, lx, ...)      
      if status then return ast else
         -- Try to replace the gg.lua location, in the error msg, with
         -- the place where the current parser started handling the
         -- lexstream.
         -- Since the error is rethrown, these places are stacked. 
         error (string.format ("%s\n - (l.%s, c.%s, k.%s) in parser %s", 
                               ast:match "gg.lua:%d+: (.*)" or ast,
                               li[1], li[2], li[3], parser.name or parser.kind))
      end
   end
end

-------------------------------------------------------------------------------
-- Turn a table into a parser, mainly by setting the metatable.
-------------------------------------------------------------------------------
function make_parser(kind, p)
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
function is_parser (x)
   return type(x)=="function" or getmetatable(x)==parser_metatable and x.kind
end

-------------------------------------------------------------------------------
-- Parse a sequence, without applying builder nor transformers
-------------------------------------------------------------------------------
local function raw_parse_sequence (lx, p)
   local r = { }
   for i=1, #p do
      e=p[i]
      if type(e) == "string" then 
         if not lx:is_keyword (lx:next(), e) then
            parse_error (lx, "A keyword was expected, probably `%s'.", e) end
      elseif is_parser (e) then
         table.insert (r, e (lx)) 
      else 
         gg.parse_error (lx,"Sequence `%s': element #%i is neither a string "..
                         "nor a parser: %s", 
                         p.name, i, table.tostring(e))
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
   if type(ast) == 'table'then
      local ali = ast.lineinfo
      if not ali or ali.first~=fli or ali.last~=lli then
         ast.lineinfo = { first = fli, last = lli }
      end
   end
   return ast
end

-------------------------------------------------------------------------------
-- Generate a tracable parsing error (not implemented yet)
-------------------------------------------------------------------------------
function parse_error(lx, fmt, ...)
   local li = lx:lineinfo_left() or {-1,-1,-1, "<unknown file>"}
   local msg  = string.format("line %i, char %i: "..fmt, li[1], li[2], ...)   
   local src = lx.src
   if li[3]>0 and src then
      local i, j = li[3], li[3]
      while src:sub(i,i) ~= '\n' and i>=0    do i=i-1 end
      while src:sub(j,j) ~= '\n' and j<=#src do j=j+1 end      
      local srcline = src:sub (i+1, j-1)
      local idx  = string.rep (" ", li[2]).."^"
      msg = string.format("%s\n>>> %s\n>>> %s", msg, srcline, idx)
   end
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
function sequence (p)
   make_parser ("sequence", p)

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
      p.name = "<anonymous>"
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
function multisequence (p)   
   make_parser ("multisequence", p)

   -------------------------------------------------------------------
   -- Add a sequence (might be just a config table for [gg.sequence])
   -------------------------------------------------------------------
   function p:add (s)
      -- compile if necessary:
      local keyword = type(s)=='table' and s[1]
      if type(s)=='table' and not is_parser(s) then sequence(s) end
      if is_parser(s)~='sequence' or type(keyword)~='string' then 
         if self.default then -- two defaults
            error ("In a multisequence parser, all but one sequences "..
                   "must start with a keyword")
         else self.default = s end -- first default
      elseif self.sequences[keyword] then -- duplicate keyword
         eprintf (" *** Warning: keyword %q overloaded in multisequence ***",
                  keyword)
         self.sequences[keyword] = s
      else -- newly caught keyword
         self.sequences[keyword] = s
      end
   end -- </multisequence.add>

   -------------------------------------------------------------------
   -- Get the sequence starting with this keyword. [kw :: string]
   -------------------------------------------------------------------
   function p:get (kw) return self.sequences [kw] end

   -------------------------------------------------------------------
   -- Remove the sequence starting with keyword [kw :: string]
   -------------------------------------------------------------------
   function p:del (kw) 
      if not self.sequences[kw] then 
         eprintf("*** Warning: trying to delete sequence starting "..
                 "with %q from a multisequence having no such "..
                 "entry ***", kw) end
      local removed = self.sequences[kw]
      self.sequences[kw] = nil 
      return removed
   end

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p:parse (lx)
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
   for i=1, #p do p:add (p[i]); p[i] = nil end

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
--   - for [suffix], it takes the suffixed expression, and theresult 
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
function expr (p)
   make_parser ("expr", p)

   -------------------------------------------------------------------
   -- parser method.
   -- In addition to the lexer, it takes an optional precedence:
   -- it won't read expressions whose precedence is lower or equal
   -- to [prec].
   -------------------------------------------------------------------
   function p:parse (lx, prec)
      prec = prec or 0

      ------------------------------------------------------
      -- Extract the right parser and the corresponding
      -- options table, for (pre|in|suff)fix operators.
      -- Options include prec, assoc, transformers.
      ------------------------------------------------------
      local function get_parser_info (tab)
         local p2 = tab:get (lx:is_keyword (lx:peek()))
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
         local fli = lx:lineinfo_right()
         local p2_func, p2 = get_parser_info (self.prefix)
         local op = p2_func and p2_func (lx)
         if op then -- Keyword-based sequence found
            local ili = lx:lineinfo_right() -- Intermediate LineInfo
            local e = p2.builder (op, self:parse (lx, p2.prec))
            local lli = lx:lineinfo_left()
            return transform (transform (e, p2, ili, lli), self, fli, lli)
         else -- No prefix found, get a primary expression         
            local e = self.primary(lx)
            local lli = lx:lineinfo_left()
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
            parse_error (lx, "non-associative operator!")

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
      if not is_parser(p[t]) then multisequence(p[t]) end
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
function list (p)
   make_parser ("list", p)

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p:parse (lx)

      ------------------------------------------------------
      -- Used to quickly check whether there's a terminator 
      -- or a separator immediately ahead
      ------------------------------------------------------
      local function peek_is_in (keywords) 
         return keywords and lx:is_keyword(lx:peek(), unpack(keywords)) end

      local x = { }
      local fli = lx:lineinfo_right()

      -- if there's a terminator to start with, don't bother trying
      if not peek_is_in (self.terminators) then 
         repeat table.insert (x, self.primary (lx)) -- read one element
         until
            -- First reason to stop: There's a separator list specified,
            -- and next token isn't one. Otherwise, consume it with [lx:next()]
            self.separators and not(peek_is_in (self.separators) and lx:next()) or
            -- Other reason to stop: terminator token ahead
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
-- Keyword-conditionned parser generator
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
-- * [peek]: if non-nil, the conditionning keyword is left in the lexeme
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
function onkeyword (p)
   make_parser ("onkeyword", p)

   -------------------------------------------------------------------
   -- Parsing method
   -------------------------------------------------------------------
   function p:parse(lx)
      if lx:is_keyword (lx:peek(), unpack(self.keywords)) then
         --local fli = lx:lineinfo_right()
         if not self.peek then lx:next() end
         local content = self.primary (lx)
         --local lli = lx:lineinfo_left()
         local fli, lli = content.lineinfo.first, content.lineinfo.last
         return transform (content, p, fli, lli)
      else return false end
   end

   -------------------------------------------------------------------
   -- Construction
   -------------------------------------------------------------------
   if not p.keywords then p.keywords = { } end
   for _, x in ipairs(p) do
      if type(x)=="string" then table.insert (p.keywords, x)
      else assert (not p.primary and is_parser (x)); p.primary = x end
   end
   if not next (p.keywords) then 
      eprintf("Warning, no keyword to trigger gg.onkeyword") end
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
function optkeyword (...)
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
function with_lexer(new_lexer, parser)

   -------------------------------------------------------------------
   -- Most gg functions take their parameters in a table, so it's 
   -- better to silently accept when with_lexer{ } is called with
   -- its arguments in a list:
   -------------------------------------------------------------------
   if not parser and #new_lexer==2 and type(new_lexer[1])=='table' then
      return with_lexer(unpack(new_lexer))
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
