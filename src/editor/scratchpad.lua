-- Live experimentation with source code
-- (C) 2012 Paul Kulchenko

local frame = ide.frame
local menu = frame.menuBar:GetMenu(frame.menuBar:FindMenu("&Project"))
local ID_RUNNOW = ID "debug.runnow"

-- insert after "Run" item
for item = 0, menu:GetMenuItemCount()-1 do
   if menu:FindItemByPosition(item):GetId() == ID_RUN then
     menu:Insert(item+1, ID_RUNNOW, "Run as Scratchpad", "Execute the current project/file and keep updating the code to see immediate results", wx.wxITEM_CHECK)
     break
   end
end

local debugger = ide.debugger
local openDocuments = ide.openDocuments

local lastCode
local lastErr
local function exploreProgram(editor)
  local editorText = editor:GetText()
  if lastCode == editorText then return end
  lastCode = editorText

  local fn, err, status = loadstring(editorText)
  if fn then
    local ll = {} -- keep a list of loaded modules to "unload" them
    local loaded = getmetatable(package.loaded)
    if loaded == nil then
      loaded = {}
      setmetatable(package.loaded, loaded)
    end
    loaded.__newindex = function (t, n, v) ll[n] = v; rawset(t, n, v) end

    -- allow global access; no attempt to sandbox
    local env = {}
    setmetatable(env,{__index = _G})
    setfenv(fn,env)

    status, err = pcall(fn)

    -- reset all newly loaded modules to allow them to be loaded again
    for k in pairs(ll) do package.loaded[k] = nil end
  end
  if lastErr then ClearOutput() end
  if err and err ~= lastErr then DisplayOutput(err .. "\n") end
  lastErr = err
end

frame:Connect(ID_RUNNOW, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if event:IsChecked() then
      lastCode = nil
      lastErr = nil

      local projectDir = FileTreeGetDir()
      if projectDir and wx.wxFileName.GetCwd() ~= projectDir then
        wx.wxFileName.SetCwd(projectDir)
      end

      if frame.menuBar:IsChecked(ID_CLEAROUTPUT) then ClearOutput() end
    end
    debugger.pid = frame.menuBar:IsChecked(ID_RUNNOW) and 0 or nil
  end)
frame:Connect(ID_RUNNOW, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable((debugger.server == nil) and (editor ~= nil))
    -- disable checkbox if the process has been stopped
    if frame.menuBar:IsChecked(ID_RUNNOW) and debugger.pid == nil then
      event:Check(false)
    end
  end)

frame:Connect(wx.wxEVT_IDLE, 
  function(event)
    if frame.menuBar:IsChecked(ID_RUNNOW) then
      local editor = GetEditor()
      exploreProgram(editor)
    end
    event:Skip() -- let other EVT_IDLE handlers to work on the event
  end)
