-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
--
-- filetree, treectrl for drive & project
--

ide.filetree = {
  dirdriveText = "",
  dirdriveTextArray = {},

  projdirText = "",
  projdirTextArray = {},

  showroot = true,

  dirdata = {
    root_id = nil,
    rootdir = "",
  },

  projdata = {
    root_id = nil,
    rootdir = "",
  },
}
local filetree = ide.filetree

-- generic tree
-- ------------

do
  local getBitmap = (ide.app.createbitmap or wx.wxArtProvider.GetBitmap)
  local size = wx.wxSize(16, 16)
  filetree.imglist = wx.wxImageList(16,16)
  -- 0 = directory
  filetree.imglist:Add(getBitmap(wx.wxART_FOLDER, wx.wxART_OTHER, size))
  -- 1 = file known spec
  filetree.imglist:Add(getBitmap(wx.wxART_HELP_PAGE, wx.wxART_OTHER, size))
  -- 2 = file rest
  filetree.imglist:Add(getBitmap(wx.wxART_NORMAL_FILE, wx.wxART_OTHER, size))
end

local function treeAddDir(tree,parent_id,rootdir)
  local item, cookie = tree:GetFirstChild(parent_id)
  local items = {}
  while true do
    if not item:IsOk() then break end
    items[tree:GetItemText(item) .. tree:GetItemImage(item)] = item
    item, cookie = tree:GetNextChild(parent_id, cookie)
  end

  local curr
  local search = rootdir..string_Pathsep.."*"

  -- append directories
  for _,dir in ipairs(FileSysGet(search,wx.wxDIR)) do
    local name = dir:match("("..stringset_File.."+)$")
    local icon = 0
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
      local dir_id = curr and tree:InsertItem(parent_id, curr, name, icon)
                           or tree:PrependItem(parent_id, name, icon)
      tree:SetItemHasChildren(dir_id,FileSysHasContent(dir))
      curr = dir_id
    end
  end

  -- then append files
  for _,file in ipairs(FileSysGet(search,wx.wxFILE)) do
    local name = file:match("("..stringset_File.."+)$")
    local known = GetSpec(GetFileExt(name))
    local icon = known and 1 or 2
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
    end
  end

  -- delete any leftovers (something that exists in the tree, but not on disk)
  while true do
    local next = curr and tree:GetNextSibling(curr)
                       or tree:GetFirstChild(parent_id)
    if not next:IsOk() then break end
    tree:Delete(next)
  end

  tree:SetItemHasChildren(parent_id,
    tree:GetChildrenCount(parent_id, false) > 0)
end

local function treeGetItemFullName(tree,treedata,item_id)
  local str = tree:GetItemText(item_id)
  local cur = str

  while (#cur > 0) do
    item_id = tree:GetItemParent(item_id)
    cur = tree:GetItemText(item_id)
    if cur and string.len(cur) > 0 then str = cur..string_Pathsep..str end
  end
  -- as root may already include path separate, normalize the path
  local fullPath = wx.wxFileName(
    filetree.showroot and str or filetree.projdata.rootdir .. str)
  fullPath:Normalize()
  return fullPath:GetFullPath()
end

local function treeSetRoot(tree,treedata,rootdir)
  tree:DeleteAllItems()
  if (not wx.wxDirExists(rootdir)) then
    treedata.root_id = nil
    return
  end

  local root_id = tree:AddRoot(rootdir,0)
  treedata.root_id = root_id
  treedata.rootdir = rootdir

  treeAddDir(tree,root_id,rootdir)
  filetree.newfiledir = rootdir

  tree:Expand(root_id)
end

local function treeSetConnectorsAndIcons(tree,treedata)
  tree:SetImageList(filetree.imglist)

  -- connect to some events from the wxTreeCtrl
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_EXPANDING,
    function( event )
      local item_id = event:GetItem()
      local dir = treeGetItemFullName(tree,treedata,item_id)
      treeAddDir(tree,item_id,dir)
      return true
    end)
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_COLLAPSED,
    function() return true end)
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED,
    function( event )
      local item_id = event:GetItem()

      local name = treeGetItemFullName(tree,treedata,item_id)
      -- refresh the folder
      if (tree:GetItemImage(item_id) == 0) then
        if wx.wxFileName(name):DirExists() then
          treeAddDir(tree,item_id,name) -- refresh the content
        else -- stale filetree information; rescan
          treeAddDir(tree,tree:GetItemParent(item_id),name)
        end
      else -- open file
        if wx.wxFileName(name):FileExists() then
          LoadFile(name,nil,true)
        else -- stale filetree information; rescan
          treeAddDir(tree,tree:GetItemParent(item_id),name)
        end 
      end
    end)
  -- toggle a folder on
  tree:Connect( wx.wxEVT_LEFT_DOWN,
    function( event )
      local item_id = tree:HitTest(event:GetPosition())
      -- only toggle if this is a folder and the click is on the label
      if item_id and tree:GetItemImage(item_id) == 0 then
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
  filetree.projdirText,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  filetree.projdirTextArray, wx.wxTE_PROCESS_ENTER)

local projbutton = wx.wxButton(projpanel, ID_PROJECTDIRCHOOSE,
  "...",wx.wxDefaultPosition, wx.wxSize(26,20))

local projtree = wx.wxTreeCtrl(projpanel, wx.wxID_ANY,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  filetree.showroot
  and (wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE)
  or (wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_HIDE_ROOT))

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

local function projcomboboxUpdate(event)
  local cur = projcombobox:GetValue()
  local fn = wx.wxFileName(cur)
  fn:Normalize()

  filetree:updateProjectDir(fn:GetFullPath(), event:GetEventType() == wx.wxEVT_COMMAND_COMBOBOX_SELECTED)
end

projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_COMBOBOX_SELECTED, projcomboboxUpdate)
projpanel:Connect(ID "filetree.proj.drivecb", wx.wxEVT_COMMAND_TEXT_ENTER, projcomboboxUpdate)

treeSetConnectorsAndIcons(projtree,filetree.projdata)

-- proj functions
-- ---------------

function filetree:updateProjectDir(newdir, cboxsel)
  if (newdir and newdir:sub(-3,-2) == string_Pathsep) then
    newdir = newdir:sub(0,-2)
  end

  if ((not newdir) or filetree.projdirText == newdir or not wx.wxDirExists(newdir)) then return end

  if ide.config.projectautoopen and filetree.projdirText then
    StoreRestoreProjectTabs(filetree.projdirText, newdir)
  end

  filetree.projdirText = newdir

  PrependStringToArray(filetree.projdirTextArray,newdir,ide.config.projecthistorylength)
  projcombobox:Clear()
  projcombobox:Append(filetree.projdirTextArray)
  if (not cboxsel) then
    projcombobox:SetValue(newdir)
  else
    projcombobox:Select(0)
  end

  ProjectUpdateProjectDir(newdir,true)
  treeSetRoot(projtree,filetree.projdata,newdir)

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
  return projpanel:IsShown() and filetree.newfiledir
    and wx.wxFileName.DirName(filetree.newfiledir):GetFullPath()
end

function FileTreeSetProjects(tab)
  filetree.projdirTextArray = tab
  if (tab and tab[1]) then
    filetree:updateProjectDir(tab[1])
  end
end

function FileTreeGetProjects()
  return filetree.projdirTextArray
end

local function findItem(tree, match)
  local node = projtree:GetRootItem()
  local label = tree:GetItemText(node)

  local s, e = string.find(match, label, 1, true)
  if not s or s ~= 1 then return end

  for token in string.gmatch(string.sub(match,e+1), "[^%"..string_Pathsep.."]+") do
    local dir = treeGetItemFullName(tree,filetree.projdata,node)
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

  -- this loop exits only when a match is found
  return node
end

local curr_file
function FileTreeMarkSelected(file)
  if not file or not filetree.projdirText or #filetree.projdirText == 0 then return end

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
      projtree:SetItemBold(item_id, true)
    end
    curr_file = file
    projtree:Refresh() -- to force refresh on Mac (ide.osname == 'Macintosh')
  end
end

function FileTreeRefresh()
  treeSetRoot(projtree,filetree.projdata,filetree.projdirText)
end
