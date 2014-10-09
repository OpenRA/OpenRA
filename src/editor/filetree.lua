-- Copyright 2011-14 Paul Kulchenko, ZeroBrane LLC
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
  projtree = nil,
}
local filetree = ide.filetree

local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
local pathsep = GetPathSeparator()
local q = EscapeMagic

-- generic tree
-- ------------

local image = { DIRECTORY = 0, FILEKNOWN = 1, FILEOTHER = 2 }

do
  local getBitmap = (ide.app.createbitmap or wx.wxArtProvider.GetBitmap)
  local size = wx.wxSize(16, 16)
  filetree.imglist = wx.wxImageList(16,16)
  filetree.imglist:Add(getBitmap("FOLDER", "OTHER", size)) -- 0 = directory
  filetree.imglist:Add(getBitmap("FILE-KNOWN", "OTHER", size)) -- 1 = file known spec
  filetree.imglist:Add(getBitmap("FILE-NORMAL", "OTHER", size)) -- 2 = file other
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

  for _, file in ipairs(FileSysGetRecursive(rootdir)) do
    local name, dir = file:match("([^"..pathsep.."]+)("..pathsep.."?)$")
    local known = GetSpec(GetFileExt(name))
    local icon = #dir>0 and image.DIRECTORY or known and image.FILEKNOWN or image.FILEOTHER
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
      if #dir>0 then tree:SetItemHasChildren(curr, FileDirHasContent(file)) end
    end
    if curr:IsOk() then cache[iscaseinsensitive and name:lower() or name] = curr end
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
  function tree:IsFileKnown(item_id) return isIt(item_id, image.FILEKNOWN) end
  function tree:IsFileOther(item_id) return isIt(item_id, image.FILEOTHER) end
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
    if (tree:IsDirectory(item_id)) then
      if wx.wxDirExists(name) then treeAddDir(tree,item_id,name)
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
    else -- open file
      if wx.wxFileExists(name) then LoadFile(name,nil,true)
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
    end
  end

  local empty = ""
  local function renameItem(itemsrc, target)
    local isdir = tree:GetItemImage(itemsrc) == image.DIRECTORY
    local isnew = tree:GetItemText(itemsrc) == empty
    local source = tree:GetItemFullName(itemsrc)
    local fn = wx.wxFileName(target)

    if wx.wxFileName(source):SameAs(fn) then return false end

    local docs = {}
    if not isnew then -- find if source is already opened in the editor
      docs = (isdir
        and ide:FindDocumentsByPartialPath(source)
        or {ide:FindDocument(source)})
      for _, doc in ipairs(docs) do
        if SaveModifiedDialog(doc.editor, true) == wx.wxID_CANCEL then return end
      end
    end

    -- check if existing file/dir is going to be overwritten
    if (wx.wxFileExists(target) or wx.wxDirExists(target))
    and not ApproveFileOverwrite() then return false end

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

    refreshAncestors(tree:GetItemParent(itemsrc))
    -- load file(s) into the same editor (if any); will also refresh the tree
    if #docs > 0 then
      for _, doc in ipairs(docs) do
        local fullpath = doc.filePath
        doc.filePath = nil -- remove path to avoid "file no longer exists" message
        -- when moving folders, /foo/bar/file.lua can be replaced with
        -- /foo/baz/bar/file.lua, so change /foo/bar to /foo/baz/bar
        LoadFile(fullpath:gsub(q(source), target), doc.editor)
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
    return true
  end
  local function deleteItem(item_id)
    local isdir = tree:GetItemImage(item_id) == image.DIRECTORY
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

  -- handle context menu
  local function addItem(item_id, name, img)
    local isdir = tree:GetItemImage(item_id) == image.DIRECTORY
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
    function()
      local tdo = wx.wxTextDataObject(tree:GetItemFullName(tree:GetSelection()))
      if wx.wxClipboard:Get():Open() then
        wx.wxClipboard:Get():SetData(tdo)
        wx.wxClipboard:Get():Close()
      end
    end)
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
  tree:Connect(ID_SHOWLOCATION, wx.wxEVT_COMMAND_MENU_SELECTED,
    function() ShowLocation(tree:GetItemFullName(tree:GetSelection())) end)

  tree:Connect(wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function (event)
      local item_id = event:GetItem()
      tree:SelectItem(item_id)

      local renamelabel = (tree:IsRoot(item_id)
        and TR("&Edit Project Directory")
        or TR("&Rename"))
      local menu = wx.wxMenu {
        { ID_NEWFILE, TR("New &File") },
        { ID_NEWDIRECTORY, TR("&New Directory") },
        { },
        { ID_RENAMEFILE, renamelabel..KSC(ID_RENAMEFILE) },
        { ID_DELETEFILE, TR("&Delete")..KSC(ID_DELETEFILE) },
        { },
        { ID_OPENEXTENSION, TR("Open With Default Program") },
        { ID_COPYFULLPATH, TR("Copy Full Path") },
        { ID_SHOWLOCATION, TR("Show Location") },
      }
      local projectdirectorymenu = wx.wxMenu {
        { },
        {ID_PROJECTDIRCHOOSE, TR("Choose...")..KSC(ID_PROJECTDIRCHOOSE), TR("Choose a project directory")},
      }
      local projectdirectory = wx.wxMenuItem(menu, ID_PROJECTDIR,
        TR("Project Directory"), TR("Set the project directory to be used"),
        wx.wxITEM_NORMAL, projectdirectorymenu)
      menu:Insert(6, projectdirectory)
      FileTreeProjectListUpdate(projectdirectorymenu, 0)

      -- disable Delete on non-empty directories
      local isdir = tree:GetItemImage(item_id) == image.DIRECTORY
      if isdir then
        local source = tree:GetItemFullName(item_id)
        menu:Enable(ID_DELETEFILE, not FileDirHasContent(source..pathsep))
        menu:Enable(ID_OPENEXTENSION, false)
      else
        local fname = tree:GetItemText(item_id)
        local ext = '.'..wx.wxFileName(fname):GetExt()
        local ft = wx.wxTheMimeTypesManager:GetFileTypeFromExtension(ext)
        menu:Enable(ID_OPENEXTENSION, ft and #ft:GetOpenCommand("") > 0)
      end

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
        if tree:GetItemImage(item_id) == image.DIRECTORY then
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
      if not itemsrc:IsOk() then event:Veto() end
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
      if tree:IsRoot(itemsrc) then
        if not event:IsEditCancelled() and wx.wxDirExists(label) then
          ProjectUpdateProjectDir(label)
        end
        return
      end

      if not parent or not parent:IsOk() then return end
      local sourcedir = tree:GetItemFullName(parent)
      local target = MergeFullPath(sourcedir, label)
      if event:IsEditCancelled() or label == empty
      or target and not renameItem(itemsrc, target)
      then refreshAncestors(parent) end
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
  + wx.wxTR_EDIT_LABELS)
projtree:SetFont(ide.font.fNormal)
filetree.projtree = projtree

local projnotebook = ide.frame.projnotebook
projnotebook:AddPage(projtree, "Project", true)

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

  local item_id = projtree:FindItem(file)

  -- if the select item is different from the current one
  -- or the current one is the same, but not bold (which may happen when
  -- the project is changed to one that includes the current item)
  if curr_file ~= file
  or item_id and not projtree:IsBold(item_id) then
    if curr_file then
      local curr_id = projtree:FindItem(curr_file)
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
