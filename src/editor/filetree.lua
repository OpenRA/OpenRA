-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
--
-- filetree, treectrl for drive & project
--

ide.filetree = {
  projdir = "",
  projdirlist = {},
  projdirpartmap = {},
  projtreeCtrl = nil,
  imglist = ide:CreateImageList("PROJECT",
    "FOLDER", "FILE-KNOWN", "FILE-NORMAL", "FILE-NORMAL-START",
    "FOLDER-MAPPED"),
  settings = {extensionignore = {}, startfile = {}, mapped = {}},
}
local filetree = ide.filetree
local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
local pathsep = GetPathSeparator()
local q = EscapeMagic
local image = {
  DIRECTORY = 0, FILEKNOWN = 1, FILEOTHER = 2, FILEOTHERSTART = 3,
  DIRECTORYMAPPED = 4,
}

MergeSettings(filetree.settings, ide:AddPackage('core.filetree', {}):GetSettings())

-- generic tree
-- ------------

local function getIcon(name, isdir)
  local startfile = GetFullPathIfExists(FileTreeGetDir(),
    filetree.settings.startfile[FileTreeGetDir()])
  local known = GetSpec(GetFileExt(name))
  local icon = isdir and image.DIRECTORY or known and image.FILEKNOWN or image.FILEOTHER
  if startfile and startfile == name then icon = image.FILEOTHERSTART end
  return icon
end

local function treeAddDir(tree,parent_id,rootdir)
  local items = {}
  local item, cookie = tree:GetFirstChild(parent_id)
  while item:IsOk() do
    items[tree:GetItemText(item) .. tree:GetItemImage(item)] = item
    item, cookie = tree:GetNextChild(parent_id, cookie)
  end

  local cache = {}
  local curr
  local files = FileSysGetRecursive(rootdir)
  local dirmapped = {}
  if tree:IsRoot(parent_id) then
    local mapped = filetree.settings.mapped[FileTreeGetDir()] or {}
    table.sort(mapped)
    -- insert into files at the sorted order
    for i, v in ipairs(mapped) do
      table.insert(files, i, v)
      dirmapped[v] = true
    end
  end

  for _, file in ipairs(files) do
    local name, dir = file:match("([^"..pathsep.."]+)("..pathsep.."?)$")
    local isdir = #dir>0
    if isdir or not filetree.settings.extensionignore[GetFileExt(name)] then
      local icon = getIcon(file, isdir)

      -- keep full name for the mapped directories
      if dirmapped[file] then name, icon = file, image.DIRECTORYMAPPED end

      local item = items[name .. icon]
      if item then -- existing item
        -- keep deleting items until we find item
        while true do
          local next = (curr
            and tree:GetNextSibling(curr)
            or tree:GetFirstChild(parent_id))
          if not next:IsOk() or name == tree:GetItemText(next) then
            curr = next
            break
          end
          tree:Delete(next)
        end
      else -- new item
        curr = (curr
          and tree:InsertItem(parent_id, curr, name, icon)
          or tree:PrependItem(parent_id, name, icon))
        if isdir then tree:SetItemHasChildren(curr, FileDirHasContent(file)) end
      end
      if curr:IsOk() then cache[iscaseinsensitive and name:lower() or name] = curr end
    end
  end

  -- delete any leftovers (something that exists in the tree, but not on disk)
  while true do
    local next = (curr
      and tree:GetNextSibling(curr)
      or tree:GetFirstChild(parent_id))
    if not next:IsOk() then break end
    tree:Delete(next)
  end

  -- cache the mapping from names to tree items
  if ide.wxver >= "2.9.5" then
    local data = wx.wxLuaTreeItemData()
    data:SetData(cache)
    tree:SetItemData(parent_id, data)
  end

  tree:SetItemHasChildren(parent_id,
    tree:GetChildrenCount(parent_id, false) > 0)
end

local function treeSetRoot(tree,rootdir)
  tree:DeleteAllItems()
  if (not wx.wxDirExists(rootdir)) then return end

  local root_id = tree:AddRoot(rootdir, image.DIRECTORY)
  tree:SetItemHasChildren(root_id, true) -- make sure that the item can expand
  tree:Expand(root_id) -- this will also populate the tree
end

local function findItem(tree, match)
  local node = tree:GetRootItem()
  local label = tree:GetItemText(node)

  local s, e
  if iscaseinsensitive then
    s, e = string.find(match:lower(), label:lower(), 1, true)
  else
    s, e = string.find(match, label, 1, true)
  end
  if not s or s ~= 1 then return end

  for token in string.gmatch(string.sub(match,e+1), "[^%"..pathsep.."]+") do
    local data = tree:GetItemData(node)
    local cache = data and data:GetData()
    if cache and cache[iscaseinsensitive and token:lower() or token] then
      node = cache[iscaseinsensitive and token:lower() or token]
    else
      -- token is missing; may need to re-scan the folder; maybe new file
      local dir = tree:GetItemFullName(node)
      treeAddDir(tree,node,dir)

      local item, cookie = tree:GetFirstChild(node)
      while true do
        if not item:IsOk() then return end -- not found
        if tree:GetItemText(item) == token then
          node = item
          break
        end
        item, cookie = tree:GetNextChild(node, cookie)
      end
    end
  end

  -- this loop exits only when a match is found
  return node
end

local function treeSetConnectorsAndIcons(tree)
  tree:AssignImageList(filetree.imglist)

  local function isIt(item, imgtype) return tree:GetItemImage(item) == imgtype end

  function tree:IsDirectory(item_id) return isIt(item_id, image.DIRECTORY) end
  function tree:IsDirMapped(item_id) return isIt(item_id, image.DIRECTORYMAPPED) end
  function tree:IsFileKnown(item_id) return isIt(item_id, image.FILEKNOWN) end
  function tree:IsFileOther(item_id) return isIt(item_id, image.FILEOTHER) end
  function tree:IsFileStart(item_id) return isIt(item_id, image.FILEOTHERSTART) end
  function tree:IsRoot(item_id) return not tree:GetItemParent(item_id):IsOk() end

  function tree:FindItem(match)
    return findItem(self, (wx.wxIsAbsolutePath(match) or match == '') and match
      or MergeFullPath(ide:GetProject(), match))
  end

  function tree:GetItemFullName(item_id)
    local tree = self
    local str = tree:GetItemText(item_id)
    local cur = str

    while (#cur > 0) do
      item_id = tree:GetItemParent(item_id)
      if not item_id:IsOk() then break end
      cur = tree:GetItemText(item_id)
      if cur and string.len(cur) > 0 then str = cur..pathsep..str end
    end
    -- as root may already include path separator, normalize the path
    local fullPath = wx.wxFileName(str)
    fullPath:Normalize()
    return fullPath:GetFullPath()
  end

  function tree:RefreshChildren(node)
    node = node or tree:GetRootItem()
    treeAddDir(tree,node,tree:GetItemFullName(node))
    local item, cookie = tree:GetFirstChild(node)
    while true do
      if not item:IsOk() then return end
      if tree:IsExpanded(item) then tree:RefreshChildren(item) end
      item, cookie = tree:GetNextChild(node, cookie)
    end
  end

  local function refreshAncestors(node)
    -- when this method is called from END_EDIT, it causes infinite loop
    -- on OSX (wxwidgets 2.9.5) as Delete in treeAddDir calls END_EDIT again.
    -- disable handlers while the tree is populated and then enable back.
    tree:SetEvtHandlerEnabled(false)
    while node:IsOk() do
      local dir = tree:GetItemFullName(node)
      treeAddDir(tree,node,dir)
      node = tree:GetItemParent(node)
    end
    tree:SetEvtHandlerEnabled(true)
  end

  function tree:ActivateItem(item_id)
    local name = tree:GetItemFullName(item_id)

    local event = wx.wxTreeEvent(wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED, item_id:GetValue())
    if PackageEventHandle("onFiletreeActivate", tree, event, item_id) == false then
      return
    end

    -- refresh the folder
    if (tree:IsDirectory(item_id) or tree:IsDirMapped(item_id)) then
      if wx.wxDirExists(name) then treeAddDir(tree,item_id,name)
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
    else -- open file
      if wx.wxFileExists(name) then LoadFile(name,nil,true)
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
    end
  end

  local function unMapDir(dir)
    local project = FileTreeGetDir()
    local mapped = filetree.settings.mapped[project] or {}
    for k, m in ipairs(mapped) do
      if m == dir then table.remove(mapped, k) end
    end
    filetree.settings.mapped[project] = mapped
    refreshAncestors(tree:GetRootItem())
  end
  local function mapDir()
    local project = FileTreeGetDir()
    local dirPicker = wx.wxDirDialog(ide.frame, TR("Choose a directory to map"),
      project ~= "" and project or wx.wxGetCwd(), wx.wxDIRP_DIR_MUST_EXIST)
    if dirPicker:ShowModal(true) ~= wx.wxID_OK then return end
    local dir = wx.wxFileName.DirName(FixDir(dirPicker:GetPath())):GetFullPath()
    local mapped = filetree.settings.mapped[project] or {}
    for _, m in ipairs(mapped) do
      if m == dir then return end -- already on the list
    end
    table.insert(mapped, dir)
    filetree.settings.mapped[project] = mapped
    refreshAncestors(tree:GetRootItem())
  end

  local empty = ""
  local function renameItem(itemsrc, target)
    local cache = type(itemsrc) == 'table' and itemsrc or nil
    local isdir = not cache and tree:IsDirectory(itemsrc) or cache and cache.isdir or false
    local isnew = not cache and tree:GetItemText(itemsrc) == empty or cache and cache.isnew or false
    local source = cache and cache.fullname or tree:GetItemFullName(itemsrc)
    local fn = wx.wxFileName(target)

    -- check if the target is the same as the source;
    -- SameAs check is not used here as "Test" and "test" are the same
    -- on case insensitive systems, but need to be allowed in renaming.
    if source == target then return end

    local docs = {}
    if not isnew then -- find if source is already opened in the editor
      docs = (isdir
        and ide:FindDocumentsByPartialPath(source)
        or {ide:FindDocument(source)})
      for _, doc in ipairs(docs) do
        if not isdir and PackageEventHandle("onEditorPreSave", doc.editor, source) == false then
          return false
        end
        if SaveModifiedDialog(doc.editor, true) == wx.wxID_CANCEL then return end
      end
    end

    -- check if existing file/dir is going to be overwritten
    local overwrite = ((wx.wxFileExists(target) or wx.wxDirExists(target))
      and not wx.wxFileName(source):SameAs(fn))
    if overwrite and not ApproveFileOverwrite() then return false end

    if not fn:Mkdir(tonumber(755,8), wx.wxPATH_MKDIR_FULL) then
      ReportError(TR("Unable to create directory '%s'."):format(target))
      return false
    end

    if isnew then -- new directory or file; create manually
      if (isdir and not wx.wxFileName.DirName(target):Mkdir(tonumber(755,8), wx.wxPATH_MKDIR_FULL))
      or (not isdir and not FileWrite(target, "")) then
        ReportError(TR("Unable to create file '%s'."):format(target))
        return false
      end
    else -- existing directory or file; rename/move it
      local ok, err = FileRename(source, target)
      if not ok then
        ReportError(TR("Unable to rename file '%s'."):format(source)
          .."\nError: "..err)
        return false
      end
    end

    refreshAncestors(cache and cache.parent or tree:GetItemParent(itemsrc))
    -- load file(s) into the same editor (if any); will also refresh the tree
    if #docs > 0 then
      for _, doc in ipairs(docs) do
        local fullpath = doc.filePath
        doc.filePath = nil -- remove path to avoid "file no longer exists" message
        -- when moving folders, /foo/bar/file.lua can be replaced with
        -- /foo/baz/bar/file.lua, so change /foo/bar to /foo/baz/bar
        local path = (not iscaseinsensitive and fullpath:gsub(q(source), target)
          or fullpath:lower():gsub(q(source:lower()), target))
        local editor = LoadFile(path)
        -- check if the file was loaded into another editor;
        -- this is possible is "foo" is renamed to "bar" and both are opened;
        -- if this happens, then "bar" is refreshed and "foo" can be closed.
        if doc.editor:GetId() ~= editor:GetId() then ClosePage(doc.index) end
        if not isdir and editor then PackageEventHandle("onEditorSave", editor) end
      end
    else -- refresh the tree and select the new item
      local itemdst = tree:FindItem(target)
      if itemdst then
        refreshAncestors(tree:GetItemParent(itemdst))
        tree:SelectItem(itemdst)
        tree:EnsureVisible(itemdst)
        tree:SetScrollPos(wx.wxHORIZONTAL, 0, true)
      end
    end

    -- refresh the target if it's open and has been overwritten
    if overwrite and not isdir then
      local doc = ide:FindDocument(target)
      if doc then LoadFile(doc:GetFilePath(), doc:GetEditor()) end
    end

    return true
  end
  local function deleteItem(item_id)
    -- if delete is for mapped directory, unmap it instead
    if tree:IsDirMapped(item_id) then
      unMapDir(tree:GetItemText(item_id))
      return
    end

    local isdir = tree:IsDirectory(item_id)
    local source = tree:GetItemFullName(item_id)

    if isdir and FileDirHasContent(source..pathsep) then return false end
    if wx.wxMessageBox(
      TR("Do you want to delete '%s'?"):format(source),
      GetIDEString("editormessage"),
      wx.wxYES_NO + wx.wxCENTRE, ide.frame) ~= wx.wxYES then return false end

    if isdir then
      if not wx.wxRmdir(source) then
        ReportError(TR("Unable to delete directory '%s': %s")
          :format(source, wx.wxSysErrorMsg()))
      end
    else
      local doc = ide:FindDocument(source)
      if doc then ClosePage(doc.index) end
      wx.wxRemoveFile(source)
    end
    refreshAncestors(tree:GetItemParent(item_id))
    return true
  end

  tree:Connect(wx.wxEVT_COMMAND_TREE_ITEM_EXPANDING,
    function (event)
      local item_id = event:GetItem()
      local dir = tree:GetItemFullName(item_id)
      if wx.wxDirExists(dir) then treeAddDir(tree,item_id,dir) -- refresh folder
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
      return true
    end)
  tree:Connect(wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED,
    function (event)
      tree:ActivateItem(event:GetItem())
    end)

  local function saveSettings()
    ide:AddPackage('core.filetree', {}):SetSettings(filetree.settings)
  end

  -- refresh the tree
  local function refreshChildren()
    tree:RefreshChildren()
    -- now mark the current file (if it was previously disabled)
    local editor = ide:GetEditor()
    if editor then FileTreeMarkSelected(ide:GetDocument(editor):GetFilePath()) end
  end

  -- handle context menu
  local function addItem(item_id, name, img)
    local isdir = tree:IsDirectory(item_id)
    local parent = isdir and item_id or tree:GetItemParent(item_id)
    if isdir then tree:Expand(item_id) end -- expand to populate if needed

    local item = tree:PrependItem(parent, name, img)
    tree:SetItemHasChildren(parent, true)
    -- temporarily disable expand as we don't need this node populated
    tree:SetEvtHandlerEnabled(false)
    tree:EnsureVisible(item)
    tree:SetEvtHandlerEnabled(true)
    return item
  end

  local function unsetStartFile()
    local project = FileTreeGetDir()
    local startfile = filetree.settings.startfile[project]
    filetree.settings.startfile[project] = nil
    if startfile then
      local item_id = tree:FindItem(startfile)
      if item_id and item_id:IsOk() then
        tree:SetItemImage(item_id, getIcon(tree:GetItemFullName(item_id)))
      end
    end
  end

  local function setStartFile(item_id)
    local project = FileTreeGetDir()
    local startfile = tree:GetItemFullName(item_id):gsub(project, "")
    filetree.settings.startfile[project] = startfile
    tree:SetItemImage(item_id, getIcon(tree:GetItemFullName(item_id)))
  end

  tree:Connect(ID_NEWFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      tree:EditLabel(addItem(tree:GetSelection(), empty, image.FILEOTHER))
    end)
  tree:Connect(ID_NEWDIRECTORY, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      tree:EditLabel(addItem(tree:GetSelection(), empty, image.DIRECTORY))
    end)
  tree:Connect(ID_RENAMEFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() tree:EditLabel(tree:GetSelection()) end)
  tree:Connect(ID_DELETEFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() deleteItem(tree:GetSelection()) end)
  tree:Connect(ID_COPYFULLPATH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() ide:CopyToClipboard(tree:GetItemFullName(tree:GetSelection())) end)
  tree:Connect(ID_OPENEXTENSION, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      local fname = tree:GetItemFullName(tree:GetSelection())
      local ext = '.'..wx.wxFileName(fname):GetExt()
      local ft = wx.wxTheMimeTypesManager:GetFileTypeFromExtension(ext)
      if ft then
        local cmd = ft:GetOpenCommand(fname:gsub('"','\\"'))
        local pid = wx.wxExecute(cmd, wx.wxEXEC_ASYNC)
        if ide.osname == 'Windows' and pid and pid > 0 then
          -- some programs on Windows (for example, PhotoViewer) accept
          -- files with spaces in names ONLY if they are not in quotes.
          -- wait for the process that failed to open file to finish
          -- and retry without quotes.
          wx.wxMilliSleep(250) -- 250ms seems enough; picked empirically.
          if not wx.wxProcess.Exists(pid) then
            local cmd = ft:GetOpenCommand(""):gsub('""%s*$', '')..fname
            wx.wxExecute(cmd, wx.wxEXEC_ASYNC)
          end
        end
      end
    end)
  tree:Connect(ID_REFRESH, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() refreshChildren() end)
  tree:Connect(ID_SHOWLOCATION, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() ShowLocation(tree:GetItemFullName(tree:GetSelection())) end)
  tree:Connect(ID_HIDEEXTENSION, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      local ext = GetFileExt(tree:GetItemText(tree:GetSelection()))
      filetree.settings.extensionignore[ext] = true
      saveSettings()
      refreshChildren()
    end)
  tree:Connect(ID_SHOWEXTENSIONALL, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      filetree.settings.extensionignore = {}
      saveSettings()
      refreshChildren()
    end)
  tree:Connect(ID_SETSTARTFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      unsetStartFile()
      setStartFile(tree:GetSelection())
      saveSettings()
    end)
  tree:Connect(ID_UNSETSTARTFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      unsetStartFile()
      saveSettings()
    end)
  tree:Connect(ID_MAPDIRECTORY, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      mapDir()
      saveSettings()
    end)
  tree:Connect(ID_UNMAPDIRECTORY, wx.wxEVT_COMMAND_MENU_SELECTED,
    function()
      unMapDir(tree:GetItemText(tree:GetSelection()))
      saveSettings()
    end)

  tree:Connect(wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function (event)
      local item_id = event:GetItem()
      tree:SelectItem(item_id)

      local renamelabel = (tree:IsRoot(item_id)
        and TR("&Edit Project Directory")
        or TR("&Rename"))
      local fname = tree:GetItemText(item_id)
      local ext = GetFileExt(fname)
      local startfile = filetree.settings.startfile[FileTreeGetDir()]
      local menu = wx.wxMenu {
        { ID_NEWFILE, TR("New &File") },
        { ID_NEWDIRECTORY, TR("&New Directory") },
        { },
        { ID_RENAMEFILE, renamelabel..KSC(ID_RENAMEFILE) },
        { ID_DELETEFILE, TR("&Delete")..KSC(ID_DELETEFILE) },
        { ID_REFRESH, TR("Refresh") },
        { },
        { ID_HIDEEXTENSION, TR("Hide '.%s' Files"):format(ext) },
        { },
        { ID_SETSTARTFILE, TR("Set As Start File") },
        { ID_UNSETSTARTFILE, TR("Unset '%s' As Start File"):format(startfile or "<none>") },
        { },
        { ID_MAPDIRECTORY, TR("Map Directory...") },
        { ID_UNMAPDIRECTORY, TR("Unmap Directory") },
        { ID_OPENEXTENSION, TR("Open With Default Program") },
        { ID_COPYFULLPATH, TR("Copy Full Path") },
        { ID_SHOWLOCATION, TR("Show Location") },
      }
      local extlist = {
        {},
        { ID_SHOWEXTENSIONALL, TR("Show All Files"), TR("Show all files") },
      }
      for ext in pairs(filetree.settings.extensionignore) do
        local id = ID("filetree.showextension."..ext)
        table.insert(extlist, 1, {id, '.'..ext})
        menu:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, function()
          filetree.settings.extensionignore[ext] = nil
          saveSettings()
          refreshChildren()
        end)
      end
      local _, _, hideextpos = ide:FindMenuItem(ID_HIDEEXTENSION, menu)
      assert(hideextpos, "Can't find HideExtension menu item")
      menu:Insert(hideextpos+1, wx.wxMenuItem(menu, ID_SHOWEXTENSION,
        TR("Show Hidden Files"), TR("Show files previously hidden"),
        wx.wxITEM_NORMAL, wx.wxMenu(extlist)))

      local projectdirectorymenu = wx.wxMenu {
        { },
        {ID_PROJECTDIRCHOOSE, TR("Choose...")..KSC(ID_PROJECTDIRCHOOSE), TR("Choose a project directory")},
      }
      local projectdirectory = wx.wxMenuItem(menu, ID_PROJECTDIR,
        TR("Project Directory"), TR("Set the project directory to be used"),
        wx.wxITEM_NORMAL, projectdirectorymenu)
      local _, _, unmapdirpos = ide:FindMenuItem(ID_UNMAPDIRECTORY, menu)
      assert(unmapdirpos, "Can't find UnMapDirectory menu item")
      menu:Insert(unmapdirpos+1, projectdirectory)
      FileTreeProjectListUpdate(projectdirectorymenu, 0)

      -- disable Delete on non-empty directories
      local isdir = tree:IsDirectory(item_id)
      local ismapped = tree:IsDirMapped(item_id)
      menu:Destroy(ismapped and ID_MAPDIRECTORY or ID_UNMAPDIRECTORY)
      if not startfile then menu:Destroy(ID_UNSETSTARTFILE) end
      if ismapped then menu:Enable(ID_RENAMEFILE, false) end
      if isdir then
        local source = tree:GetItemFullName(item_id)
        menu:Enable(ID_DELETEFILE, not FileDirHasContent(source..pathsep))
        menu:Enable(ID_OPENEXTENSION, false)
        menu:Enable(ID_HIDEEXTENSION, false)
      else
        local ft = wx.wxTheMimeTypesManager:GetFileTypeFromExtension('.'..ext)
        menu:Enable(ID_OPENEXTENSION, ft and #ft:GetOpenCommand("") > 0)
        menu:Enable(ID_HIDEEXTENSION, not filetree.settings.extensionignore[ext])
      end
      menu:Enable(ID_SETSTARTFILE, tree:IsFileOther(item_id) or tree:IsFileKnown(item_id))
      menu:Enable(ID_SHOWEXTENSION, next(filetree.settings.extensionignore) ~= nil)

      PackageEventHandle("onMenuFiletree", menu, tree, event)

      tree:PopupMenu(menu)
    end)

  tree:Connect(wx.wxEVT_RIGHT_DOWN,
    function (event)
      local item_id = tree:HitTest(event:GetPosition())
      if PackageEventHandle("onFiletreeRDown", tree, event, item_id) == false then
        return
      end
      event:Skip()
    end)

  -- toggle a folder on a single click
  tree:Connect(wx.wxEVT_LEFT_DOWN,
    function (event)
      -- only toggle if this is a folder and the click is on the item line
      -- (exclude the label as it's used for renaming and dragging)
      local mask = (wx.wxTREE_HITTEST_ONITEMINDENT
        + wx.wxTREE_HITTEST_ONITEMICON + wx.wxTREE_HITTEST_ONITEMRIGHT)
      local item_id, flags = tree:HitTest(event:GetPosition())

      if PackageEventHandle("onFiletreeLDown", tree, event, item_id) == false then
        return
      end

      if item_id and bit.band(flags, mask) > 0 then
        if tree:IsDirectory(item_id) then
          tree:Toggle(item_id)
          tree:SelectItem(item_id)
        else
          local name = tree:GetItemFullName(item_id)
          if wx.wxFileExists(name) then LoadFile(name,nil,true) end
        end
      else
        event:Skip()
      end
      return true
    end)
  local parent
  tree:Connect(wx.wxEVT_COMMAND_TREE_BEGIN_LABEL_EDIT,
    function (event)
      local itemsrc = event:GetItem()
      parent = tree:GetItemParent(itemsrc)
      if not itemsrc:IsOk() or tree:IsDirMapped(itemsrc) then event:Veto() end
    end)
  tree:Connect(wx.wxEVT_COMMAND_TREE_END_LABEL_EDIT,
    function (event)
      -- veto the event to keep the original label intact as the tree
      -- is going to be refreshed with the correct names.
      event:Veto()

      local itemsrc = event:GetItem()
      if not itemsrc:IsOk() then return end

      local label = event:GetLabel():gsub("^%s+$","") -- clean all spaces

      -- edited the root element; set the new project directory if needed
      local cancelled = event:IsEditCancelled()
      if tree:IsRoot(itemsrc) then
        if not cancelled and wx.wxDirExists(label) then
          ProjectUpdateProjectDir(label)
        end
        return
      end

      if not parent or not parent:IsOk() then return end
      local target = MergeFullPath(tree:GetItemFullName(parent), label)
      if cancelled or label == empty then refreshAncestors(parent)
      elseif target then
        -- normally, none of this caching would be needed as `renameItem`
        -- would be called to check if the item can be renamed;
        -- however, as it may open a dialog box, on Linux it's causing a crash
        -- (caused by the same END_LABEL_EDIT even triggered one more time),
        -- so to protect from that, `renameItem` is called from IDLE event.
        -- Unfortunately, by that time, the filetree item (`itemsrc`) may
        -- already have incorrect state (as it's removed from the tree),
        -- so its properties need to be cached to be used from IDLE event.
        local cache = {
          isdir = tree:IsDirectory(itemsrc),
          isnew = tree:GetItemText(itemsrc) == empty,
          fullname = tree:GetItemFullName(itemsrc),
          parent = parent,
        }
        ide:DoWhenIdle(function()
            if not renameItem(cache, target) then refreshAncestors(parent) end
          end)
      end
    end)

  local itemsrc
  tree:Connect(wx.wxEVT_COMMAND_TREE_BEGIN_DRAG,
    function (event)
      if ide.config.filetree.mousemove and tree:GetItemParent(event:GetItem()):IsOk() then
        itemsrc = event:GetItem()
        event:Allow()
      end
    end)
  tree:Connect(wx.wxEVT_COMMAND_TREE_END_DRAG,
    function (event)
      local itemdst = event:GetItem()
      if not itemdst:IsOk() or not itemsrc:IsOk() then return end

      -- check if itemdst is a folder
      local target = tree:GetItemFullName(itemdst)
      if wx.wxDirExists(target) then
        local source = tree:GetItemFullName(itemsrc)
        -- check if moving the directory and target is a subfolder of source
        if (target..pathsep):find("^"..q(source)..pathsep) then return end
        renameItem(itemsrc, MergeFullPath(target, tree:GetItemText(itemsrc)))
      end
    end)
end

-- project
local projtree = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_LINES_AT_ROOT
  + wx.wxTR_EDIT_LABELS + wx.wxNO_BORDER)
projtree:SetFont(ide.font.fNormal)
filetree.projtreeCtrl = projtree

ide:GetProjectNotebook():AddPage(projtree, TR("Project"), true)

-- proj connectors
-- ---------------

treeSetConnectorsAndIcons(projtree)

-- proj functions
-- ---------------

function filetree:updateProjectDir(newdir)
  if (not newdir) or not wx.wxDirExists(newdir) then return end
  local dirname = wx.wxFileName.DirName(newdir)

  if filetree.projdir and #filetree.projdir > 0
  and dirname:SameAs(wx.wxFileName.DirName(filetree.projdir)) then return end

  -- strip the last path separator if any
  local newdir = dirname:GetPath(wx.wxPATH_GET_VOLUME)

  if filetree.projdir and #filetree.projdir > 0 then
    PackageEventHandle("onProjectClose", filetree.projdir)
  end

  PackageEventHandle("onProjectPreLoad", newdir)

  if ide.config.projectautoopen and filetree.projdir then
    StoreRestoreProjectTabs(filetree.projdir, newdir)
  end

  filetree.projdir = newdir
  filetree.projdirpartmap = {}

  PrependStringToArray(
    filetree.projdirlist,
    newdir,
    ide.config.projecthistorylength,
    function(s1, s2) return dirname:SameAs(wx.wxFileName.DirName(s2)) end)

  ProjectUpdateProjectDir(newdir,true)
  treeSetRoot(projtree,newdir)

  -- sync with the current editor window and activate selected file
  local editor = GetEditor()
  if editor then FileTreeMarkSelected(ide:GetDocument(editor):GetFilePath()) end

  -- refresh Recent Projects menu item
  ide.frame:AddPendingEvent(wx.wxUpdateUIEvent(ID_RECENTPROJECTS))

  PackageEventHandle("onProjectLoad", newdir)
end

function FileTreeGetDir()
  return (filetree.projdir and #filetree.projdir > 0
    and wx.wxFileName.DirName(filetree.projdir):GetFullPath() or nil)
end

function FileTreeSetProjects(tab)
  filetree.projdirlist = tab
  if (tab and tab[1]) then
    filetree:updateProjectDir(tab[1])
  end
end

function FileTreeGetProjects()
  return filetree.projdirlist
end

local function getProjectLabels()
  local labels = {}
  local fmt = ide.config.format.menurecentprojects or '%f'
  for _, proj in ipairs(FileTreeGetProjects()) do
    local config = ide.session.projects[proj]
    local intfname = config and config[2] and config[2].interpreter or ide.interpreter:GetFileName()
    local interpreter = intfname and ide.interpreters[intfname]
    local parts = wx.wxFileName(proj..pathsep):GetDirs()
    table.insert(labels, ExpandPlaceholders(fmt, {
          f = proj,
          i = interpreter and interpreter:GetName() or '?',
          s = parts[#parts] or '',
        }))
  end
  return labels
end

function FileTreeProjectListClear()
  -- remove all items from the list except the current one
  filetree.projdirlist = {FileTreeGetDir()}
end

function FileTreeProjectListUpdate(menu, items)
  -- protect against recent project menu not being present
  if not ide:FindMenuItem(ID_RECENTPROJECTS) then return end

  local list = getProjectLabels()
  for i=#list, 1, -1 do
    local id = ID("file.recentprojects."..i)
    local label = list[i]
    if i <= items then -- this is an existing item; update the label
      menu:FindItem(id):SetItemLabel(label)
    else -- need to add an item
      local item = wx.wxMenuItem(menu, id, label, "")
      menu:Insert(items, item)
      ide.frame:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, function()
          wx.wxSafeYield() -- let the menu on screen (if any) disappear
          ProjectUpdateProjectDir(FileTreeGetProjects()[i])
        end)
    end
    -- disable the currently selected project
    if i == 1 then menu:Enable(id, false) end
  end
  for i=items, #list+1, -1 do -- delete the rest if the list got shorter
    menu:Delete(menu:FindItemByPosition(i-1))
  end
  return #list
end

local curr_file
function FileTreeMarkSelected(file)
  if not file or not filetree.projdir or #filetree.projdir == 0 then return end

  local item_id = wx.wxIsAbsolutePath(file) and projtree:FindItem(file)

  -- if the select item is different from the current one
  -- or the current one is the same, but not bold (which may happen when
  -- the project is changed to one that includes the current item)
  if curr_file ~= file
  or item_id and not projtree:IsBold(item_id) then
    if curr_file then
      local curr_id = wx.wxIsAbsolutePath(curr_file) and projtree:FindItem(curr_file)
      if curr_id and projtree:IsBold(curr_id) then
        projtree:SetItemBold(curr_id, false)
      end
    end
    if item_id then
      projtree:EnsureVisible(item_id)
      projtree:SetScrollPos(wx.wxHORIZONTAL, 0, true)
      projtree:SetItemBold(item_id, true)
    end
    curr_file = file
    if ide.wxver < "2.9.5" and ide.osname == 'Macintosh' then
      projtree:Refresh()
    end
  end
end

function FileTreeFindByPartialName(name)
  -- check if it's already cached
  if filetree.projdirpartmap[name] then return filetree.projdirpartmap[name] end

  -- this function may get a partial name that starts with ... and has
  -- an abbreviated path (as generated by stack traces);
  -- remove starting "..." if any and escape
  local pattern = q(name:gsub("^%.%.%.","")):gsub("[\\/]", "[\\/]").."$"
  local lpattern = pattern:lower()

  for _, file in ipairs(FileSysGetRecursive(filetree.projdir, true)) do
    if file:find(pattern) or iscaseinsensitive and file:lower():find(lpattern) then
      filetree.projdirpartmap[name] = file
      return file
    end
  end
  return
end
