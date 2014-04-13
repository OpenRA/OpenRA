-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--
-- filetree, treectrl for drive & project
--

ide.filetree = {
  projdir = "",
  projdirlist = {},
  projdirmap = {},
  projdirpartmap = {},
}
local filetree = ide.filetree

local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
local pathsep = GetPathSeparator()
local q = EscapeMagic

-- generic tree
-- ------------

local IMG_DIRECTORY, IMG_FILE_KNOWN, IMG_FILE_OTHER = 0, 1, 2

do
  local getBitmap = (ide.app.createbitmap or wx.wxArtProvider.GetBitmap)
  local size = wx.wxSize(16, 16)
  filetree.imglist = wx.wxImageList(16,16)
  -- 0 = directory
  filetree.imglist:Add(getBitmap(wx.wxART_FOLDER, wx.wxART_OTHER, size))
  -- 1 = file known spec
  filetree.imglist:Add(getBitmap(wx.wxART_HELP_PAGE, wx.wxART_OTHER, size))
  -- 2 = file other
  filetree.imglist:Add(getBitmap(wx.wxART_NORMAL_FILE, wx.wxART_OTHER, size))
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
    local icon = #dir>0 and IMG_DIRECTORY or known and IMG_FILE_KNOWN or IMG_FILE_OTHER
    local item = items[name .. icon]
    if item then -- existing item
      -- keep deleting items until we find item
      while true do
        local next = curr and tree:GetNextSibling(curr)
                           or tree:GetFirstChild(parent_id)
        if not next:IsOk() or name == tree:GetItemText(next) then
          curr = next
          break
        end
        tree:Delete(next)
      end
    else -- new item
      curr = curr and tree:InsertItem(parent_id, curr, name, icon)
                   or tree:PrependItem(parent_id, name, icon)
      if #dir>0 then tree:SetItemHasChildren(curr, FileSysHasContent(file)) end
    end
    if curr:IsOk() then cache[iscaseinsensitive and name:lower() or name] = curr end
  end

  -- delete any leftovers (something that exists in the tree, but not on disk)
  while true do
    local next = curr and tree:GetNextSibling(curr)
                       or tree:GetFirstChild(parent_id)
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

  local root_id = tree:AddRoot(rootdir, IMG_DIRECTORY)
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
  tree:SetImageList(filetree.imglist)

  local function isIt(item, imgtype) return tree:GetItemImage(item) == imgtype end

  function tree:IsDirectory(item_id) return isIt(item_id, IMG_DIRECTORY) end
  function tree:IsFileKnown(item_id) return isIt(item_id, IMG_FILE_KNOWN) end
  function tree:IsFileOther(item_id) return isIt(item_id, IMG_FILE_OTHER) end

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

  function tree:ActivateItem(item_id)
    local name = tree:GetItemFullName(item_id)

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

  local empty = ""
  local function renameItem(itemsrc, target)
    local isdir = tree:GetItemImage(itemsrc) == IMG_DIRECTORY
    local isnew = tree:GetItemText(itemsrc) == empty
    local source = tree:GetItemFullName(itemsrc)
    local fn = wx.wxFileName(target)

    if wx.wxFileName(source):SameAs(fn) then return false end

    local docs = {}
    if not isnew then -- find if source is already opened in the editor
      docs = isdir
        and ide:FindDocumentsByPartialPath(source)
        or {ide:FindDocument(source)}
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
      local itemdst = findItem(tree, target)
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
    local isdir = tree:GetItemImage(item_id) == IMG_DIRECTORY
    local source = tree:GetItemFullName(item_id)

    if isdir and FileSysHasContent(source..pathsep) then return false end
    if wx.wxMessageBox(
      TR("Do you want to delete '%s'?"):format(source),
      GetIDEString("editormessage"),
      wx.wxYES_NO + wx.wxCENTRE, ide.frame) ~= wx.wxYES then return false end

    if isdir then
      wx.wxRmdir(source)
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
  tree:Connect(wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function (event)
      local item_id = event:GetItem()
      tree:SelectItem(item_id)

      local menu = wx.wxMenu {
        { ID_NEWFILE, TR("New &File") },
        { ID_NEWDIRECTORY, TR("&New Directory") },
        { },
        { ID_RENAMEFILE, TR("&Rename")..KSC(ID_RENAMEFILE) },
        { ID_DELETEFILE, TR("&Delete")..KSC(ID_DELETEFILE) },
        { },
        { ID_OPENEXTENSION, TR("Open With Default Program") },
        { ID_COPYFULLPATH, TR("Copy Full Path") },
        { ID_SHOWLOCATION, TR("Show Location") },
      }

      local function addItem(item_id, name, image)
        local isdir = tree:GetItemImage(item_id) == IMG_DIRECTORY
        local parent = isdir and item_id or tree:GetItemParent(item_id)
        if isdir then tree:Expand(item_id) end -- expand to populate if needed

        local item = tree:PrependItem(parent, name, image)
        tree:SetItemHasChildren(parent, true)
        -- temporarily disable expand as we don't need this node populated
        tree:SetEvtHandlerEnabled(false)
        tree:EnsureVisible(item)
        tree:SetEvtHandlerEnabled(true)
        return item
      end

      -- disable Delete on non-empty directories
      local isdir = tree:GetItemImage(item_id) == IMG_DIRECTORY
      if isdir then
        local source = tree:GetItemFullName(item_id)
        menu:Enable(ID_DELETEFILE, not FileSysHasContent(source..pathsep))
        menu:Enable(ID_OPENEXTENSION, false)
      else
        local fname = tree:GetItemText(item_id)
        local ext = '.'..wx.wxFileName(fname):GetExt()
        local ft = wx.wxTheMimeTypesManager:GetFileTypeFromExtension(ext)
        menu:Enable(ID_OPENEXTENSION, ft and #ft:GetOpenCommand("") > 0)
      end

      tree:Connect(ID_NEWFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
        function()
          tree:EditLabel(addItem(item_id, empty, IMG_FILE_OTHER))
        end)
      tree:Connect(ID_NEWDIRECTORY, wx.wxEVT_COMMAND_MENU_SELECTED,
        function()
          tree:EditLabel(addItem(item_id, empty, IMG_DIRECTORY))
        end)
      tree:Connect(ID_RENAMEFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
        function() tree:EditLabel(item_id) end)
      tree:Connect(ID_DELETEFILE, wx.wxEVT_COMMAND_MENU_SELECTED,
        function() deleteItem(item_id) end)
      tree:Connect(ID_COPYFULLPATH, wx.wxEVT_COMMAND_MENU_SELECTED,
        function()
          local tdo = wx.wxTextDataObject(tree:GetItemFullName(item_id))
          if wx.wxClipboard:Get():Open() then
            wx.wxClipboard:Get():SetData(tdo)
            wx.wxClipboard:Get():Close()
          end
        end)
      tree:Connect(ID_OPENEXTENSION, wx.wxEVT_COMMAND_MENU_SELECTED,
        function()
          local fname = tree:GetItemFullName(item_id)
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
        function() ShowLocation(tree:GetItemFullName(item_id)) end)

      PackageEventHandle("onMenuFiletree", menu, tree, event)

      tree:PopupMenu(menu)
    end)
  tree:Connect(wx.wxEVT_RIGHT_DOWN,
    function (event)
      local item_id = tree:HitTest(event:GetPosition())
      if PackageEventHandle("onFiletreeRDown", tree, event, item_id) == false then
        return
      end
    end)

  -- toggle a folder on a single click
  tree:Connect(wx.wxEVT_LEFT_DOWN,
    function (event)
      -- only toggle if this is a folder and the click is on the item line
      -- (exclude the label as it's used for renaming and dragging)
      local mask = wx.wxTREE_HITTEST_ONITEMINDENT
        + wx.wxTREE_HITTEST_ONITEMICON + wx.wxTREE_HITTEST_ONITEMRIGHT
      local item_id, flags = tree:HitTest(event:GetPosition())

      if PackageEventHandle("onFiletreeLDown", tree, event, item_id) == false then
        return
      end

      if item_id and bit.band(flags, mask) > 0 then
        if tree:GetItemImage(item_id) == IMG_DIRECTORY then
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
      if not (itemsrc:IsOk() and parent:IsOk()) then event:Veto() end
    end)
  tree:Connect(wx.wxEVT_COMMAND_TREE_END_LABEL_EDIT,
    function (event)
      -- veto the event to keep the original label intact as the tree
      -- is going to be refreshed with the correct names.
      event:Veto()

      local itemsrc = event:GetItem()
      if not itemsrc:IsOk() or not parent or not parent:IsOk() then return end

      local label = event:GetLabel():gsub("^%s+$","") -- clean all spaces
      local sourcedir = tree:GetItemFullName(parent)
      local target = MergeFullPath(sourcedir, label)
      if event:IsEditCancelled() or label == empty
      or target and not renameItem(itemsrc, target)
      then refreshAncestors(parent) end
    end)
  tree:Connect(wx.wxEVT_KEY_DOWN,
    function (event)
      local item = tree:GetSelection()
      if item:IsOk() then
        local keycode = event:GetKeyCode()
        if keycode == wx.WXK_F2 then return tree:EditLabel(item)
        elseif keycode == wx.WXK_DELETE then return deleteItem(item)
        elseif keycode == wx.WXK_RETURN or keycode == wx.WXK_NUMPAD_ENTER then
          tree:Toggle(item) end
      end
      event:Skip()
    end)

  local itemsrc
  tree:Connect(wx.wxEVT_COMMAND_TREE_BEGIN_DRAG,
    function (event)
      if tree:GetItemParent(event:GetItem()):IsOk() then
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
-- panel
-- (combobox, button)
-- (treectrl)
local projpanel = ide.frame.projpanel
local projcombobox = wx.wxComboBox(projpanel, ID "filetree.proj.drivecb",
  filetree.projdir,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  filetree.projdirlist, wx.wxTE_PROCESS_ENTER)

local projbutton = wx.wxButton(projpanel, ID_PROJECTDIRCHOOSE,
  "...", wx.wxDefaultPosition, wx.wxSize(26,20))

local projtree = wx.wxTreeCtrl(projpanel, wx.wxID_ANY,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_LINES_AT_ROOT
  + wx.wxTR_EDIT_LABELS)

-- use the same font in the combobox as is used in the filetree
projtree:SetFont(ide.font.fNormal)
projcombobox:SetFont(ide.font.fNormal)

local projTopSizer = wx.wxBoxSizer( wx.wxHORIZONTAL );
projTopSizer:Add(projcombobox, 1, wx.wxALL + wx.wxALIGN_LEFT + wx.wxGROW, 0)
projTopSizer:Add(projbutton, 0, wx.wxALL + wx.wxALIGN_RIGHT + wx.wxADJUST_MINSIZE + wx.wxALIGN_CENTER_VERTICAL, 0)

local projSizer = wx.wxBoxSizer( wx.wxVERTICAL );
projSizer:Add(projTopSizer, 0, wx.wxALL + wx.wxALIGN_CENTER_HORIZONTAL + wx.wxGROW, 0)
projSizer:Add(projtree, 1, wx.wxALL + wx.wxALIGN_LEFT + wx.wxGROW, 0)

projpanel:SetSizer(projSizer)

-- proj connectors
-- ---------------

local inupdate = false
local function projcomboboxUpdate(event)
  if inupdate then return end
  local cur = projcombobox:GetValue()
  local fn = wx.wxFileName(filetree.projdirmap[cur] or cur)
  fn:Normalize()

  -- on Windows, wxwidgets (2.9.5+) generates two COMMAND_COMBOBOX_SELECTED
  -- events when the selection is done with ENTER, which causes recursive
  -- call of updateProjectDir. To prevent this the second call is ignored.
  inupdate = true
  filetree:updateProjectDir(fn:GetFullPath())
  inupdate = false
end

projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_COMBOBOX_SELECTED, projcomboboxUpdate)
projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_TEXT_ENTER, projcomboboxUpdate)

treeSetConnectorsAndIcons(projtree)

-- proj functions
-- ---------------

local function abbreviateProjList(projdirlist)
  filetree.projdirmap = {}
  local sep = "\t"
  local dirs = table.concat(projdirlist, sep)..sep
  local projlist = {}
  for _, v in ipairs(projdirlist) do
    -- using FileName because the path doesn't have trailing slash
    local parts = wx.wxFileName(v..pathsep):GetDirs()
    local name = table.remove(parts, #parts) or v
    while #parts > 0
    and select(2, dirs:gsub("%f[^".. pathsep .."]"..q(name)..sep, "")) > 1 do
      name = table.remove(parts, #parts) .. pathsep .. name
    end
    local abbrev = ("%s (%s)"):format(name, v)
    filetree.projdirmap[abbrev] = v
    table.insert(projlist, abbrev)
  end
  return projlist
end

function filetree:updateProjectDir(newdir)
  if (not newdir) or not wx.wxDirExists(newdir) then return end
  local dirname = wx.wxFileName.DirName(newdir)

  if filetree.projdir and #filetree.projdir > 0
  and dirname:SameAs(wx.wxFileName.DirName(filetree.projdir)) then return end

  -- strip the last path separator if any
  local newdir = dirname:GetPath(wx.wxPATH_GET_VOLUME)

  if filetree.projdir and #filetree.projdir > 0 then
    PackageEventHandle("onProjectClose", filetree.projdir) end

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
  projcombobox:Clear()
  projcombobox:Append(abbreviateProjList(filetree.projdirlist))
  projcombobox:Select(0)

  ProjectUpdateProjectDir(newdir,true)
  treeSetRoot(projtree,newdir)

  -- sync with the current editor window and activate selected file
  local editor = GetEditor()
  if (editor) then
    local id = GetEditor():GetId()
    if ide.openDocuments[id] then
      FileTreeMarkSelected(ide.openDocuments[id].filePath)
    end
  end

  PackageEventHandle("onProjectLoad", newdir)
end

projpanel.projbutton = projbutton
projpanel.projcombobox = projcombobox
projpanel.projtree = projtree

function FileTreeGetDir()
  return filetree.projdir and #filetree.projdir > 0
    and wx.wxFileName.DirName(filetree.projdir):GetFullPath()
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

local curr_file
function FileTreeMarkSelected(file)
  if not file or not filetree.projdir or #filetree.projdir == 0 then return end

  local item_id = findItem(projtree, file)

  -- if the select item is different from the current one
  -- or the current one is the same, but not bold (which may happen when
  -- the project is changed to one that includes the current item)
  if curr_file ~= file
  or item_id and not projtree:IsBold(item_id) then
    if curr_file then
      local curr_id = findItem(projtree, curr_file)
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
      projtree:Refresh() end
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
