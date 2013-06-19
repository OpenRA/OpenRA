-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--
-- filetree, treectrl for drive & project
--

ide.filetree = {
  projdir = "",
  projdirlist = {},
}
local filetree = ide.filetree

local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))

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
    local name, dir = file:match("([^"..string_Pathsep.."]+)("..string_Pathsep.."?)$")
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
  local data = wx.wxLuaTreeItemData()
  data:SetData(cache)
  tree:SetItemData(parent_id, data)

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

local function treeSetConnectorsAndIcons(tree)
  tree:SetImageList(filetree.imglist)

  function tree:GetItemFullName(item_id)
    local tree = self
    local str = tree:GetItemText(item_id)
    local cur = str

    while (#cur > 0) do
      item_id = tree:GetItemParent(item_id)
      if not item_id:IsOk() then break end
      cur = tree:GetItemText(item_id)
      if cur and string.len(cur) > 0 then str = cur..string_Pathsep..str end
    end
    -- as root may already include path separator, normalize the path
    local fullPath = wx.wxFileName(str)
    fullPath:Normalize()
    return fullPath:GetFullPath()
  end

  local function refreshAncestors(node)
    while node:IsOk() do
      local dir = tree:GetItemFullName(node)
      treeAddDir(tree,node,dir)
      node = tree:GetItemParent(node)
    end
  end

  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_EXPANDING,
    function( event )
      local item_id = event:GetItem()
      local dir = tree:GetItemFullName(item_id)

      if wx.wxDirExists(dir) then treeAddDir(tree,item_id,dir) -- refresh folder
      else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
      return true
    end)
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED,
    function( event )
      local item_id = event:GetItem()
      local name = tree:GetItemFullName(item_id)
      -- refresh the folder
      if (tree:GetItemImage(item_id) == IMG_DIRECTORY) then
        if wx.wxDirExists(name) then treeAddDir(tree,item_id,name)
        else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
      else -- open file
        if wx.wxFileExists(name) then LoadFile(name,nil,true)
        else refreshAncestors(tree:GetItemParent(item_id)) end -- stale content
      end
    end)
  -- handle context menu
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function( event )
      local item_id = event:GetItem()
      tree:SelectItem(item_id)
      local menu = wx.wxMenu()
      menu:Append(ID_SHOWLOCATION, TR("Show Location"))
      tree:Connect(ID_SHOWLOCATION, wx.wxEVT_COMMAND_MENU_SELECTED,
        function() ShowLocation(tree:GetItemFullName(item_id)) end)

      PackageEventHandle("onMenuFiletree", menu, tree, event)

      tree:PopupMenu(menu)
    end)
  -- toggle a folder on a single click
  tree:Connect( wx.wxEVT_LEFT_DOWN,
    function( event )
      local item_id = tree:HitTest(event:GetPosition())
      -- only toggle if this is a folder and the click is on the label
      if item_id and tree:GetItemImage(item_id) == IMG_DIRECTORY then
        tree:Toggle(item_id)
        tree:SelectItem(item_id)
      else
        event:Skip()
      end
      return true
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
  wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_LINES_AT_ROOT)

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
  local fn = wx.wxFileName(cur)
  fn:Normalize()

  -- on Windows, wxwidgets (2.9.5+) generates two COMMAND_COMBOBOX_SELECTED
  -- events when the selection is done with ENTER, which causes recursive
  -- call of updateProjectDir. To prevent this the second call is ignored.
  inupdate = true
  filetree:updateProjectDir(fn:GetFullPath(), event:GetEventType() == wx.wxEVT_COMMAND_COMBOBOX_SELECTED)
  inupdate = false
end

projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_COMBOBOX_SELECTED, projcomboboxUpdate)
projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_TEXT_ENTER, projcomboboxUpdate)

treeSetConnectorsAndIcons(projtree)

-- proj functions
-- ---------------

function filetree:updateProjectDir(newdir, cboxsel)
  if (not newdir) or not wx.wxDirExists(newdir) then return end
  local dirname = wx.wxFileName.DirName(newdir)

  if filetree.projdir and #filetree.projdir > 0
  and dirname:SameAs(wx.wxFileName.DirName(filetree.projdir)) then return end

  -- strip the last path separator if any
  local newdir = dirname:GetPath(wx.wxPATH_GET_VOLUME)

  if filetree.projdir and #filetree.projdir > 0 then
    PackageEventHandle("onProjectClose", filetree.projdir) end

  if ide.config.projectautoopen and filetree.projdir then
    StoreRestoreProjectTabs(filetree.projdir, newdir)
  end

  filetree.projdir = newdir

  PackageEventHandle("onProjectLoad", filetree.projdir)

  PrependStringToArray(
    filetree.projdirlist,
    newdir,
    ide.config.projecthistorylength,
    function(s1, s2) return dirname:SameAs(wx.wxFileName.DirName(s2)) end)
  projcombobox:Clear()
  projcombobox:Append(filetree.projdirlist)
  if (not cboxsel) then
    projcombobox:SetValue(newdir)
  else
    projcombobox:Select(0)
  end

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

local function findItem(tree, match)
  local node = projtree:GetRootItem()
  local label = tree:GetItemText(node)

  local s, e
  if iscaseinsensitive then
    s, e = string.find(match:lower(), label:lower(), 1, true)
  else
    s, e = string.find(match, label, 1, true)
  end
  if not s or s ~= 1 then return end

  for token in string.gmatch(string.sub(match,e+1), "[^%"..string_Pathsep.."]+") do
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
