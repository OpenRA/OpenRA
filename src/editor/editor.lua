-- Copyright 2011-13 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local wxkeywords = nil -- a string of the keywords for scintilla of wxLua's wx.XXX items

local editorID = 100 -- window id to create editor pages with, incremented for new editors

local openDocuments = ide.openDocuments
local statusBar = ide.frame.statusBar
local notebook = ide.frame.notebook
local funclist = ide.frame.toolBar.funclist
local edcfg = ide.config.editor
local styles = ide.config.styles

local DEFAULT_STYLE = 32
local margin = { LINENUMBER = 0, MARKER = 1, FOLD = 2 }

-- ----------------------------------------------------------------------------
-- Update the statusbar text of the frame using the given editor.
-- Only update if the text has changed.
local statusTextTable = { "OVR?", "R/O?", "Cursor Pos" }

funclist:SetFont(ide.font.dNormal)

local function updateStatusText(editor)
  local texts = { "", "", "" }
  if ide.frame and editor then
    local pos = editor:GetCurrentPos()
    local line = editor:LineFromPosition(pos)
    local col = 1 + pos - editor:PositionFromLine(line)

    texts = {
      iff(editor:GetOvertype(), TR("OVR"), TR("INS")),
      iff(editor:GetReadOnly(), TR("R/O"), TR("R/W")),
      TR("Ln: %d"):format(line + 1).." "..TR("Col: %d"):format(col) }
  end

  if ide.frame then
    for n = 1, 3 do
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
    if MarkupIsSpecial and MarkupIsSpecial(style)
      or editor.spec.iscomment[style] then return end

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

local function getFileTitle (editor)
  if not editor or not openDocuments[editor:GetId()] then return GetIDEString("editor") end
  local id = editor:GetId()
  local filePath = openDocuments[id].filePath
  local fileName = openDocuments[id].fileName
  if not filePath or not fileName then return GetIDEString("editor") end
  return GetIDEString("editor").." ["..filePath.."]"
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

        if ret ~= wx.wxYES or LoadFile(filePath, editor, true) then
          openDocuments[id].modTime = GetFileModTime(filePath)
        end
      end
    end
  end
end

local function navigateToPosition(editor, fromPosition, toPosition, length)
  table.insert(editor.jumpstack, fromPosition)
  editor:GotoPos(toPosition)
  if length then
    editor:SetAnchor(toPosition + length)
  end
end

local function navigateBack(editor)
  if #editor.jumpstack == 0 then return end
  local pos = table.remove(editor.jumpstack)
  editor:GotoPos(pos)
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
  ide.frame:SetTitle(getFileTitle(editor))

  if editor then
    if funclist:IsEmpty() then funclist:Append(TR("Jump to a function definition..."), 0) end
    funclist:SetSelection(0)

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

-- Set if the document is modified and update the notebook page text
function SetDocumentModified(id, modified, text)
  local modpref, doc = '* ', openDocuments[id]
  if not doc then return end
  local pageText = text or notebook:GetPageText(doc.index):gsub("^"..EscapeMagic(modpref), "")

  if modified then pageText = modpref..pageText end
  openDocuments[id].isModified = modified
  notebook:SetPageText(doc.index, pageText)
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
  -- match from starting brace
  lt = lt:match("[^%[%(%{%s,]*$")

  -- know now which string is to be completed
  local userList = CreateAutoCompList(editor,lt)
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

function EditorCallTip(editor, pos, x, y)
  -- don't show anything if the calltip/auto-complete is active;
  -- this may happen after typing function name, while the mouse is over
  -- a different function or when auto-complete is on for a parameter.
  if editor:CallTipActive() or editor:AutoCompActive() then return end

  -- don't activate if the window itself is not active (in the background)
  if not ide.frame:IsActive() then return end

  local var, funccall = getValAtPosition(editor, pos)
  -- if this is a value type rather than a function/method call, then use
  -- full match to avoid calltip about coroutine.status for "status" vars
  local tip = GetTipInfo(editor, funccall or var, false, not funccall)
  if ide.debugger and ide.debugger.server then
    if var then
      local limit = 128
      ide.debugger.quickeval(var, function(val)
        if #val > limit then val = val:sub(1, limit-3).."..." end
        -- check if the mouse position is specified and the mouse has moved,
        -- then don't show the tooltip as it's already too late for it.
        if x and y then
          local mpos = wx.wxGetMousePosition()
          if mpos.x ~= x or mpos.y ~= y then return end
        end
        editor:CallTipShow(pos, val) end)
    end
  elseif tip then
    -- only shorten if shown on mouse-over. Use shortcut to get full info.
    local shortento = 450
    local showtooltip = ide.frame.menuBar:FindItem(ID_SHOWTOOLTIP)
    local suffix = "...\n"
        ..TR("Use '%s' to see full description."):format(showtooltip:GetLabel())
    if x and y and #tip > shortento then
      tip = tip:sub(1, shortento-#suffix):gsub("%W*%w*$","")..suffix
    end
    editor:CallTipShow(pos, tip)
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
local function indicateFunctions28(editor, lines, linee)
  if not (edcfg.showfncall and editor.spec and editor.spec.isfncall)
  or not (styles.indicator and styles.indicator.fncall) then return end

  local es = editor:GetEndStyled()
  local lines = lines or 0
  local linee = linee or editor:GetLineCount()-1

  if (lines < 0) then return end

  local isfncall = editor.spec.isfncall
  local isinvalid = {}
  for i,v in pairs(editor.spec.iscomment) do isinvalid[i] = v end
  for i,v in pairs(editor.spec.iskeyword0) do isinvalid[i] = v end
  for i,v in pairs(editor.spec.isstring) do isinvalid[i] = v end

  local INDICS_MASK = wxstc.wxSTC_INDICS_MASK
  local INDIC0_MASK = wxstc.wxSTC_INDIC0_MASK

  for line=lines,linee do
    local tx = editor:GetLine(line)
    local ls = editor:PositionFromLine(line)

    local from = 1
    local off = -1

    editor:StartStyling(ls,INDICS_MASK)
    editor:SetStyling(#tx,0)
    while from do
      tx = from==1 and tx or string.sub(tx,from)

      local f,t,w = isfncall(tx)

      if (f) then
        local p = ls+f+off
        local s = bit.band(editor:GetStyleAt(p),31)
        editor:StartStyling(p,INDICS_MASK)
        editor:SetStyling(#w,isinvalid[s] and 0 or (INDIC0_MASK + 1))
        off = off + t
      end
      from = t and (t+1)
    end
  end
  editor:StartStyling(es,31)
end

local delayed = {}
local tokenlists = {}

-- indicator.MASKED is handled separately, so don't include in MAX
local indicator = {FNCALL = 0, LOCAL = 1, GLOBAL = 2, MASKING = 3, MASKED = 4, MAX = 3}

function IndicateIfNeeded()
  local editor = GetEditor()
  -- do the current one first
  if delayed[editor] then return IndicateAll(editor) end
  for editor in pairs(delayed) do return IndicateAll(editor) end
end

-- find all instances of a symbol at pos
-- return table with [0] as the definition position (if local)
local function indicateFindInstances(editor, name, pos)
  local tokens = tokenlists[editor] or {}
  local instances = {{[-1] = 1}}
  local this
  for _, token in ipairs(tokens) do
    local op = token[1]

    if op == 'EndScope' then -- EndScope has "new" level, so need +1
      if this and token.fpos > pos and this == token.at+1 then break end

      if #instances > 1 and instances[#instances][-1] == token.at+1 then
        table.remove(instances) end
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

function IndicateAll(editor, lines, linee)
  local d = delayed[editor]
  delayed[editor] = nil -- assume this can be finished for now

  -- this function can be called for an editor tab that is already closed
  -- when there are still some pending events for it, so handle it.
  if not pcall(function() return editor:GetId() end) then return end

  if not (editor.spec and editor.spec.markvars) then return end
  local indic = styles.indicator or {}

  local pos, vars = d and d[1] or 1, d and d[2] or nil
  local start = lines and editor:PositionFromLine(lines)+1 or nil
  if d and start and pos >= start then
    -- ignore delayed processing as the change is earlier in the text
    pos, vars = 1, nil
  end

  tokenlists[editor] = tokenlists[editor] or {}
  local tokens = tokenlists[editor]

  if start then -- if the range is specified
    local curindic = editor:GetIndicatorCurrent()
    editor:SetIndicatorCurrent(indicator.MASKED)
    for n = #tokens, 1, -1 do
      local token = tokens[n]
      -- find the last token before the range
      if token[1] == 'EndScope' and token.name and token.fpos+#token.name < start then
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
        while type(var) == 'table' and var.fpos and (var.fpos > pos) do
          var = var.masked -- restored a masked var
          vars[name] = var
        end
      end
      vars = getmetatable(vars) and getmetatable(vars).__index
    end
  else
    if pos == 1 then -- if not continuing, then trim the list
      tokens = {}
      tokenlists[editor] = tokens
    end
  end

  local cleared = {}
  for indic = 0, indicator.MAX do cleared[indic] = pos end

  local function IndicateOne(indic, pos, length)
    editor:SetIndicatorCurrent(indic)
    editor:IndicatorClearRange(cleared[indic]-1, pos-cleared[indic])
    editor:IndicatorFillRange(pos-1, length)
    cleared[indic] = pos+length
  end

  local s = TimeGet()
  local canwork = start and 0.010 or 0.100 -- use shorter interval when typing
  local f = editor.spec.markvars(editor:GetText(), pos, vars)

  while true do
    local op, name, lineinfo, vars, at = f()
    if not op then break end
    local var = vars and vars[name]
    local token = {op, name=name, fpos=lineinfo, at=at, context=vars,
      self = (op == 'VarSelf') or nil }
    if op == 'FunctionCall' then
      if indic.fncall and edcfg.showfncall then
        IndicateOne(indicator.FNCALL, lineinfo, #name)
      end
    elseif op ~= 'VarNext' and op ~= 'VarInside' and op ~= 'Statement' then
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
        table.insert(tokens, {"Masked", name=name, fpos=fpos})
      end

      if indic.varmasking then IndicateOne(indicator.MASKING, lineinfo, #name) end
    end
    if op == 'EndScope' and name and TimeGet()-s > canwork then
      delayed[editor] = {lineinfo+#name, vars}
      break
    end
  end

  -- clear indicators till the end of processed fragment
  local pos = delayed[editor] and delayed[editor][1] or editor:GetLength()+1

  -- don't clear "masked" indicators as those can be set out of order (so
  -- last updated fragment is not always the last in terms of its position);
  -- these indicators should be up-to-date to the end of the code fragment.
  for indic = 0, indicator.MAX do IndicateOne(indic, pos, 0) end

  return delayed[editor] ~= nil -- request more events if still need to work
end

if ide.wxver < "2.9.5" or not ide.config.autoanalizer then
  IndicateAll = indicateFunctions28 end

-- ----------------------------------------------------------------------------
-- Create an editor
function CreateEditor()
  local editor = wxstc.wxStyledTextCtrl(notebook, editorID,
    wx.wxDefaultPosition, wx.wxSize(0, 0),
    wx.wxBORDER_STATIC)

  editorID = editorID + 1 -- increment so they're always unique

  editor.matchon = false
  editor.assignscache = false
  editor.autocomplete = false
  editor.jumpstack = {}

  editor:SetBufferedDraw(not ide.config.hidpi and true or false)
  editor:StyleClearAll()

  editor:SetFont(ide.font.eNormal)
  editor:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font.eNormal)

  editor:SetTabWidth(ide.config.editor.tabwidth or 2)
  editor:SetIndent(ide.config.editor.tabwidth or 2)
  editor:SetUseTabs(ide.config.editor.usetabs and true or false)
  editor:SetIndentationGuides(true)
  editor:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)

  if (ide.config.editor.usewrap) then
    editor:SetWrapMode(wxstc.wxSTC_WRAP_WORD)
    editor:SetWrapStartIndent(0)
    editor:SetWrapVisualFlagsLocation(wxstc.wxSTC_WRAPVISUALFLAGLOC_END_BY_TEXT)
  end

  if (ide.config.editor.defaulteol == wxstc.wxSTC_EOL_CRLF
     or ide.config.editor.defaulteol == wxstc.wxSTC_EOL_LF) then
    editor:SetEOLMode(ide.config.editor.defaulteol)
  -- else: keep wxStyledTextCtrl default behavior (CRLF on Windows, LF on Unix)
  end

  editor:SetCaretLineVisible(ide.config.editor.caretline and 1 or 0)

  editor:SetVisiblePolicy(wxstc.wxSTC_VISIBLE_STRICT, 3)

  editor:SetMarginWidth(margin.LINENUMBER, editor:TextWidth(DEFAULT_STYLE, "99999_"))

  editor:SetMarginWidth(margin.MARKER, 16)
  editor:SetMarginType(margin.MARKER, wxstc.wxSTC_MARGIN_SYMBOL)
  editor:SetMarginSensitive(margin.MARKER, true)

  editor:MarkerDefine(StylesGetMarker("currentline"))
  editor:MarkerDefine(StylesGetMarker("breakpoint"))

  if ide.config.editor.fold then
    editor:SetMarginWidth(margin.FOLD, 16)
    editor:SetMarginType(margin.FOLD, wxstc.wxSTC_MARGIN_SYMBOL)
    editor:SetMarginMask(margin.FOLD, wxstc.wxSTC_MASK_FOLDERS)
    editor:SetMarginSensitive(margin.FOLD, true)
  end

  editor:SetFoldFlags(wxstc.wxSTC_FOLDFLAG_LINEBEFORE_CONTRACTED +
    wxstc.wxSTC_FOLDFLAG_LINEAFTER_CONTRACTED)

  -- allow multiple selection and multi-cursor editing if supported
  if ide.wxver >= "2.9.5" then
    editor:SetMultipleSelection(1)
    editor:SetAdditionalCaretsBlink(1)
    editor:SetAdditionalSelectionTyping(1)
  end

  do
    local fg, bg = wx.wxWHITE, wx.wxColour(128, 128, 128)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPEN, wxstc.wxSTC_MARK_BOXMINUS, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDER, wxstc.wxSTC_MARK_BOXPLUS, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERSUB, wxstc.wxSTC_MARK_VLINE, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERTAIL, wxstc.wxSTC_MARK_LCORNER, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEREND, wxstc.wxSTC_MARK_BOXPLUSCONNECTED, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPENMID, wxstc.wxSTC_MARK_BOXMINUSCONNECTED, fg, bg)
    editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL, wxstc.wxSTC_MARK_TCORNER, fg, bg)
    bg:delete()
  end

  if ide.config.editor.calltipdelay and ide.config.editor.calltipdelay > 0 then
    editor:SetMouseDwellTime(ide.config.editor.calltipdelay)
  end

  editor:AutoCompSetIgnoreCase(ide.config.acandtip.ignorecase)
  if (ide.config.acandtip.strategy > 0) then
    editor:AutoCompSetAutoHide(0)
    editor:AutoCompStops([[ \n\t=-+():.,;*/!"'$%&~'#°^@?´`<>][|}{]])
  end

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
          self:GotoPos(pos)
        end
      elseif not badtime and redolater then
        -- reset the left margin first to make sure that the position
        -- is set "from the left" to get the best content displayed.
        self:SetXOffset(0)
        self:GotoPos(redolater)
        redolater = nil
      end
    end
  end

  editor.ev = {}
  editor:Connect(wxstc.wxEVT_STC_MARGINCLICK,
    function (event)
      local line = editor:LineFromPosition(event:GetPosition())
      local margin = event:GetMargin()
      if margin == 1 then
        DebuggerToggleBreakpoint(editor, line)
      elseif margin == 2 then
        if wx.wxGetKeyState(wx.WXK_SHIFT) and wx.wxGetKeyState(wx.WXK_CONTROL) then
          FoldSome()
        else
          local level = editor:GetFoldLevel(line)
          if HasBit(level, wxstc.wxSTC_FOLDLEVELHEADERFLAG) then
            editor:ToggleFold(line)
          end
        end
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_MODIFIED,
    function (event)
      if (editor.assignscache and editor:GetCurrentLine() ~= editor.assignscache.line) then
        editor.assignscache = false
      end
      local evtype = event:GetModificationType()
      if (bit.band(evtype,wxstc.wxSTC_MOD_INSERTTEXT) ~= 0) then
        SetAutoRecoveryMark()
        table.insert(editor.ev,{event:GetPosition(),event:GetLinesAdded()})
        DynamicWordsAdd("post",editor,nil,editor:LineFromPosition(event:GetPosition()),event:GetLinesAdded())
      end
      if (bit.band(evtype,wxstc.wxSTC_MOD_DELETETEXT) ~= 0) then
        SetAutoRecoveryMark()
        table.insert(editor.ev,{event:GetPosition(),0})
        DynamicWordsAdd("post",editor,nil,editor:LineFromPosition(event:GetPosition()),0)
      end
      
      if ide.config.acandtip.nodynwords then return end
      -- only required to track changes
      if (bit.band(evtype,wxstc.wxSTC_MOD_BEFOREDELETE) ~= 0) then
        local _, numlines = event:GetText():gsub("\r?\n","%1")
        DynamicWordsRem("pre",editor,nil,editor:LineFromPosition(event:GetPosition()), numlines)
      end
      if (bit.band(evtype,wxstc.wxSTC_MOD_BEFOREINSERT) ~= 0) then
        DynamicWordsRem("pre",editor,nil,editor:LineFromPosition(event:GetPosition()), 0)
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_CHARADDED,
    function (event)
      -- auto-indent
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
        if (line > 0) then
          local indent = editor:GetLineIndentation(line - 1)
          local linedone = editor:GetLine(line - 1)

          -- if the indentation is 0 and the current line is not empty
          -- then take indentation from the current line (instead of the
          -- previous one). This may happen when CR is hit at the beginning
          -- of a line (rather than at the end).
          if indent == 0 and not linetx:match("^[\010\013]*$") then
            indent = editor:GetLineIndentation(line)
          end

          local ut = editor:GetUseTabs()
          local tw = ut and editor:GetTabWidth() or editor:GetIndent()

          if ide.config.editor.smartindent
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
          editor:CallTipShow(pos,tip)
        end

      elseif ide.config.autocomplete then -- code completion prompt
        local trigger = linetxtopos:match("["..editor.spec.sep.."%w_]+$")
        if (trigger and (#trigger > 1 or trigger:match("["..editor.spec.sep.."]"))) then
          editor.autocomplete = true
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
          local start_pos = editor:WordStartPosition(pos, true)
          editor:SetSelection(start_pos, pos)
          editor:ReplaceSelection(text)
          -- if this is the main position, save new cursor position to restore
          if pos == mainpos then mainpos = editor:GetCurrentPos()
          elseif pos < mainpos then
            -- adjust main position as earlier changes may affect it
            mainpos = mainpos + #text - (pos - start_pos)
          end
        end
        editor:EndUndoAction()

        editor:GotoPos(mainpos)
      else
        local pos = editor:GetCurrentPos()
        local start_pos = editor:WordStartPosition(pos, true)
        editor:SetSelection(start_pos, pos)
        editor:ReplaceSelection(event:GetText())
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTREACHED,
    function ()
      SetDocumentModified(editor:GetId(), false)
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTLEFT,
    function ()
      SetDocumentModified(editor:GetId(), true)
    end)

  -- "updateStatusText" should be called in UPDATEUI event, but it creates
  -- several performance problems on Windows (using wx2.9.5+) when
  -- brackets or backspace is used (very slow screen repaint with 0.5s delay).
  -- Moving it to PAINTED event creates problems on OSX (using wx2.9.5+),
  -- where refresh of R/W and R/O status in the status bar is delayed.

  editor:Connect(wxstc.wxEVT_STC_PAINTED,
    function ()
      if ide.osname == 'Windows' then updateStatusText(editor) end
    end)

  editor:Connect(wxstc.wxEVT_STC_UPDATEUI,
    function ()
      if ide.osname ~= 'Windows' then updateStatusText(editor) end

      editor:GotoPosDelayed()
      updateBraceMatch(editor)
      local minupdated
      for _,iv in ipairs(editor.ev) do
        local line = editor:LineFromPosition(iv[1])
        if not minupdated or line < minupdated then minupdated = line end
        local ok, res = pcall(IndicateAll, editor,line,line+iv[2])
        if not ok then DisplayOutputLn("Internal error: ",res,line,line+iv[2]) end
      end
      local firstvisible = editor:DocLineFromVisible(editor:GetFirstVisibleLine())
      local lastline = math.min(editor:GetLineCount(),
        firstvisible + editor:LinesOnScreen())
      -- lastline - editor:LinesOnScreen() can get negative; fix it
      local firstline = math.min(math.max(0, lastline - editor:LinesOnScreen()),
        firstvisible)
      MarkupStyle(editor,minupdated or firstline,lastline)
      editor.ev = {}

      -- show auto-complete if needed
      if editor.autocomplete then
        EditorAutoComplete(editor)
        editor.autocomplete = false
      end
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
        local value = pos ~= wxstc.wxSTC_INVALID_POSITION and getValAtPosition(editor, pos) or nil
        local instances = value and indicateFindInstances(editor, value, pos+1)
        if instances and instances[0] then
          navigateToPosition(editor, pos, instances[0]-1, #value)
          return
        end
      end
      event:Skip()
    end)

  if ide.config.editor.nomousezoom then
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
      elseif keycode == wx.WXK_ESCAPE and ide.frame:IsFullScreen() then
        ShowFullScreen(false)
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
          editor:SetTargetStart(editor:GetSelectionStart())
          editor:SetTargetEnd(editor:GetSelectionEnd())
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
            event:Skip()
            return
          end

          editor:SetTargetStart(pos)
          editor:SetTargetEnd(pos+1)
        end
        editor:ReplaceTarget("")
      elseif ide.osname == "Unix" and ide.wxver >= "2.9.5"
      and keycode == ('T'):byte() and mod == wx.wxMOD_CONTROL then
        ide.frame:AddPendingEvent(wx.wxCommandEvent(
          wx.wxEVT_COMMAND_MENU_SELECTED, ID_SHOWTOOLTIP))
      elseif mod == wx.wxMOD_ALT and keycode == wx.WXK_LEFT then
        navigateBack(editor)
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
    for i, pos in pairs(instances) do
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
        local value = pos ~= wxstc.wxSTC_INVALID_POSITION and getValAtPosition(editor, pos) or nil
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
      editor:SetMarginWidth(margin.LINENUMBER, editor:TextWidth(DEFAULT_STYLE, "99999_"))
      event:Skip()
    end)

  local pos, value, instances
  editor:Connect(wx.wxEVT_CONTEXT_MENU,
    function (event)
      local point = editor:ScreenToClient(event:GetPosition())
      pos = editor:PositionFromPointClose(point.x, point.y)
      value = pos ~= wxstc.wxSTC_INVALID_POSITION and getValAtPosition(editor, pos) or nil
      instances = value and indicateFindInstances(editor, value, pos+1)

      local occurrences = (not instances or #instances == 0) and ""
        or ("  (%d)"):format(#instances+(instances[0] and 1 or 0))
      local line = instances and instances[0] and editor:LineFromPosition(instances[0]-1)+1
      local def =  line and " ("..TR("on line %d"):format(line)..")" or ""

      local menu = wx.wxMenu()
      menu:Append(ID_UNDO, TR("&Undo"))
      menu:Append(ID_REDO, TR("&Redo"))
      menu:AppendSeparator()
      menu:Append(ID_CUT, TR("Cu&t"))
      menu:Append(ID_COPY, TR("&Copy"))
      menu:Append(ID_PASTE, TR("&Paste"))
      menu:Append(ID_SELECTALL, TR("Select &All"))
      menu:AppendSeparator()
      menu:Append(ID_GOTODEFINITION, TR("Go To Definition")..def)
      menu:Append(ID_RENAMEALLINSTANCES, TR("Rename All Instances")..occurrences)
      menu:AppendSeparator()
      menu:Append(ID_QUICKADDWATCH, TR("Add Watch Expression"))
      menu:Append(ID_QUICKEVAL, TR("Evaluate In Console"))
      menu:Append(ID_ADDTOSCRATCHPAD, TR("Add To Scratchpad"))

      menu:Enable(ID_GOTODEFINITION, instances and instances[0])
      menu:Enable(ID_RENAMEALLINSTANCES, instances and (instances[0] or #instances > 0))
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
        selectAllInstances(instances, value, pos)
      end
    end)

  editor:Connect(ID_QUICKADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) DebuggerAddWatch(value) end)

  editor:Connect(ID_QUICKEVAL, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) ShellExecuteCode(value) end)

  editor:Connect(ID_ADDTOSCRATCHPAD, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) DebuggerScratchpadOn(editor) end)

  return editor
end

-- ----------------------------------------------------------------------------
-- Add an editor to the notebook
function AddEditor(editor, name)
  if notebook:AddPage(editor, name, true) then
    local id = editor:GetId()
    local document = setmetatable({}, ide.proto.Document)
    document.editor = editor
    document.index = notebook:GetPageIndex(editor)
    document.fileName = name
    document.filePath = nil
    document.modTime = nil
    document.isModified = false
    openDocuments[id] = document
    return document
  end
  return
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

    if (spec.api == "lua") then
      -- Get the items in the global "wx" table for autocompletion
      if not wxkeywords then
        local keyword_table = {}
        for index in pairs(wx) do
          table.insert(keyword_table, "wx."..index.." ")
        end

        for index in pairs(wxstc) do
          table.insert(keyword_table, "wxstc."..index.." ")
        end

        table.sort(keyword_table)
        wxkeywords = table.concat(keyword_table)
      end
      local offset = spec.keywords and #spec.keywords or 5
      editor:SetKeyWords(offset, wxkeywords)
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
  if ide.config.editor.fold then
    editor:SetProperty("fold", "1")
    editor:SetProperty("fold.html", "1")
    editor:SetProperty("fold.compact", ide.config.editor.foldcompact and "1" or "0")
    editor:SetProperty("fold.comment", "1")
  end
  
  -- quickfix to prevent weird looks, otherwise need to update styling mechanism for cpp
  -- cpp "greyed out" styles are  styleid + 64
  editor:SetProperty("lexer.cpp.track.preprocessor", "0")
  editor:SetProperty("lexer.cpp.update.preprocessor", "0")

  StylesApplyToEditor(styles or ide.config.styles, editor,
    font or ide.font.eNormal,fontitalic or ide.font.eItalic,lexerstyleconvert)
end

----------------------------------------------------
-- function list for current file

local function refreshFunctionList(event)
  event:Skip()

  local editor = GetEditor()
  if (editor and not (editor.spec and editor.spec.isfndef)) then return end

  -- parse current file and update list
  -- first populate with the current label to minimize flicker
  -- then populate the list and update the label
  local current = funclist:GetCurrentSelection()
  local label = funclist:GetString(current)
  local default = funclist:GetString(0)
  funclist:Clear()
  funclist:Append(current ~= wx.wxNOT_FOUND and label or default, 0)
  funclist:SetSelection(0)

  local lines = 0
  local linee = (editor and editor:GetLineCount() or 0)-1
  for line=lines,linee do
    local tx = editor:GetLine(line)
    local s,_,cap,l = editor.spec.isfndef(tx)
    if (s) then
      local ls = editor:PositionFromLine(line)
      local style = bit.band(editor:GetStyleAt(ls+s),31)
      if not (editor.spec.iscomment[style] or editor.spec.isstring[style]) then
        funclist:Append((l and "  " or "")..cap,line)
      end
    end
  end

  funclist:SetString(0, default)
  funclist:SetSelection(current ~= wx.wxNOT_FOUND and current or 0)
end

-- wx.wxEVT_SET_FOCUS is not triggered for wxChoice on Mac (wx 2.8.12),
-- so use wx.wxEVT_LEFT_DOWN instead; none of the events are triggered for
-- wxChoice on Linux (wx 2.9.5+), so use EVT_ENTER_WINDOW attached to the
-- toolbar itself until something better is available.
if ide.osname == 'Unix' then
  ide.frame.toolBar:Connect(wx.wxEVT_ENTER_WINDOW, refreshFunctionList)
else
  local event = ide.osname == 'Macintosh' and wx.wxEVT_LEFT_DOWN or wx.wxEVT_SET_FOCUS
  funclist:Connect(event, refreshFunctionList)
end

funclist:Connect(wx.wxEVT_COMMAND_CHOICE_SELECTED,
  function (event)
    -- test if updated
    -- jump to line
    event:Skip()
    local l = event:GetClientData()
    if (l and l > 0) then
      local editor = GetEditor()
      editor:GotoLine(l)
      editor:SetFocus()
      editor:SetSTCFocus(true)
    end
  end)
