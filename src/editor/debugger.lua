-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- Original authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
-- Integration with MobDebug
---------------------------------------------------------

local copas = require "copas"
local socket = require "socket"
local mobdebug = require "mobdebug"
local unpack = table.unpack or unpack

local ide = ide
local debugger = setmetatable(ide.debugger, ide.proto.Debugger)
debugger.server = nil -- DebuggerServer object when debugging, else nil
debugger.running = false -- true when the debuggee is running
debugger.listening = false -- true when the debugger is listening for a client
debugger.portnumber = ide.config.debugger.port or mobdebug.port -- the port # to use for debugging
debugger.watchCtrl = nil -- the watch ctrl that shows watch information
debugger.stackCtrl = nil -- the stack ctrl that shows stack information
debugger.toggleview = {
  bottomnotebook = true, -- output/console is "on" by default
  stackpanel = false, watchpanel = false, toolbar = false }
debugger.needrefresh = {} -- track components that may need a refresh
debugger.hostname = ide.config.debugger.hostname or (function()
  local hostname = socket.dns.gethostname()
  return hostname and socket.dns.toip(hostname) and hostname or "localhost"
end)()
debugger.imglist = ide:CreateImageList("STACK", "VALUE-CALL", "VALUE-LOCAL", "VALUE-UP")

local image = { STACK = 0, LOCAL = 1, UPVALUE = 2 }
local notebook = ide.frame.notebook

local CURRENT_LINE_MARKER = StylesGetMarker("currentline")
local CURRENT_LINE_MARKER_VALUE = 2^CURRENT_LINE_MARKER
local BREAKPOINT_MARKER = StylesGetMarker("breakpoint")
local BREAKPOINT_MARKER_VALUE = 2^BREAKPOINT_MARKER

local activate = {CHECKONLY = 1, NOREPORT = 2}

local function serialize(value, options) return mobdebug.line(value, options) end

local stackmaxlength = ide.config.debugger.maxdatalength
local stackmaxnum = ide.config.debugger.maxdatanum
local stackmaxlevel = ide.config.debugger.maxdatalevel
local params = {comment = false, nocode = true, maxlevel = stackmaxlevel, maxnum = stackmaxnum}

local function fixUTF8(...)
  local t = {...}
  -- convert to escaped decimal code as these can only appear in strings
  local function fix(s) return '\\'..string.byte(s) end
  for i = 1, #t do t[i] = FixUTF8(t[i], fix) end
  return unpack(t)
end

local function trimToMaxLength(...)
  local t = {...}
  for i = 1, #t do
    t[i] = t[i]:sub(1, stackmaxlength)..(#t[i] > stackmaxlength and '...' or '')
  end
  return unpack(t)
end

local q = EscapeMagic

local function updateWatchesSync(onlyitem)
  local watchCtrl = debugger.watchCtrl
  local pane = ide.frame.uimgr:GetPane("watchpanel")
  local shown = watchCtrl and (pane:IsOk() and pane:IsShown() or not pane:IsOk() and watchCtrl:IsShown())
  local canupdate = (debugger.server and not debugger.running and not debugger.scratchpad
    and not (debugger.options or {}).noeval)
  if shown and canupdate then
    local bgcl = watchCtrl:GetBackgroundColour()
    local hicl = wx.wxColour(math.floor(bgcl:Red()*.9),
      math.floor(bgcl:Green()*.9), math.floor(bgcl:Blue()*.9))

    local root = watchCtrl:GetRootItem()
    if not root or not root:IsOk() then return end

    local item = onlyitem or watchCtrl:GetFirstChild(root)
    while true do
      if not item:IsOk() then break end

      local expression = watchCtrl:GetItemExpression(item)
      if expression then
        local _, values, error = debugger.evaluate(expression)
        local curchildren = watchCtrl:GetItemChildren(item)
        if error then
          error = error:gsub("%[.-%]:%d+:%s+","")
          watchCtrl:SetItemValueIfExpandable(item, nil)
        else
          if #values == 0 then values = {'nil'} end
          local _, res = LoadSafe("return "..values[1])
          watchCtrl:SetItemValueIfExpandable(item, res)
        end

        local newval = (expression .. ' = '
          .. (error and ('error: '..error) or table.concat(values, ", ")))
        local val = watchCtrl:GetItemText(item)

        watchCtrl:SetItemBackgroundColour(item, val ~= newval and hicl or bgcl)
        watchCtrl:SetItemText(item, newval)

        if onlyitem or val ~= newval then
          local newchildren = watchCtrl:GetItemChildren(item)
          if next(curchildren) ~= nil and next(newchildren) == nil then
            watchCtrl:SetItemHasChildren(item, true)
            watchCtrl:CollapseAndReset(item)
            watchCtrl:SetItemHasChildren(item, false)
          elseif next(curchildren) ~= nil and next(newchildren) ~= nil then
            watchCtrl:CollapseAndReset(item)
            watchCtrl:Expand(item)
          end
        end
      end

      if onlyitem then break end
      item = watchCtrl:GetNextSibling(item)
    end
  elseif not shown and canupdate then
    debugger.needrefresh.watches = true
  end
end

local simpleType = {['nil'] = true, ['string'] = true, ['number'] = true, ['boolean'] = true}
local callData = {}

local function updateStackSync()
  local stackCtrl = debugger.stackCtrl
  local pane = ide.frame.uimgr:GetPane("stackpanel")
  local shown = stackCtrl and (pane:IsOk() and pane:IsShown() or not pane:IsOk() and stackCtrl:IsShown())
  local canupdate = debugger.server and not debugger.running and not debugger.scratchpad
  if shown and canupdate then
    local stack, _, err = debugger.stack()
    if not stack or #stack == 0 then
      stackCtrl:DeleteAll()
      if err then -- report an error if any
        stackCtrl:AppendItem(stackCtrl:AddRoot("Stack"), "Error: " .. err, image.STACK)
      end
      return
    end
    stackCtrl:Freeze()
    stackCtrl:DeleteAll()

    local root = stackCtrl:AddRoot("Stack")
    callData = {} -- reset call cache
    for _,frame in ipairs(stack) do
      -- "main chunk at line 24"
      -- "foo() at line 13 (defined at foobar.lua:11)"
      -- call = { source.name, source.source, source.linedefined,
      --   source.currentline, source.what, source.namewhat, source.short_src }
      local call = frame[1]

      -- format the function name to a readable user string
      local func = call[5] == "main" and "main chunk"
        or call[5] == "C" and (call[1] or "C function")
        or call[5] == "tail" and "tail call"
        or (call[1] or "anonymous function")

      -- format the function treeitem text string, including the function name
      local text = func ..
        (call[4] == -1 and '' or " at line "..call[4]) ..
        (call[5] ~= "main" and call[5] ~= "Lua" and ''
         or (call[3] > 0 and " (defined at "..call[7]..":"..call[3]..")"
                          or " (defined in "..call[7]..")"))

      -- create the new tree item for this level of the call stack
      local callitem = stackCtrl:AppendItem(root, text, image.STACK)

      -- register call data to provide stack navigation
      callData[callitem:GetValue()] = { call[2], call[4] }

      -- add the local variables to the call stack item
      for name,val in pairs(frame[2]) do
        -- format the variable name, value as a single line and,
        -- if not a simple type, the string value.

        -- comment can be not necessarily a string for tables with metatables
        -- that provide its own __tostring method
        local value, comment = val[1], fixUTF8(trimToMaxLength(tostring(val[2])))
        local text = ("%s = %s%s"):
          format(name, fixUTF8(trimToMaxLength(serialize(value, params))),
                 simpleType[type(value)] and "" or ("  --[["..comment.."]]"))
        local item = stackCtrl:AppendItem(callitem, text, image.LOCAL)
        stackCtrl:SetItemValueIfExpandable(item, value)
      end

      -- add the upvalues for this call stack level to the tree item
      for name,val in pairs(frame[3]) do
        local value, comment = val[1], fixUTF8(trimToMaxLength(tostring(val[2])))
        local text = ("%s = %s%s"):
          format(name, fixUTF8(trimToMaxLength(serialize(value, params))),
                 simpleType[type(value)] and "" or ("  --[["..comment.."]]"))
        local item = stackCtrl:AppendItem(callitem, text, image.UPVALUE)
        stackCtrl:SetItemValueIfExpandable(item, value)
      end

      stackCtrl:SortChildren(callitem)
      stackCtrl:Expand(callitem)
    end
    stackCtrl:EnsureVisible(stackCtrl:GetFirstChild(root))
    stackCtrl:SetScrollPos(wx.wxHORIZONTAL, 0, true)
    stackCtrl:Thaw()
  elseif not shown and canupdate then
    debugger.needrefresh.stack = true
  end
end

local function updateStackAndWatches()
  -- check if the debugger is running and may be waiting for a response.
  -- allow that request to finish, otherwise this function does nothing.
  if debugger.running then debugger.update() end
  if debugger.server and not debugger.running then
    copas.addthread(function() updateStackSync() updateWatchesSync() end)
  end
end

local function updateWatches(item)
  -- check if the debugger is running and may be waiting for a response.
  -- allow that request to finish, otherwise this function does nothing.
  if debugger.running then debugger.update() end
  if debugger.server and not debugger.running then
    copas.addthread(function() updateWatchesSync(item) end)
  end
end

local function updateStack()
  -- check if the debugger is running and may be waiting for a response.
  -- allow that request to finish, otherwise this function does nothing.
  if debugger.running then debugger.update() end
  if debugger.server and not debugger.running then
    copas.addthread(function() updateStackSync() end)
  end
end

local function debuggerToggleViews(show)
  -- don't toggle if the current state is the same as the new one
  local shown = debugger.toggleview.shown
  if (show and shown) or (not show and not shown) then return end

  debugger.toggleview.shown = nil

  local mgr = ide.frame.uimgr
  local refresh = false
  for view, needed in pairs(debugger.toggleview) do
    local bar = view == 'toolbar'
    local pane = mgr:GetPane(view)
    if show then -- starting debugging and pane is not shown
      debugger.toggleview[view] = not pane:IsShown()
      if debugger.toggleview[view] and (needed or bar)
      and (not bar or not ide.frame:IsFullScreen()) then
        pane:Show()
        refresh = true
      end
    else -- completing debugging and pane is shown
      debugger.toggleview[view] = pane:IsShown() and needed
      if debugger.toggleview[view] then
        pane:Hide()
        refresh = true
      end
    end
  end
  if refresh then mgr:Update() end
  if show then debugger.toggleview.shown = true end
end

local function killClient()
  if (debugger.pid and wx.wxProcess.Exists(debugger.pid)) then
    -- using SIGTERM for some reason kills not only the debugee process,
    -- but also some system processes, which leads to a blue screen crash
    -- (at least on Windows Vista SP2)
    local ret = wx.wxProcess.Kill(debugger.pid, wx.wxSIGKILL, wx.wxKILL_CHILDREN)
    if ret == wx.wxKILL_OK then
      DisplayOutputLn(TR("Program stopped (pid: %d)."):format(debugger.pid))
    elseif ret ~= wx.wxKILL_NO_PROCESS then
      DisplayOutputLn(TR("Unable to stop program (pid: %d), code %d.")
        :format(debugger.pid, ret))
    end
  end
  debugger.pid = nil
end

local function activateDocument(file, line, activatehow)
  if not file then return end

  -- file can be a filename or serialized file content; deserialize first.
  -- check if the filename starts with '"' and is deserializable
  -- to avoid showing filenames that may look like valid lua code
  -- (for example: 'mobdebug.lua').
  local content
  if not wx.wxFileName(file):FileExists() and file:find('^"') then
    local ok, res = LoadSafe("return "..file)
    if ok then content = res end
  end

  -- in some cases filename can be returned quoted if the chunk is loaded with
  -- loadstring(chunk, "filename") instead of loadstring(chunk, "@filename")
  if content then
    -- if the returned content can be matched with a file, it's a file name
    local fname = GetFullPathIfExists(debugger.basedir, content) or content
    if wx.wxFileName(fname):FileExists() then file, content = fname, nil end
  elseif not wx.wxIsAbsolutePath(file) and debugger.basedir then
    file = debugger.basedir .. file
  end

  local activated
  local indebugger = file:find('mobdebug%.lua$')
  local fileName = wx.wxFileName(file)

  for _, document in pairs(ide.openDocuments) do
    local editor = document.editor
    -- either the file name matches, or the content;
    -- when checking for the content remove all newlines as they may be
    -- reported differently from the original by the Lua engine.
    if document.filePath and fileName:SameAs(wx.wxFileName(document.filePath))
    or content and content:gsub("[\n\r]","") == editor:GetText():gsub("[\n\r]","") then
      ClearAllCurrentLineMarkers()
      if line then
        if line == 0 then -- special case; find the first executable line
          line = math.huge
          local func = loadstring(editor:GetText())
          if func then -- .activelines == {[3] = true, [4] = true, ...}
            for l in pairs(debug.getinfo(func, "L").activelines) do
              if l < line then line = l end
            end
          end
          if line == math.huge then line = 1 end
        end
        local line = line - 1 -- editor line operations are zero-based
        if debugger.runtocursor then
          local ed, ln = unpack(debugger.runtocursor)
          if ed:GetId() == editor:GetId() and ln == line then
            DebuggerToggleBreakpoint(ed, ln)
            debugger.runtocursor = nil
          end
        end
        editor:MarkerAdd(line, CURRENT_LINE_MARKER)
        editor:Refresh() -- needed for background markers that don't get refreshed (wx2.9.5)

        -- found and marked what we are looking for;
        -- don't need to activate with CHECKONLY (this assumes line is given)
        if activatehow == activate.CHECKONLY then return editor end

        local firstline = editor:DocLineFromVisible(editor:GetFirstVisibleLine())
        local lastline = math.min(editor:GetLineCount(),
          editor:DocLineFromVisible(editor:GetFirstVisibleLine() + editor:LinesOnScreen()))
        -- if the line is already on the screen, then don't enforce policy
        if line <= firstline or line >= lastline then
          editor:EnsureVisibleEnforcePolicy(line)
        end
      end

      local selection = document.index
      RequestAttention()
      notebook:SetSelection(selection)
      SetEditorSelection(selection)

      if content then
        -- it's possible that the current editor tab already has
        -- breakpoints that have been set based on its filepath;
        -- if the content has been matched, then existing breakpoints
        -- need to be removed and new ones set, based on the content.
        if not debugger.editormap[editor] and document.filePath then
          local filePath = document.filePath
          local line = editor:MarkerNext(0, BREAKPOINT_MARKER_VALUE)
          while filePath and line ~= -1 do
            debugger.handle("delb " .. filePath .. " " .. (line+1))
            debugger.handle("setb " .. file .. " " .. (line+1))
            line = editor:MarkerNext(line + 1, BREAKPOINT_MARKER_VALUE)
          end
        end

        -- keep track of those editors that have been activated based on
        -- content rather than file names as their breakpoints have to be
        -- specified in a different way
        debugger.editormap[editor] = file
      end

      activated = editor
      break
    end
  end

  if not (activated or indebugger or debugger.loop or activatehow == activate.CHECKONLY)
  and (ide.config.editor.autoactivate or content and activatehow == activate.NOREPORT) then
    -- found file, but can't activate yet (because this part may be executed
    -- in a different coroutine), so schedule pending activation.
    if content or wx.wxFileName(file):FileExists() then
      debugger.activate = {file, line, content}
      return true -- report successful activation, even though it's pending
    end

    -- only report files once per session and if not asked to skip
    if not debugger.missing[file] and activatehow ~= activate.NOREPORT then
      debugger.missing[file] = true
      DisplayOutputLn(TR("Couldn't activate file '%s' for debugging; continuing without it.")
        :format(file))
    end
  end

  return activated ~= nil
end

local function reSetBreakpoints()
  -- remove all breakpoints that may still be present from the last session
  -- this only matters for those remote clients that reload scripts
  -- without resetting their breakpoints
  debugger.handle("delallb")

  -- go over all windows and find all breakpoints
  if (not debugger.scratchpad) then
    for _, document in pairs(ide.openDocuments) do
      local editor = document.editor
      local filePath = document.filePath
      local line = editor:MarkerNext(0, BREAKPOINT_MARKER_VALUE)
      while filePath and line ~= -1 do
        debugger.handle("setb " .. filePath .. " " .. (line+1))
        line = editor:MarkerNext(line + 1, BREAKPOINT_MARKER_VALUE)
      end
    end
  end
end

debugger.shell = function(expression, isstatement)
  -- check if the debugger is running and may be waiting for a response.
  -- allow that request to finish, otherwise this function does nothing.
  if debugger.running then debugger.update() end
  if debugger.server and not debugger.running
  and (not debugger.scratchpad or debugger.scratchpad.paused) then
    copas.addthread(function ()
        -- exec command is not expected to return anything.
        -- eval command returns 0 or more results.
        -- 'values' has a list of serialized results returned.
        -- as it is not possible to distinguish between 0 results and one
        -- 'nil' value returned, 'nil' is always returned in this case.
        -- the first value returned by eval command is not used;
        -- this may need to be taken into account by other debuggers.
        local addedret, forceexpression = true, expression:match("^%s*=%s*")
        expression = expression:gsub("^%s*=%s*","")
        local _, values, err = debugger.evaluate(expression)
        if not forceexpression and err then
          _, values, err = debugger.execute(expression)
          addedret = false
        end

        if err then
          if addedret then err = err:gsub('^%[string "return ', '[string "') end
          DisplayShellErr(err)
        elseif addedret or #values > 0 then
          if forceexpression then -- display elements as multi-line
            for i,v in pairs(values) do -- stringify each of the returned values
              local func = loadstring('return '..v) -- deserialize the value first
              if func then -- if it's deserialized correctly
                values[i] = (forceexpression and i > 1 and '\n' or '') ..
                  serialize(func(), {nocode = true, comment = 0,
                    -- if '=' is used, then use multi-line serialized output
                    indent = forceexpression and '  ' or nil})
              end
            end
          end

          -- if empty table is returned, then show nil if this was an expression
          if #values == 0 and (forceexpression or not isstatement) then
            values = {'nil'}
          end
          DisplayShell(unpack(values))
        end

        -- refresh Stack and Watch windows if executed a statement (and no err)
        if isstatement and not err and not addedret and #values == 0 then
          updateStackSync() updateWatchesSync()
        end
      end)
  elseif debugger.server then
    DisplayShellErr(TR("Can't evaluate the expression while the application is running."))
  end
end

local function stoppedAtBreakpoint(file, line)
  -- if this document can be activated and the current line has a breakpoint
  local editor = activateDocument(file, line, activate.CHECKONLY)
  if not editor then return false end

  local current = editor:MarkerNext(0, CURRENT_LINE_MARKER_VALUE)
  local breakpoint = editor:MarkerNext(current, BREAKPOINT_MARKER_VALUE)
  return breakpoint > -1 and breakpoint == current
end

local function mapRemotePath(basedir, file, line, method)
  if not file then return end

  -- file is /foo/bar/my.lua; basedir is d:\local\path\
  -- check for d:\local\path\my.lua, d:\local\path\bar\my.lua, ...
  -- wxwidgets on Windows handles \\ and / as separators, but on OSX
  -- and Linux it only handles 'native' separator;
  -- need to translate for GetDirs to work.
  local file = file:gsub("\\", "/")
  local parts = wx.wxFileName(file):GetDirs()
  local name = wx.wxFileName(file):GetFullName()

  -- find the longest remote path that can be mapped locally
  local longestpath, remotedir
  while true do
    local mapped = GetFullPathIfExists(basedir, name)
    if mapped then
      longestpath = mapped
      remotedir = file:gsub(q(name):gsub("/", ".").."$", "")
    end
    if #parts == 0 then break end
    name = table.remove(parts, #parts) .. "/" .. name
  end

  -- if found a local mapping under basedir
  local activated = longestpath and activateDocument(longestpath, line, method or activate.NOREPORT)
  if activated then
    -- find remote basedir by removing the tail from remote file
    debugger.handle("basedir " .. debugger.basedir .. "\t" .. remotedir)
    -- reset breakpoints again as remote basedir has changed
    reSetBreakpoints()
    DisplayOutputLn(TR("Mapped remote request for '%s' to '%s'.")
      :format(remotedir, debugger.basedir))

    return longestpath
  end

  return nil
end

debugger.listen = function(start)
  if start == false then
    if debugger.listening then
      debugger.terminate() -- terminate if running
      copas.removeserver(debugger.listening)
      DisplayOutputLn(TR("Debugger server stopped at %s:%d.")
        :format(debugger.hostname, debugger.portnumber))
      debugger.listening = false
    else
      DisplayOutputLn(TR("Can't stop debugger server as it is not started."))
    end
    return
  end

  local server, err = socket.bind("*", debugger.portnumber)
  if not server then
    DisplayOutputLn(TR("Can't start debugger server at %s:%d: %s.")
      :format(debugger.hostname, debugger.portnumber, err or TR("unknown error")))
    return
  end
  DisplayOutputLn(TR("Debugger server started at %s:%d.")
    :format(debugger.hostname, debugger.portnumber))

  copas.autoclose = false
  copas.addserver(server, function (skt)
      -- pull any pending data not processed yet
      if debugger.running then debugger.update() end
      if debugger.server then
        DisplayOutputLn(TR("Refused a request to start a new debugging session as there is one in progress already."))
        return
      end

      copas.setErrorHandler(function(error)
        -- ignore errors that happen because debugging session is
        -- terminated during handshake (server == nil in this case).
        if debugger.server then
          DisplayOutputLn(TR("Can't start debugging session due to internal error '%s'."):format(error))
        end
        debugger.terminate()
      end)

      local options = debugger.options or {}
      -- this may be a remote call without using an interpreter and as such
      -- debugger.options may not be set, but runonstart is still configured.
      if not options.runstart then options.runstart = ide.config.debugger.runonstart end

      -- support allowediting as set in the interpreter or config
      if not options.allowediting then options.allowediting = ide.config.debugger.allowediting end

      if not debugger.scratchpad and not options.allowediting then
        SetAllEditorsReadOnly(true)
      end

      debugger.server = copas.wrap(skt)
      debugger.socket = skt
      debugger.loop = false
      debugger.scratchable = false
      debugger.stats = {line = 0}
      debugger.missing = {}
      debugger.editormap = {}
      debugger.runtocursor = nil

      local wxfilepath = GetEditorFileAndCurInfo()
      local startfile = ide:GetProjectStartFile() or options.startwith
        or (wxfilepath and wxfilepath:GetFullPath())

      if not startfile then
        DisplayOutputLn(TR("Can't start debugging without an opened file or with the current file not being saved ('%s').")
          :format(ide.config.default.fullname))
        return debugger.terminate()
      end

      local startpath = wx.wxFileName(startfile):GetPath(wx.wxPATH_GET_VOLUME + wx.wxPATH_GET_SEPARATOR)
      local basedir = options.basedir or FileTreeGetDir() or startpath
      -- guarantee that the path has a trailing separator
      debugger.basedir = wx.wxFileName.DirName(basedir):GetFullPath()

      -- load the remote file into the debugger
      -- set basedir first, before loading to make sure that the path is correct
      debugger.handle("basedir " .. debugger.basedir)

      local init = options.init or ide.config.debugger.init
      if init then
        local _, _, err = debugger.execute(init)
        if err then DisplayOutputLn(TR("Ignored error in debugger initialization code: %s."):format(err)) end
      end

      reSetBreakpoints()

      local redirect = ide.config.debugger.redirect or options.redirect
      if redirect then
        debugger.handle("output stdout " .. redirect, nil,
          { handler = function(m)
              -- if it's an error returned, then handle the error
              if m and m:find("stack traceback:", 1, true) then
                -- this is an error message sent remotely
                local ok, res = LoadSafe("return "..m)
                if ok then
                  DisplayOutputLn(res)
                  return
                end
              end

              if ide.config.debugger.outputfilter then
                local ok, res = pcall(ide.config.debugger.outputfilter, m)
                if ok then
                  m = res
                else
                  DisplayOutputLn("Output filter failed: "..res)
                  return
                end
              elseif m then
                local max = 240
                m = #m < max+4 and m or m:sub(1,max) .. "...\n"
              end
              if m then DisplayOutputNoMarker(m) end
            end})
      end

      if (options.startwith) then
        local file, line, err = debugger.loadfile(options.startwith)
        if err then
          DisplayOutputLn(TR("Can't run the entry point script ('%s').")
            :format(options.startwith)
            .." "..TR("Compilation error")
            ..":\n"..err)
          return debugger.terminate()
        elseif options.runstart and not debugger.scratchpad then
          if stoppedAtBreakpoint(file, line) then
            activateDocument(file, line)
            options.runstart = false
          end
        elseif file and line then
          DisplayOutputLn(TR("Debugging suspended at '%s:%s' (couldn't activate the file).")
            :format(file, line))
        end
      elseif not (options.run or debugger.scratchpad) then
        local file, line, err = debugger.loadfile(startfile)
        -- "load" can work in two ways: (1) it can load the requested file
        -- OR (2) it can "refuse" to load it if the client was started
        -- with start() method, which can't load new files
        -- if file and line are set, this indicates option #2
        if err then
          DisplayOutputLn(TR("Can't start debugging for '%s'."):format(startfile)
            .." "..TR("Compilation error")
            ..":\n"..err)
          return debugger.terminate()
        elseif options.runstart then
          local file = (mapRemotePath(basedir, file, line or 0, activate.CHECKONLY)
            or file or startfile)

          if stoppedAtBreakpoint(file, line or 0) then
            activateDocument(file, line or 0)
            options.runstart = false
          end
        elseif file and line then
          local activated = activateDocument(file, line, activate.NOREPORT)

          -- if not found, check using full file path and reset basedir
          if not activated and not wx.wxIsAbsolutePath(file) then
            activated = activateDocument(startpath..file, line, activate.NOREPORT)
            if activated then
              debugger.basedir = startpath
              debugger.handle("basedir " .. debugger.basedir)
              -- reset breakpoints again as basedir has changed
              reSetBreakpoints()
            end
          end

          -- if not found and the files doesn't exist, it may be
          -- a remote call; try to map it to the project folder.
          -- also check for absolute path as it may need to be remapped
          -- when autoactivation is disabled.
          if not activated and (not wx.wxFileName(file):FileExists()
                                or wx.wxIsAbsolutePath(file)) then
            if mapRemotePath(basedir, file, line, activate.NOREPORT) then
              activated = true
            end
          end

          if not activated then
            DisplayOutputLn(TR("Debugging suspended at '%s:%s' (couldn't activate the file).")
              :format(file, line))
          end

          -- debugger may still be available for scratchpad,
          -- if the interpreter signals scratchpad support, so enable it.
          debugger.scratchable = ide.interpreter.scratchextloop ~= nil
        else
          debugger.scratchable = true
          local activated = activateDocument(startfile, 0) -- find the appropriate line
          if not activated then
            DisplayOutputLn(TR("Debugging suspended at '%s:%s' (couldn't activate the file).")
              :format(startfile, '?'))
          end
        end
      end

      if (not options.noshell and not debugger.scratchpad) then
        ShellSupportRemote(debugger.shell)
      end

      debuggerToggleViews(true)
      updateStackSync()
      updateWatchesSync()

      DisplayOutputLn(TR("Debugging session started in '%s'."):format(debugger.basedir))

      if (debugger.scratchpad) then
        debugger.scratchpad.updated = true
      else
        if (options.runstart) then
          ClearAllCurrentLineMarkers()
          debugger.run()
        end
        if (options.run) then
          local file, line = debugger.handle("run")
          activateDocument(file, line)
        end
      end

      -- refresh toolbar and menus in case the main app is not active
      if ide.config.debugger.requestattention then RequestAttention() end
      ide:GetMainFrame():UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
      ide:GetToolBar():UpdateWindowUI(wx.wxUPDATE_UI_FROMIDLE)
    end)
  debugger.listening = server
end

local function nameOutputTab(name)
  local nbk = ide.frame.bottomnotebook
  local index = nbk:GetPageIndex(ide:GetOutput())
  if index ~= -1 then nbk:SetPageText(index, name) end
end

debugger.handle = function(command, server, options)
  local verbose = ide.config.debugger.verbose
  local osexit, gprint
  osexit, os.exit = os.exit, function () end
  gprint, _G.print = _G.print, function (...)
    if verbose then DisplayOutputLn(...) end
  end

  nameOutputTab(TR("Output (running)"))
  debugger.running = true
  if verbose then DisplayOutputLn("Debugger sent (command):", command) end
  local file, line, err = mobdebug.handle(command, server or debugger.server, options)
  if verbose then DisplayOutputLn("Debugger received (file, line, err):", file, line, err) end
  debugger.running = false
  -- only set suspended if the debugging hasn't been terminated
  if debugger.server then nameOutputTab(TR("Output (suspended)")) end

  os.exit = osexit
  _G.print = gprint
  return file, line, err
end

debugger.exec = function(command, func)
  if debugger.server and not debugger.running then
    copas.addthread(function ()
        -- execute a custom function (if any) in the context of this thread
        if type(func) == 'function' then func() end
        local out
        local attempts = 0
        while true do
          -- clear markers before running the command
          -- don't clear if running trace as the marker is then invisible,
          -- and it needs to be visible during tracing
          if not debugger.loop then ClearAllCurrentLineMarkers() end
          debugger.breaking = false
          local file, line, err = debugger.handle(out or command)
          if out then out = nil end
          if line == nil then
            if err then DisplayOutputLn(err) end
            DebuggerStop()
            return
          elseif not debugger.server then
            -- it is possible that while debugger.handle call was executing
            -- the debugging was terminated; simply return in this case.
            return
          else
            if activateDocument(file, line) then
              debugger.stats.line = debugger.stats.line + 1
              if debugger.loop then
                updateStackSync()
                updateWatchesSync()
              else
                updateStackAndWatches()
                return
              end
            else
              -- clear the marker as it wasn't cleared earlier
              if debugger.loop then ClearAllCurrentLineMarkers() end
              -- we may be in some unknown location at this point;
              -- If this happens, stop and report allowing users to set
              -- breakpoints and step through.
              if debugger.breaking then
                DisplayOutputLn(TR("Debugging suspended at '%s:%s' (couldn't activate the file).")
                  :format(file, line))
                updateStackAndWatches()
                return
              end
              -- redo now; if the call is from the debugger, then repeat
              -- the same command, except when it was "run" (switch to 'step');
              -- this is needed to "break" execution that happens in on() call.
              -- in all other cases get out of this file.
              -- don't get out of "mobdebug", because it may happen with
              -- start() or on() call, which will get us out of the current
              -- file, which is not what we want.
              -- Some engines (Corona SDK) report =?:0 as the current location.
              -- repeat the same command, but check if this has been tried
              -- too many times already; if so, get "out"
              out = ((tonumber(line) == 0 and attempts < 10) and command
                or (file:find('mobdebug%.lua$')
                  and (command == 'run' and 'step' or command) or "out"))
              attempts = attempts + 1
            end
          end
        end
      end)
  end
end

debugger.handleAsync = function(command)
  if debugger.server and not debugger.running then
    copas.addthread(function () debugger.handle(command) end)
  end
end
debugger.handleDirect = function(command)
  local sock = debugger.socket
  if debugger.server and sock then
    local running = debugger.running
    -- this needs to be short as it will block the UI
    sock:settimeout(0.25)
    debugger.handle(command, sock)
    sock:settimeout(0)
    -- restore running status
    debugger.running = running
  end
end

debugger.loadfile = function(file)
  return debugger.handle("load " .. file)
end
debugger.loadstring = function(file, string)
  return debugger.handle("loadstring '" .. file .. "' " .. string)
end

do
  local nextupdatedelta = 0.250
  local nextupdate = TimeGet() + nextupdatedelta
  local function forceUpdateOnWrap(editor)
    -- http://www.scintilla.org/ScintillaDoc.html#LineWrapping
    -- Scintilla doesn't perform wrapping immediately after a content change
    -- for performance reasons, so the activation calculations can be wrong
    -- if there is wrapping that pushes the current line out of the screen.
    -- force editor update that performs wrapping recalculation.
    if ide.config.editor.usewrap then editor:Update(); editor:Refresh() end
  end
  debugger.update = function()
    if debugger.server or debugger.listening and TimeGet() > nextupdate then
      copas.step(0)
      nextupdate = TimeGet() + nextupdatedelta
    end

    -- if there is any pending activation
    if debugger.activate then
      local file, line, content = unpack(debugger.activate)
      if content then
        local editor = NewFile()
        editor:SetText(content)
        if not ide.config.debugger.allowediting
        and not (debugger.options or {}).allowediting then
          editor:SetReadOnly(true)
        end
        forceUpdateOnWrap(editor)
        activateDocument(file, line)
      else
        local editor = LoadFile(file)
        if editor then
          forceUpdateOnWrap(editor)
          activateDocument(file, line)
        end
      end
      debugger.activate = nil
    end
  end
end

local function isemptyline(editor, line)
  local text = editor:GetLine(line-1)
  return not text:find("%S")
  or (text:find("^%s*%-%-") ~= nil and text:find("^%s*%-%-%[=*%[") == nil)
end

debugger.terminate = function()
  if debugger.server then
    if debugger.pid then -- if there is PID, try local kill
      killClient()
    else -- otherwise, try graceful exit for the remote process
      debugger.breaknow("exit")
    end
    DebuggerStop()
  end
end
debugger.step = function() debugger.exec("step") end
debugger.trace = function()
  debugger.loop = true
  debugger.exec("step")
end
debugger.runto = function(editor, line)
  -- check if the location is valid for a breakpoint
  if isemptyline(editor, line+1) then return end

  local ed, ln = unpack(debugger.runtocursor or {})
  local same = ed and ln and ed:GetId() == editor:GetId() and ln == line

  -- check if there is already a breakpoint in the "run to" location;
  -- if so, don't mark the location as "run to" as it will stop there anyway
  if bit.band(editor:MarkerGet(line), BREAKPOINT_MARKER_VALUE) > 0
  and not same then
    debugger.runtocursor = nil
    debugger.run()
    return
  end

  -- save the location of the breakpoint
  debugger.runtocursor = {editor, line}
  -- set breakpoint and execute run
  debugger.exec("run", function()
      -- if run-to-cursor location is already set, then remove the breakpoint,
      -- but only if this location is different
      if ed and ln and not same then
        DebuggerToggleBreakpoint(ed, ln)
        debugger.wait()
      end
      if not same then
        DebuggerToggleBreakpoint(editor, line)
        debugger.wait()
      end
    end)
end
debugger.wait = function()
  -- wait for all results to come back
  while debugger.running do debugger.update() end
end
debugger.over = function() debugger.exec("over") end
debugger.out = function() debugger.exec("out") end
debugger.run = function() debugger.exec("run") end
debugger.detach = function()
  if debugger.running then
    debugger.handleDirect("done")
    debugger.server = nil
  else
    debugger.exec("done")
  end
end
debugger.evaluate = function(expression) return debugger.handle('eval ' .. expression) end
debugger.execute = function(expression) return debugger.handle('exec ' .. expression) end
debugger.stack = function() return debugger.handle('stack') end
debugger.breaknow = function(command)
  -- stop if we're running a "trace" command
  debugger.loop = false

  -- force suspend command; don't use copas interface as it checks
  -- for the other side "reading" and the other side is not reading anything.
  -- use the "original" socket to send "suspend" command.
  -- this will only break on the next Lua command.
  if debugger.socket then
    local running = debugger.running
    -- this needs to be short as it will block the UI
    debugger.socket:settimeout(0.25)
    local file, line, err = debugger.handle(command or "suspend", debugger.socket)
    debugger.socket:settimeout(0)
    -- restore running status
    debugger.running = running
    debugger.breaking = true
    -- don't need to do anything else as the earlier call (run, step, etc.)
    -- will get the results (file, line) back and will update the UI
    return file, line, err
  end
end
debugger.breakpoint = function(file, line, state)
  if debugger.running then
    return debugger.handleDirect((state and "asetb " or "adelb ") .. file .. " " .. line)
  end
  return debugger.handleAsync((state and "setb " or "delb ") .. file .. " " .. line)
end
debugger.quickeval = function(var, callback)
  if debugger.server and not debugger.running
  and not debugger.scratchpad and not (debugger.options or {}).noeval then
    copas.addthread(function ()
      local _, values, err = debugger.evaluate(var)
      local val = err
        and err:gsub("%[.-%]:%d+:%s*","error: ")
        or (var .. " = " .. (#values > 0 and values[1] or 'nil'))
      if callback then callback(val) end
    end)
  end
end

local width, height = 360, 200

local keyword = {}
for _,k in ipairs({'and', 'break', 'do', 'else', 'elseif', 'end', 'false',
  'for', 'function', 'goto', 'if', 'in', 'local', 'nil', 'not', 'or', 'repeat',
  'return', 'then', 'true', 'until', 'while'}) do keyword[k] = true end

local function stringifyKeyIntoPrefix(name, num)
  return (type(name) == "number"
    and (num and num == name and '' or ("[%s] = "):format(name))
    or type(name) == "string" and (name:match("^[%l%u_][%w_]*$") and not keyword[name]
      and ("%s = "):format(name)
      or ("[%q] = "):format(name))
    or ("[%s] = "):format(tostring(name)))
end

local function debuggerCreateStackWindow()
  local stackCtrl = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(width, height),
    wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE
    + wx.wxTR_HIDE_ROOT + wx.wxNO_BORDER)

  debugger.stackCtrl = stackCtrl

  stackCtrl:SetImageList(debugger.imglist)

  local valuecache = {}
  function stackCtrl:SetItemValueIfExpandable(item, value)
    local expandable = type(value) == 'table' and next(value) ~= nil
    if expandable then -- cache table value to expand when requested
      valuecache[item:GetValue()] = value
    end
    self:SetItemHasChildren(item, expandable)
  end

  function stackCtrl:DeleteAll()
    self:DeleteAllItems()
    valuecache = {}
  end

  function stackCtrl:GetItemChildren(item)
    return valuecache[item:GetValue()] or {}
  end

  stackCtrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_EXPANDING,
    function (event)
      local item_id = event:GetItem()
      local count = stackCtrl:GetChildrenCount(item_id, false)
      if count > 0 then return true end

      local image = stackCtrl:GetItemImage(item_id)
      local num = 1
      for name,value in pairs(stackCtrl:GetItemChildren(item_id)) do
        local strval = fixUTF8(trimToMaxLength(serialize(value, params)))
        local text = stringifyKeyIntoPrefix(name, num)..strval
        local item = stackCtrl:AppendItem(item_id, text, image)
        stackCtrl:SetItemValueIfExpandable(item, value)

        num = num + 1
        if num > stackmaxnum then break end
      end
      return true
    end)

  stackCtrl:Connect(wx.wxEVT_SET_FOCUS, function(event)
      if debugger.needrefresh.stack then
        updateStack()
        debugger.needrefresh.stack = false
      end
    end)

  -- register navigation callback
  stackCtrl:Connect(wx.wxEVT_LEFT_DCLICK, function (event)
    local item_id = stackCtrl:HitTest(event:GetPosition())
    if not item_id or not item_id:IsOk() then event:Skip() return end

    local coords = callData[item_id:GetValue()]
    if not coords then event:Skip() return end

    local file, line = coords[1], coords[2]
    if file:match("@") then file = string.sub(file, 2) end
    file = GetFullPathIfExists(debugger.basedir, file)
    if file then
      local editor = LoadFile(file,nil,true)
      editor:SetFocus()
      if line then editor:GotoLine(line-1) end
    end
  end)

  local layout = ide:GetSetting("/view", "uimgrlayout")
  if layout and not layout:find("stackpanel") then
    ide:AddPanelDocked(ide.frame.bottomnotebook, stackCtrl, "stackpanel", TR("Stack"))
  else
    ide:AddPanel(stackCtrl, "stackpanel", TR("Stack"))
  end
end

local function debuggerCreateWatchWindow()
  local watchCtrl = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(width, height),
    wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE
    + wx.wxTR_HIDE_ROOT + wx.wxTR_EDIT_LABELS + wx.wxNO_BORDER)

  debugger.watchCtrl = watchCtrl

  local root = watchCtrl:AddRoot("Watch")
  watchCtrl:SetImageList(debugger.imglist)

  local defaultExpr = "watch expression"
  local expressions = {} -- table to keep track of expressions

  function watchCtrl:SetItemExpression(item, expr, value)
    expressions[item:GetValue()] = expr
    self:SetItemText(item, expr .. ' = ' .. (value or '?'))
    self:SelectItem(item, true)
    if not value then updateWatches(item) end
  end

  function watchCtrl:GetItemExpression(item)
    return expressions[item:GetValue()]
  end

  local names = {}
  function watchCtrl:SetItemName(item, name)
    local nametype = type(name)
    names[item:GetValue()] = (
      (nametype == 'string' or nametype == 'number' or nametype == 'boolean')
      and name or nil
    )
  end

  function watchCtrl:GetItemName(item)
    return names[item:GetValue()]
  end

  local valuecache = {}
  function watchCtrl:SetItemValueIfExpandable(item, value)
    local expandable = type(value) == 'table' and next(value) ~= nil
    valuecache[item:GetValue()] = expandable and value or nil
    self:SetItemHasChildren(item, expandable)
  end

  function watchCtrl:GetItemChildren(item)
    return valuecache[item:GetValue()] or {}
  end

  function watchCtrl:IsWatch(item)
    return item:IsOk() and watchCtrl:GetItemParent(item):GetValue() == root:GetValue()
  end

  function watchCtrl:IsEditable(item)
    return (item and item:IsOk()
      and (watchCtrl:IsWatch(item) or watchCtrl:GetItemName(item) ~= nil))
  end

  function watchCtrl:GetItemFullExpression(item)
    local expr = ''
    while true do
      local name = watchCtrl:GetItemName(item)
      expr = (watchCtrl:IsWatch(item)
        and ('({%s})[1]'):format(watchCtrl:GetItemExpression(item))
        or (type(name) == 'string' and '[%q]' or '[%s]'):format(tostring(name))
      )..expr
      if watchCtrl:IsWatch(item) then break end
      item = watchCtrl:GetItemParent(item)
      if not item:IsOk() then break end
    end
    return expr, item:IsOk() and item or nil
  end

  function watchCtrl:CopyItemValue(item)
    local expr = self:GetItemFullExpression(item)

    if debugger.running then debugger.update() end
    if debugger.server and not debugger.running
    and (not debugger.scratchpad or debugger.scratchpad.paused) then
      copas.addthread(function ()
        local _, values, error = debugger.evaluate(expr)
        ide:CopyToClipboard(error and error:gsub("%[.-%]:%d+:%s+","")
          or (#values == 0 and 'nil' or values[1]))
      end)
    end
  end

  function watchCtrl:UpdateItemValue(item, value)
    local expr, itemupd = self:GetItemFullExpression(item)

    if debugger.running then debugger.update() end
    if debugger.server and not debugger.running
    and (not debugger.scratchpad or debugger.scratchpad.paused) then
      copas.addthread(function ()
        local _, _, err = debugger.execute(expr..'='..value)
        if err then
          watchCtrl:SetItemText(item, 'error: '..err:gsub("%[.-%]:%d+:%s+",""))
        elseif itemupd then
          updateWatchesSync(itemupd)
        end
        updateStackSync()
      end)
    end
  end

  watchCtrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_EXPANDING,
    function (event)
      local item_id = event:GetItem()
      local count = watchCtrl:GetChildrenCount(item_id, false)
      if count > 0 then return true end

      local image = watchCtrl:GetItemImage(item_id)
      local num = 1
      for name,value in pairs(watchCtrl:GetItemChildren(item_id)) do
        local strval = fixUTF8(trimToMaxLength(serialize(value, params)))
        local text = stringifyKeyIntoPrefix(name, num)..strval
        local item = watchCtrl:AppendItem(item_id, text, image)
        watchCtrl:SetItemValueIfExpandable(item, value)
        watchCtrl:SetItemName(item, name)

        num = num + 1
        if num > stackmaxnum then break end
      end
      return true
    end)

  watchCtrl:Connect(wx.wxEVT_COMMAND_TREE_DELETE_ITEM,
    function (event)
      local value = event:GetItem():GetValue()
      expressions[value] = nil
      valuecache[value] = nil
      names[value] = nil
    end)

  watchCtrl:Connect(wx.wxEVT_SET_FOCUS, function(event)
      if debugger.needrefresh.watches then
        updateWatches()
        debugger.needrefresh.watches = false
      end
    end)


  local item
  -- wx.wxEVT_CONTEXT_MENU is only triggered over tree items on OSX,
  -- but it needs to be also triggered below any item to add a watch,
  -- so use RIGHT_DOWN instead
  watchCtrl:Connect(wx.wxEVT_RIGHT_DOWN,
    function (event)
      -- store the item to be used in edit/delete actions
      item = watchCtrl:HitTest(watchCtrl:ScreenToClient(wx.wxGetMousePosition()))
      local editlabel = watchCtrl:IsWatch(item) and TR("&Edit Watch") or TR("&Edit Value")
      watchCtrl:PopupMenu(wx.wxMenu {
        { ID_ADDWATCH, TR("&Add Watch")..KSC(ID_ADDWATCH) },
        { ID_EDITWATCH, editlabel..KSC(ID_EDITWATCH) },
        { ID_DELETEWATCH, TR("&Delete Watch")..KSC(ID_DELETEWATCH) },
        { ID_COPYWATCHVALUE, TR("&Copy Value")..KSC(ID_COPYWATCHVALUE) },
      })
      item = nil
    end)

  watchCtrl:Connect(ID_ADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event) watchCtrl:EditLabel(watchCtrl:AppendItem(root, defaultExpr, image.LOCAL)) end)

  watchCtrl:Connect(ID_EDITWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event) watchCtrl:EditLabel(item or watchCtrl:GetSelection()) end)
  watchCtrl:Connect(ID_EDITWATCH, wx.wxEVT_UPDATE_UI,
    function (event) event:Enable(watchCtrl:IsEditable(item or watchCtrl:GetSelection())) end)

  watchCtrl:Connect(ID_DELETEWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event) watchCtrl:Delete(item or watchCtrl:GetSelection()) end)
  watchCtrl:Connect(ID_DELETEWATCH, wx.wxEVT_UPDATE_UI,
    function (event) event:Enable(watchCtrl:IsWatch(item or watchCtrl:GetSelection())) end)

  watchCtrl:Connect(ID_COPYWATCHVALUE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event) watchCtrl:CopyItemValue(item or watchCtrl:GetSelection()) end)
  watchCtrl:Connect(ID_COPYWATCHVALUE, wx.wxEVT_UPDATE_UI, function (event)
    -- allow copying only when the debugger is available
    event:Enable(item:IsOk() and debugger.server and not debugger.running
     and (not debugger.scratchpad or debugger.scratchpad.paused))
  end)

  local label
  watchCtrl:Connect(wx.wxEVT_COMMAND_TREE_BEGIN_LABEL_EDIT,
    function (event)
      local item = event:GetItem()
      if not (item:IsOk() and watchCtrl:IsEditable(item)) then
        event:Veto()
        return
      end

      label = watchCtrl:GetItemText(item)

      if watchCtrl:IsWatch(item) then
        local expr = watchCtrl:GetItemExpression(item)
        if expr then watchCtrl:SetItemText(item, expr) end
      else
        local prefix = stringifyKeyIntoPrefix(watchCtrl:GetItemName(item))
        local val = watchCtrl:GetItemText(item):gsub(q(prefix),'')
        watchCtrl:SetItemText(item, val)
      end
    end)
  watchCtrl:Connect(wx.wxEVT_COMMAND_TREE_END_LABEL_EDIT,
    function (event)
      event:Veto()

      local item = event:GetItem()
      if event:IsEditCancelled() then
        if watchCtrl:GetItemText(item) == defaultExpr then
          -- when Delete is called from END_EDIT, it causes infinite loop
          -- on OSX (wxwidgets 2.9.5) as Delete calls END_EDIT again.
          -- disable handlers during Delete and then enable back.
          watchCtrl:SetEvtHandlerEnabled(false)
          watchCtrl:Delete(item)
          watchCtrl:SetEvtHandlerEnabled(true)
        else
          watchCtrl:SetItemText(item, label)
        end
      else
        if watchCtrl:IsWatch(item) then
          watchCtrl:SetItemExpression(item, event:GetLabel())
        else
          watchCtrl:UpdateItemValue(item, event:GetLabel())
        end
      end
      event:Skip()
    end)

  local layout = ide:GetSetting("/view", "uimgrlayout")
  if layout and not layout:find("watchpanel") then
    ide:AddPanelDocked(ide.frame.bottomnotebook, watchCtrl, "watchpanel", TR("Watch"))
  else
    ide:AddPanel(watchCtrl, "watchpanel", TR("Watch"))
  end
end

debuggerCreateStackWindow()
debuggerCreateWatchWindow()

----------------------------------------------
-- public api

DebuggerRefreshPanels = updateStackAndWatches

function DebuggerAttachDefault(options)
  debugger.options = options
  if (debugger.listening) then return end
  debugger.listen()
end

function DebuggerShutdown()
  if debugger.server then debugger.terminate() end
  if debugger.pid then killClient() end
end

function DebuggerStop(resetpid)
  if (debugger.server) then
    debugger.server = nil
    SetAllEditorsReadOnly(false)
    ShellSupportRemote(nil)
    ClearAllCurrentLineMarkers()
    DebuggerScratchpadOff()
    debuggerToggleViews(false)
    local lines = TR("traced %d instruction", debugger.stats.line):format(debugger.stats.line)
    DisplayOutputLn(TR("Debugging session completed (%s)."):format(lines))
    nameOutputTab(debugger.pid and TR("Output (running)") or TR("Output"))
    if debugger.runtocursor then
      local ed, ln = unpack(debugger.runtocursor)
      DebuggerToggleBreakpoint(ed, ln)
    end
  else
    -- it's possible that the application couldn't start, or that the
    -- debugger in the application didn't start, which means there is
    -- no debugger.server, but scratchpad may still be on. Turn it off.
    DebuggerScratchpadOff()
  end
  -- reset pid for "running" (not debugged) processes
  if resetpid then debugger.pid = nil end
end

local function debuggerMakeFileName(editor)
  return ide:GetDocument(editor):GetFilePath()
  or ide:GetDocument(editor):GetFileName()
  or ide.config.default.fullname
end

function DebuggerToggleBreakpoint(editor, line)
  local filePath = debugger.editormap and debugger.editormap[editor]
    or debuggerMakeFileName(editor)
  if bit.band(editor:MarkerGet(line), BREAKPOINT_MARKER_VALUE) > 0 then
    -- if there is pending "run-to-cursor" call at this location, remove it
    local ed, ln = unpack(debugger.runtocursor or {})
    local same = ed and ln and ed:GetId() == editor:GetId() and ln == line
    if same then debugger.runtocursor = nil end

    editor:MarkerDelete(line, BREAKPOINT_MARKER)
    if debugger.server then debugger.breakpoint(filePath, line+1, false) end
  else
    if isemptyline(editor, line+1) then return end

    editor:MarkerAdd(line, BREAKPOINT_MARKER)
    if debugger.server then debugger.breakpoint(filePath, line+1, true) end
  end
end

-- scratchpad functions

function DebuggerRefreshScratchpad()
  if debugger.scratchpad and debugger.scratchpad.updated and not debugger.scratchpad.paused then

    local scratchpadEditor = debugger.scratchpad.editor
    if scratchpadEditor.spec.apitype
    and scratchpadEditor.spec.apitype == "lua"
    and not ide.interpreter.skipcompile
    and not CompileProgram(scratchpadEditor, { jumponerror = false, reportstats = false })
    then return end

    local code = StripShebang(scratchpadEditor:GetText())
    if debugger.scratchpad.running then
      -- break the current execution first
      -- don't try too frequently to avoid overwhelming the debugger
      local now = TimeGet()
      if now - debugger.scratchpad.running > 0.250 then
        debugger.breaknow()
        debugger.scratchpad.running = now
      end
    else
      local clear = ide:GetMenuBar():IsChecked(ID_CLEAROUTPUT)
      local filePath = debuggerMakeFileName(scratchpadEditor)

      -- wrap into a function call to make "return" to work with scratchpad
      code = "(function()"..code.."\nend)()"

      -- this is a special error message that is generated at the very end
      -- of each script to avoid exiting the (debugee) scratchpad process.
      -- these errors are handled and not reported to the user
      local errormsg = 'execution suspended at ' .. TimeGet()
      local stopper = "error('" .. errormsg .. "')"
      -- store if interpreter requires a special handling for external loop
      local extloop = ide.interpreter.scratchextloop

      local function reloadScratchpadCode()
        debugger.scratchpad.running = TimeGet()
        debugger.scratchpad.updated = false
        debugger.scratchpad.runs = (debugger.scratchpad.runs or 0) + 1

        if clear then ClearOutput(true) end

        -- the code can be running in two ways under scratchpad:
        -- 1. controlled by the application, requires stopper (most apps)
        -- 2. controlled by some external loop (for example, love2d).
        -- in the first case we need to reload the app after each change
        -- in the second case, we need to load the app once and then
        -- "execute" new code to reflect the changes (with some limitations).
        local _, _, err
        if extloop then -- if the execution is controlled by an external loop
          if debugger.scratchpad.runs == 1
          then _, _, err = debugger.loadstring(filePath, code)
          else _, _, err = debugger.execute(code) end
        else   _, _, err = debugger.loadstring(filePath, code .. stopper) end

        -- when execute() is used, it's not possible to distinguish between
        -- compilation and run-time error, so just report as "Scratchpad error"
        local prefix = extloop and TR("Scratchpad error") or TR("Compilation error")

        if not err then
          _, _, err = debugger.handle("run")
          prefix = TR("Execution error")
        end
        if err and not err:find(errormsg) then
          local fragment, line = err:match('.-%[string "([^\010\013]+)"%]:(%d+)%s*:')
          -- make the code shorter to better see the error message
          if prefix == TR("Scratchpad error") and fragment and #fragment > 30 then
            err = err:gsub(q(fragment), function(s) return s:sub(1,30)..'...' end)
          end
          DisplayOutputLn(prefix
            ..(line and (" "..TR("on line %d"):format(line)) or "")
            ..":\n"..err:gsub('stack traceback:.+', ''):gsub('\n+$', ''))
        end
        debugger.scratchpad.running = false
      end

      copas.addthread(reloadScratchpadCode)
    end
  end
end

local numberStyle = wxstc.wxSTC_LUA_NUMBER

function DebuggerScratchpadOn(editor)
  -- first check if there is already scratchpad editor.
  -- this may happen when more than one editor is being added...

  if debugger.scratchpad and debugger.scratchpad.editors then
    debugger.scratchpad.editors[editor] = true
  else
    debugger.scratchpad = {editor = editor, editors = {[editor] = true}}

    -- check if the debugger is already running; this happens when
    -- scratchpad is turned on after external script has connected
    if debugger.server then
      debugger.scratchpad.updated = true
      ClearAllCurrentLineMarkers()
      SetAllEditorsReadOnly(false)
      ShellSupportRemote(nil) -- disable remote shell
      DebuggerRefreshScratchpad()
    elseif not ProjectDebug(true, "scratchpad") then
      debugger.scratchpad = nil
      return
    end
  end

  local scratchpadEditor = editor
  scratchpadEditor:StyleSetUnderline(numberStyle, true)
  debugger.scratchpad.margin = scratchpadEditor:GetMarginWidth(0) +
    scratchpadEditor:GetMarginWidth(1) + scratchpadEditor:GetMarginWidth(2)

  scratchpadEditor:Connect(wxstc.wxEVT_STC_MODIFIED, function(event)
    local evtype = event:GetModificationType()
    if (bit.band(evtype,wxstc.wxSTC_MOD_INSERTTEXT) ~= 0 or
        bit.band(evtype,wxstc.wxSTC_MOD_DELETETEXT) ~= 0 or
        bit.band(evtype,wxstc.wxSTC_PERFORMED_UNDO) ~= 0 or
        bit.band(evtype,wxstc.wxSTC_PERFORMED_REDO) ~= 0) then
      debugger.scratchpad.updated = true
      debugger.scratchpad.editor = scratchpadEditor
    end
    event:Skip()
  end)

  scratchpadEditor:Connect(wx.wxEVT_LEFT_DOWN, function(event)
    local scratchpad = debugger.scratchpad

    local point = event:GetPosition()
    local pos = scratchpadEditor:PositionFromPoint(point)

    -- are we over a number in the scratchpad? if not, it's not our event
    if ((not scratchpad) or
        (bit.band(scratchpadEditor:GetStyleAt(pos),31) ~= numberStyle)) then
      event:Skip()
      return
    end

    -- find start position and length of the number
    local text = scratchpadEditor:GetText()

    local nstart = pos
    while nstart >= 0
      and (bit.band(scratchpadEditor:GetStyleAt(nstart),31) == numberStyle)
      do nstart = nstart - 1 end

    local nend = pos
    while nend < string.len(text)
      and (bit.band(scratchpadEditor:GetStyleAt(nend),31) == numberStyle)
      do nend = nend + 1 end

    -- check if there is minus sign right before the number and include it
    if nstart >= 0 and scratchpadEditor:GetTextRange(nstart,nstart+1) == '-' then 
      nstart = nstart - 1
    end
    scratchpad.start = nstart + 1
    scratchpad.length = nend - nstart - 1
    scratchpad.origin = scratchpadEditor:GetTextRange(nstart+1,nend)
    if tonumber(scratchpad.origin) then
      scratchpad.point = point
      scratchpadEditor:CaptureMouse()
    end
  end)

  scratchpadEditor:Connect(wx.wxEVT_LEFT_UP, function(event)
    if debugger.scratchpad and debugger.scratchpad.point then
      debugger.scratchpad.point = nil
      scratchpadEditor:ReleaseMouse()
      wx.wxSetCursor(wx.wxNullCursor) -- restore cursor
    else event:Skip() end
  end)

  scratchpadEditor:Connect(wx.wxEVT_MOTION, function(event)
    local point = event:GetPosition()
    local pos = scratchpadEditor:PositionFromPoint(point)
    local scratchpad = debugger.scratchpad
    local ipoint = scratchpad and scratchpad.point

    -- record the fact that we are over a number or dragging slider
    scratchpad.over = scratchpad and
      (ipoint ~= nil or (bit.band(scratchpadEditor:GetStyleAt(pos),31) == numberStyle))

    if ipoint then
      local startpos = scratchpad.start
      local endpos = scratchpad.start+scratchpad.length

      -- calculate difference in point position
      local dx = point.x - ipoint.x

      -- calculate the number of decimal digits after the decimal point
      local origin = scratchpad.origin
      local decdigits = #(origin:match('%.(%d+)') or '')

      -- calculate new value
      local value = tonumber(origin) + dx * 10^-decdigits

      -- convert new value back to string to check the number of decimal points
      -- this is needed because the rate of change is determined by the
      -- current value. For example, for number 1, the next value is 2,
      -- but for number 1.1, the next is 1.2 and for 1.01 it is 1.02.
      -- But if 1.01 becomes 1.00, the both zeros after the decimal point
      -- need to be preserved to keep the increment ratio the same when
      -- the user wants to release the slider and start again.
      origin = tostring(value)
      local newdigits = #(origin:match('%.(%d+)') or '')
      if decdigits ~= newdigits then
        origin = origin .. (origin:find('%.') and '' or '.') .. ("0"):rep(decdigits-newdigits)
      end

      -- update length
      scratchpad.length = #origin

      -- update the value in the document
      scratchpadEditor:SetTargetStart(startpos)
      scratchpadEditor:SetTargetEnd(endpos)
      scratchpadEditor:ReplaceTarget(origin)
    else event:Skip() end
  end)

  scratchpadEditor:Connect(wx.wxEVT_SET_CURSOR, function(event)
    if (debugger.scratchpad and debugger.scratchpad.over) then
      event:SetCursor(wx.wxCursor(wx.wxCURSOR_SIZEWE))
    elseif debugger.scratchpad and ide.osname == 'Unix' then
      -- restore the cursor manually on Linux since event:Skip() doesn't reset it
      local ibeam = event:GetX() > debugger.scratchpad.margin
      event:SetCursor(wx.wxCursor(ibeam and wx.wxCURSOR_IBEAM or wx.wxCURSOR_RIGHT_ARROW))
    else event:Skip() end
  end)

  return true
end

function DebuggerScratchpadOff()
  if not debugger.scratchpad then return end

  for scratchpadEditor in pairs(debugger.scratchpad.editors) do
    scratchpadEditor:StyleSetUnderline(numberStyle, false)
    scratchpadEditor:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wxstc.wxEVT_STC_MODIFIED)
    scratchpadEditor:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_MOTION)
    scratchpadEditor:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_LEFT_DOWN)
    scratchpadEditor:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_LEFT_UP)
    scratchpadEditor:Disconnect(wx.wxID_ANY, wx.wxID_ANY, wx.wxEVT_SET_CURSOR)
  end

  wx.wxSetCursor(wx.wxNullCursor) -- restore cursor

  debugger.scratchpad = nil
  debugger.terminate()

  -- disable menu if it is still enabled
  -- (as this may be called when the debugger is being shut down)
  local menuBar = ide.frame.menuBar
  if menuBar:IsChecked(ID_RUNNOW) then menuBar:Check(ID_RUNNOW, false) end

  return true
end
