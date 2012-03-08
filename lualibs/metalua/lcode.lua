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

  lcode.lua
  Lua 5 code generator in Lua
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
-- * one function manipulate a pointer argument with a simple data type
--   (can't be emulated by a table, ambiguous), now returns that value:
--   luaK:concat(fs, l1, l2)
-- * some function parameters changed to boolean, additional code
--   translates boolean back to 1/0 for instruction fields
-- * Added:
--   luaK:ttisnumber(o) (from lobject.h)
--   luaK:nvalue(o) (from lobject.h)
--   luaK:setnilvalue(o) (from lobject.h)
--   luaK:setsvalue(o) (from lobject.h)
--   luaK:setnvalue(o) (from lobject.h)
--   luaK:sethvalue(o) (from lobject.h)
----------------------------------------------------------------------]]

module("bytecode", package.seeall)

local function debugf() end

luaK = {}

luaK.MAXSTACK    = 250        -- (llimits.h, used in lcode.lua)
luaK.LUA_MULTRET = -1         -- (lua.h)

------------------------------------------------------------------------
-- Marks the end of a patch list. It is an invalid value both as an absolute
-- address, and as a list link (would link an element to itself).
------------------------------------------------------------------------
luaK.NO_JUMP = -1

--FF 5.1
function luaK:isnumeral(e)
   return e.k=="VKNUM" and e.t==self.NO_JUMP and e.t==self.NO_JUMP
end

------------------------------------------------------------------------
-- emulation of TObject macros (these are from lobject.h)
-- * TObject is a table since lcode passes references around
-- * tt member field removed, using Lua's type() instead
------------------------------------------------------------------------
function luaK:ttisnumber(o)
  if o then return type(o.value) == "number" else return false end
end
function luaK:nvalue(o) return o.value end
function luaK:setnilvalue(o) o.value = nil end
function luaK:setsvalue(o, s) o.value = s end
luaK.setnvalue = luaK.setsvalue
luaK.sethvalue = luaK.setsvalue

------------------------------------------------------------------------
-- returns the instruction object for given e (expdesc)
------------------------------------------------------------------------
function luaK:getcode(fs, e)
  return fs.f.code[e.info]
end

------------------------------------------------------------------------
-- codes an instruction with a signed Bx (sBx) field
------------------------------------------------------------------------
function luaK:codeAsBx(fs, o, A, sBx)
  return self:codeABx(fs, o, A, sBx + luaP.MAXARG_sBx)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:hasjumps(e)
  return e.t ~= e.f
end

------------------------------------------------------------------------
-- FF updated 5.1
------------------------------------------------------------------------
function luaK:_nil(fs, from, n)
   if fs.pc > fs.lasttarget then  -- no jumps to current position?
      if fs.pc == 0 then return end --function start, positions are already clean
      local previous = fs.f.code[fs.pc - 1]
      if luaP:GET_OPCODE(previous) == "OP_LOADNIL" then
         local pfrom = luaP:GETARG_A(previous)
         local pto = luaP:GETARG_B(previous)
         if pfrom <= from and from <= pto + 1 then  -- can connect both?
            if from + n - 1 > pto then
               luaP:SETARG_B(previous, from + n - 1)
            end
            return
         end
      end
   end
   self:codeABC(fs, "OP_LOADNIL", from, from + n - 1, 0)  -- else no optimization
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:jump(fs)
  local jpc = fs.jpc  -- save list of jumps to here
  fs.jpc = self.NO_JUMP
  local j = self:codeAsBx(fs, "OP_JMP", 0, self.NO_JUMP)
  return self:concat(fs, j, jpc)  -- keep them on hold
end

--FF 5.1
function luaK:ret (fs, first, nret)
   luaK:codeABC (fs, "OP_RETURN", first, nret+1, 0)
end


------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:condjump(fs, op, A, B, C)
  self:codeABC(fs, op, A, B, C)
  return self:jump(fs)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:fixjump(fs, pc, dest)
  local jmp = fs.f.code[pc]
  local offset = dest - (pc + 1)
  assert(dest ~= self.NO_JUMP)
  if math.abs(offset) > luaP.MAXARG_sBx then
    luaX:syntaxerror(fs.ls, "control structure too long")
  end
  luaP:SETARG_sBx(jmp, offset)
end

------------------------------------------------------------------------
-- returns current 'pc' and marks it as a jump target (to avoid wrong
-- optimizations with consecutive instructions not in the same basic block).
------------------------------------------------------------------------
function luaK:getlabel(fs)
  fs.lasttarget = fs.pc
  return fs.pc
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:getjump(fs, pc)
  local offset = luaP:GETARG_sBx(fs.f.code[pc])
  if offset == self.NO_JUMP then  -- point to itself represents end of list
    return self.NO_JUMP  -- end of list
  else
    return (pc + 1) + offset  -- turn offset into absolute position
  end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:getjumpcontrol(fs, pc)
  local pi = fs.f.code[pc]
  local ppi = fs.f.code[pc - 1]
  if pc >= 1 and luaP:testOpMode(luaP:GET_OPCODE(ppi), "OpModeT") then
    return ppi
  else
    return pi
  end
end

------------------------------------------------------------------------
-- check whether list has any jump that do not produce a value
-- (or produce an inverted value)
------------------------------------------------------------------------
--FF updated 5.1
function luaK:need_value(fs, list, cond)
  while list ~= self.NO_JUMP do
    local i = self:getjumpcontrol(fs, list)
    if luaP:GET_OPCODE(i) ~= "OP_TESTSET" or
       luaP:GETARG_A(i) ~= luaP.NO_REG or
       luaP:GETARG_C(i) ~= cond then
      return true
    end
    list = self:getjump(fs, list)
  end
  return false  -- not found
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
--FF updated 5.1
function luaK:patchtestreg(fs, node, reg)
   assert(reg) -- pour assurer, vu que j'ai ajoute un parametre p/r a 5.0
   local i = self:getjumpcontrol(fs, node)
   if luaP:GET_OPCODE(i) ~= "OP_TESTSET" then 
      return false end -- cannot patch other instructions
   if reg ~= luaP.NO_REG and reg ~= luaP:GETARG_B(i) then
      luaP:SETARG_A(i, reg)
   else 
      -- no register to put value or register already has the value
      luaP:SET_OPCODE(i, "OP_TEST")
      luaP:SETARG_A(i, luaP:GETARG_B(i))
      luaP:SETARG_B(i, 0)
      luaP:SETARG_C(i, luaP:GETARG_C(i))
   end
   return true
end

--FF added 5.1
function luaK:removevalues (fs, list)
   while list ~= self.NO_JUMP do
      self:patchtestreg (fs, list, luaP.NO_REG)
      list = self:getjump (fs, list)
   end
end

------------------------------------------------------------------------
-- FF updated 5.1
------------------------------------------------------------------------
function luaK:patchlistaux(fs, list, vtarget, reg, dtarget)
   while list ~= self.NO_JUMP do
      local _next = self:getjump(fs, list)
      if self:patchtestreg (fs, list, reg) then
         self:fixjump(fs, list, vtarget)
      else
         self:fixjump (fs, list, dtarget)
      end
      list = _next
   end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:dischargejpc(fs)
  self:patchlistaux(fs, fs.jpc, fs.pc, luaP.NO_REG, fs.pc)
  fs.jpc = self.NO_JUMP
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:patchlist(fs, list, target)
  if target == fs.pc then
    self:patchtohere(fs, list)
  else
    assert(target < fs.pc)
    self:patchlistaux(fs, list, target, luaP.NO_REG, target)
  end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:patchtohere(fs, list)
  self:getlabel(fs)
  fs.jpc = self:concat(fs, fs.jpc, list)
end

------------------------------------------------------------------------
-- * l1 was a pointer, now l1 is returned and callee assigns the value
------------------------------------------------------------------------
function luaK:concat(fs, l1, l2)
  if l2 == self.NO_JUMP then return l1  -- unchanged
  elseif l1 == self.NO_JUMP then
    return l2  -- changed
  else
    local list = l1
    local _next = self:getjump(fs, list)
    while _next ~= self.NO_JUMP do  -- find last element
      list = _next
      _next = self:getjump(fs, list)
    end
    self:fixjump(fs, list, l2)
  end
  return l1  -- unchanged
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:checkstack(fs, n)
  local newstack = fs.freereg + n
  if newstack > fs.f.maxstacksize then
    if newstack >= luaK.MAXSTACK then
      luaX:syntaxerror(fs.ls, "function or expression too complex")
    end
    fs.f.maxstacksize = newstack
  end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:reserveregs(fs, n)
  self:checkstack(fs, n)
  fs.freereg = fs.freereg + n
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:freereg(fs, reg)
  if not luaP:ISK (reg) and reg >= fs.nactvar then
    fs.freereg = fs.freereg - 1
    assert(reg == fs.freereg, 
           string.format("reg=%i, fs.freereg=%i", reg, fs.freereg))
  end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:freeexp(fs, e)
  if e.k == "VNONRELOC" then
    self:freereg(fs, e.info)
  end
end

------------------------------------------------------------------------
-- k is a constant, v is... what?
-- fs.h is a hash value --> index in f.k
------------------------------------------------------------------------
-- * luaH_get, luaH_set deleted; direct table access used instead
-- * luaO_rawequalObj deleted in first assert
-- * setobj2n deleted in assignment of v to f.k table
------------------------------------------------------------------------
--FF radically updated, not completely understood
function luaK:addk(fs, k, v)
   local idx = fs.h[k.value]
   local f = fs.f
--   local oldsize = f.sizek
   if self:ttisnumber (idx) then
      --TODO this assert currently FAILS
      --assert(fs.f.k[self:nvalue(idx)] == v)
      return self:nvalue(idx)
   else  -- constant not found; create a new entry
      do
         local t = type (v.value)
         assert(t=="nil" or t=="string" or t=="number" or t=="boolean")
      end
      --debugf("[const: k[%i] = %s ]", fs.nk, tostringv(v.value))
      fs.f.k[fs.nk] = v
      fs.h[k.value] = { }
      self:setnvalue(fs.h[k.value], fs.nk)
      local nk = fs.nk
      fs.nk = fs.nk+1
      return nk
   end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:stringK(fs, s)
   assert (type(s)=="string")
   local o = {}  -- TObject
   self:setsvalue(o, s)
   return self:addk(fs, o, o)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:numberK(fs, r)
   assert (type(r)=="number")
  local o = {}  -- TObject
  self:setnvalue(o, r)
  return self:addk(fs, o, o)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:boolK(fs, r)
   assert (type(r)=="boolean")
   local o = {}  -- TObject
   self:setnvalue(o, r)
   return self:addk(fs, o, o)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:nilK(fs)
  local k, v = {}, {}  -- TObject
  self:setnilvalue(v)
  self:sethvalue(k, fs.h)  -- cannot use nil as key; instead use table itself
  return self:addk(fs, k, v)
end


--FF 5.1
function luaK:setreturns (fs, e, nresults)
   if e.k == "VCALL" then  -- expression is an open function call?
      luaP:SETARG_C(self:getcode(fs, e), nresults + 1)
   elseif e.k == "VVARARG" then
      luaP:SETARG_B (self:getcode (fs, e), nresults + 1)
      luaP:SETARG_A (self:getcode (fs, e), fs.freereg)
      self:reserveregs (fs, 1)
   end
end

--FF 5.1
function luaK:setmultret (fs, e)
   self:setreturns (fs, e, self.LUA_MULTRET)
end

--FF 5.1
function luaK:setoneret (fs, e)
   if e.k == "VCALL" then  -- expression is an open function call?
      e.k = "VNONRELOC"
      e.info = luaP:GETARG_A(self:getcode(fs, e))
   elseif e.k == "VVARARG" then
      luaP:SETARG_B (self:getcode (fs, e), 2)
      e.k = "VRELOCABLE"
   end
end


------------------------------------------------------------------------
--FF deprecated in 5.1
------------------------------------------------------------------------
function luaK:setcallreturns(fs, e, nresults)
   assert (false, "setcallreturns deprecated")
   --print "SCR:"
   --printv(e)
   --printv(self:getcode(fs, e))
   if e.k == "VCALL" then  -- expression is an open function call?
      luaP:SETARG_C(self:getcode(fs, e), nresults + 1)
      if nresults == 1 then  -- 'regular' expression?
         e.k = "VNONRELOC"
         e.info = luaP:GETARG_A(self:getcode(fs, e))
      end
   elseif e.k == "VVARARG" then
      --printf("Handle vararg return on expr %s, whose code is %s", 
      --       tostringv(e), tostringv(self:getcode(fs, e)))
      if nresults == 1 then
         luaP:SETARG_B (self:getcode (fs, e), 2)
         e.k = "VRELOCABLE"
--FIXME: why no SETARG_A???
      else
         luaP:SETARG_B (self:getcode (fs, e), nresults + 1)
         luaP:SETARG_A (self:getcode (fs, e), fs.freereg)
         self:reserveregs (fs, 1)
      --printf("Now code is %s", tostringv(self:getcode(fs, e)))
      end
   end
end

------------------------------------------------------------------------
-- Ajoute le code pour effectuer l'extraction de la locvar/upval/globvar
-- /idx, sachant
------------------------------------------------------------------------
function luaK:dischargevars(fs, e)
--printf("\ndischargevars\n")
  local k = e.k
  if k == "VLOCAL" then
    e.k = "VNONRELOC"
  elseif k == "VUPVAL" then
    e.info = self:codeABC(fs, "OP_GETUPVAL", 0, e.info, 0)
    e.k = "VRELOCABLE"
  elseif k == "VGLOBAL" then
    e.info = self:codeABx(fs, "OP_GETGLOBAL", 0, e.info)
    e.k = "VRELOCABLE"
  elseif k == "VINDEXED" then
    self:freereg(fs, e.aux)
    self:freereg(fs, e.info)
    e.info = self:codeABC(fs, "OP_GETTABLE", 0, e.info, e.aux)
    e.k = "VRELOCABLE"
  elseif k == "VCALL" or k == "VVARARG" then
    self:setoneret(fs, e)
  else
    -- there is one value available (somewhere)
  end
--printf("\n/dischargevars\n")
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:code_label(fs, A, b, jump)
  self:getlabel(fs)  -- those instructions may be jump targets
  return self:codeABC(fs, "OP_LOADBOOL", A, b, jump)
end

------------------------------------------------------------------------
-- FF updated 5.1
------------------------------------------------------------------------
function luaK:discharge2reg(fs, e, reg)
   self:dischargevars(fs, e)
   local k = e.k
   if k == "VNIL" then
      self:_nil(fs, reg, 1)
   elseif k == "VFALSE" or k == "VTRUE" then
      self:codeABC(fs, "OP_LOADBOOL", reg, (e.k == "VTRUE") and 1 or 0, 0)
   elseif k == "VKNUM" then
      self:codeABx (fs, "OP_LOADK", reg, self:numberK(fs, e.nval))
   elseif k == "VK" then
      self:codeABx(fs, "OP_LOADK", reg, e.info)
   elseif k == "VRELOCABLE" then
      local pc = self:getcode(fs, e)
      luaP:SETARG_A(pc, reg)
   elseif k == "VNONRELOC" then
      if reg ~= e.info then
         self:codeABC(fs, "OP_MOVE", reg, e.info, 0)
      end
   else
      assert(e.k == "VVOID" or e.k == "VJMP")
      return  -- nothing to do...
   end
   e.info = reg
   e.k = "VNONRELOC"
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:discharge2anyreg(fs, e)
  if e.k ~= "VNONRELOC" then
    self:reserveregs(fs, 1)
    self:discharge2reg(fs, e, fs.freereg - 1)
  end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:exp2reg(fs, e, reg)
  self:discharge2reg(fs, e, reg)
  if e.k == "VJMP" then
    e.t = self:concat(fs, e.t, e.info)  -- put this jump in 't' list
  end
  if self:hasjumps(e) then
    local final  -- position after whole expression
    local p_f = self.NO_JUMP  -- position of an eventual LOAD false
    local p_t = self.NO_JUMP  -- position of an eventual LOAD true
    if self:need_value(fs, e.t, 1) or self:need_value(fs, e.f, 0) then
      local fj = self.NO_JUMP  -- first jump (over LOAD ops.)
      if e.k ~= "VJMP" then fj = self:jump(fs) end
      p_f = self:code_label(fs, reg, 0, 1)
      p_t = self:code_label(fs, reg, 1, 0)
      self:patchtohere(fs, fj)
    end
    final = self:getlabel(fs)
    self:patchlistaux(fs, e.f, final, reg, p_f)
    self:patchlistaux(fs, e.t, final, reg, p_t)
  end
  e.f, e.t = self.NO_JUMP, self.NO_JUMP
  e.info = reg
  e.k = "VNONRELOC"
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:exp2nextreg(fs, e)
  self:dischargevars(fs, e)
  --[FF] Allready in place (added for expr.Stat)
  if e.k == "VNONRELOC" and e.info == fs.freereg then 
     --printf("Expression already in next reg %i: %s", fs.freereg, tostringv(e))
     return end
  self:freeexp(fs, e)
  self:reserveregs(fs, 1)
  self:exp2reg(fs, e, fs.freereg - 1)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:exp2anyreg(fs, e)
   --printf("exp2anyregs(e=%s)", tostringv(e))
   self:dischargevars(fs, e)
   if e.k == "VNONRELOC" then
      if not self:hasjumps(e) then  -- exp is already in a register
         return e.info
      end
      if e.info >= fs.nactvar then  -- reg. is not a local?
         self:exp2reg(fs, e, e.info)  -- put value on it
         return e.info
      end
   end
   self:exp2nextreg(fs, e)  -- default
   return e.info
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:exp2val(fs, e)
  if self:hasjumps(e) then
    self:exp2anyreg(fs, e)
  else
    self:dischargevars(fs, e)
  end
end

------------------------------------------------------------------------
-- FF updated 5.1
------------------------------------------------------------------------
function luaK:exp2RK(fs, e)
   self:exp2val(fs, e)
   local k = e.k
   if k=="VNIL" or k=="VTRUE" or k=="VFALSE" or k=="VKNUM" then
      if fs.nk <= luaP.MAXINDEXRK then
         if     k=="VNIL"  then e.info = self:nilK(fs)
         elseif k=="VKNUM" then e.info = self:numberK (fs, e.nval)
         else                   e.info = self:boolK(fs, e.k=="VTRUE") end
         e.k = "VK"
         return luaP:RKASK(e.info)
      end
   elseif k == "VK" then
      if e.info <= luaP.MAXINDEXRK then  -- constant fit in argC?
         return luaP:RKASK (e.info)
      end
   end
   -- not a constant in the right range: put it in a register
   return self:exp2anyreg(fs, e)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:storevar(fs, var, exp)
   --print("STOREVAR")
   --printf("var=%s", tostringv(var))
   --printf("exp=%s", tostringv(exp))

   local k = var.k
   if k == "VLOCAL" then
      self:freeexp(fs, exp)
      self:exp2reg(fs, exp, var.info)
      return
   elseif k == "VUPVAL" then
      local e = self:exp2anyreg(fs, exp)
      self:codeABC(fs, "OP_SETUPVAL", e, var.info, 0)
   elseif k == "VGLOBAL" then
      --printf("store global, exp=%s", tostringv(exp))
      local e = self:exp2anyreg(fs, exp)
      self:codeABx(fs, "OP_SETGLOBAL", e, var.info)
   elseif k == "VINDEXED" then
      local e = self:exp2RK(fs, exp)
      self:codeABC(fs, "OP_SETTABLE", var.info, var.aux, e)
   else
      assert(0)  -- invalid var kind to store
   end
   self:freeexp(fs, exp)
   --print("/STOREVAR")
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:_self(fs, e, key)
  self:exp2anyreg(fs, e)
  self:freeexp(fs, e)
  local func = fs.freereg
  self:reserveregs(fs, 2)
  self:codeABC(fs, "OP_SELF", func, e.info, self:exp2RK(fs, key))
  self:freeexp(fs, key)
  e.info = func
  e.k = "VNONRELOC"
end

------------------------------------------------------------------------
-- FF updated 5.1
------------------------------------------------------------------------
function luaK:invertjump(fs, e)
   --printf("invertjump on jump instruction #%i", e.info)
   --printv(self:getcode(fs, e))
   local pc = self:getjumpcontrol(fs, e.info)
   assert(luaP:testOpMode(luaP:GET_OPCODE(pc), "OpModeT") and
             luaP:GET_OPCODE(pc) ~= "OP_TESTSET" and
             luaP:GET_OPCODE(pc) ~= "OP_TEST")
   --printf("Before invert:")
   --printv(pc)
   luaP:SETARG_A(pc, (luaP:GETARG_A(pc) == 0) and 1 or 0)
   --printf("After invert:")
   --printv(pc)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:jumponcond(fs, e, cond)
  if e.k == "VRELOCABLE" then
    local ie = self:getcode(fs, e)
    if luaP:GET_OPCODE(ie) == "OP_NOT" then
      fs.pc = fs.pc - 1  -- remove previous OP_NOT
      return self:condjump(fs, "OP_TEST", luaP:GETARG_B(ie), 0,
                           cond and 0 or 1)
    end
    -- else go through
  end
  self:discharge2anyreg(fs, e)
  self:freeexp(fs, e)
  return self:condjump(fs, "OP_TESTSET", luaP.NO_REG, e.info, cond and 1 or 0)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:goiftrue(fs, e)
  local pc  -- pc of last jump
  self:dischargevars(fs, e)
  local k = e.k
  if k == "VK" or k == "VTRUE" or k == "VKNUM" then
    pc = self.NO_JUMP  -- always true; do nothing
  elseif k == "VFALSE" then
    pc = self:jump(fs)  -- always jump
  elseif k == "VJMP" then
    self:invertjump(fs, e)
    pc = e.info
  else
    pc = self:jumponcond(fs, e, false)
 end
  e.f = self:concat(fs, e.f, pc)  -- insert last jump in 'f' list
  self:patchtohere(fs, e.t)
  e.t = self.NO_JUMP
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:goiffalse(fs, e)
  local pc  -- pc of last jump
  self:dischargevars(fs, e)
  local k = e.k
  if k == "VNIL" or k == "VFALSE"then
    pc = self.NO_JUMP  -- always false; do nothing
  elseif k == "VTRUE" then
    pc = self:jump(fs)  -- always jump
  elseif k == "VJMP" then
    pc = e.info
  else
    pc = self:jumponcond(fs, e, true)
  end
  e.t = self:concat(fs, e.t, pc)  -- insert last jump in 't' list
  self:patchtohere(fs, e.f)
  e.f = self.NO_JUMP
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:codenot(fs, e)
  self:dischargevars(fs, e)
  local k = e.k
  if k == "VNIL" or k == "VFALSE" then
    e.k = "VTRUE"
  elseif k == "VK" or k == "VKNUM" or k == "VTRUE" then
    e.k = "VFALSE"
  elseif k == "VJMP" then
    self:invertjump(fs, e)
  elseif k == "VRELOCABLE" or k == "VNONRELOC" then
    self:discharge2anyreg(fs, e)
    self:freeexp(fs, e)
    e.info = self:codeABC(fs, "OP_NOT", 0, e.info, 0)
    e.k = "VRELOCABLE"
  else
    assert(0)  -- cannot happen
  end
  -- interchange true and false lists
  e.f, e.t = e.t, e.f
  self:removevalues(fs, e.f)
  self:removevalues(fs, e.t)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:indexed(fs, t, k)
  t.aux = self:exp2RK(fs, k)
  t.k = "VINDEXED"
end

--FF 5.1
function luaK:constfolding (op, e1, e2)
   if not self:isnumeral(e1) or not self:isnumeral(e2) then return false end
   local v1, v2, e, r = e1.nval, e2 and e2.nval, nil
   if     op == "OP_ADD" then r = v1+v2
   elseif op == "OP_SUB" then r = v1-v2
   elseif op == "OP_MUL" then r = v1*v2
   elseif op == "OP_DIV" then if v2==0 then return false end r = v1/v2
   elseif op == "OP_MOD" then if v2==0 then return false end r = v1%v2
   elseif op == "OP_POW" then r = v1^v2
   elseif op == "OP_UNM" then r = -v1
   elseif op == "OP_LEN" then return false
   else   assert (false, "Unknown numeric value") end
   e1.nval = r
   return true
end

--FF 5.1
function luaK:codearith (fs, op, e1, e2)
   if self:constfolding (op, e1, e2) then return else
      local o1 = self:exp2RK (fs, e1)
      local o2 = 0
      if op ~= "OP_UNM" and op ~= "OP_LEN" then 
         o2 = self:exp2RK (fs, e2) end
      self:freeexp(fs, e2)
      self:freeexp(fs, e1)
      e1.info = self:codeABC (fs, op, 0, o1, o2)
      e1.k = "VRELOCABLE"
   end
end

--FF 5.1
function luaK:codecomp (fs, op, cond, e1, e2)
   assert (type (cond) == "boolean")
   local o1 = self:exp2RK (fs, e1)
   local o2 = self:exp2RK (fs, e2)
   self:freeexp (fs, e2)
   self:freeexp (fs, e1)
   if not cond and op ~= "OP_EQ" then 
      local temp = o1; o1=o2; o2=temp cond = true end
   e1.info = self:condjump (fs, op, cond and 1 or 0, o1, o2)
   e1.k = "VJMP"
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:prefix (fs, op, e)
   local e2 = { t = self.NO_JUMP; f = self.NO_JUMP;
                k = "VKNUM"; nval = 0 }
   if op == "unm" then
      if e.k == "VK" then
         self:exp2anyreg (fs, e) end
      self:codearith (fs, "OP_UNM", e, e2)
   elseif op == "not" then
      self:codenot (fs, e)
   elseif op == "len" then
      self:exp2anyreg (fs, e)
      self:codearith (fs, "OP_LEN", e, e2)
   else
      assert (false, "Unknown unary operator")
   end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:infix (fs, op, v)
  if op == "and" then
    self:goiftrue(fs, v)
  elseif op == "or" then
    self:goiffalse(fs, v)
  elseif op == "concat" then
    self:exp2nextreg(fs, v)  -- operand must be on the 'stack'
 else
    if not self:isnumeral (v) then self:exp2RK(fs, v) end
  end
end

------------------------------------------------------------------------
--
-- grep "ORDER OPR" if you change these enums
------------------------------------------------------------------------
luaK.arith_opc = {  -- done as a table lookup instead of a calc
   add = "OP_ADD",
   sub = "OP_SUB",
   mul = "OP_MUL",
   mod = "OP_MOD",
   div = "OP_DIV",
   pow = "OP_POW",
   len = "OP_LEN",
   ["not"] = "OP_NOT"
}
luaK.test_opc = {  -- was ops[] in the codebinop function
  eq = {opc="OP_EQ", cond=true},
  lt = {opc="OP_LT", cond=true},
  le = {opc="OP_LE", cond=true},

  -- Pseudo-ops, with no metatable equivalent:
  ne = {opc="OP_EQ", cond=false},
  gt = {opc="OP_LT", cond=false},
  ge = {opc="OP_LE", cond=false}
}

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:posfix(fs, op, e1, e2)
   if op == "and" then
      assert(e1.t == self.NO_JUMP)  -- list must be closed
      self:dischargevars(fs, e2)
      e2.f = self:concat(fs, e2.f, e1.f)
      for k,v in pairs(e2) do e1[k]=v end -- *e1 = *e2
   elseif op == "or" then
      assert(e1.f == self.NO_JUMP)  -- list must be closed
      self:dischargevars(fs, e2)
      e2.t = self:concat(fs, e2.t, e1.t)
      for k,v in pairs(e2) do e1[k]=v end -- *e1 = *e2
   elseif op == "concat" then
      self:exp2val(fs, e2)
      if e2.k == "VRELOCABLE"
         and luaP:GET_OPCODE(self:getcode(fs, e2)) == "OP_CONCAT" then
         assert(e1.info == luaP:GETARG_B(self:getcode(fs, e2)) - 1)
         self:freeexp(fs, e1)
         luaP:SETARG_B(self:getcode(fs, e2), e1.info)
         e1.k = "VRELOCABLE"; e1.info = e2.info
      else
         self:exp2nextreg(fs, e2)
         self:codearith (fs, "OP_CONCAT", e1, e2)
      end
   else
      local opc = self.arith_opc[op]
      if opc then self:codearith (fs, opc, e1, e2) else
         opc = self.test_opc[op] or error ("Unknown operator "..op)
         self:codecomp (fs, opc.opc, opc.cond, e1, e2)
      end
   end
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:fixline(fs, line)
   --assert (line)
   if not line then
     --print(debug.traceback "fixline (line == nil)")
   end
   fs.f.lineinfo[fs.pc - 1] = line or 0
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:code(fs, i, line)
  if not line then 
    line = 0
    --print(debug.traceback "line == nil")
  end
  local f = fs.f

  do -- print it
    local params = { }
    for _,x in ipairs{"A","B","Bx", "sBx", "C"} do
      if i[x] then table.insert (params, string.format ("%s=%i", x, i[x])) end
    end
    debugf ("[code:\t%s\t%s]", luaP.opnames[i.OP], table.concat (params, ", "))
  end

  self:dischargejpc(fs)  -- 'pc' will change

  f.code[fs.pc] = i
  f.lineinfo[fs.pc] = line

  if line == 0 then
    f.lineinfo[fs.pc] = fs.lastline
    if fs.lastline == 0 then
      --print(debug.traceback())
    end    
  end

  if f.lineinfo[fs.pc] == 0 then
    f.lineinfo[fs.pc] = 42
  end

  local pc = fs.pc
  fs.pc = fs.pc + 1
  return pc
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
function luaK:codeABC(fs, o, a, b, c)
  assert(luaP:getOpMode(o) == "iABC", o.." is not an ABC operation")
  --assert getbmode(o) ~= opargn or b == 0
  --assert getcmode(o) ~= opargn or c == 0
  --FF
  --return self:code(fs, luaP:CREATE_ABC(o, a, b, c), fs.ls.lastline)
  return self:code(fs, luaP:CREATE_ABC(o, a, b, c), fs.lastline)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:codeABx(fs, o, a, bc)
  assert(luaP:getOpMode(o) == "iABx" or luaP:getOpMode(o) == "iAsBx")
  --assert getcmode(o) == opargn
  --FF
  --return self:code(fs, luaP:CREATE_ABx(o, a, bc), fs.ls.lastline)
  return self:code(fs, luaP:CREATE_ABx(o, a, bc), fs.lastline)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
function luaK:setlist (fs, base, nelems, tostore)
   local c = math.floor ((nelems-1) / luaP.LFIELDS_PER_FLUSH + 1)
   local b = tostore == self.LUA_MULTRET and 0 or tostore
   assert (tostore ~= 0)
   if c <= luaP.MAXARG_C then self:codeABC (fs, "OP_SETLIST", base, b, c)
   else
      self:codeABC (fs, "OP_SETLIST", base, b, 0)
      self:code (fs, c, fs.lastline)--FIXME
   end
   fs.freereg = base + 1
end
