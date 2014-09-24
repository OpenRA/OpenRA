-- author: Christoph Kubisch
---------------------------------------------------------
local StripCommentsC = StripCommentsC
if not StripCommentsC then
  StripCommentsC = function(tx)
    local out = ""
    local lastc = ""
    local skip
    local skipline
    local skipmulti
    local tx = string.gsub(tx, "\r\n", "\n")
    for c in tx:gmatch(".") do
      local oc = c
      local tu = lastc..c
      skip = c == '/'

      if ( not (skipmulti or skipline)) then
        if (tu == "//") then
          skipline = true
        elseif (tu == "/*") then
          skipmulti = true
          c = ""
        elseif (lastc == '/') then
          oc = tu
        end
      elseif (skipmulti and tu == "*/") then
        skipmulti = false
        c = ""
      elseif (skipline and lastc == "\n") then
        out = out.."\n"
        skipline = false
      end

      lastc = c
      if (not (skip or skipline or skipmulti)) then
        out = out..oc
      end
    end

    return out..lastc
  end

end

local function ffiToApi(ffidef)
  local str = ffidef
  str = ffidef:match("(%-%-%[%[.+%]%])")
  local header = ffidef:match("[^\r\n]+")
  ffidef = StripCommentsC(ffidef)

  local description = header:match("|%s*(.*)")
  local descrprefixes = header:match("(.-)%s*|")
  if not descrprefixes then return end

  local prefixes = {}
  for prefix in descrprefixes:gmatch("([_%w]+)") do
    table.insert(prefixes,prefix)
  end
  local ns = prefixes[1]
  if not ns then return end

  local lktypes = {
    ["string"] = "string",
  }

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
      local args = fn.ARGS:match("%(%s*(.-)%s*%)%s*;") or ""
      fn.ARGS = "("..args..")"

      -- skip return void types
      local what = fn.RET == "void" and "" or fn.RET
      what = what:match("%s*(.-)%s*$")
      fn.RET = "("..what..")"
      fn.DESCR = ""
      if (what ~= "") then
        fn.TYPE = what
      end

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
      -- void * ( * func )(blubb);
      local typedef = l:match("typedef")
      local ret,name,args = string.match(typedef and "" or l,
      "%s*([_%w%*%s]+)%s+%(?[%s%*]*([_%w]+)%s*%)?%s*(%([^%(]*;)")

      if (not (curfunc or typedef) and (ret and name and args)) then
        ret = ret:gsub("^%s*extern%s*","")
        curfunc = {RET=ret,NAME=name,ARGS=args}
        registerfunc()
      elseif (not typedef) then
        local typ,names,val = l:match("%s*([_%w%s%*]+)%s+([_%w%[%]]+)[\r\n%s]*=[\r\n%s]*([_%w]+)[\r\n%s]*;")
        if (not (typ and names and val)) then
          typ,names = l:match("%s*([_%w%s%*]+)%s+([_%w%[%]%:%s,]+)[\r\n%s]*;")
        end
        if (typ and names) then
          for name,rest in names:gmatch("([_%w]+)([^,]*)") do
            rest = rest and rest:gsub("%s","") or ""
            local what = typ..(rest:gsub("%b[]","*"))
            table.insert(values,{NAME=name, DESCR=(typ..rest..(val and (" = "..val) or "")), TYPE = what,})
          end
        end
      elseif(typedef) then
        -- typedef struct lxgTextureUpdate_s * lxgTextureUpdatePTR;
        -- typedef float lxVector2[2];
        local what,name = l:match("typedef%s+([_%w%s%*]-)%s+([_%w%[%]]+)%s*;")
        if (what and name) then
          what = what:gsub("const%s","")
          what = what:gsub("static%s","")
          what = what:gsub("%s+"," ")
          what = what:gsub("%s+%*","*")
          local name,rest = name:match("([_%w]+)(.*)")
          rest = rest and rest:gsub("%s","") or ""
          if (what and name) then
            lktypes[name] = what..(rest:gsub("%b[]","*"))
          end
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
      final = final:match("[_%w]+")
      if (final) then
        lktypes["struct "..class] = ns.."."..final
        lktypes[final] = ns.."."..final
        lktypes[ns.."."..final] = ns.."."..final
      else
        lktypes["struct "..class] = ns.."."..class
        lktypes[ns.."."..class] = ns.."."..class
      end
      table.insert(classes,{NAME= final or class,DESCR = "",content = gencontent(def:sub(2,-2))})
    end

    return (#classes > 0 or #funcs > 0 or #enums > 0 or #values > 0) and
    {classes=classes,funcs=funcs, enums=enums, values=values}
  end

  local content = gencontent(ffidef)
  local function fixtypes(tab)
    for i,v in ipairs(tab) do
      local vt = v.TYPE
      if (vt) then
        local nt = vt

        repeat
          nt = nt:match("%s*(.-)%s*$")
          nt = nt:gsub("%s+"," ")
          nt = nt:gsub("%s%*","*")
          nt = nt == "const char*" and "string" or nt
          nt = nt:gsub("%*","")
          nt = nt:gsub("const%s","")
          nt = nt:gsub("static%s","")
          vt = nt
          local typ,qual = nt:match("([_%w%.%s]+)(%**)")
          nt = (lktypes[typ] or "")..(qual or "")
        until nt==vt
        v.TYPE = nt ~= "" and '"'..nt..'"' or "nil"
      else
        v.TYPE = "nil"
      end
    end
  end
  local function fixcontent(tab)
    fixtypes(tab.values)
    fixtypes(tab.funcs)
    for i,v in ipairs(tab.classes) do
      fixcontent(v.content)
    end
  end
  fixcontent(content)

  str = str..[[

  --auto-generated api from ffi headers
  local api =
  ]]

  -- serialize api string
  local function serialize(str,id,tab,lvl)
    lvl = string.rep(" ",lvl or 1)
    for i,k in ipairs(tab) do
      str = str..string.gsub(id,"%$([%w]+)%$",k):gsub("##",lvl)
    end
    return str
  end

  local function genapi(str,content,lvl)
    lvl = lvl or 1
    str = str..string.gsub([[
      ##{
        ]],"##",string.rep(" ",lvl))

      local value =
      [[##["$NAME$"] = { type ='value', description = "$DESCR$", valuetype = $TYPE$, },
      ]]
      local enum =
      [[##["$NAME$"] = { type ='value', },
      ]]
      local funcdef =
      [[##["$NAME$"] = { type ='function',
        ## description = "$DESCR$",
        ## returns = "$RET$",
        ## valuetype = $TYPE$,
        ## args = "$ARGS$", },
      ]]
      str = serialize(str,value,content.values or {},lvl)
      str = serialize(str,enum,content.enums or {},lvl)
      str = serialize(str,funcdef,content.funcs or {},lvl)

      local classdef =
      [[##["$NAME$"] = { type ='class',
        ## description = "$DESCR$",
        ## $CHILDS$
        ##},
      ]]
      for i,v in pairs(content.classes or {}) do
        v.CHILDS = v.content and genapi("childs = ",v.content,lvl+1) or ""
      end

      str = serialize(str,classdef,content.classes or {},lvl)

      str = str..string.gsub([[
        ##}]],"##",string.rep(" ",lvl))

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

    local libs = {}
    for i,prefix in ipairs(prefixes) do
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
  if (not editor) then return end
  local tx = editor:GetText()
  tx = ffiToApi(tx)
  -- replace text
  if tx then editor:SetText(tx) end
end

if (not ide) then
  ffitoapi = function(fname)
    local f = io.open(fname,"rb")
    local tx = f:read("*a")
    f:close()
    tx = ffiToApi(tx)
    local f = io.open(fname,"wb")
    f:write(tx)
    f:close()
  end
end

return {
  exec = {
    name = "luajit ffi string to editor api",
    description = "converts current file to api, for autocompletion inside this editor",
    fn = exec,
  },
}
