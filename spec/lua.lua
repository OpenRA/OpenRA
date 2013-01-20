-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local funcdef = "([A-Za-z_][A-Za-z0-9_%.%:]*)%s*"
local funccall = "([A-Za-z_][A-Za-z0-9_]*)%s*"
local decindent = {
  ['else'] = true, ['elseif'] = true, ['end'] = true}
local incindent = {
  ['else'] = true, ['elseif'] = true, ['for'] = true, ['do'] = true,
  ['if'] = true, ['repeat'] = true, ['until'] = true, ['while'] = true}
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

return {
  exts = {"lua", "rockspec"},
  lexer = wxstc.wxSTC_LEX_LUA,
  apitype = "lua",
  linecomment = "--",
  sep = "%.:",
  isfncall = function(str)
    return string.find(str, funccall .. "[%({'\"]")
  end,
  isfndef = isfndef,
  isdecindent = function(str)
    -- this handles three different cases:
    local term = str:match("^%s*(%w+)%s*$")
    -- (1) 'end', 'elseif', 'else'
    local match = term and decindent[term]
    -- (2) 'end)' and 'end}'
    if not term then term, match = str:match("^%s*(end)%s*([%)%}]+)%s*[,;]?") end
    -- (3) '},', '};', '),' and ');'
    if not term then match = str:match("^%s*[%)%}]+%s*[,;]?%s*$") end

    return match and 1 or 0, match and term and 1 or 0
  end,
  isincindent = function(str)
    local term = str:match("^%s*(%w+)[^%w]*")
    term = term and incindent[term] and 1 or 0
    local _, opened = str:gsub("([%{%(])", "%1")
    local _, closed = str:gsub("([%}%)])", "%1")
    local func = (isfndef(str) or str:match("[^%w]+function%s*%(")) and 1 or 0
    -- ended should only be used to negate term and func effects
    local ended = (term + func > 0) and str:match("[^%w]+end%s*$") and 1 or 0

    return opened - closed + func + term - ended
  end,

  typeassigns = function(editor)
    local line = editor:GetCurrentLine()
    line = line-1

    local scopestart = {"if","do","while","function", "local%s+function", "for", "else", "elseif"}
    local scopeend = {"end"}
    local iscomment = editor.spec.iscomment

    local assigns = {}

    -- iterate up until a line starts with scopestart
    -- always ignore lines whose first symbol is styled as comment
    local endline = line
    while (line >= 0) do
      local ls = editor:PositionFromLine(line)
      local s = bit.band(editor:GetStyleAt(ls),31)

      if (not iscomment[s]) then
        local tx = editor:GetLine(line)
        local leftscope

        for i,v in ipairs(scopestart) do
          if (tx:match("^"..v)) then
            leftscope = true
          end
        end
        if (leftscope) then
          break
        end
      end
      line = line -1
    end

    local added
    while (line <= endline) do
      local ls = editor:PositionFromLine(line)
      local s = bit.band(editor:GetStyleAt(ls),31)

      if (not iscomment[s]) then
        local tx = editor:GetLine(line) --= string

        -- check for assignments
        local varname = "([%w_%.]+)"
        local identifier = "([%w_%.:]+)"

        -- special hint
        local typ,var = tx:match("%s*%-%-=%s*"..varname.."%s+"..identifier)
        if (var and typ) then
          assigns[var] = typ
          added = true
        else
          -- real assignments
          local var,typ,rest = tx:match("%s*"..identifier.."%s*=%s*"..identifier.."(.*)")
          local comment = rest and rest:match(".*%-%-=%s*"..varname.."%s*$")
          local comma = rest and rest:match(".-%s*([,]*)%s*$")
          if (var and comment) then
            assigns[var] = comment
            added = true
          elseif (var and typ and comma=="") then
            class,func = typ:match(varname.."[%.:]"..varname)
            if (func) then
              local funcnames = {"new","load","create"}
              for i,v in ipairs(funcnames) do
                if (func:match("^"..v)) then
                  assigns[var] = class
                  added = true
                  break
                end
              end
            elseif (assigns[typ]) then
              assigns[var] = assigns[typ]
              added = true
            end
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

    [[_G _VERSION _ENV false io.stderr io.stdin io.stdout nil math.huge math.pi package.config
      package.cpath package.loaded package.loaders package.path package.preload package.searchers self true]],

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
      package.loadlib package.searchpath package.seeall
      string.byte string.char string.dump string.find string.format string.gmatch string.gsub string.len
      string.lower string.match string.rep string.reverse string.sub string.upper
      byte find format gmatch gsub len lower match rep reverse sub upper
      table.concat table.insert table.maxn table.pack table.remove table.sort table.unpack]]
  },
}
