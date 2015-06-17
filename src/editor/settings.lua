-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local layoutlabel = {
  UIMANAGER = "uimgrlayout",
  NOTEBOOK = "nblayout",
  NOTEBOOKOUTPUT = "nbbtmlayout",
  NOTEBOOKPROJECT = "nbprojlayout",
  DOCKNOTEBOOK = "nbdocklayout",
  DOCKNOTEBOOKOUTPUT = "nbbtmdocklayout",
  DOCKNOTEBOOKPROJECT = "nbprojdocklayout",
  STATUSBAR = "statusbar",
}

-- ----------------------------------------------------------------------------
-- Initialize the wxConfig for loading/saving the preferences

local ini = ide.config.ini
-- if ini path is relative and includes a directory name, make it relative to the IDE location
ini = ini and (not wx.wxIsAbsolutePath(ini) and wx.wxFileName(ini):GetDirCount() > 0
  and MergeFullPath(GetPathWithSep(ide.editorFilename), ini) or ini)
-- check that the ini file doesn't point to a directory
if ini and (wx.wxFileName(ini):IsDir() or wx.wxIsAbsolutePath(ini) and wx.wxDirExists(ini)) then
  print(("Can't use 'ini' configuration setting '%s' that points to a directory instead of a file; ignored.")
    :format(ini))
  ini = nil
end
-- check that the directory is writable
if ini and wx.wxIsAbsolutePath(ini) and not wx.wxFileName(ini):IsDirWritable() then
  print(("Can't use 'ini' configuration setting '%s' that points to a non-writable directory; ignored.")
    :format(ini))
  ini = nil
end

local settings = wx.wxFileConfig(GetIDEString("settingsapp"), GetIDEString("settingsvendor"), ini or "")
ide.settings = settings

local function settingsReadSafe(settings,what,default)
  local cr,out = settings:Read(what,default)
  return cr and out or default
end

-- ----------------------------------------------------------------------------
-- wxConfig load/save preferences functions

function SettingsRestoreFramePosition(window, windowName)
  local path = settings:GetPath()
  settings:SetPath("/"..windowName)

  local s = -1
  s = tonumber(select(2,settings:Read("s", -1)))
  local x = tonumber(select(2,settings:Read("x", 0)))
  local y = tonumber(select(2,settings:Read("y", 0)))
  local w = tonumber(select(2,settings:Read("w", 1000)))
  local h = tonumber(select(2,settings:Read("h", 700)))

  if (s ~= -1) and (s ~= 1) and (s ~= 2) then
    local clientX, clientY, clientWidth, clientHeight
    clientX, clientY, clientWidth, clientHeight = wx.wxClientDisplayRect()

    if x < clientX then x = clientX end
    if y < clientY then y = clientY end

    if w > clientWidth then w = clientWidth end
    if h > clientHeight then h = clientHeight end

    window:SetSize(x, y, w, h)
  elseif s == 1 then
    window:Maximize(true)
  end

  settings:SetPath(path)
end

function SettingsSaveFramePosition(window, windowName)
  local path = settings:GetPath()
  settings:SetPath("/"..windowName)

  local s = 0
  local w, h = window:GetSizeWH()
  local x, y = window:GetPositionXY()

  if window:IsMaximized() then
    s = 1
  elseif window:IsIconized() then
    s = 2
  end

  settings:Write("s", s==2 and 0 or s) -- iconized maybe - but that shouldnt be saved

  if s == 0 then
    settings:Write("x", x)
    settings:Write("y", y)
    settings:Write("w", w)
    settings:Write("h", h)
  end

  settings:SetPath(path)
end

---
-- (table) SettingsRestoreFileHistory (function)
-- restores a list of recently loaded documents from the settings table
-- a table is returned which contains tables each with a filename key, pointing to
-- the filename
function SettingsRestoreFileHistory(fntab)
  local path = settings:GetPath()
  local listname = "/filehistory"
  settings:SetPath(listname)

  local outtab = {}
  local inlist = {}
  for id=1,ide.config.filehistorylength do
    local couldread, name = settings:Read(tostring(id), "")
    if not couldread or name == "" then break end
    if not inlist[name] then
      inlist[name] = true
      table.insert(outtab,{filename = name})
    end
  end

  if fntab then fntab(outtab) end

  settings:SetPath(path)

  return outtab
end

function SettingsSaveFileHistory (filehistory)
  local listname = "/filehistory"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  for i,doc in ipairs(filehistory) do
    settings:Write(tostring(i), doc.filename)
  end

  settings:SetPath(path)
end

---
-- () SettingsRestoreFileSession (function [, string section])
-- restores a list of opened files from the file settings
-- calls the given function with the restored table, a list
-- of tables containing tables like
-- {filename = "filename", cursorpos = <cursor position>}
function SettingsRestoreFileSession(fntab, section)
  local listname = section or "/session"
  local path = settings:GetPath()
  settings:SetPath(listname)
  local outtab = {}
  local params = {}
  local ismore, key, index = settings:GetFirstEntry("", 0)
  while (ismore) do
    local couldread, value = settings:Read(key, "")
    if tonumber(key) then
      local fname,cursorpos = value:match("^(.+);(.-)$")
      if (couldread and value ~= "") then
        outtab[tonumber(key)] =
          {filename = fname or value, cursorpos = tonumber(cursorpos) or 0}
      end
    else
      params[key] = tonumber(value) or value
    end
    ismore, key, index = settings:GetNextEntry(index)
  end

  if fntab then fntab(outtab, params) end

  settings:SetPath(path)

  return outtab
end

---
-- () SettingsSaveFileSession (table opendocs, table params [, string section])
-- saves the list of currently opened documents (passed in the opendocs table)
-- in the settings.
function SettingsSaveFileSession(opendocs, params, section)
  local listname = section or "/session"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  for i,doc in ipairs(opendocs) do
    settings:Write(tostring(i), doc.filename..";"..doc.cursorpos)
  end

  -- save all other parameters
  for k,v in pairs(params) do settings:Write(k, v) end

  settings:SetPath(path)
end

---
-- () SettingsRestoreProjectSession (function)
function SettingsRestoreProjectSession(fntab)
  local listname = "/projectsession"
  local path = settings:GetPath()
  settings:SetPath(listname)
  local outtab = {}
  local couldread = true
  local id = 1
  local name
  while (couldread) do
    couldread, name = settings:Read(tostring(id), "")
    couldread = couldread and name ~= ""
    if (couldread) then
      if (wx.wxDirExists(name)) then
        table.insert(outtab,name)

        local function projsession(...) ProjectConfig(name, {...}) end
        SettingsRestoreFileSession(projsession, listname .. "/" .. tostring(id))
      end
      id = id + 1
    end
  end

  if fntab then fntab(outtab) end

  settings:SetPath(path)

  return outtab
end

---
-- () SettingsSaveProjectSession (table projdirs)
-- saves the list of currently active projects
-- in the settings.
function SettingsSaveProjectSession(projdirs)
  local listname = "/projectsession"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  for i,dir in ipairs(projdirs) do
    settings:Write(tostring(i), dir)

    local opendocs, params = ProjectConfig(dir)
    if opendocs then
      SettingsSaveFileSession(opendocs, params, listname .. "/" .. tostring(i))
    end
  end

  settings:SetPath(path)
end

function SettingsRestorePackage(package)
  local packagename = "/package/"..package
  local path = settings:GetPath()
  settings:SetPath(packagename)
  local outtab = {}
  local ismore, key, index = settings:GetFirstEntry("", 0)
  while (ismore) do
    local couldread, value = settings:Read(key, "")
    if couldread then
      local ok, res = LoadSafe("return "..value)
      if ok then outtab[key] = res
      else outtab[key] = nil end
    end
    ismore, key, index = settings:GetNextEntry(index)
  end
  settings:SetPath(path)
  return outtab
end

function SettingsSavePackage(package, values)
  local packagename = "/package/"..package
  local path = settings:GetPath()
  local mdb = require('mobdebug')

  settings:DeleteGroup(packagename)
  settings:SetPath(packagename)
  for k,v in pairs(values or {}) do
    settings:Write(k, mdb.line(v, {comment = false, nocode = true}))
  end
  settings:SetPath(path)
end

-----------------------------------

local function saveNotebook(nb)
  local cnt = nb:GetPageCount()
  
  local function addTo(tab,key,value)
    local out = tab[key] or {}
    table.insert(out,value)
    tab[key] = out
  end
  
  local pagesX = {}
  local pagesY = {}
  
  local str = "nblayout|"
  
  for i=1,cnt do
    local id = nb:GetPageText(i-1)
    local pg = nb:GetPage(i-1)
    local x,y = pg:GetPosition():GetXY()
    addTo(pagesX,x,id)
    addTo(pagesY,y,id)
  end
  
  local function sortedPages(tab)
    local t = {}
    for i in pairs(tab) do
      table.insert(t,i)
    end
    table.sort(t)
    return t
  end
  
  local sortedX = sortedPages(pagesX)
  local sortedY = sortedPages(pagesY)
  
  -- for now only support "1D" splits and prefer
  -- dimension which has more, anything else
  -- requires a more complex algorithm, yet to do
  
  local pagesUse
  local sortedUse
  local split
  
  if ( #sortedX >= #sortedY) then
    pagesUse  = pagesX
    sortedUse = sortedX
    split = "<X>"
  else
    pagesUse  = pagesY
    sortedUse = sortedY
    split = "<Y>"
  end
  
  for _, v in ipairs(sortedUse) do
    local pages = pagesUse[v]
    for _, id in ipairs(pages) do
      str = str..id.."|"
    end
    str = str..split.."|"
  end
  
  return str
end

local function loadNotebook(nb,str,fnIdConvert)
  str = str:match("nblayout|(.+)")
  if (not str) then return end
  local cnt = nb:GetPageCount()
  local sel = nb:GetSelection()

  -- store old pages
  local currentpages, order = {}, {}
  for i=1,cnt do
    local id = nb:GetPageText(i-1)
    local newid = fnIdConvert and fnIdConvert(id) or id
    currentpages[newid] = currentpages[newid] or {}
    table.insert(currentpages[newid], {page = nb:GetPage(i-1), text = id, index = i-1})
    order[i] = newid
  end

  -- remove them
  for i=cnt,1,-1 do nb:RemovePage(i-1) end

  -- read them and perform splits
  local t = 0
  local newsel
  local function finishPage(page)
    if (page.index == sel) then
      newsel = t
    end
    t = t + 1
  end

  local direction
  local splits = { X = wx.wxRIGHT, Y = wx.wxBOTTOM }
  for cmd in str:gmatch("([^|]+)") do
    local instr = cmd:match("<(%w)>")
    if (not instr) then
      local id = fnIdConvert and fnIdConvert(cmd) or cmd
      local pageind = next(currentpages[id] or {})
      if (pageind) then
        local page = currentpages[id][pageind]
        currentpages[id][pageind] = nil

        nb:AddPage(page.page, page.text)
        if (direction) then nb:Split(t, direction) end
        finishPage(page)
      end
    end
    direction = instr and splits[instr]
  end
  
  -- add anything we forgot; make sure page groups are in the order specified
  for i=1,cnt do
    local pagelist = currentpages[order[i]]
    for _,page in pairs(pagelist) do
      nb:AddPage(page.page, page.text)
      finishPage(page)
    end
  end
  
  -- set the active page as it was before
  if (newsel) then nb:SetSelection(newsel) end
end

function SettingsRestoreView()
  local listname = "/view"
  local path = settings:GetPath()
  settings:SetPath(listname)

  local frame = ide.frame
  local uimgr = frame.uimgr
  
  local layoutcur = uimgr:SavePerspective()
  local layout = settingsReadSafe(settings,layoutlabel.UIMANAGER,layoutcur)
  if (layout ~= layoutcur) then
    -- save the current toolbar besth and re-apply after perspective is loaded
    -- bestw and besth has two separate issues:
    -- (1) layout includes bestw that is only as wide as the toolbar size,
    -- this leaves default background on the right side of the toolbar;
    -- fix it by explicitly replacing with the screen width.
    -- (2) besth may be wrong after icon size changes.
    local toolbar = frame.uimgr:GetPane("toolbar")
    local besth = toolbar:IsOk() and tonumber(uimgr:SavePaneInfo(toolbar):match("besth=([^;]+)"))
    uimgr:LoadPerspective(layout, false)
    if toolbar:IsOk() then -- fix bestw and besth values
      toolbar:BestSize(wx.wxSystemSettings.GetMetric(wx.wxSYS_SCREEN_X), besth or -1)
    end

    -- check if debugging panes are not mentioned and float them
    for _, name in pairs({"stackpanel", "watchpanel", "searchpanel"}) do
      local pane = frame.uimgr:GetPane(name)
      if pane:IsOk() and not layout:find(name) then pane:Float() end
    end

    -- check if the toolbar is not mentioned in the layout and show it
    for _, name in pairs({"toolbar"}) do
      local pane = frame.uimgr:GetPane(name)
      if pane:IsOk() and not layout:find(name) then pane:Show() end
    end

    -- remove captions from all panes
    local panes = frame.uimgr:GetAllPanes()
    for index = 0, panes:GetCount()-1 do
      uimgr:GetPane(panes:Item(index).name):CaptionVisible(false)
    end
  end

  frame:GetStatusBar():Show(settingsReadSafe(settings,layoutlabel.STATUSBAR,true))

  uimgr:Update()
  
  layoutcur = saveNotebook(ide:GetOutputNotebook())
  layout = settingsReadSafe(settings,layoutlabel.NOTEBOOKOUTPUT,layoutcur)
  if (layout ~= layoutcur) then
    loadNotebook(ide:GetOutputNotebook(),layout,
      -- treat "Output (running)" same as "Output"
      function(name) return
        name:match(TR("Output")) or name:match("Output") or name end)
  end

  layoutcur = saveNotebook(ide:GetProjectNotebook())
  layout = settingsReadSafe(settings,layoutlabel.NOTEBOOKPROJECT,layoutcur)
  if (layout ~= layoutcur) then
    loadNotebook(ide:GetProjectNotebook(),layout)
  end

  -- always select Output tab
  local bottomnotebook = ide:GetOutputNotebook()
  local index = bottomnotebook:GetPageIndex(bottomnotebook.errorlog)
  if index >= 0 then bottomnotebook:SetSelection(index) end

  layoutcur = saveNotebook(frame.notebook)
  layout = settingsReadSafe(settings,layoutlabel.NOTEBOOK,layoutcur)
  if (layout ~= layoutcur) then
    loadNotebook(ide.frame.notebook,layout)
    local openDocuments = ide.openDocuments
    local nb = frame.notebook
    local cnt = nb:GetPageCount()
    for i=0,cnt-1 do
      openDocuments[nb:GetPage(i):GetId()].index = i
    end
  end

  -- restore configuration for notebook pages that have been split;
  -- load saved dock_size values and update current values with saved ones
  -- where dock_size configuration matches
  for l, m in pairs({
    [layoutlabel.DOCKNOTEBOOK] = ide:GetEditorNotebook():GetAuiManager(),
    [layoutlabel.DOCKNOTEBOOKOUTPUT] = ide:GetOutputNotebook():GetAuiManager(),
    [layoutlabel.DOCKNOTEBOOKPROJECT] = ide:GetProjectNotebook():GetAuiManager(),
  }) do
    -- ...|dock_size(5,0,0)=20|dock_size(2,1,0)=200|...
    local prevlayout = settingsReadSafe(settings, l, "")
    local curlayout = m:SavePerspective()
    local newlayout = curlayout:gsub('(dock_size[^=]+=)(%d+)', function(t,v)
        local val = prevlayout:match(EscapeMagic(t)..'(%d+)')
        return t..(val or v)
      end)
    if newlayout ~= curlayout then m:LoadPerspective(newlayout) end
  end

  local editor = GetEditor()
  if editor then editor:SetFocus() end

  settings:SetPath(path)
end

function SettingsSaveView()
  local listname = "/view"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  local frame = ide.frame
  local uimgr = frame.uimgr
  
  settings:Write(layoutlabel.UIMANAGER, uimgr:SavePerspective())
  settings:Write(layoutlabel.NOTEBOOK, saveNotebook(ide:GetEditorNotebook()))
  settings:Write(layoutlabel.NOTEBOOKOUTPUT, saveNotebook(ide:GetOutputNotebook()))
  settings:Write(layoutlabel.NOTEBOOKPROJECT, saveNotebook(ide:GetProjectNotebook()))
  settings:Write(layoutlabel.DOCKNOTEBOOK, ide:GetEditorNotebook():GetAuiManager():SavePerspective())
  settings:Write(layoutlabel.DOCKNOTEBOOKOUTPUT, ide:GetOutputNotebook():GetAuiManager():SavePerspective())
  settings:Write(layoutlabel.DOCKNOTEBOOKPROJECT, ide:GetProjectNotebook():GetAuiManager():SavePerspective())
  settings:Write(layoutlabel.STATUSBAR, frame:GetStatusBar():IsShown())

  settings:SetPath(path)
end

function SettingsRestoreEditorSettings()
  local listname = "/editor"
  local path = settings:GetPath()
  settings:SetPath(listname)

  ide.config.interpreter = settingsReadSafe(settings,"interpreter",ide.config.interpreter)
  ProjectSetInterpreter(ide.config.interpreter)

  settings:SetPath(path)
end

function SettingsSaveEditorSettings()
  local listname = "/editor"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  settings:Write("interpreter", ide.interpreter and ide.interpreter.fname or "_undefined_")

  settings:SetPath(path)
end

function SettingsSaveAll()
  SettingsSaveProjectSession(FileTreeGetProjects())
  SettingsSaveFileSession(GetOpenFiles())
  SettingsSaveView()
  SettingsSaveFileHistory(GetFileHistory())
  SettingsSaveFramePosition(ide.frame, "MainFrame")
  SettingsSaveEditorSettings()
end
