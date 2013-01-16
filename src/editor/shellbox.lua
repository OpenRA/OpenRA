-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--
-- shellbox - a lua testbed environment within the IDE
--

local bottomnotebook = ide.frame.bottomnotebook
local out = bottomnotebook.shellbox
local remotesend

local PROMPT_MARKER = StylesGetMarker("prompt")
local PROMPT_MARKER_VALUE = 2^PROMPT_MARKER
local ERROR_MARKER = StylesGetMarker("error")
local OUTPUT_MARKER = StylesGetMarker("output")
local MESSAGE_MARKER = StylesGetMarker("message")

out:SetFont(ide.font.oNormal)
out:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font.oNormal)
out:StyleClearAll()
out:SetBufferedDraw(true)

out:SetTabWidth(ide.config.editor.tabwidth or 2)
out:SetIndent(ide.config.editor.tabwidth or 2)
out:SetUseTabs(ide.config.editor.usetabs and true or false)
out:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)
out:SetIndentationGuides(true)

out:SetWrapMode(wxstc.wxSTC_WRAP_WORD)
out:SetWrapStartIndent(0)
out:SetWrapVisualFlagsLocation(wxstc.wxSTC_WRAPVISUALFLAGLOC_END_BY_TEXT)
out:SetWrapVisualFlags(wxstc.wxSTC_WRAPVISUALFLAG_END)

out:MarkerDefine(StylesGetMarker("prompt"))
out:MarkerDefine(StylesGetMarker("error"))
out:MarkerDefine(StylesGetMarker("output"))
out:MarkerDefine(StylesGetMarker("message"))
out:SetReadOnly(false)

SetupKeywords(out,"lua",nil,ide.config.stylesoutshell,ide.font.oNormal,ide.font.oItalic)

local function getPromptLine()
  local totalLines = out:GetLineCount()
  return out:MarkerPrevious(totalLines+1, PROMPT_MARKER_VALUE)
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

local function caretOnPromptLine(disallowLeftmost, line)
  local promptLine = getPromptLine()
  local currentLine = line or out:GetCurrentLine()
  local boundary = disallowLeftmost and 0 or -1
  return (currentLine > promptLine
    or currentLine == promptLine and positionInLine(promptLine) > boundary)
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
    currentHistory = out:MarkerNext(currentHistory+1, PROMPT_MARKER_VALUE)
    if currentHistory == -1 then
      currentHistory = count
      return ""
    end
  else
    currentHistory = out:MarkerPrevious(currentHistory-1, PROMPT_MARKER_VALUE)
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
  if cnt == 0 then return end -- return if nothing to print

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
    if promptLine then out:MarkerDelete(promptLine, PROMPT_MARKER) end
    for line = insertLineAt, insertLineAt + linesAdded - 1 do
      out:MarkerAdd(line, marker)
    end
    if promptLine then out:MarkerAdd(promptLine+linesAdded, PROMPT_MARKER) end
  end

  out:EmptyUndoBuffer() -- don't allow the user to undo shell text
  out:GotoPos(out:GetLength())
  out:EnsureVisibleEnforcePolicy(out:GetLineCount()-1)
end

DisplayShell = function (...)
  shellPrint(OUTPUT_MARKER, ...)
end
DisplayShellErr = function (...)
  shellPrint(ERROR_MARKER, ...)
end
DisplayShellMsg = function (...)
  shellPrint(MESSAGE_MARKER, ...)
end
DisplayShellDirect = function (...)
  shellPrint(nil, ...)
end
DisplayShellPrompt = function (...)
  -- don't print anything; just mark the line with a prompt mark
  out:MarkerAdd(out:GetLineCount()-1, PROMPT_MARKER)
end

local function filterTraceError(err, addedret)
  local err = err:match("(.-:%d+:.-)\n[^\n]*\n[^\n]*\n[^\n]*src/editor/shellbox.lua:.*in function 'executeShellCode'")
              or err
        err = err:gsub("stack traceback:.-\n[^\n]+\n?","")
        if addedret then err = err:gsub('^%[string "return ', '[string "') end
        err = err:match("(.*)\n[^\n]*%(tail call%): %?$") or err
  return err
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
    local name = luafilepath(3)
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
      return fn(unpack(args))
    end
  end

  local os = { exit = function()
    ide.frame:AddPendingEvent(wx.wxCommandEvent(
      wx.wxEVT_COMMAND_MENU_SELECTED, ID_EXIT))
  end }
  env.os = setmetatable(os, {__index = _G.os})
  env.print = DisplayShell
  env.dofile = dofile
  env.loadfile = loadfile
  env.RELFILE = relativeFilename
  env.RELPATH = relativeFilepath

  return env
end

local env = createenv()

local function packResults(status, ...) return status, {...} end

local function executeShellCode(tx)
  if tx == nil or tx == '' then return end

  DisplayShellPrompt('')

  -- try to compile as statement
  local _, err = loadstring(tx)
  local isstatement = not err

  if remotesend then remotesend(tx, isstatement); return end

  local addedret, forceexpression = true, tx:match("^%s*=%s*")
  tx = tx:gsub("^%s*=%s*","")
  fn, err = loadstring("return "..tx)
  if not forceexpression and err and
     (err:find("'?<eof>'? expected near '") or
      err:find("'%(' expected near") or
      err:find("unexpected symbol near '")) then
    fn, err = loadstring(tx)
    addedret = false
  end
  
  if fn == nil and err then
    DisplayShellErr(filterTraceError(err, addedret))
  elseif fn then
    setfenv(fn,env)

    -- set the project dir as the current dir to allow "require" calls
    -- to work from shell
    local projectDir, cwd = FileTreeGetDir()
    if projectDir then
      cwd = wx.wxFileName.GetCwd()
      wx.wxFileName.SetCwd(projectDir)
    end

    local ok, res = packResults(xpcall(fn,
      function(err)
        DisplayShellErr(filterTraceError(debug.traceback(err), addedret))
      end))

    -- restore the current dir
    if projectDir then wx.wxFileName.SetCwd(cwd) end
    
    if ok and (addedret or #res > 0) then
      if addedret then
        local mobdebug = require "mobdebug"
        for i,v in pairs(res) do -- stringify each of the returned values
          res[i] = (forceexpression and i > 1 and '\n' or '') ..
            mobdebug.line(v, {nocode = true, comment = 1,
              -- if '=' is used, then use multi-line serialized output
              indent = forceexpression and '  ' or nil})
        end
        -- add nil only if we are forced (using =) or if this is not a statement
        -- this is needed to print 'nil' when asked for 'foo',
        -- and don't print it when asked for 'print(1)'
        if #res == 0 and (forceexpression or not isstatement) then
          res = {'nil'}
        end
      end
      DisplayShell((table.unpack or unpack)(res))
    end
  end
end

function ShellSupportRemote(client)
  remotesend = client

  local index = bottomnotebook:GetPageIndex(out)
  if index then
    bottomnotebook:SetPageText(index,
      client and TR("Remote console") or TR("Local console"))
  end
end

function ShellExecuteFile(wfilename)
  if (not wfilename) then return end
  local cmd = 'dofile([['..wfilename:GetFullPath()..']])'
  ShellExecuteCode(cmd)
end

function ShellExecuteCode(code)
  local index = bottomnotebook:GetPageIndex(bottomnotebook.shellbox)
  if ide.config.activateoutput and bottomnotebook:GetSelection() ~= index then
    bottomnotebook:SetSelection(index)
  end

  DisplayShellDirect(code)
  executeShellCode(code)
end

local function displayShellIntro()
  DisplayShellMsg(TR("Welcome to the interactive Lua interpreter.").."\n"
    ..TR("Enter Lua code and press Enter to run it.").." "
    ..TR("Use Shift-Enter for multiline code.").."\n"
    ..TR("Use 'clear' to clear the shell output and the history.").." "
    ..TR("Prepend '=' to show complex values on multiple lines."))
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
        -- if we are above the last line, then allow to go down
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
      elseif key == wx.WXK_RETURN or key == wx.WXK_NUMPAD_ENTER then
        if not caretOnPromptLine()
        or out:LineFromPosition(out:GetSelectionStart()) < getPromptLine() then
          return
        end

        -- allow multiline entry for shift+enter
        if caretOnPromptLine(true) and event:ShiftDown() then break end

        local promptText = getPromptText()
        if #promptText == 0 then return end -- nothing to execute, exit
        if promptText == 'clear' then
          out:ClearAll()
          displayShellIntro()
        else
          DisplayShellDirect('\n')
          executeShellCode(promptText)
        end
        currentHistory = getPromptLine() -- reset history
        return -- don't need to do anything else with return
      else
        -- move cursor to end if not already there
        if not caretOnPromptLine() then
          out:GotoPos(out:GetLength())
        -- check if the selection starts before the prompt line and reset it
        elseif out:LineFromPosition(out:GetSelectionStart()) < getPromptLine() then
          out:GotoPos(out:GetLength())
          out:SetSelection(out:GetSelectionEnd()+1,out:GetSelectionEnd())
        end
      end
      break
    end
    event:Skip()
  end)

local function inputEditable(line)
  return caretOnPromptLine(fale, line) and
    not (out:LineFromPosition(out:GetSelectionStart()) < getPromptLine())
end

out:Connect(wxstc.wxEVT_STC_UPDATEUI,
  function (event) out:SetReadOnly(not inputEditable()) end)

-- only allow copy/move text by dropping to the input line
out:Connect(wxstc.wxEVT_STC_DO_DROP,
  function (event)
    if not inputEditable(out:LineFromPosition(event:GetPosition())) then
      event:SetDragResult(wx.wxDragNone)
    end
  end)

displayShellIntro()
