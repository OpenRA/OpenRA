-- Copyright 2014-15 Paul Kulchenko, ZeroBrane LLC

local ide = ide
ide.outline = {
  imglist = ide:CreateImageList("OUTLINE", "FILE-NORMAL", "VALUE-LCALL",
    "VALUE-GCALL", "VALUE-ACALL", "VALUE-SCALL", "VALUE-MCALL"),
}

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

local function outlineRefresh(editor, force)
  if not editor then return end
  local tokens = editor:GetTokenList()
  local text = editor:GetText()
  local sep = editor.spec.sep
  local varname = "([%w_][%w_"..q(sep:sub(1,1)).."]*)"
  local funcs = {}
  local var = {}
  local outcfg = ide.config.outline or {}
  for _, token in ipairs(tokens) do
    local op = token[1]
    if op == 'Var' or op == 'Id' then
      var = {name = token.name, fpos = token.fpos, global = token.context[token.name] == nil}
    elseif op == 'Function' then
      local depth = token.context['function'] or 1
      local name, pos = token.name, token.fpos
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
      if name or outcfg.showanonymous then
        funcs[#funcs+1] = {
          name = (name or outcfg.showanonymous)..params,
          depth = depth,
          image = ftype,
          pos = name and pos or token.fpos,
        }
      end
    end
  end

  local ctrl = ide.outline.outlineCtrl
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
        elseif nochange then
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

  local stack = {fileitem}
  local resort = {} -- items that need to be re-sorted
  for n, func in ipairs(funcs) do
    local depth = outcfg.showflat and 1 or func.depth
    local parent = stack[depth]
    while not parent do depth = depth - 1; parent = stack[depth] end
    local item = ctrl:AppendItem(parent, func.name, func.image)
    if outcfg.sort then resort[parent] = true end
    setData(ctrl, item, n)
    func.item = item
    stack[func.depth+1] = item
  end
  if outcfg.sort then -- resort items for all parents that have been modified
    for item in pairs(resort) do ctrl:SortChildren(item) end
  end
  ctrl:ExpandAllChildren(fileitem)
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

local function outlineCreateOutlineWindow()
  local width, height = 360, 200
  local ctrl = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(width, height),
    wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE
    + wx.wxTR_HIDE_ROOT + wx.wxNO_BORDER)

  ide.outline.outlineCtrl = ctrl
  ide.timers.outline = wx.wxTimer(ctrl)

  ctrl:AddRoot("Outline")
  ctrl:SetImageList(ide.outline.imglist)
  ctrl:SetFont(ide.font.fNormal)

  function ctrl:ActivateItem(item_id)
    ctrl:SelectItem(item_id, true)
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
    -- only toggle if this is a folder and the click is on the item line
    -- (exclude the label as it's used for renaming and dragging)
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

  ctrl:Connect(wx.wxEVT_TIMER, function() outlineRefresh(GetEditor()) end)
  ctrl:Connect(wx.wxEVT_LEFT_DOWN, activateByPosition)
  ctrl:Connect(wx.wxEVT_LEFT_DCLICK, activateByPosition)
  ctrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED, function(event)
      ctrl:ActivateItem(event:GetItem())
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

local function eachNode(eachFunc, root)
  local ctrl = ide.outline.outlineCtrl
  local item = ctrl:GetFirstChild(root or ctrl:GetRootItem())
  while true do
    if not item:IsOk() then break end
    if eachFunc and eachFunc(ctrl, item) then break end
    item = ctrl:GetNextSibling(item)
  end
end

outlineCreateOutlineWindow()

function OutlineFunctions(editor)
  -- force token refresh (as these may be not updated yet)
  if #editor:GetTokenList() == 0 then
    while IndicateAll(editor) do end
  end

  outlineRefresh(editor, true)
  return caches[editor].funcs
end

ide:AddPackage('core.outline', {
    -- remove the editor from the list
    onEditorClose = function(self, editor)
      local cache = caches[editor]
      local fileitem = cache and cache.fileitem
      caches[editor] = nil -- remove from cache
      if (ide.config.outline or {}).showonefile then return end
      if fileitem then ide.outline.outlineCtrl:Delete(fileitem) end
    end,

    -- handle rename of the file in the current editor
    onEditorSave = function(self, editor)
      if (ide.config.outline or {}).showonefile then return end
      local cache = caches[editor]
      local fileitem = cache and cache.fileitem
      local doc = ide:GetDocument(editor)
      local ctrl = ide.outline.outlineCtrl
      if doc and fileitem and ctrl:GetItemText(fileitem) ~= doc:GetTabText() then
        ctrl:SetItemText(fileitem, doc:GetTabText())
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
      local ctrl = ide.outline.outlineCtrl
      local itemname = ide:GetDocument(editor):GetTabText()

      -- fix file name if it changed in the editor
      if fileitem and ctrl:GetItemText(fileitem) ~= itemname then
        ctrl:SetItemText(fileitem, itemname)
      end

      -- if the editor is not in the cache, which may happen if the user
      -- quickly switches between tabs that don't have outline generated,
      -- regenerate it manually
      if not cache and ide.config.outlineinactivity then
        ide.timers.outline:Start(ide.config.outlineinactivity*1000, wx.wxTIMER_ONE_SHOT)
      end

      eachNode(function(ctrl, item)
          local found = fileitem and item:GetValue() == fileitem:GetValue()
          if not found and ctrl:IsBold(item) then
            ctrl:SetItemBold(item, false)
            ctrl:CollapseAllChildren(item)
          end
        end)

      if fileitem and not ctrl:IsBold(fileitem) then
        ctrl:SetItemBold(fileitem, true)
        ctrl:ExpandAllChildren(fileitem)
        ctrl:ScrollTo(fileitem)
        ctrl:SetScrollPos(wx.wxHORIZONTAL, 0, true)
      end
    end,
  })
