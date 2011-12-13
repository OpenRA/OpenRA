-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- the preferences dialog

preferencesDialog = {
  category = {};
  uifactory = {};
}
local cats = preferencesDialog.category

local function checkstring (v,m)
  if type(v)~="string" then
    error(m.." ("..type(v)..")")
  end
end

function preferencesDialog.addCategory(category)
  checkstring(category.title,"Invalid category title")
  checkstring(category.category,"No category")
  assert(not cats[category.category],"Category already declared")
  cats[category.category] = category
  cats[#cats+1] = category
  category.order = category.order or #cats
  category.entry = {}
end

function preferencesDialog.addPage(page)
  assert(page.category and cats[page.category],"Invalid category given")
  checkstring(page.title,"Invalid title")
  local c = cats[page.category]
  c.entry[#c.entry+1] = page
  page.order = page.order or #c.entry
end

function preferencesDialog.uifactory.space(page,layout,element)
  layout.currentx = layout.currentx + element.space
  return layout
end
function preferencesDialog.uifactory.group(page,layout,element)
  local margin = element.margin or 6
  local nl = {
    currentx = margin;
    currenty = margin+ (element.title and 12 or 8);
    maxsizex = 0;
    maxsizey = 0;
    minwidth = element.minwidth or 0;
    minheight = element.minheight or 0;
    margin = margin;
    layout = layout;
    parent = wx.wxStaticBox(layout.parent,wx.wxID_ANY,element.title or "",
      wx.wxPoint(layout.currentx,layout.currenty),
      wx.wxDefaultSize, element.borderstyle and wx["wxBORDER_"..element.borderstyle:upper()] or 0);
  }
  return nl
end
function preferencesDialog.uifactory.finishgroup(page,layout,element)
  local l = layout.layout
  layout.maxsizex = math.max(layout.minwidth,layout.maxsizex + layout.margin)
  layout.maxsizey = math.max(layout.minheight,layout.maxsizey + layout.margin)
  l.maxsizey = math.max(l.maxsizey,layout.maxsizey+l.currenty)
  l.currentx = l.currentx + layout.maxsizex
  l.maxsizex = math.max(l.maxsizex,l.currentx)
  layout.parent:SetSize(wx.wxSize(layout.maxsizex,layout.maxsizey))
  return l
end

local function pos(layout) return layout.currentx,layout.currenty end
local function fitin(el,layout)
  local x,y = pos(layout)
  local sz = el:GetBestFittingSize()
  el:SetSize(sz)
  layout.maxsizex = math.max(x+sz:GetWidth(),layout.maxsizex)
  layout.maxsizey = math.max(y+sz:GetHeight(),layout.maxsizey)
  layout.currentx = x+sz:GetWidth()
  return layout
end

function preferencesDialog.uifactory.combobox(page,layout,element,value)
  local x,y = pos(layout)
  local id = ID("view.preferences.dialog.page."..page.title.."."..element.name)
  local cbox = wx.wxComboBox(layout.parent,id,"",wx.wxPoint(x,y-4),wx.wxDefaultSize,
    wx.wxArrayString(),wx.wxCB_READONLY)

  if value then
    for i=1,#value do
      cbox:Append(value[i])
    end
  end

  return fitin(cbox,layout)
end

function preferencesDialog.uifactory.dirpicker(page,layout,element,value)
  local x,y = pos(layout)
  local id = ID("view.preferences.dialog.page."..page.title.."."..element.name)
  local picker = wx.wxDirPickerCtrl(layout.parent,id,value or "",element.title or "",wx.wxPoint(x,y-4))
  return fitin(picker,layout)
end

function preferencesDialog.uifactory.edit (page,layout,element,value)
  local x,y = pos(layout)
  local id = ID("view.preferences.dialog.page."..page.title.."."..element.name)
  local edit = wx.wxTextCtrl(layout.parent,id,value or (""..x..","..y), wx.wxPoint(x,y-4))
  return fitin(edit,layout)
end

function preferencesDialog.uifactory.linebreak(page,layout,element)
  layout.currentx = layout.margin or 0
  layout.currenty = layout.maxsizey + (element.space or 0)
  return layout
end
function preferencesDialog.uifactory.checkbox (page,layout,element, value)
  local x,y = pos(layout)
  local id = ID("view.preferences.dialog.page."..page.title.."."..element.name)
  local cbox = wx.wxCheckBox(layout.parent,id,element.title,wx.wxPoint(x,y))
  if value then cbox:SetValue(value) end
  return fitin(cbox,layout)
end
function preferencesDialog.uifactory.static(page,layout,element)
  local x,y = pos(layout)
  local static = wx.wxStaticText(layout.parent,wx.wxID_ANY,element.title,wx.wxPoint(x,y))
  return fitin(static,layout)
end

function preferencesDialog.uifactory.space(page,layout,element)
  layout.currentx = layout.currentx + element.space
  return layout
end
function preferencesDialog.uifactory.group(page,layout,element)
  local margin = element.margin or 6
  local nl = {
    currentx = margin;
    currenty = margin+ (element.title and 12 or 8);
    maxsizex = 0;
    maxsizey = 0;
    minwidth = element.minwidth or 0;
    minheight = element.minheight or 0;
    margin = margin;
    layout = layout;
    parent = wx.wxStaticBox(layout.parent,wx.wxID_ANY,element.title or "",
      wx.wxPoint(layout.currentx,layout.currenty),
      wx.wxDefaultSize, element.borderstyle and wx["wxBORDER_"..element.borderstyle:upper()] or 0);
  }
  return nl
end
function preferencesDialog.uifactory.finishgroup(page,layout,element)
  local l = layout.layout
  layout.maxsizex = math.max(layout.minwidth,layout.maxsizex + layout.margin)
  layout.maxsizey = math.max(layout.minheight,layout.maxsizey + layout.margin)
  l.maxsizey = math.max(l.maxsizey,layout.maxsizey+l.currenty)
  l.currentx = l.currentx + layout.maxsizex
  l.maxsizex = math.max(l.maxsizex,l.currentx)
  layout.parent:SetSize(wx.wxSize(layout.maxsizex,layout.maxsizey))
  return l
end
function preferencesDialog.uifactory.linebreak(page,layout,element)
  layout.currentx = layout.margin or 0
  layout.currenty = layout.maxsizey + (element.space or 0)
  return layout
end
function preferencesDialog.uifactory.checkbox (page,layout,element, value)
  local x,y = layout.currentx,layout.currenty
  local id = ID("view.preferences.dialog.page."..page.title.."."..element.name)
  local cbox = wx.wxCheckBox(layout.parent,id,element.title,wx.wxPoint(x,y))
  local sz = cbox:GetBestFittingSize()
  cbox:SetSize(sz)
  layout.maxsizex = math.max(x+sz:GetWidth(),layout.maxsizex)
  layout.maxsizey = math.max(y+sz:GetHeight(),layout.maxsizey)
  layout.currentx = layout.maxsizex
  if value then cbox:SetValue(value) end
  return layout
end
function preferencesDialog.uifactory.static(page,layout,element)
  local x,y = layout.currentx,layout.currenty
  local static = wx.wxStaticText(layout.parent,wx.wxID_ANY,element.title,wx.wxPoint(x,y))
  local sz = static:GetBestFittingSize()
  static:SetSize(sz)
  layout.maxsizex = math.max(x+sz:GetWidth(),layout.maxsizex)
  layout.maxsizey = math.max(y+sz:GetHeight(),layout.maxsizey)
  layout.currentx = layout.maxsizex

  return layout
end

local function showpage(panel,page)
  --TODO: layout the page, load values, etc
  local data = page.onload()
  local layout = page.layout
  local layoutdata = {
    currentx = 0;
    currenty = 0;
    maxsizex = 0;
    maxsizey = 0;
    parent = panel;
  }
  for i,el in ipairs(layout) do
    assert(preferencesDialog.uifactory[el.type],"Unknown ui type type")
    layoutdata = assert(
      preferencesDialog.uifactory[el.type](page,layoutdata,el,data[el.name])
    )
  end
  panel:SetSize(layoutdata.maxsizex,layoutdata.maxsizey)
  --print(layoutdata.maxsizex,layoutdata.maxsizey)
end

function preferencesDialog.show(event)
  local dialog = wx.wxDialog(ide.frame, ID "view.preferences.dialog","Preferences")

  local id_btn_ok = ID "view.preferences.dialog.button.ok"
  local id_btn_cancel = ID "view.preferences.dialog.button.cancel"
  local id_btn_apply = ID "view.preferences.dialog.button.apply"

  local panel_buttons = wx.wxPanel(dialog,ID "view.preferences.dialog.buttonpanel")
  local btn_ok = wx.wxButton(panel_buttons,id_btn_ok, "OK")
  local btn_cancel = wx.wxButton(panel_buttons,id_btn_cancel, "Cancel")
  local btn_apply = wx.wxButton(panel_buttons,id_btn_apply, "Apply")

  dialog:Connect(id_btn_cancel, wx.wxEVT_COMMAND_BUTTON_CLICKED,
    function (event)
      dialog:EndModal(0)
    end)
  dialog:Connect(id_btn_ok, wx.wxEVT_COMMAND_BUTTON_CLICKED,
    function (event)
      dialog:EndModal(0)
    end)

  local panel = wx.wxPanel(dialog,ID "view.preferences.dialog.panel",
    wx.wxDefaultPosition, wx.wxSize(600,400))

  local projtree = wx.wxTreeCtrl(panel, ID "view.preferences.dialog.panel.tree",
    wx.wxDefaultPosition, wx.wxSize(180,400),
    wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE + wx.wxTR_HIDE_ROOT)
  local preferencesPage = wx.wxPanel(panel,ID "view.preferences.dialog.page",
    wx.wxDefaultPosition, wx.wxSize(500,400))
  local panelsizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
  panelsizer:Add(projtree,0,wx.wxALL + wx.wxALIGN_LEFT + wx.wxTOP + wx.wxBOTTOM,0)
  panelsizer:AddSpacer(5)
  panelsizer:Add(preferencesPage)
  panel:SetSizer(panelsizer)

  local treecats = {}
  local catdata = {}
  table.sort(cats,function(a,b) return a.order < b.order end)
  local rootit = projtree:AddRoot("")

  catdata[rootit:GetValue()] = {category = "root", children = treecats}
  for i=1,#cats do
    local it = projtree:AppendItem(rootit,cats[i].title)
    treecats[i] = it
    local c = cats[i]
    local children = {}
    catdata[it:GetValue()] = {category = c,children = children}
    for i=1,#c.entry do
      local e = c.entry[i]
      local it = projtree:AppendItem(it,e.title)
      catdata[it:GetValue()] = {page = e}
      children[i] = it
    end
    projtree:Expand(it)
  end

  projtree:Expand(rootit)

  local preferencesContent
  projtree:Connect( wx.wxEVT_COMMAND_TREE_SEL_CHANGED,
    function( event )
      local item_id = event:GetItem():GetValue()
      local data = catdata[item_id]
      if data.category then
        if data.children[1] then
          projtree:SelectItem(data.children[1])
        end
      else
        if preferencesContent then
          preferencesPage:RemoveChild(preferencesContent)
        end
        preferencesContent = wx.wxPanel(preferencesPage,wx.wxID_ANY)

        showpage(preferencesContent,data.page)
      end
    end )

  local topsizer = wx.wxBoxSizer(wx.wxVERTICAL)
  topsizer:Add(panel,0,wx.wxALL + wx.wxALIGN_CENTER,10)
  topsizer:Add(wx.wxStaticLine(dialog, wx.wxID_ANY), 0, wx.wxEXPAND + wx.wxLEFT + wx.wxRIGHT, 10)
  topsizer:Add(panel_buttons, 0, wx.wxALL + wx.wxALIGN_RIGHT, 10)

  local buttonpanelsizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
  buttonpanelsizer:Add(btn_cancel,0,wx.wxALL + wx.wxALIGN_RIGHT,0)
  buttonpanelsizer:AddSpacer(5)
  buttonpanelsizer:Add(btn_apply,0,wx.wxALL + wx.wxALIGN_RIGHT,0)
  buttonpanelsizer:AddSpacer(5)
  buttonpanelsizer:Add(btn_ok,0,wx.wxALL + wx.wxALIGN_RIGHT,0)
  panel_buttons:SetSizer(buttonpanelsizer)
  buttonpanelsizer:Fit(panel_buttons)

  dialog:SetAutoLayout(true)
  dialog:SetSizer(topsizer)
  topsizer:Fit(dialog)
  dialog:Center()
  dialog:ShowModal()
end
