-- luainspect.ast - Lua Abstract Syntax Tree (AST) and token list operations.
--
-- Two main structures are maintained.  A Metalua-style AST represents the
-- nested syntactic structure obtained from the parse.
-- A separate linear ordered list of tokens represents the syntactic structure
-- from the lexing, including line information (character positions only not row/columns),
-- comments, and keywords, which is originally built from the lineinfo attributes
-- injected by Metalua into the AST (IMPROVE: it probably would be simpler
-- to obtain this from the lexer directly rather then inferring it from the parsing).
-- During AST manipulations, the lineinfo maintained in the AST is ignored
-- because it was found more difficult to maintain and not in the optimal format.
--
-- The contained code deals with
--   - Building the AST from source.
--   - Building the tokenlist from the AST lineinfo.
--   - Querying the AST+tokenlist.
--   - Modifying the AST+tokenlist (including incremental parsing source -> AST)
--   - Annotating the AST with navigational info (e.g. parent links) to assist queries.
--   - Dumping the tokenlist for debugging.
--
-- (c) 2010 David Manura, MIT License.


--! require 'luainspect.typecheck' (context)

local mlc = require 'metalua.compiler'.new()

local M = {}

--[=TESTSUITE
-- utilities
local ops = {}
ops['=='] = function(a,b) return a == b end
local function check(opname, a, b)
  local op = assert(ops[opname])
  if not op(a,b) then
    error("fail == " .. tostring(a) .. " " .. tostring(b))
  end
end
--]=]

-- CATEGORY: debug
local function DEBUG(...)
  if LUAINSPECT_DEBUG then
    print('DEBUG:', ...)
  end
end


-- Converts character position to row,column position in string src.
-- Add values are 1-indexed.
function M.pos_to_linecol(pos, src)
  local linenum = 1
  local lasteolpos = 0
  for eolpos in src:gmatch"()\n" do
    if eolpos > pos then break end
    linenum = linenum + 1
    lasteolpos = eolpos
  end
  local colnum = pos - lasteolpos
  return linenum, colnum
end

-- Removes any sheband ("#!") line from Lua source string.
-- CATEGORY: Lua parsing
function M.remove_shebang(src)
  local shebang = src:match("^#![^\r\n]*")
  return shebang and (" "):rep(#shebang) .. src:sub(#shebang+1) or src
end


-- Custom version of loadstring that parses out line number info
-- CATEGORY: Lua parsing
function M.loadstring(src)
  local f, err = loadstring(src, "")
  if f then
    return f
  else
    err = err:gsub('^%[string ""%]:', "")
    local linenum = assert(err:match("(%d+):"))
    local colnum = 0
    local linenum2 = err:match("^%d+: '[^']+' expected %(to close '[^']+' at line (%d+)")
    return nil, err, linenum, colnum, linenum2
  end
end


-- helper for ast_from_string.  Raises on error.
-- FIX? filename currently ignored in Metalua
-- CATEGORY: Lua parsing
local function ast_from_string_helper(src, filename)
  return mlc:src_to_ast(src, filename)
end


-- Counts number of lines in text.
-- Warning: the decision of whether to count a trailing new-line in a file
-- or an empty file as a line is a little subjective.  This function currently
-- defines the line count as 1 plus the number of new line characters.
-- CATEGORY: utility/string
local function linecount(text)
  local n = 1
  for _ in text:gmatch'\n' do
    n = n + 1
  end
  return n
end


-- Converts Lua source string to Lua AST (via mlp/gg).
-- CATEGORY: Lua parsing
function M.ast_from_string(src, filename)
  local ok, ast = pcall(ast_from_string_helper, src, filename)
  if not ok then
    local err = ast
    err = err:match('[^\n]*')
    err = err:gsub("^.-:%s*line", "line")
        -- mlp.chunk prepending this is undesirable.   error(msg,0) would be better in gg.lua. Reported.
        -- TODO-Metalua: remove when fixed in Metalua.
    local linenum, colnum = err:match("line (%d+), char (%d+)")
    if not linenum then
      -- Metalua libraries may return "...gg.lua:56: .../mlp_misc.lua:179: End-of-file expected"
      -- without the normal line/char numbers given things like "if x then end end".  Should be
      -- fixed probably with gg.parse_error in _chunk in mlp_misc.lua.
      -- TODO-Metalua: remove when fixed in Metalua.
      linenum = linecount(src)
      colnum = 1
    end
    local linenum2 = nil
    return nil, err, linenum, colnum, linenum2
  else
    return ast
  end
end


-- Simple comment parser.  Returns Metalua-style comment.
-- CATEGORY: Lua lexing
local function quick_parse_comment(src)
  local s = src:match"^%-%-([^\n]*)()\n$"
  if s then return {s, 1, #src, 'short'} end
  local _, s = src:match(lexer.lexer.patterns.long_comment .. '\r?\n?$')
  if s then return {s, 1, #src, 'long'} end
  return nil
end
--FIX:check new-line correctness
--note: currently requiring \n at end of single line comment to avoid
-- incremental compilation with `--x\nf()` and removing \n from still
-- recognizing as comment `--x`.
-- currently allowing \r\n at end of long comment since Metalua includes
-- it in lineinfo of long comment (FIX:Metalua?)


-- Gets length of longest prefix string in both provided strings.
-- Returns max n such that text1:sub(1,n) == text2:sub(1,n) and n <= max(#text1,#text2)
-- CATEGORY: string utility
local function longest_prefix(text1, text2)
  local nmin = 0
  local nmax = math.min(#text1, #text2)
  while nmax > nmin do
    local nmid = math.ceil((nmin+nmax)/2)
    if text1:sub(1,nmid) == text2:sub(1,nmid) then
      nmin = nmid
    else
      nmax = nmid-1
    end
  end
  return nmin
end


-- Gets length of longest postfix string in both provided strings.
-- Returns max n such that text1:sub(-n) == text2:sub(-n) and n <= max(#text1,#text2)
-- CATEGORY: string utility
local function longest_postfix(text1, text2)
  local nmin = 0
  local nmax = math.min(#text1, #text2)
  while nmax > nmin do
    local nmid = math.ceil((nmin+nmax)/2)
    if text1:sub(-nmid) == text2:sub(-nmid) then --[*]
      nmin = nmid
    else
      nmax = nmid-1
    end
  end
  return nmin
end  -- differs from longest_prefix only on line [*]



-- Determines AST node that must be re-evaluated upon changing code string from
-- `src` to `bsrc`, given previous top_ast/tokenlist/src.
-- Note: decorates top_ast as side-effect.
-- If preserve is true, then does not expand AST match even if replacement is invalid.
-- CATEGORY: AST/tokenlist manipulation
function M.invalidated_code(top_ast, tokenlist, src, bsrc, preserve)
  -- Converts posiiton range in src to position range in bsrc.
  local function range_transform(src_fpos, src_lpos)
    local src_nlpos = #src - src_lpos
    local bsrc_fpos = src_fpos
    local bsrc_lpos = #bsrc - src_nlpos
    return bsrc_fpos, bsrc_lpos
  end

  if src == bsrc then return end -- up-to-date

  -- Find range of positions in src that differences correspond to.
  -- Note: for zero byte range, src_pos2 = src_pos1 - 1.
  local npre = longest_prefix(src, bsrc)
  local npost = math.min(#src-npre, longest_postfix(src, bsrc))
      -- note: min avoids overlap ambiguity
  local src_fpos, src_lpos = 1 + npre, #src - npost

  -- Find smallest AST node containing src range above.  May also
  -- be contained in (smaller) comment or whitespace.
  local match_ast, match_comment, iswhitespace =
      M.smallest_ast_containing_range(top_ast, tokenlist, src_fpos, src_lpos)
  DEBUG('invalidate-smallest:', match_ast and (match_ast.tag or 'notag'), match_comment, iswhitespace)

  -- Determine which (ast, comment, or whitespace) to match, and get its pos range in src and bsrc.
  local srcm_fpos, srcm_lpos, bsrcm_fpos, bsrcm_lpos, mast, mtype
  if iswhitespace then
    mast, mtype = nil, 'whitespace'
    srcm_fpos, srcm_lpos = src_fpos, src_lpos
  elseif match_comment then
    mast, mtype = match_comment, 'comment'
    srcm_fpos, srcm_lpos = match_comment.fpos, match_comment.lpos
  else
    mast, mtype = match_ast, 'ast'
    repeat
      srcm_fpos, srcm_lpos = M.ast_pos_range(mast, tokenlist)
      if not srcm_fpos then
        if mast == top_ast then
          srcm_fpos, srcm_lpos = 1, #src
          break
        else
          M.ensure_parents_marked(top_ast)
          mast = mast.parent
        end
      end
    until srcm_fpos
  end
  bsrcm_fpos, bsrcm_lpos = range_transform(srcm_fpos, srcm_lpos)

  -- Never expand match if preserve specified.
  if preserve then
    return srcm_fpos, srcm_lpos, bsrcm_fpos, bsrcm_lpos, mast, mtype
  end

  -- Determine if replacement could break parent nodes.
  local isreplacesafe
  if mtype == 'whitespace' then
    if bsrc:sub(bsrcm_fpos, bsrcm_lpos):match'^%s*$' then -- replaced with whitespace
      if bsrc:sub(bsrcm_fpos-1, bsrcm_lpos+1):match'%s' then -- not eliminating whitespace
        isreplacesafe = true
      end
    end
  elseif mtype == 'comment' then
    local m2src = bsrc:sub(bsrcm_fpos, bsrcm_lpos)
    DEBUG('invalidate-comment[' .. m2src .. ']')
    if quick_parse_comment(m2src) then  -- replaced with comment
      isreplacesafe = true
    end
  end
  if isreplacesafe then  -- return on safe replacement
    return srcm_fpos, srcm_lpos, bsrcm_fpos, bsrcm_lpos, mast, mtype
  end

  -- Find smallest containing statement block that will compile (or top_ast).
  while 1 do
    match_ast = M.get_containing_statementblock(match_ast, top_ast)
    if match_ast == top_ast then
      return 1,#src, 1, #bsrc, match_ast, 'statblock'
         -- entire AST invalidated
    end
    local srcm_fpos, srcm_lpos = M.ast_pos_range(match_ast, tokenlist)
    local bsrcm_fpos, bsrcm_lpos = range_transform(srcm_fpos, srcm_lpos)
    local msrc = bsrc:sub(bsrcm_fpos, bsrcm_lpos)
    DEBUG('invalidate-statblock:', match_ast and match_ast.tag, '[' .. msrc .. ']')
    if loadstring(msrc) then -- compiled
      return srcm_fpos, srcm_lpos, bsrcm_fpos, bsrcm_lpos, match_ast, 'statblock'
    end
    M.ensure_parents_marked(top_ast)
    match_ast = match_ast.parent
  end
end


-- Walks AST `ast` in arbitrary order, visiting each node `n`, executing `fdown(n)` (if specified)
-- when doing down and `fup(n)` (if specified) when going if.
-- CATEGORY: AST walk
function M.walk(ast, fdown, fup)
  assert(type(ast) == 'table')
  if fdown then fdown(ast) end
  for _,bast in ipairs(ast) do
    if type(bast) == 'table' then
      M.walk(bast, fdown, fup)
    end
  end
  if fup then fup(ast) end
end


-- Replaces contents of table t1 with contents of table t2.
-- Does not change metatable (if any).
-- This function is useful for swapping one AST node with another
-- while preserving any references to the node.
-- CATEGORY: table utility
function M.switchtable(t1, t2)
  for k in pairs(t1) do t1[k] = nil end
  for k in pairs(t2) do t1[k] = t2[k] end
end


-- Inserts all elements in list bt at index i in list t.
-- CATEGORY: table utility
local function tinsertlist(t, i, bt)
  local oldtlen, delta = #t, i - 1
  for ti = #t + 1, #t + #bt do t[ti] = false end -- preallocate (avoid holes)
  for ti = oldtlen, i, -1 do t[ti + #bt] = t[ti] end -- shift
  for bi = 1, #bt do t[bi + delta] = bt[bi] end -- fill
end
--[=[TESTSUITE:
local function _tinsertlist(t, i, bt)
  for bi=#bt,1,-1 do table.insert(t, i, bt[bi]) end
end -- equivalent but MUCH less efficient for large tables
local function _tinsertlist(t, i, bt)
  for bi=1,#bt do table.insert(t, i+bi-1, bt[bi]) end
end -- equivalent but MUCH less efficient for large tables
local t = {}; tinsertlist(t, 1, {}); assert(table.concat(t)=='')
local t = {}; tinsertlist(t, 1, {2,3}); assert(table.concat(t)=='23')
local t = {4}; tinsertlist(t, 1, {2,3}); assert(table.concat(t)=='234')
local t = {2}; tinsertlist(t, 2, {3,4}); assert(table.concat(t)=='234')
local t = {4,5}; tinsertlist(t, 1, {2,3}); assert(table.concat(t)=='2345')
local t = {2,5}; tinsertlist(t, 2, {3,4}); assert(table.concat(t)=='2345')
local t = {2,3}; tinsertlist(t, 3, {4,5}); assert(table.concat(t)=='2345')
print 'DONE'
--]=]



-- Gets list of keyword positions related to node ast in source src
-- note: ast must be visible, i.e. have lineinfo (e.g. unlike `Id "self" definition).
-- Note: includes operators.
-- Note: Assumes ast Metalua-style lineinfo is valid.
-- CATEGORY: tokenlist build
function M.get_keywords(ast, src)
  local list = {}
  if not ast.lineinfo then return list end
  -- examine space between each pair of children i and j.
  -- special cases: 0 is before first child and #ast+1 is after last child

  -- Put children in lexical order.
  -- Some binary operations have arguments reversed from lexical order.
  -- For example, `a > b` becomes `Op{'lt', `Id 'b', `Id 'a'}
  local oast =
    (ast.tag == 'Op' and #ast == 3 and tostring(ast[2].lineinfo.first):match('|L(%d+)') > tostring(ast[3].lineinfo.first):match('|L(%d+)'))
    and {ast[1], ast[3], ast[2]} or ast

  local i = 0
  while i <= #ast do
    -- j is node following i that has lineinfo
    local j = i+1; while j < #ast+1 and not oast[j].lineinfo do j=j+1 end

    -- Get position range [fpos,lpos] between subsequent children.
    local fpos
    if i == 0 then  -- before first child
      fpos = tonumber(tostring(ast.lineinfo.first):match('|L(%d+)'))
    else
      local last = oast[i].lineinfo.last; local c = last.comments
      fpos = (c and #c > 0 and c[#c][3] or tostring(last):match('|L(%d+)')) + 1
    end
    local lpos
    if j == #ast+1 then  -- after last child
      lpos = tonumber(tostring(ast.lineinfo.last):match('|L(%d+)'))
    else
      local first = oast[j].lineinfo.first; local c = first.comments
      lpos = (c and #c > 0 and c[1][2] or tostring(first):match('|L(%d+)')) - 1
    end

    -- Find keyword in range.
    local spos = fpos
    repeat
      local mfpos, tok, mlppos = src:match("^%s*()(%a+)()", spos)
      if not mfpos then
        mfpos, tok, mlppos = src:match("^%s*()(%p+)()", spos)
      end
      if mfpos then
        local mlpos = mlppos-1
        if mlpos > lpos then mlpos = lpos end
        if mlpos >= mfpos then
          list[#list+1] = mfpos
          list[#list+1] = mlpos
        end
      end
      spos = mlppos
    until not spos or spos > lpos
    -- note: finds single keyword.  in `local function` returns only `local`
    --DEBUG(i,j ,'test[' .. src:sub(fpos, lpos) .. ']')

    i = j  -- next

    --DESIGN:Lua: comment: string.match accepts a start position but not a stop position
  end
  return list
end
-- Q:Metalua: does ast.lineinfo[loc].comments imply #ast.lineinfo[loc].comments > 0 ?



-- Generates ordered list of tokens in top_ast/src.
-- Note: currently ignores operators and parens.
-- Note: Modifies ast.
-- Note: Assumes ast Metalua-style lineinfo is valid.
-- CATEGORY: AST/tokenlist query
local isterminal = {Nil=true, Dots=true, True=true, False=true, Number=true, String=true,
  Dots=true, Id=true}
local function compare_tokens_(atoken, btoken) return atoken.fpos < btoken.fpos end
function M.ast_to_tokenlist(top_ast, src)
  local tokens = {} -- {nbytes=#src}
  local isseen = {}
  M.walk(top_ast, function(ast)
    if isterminal[ast.tag] then -- Extract terminal
      local token = ast
      if ast.lineinfo then
        token.fpos = tonumber(tostring(ast.lineinfo.first):match('|L(%d+)'))
        token.lpos = tonumber(tostring(ast.lineinfo.last):match('|L(%d+)'))
        token.ast = ast
        table.insert(tokens, token)
      end
    else -- Extract non-terminal
      local keywordposlist = M.get_keywords(ast, src)
      for i=1,#keywordposlist,2 do
        local fpos, lpos = keywordposlist[i], keywordposlist[i+1]
        local toksrc = src:sub(fpos, lpos)
        local token = {tag='Keyword', fpos=fpos, lpos=lpos, ast=ast, toksrc}
        table.insert(tokens, token)
      end
    end
    -- Extract comments
    for i=1,2 do
      local comments = ast.lineinfo and ast.lineinfo[i==1 and 'first' or 'last'].comments
      if comments then for _, comment in ipairs(comments) do
        if not isseen[comment] then
          comment.tag = 'Comment'
          local token = comment
          token.fpos = tonumber(tostring(comment.lineinfo.first):match('|L(%d+)'))
          token.lpos = tonumber(tostring(comment.lineinfo.last):match('|L(%d+)'))
          token.ast = comment
          table.insert(tokens, token)
          isseen[comment] = true
        end
      end end
    end
  end, nil)
  table.sort(tokens, compare_tokens_)
  return tokens
end


-- Gets tokenlist range [fidx,lidx] covered by ast.  Returns nil,nil if not found.
--FIX:PERFORMANCE:this is slow on large files.
-- CATEGORY: AST/tokenlist query
function M.ast_idx_range_in_tokenlist(tokenlist, ast)
  -- Get list of primary nodes under ast.
  local isold = {}; M.walk(ast, function(ast) isold[ast] = true end)
  -- Get range.
  local fidx, lidx
  for idx=1,#tokenlist do
    local token = tokenlist[idx]
    if isold[token.ast] then
      lidx = idx
      if not fidx then fidx = idx end
    end
  end
  return fidx, lidx
end


-- Gets index range in tokenlist overlapped by character position range [fpos, lpos].
-- For example, `do ff() end` with range ` ff() ` would match tokens `ff()`.
-- Tokens partly inside range are counted, so range `f()` would match tokens `ff()`.
-- If lidx = fidx - 1, then position range is whitespace between tokens lidx (on left)
-- and fidx (on right), and this may include token pseudoindices 0 (start of file) and
-- #tokenlist+1 (end of file).
-- Note: lpos == fpos - 1 indicates zero-width range between chars lpos and fpos.
-- CATEGORY: tokenlist query
function M.tokenlist_idx_range_over_pos_range(tokenlist, fpos, lpos)
  -- Find first/last indices of tokens overlapped (even partly) by position range.
  local fidx, lidx
  for idx=1,#tokenlist do
    local token = tokenlist[idx]
    --if (token.fpos >= fpos and token.fpos <= lpos) or (token.lpos >= fpos and token.lpos <= lpos) then -- token overlaps range
    if fpos <= token.lpos and lpos >= token.fpos then -- range overlaps token (even partially)
      if not fidx then fidx = idx end
      lidx = idx
    end
  end
  if not fidx then -- on fail, check between tokens
    for idx=1,#tokenlist+1 do  -- between idx-1 and idx
      local tokfpos, toklpos = tokenlist[idx-1] and tokenlist[idx-1].lpos, tokenlist[idx] and tokenlist[idx].fpos
      if (not tokfpos or fpos > tokfpos) and (not toklpos or lpos < toklpos) then -- range between tokens
        return idx, idx-1
      end
    end
  end
  return fidx, lidx
end
--[=[TESTSUITE
local function test(...)
  return table.concat({M.tokenlist_idx_range_over_pos_range(...)}, ',')
end
check('==', test({}, 2, 2), "1,0")  -- no tokens
check('==', test({{tag='Id', fpos=1, lpos=1}}, 2, 2), "2,1")  -- right of one token
check('==', test({{tag='Id', fpos=3, lpos=3}}, 2, 2), "1,0")  -- left of one token
check('==', test({{tag='Id', fpos=3, lpos=4}}, 2, 3), "1,1")  -- left partial overlap one token
check('==', test({{tag='Id', fpos=3, lpos=4}}, 4, 5), "1,1")  -- right partial overlap one token
check('==', test({{tag='Id', fpos=3, lpos=6}}, 4, 5), "1,1")  -- partial inner overlap one token
check('==', test({{tag='Id', fpos=3, lpos=6}}, 3, 6), "1,1")  -- exact overlap one token
check('==', test({{tag='Id', fpos=4, lpos=5}}, 3, 6), "1,1")  -- extra overlap one token
check('==', test({{tag='Id', fpos=2, lpos=3}, {tag='Id', fpos=5, lpos=6}}, 4, 4), "2,1")  -- between tokens, " " exact
check('==', test({{tag='Id', fpos=2, lpos=3}, {tag='Id', fpos=5, lpos=6}}, 4, 3), "2,1")  -- between tokens, "" on left
check('==', test({{tag='Id', fpos=2, lpos=3}, {tag='Id', fpos=5, lpos=6}}, 5, 4), "2,1")  -- between tokens, "" on right
check('==', test({{tag='Id', fpos=2, lpos=3}, {tag='Id', fpos=4, lpos=5}}, 4, 3), "2,1")  -- between tokens, "" exact
--]=]

-- Removes tokens in tokenlist covered by ast.
-- CATEGORY: tokenlist manipulation
local function remove_ast_in_tokenlist(tokenlist, ast)
  local fidx, lidx  = M.ast_idx_range_in_tokenlist(tokenlist, ast)
  if fidx then  -- note: fidx implies lidx
    for idx=lidx,fidx,-1 do table.remove(tokenlist, idx) end
  end
end


-- Inserts tokens from btokenlist into tokenlist.  Preserves sort.
-- CATEGORY: tokenlist manipulation
local function insert_tokenlist(tokenlist, btokenlist)
  local ftoken = btokenlist[1]
  if ftoken then
    -- Get index in tokenlist in which to insert tokens in btokenlist.
    local fidx
    for idx=1,#tokenlist do
      if tokenlist[idx].fpos > ftoken.fpos then fidx = idx; break end
    end
    fidx = fidx or #tokenlist + 1  -- else append

    -- Insert tokens.
    tinsertlist(tokenlist, fidx, btokenlist)
  end
end


-- Get character position range covered by ast in tokenlist.  Returns nil,nil on not found.
-- CATEGORY: AST/tokenlist query
function M.ast_pos_range(ast, tokenlist) -- IMPROVE:style: ast_idx_range_in_tokenlist has params reversed
  local fidx, lidx  = M.ast_idx_range_in_tokenlist(tokenlist, ast)
  if fidx then
    return tokenlist[fidx].fpos, tokenlist[lidx].lpos
  else
    return nil, nil
  end
end


-- Gets string representation of AST node.  nil if none.
-- IMPROVE: what if node is empty block?
-- CATEGORY: AST/tokenlist query
function M.ast_to_text(ast, tokenlist, src) -- IMPROVE:style: ast_idx_range_in_tokenlist has params reversed
  local fpos, lpos = M.ast_pos_range(ast, tokenlist)
  if fpos then
    return src:sub(fpos, lpos)
  else
    return nil
  end
end



-- Gets smallest AST node in top_ast/tokenlist/src
-- completely containing position range [pos1, pos2].
-- careful: "function" is not part of the `Function node.
-- If range is inside comment, returns comment also.
-- If range is inside whitespace, then returns true in third return value.
-- CATEGORY: AST/tokenlist query
function M.smallest_ast_containing_range(top_ast, tokenlist, pos1, pos2)
  local f0idx, l0idx = M.tokenlist_idx_range_over_pos_range(tokenlist, pos1, pos2)

  -- Find enclosing AST.
  M.ensure_parents_marked(top_ast)
  local fidx, lidx = f0idx, l0idx
  while tokenlist[fidx] and not tokenlist[fidx].ast.parent do fidx = fidx - 1 end
  while tokenlist[lidx] and not tokenlist[lidx].ast.parent do lidx = lidx + 1 end
  -- DEBUG(fidx, lidx, f0idx, l0idx, #tokenlist, pos1, pos2, tokenlist[fidx], tokenlist[lidx])
  local ast = not (tokenlist[fidx] and tokenlist[lidx]) and top_ast or
      M.common_ast_parent(tokenlist[fidx].ast, tokenlist[lidx].ast, top_ast)
  -- DEBUG('m2', tokenlist[fidx], tokenlist[lidx], top_ast, ast, ast and ast.tag)
  if l0idx == f0idx - 1 then -- whitespace
    return ast, nil, true
  elseif l0idx == f0idx and tokenlist[l0idx].tag == 'Comment' then
    return ast, tokenlist[l0idx], nil
  else
    return ast, nil, nil
  end
end
--IMPROVE: handle string edits and maybe others


-- Gets smallest statement block containing position pos or
-- nearest statement block before pos, whichever is smaller, given ast/tokenlist.
function M.current_statementblock(ast, tokenlist, pos)
  local fidx,lidx = M.tokenlist_idx_range_over_pos_range(tokenlist, pos, pos)
  if fidx > lidx then fidx = lidx end -- use nearest backward

  -- Find closest AST node backward
  while fidx >= 1 and tokenlist[fidx].tag == 'Comment' do fidx=fidx-1 end

  if fidx < 1 then return ast, false end
  local mast = tokenlist[fidx].ast
  if not mast then return ast, false end
  mast = M.get_containing_statementblock(mast, ast)
  local isafter = false
  if mast.tag2 ~= 'Block' then
    local mfidx,mlidx = M.ast_idx_range_in_tokenlist(tokenlist, mast)
    if pos > mlidx then
      isafter = true
    end
  end

  return mast, isafter
end

-- Gets index of bast in ast (nil if not found).
-- CATEGORY: AST query
function M.ast_idx(ast, bast)
  for idx=1,#ast do
    if ast[idx] == bast then return idx end
  end
  return nil
end


-- Gets parent of ast and index of ast in parent.
-- Root node top_ast must also be provided.  Returns nil, nil if ast is root.
-- Note: may call mark_parents.
-- CATEGORY: AST query
function M.ast_parent_idx(top_ast, ast)
  if ast == top_ast then return nil, nil end
  M.ensure_parents_marked(top_ast); assert(ast.parent)
  local idx = M.ast_idx(ast.parent, ast)
  return ast.parent, idx
end


-- Gets common parent of aast and bast.  Always returns value.
-- Must provide root top_ast too.
-- CATEGORY: AST query
function M.common_ast_parent(aast, bast, top_ast)
  M.ensure_parents_marked(top_ast)
  local isparent = {}
  local tast = bast; repeat isparent[tast] = true; tast = tast.parent until not tast
  local uast = aast; repeat if isparent[uast] then return uast end; uast = uast.parent until not uast
  assert(false)
end


-- Replaces old_ast with new_ast/new_tokenlist in top_ast/tokenlist.
-- Note: assumes new_ast is a block.  assumes old_ast is a statement or block.
-- CATEGORY: AST/tokenlist
function M.replace_statements(top_ast, tokenlist, old_ast, new_ast, new_tokenlist)
  remove_ast_in_tokenlist(tokenlist, old_ast)
  insert_tokenlist(tokenlist, new_tokenlist)
  if old_ast == top_ast then -- special case: no parent
    M.switchtable(old_ast, new_ast) -- note: safe since block is not in tokenlist.
  else
    local parent_ast, idx = M.ast_parent_idx(top_ast, old_ast)
    table.remove(parent_ast, idx)
    tinsertlist(parent_ast, idx, new_ast)
  end

  -- fixup annotations
  for _,bast in ipairs(new_ast) do
    if top_ast.tag2 then M.mark_tag2(bast, bast.tag == 'Do' and 'StatBlock' or 'Block') end
    if old_ast.parent then M.mark_parents(bast, old_ast.parent) end
  end
end


-- Adjusts lineinfo in tokenlist.
-- All char positions starting at pos1 are shifted by delta number of chars.
-- CATEGORY: tokenlist
function M.adjust_lineinfo(tokenlist, pos1, delta)
  for _,token in ipairs(tokenlist) do
    if token.fpos >= pos1 then
       token.fpos = token.fpos + delta
    end
    if token.lpos >= pos1 then
      token.lpos = token.lpos + delta
    end
  end
  --tokenlist.nbytes = tokenlist.nbytes + delta
end


-- For each node n in ast, sets n.parent to parent node of n.
-- Assumes ast.parent will be parent_ast (may be nil)
-- CATEGORY: AST query
function M.mark_parents(ast, parent_ast)
  ast.parent = parent_ast
  for _,ast2 in ipairs(ast) do
    if type(ast2) == 'table' then
      M.mark_parents(ast2, ast)
    end
  end
end


-- Calls mark_parents(ast) if ast not marked.
-- CATEGORY: AST query
function M.ensure_parents_marked(ast)
  if ast[1] and not ast[1].parent then M.mark_parents(ast) end
end


-- For each node n in ast, sets n.tag2 to context string:
-- 'Block' - node is block
-- 'Stat' - node is statement
-- 'StatBlock' - node is statement and block (i.e. `Do)
-- 'Exp' - node is expression
-- 'Explist' - node is expression list (or identifier list)
-- 'Pair' - node is key-value pair in table constructor
-- note: ast.tag2 will be set to context.
-- CATEGORY: AST query
local iscertainstat = {Do=true, Set=true, While=true, Repeat=true, If=true,
  Fornum=true, Forin=true, Local=true, Localrec=true, Return=true, Break=true}
function M.mark_tag2(ast, context)
  context = context or 'Block'
  ast.tag2 = context
  for i,bast in ipairs(ast) do
    if type(bast) == 'table' then
      local nextcontext
      if bast.tag == 'Do' then
        nextcontext = 'StatBlock'
      elseif iscertainstat[bast.tag] then
        nextcontext = 'Stat'
      elseif bast.tag == 'Call' or bast.tag == 'Invoke' then
        nextcontext = context == 'Block' and 'Stat' or 'Exp'
        --DESIGN:Metalua: these calls actually contain expression lists,
        --  but the expression list is not represented as a complete node
        --  by Metalua (as blocks are in `Do statements)
      elseif bast.tag == 'Pair' then
        nextcontext = 'Pair'
      elseif not bast.tag then
        if ast.tag == 'Set' or ast.tag == 'Local' or ast.tag == 'Localrec'
          or ast.tag == 'Forin' and i <= 2
          or ast.tag == 'Function'  and i == 1
        then
          nextcontext = 'Explist'
        else
          nextcontext = 'Block'
        end
      else
        nextcontext = 'Exp'
      end
      M.mark_tag2(bast, nextcontext)
    end
  end
end


-- Gets smallest statement or block containing or being `ast`.
-- The AST root node `top_ast` must also be provided.
-- Note: may decorate AST as side-effect (mark_tag2/mark_parents).
-- top_ast is assumed a block, so this is always successful.
-- CATEGORY: AST query
function M.get_containing_statementblock(ast, top_ast)
  if not top_ast.tag2 then M.mark_tag2(top_ast) end
  if ast.tag2 == 'Stat' or ast.tag2 == 'StatBlock' or ast.tag2 == 'Block' then
    return ast
  else
    M.ensure_parents_marked(top_ast)
    return M.get_containing_statementblock(ast.parent, top_ast)
  end
end


-- Finds smallest statement, block, or comment AST  in ast/tokenlist containing position
-- range [fpos, lpos].  If allowexpand is true (default nil) and located AST
-- coincides with position range, then next containing statement is used
-- instead (this allows multiple calls to further expand the statement selection).
-- CATEGORY: AST query
function M.select_statementblockcomment(ast, tokenlist, fpos, lpos, allowexpand)
--IMPROVE: rename ast to top_ast
  local match_ast, comment_ast = M.smallest_ast_containing_range(ast, tokenlist, fpos, lpos)
  local select_ast = comment_ast or M.get_containing_statementblock(match_ast, ast)
  local nfpos, nlpos = M.ast_pos_range(select_ast, tokenlist)
  --DEBUG('s', nfpos, nlpos, fpos, lpos, match_ast.tag, select_ast.tag)
  if allowexpand and fpos == nfpos and lpos == nlpos then
    if comment_ast then
      -- Select enclosing statement.
      select_ast = match_ast
      nfpos, nlpos = M.ast_pos_range(select_ast, tokenlist)
    else
      -- note: multiple times may be needed to expand selection.  For example, in
      --   `for x=1,2 do f() end` both the statement `f()` and block `f()` have
      --   the same position range.
      M.ensure_parents_marked(ast)
      while select_ast.parent and fpos == nfpos and lpos == nlpos do
        select_ast = M.get_containing_statementblock(select_ast.parent, ast)
        nfpos, nlpos = M.ast_pos_range(select_ast, tokenlist)
      end
    end
  end
  return nfpos, nlpos
end


-- Converts tokenlist to string representation for debugging.
-- CATEGORY: tokenlist debug
function M.dump_tokenlist(tokenlist)
  local ts = {}
  for i,token in ipairs(tokenlist) do
    ts[#ts+1] = 'tok.' .. i .. ': [' .. token.fpos .. ',' .. token.lpos .. '] '
       .. tostring(token[1]) .. ' ' .. tostring(token.ast.tag)
  end
  return table.concat(ts, '\n') -- .. 'nbytes=' .. tokenlist.nbytes .. '\n'
end


--FIX:Q: does this handle Unicode ok?

--FIX?:Metalua: fails on string with escape sequence '\/'.  The Reference Manual
-- doesn't say this sequence is valid though.

--FIX:Metalua: In `local --[[x]] function --[[y]] f() end`,
--   'x' comment omitted from AST.

--FIX:Metalua: `do --[[x]] end` doesn't generate comments in AST.
--  `if x then --[[x]] end` and `while 1 do --[[x]] end` generates
--   comments in first/last of block

--FIX:Metalua: `--[[x]] f() --[[y]]` returns lineinfo around `f()`.
--  `--[[x]] --[[y]]` returns lineinfo around everything.

--FIX:Metalua: `while 1 do --[[x]] --[[y]] end` returns first > last
--   lineinfo for contained block

--FIX:Metalua: search for "PATCHED:LuaInspect" in the metalualib folder.

--FIX?:Metalua: loadstring parses "--x" but metalua omits the comment in the AST

--FIX?:Metalua: `local x` is generating `Local{{`Id{x}}, {}}`, which
--  has no lineinfo on {}.  This is contrary to the Metalua
--  spec: `Local{ {ident+} {expr+}? }.
--  Other things like `self` also generate no lineinfo.
--  The ast2.lineinfo  above avoids this.

--FIX:Metalua: Metalua shouldn't overwrite ipairs/pairs.  Note: Metalua version
--  doesn't set errorlevel correctly.

--Q:Metalua: Why does `return --[[y]]  z  --[[x]]` have
--  lineinfo.first.comments, lineinfo.last.comments,
--  plus lineinfo.comments (which is the same as lineinfo.first.comments) ?

--CAUTION:Metalua: `do  f()   end` returns lineinfo around `do  f()   end`, while
--  `while 1 do  f()  end` returns lineinfo around `f()` for inner block.

--CAUTION:Metalua: The lineinfo on Metalua comments is inconsistent with other
--   nodes

--CAUTION:Metalua: lineinfo of table in `f{}` is [3,2], of `f{ x,y }` it's [4,6].
--  This is inconsistent with `x={}` which is [3,4] and `f""` which is [1,2]
--  for the string.

--CAUTION:Metalua: only the `function()` form of `Function includes `function`
--   in lineinfo.  'function' is part of `Localrec and `Set in syntactic sugar form.


--[=[TESTSUITE
-- test longest_prefix/longest_postfix
local function pr(text1, text2)
  local lastv
  local function same(v)
    assert(not lastv or v == lastv); lastv = v; return v
  end
  local function test1(text1, text2) -- test prefix/postfix
    same(longest_prefix(text1, text2))
    same(longest_postfix(text1:reverse(), text2:reverse()))
  end
  local function test2(text1, text2) -- test swap
    test1(text1, text2)
    test1(text2, text1)
  end
  for _,extra in ipairs{"", "x", "xy", "xyz"} do -- test extra chars
    test2(text1, text2..extra)
    test2(text2, text1..extra)
  end
  return lastv
end
check('==', pr("",""), 0)
check('==', pr("a",""), 0)
check('==', pr("a","a"), 1)
check('==', pr("ab",""), 0)
check('==', pr("ab","a"), 1)
check('==', pr("ab","ab"), 2)
check('==', pr("abcdefg","abcdefgh"), 7)
--]=]

--[=[TESTSUITE
print 'DONE'
--]=]


return M
