-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local frame = ide.frame
local notebook = frame.notebook
local bottomnotebook = frame.bottomnotebook
local errorlog = bottomnotebook.errorlog

-------
-- setup errorlog
local MESSAGE_MARKER = StylesGetMarker("message")
local PROMPT_MARKER = StylesGetMarker("prompt")
local PROMPT_MARKER_VALUE = 2^PROMPT_MARKER

errorlog:Show(true)
errorlog:SetFont(ide.font.oNormal)
errorlog:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font.oNormal)
errorlog:StyleClearAll()
errorlog:SetMarginWidth(1, 16) -- marker margin
errorlog:SetMarginType(1, wxstc.wxSTC_MARGIN_SYMBOL);
errorlog:MarkerDefine(StylesGetMarker("message"))
errorlog:MarkerDefine(StylesGetMarker("prompt"))
errorlog:SetReadOnly(true)
StylesApplyToEditor(ide.config.stylesoutshell,errorlog,ide.font.oNormal,ide.font.oItalic)

function ClearOutput()
  errorlog:SetReadOnly(false)
  errorlog:ClearAll()
  errorlog:SetReadOnly(true)
end

function DisplayOutputNoMarker(...)
  local message = ""
  local cnt = select('#',...)
  for i=1,cnt do
    local v = select(i,...)
    message = message..tostring(v)..(i<cnt and "\t" or "")
  end

  local current = errorlog:GetReadOnly()
  errorlog:SetReadOnly(false)
  errorlog:AppendText(message)
  errorlog:EmptyUndoBuffer()
  errorlog:SetReadOnly(current)
  errorlog:GotoPos(errorlog:GetLength())
end
function DisplayOutput(...)
  errorlog:MarkerAdd(errorlog:GetLineCount()-1, MESSAGE_MARKER)
  DisplayOutputNoMarker(...)
end
function DisplayOutputLn(...)
  DisplayOutput(...)
  DisplayOutputNoMarker("\n")
end

local streamins = {}
local streamerrs = {}
local streamouts = {}
local customprocs = {}
local textout = '' -- this is a buffer for any text sent to external scripts

function CommandLineRunning(uid)
  for pid,custom in pairs(customprocs) do
    if (custom.uid == uid and custom.proc and custom.proc.Exists(tonumber(tostring(pid))) )then
      return true
    end
  end

  return false
end

function CommandLineToShell(uid,state)
  for pid,custom in pairs(customprocs) do
    if ((pid == uid or custom.uid == uid) and custom.proc and custom.proc.Exists(tonumber(tostring(pid))) )then
      if (streamins[pid]) then streamins[pid].toshell = state end
      if (streamerrs[pid]) then streamerrs[pid].toshell = state end
      return true
    end
  end
end

-- logic to "unhide" wxwidget window using winapi
pcall(function () return require 'winapi' end)
local pid = nil
local function unHideWindow(pidAssign)
  -- skip if not configured to do anything
  if not ide.config.unhidewindow then return end
  if pidAssign then
    pid = pidAssign > 0 and pidAssign or nil
  end
  if pid and winapi then
    local wins = winapi.find_all_windows(function(w)
      return w:get_process():get_pid() == pid
    end)
    local any = ide.interpreter.unhideanywindow
    local show, hide, ignore = 1, 2, 0
    for _,win in pairs(wins) do
      -- win:get_class_name() can return nil if the window is already gone
      -- between getting the list and this check.
      local action = ide.config.unhidewindow[win:get_class_name()]
        or (any and show or ignore)
      if action == show and not win:is_visible()
      or action == hide and win:is_visible() then
        -- use show_async call (ShowWindowAsync) to avoid blocking the IDE
        -- if the app is busy or is being debugged
        win:show_async(show and winapi.SW_SHOW or winapi.SW_HIDE)
        notebook:SetFocus() -- set focus back to the IDE window
        pid = nil
      end
    end
  end
end

local function nameTab(tab, name)
  local index = bottomnotebook:GetPageIndex(tab)
  if index then bottomnotebook:SetPageText(index, name) end
end

function CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
  if (not cmd) then return end

  -- try to extract the name of the executable from the command
  -- the executable may not have the extension and may be in quotes
  local exename = string.gsub(cmd, "\\", "/")
  local _,_,fullname = string.find(exename,'^[\'"]([^\'"]+)[\'"]')
  exename = fullname and string.match(fullname,'/?([^/]+)$')
    or string.match(exename,'/?([^/]-)%s') or exename

  uid = uid or exename

  if (CommandLineRunning(uid)) then
    DisplayOutputLn(TR("Program can't start because conflicting process is running as '%s'.")
      :format(cmd))
    return
  end

  DisplayOutputLn(TR("Program starting as '%s'."):format(cmd))

  local proc = wx.wxProcess(errorlog)
  if (tooutput) then proc:Redirect() end -- redirect the output if requested

  -- manipulate working directory
  local oldcwd
  if (wdir) then
    oldcwd = wx.wxFileName.GetCwd()
    oldcwd = wx.wxFileName.SetCwd(wdir) and oldcwd
  end

  -- launch process
  local params = wx.wxEXEC_ASYNC + wx.wxEXEC_MAKE_GROUP_LEADER + (nohide and wx.wxEXEC_NOHIDE or 0)
  local pid = wx.wxExecute(cmd, params, proc)

  if (oldcwd) then
    wx.wxFileName.SetCwd(oldcwd)
  end

  -- For asynchronous execution, the return value is the process id and
  -- zero value indicates that the command could not be executed.
  -- The return value of -1 in this case indicates that we didn't launch
  -- a new process, but connected to the running one (e.g. DDE under Windows).
  if not pid or pid == -1 or pid == 0 then
    DisplayOutputLn(TR("Program unable to run as '%s'."):format(cmd))
    return
  end

  DisplayOutputLn(TR("Program '%s' started in '%s' (pid: %d).")
    :format(uid, (wdir and wdir or wx.wxFileName.GetCwd()), pid))
  customprocs[pid] = {proc=proc, uid=uid, endcallback=endcallback, started = TimeGet()}

  local streamin = proc and proc:GetInputStream()
  local streamerr = proc and proc:GetErrorStream()
  local streamout = proc and proc:GetOutputStream()
  if (streamin) then
    streamins[pid] = {stream=streamin, callback=stringcallback}
  end
  if (streamerr) then
    streamerrs[pid] = {stream=streamerr, callback=stringcallback}
  end
  if (streamout) then
    streamouts[pid] = {stream=streamout, callback=stringcallback, out=true}
  end

  unHideWindow(pid)
  nameTab(errorlog, TR("Output (running)"))

  return pid
end

local inputBound -- to track where partial output ends for input editing purposes
local function getInputLine()
  local totalLines = errorlog:GetLineCount()
  return errorlog:MarkerPrevious(totalLines+1, PROMPT_MARKER_VALUE)
end
local function getInputText(bound)
  return errorlog:GetTextRange(
    errorlog:PositionFromLine(getInputLine())+(bound or 0), errorlog:GetLength())
end
local function updateInputMarker()
  local lastline = errorlog:GetLineCount()-1
  errorlog:MarkerDeleteAll(PROMPT_MARKER)
  errorlog:MarkerAdd(lastline, PROMPT_MARKER)
  inputBound = #getInputText()
end

local function getStreams()
  local function readStream(tab)
    for _,v in pairs(tab) do
      while(v.stream:CanRead()) do
        local str = v.stream:Read(4096)
        local pfn
        if (v.callback) then
          str,pfn = v.callback(str)
        end
        if (v.toshell) then
          DisplayShell(str)
        else
          DisplayOutputNoMarker(str)
        end
        if str and ide.config.allowinteractivescript and
          (getInputLine() > -1 or errorlog:GetReadOnly()) then
          ActivateOutput()
          updateInputMarker()
        end
        pfn = pfn and pfn()
      end
    end
  end
  local function sendStream(tab)
    local str = textout
    if not str then return end
    textout = nil
    str = str .. "\n"
    for _,v in pairs(tab) do
      local pfn
      if (v.callback) then
        str,pfn = v.callback(str)
      end
      v.stream:Write(str, #str)
      updateInputMarker()
      pfn = pfn and pfn()
    end
  end

  readStream(streamins)
  readStream(streamerrs)
  sendStream(streamouts)
end

errorlog:Connect(wx.wxEVT_END_PROCESS, function(event)
    local pid = event:GetPid()
    if (pid ~= -1) then
      getStreams()
      -- delete markers and set focus to the editor if there is an input marker
      if errorlog:MarkerPrevious(errorlog:GetLineCount(), PROMPT_MARKER_VALUE) > -1 then
        errorlog:MarkerDeleteAll(PROMPT_MARKER)
        local editor = GetEditor()
        -- check if editor still exists; it may not if the window is closed
        if editor then editor:SetFocus() end
      end
      nameTab(errorlog, TR("Output"))
      local runtime = TimeGet() - customprocs[pid].started

      streamins[pid] = nil
      streamerrs[pid] = nil
      streamouts[pid] = nil
      if (customprocs[pid].endcallback) then
        customprocs[pid].endcallback()
      end
      customprocs[pid] = nil
      unHideWindow(0)
      DebuggerStop()
      DisplayOutputLn(TR("Program completed in %.2f seconds (pid: %d).")
        :format(runtime, pid))
    end
  end)

errorlog:Connect(wx.wxEVT_IDLE, function()
    if (#streamins or #streamerrs) then getStreams() end
    unHideWindow()
  end)

local jumptopatterns = {
  -- <filename>(line,linepos):
  "^%s*(.-)%((%d+),(%d+)%)%s*:",
  -- <filename>(line):
  "^%s*(.-)%((%d+).*%)%s*:",
  --[string "<filename>"]:line:
  '^.-%[string "([^"]+)"%]:(%d+)%s*:',
  -- <filename>:line:
  "^%s*(.-):(%d+)%s*:",
}

errorlog:Connect(wxstc.wxEVT_STC_DOUBLECLICK,
  function()
    local line = errorlog:GetCurrentLine()
    local linetx = errorlog:GetLine(line)
    -- try to detect a filename + line
    -- in linetx

    local fname
    local jumpline
    local jumplinepos

    for _,pattern in ipairs(jumptopatterns) do
      fname,jumpline,jumplinepos = linetx:match(pattern)
      if (fname and jumpline) then
        break
      end
    end

    if (fname and jumpline) then
      LoadFile(fname,nil,true)
      local editor = GetEditor()
      if (editor) then
        jumpline = tonumber(jumpline)
        jumplinepos = tonumber(jumplinepos)

        --editor:ScrollToLine(jumpline)
        editor:GotoPos(editor:PositionFromLine(math.max(0,jumpline-1)) + (jumplinepos and (math.max(0,jumplinepos-1)) or 0))
        editor:SetFocus()
      end
    end
  end)

local function positionInLine(line)
  return errorlog:GetCurrentPos() - errorlog:PositionFromLine(line)
end
local function caretOnInputLine(disallowLeftmost)
  local inputLine = getInputLine()
  local boundary = inputBound + (disallowLeftmost and 0 or -1)
  return (errorlog:GetCurrentLine() > inputLine
    or errorlog:GetCurrentLine() == inputLine
   and positionInLine(inputLine) > boundary)
end

errorlog:Connect(wx.wxEVT_KEY_DOWN,
  function (event)
    -- this loop is only needed to allow to get to the end of function easily
    -- "return" aborts the processing and ignores the key
    -- "break" aborts the processing and processes the key normally
    while true do
      -- no special processing if it's readonly
      if errorlog:GetReadOnly() then break end

      local key = event:GetKeyCode()
      if key == wx.WXK_UP or key == wx.WXK_NUMPAD_UP then
        if errorlog:GetCurrentLine() > getInputLine() then break
        else return end
      elseif key == wx.WXK_DOWN or key == wx.WXK_NUMPAD_DOWN then
        break -- can go down
      elseif key == wx.WXK_LEFT or key == wx.WXK_NUMPAD_LEFT then
        if not caretOnInputLine(true) then return end
      elseif key == wx.WXK_BACK then
        if not caretOnInputLine(true) then return end
      elseif key == wx.WXK_DELETE or key == wx.WXK_NUMPAD_DELETE then
        if not caretOnInputLine()
        or errorlog:LineFromPosition(errorlog:GetSelectionStart()) < getInputLine() then
          return
        end
      elseif key == wx.WXK_PAGEUP or key == wx.WXK_NUMPAD_PAGEUP
          or key == wx.WXK_PAGEDOWN or key == wx.WXK_NUMPAD_PAGEDOWN
          or key == wx.WXK_END or key == wx.WXK_NUMPAD_END
          or key == wx.WXK_HOME or key == wx.WXK_NUMPAD_HOME
          or key == wx.WXK_RIGHT or key == wx.WXK_NUMPAD_RIGHT
          or key == wx.WXK_SHIFT or key == wx.WXK_CONTROL
          or key == wx.WXK_ALT then
        break
      elseif key == wx.WXK_RETURN or key == wx.WXK_NUMPAD_ENTER then
        if not caretOnInputLine()
        or errorlog:LineFromPosition(errorlog:GetSelectionStart()) < getInputLine() then
          return
        end
        errorlog:GotoPos(errorlog:GetLength()) -- move to the end
        textout = (textout or '') .. getInputText(inputBound)
        -- remove selection if any, otherwise the text gets replaced
        errorlog:SetSelection(errorlog:GetSelectionEnd()+1,errorlog:GetSelectionEnd())
        break -- don't need to do anything else with return
      else
        -- move cursor to end if not already there
        if not caretOnInputLine() then
          errorlog:GotoPos(errorlog:GetLength())
        -- check if the selection starts before the input line and reset it
        elseif errorlog:LineFromPosition(errorlog:GetSelectionStart()) < getInputLine(-1) then
          errorlog:GotoPos(errorlog:GetLength())
          errorlog:SetSelection(errorlog:GetSelectionEnd()+1,errorlog:GetSelectionEnd())
        end
      end
      break
    end
    event:Skip()
  end)

local function inputEditable(line)
  local inputLine = getInputLine()
  local currentLine = line or errorlog:GetCurrentLine()
  return inputLine > -1 and
    (currentLine > inputLine or
     currentLine == inputLine and positionInLine(inputLine) >= inputBound) and
    not (errorlog:LineFromPosition(errorlog:GetSelectionStart()) < getInputLine())
end

errorlog:Connect(wxstc.wxEVT_STC_UPDATEUI,
  function () errorlog:SetReadOnly(not inputEditable()) end)

-- only allow copy/move text by dropping to the input line
errorlog:Connect(wxstc.wxEVT_STC_DO_DROP,
  function (event)
    if not inputEditable(errorlog:LineFromPosition(event:GetPosition())) then
      event:SetDragResult(wx.wxDragNone)
    end
  end)
