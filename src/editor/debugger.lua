-- Integration with MobDebug
-- Copyright Paul Kulchenko 2011
-- Original authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)

local copas = require "copas"
local socket = require "socket"
local mobdebug = require "mobdebug"

local ide = ide
local debugger = ide.debugger
debugger.server = nil -- DebuggerServer object when debugging, else nil
debugger.running = false -- true when the debuggee is running
debugger.listening = false -- true when the debugger is listening for a client
debugger.portnumber = 8171 -- the port # to use for debugging
debugger.watchWindow = nil -- the watchWindow, nil when not created
debugger.watchListCtrl = nil -- the child listctrl in the watchWindow

local notebook = ide.frame.notebook

local function updateWatchesSync()
  local watchListCtrl = debugger.watchListCtrl
  if watchListCtrl and debugger.server and not debugger.running then
    for idx = 0, watchListCtrl:GetItemCount() - 1 do
      local expression = watchListCtrl:GetItemText(idx)
      local value, _, error = debugger.evaluate(expression)
      watchListCtrl:SetItem(idx, 1, value or ('error: ' .. error))
    end
  end
end

local function updateWatches()
  if debugger.watchListCtrl and debugger.server and not debugger.running then
    copas.addthread(updateWatchesSync)
  end
end

local function activateDocument(fileName, line)
  if (not fileName and line) then return end

  if not wx.wxIsAbsolutePath(fileName) then
    fileName = wx.wxGetCwd().."/"..fileName
  end

  if wx.__WXMSW__ then
    fileName = wx.wxUnix2DosFilename(fileName)
  end

  local fileFound = false
  for id, document in pairs(ide.openDocuments) do
    local editor = document.editor
    -- for running in cygwin, use same type of separators
    filePath = string.gsub(document.filePath, "\\", "/")
    local fileName = string.gsub(fileName, "\\", "/")
    if string.upper(filePath) == string.upper(fileName) then
      local selection = document.index
      notebook:SetSelection(selection)
      SetEditorSelection(selection)
      ClearAllCurrentLineMarkers()
      editor:MarkerAdd(line-1, CURRENT_LINE_MARKER)
      editor:EnsureVisibleEnforcePolicy(line-1)
      fileFound = true
      break
    end
  end

  return fileFound
end

debugger.shell = function(expression)
  if debugger.server and not debugger.running then
    copas.addthread(function ()
        local addedret = false
        local value, _, err = debugger.handle('exec ' .. expression)
        if err and (err:find("'=' expected near '<eof>'") or
                    err:find("unexpected symbol near '")) then
          value, _, err = debugger.handle('eval ' .. expression:gsub("^%s*=%s*",""))
          addedret = true
        end

        if err then
          if addedret then err = err:gsub('^%[string "return ', '[string "') end
          DisplayShellErr(err)
        elseif addedret or (value ~= nil and value ~= 'nil') then
          DisplayShell(value)
        end
      end)
  end
end

debugger.listen = function()
  local server = socket.bind("*", debugger.portnumber)
  DisplayOutput("Started debugger server; clients can connect to "..wx.wxGetHostName()..":"..debugger.portnumber..".\n")
  copas.autoclose = false
  copas.addserver(server, function (skt)
      SetAllEditorsReadOnly(true)
      local options = debugger.options or {}
      local wxfilepath = GetEditorFileAndCurInfo()
      local startfile = options.startfile or wxfilepath:GetFullPath()
      local basedir = options.basedir
        or FileTreeGetDir()
        or wxfilepath:GetPath(wx.wxPATH_GET_VOLUME)..string_Pathsep
      debugger.basedir = basedir
      debugger.server = copas.wrap(skt)
      debugger.socket = skt
      debugger.loop = false

      -- load the remote file into the debugger
      -- set basedir first, before loading to make sure that the path is correct
      debugger.handle("basedir " .. debugger.basedir)

      -- remove all breakpoints that may still be present from the last session
      -- this only matters for those remote clients that reload scripts
      -- without resetting their breakpoints
      debugger.handle("delallb")

      -- go over all windows and find all breakpoints
      for id, document in pairs(ide.openDocuments) do
        local editor = document.editor
        local filePath = document.filePath
        line = editor:MarkerNext(0, BREAKPOINT_MARKER_VALUE)
        while line ~= -1 do
          debugger.handle("setb " .. filePath .. " " .. (line+1))
          line = editor:MarkerNext(line + 1, BREAKPOINT_MARKER_VALUE)
        end
      end

      if (options.run) then
        activateDocument(debugger.handle("run"))
      else
        local file, line = debugger.handle("load " .. startfile)
        -- "load" can work in two ways: (1) it can load the requested file
        -- OR (2) it can "refuse" to load it if the client was started
        -- with start() method, which can't load new files
        -- if file and line are set, this indicates option #2
        if file and line then
          -- if the file name is absolute, try to load it
          local activated
          if wx.wxIsAbsolutePath(file) then
            activated = activateDocument(file, line)
          else
            -- try to find a proper file based on file name
            -- first check using basedir that was set based on current file path
            if not activated then
              local fullPath = debugger.basedir..file
              activated = activateDocument(fullPath, line)
            end

            -- if not found, check using full file path and reset basedir
            if not activated then
              local path = wxfilepath:GetPath(wx.wxPATH_GET_VOLUME)..string_Pathsep
              fullPath = path..file
              activated = activateDocument(fullPath, line)
              if activated then
                debugger.basedir = path
                debugger.handle("basedir " .. debugger.basedir)
              end
            end
          end

          if not activated then
            DisplayOutput("Can't find file '" .. file .. "' to activate for debugging; open the file before debugging.\n")
            return debugger.terminate()
          end
        else
          activateDocument(startfile, 1)
        end
      end

      if (not options.noshell) then
        ShellSupportRemote(debugger.shell, debugger.pid)
      end

      DisplayOutput("Started remote debugging session (base directory: '" .. debugger.basedir .. "').\n")

    end)
  debugger.listening = true
end

debugger.handle = function(command, server)
  local _G = _G
  local os = os
  os.exit = function () end
  _G.print = function (...)
    if (ide.config.debugger.verbose) then
      DisplayOutput(...)
      DisplayOutput("\n")
    end
  end

  debugger.running = true
  local file, line, err = mobdebug.handle(command, server and server or debugger.server)
  debugger.running = false

  return file, line, err
end

debugger.exec = function(command)
  if debugger.server and not debugger.running then
    copas.addthread(function ()
        while true do
          local file, line, err = debugger.handle(command)
          if line == nil then
            if err then DisplayOutput(err .. "\n") end
            DebuggerStop()
            return
          else
            if debugger.basedir and not wx.wxIsAbsolutePath(file) then
              file = debugger.basedir .. file
            end
            if activateDocument(file, line) then
              if debugger.loop then
                updateWatchesSync()
              else
                updateWatches()
                return
              end
            else
              command = "out" -- redo now trying to get out of this file
            end
          end
        end
      end)
  end
end

debugger.updateBreakpoint = function(command)
  if debugger.server and not debugger.running then
    copas.addthread(function ()
        debugger.handle(command)
      end)
  end
end

debugger.update = function() copas.step(0) end
debugger.terminate = function()
  if debugger.server then
    if debugger.pid then -- if there is PID, try local kill
      DebuggerKillClient()
    else -- otherwise, trace graceful exit for the remote process
      debugger.exec("exit")
      copas.step(1) -- process 'exit' right away; doesn't guarantee the response
    end
    DebuggerStop()
  end
end
debugger.step = function() debugger.exec("step") end
debugger.trace = function()
  debugger.loop = true
  debugger.exec("step")
end
debugger.over = function() debugger.exec("over") end
debugger.out = function() debugger.exec("out") end
debugger.run = function() debugger.exec("run") end
debugger.evaluate = function(expression) return debugger.handle('eval ' .. expression) end
debugger.breaknow = function()
  -- stop if we're running a "trace" command
  debugger.loop = false

  -- force a step command; don't use copas interface as it checks
  -- for the other side "reading" and the other side is not reading anything.
  -- use the "original" socket to write a "step" command.
  -- this will only break on the next Lua command.
  if debugger.socket then
    local running = debugger.running
    -- this needs to be short as it will block the UI
    debugger.socket:settimeout(0.25)
    local file, line, err = debugger.handle("step", debugger.socket)
    debugger.socket:settimeout(0)
    -- restore running status
    debugger.running = running
    -- don't need to do anything else as the earlier call (run, step, etc.)
    -- will get the results (file, line) back and will update the UI
  end
end
debugger.breakpoint = function(file, line, state)
  debugger.updateBreakpoint((state and "setb " or "delb ") .. file .. " " .. line)
end

----------------------------------------------
-- public api

function DebuggerAttachDefault(options)
  debugger.options = options
  if (debugger.listening) then return end
  debugger.listen()
end

function DebuggerKillClient()
  if (debugger.pid) then
    -- using SIGTERM for some reason kills not only the debugee process,
    -- but also some system processes, which leads to a blue screen crash
    -- (at least on Windows Vista SP2)
    local ret = wx.wxProcess.Kill(debugger.pid, wx.wxSIGKILL, wx.wxKILL_CHILDREN)
    if ret == wx.wxKILL_OK then
      DisplayOutput("Stopped debuggee process (pid: "..debugger.pid..").\n")
    elseif ret ~= wx.wxKILL_NO_PROCESS then
      DisplayOutput("Unable to kill debuggee process (pid: "..debugger.pid.."), code "..tostring(ret)..".\n")
    end
    debugger.pid = nil
  end
end

function DebuggerStop()
  if (debugger.server) then
    debugger.server = nil
    debugger.pid = nil
    SetAllEditorsReadOnly(false)
    ShellSupportRemote(nil, 0)
    ClearAllCurrentLineMarkers()
    DisplayOutput("Completed debugging session.\n")
  end
end

function DebuggerCreateStackWindow()
  DisplayOutput("Not Yet Implemented\n")
end

function DebuggerCloseStackWindow()

end

function DebuggerCloseWatchWindow()
  if (debugger.watchWindow) then
    SettingsSaveFramePosition(debugger.watchWindow, "WatchWindow")
    debugger.watchListCtrl = nil
    debugger.watchWindow = nil
  end
end

function DebuggerCreateWatchWindow()
  local width = 200
  local watchWindow = wx.wxFrame(ide.frame, wx.wxID_ANY,
    "Watch Window",
    wx.wxDefaultPosition, wx.wxSize(width, 160),
    wx.wxDEFAULT_FRAME_STYLE + wx.wxFRAME_FLOAT_ON_PARENT)

  debugger.watchWindow = watchWindow

  local watchMenu = wx.wxMenu{
    { ID_ADDWATCH, "&Add Watch" },
    { ID_EDITWATCH, "&Edit Watch\tF2" },
    { ID_REMOVEWATCH, "&Remove Watch" },
    { ID_EVALUATEWATCH, "Evaluate &Watches" }}

  local watchMenuBar = wx.wxMenuBar()
  watchMenuBar:Append(watchMenu, "&Watches")
  watchWindow:SetMenuBar(watchMenuBar)

  local watchListCtrl = wx.wxListCtrl(watchWindow, ID_WATCH_LISTCTRL,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wx.wxLC_REPORT + wx.wxLC_EDIT_LABELS)

  debugger.watchListCtrl = watchListCtrl

  local info = wx.wxListItem()
  info:SetMask(wx.wxLIST_MASK_TEXT + wx.wxLIST_MASK_WIDTH)
  info:SetText("Expression")
  info:SetWidth(width * 0.45)
  watchListCtrl:InsertColumn(0, info)

  info:SetText("Value")
  info:SetWidth(width * 0.45)
  watchListCtrl:InsertColumn(1, info)

  watchWindow:CentreOnParent()
  SettingsRestoreFramePosition(watchWindow, "WatchWindow")
  watchWindow:Show(true)

  local function findSelectedWatchItem()
    local count = watchListCtrl:GetSelectedItemCount()
    if count > 0 then
      for idx = 0, watchListCtrl:GetItemCount() - 1 do
        if watchListCtrl:GetItemState(idx, wx.wxLIST_STATE_FOCUSED) ~= 0 then
          return idx
        end
      end
    end
    return -1
  end

  watchWindow:Connect( wx.wxEVT_CLOSE_WINDOW,
    function (event)
      DebuggerCloseWatchWindow()
      watchWindow = nil
      watchListCtrl = nil
      event:Skip()
    end)

  watchWindow:Connect(ID_ADDWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      local row = watchListCtrl:InsertItem(watchListCtrl:GetItemCount(), "Expr")
      watchListCtrl:SetItem(row, 0, "Expr")
      watchListCtrl:SetItem(row, 1, "Value")
      watchListCtrl:EditLabel(row)
    end)

  watchWindow:Connect(ID_EDITWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      local row = findSelectedWatchItem()
      if row >= 0 then
        watchListCtrl:EditLabel(row)
      end
    end)
  watchWindow:Connect(ID_EDITWATCH, wx.wxEVT_UPDATE_UI,
    function (event)
      event:Enable(watchListCtrl:GetSelectedItemCount() > 0)
    end)

  watchWindow:Connect(ID_REMOVEWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      local row = findSelectedWatchItem()
      if row >= 0 then
        watchListCtrl:DeleteItem(row)
      end
    end)
  watchWindow:Connect(ID_REMOVEWATCH, wx.wxEVT_UPDATE_UI,
    function (event)
      event:Enable(watchListCtrl:GetSelectedItemCount() > 0)
    end)

  watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      updateWatches()
    end)
  watchWindow:Connect(ID_EVALUATEWATCH, wx.wxEVT_UPDATE_UI,
    function (event)
      event:Enable(watchListCtrl:GetItemCount() > 0)
    end)

  watchListCtrl:Connect(wx.wxEVT_COMMAND_LIST_END_LABEL_EDIT,
    function (event)
      watchListCtrl:SetItem(event:GetIndex(), 0, event:GetText())
      updateWatches()
      event:Skip()
    end)
end

function DebuggerMakeFileName(editor, filePath)
  if not filePath then
    filePath = "file"..tostring(editor)
  end
  return filePath
end

function DebuggerToggleBreakpoint(editor, line)
  local markers = editor:MarkerGet(line)
  if markers >= CURRENT_LINE_MARKER_VALUE then
    markers = markers - CURRENT_LINE_MARKER_VALUE
  end
  local id = editor:GetId()
  local filePath = DebuggerMakeFileName(editor, ide.openDocuments[id].filePath)
  if markers >= BREAKPOINT_MARKER_VALUE then
    editor:MarkerDelete(line, BREAKPOINT_MARKER)
    if debugger.server then
      debugger.breakpoint(filePath, line+1, false)
    end
  else
    editor:MarkerAdd(line, BREAKPOINT_MARKER)
    if debugger.server then
      debugger.breakpoint(filePath, line+1, true)
    end
  end
end
