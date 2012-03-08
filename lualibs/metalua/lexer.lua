----------------------------------------------------------------------
-- Metalua:  $Id: mll.lua,v 1.3 2006/11/15 09:07:50 fab13n Exp $
--
-- Summary: generic Lua-style lexer definition. You need this plus
-- some keyword additions to create the complete Lua lexer,
-- as is done in mlp_lexer.lua.
--
-- TODO: 
--
-- * Make it easy to define new flavors of strings. Replacing the
--   lexer.patterns.long_string regexp by an extensible list, with
--   customizable token tag, would probably be enough. Maybe add:
--   + an index of capture for the regexp, that would specify 
--     which capture holds the content of the string-like token
--   + a token tag
--   + or a string->string transformer function.
--
-- * There are some _G.table to prevent a namespace clash which has
--   now disappered. remove them.
----------------------------------------------------------------------
--
-- Copyright (c) 2006, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------

module ("lexer", package.seeall)

require 'metalua.runtime'


lexer = { alpha={ }, sym={ } }
lexer.__index=lexer

local debugf = function() end
--local debugf=printf

----------------------------------------------------------------------
-- Patterns used by [lexer:extract] to decompose the raw string into
-- correctly tagged tokens.
----------------------------------------------------------------------
lexer.patterns = {
   spaces              = "^[ \r\n\t]*()",
   short_comment       = "^%-%-([^\n]*)()\n",
   final_short_comment = "^%-%-([^\n]*)()$",
   long_comment        = "^%-%-%[(=*)%[\n?(.-)%]%1%]()",
   long_string         = "^%[(=*)%[\n?(.-)%]%1%]()",
   number_mantissa     = { "^%d+%.?%d*()", "^%d*%.%d+()" },
   number_exponant     = "^[eE][%+%-]?%d+()",
   number_hex          = "^0[xX]%x+()",
   word                = "^([%a_][%w_]*)()"
}

----------------------------------------------------------------------
-- unescape a whole string, applying [unesc_digits] and
-- [unesc_letter] as many times as required.
----------------------------------------------------------------------
local function unescape_string (s)

   -- Turn the digits of an escape sequence into the corresponding
   -- character, e.g. [unesc_digits("123") == string.char(123)].
   local function unesc_digits (backslashes, digits)
      if #backslashes%2==0 then
         -- Even number of backslashes, they escape each other, not the digits.
         -- Return them so that unesc_letter() can treaat them
         return backslashes..digits
      else
         -- Remove the odd backslash, which escapes the number sequence.
         -- The rest will be returned and parsed by unesc_letter()
         backslashes = backslashes :sub (1,-2)
      end
      local k, j, i = digits:reverse():byte(1, 3)
      local z = _G.string.byte "0"
      local code = (k or z) + 10*(j or z) + 100*(i or z) - 111*z
      if code > 255 then 
      	 error ("Illegal escape sequence '\\"..digits..
                "' in string: ASCII codes must be in [0..255]") 
      end
      return backslashes .. string.char (code)
   end

   -- Take a letter [x], and returns the character represented by the 
   -- sequence ['\\'..x], e.g. [unesc_letter "n" == "\n"].
   local function unesc_letter(x)
      local t = { 
         a = "\a", b = "\b", f = "\f",
         n = "\n", r = "\r", t = "\t", v = "\v",
         ["\\"] = "\\", ["'"] = "'", ['"'] = '"', ["\n"] = "\n" }
      return t[x] or error([[Unknown escape sequence '\]]..x..[[']])
   end

   return s
      :gsub ("(\\+)([0-9][0-9]?[0-9]?)", unesc_digits)
      :gsub ("\\(%D)",unesc_letter)
end

lexer.extractors = {
   "skip_whitespaces_and_comments",
   "extract_short_string", "extract_word", "extract_number", 
   "extract_long_string", "extract_symbol" }

lexer.token_metatable = { 
--         __tostring = function(a) 
--            return string.format ("`%s{'%s'}",a.tag, a[1]) 
--         end 
} 
      
lexer.lineinfo_metatable = { }

----------------------------------------------------------------------
-- Really extract next token fron the raw string 
-- (and update the index).
-- loc: offset of the position just after spaces and comments
-- previous_i: offset in src before extraction began
----------------------------------------------------------------------
function lexer:extract ()
   local previous_i = self.i
   local loc = self.i
   local eof, token

   -- Put line info, comments and metatable around the tag and content
   -- provided by extractors, thus returning a complete lexer token.
   -- first_line: line # at the beginning of token
   -- first_column_offset: char # of the last '\n' before beginning of token
   -- i: scans from beginning of prefix spaces/comments to end of token.
   local function build_token (tag, content)
      assert (tag and content)
      local i, first_line, first_column_offset, previous_line_length =
         previous_i, self.line, self.column_offset, nil

      -- update self.line and first_line. i := indexes of '\n' chars
      while true do
         i = self.src :find ("\n", i+1, true)
         if not i or i>self.i then break end -- no more '\n' until end of token
         previous_line_length = i - self.column_offset
         if loc and i <= loc then -- '\n' before beginning of token
            first_column_offset = i
            first_line = first_line+1 
         end
         self.line   = self.line+1 
         self.column_offset = i 
      end

      -- lineinfo entries: [1]=line, [2]=column, [3]=char, [4]=filename
      local fli = { first_line, loc-first_column_offset, loc, self.src_name }
      local lli = { self.line, self.i-self.column_offset-1, self.i-1, self.src_name }
      --Pluto barfes when the metatable is set:(
      setmetatable(fli, lexer.lineinfo_metatable)
      setmetatable(lli, lexer.lineinfo_metatable)
      local a = { tag = tag, lineinfo = { first=fli, last=lli }, content } 
      if lli[2]==-1 then lli[1], lli[2] = lli[1]-1, previous_line_length-1 end
      if #self.attached_comments > 0 then 
         a.lineinfo.comments = self.attached_comments 
         fli.comments = self.attached_comments
         if self.lineinfo_last then
            self.lineinfo_last.comments = self.attached_comments
         end
      end
      self.attached_comments = { }
      return setmetatable (a, self.token_metatable)
   end --</function build_token>

   for ext_idx, extractor in ipairs(self.extractors) do
      -- printf("method = %s", method)
      local tag, content = self [extractor] (self)
      -- [loc] is placed just after the leading whitespaces and comments;
      -- for this to work, the whitespace extractor *must be* at index 1.
      if ext_idx==1 then loc = self.i end

      if tag then 
         --printf("`%s{ %q }\t%i", tag, content, loc);
         return build_token (tag, content) 
      end
   end

   error "None of the lexer extractors returned anything!"
end   

----------------------------------------------------------------------
-- skip whites and comments
-- FIXME: doesn't take into account:
-- - unterminated long comments
-- - short comments at last line without a final \n
----------------------------------------------------------------------
function lexer:skip_whitespaces_and_comments()
   local table_insert = _G.table.insert
   repeat -- loop as long as a space or comment chunk is found
      local _, j
      local again = false
      local last_comment_content = nil
      -- skip spaces
      self.i = self.src:match (self.patterns.spaces, self.i)
      -- skip a long comment if any
      _, last_comment_content, j = 
         self.src :match (self.patterns.long_comment, self.i)
      if j then 
         table_insert(self.attached_comments, 
                         {last_comment_content, self.i, j, "long"})
         self.i=j; again=true 
      end
      -- skip a short comment if any
      last_comment_content, j = self.src:match (self.patterns.short_comment, self.i)
      if j then
         table_insert(self.attached_comments, 
                         {last_comment_content, self.i, j, "short"})
         self.i=j; again=true 
      end
      if self.i>#self.src then return "Eof", "eof" end
   until not again

   if self.src:match (self.patterns.final_short_comment, self.i) then 
      return "Eof", "eof" end
   --assert (not self.src:match(self.patterns.short_comment, self.i))
   --assert (not self.src:match(self.patterns.long_comment, self.i))
   -- --assert (not self.src:match(self.patterns.spaces, self.i))
   return
end

----------------------------------------------------------------------
-- extract a '...' or "..." short string
----------------------------------------------------------------------
function lexer:extract_short_string()
   -- [k] is the first unread char, [self.i] points to [k] in [self.src]
   local j, k = self.i, self.src :sub (self.i,self.i)
   if k~="'" and k~='"' then return end
   local i = self.i + 1
   local j = i
   while true do
      -- k = opening char: either simple-quote or double-quote
      -- i = index of beginning-of-string
      -- x = next "interesting" character
      -- j = position after interesting char
      -- y = char just after x
      local x, y
      x, j, y = self.src :match ("([\\\r\n"..k.."])()(.?)", j)
      if x == '\\' then j=j+1  -- don't parse escaped char
      elseif x == k then break -- unescaped end of string
      else -- eof or '\r' or '\n' reached before end of string
         assert (not x or x=="\r" or x=="\n")
         error "Unterminated string"
      end
   end
   self.i = j

   return "String", unescape_string (self.src:sub (i,j-2))
end

----------------------------------------------------------------------
--
----------------------------------------------------------------------
function lexer:extract_word()
   -- Id / keyword
   local word, j = self.src:match (self.patterns.word, self.i)
   if word then
      self.i = j
      if self.alpha [word] then return "Keyword", word
      else return "Id", word end
   end
end

----------------------------------------------------------------------
--
----------------------------------------------------------------------
function lexer:extract_number()
   -- Number
   local j = self.src:match(self.patterns.number_hex, self.i)
   if not j then
      j = self.src:match (self.patterns.number_mantissa[1], self.i) or
          self.src:match (self.patterns.number_mantissa[2], self.i)
      if j then
         j = self.src:match (self.patterns.number_exponant, j) or j;
      end
   end
   if not j then return end
   -- Number found, interpret with tonumber() and return it
   local n = tonumber (self.src:sub (self.i, j-1))
   self.i = j
   return "Number", n
end

----------------------------------------------------------------------
--
----------------------------------------------------------------------
function lexer:extract_long_string()
   -- Long string
   local _, content, j = self.src:match (self.patterns.long_string, self.i)
   if j then self.i = j; return "String", content end
end

----------------------------------------------------------------------
--
----------------------------------------------------------------------
function lexer:extract_symbol()
   -- compound symbol
   local k = self.src:sub (self.i,self.i)
   local symk = self.sym [k]
   if not symk then 
      self.i = self.i + 1
      return "Keyword", k
   end
   for _, sym in pairs (symk) do
      if sym == self.src:sub (self.i, self.i + #sym - 1) then 
         self.i = self.i + #sym; 
         return "Keyword", sym
      end
   end
   -- single char symbol
   self.i = self.i+1
   return "Keyword", k
end

----------------------------------------------------------------------
-- Add a keyword to the list of keywords recognized by the lexer.
----------------------------------------------------------------------
function lexer:add (w, ...)
   assert(not ..., "lexer:add() takes only one arg, although possibly a table")
   if type (w) == "table" then
      for _, x in ipairs (w) do self:add (x) end
   else
      if w:match (self.patterns.word .. "$") then self.alpha [w] = true
      elseif w:match "^%p%p+$" then 
         local k = w:sub(1,1)
         local list = self.sym [k]
         if not list then list = { }; self.sym [k] = list end
         _G.table.insert (list, w)
      elseif w:match "^%p$" then return
      else error "Invalid keyword" end
   end
end

----------------------------------------------------------------------
-- Return the [n]th next token, without consumming it.
-- [n] defaults to 1. If it goes pass the end of the stream, an EOF
-- token is returned.
----------------------------------------------------------------------
function lexer:peek (n)
   if not n then n=1 end
   if n > #self.peeked then
      for i = #self.peeked+1, n do
         self.peeked [i] = self:extract()
      end
   end
  return self.peeked [n]
end

----------------------------------------------------------------------
-- Return the [n]th next token, removing it as well as the 0..n-1
-- previous tokens. [n] defaults to 1. If it goes pass the end of the
-- stream, an EOF token is returned.
----------------------------------------------------------------------
function lexer:next (n)
   n = n or 1
   self:peek (n)
   local a
   for i=1,n do 
      a = _G.table.remove (self.peeked, 1) 
      if a then 
         --debugf ("lexer:next() ==> %s %s",
         --        table.tostring(a), tostring(a))
      end
      self.lastline = a.lineinfo.last[1]
   end
   self.lineinfo_last = a.lineinfo.last
   return a or eof_token
end

----------------------------------------------------------------------
-- Returns an object which saves the stream's current state.
----------------------------------------------------------------------
-- FIXME there are more fields than that to save
function lexer:save () return { self.i; _G.table.cat(self.peeked) } end

----------------------------------------------------------------------
-- Restore the stream's state, as saved by method [save].
----------------------------------------------------------------------
-- FIXME there are more fields than that to restore
function lexer:restore (s) self.i=s[1]; self.peeked=s[2] end

----------------------------------------------------------------------
-- Resynchronize: cancel any token in self.peeked, by emptying the
-- list and resetting the indexes
----------------------------------------------------------------------
function lexer:sync()
   local p1 = self.peeked[1]
   if p1 then 
      li = p1.lineinfo.first
      self.line, self.i = li[1], li[3]
      self.column_offset = self.i - li[2]
      self.peeked = { }
      self.attached_comments = p1.lineinfo.first.comments or { }
   end
end

----------------------------------------------------------------------
-- Take the source and offset of an old lexer.
----------------------------------------------------------------------
function lexer:takeover(old)
   self:sync()
   self.line, self.column_offset, self.i, self.src, self.attached_comments =
      old.line, old.column_offset, old.i, old.src, old.attached_comments
   return self
end

-- function lexer:lineinfo()
-- 	if self.peeked[1] then return self.peeked[1].lineinfo.first
--     else return { self.line, self.i-self.column_offset, self.i } end
-- end


----------------------------------------------------------------------
-- Return the current position in the sources. This position is between
-- two tokens, and can be within a space / comment area, and therefore
-- have a non-null width. :lineinfo_left() returns the beginning of the
-- separation area, :lineinfo_right() returns the end of that area.
--
--    ____ last consummed token    ____ first unconsummed token
--   /                            /
-- XXXXX  <spaces and comments> YYYYY
--      \____                    \____
--           :lineinfo_left()         :lineinfo_right()
----------------------------------------------------------------------
function lexer:lineinfo_right()
   return self:peek(1).lineinfo.first
end

function lexer:lineinfo_left()
   return self.lineinfo_last
end

----------------------------------------------------------------------
-- Create a new lexstream.
----------------------------------------------------------------------
function lexer:newstream (src_or_stream, name)
   name = name or "?"
   if type(src_or_stream)=='table' then -- it's a stream
      return setmetatable ({ }, self) :takeover (src_or_stream)
   elseif type(src_or_stream)=='string' then -- it's a source string
      local src = src_or_stream
      local stream = { 
         src_name      = name;   -- Name of the file
         src           = src;    -- The source, as a single string
         peeked        = { };    -- Already peeked, but not discarded yet, tokens
         i             = 1;      -- Character offset in src
         line          = 1;      -- Current line number
         column_offset = 0;      -- distance from beginning of file to last '\n'
         attached_comments = { },-- comments accumulator
         lineinfo_last = { 1, 1, 1, name }
      }
      setmetatable (stream, self)

      -- skip initial sharp-bang for unix scripts
      -- FIXME: redundant with mlp.chunk()
      if src and src :match "^#" then stream.i = src :find "\n" + 1 end
      return stream
   else
      assert(false, ":newstream() takes a source string or a stream, not a "..
                    type(src_or_stream))
   end
end

----------------------------------------------------------------------
-- if there's no ... args, return the token a (whose truth value is
-- true) if it's a `Keyword{ }, or nil.  If there are ... args, they
-- have to be strings. if the token a is a keyword, and it's content
-- is one of the ... args, then returns it (it's truth value is
-- true). If no a keyword or not in ..., return nil.
----------------------------------------------------------------------
function lexer:is_keyword (a, ...)
   if not a or a.tag ~= "Keyword" then return false end
   local words = {...}
   if #words == 0 then return a[1] end
   for _, w in ipairs (words) do
      if w == a[1] then return w end
   end
   return false
end

----------------------------------------------------------------------
-- Cause an error if the next token isn't a keyword whose content
-- is listed among ... args (which have to be strings).
----------------------------------------------------------------------
function lexer:check (...)
   local words = {...}
   local a = self:next()
   local function err ()
      error ("Got " .. tostring (a) .. 
             ", expected one of these keywords : '" ..
             _G.table.concat (words,"', '") .. "'") end
          
   if not a or a.tag ~= "Keyword" then err () end
   if #words == 0 then return a[1] end
   for _, w in ipairs (words) do
       if w == a[1] then return w end
   end
   err ()
end

----------------------------------------------------------------------
-- 
----------------------------------------------------------------------
function lexer:clone()
   local clone = {
      alpha = table.deep_copy(self.alpha),
      sym   = table.deep_copy(self.sym) }
   setmetatable(clone, self)
   clone.__index = clone
   return clone
end
