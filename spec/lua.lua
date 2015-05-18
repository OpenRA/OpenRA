-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local funcdef = "([A-Za-z_][A-Za-z0-9_%.%:]*)%s*"
local decindent = {
  ['else'] = true, ['elseif'] = true, ['until'] = true, ['end'] = true}
local incindent = {
  ['else'] = true, ['elseif'] = true, ['for'] = true, ['do'] = true,
  ['if'] = true, ['repeat'] = true, ['while'] = true}
local function isfndef(str)
  local l
  local s,e,cap,par = string.find(str, "function%s+" .. funcdef .. "(%(.-%))")
  -- try to match without brackets now, but only at the beginning of the line
  if (not s) then
    s,e,cap = string.find(str, "^%s*function%s+" .. funcdef)
  end
  -- try to match "foo = function()"
  if (not s) then
    s,e,cap,par = string.find(str, funcdef .. "=%s*function%s*(%(.-%))")
  end
  if (s) then
    l = string.find(string.sub(str,1,s-1),"local%s+$")
    cap = cap .. " " .. (par or "(?)")
  end
  return s,e,cap,l
end
local q = EscapeMagic

local function ldoc(tx, typepatt)
  local varname = "([%w_]+)"
  -- <type> == ?string, ?|T1|T2
  -- anything that follows optional "|..." is ignored
  local typename = "%??"..typepatt
  -- @tparam[...] <type> <paramname>
  -- @param[type=<type>] <paramname>
  -- @string[...] <paramname>; not handled
  local t, v = tx:match("--%s*@tparam%b[]%s+"..typename.."%s+"..varname)
  if not t then -- try without optional [...] part
    t, v = tx:match("--%s*@tparam%s+"..typename.."%s+"..varname)
  end
  if not t then
    t, v = tx:match("--%s*@param%s*%[type="..typename.."%]%s+"..varname)
  end
  return t, v
end

return {
  exts = {"lua", "rockspec", "wlua"},
  lexer = wxstc.wxSTC_LEX_LUA,
  apitype = "lua",
  linecomment = "--",
  sep = ".:",
  isdecindent = function(str)
    str = str:gsub('%-%-%[=*%[.*%]=*%]',''):gsub('%-%-.*','')
    -- this handles three different cases:
    local term = (str:match("^%s*(%w+)%s*$")
      or str:match("^%s*(elseif)[%s%(]")
      or str:match("^%s*(until)[%s%(]")
      or str:match("^%s*(else)%f[%W]")
    )
    -- (1) 'end', 'elseif', 'else', 'until'
    local match = term and decindent[term]
    -- (2) 'end)', 'end}', 'end,', and 'end;'
    if not term then term, match = str:match("^%s*(end)%s*([%)%}]*)%s*[,;]?") end
    -- endFoo could be captured as well; filter it out
    if term and str:match("^%s*(end)%w") then term = nil end
    -- (3) '},', '};', '),' and ');'
    if not term then match = str:match("^%s*[%)%}]+%s*[,;]?%s*$") end

    return match and 1 or 0, match and term and 1 or 0
  end,
  isincindent = function(str)
    -- remove "long" comments and escaped slashes (to process \' and \" below)
    str = str:gsub('%-%-%[=*%[.-%]=*%]',''):gsub('\\[\\\'"]','')
    while true do
      local num, sep = nil, str:match("['\"]")
      if not sep then break end
      str, num = str:gsub(sep..".-\\"..sep,sep):gsub(sep..".-"..sep,"")
      if num == 0 then break end
    end
    -- strip comments after strings are processed and remove all function calls
    str = str:gsub('%-%-.*',''):gsub("%b()","()")

    local term = str:match("^%s*(%w+)%W*")
    local terminc = term and incindent[term] and 1 or 0
    -- fix 'if' not terminated with 'then'
    -- or 'then' not started with 'if'
    if (term == 'if' or term == 'elseif') and not str:match("%f[%w]then%f[%W]")
    or (term == 'for') and not str:match("%S%s+do%f[%W]")
    or (term == 'while') and not str:match("%f[%w]do%f[%W]") then
      terminc = 0
    elseif not (term == 'if' or term == 'elseif') and str:match("%f[%w]then%f[%W]")
    or not (term == 'for') and str:match("%S%s+do%f[%W]")
    or not (term == 'while') and str:match("%f[%w]do%f[%W]") then
      terminc = 1
    end
    local _, opened = str:gsub("([%{%(])", "%1")
    local _, closed = str:gsub("([%}%)])", "%1")
    local func = (isfndef(str) or str:match("%W+function%s*%(")) and 1 or 0
    -- ended should only be used to negate term and func effects
    local anon = str:match("%W+function%s*%(.+%Wend%W")
    local ended = (terminc + func > 0) and (str:match("%W+end%s*$") or anon) and 1 or 0

    return opened - closed + func + terminc - ended
  end,
  marksymbols = function(code, pos, vars)
    local PARSE = require 'lua_parser_loose'
    local LEX = require 'lua_lexer_loose'
    local lx = LEX.lexc(code, nil, pos)
    return coroutine.wrap(function()
      local varnext = {}
      PARSE.parse_scope_resolve(lx, function(op, name, lineinfo, vars, nobreak)
        if not(op == 'Id' or op == 'Statement' or op == 'Var'
            or op == 'Function' or op == 'String'
            or op == 'VarNext' or op == 'VarInside' or op == 'VarSelf'
            or op == 'FunctionCall' or op == 'Scope' or op == 'EndScope') then
          return end -- "normal" return; not interested in other events

        -- level needs to be adjusted for VarInside as it comes into scope
        -- only after next block statement
        local at = vars[0] and (vars[0] + (op == 'VarInside' and 1 or 0))
        if op == 'Statement' then
          for _, token in pairs(varnext) do coroutine.yield(unpack(token)) end
          varnext = {}
        elseif op == 'VarNext' or op == 'VarInside' then
          table.insert(varnext, {'Var', name, lineinfo, vars, at, nobreak})
        end

        coroutine.yield(op, name, lineinfo, vars, op == 'Function' and at-1 or at, nobreak)
      end, vars)
    end)
  end,

  typeassigns = function(editor)
    local maxlines = 48 -- scan up to this many lines back
    local iscomment = editor.spec.iscomment
    local assigns = {}
    local endline = editor:GetCurrentLine()-1
    local line = math.max(endline-maxlines, 0)

    while (line <= endline) do
      local ls = editor:PositionFromLine(line)
      local tx = editor:GetLine(line) --= string
      local s = bit.band(editor:GetStyleAt(ls + #tx:match("^%s*") + 2),31)

      -- check for assignments
      local sep = editor.spec.sep
      local varname = "([%w_][%w_"..q(sep:sub(1,1)).."]*)"
      local identifier = "([%w_][%w_"..q(sep).."%s]*)"

      -- special hint
      local typ, var = tx:match("%s*%-%-=%s*"..varname.."%s+"..identifier)
      local ldoctype, ldocvar = ldoc(tx, varname)
      if var and typ then
        assigns[var] = typ:gsub("%s","")
      elseif ldoctype and ldocvar then
        assigns[ldocvar] = ldoctype
      elseif not iscomment[s] then
        -- real assignments
        local var,typ = tx:match("%s*"..identifier.."%s*=%s*([^;]+)")

        var = var and var:gsub("local",""):gsub("%s","")
        typ = (typ and typ
          :gsub("%b()","")
          :gsub("%b{}","")
          :gsub("%b[]",".0")
          -- replace concatenation with addition to avoid misidentifying types
          :gsub("%.%.+","+")
          -- remove comments; they may be in strings, but that's okay here
          :gsub("%-%-.*",""))
        if (typ and (typ:match(",") or typ:match("%sor%s") or typ:match("%sand%s"))) then
          typ = nil
        end
        typ = typ and typ:gsub("%s","")
        typ = typ and typ:gsub(".+", function(s)
            return (s:find("^'[^']*'$")
              or s:find('^"[^"]*"$')
              or s:find('^%[=*%[.*%]=*%]$')) and 'string' or s
          end)

        -- filter out everything that is not needed
        if typ and typ ~= 'string' -- special value for all strings
        and (not typ:match('^'..identifier..'$') -- not an identifier
          or typ:match('^%d') -- or a number
          or editor.api.tip.keys[typ] -- or a keyword
          ) then
          typ = nil
        end

        if (var and typ) then
          local class,func = typ:match(varname.."["..q(sep).."]"..varname)
          if (assigns[typ]) then
            assigns[var] = assigns[typ]
          elseif (func) then
            local added
            local funcnames = {"new","load","create"}
            for _,v in ipairs(funcnames) do
              if (func == v) then
                assigns[var] = class
                added = true
                break
              end
            end
            if (not added) then
              -- let's hope autocomplete info can resolve this
              assigns[var] = typ
            end
          else
            assigns[var] = typ
          end
        end
      end
      line = line+1
    end

    return assigns
  end,

  lexerstyleconvert = {
    text = {wxstc.wxSTC_LUA_IDENTIFIER,},

    lexerdef = {wxstc.wxSTC_LUA_DEFAULT,},
    comment = {wxstc.wxSTC_LUA_COMMENT,
      wxstc.wxSTC_LUA_COMMENTLINE,
      wxstc.wxSTC_LUA_COMMENTDOC,},
    stringtxt = {wxstc.wxSTC_LUA_STRING,
      wxstc.wxSTC_LUA_CHARACTER,
      wxstc.wxSTC_LUA_LITERALSTRING,},
    stringeol = {wxstc.wxSTC_LUA_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_LUA_PREPROCESSOR,},
    operator = {wxstc.wxSTC_LUA_OPERATOR,},
    number = {wxstc.wxSTC_LUA_NUMBER,},

    keywords0 = {wxstc.wxSTC_LUA_WORD,},
    keywords1 = {wxstc.wxSTC_LUA_WORD2,},
    keywords2 = {wxstc.wxSTC_LUA_WORD3,},
    keywords3 = {wxstc.wxSTC_LUA_WORD4,},
    keywords4 = {wxstc.wxSTC_LUA_WORD5,},
    keywords5 = {wxstc.wxSTC_LUA_WORD6,},
    keywords6 = {wxstc.wxSTC_LUA_WORD7,},
    keywords7 = {wxstc.wxSTC_LUA_WORD8,},
  },

  keywords = {
    [[and break do else elseif end for function goto if in local not or repeat return then until while]],

    [[_G _VERSION _ENV false io.stderr io.stdin io.stdout nil math.huge math.pi self true]],

    [[assert collectgarbage dofile error getfenv getmetatable ipairs load loadfile loadstring
      module next pairs pcall print rawequal rawget rawlen rawset require
      select setfenv setmetatable tonumber tostring type unpack xpcall]],

    [[bit32.arshift bit32.band bit32.bnot bit32.bor bit32.btest bit32.bxor bit32.extract
      bit32.lrotate bit32.lshift bit32.replace bit32.rrotate bit32.rshift
      coroutine.create coroutine.resume coroutine.running coroutine.status coroutine.wrap coroutine.yield
      debug.debug debug.getfenv debug.gethook debug.getinfo debug.getlocal
      debug.getmetatable debug.getregistry debug.getupvalue debug.getuservalue debug.setfenv
      debug.sethook debug.setlocal debug.setmetatable debug.setupvalue debug.setuservalue
      debug.traceback debug.upvalueid debug.upvaluejoin
      io.close io.flush io.input io.lines io.open io.output io.popen io.read io.tmpfile io.type io.write
      close flush lines read seek setvbuf write
      math.abs math.acos math.asin math.atan math.atan2 math.ceil math.cos math.cosh math.deg math.exp
      math.floor math.fmod math.frexp math.ldexp math.log math.log10 math.max math.min math.modf
      math.pow math.rad math.random math.randomseed math.sin math.sinh math.sqrt math.tan math.tanh
      os.clock os.date os.difftime os.execute os.exit os.getenv os.remove os.rename os.setlocale os.time os.tmpname
      package.loadlib package.searchpath package.seeall package.config
      package.cpath package.loaded package.loaders package.path package.preload package.searchers
      string.byte string.char string.dump string.find string.format string.gmatch string.gsub string.len
      string.lower string.match string.rep string.reverse string.sub string.upper
      byte find format gmatch gsub len lower match rep reverse sub upper
      table.move, string.pack, string.unpack, string.packsize
      table.concat table.insert table.maxn table.pack table.remove table.sort table.unpack]]
  },
}
