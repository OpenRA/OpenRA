--[[

  compat_env v$(_VERSION) - Lua 5.1/5.2 environment compatibility functions

SYNOPSIS

  -- Get load/loadfile compatibility functions only if using 5.1.
  local CL = pcall(load, '') and _G or require 'compat_env'
  local load     = CL.load
  local loadfile = CL.loadfile
  
  -- The following now works in both Lua 5.1 and 5.2:
  assert(load('return 2*pi', nil, 't', {pi=math.pi}))()
  assert(loadfile('ex.lua', 't', {print=print}))()
  
  -- Get getfenv/setfenv compatibility functions only if using 5.2.
  local getfenv = _G.getfenv or require 'compat_env'.getfenv
  local setfenv = _G.setfenv or require 'compat_env'.setfenv
  local function f() return x end
  setfenv(f, {x=2})
  print(x, getfenv(f).x) --> 2, 2

DESCRIPTION

  This module provides Lua 5.1/5.2 environment related compatibility functions.
  This includes implementations of Lua 5.2 style `load` and `loadfile`
  for use in Lua 5.1.  It also includes Lua 5.1 style `getfenv` and `setfenv`
  for use in Lua 5.2.
 
API

  local CL = require 'compat_env'
  
  CL.load (ld [, source [, mode [, env] ] ]) --> f [, err]

    This behaves the same as the Lua 5.2 `load` in both
    Lua 5.1 and 5.2.
    http://www.lua.org/manual/5.2/manual.html#pdf-load
    
  CL.loadfile ([filename [, mode [, env] ] ]) --> f [, err]
  
    This behaves the same as the Lua 5.2 `loadfile` in both
    Lua 5.1 and 5.2.
    http://www.lua.org/manual/5.2/manual.html#pdf-loadfile
    
  CL.getfenv ([f]) --> t

    This is identical to the Lua 5.1 `getfenv` in Lua 5.1.
    This behaves similar to the Lua 5.1 `getfenv` in Lua 5.2.
    When a global environment is to be returned, or when `f` is a
    C function, this returns `_G`  since Lua 5.2 doesn't have
    (thread) global and C function environments.  This will also
    return `_G` if the Lua function `f` lacks an `_ENV`
    upvalue, but it will raise an error if uncertain due to lack of
    debug info.  It is not normally considered good design to use
    this function; when possible, use `load` or `loadfile` instead.
    http://www.lua.org/manual/5.1/manual.html#pdf-getfenv
    
  CL.setfenv (f, t)
  
    This is identical to the Lua 5.1 `setfenv` in Lua 5.1.
    This behaves similar to the Lua 5.1 `setfenv` in Lua 5.2.
    This will do nothing if `f` is a Lua function that
    lacks an `_ENV` upvalue, but it will raise an error if uncertain
    due to lack of debug info.  See also Design Notes below.
    It is not normally considered good design to use
    this function; when possible, use `load` or `loadfile` instead.
    http://www.lua.org/manual/5.1/manual.html#pdf-setfenv
    
DESIGN NOTES

  This module intends to provide robust and fairly complete reimplementations
  of the environment related Lua 5.1 and Lua 5.2 functions.
  No effort is made, however, to simulate rare or difficult to simulate features,
  such as thread environments, although this is liable to change in the future.
  Such 5.1 capabilities are discouraged and ideally
  removed from 5.1 code, thereby allowing your code to work in both 5.1 and 5.2.
  
  In Lua 5.2, a `setfenv(f, {})`, where `f` lacks any upvalues, will be silently
  ignored since there is no `_ENV` in this function to write to, and the
  environment will have no effect inside the function anyway.  However,
  this does mean that `getfenv(setfenv(f, t))` does not necessarily equal `t`,
  which is incompatible with 5.1 code (a possible workaround would be [1]).
  If `setfenv(f, {})` has an upvalue but no debug info, then this will raise
  an error to prevent inadvertently executing potentially untrusted code in the
  global environment.
  
  It is not normally considered good design to use `setfenv` and `getfenv`
  (one reason they were removed in 5.2).  When possible, consider replacing
  these with `load` or `loadfile`, which are more restrictive and have native
  implementations in 5.2.
  
  This module might be merged into a more general Lua 5.1/5.2 compatibility
  library (e.g. a full reimplementation of Lua 5.2 `_G`).  However,
  `load/loadfile/getfenv/setfenv` perhaps are among the more cumbersome
  functions not to have.

INSTALLATION

  Download compat_env.lua:
  
    wget https://raw.github.com/gist/1654007/compat_env.lua

  Copy compat_env.lua into your LUA_PATH.
  
  Alternately, unpack, test, and install into LuaRocks:
  
     wget https://raw.github.com/gist/1422205/sourceunpack.lua
     lua sourceunpack.lua compat_env.lua
     (cd out && luarocks make)

Related work

  http://lua-users.org/wiki/LuaVersionCompatibility
  https://github.com/stevedonovan/Penlight/blob/master/lua/pl/utils.lua
    - penlight implementations of getfenv/setfenv
  http://lua-users.org/lists/lua-l/2010-06/msg00313.html
    - initial getfenv/setfenv implementation
    
References

  [1] http://lua-users.org/lists/lua-l/2010-06/msg00315.html

Copyright

(c) 2012 David Manura.  Licensed under the same terms as Lua 5.1/5.2 (MIT license).

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

--]]---------------------------------------------------------------------

local M = {_TYPE='module', _NAME='compat_env', _VERSION='0.2.20120124'}

local function check_chunk_type(s, mode)
  local nmode = mode or 'bt' 
  local is_binary = s and #s > 0 and s:byte(1) == 27
  if is_binary and not nmode:match'b' then
    return nil, ("attempt to load a binary chunk (mode is '%s')"):format(mode)
  elseif not is_binary and not nmode:match't' then
    return nil, ("attempt to load a text chunk (mode is '%s')"):format(mode)
  end
  return true
end

local IS_52_LOAD = pcall(load, '')
if IS_52_LOAD then
  M.load     = _G.load
  M.loadfile = _G.loadfile
else
  -- 5.2 style `load` implemented in 5.1
  function M.load(ld, source, mode, env)
    local f
    if type(ld) == 'string' then
      local s = ld
      local ok, err = check_chunk_type(s, mode); if not ok then return ok, err end
      local err; f, err = loadstring(s, source); if not f then return f, err end
    elseif type(ld) == 'function' then
      local ld2 = ld
      if (mode or 'bt') ~= 'bt' then
        local first = ld()
        local ok, err = check_chunk_type(first, mode); if not ok then return ok, err end
        ld2 = function()
          if first then
            local chunk=first; first=nil; return chunk
          else return ld() end
        end
      end
      local err; f, err = load(ld2, source); if not f then return f, err end
    else
      error(("bad argument #1 to 'load' (function expected, got %s)"):format(type(ld)), 2)
    end
    if env then setfenv(f, env) end
    return f
  end

  -- 5.2 style `loadfile` implemented in 5.1
  function M.loadfile(filename, mode, env)
    if (mode or 'bt') ~= 'bt' then
      local ioerr
      local fh, err = io.open(filename, 'rb'); if not fh then return fh, err end
      local function ld() local chunk; chunk,ioerr = fh:read(4096); return chunk end
      local f, err = M.load(ld, filename and '@'..filename, mode, env)
      fh:close()
      if not f then return f, err end
      if ioerr then return nil, ioerr end
      return f
    else
      local f, err = loadfile(filename); if not f then return f, err end
      if env then setfenv(f, env) end
      return f
    end
  end
end

if _G.setfenv then -- Lua 5.1
  M.setfenv = _G.setfenv
  M.getfenv = _G.getfenv
else -- >= Lua 5.2
  -- helper function for `getfenv`/`setfenv`
  local function envlookup(f)
    local name, val
    local up = 0
    local unknown
    repeat
      up=up+1; name, val = debug.getupvalue(f, up)
      if name == '' then unknown = true end
    until name == '_ENV' or name == nil
    if name ~= '_ENV' then
      up = nil
      if unknown then error("upvalues not readable in Lua 5.2 when debug info missing", 3) end
    end
    return (name == '_ENV') and up, val, unknown
  end

  -- helper function for `getfenv`/`setfenv`
  local function envhelper(f, name)
    if type(f) == 'number' then
      if f < 0 then
        error(("bad argument #1 to '%s' (level must be non-negative)"):format(name), 3)
      elseif f < 1 then
        error("thread environments unsupported in Lua 5.2", 3) --[*]
      end
      f = debug.getinfo(f+2, 'f').func
    elseif type(f) ~= 'function' then
      error(("bad argument #1 to '%s' (number expected, got %s)"):format(type(name, f)), 2)
    end
    return f
  end
  -- [*] might simulate with table keyed by coroutine.running()
  
  -- 5.1 style `setfenv` implemented in 5.2
  function M.setfenv(f, t)
    local f = envhelper(f, 'setfenv')
    local up, val, unknown = envlookup(f)
    if up then
      debug.upvaluejoin(f, up, function() return up end, 1) -- unique upvalue [*]
      debug.setupvalue(f, up, t)
    else
      local what = debug.getinfo(f, 'S').what
      if what ~= 'Lua' and what ~= 'main' then -- not Lua func
        error("'setfenv' cannot change environment of given object", 2)
      end -- else ignore no _ENV upvalue (warning: incompatible with 5.1)
    end
  end
  -- [*] http://lua-users.org/lists/lua-l/2010-06/msg00313.html

  -- 5.1 style `getfenv` implemented in 5.2
  function M.getfenv(f)
    if f == 0 or f == nil then return _G end -- simulated behavior
    local f = envhelper(f, 'setfenv')
    local up, val = envlookup(f)
    if not up then return _G end -- simulated behavior [**]
    return val
  end
  -- [**] possible reasons: no _ENV upvalue, C function
end


return M

--[[ FILE rockspec.in

package = 'compat_env'
version = '$(_VERSION)-1'
source = {
  url = 'https://raw.github.com/gist/1654007/$(GITID)/compat_env.lua',
  --url = 'https://raw.github.com/gist/1654007/compat_env.lua', -- latest raw
  --url = 'https://gist.github.com/gists/1654007/download',
  md5 = '$(MD5)'
}
description = {
  summary = 'Lua 5.1/5.2 environment compatibility functions',
  detailed = [=[
    Provides Lua 5.1/5.2 environment related compatibility functions.
    This includes implementations of Lua 5.2 style `load` and `loadfile`
    for use in Lua 5.1.  It also includes Lua 5.1 style `getfenv` and `setfenv`
    for use in Lua 5.2.
  ]=],
  license = 'MIT/X11',
  homepage = 'https://gist.github.com/1654007',
  maintainer = 'David Manura'
}
dependencies = {}  -- Lua 5.1 or 5.2
build = {
  type = 'builtin',
  modules = {
    ['compat_env'] = 'compat_env.lua'
  }
}

--]]---------------------------------------------------------------------

--[[ FILE test.lua

-- test.lua - test suite for compat_env module.

local CL = require 'compat_env'
local load     = CL.load
local loadfile = CL.loadfile
local setfenv  = CL.setfenv
local getfenv  = CL.getfenv

local function checkeq(a, b, e)
  if a ~= b then error(
    'not equal ['..tostring(a)..'] ['..tostring(b)..'] ['..tostring(e)..']')
  end
end
local function checkerr(pat, ok, err)
  assert(not ok, 'checkerr')
  assert(type(err) == 'string' and err:match(pat), err)
end

-- test `load`
checkeq(load('return 2')(), 2)
checkerr('expected near', load'return 2 2')
checkerr('text chunk', load('return 2', nil, 'b'))
checkerr('text chunk', load('', nil, 'b'))
checkerr('binary chunk', load('\027', nil, 't'))
checkeq(load('return 2*x',nil,'bt',{x=5})(), 10)
checkeq(debug.getinfo(load('')).source, '')
checkeq(debug.getinfo(load('', 'foo')).source, 'foo')

-- test `loadfile`
local fh = assert(io.open('tmp.lua', 'wb'))
fh:write('return (...) or x')
fh:close()
checkeq(loadfile('tmp.lua')(2), 2)
checkeq(loadfile('tmp.lua', 't')(2), 2)
checkerr('text chunk', loadfile('tmp.lua', 'b'))
checkeq(loadfile('tmp.lua', nil, {x=3})(), 3)
checkeq(debug.getinfo(loadfile('tmp.lua')).source, '@tmp.lua')
checkeq(debug.getinfo(loadfile('tmp.lua', 't', {})).source, '@tmp.lua')
os.remove'tmp.lua'

-- test `setfenv`/`getfenv`
x = 5
local a,b=true; local function f(c) if a then return x,b,c end end
setfenv(f, {x=3})
checkeq(f(), 3)
checkeq(getfenv(f).x, 3)
checkerr('cannot change', pcall(setfenv, string.len, {})) -- C function
checkeq(getfenv(string.len), _G) -- C function
local function g()
  setfenv(1, {x=4})
  checkeq(getfenv(1).x, 4)
  return x
end
checkeq(g(), 4) -- numeric level
if _G._VERSION ~= 'Lua 5.1' then
  checkerr('unsupported', pcall(setfenv, 0, {}))
end
checkeq(getfenv(0), _G)
checkeq(getfenv(), _G) -- no arg
checkeq(x, 5) -- main unaltered
setfenv(function()end, {}) -- no upvalues, ignore
checkeq(getfenv(function()end), _G) -- no upvaluse
if _G._VERSION ~= 'Lua 5.1' then
  checkeq(getfenv(setfenv(function()end, {})), _G) -- warning: incompatible with 5.1
end
x = nil

print 'OK'

--]]---------------------------------------------------------------------

--[[ FILE CHANGES.txt
0.2.20120124
  Renamed module to compat_env (from compat_load)
  Add getfenv/setfenv functions

0.1.20120121
  Initial public release
--]]

