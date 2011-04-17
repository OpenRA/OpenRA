-- author: Christoph Kubisch
---------------------------------------------------------

local function ffiToApi(ffidef)
  local str = ffidef
  str = ffidef:match("(%-%-%[%[.+%]%])")
  local header = ffidef:match("[^\r\n]+")
  ffidef = StripCommentsC(ffidef)
  
  local function gencontent(tx)
    local enums = {}
    local funcs = {}
    local values = {}
    local classes = {}
    
    -- extract function names
    local curfunc
    local function registerfunc()
      local fn = curfunc
      -- parse args
      local args = fn.ARGS:match("%(%s*(.-)%s*%);")
      fn.ARGS = "("..args..")"
      
      -- skip return void types
      fn.RET = fn.RET == "void" and "" or fn.RET
      fn.RET = "("..fn.RET..")"
      fn.DESCR = ""
      
      table.insert(funcs,curfunc)
      curfunc = nil
    end
    
    local outer = tx:gsub("(%b{})","{}")
    
    -- FIXME pattern doesnt recognize multiline defs
    for l in outer:gmatch("[^\r\n]+") do 
      -- extern void func(blubb);
      -- extern void ( * func )(blubb);
      -- void func(blubb);
      -- void ( * func )(blubb);
      local typedef = l:match("typedef")
      local ret,name,args = string.match(typedef and "" or l,
        "%s*([_%w%*%s]+)%s+%(?[%s%*]*([_%w]+)%s*%)?%s*(%([^%(]*;)")
        
      if (not (curfunc or typedef) and (ret and name and args)) then
        ret = ret:gsub("^%s*extern%s*","")
        curfunc = {RET=ret,NAME=name,ARGS=args}
        registerfunc()
      elseif (not typedef) then
        local typ,name,val = l:match("%s*([_%w%s%*]-)%s*([_%w%[%]]+)[\r\n%s]*=[\r\n%s]*([_%w]+)[\r\n%s]*;")
        if (not (typ and name and val)) then
              typ,name     = l:match("%s*([_%w%s%*]-)%s*([_%w%[%]]+)[\r\n%s]*;")
        end
        if (typ and name) then
          local name,rest = name:match("([_%w]+)(.*)")
          table.insert(values,{NAME=name, DESCR=typ..rest..(val and (" = "..val) or "")})
        end
      end
    end 
    
    -- search for enums
    for def in tx:gmatch("enum[_%w%s\r\n]*(%b{})[_%w%s\r\n]*;") do
      for enum in def:gmatch("([_%w]+).-[,}]") do
        table.insert(enums,{NAME=enum})
      end
    end
    
    -- search for classes
    for class,def,final in tx:gmatch("struct%s+([_%w]*)[%s\r\n]*(%b{})([_%w%s\r\n]*);") do
      class = final:match("[_%w]+") or class
      table.insert(classes,{NAME=class,DESCR = "",content = gencontent(def:sub(2,-2))})
    end
    
    
    return (#classes > 0 or #funcs > 0 or #enums > 0 or #values > 0) and 
      {classes=classes,funcs=funcs, enums=enums, values=values}
  end
  
  local content = gencontent(ffidef)
  
  
  str = str..[[
  
--auto-generated api from ffi headers
local api =
]]
  
  -- serialize api string
  local function serialize(str,id,tab,lvl)
    lvl = string.rep("  ",lvl or 1)
    for i,k in ipairs(tab) do
      str = str..string.gsub(id,"%$([%w]+)%$",k):gsub("##",lvl)
    end
    return str
  end
  
  local function genapi(str,content,lvl)
    lvl = lvl or 1
    str = str..string.gsub([[
##{
]],"##",string.rep("  ",lvl))

    local value = 
[[##["$NAME$"] = { type ='value', description = "$DESCR$", },
]]
    local enum = 
[[##["$NAME$"] = { type ='value', },
]]
    local funcdef =
[[##["$NAME$"] = { type ='function', 
##  description = "$DESCR$", 
##  returns = "$RET$",
##  args = "$ARGS$", },
]]
    str = serialize(str,value,content.values or {},lvl)
    str = serialize(str,enum,content.enums or {},lvl)
    str = serialize(str,funcdef,content.funcs or {},lvl)
  
    local classdef =
[[##["$NAME$"] = { type ='class', 
##  description = "$DESCR$", 
##  $CHILDS$
##},
]]
    for i,v in pairs(content.classes or {}) do
      v.CHILDS = v.content and genapi("childs = ",v.content,lvl+1) or ""
    end
  
    str = serialize(str,classdef,content.classes or {},lvl)

    str = str..string.gsub([[
##}]],"##",string.rep("  ",lvl))

    return str
  end

  str = genapi(str,content)

  str = str..[[

return {
]]

  local lib =
[[
  $NAME$ = {
    type = 'lib',
    description = "$DESCR$",
    childs = $API$,
  },
]]

  local description = header:match("|%s*(.*)")
  local prefixes = header:match("(.-)%s*|")
  local libs = {}
  
  for prefix in prefixes:gmatch("([_%w]+)") do 
    local p = {NAME=prefix, DESCR = description, API="api"}
    table.insert(libs,p)
  end

  str = serialize(str,lib,libs)
  str = str..[[
}
]]

  return str
end

local function exec(wxfname,projectdir)
  -- get cur editor text
  local editor = GetEditor()
  if (not editor) then end
  local tx = editor:GetText()
  tx = ffiToApi(tx)
  -- replace text
  editor:SetText(tx)
end

if (RELPATH) then
  ffitoapiExec = exec
end

return {
  exec = {
    name = "luajit ffi string to Estrela api",
    description = "converts current file to api",
    fn = exec,
  },
}