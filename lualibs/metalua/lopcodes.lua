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

  lopcodes.lua
  Lua 5 virtual machine opcodes in Lua
  This file is part of Yueliang.

  Copyright (c) 2005 Kein-Hong Man <khman@users.sf.net>
  The COPYRIGHT file describes the conditions
  under which this software may be distributed.

  See the ChangeLog for more information.

------------------------------------------------------------------------

  [FF] Slightly modified, mainly to produce Lua 5.1 bytecode.

----------------------------------------------------------------------]]

--[[--------------------------------------------------------------------
-- Notes:
-- * an Instruction is a table with OP, A, B, C, Bx elements; this
--   should allow instruction handling to work with doubles and ints
-- * Added:
--   luaP:Instruction(i): convert field elements to a 4-char string
--   luaP:DecodeInst(x): convert 4-char string into field elements
-- * WARNING luaP:Instruction outputs instructions encoded in little-
--   endian form and field size and positions are hard-coded
----------------------------------------------------------------------]]

module("bytecode", package.seeall)

local function debugf() end

luaP = { }

--[[
===========================================================================
  We assume that instructions are unsigned numbers.
  All instructions have an opcode in the first 6 bits.
  Instructions can have the following fields:
        'A' : 8 bits
        'B' : 9 bits
        'C' : 9 bits
        'Bx' : 18 bits ('B' and 'C' together)
        'sBx' : signed Bx

  A signed argument is represented in excess K; that is, the number
  value is the unsigned value minus K. K is exactly the maximum value
  for that argument (so that -max is represented by 0, and +max is
  represented by 2*max), which is half the maximum for the corresponding
  unsigned argument.
===========================================================================
--]]

luaP.OpMode = {"iABC", "iABx", "iAsBx"}  -- basic instruction format

------------------------------------------------------------------------
-- size and position of opcode arguments.
-- * WARNING size and position is hard-coded elsewhere in this script
------------------------------------------------------------------------
luaP.SIZE_C  = 9
luaP.SIZE_B  = 9
luaP.SIZE_Bx = luaP.SIZE_C + luaP.SIZE_B
luaP.SIZE_A  = 8

luaP.SIZE_OP = 6

luaP.POS_C  = luaP.SIZE_OP
luaP.POS_B  = luaP.POS_C + luaP.SIZE_C
luaP.POS_Bx = luaP.POS_C
luaP.POS_A  = luaP.POS_B + luaP.SIZE_B

--FF from 5.1
luaP.BITRK = 2^(luaP.SIZE_B - 1)
function luaP:ISK(x) return x >= self.BITRK end
luaP.MAXINDEXRK = luaP.BITRK - 1
function luaP:RKASK(x)
   if x < self.BITRK then return x+self.BITRK else return x end
end



------------------------------------------------------------------------
-- limits for opcode arguments.
-- we use (signed) int to manipulate most arguments,
-- so they must fit in BITS_INT-1 bits (-1 for sign)
------------------------------------------------------------------------
-- removed "#if SIZE_Bx < BITS_INT-1" test, assume this script is
-- running on a Lua VM with double or int as LUA_NUMBER

luaP.MAXARG_Bx  = math.ldexp(1, luaP.SIZE_Bx) - 1
luaP.MAXARG_sBx = math.floor(luaP.MAXARG_Bx / 2)  -- 'sBx' is signed

luaP.MAXARG_A = math.ldexp(1, luaP.SIZE_A) - 1
luaP.MAXARG_B = math.ldexp(1, luaP.SIZE_B) - 1
luaP.MAXARG_C = math.ldexp(1, luaP.SIZE_C) - 1

-- creates a mask with 'n' 1 bits at position 'p'
-- MASK1(n,p) deleted
-- creates a mask with 'n' 0 bits at position 'p'
-- MASK0(n,p) deleted

--[[--------------------------------------------------------------------
  Visual representation for reference:

   31     |     |     |          0      bit position
    +-----+-----+-----+----------+
    |  B  |  C  |  A  |  Opcode  |      iABC format
    +-----+-----+-----+----------+
    -  9  -  9  -  8  -    6     -      field sizes
    +-----+-----+-----+----------+
    |   [s]Bx   |  A  |  Opcode  |      iABx | iAsBx format
    +-----+-----+-----+----------+
----------------------------------------------------------------------]]

------------------------------------------------------------------------
-- the following macros help to manipulate instructions
-- * changed to a table object representation, very clean compared to
--   the [nightmare] alternatives of using a number or a string
------------------------------------------------------------------------

-- these accept or return opcodes in the form of string names
function luaP:GET_OPCODE(i) return self.ROpCode[i.OP] end
function luaP:SET_OPCODE(i, o) i.OP = self.OpCode[o] end

function luaP:GETARG_A(i) return i.A end
function luaP:SETARG_A(i, u) i.A = u end

function luaP:GETARG_B(i) return i.B end
function luaP:SETARG_B(i, b) i.B = b end

function luaP:GETARG_C(i) return i.C end
function luaP:SETARG_C(i, b) i.C = b end

function luaP:GETARG_Bx(i) return i.Bx end
function luaP:SETARG_Bx(i, b) i.Bx = b end

function luaP:GETARG_sBx(i) return i.Bx - self.MAXARG_sBx end
function luaP:SETARG_sBx(i, b) i.Bx = b + self.MAXARG_sBx end

function luaP:CREATE_ABC(o,a,b,c)
  return {OP = self.OpCode[o], A = a, B = b, C = c}
end

function luaP:CREATE_ABx(o,a,bc)
  return {OP = self.OpCode[o], A = a, Bx = bc}
end

------------------------------------------------------------------------
-- Bit shuffling stuffs
------------------------------------------------------------------------

if false and pcall (require, 'bit') then
   ------------------------------------------------------------------------
   -- Return a 4-char string little-endian encoded form of an instruction
   ------------------------------------------------------------------------
   function luaP:Instruction(i)
      --FIXME
   end
else
   ------------------------------------------------------------------------   
   -- Version without bit manipulation library.
   ------------------------------------------------------------------------
   local p2 = {1,2,4,8,16,32,64,128,256, 512, 1024, 2048, 4096}
   -- keeps [n] bits from [x]
   local function keep (x, n) return x % p2[n+1] end
   -- shifts bits of [x] [n] places to the right
   local function srb (x,n) return math.floor (x / p2[n+1]) end
   -- shifts bits of [x] [n] places to the left
   local function slb (x,n) return x * p2[n+1] end

   ------------------------------------------------------------------------
   -- Return a 4-char string little-endian encoded form of an instruction
   ------------------------------------------------------------------------
   function luaP:Instruction(i)
      -- printf("Instr->string: %s %s", self.opnames[i.OP], table.tostring(i))
      local c0, c1, c2, c3
      -- change to OP/A/B/C format if needed
      if i.Bx then i.C = keep (i.Bx, 9); i.B = srb (i.Bx, 9) end
      -- c0 = 6B from opcode + 2LSB from A (flushed to MSB)
      c0 = i.OP + slb (keep (i.A, 2), 6) 
      -- c1 = 6MSB from A + 2LSB from C (flushed to MSB)
      c1 = srb (i.A, 2) + slb (keep (i.C, 2), 6)
      -- c2 = 7MSB from C + 1LSB from B (flushed to MSB)
      c2 = srb (i.C, 2) + slb (keep (i.B, 1), 7)
      -- c3 = 8MSB from B
      c3 = srb (i.B, 1)
      --printf ("Instruction:   %s %s", self.opnames[i.OP], tostringv (i))
      --printf ("Bin encoding:  %x %x %x %x", c0, c1, c2, c3)  
      return string.char(c0, c1, c2, c3)
   end
end
------------------------------------------------------------------------
-- decodes a 4-char little-endian string into an instruction struct
------------------------------------------------------------------------
function luaP:DecodeInst(x)
  error "Not implemented"
end

------------------------------------------------------------------------
-- invalid register that fits in 8 bits
------------------------------------------------------------------------
luaP.NO_REG = luaP.MAXARG_A

------------------------------------------------------------------------
-- R(x) - register
-- Kst(x) - constant (in constant table)
-- RK(x) == if x < MAXSTACK then R(x) else Kst(x-MAXSTACK)
------------------------------------------------------------------------

------------------------------------------------------------------------
-- grep "ORDER OP" if you change these enums
------------------------------------------------------------------------

--[[--------------------------------------------------------------------
Lua virtual machine opcodes (enum OpCode):
------------------------------------------------------------------------
name          args    description
------------------------------------------------------------------------
OP_MOVE       A B     R(A) := R(B)
OP_LOADK      A Bx    R(A) := Kst(Bx)
OP_LOADBOOL   A B C   R(A) := (Bool)B; if (C) PC++
OP_LOADNIL    A B     R(A) := ... := R(B) := nil
OP_GETUPVAL   A B     R(A) := UpValue[B]
OP_GETGLOBAL  A Bx    R(A) := Gbl[Kst(Bx)]
OP_GETTABLE   A B C   R(A) := R(B)[RK(C)]
OP_SETGLOBAL  A Bx    Gbl[Kst(Bx)] := R(A)
OP_SETUPVAL   A B     UpValue[B] := R(A)
OP_SETTABLE   A B C   R(A)[RK(B)] := RK(C)
OP_NEWTABLE   A B C   R(A) := {} (size = B,C)
OP_SELF       A B C   R(A+1) := R(B); R(A) := R(B)[RK(C)]
OP_ADD        A B C   R(A) := RK(B) + RK(C)
OP_SUB        A B C   R(A) := RK(B) - RK(C)
OP_MUL        A B C   R(A) := RK(B) * RK(C)
OP_DIV        A B C   R(A) := RK(B) / RK(C)
OP_POW        A B C   R(A) := RK(B) ^ RK(C)
OP_UNM        A B     R(A) := -R(B)
OP_NOT        A B     R(A) := not R(B)
OP_CONCAT     A B C   R(A) := R(B).. ... ..R(C)
OP_JMP        sBx     PC += sBx
OP_EQ         A B C   if ((RK(B) == RK(C)) ~= A) then pc++
OP_LT         A B C   if ((RK(B) <  RK(C)) ~= A) then pc++
OP_LE         A B C   if ((RK(B) <= RK(C)) ~= A) then pc++
OP_TEST       A B C   if (R(B) <=> C) then R(A) := R(B) else pc++
OP_CALL       A B C   R(A), ... ,R(A+C-2) := R(A)(R(A+1), ... ,R(A+B-1))
OP_TAILCALL   A B C   return R(A)(R(A+1), ... ,R(A+B-1))
OP_RETURN     A B     return R(A), ... ,R(A+B-2)  (see note)
OP_FORLOOP    A sBx   R(A)+=R(A+2); if R(A) <?= R(A+1) then PC+= sBx
OP_TFORLOOP   A C     R(A+2), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2));
                      if R(A+2) ~= nil then pc++
OP_TFORPREP   A sBx   if type(R(A)) == table then R(A+1):=R(A), R(A):=next;
                      PC += sBx
OP_SETLIST    A Bx    R(A)[Bx-Bx%FPF+i] := R(A+i), 1 <= i <= Bx%FPF+1
OP_SETLISTO   A Bx    (see note)
OP_CLOSE      A       close all variables in the stack up to (>=) R(A)
OP_CLOSURE    A Bx    R(A) := closure(KPROTO[Bx], R(A), ... ,R(A+n))
----------------------------------------------------------------------]]

luaP.opnames = {}  -- opcode names
luaP.OpCode = {}   -- lookup name -> number
luaP.ROpCode = {}  -- lookup number -> name

local i = 0
for v in string.gfind([[
MOVE -- 0
LOADK
LOADBOOL
LOADNIL
GETUPVAL
GETGLOBAL -- 5
GETTABLE
SETGLOBAL
SETUPVAL
SETTABLE
NEWTABLE -- 10
SELF
ADD
SUB
MUL
DIV -- 15
MOD
POW
UNM
NOT
LEN -- 20
CONCAT
JMP
EQ
LT
LE -- 25
TEST
TESTSET
CALL
TAILCALL
RETURN -- 30
FORLOOP
FORPREP
TFORLOOP
SETLIST
CLOSE -- 35
CLOSURE
VARARG
]], "[%a]+") do
  local n = "OP_"..v
  luaP.opnames[i] = v
  luaP.OpCode[n] = i
  luaP.ROpCode[i] = n
  i = i + 1
end
luaP.NUM_OPCODES = i

--[[
===========================================================================
  Notes:
  (1) In OP_CALL, if (B == 0) then B = top. C is the number of returns - 1,
      and can be 0: OP_CALL then sets 'top' to last_result+1, so
      next open instruction (OP_CALL, OP_RETURN, OP_SETLIST) may use 'top'.

  (2) In OP_RETURN, if (B == 0) then return up to 'top'

  (3) For comparisons, B specifies what conditions the test should accept.

  (4) All 'skips' (pc++) assume that next instruction is a jump

  (5) OP_SETLISTO is used when the last item in a table constructor is a
      function, so the number of elements set is up to top of stack
===========================================================================
--]]

------------------------------------------------------------------------
-- masks for instruction properties
------------------------------------------------------------------------
-- was enum OpModeMask:
luaP.OpModeBreg = 2  -- B is a register
luaP.OpModeBrk  = 3  -- B is a register/constant
luaP.OpModeCrk  = 4  -- C is a register/constant
luaP.OpModesetA = 5  -- instruction set register A
luaP.OpModeK    = 6  -- Bx is a constant
luaP.OpModeT    = 1  -- operator is a test

------------------------------------------------------------------------
-- get opcode mode, e.g. "iABC"
------------------------------------------------------------------------
function luaP:getOpMode(m)
   --printv(m)
   --printv(self.OpCode[m])
   --printv(self.opmodes [self.OpCode[m]+1])
   return self.OpMode[tonumber(string.sub(self.opmodes[self.OpCode[m] + 1], 7, 7))]
end

------------------------------------------------------------------------
-- test an instruction property flag
-- * b is a string, e.g. "OpModeBreg"
------------------------------------------------------------------------
function luaP:testOpMode(m, b)
  return (string.sub(self.opmodes[self.OpCode[m] + 1], self[b], self[b]) == "1")
end

-- number of list items to accumulate before a SETLIST instruction
-- (must be a power of 2)
-- * used in lparser, lvm, ldebug, ltests
luaP.LFIELDS_PER_FLUSH = 50 --FF updated to match 5.1

-- luaP_opnames[] is set above, as the luaP.opnames table
-- opmode(t,b,bk,ck,sa,k,m) deleted

--[[--------------------------------------------------------------------
  Legend for luaP:opmodes:
  1 T  -> T (is a test?)
  2 B  -> B is a register
  3 b  -> B is an RK register/constant combination
  4 C  -> C is an RK register/constant combination
  5 A  -> register A is set by the opcode
  6 K  -> Bx is a constant
  7 m  -> 1 if iABC  layout,
          2 if iABx  layout, 
          3 if iAsBx layout
----------------------------------------------------------------------]]

luaP.opmodes = {
-- TBbCAKm      opcode
  "0100101", -- OP_MOVE      0
  "0000112", -- OP_LOADK
  "0000101", -- OP_LOADBOOL
  "0100101", -- OP_LOADNIL
  "0000101", -- OP_GETUPVAL
  "0000112", -- OP_GETGLOBAL 5
  "0101101", -- OP_GETTABLE
  "0000012", -- OP_SETGLOBAL
  "0000001", -- OP_SETUPVAL
  "0011001", -- OP_SETTABLE
  "0000101", -- OP_NEWTABLE 10
  "0101101", -- OP_SELF
  "0011101", -- OP_ADD
  "0011101", -- OP_SUB
  "0011101", -- OP_MUL
  "0011101", -- OP_DIV      15
  "0011101", -- OP_MOD
  "0011101", -- OP_POW
  "0100101", -- OP_UNM
  "0100101", -- OP_NOT
  "0100101", -- OP_LEN      20
  "0101101", -- OP_CONCAT
  "0000003", -- OP_JMP
  "1011001", -- OP_EQ
  "1011001", -- OP_LT
  "1011001", -- OP_LE       25
  "1000101", -- OP_TEST
  "1100101", -- OP_TESTSET
  "0000001", -- OP_CALL
  "0000001", -- OP_TAILCALL
  "0000001", -- OP_RETURN   30
  "0000003", -- OP_FORLOOP
  "0000103", -- OP_FORPREP
  "1000101", -- OP_TFORLOOP
  "0000001", -- OP_SETLIST
  "0000001", -- OP_CLOSE    35
  "0000102", -- OP_CLOSURE
  "0000101"  -- OP_VARARG
}
