----------------------------------------------------------------------
--
-- WARNING! You're entering a hackish area, proceed at your own risks!
--
-- This code results from the borrowing, then ruthless abuse, of
-- Yueliang's implementation of Lua 5.0 compiler. I claim
-- responsibility for all of the ugly, dirty stuff that you might spot
-- in it.
--
-- Eventually, this code will be rewritten, either in Lua or more
-- probably in C. Meanwhile, if you're interested into digging
-- metalua's sources, this is not the best part to invest your time
-- on.
--
-- End of warning.
--
----------------------------------------------------------------------

--[[--------------------------------------------------------------------

  $Id$

  ldump.lua
  Save bytecodes in Lua
  This file is part of Yueliang.

  Copyright (c) 2005 Kein-Hong Man <khman@users.sf.net>
  The COPYRIGHT file describes the conditions
  under which this software may be distributed.

------------------------------------------------------------------------

  [FF] Slightly modified, mainly to produce Lua 5.1 bytecode.

----------------------------------------------------------------------]]

--[[--------------------------------------------------------------------
-- Notes:
-- * LUA_NUMBER (double), byte order (little endian) and some other
--   header values hard-coded; see other notes below...
-- * One significant difference is that instructions are still in table
--   form (with OP/A/B/C/Bx fields) and luaP:Instruction() is needed to
--   convert them into 4-char strings
-- * Deleted:
--   luaU:DumpVector: folded into DumpLines, DumpCode
-- * Added:
--   luaU:endianness() (from lundump.c)
--   luaU:make_setS: create a chunk writer that writes to a string
--   luaU:make_setF: create a chunk writer that writes to a file
--     (lua.h contains a typedef for a Chunkwriter pointer, and
--      a Lua-based implementation exists, writer() in lstrlib.c)
--   luaU:from_double(x): encode double value for writing
--   luaU:from_int(x): encode integer value for writing
--     (error checking is limited for these conversion functions)
--     (double conversion does not support denormals or NaNs)
--   luaU:ttype(o) (from lobject.h)
----------------------------------------------------------------------]]

module("bytecode", package.seeall)

format = { }
format.header = string.dump(function()end):sub(1, 12)
format.little_endian, format.int_size, 
format.size_t_size,   format.instr_size, 
format.number_size,   format.integral = format.header:byte(7, 12)
format.little_endian = format.little_endian~=0
format.integral      = format.integral     ~=0

assert(format.integral or format.number_size==8, "Number format not supported by dumper")
assert(format.little_endian, "Big endian architectures not supported by dumper")

--requires luaP
luaU = {}

-- constants used by dumper
luaU.LUA_TNIL     = 0
luaU.LUA_TBOOLEAN = 1
luaU.LUA_TNUMBER  = 3 -- (all in lua.h)
luaU.LUA_TSTRING  = 4
luaU.LUA_TNONE   = -1

-- definitions for headers of binary files
--luaU.LUA_SIGNATURE = "\27Lua"   -- binary files start with "<esc>Lua"
--luaU.VERSION = 81               -- 0x50; last format change was in 5.0
--luaU.FORMAT_VERSION = 0         -- 0 is official version. yeah I know I'm a liar.

-- a multiple of PI for testing native format
-- multiplying by 1E7 gives non-trivial integer values
--luaU.TEST_NUMBER = 3.14159265358979323846E7

--[[--------------------------------------------------------------------
-- Additional functions to handle chunk writing
-- * to use make_setS and make_setF, see test_ldump.lua elsewhere
----------------------------------------------------------------------]]

------------------------------------------------------------------------
-- works like the lobject.h version except that TObject used in these
-- scripts only has a 'value' field, no 'tt' field (native types used)
------------------------------------------------------------------------
function luaU:ttype(o)
  local tt = type(o.value)
  if     tt == "number"  then return self.LUA_TNUMBER
  elseif tt == "string"  then return self.LUA_TSTRING
  elseif tt == "nil"     then return self.LUA_TNIL
  elseif tt == "boolean" then return self.LUA_TBOOLEAN
  else
    return self.LUA_TNONE  -- the rest should not appear
  end
end

------------------------------------------------------------------------
-- create a chunk writer that writes to a string
-- * returns the writer function and a table containing the string
-- * to get the final result, look in buff.data
------------------------------------------------------------------------
function luaU:make_setS()
  local buff = {}
        buff.data = ""
  local writer =
    function(s, buff)  -- chunk writer
      if not s then return end
      buff.data = buff.data..s
    end
  return writer, buff
end

------------------------------------------------------------------------
-- create a chunk writer that writes to a file
-- * returns the writer function and a table containing the file handle
-- * if a nil is passed, then writer should close the open file
------------------------------------------------------------------------
function luaU:make_setF(filename)
  local buff = {}
        buff.h = io.open(filename, "wb")
  if not buff.h then return nil end
  local writer =
    function(s, buff)  -- chunk writer
      if not buff.h then return end
      if not s then buff.h:close(); return end
      buff.h:write(s)
    end
  return writer, buff
end

-----------------------------------------------------------------------
-- converts a IEEE754 double number to an 8-byte little-endian string
-- * luaU:from_double() and luaU:from_int() are from ChunkBake project
-- * supports +/- Infinity, but not denormals or NaNs
-----------------------------------------------------------------------
function luaU:from_double(x)
  local function grab_byte(v)
    return math.floor(v / 256),
           string.char(math.mod(math.floor(v), 256))
  end
  local sign = 0
  if x < 0 then sign = 1; x = -x end
  local mantissa, exponent = math.frexp(x)
  if x == 0 then -- zero
    mantissa, exponent = 0, 0
  elseif x == 1/0 then
    mantissa, exponent = 0, 2047
  else
    mantissa = (mantissa * 2 - 1) * math.ldexp(0.5, 53)
    exponent = exponent + 1022
  end
  local v, byte = "" -- convert to bytes
  x = mantissa
  for i = 1,6 do
    x, byte = grab_byte(x); v = v..byte -- 47:0
  end
  x, byte = grab_byte(exponent * 16 + x); v = v..byte -- 55:48
  x, byte = grab_byte(sign * 128 + x); v = v..byte -- 63:56
  return v
end

-----------------------------------------------------------------------
-- converts a number to a little-endian 32-bit integer string
-- * input value assumed to not overflow, can be signed/unsigned
-----------------------------------------------------------------------
function luaU:from_int(x, size)
  local v = ""
  x = math.floor(x)
  if x >= 0 then
    for i = 1, size do
      v = v..string.char(math.mod(x, 256)); x = math.floor(x / 256)
    end
  else -- x < 0
    x = -x
    local carry = 1
    for i = 1, size do
      local c = 255 - math.mod(x, 256) + carry
      if c == 256 then c = 0; carry = 1 else carry = 0 end
      v = v..string.char(c); x = math.floor(x / 256)
    end
  end
  return v
end

--[[--------------------------------------------------------------------
-- Functions to make a binary chunk
-- * many functions have the size parameter removed, since output is
--   in the form of a string and some sizes are implicit or hard-coded
-- * luaU:DumpVector has been deleted (used in DumpCode & DumpLines)
----------------------------------------------------------------------]]

------------------------------------------------------------------------
-- dump a block of literal bytes
------------------------------------------------------------------------
function luaU:DumpLiteral(s, D) self:DumpBlock(s, D) end

--[[--------------------------------------------------------------------
-- struct DumpState:
--   L  -- lua_State (not used in this script)
--   write  -- lua_Chunkwriter (chunk writer function)
--   data  -- void* (chunk writer context or data already written)
----------------------------------------------------------------------]]

------------------------------------------------------------------------
-- dumps a block of bytes
-- * lua_unlock(D.L), lua_lock(D.L) deleted
------------------------------------------------------------------------
function luaU:DumpBlock(b, D) D.write(b, D.data) end

------------------------------------------------------------------------
-- dumps a single byte
------------------------------------------------------------------------
function luaU:DumpByte(y, D)
  self:DumpBlock(string.char(y), D)
end

------------------------------------------------------------------------
-- dumps a 32-bit signed integer (for int)
------------------------------------------------------------------------
function luaU:DumpInt(x, D)
  self:DumpBlock(self:from_int(x, format.int_size), D)
end

------------------------------------------------------------------------
-- dumps a 32-bit unsigned integer (for size_t)
------------------------------------------------------------------------
function luaU:DumpSize(x, D)
  self:DumpBlock(self:from_int(x, format.size_t_size), D)
end

------------------------------------------------------------------------
-- dumps a LUA_NUMBER (hard-coded as a double)
------------------------------------------------------------------------
function luaU:DumpNumber(x, D)
   if format.integral then
      self:DumpBlock(self:from_int(x, format.number_size), D)
   else
      self:DumpBlock(self:from_double(x), D)
   end
end

------------------------------------------------------------------------
-- dumps a Lua string
------------------------------------------------------------------------
function luaU:DumpString(s, D)
  if s == nil then
    self:DumpSize(0, D)
  else
    s = s.."\0"  -- include trailing '\0'
    self:DumpSize(string.len(s), D)
    self:DumpBlock(s, D)
  end
end

------------------------------------------------------------------------
-- dumps instruction block from function prototype
------------------------------------------------------------------------
function luaU:DumpCode(f, D)
  local n = f.sizecode
  self:DumpInt(n, D)
  --was DumpVector
  for i = 0, n - 1 do
    self:DumpBlock(luaP:Instruction(f.code[i]), D)
  end
end

------------------------------------------------------------------------
-- dumps local variable names from function prototype
------------------------------------------------------------------------
function luaU:DumpLocals(f, D)
  local n = f.sizelocvars
  self:DumpInt(n, D)
  for i = 0, n - 1 do
    -- Dirty temporary fix: 
    -- `Stat{ } keeps properly count of the number of local vars,
    -- but fails to keep score of their debug info (names).
    -- It therefore might happen that #f.localvars < f.sizelocvars, or
    -- that a variable's startpc and endpc fields are left unset.
    -- FIXME: This might not be needed anymore, check the bug report
    --        by J. Belmonte.
    local var = f.locvars[i]
    if not var then break end 
    -- printf("[DUMPLOCALS] dumping local var #%i = %s", i, table.tostring(var))
    self:DumpString(var.varname, D)
    self:DumpInt(var.startpc or 0, D)
    self:DumpInt(var.endpc or 0, D)
  end
end

------------------------------------------------------------------------
-- dumps line information from function prototype
------------------------------------------------------------------------
function luaU:DumpLines(f, D)
  local n = f.sizelineinfo
  self:DumpInt(n, D)
  --was DumpVector
  for i = 0, n - 1 do
    self:DumpInt(f.lineinfo[i], D)  -- was DumpBlock
    --print(i, f.lineinfo[i])
  end
end

------------------------------------------------------------------------
-- dump upvalue names from function prototype
------------------------------------------------------------------------
function luaU:DumpUpvalues(f, D)
  local n = f.sizeupvalues
  self:DumpInt(n, D)
  for i = 0, n - 1 do
    self:DumpString(f.upvalues[i], D)
  end
end

------------------------------------------------------------------------
-- dump constant pool from function prototype
-- * nvalue(o) and tsvalue(o) macros removed
------------------------------------------------------------------------
function luaU:DumpConstants(f, D)
  local n = f.sizek
  self:DumpInt(n, D)
  for i = 0, n - 1 do
    local o = f.k[i]  -- TObject
    local tt = self:ttype(o)
    assert (tt >= 0)
    self:DumpByte(tt, D)
    if tt == self.LUA_TNUMBER then
       self:DumpNumber(o.value, D)
    elseif tt == self.LUA_TSTRING then
       self:DumpString(o.value, D)
    elseif tt == self.LUA_TBOOLEAN then
       self:DumpByte (o.value and 1 or 0, D)
    elseif tt == self.LUA_TNIL then
    else
      assert(false)  -- cannot happen
    end
  end
end


function luaU:DumpProtos (f, D)
  local n = f.sizep
  assert (n)
  self:DumpInt(n, D)
  for i = 0, n - 1 do
    self:DumpFunction(f.p[i], f.source, D)
  end
end

function luaU:DumpDebug(f, D)
  self:DumpLines(f, D)
  self:DumpLocals(f, D)
  self:DumpUpvalues(f, D)
end


------------------------------------------------------------------------
-- dump child function prototypes from function prototype
--FF completely reworked for 5.1 format
------------------------------------------------------------------------
function luaU:DumpFunction(f, p, D)
   -- print "Dumping function:"
   -- table.print(f, 60)

  local source = f.source
  if source == p then source = nil end
  self:DumpString(source, D)
  self:DumpInt(f.lineDefined, D)
  self:DumpInt(f.lastLineDefined or 42, D)
  self:DumpByte(f.nups, D)
  self:DumpByte(f.numparams, D)
  self:DumpByte(f.is_vararg, D)
  self:DumpByte(f.maxstacksize, D)
  self:DumpCode(f, D)
  self:DumpConstants(f, D)
  self:DumpProtos( f, D)
  self:DumpDebug(f, D)
end

------------------------------------------------------------------------
-- dump Lua header section (some sizes hard-coded)
--FF: updated for version 5.1
------------------------------------------------------------------------
function luaU:DumpHeader(D)
  self:DumpLiteral(format.header, D)
end

------------------------------------------------------------------------
-- dump function as precompiled chunk
-- * w, data are created from make_setS, make_setF
--FF: suppressed extraneous [L] param
------------------------------------------------------------------------
function luaU:dump (Main, w, data)
  local D = {}  -- DumpState
  D.write = w
  D.data = data
  self:DumpHeader(D)
  self:DumpFunction(Main, nil, D)
  -- added: for a chunk writer writing to a file, this final call with
  -- nil data is to indicate to the writer to close the file
  D.write(nil, D.data)
end

------------------------------------------------------------------------
-- find byte order (from lundump.c)
-- * hard-coded to little-endian
------------------------------------------------------------------------
function luaU:endianness()
  return 1
end

-- FIXME: ugly concat-base generation in [make_setS], bufferize properly! 
function dump_string (proto)
   local writer, buff = luaU:make_setS()
   luaU:dump (proto, writer, buff)
   return buff.data
end

-- FIXME: [make_setS] sucks, perform synchronous file writing
-- Now unused
function dump_file (proto, filename)
   local writer, buff = luaU:make_setS()
   luaU:dump (proto, writer, buff)
   local file = io.open (filename, "wb")
   file:write (buff.data)
   io.close(file)
   if UNIX_SHARPBANG then os.execute ("chmod a+x "..filename) end
end