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
local projcombobox = ide.frame.projpanel.projcombobox

-- ----------------------------------------------------------------------------
-- Update the statusbar text of the frame using the given editor.
-- Only update if the text has changed.
local statusTextTable = { "OVR?", "R/O?", "Cursor Pos" }

-- set funclist font to be the same as the combobox in the project dropdown
funclist:SetFont(ide.font.fNormal)

local function updateStatusText(editor)
  local texts = { "", "", "" }
  if ide.frame and editor then
    local pos = editor:GetCurrentPos()
    local line = editor:LineFromPosition(pos)
    local col = 1 + pos - editor:PositionFromLine(line)

    texts = {
      iff(editor:GetOvertype(), "OVR", "INS"),
      iff(editor:GetReadOnly(), "R/O", "R/W"),
      "Ln: "..tostring(line + 1).." Col: "..tostring(col) }
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
        wx.wxMessageBox(fileName.." is no longer on the disk.",
          GetIDEString("editormessage"),
          wx.wxOK + wx.wxCENTRE, ide.frame)
      elseif not editor:GetReadOnly() and modTime:IsValid() and oldModTime:IsEarlierThan(modTime) then
        local ret = wx.wxMessageBox(fileName.." has been modified on disk.\nDo you want to reload it?",
          GetIDEString("editormessage"),
          wx.wxYES_NO + wx.wxCENTRE, ide.frame)

        if ret ~= wx.wxYES or LoadFile(filePath, editor, true) then
          openDocuments[id].modTime = GetFileModTime(filePath)
        end
      end
    end
  end
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
    if funclist:IsEmpty() then funclist:Append('Jump to a function definition...', 0) end
    funclist:SetSelection(0)

    editor:SetFocus()
    editor:SetSTCFocus(true)
    local id = editor:GetId()
    if openDocuments[id] and openDocuments[id].filePath then
      FileTreeMarkSelected(openDocuments[id].filePath)
    end
  else
    FileTreeMarkSelected('')
  end
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
function SetDocumentModified(id, modified)
  local pageText = openDocuments[id].fileName or ide.config.default.fullname

  if modified then
    pageText = "* "..pageText
  end

  openDocuments[id].isModified = modified
  notebook:SetPageText(openDocuments[id].index, pageText)
end

function EditorAutoComplete(editor)
  if (editor == nil or not editor.spec) then return end

  -- retrieve the current line and get a string to the current cursor position in the line
  local pos = editor:GetCurrentPos()
  local line = editor:GetCurrentLine()
  local linetx = editor:GetLine(line)
  local linestart = editor:PositionFromLine(line)
  local localpos = pos-linestart

  local lt = linetx:sub(1,localpos)
  lt = lt:gsub("%s*("..editor.spec.sep..")%s*",function(a) return a end)
  lt = lt:gsub("%s*%b[]%s*","")
  lt = lt:gsub("%s*%b()%s*","")
  lt = lt:gsub("%s*%b{}%s*","")
  lt = lt:match("[^%[%(%s]*$")
  lt = lt:gsub("%s","")

  -- know now which string is to be completed
  local userList = CreateAutoCompList(editor,lt)
  if userList and string.len(userList) > 0 then
    editor:UserListShow(1, userList)
  elseif editor:AutoCompActive() then
    editor:AutoCompCancel()
  end
end

local function getValAtPosition(editor, pos)
  local line = editor:LineFromPosition(pos)
  local linetx = editor:GetLine(line)
  local linestart = editor:PositionFromLine(line)
  local localpos = pos-linestart

  local ident = "([a-zA-Z_][a-zA-Z_0-9%.%:]*)"
  local linetxtopos = linetx:sub(1,localpos)
  linetxtopos = linetxtopos..")"
  linetxtopos = linetxtopos:match(ident .. "%b()$")

  local selected = editor:GetSelectionStart() ~= editor:GetSelectionEnd()
    and pos >= editor:GetSelectionStart() and pos <= editor:GetSelectionEnd()

  -- check if we have a selected text or an identifier
  -- for an identifier, check fragments on the left and on the right.
  -- this is to match 'io' in 'i^o.print' and 'io.print' in 'io.pr^int'.
  -- remove square brackets to make tbl[index].x show proper values.
  local start = linetx:sub(1,localpos)
    :gsub("%b[]", function(s) return ("."):rep(#s) end)
    :find(ident.."$")

  -- check if the style is the right one; this is to ignore
  -- comments, strings, numbers (to avoid '1 = 1'), keywords, and such
  if start and not selected then
    local style = bit.band(editor:GetStyleAt(linestart+start),31)
    if editor.spec.iscomment[style]
    or (MarkupIsAny and MarkupIsAny(style)) -- markup in comments
    or editor.spec.isstring[style]
    or style == wxstc.wxSTC_LUA_NUMBER
    or style == wxstc.wxSTC_LUA_WORD then
      -- don't do anything for strings or comments or numbers
      return nil, linetxtopos
    end
  end

  local right = linetx:sub(localpos+1,#linetx):match("^[a-zA-Z_0-9]*")
  local var = selected and editor:GetSelectedText()
    or (start and linetx:sub(start,localpos):gsub(":",".")..right or nil)

  return var, linetxtopos
end

function EditorCallTip(editor, pos, x, y)
  local var, linetxtopos = getValAtPosition(editor, pos)
  local tip = linetxtopos and GetTipInfo(editor,linetxtopos.."(",false)
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
    editor:CallTipShow(pos, tip)
  end
end

-- ----------------------------------------------------------------------------
-- Create an editor and add it to the notebook
function CreateEditor(name)
  local editor = wxstc.wxStyledTextCtrl(notebook, editorID,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxBORDER_STATIC)

  editorID = editorID + 1 -- increment so they're always unique

  editor.matchon = false
  editor.assignscache = false

  editor:SetBufferedDraw(true)
  editor:StyleClearAll()

  editor:SetFont(ide.font.eNormal)
  editor:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font.eNormal)

  editor:SetTabWidth(ide.config.editor.tabwidth or 4)
  editor:SetIndent(ide.config.editor.tabwidth or 4)
  editor:SetUseTabs(ide.config.editor.usetabs and true or false)
  editor:SetIndentationGuides(true)
  editor:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)

  if (ide.config.editor.usewrap) then
    editor:SetWrapMode(wxstc.wxSTC_WRAP_WORD)
    editor:SetWrapStartIndent(0)
    editor:SetWrapVisualFlagsLocation(wxstc.wxSTC_WRAPVISUALFLAGLOC_END_BY_TEXT)
  end

  editor:SetCaretLineVisible(ide.config.editor.caretline and 1 or 0)

  editor:SetVisiblePolicy(wxstc.wxSTC_VISIBLE_SLOP, 3)
  --editor:SetXCaretPolicy(wxstc.wxSTC_CARET_SLOP, 10)
  --editor:SetYCaretPolicy(wxstc.wxSTC_CARET_SLOP, 3)

  editor:SetMarginWidth(0, editor:TextWidth(32, "99999_")) -- line # margin

  editor:SetMarginWidth(1, 16) -- marker margin
  editor:SetMarginType(1, wxstc.wxSTC_MARGIN_SYMBOL)
  editor:SetMarginSensitive(1, true)

  editor:MarkerDefine(BREAKPOINT_MARKER, wxstc.wxSTC_MARK_ROUNDRECT, wx.wxWHITE, wx.wxRED)
  editor:MarkerDefine(CURRENT_LINE_MARKER, wxstc.wxSTC_MARK_ARROW, wx.wxBLACK, wx.wxGREEN)

  editor:SetMarginWidth(2, 16) -- fold margin
  editor:SetMarginType(2, wxstc.wxSTC_MARGIN_SYMBOL)
  editor:SetMarginMask(2, wxstc.wxSTC_MASK_FOLDERS)
  editor:SetMarginSensitive(2, true)

  editor:SetFoldFlags(wxstc.wxSTC_FOLDFLAG_LINEBEFORE_CONTRACTED +
    wxstc.wxSTC_FOLDFLAG_LINEAFTER_CONTRACTED)

  editor:SetProperty("fold", "1")
  editor:SetProperty("fold.compact", "1")
  editor:SetProperty("fold.comment", "1")

  local grey = wx.wxColour(128, 128, 128)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPEN, wxstc.wxSTC_MARK_BOXMINUS, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDER, wxstc.wxSTC_MARK_BOXPLUS, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERSUB, wxstc.wxSTC_MARK_VLINE, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERTAIL, wxstc.wxSTC_MARK_LCORNER, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEREND, wxstc.wxSTC_MARK_BOXPLUSCONNECTED, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPENMID, wxstc.wxSTC_MARK_BOXMINUSCONNECTED, wx.wxWHITE, grey)
  editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL, wxstc.wxSTC_MARK_TCORNER, wx.wxWHITE, grey)
  grey:delete()

  if ide.config.editor.calltipdelay and ide.config.editor.calltipdelay > 0 then
    editor:SetMouseDwellTime(ide.config.editor.calltipdelay)
  end

  editor:AutoCompSetIgnoreCase(ide.config.acandtip.ignorecase)
  if (ide.config.acandtip.strategy > 0) then
    editor:AutoCompSetAutoHide(0)
    editor:AutoCompStops([[ \n\t=-+():.,;*/!"'$%&~'#°^@?´`<>][|}{]])
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
        table.insert(editor.ev,{event:GetPosition(),event:GetLinesAdded()})
        DynamicWordsAdd("post",editor,nil,editor:LineFromPosition(event:GetPosition()),event:GetLinesAdded())
      end
      if (bit.band(evtype,wxstc.wxSTC_MOD_DELETETEXT) ~= 0) then
        table.insert(editor.ev,{event:GetPosition(),0})
        DynamicWordsAdd("post",editor,nil,editor:LineFromPosition(event:GetPosition()),0)
      end
      
      if ide.config.acandtip.nodynwords then return end
      -- only required to track changes
      if (bit.band(evtype,wxstc.wxSTC_MOD_BEFOREDELETE) ~= 0) then
        local numlines = 0
        event:GetText():gsub("(\r?\n)",function() numlines = numlines + 1 end)
        DynamicWordsRem("pre",editor,nil,editor:LineFromPosition(event:GetPosition()), numlines)
      end
      if (bit.band(evtype,wxstc.wxSTC_MOD_BEFOREINSERT) ~= 0) then
        DynamicWordsRem("pre",editor,nil,editor:LineFromPosition(event:GetPosition()), 0)
      end
    end)

  editor:Connect(wxstc.wxEVT_STC_CHARADDED,
    function (event)
      -- auto-indent
      local ch = event:GetKey()
      local eol = editor:GetEOLMode()
      local pos = editor:GetCurrentPos()
      local line = editor:GetCurrentLine()
      local linetx = editor:GetLine(line)
      local linestart = editor:PositionFromLine(line)
      local localpos = pos-linestart

      local linetxtopos = linetx:sub(1,localpos)

      if (ch == char_CR and eol==2) or (ch == char_LF and eol==0) then
        if (line > 0) then
          local indent = editor:GetLineIndentation(line - 1)
          if indent > 0 then
            editor:SetLineIndentation(line, indent)
            local tw = editor:GetTabWidth()
            local ut = editor:GetUseTabs()
            local indent = ut and (indent / tw) or indent
            editor:GotoPos(pos+indent)
          end
        end

      elseif ch == ("("):byte() then
        local tip = GetTipInfo(editor,linetxtopos,ide.config.acandtip.shorttip)
        if tip then
          editor:CallTipShow(pos,tip)
        end

      elseif ide.config.autocomplete then -- code completion prompt
        local trigger = linetxtopos:match("["..editor.spec.sep.."%w_]+$")
        if (trigger and (#trigger > 1 or trigger:match("[%.:]"))) then
          ide.frame:AddPendingEvent(wx.wxCommandEvent(
            wx.wxEVT_COMMAND_MENU_SELECTED, ID_AUTOCOMPLETE))
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
      local mpos = wx.wxGetMousePosition()
      local cpos = editor:ScreenToClient(mpos)
      local position = editor:PositionFromPointClose(
        linux and cpos.x or event:GetX(), linux and cpos.y or event:GetY())
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

  editor:Connect(wxstc.wxEVT_STC_USERLISTSELECTION,
    function (event)
      local pos = editor:GetCurrentPos()
      local start_pos = editor:WordStartPosition(pos, true)
      editor:SetSelection(start_pos, pos)
      editor:ReplaceSelection(event:GetText())
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTREACHED,
    function ()
      SetDocumentModified(editor:GetId(), false)
    end)

  editor:Connect(wxstc.wxEVT_STC_SAVEPOINTLEFT,
    function ()
      SetDocumentModified(editor:GetId(), true)
    end)

  editor:Connect(wxstc.wxEVT_STC_UPDATEUI,
    function ()
      updateStatusText(editor)
      updateBraceMatch(editor)
      for _,iv in ipairs(editor.ev) do
        local line = editor:LineFromPosition(iv[1])
        IndicateFunctions(editor,line,line+iv[2])
        if MarkupStyle then MarkupStyle(editor,line,line+iv[2]+1) end
      end
      if MarkupStyleRefresh then MarkupStyleRefresh(editor, editor.ev) end
      editor.ev = {}
    end)

  editor:Connect(wx.wxEVT_LEFT_DOWN,
    function (event)
      if MarkupHotspotClick then
        local position = editor:PositionFromPointClose(event:GetX(),event:GetY())
        if position ~= wxstc.wxSTC_INVALID_POSITION then
          if MarkupHotspotClick(position, editor) then return end
        end
      end
      event:Skip()
    end)

  local inhandler = false
  editor:Connect(wx.wxEVT_SET_FOCUS,
    function (event)
      event:Skip()
      if inhandler or ide.exitingProgram then return end
      inhandler = true
      isFileAlteredOnDisk(editor)
      inhandler = false
    end)

  editor:Connect(wx.wxEVT_KEY_DOWN,
    function (event)
      local keycode = event:GetKeyCode()
      local first, last = 0, notebook:GetPageCount()-1
      if keycode == wx.WXK_ESCAPE and frame:IsFullScreen() then
        ShowFullScreen(false)
      elseif event:ControlDown() and
        (keycode == wx.WXK_PAGEUP or keycode == wx.WXK_TAB and event:ShiftDown()) then
        if notebook:GetSelection() == first
        then notebook:SetSelection(last)
        else notebook:AdvanceSelection(false) end
      elseif event:ControlDown() and
        (keycode == wx.WXK_PAGEDOWN or keycode == wx.WXK_TAB) then
        if notebook:GetSelection() == last
        then notebook:SetSelection(first)
        else notebook:AdvanceSelection(true) end
      else
        if ide.osname == 'Macintosh' and event:CmdDown() then
          return -- ignore a key press if Command key is also pressed
        end
        event:Skip()
      end
    end)

  local value
  editor:Connect(wx.wxEVT_CONTEXT_MENU,
    function (event)
      local menu = wx.wxMenu()
      menu:Append(wx.wxID_UNDO, "&Undo")
      menu:Append(wx.wxID_REDO, "&Redo")
      menu:AppendSeparator()
      menu:Append(wx.wxID_CUT, "Cu&t")
      menu:Append(wx.wxID_COPY, "&Copy")
      menu:Append(wx.wxID_PASTE, "&Paste")
      menu:Append(wx.wxID_SELECTALL, "Select &All")
      menu:AppendSeparator()
      menu:Append(ID_QUICKADDWATCH, "Add Watch Expression")
      menu:Append(ID_QUICKEVAL, "Evaluate in Console")

      local point = editor:ScreenToClient(event:GetPosition())
      local pos = editor:PositionFromPointClose(point.x, point.y)
      value = pos ~= wxstc.wxSTC_INVALID_POSITION and getValAtPosition(editor, pos) or nil
      menu:Enable(ID_QUICKADDWATCH, value ~= nil)
      menu:Enable(ID_QUICKEVAL, value ~= nil)

      -- cancel calltip as it interferes with popup menu
      if editor:CallTipActive() then editor:CallTipCancel() end
      editor:PopupMenu(menu)
    end)

  editor:Connect(ID_QUICKADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) DebuggerAddWatch(value) end)

  editor:Connect(ID_QUICKEVAL, wx.wxEVT_COMMAND_MENU_SELECTED,
    function(event) ShellExecuteCode(value) end)

  if notebook:AddPage(editor, name, true) then
    local id = editor:GetId()
    local document = {}
    document.editor = editor
    document.index = notebook:GetSelection()
    document.fileName = nil
    document.filePath = nil
    document.modTime = nil
    document.isModified = false
    openDocuments[id] = document
  end

  return editor
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

function IndicateFunctions(editor, lines, linee)
  if (not (edcfg.showfncall and editor.spec and editor.spec.isfncall)) then return end

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

  StylesApplyToEditor(styles or ide.config.styles, editor,
    font or ide.font.eNormal,fontitalic or ide.font.eItalic,lexerstyleconvert)
end

----------------------------------------------------
-- function list for current file

funclist:Connect(wx.wxEVT_SET_FOCUS,
  function (event)
    event:Skip()

    -- parse current file and update list
    local editor = GetEditor()

    if (not (editor and editor.spec and editor.spec.isfndef)) then return end

    -- first populate with the current label to minimize flicker
    -- then populate the list and update the label
    local current = funclist:GetCurrentSelection()
    local label = funclist:GetString(current)
    local default = funclist:GetString(0)
    funclist:Clear()
    funclist:Append(current ~= wx.wxNOT_FOUND and label or default, 0)
    funclist:SetSelection(0)

    local lines = 0
    local linee = editor:GetLineCount()-1
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
  end)

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
