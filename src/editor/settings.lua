-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide

-- ----------------------------------------------------------------------------
-- Initialize the wxConfig for loading/saving the preferences

local settings = wx.wxFileConfig(GetIDEString("settingsapp"),GetIDEString("settingsvendor"))
ide.settings = settings

local function settingsReadSafe(settings,what,default)
  local cr,out = settings:Read(what,default)

  if (cr) then
    return out
  else
    return default
  end
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

  if (s ~= 1) and (s ~= 2) then
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

function SettingsAppendFileToHistory (filename)
  local listname = "/filehistory"
  local oldlist = SettingsRestoreFileHistory(nil,listname)

  -- if the file has been in the history before, remove it
  for i=#oldlist,1,-1 do
    if oldlist[i] == filename then table.remove(oldlist,i) end
  end

  table.insert(oldlist,1,{filename=filename})

  -- remove all entries that are no longer needed
  while #oldlist>ide.config.filehistorylength do table.remove(oldlist) end

  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  for i,doc in ipairs(oldlist) do
    settings:Write(tostring(i), doc.filename)
  end

  UpdateFileHistoryUI(oldlist)

  settings:SetPath(path)
end

---
-- () SettingsRestoreFileSession (function)
-- restores a list of opened files from the file settings
-- calls the given function with the restored table, a list
-- of tables containing tables like
-- {filename = "filename", cursorpos = <cursor position>}
function SettingsRestoreFileSession(fntab)
  local listname = "/session"
  local path = settings:GetPath()
  settings:SetPath(listname)
  local outtab = {}
  local couldread = true
  local id = 1
  local name
  while(couldread) do
    couldread, name = settings:Read(tostring(id), "")
    local fname,cursorpos = name:match("^(.+);(.-)$")
    name = fname or name
    cursorpos = tonumber(cursorpos or 0)
    couldread = couldread and name ~= ""
    if (couldread) then
      table.insert(outtab,{filename = name, cursorpos = cursorpos})
      id = id + 1
    end
  end

  local index = settingsReadSafe(settings,"index",1)

  if fntab then fntab(outtab,index) end

  settings:SetPath(path)

  return outtab
end

---
-- () SettingsSaveFileList (table opendocs)
-- saves the list of currently opened documents (passed in the opendocs table)
-- in the settings.
function SettingsSaveFileSession(opendocs,index)
  local listname = "/session"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  for i,doc in ipairs(opendocs) do
    settings:Write(tostring(i), doc.filename..";"..doc.cursorpos)
  end
  settings:Write("index",index)

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
  while(couldread) do
    couldread, name = settings:Read(tostring(id), "")
    couldread = couldread and name ~= ""
    if (couldread) then
      if (wx.wxDirExists(name)) then
        table.insert(outtab,name)
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
    for i,v in pairs(tab) do
      table.insert(t,i)
    end
    table.sort(t)
    return t
  end
  
  sortedX = sortedPages(pagesX)
  sortedY = sortedPages(pagesY)
  
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
  
  for i,v in ipairs(sortedUse) do
    local pages = pagesUse[v]
    for n,id in ipairs(pages) do
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
  local currentpages = {}
  for i=1,cnt do
    local id = nb:GetPageText(i-1)
    local newid = fnIdConvert and fnIdConvert(id) or id
    currentpages[newid] = {page = nb:GetPage(i-1), text = id, index = i-1}
  end
  
  -- remove them
  for i=cnt,1,-1 do
    nb:RemovePage(i-1)
  end

  -- readd them and perform splits
  local direction
  local splits = {
    X = wx.wxRIGHT,
    Y = wx.wxBOTTOM,
  }
  local t = 0
  local newsel
  local function finishPage(page)
    if (page.index == sel) then
      newsel = t
    end
    t = t + 1
  end
  
  for cmd in str:gmatch("([^|]+)") do
    local instr = cmd:match("<(%w)>")
    if (not instr) then
      local id = fnIdConvert and fnIdConvert(cmd) or cmd
      local page = currentpages[id]
      if (page) then
        nb:AddPage(page.page, page.text)
        currentpages[id] = nil
        if (direction) then
          nb:Split(t, direction)
        end
        finishPage(page)
      end
    end
    direction = instr and splits[instr]
  end
  
  -- add anything we forgot
  for i,page in pairs(currentpages) do
    nb:AddPage(page.page, page.text)
    finishPage(page)
  end
  
  if (newsel) then
    nb:SetSelection(newsel)
  end

end


function SettingsRestoreView()
  local listname = "/view"
  local path = settings:GetPath()
  settings:SetPath(listname)

  local frame = ide.frame
  local uimgr = frame.uimgr
  
  local layoutcur = uimgr:SavePerspective()
  local layout = settingsReadSafe(settings,"uimgrlayout",layoutcur)
  if (layout ~= layoutcur) then
    uimgr:LoadPerspective(layout)
    uimgr:Update()
  end
  
  local layoutcur = saveNotebook(frame.notebook)
  local layout = settingsReadSafe(settings,"nblayout",layoutcur)
  if (layout ~= layoutcur) then
    loadNotebook(ide.frame.notebook,layout)
    local openDocuments = ide.openDocuments
    local nb = frame.notebook
    local cnt = nb:GetPageCount()
    for i=0,cnt-1 do
      openDocuments[nb:GetPage(i):GetId()].index = i
    end
  end
  
  local layoutcur = saveNotebook(frame.bottomnotebook)
  local layout = settingsReadSafe(settings,"nbbtmlayout",layoutcur)
  if (layout ~= layoutcur) then
    loadNotebook(ide.frame.bottomnotebook,layout,
      function(name) return name:match("console") or name end)
  end

  settings:SetPath(path)
end

function SettingsSaveView()
  local listname = "/view"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  local frame = ide.frame
  local uimgr = frame.uimgr
  
  settings:Write("uimgrlayout",uimgr:SavePerspective())
  settings:Write("nblayout",   saveNotebook(frame.notebook))
  settings:Write("nbbtmlayout",saveNotebook(frame.bottomnotebook))

  settings:SetPath(path)
end

function SettingsRestoreEditorSettings()
  local listname = "/editor"
  local path = settings:GetPath()
  settings:SetPath(listname)

  ide.config.interpreter = settingsReadSafe(settings,"interpreter",ide.config.interpreter)
  ProjectSetInterpreter(ide.config.interpreter)
end
function SettingsSaveEditorSettings()
  local listname = "/editor"
  local path = settings:GetPath()
  settings:DeleteGroup(listname)
  settings:SetPath(listname)

  settings:Write("interpreter", ide.interpreter and ide.interpreter.fname or "_undefined_")

  settings:SetPath(path)
end
