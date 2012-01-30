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
  imglist,

  newfiledir,
}
local filetree = ide.filetree
local frame = ide.frame

-- generic tree
-- ------------

do
  local size = wx.wxSize(16, 16)
  filetree.imglist = wx.wxImageList(16,16)
  -- 0 = directory
  filetree.imglist:Add(wx.wxArtProvider.GetBitmap(wx.wxART_FOLDER, wx.wxART_OTHER, size))
  -- 1 = file known spec
  filetree.imglist:Add(wx.wxArtProvider.GetBitmap(wx.wxART_HELP_PAGE, wx.wxART_OTHER, size))
  -- 2 = file rest
  filetree.imglist:Add(wx.wxArtProvider.GetBitmap(wx.wxART_NORMAL_FILE, wx.wxART_OTHER, size))
end

local function treeAddDir(tree,parent_id,rootdir)
  local item, cookie = tree:GetFirstChild(parent_id)
  local items = {}
  while true do
    if not item:IsOk() then break end
    items[tree:GetItemText(item) .. tree:GetItemImage(item)] = item
    item, cookie = tree:GetNextChild(item, cookie)
  end

  local curr
  local search = rootdir..string_Pathsep.."*.*"
  local dirs = FileSysGet(search,wx.wxDIR)

  -- append directories
  for i,dir in ipairs(dirs) do
    local name = dir:match("%"..string_Pathsep.."("..stringset_File.."+)$")
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
  local files = FileSysGet(search,wx.wxFILE)
  for i,file in ipairs(files) do
    local name = file:match("%"..string_Pathsep.."("..stringset_File.."+)$")
    local known = GetSpec(GetFileExt(fname))
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
  return ((not filetree.showroot) and filetree.projdata.rootdir or "")..str
end

local function treeSetRoot(tree,treedata,rootdir)
  tree:DeleteAllItems()

  if (not wx.wxDirExists(rootdir)) then
    treedata.root_id = nil
    tree:AddRoot("Invalid")
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
    end )
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_COLLAPSED,
    function( event )
      -- don't need to do anything here
      return true
    end )
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
          FileTreeMarkSelected(name)
        else -- stale filetree information; rescan
          treeAddDir(tree,tree:GetItemParent(item_id),name)
        end 
      end
    end )
  tree:Connect( wx.wxEVT_COMMAND_TREE_SEL_CHANGED,
    function( event )
      local item_id = event:GetItem()

      -- set "newfile-path"
      local isfile = tree:GetItemImage(item_id) ~= 0
      filetree.newfiledir = treeGetItemFullName(tree,treedata,item_id)

      if (isfile) then
        -- remove file
        filetree.newfiledir = wx.wxFileName(filetree.newfiledir):GetPath(wx.wxPATH_GET_VOLUME)
      end

      filetree.newfiledir = filetree.newfiledir..string_Pathsep
    end )
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

local projbutton = wx.wxButton(projpanel, ID "debug.projectdir.choose", "...",wx.wxDefaultPosition, wx.wxSize(26,20))

local projtree = wx.wxTreeCtrl(projpanel, ID "filetree.projtree",
  wx.wxDefaultPosition, wx.wxDefaultSize,
  filetree.showroot
  and (wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE)
  or (wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_HIDE_ROOT))

-- use the same font in the combobox as is used in the filetree
projcombobox:SetFont(projtree:GetFont())

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

  local s, e = string.find(match, label)
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
      item, cookie = tree:GetNextChild(item, cookie)
    end
  end

  -- this loop exits only when a match is found
  return node
end

local curr_id
function FileTreeMarkSelected(file)
  if not file then return end
  local item_id = findItem(projtree, file)
  if curr_id ~= item_id then
    if curr_id and projtree:IsBold(curr_id) then
      projtree:SetItemBold(curr_id, false)
    end
    if item_id then
      projtree:EnsureVisible(item_id)
      projtree:SetItemBold(item_id, true)
      curr_id = item_id
    end
  end
end
