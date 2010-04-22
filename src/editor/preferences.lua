-- the preferences dialog

preferencesDialog = {
	category = {};
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

preferencesDialog.addCategory {
	category = "editor";
	title = "Editor";
}

preferencesDialog.addPage {
	title = "Basic preferences";
	category = "editor";
	values = {
		testbox = {type = "checkbox"; title = "Testing";};
	};
	onload = function ()
		return {testbox = true}
	end;
	onsave = function (values)
	end
}
local function showpage(panel,page)
	--TODO: layout the page, load values, etc
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
						wx.wxTR_LINES_AT_ROOT + wx.wxTR_HAS_BUTTONS + wx.wxTR_SINGLE)
	local preferencesPage = wx.wxPanel(panel,ID "view.preferences.dialog.page",
		wx.wxDefaultPosition, wx.wxSize(500,400))
	local panelsizer = wx.wxBoxSizer(wx.wxHORIZONTAL)
	panelsizer:Add(projtree,0,wx.wxALL + wx.wxALIGN_LEFT + wx.wxTOP + wx.wxBOTTOM,0)
	panelsizer:Add(preferencesPage)
	panel:SetSizer(panelsizer)
	
	local treecats = {}
	local catdata = {}
	table.sort(cats,function(a,b) return a.order < b.order end)
	for i=1,#cats do
		local it = projtree:AddRoot(cats[i].title)
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
	
	projtree:Connect( wx.wxEVT_COMMAND_TREE_SEL_CHANGED,
		function( event )
			local item_id = event:GetItem():GetValue()
			local data = catdata[item_id]
			if data.category then
				if data.children[1] then
					projtree:SelectItem(data.children[1])
				end
			else
				showpage(preferencesPage,data.page)
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
