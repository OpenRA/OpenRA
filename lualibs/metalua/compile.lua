----------------------------------------------------------------------
--
-- WARNING! You're entering a hackish area, proceed at your own risks!
--
-- This code partly results from the borrowing, then ruthless abuse, of
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

----------------------------------------------------------------------
-- Metalua.
--
-- Summary: Compile ASTs to Lua 5.1 VM function prototype. 
-- Largely based on:
--
-- * Yueliang (http://luaforge.net/projects/yueliang),
--   yueliang-0.1.2/orig-5.0.2/lparser.lua
--
-- * Lua 5.1 sources (http://www.lua.org), src/lparser.c
--
----------------------------------------------------------------------
--
-- Copyright (c) 2006-2008, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------

module ("bytecode", package.seeall)

local debugf = function() end
--local debugf=printf

local stat = { }
local expr = { }

MAX_INT            = 2147483645 -- INT_MAX-2 for 32-bit systems (llimits.h)
MAXVARS            = 200        -- (llimits.h)
MAXUPVALUES        = 32         -- (llimits.h)
MAXPARAMS          = 100        -- (llimits.h)
LUA_MAXPARSERLEVEL = 200        -- (llimits.h)

-- from lobject.h
VARARG_HASARG   = 1
VARARG_ISVARARG = 2
VARARG_NEEDSARG = 4

local function hasmultret (k) 
   return k=="VCALL" or k=="VVARARG"
end

-----------------------------------------------------------------------
-- Some ASTs take expression lists as children; it should be
-- acceptible to give an expression instead, and to automatically
-- interpret it as a single element list. That's what does this
-- function, adding a surrounding list iff needed.
--
-- WARNING: "Do" is the tag for chunks, which are essentially lists.
-- Therefore, we don't listify stuffs with a "Do" tag.
-----------------------------------------------------------------------
local function ensure_list (ast)
   return ast.tag and ast.tag ~= "Do" and {ast} or ast end

-----------------------------------------------------------------------
-- Get a localvar structure { varname, startpc, endpc } from a 
-- (zero-based) index of active variable. The catch is: don't get
-- confused between local index and active index.
--
-- locvars[x] contains { varname, startpc, endpc };
-- actvar[i] contains the index of the variable in locvars
-----------------------------------------------------------------------
local function getlocvar (fs, i)
  return fs.f.locvars[fs.actvar[i]] 
end

local function removevars (fs, tolevel)
  while fs.nactvar > tolevel do
     fs.nactvar = fs.nactvar - 1
     -- There may be dummy locvars due to expr.Stat
     -- FIXME: strange that they didn't disappear?!
     local locvar = getlocvar (fs, fs.nactvar)
     --printf("[REMOVEVARS] removing var #%i = %s", fs.nactvar,
     --    locvar and tostringv(locvar) or "<nil>")
     if locvar then locvar.endpc = fs.pc end
  end
end

-----------------------------------------------------------------------
-- [f] has a list of all its local variables, active and inactive.
-- Some local vars can correspond to the same register, if they exist
-- in different scopes. 
-- [fs.nlocvars] is the total number of local variables, not to be
-- confused with [fs.nactvar] the numebr of variables active at the
-- current PC.
-- At this stage, the existence of the variable is not yet aknowledged,
-- since [fs.nactvar] and [fs.freereg] aren't updated.
-----------------------------------------------------------------------
local function registerlocalvar (fs, varname)
   debugf("[locvar: %s = reg %i]", varname, fs.nlocvars)
   local f = fs.f
   f.locvars[fs.nlocvars] = { } -- LocVar
   f.locvars[fs.nlocvars].varname = varname
   local nlocvars = fs.nlocvars
   fs.nlocvars = fs.nlocvars + 1
   return nlocvars
end

-----------------------------------------------------------------------
-- update the active vars counter in [fs] by adding [nvars] of them,
-- and sets those variables' [startpc] to the current [fs.pc].
-- These variables were allready created, but not yet counted, by
-- new_localvar.
-----------------------------------------------------------------------
local function adjustlocalvars (fs, nvars)
   --debugf("adjustlocalvars, nvars=%i, previous fs.nactvar=%i,"..
   --       " #locvars=%i, #actvar=%i", 
   --       nvars, fs.nactvar, #fs.f.locvars, #fs.actvar)

   fs.nactvar = fs.nactvar + nvars
   for i = nvars, 1, -1 do
      --printf ("adjusting actvar #%i", fs.nactvar - i)
      getlocvar (fs, fs.nactvar - i).startpc = fs.pc
   end
end

------------------------------------------------------------------------
-- check whether, in an assignment to a local variable, the local variable
-- is needed in a previous assignment (to a table). If so, save original
-- local value in a safe place and use this safe copy in the previous
-- assignment.
------------------------------------------------------------------------
local function check_conflict (fs, lh, v)
  local extra = fs.freereg  -- eventual position to save local variable
  local conflict = false
  while lh do
    if lh.v.k == "VINDEXED" then
      if lh.v.info == v.info then  -- conflict?
        conflict = true
        lh.v.info = extra  -- previous assignment will use safe copy
      end
      if lh.v.aux == v.info then  -- conflict?
        conflict = true
        lh.v.aux = extra  -- previous assignment will use safe copy
      end
    end
    lh = lh.prev
  end
  if conflict then
    luaK:codeABC (fs, "OP_MOVE", fs.freereg, v.info, 0)  -- make copy
    luaK:reserveregs (fs, 1)
  end
end

-----------------------------------------------------------------------
-- Create an expdesc. To be updated when expdesc is lua-ified.
-----------------------------------------------------------------------
local function init_exp (e, k, i)
  e.f, e.t, e.k, e.info = luaK.NO_JUMP, luaK.NO_JUMP, k, i end

-----------------------------------------------------------------------
-- Reserve the string in tthe constant pool, and return an expdesc
-- referring to it.
-----------------------------------------------------------------------
local function codestring (fs, e, str)
  --printf( "codestring(%s)", disp.ast(str))
  init_exp (e, "VK", luaK:stringK (fs, str))
end

-----------------------------------------------------------------------
-- search for a local variable named [name] in the function being
-- built by [fs]. Doesn't try to visit upvalues.
-----------------------------------------------------------------------
local function searchvar (fs, name)
   for i = fs.nactvar - 1, 0, -1 do
      -- Because of expr.Stat, there can be some actvars which don't
      -- correspond to any locvar. Hence the checking for locvar's 
      -- nonnilness before getting the varname.
      local locvar = getlocvar(fs, i)
      if locvar and name == locvar.varname then 
         --printf("Found local var: %s; i = %i", tostringv(locvar), i)
         return i 
      end
   end
   return -1  -- not found
end

-----------------------------------------------------------------------
-- create and return a new proto [f]
-----------------------------------------------------------------------
local function newproto () 
  local f = {}
  f.k = {}
  f.sizek = 0
  f.p = {}
  f.sizep = 0
  f.code = {}
  f.sizecode = 0
  f.sizelineinfo = 0
  f.sizeupvalues = 0
  f.nups = 0
  f.upvalues = {}
  f.numparams = 0
  f.is_vararg = 0
  f.maxstacksize = 0
  f.lineinfo = {}
  f.sizelocvars = 0
  f.locvars = {}
  f.lineDefined = 0
  f.source = nil
  return f
end

------------------------------------------------------------------------
-- create and return a function state [new_fs] as a sub-funcstate of [fs].
------------------------------------------------------------------------
local function open_func (old_fs)
  local new_fs = { }
  new_fs.upvalues = { }
  new_fs.actvar = { }
  local f = newproto ()
  new_fs.f = f
  new_fs.prev = old_fs  -- linked list of funcstates
  new_fs.pc = 0
  new_fs.lasttarget = -1
  new_fs.jpc = luaK.NO_JUMP
  new_fs.freereg = 0
  new_fs.nk = 0
  new_fs.h = {}  -- constant table; was luaH_new call
  new_fs.np = 0
  new_fs.nlocvars = 0
  new_fs.nactvar = 0
  new_fs.bl = nil
  new_fs.nestlevel =  old_fs and old_fs.nestlevel or 0
  f.maxstacksize = 2  -- registers 0/1 are always valid
  new_fs.lastline = 0
  new_fs.forward_gotos = { }
  new_fs.labels = { }
  return new_fs
end

------------------------------------------------------------------------
-- Finish to set up [f] according to final state of [fs]
------------------------------------------------------------------------
local function close_func (fs)
  local f = fs.f
  --printf("[CLOSE_FUNC] remove any remaining var")
  removevars (fs, 0)
  luaK:ret (fs, 0, 0)
  f.sizecode = fs.pc
  f.sizelineinfo = fs.pc
  f.sizek = fs.nk
  f.sizep = fs.np
  f.sizelocvars = fs.nlocvars
  f.sizeupvalues = f.nups
  assert (fs.bl == nil)
  if next(fs.forward_gotos) then
     local x = table.tostring(fs.forward_gotos)
     error ("Unresolved goto: "..x)
  end
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function pushclosure(fs, func, v)
   local f = fs.f
   f.p [fs.np] = func.f
   fs.np = fs.np + 1
   init_exp (v, "VRELOCABLE", luaK:codeABx (fs, "OP_CLOSURE", 0, fs.np - 1))
  for i = 0, func.f.nups - 1 do
    local o = (func.upvalues[i].k == "VLOCAL") and "OP_MOVE" or "OP_GETUPVAL"
    luaK:codeABC (fs, o, 0, func.upvalues[i].info, 0)
  end
end

------------------------------------------------------------------------
-- FIXME: is there a need for f=fs.f? if yes, why not always using it? 
------------------------------------------------------------------------
function indexupvalue(fs, name, v)
   local f = fs.f
   for i = 0, f.nups - 1 do
      if fs.upvalues[i].k == v.k and fs.upvalues[i].info == v.info then
         assert(fs.f.upvalues[i] == name)
         return i
      end
   end
  -- new one
  f.upvalues[f.nups] = name
  assert (v.k == "VLOCAL" or v.k == "VUPVAL")
  fs.upvalues[f.nups] = { k = v.k; info = v.info }
  local nups = f.nups
  f.nups = f.nups + 1
  return nups
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function markupval(fs, level)
  local bl = fs.bl
  while bl and bl.nactvar > level do bl = bl.previous end
  if bl then bl.upval = true end
end


--for debug only
--[[
local function bldepth(fs)
   local i, x= 1, fs.bl
   while x do i=i+1; x=x.previous end
   return i
end
--]]

------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function enterblock (fs, bl, isbreakable)
  bl.breaklist = luaK.NO_JUMP
  bl.isbreakable = isbreakable
  bl.nactvar = fs.nactvar
  bl.upval = false
  bl.previous = fs.bl
  fs.bl = bl
  assert (fs.freereg == fs.nactvar)
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function leaveblock (fs)
   local bl = fs.bl
   fs.bl = bl.previous
   --printf("[LEAVEBLOCK] Removing vars...")
   removevars (fs, bl.nactvar)
   --printf("[LEAVEBLOCK] ...Vars removed")
   if bl.upval then
      luaK:codeABC (fs, "OP_CLOSE", bl.nactvar, 0, 0)
   end
   -- a block either controls scope or breaks (never both)
   assert (not bl.isbreakable or not bl.upval)
   assert (bl.nactvar == fs.nactvar)
   fs.freereg = fs.nactvar  -- free registers
   luaK:patchtohere (fs, bl.breaklist)
end


------------------------------------------------------------------------
-- read a list of expressions from a list of ast [astlist]
-- starts at the [offset]th element of the list (defaults to 1)
------------------------------------------------------------------------
local function explist(fs, astlist, v, offset)
  offset = offset or 1
  if #astlist < offset then error "I don't handle empty expr lists yet" end
  --printf("[EXPLIST] about to precompile 1st element %s", disp.ast(astlist[offset]))
  expr.expr (fs, astlist[offset], v)
  --printf("[EXPLIST] precompiled first element v=%s", tostringv(v))
  for i = offset+1, #astlist do
    luaK:exp2nextreg (fs, v)
    --printf("[EXPLIST] flushed v=%s", tostringv(v))
    expr.expr (fs, astlist[i], v)
    --printf("[EXPLIST] precompiled element v=%s", tostringv(v))
  end
  return #astlist - offset + 1
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function funcargs (fs, ast, v, idx_from)
  local args = { }  -- expdesc
  local nparams
  if #ast < idx_from then args.k = "VVOID" else
     explist(fs, ast, args, idx_from)
     luaK:setmultret(fs, args)
  end
  assert(v.k == "VNONRELOC")
  local base = v.info  -- base register for call
  if hasmultret(args.k) then nparams = luaK.LUA_MULTRET else -- open call
    if args.k ~= "VVOID" then 
       luaK:exp2nextreg(fs, args) end -- close last argument
    nparams = fs.freereg - (base + 1)
  end
  init_exp(v, "VCALL", luaK:codeABC(fs, "OP_CALL", base, nparams + 1, 2))
  if ast.lineinfo then
     luaK:fixline(fs, ast.lineinfo.first[1])
  else 
    luaK:fixline(fs, ast.line)
  end
  fs.freereg = base + 1  -- call remove function and arguments and leaves
                         -- (unless changed) one result
end

------------------------------------------------------------------------
-- calculates log value for encoding the hash portion's size
------------------------------------------------------------------------
local function log2(x)
  -- math result is always one more than lua0_log2()
  local mn, ex = math.frexp(x)
  return ex - 1
end

------------------------------------------------------------------------
-- converts an integer to a "floating point byte", represented as
-- (mmmmmxxx), where the real value is (xxx) * 2^(mmmmm)
------------------------------------------------------------------------

-- local function int2fb(x)
--   local m = 0  -- mantissa
--   while x >= 8 do x = math.floor((x + 1) / 2); m = m + 1 end
--   return m * 8 + x
-- end

local function int2fb(x)
   local e = 0
   while x >= 16 do
      x = math.floor ( (x+1) / 2)
      e = e+1
   end
   if x<8 then return x
   else return (e+1) * 8 + x - 8 end
end


------------------------------------------------------------------------
-- FIXME: to be unified with singlevar
------------------------------------------------------------------------
local function singlevaraux(fs, n, var, base)
--[[
print("\n\nsinglevaraux: fs, n, var, base")
printv(fs)
printv(n)
printv(var)
printv(base)
print("\n")
--]]
   if fs == nil then  -- no more levels?
      init_exp(var, "VGLOBAL", luaP.NO_REG)  -- default is global variable
      return "VGLOBAL"
   else
      local v = searchvar(fs, n)  -- look up at current level
      if v >= 0 then
         init_exp(var, "VLOCAL", v)
         if not base then
            markupval(fs, v)  -- local will be used as an upval
         end
      else  -- not found at current level; try upper one
         if singlevaraux(fs.prev, n, var, false) == "VGLOBAL" then
            return "VGLOBAL" end
         var.info = indexupvalue (fs, n, var)
         var.k = "VUPVAL"
         return "VUPVAL"
      end
   end
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function singlevar(fs, varname, var)   
  if singlevaraux(fs, varname, var, true) == "VGLOBAL" then
     var.info = luaK:stringK (fs, varname) end
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function new_localvar (fs, name, n)
  assert (type (name) == "string")
  if fs.nactvar + n > MAXVARS then error ("too many local vars") end
  fs.actvar[fs.nactvar + n] = registerlocalvar (fs, name)
  --printf("[NEW_LOCVAR] %i = %s", fs.nactvar+n, name)
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function parlist (fs, ast_params)
   local dots = (#ast_params > 0 and ast_params[#ast_params].tag == "Dots")
   local nparams = dots and #ast_params - 1 or #ast_params
   for i = 1, nparams do
      assert (ast_params[i].tag == "Id", "Function parameters must be Ids")
      new_localvar (fs, ast_params[i][1], i-1)
   end
   -- from [code_param]:
   --checklimit (fs, fs.nactvar, self.MAXPARAMS, "parameters")
   fs.f.numparams = fs.nactvar
   fs.f.is_vararg = dots and VARARG_ISVARARG or 0 
   adjustlocalvars (fs, nparams)
   fs.f.numparams = fs.nactvar --FIXME vararg must be taken in account
   luaK:reserveregs (fs, fs.nactvar)  -- reserve register for parameters
end

------------------------------------------------------------------------
-- if there's more variables than expressions in an assignment,
-- some assignations to nil are made for extraneous vars.
-- Also handles multiret functions
------------------------------------------------------------------------
local function adjust_assign (fs, nvars, nexps, e)
  local extra = nvars - nexps
  if hasmultret (e.k) then
    extra = extra+1  -- includes call itself
    if extra <= 0 then extra = 0 end
    luaK:setreturns(fs, e, extra)  -- call provides the difference
    if extra > 1 then luaK:reserveregs(fs, extra-1) end
  else
    if e.k ~= "VVOID" then 
       luaK:exp2nextreg(fs, e) end  -- close last expression
    if extra > 0 then
      local reg = fs.freereg
      luaK:reserveregs(fs, extra)
      luaK:_nil(fs, reg, extra)
    end
  end
end


------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function enterlevel (fs)
   fs.nestlevel = fs.nestlevel + 1
   assert (fs.nestlevel <= LUA_MAXPARSERLEVEL, "too many syntax levels")
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function leavelevel (fs)
  fs.nestlevel = fs.nestlevel - 1
end

------------------------------------------------------------------------
-- Parse conditions in if/then/else, while, repeat
------------------------------------------------------------------------
local function cond (fs, ast)
   local v = { }
   expr.expr(fs, ast, v)  -- read condition
   if v.k == "VNIL" then v.k = "VFALSE" end  -- 'falses' are all equal here
   luaK:goiftrue (fs, v)
   return v.f
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function chunk (fs, ast)
   enterlevel (fs)
   assert (not ast.tag)
   for i=1, #ast do 
      stat.stat (fs, ast[i]); 
      fs.freereg = fs.nactvar
   end
   leavelevel (fs)
end

------------------------------------------------------------------------
-- 
------------------------------------------------------------------------
local function block (fs, ast)
  local bl = {}
  enterblock (fs, bl, false)
  for i=1, #ast do
     stat.stat (fs, ast[i])
     fs.freereg = fs.nactvar
  end
  assert (bl.breaklist == luaK.NO_JUMP)
  leaveblock (fs)
end  

------------------------------------------------------------------------
-- Forin / Fornum body parser
-- [fs]
-- [body]
-- [base]
-- [nvars]
-- [isnum]
------------------------------------------------------------------------
local function forbody (fs, ast_body, base, nvars, isnum)
   local bl = {}  -- BlockCnt
   adjustlocalvars (fs, 3)  -- control variables
   local prep = 
      isnum and luaK:codeAsBx (fs, "OP_FORPREP", base, luaK.NO_JUMP)
      or luaK:jump (fs) 
   enterblock (fs, bl, false)  -- loop block
   adjustlocalvars (fs, nvars)  -- scope for declared variables
   luaK:reserveregs (fs, nvars)
   block (fs, ast_body)
   leaveblock (fs)
   --luaK:patchtohere (fs, prep-1)
   luaK:patchtohere (fs, prep)
   local endfor = 
      isnum and luaK:codeAsBx (fs, "OP_FORLOOP", base, luaK.NO_JUMP)
      or luaK:codeABC (fs, "OP_TFORLOOP", base, 0, nvars)
   luaK:fixline (fs, ast_body.line)  -- pretend that 'OP_FOR' starts the loop
   luaK:patchlist (fs, isnum and endfor or luaK:jump(fs), prep + 1)
end


------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function recfield (fs, ast, cc)
  local reg = fs.freereg
  local key, val = {}, {}  -- expdesc
  --FIXME: expr + exp2val = index -->
  --       check reduncancy between exp2val and exp2rk
  cc.nh = cc.nh + 1
  expr.expr(fs, ast[1], key); 
  luaK:exp2val (fs, key) 
  local keyreg = luaK:exp2RK (fs, key)
  expr.expr(fs, ast[2], val)
  local valreg = luaK:exp2RK (fs, val)
  luaK:codeABC(fs, "OP_SETTABLE", cc.t.info, keyreg, valreg)
  fs.freereg = reg  -- free registers
end


------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function listfield(fs, ast, cc)
  expr.expr(fs, ast, cc.v)
  assert (cc.na <= luaP.MAXARG_Bx) -- FIXME check <= or <
  cc.na = cc.na + 1
  cc.tostore = cc.tostore + 1
end

------------------------------------------------------------------------
--
------------------------------------------------------------------------
local function closelistfield(fs, cc)
   if cc.v.k == "VVOID" then return end  -- there is no list item
   luaK:exp2nextreg(fs, cc.v)
   cc.v.k = "VVOID"
   if cc.tostore == luaP.LFIELDS_PER_FLUSH then
      luaK:setlist (fs, cc.t.info, cc.na, cc.tostore)
      cc.tostore = 0
   end
end

------------------------------------------------------------------------
-- The last field might be a call to a multireturn function. In that
-- case, we must unfold all of its results into the list.
------------------------------------------------------------------------
local function lastlistfield(fs, cc)
  if cc.tostore == 0 then return end
  if hasmultret (cc.v.k) then
    luaK:setmultret(fs, cc.v)
    luaK:setlist (fs, cc.t.info, cc.na, luaK.LUA_MULTRET)
    cc.na = cc.na - 1
  else
    if cc.v.k ~= "VVOID" then luaK:exp2nextreg(fs, cc.v) end
    luaK:setlist (fs, cc.t.info, cc.na, cc.tostore)
  end
end
------------------------------------------------------------------------
------------------------------------------------------------------------
-- 
-- Statement parsers table
-- 
------------------------------------------------------------------------
------------------------------------------------------------------------

function stat.stat (fs, ast)
   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1] end
   -- debugf (" - Statement %s", disp.ast (ast) )

   if not ast.tag then chunk (fs, ast) else

      local parser = stat [ast.tag]
      if not parser then 
         error ("A statement cannot have tag `"..ast.tag) end
      parser (fs, ast)
   end
   --debugf (" - /Statement `%s", ast.tag or "<nil>")
   debugf (" - /Statement `%s", ast.tag)
end

------------------------------------------------------------------------

stat.Do = block

------------------------------------------------------------------------

function stat.Break (fs, ast)
   --   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1]
   local bl, upval = fs.bl, false
   while bl and not bl.isbreakable do
      if bl.upval then upval = true end
      bl = bl.previous
   end
   assert (bl, "no loop to break")
   if upval then luaK:codeABC(fs, "OP_CLOSE", bl.nactvar, 0, 0) end
   bl.breaklist = luaK:concat(fs, bl.breaklist, luaK:jump(fs))
end

------------------------------------------------------------------------

function stat.Return (fs, ast)
   local e = {}  -- expdesc
   local first -- registers with returned values
   local nret = #ast

   if nret == 0 then first = 0
   else
      --printf("[RETURN] compiling explist")
      explist (fs, ast, e)
      --printf("[RETURN] explist e=%s", tostringv(e))
      if hasmultret (e.k) then
         luaK:setmultret(fs, e)
         if e.k == "VCALL" and nret == 1 then
            luaP:SET_OPCODE(luaK:getcode(fs, e), "OP_TAILCALL")
            assert(luaP:GETARG_A(luaK:getcode(fs, e)) == fs.nactvar)
         end
         first = fs.nactvar
         nret = luaK.LUA_MULTRET  -- return all values
      elseif nret == 1 then 
         --printf("[RETURN] 1 val: e=%s", tostringv(e))
         first = luaK:exp2anyreg(fs, e)
         --printf("[RETURN] 1 val in reg %i", first)
      else
         --printf("* Return multiple vals in nextreg %i", fs.freereg)
         luaK:exp2nextreg(fs, e)  -- values must go to the 'stack'
         first = fs.nactvar  -- return all 'active' values
         assert(nret == fs.freereg - first)
      end
   end
   luaK:ret(fs, first, nret)
end
------------------------------------------------------------------------

function stat.Local (fs, ast)
  local names, values = ast[1], ast[2] or { }
  for i = 1, #names do new_localvar (fs, names[i][1], i-1) end
  local e = { }
  if #values == 0 then e.k = "VVOID" else explist (fs, values, e) end
  adjust_assign (fs, #names, #values, e)
  adjustlocalvars (fs, #names)
end

------------------------------------------------------------------------

function stat.Localrec (fs, ast)
   assert(#ast[1]==1 and #ast[2]==1, "Multiple letrecs not implemented yet")
   local ast_var, ast_val, e_var, e_val = ast[1][1], ast[2][1], { }, { }
   new_localvar (fs, ast_var[1], 0)
   init_exp (e_var, "VLOCAL", fs.freereg)
   luaK:reserveregs (fs, 1)
   adjustlocalvars (fs, 1)
   expr.expr (fs, ast_val, e_val)
   luaK:storevar (fs, e_var, e_val)
   getlocvar (fs, fs.nactvar-1).startpc = fs.pc
end

------------------------------------------------------------------------

function stat.If (fs, ast)
  local astlen = #ast
  -- Degenerate case #1: no statement
  if astlen==0 then return block(fs, { }) end
  -- Degenerate case #2: only an else statement
  if astlen==1 then return block(fs, ast[1]) end   

  local function test_then_block (fs, test, body)
    local condexit = cond (fs, test); 
    block (fs, body) 
    return condexit
  end

  local escapelist = luaK.NO_JUMP

  local flist = test_then_block (fs, ast[1], ast[2]) -- 'then' statement
  for i = 3, #ast - 1, 2 do -- 'elseif' statement
    escapelist = luaK:concat( fs, escapelist, luaK:jump(fs))
    luaK:patchtohere (fs, flist)
    flist = test_then_block (fs, ast[i], ast[i+1])
  end
  if #ast % 2 == 1 then -- 'else' statement
    escapelist = luaK:concat(fs, escapelist, luaK:jump(fs))
    luaK:patchtohere(fs, flist)
    block (fs, ast[#ast])
  else
    escapelist = luaK:concat(fs, escapelist, flist)
  end
  luaK:patchtohere(fs, escapelist)
end

------------------------------------------------------------------------

function stat.Forin (fs, ast)
   local vars, vals, body = ast[1], ast[2], ast[3]
   -- imitating forstat:
   local bl = { }
   enterblock (fs, bl, true)
   -- imitating forlist:
   local e, base = { }, fs.freereg
   new_localvar (fs, "(for generator)", 0)
   new_localvar (fs, "(for state)", 1)
   new_localvar (fs, "(for control)", 2)
   for i = 1, #vars do new_localvar (fs, vars[i][1], i+2) end
   explist (fs, vals, e)
   adjust_assign (fs, 3, #vals, e)
   luaK:checkstack (fs, 3)
   forbody (fs, body, base, #vars, false)
   -- back to forstat:
   leaveblock (fs)
end

------------------------------------------------------------------------

function stat.Fornum (fs, ast)

   local function exp1 (ast_e)
      local e = { }
      expr.expr (fs, ast_e, e)
      luaK:exp2nextreg (fs, e)
   end
   -- imitating forstat:
   local bl = { }
   enterblock (fs, bl, true)
   -- imitating fornum:
   local base = fs.freereg
   new_localvar (fs, "(for index)", 0)
   new_localvar (fs, "(for limit)", 1)
   new_localvar (fs, "(for step)", 2)
   new_localvar (fs, ast[1][1], 3) 
   exp1 (ast[2]) -- initial value
   exp1 (ast[3]) -- limit
   if #ast == 5 then exp1 (ast[4]) else -- default step = 1
      luaK:codeABx(fs, "OP_LOADK", fs.freereg, luaK:numberK(fs, 1))
      luaK:reserveregs(fs, 1)
   end
   forbody (fs, ast[#ast], base, 1, true)
   -- back to forstat:
   leaveblock (fs)
end

------------------------------------------------------------------------
function stat.Repeat (fs, ast)
  local repeat_init = luaK:getlabel (fs)
  local bl1, bl2 = { }, { }
  enterblock (fs, bl1, true)
  enterblock (fs, bl2, false)
  chunk (fs, ast[1])
  local condexit = cond (fs, ast[2])
  if not bl2.upval then
    leaveblock (fs)
    luaK:patchlist (fs, condexit, repeat_init)
  else
    stat.Break (fs)
    luaK:patchtohere (fs, condexit)
    leaveblock (fs)
    luaK:patchlist (fs, luaK:jump (fs), repeat_init)
  end
  leaveblock (fs)
end

------------------------------------------------------------------------

function stat.While (fs, ast)
   local whileinit = luaK:getlabel (fs)
   local condexit = cond (fs, ast[1])
   local bl = { }
   enterblock (fs, bl, true)
   block (fs, ast[2])
   luaK:patchlist (fs, luaK:jump (fs), whileinit)
   leaveblock (fs)
   luaK:patchtohere (fs, condexit);
end

------------------------------------------------------------------------

-- FIXME: it's cumbersome to write this in this semi-recursive way.
function stat.Set (fs, ast)
   local ast_lhs, ast_vals, e = ast[1], ast[2], { }

   --print "\n\nSet ast_lhs ast_vals:"
   --print(disp.ast(ast_lhs))
   --print(disp.ast(ast_vals))

   local function let_aux (lhs, nvars)
      local legal = { VLOCAL=1, VUPVAL=1, VGLOBAL=1, VINDEXED=1 }
      --printv(lhs)
      if not legal [lhs.v.k] then 
         error ("Bad lhs expr: "..table.tostring(ast_lhs)) 
      end
      if nvars < #ast_lhs then -- this is not the last lhs
         local nv = { v = { }, prev = lhs }
         expr.expr (fs, ast_lhs [nvars+1], nv.v)
         if nv.v.k == "VLOCAL" then check_conflict (fs, lhs, nv.v) end
         let_aux (nv, nvars+1)
      else -- this IS the last lhs
         explist (fs, ast_vals, e)
         if #ast_vals < nvars then            
            adjust_assign (fs, nvars, #ast_vals, e)
         elseif #ast_vals > nvars then 
            adjust_assign (fs, nvars, #ast_vals, e)
            fs.freereg = fs.freereg - #ast_vals + nvars
         else -- #ast_vals == nvars (and we're at last lhs)
            luaK:setoneret (fs, e)  -- close last expression
            luaK:storevar (fs, lhs.v, e)
            return  -- avoid default
         end
      end
      init_exp (e, "VNONRELOC", fs.freereg - 1)  -- default assignment
      luaK:storevar (fs, lhs.v, e)
   end

   local lhs = { v = { }, prev = nil }
   expr.expr (fs, ast_lhs[1], lhs.v)
   let_aux( lhs, 1)
end  

------------------------------------------------------------------------

function stat.Call (fs, ast)
   local v = {  }
   expr.Call (fs, ast, v)
   luaP:SETARG_C (luaK:getcode(fs, v), 1)
end

------------------------------------------------------------------------

function stat.Invoke (fs, ast)
   local v = {  }
   expr.Invoke (fs, ast, v)
   --FIXME: didn't check that, just copied from stat.Call
   luaP:SETARG_C (luaK:getcode(fs, v), 1)
end


local function patch_goto (fs, src, dst)

end


------------------------------------------------------------------------
-- Goto/Label data:
-- fs.labels        :: string => { nactvar :: int; pc :: int }
-- fs.forward_gotos :: string => list(int)
--
-- fs.labels goes from label ids to the number of active variables at
-- the label's PC, and that PC
--
-- fs.forward_gotos goes from label ids to the list of the PC where
-- some goto wants to jump to this label. Since gotos are actually made
-- up of two instructions OP_CLOSE and OP_JMP, it's the first instruction's
-- PC that's stored in fs.forward_gotos
--
-- Note that backward gotos aren't stored: since their destination is knowns
-- when they're compiled, their target is directly set.
------------------------------------------------------------------------

------------------------------------------------------------------------
-- Set a Label to jump to with Goto
------------------------------------------------------------------------
function stat.Label (fs, ast)
   local label_id = ast[1]
   if type(label_id)=='table' then label_id=label_id[1] end
   -- printf("Label %s at PC %i", label_id, fs.pc)
   -------------------------------------------------------------------
   -- Register the label, so that future gotos can use it.
   -------------------------------------------------------------------
   if   fs.labels [label_id] then error "Duplicate label in function"
   else fs.labels [label_id] = { pc = fs.pc; nactvar = fs.nactvar } end
   local gotos = fs.forward_gotos [label_id]
   if gotos then 
      ----------------------------------------------------------------
      -- Patch forward gotos which were targetting this label.
      ----------------------------------------------------------------
      for _, goto_pc in ipairs(gotos) do
         local close_instr  = fs.f.code[goto_pc]
         local jmp_instr    = fs.f.code[goto_pc+1]
         local goto_nactvar = luaP:GETARG_A (close_instr)
         if fs.nactvar < goto_nactvar then 
            luaP:SETARG_A (close_instr, fs.nactvar) end
         luaP:SETARG_sBx (jmp_instr, fs.pc - goto_pc - 2)
      end
      ----------------------------------------------------------------
      -- Gotos are patched, they can be forgotten about (when the
      -- function will be finished, it will be checked that all gotos
      -- have been patched, by checking that forward_goto is empty).
      ----------------------------------------------------------------
      fs.forward_gotos[label_id] = nil
   end 
end

------------------------------------------------------------------------
-- jumps to a label set with stat.Label. 
-- Argument must be a String or an Id
-- FIXME/optim: get rid of useless OP_CLOSE when nactvar doesn't change.
-- Thsi must be done both here for backward gotos, and in
-- stat.Label for forward gotos.
------------------------------------------------------------------------
function stat.Goto (fs, ast)
   local label_id = ast[1]
   if type(label_id)=='table' then label_id=label_id[1] end
   -- printf("Goto %s at PC %i", label_id, fs.pc)
   local label = fs.labels[label_id]
   if label then
      ----------------------------------------------------------------
      -- Backward goto: the label already exists, so I can get its
      -- nactvar and address directly. nactvar is used to close
      -- upvalues if we get out of scoping blocks by jumping.
      ----------------------------------------------------------------
      if fs.nactvar > label.nactvar then
         luaK:codeABC  (fs, "OP_CLOSE", label.nactvar, 0, 0) end
      local offset = label.pc - fs.pc - 1
      luaK:codeAsBx (fs, "OP_JMP", 0, offset)
   else
      ----------------------------------------------------------------
      -- Forward goto: will be patched when the matching label is
      -- found, forward_gotos[label_id] keeps the PC of the CLOSE
      -- instruction just before the JMP. [stat.Label] will use it to
      -- patch the OP_CLOSE and the OP_JMP.
      ----------------------------------------------------------------
      if not fs.forward_gotos[label_id] then 
         fs.forward_gotos[label_id] = { } end
      table.insert (fs.forward_gotos[label_id], fs.pc)
      luaK:codeABC  (fs, "OP_CLOSE", fs.nactvar, 0, 0)
      luaK:codeAsBx (fs, "OP_JMP", 0, luaK.NO_JUMP)
   end
end

------------------------------------------------------------------------
------------------------------------------------------------------------
-- 
-- Expression parsers table
-- 
------------------------------------------------------------------------
------------------------------------------------------------------------

function expr.expr (fs, ast, v)
   if type(ast) ~= "table" then 
      error ("Expr AST expected, got "..table.tostring(ast)) end

   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1] end

   --debugf (" - Expression %s", tostringv (ast))
   local parser = expr[ast.tag]
   if parser then parser (fs, ast, v)
   elseif not ast.tag then 
      error ("No tag in expression "..table.tostring(ast, 'nohash', 80))
   else 
      error ("No parser for node `"..ast.tag) end
   debugf (" - /`%s", ast.tag)
end

------------------------------------------------------------------------

function expr.Nil (fs, ast, v) init_exp (v, "VNIL", 0) end
function expr.True (fs, ast, v) init_exp (v, "VTRUE", 0) end
function expr.False (fs, ast, v) init_exp (v, "VFALSE", 0) end
function expr.String (fs, ast, v) codestring (fs, v, ast[1]) end
function expr.Number (fs, ast, v)
   init_exp (v, "VKNUM", 0)
   v.nval = ast[1] 
end

function expr.Paren (fs, ast, v) 
   expr.expr (fs, ast[1], v)
   luaK:setoneret (fs, v)
end

function expr.Dots (fs, ast, v)
   assert (fs.f.is_vararg ~= 0, "No vararg in this function")
   -- NEEDSARG flag is set if and only if the function is a vararg,
   -- but no vararg has been used yet in its code.
   if fs.f.is_vararg < VARARG_NEEDSARG then 
      fs.f.is_varag = fs.f.is_vararg - VARARG_NEEDSARG end
   init_exp (v, "VVARARG", luaK:codeABC (fs, "OP_VARARG", 0, 1, 0))
end

------------------------------------------------------------------------

function expr.Table (fs, ast, v)
  local pc = luaK:codeABC(fs, "OP_NEWTABLE", 0, 0, 0)
  local cc = { v = { } , na = 0, nh = 0, tostore = 0, t = v }  -- ConsControl
  init_exp (v, "VRELOCABLE", pc)
  init_exp (cc.v, "VVOID", 0)  -- no value (yet)
  luaK:exp2nextreg (fs, v)  -- fix it at stack top (for gc)
  for i = 1, #ast do
    assert(cc.v.k == "VVOID" or cc.tostore > 0)
    closelistfield(fs, cc);
    (ast[i].tag == "Pair" and recfield or listfield) (fs, ast[i], cc)
  end    
  lastlistfield(fs, cc)

  -- Configure [OP_NEWTABLE] dimensions
  luaP:SETARG_B(fs.f.code[pc], int2fb(cc.na)) -- set initial array size
  luaP:SETARG_C(fs.f.code[pc], int2fb(cc.nh))  -- set initial table size
  --printv(fs.f.code[pc])
end  


------------------------------------------------------------------------

function expr.Function (fs, ast, v)
   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1] end

  local new_fs = open_func(fs)
  if ast.lineinfo then 
    new_fs.f.lineDefined, new_fs.f.lastLineDefined = 
        ast.lineinfo.first[1], ast.lineinfo.last[1]
  end
  parlist (new_fs, ast[1])
  chunk (new_fs, ast[2])
  close_func (new_fs)
  pushclosure(fs, new_fs, v)
end  

------------------------------------------------------------------------

function expr.Op (fs, ast, v)
   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1] end
   local op = ast[1]

   if #ast == 2 then
      expr.expr (fs, ast[2], v)
      luaK:prefix (fs, op, v)
   elseif #ast == 3 then
      local v2 = { }
      expr.expr (fs, ast[2], v)
      luaK:infix (fs, op, v)
      expr.expr (fs, ast[3], v2)
      luaK:posfix (fs, op, v, v2)
   else
      error "Wrong arg number"
   end
end  

------------------------------------------------------------------------

function expr.Call (fs, ast, v)
   expr.expr (fs, ast[1], v)
   luaK:exp2nextreg (fs, v)
   funcargs(fs, ast, v, 2)
   --debugf("after expr.Call: %s, %s", v.k, luaP.opnames[luaK:getcode(fs, v).OP])
end  

------------------------------------------------------------------------
-- `Invoke{ table key args }
function expr.Invoke (fs, ast, v)
   expr.expr (fs, ast[1], v)
   luaK:dischargevars (fs, v)
   local key = { }
   codestring (fs, key, ast[2][1])
   luaK:_self (fs, v, key)
   funcargs (fs, ast, v, 3)
end  

------------------------------------------------------------------------

function expr.Index (fs, ast, v)
   if #ast ~= 2 then
      print"\n\nBAD INDEX AST:"
      table.print(ast)
      error "generalized indexes not implemented" end

   if ast.lineinfo then fs.lastline = ast.lineinfo.last[1] end

   --assert(fs.lastline ~= 0, ast.tag)

   expr.expr (fs, ast[1], v)
   luaK:exp2anyreg (fs, v)

   local k = { }
   expr.expr (fs, ast[2], k)
   luaK:exp2val (fs, k)
   luaK:indexed (fs, v, k)
end  

------------------------------------------------------------------------

function expr.Id (fs, ast, v)
   assert (ast.tag == "Id")
   singlevar (fs, ast[1], v)
end

------------------------------------------------------------------------

function expr.Stat (fs, ast, v)
   --printf(" * Stat: %i actvars, first freereg is %i", fs.nactvar, fs.freereg)
   --printf("   actvars: %s", table.tostring(fs.actvar))

   -- Protect temporary stack values by pretending they are local
   -- variables. Local vars are in registers 0 ... fs.nactvar-1, 
   -- and temporary unnamed variables in fs.nactvar ... fs.freereg-1
   local save_nactvar = fs.nactvar

   -- Eventually, the result should go on top of stack *after all
   -- `Stat{ } related computation and string usage is over. The index
   -- of this destination register is kept here:
   local dest_reg = fs.freereg

   -- There might be variables in actvar whose register is > nactvar,
   -- and therefore will not be protected by the "nactvar := freereg"
   -- trick. Indeed, `Local only increases nactvar after the variable
   -- content has been computed. Therefore, in 
   -- "local foo = -{`Stat{...}}", variable foo will be messed up by
   -- the compilation of `Stat.
   -- FIX: save the active variables at indices >= nactvar in
   -- save_actvar, and restore them after `Stat has been computer.
   --
   -- I use a while rather than for loops and length operators because
   -- fs.actvar is a 0-based array...
   local save_actvar = { } do
      local i = fs.nactvar
      while true do
         local v = fs.actvar[i]
         if not v then break end
         --printf("save hald-baked actvar %s at index %i", table.tostring(v), i)
         save_actvar[i] = v
         i=i+1
      end
   end

   fs.nactvar = fs.freereg -- Now temp unnamed registers are protected
   enterblock (fs, { }, false)
   chunk (fs, ast[1])
   expr.expr (fs, ast[2], v)
   luaK:exp2nextreg (fs, v)
   leaveblock (fs)
   luaK:exp2reg (fs, v, dest_reg)

   -- Reserve the newly allocated stack level
   -- Puzzled note: here was written "fs.freereg = fs.freereg+1".
   -- I'm pretty sure it should rather be dest_reg+1, but maybe
   -- both are equivalent?
   fs.freereg = dest_reg+1

   -- Restore nactvar, so that intermediate stacked value stop
   -- being protected.
   --printf("   nactvar back from %i to %i", fs.nactvar, save_nactvar)
   fs.nactvar = save_nactvar

   -- restore messed-up unregistered local vars
   for i, j in pairs(save_actvar) do
      --printf("   Restoring actvar %i", i)
      fs.actvar[i] = j
   end
   --printf(" * End of Stat")
end



------------------------------------------------------------------------
-- Main function: ast --> proto
------------------------------------------------------------------------
function metalua_compile (ast, source)
  local fs = open_func (nil)
  fs.f.is_vararg = VARARG_ISVARARG
  chunk (fs, ast)
  close_func (fs)
  assert (fs.prev == nil)
  assert (fs.f.nups == 0)
  assert (fs.nestlevel == 0)
  if source then fs.f.source = source end
  return fs.f
end
