--
-- Copyright (C) 2012 Paul Kulchenko
-- A simple testing library
-- Based on lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
-- Copyright (c) 2009-2011 Francois Perrad
-- This library is licensed under the terms of the MIT/X11 license,
-- like Lua itself.
--

local m = {
  _NAME = "testwell",
  _VERSION = "0.2",
  _COPYRIGHT = "Paul Kulchenko",
  _DESCRIPTION = "Simple testing library",
}

local pairs = pairs
local tostring = tostring
local type = type
local _G = _G or _ENV

-----------------------------------------------------------

local tb = {
  curr_test = 0,
  good_test = 0,
  skip_test = 0,
}

function tb:print(...)
  print(...)
end

function tb:note(...)
  self:print(...)
end

function tb:diag(...)
  local arg = {...}
  for k, v in pairs(arg) do
    arg[k] = tostring(v)
  end
  local msg = table.concat(arg)
  msg = msg:gsub("\n", "\n# ")
  msg = msg:gsub("\n# \n", "\n#\n")
  msg = msg:gsub("\n# $", '')
  self:print("# " .. msg)
end

function tb:ok(test, name, more)
  self.curr_test = self.curr_test + 1
  self.good_test = self.good_test + (test and 1 or 0)
  self.skip_test = self.skip_test + (test == nil and 1 or 0)
  name = tostring(name or '')
  local out
  if test == nil then
    out = "skip "
  elseif test then
    out = "ok "
  else
    out = "not ok "
  end
  out = out .. self.curr_test
  if name ~= '' then
    out = out .. " - " .. name
  end
  self:print(out)
  if test == false then
    self:diag("    Failed test " .. (name and ("'" .. name .. "'") or ''))
    if debug then
      local info = debug.getinfo(3)
      local file = info.short_src
      local line = info.currentline
      self:diag("    in " .. file .. " at line " .. line .. ".")
    end
    self:diag(more)
  end
end

function tb:done_testing(reset)
  local c, g, s = self.curr_test, self.good_test, self.skip_test
  if reset then
    self.curr_test = 0
    self.good_test = 0
    self.skip_test = 0
  end
  return c, g, s
end

-----------------------------------------------------------

local serpent = (function() ---- include Serpent module for serialization
local n, v = "serpent", 0.15 -- (C) 2012 Paul Kulchenko; MIT License
local c, d = "Paul Kulchenko", "Serializer and pretty printer of Lua data types"
local snum = {[tostring(1/0)]='1/0 --[[math.huge]]',[tostring(-1/0)]='-1/0 --[[-math.huge]]',[tostring(0/0)]='0/0'}
local badtype = {thread = true, userdata = true}
local keyword, globals, G = {}, {}, (_G or _ENV)
for _,k in ipairs({'and', 'break', 'do', 'else', 'elseif', 'end', 'false',
  'for', 'function', 'goto', 'if', 'in', 'local', 'nil', 'not', 'or', 'repeat',
  'return', 'then', 'true', 'until', 'while'}) do keyword[k] = true end
for k,v in pairs(G) do globals[v] = k end -- build func to name mapping
for _,g in ipairs({'coroutine', 'debug', 'io', 'math', 'string', 'table', 'os'}) do
  for k,v in pairs(G[g]) do globals[v] = g..'.'..k end end

local function s(t, opts)
  local name, indent, fatal = opts.name, opts.indent, opts.fatal
  local sparse, custom, huge = opts.sparse, opts.custom, not opts.nohuge
  local space, maxl = (opts.compact and '' or ' '), (opts.maxlevel or math.huge)
  local comm = opts.comment and (tonumber(opts.comment) or math.huge)
  local seen, sref, syms, symn = {}, {}, {}, 0
  local function gensym(val) return tostring(val):gsub("[^%w]",""):gsub("(%d%w+)",
    function(s) if not syms[s] then symn = symn+1; syms[s] = symn end return syms[s] end) end
  local function safestr(s) return type(s) == "number" and (huge and snum[tostring(s)] or s)
    or type(s) ~= "string" and tostring(s) -- escape NEWLINE/010 and EOF/026
    or ("%q"):format(s):gsub("\010","n"):gsub("\026","\\026") end
  local function comment(s,l) return comm and (l or 0) < comm and ' --[['..tostring(s)..']]' or '' end
  local function globerr(s,l) return globals[s] and globals[s]..comment(s,l) or not fatal
    and safestr(tostring(s))..comment('err',l) or error("Can't serialize "..tostring(s)) end
  local function safename(path, name) -- generates foo.bar, foo[3], or foo['b a r']
    local n = name == nil and '' or name
    local plain = type(n) == "string" and n:match("^[%l%u_][%w_]*$") and not keyword[n]
    local safe = plain and n or '['..safestr(n)..']'
    return (path or '')..(plain and path and '.' or '')..safe, safe end
  local alphanumsort = type(opts.sortkeys) == 'function' and opts.sortkeys or function(o, n)
    local maxn, to = tonumber(n) or 12, {number = 'a', string = 'b'}
    local function padnum(d) return ("%0"..maxn.."d"):format(d) end
    table.sort(o, function(a,b)
      return (o[a] and 0 or to[type(a)] or 'z')..(tostring(a):gsub("%d+",padnum))
           < (o[b] and 0 or to[type(b)] or 'z')..(tostring(b):gsub("%d+",padnum)) end) end
  local function val2str(t, name, indent, path, plainindex, level)
    local ttype, level = type(t), (level or 0)
    local spath, sname = safename(path, name)
    local tag = plainindex and
      ((type(name) == "number") and '' or name..space..'='..space) or
      (name ~= nil and sname..space..'='..space or '')
    if seen[t] then
      table.insert(sref, spath..space..'='..space..seen[t])
      return tag..'nil'..comment('ref', level)
    elseif badtype[ttype] then return tag..globerr(t, level)
    elseif ttype == 'function' then
      seen[t] = spath
      local ok, res = pcall(string.dump, t)
      local func = ok and ((opts.nocode and "function() end" or
        "loadstring("..safestr(res)..",'@serialized')")..comment(t, level))
      return tag..(func or globerr(t, level))
    elseif ttype == "table" then
      if level >= maxl then return tag..'{}'..comment('max', level) end
      seen[t] = spath
      if next(t) == nil then return tag..'{}'..comment(t, level) end -- table empty
      local maxn, o, out = #t, {}, {}
      for key = 1, maxn do table.insert(o, key) end
      for key in pairs(t) do if not o[key] then table.insert(o, key) end end
      if opts.sortkeys then alphanumsort(o, opts.sortkeys) end
      for n, key in ipairs(o) do
        local value, ktype, plainindex = t[key], type(key), n <= maxn and not sparse
        if opts.ignore and opts.ignore[value] -- skip ignored values; do nothing
        or sparse and value == nil then -- skipping nils; do nothing
        elseif ktype == 'table' or ktype == 'function' then
          if not seen[key] and not globals[key] then
            table.insert(sref, 'local '..val2str(key,gensym(key),indent)) end
          table.insert(sref, seen[t]..'['..(seen[key] or globals[key] or gensym(key))
            ..']'..space..'='..space..(seen[value] or val2str(value,nil,indent)))
        else
          if badtype[ktype] then plainindex, key = true, '['..globerr(key, level+1)..']' end
          table.insert(out,val2str(value,key,indent,spath,plainindex,level+1))
        end
      end
      local prefix = string.rep(indent or '', level)
      local head = indent and '{\n'..prefix..indent or '{'
      local body = table.concat(out, ','..(indent and '\n'..prefix..indent or space))
      local tail = indent and "\n"..prefix..'}' or '}'
      return (custom and custom(tag,head,body,tail) or tag..head..body..tail)..comment(t, level)
    else return tag..safestr(t) end -- handle all other types
  end
  local sepr = indent and "\n" or ";"..space
  local body = val2str(t, name, indent) -- this call also populates sref
  local tail = #sref>0 and table.concat(sref, sepr)..sepr or ''
  return not name and body or "do local "..body..sepr..tail.."return "..name..sepr.."end"
end

local function merge(a, b) if b then for k,v in pairs(b) do a[k] = v end end; return a; end
return { _NAME = n, _COPYRIGHT = c, _DESCRIPTION = d, _VERSION = v, serialize = s,
  dump = function(a, opts) return s(a, merge({name = '_', compact = true, sparse = true}, opts)) end,
  line = function(a, opts) return s(a, merge({sortkeys = true, comment = true}, opts)) end,
  block = function(a, opts) return s(a, merge({indent = '  ', sortkeys = true, comment = true}, opts)) end }
end)() ---- end of Serpent module

-----------------------------------------------------------

function m.ok(test, name)
  tb:ok(test, name)
end

local parms = {comment = false}
function m.is(got, expected, name)
  local tgot, texp = type(got), type(expected)
  local vgot, vexp = serpent.line(got, parms), serpent.line(expected, parms)
  local pass = vgot == vexp
  if got == nil then pass = nil end
  tb:ok(pass, name, not pass and 
      "         got: " .. vgot .. " (" .. tgot .. ")" ..
    "\n    expected: " .. vexp .. " (" .. texp .. ")")
end

function m.isnt(got, expected, name)
  local tgot, texp = type(got), type(expected)
  local vgot, vexp = serpent.line(got, parms), serpent.line(expected, parms)
  local pass = vgot ~= vexp
  if got == nil then pass = nil end
  tb:ok(pass, name, not pass and
      "         got: " .. vgot .. " (" .. tgot .. ")" ..
    "\n    expected: anything else")
end

function m.like(got, pattern, name)
  if type(pattern) ~= 'string' then
    return tb:ok(false, name, "pattern isn't a string : " .. tostring(pattern))
  end

  local pass = tostring(got):match(pattern)
  if got == nil then pass = nil end
  tb:ok(pass, name, not pass and 
      "                  '" .. tostring(got) .. "'" ..
    "\n    doesn't match '" .. pattern .. "'")
end

function m.unlike(got, pattern, name)
  if type(pattern) ~= 'string' then
    return tb:ok(false, name, "pattern isn't a string : " .. tostring(pattern))
  end

  local pass = not tostring(got):match(pattern)
  if got == nil then pass = nil end
  tb:ok(pass, name, not pass and 
      "                  '" .. tostring(got) .. "'" ..
    "\n          matches '" .. pattern .. "'")
end

local cmp = {
    ['<']  = function (a, b) return a <  b end,
    ['<='] = function (a, b) return a <= b end,
    ['>']  = function (a, b) return a >  b end,
    ['>='] = function (a, b) return a >= b end,
    ['=='] = function (a, b) return a == b end,
    ['~='] = function (a, b) return a ~= b end,
}

function m.cmp_ok(this, op, that, name)
  local f = cmp[op]
  if not f then
    return tb:ok(false, name, "unknown operator : " .. tostring(op))
  end

  local pass = f(this, that)
  if this == nil then pass = nil end
  tb:ok(pass, name, not pass and 
      "    " .. tostring(this) ..
    "\n        " .. op ..
    "\n    " .. tostring(that))
end

function m.type_ok(val, t, name)
  if type(t) ~= 'string' then
    return tb:ok(false, name, "type isn't a string : " .. tostring(t))
  end

  if type(val) == t then
    tb:ok(true, name)
  else
    tb:ok(false, name,
      "    " .. tostring(val) .. " isn't a '" .. t .."', it's a '" .. type(val) .. "'")
  end
end

function m.diag(...)
  tb:diag(...)
end

function m.report()
  local total, good, skipped = tb:done_testing(true)
  if total == 0 then return end
  local failed = total - good - skipped
  local sum = ("(%d/%d/%d)."):format(good, skipped, total)
  local num, msg = 0, ""
  if good > 0 then
    num, msg = good, msg .. "passed " .. good
  end
  if failed > 0 then
    num, msg = failed, msg .. (#msg > 0 and (skipped > 0 and ", " or " and ") or "")
      .. "failed " .. failed
  end
  if skipped > 0 then 
    num, msg = skipped, msg .. (#msg > 0 and ((good > 0 and failed > 0 and ',' or '') .." and ") or "")
      .. "skipped " .. skipped
  end
  msg = ("Looks like you %s test%s of %d %s"):format(msg, (num > 1 and 's' or ''), total, sum)
  if skipped == total then msg = "Looks like you skipped all tests " .. sum end
  if good == total then msg = "All tests passed " .. sum end
  tb:note(("1..%d # %s"):format(total, msg))
end

function m.ismain()
  for l = 3, 64 do -- only check up to 64 level; no more needed
    local info = debug.getinfo(l)
    if not info then return true end
    if info.func == require then return false end
  end
  return true
end

-- this is needed to call report() when the test object is destroyed
if _VERSION >= "Lua 5.2" then
  setmetatable(m, {__gc = m.report})
else
  -- keep sentinel alive until 'm' is garbage collected
  m.sentinel = newproxy(true)
  getmetatable(m.sentinel).__gc = m.report
end

for k, v in pairs(m) do -- injection
  _G[k] = v
end

return m
