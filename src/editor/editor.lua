-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local editorID = 100 -- window id to create editor pages with, incremented for new editors

local openDocuments = ide.openDocuments
local statusBar = ide.frame.statusBar
local notebook = ide.frame.notebook
local edcfg = ide.config.editor
local styles = ide.config.styles
local unpack = table.unpack or unpack
local q = EscapeMagic

local margin = { LINENUMBER = 0, MARKER = 1, FOLD = 2 }
local linenummask = "99999"
local foldtypes = {
  [0] = { wxstc.wxSTC_MARKNUM_FOLDEROPEN, wxstc.wxSTC_MARKNUM_FOLDER,
    wxstc.wxSTC_MARKNUM_FOLDERSUB, wxstc.wxSTC_MARKNUM_FOLDERTAIL, wxstc.wxSTC_MARKNUM_FOLDEREND,
    wxstc.wxSTC_MARKNUM_FOLDEROPENMID, wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL,
  },
  box = { wxstc.wxSTC_MARK_BOXMINUS, wxstc.wxSTC_MARK_BOXPLUS,
    wxstc.wxSTC_MARK_VLINE, wxstc.wxSTC_MARK_LCORNER, wxstc.wxSTC_MARK_BOXPLUSCONNECTED,
    wxstc.wxSTC_MARK_BOXMINUSCONNECTED, wxstc.wxSTC_MARK_TCORNER,
  },
  circle = { wxstc.wxSTC_MARK_CIRCLEMINUS, wxstc.wxSTC_MARK_CIRCLEPLUS,
    wxstc.wxSTC_MARK_VLINE, wxstc.wxSTC_MARK_LCORNERCURVE, wxstc.wxSTC_MARK_CIRCLEPLUSCONNECTED,
    wxstc.wxSTC_MARK_CIRCLEMINUSCONNECTED, wxstc.wxSTC_MARK_TCORNERCURVE,
  },
  plus = { wxstc.wxSTC_MARK_MINUS, wxstc.wxSTC_MARK_PLUS },
  arrow = { wxstc.wxSTC_MARK_ARROWDOWN, wxstc.wxSTC_MARK_ARROW },
}

-- ----------------------------------------------------------------------------
-- Update the statusbar text of the frame using the given editor.
-- Only update if the text has changed.
local statusTextTable = { "OVR?", "R/O?", "Cursor Pos" }

local function updateStatusText(editor)
  local texts = { "", "", "" }
  if ide.frame and editor then
    local pos = editor:GetCurrentPos()
    local selected = #editor:GetSelectedText()
    local selections = ide.wxver >= "2.9.5" and editor:GetSelections() or 1

    texts = {
      iff(editor:GetOvertype(), TR("OVR"), TR("INS")),
      iff(editor:GetReadOnly(), TR("R/O"), TR("R/W")),
      table.concat({
        TR("Ln: %d"):format(editor:LineFromPosition(pos) + 1),
        TR("Col: %d"):format(editor:GetColumn(pos) + 1),
        selected > 0 and TR("Sel: %d/%d"):format(selected, selections) or "",
      }, ' ')}
  end

  if ide.frame then
    for n in ipairs(texts) do
      if (texts[n] ~= statusTextTable[n]) then
        statusBar:SetStatusText(texts[n], n+1)
        statusTextTable[n] = texts[n]
      end
    end
  end
end

local function updateBraceMatch(editor)
  local pos = editor:GetCurrentPos()
  local posp = pos > 0 and pos-1
  local char = editor:GetCharAt(pos)
  local charp = posp and editor:GetCharAt(posp)
  local match = { [string.byte("<")] = true,
    [string.byte(">")] = true,
    [string.byte("(")] = true,
    [string.byte(")")] = true,
    [string.byte("{")] = true,
    [string.byte("}")] = true,
    [string.byte("[")] = true,
    [string.byte("]")] = true,
  }

  pos = (match[char] and pos) or (charp and match[charp] and posp)

  if (pos) then
    -- don't match brackets in markup comments
    local style = bit.band(editor:GetStyleAt(pos), 31)
    if (MarkupIsSpecial and MarkupIsSpecial(style)
      or editor.spec.iscomment[style]) then return end

    local pos2 = editor:BraceMatch(pos)
    if (pos2 == wxstc.wxSTC_INVALID_POSITION) then
      editor:BraceBadLight(pos)
    else
      editor:BraceHighlight(pos,pos2)
    end
    editor.matchon = true
  elseif(editor.matchon) then
    editor:BraceBadLight(wxstc.wxSTC_INVALID_POSITION)
    editor:BraceHighlight(wxstc.wxSTC_INVALID_POSITION,-1)
    editor.matchon = false
  end
end

-- Check if file is altered, show dialog to reload it
local function isFileAlteredOnDisk(editor)
  if not editor then return end

  local id = editor:GetId()
  if openDocuments[id] then
    local filePath = openDocuments[id].filePath
    local fileName = openDocuments[id].fileName
    local oldModTime = openDocuments[id].modTime

    if filePath and (string.len(filePath) > 0) and oldModTime and oldModTime:IsValid() then
      local modTime = GetFileModTime(filePath)
      if modTime == nil then
        openDocuments[id].modTime = nil
        wx.wxMessageBox(
          TR("File '%s' no longer exists."):format(fileName),
          GetIDEString("editormessage"),
          wx.wxOK + wx.wxCENTRE, ide.frame)
      elseif not editor:GetReadOnly() and modTime:IsValid() and oldModTime:IsEarlierThan(modTime) then
        local ret = (edcfg.autoreload and (not EditorIsModified(editor)) and wx.wxYES)
          or wx.wxMessageBox(
            TR("File '%s' has been modified on disk."):format(fileName)
            .."\n"..TR("Do you want to reload it?"),
            GetIDEString("editormessage"),
            wx.wxYES_NO + wx.wxCENTRE, ide.frame)

        if ret ~= wx.wxYES or ReLoadFile(filePath, editor, true) then
          openDocuments[id].modTime = GetFileModTime(filePath)
        end
      end
    end
  end
end

local function navigateToPosition(editor, fromPosition, toPosition, length)
  table.insert(editor.jumpstack, fromPosition)
  editor:GotoPosEnforcePolicy(toPosition)
  if length then
    editor:SetAnchor(toPosition + length)
  end
end

local function navigateBack(editor)
  if #editor.jumpstack == 0 then return end
  local pos = table.remove(editor.jumpstack)
  editor:GotoPosEnforcePolicy(pos)
  return true
end

-- ----------------------------------------------------------------------------
-- Get/Set notebook editor page, use nil for current page, returns nil if none
function GetEditor(selection)
  if selection == nil then
    selection = notebook:GetSelection()
  end
  local editor
  if (selection >= 0) and (selection < notebook:GetPageCount())
    and (notebook:GetPage(selection):GetClassInfo():GetClassName()=="wxStyledTextCtrl") then
    editor = notebook:GetPage(selection):DynamicCast("wxStyledTextCtrl")
  end
  return editor
end

-- init new notebook page selection, use nil for current page
function SetEditorSelection(selection)
  local editor = GetEditor(selection)
  updateStatusText(editor) -- update even if nil
  statusBar:SetStatusText("",1)
  ide.frame:SetTitle(ExpandPlaceholders(ide.config.format.apptitle))

  if editor then
    editor:SetFocus()
    editor:SetSTCFocus(true)

    local id = editor:GetId()
    FileTreeMarkSelected(openDocuments[id] and openDocuments[id].filePath or '')
    AddToFileHistory(openDocuments[id] and openDocuments[id].filePath)
  else
    FileTreeMarkSelected('')
  end

  SetAutoRecoveryMark()
end

function GetEditorFileAndCurInfo(nochecksave)
  local editor = GetEditor()
  if (not (editor and (nochecksave or SaveIfModified(editor)))) then
    return
  end

  local id = editor:GetId()
  local filepath = openDocuments[id].filePath
  if not filepath then return end

  local fn = wx.wxFileName(filepath)
  fn:Normalize()

  local info = {}
  info.pos = editor:GetCurrentPos()
  info.line = editor:GetCurrentLine()
  info.sel = editor:GetSelectedText()
  info.sel = info.sel and info.sel:len() > 0 and info.sel or nil
  info.selword = info.sel and info.sel:match("([^a-zA-Z_0-9]+)") or info.sel

  return fn,info
end

function EditorAutoComplete(editor)
  if not (editor and editor.spec) then return end

  local pos = editor:GetCurrentPos()
  -- don't do auto-complete in comments or strings.
  -- the current position and the previous one have default style (0),
  -- so we need to check two positions back.
  local style = pos >= 2 and bit.band(editor:GetStyleAt(pos-2),31) or 0
  if editor.spec.iscomment[style] or editor.spec.isstring[style] then return end

  -- retrieve the current line and get a string to the current cursor position in the line
  local line = editor:GetCurrentLine()
  local linetx = editor:GetLine(line)
  local linestart = editor:PositionFromLine(line)
  local localpos = pos-linestart

  local lt = linetx:sub(1,localpos)
  lt = lt:gsub("%s*(["..editor.spec.sep.."])%s*", "%1")
  -- strip closed brace scopes
  lt = lt:gsub("%b()","")
  lt = lt:gsub("%b{}","")
  lt = lt:gsub("%b[]",".0")
  -- remove everything that can't be auto-completed
  lt = lt:match("[%w_"..q(editor.spec.sep).."]*$")

  -- know now which string is to be completed
  local userList = CreateAutoCompList(editor, lt, pos)

  -- remove any suggestions that match the word the cursor is on
  -- for example, if typing 'foo' in front of 'bar', 'foobar' is not offered
  local right = linetx:sub(localpos+1,#linetx):match("^([%a_]+[%w_]*)")
  if userList and right then
    -- remove all spaces that may be left if "foo" removed from "foo foo"
    userList = (userList:gsub("%f[%w_]"..lt..right.."%f[%W]","")
      :gsub("^ +",""):gsub(" +$",""):gsub("  +"," "))
  end

  -- don't show the list if it only suggests what's already typed
  if userList and #userList > 0 and not lt:find(userList.."$") then
    editor:UserListShow(1, userList)
  elseif editor:AutoCompActive() then
    editor:AutoCompCancel()
  end
end

local ident = "([a-zA-Z_][a-zA-Z_0-9%.%:]*)"
local function getValAtPosition(editor, pos)
  local line = editor:LineFromPosition(pos)
  local linetx = editor:GetLine(line)
  local linestart = editor:PositionFromLine(line)
  local localpos = pos-linestart

  local selected = editor:GetSelectionStart() ~= editor:GetSelectionEnd()
    and pos >= editor:GetSelectionStart() and pos <= editor:GetSelectionEnd()

  -- check if we have a selected text or an identifier.
  -- for an identifier, check fragments on the left and on the right.
  -- this is to match 'io' in 'i^o.print' and 'io.print' in 'io.pr^int'.
  -- remove square brackets to make tbl[index].x show proper values.
  local start = linetx:sub(1,localpos)
    :gsub("%b[]", function(s) return ("."):rep(#s) end)
    :find(ident.."$")

  local right, funccall = linetx:sub(localpos+1,#linetx):match("^([a-zA-Z_0-9]*)%s*(['\"{%(]?)")
  local var = selected
    -- GetSelectedText() returns concatenated text when multiple instances
    -- are selected, so get the selected text based on start/end
    and editor:GetTextRange(editor:GetSelectionStart(), editor:GetSelectionEnd())
    or (start and linetx:sub(start,localpos):gsub(":",".")..right or nil)

  -- since this function can be called in different contexts, we need
  -- to detect function call of different types:
  -- 1. foo.b^ar(... -- the cursor (pos) is on the function name
  -- 2. foo.bar(..^. -- the cursor (pos) is on the parameter list
  -- "var" has value for #1 and the following fragment checks for #2

  -- check if the style is the right one; this is to ignore
  -- comments, strings, numbers (to avoid '1 = 1'), keywords, and such
  local goodpos = true
  if start and not selected then
    local style = bit.band(editor:GetStyleAt(linestart+start),31)
    if editor.spec.iscomment[style]
    or (MarkupIsAny and MarkupIsAny(style)) -- markup in comments
    or editor.spec.isstring[style]
    or style == wxstc.wxSTC_LUA_NUMBER
    or style == wxstc.wxSTC_LUA_WORD then
      goodpos = false
    end
  end

  local linetxtopos = linetx:sub(1,localpos)
  funccall = (#funccall > 0) and goodpos and var
    or (linetxtopos..")"):match(ident .. "%s*%b()$")
    or (linetxtopos.."}"):match(ident .. "%s*%b{}$")
    or (linetxtopos.."'"):match(ident .. "%s*'[^']*'$")
    or (linetxtopos..'"'):match(ident .. '%s*"[^"]*"$')
    or nil

  -- don't do anything for strings or comments or numbers
  if not goodpos then return nil, funccall end

  return var, funccall
end

local function formatUpToX(s)
  local x = math.max(20, ide.config.acandtip.width)
  local splitstr = "([ \t]*)(%S*)([ \t]*)(\n?)"
  local t = {""}
  for prefix, word, suffix, newline in s:gmatch(splitstr) do
    if #(t[#t]) + #prefix + #word > x and #t > 0 then
      table.insert(t, word..suffix)
    else
      t[#t] = t[#t]..prefix..word..suffix
    end
    if #newline > 0 then table.insert(t, "") end
  end
  return table.concat(t, "\n")
end

local function callTipFitAndShow(editor, pos, tip)
  local point = editor:PointFromPosition(pos)
  local height = editor:TextHeight(pos)
  local maxlines = math.max(1, math.floor(
    math.max(editor:GetSize():GetHeight()-point:GetY()-height, point:GetY())/height-1
  ))
  -- cut the tip to not exceed the number of maxlines.
  -- move the position to the left if needed to fit.
  -- find the longest line in terms of width in pixels.
  local maxwidth = 0
  local lines = {}
  for line in formatUpToX(tip):gmatch("[^\n]*\n?") do
    local width = editor:TextWidth(wxstc.wxSTC_STYLE_DEFAULT, line)
    if width > maxwidth then maxwidth = width end
    table.insert(lines, line)
    if #lines >= maxlines then
      lines[#lines] = lines[#lines]:gsub("%s*\n$","")..'...'
      break
    end
  end
  tip = table.concat(lines, '')

  local startpos = editor:PositionFromLine(editor:LineFromPosition(pos))
  local afterwidth = editor:GetSize():GetWidth()-point:GetX()
  if maxwidth > afterwidth then
    local charwidth = editor:TextWidth(wxstc.wxSTC_STYLE_DEFAULT, 'A')
    pos = math.max(startpos, pos - math.floor((maxwidth - afterwidth) / charwidth))
  end

  editor:CallTipShow(pos, tip)
end

function EditorCallTip(editor, pos, x, y)
  -- don't show anything if the calltip/auto-complete is active;
  -- this may happen after typing function name, while the mouse is over
  -- a different function or when auto-complete is on for a parameter.
  if editor:CallTipActive() or editor:AutoCompActive() then return end

  -- don't activate if the window itself is not active (in the background)
  if not ide.frame:IsActive() then return end

  local var, funccall = editor:ValueFromPosition(pos)
  -- if this is a value type rather than a function/method call, then use
  -- full match to avoid calltip about coroutine.status for "status" vars
  local tip = GetTipInfo(editor, funccall or var, false, not funccall)
  local limit = ide.config.acandtip.maxlength
  if ide.debugger and ide.debugger.server then
    if var then
      ide.debugger.quickeval(var, function(val)
        if #val > limit then val = val:sub(1, limit-3).."..." end
        -- check if the mouse position is specified and the mouse has moved,
        -- then don't show the tooltip as it's already too late for it.
        if x and y then
          local mpos = wx.wxGetMousePosition()
          if mpos.x ~= x or mpos.y ~= y then return end
        end
        if PackageEventHandle("onEditorCallTip", editor, val, funccall or var, true) ~= false then
          callTipFitAndShow(editor, pos, val)
        end
      end)
    end
  elseif tip then
    local oncalltip = PackageEventHandle("onEditorCallTip", editor, tip, funccall or var, false)
    -- only shorten if shown on mouse-over. Use shortcut to get full info.
    local showtooltip = ide.frame.menuBar:FindItem(ID_SHOWTOOLTIP)
    local suffix = "...\n"
        ..TR("Use '%s' to see full description."):format(showtooltip:GetLabel())
    if x and y and #tip > limit then
      tip = tip:sub(1, limit-#suffix):gsub("%W*%w*$","")..suffix
    end
    if oncalltip ~= false then callTipFitAndShow(editor, pos, tip) end
  end
end

function EditorIsModified(editor)
  local modified = false
  if editor then
    local id = editor:GetId()
    modified = openDocuments[id]
      and (openDocuments[id].isModified or not openDocuments[id].filePath)
  end
  return modified
end

-- Indicator handling for functions and local/global variables
local indicator = {
  FNCALL = ide:GetIndicator("core.fncall"),
  LOCAL = ide:GetIndicator("core.varlocal"),
  GLOBAL = ide:GetIndicator("core.varglobal"),
  MASKING = ide:GetIndicator("core.varmasking"),
  MASKED = ide:GetIndicator("core.varmasked"),
}

function IndicateFunctionsOnly(editor, lines, linee)
  local sindic = styles.indicator
  if not (edcfg.showfncall and editor.spec and editor.spec.isfncall)
  or not (sindic and sindic.fncall and sindic.fncall.st ~= wxstc.wxSTC_INDIC_HIDDEN) then return end

  local lines = lines or 0
  local linee = linee or editor:GetLineCount()-1

  if (lines < 0) then return end

  local isfncall = editor.spec.isfncall
  local isinvalid = {}
  for i,v in pairs(editor.spec.iscomment) do isinvalid[i] = v end
  for i,v in pairs(editor.spec.iskeyword0) do isinvalid[i] = v end
  for i,v in pairs(editor.spec.isstring) do isinvalid[i] = v end

  editor:SetIndicatorCurrent(indicator.FNCALL)
  for line=lines,linee do
    local tx = editor:GetLine(line)
    local ls = editor:PositionFromLine(line)
    editor:IndicatorClearRange(ls, #tx)

    local from = 1
    local off = -1
    while from do
      tx = from==1 and tx or string.sub(tx,from)

      local f,t,w = isfncall(tx)

      if (f) then
        local p = ls+f+off
        local s = bit.band(editor:GetStyleAt(p),31)
        if not isinvalid[s] then editor:IndicatorFillRange(p, #w) end
        off = off + t
      end
      from = t and (t+1)
    end
  end
end

local delayed = {}

function IndicateIfNeeded()
  local editor = GetEditor()
  -- do the current one first
  if delayed[editor] then return IndicateAll(editor) end
  for ed in pairs(delayed) do return IndicateAll(ed) end
end

-- find all instances of a symbol at pos
-- return table with [0] as the definition position (if local)
local function indicateFindInstances(editor, name, pos)
  local tokens = editor:GetTokenList()
  local instances = {{[-1] = 1}}
  local this
  for _, token in ipairs(tokens) do
    local op = token[1]

    if op == 'EndScope' then -- EndScope has "new" level, so need +1
      if this and token.fpos > pos and this == token.at+1 then break end

      if #instances > 1 and instances[#instances][-1] == token.at+1 then
        table.remove(instances)
      end
    elseif token.name == name then
      if op == 'Id' then
        table.insert(instances[#instances], token.fpos)
      elseif op:find("^Var") then
        if this and this == token.at then break end

        -- if new Var is defined at the same level, replace the current frame;
        -- if not, add a new one; skip implicit definition of "self" variable.
        instances[#instances + (token.at > instances[#instances][-1] and 1 or 0)]
          = {[0] = (not token.self and token.fpos or nil), [-1] = token.at}
      end
      if token.fpos <= pos and pos <= token.fpos+#name then this = instances[#instances][-1] end
    end
  end
  instances[#instances][-1] = nil -- remove the current level
  -- only return the list if "this" instance has been found;
  -- this is to avoid reporting (improper) instances when checking for
  -- comments, strings, table fields, etc.
  return this and instances[#instances] or {}
end

function IndicateAll(editor, lines)
  if not ide.config.autoanalyzer then return end

  local d = delayed[editor]
  delayed[editor] = nil -- assume this can be finished for now

  -- this function can be called for an editor tab that is already closed
  -- when there are still some pending events for it, so handle it.
  if not ide:IsValidCtrl(editor) then return end

  -- if markvars is not set in the spec, nothing else to do
  if not (editor.spec and editor.spec.marksymbols) then return end

  local indic = styles.indicator or {}

  local pos, vars = d and d[1] or 1, d and d[2] or nil
  local start = lines and editor:PositionFromLine(lines)+1 or nil
  if d and start and pos >= start then
    -- ignore delayed processing as the change is earlier in the text
    pos, vars = 1, nil
  end

  local tokens = editor:GetTokenList()

  if start then -- if the range is specified
    local curindic = editor:GetIndicatorCurrent()
    editor:SetIndicatorCurrent(indicator.MASKED)
    for n = #tokens, 1, -1 do
      local token = tokens[n]
      -- find the last token before the range
      if not token.nobreak and token.name and token.fpos+#token.name < start then
        pos, vars = token.fpos+#token.name, token.context
        break
      end
      -- unmask all variables from the rest of the list
      if token[1] == 'Masked' then
        editor:IndicatorClearRange(token.fpos-1, #token.name)
      end
      -- trim the list as it will be re-generated
      table.remove(tokens, n)
    end

    -- Clear masked indicators from the current position to the end as these
    -- will be re-calculated and re-applied based on masking variables.
    -- This step is needed as some positions could have shifted after updates.
    editor:IndicatorClearRange(pos-1, editor:GetLength()-pos+1)

    editor:SetIndicatorCurrent(curindic)

    -- need to cleanup vars as they may include variables from later
    -- fragments (because the cut-point was arbitrary). Also need
    -- to clean variables in other scopes, hence getmetatable use.
    local vars = vars
    while vars do
      for name, var in pairs(vars) do
        -- remove all variables that are created later than the current pos
        -- skip all non-variable elements from the vars table
        if type(name) == 'string' then
          while type(var) == 'table' and var.fpos and (var.fpos > pos) do
            var = var.masked -- restored a masked var
            vars[name] = var
          end
        end
      end
      vars = getmetatable(vars) and getmetatable(vars).__index
    end
  else
    if pos == 1 then -- if not continuing, then trim the list
      tokens = editor:ResetTokenList()
    end
  end

  local cleared = {}
  for _, indic in ipairs {indicator.FNCALL, indicator.LOCAL, indicator.GLOBAL, indicator.MASKING} do
    cleared[indic] = pos
  end

  local function IndicateOne(indic, pos, length)
    editor:SetIndicatorCurrent(indic)
    editor:IndicatorClearRange(cleared[indic]-1, pos-cleared[indic])
    editor:IndicatorFillRange(pos-1, length)
    cleared[indic] = pos+length
  end

  local s = TimeGet()
  local canwork = start and 0.010 or 0.100 -- use shorter interval when typing
  local f = editor.spec.marksymbols(editor:GetText(), pos, vars)
  while true do
    local op, name, lineinfo, vars, at, nobreak = f()
    if not op then break end
    local var = vars and vars[name]
    local token = {op, name=name, fpos=lineinfo, at=at, context=vars,
      self = (op == 'VarSelf') or nil, nobreak=nobreak}
    if op == 'Function' then
      vars['function'] = (vars['function'] or 0) + 1
    end
    if op == 'FunctionCall' then
      if indic.fncall and edcfg.showfncall then
        IndicateOne(indicator.FNCALL, lineinfo, #name)
      end
    elseif op ~= 'VarNext' and op ~= 'VarInside' and op ~= 'Statement' and op ~= 'String' then
      table.insert(tokens, token)
    end

    -- indicate local/global variables
    if op == 'Id'
    and (var and indic.varlocal or not var and indic.varglobal) then
      IndicateOne(var and indicator.LOCAL or indicator.GLOBAL, lineinfo, #name)
    end

    -- indicate masked values at the same level
    if op == 'Var' and var and (var.masked and at == var.masked.at) then
      local fpos = var.masked.fpos
      -- indicate masked if it's not implicit self
      if indic.varmasked and not var.masked.self then
        editor:SetIndicatorCurrent(indicator.MASKED)
        editor:IndicatorFillRange(fpos-1, #name)
        table.insert(tokens, {"Masked", name=name, fpos=fpos, nobreak=nobreak})
      end

      if indic.varmasking then IndicateOne(indicator.MASKING, lineinfo, #name) end
    end
    -- in some rare cases `nobreak` may be a number indicating a desired
    -- position from which to start in case of a break
    if lineinfo and nobreak ~= true and (op == 'Statement' or op == 'String') and TimeGet()-s > canwork then
      delayed[editor] = {tonumber(nobreak) or lineinfo, vars}
      break
    end
  end

  -- clear indicators till the end of processed fragment
  pos = delayed[editor] and delayed[editor][1] or editor:GetLength()+1

  -- don't clear "masked" indicators as those can be set out of order (so
  -- last updated fragment is not always the last in terms of its position);
  -- these indicators should be up-to-date to the end of the code fragment.
  local funconly = ide.config.editor.showfncall and editor.spec.isfncall
  for _, indic in ipairs {indicator.FNCALL, indicator.LOCAL, indicator.GLOBAL, indicator.MASKING} do
    -- don't clear "funccall" indicators as those can be set based on
    -- IndicateFunctionsOnly processing, which is dealt with separately
    if indic ~= indicator.FNCALL or not funconly then IndicateOne(indic, pos, 0) end
  end

  local needmore = delayed[editor] ~= nil
  if ide.config.outlineinactivity then
    if needmore then ide.timers.outline:Stop()
    else ide.timers.outline:Start(ide.config.outlineinactivity*1000, wx.wxTIMER_ONE_SHOT)
    end
  end
  return needmore -- request more events if still need to work
end

-- ----------------------------------------------------------------------------
-- Create an editor
function CreateEditor(bare)
  local editor = ide:CreateStyledTextCtrl(notebook, editorID,
    wx.wxDefaultPosition, wx.wxSize(0, 0), wx.wxBORDER_NONE)

  editorID = editorID + 1 -- increment so they're always unique

  editor.matchon = false
  editor.assignscache = false
  editor.bom = false
  editor.jumpstack = {}
  editor.ctrlcache = {}
  editor.tokenlist = {}
  editor.onidle = {}
  -- populate cache with Ctrl-<letter> combinations for workaround on Linux
  -- http://wxwidgets.10942.n7.nabble.com/Menu-shortcuts-inconsistentcy-issue-td85065.html
  for id, shortcut in pairs(ide.config.keymap) do
    local key = shortcut:match('^Ctrl[-+](.)$')
    if key then editor.ctrlcache[key:byte()] = id end
  end

  -- populate editor keymap with configured combinations
  for _, map in ipairs(edcfg.keymap or {}) do
    local key, mod, cmd, os = unpack(map)
    if not os or os == ide.osname then
      if cmd then
        editor:CmdKeyAssign(key, mod, cmd)
      else
        editor:CmdKeyClear(key, mod)
      end
    end
  end

  editor:SetBufferedDraw(not ide.config.hidpi and true or false)
  editor:StyleClearAll()

  editor:SetFont(ide.font.eNormal)
  editor:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font.eNormal)

  editor:SetTabWidth(tonumber(edcfg.tabwidth) or 2)
  editor:SetIndent(tonumber(edcfg.tabwidth) or 2)
  editor:SetUseTabs(edcfg.usetabs and true or false)
  editor:SetIndentationGuides(edcfg.indentguide and true or false)
  editor:SetViewWhiteSpace(edcfg.whitespace and true or false)

  if (edcfg.usewrap) then
    editor:SetWrapMode(wxstc.wxSTC_WRAP_WORD)
    editor:SetWrapStartIndent(0)
    if ide.wxver >= "2.9.5" then
      if edcfg.wrapflags then
        editor:SetWrapVisualFlags(tonumber(edcfg.wrapflags) or wxstc.wxSTC_WRAPVISUALFLAG_NONE)
      end
      if edcfg.wrapstartindent then
        editor:SetWrapStartIndent(tonumber(edcfg.wrapstartindent) or 0)
      end
      if edcfg.wrapindentmode then
        editor:SetWrapIndentMode(edcfg.wrapindentmode)
      end
    end
  else
    editor:SetScrollWidth(100) -- set default width
    editor:SetScrollWidthTracking(1) -- enable width auto-adjustment
  end

  if edcfg.defaulteol == wxstc.wxSTC_EOL_CRLF
  or edcfg.defaulteol == wxstc.wxSTC_EOL_LF then
    editor:SetEOLMode(edcfg.defaulteol)
  -- else: keep wxStyledTextCtrl default behavior (CRLF on Windows, LF on Unix)
  end

  editor:SetCaretLineVisible(edcfg.caretline and true or false)

  editor:SetVisiblePolicy(wxstc.wxSTC_VISIBLE_STRICT, 3)

  editor:SetMarginType(margin.LINENUMBER, wxstc.wxSTC_MARGIN_NUMBER)
  editor:SetMarginMask(margin.LINENUMBER, 0)
  editor:SetMarginWidth(margin.LINENUMBER,
    editor:TextWidth(wxstc.wxSTC_STYLE_DEFAULT, linenummask))

  editor:SetMarginWidth(margin.MARKER, 18)
  editor:SetMarginType(margin.MARKER, wxstc.wxSTC_MARGIN_SYMBOL)
  editor:SetMarginMask(margin.MARKER, 0xffffffff - wxstc.wxSTC_MASK_FOLDERS)
  editor:SetMarginSensitive(margin.MARKER, true)

  editor:MarkerDefine(StylesGetMarker("currentline"))
  editor:MarkerDefine(StylesGetMarker("breakpoint"))
  editor:MarkerDefine(StylesGetMarker("bookmark"))

  if edcfg.fold then
    editor:SetMarginWidth(margin.FOLD, 18)
    editor:SetMarginType(margin.FOLD, wxstc.wxSTC_MARGIN_SYMBOL)
    editor:SetMarginMask(margin.FOLD, wxstc.wxSTC_MASK_FOLDERS)
    editor:SetMarginSensitive(margin.FOLD, true)
  end

  editor:SetFoldFlags(tonumber(edcfg.foldflags) or wxstc.wxSTC_FOLDFLAG_LINEAFTER_CONTRACTED)

  if ide.wxver >= "2.9.5" then
    -- allow multiple selection and multi-cursor editing if supported
    editor:SetMultipleSelection(1)
    editor:SetAdditionalCaretsBlink(1)
    editor:SetAdditionalSelectionTyping(1)
    -- allow extra ascent/descent
    editor:SetExtraAscent(tonumber(edcfg.extraascent) or 0)
    editor:SetExtraDescent(tonumber(edcfg.extradescent) or 0)
  end

  do
    local fg, bg = wx.wxWHITE, wx.wxColour(128, 128, 128)
    local foldtype = foldtypes[edcfg.foldtype] or foldtypes.box
    local foldmarkers = foldtypes[0]
    for m = 1, #foldmarkers do
      editor:MarkerDefine(foldmarkers[m], foldtype[m] or wxstc.wxSTC_MARK_EMPTY, fg, bg)
    end
    bg:delete()
  end

  if edcfg.calltipdelay and edcfg.calltipdelay > 0 then
    editor:SetMouseDwellTime(edcfg.calltipdelay)
  end

  editor:AutoCompSetIgnoreCase(ide.config.acandtip.ignorecase)
  if (ide.config.acandtip.strategy > 0) then
    editor:AutoCompSetAutoHide(0)
    editor:AutoCompStops([[ \n\t=-+():.,;*/!"'$%&~'#°^@?´`<>][|}{]])
  end

  function editor:GetTokenList() return self.tokenlist end
  function editor:ResetTokenList() self.tokenlist = {}; return self.tokenlist end

  function editor:SetupKeywords(...) return SetupKeywords(self, ...) end
  function editor:ValueFromPosition(pos) return getValAtPosition(self, pos) end

  function editor:DoWhenIdle(func) table.insert(self.onidle, func) end

  -- GotoPos should work by itself, but it doesn't (wx 2.9.5).
  -- This is likely because the editor window hasn't been refreshed yet,
  -- so its LinesOnScreen method returns 0/-1, which skews the calculations.
  -- To avoid this, the caret line is made visible at the first opportunity.
  do
    local redolater
    function editor:GotoPosDelayed(pos)
      local badtime = self:LinesOnScreen() <= 0 -- -1 on OSX, 0 on Windows
      if pos then
        if badtime then
          redolater = pos
          -- without this GotoPos the content is not scrolled correctly on
          -- Windows, but with this it's not scrolled correctly on OSX.
          if ide.osname ~= 'Macintosh' then self:GotoPos(pos) end
        else
          redolater = nil
          self:GotoPosEnforcePolicy(pos)
        end
      elseif not badtime and redolater then
        -- reset the left margin first to make sure that the position
        -- is set "from the left" to get the best content displayed.
        self:SetXOffset(0)
        self:GotoPosEnforcePolicy(redolater)
        redolater = nil
      end
    end
  end

  if bare then return editor end -- bare editor doesn't have any event handlers

  editor.ev = {}
  editor:Connect(wxstc.wxEVT_STC_MARGINCLICK,
    function (event)
      local line = editor:LineFromPosition(event:GetPosition())
      local marginno = event:GetMargin()
      if marginno == margin.MARKER then
        DebuggerToggleBreakpoint(editor, line)
      elseif marginno == margin.FOLD then
        local header = bit.band(editor:GetFoldLevel(line),
          wxstc.wxSTC_FOLDLEVELHEADERFLAG) == wxstc.wxSTC_FOLDLEVELHEADERFLAG
        if wx.wxGetKeyState(wx.WXK_SHIFT) and wx.wxGetKeyState(wx.WXK_CONTROL) then
          editor:FoldSome()
        elseif header then
          editor:ToggleFold(line)
        end
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_MODIFIED,
    function (event)
      if (editor.assignscache and editor:GetCurrentLine() ~= editor.assignscache.line) then
        editor.assignscache = false
      end
      local evtype = event:GetModificationType()
      local pos = event:GetPosition()
      local firstLine = editor:LineFromPosition(pos)
      local inserted = bit.band(evtype, wxstc.wxSTC_MOD_INSERTTEXT) ~= 0
      local deleted = bit.band(evtype, wxstc.wxSTC_MOD_DELETETEXT) ~= 0
      if (inserted or deleted) then
        SetAutoRecoveryMark()

        local linesChanged = inserted and event:GetLinesAdded() or 0
        -- collate events if they are for the same line
        local events = #editor.ev
        if events == 0 or editor.ev[events][1] ~= firstLine then
          editor.ev[events+1] = {firstLine, linesChanged}
        elseif events > 0 and editor.ev[events][1] == firstLine then
          editor.ev[events][2] = math.max(editor.ev[events][2], linesChanged)
        end
        DynamicWordsAdd(editor, nil, firstLine, linesChanged)
      end

      local beforeInserted = bit.band(evtype,wxstc.wxSTC_MOD_BEFOREINSERT) ~= 0
      local beforeDeleted = bit.band(evtype,wxstc.wxSTC_MOD_BEFOREDELETE) ~= 0

      if (beforeInserted or beforeDeleted) then
        -- unfold the current line being changed if folded
        local lastLine = editor:LineFromPosition(pos+event:GetLength())
        if not editor:GetFoldExpanded(firstLine)
        or not editor:GetLineVisible(firstLine)
        or not editor:GetLineVisible(lastLine) then
          editor:ToggleFold(firstLine)
          for line = firstLine, lastLine do
            if not editor:GetLineVisible(line) then editor:ToggleFold(line) end
          end
        end
      end

      -- hide calltip/auto-complete after undo/redo/delete
      local undodelete = (wxstc.wxSTC_MOD_DELETETEXT
        + wxstc.wxSTC_PERFORMED_UNDO + wxstc.wxSTC_PERFORMED_REDO)
      if bit.band(evtype, undodelete) ~= 0 then
        editor:DoWhenIdle(function(editor)
            if editor:CallTipActive() then editor:CallTipCancel() end
            if editor:AutoCompActive() then editor:AutoCompCancel() end
          end)
      end
      
      if ide.config.acandtip.nodynwords then return end
      -- only required to track changes

      if beforeDeleted then
        local text = editor:GetTextRange(pos, pos+event:GetLength())
        local _, numlines = text:gsub("\r?\n","%1")
        DynamicWordsRem(editor,nil,firstLine, numlines)
      end
      if beforeInserted then
        DynamicWordsRem(editor,nil,firstLine, 0)
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_CHARADDED,
    function (event)
      local LF = string.byte("\n")
      local ch = event:GetKey()
      local pos = editor:GetCurrentPos()
      local line = editor:GetCurrentLine()
      local linetx = editor:GetLine(line)
      local linestart = editor:PositionFromLine(line)
      local localpos = pos-linestart
      local linetxtopos = linetx:sub(1,localpos)

      if PackageEventHandle("onEditorCharAdded", editor, event) == false then
        -- this event has already been handled
      elseif (ch == LF) then
        -- auto-indent
        if (line > 0) then
          local indent = editor:GetLineIndentation(line - 1)
          local linedone = editor:GetLine(line - 1)

          -- if the indentation is 0 and the current line is not empty,
          -- but the previous line is empty, then take indentation from the
          -- current line (instead of the previous one). This may happen when
          -- CR is hit at the beginning of a line (rather than at the end).
          if indent == 0 and not linetx:match("^[\010\013]*$")
          and linedone:match("^[\010\013]*$") then
            indent = editor:GetLineIndentation(line)
          end

          local ut = editor:GetUseTabs()
          local tw = ut and editor:GetTabWidth() or editor:GetIndent()
          local style = bit.band(editor:GetStyleAt(editor:PositionFromLine(line-1)), 31)

          if edcfg.smartindent
          -- don't apply smartindent to multi-line comments or strings
          and not (editor.spec.iscomment[style]
            or editor.spec.isstring[style]
            or (MarkupIsAny and MarkupIsAny(style)))
          and editor.spec.isdecindent and editor.spec.isincindent then
            local closed, blockend = editor.spec.isdecindent(linedone)
            local opened = editor.spec.isincindent(linedone)

            -- if the current block is already indented, skip reverse indenting
            if (line > 1) and (closed > 0 or blockend > 0)
            and editor:GetLineIndentation(line-2) > indent then
              -- adjust opened first; this is needed when use ENTER after })
              if blockend == 0 then opened = opened + closed end
              closed, blockend = 0, 0
            end
            editor:SetLineIndentation(line-1, indent - tw * closed)
            indent = indent + tw * (opened - blockend)
            if indent < 0 then indent = 0 end
          end
          editor:SetLineIndentation(line, indent)

          indent = ut and (indent / tw) or indent
          editor:GotoPos(editor:GetCurrentPos()+indent)
        end

      elseif ch == ("("):byte() then
        local tip = GetTipInfo(editor,linetxtopos,ide.config.acandtip.shorttip)
        if tip then
          if editor:CallTipActive() then editor:CallTipCancel() end
          if PackageEventHandle("onEditorCallTip", editor, tip) ~= false then
            callTipFitAndShow(editor, pos, tip)
          end
        end

      elseif ide.config.autocomplete then -- code completion prompt
        local trigger = linetxtopos:match("["..editor.spec.sep.."%w_]+$")
        if trigger and (#trigger > 1 or trigger:match("["..editor.spec.sep.."]")) then
          editor:DoWhenIdle(function(editor) EditorAutoComplete(editor) end)
        end
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_DWELLSTART,
    function (event)
      -- on Linux DWELLSTART event seems to be generated even for those
      -- editor windows that are not active. What's worse, when generated
      -- the event seems to report "old" position when retrieved using
      -- event:GetX and event:GetY, so instead we use wxGetMousePosition.
      local linux = ide.osname == 'Unix'
      if linux and editor ~= GetEditor() then return end

      -- check if this editor has focus; it may not when Stack/Watch window
      -- is on top, but DWELL events are still triggered in this case.
      -- Don't want to show calltip as it is still shown when the focus
      -- is switched to a different application.
      local focus = editor:FindFocus()
      if focus and focus:GetId() ~= editor:GetId() then return end

      -- event:GetX() and event:GetY() positions don't correspond to
      -- the correct positions calculated using ScreenToClient (at least
      -- on Windows and Linux), so use what's calculated.
      local mpos = wx.wxGetMousePosition()
      local cpos = editor:ScreenToClient(mpos)
      local position = editor:PositionFromPointClose(cpos.x, cpos.y)
      if position ~= wxstc.wxSTC_INVALID_POSITION then
        EditorCallTip(editor, position, mpos.x, mpos.y)
      end
      event:Skip()
    end)

  editor:Connect(wxstc.wxEVT_STC_DWELLEND,
    function (event)
      if editor:CallTipActive() then editor:CallTipCancel() end
      event:Skip()
    end)

  editor:Connect(wx.wxEVT_KILL_FOCUS,
    function (event)
      if editor:AutoCompActive() then editor:AutoCompCancel() end
      PackageEventHandle("onEditorFocusLost", editor)
      event:Skip()
    end)

  editor:Connect(wxstc.wxEVT_STC_USERLISTSELECTION,
    function (event)
      if PackageEventHandle("onEditorUserlistSelection", editor, event) == false then
        return
      end

      if ide.wxver >= "2.9.5" and editor:GetSelections() > 1 then
        local text = event:GetText()
        -- capture all positions as the selection may change
        local positions = {}
        for s = 0, editor:GetSelections()-1 do
          table.insert(positions, editor:GetSelectionNCaret(s))
        end
        -- process all selections from last to first
        table.sort(positions)
        local mainpos = editor:GetSelectionNCaret(editor:GetMainSelection())

        editor:BeginUndoAction()
        for s = #positions, 1, -1 do
          local pos = positions[s]
          local startpos = editor:WordStartPosition(pos, true)
          editor:SetSelection(startpos, pos)
          editor:ReplaceSelection(text)
          -- if this is the main position, save new cursor position to restore
          if pos == mainpos then mainpos = editor:GetCurrentPos()
          elseif pos < mainpos then
            -- adjust main position as earlier changes may affect it
            mainpos = mainpos + #text - (pos - startpos)
          end
        end
        editor:EndUndoAction()

        editor:GotoPos(mainpos)
      else
        local pos = editor:GetCurrentPos()
        local startpos = editor:WordStartPosition(pos, true)
        local endpos = editor:WordEndPosition(pos, true)
        editor:SetSelection(startpos, ide.config.acandtip.droprest and endpos or pos)
        editor:ReplaceSelection(event:GetText())
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTREACHED,
    function ()
      local doc = ide:GetDocument(editor)
      if doc then doc:SetModified(false) end
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTLEFT,
    function ()
      local doc = ide:GetDocument(editor)
      if doc then doc:SetModified(true) end
    end)

  -- "updateStatusText" should be called in UPDATEUI event, but it creates
  -- several performance problems on Windows (using wx2.9.5+) when
  -- brackets or backspace is used (very slow screen repaint with 0.5s delay).
  -- Moving it to PAINTED event creates problems on OSX (using wx2.9.5+),
  -- where refresh of R/W and R/O status in the status bar is delayed.

  editor:Connect(wxstc.wxEVT_STC_PAINTED,
    function (event)
      PackageEventHandle("onEditorPainted", editor, event)

      if ide.osname == 'Windows' then
        -- STC_PAINTED is called on multiple editors when they point to
        -- the same document; only update status for the active one
        if notebook:GetSelection() == notebook:GetPageIndex(editor) then
          updateStatusText(editor)
        end

        if edcfg.usewrap ~= true and editor:AutoCompActive() then
          -- showing auto-complete list leaves artifacts on the screen,
          -- which can only be fixed by a forced refresh.
          -- shows with wxSTC 3.21 and both wxwidgets 2.9.5 and 3.1
          editor:Update()
          editor:Refresh()
        end
      end
    end)

  local alreadyProcessed = 0
  editor:Connect(wxstc.wxEVT_STC_UPDATEUI,
    function (event)
      PackageEventHandle("onEditorUpdateUI", editor, event)

      -- some of UPDATEUI events are triggered by blinking cursor, and since
      -- there are no changes, the rest of the processing can be skipped;
      -- the reason for `alreadyProcessed` is that it is not possible
      -- to completely skip all of these updates as this causes the issue
      -- of markup styling becoming visible after text deletion by Backspace.
      -- to avoid this, we allow the first update after any updates caused
      -- by real changes; the rest of UPDATEUI events are skipped.
      if event:GetUpdated() == wxstc.wxSTC_UPDATE_CONTENT
      and not next(editor.ev) then
         if alreadyProcessed > 1 then return end
      else
         alreadyProcessed = 0
      end
      alreadyProcessed = alreadyProcessed + 1

      if ide.osname ~= 'Windows' then updateStatusText(editor) end

      editor:GotoPosDelayed()
      updateBraceMatch(editor)
      local minupdated
      for _,iv in ipairs(editor.ev) do
        local line = iv[1]
        if not minupdated or line < minupdated then minupdated = line end
        IndicateFunctionsOnly(editor,line,line+iv[2])
      end
      if minupdated then
        local ok, res = pcall(IndicateAll, editor, minupdated)
        if not ok then DisplayOutputLn("Internal error: ",res,minupdated) end
      end
      local firstvisible = editor:DocLineFromVisible(editor:GetFirstVisibleLine())
      local lastline = math.min(editor:GetLineCount(),
        firstvisible + editor:LinesOnScreen())
      -- lastline - editor:LinesOnScreen() can get negative; fix it
      local firstline = math.min(math.max(0, lastline - editor:LinesOnScreen()),
        firstvisible)
      MarkupStyle(editor,minupdated or firstline,lastline)
      editor.ev = {}
    end)

  editor:Connect(wx.wxEVT_IDLE,
    function (event)
      while #editor.onidle > 0 do table.remove(editor.onidle)(editor) end
    end)

  editor:Connect(wx.wxEVT_LEFT_DOWN,
    function (event)
      if MarkupHotspotClick then
        local position = editor:PositionFromPointClose(event:GetX(),event:GetY())
        if position ~= wxstc.wxSTC_INVALID_POSITION then
          if MarkupHotspotClick(position, editor) then return end
        end
      end

      if event:ControlDown() and event:AltDown()
      -- ide.wxver >= "2.9.5"; fix after GetModifiers is added to wxMouseEvent in wxlua
      and not event:ShiftDown() and not event:MetaDown() then
        local point = event:GetPosition()
        local pos = editor:PositionFromPointClose(point.x, point.y)
        local value = pos ~= wxstc.wxSTC_INVALID_POSITION and editor:ValueFromPosition(pos) or nil
        local instances = value and indicateFindInstances(editor, value, pos+1)
        if instances and instances[0] then
          navigateToPosition(editor, pos, instances[0]-1, #value)
          return
        end
      end
      event:Skip()
    end)

  if edcfg.nomousezoom then
    -- disable zoom using mouse wheel as it triggers zooming when scrolling
    -- on OSX with kinetic scroll and then pressing CMD.
    editor:Connect(wx.wxEVT_MOUSEWHEEL,
      function (event)
        if wx.wxGetKeyState(wx.WXK_CONTROL) then return end
        event:Skip()
      end)
  end

  local inhandler = false
  editor:Connect(wx.wxEVT_SET_FOCUS,
    function (event)
      event:Skip()
      if inhandler or ide.exitingProgram then return end
      inhandler = true
      PackageEventHandle("onEditorFocusSet", editor)
      isFileAlteredOnDisk(editor)
      inhandler = false
    end)

  editor:Connect(wx.wxEVT_KEY_DOWN,
    function (event)
      local keycode = event:GetKeyCode()
      local mod = event:GetModifiers()
      local first, last = 0, notebook:GetPageCount()-1
      if PackageEventHandle("onEditorKeyDown", editor, event) == false then
        -- this event has already been handled
      elseif keycode == wx.WXK_ESCAPE then
        if editor:CallTipActive() or editor:AutoCompActive() then
          event:Skip()
        elseif ide.findReplace:IsShown() then
          ide.findReplace:Hide()
        elseif ide:GetMainFrame():IsFullScreen() then
          ShowFullScreen(false)
        end
      -- Ctrl-Home and Ctrl-End don't work on OSX with 2.9.5+; fix it
      elseif ide.osname == 'Macintosh' and ide.wxver >= "2.9.5"
        and (mod == wx.wxMOD_RAW_CONTROL or mod == (wx.wxMOD_RAW_CONTROL + wx.wxMOD_SHIFT))
        and (keycode == wx.WXK_HOME or keycode == wx.WXK_END) then
        local pos = keycode == wx.WXK_HOME and 0 or editor:GetLength()
        if event:ShiftDown() -- mark selection and scroll to caret
        then editor:SetCurrentPos(pos) editor:EnsureCaretVisible()
        else editor:GotoPos(pos) end
      elseif mod == wx.wxMOD_RAW_CONTROL and keycode == wx.WXK_PAGEUP
        or mod == (wx.wxMOD_RAW_CONTROL + wx.wxMOD_SHIFT) and keycode == wx.WXK_TAB then
        if notebook:GetSelection() == first
        then notebook:SetSelection(last)
        else notebook:AdvanceSelection(false) end
      elseif mod == wx.wxMOD_RAW_CONTROL
        and (keycode == wx.WXK_PAGEDOWN or keycode == wx.WXK_TAB) then
        if notebook:GetSelection() == last
        then notebook:SetSelection(first)
        else notebook:AdvanceSelection(true) end
      elseif (keycode == wx.WXK_DELETE or keycode == wx.WXK_BACK)
        and (mod == wx.wxMOD_NONE) then
        -- Delete and Backspace behave the same way for selected text
        if #(editor:GetSelectedText()) > 0 then
          editor:ClearAny()
        else
          local pos = editor:GetCurrentPos()
          if keycode == wx.WXK_BACK then
            pos = pos - 1
            if pos < 0 then return end
          end

          -- check if the modification is to one of "invisible" characters.
          -- if not, proceed with "normal" processing as there are other
          -- events that may depend on Backspace, for example, re-calculating
          -- auto-complete suggestions.
          local style = bit.band(editor:GetStyleAt(pos), 31)
          if not MarkupIsSpecial or not MarkupIsSpecial(style) then
            -- if BACKSPACE is used at tab stop, with spaces for indentation,
            -- and only whilespaces on the left, reduce indent
            if edcfg.backspaceunindent and keycode == wx.WXK_BACK and not editor:GetUseTabs() then
              -- get the line number from the *current* position of the cursor
              local line = editor:LineFromPosition(pos+1)
              local text = editor:GetLine(line):sub(1, pos-editor:PositionFromLine(line)+1)
              local tw = editor:GetIndent()
              -- if on the tab stop position and only white spaces on the left
              if text:find('^%s+$') and #text % tw == 0 then
                editor:SetLineIndentation(line, editor:GetLineIndentation(line) - tw)
                editor:GotoPos(pos+1-tw)
                return
              end
            end
            event:Skip()
            return
          end

          editor:SetTargetStart(pos)
          editor:SetTargetEnd(pos+1)
          editor:ReplaceTarget("")
        end
      elseif mod == wx.wxMOD_ALT and keycode == wx.WXK_LEFT then
        -- if no "jump back" is needed, then do normal processing as this
        -- combination can be mapped to some action
        if not navigateBack(editor) then event:Skip() end
      elseif (keycode == wx.WXK_DELETE and mod == wx.wxMOD_SHIFT)
          or (keycode == wx.WXK_INSERT and mod == wx.wxMOD_CONTROL) then
        ide.frame:AddPendingEvent(wx.wxCommandEvent(
          wx.wxEVT_COMMAND_MENU_SELECTED, keycode == wx.WXK_INSERT and ID_COPY or ID_CUT))
      elseif ide.osname == "Unix" and ide.wxver >= "2.9.5"
      and mod == wx.wxMOD_CONTROL and editor.ctrlcache[keycode] then
        ide.frame:AddPendingEvent(wx.wxCommandEvent(
          wx.wxEVT_COMMAND_MENU_SELECTED, editor.ctrlcache[keycode]))
      else
        if ide.osname == 'Macintosh' and mod == wx.wxMOD_META then
          return -- ignore a key press if Command key is also pressed
        end
        event:Skip()
      end
    end)

  local function selectAllInstances(instances, name, curpos)
    local this
    local idx = 0
    for _, pos in pairs(instances) do
      pos = pos - 1 -- positions are 0-based in Scintilla
      if idx == 0 then
        -- clear selections first as there seems to be a bug (Scintilla 3.2.3)
        -- that doesn't reset selection after right mouse click.
        editor:ClearSelections()
        editor:SetSelection(pos, pos+#name)
      else
        editor:AddSelection(pos+#name, pos)
      end

      -- check if this is the current selection
      if curpos >= pos and curpos <= pos+#name then this = idx end
      idx = idx + 1
    end
    if this then editor:SetMainSelection(this) end
  end

  editor:Connect(wxstc.wxEVT_STC_DOUBLECLICK,
    function(event)
      -- only activate selection of instances on Ctrl/Cmd-DoubleClick
      if event:GetModifiers() == wx.wxMOD_CONTROL then
        local pos = event:GetPosition()
        local value = pos ~= wxstc.wxSTC_INVALID_POSITION and editor:ValueFromPosition(pos) or nil
        local instances = value and indicateFindInstances(editor, value, pos+1)
        if instances and (instances[0] or #instances > 0) then
          selectAllInstances(instances, value, pos)
          return
        end
      end

      event:Skip()
    end)

  editor:Connect(wxstc.wxEVT_STC_ZOOM,
    function(event)
      editor:SetMarginWidth(margin.LINENUMBER,
        editor:TextWidth(wxstc.wxSTC_STYLE_DEFAULT, linenummask))
      -- if Shift+Zoom is used, then zoom all editors, not just the current one
      if wx.wxGetKeyState(wx.WXK_SHIFT) then
        local zoom = editor:GetZoom()
        for _, doc in pairs(openDocuments) do
          -- check the editor zoom level to avoid recursion
          if doc.editor:GetZoom() ~= zoom then doc.editor:SetZoom(zoom) end
        end
      end
      event:Skip()
    end)

  local pos, value, instances
  editor:Connect(wx.wxEVT_CONTEXT_MENU,
    function (event)
      local point = editor:ScreenToClient(event:GetPosition())
      pos = editor:PositionFromPointClose(point.x, point.y)
      value = pos ~= wxstc.wxSTC_INVALID_POSITION and editor:ValueFromPosition(pos) or nil
      instances = value and indicateFindInstances(editor, value, pos+1)

      local occurrences = (not instances or #instances == 0) and ""
        or ("  (%d)"):format(#instances+(instances[0] and 1 or 0))
      local line = instances and instances[0] and editor:LineFromPosition(instances[0]-1)+1
      local def =  line and " ("..TR("on line %d"):format(line)..")" or ""
      local selections = ide.wxver >= "2.9.5" and editor:GetSelections() or 1

      local menu = wx.wxMenu {
        { ID_UNDO, TR("&Undo") },
        { ID_REDO, TR("&Redo") },
        { },
        { ID_CUT, TR("Cu&t") },
        { ID_COPY, TR("&Copy") },
        { ID_PASTE, TR("&Paste") },
        { ID_SELECTALL, TR("Select &All") },
        { },
        { ID_GOTODEFINITION, TR("Go To Definition")..def },
        { ID_RENAMEALLINSTANCES, TR("Rename All Instances")..occurrences },
        { ID_REPLACEALLSELECTIONS, TR("Replace All Selections") },
        { },
        { ID_QUICKADDWATCH, TR("Add Watch Expression") },
        { ID_QUICKEVAL, TR("Evaluate In Console") },
        { ID_ADDTOSCRATCHPAD, TR("Add To Scratchpad") },
      }

      menu:Enable(ID_GOTODEFINITION, instances and instances[0])
      menu:Enable(ID_RENAMEALLINSTANCES, instances and (instances[0] or #instances > 0)
        or editor:GetSelectionStart() ~= editor:GetSelectionEnd())
      menu:Enable(ID_REPLACEALLSELECTIONS, selections > 1)
      menu:Enable(ID_QUICKADDWATCH, value ~= nil)
      menu:Enable(ID_QUICKEVAL, value ~= nil)

      local debugger = ide.debugger
      menu:Enable(ID_ADDTOSCRATCHPAD, debugger.scratchpad
        and debugger.scratchpad.editors and not debugger.scratchpad.editors[editor])

      -- disable calltips that could open over the menu
      local dwelltime = editor:GetMouseDwellTime()
      editor:SetMouseDwellTime(0) -- disable dwelling

      -- cancel calltip if it's already shown as it interferes with popup menu
      if editor:CallTipActive() then editor:CallTipCancel() end

      PackageEventHandle("onMenuEditor", menu, editor, event)

      editor:PopupMenu(menu)
      editor:SetMouseDwellTime(dwelltime) -- restore dwelling
    end)

  editor:Connect(ID_GOTODEFINITION, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event)
      if value and instances[0] then
        navigateToPosition(editor, editor:GetCurrentPos(), instances[0]-1, #value)
      end
    end)

  editor:Connect(ID_RENAMEALLINSTANCES, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event)
      if value and pos then
        if not (instances and (instances[0] or #instances > 0)) then
          -- if multiple instances (of a variable) are not detected,
          -- then simply find all instances of (selected) `value`
          instances = {}
          local length, pos = editor:GetLength(), 0
          while true do
            editor:SetTargetStart(pos)
            editor:SetTargetEnd(length)
            pos = editor:SearchInTarget(value)
            if pos == -1 then break end
            table.insert(instances, pos+1)
            pos = pos + #value
          end
        end
        selectAllInstances(instances, value, pos)
      end
    end)

  editor:Connect(ID_REPLACEALLSELECTIONS, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event)
      local main = editor:GetMainSelection()
      local text = wx.wxGetTextFromUser(
        TR("Enter replacement text"),
        TR("Replace All Selections"),
        editor:GetTextRange(editor:GetSelectionNStart(main), editor:GetSelectionNEnd(main))
      )
      if not text or text == "" then return end

      editor:BeginUndoAction()
      for s = 0, editor:GetSelections()-1 do
        local selst, selend = editor:GetSelectionNStart(s), editor:GetSelectionNEnd(s)
        editor:SetTargetStart(selst)
        editor:SetTargetEnd(selend)
        editor:ReplaceTarget(text)
        editor:SetSelectionNStart(s, selst)
        editor:SetSelectionNEnd(s, selst+#text)
      end
      editor:EndUndoAction()
      editor:SetMainSelection(main)
    end)

  editor:Connect(ID_QUICKADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) ide:AddWatch(value) end)

  editor:Connect(ID_QUICKEVAL, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) ShellExecuteCode(value) end)

  editor:Connect(ID_ADDTOSCRATCHPAD, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) DebuggerScratchpadOn(editor) end)

  return editor
end

-- ----------------------------------------------------------------------------
-- Add an editor to the notebook
function AddEditor(editor, name)
  assert(notebook:GetPageIndex(editor) == -1, "Editor being added is not in the notebook: failed")

  -- set the document properties
  local id = editor:GetId()
  local document = setmetatable({}, ide.proto.Document)
  document.editor = editor
  document.fileName = name
  document.filePath = nil
  document.modTime = nil
  document.isModified = false
  openDocuments[id] = document

  -- add page only after document is created as there may be handlers
  -- that expect the document (for example, onEditorFocusSet)
  if not notebook:AddPage(editor, name, true) then
    openDocuments[id] = nil
    return
  else
    document.index = notebook:GetPageIndex(editor)
    return document
  end
end

function GetSpec(ext,forcespec)
  local spec = forcespec

  -- search proper spec
  -- allow forcespec for "override"
  if ext and not spec then
    for _,curspec in pairs(ide.specs) do
      local exts = curspec.exts
      if (exts) then
        for _,curext in ipairs(exts) do
          if (curext == ext) then
            spec = curspec
            break
          end
        end
        if (spec) then
          break
        end
      end
    end
  end
  return spec
end

function SetupKeywords(editor, ext, forcespec, styles, font, fontitalic)
  local lexerstyleconvert = nil
  local spec = forcespec or GetSpec(ext)
  -- found a spec setup lexers and keywords
  if spec then
    editor:SetLexer(spec.lexer or wxstc.wxSTC_LEX_NULL)
    lexerstyleconvert = spec.lexerstyleconvert

    if (spec.keywords) then
      for i,words in ipairs(spec.keywords) do
        editor:SetKeyWords(i-1,words)
      end
    end

    editor.api = GetApi(spec.apitype or "none")
    editor.spec = spec
  else
    editor:SetLexer(wxstc.wxSTC_LEX_NULL)
    editor:SetKeyWords(0, "")

    editor.api = GetApi("none")
    editor.spec = ide.specs.none
  end

  -- need to set folding property after lexer is set, otherwise
  -- the folds are not shown (wxwidgets 2.9.5)
  if edcfg.fold then
    editor:SetProperty("fold", "1")
    editor:SetProperty("fold.html", "1")
    editor:SetProperty("fold.compact", edcfg.foldcompact and "1" or "0")
    editor:SetProperty("fold.comment", "1")
  end
  
  -- quickfix to prevent weird looks, otherwise need to update styling mechanism for cpp
  -- cpp "greyed out" styles are  styleid + 64
  editor:SetProperty("lexer.cpp.track.preprocessor", "0")
  editor:SetProperty("lexer.cpp.update.preprocessor", "0")

  -- create italic font if only main font is provided
  if font and not fontitalic then
    fontitalic = wx.wxFont(font)
    fontitalic:SetStyle(wx.wxFONTSTYLE_ITALIC)
  end

  StylesApplyToEditor(styles or ide.config.styles, editor,
    font or ide.font.eNormal,fontitalic or ide.font.eItalic,lexerstyleconvert)
end
