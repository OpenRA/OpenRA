-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local statusBar = ide.frame.statusBar

local q = EscapeMagic

-- api loading depends on Lua interpreter
-- and loaded specs

------------
-- API

local function newAPI(api)
  api = api or {}
  for i in pairs(api) do
    api[i] = nil
  end
  -- tool tip info and reserved names
  api.tip = {
    staticnames = {},
    keys = {},
    finfo = {},
    finfoclass = {},
    shortfinfo = {},
    shortfinfoclass = {},
  }
  -- autocomplete hierarchy
  api.ac = {
    childs = {},
  }

  return api
end

local apis = {
  none = newAPI(),
  lua = newAPI(),
}

function GetApi(apitype) return apis[apitype] or apis.none end

----------
-- API loading

local function gennames(tab, prefix)
  for i,v in pairs(tab) do
    v.classname = (prefix and (prefix..".") or "")..i
    if (v.childs) then
      gennames(v.childs,v.classname)
    end
  end
end

local function addAPI(ftype, fname) -- relative to API directory
  local env = apis[ftype] or newAPI()

  local res
  local api = ide.apis[ftype][fname]

  if type(api) == 'table' then
    res = api
  else
    local fn, err = loadfile(api)
    if err then
      DisplayOutputLn(TR("Error while loading API file: %s"):format(err))
      return
    end
    local suc
    suc, res = pcall(function() return fn(env.ac.childs) end)
    if (not suc) then
      DisplayOutputLn(TR("Error while processing API file: %s"):format(res))
      return
    end
    -- cache the result
    ide.apis[ftype][fname] = res
  end
  apis[ftype] = env

  gennames(res)
  for i,v in pairs(res) do env.ac.childs[i] = v end
end

local function loadallAPIs(only, subapis, known)
  for ftype, v in pairs(only and {[only] = ide.apis[only]} or ide.apis) do
    if (not known or known[ftype]) then
      for fname in pairs(v) do
        if (not subapis or subapis[fname]) then addAPI(ftype, fname) end
      end
    end
  end
end

local function scanAPIs()
  for _, file in ipairs(FileSysGetRecursive("api", true, "*.lua")) do
    if not IsDirectory(file) then
      local ftype, fname = file:match("api[/\\]([^/\\]+)[/\\](.*)%.")
      if not ftype or not fname then
        DisplayOutputLn(TR("The API file must be located in a subdirectory of the API directory."))
        return
      end
      ide.apis[ftype] = ide.apis[ftype] or {}
      ide.apis[ftype][fname] = file
    end
  end
end

---------
-- ToolTip and reserved words list
-- also fixes function descriptions

local function fillTips(api,apibasename)
  local apiac = api.ac
  local tclass = api.tip

  tclass.staticnames = {}
  tclass.keys = {}
  tclass.finfo = {}
  tclass.finfoclass = {}
  tclass.shortfinfo = {}
  tclass.shortfinfoclass = {}

  local staticnames = tclass.staticnames
  local keys = tclass.keys
  local finfo = tclass.finfo
  local finfoclass = tclass.finfoclass
  local shortfinfo = tclass.shortfinfo
  local shortfinfoclass = tclass.shortfinfoclass

  local function traverse (tab, libname, format)
    if not tab.childs then return end
    format = tab.format or format
    for key,info in pairs(tab.childs) do
      local fullkey = (libname ~= "" and libname.."." or "")..key
      traverse(info, fullkey, format)

      if info.type == "function" or info.type == "method" or info.type == "value" then
        local frontname = (info.returns or "(?)").." "..fullkey.." "..(info.args or "(?)")
        frontname = frontname:gsub("\n"," "):gsub("\t","")
        local description = info.description or ""

        -- build info
        local inf = ((info.type == "value" and "" or frontname.."\n")
          ..description)
        local sentence = description:match("^(.-)%. ?\n")
        local infshort = ((info.type == "value" and "" or frontname.."\n")
          ..(sentence and sentence.."..." or description))
        if type(format) == 'function' then -- apply custom formatting if requested
          inf = format(fullkey, info, inf)
          infshort = format(fullkey, info, infshort)
        end
        local infshortbatch = (info.returns and info.args) and frontname or infshort

        -- add to infoclass
        if not finfoclass[libname] then finfoclass[libname] = {} end
        if not shortfinfoclass[libname] then shortfinfoclass[libname] = {} end
        finfoclass[libname][key] = inf
        shortfinfoclass[libname][key] = infshort

        -- add to info
        if not finfo[key] or #finfo[key]<200 then
          if finfo[key] then finfo[key] = finfo[key] .. "\n\n"
          else finfo[key] = "" end
          finfo[key] = finfo[key] .. inf
        elseif not finfo[key]:match("\n %(%.%.%.%)$") then
          finfo[key] = finfo[key].."\n (...)"
        end

        -- add to shortinfo
        if not shortfinfo[key] or #shortfinfo[key]<200 then
          if shortfinfo[key] then shortfinfo[key] = shortfinfo[key] .. "\n"
          else shortfinfo[key] = "" end
          shortfinfo[key] = shortfinfo[key] .. infshortbatch
        elseif not shortfinfo[key]:match("\n %(%.%.%.%)$") then
          shortfinfo[key] = shortfinfo[key].."\n (...)"
        end
      end
      if info.type == "keyword" then
        keys[key] = true
      end
      staticnames[key] = true
    end
  end
  traverse(apiac,apibasename)
end

local function generateAPIInfo(only)
  for i,api in pairs(apis) do
    if ((not only) or i == only) then
      fillTips(api,"")
    end
  end
end

local function updateAssignCache(editor)
  if (editor.spec.typeassigns and not editor.assignscache) then
    local assigns = editor.spec.typeassigns(editor)
    editor.assignscache = {
      assigns = assigns,
      line = editor:GetCurrentLine(),
    }
  end
end

-- assumes a tidied up string (no spaces, braces..)
local function resolveAssign(editor,tx)
  local ac = editor.api.ac
  local sep = editor.spec.sep
  local anysep = "["..q(sep).."]"
  local assigns = editor.assignscache and editor.assignscache.assigns
  local function getclass(tab,a)
    local key,rest = a:match("([%w_]+)"..anysep.."(.*)")
    key = tonumber(key) or key -- make this work for childs[0]
    if (key and rest and tab.childs and tab.childs[key]) then
      return getclass(tab.childs[key],rest)
    end
    -- process valuetype, but only if it doesn't reference the current tab
    if (tab.valuetype and tab ~= ac.childs[tab.valuetype]) then
      return getclass(ac,tab.valuetype..sep:sub(1,1)..a)
    end
    return tab,a
  end

  local c
  if (assigns) then
    -- find assign
    local change, n, stopat = true, 0, os.clock() + 0.2
    while (change) do
      -- abort the check if the auto-complete is taking too long
      if n > 50 and os.clock() > stopat then
        if ide.config.acandtip.warning then
          DisplayOutputLn("Warning: Auto-complete was aborted after taking too long to complete."
            .. " Please report this warning along with the text you were typing to support@zerobrane.com.")
        end
        break
      else
        n = n + 1
      end

      local classname = nil
      c = ""
      change = false
      for w,s in tx:gmatch("([%w_]+)("..anysep.."?)") do
        local old = classname
        -- check if what we have so far can be matched with a class name
        -- this can happen if it's a reference to a value with a known type
        classname = classname or assigns[c..w]
        if (s ~= "" and old ~= classname) then
          -- continue checking unless this can lead to recursive substitution
          change = not classname:find("^"..w) and not classname:find("^"..c..w)
          c = classname..s
        else
          c = c..w..s
        end
      end
      tx = c
      -- if there is any class duplication, abort the loop
      if classname and select(2, c:gsub(classname, classname)) > 1 then break end
    end
  else
    c = tx
  end

  -- then work from api
  return getclass(ac,c)
end

function GetTipInfo(editor, content, short, fullmatch)
  if not content then return end

  updateAssignCache(editor)

  -- try to resolve the class
  content = content:gsub("%b[]",".0")
  local tab = resolveAssign(editor, content)
  local sep = editor.spec.sep
  local anysep = "["..q(sep).."]"

  local caller = content:match("([%w_]+)%(?%s*$")
  local class = (tab and tab.classname
    or caller and content:match("([%w_]+)"..anysep..caller.."%(?%s*$") or "")
  local tip = editor.api.tip

  local classtab = short and tip.shortfinfoclass or tip.finfoclass
  local funcstab = short and tip.shortfinfo or tip.finfo

  if (editor.assignscache and not (class and classtab[class])) then
    local assigns = editor.assignscache.assigns
    class = assigns and assigns[class] or class
  end

  local res = (caller and (class and classtab[class]) and classtab[class][caller]
    or (not fullmatch and funcstab[caller] or nil))
  -- some values may not have descriptions (for example, true/false);
  -- don't return empty strings as they are displayed as empty tooltips.
  return res and #res > 0 and res or nil
end

local function reloadAPI(only,subapis)
  newAPI(apis[only])
  loadallAPIs(only,subapis)
  generateAPIInfo(only)
end

function ReloadLuaAPI()
  local interp = ide.interpreter
  local cfgapi = ide.config.api
  local fname = interp and interp.fname
  local intapi = cfgapi and fname and cfgapi[fname]
  local apinames = {}
  -- general APIs as configured
  for _, v in ipairs(type(cfgapi) == 'table' and cfgapi or {}) do apinames[v] = true end
  -- interpreter-specific APIs as configured
  for _, v in ipairs(type(intapi) == 'table' and intapi or {}) do apinames[v] = true end
  -- interpreter APIs
  for _, v in ipairs(interp and interp.api or {}) do apinames[v] = true end
  reloadAPI("lua",apinames)
end

do
  local known = {}
  for _, spec in pairs(ide.specs) do
    if (spec.apitype) then
      known[spec.apitype] = true
    end
  end
  -- by defaul load every known api except lua
  known.lua = false

  scanAPIs()
  loadallAPIs(nil,nil,known)
  generateAPIInfo()
end

-------------
-- Dynamic Words

local dywordentries = {}
local dynamicwords = {}

local function addDynamicWord (api,word)
  if api.tip.keys[word] or api.tip.staticnames[word] then return end
  local cnt = dywordentries[word]
  if cnt then
    dywordentries[word] = cnt + 1
    return
  end
  dywordentries[word] = 1
  local wlow = word:lower()
  for i=0,#word do
    local k = wlow:sub(1,i)
    dynamicwords[k] = dynamicwords[k] or {}
    table.insert(dynamicwords[k], word)
  end
end
local function removeDynamicWord (api,word)
  if api.tip.keys[word] or api.tip.staticnames[word] then return end
  local cnt = dywordentries[word]
  if not cnt then return end

  if (cnt == 1) then
    dywordentries[word] = nil
    for i=0,#word do
      local wlow = word:lower()
      local k = wlow : sub (1,i)
      local page = dynamicwords[k]
      if page then
        local cnt  = #page
        for n=1,cnt do
          if page[n] == word then
            if cnt == 1 then
              dynamicwords[k] = nil
            else
              table.remove(page,n)
            end
            break
          end
        end
      end
    end
  else
    dywordentries[word] = cnt - 1
  end
end
function DynamicWordsReset ()
  dywordentries = {}
  dynamicwords = {}
end

local function getEditorLines(editor,line,numlines)
  return editor:GetTextRange(
    editor:PositionFromLine(line),editor:PositionFromLine(line+numlines+1))
end

function DynamicWordsAdd(editor,content,line,numlines)
  if ide.config.acandtip.nodynwords then return end
  local api = editor.api
  local anysep = "["..q(editor.spec.sep).."]"
  content = content or getEditorLines(editor,line,numlines)
  for word in content:gmatch(anysep.."?%s*([a-zA-Z_]+[a-zA-Z_0-9]+)") do
    addDynamicWord(api,word)
  end
end

function DynamicWordsRem(editor,content,line,numlines)
  if ide.config.acandtip.nodynwords then return end
  local api = editor.api
  local anysep = "["..q(editor.spec.sep).."]"
  content = content or getEditorLines(editor,line,numlines)
  for word in content:gmatch(anysep.."?%s*([a-zA-Z_]+[a-zA-Z_0-9]+)") do
    removeDynamicWord(api,word)
  end
end

function DynamicWordsRemoveAll(editor)
  DynamicWordsRem(editor,editor:GetText())
end

------------
-- Final Autocomplete

local cachemain = {}
local cachemethod = {}
local laststrategy
local function getAutoCompApiList(childs,fragment,method)
  fragment = fragment:lower()
  local strategy = ide.config.acandtip.strategy
  if (laststrategy ~= strategy) then
    cachemain = {}
    cachemethod = {}
    laststrategy = strategy
  end

  local cache = method and cachemethod or cachemain

  if (strategy == 2) then
    local wlist = cache[childs]
    if not wlist then
      wlist = " "
      for i,v in pairs(childs) do
        -- in some cases (tip.finfo), v may be a string; check for that first.
        -- if a:b typed, then value (type == "value") not allowed
        -- if a.b typed, then method (type == "method") not allowed
        if type(v) ~= 'table' or (v.type and
          ((method and v.type ~= "value")
            or (not method and v.type ~= "method"))) then
          wlist = wlist..i.." "
        end
      end
      cache[childs] = wlist
    end
    local ret = {}
    local g = string.gmatch
    local pat = fragment ~= "" and ("%s("..fragment:gsub(".",
        function(c)
          local l = c:lower()..c:upper()
          return "["..l.."][%w_]*"
        end)..")") or "([%w_]+)"
    pat = pat:gsub("%s","")
    for c in g(wlist,pat) do
      table.insert(ret,c)
    end

    return ret
  end

  if cache[childs] and cache[childs][fragment] then
    return cache[childs][fragment]
  end

  local t = {}
  cache[childs] = t

  local sub = strategy == 1
  for key,v in pairs(childs) do
    -- in some cases (tip.finfo), v may be a string; check for that first.
    -- if a:b typed, then value (type == "value") not allowed
    -- if a.b typed, then method (type == "method") not allowed
    if type(v) ~= 'table' or (v.type and
      ((method and v.type ~= "value")
        or (not method and v.type ~= "method"))) then
      local used = {}
      local kl = key:lower()
      for i=0,#key do
        local k = kl:sub(1,i)
        t[k] = t[k] or {}
        used[k] = true
        table.insert(t[k],key)
      end
      if (sub) then
        -- find camel case / _ separated subwords
        -- glfwGetGammaRamp -> g, gg, ggr
        -- GL_POINT_SPRIT -> g, gp, gps
        local last = ""
        for ks in string.gmatch(key,"([A-Z%d]*[a-z%d]*_?)") do
          local k = last..(ks:sub(1,1):lower())
          last = k

          t[k] = t[k] or {}
          if (not used[k]) then
            used[k] = true
            table.insert(t[k],key)
          end
        end
      end
    end
  end

  return t
end

function CreateAutoCompList(editor,key,pos)
  local api = editor.api
  local tip = api.tip
  local ac = api.ac
  local sep = editor.spec.sep

  local method = key:match(":[^"..q(sep).."]*$") ~= nil

  -- ignore keywords
  if tip.keys[key] then return end

  updateAssignCache(editor)

  local tab,rest = resolveAssign(editor,key)
  local progress = tab and tab.childs
  statusBar:SetStatusText(progress and tab.classname or "",1)
  if not (progress) then return end

  if (tab == ac) then
    local _, krest = rest:match("([%w_]+)["..q(sep).."]([%w_]*)%s*$")
    if (krest) then
      tab = #krest >= (ide.config.acandtip.startat or 2) and tip.finfo or {}
      rest = krest:gsub("[^%w_]","")
    else
      rest = rest:gsub("[^%w_]","")
    end
  else
    rest = rest:gsub("[^%w_]","")
  end

  local last = key:match("([%w_]+)%s*$")

  -- build dynamic word list
  -- only if api search couldnt descend
  -- ie we couldnt find matching sub items
  local dw = ""
  if (last and #last >= (ide.config.acandtip.startat or 2)) then
    last = last:lower()
    if dynamicwords[last] then
      local list = dynamicwords[last]
      table.sort(list,function(a,b)
          local ma,mb = a:sub(1,#last)==last, b:sub(1,#last)==last
          if (ma and mb) or (not ma and not mb) then return a<b end
          return ma
        end)
      -- ignore if word == last and sole user
      for i,v in ipairs(list) do
        if (v:lower() == last and dywordentries[v] == 1) then
          table.remove(list,i)
          break
        end
      end

      dw = table.concat(list," ")
    end
  end

  -- list from api
  local apilist = getAutoCompApiList(tab.childs or tab,rest,method)

  local function addInheritance(tab, apilist, seen)
    if not tab.inherits then return end
    for base in tab.inherits:gmatch("[%w_"..q(sep).."]+") do
      local tab = ac
      -- map "a.b.c" to class hierarchy (a.b.c)
      for class in base:gmatch("[%w_]+") do tab = tab.childs[class] end

      if tab and not seen[tab] then
        seen[tab] = true
        for _,v in pairs(getAutoCompApiList(tab.childs,rest,method)) do
          table.insert(apilist, v)
        end
        addInheritance(tab, apilist, seen)
      end
    end
  end

  -- handle (multiple) inheritance; add matches from the parent class/lib
  addInheritance(tab, apilist, {[tab] = true})

  -- include local/global variables
  if ide.config.acandtip.symbols and not key:find(q(sep)) then
    local vars, context = {}
    local tokens = editor:GetTokenList()
    for _, token in ipairs(tokens) do
      if token.fpos and pos and token.fpos > pos then break end
      if token[1] == 'Id' or token[1] == 'Var' then
        local var = token.name
        if var ~= key and var:find(key, 1, true) == 1 then
          -- if it's a global variable, store in the auto-complete list,
          -- but if it's local, store separately as it needs to be checked
          table.insert(token.context[var] and vars or apilist, var)
        end
        context = token.context
      end
    end
    for _, var in pairs(context and vars or {}) do
      if context[var] then table.insert(apilist, var) end
    end
  end

  local compstr = ""
  if apilist then
    if (#rest > 0) then
      local strategy = ide.config.acandtip.strategy

      if (strategy == 2 and #apilist < 128) then
        -- when matching "ret": "ret." < "re.t" < "r.et"
        local pat = rest:gsub(".", function(c) return "["..c:lower()..c:upper().."](.-)" end)
        local weights = {}
        local penalty = 0.1
        local function weight(str)
          if not weights[str] then
            local w = 0
            str:gsub(pat,function(...)
                local l = {...}
                -- penalize gaps between matches, more so at the beginning
                for n, v in ipairs(l) do w = w + #v * (1 + (#l-n)*penalty) end
              end)
            weights[str] = w
          end
          return weights[str]
        end
        table.sort(apilist,function(a,b)
            local ma, mb = weight(a), weight(b)
            if (ma == mb) then return a:lower()<b:lower() end
            return ma<mb
          end)
      else
        table.sort(apilist,function(a,b)
            local ma,mb = a:sub(1,#rest)==rest, b:sub(1,#rest)==rest
            if (ma and mb) or (not ma and not mb) then return a<b end
            return ma
          end)
      end
    else
      table.sort(apilist)
    end

    local prev = apilist[#apilist]
    for i = #apilist-1,1,-1 do
      if prev == apilist[i] then
        table.remove(apilist, i+1)
      else prev = apilist[i] end
    end

    compstr = table.concat(apilist," ")
  end

  -- concat final, list complete first
  local li = compstr .. (#compstr > 0 and #dw > 0 and " " or "") .. dw

  return li ~= "" and (#li > 1024 and li:sub(1,1024).."..." or li) or nil
end
