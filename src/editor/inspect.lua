-- Integration with LuaInspect
-- (C) 2012 Paul Kulchenko

local M, LA, LI, T = {}
local FAST = true

local function init()
  if LA then return end

  require "metalua"
  LA = require "luainspect.ast"
  LI = require "luainspect.init"
  T = require "luainspect.types"

  if FAST then
    LI.eval_comments = function () end
    LI.infer_values = function () end
  end
end

function M.warnings_from_string(src, file)
  init()

  local ast, err, linenum, colnum = LA.ast_from_string(src, file)
  if err then return nil, err, linenum, colnum end

  if FAST then
    LI.inspect(ast, nil, src)
    LA.ensure_parents_marked(ast)
  else
    local tokenlist = LA.ast_to_tokenlist(ast, src)
    LI.inspect(ast, tokenlist, src)
    LI.mark_related_keywords(ast, tokenlist, src)
  end

  return M.show_warnings(ast)
end

function M.show_warnings(top_ast)
  local warnings = {}
  local function warn(msg, linenum, path)
    warnings[#warnings+1] = (path or "?") .. "(" .. (linenum or 0) .. "): " .. msg
  end
  local function known(o) return not T.istype[o] end
  local function index(f) -- build abc.def.xyz name recursively
    return (f[1].tag == 'Id' and f[1][1] or index(f[1])) .. '.' .. f[2][1] end
  local isseen, globseen = {}, {}
  LA.walk(top_ast, function(ast)
    local line = ast.lineinfo and ast.lineinfo.first[1] or 0
    local path = ast.lineinfo and ast.lineinfo.first[4] or '?'
    local name = ast[1]
    -- check if we're masking a variable in the same scope
    if ast.localmasking and name ~= '_' and
       ast.level == ast.localmasking.level then
      local linenum = ast.localmasking.lineinfo.first[1]
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
          local func1 = func[1][1]
          local fname = assignment and func1 and type(func1[1]) == 'string' and func1[1]
            or (func1.tag == 'Index' and index(func1))
          -- "function foo(bar)" => func.tag == 'Set'
          --   `Set{{`Id{"foo"}},{`Function{{`Id{"bar"}},{}}}}
          -- "local function foo(bar)" => func.tag == 'Localrec'
          -- "local _, foo = 1, function(bar)" => func.tag == 'Local'
          -- "print(function(bar) end)" => func.tag == nil
          -- "function tbl:foo(bar)" => func.tag == 'Set'
          --   `Set{{`Index{`Id{"tbl"},`String{"foo"}}},{`Function{{`Id{"self"},`Id{"bar"}},{}}}}
          -- "function tbl.abc:foo(bar)" => func.tag == 'Set'
          --   `Set{{`Index{`Index{`Id{"tbl"},`String{"abc"}},`String{"foo"}}},{`Function{{`Id{"self"},`Id{"bar"}},{}}}},
          warn("unused parameter '" .. name .. "'" ..
               (func and assignment
                     and (fname and func.tag
                               and (" in function '" .. fname .. "'")
                               or " in anonymous function")
                     or ""),
               line, path)
        end
      else
        if parent.tag == 'Localrec' then -- local function foo...
          warn("unused local function '" .. name .. "'", line, path)
        else
          warn("unused local variable '" .. name .. "'; "..
               "consider removing or replacing with '_'", line, path)
        end
      end
    end
    -- added check for FAST as ast.seevalue relies on value evaluation,
    -- which is very slow even on simple and short scripts
    if not FAST and ast.isfield and not(known(ast.seevalue.value) and ast.seevalue.value ~= nil) then
      warn("unknown field " .. name, ast.lineinfo.first[1], path)
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
    if note and not isseen[vast.parent] then
      isseen[vast.parent] = true
      warn("function '" .. name .. "': " .. note, line, path)
    end
  end)
  return warnings
end

local frame = ide.frame
local menu = frame.menuBar:GetMenu(frame.menuBar:FindMenu(TR("&Project")))

-- insert after "Compile" item
for item = 0, menu:GetMenuItemCount()-1 do
   if menu:FindItemByPosition(item):GetId() == ID_COMPILE then
     menu:Insert(item+1, ID_ANALYZE, TR("Analyze")..KSC(ID_ANALYZE), TR("Analyze the source code"))
     break
   end
end

local debugger = ide.debugger
local openDocuments = ide.openDocuments

local function analyzeProgram(editor)
  local editorText = editor:GetText()
  local id = editor:GetId()
  local filePath = DebuggerMakeFileName(editor, openDocuments[id].filePath)

  if frame.menuBar:IsChecked(ID_CLEAROUTPUT) then ClearOutput() end
  DisplayOutput("Analyzing the source code")
  frame:Update()

  local warn, err = M.warnings_from_string(editorText, filePath)
  if err then -- report compilation error
    DisplayOutput(": not completed\n")
    return false
  end

  DisplayOutput((": %s warning%s.\n")
    :format(#warn > 0 and #warn or 'no', #warn == 1 and '' or 's'))
  DisplayOutputNoMarker(table.concat(warn, "\n") .. "\n")

  return true -- analyzed ok
end

frame:Connect(ID_ANALYZE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    ActivateOutput()
    local editor = GetEditor()
    if not analyzeProgram(editor) then CompileProgram(editor) end
  end)
frame:Connect(ID_ANALYZE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil and debugger.pid == nil) and (editor ~= nil))
  end)
