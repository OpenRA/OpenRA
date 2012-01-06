-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--
-- shellbox - a lua testbed environment within the IDE
--

local bottomnotebook = ide.frame.vsplitter.splitter.bottomnotebook
local out = bottomnotebook.shellbox

local remotesend
local remoteuid

local OUTPUT_MARKER = 3
local OUTPUT_MARKER_VALUE = 8 -- = 2^OUTPUT_MARKER

local frame = ide.frame
out:SetFont(ide.ofont)
out:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.ofont)
out:StyleClearAll()
out:SetBufferedDraw(true)

out:SetTabWidth(ide.config.editor.tabwidth or 2)
out:SetIndent(ide.config.editor.tabwidth or 2)
out:SetUseTabs(ide.config.editor.usetabs and true or false)
out:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)
out:SetIndentationGuides(true)

out:SetWrapMode(wxstc.wxSTC_WRAP_WORD)
out:SetWrapStartIndent(2)
out:SetWrapVisualFlagsLocation(wxstc.wxSTC_WRAPVISUALFLAGLOC_END_BY_TEXT)
out:SetWrapVisualFlags(wxstc.wxSTC_WRAPVISUALFLAG_START)
out:WrapCount(80)

out:MarkerDefine(CURRENT_LINE_MARKER, wxstc.wxSTC_MARK_CHARACTER+string.byte('>'), wx.wxBLACK, wx.wxColour(240, 240, 240))
out:MarkerDefine(BREAKPOINT_MARKER, wxstc.wxSTC_MARK_BACKGROUND, wx.wxBLACK, wx.wxColour(255, 220, 220))
out:MarkerDefine(OUTPUT_MARKER, wxstc.wxSTC_MARK_BACKGROUND, wx.wxBLACK, wx.wxColour(240, 240, 240))
out:SetReadOnly(false)

SetupKeywords(out,"lua",nil,ide.config.stylesoutshell,ide.ofont,ide.ofontItalic)

local function getPromptLine()
  local totalLines = out:GetLineCount()
  return out:MarkerPrevious(totalLines+1, CURRENT_LINE_MARKER_VALUE)
end

local function getPromptText()
  local prompt = getPromptLine()
  return out:GetTextRange(out:PositionFromLine(prompt), out:GetLength())
end

local function setPromptText(text)
  local length = out:GetLength()
  out:SetTargetStart(length - string.len(getPromptText()))
  out:SetTargetEnd(length)
  out:ReplaceTarget(text)
  out:GotoPos(out:GetLength())
end

local function positionInLine(line)
  return out:GetCurrentPos() - out:PositionFromLine(line)
end

local function caretOnPromptLine(disallowLeftmost)
  local promptLine = getPromptLine()
  local boundary = disallowLeftmost and 0 or -1
  return (out:GetCurrentLine() > promptLine
    or out:GetCurrentLine() == promptLine and positionInLine(promptLine) > boundary)
end

local function chomp(line)
  return line:gsub("%s+$", "")
end

local function getInput(line)
  local nextMarker = line

  repeat
    nextMarker = nextMarker+1
  until out:MarkerGet(nextMarker) > 0 -- check until we find at least some marker
  return chomp(out:GetTextRange(out:PositionFromLine(line),
                                out:PositionFromLine(nextMarker)))
end

local currentHistory
local function getNextHistoryLine(forward, promptText)
  local count = out:GetLineCount()
  if currentHistory == nil then currentHistory = count end

  if forward then
    currentHistory = out:MarkerNext(currentHistory+1, CURRENT_LINE_MARKER_VALUE)
    if currentHistory == -1 then
      currentHistory = count
      return ""
    end
  else
    currentHistory = out:MarkerPrevious(currentHistory-1, CURRENT_LINE_MARKER_VALUE)
    if currentHistory == -1 then
      currentHistory = -1
      return ""
    end
  end
  -- need to skip the current prompt line
  -- or skip repeated commands
  if currentHistory == getPromptLine()
  or getInput(currentHistory) == promptText then
    return getNextHistoryLine(forward, promptText)
  end
  return getInput(currentHistory)
end

local function shellPrint(marker, ...)
  local cnt = select('#',...)
  local isPrompt = marker and (getPromptLine() > -1)

  local text = ''
  for i=1,cnt do
    local x = select(i,...)
    text = text .. tostring(x)..(i < cnt and "\t" or "")
  end
  -- add "\n" if it is missing
  if text then text = text:gsub("\n$", "") .. "\n" end

  local lines = out:GetLineCount()
  local promptLine = isPrompt and getPromptLine() or nil
  local insertLineAt = isPrompt and getPromptLine() or out:GetLineCount()-1
  local insertAt = isPrompt and out:PositionFromLine(getPromptLine()) or out:GetLength()
  out:InsertText(insertAt, text)
  local linesAdded = out:GetLineCount() - lines

  if marker then
    if promptLine then out:MarkerDelete(promptLine, CURRENT_LINE_MARKER) end
    for line = insertLineAt, insertLineAt + linesAdded - 1 do
      out:MarkerAdd(line, marker)
    end
    if promptLine then out:MarkerAdd(promptLine+linesAdded, CURRENT_LINE_MARKER) end
  end

  out:EmptyUndoBuffer() -- don't allow the user to undo shell text
  out:GotoPos(out:GetLength())
end

DisplayShell = function (...)
  shellPrint(OUTPUT_MARKER, ...)
end
DisplayShellErr = function (...)
  shellPrint(BREAKPOINT_MARKER, ...)
end
DisplayShellDirect = function (...)
  shellPrint(nil, ...)
end
DisplayShellPrompt = function (...)
  -- don't print anything; just mark the line with a prompt mark
  out:MarkerAdd(out:GetLineCount()-1, CURRENT_LINE_MARKER)
end

local function createenv ()
  local env = {}
  setmetatable(env,{__index = _G})

  local function luafilename(level)
    level = level and level + 1 or 2
    local src
    while (true) do
      src = debug.getinfo(level)
      if (src == nil) then return nil,level end
      if (string.byte(src.source) == string.byte("@")) then
        return string.sub(src.source,2),level
      end
      level = level + 1
    end
  end

  local function luafilepath(level)
    local src,level = luafilename(level)
    if (src == nil) then return src,level end
    src = string.gsub(src,"[\\/][^\\//]*$","")
    return src,level
  end

  local function relativeFilename(file)
    assert(type(file)=='string',"String as filename expected")
    local name = file
    local level = 3
    while (name) do
      if (wx.wxFileName(name):FileExists()) then return name end
      name,level = luafilepath(level)
      if (name == nil) then break end
      name = name .. "/" .. file
    end

    return file
  end

  local function relativeFilepath(file)
    local name,level = luafilepath(3)
    return (file and name) and name.."/"..file or file or name
  end

  local _loadfile = loadfile
  local function loadfile(file)
    assert(type(file)=='string',"String as filename expected")
    local name = relativeFilename(file)

    return _loadfile(name)
  end

  local function dofile(file, ...)
    assert(type(file) == 'string',"String as filename expected")
    local fn,err = loadfile(file)
    local args = {...}
    if not fn then
      DisplayShellErr(err)
    else
      setfenv(fn,env)
      xpcall(function() return fn(unpack(args)) end,function(err)
          DisplayShellErr(debug.traceback(err))
        end)
    end
  end

  env.print = DisplayShell
  env.dofile = dofile
  env.loadfile = loadfile
  env.RELFILE = relativeFilename
  env.RELPATH = relativeFilepath

  return env
end

local env = createenv()

local function executeShellCode(tx)
  if tx == nil or tx == '' then return end

  DisplayShellDirect('\n')
  DisplayShellPrompt('')

  local fn,err
  if remotesend then
    remotesend(tx)
  else
    -- for some direct queries
    fn,err = loadstring("return("..tx..")")
    -- otherise use string directly
    if err then
      fn,err = loadstring(tx)
    end

    if fn == nil and err then
      DisplayShellErr(err)
    elseif fn then
      setfenv(fn,env)
      local ok, res = pcall(fn)
      if ok then
        if res ~= nil then DisplayShell(res) end
      else
        DisplayShellErr(res)
      end
    end
  end
end

function ShellSupportRemote(client,uid)
  remotesend = client
  remoteuid = client and uid

  -- change the name of the tab: console is the second page in the notebook
  bottomnotebook:SetPageText(1,
    client and "Remote console" or "Local console")
end

function ShellExecuteCode(wfilename)
  if (not wfilename) then return end
  local cmd = 'dofile([['..wfilename:GetFullPath()..']])'
  DisplayShellDirect(cmd)
  executeShellCode(cmd)
end

local function displayShellIntro()
  DisplayShellDirect([[Welcome to the interactive Lua interpreter.
Enter Lua code and press Enter to run it. Use Shift-Enter for multiline code.
Use 'clear' to clear the shell output and the history.]])
  DisplayShellPrompt('')
end

out:Connect(wx.wxEVT_KEY_DOWN,
  function (event)
    -- this loop is only needed to allow to get to the end of function easily
    -- "return" aborts the processing and ignores the key
    -- "break" aborts the processing and processes the key normally
    while true do
      local key = event:GetKeyCode()
      if key == wx.WXK_UP or key == wx.WXK_NUMPAD_UP then
        -- if we are below the prompt line, then allow to go up
        -- through multiline entry
        if out:GetCurrentLine() > getPromptLine() then break end

        -- if we are not on the caret line, then don't move
        if not caretOnPromptLine() then return end

        local promptText = getPromptText()
        setPromptText(getNextHistoryLine(false, promptText))
        return
      elseif key == wx.WXK_DOWN or key == wx.WXK_NUMPAD_DOWN then
        -- if we are below the prompt line, then allow to go down
        -- through multiline entry
        local totalLines = out:GetLineCount()-1
        if out:GetCurrentLine() < totalLines then break end

        if not caretOnPromptLine() then break end
        local promptText = getPromptText()
        setPromptText(getNextHistoryLine(true, promptText))
        return
      elseif key == wx.WXK_LEFT or key == wx.WXK_NUMPAD_LEFT then
        if not caretOnPromptLine(true) then return end
      elseif key == wx.WXK_BACK then
        if not caretOnPromptLine(true) then return end
      elseif key == wx.WXK_DELETE or key == wx.WXK_NUMPAD_DELETE then
        if not caretOnPromptLine()
        or out:LineFromPosition(out:GetSelectionStart()) < getPromptLine() then
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
      elseif key == wx.WXK_RETURN or key == WXK_NUMPAD_ENTER then
        if not caretOnPromptLine()
        or out:LineFromPosition(out:GetSelectionStart()) < getPromptLine() then
          return
        end

        -- allow multiline entry for shift+enter
        if caretOnPromptLine(true) and event:ShiftDown() then break end

        local promptText = getPromptText()
        if promptText == 'clear' then
          out:ClearAll()
          displayShellIntro()
        else
          executeShellCode(promptText)
        end
        currentHistory = getPromptLine() -- reset history
        return -- don't need to do anything else with return
      else
        -- move cursor to end if not already there
        if not caretOnPromptLine() then
          out:GotoPos(out:GetLength())
        elseif out:LineFromPosition(out:GetSelectionStart()) < getPromptLine() then
          out:GotoPos(out:GetLength())
          out:SetSelection(out:GetSelectionEnd()+1,out:GetSelectionEnd())
        end
      end
      break
    end
    event:Skip()
  end)

displayShellIntro()

