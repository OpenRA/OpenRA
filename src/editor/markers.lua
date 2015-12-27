-- Copyright 2015 Paul Kulchenko, ZeroBrane LLC

local ide = ide
ide.markers = {
  markersCtrl = nil,
  imglist = ide:CreateImageList("MARKERS", "FILE-NORMAL", "DEBUG-BREAKPOINT-TOGGLE", "BOOKMARK-TOGGLE"),
  needrefresh = {},
  settings = {
  },
}

local markers = ide.markers
local image = { FILE = 0, BREAKPOINT = 1, BOOKMARK = 2 }
local markertypes = {breakpoint = 0, bookmark = 0}

local caches = {}

local function resetMarkersTimer()
  if ide.config.markersinactivity then
    ide.timers.markers:Start(ide.config.markersinactivity*1000, wx.wxTIMER_ONE_SHOT)
  end
end

local function needRefresh(editor)
  ide.markers.needrefresh[editor] = true
  resetMarkersTimer()
end

local function markersRefresh()
  local ctrl = ide.markers.markersCtrl
  local win = ide:GetMainFrame():FindFocus()
  ctrl:Freeze()

  local maskall = 0
  for markertype in pairs(markertypes) do
    local val = 2^ide:GetMarker(markertype)
    maskall = maskall + val
    markertypes[markertype] = val
  end

  for editor in pairs(ide.markers.needrefresh) do
    local cache = caches[editor]
    if cache then
      local fileitem = cache.fileitem
      if not fileitem then
        local filename = ide:GetDocument(editor):GetTabText()
        local root = ctrl:GetRootItem()
        if not root or not root:IsOk() then return end
        fileitem = ctrl:AppendItem(root, filename, image.FILE)
        cache.fileitem = fileitem
      end

      -- disabling event handlers is not strictly necessary, but it's expected
      -- to fix a crash on Windows that had DeleteChildren in the trace (#442).
      ctrl:SetEvtHandlerEnabled(false)
      ctrl:DeleteChildren(fileitem)
      ctrl:SetEvtHandlerEnabled(true)

      local line = editor:MarkerNext(0, maskall)
      while line > -1 do
        local text = ("%d: %s"):format(line+1, FixUTF8(editor:GetLineDyn(line)))
        local markerval = editor:MarkerGet(line)
        for markertype, val in pairs(markertypes) do
          if bit.band(markerval, val) > 0 then
            ctrl:AppendItem(fileitem, text, image[markertype:upper()])
          end
        end
        line = editor:MarkerNext(line + 1, maskall)
      end

      -- if no markers added, then remove the file from the markers list
      ctrl:Expand(fileitem)
      if not ctrl:ItemHasChildren(fileitem) then
        ctrl:Delete(fileitem)
        cache.fileitem = nil
      end
    end
  end

  ctrl:Thaw()
  if win and win ~= ide:GetMainFrame():FindFocus() then win:SetFocus() end
end

local function item2editor(item_id)
  for editor, cache in pairs(caches) do
    if cache.fileitem:GetValue() == item_id:GetValue() then return editor end
  end
end

local function createMarkersWindow()
  local width, height = 360, 200
  local ctrl = wx.wxTreeCtrl(ide.frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(width, height),
    wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_HIDE_ROOT + wx.wxNO_BORDER)

  markers.markersCtrl = ctrl
  ide.timers.markers = wx.wxTimer(ctrl)

  ctrl:AddRoot("Markers")
  ctrl:SetImageList(markers.imglist)
  ctrl:SetFont(ide.font.fNormal)

  function ctrl:ActivateItem(item_id, toggle)
    if ctrl:GetItemImage(item_id) == image.FILE then
      -- activate editor tab
      local editor = item2editor(item_id)
      if editor then ide:GetDocument(editor):SetActive() end
    else -- clicked on the marker item
      local parent = ctrl:GetItemParent(item_id)
      if parent:IsOk() and ctrl:GetItemImage(parent) == image.FILE then
        local editor = item2editor(parent)
        if editor then
          local line = tonumber(ctrl:GetItemText(item_id):match("^(%d+):"))
          ide:GetDocument(editor):SetActive()
          if line then
            editor:GotoLine(line-1)
            local evtype = (ctrl:GetItemImage(item_id) == image.BOOKMARK and ID.BOOKMARKTOGGLE
              or ctrl:GetItemImage(item_id) == image.BREAKPOINT and ID.TOGGLEBREAKPOINT
              or nil)
            if toggle and evtype then
              ctrl:Delete(item_id)
              ide.frame:AddPendingEvent(wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED, evtype))
            end
          end
        end
      end
    end
  end

  local function activateByPosition(event)
    local mask = (wx.wxTREE_HITTEST_ONITEMINDENT + wx.wxTREE_HITTEST_ONITEMLABEL
      + wx.wxTREE_HITTEST_ONITEMICON + wx.wxTREE_HITTEST_ONITEMRIGHT)
    local item_id, flags = ctrl:HitTest(event:GetPosition())

    if item_id and item_id:IsOk() and bit.band(flags, mask) > 0 then
      ctrl:ActivateItem(item_id, bit.band(flags, wx.wxTREE_HITTEST_ONITEMICON) > 0)
    else
      event:Skip()
    end
    return true
  end

  ctrl:Connect(wx.wxEVT_TIMER, function() markersRefresh() end)
  ctrl:Connect(wx.wxEVT_LEFT_DOWN, activateByPosition)
  ctrl:Connect(wx.wxEVT_LEFT_DCLICK, activateByPosition)
  ctrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_ACTIVATED, function(event)
      ctrl:ActivateItem(event:GetItem())
    end)

  ctrl:Connect(wx.wxEVT_COMMAND_TREE_ITEM_MENU,
    function (event)
      local menu = wx.wxMenu {}

      PackageEventHandle("onMenuMarkers", menu, ctrl, event)

      ctrl:PopupMenu(menu)
    end)

  local function reconfigure(pane)
    pane:TopDockable(false):BottomDockable(false)
        :MinSize(150,-1):BestSize(300,-1):FloatingSize(200,300)
  end

  local layout = ide:GetSetting("/view", "uimgrlayout")
  if not layout or not layout:find("markerspanel") then
    ide:AddPanelDocked(ide:GetOutputNotebook(), ctrl, "markerspanel", TR("Markers"), reconfigure, false)
  else
    ide:AddPanel(ctrl, "markerspanel", TR("Markers"), reconfigure)
  end
end

createMarkersWindow()

local package = ide:AddPackage('core.markers', {
    -- save markers; remove tab from the list
    onEditorClose = function(self, editor)
      caches[editor] = nil
      needRefresh(editor)
    end,

    -- schedule marker update if the change is for one of the editors with markers
    onEditorUpdateUI = function(self, editor, event)
      if not caches[editor] then return end
      if bit.band(event:GetUpdated(), wxstc.wxSTC_UPDATE_CONTENT) == 0 then return end
      needRefresh(editor)
    end,

    onEditorMarkerUpdate = function(self, editor)
      -- if no marker, then all markers in a file need to be refreshed
      if not caches[editor] then caches[editor] = {} end
      needRefresh(editor)
    end,
  })

MergeSettings(markers.settings, package:GetSettings())
