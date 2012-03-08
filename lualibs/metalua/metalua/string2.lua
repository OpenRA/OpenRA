----------------------------------------------------------------------
----------------------------------------------------------------------
--
-- String module extension
--
----------------------------------------------------------------------
----------------------------------------------------------------------

-- Courtesy of lua-users.org
function string.split(str, pat)
   local t = {} 
   local fpat = "(.-)" .. pat
   local last_end = 1
   local s, e, cap = string.find(str, fpat, 1)
   while s do
      if s ~= 1 or cap ~= "" then
          table.insert(t,cap)
       end
      last_end = e+1
      s, e, cap = string.find(str, fpat, last_end)
   end
   if last_end <= string.len(str) then
      cap = string.sub(str, last_end)
      table.insert(t, cap)
   end
   return t
end

-- "match" is regularly used as a keyword for pattern matching, 
-- so here is an always available substitute.
string.strmatch = string["match"]

-- change a compiled string into a function
function string.undump(str)
   if str:strmatch '^\027LuaQ' or str:strmatch '^#![^\n]+\n\027LuaQ' then
      local f = (lua_loadstring or loadstring)(str)
      return f
   else
      error "Not a chunk dump"
   end
end

return string