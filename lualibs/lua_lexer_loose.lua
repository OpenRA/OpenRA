--[[
 lua_lexer_loose.lua.
 Loose lexing of Lua code.  See README.
 
 WARNING: This code is preliminary and may have errors
 in its current form.
 
 (c) 2013 David Manura. MIT License.
--]]

local M = {}

-- based on LuaBalanced
local function match_string(s, pos)
  pos = pos or 1
  local posa = pos
  local c = s:sub(pos,pos)
  if c == '"' or c == "'" then
    pos = pos + 1
    while 1 do
      pos = s:find("[" .. c .. "\\]", pos)
      if not pos then return s:sub(posa), #s + 1 end -- not terminated string
      if s:sub(pos,pos) == c then
        local part = s:sub(posa, pos)
        return part, pos + 1
      else
        pos = pos + 2
      end
    end
  else
    local sc = s:match("^%[(=*)%[", pos)
    if sc then
      local _; _, pos = s:find("%]" .. sc .. "%]", pos)
      if not pos then return s:sub(posa), #s + 1 end -- not terminated string
      local part = s:sub(posa, pos)
      return part, pos + 1
    else
      return nil, pos
    end
  end
end

-- based on LuaBalanced
local function match_comment(s, pos)
  pos = pos or 1
  if s:sub(pos, pos+1) ~= '--' then
    return nil, pos
  end
  pos = pos + 2
  if s:sub(pos,pos) == '[' then
    local partt, post = match_string(s, pos)
    if partt then
      return '--' .. partt, post
    end
  end
  local part; part, pos = s:match('^([^\n]*\n?)()', pos)
  return '--' .. part, pos
end

-- note: matches invalid numbers too (for example, 0x)
local function match_numberlike(s, pos)
  local hex = s:match('^0[xX]', pos)
  if hex then pos = pos + #hex end

  local longint = (hex and '^%x+' or '^%d+') .. '[uU]?[lL][lL]'
  local mantissa1 = hex and '^%x+%.?%x*' or '^%d+%.?%d*'
  local mantissa2 = hex and '^%.%x+' or '^%.%d+'
  local exponent = hex and '^[pP][+%-]?%x*' or '^[eE][+%-]?%d*'
  local imaginary = '^[iI]'
  local tok = s:match(longint, pos)
  if not tok then
    tok = s:match(mantissa1, pos) or s:match(mantissa2, pos)
    if tok then
      local tok2 = s:match(exponent, pos + #tok)
      if tok2 then tok = tok..tok2 end
      tok2 = s:match(imaginary, pos + #tok)
      if tok2 then tok = tok..tok2 end
    end
  end
  return tok and (hex or '') .. tok or hex
end

local function newset(s)
  local t = {}
  for c in s:gmatch'.' do t[c] = true end
  return t
end
local function qws(s)
  local t = {}
  for k in s:gmatch'%S+' do t[k] = true end
  return t
end

local sym = newset("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_")
local dig = newset('0123456789')
local name = "([_A-Za-z][_A-Za-z0-9]*)"
local op = newset('=~<>.+-*/%^#=<>;:,.{}[]()')

op['=='] = true
op['<='] = true
op['>='] = true
op['~='] = true
op['..'] = true
op['<<'] = true
op['>>'] = true
op['//'] = true

local is_keyword = qws[[
  and break do else elseif end false for function if
  in local nil not or repeat return
  then true until while goto]]

function M.lex(code, f, pos)
  local pos = pos or 1
  local tok = code:match('^#![^\n]*\n', pos) -- shebang
  if tok then f('Shebang', tok, 1) pos = pos + #tok end
  while pos <= #code do
    local p2, n2, n1, n3 = code:match('^%s*()((%S)(%S?))', pos)
    if not p2 then assert(code:sub(pos):match('^%s*$')); break end
    pos = p2
    
    if sym[n1] then
      local tok = code:match('^'..name, pos)
      assert(tok)
      if is_keyword[tok] then
        f('Keyword', tok, pos)
      else
        f('Id', tok, pos)
      end
      pos = pos + #tok
    elseif n2 == '--' then
      local tok, pos2 = match_comment(code, pos)
      assert(tok)
      f('Comment', tok, pos)
      pos = pos2
    elseif n2 == '::' then
      local tok = code:match('^(::%s*'..name..'%s*::)', pos)
      if tok then
        f('Label', tok, pos)
        pos = pos + #tok
      else
        f('Unknown', code:sub(pos, pos+1), pos) -- unterminated label
        pos = pos + 2
      end
    elseif n1 == '\'' or n1 == '\"' or n2 == '[[' or n2 == '[=' then
      local tok = match_string(code, pos)
      if tok then
        f('String', tok, pos)
        pos = pos + #tok
      else
        f('Unknown', code:sub(pos), pos) -- unterminated string
        pos = #code + 1
      end
    elseif dig[n1] or (n1 == '.' and dig[n3]) then
      local tok = match_numberlike(code, pos)
      assert(tok)
      f('Number', tok, pos)
      pos = pos + #tok
    elseif op[n2] then
      if n2 == '..' and code:match('^%.', pos+2) then
        tok = '...'
      else
        tok = n2
      end
      f('Keyword', tok, pos)
      pos = pos + #tok
    elseif op[n1] then
      local tok = n1
      f('Keyword', tok, pos)
      pos = pos + #tok
    else
      f('Unknown', n1, pos)
      pos = pos + 1
    end
  end
end

local Stream = {}
Stream.__index = Stream
function Stream:next(val)
  if self._next then
    local _next = self._next
    self._next = nil
    return _next
  else
    self._next = nil
    return self.f()
  end
end
function Stream:peek()
  if self._next then
    return self._next
  else
    local _next = self.f()
    self._next = _next
    return _next
  end
end

function M.lexc(code, f, pos)
  local yield = coroutine.yield
  local func = coroutine.wrap(f or function()
    M.lex(code, function(tag, name, pos)
      -- skip Comment tags as they may arbitrarily split statements and affects their processing
      if tag ~= 'Comment' then yield {tag=tag, name, lineinfo=pos} end
    end, pos)
    yield {tag='Eof', lineinfo = #code+1}
  end)
  return setmetatable({f=func}, Stream)
end

return M
