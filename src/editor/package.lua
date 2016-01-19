-- Copyright 2013-15 Paul Kulchenko, ZeroBrane LLC
---------------------------------------------------------

local ide = ide
local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
local unpack = table.unpack or unpack
local q = EscapeMagic

local function eventHandle(handlers, event, ...)
  local success
  for package, handler in pairs(handlers) do
    local ok, res = pcall(handler, package, ...)
    if ok then
      if res == false then success = false end
    else
      DisplayOutputLn(TR("%s event failed: %s"):format(event, res))
    end
  end
  return success
end

local function getEventHandlers(packages, event)
  local handlers = {}
  for _, package in pairs(packages) do
    if package[event] then handlers[package] = package[event] end
  end
  return handlers
end

function PackageEventHandle(event, ...)
  return eventHandle(getEventHandlers(ide.packages, event), event, ...)
end

function PackageEventHandleOnce(event, ...)
  -- copy packages as the event that is handled only once needs to be removed
  local handlers = getEventHandlers(ide.packages, event)
  -- remove all handlers as they need to be called only once
  -- this allows them to be re-installed if needed
  for _, package in pairs(ide.packages) do package[event] = nil end
  return eventHandle(handlers, event, ...)
end

local function PackageEventHandleOne(file, event, ...)
  local package = ide.packages[file]
  if package and type(package[event]) == 'function' then
    local ok, res = pcall(package[event], package, ...)
    if ok then
      if res == false then return false end
    else
      DisplayOutputLn(TR("%s event failed: %s"):format(event, res))
    end
  end
end

function PackageUnRegister(file, ...)
  PackageEventHandleOne(file, "onUnRegister", ...)
  -- remove from the list of installed packages
  local package = ide.packages[file]
  ide.packages[file] = nil
  return package
end

function PackageRegister(file, ...)
  if not ide.packages[file] then
    local packages = {}
    local package = MergeFullPath(
      GetPathWithSep(ide.editorFilename), "packages/"..file..".lua")
    LoadLuaFileExt(packages, package, ide.proto.Plugin)
    packages[file].fname = file
    ide.packages[file] = packages[file]
  end
  return PackageEventHandleOne(file, "onRegister", ...)
end

function ide:GetRootPath(path)
  return MergeFullPath(GetPathWithSep(self.editorFilename), path or '')
end
function ide:GetPackagePath(packname)
  return MergeFullPath(
    self.oshome and MergeFullPath(self.oshome, '.'..self:GetAppName()..'/') or self:GetRootPath(),
    MergeFullPath('packages', packname or '')
  )
end
function ide:GetApp() return self.editorApp end
function ide:GetAppName() return self.appname end
function ide:GetEditor(index) return GetEditor(index) end
function ide:GetEditorWithFocus(...) return GetEditorWithFocus(...) end
function ide:GetEditorWithLastFocus()
  -- make sure ide.infocus is still a valid component and not "some" userdata
  return (self:IsValidCtrl(self.infocus)
    and self.infocus:GetClassInfo():GetClassName() == "wxStyledTextCtrl"
    and self.infocus:DynamicCast("wxStyledTextCtrl") or nil)
end
function ide:GetMenuBar() return self.frame.menuBar end
function ide:GetStatusBar() return self.frame.statusBar end
function ide:GetToolBar() return self.frame.toolBar end
function ide:GetDebugger() return self.debugger end
function ide:GetMainFrame() return self.frame end
function ide:GetUIManager() return self.frame.uimgr end
function ide:GetDocument(ed) return ed and self.openDocuments[ed:GetId()] end
function ide:GetDocuments() return self.openDocuments end
function ide:GetKnownExtensions(ext)
  local knownexts, extmatch = {}, ext and ext:lower()
  for _, spec in pairs(self.specs) do
    for _, ext in ipairs(spec.exts or {}) do
      if not extmatch or extmatch == ext:lower() then
        table.insert(knownexts, ext)
      end
    end
  end
  table.sort(knownexts)
  return knownexts
end

function ide:DoWhenIdle(func) table.insert(self.onidle, func) end

function ide:FindTopMenu(item)
  local index = self:GetMenuBar():FindMenu((TR)(item))
  return self:GetMenuBar():GetMenu(index), index
end
function ide:FindMenuItem(itemid, menu)
  local item, imenu = self:GetMenuBar():FindItem(itemid, menu)
  if menu and not item then item = menu:FindItem(itemid) end
  if not item then return end
  menu = menu or imenu

  for pos = 0, menu:GetMenuItemCount()-1 do
    if menu:FindItemByPosition(pos):GetId() == itemid then
      return item, menu, pos
    end
  end
  return
end
function ide:AttachMenu(...)
  -- AttachMenu([targetmenu,] id, submenu)
  -- `targetmenu` is only needed for menus not attached to the main menubar
  local menu, id, submenu = ...
  if select('#', ...) == 2 then menu, id, submenu = nil, ... end
  local item, menu, pos = self:FindMenuItem(id, menu)
  if not item then return end

  local menuitem = wx.wxMenuItem(menu, id, item:GetItemLabel(), item:GetHelp(), wx.wxITEM_NORMAL, submenu)
  menu:Destroy(item)
  return menu:Insert(pos, menuitem), pos
end
function ide:CloneMenu(menu)
  if not menu then return end
  local newmenu = wx.wxMenu()
  local node = menu:GetMenuItems():GetFirst()
  while node do
    local item = node:GetData():DynamicCast("wxMenuItem")
    newmenu:Append(item:GetId(), item:GetItemLabel(), item:GetHelp(), item:GetKind())
    node = node:GetNext()
  end
  return newmenu
end

function ide:FindDocument(path)
  local fileName = wx.wxFileName(path)
  for _, doc in pairs(self:GetDocuments()) do
    if doc.filePath and fileName:SameAs(wx.wxFileName(doc.filePath)) then
      return doc
    end
  end
  return
end
function ide:FindDocumentsByPartialPath(path)
  local seps = "[\\/]"
  -- add trailing path separator to make sure full directory match
  if not path:find(seps.."$") then path = path .. GetPathSeparator() end
  local pattern = "^"..q(path):gsub(seps, seps)
  local lpattern = pattern:lower()

  local docs = {}
  for _, doc in pairs(self:GetDocuments()) do
    if doc.filePath
    and (doc.filePath:find(pattern)
         or iscaseinsensitive and doc.filePath:lower():find(lpattern)) then
      table.insert(docs, doc)
    end
  end
  return docs
end
function ide:GetInterpreter() return self.interpreter end
function ide:GetInterpreters() return self.interpreters end
function ide:GetConfig() return self.config end
function ide:GetOutput() return self.frame.bottomnotebook.errorlog end
function ide:GetConsole() return self.frame.bottomnotebook.shellbox end
function ide:GetEditorNotebook() return self.frame.notebook end
function ide:GetOutputNotebook() return self.frame.bottomnotebook end
function ide:GetOutline() return self.outline end
function ide:GetProjectNotebook() return self.frame.projnotebook end
function ide:GetProject() return FileTreeGetDir() end
function ide:GetProjectStartFile()
  local projectdir = FileTreeGetDir()
  local startfile = self.filetree.settings.startfile[projectdir]
  return MergeFullPath(projectdir, startfile), startfile
end
function ide:GetLaunchedProcess() return self.debugger and self.debugger.pid end
function ide:GetProjectTree() return self.filetree.projtreeCtrl end
function ide:GetOutlineTree() return self.outline.outlineCtrl end
function ide:GetWatch() return self.debugger and self.debugger.watchCtrl end
function ide:GetStack() return self.debugger and self.debugger.stackCtrl end

local statusreset
function ide:SetStatusFor(text, interval, field)
  field = field or 0
  interval = interval or 2
  local statusbar = self:GetStatusBar()
  if not ide.timers.status then
    ide.timers.status = wx.wxTimer(statusbar)
    statusbar:Connect(wx.wxEVT_TIMER, function(event) if statusreset then statusreset() end end)
  end
  statusreset = function()
    if statusbar:GetStatusText(field) == text then statusbar:SetStatusText("", field) end
  end
  ide.timers.status:Start(interval*1000, wx.wxTIMER_ONE_SHOT)
  statusbar:SetStatusText(text, field)
end
function ide:SetStatus(text, field) self:GetStatusBar():SetStatusText(text, field or 0) end
function ide:GetStatus(field) return self:GetStatusBar():GetStatusText(field or 0) end
function ide:PushStatus(text, field) self:GetStatusBar():PushStatusText(text, field or 0) end
function ide:PopStatus(field) self:GetStatusBar():PopStatusText(field or 0) end
function ide:Yield() wx.wxYield() end
function ide:CreateBareEditor() return CreateEditor(true) end

local rawMethods = {"AddTextDyn", "InsertTextDyn", "AppendTextDyn", "SetTextDyn",
  "GetTextDyn", "GetLineDyn", "GetSelectedTextDyn", "GetTextRangeDyn"}
local useraw = nil

function ide:CreateStyledTextCtrl(...)
  local editor = wxstc.wxStyledTextCtrl(...)
  if not editor then return end

  if useraw == nil then
    useraw = true
    for _, m in ipairs(rawMethods) do
      if not pcall(function() return editor[m:gsub("Dyn", "Raw")] end) then useraw = false; break end
    end
  end

  -- map all `GetTextDyn` to `GetText` or `GetTextRaw` if `*Raw` methods are present
  editor.useraw = useraw
  for _, m in ipairs(rawMethods) do
    -- some `*Raw` methods return `nil` instead of `""` as their "normal" calls do
    -- (for example, `GetLineRaw` and `GetTextRangeRaw` for parameters outside of text)
    local def = m:find("^Get") and "" or nil
    editor[m] = function(...) return editor[m:gsub("Dyn", useraw and "Raw" or "")](...) or def end
  end

  local suffix = "\1\0"
  function editor:CopyDyn()
    if not self.useraw then return self:Copy() end
    -- check if selected fragment is a valid UTF-8 sequence
    local text = self:GetSelectedTextRaw()
    if text == "" or wx.wxString.FromUTF8(text) ~= "" then return self:Copy() end
    local tdo = wx.wxTextDataObject()
    -- append suffix as wxwidgets (3.1+ on Windows) truncate last char for odd-length strings
    local workaround = ide.osname == "Windows" and (#text % 2 > 0) and suffix or ""
    tdo:SetData(wx.wxDataFormat(wx.wxDF_TEXT), text..workaround)
    local clip = wx.wxClipboard.Get()
    clip:Open()
    clip:SetData(tdo)
    clip:Close()
  end

  function editor:PasteDyn()
    if not self.useraw then return self:Paste() end
    local tdo = wx.wxTextDataObject()
    local clip = wx.wxClipboard.Get()
    clip:Open()
    clip:GetData(tdo)
    clip:Close()
    local ok, text = tdo:GetDataHere(wx.wxDataFormat(wx.wxDF_TEXT))
    -- check if the fragment being pasted is a valid UTF-8 sequence
    if not ok or text == "" or wx.wxString.FromUTF8(text) ~= "" then return self:Paste() end
    if ide.osname == "Windows" then text = text:gsub(suffix.."+$","") end
    self:AddTextRaw(text)
    self:GotoPos(self:GetCurrentPos())
  end

  function editor:GotoPosEnforcePolicy(pos)
    self:GotoPos(pos)
    self:EnsureVisibleEnforcePolicy(self:LineFromPosition(pos))
  end

  function editor:CanFold()
    local foldable = false
    for m = 0, ide.MAXMARGIN do
      if editor:GetMarginWidth(m) > 0
      and editor:GetMarginMask(m) == wxstc.wxSTC_MASK_FOLDERS then
        foldable = true
      end
    end
    return foldable
  end

  -- circle through "fold all" => "hide base lines" => "unfold all"
  function editor:FoldSome()
    editor:Colourise(0, -1) -- update doc's folding info
    local foldall = false -- at least on header unfolded => fold all
    local hidebase = false -- at least one base is visible => hide all
    local lines = editor:GetLineCount()

    for ln = 0, lines-1 do
      local foldRaw = editor:GetFoldLevel(ln)
      local foldLvl = foldRaw % 4096
      local foldHdr = (math.floor(foldRaw / 8192) % 2) == 1

      -- at least one header is expanded
      foldall = foldall or (foldHdr and editor:GetFoldExpanded(ln))

      -- at least one base can be hidden
      hidebase = hidebase or (
        not foldHdr
        and ln > 1 -- first line can't be hidden, so ignore it
        and foldLvl == wxstc.wxSTC_FOLDLEVELBASE
        and bit.band(foldRaw, wxstc.wxSTC_FOLDLEVELWHITEFLAG) == 0
        and editor:GetLineVisible(ln))
    end

    -- shows lines; this doesn't change fold status for folded lines
    if not foldall and not hidebase then editor:ShowLines(0, lines-1) end

    for ln = 0, lines-1 do
      local foldRaw = editor:GetFoldLevel(ln)
      local foldLvl = foldRaw % 4096
      local foldHdr = (math.floor(foldRaw / 8192) % 2) == 1

      if foldall then
        if foldHdr and editor:GetFoldExpanded(ln) then
          editor:ToggleFold(ln)
        end
      elseif hidebase then
        if not foldHdr and (foldLvl == wxstc.wxSTC_FOLDLEVELBASE) then
          editor:HideLines(ln, ln)
        end
      else -- unfold all
        if foldHdr and not editor:GetFoldExpanded(ln) then
          editor:ToggleFold(ln)
        end
      end
    end
    editor:EnsureCaretVisible()
  end

  local function getMarginWidth(editor)
    local width = 0
    for m = 0, ide.MAXMARGIN do width = width + editor:GetMarginWidth(m) end
    return width
  end

  function editor:ShowPosEnforcePolicy(pos)
    local line = self:LineFromPosition(pos)
    self:EnsureVisibleEnforcePolicy(line)
    -- skip the rest if line wrapping is on
    if editor:GetWrapMode() ~= wxstc.wxSTC_WRAP_NONE then return end
    local xwidth = self:GetClientSize():GetWidth() - getMarginWidth(self)
    local xoffset = self:GetTextExtent(self:GetLineDyn(line):sub(1, pos-self:PositionFromLine(line)+1))
    self:SetXOffset(xoffset > xwidth and xoffset-xwidth or 0)
  end

  function editor:ClearAny()
    local length = self:GetLength()
    local selections = ide.wxver >= "2.9.5" and self:GetSelections() or 1
    self:Clear() -- remove selected fragments

    -- check if the modification has failed, which may happen
    -- if there is "invisible" text in the selected fragment.
    -- if there is only one selection, then delete manually.
    if length == self:GetLength() and selections == 1 then
      self:SetTargetStart(self:GetSelectionStart())
      self:SetTargetEnd(self:GetSelectionEnd())
      self:ReplaceTarget("")
    end
  end

  function editor:MarkerGetAll(mask, from, to)
    mask = mask or 2^24-1
    local markers = {}
    local line = editor:MarkerNext(from or 0, mask)
    while line > -1 do
      table.insert(markers, {line, editor:MarkerGet(line)})
      if to and line > to then break end
      line = editor:MarkerNext(line + 1, mask)
    end
    return markers
  end

  editor:Connect(wx.wxEVT_KEY_DOWN,
    function (event)
      local keycode = event:GetKeyCode()
      local mod = event:GetModifiers()
      if (keycode == wx.WXK_DELETE and mod == wx.wxMOD_SHIFT)
      or (keycode == wx.WXK_INSERT and mod == wx.wxMOD_CONTROL)
      or (keycode == wx.WXK_INSERT and mod == wx.wxMOD_SHIFT) then
        local id = keycode == wx.WXK_DELETE and ID.CUT or mod == wx.wxMOD_SHIFT and ID.PASTE or ID.COPY
        ide.frame:AddPendingEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, id))
      elseif keycode == wx.WXK_CAPITAL and mod == wx.wxMOD_CONTROL then
        -- ignore Ctrl+CapsLock
      else
        event:Skip()
      end
    end)
  return editor
end

function ide:LoadFile(...) return LoadFile(...) end

function ide:CopyToClipboard(text)
  if wx.wxClipboard:Get():Open() then
    wx.wxClipboard:Get():SetData(wx.wxTextDataObject(text))
    wx.wxClipboard:Get():Close()
    return true
  end
  return false
end

function ide:GetSetting(path, setting)
  local settings = self.settings
  local curpath = settings:GetPath()
  settings:SetPath(path)
  local ok, value = settings:Read(setting)
  settings:SetPath(curpath)
  return ok and value or nil
end

function ide:RemoveMenuItem(id, menu)
  local _, menu, pos = self:FindMenuItem(id, menu)
  if menu then
    self:GetMainFrame():Disconnect(id, wx.wxID_ANY, wx.wxEVT_COMMAND_MENU_SELECTED)
    self:GetMainFrame():Disconnect(id, wx.wxID_ANY, wx.wxEVT_UPDATE_UI)
    menu:Disconnect(id, wx.wxID_ANY, wx.wxEVT_COMMAND_MENU_SELECTED)
    menu:Disconnect(id, wx.wxID_ANY, wx.wxEVT_UPDATE_UI)
    menu:Remove(id)

    local positem = menu:FindItemByPosition(pos)
    if (not positem or positem:GetKind() == wx.wxITEM_SEPARATOR)
    and pos > 0
    and (menu:FindItemByPosition(pos-1):GetKind() == wx.wxITEM_SEPARATOR) then
      menu:Destroy(menu:FindItemByPosition(pos-1))
    end
    return true
  end
  return false
end

function ide:ExecuteCommand(cmd, wdir, callback, endcallback)
  local proc = wx.wxProcess(self:GetOutput())
  proc:Redirect()

  local cwd
  if (wdir and #wdir > 0) then -- ignore empty directory
    cwd = wx.wxFileName.GetCwd()
    cwd = wx.wxFileName.SetCwd(wdir) and cwd
  end

  local pid = wx.wxExecute(cmd, wx.wxEXEC_ASYNC, proc)
  pid = pid ~= -1 and pid ~= 0 and pid or nil
  if cwd then wx.wxFileName.SetCwd(cwd) end -- restore workdir
  if not pid then return pid, wx.wxSysErrorMsg() end

  OutputSetCallbacks(pid, proc, callback or function() end, endcallback)
  return pid
end

function ide:CreateImageList(group, ...)
  local _ = wx.wxLogNull() -- disable error reporting in popup
  local size = wx.wxSize(16,16)
  local imglist = wx.wxImageList(16,16)
  for i = 1, select('#', ...) do
    local icon, file = self:GetBitmap(select(i, ...), group, size)
    if imglist:Add(icon) == -1 then
      DisplayOutputLn(("Failed to add image '%s' to the image list.")
        :format(file or select(i, ...)))
    end
  end
  return imglist
end

local tintdef = 100
local function iconFilter(bitmap, tint)
  if type(tint) == 'function' then return tint(bitmap) end
  if type(tint) ~= 'table' or #tint ~= 3 then return bitmap end

  local tr, tg, tb = tint[1]/255, tint[2]/255, tint[3]/255
  local pi = 0.299*tr + 0.587*tg + 0.114*tb -- pixel intensity
  local perc = (tint[0] or tintdef)/tintdef
  tr, tg, tb = tr*perc, tg*perc, tb*perc

  local img = bitmap:ConvertToImage()
  for x = 0, img:GetWidth()-1 do
    for y = 0, img:GetHeight()-1 do
      if not img:IsTransparent(x, y) then
        local r, g, b = img:GetRed(x, y)/255, img:GetGreen(x, y)/255, img:GetBlue(x, y)/255
        local gs = (r + g + b) / 3
        local weight = 1-4*(gs-0.5)*(gs-0.5)
        r = math.max(0, math.min(255, math.floor(255 * (gs + (tr-pi) * weight))))
        g = math.max(0, math.min(255, math.floor(255 * (gs + (tg-pi) * weight))))
        b = math.max(0, math.min(255, math.floor(255 * (gs + (tb-pi) * weight))))
        img:SetRGB(x, y, r, g, b)
      end
    end
  end
  return wx.wxBitmap(img)
end

local icons = {} -- icon cache to avoid reloading the same icons
function ide:GetBitmap(id, client, size)
  local im = self.config.imagemap
  local width = size:GetWidth()
  local key = width.."/"..id
  local keyclient = key.."-"..client
  local mapped = im[keyclient] or im[id.."-"..client] or im[key] or im[id]
  -- mapped may be a file name/path or wxImage object; take that into account
  if type(im[id.."-"..client]) == 'string' then keyclient = width.."/"..im[id.."-"..client]
  elseif type(im[keyclient]) == 'string' then keyclient = im[keyclient]
  elseif type(im[id]) == 'string' then
    id = im[id]
    key = width.."/"..id
    keyclient = key.."-"..client
  end

  local fileClient = self:GetAppName() .. "/res/" .. keyclient .. ".png"
  local fileKey = self:GetAppName() .. "/res/" .. key .. ".png"
  local isImage = type(mapped) == 'userdata' and mapped:GetClassInfo():GetClassName() == 'wxImage'
  local file
  if mapped and (isImage or wx.wxFileName(mapped):FileExists()) then file = mapped
  elseif wx.wxFileName(fileClient):FileExists() then file = fileClient
  elseif wx.wxFileName(fileKey):FileExists() then file = fileKey
  else return wx.wxArtProvider.GetBitmap(id, client, size) end
  local icon = icons[file] or iconFilter(wx.wxBitmap(file), self.config.imagetint)
  icons[file] = icon
  return icon, file
end

function ide:AddPackage(name, package)
  self.packages[name] = setmetatable(package, self.proto.Plugin)
  self.packages[name].fname = name
  return self.packages[name]
end
function ide:RemovePackage(name) self.packages[name] = nil end

function ide:AddWatch(watch, value)
  local mgr = self.frame.uimgr
  local pane = mgr:GetPane("watchpanel")
  if (pane:IsOk() and not pane:IsShown()) then
    pane:Show()
    mgr:Update()
  end

  local watchCtrl = self.debugger.watchCtrl
  if not watchCtrl then return end

  local root = watchCtrl:GetRootItem()
  if not root or not root:IsOk() then return end

  local item = watchCtrl:GetFirstChild(root)
  while true do
    if not item:IsOk() then break end
    if watchCtrl:GetItemExpression(item) == watch then
      if value then watchCtrl:SetItemText(item, watch .. ' = ' .. tostring(value)) end
      return item
    end
    item = watchCtrl:GetNextSibling(item)
  end

  item = watchCtrl:AppendItem(root, watch, 1)
  watchCtrl:SetItemExpression(item, watch, value)
  return item
end

function ide:AddInterpreter(name, interpreter)
  self.interpreters[name] = setmetatable(interpreter, self.proto.Interpreter)
  ProjectUpdateInterpreters()
end
function ide:RemoveInterpreter(name)
  self.interpreters[name] = nil
  ProjectUpdateInterpreters()
end

function ide:AddSpec(name, spec)
  self.specs[name] = spec
  UpdateSpecs()
end
function ide:RemoveSpec(name) self.specs[name] = nil end

function ide:AddAPI(type, name, api)
  self.apis[type] = self.apis[type] or {}
  self.apis[type][name] = api
end
function ide:RemoveAPI(type, name) self.apis[type][name] = nil end

function ide:AddConsoleAlias(alias, table) return ShellSetAlias(alias, table) end
function ide:RemoveConsoleAlias(alias) return ShellSetAlias(alias, nil) end

function ide:AddMarker(...) return StylesAddMarker(...) end
function ide:GetMarker(marker) return StylesGetMarker(marker) end
function ide:RemoveMarker(marker) StylesRemoveMarker(marker) end

local indicators = {}
function ide:AddIndicator(indic, num)
  num = num or indicators[indic]
  if not num then -- new indicator; find the smallest available number
    local nums = {}
    for _, indicator in pairs(indicators) do
      if indicator >= wxstc.wxSTC_INDIC_CONTAINER then
        nums[indicator-wxstc.wxSTC_INDIC_CONTAINER+1] = true
      end
    end
    num = #nums + wxstc.wxSTC_INDIC_CONTAINER
    if num > wxstc.wxSTC_INDIC_MAX then return end
  end
  indicators[indic] = num
  return num
end
function ide:GetIndicator(indic) return indicators[indic] end
function ide:GetIndicators() return indicators end
function ide:RemoveIndicator(indic) indicators[indic] = nil end

-- this provides a simple stack for saving/restoring current configuration
local configcache = {}
function ide:AddConfig(name, files)
  if not name or configcache[name] then return end -- don't overwrite existing slots
  if type(files) ~= "table" then files = {files} end -- allow to pass one value
  configcache[name] = {
    config = require('mobdebug').dump(self.config, {nocode = true}),
    configmeta = getmetatable(self.config),
    packages = {},
  }
  -- build a list of existing packages
  local packages = {}
  for package in pairs(self.packages) do packages[package] = true end
  -- load config file(s)
  for _, file in pairs(files) do LoadLuaConfig(MergeFullPath(name, file)) end
  -- register newly added packages (if any)
  for package in pairs(self.packages) do
    if not packages[package] then -- this is a newly added package
      PackageEventHandleOne(package, "onRegister")
      configcache[name].packages[package] = true
    end
  end
  ReApplySpecAndStyles() -- apply current config to the UI
end
function ide:RemoveConfig(name)
  if not name or not configcache[name] then return end
  -- unregister cached packages
  for package in pairs(configcache[name].packages) do PackageUnRegister(package) end
  -- load original config
  local ok, res = LoadSafe(configcache[name].config)
  if ok then
    self.config = res
    if configcache[name].configmeta then setmetatable(self.config, configcache[name].configmeta) end
  else
    DisplayOutputLn(("Error while restoring configuration: '%s'."):format(res))
  end
  configcache[name] = nil -- clear the slot after use
  ReApplySpecAndStyles() -- apply current config to the UI
end

local panels = {}
function ide:AddPanel(ctrl, panel, name, conf)
  local width, height = 360, 200
  local notebook = wxaui.wxAuiNotebook(self.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wxaui.wxAUI_NB_DEFAULT_STYLE + wxaui.wxAUI_NB_TAB_EXTERNAL_MOVE
    - wxaui.wxAUI_NB_CLOSE_ON_ACTIVE_TAB + wx.wxNO_BORDER)
  notebook:AddPage(ctrl, name, true)
  notebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_BG_DCLICK,
    function() PaneFloatToggle(notebook) end)
  notebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_PAGE_CLOSE,
    function(event) event:Veto() end)

  local mgr = self.frame.uimgr
  mgr:AddPane(notebook, wxaui.wxAuiPaneInfo():
              Name(panel):Float():CaptionVisible(false):PaneBorder(false):
              MinSize(width/2,height/2):
              BestSize(width,height):FloatingSize(width,height):
              PinButton(true):Hide())
  if type(conf) == "function" then conf(mgr:GetPane(panel)) end
  mgr.defaultPerspective = mgr:SavePerspective() -- resave default perspective

  panels[name] = {ctrl, panel, name, conf}
  return mgr:GetPane(panel), notebook
end

function ide:RemovePanel(panel)
  local mgr = self.frame.uimgr
  local pane = mgr:GetPane(panel)
  if pane:IsOk() then
    local win = pane.window
    mgr:DetachPane(win)
    win:Destroy()
    mgr:Update()
  end
end

function ide:AddPanelDocked(notebook, ctrl, panel, name, conf, activate)
  notebook:AddPage(ctrl, name, activate ~= false)
  panels[name] = {ctrl, panel, name, conf}
  return notebook
end
function ide:IsPanelDocked(panel)
  local layout = self:GetSetting("/view", "uimgrlayout")
  return layout and not layout:find(panel)
end

function ide:IsValidCtrl(ctrl)
  return ctrl and pcall(function() ctrl:GetId() end)
end

function ide:IsValidProperty(ctrl, prop)
  return ide:IsValidCtrl(ctrl) and pcall(function() return ctrl[prop] end)
end

function ide:IsWindowShown(win)
  while win do
    if not win:IsShown() then return false end
    win = win:GetParent()
  end
  return true
end

function ide:RestorePanelByLabel(name)
  if not panels[name] then return end
  return self:AddPanel(unpack(panels[name]))
end

function ide:AddTool(name, command, updateui)
  return ToolsAddTool(name, command, updateui)
end

function ide:RemoveTool(name)
  return ToolsRemoveTool(name)
end
