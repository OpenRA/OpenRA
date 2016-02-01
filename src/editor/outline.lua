-- Copyright 2014-15 Paul Kulchenko, ZeroBrane LLC

local ide = ide
ide.outline = {
  outlineCtrl = nil,
  imglist = ide:CreateImageList("OUTLINE", "FILE-NORMAL", "VALUE-LCALL",
    "VALUE-GCALL", "VALUE-ACALL", "VALUE-SCALL", "VALUE-MCALL"),
  settings = {
    symbols = {},
    ignoredirs = {},
  },
  needsaving = false,
  needrefresh = nil,
  indexqueue = {[0] = {}},
  indexpurged = false, -- flag that the index has been purged from old records; once per session
}

local outline = ide.outline
local image = { FILE = 0, LFUNCTION = 1, GFUNCTION = 2, AFUNCTION = 3,
  SMETHOD = 4, METHOD = 5,
}
local q = EscapeMagic
local caches = {}

local function setData(ctrl, item, value)
  if ide.wxver >= "2.9.5" then
    local data = wx.wxLuaTreeItemData()
    data:SetData(value)
    ctrl:SetItemData(item, data)
  end
end

local function resetOutlineTimer()
  if ide.config.outlineinactivity then
    ide.timers.outline:Start(ide.config.outlineinactivity*1000, wx.wxTIMER_ONE_SHOT)
  end
end

local function resetIndexTimer(interval)
  if ide.config.symbolindexinactivity and not ide.timers.symbolindex:IsRunning() then
    ide.timers.symbolindex:Start(interval or ide.config.symbolindexinactivity*1000, wx.wxTIMER_ONE_SHOT)
  end
end

local function outlineRefresh(editor, force)
  if not editor then return end
  local tokens = editor:GetTokenList()
  local sep = editor.spec.sep
  local varname = "([%w_][%w_"..q(sep:sub(1,1)).."]*)"
  local funcs = {updated = TimeGet()}
  local var = {}
  local outcfg = ide.config.outline or {}
  local scopes = {}
  local funcnum = 0
  local SCOPENUM, FUNCNUM = 1, 2
  local text
  for _, token in ipairs(tokens) do
    local op = token[1]
    if op == 'Var' or op == 'Id' then
      var = {name = token.name, fpos = token.fpos, global = token.context[token.name] == nil}
    elseif outcfg.showcurrentfunction and op == 'Scope' then
      local fundepth = #scopes
      if token.name == '(' then -- a function starts a new scope
        funcnum = funcnum + 1 -- increment function count
        local nested = fundepth == 0 or scopes[fundepth][SCOPENUM] > 0
        scopes[fundepth + (nested and 1 or 0)] = {1, funcnum}
      elseif fundepth > 0 then
        scopes[fundepth][SCOPENUM] = scopes[fundepth][SCOPENUM] + 1
      end
    elseif outcfg.showcurrentfunction and op == 'EndScope' then
      local fundepth = #scopes
      if fundepth > 0 and scopes[fundepth][SCOPENUM] > 0 then
        scopes[fundepth][SCOPENUM] = scopes[fundepth][SCOPENUM] - 1
        if scopes[fundepth][SCOPENUM] == 0 then
          local funcnum = scopes[fundepth][FUNCNUM]
          if funcs[funcnum] then
            funcs[funcnum].poe = token.fpos + (token.name and #token.name or 0)
          end
          table.remove(scopes)
        end
      end
    elseif op == 'Function' then
      local depth = token.context['function'] or 1
      local name, pos = token.name, token.fpos
      text = text or editor:GetTextDyn()
      local _, _, rname, params = text:find('([^%(]*)(%b())', pos)
      if name and rname:find(token.name, 1, true) ~= 1 then
        name = rname:gsub("%s+$","")
      end
      if not name then
        local s = editor:PositionFromLine(editor:LineFromPosition(pos-1))
        local rest
        rest, pos, name = text:sub(s+1, pos-1):match('%s*(.-)()'..varname..'%s*=%s*function%s*$')
        if rest then
          pos = s + pos
          -- guard against "foo, bar = function() end" as it would get "bar"
          if #rest>0 and rest:find(',') then name = nil end
        end
      end
      local ftype = image.LFUNCTION
      if not name then
        ftype = image.AFUNCTION
      elseif outcfg.showmethodindicator and name:find('['..q(sep)..']') then
        ftype = name:find(q(sep:sub(1,1))) and image.SMETHOD or image.METHOD
      elseif var.name == name and var.fpos == pos
      or var.name and name:find('^'..var.name..'['..q(sep)..']') then
        ftype = var.global and image.GFUNCTION or image.LFUNCTION
      end
      name = name or outcfg.showanonymous
      funcs[#funcs+1] = {
        name = ((name or '~')..params):gsub("%s+", " "),
        skip = (not name) and true or nil,
        depth = depth,
        image = ftype,
        pos = name and pos or token.fpos,
      }
    end
  end

  if force == nil then return funcs end

  local ctrl = outline.outlineCtrl
  local cache = caches[editor] or {}
  caches[editor] = cache

  -- add file
  local filename = ide:GetDocument(editor):GetTabText()
  local fileitem = cache.fileitem
  if not fileitem then
    local root = ctrl:GetRootItem()
    if not root or not root:IsOk() then return end

    if outcfg.showonefile then
      fileitem = root
    else
      fileitem = ctrl:AppendItem(root, filename, image.FILE)
      setData(ctrl, fileitem, editor)
      ctrl:SetItemBold(fileitem, true)
      ctrl:SortChildren(root)
    end
    cache.fileitem = fileitem
  end

  do -- check if any changes in the cached function list
    local prevfuncs = cache.funcs or {}
    local nochange = #funcs == #prevfuncs
    local resort = {} -- items that need to be re-sorted
    if nochange then
      for n, func in ipairs(funcs) do
        func.item = prevfuncs[n].item -- carry over cached items
        if func.depth ~= prevfuncs[n].depth then
          nochange = false
        elseif nochange and prevfuncs[n].item then
          if func.name ~= prevfuncs[n].name then
            ctrl:SetItemText(prevfuncs[n].item, func.name)
            if outcfg.sort then resort[ctrl:GetItemParent(prevfuncs[n].item)] = true end
          end
          if func.image ~= prevfuncs[n].image then
            ctrl:SetItemImage(prevfuncs[n].item, func.image)
          end
        end
      end
    end
    cache.funcs = funcs -- set new cache as positions may change
    if nochange and not force then -- return if no visible changes
      if outcfg.sort then -- resort items for all parents that have been modified
        for item in pairs(resort) do ctrl:SortChildren(item) end
      end
      return
    end
  end

  -- refresh the tree
  -- refreshing shouldn't change the focus of the current element,
  -- but it appears that DeleteChildren (wxwidgets 2.9.5 on Windows)
  -- moves the focus from the current element to wxTreeCtrl.
  -- need to save the window having focus and restore after the refresh.
  local win = ide:GetMainFrame():FindFocus()

  ctrl:Freeze()

  -- disabling event handlers is not strictly necessary, but it's expected
  -- to fix a crash on Windows that had DeleteChildren in the trace (#442).
  ctrl:SetEvtHandlerEnabled(false)
  ctrl:DeleteChildren(fileitem)
  ctrl:SetEvtHandlerEnabled(true)

  local edpos = editor:GetCurrentPos()+1
  local stack = {fileitem}
  local resort = {} -- items that need to be re-sorted
  for n, func in ipairs(funcs) do
    local depth = outcfg.showflat and 1 or func.depth
    local parent = stack[depth]
    while not parent do depth = depth - 1; parent = stack[depth] end
    if not func.skip then
      local item = ctrl:AppendItem(parent, func.name, func.image)
      if ide.config.outline.showcurrentfunction
      and edpos >= func.pos and func.poe and edpos <= func.poe then
        ctrl:SetItemBold(item, true)
      end
      if outcfg.sort then resort[parent] = true end
      setData(ctrl, item, n)
      func.item = item
      stack[func.depth+1] = item
    end
    func.skip = nil
  end
  if outcfg.sort then -- resort items for all parents that have been modified
    for item in pairs(resort) do ctrl:SortChildren(item) end
  end
  if outcfg.showcompact then ctrl:Expand(fileitem) else ctrl:ExpandAllChildren(fileitem) end

  -- scroll to the fileitem, but only if it's not a root item (as it's hidden)
  if fileitem:GetValue() ~= ctrl:GetRootItem():GetValue() then
    ctrl:ScrollTo(fileitem)
    ctrl:SetScrollPos(wx.wxHORIZONTAL, 0, true)
  else -- otherwise, scroll to the top
    ctrl:SetScrollPos(wx.wxVERTICAL, 0, true)
  end
  ctrl:Thaw()

  if win and win ~= ide:GetMainFrame():FindFocus() then win:SetFocus() end
end

local function indexFromQueue()
  if #outline.indexqueue == 0 then return end

  local editor = ide:GetEditor()
  local inactivity = ide.config.symbolindexinactivity
  if editor and inactivity and editor.updated > TimeGet()-inactivity then
    -- reschedule timer for later time
    resetIndexTimer()
  else
    local fname = table.remove(outline.indexqueue, 1)
    outline.indexqueue[0][fname] = nil
    -- check if fname is already loaded
    ide:SetStatusFor(TR("Indexing %d files: '%s'..."):format(#outline.indexqueue+1, fname))
    local content, err = FileRead(fname)
    if content then
      local editor = ide:CreateBareEditor()
      editor:SetupKeywords(GetFileExt(fname))
      editor:SetTextDyn(content)
      editor:Colourise(0, -1)
      editor:ResetTokenList()
      while IndicateAll(editor) do end

      outline:UpdateSymbols(fname, outlineRefresh(editor))
      editor:Destroy()
    else
      DisplayOutputLn(TR("Can't open file '%s': %s"):format(fname, err))
    end
    if #outline.indexqueue == 0 then
      outline:SaveSettings()
      ide:SetStatusFor(TR("Indexing completed."))
    end
    ide:DoWhenIdle(indexFromQueue)
  end
  return
end

local function createOutlineWindow()
  local REFRESH, REINDEX = 1, 2
  local width, height = 360, 200
  local ctrl = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(width, height),
    wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS
    + wx.wxTR_HIDE_ROOT + wx.wxNO_BORDER)

  outline.outlineCtrl = ctrl
  ide.timers.outline = wx.wxTimer(ctrl, REFRESH)
  ide.timers.symbolindex = wx.wxTimer(ctrl, REINDEX)

  ctrl:AddRoot("Outline")
  ctrl:SetImageList(outline.imglist)
  ctrl:SetFont(ide.font.fNormal)

  function ctrl:ActivateItem(item_id)
    local data = ctrl:GetItemData(item_id)
    if ctrl:GetItemImage(item_id) == image.FILE then
      -- activate editor tab
      local editor = data:GetData()
      if not ide:GetEditorWithFocus(editor) then ide:GetDocument(editor):SetActive() end
    else
      -- activate tab and move cursor based on stored pos
      -- get file parent
      local onefile = (ide.config.outline or {}).showonefile
      local parent = ctrl:GetItemParent(item_id)
      if not onefile then -- find the proper parent
        while parent:IsOk() and ctrl:GetItemImage(parent) ~= image.FILE do
          parent = ctrl:GetItemParent(parent)
        end
        if not parent:IsOk() then return end
      end
      -- activate editor tab
      local editor = onefile and GetEditor() or ctrl:GetItemData(parent):GetData()
      local cache = caches[editor]
      if editor and cache then
        -- move to position in the file
        editor:GotoPosEnforcePolicy(cache.funcs[data:GetData()].pos-1)
        -- only set editor active after positioning as this may change focus,
        -- which may regenerate the outline, which may invalidate `data` value
        if not ide:GetEditorWithFocus(editor) then ide:GetDocument(editor):SetActive() end
      end
    end
  end

  local function activateByPosition(event)
    local mask = (wx.wxTREE_HITTEST_ONITEMINDENT + wx.wxTREE_HITTEST_ONITEMLABEL
      + wx.wxTREE_HITTEST_ONITEMICON + wx.wxTREE_HITTEST_ONITEMRIGHT)
    local item_id, flags = ctrl:HitTest(event:GetPosition())

    if item_id and item_id:IsOk() and bit.band(flags, mask) > 0 then
      ctrl:ActivateItem(item_id)
    else
      event:Skip()
    end
    return true
  end

  ctrl:Connect(wx.wxEVT_TIMER, function(event)
      if event:GetId() == REFRESH then outlineRefresh(GetEditor(), false) end
      if event:GetId() == REINDEX then ide:DoWhenIdle(indexFromQueue) end
    end)
  ctrl:Connect(wx.wxEVT_LEFT_DOWN, activateByPosition)
  ctrl:Connect(wx.wxEVT_LEFT_DCLICK, activateByPosition)
  ctrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED, function(event)
      ctrl:ActivateItem(event:GetItem())
    end)

  ctrl:Connect(ID_OUTLINESORT, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      ide.config.outline.sort = not ide.config.outline.sort
      for editor, cache in pairs(caches) do
        ide:SetStatus(("Refreshing '%s'..."):format(ide:GetDocument(editor):GetFileName()))
        local isexpanded = ctrl:IsExpanded(cache.fileitem)
        outlineRefresh(editor, true)
        if not isexpanded then ctrl:Collapse(cache.fileitem) end
      end
      ide:SetStatus('')
    end)

  ctrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function (event)
      local menu = wx.wxMenu {
        { ID_OUTLINESORT, TR("Sort By Name"), "", wx.wxITEM_CHECK },
      }
      menu:Check(ID_OUTLINESORT, ide.config.outline.sort)

      PackageEventHandle("onMenuOutline", menu, ctrl, event)

      ctrl:PopupMenu(menu)
    end)


  local function reconfigure(pane)
    pane:TopDockable(false):BottomDockable(false)
        :MinSize(150,-1):BestSize(300,-1):FloatingSize(200,300)
  end

  local layout = ide:GetSetting("/view", "uimgrlayout")
  if not layout or not layout:find("outlinepanel") then
    ide:AddPanelDocked(ide:GetProjectNotebook(), ctrl, "outlinepanel", TR("Outline"), reconfigure, false)
  else
    ide:AddPanel(ctrl, "outlinepanel", TR("Outline"), reconfigure)
  end
end

local function eachNode(eachFunc, root, recursive)
  local ctrl = outline.outlineCtrl
  local item = ctrl:GetFirstChild(root or ctrl:GetRootItem())
  while true do
    if not item:IsOk() then break end
    if eachFunc and eachFunc(ctrl, item) then break end
    if recursive and ctrl:ItemHasChildren(item) then eachNode(eachFunc, item, recursive) end
    item = ctrl:GetNextSibling(item)
  end
end

createOutlineWindow()

local pathsep = GetPathSeparator()
local function isInSubDir(name, path)
  return #name > #path and path..pathsep == name:sub(1, #path+#pathsep)
end

local function isIgnoredInIndex(name)
  local ignoredirs = outline.settings.ignoredirs
  if ignoredirs[name] then return true end

  -- check through ignored dirs to see if any of them match the file
  for path in pairs(ignoredirs) do
    if isInSubDir(name, path) then return true end
  end

  return false
end

local function purgeIndex(path)
  local symbols = outline.settings.symbols
  for name in pairs(symbols) do
    if isInSubDir(name, path) then outline:UpdateSymbols(name, nil) end
  end
end

local function purgeQueue(path)
  local curqueue = outline.indexqueue
  local newqueue = {[0] = {}}
  for _, name in ipairs(curqueue) do
    if not isInSubDir(name, path) then
      table.insert(newqueue, name)
      newqueue[0][name] = true
    end
  end
  outline.indexqueue = newqueue
end

local function disableIndex(path)
  outline.settings.ignoredirs[path] = true
  outline:SaveSettings(true)

  -- purge the path from the index and the (current) queue
  purgeIndex(path)
  purgeQueue(path)
end

local function enableIndex(path)
  outline.settings.ignoredirs[path] = nil
  outline:SaveSettings(true)
  outline:RefreshSymbols(path)
end

local package = ide:AddPackage('core.outline', {
    -- remove the editor from the list
    onEditorClose = function(self, editor)
      local cache = caches[editor]
      local fileitem = cache and cache.fileitem
      caches[editor] = nil -- remove from cache
      if (ide.config.outline or {}).showonefile then return end
      if fileitem then outline.outlineCtrl:Delete(fileitem) end
    end,

    -- handle rename of the file in the current editor
    onEditorSave = function(self, editor)
      if (ide.config.outline or {}).showonefile then return end
      local cache = caches[editor]
      local fileitem = cache and cache.fileitem
      local doc = ide:GetDocument(editor)
      local ctrl = outline.outlineCtrl
      if doc and fileitem and ctrl:GetItemText(fileitem) ~= doc:GetTabText() then
        ctrl:SetItemText(fileitem, doc:GetTabText())
      end
      local path = doc and doc:GetFilePath()
      if path and cache and cache.funcs then
        outline:UpdateSymbols(path, cache.funcs.updated > editor.updated and cache.funcs or nil)
        outline:SaveSettings()
      end
    end,

    -- go over the file items to turn bold on/off or collapse/expand
    onEditorFocusSet = function(self, editor)
      if (ide.config.outline or {}).showonefile and ide.config.outlineinactivity then
        outlineRefresh(editor, true)
        return
      end

      local cache = caches[editor]
      local fileitem = cache and cache.fileitem
      local ctrl = outline.outlineCtrl
      local itemname = ide:GetDocument(editor):GetTabText()

      -- update file name if it changed in the editor
      if fileitem and ctrl:GetItemText(fileitem) ~= itemname then
        ctrl:SetItemText(fileitem, itemname)
      end

      -- if the editor is not in the cache, which may happen if the user
      -- quickly switches between tabs that don't have outline generated,
      -- regenerate it manually
      if not cache then resetOutlineTimer() end
      resetIndexTimer()

      eachNode(function(ctrl, item)
          local found = fileitem and item:GetValue() == fileitem:GetValue()
          if not found and ctrl:IsBold(item) then
            ctrl:SetItemBold(item, false)
            ctrl:CollapseAllChildren(item)
          end
        end)

      if fileitem and not ctrl:IsBold(fileitem) then
        -- run the following changes on idle as doing them inline is causing a strange
        -- issue on OSX when clicking on a tab may skip several tabs (#546);
        -- this is somehow caused by `ExpandAllChildren` triggered from `SetFocus` inside
        -- `PAGE_CHANGED` handler for the notebook.
        ide:DoWhenIdle(function()
            -- check if this editor is still in the cache,
            -- as it may be closed before this handler is executed
            if not caches[editor] then return end
            ctrl:SetItemBold(fileitem, true)
            if (ide.config.outline or {}).showcompact then
              ctrl:Expand(fileitem)
            else
              ctrl:ExpandAllChildren(fileitem)
            end
            ctrl:ScrollTo(fileitem)
            ctrl:SetScrollPos(wx.wxHORIZONTAL, 0, true)
          end)
      end
    end,

    onMenuFiletree = function(self, menu, tree, event)
      local item_id = event:GetItem()
      local name = tree:GetItemFullName(item_id)
      local symboldirmenu = wx.wxMenu {
        {ID_SYMBOLDIRREFRESH, TR("Refresh Index"), TR("Refresh indexed symbols from files in the selected directory")},
        {ID_SYMBOLDIRDISABLE, TR("Disable Indexing For '%s'"):format(name), TR("Ignore and don't index symbols from files in the selected directory")},
      }
      local _, _, projdirpos = ide:FindMenuItem(ID_PROJECTDIR, menu)
      if projdirpos then
        local ignored = isIgnoredInIndex(name)
        local enabledirmenu = wx.wxMenu()
        local paths = {}
        for path in pairs(outline.settings.ignoredirs) do table.insert(paths, path) end
        table.sort(paths)
        for i, path in ipairs(paths) do
          local id = ID("file.enablesymboldir."..i)
          enabledirmenu:Append(id, path, "")
          tree:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, function() enableIndex(path) end)
        end

        symboldirmenu:Append(wx.wxMenuItem(symboldirmenu, ID_SYMBOLDIRENABLE,
          TR("Enable Indexing"), "", wx.wxITEM_NORMAL, enabledirmenu))
        menu:Insert(projdirpos+1, wx.wxMenuItem(menu, ID_SYMBOLDIRINDEX,
          TR("Symbol Index"), "", wx.wxITEM_NORMAL, symboldirmenu))

        -- disable "enable" if it's empty
        menu:Enable(ID_SYMBOLDIRENABLE, #paths > 0)
        -- disable "refresh" and "disable" if the directory is ignored
        -- or if any of the directories above it are ignored
        menu:Enable(ID_SYMBOLDIRREFRESH, tree:IsDirectory(item_id) and not ignored)
        menu:Enable(ID_SYMBOLDIRDISABLE, tree:IsDirectory(item_id) and not ignored)

        tree:Connect(ID_SYMBOLDIRREFRESH, wx.wxEVT_COMMAND_MENU_SELECTED, function()
            -- purge files in this directory as some might have been removed;
            -- files will be purged based on time, but this is a good time to clean.
            purgeIndex(name)
            outline:RefreshSymbols(name)
            resetIndexTimer(1) -- start after 1ms
          end)
        tree:Connect(ID_SYMBOLDIRDISABLE, wx.wxEVT_COMMAND_MENU_SELECTED, function()
            disableIndex(name)
          end)
       end
    end,

    onEditorUpdateUI = function(self, editor, event)
      -- only update when content or selection changes; ignore scrolling events
      if bit.band(event:GetUpdated(), wxstc.wxSTC_UPDATE_CONTENT + wxstc.wxSTC_UPDATE_SELECTION) > 0 then
        ide.outline.needrefresh = editor
      end
    end,

    onIdle = function(self)
      local editor = ide.outline.needrefresh
      if not editor then return end

      ide.outline.needrefresh = nil

      local ctrl = ide.outline.outlineCtrl
      if not ide:IsWindowShown(ctrl) then return end

      local cache = ide:IsValidCtrl(editor) and caches[editor]
      if not cache or not ide.config.outline.showcurrentfunction then return end

      local edpos = editor:GetCurrentPos()+1
      local edline = editor:LineFromPosition(edpos-1)+1
      if cache.pos and cache.pos == edpos then return end
      if cache.line and cache.line == edline then return end

      cache.pos = edpos
      cache.line = edline

      local n = 0
      local MIN, MAX = 1, 2
      local visible = {[MIN] = math.huge, [MAX] = 0}
      local needshown = {[MIN] = math.huge, [MAX] = 0}

      ctrl:Unselect()
      -- scan all items recursively starting from the current file
      eachNode(function(ctrl, item)
          local func = cache.funcs[ctrl:GetItemData(item):GetData()]
          local val = edpos >= func.pos and func.poe and edpos <= func.poe
          if edline == editor:LineFromPosition(func.pos)+1
          or (func.poe and edline == editor:LineFromPosition(func.poe)+1) then
            cache.line = nil
          end
          ctrl:SetItemBold(item, val)
          if val then ctrl:SelectItem(item, val) end

          if not ide.config.outline.jumptocurrentfunction then return end
          n = n + 1
          -- check that this and the items around it are all visible;
          -- this is to avoid the situation when the current item is only partially visible
          local isvisible = ctrl:IsVisible(item) and ctrl:GetNextVisible(item):IsOk() and ctrl:GetPrevVisible(item):IsOk()
          if val and not isvisible then
            needshown[MIN] = math.min(needshown[MIN], n)
            needshown[MAX] = math.max(needshown[MAX], n)
          elseif isvisible then
            visible[MIN] = math.min(visible[MIN], n)
            visible[MAX] = math.max(visible[MAX], n)
          end
        end, cache.fileitem, true)

      if not ide.config.outline.jumptocurrentfunction then return end
      if needshown[MAX] > visible[MAX] then
        ctrl:ScrollLines(needshown[MAX]-visible[MAX]) -- scroll forward to the last hidden line
      elseif needshown[MIN] < visible[MIN] then
        ctrl:ScrollLines(needshown[MIN]-visible[MIN]) -- scroll backward to the first hidden line
      end
    end,
  })

local function queuePath(path)
  -- only queue if symbols inactivity is set, so files will be indexed
  if ide.config.symbolindexinactivity and not outline.indexqueue[0][path] then
    outline.indexqueue[0][path] = true
    table.insert(outline.indexqueue, 1, path)
  end
end

function outline:GetFileSymbols(path)
  local symbols = self.settings.symbols[path]
  -- queue path to process when appropriate
  if not symbols then queuePath(path) end
  return symbols
end

function outline:GetEditorSymbols(editor)
  -- force token refresh (as these may be not updated yet)
  if #editor:GetTokenList() == 0 then
    while IndicateAll(editor) do end
  end

  -- only refresh the functions when none is present
  if not caches[editor] or #caches[editor].funcs == 0 then outlineRefresh(editor, true) end
  return caches[editor].funcs
end

function outline:RefreshSymbols(path, callback)
  if isIgnoredInIndex(path) then return end

  local exts = {}
  for _, ext in pairs(ide:GetKnownExtensions()) do
    local spec = GetSpec(ext)
    if spec and spec.marksymbols then table.insert(exts, ext) end
  end

  local opts = {sort = false, folder = false, skipbinary = true, yield = true,
    -- skip those directories that are on the "ignore" list
    ondirectory = function(name) return outline.settings.ignoredirs[name] == nil end
  }
  local nextfile = coroutine.wrap(function() FileSysGetRecursive(path, true, table.concat(exts, ";"), opts) end)
  while true do
    local file = nextfile()
    if not file then break end
    if not isIgnoredInIndex(file) then (callback or queuePath)(file) end
  end
end

function outline:UpdateSymbols(fname, symb)
  local symbols = self.settings.symbols
  symbols[fname] = symb

  -- purge outdated records
  local threshold = TimeGet() - 60*60*24*7 -- cache for 7 days
  if not self.indexpurged then
    for k, v in pairs(symbols) do
      if v.updated < threshold then symbols[k] = nil end
    end
    self.indexpurged = true
  end

  self.needsaving = true
end

function outline:SaveSettings(force)
  if self.needsaving or force then
    ide:PushStatus(TR("Updating symbol index and settings..."))
    package:SetSettings(self.settings, {keyignore = {depth = true, image = true, poe = true, item = true, skip = true}})
    ide:PopStatus()
    self.needsaving = false
  end
end

MergeSettings(outline.settings, package:GetSettings())
