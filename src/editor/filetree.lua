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
local sidenotebook = ide.frame.vsplitter.sidenotebook

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
  tree:DeleteChildren(parent_id)
  local search = rootdir..string_Pathsep.."*.*"
  local dirs = FileSysGet(search,wx.wxDIR)
  -- append directories
  for i,dir in ipairs(dirs) do
    local dir_id = tree:AppendItem(parent_id, dir:match("(%"..string_Pathsep..stringset_File.."+)$"),0)

    tree:SetItemHasChildren(dir_id,FileSysHasContent(dir))
  end
  -- then append files
  local files = FileSysGet(search,wx.wxFILE)
  for i,file in ipairs(files) do
    local fname = file:match("%"..string_Pathsep.."("..stringset_File.."+)$")
    local known = GetSpec(GetFileExt(fname))
    tree:AppendItem(parent_id, fname,known and 1 or 2)
  end
end

local function treeGetItemFullName(tree,treedata,item_id,isfile)
  local str = isfile and string_Pathsep or ""
  str = str..tree:GetItemText(item_id)
  local cur = str

  local stop = 4
  while (#cur > 0) do
    item_id = tree:GetItemParent(item_id)
    cur = tree:GetItemText(item_id)
    str = cur..str
  end
  return ((not filetree.showroot) and filetree.projdata.rootdir or "").. str
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

  filetree.newfiledir = rootdir..string_Pathsep

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
      local item_id = event:GetItem()
      tree:DeleteChildren(item_id)

      -- directories must stay expandable if they have content
      local dir = treeGetItemFullName(tree,treedata,item_id)
      tree:SetItemHasChildren(item_id,FileSysHasContent(dir))

      return true
    end )
  tree:Connect( wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED,
    function( event )
      local item_id = event:GetItem()

      if (tree:GetItemImage(item_id) == 0) then return end
      -- openfile
      local name = treeGetItemFullName(tree,treedata,item_id,true)
      LoadFile(name,nil,true)
      FileTreeMarkSelected(name)
    end )
  tree:Connect( wx.wxEVT_COMMAND_TREE_SEL_CHANGED,
    function( event )
      local item_id = event:GetItem()

      -- set "newfile-path"
      local isfile = tree:GetItemImage(item_id) ~= 0
      filetree.newfiledir = treeGetItemFullName(tree,treedata,item_id,isfile)

      if (isfile) then
        -- remove file
        filetree.newfiledir = wx.wxFileName(filetree.newfiledir):GetPath(wx.wxPATH_GET_VOLUME)
      end

      filetree.newfiledir = filetree.newfiledir..string_Pathsep
    end )
end

-- project
-- panel
-- ( combobox, button)
-- ( treectrl)

local projpanel = wx.wxPanel(sidenotebook,wx.wxID_ANY)
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
  local id = GetEditor():GetId()
  if ide.openDocuments[id] then
    FileTreeMarkSelected(ide.openDocuments[id].filePath)
  end
end

projpanel.projbutton = projbutton
projpanel.projcombobox = projcombobox
projpanel.projtree = projtree
sidenotebook.projpanel = projpanel

sidenotebook:AddPage(projpanel, "Project",true)

function FileTreeGetDir()
  -- atm only projtree
  return ide.frame.vsplitter:IsSplit() and filetree.newfiledir
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

local curr_id
function FileTreeMarkSelected(file)
  local item_id = findItem(projtree, file, projtree:GetRootItem())
  if curr_id ~= item_id then
    if curr_id and projtree:IsBold(curr_id) then
      projtree:SetItemBold(curr_id, false)
    end
    if item_id then
      projtree:SetItemBold(item_id, true)
      projtree:EnsureVisible(item_id)
      curr_id = item_id
    end
  end
end

function findItem(tree, match, start)
  local item, cookie = tree:GetFirstChild(start)

  while item:IsOk() do
    if tree:ItemHasChildren(item) then
      if findItem(tree, match, item) then return item end
    elseif match == treeGetItemFullName(tree,filetree.projdata,item,true) then
      return item
    end
    item, cookie = tree:GetNextChild(start, cookie)
  end

  return
end
