-- Copyright 2012-15 Paul Kulchenko, ZeroBrane LLC
-- Integration with LuaInspect
---------------------------------------------------------

local M, LA, LI, T = {}

local function init()
  if LA then return end

  -- metalua is using 'checks', which noticeably slows the execution
  -- stab it with out own
  package.loaded.checks = {}
  checks = function() end

  LA = require "luainspect.ast"
  LI = require "luainspect.init"
  T = require "luainspect.types"
end

function M.pos2line(pos)
  return pos and 1 + select(2, M.src:sub(1,pos):gsub(".-\n[^\n]*", ""))
end

function M.warnings_from_string(src, file)
  init()

  local ast, err, linenum, colnum = LA.ast_from_string(src, file)
  if not ast and err then return nil, err, linenum, colnum end

  if ide.config.staticanalyzer.infervalue then
    local tokenlist = LA.ast_to_tokenlist(ast, src)
    LI.inspect(ast, tokenlist, src)
    LI.mark_related_keywords(ast, tokenlist, src)
  else
    -- stub out LI functions that depend on tokenlist,
    -- which is not built in the "fast" mode
    local ec, iv = LI.eval_comments, LI.infer_values
    LI.eval_comments, LI.infer_values = function() end, function() end

    LI.inspect(ast, nil, src)
    LA.ensure_parents_marked(ast)

    LI.eval_comments, LI.infer_values = ec, iv
  end

  local globinit = {}
  local spec = GetSpec(wx.wxFileName(file):GetExt())
  for k in pairs(spec and GetApi(spec.apitype or "none").ac.childs or {}) do
    globinit[k] = true
  end

  M.src, M.file = src, file
  return M.show_warnings(ast, globinit)
end

local function cleanError(err)
  return err and err:gsub(".-:%d+: file%s+",""):gsub(", line (%d+), char %d+", ":%1")
end

function AnalyzeFile(file)
  local warn, err, line, pos = M.warnings_from_string(FileRead(file), file)
  return warn, cleanError(err), line, pos
end

function AnalyzeString(src, file)
  local warn, err, line, pos = M.warnings_from_string(src, file or "<string>")
  return warn, cleanError(err), line, pos
end

function M.show_warnings(top_ast, globinit)
  local warnings = {}
  local function warn(msg, linenum, path)
    warnings[#warnings+1] = (path or M.file or "?") .. ":" .. (linenum or M.pos2line(M.ast.pos) or 0) .. ": " .. msg
  end
  local function known(o) return not T.istype[o] end
  local function index(f) -- build abc.def.xyz name recursively
    if not f or f.tag ~= 'Index' or not f[1] or not f[2] then return end
    local main = f[1].tag == 'Id' and f[1][1] or index(f[1])
    return main and type(f[2][1]) == "string" and (main .. '.' .. f[2][1]) or nil
  end
  local globseen, isseen, fieldseen = globinit or {}, {}, {}
  LA.walk(top_ast, function(ast)
    M.ast = ast
    local path, line = tostring(ast.lineinfo):gsub('<C|','<'):match('<([^|]+)|L(%d+)')
    local name = ast[1]
    -- check if we're masking a variable in the same scope
    if ast.localmasking and name ~= '_' and
       ast.level == ast.localmasking.level then
      local linenum = ast.localmasking.lineinfo
        and tostring(ast.localmasking.lineinfo.first):match('|L(%d+)')
        or M.pos2line(ast.localmasking.pos)
      local parent = ast.parent and ast.parent.parent
      local func = parent and parent.tag == 'Localrec'
      warn("local " .. (func and 'function' or 'variable') .. " '" ..
        name .. "' masks earlier declaration " ..
        (linenum and "on line " .. linenum or "in the same scope"),
        line, path)
    end
    if ast.localdefinition == ast and not ast.isused and
       not ast.isignore then
      local parent = ast.parent and ast.parent.parent
      local isparam = parent and parent.tag == 'Function'
      if isparam then
        if name ~= 'self' then
          local func = parent.parent and parent.parent.parent
          local assignment = not func.tag or func.tag == 'Set' or func.tag == 'Localrec'
          -- anonymous functions can also be defined in expressions,
          -- for example, 'Op' or 'Return' tags
          local expression = not assignment and func.tag
          local func1 = func[1][1]
          local fname = assignment and func1 and type(func1[1]) == 'string'
            and func1[1] or (func1 and func1.tag == 'Index' and index(func1))
          -- "function foo(bar)" => func.tag == 'Set'
          --   `Set{{`Id{"foo"}},{`Function{{`Id{"bar"}},{}}}}
          -- "local function foo(bar)" => func.tag == 'Localrec'
          -- "local _, foo = 1, function(bar)" => func.tag == 'Local'
          -- "print(function(bar) end)" => func.tag == nil
          -- "a = a or function(bar) end" => func.tag == nil
          -- "return(function(bar) end)" => func.tag == 'Return'
          -- "function tbl:foo(bar)" => func.tag == 'Set'
          --   `Set{{`Index{`Id{"tbl"},`String{"foo"}}},{`Function{{`Id{"self"},`Id{"bar"}},{}}}}
          -- "function tbl.abc:foo(bar)" => func.tag == 'Set'
          --   `Set{{`Index{`Index{`Id{"tbl"},`String{"abc"}},`String{"foo"}}},{`Function{{`Id{"self"},`Id{"bar"}},{}}}},
          warn("unused parameter '" .. name .. "'" ..
               (func and (assignment or expression)
                     and (fname and func.tag
                               and (" in function '" .. fname .. "'")
                               or " in anonymous function")
                     or ""),
               line, path)
        end
      else
        if parent and parent.tag == 'Localrec' then -- local function foo...
          warn("unused local function '" .. name .. "'", line, path)
        else
          warn("unused local variable '" .. name .. "'; "..
               "consider removing or replacing with '_'", line, path)
        end
      end
    end
    -- added check for "fast" mode as ast.seevalue relies on value evaluation,
    -- which is very slow even on simple and short scripts
    if ide.config.staticanalyzer.infervalue and ast.isfield
    and not(known(ast.seevalue.value) and ast.seevalue.value ~= nil) then
      if not fieldseen[name] then
        fieldseen[name] = true
        local var = index(ast.parent)
        local parent = ast.parent and var
          and (" in '"..var:gsub("%."..name.."$","").."'")
          or ""

        local tblref = ast.parent and ast.parent[1]
        local localparam = (tblref and tblref.localdefinition
          and tblref.localdefinition.isparam)
        if not localparam then
          warn("first use of unknown field '" .. name .."'"..parent,
            ast.lineinfo and tostring(ast.lineinfo.first):match('|L(%d+)'), path)
        end
      end
    elseif ast.tag == 'Id' and not ast.localdefinition and not ast.definedglobal then
      if not globseen[name] then
        globseen[name] = true
        local parent = ast.parent
        -- if being called and not one of the parameters
        if parent and parent.tag == 'Call' and parent[1] == ast then
          warn("first use of unknown global function '" .. name .. "'", line, path)
        else
          warn("first use of unknown global variable '" .. name .. "'", line, path)
        end
      end
    elseif ast.tag == 'Id' and not ast.localdefinition and ast.definedglobal then
      local parent = ast.parent and ast.parent.parent
      if parent and parent.tag == 'Set' and not globseen[name] -- report assignments to global
        -- only report if it is on the left side of the assignment
        -- this is a bit tricky as it can be assigned as part of a, b = c, d
        -- `Set{ {lhs+} {expr+} } -- lhs1, lhs2... = e1, e2...
        and parent[1] == ast.parent
        and parent[2][1].tag ~= "Function" then -- but ignore global functions
        warn("first assignment to global variable '" .. name .. "'", line, path)
        globseen[name] = true
      end
    elseif (ast.tag == 'Set' or ast.tag == 'Local') and #(ast[2]) > #(ast[1]) then
      warn(("value discarded in multiple assignment: %d values assigned to %d variable%s")
        :format(#(ast[2]), #(ast[1]), #(ast[1]) > 1 and 's' or ''), line, path)
    end
    local vast = ast.seevalue or ast
    local note = vast.parent
             and (vast.parent.tag == 'Call' or vast.parent.tag == 'Invoke')
             and vast.parent.note
    if note and not isseen[vast.parent] and type(name) == "string" then
      isseen[vast.parent] = true
      warn("function '" .. name .. "': " .. note, line, path)
    end
  end)
  return warnings
end

local frame = ide.frame

-- insert after "Compile" item
local _, menu, compilepos = ide:FindMenuItem(ID_COMPILE)
if compilepos then
  menu:Insert(compilepos+1, ID_ANALYZE, TR("Analyze")..KSC(ID_ANALYZE), TR("Analyze the source code"))
end

local function analyzeProgram(editor)
  if ide:GetLaunchedProcess() == nil and not ide:GetDebugger():IsConnected() then ClearOutput() end
  DisplayOutput("Analyzing the source code")
  frame:Update()

  local editorText = editor:GetText()
  local doc = ide:GetDocument(editor)
  local filePath = doc:GetFilePath() or doc:GetFileName()
  local warn, err = M.warnings_from_string(editorText, filePath)
  if err then -- report compilation error
    DisplayOutputLn((": not completed.\n%s"):format(cleanError(err)))
    return false
  end

  DisplayOutputLn((": %s warning%s.")
    :format(#warn > 0 and #warn or 'no', #warn == 1 and '' or 's'))
  DisplayOutputNoMarker(table.concat(warn, "\n") .. (#warn > 0 and "\n" or ""))

  return true -- analyzed ok
end

frame:Connect(ID_ANALYZE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    ActivateOutput()
    local editor = GetEditor()
    if not analyzeProgram(editor) then
      CompileProgram(editor, { reportstats = false, keepoutput = true })
    end
  end)
frame:Connect(ID_ANALYZE, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(GetEditor() ~= nil) end)
