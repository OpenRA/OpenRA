-- Integration with LuaInspect
-- (C) 2012 Paul Kulchenko

require "metalua"

local M = {}

local LA = require "luainspect.ast"
local LI = require "luainspect.init"
local T = require "luainspect.types"

local FAST = true
if FAST then
  LI.eval_comments = function () end
  LI.infer_values = function () end
end

function M.warnings_from_string(src, file)
  local ast, err, linenum, colnum = LA.ast_from_string(src, file)
  if err then return nil, err, linenum, colnum end

  if FAST then
    LI.inspect(ast, nil, src)
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
    warnings[#warnings+1] = (path or "?") .. "(" .. linenum .. "): " .. msg
  end
  local function known(o) return not T.istype[o] end
  local isseen = {}
  LA.walk(top_ast, function(ast)
      local line = ast.lineinfo and ast.lineinfo.first[1] or 0
      local path = ast.lineinfo and ast.lineinfo.first[4] or '?'
      -- check if we're masking a variable in the same scope
      if ast.localmasking and 
         ast.level == ast.localmasking.level then
        local linenum = ast.localmasking.lineinfo.first[1]
        warn("local variable '" .. ast[1] .. "' masks earlier declaration " ..
          (linenum and "on line " .. linenum or "in the same scope"),
          line, path)
      end
      if ast.localdefinition == ast and not ast.isused
      and not ast.isignore and ast[1] ~= 'self' then
        warn("unused local variable '" .. ast[1] .. "'"..
             "; consider removing or replacing with '_'", line, path)
      end
      -- remove as it requires value evaluation, which is very slow
      -- even on simple and short scripts
      if false and ast.isfield and not(known(ast.seevalue.value) and ast.seevalue.value ~= nil) then
        warn("unknown field " .. ast[1], ast.lineinfo.first[1], path)
      elseif ast.tag == 'Id' and not ast.localdefinition and not ast.definedglobal then
        warn("unknown global variable '" .. ast[1] .. "'", line, path)
      end
      local vast = ast.seevalue or ast
      local note = vast.parent and (vast.parent.tag == 'Call' or vast.parent.tag == 'Invoke')
                    and vast.parent.note
      if note and not isseen[vast.parent] then
        isseen[vast.parent] = true
        warn("function '" .. ast[1] .. "': " .. note, line, path)
      end
  end)
  return warnings
end

local frame = ide.frame
local menu = frame.menuBar:GetMenu(frame.menuBar:FindMenu("&Project"))
local ID_ANALYZE = ID "debug.analyze"

-- insert after "Compile" item
for item = 0, menu:GetMenuItemCount()-1 do
   if menu:FindItemByPosition(item):GetId() == ID_COMPILE then
     menu:Insert(item+1, ID_ANALYZE, "Analyze\tShift-F7", "Analyze the source code")
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
  DisplayOutput("Analizing the source code")
  frame:Update()

  local warn, err = M.warnings_from_string(editorText, filePath)
  DisplayOutput(": " .. (#warn > 0 and (#warn .. " warnings") or "no warnings.") .. "\n")

  if err then return false end -- report compilation error
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
