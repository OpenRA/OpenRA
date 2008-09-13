local wxkeywords       = nil    -- a string of the keywords for scintilla of wxLua's wx.XXX items

local in_evt_focus     = false  -- true when in editor focus event to avoid recursion
local editorID         = 100    -- window id to create editor pages with, incremented for new editors

local openDocuments 	= ide.openDocuments
local ignoredFilesList 	= ide.ignoredFilesList
local notebook			= ide.frame.vsplitter.splitter.notebook

-- ----------------------------------------------------------------------------
-- Get/Set notebook editor page, use nil for current page, returns nil if none
function GetEditor(selection)
	local editor = nil
	if selection == nil then
		selection = notebook:GetSelection()
	end
	if (selection >= 0) and (selection < notebook:GetPageCount()) and (notebook:GetPage(selection):GetClassInfo():GetClassName()=="wxStyledTextCtrl") then
		editor = notebook:GetPage(selection):DynamicCast("wxStyledTextCtrl")
	end
	return editor
end

-- init new notebook page selection, use nil for current page
function SetEditorSelection(selection)
	local editor = GetEditor(selection)
	UpdateStatusText(editor) -- update even if nil
	ide.frame:SetTitle(GetFileInfo(editor))
	
	if editor then
		editor:SetFocus()
		editor:SetSTCFocus(true)
		IsFileAlteredOnDisk(editor)
	end
end

function GetEditorFileAndCurInfo(nochecksave)
	local editor = GetEditor()
	if (not (editor and (nochecksave or SaveIfModified(editor)))) then
		return
	end
	
	local id = editor:GetId();
	local filepath = openDocuments[id].filePath
	if (nochecksave and not filepath) then
		return
	end
	
	local fn = wx.wxFileName(filepath)
	fn:Normalize()
	
	local info = {}
	info.pos  = editor:GetCurrentPos()
	info.line = editor:GetCurrentLine()
	info.sel  = editor:GetSelectedText()
	info.sel = info.sel and info.sel:len() > 0 and info.sel or nil
	info.selword = info.sel and info.sel:match("([^a-zA-Z_0-9]+)") or info.sel
	
	
	return fn,info
end

-- ----------------------------------------------------------------------------
-- Update the statusbar text of the frame using the given editor.
--  Only update if the text has changed.
statusTextTable = { "OVR?", "R/O?", "Cursor Pos" }

function UpdateStatusText(editor)
	local texts = { "", "", "" }
	if ide.frame and editor then
		local pos  = editor:GetCurrentPos()
		local line = editor:LineFromPosition(pos)
		local col  = 1 + pos - editor:PositionFromLine(line)

		texts = { iff(editor:GetOvertype(), "OVR", "INS"),
				  iff(editor:GetReadOnly(), "R/O", "R/W"),
				  "Ln "..tostring(line + 1).." Col "..tostring(col) }
	end

	if ide.frame then
		for n = 1, 3 do
			if (texts[n] ~= statusTextTable[n]) then
				ide.frame:SetStatusText(texts[n], n)
				statusTextTable[n] = texts[n]
			end
		end
	end
end

function GetFileInfo (editor)
	if not editor or not openDocuments[editor:GetId()] then return "Estrela Editor" end
	local id = editor:GetId()
	local filePath   = openDocuments[id].filePath
	local fileName   = openDocuments[id].fileName
	if not filePath or not filename then return "Estrela Editor" end
	return "Estrela Editor ["..filePath.." - "..fileName.."]"
end

-- Check if file is altered, show dialog to reload it
function IsFileAlteredOnDisk(editor)
	if not editor then return end

	local id = editor:GetId()
	if openDocuments[id] then
		local filePath   = openDocuments[id].filePath
		local fileName   = openDocuments[id].fileName
		local oldModTime = openDocuments[id].modTime

		if filePath and (string.len(filePath) > 0) and oldModTime and oldModTime:IsValid() then
			local modTime = GetFileModTime(filePath)
			if modTime == nil then
				openDocuments[id].modTime = nil
				wx.wxMessageBox(fileName.." is no longer on the disk.",
								"Estrela Editor Message",
								wx.wxOK + wx.wxCENTRE, ide.frame)
			elseif modTime:IsValid() and oldModTime:IsEarlierThan(modTime) then
				local ret = wx.wxMessageBox(fileName.." has been modified on disk.\nDo you want to reload it?",
											"Estrela Editor Message",
											wx.wxYES_NO + wx.wxCENTRE, ide.frame)
				
				if ret ~= wx.wxYES or LoadFile(filePath, editor, true) then
					openDocuments[id].modTime = GetFileModTime(filePath)
				end
			end
		end
	end
end

-- Set if the document is modified and update the notebook page text
function SetDocumentModified(id, modified)
	local pageText = openDocuments[id].fileName or "untitled.lua"

	if modified then
		pageText = "* "..pageText
	end

	openDocuments[id].isModified = modified
	notebook:SetPageText(openDocuments[id].index, pageText)
end

-- ----------------------------------------------------------------------------
-- Create an editor and add it to the notebook
function CreateEditor(name)
	local editor = wxstc.wxStyledTextCtrl(notebook, editorID,
										  wx.wxDefaultPosition, wx.wxDefaultSize,
										  wx.wxBORDER_STATIC)

	editorID = editorID + 1 -- increment so they're always unique

	editor:SetBufferedDraw(true)
	editor:StyleClearAll()

	editor:SetFont(ide.font)
	editor:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font)

	editor:SetUseTabs(false)
	editor:SetTabWidth(4)
	editor:SetIndent(4)
	editor:SetUseTabs(true)
	editor:SetIndentationGuides(true)
	
	editor:SetCaretLineVisible(ide.config.editor.caretline and 1 or 0)
	
	editor:SetVisiblePolicy(wxstc.wxSTC_VISIBLE_SLOP, 3)
	--editor:SetXCaretPolicy(wxstc.wxSTC_CARET_SLOP, 10)
	--editor:SetYCaretPolicy(wxstc.wxSTC_CARET_SLOP, 3)

	editor:SetMarginWidth(0, editor:TextWidth(32, "99999_")) -- line # margin

	editor:SetMarginWidth(1, 16) -- marker margin
	editor:SetMarginType(1, wxstc.wxSTC_MARGIN_SYMBOL)
	editor:SetMarginSensitive(1, true)

	editor:MarkerDefine(BREAKPOINT_MARKER,   wxstc.wxSTC_MARK_ROUNDRECT, wx.wxWHITE, wx.wxRED)
	editor:MarkerDefine(CURRENT_LINE_MARKER, wxstc.wxSTC_MARK_ARROW,     wx.wxBLACK, wx.wxGREEN)

	editor:SetMarginWidth(2, 16) -- fold margin
	editor:SetMarginType(2, wxstc.wxSTC_MARGIN_SYMBOL)
	editor:SetMarginMask(2, wxstc.wxSTC_MASK_FOLDERS)
	editor:SetMarginSensitive(2, true)

	editor:SetFoldFlags(wxstc.wxSTC_FOLDFLAG_LINEBEFORE_CONTRACTED +
						wxstc.wxSTC_FOLDFLAG_LINEAFTER_CONTRACTED)

	editor:SetProperty("fold", "1")
	editor:SetProperty("fold.compact", "1")
	editor:SetProperty("fold.comment", "1")

	local grey = wx.wxColour(128, 128, 128)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPEN,    wxstc.wxSTC_MARK_BOXMINUS, wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDER,        wxstc.wxSTC_MARK_BOXPLUS,  wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERSUB,     wxstc.wxSTC_MARK_VLINE,    wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERTAIL,    wxstc.wxSTC_MARK_LCORNER,  wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEREND,     wxstc.wxSTC_MARK_BOXPLUSCONNECTED,  wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDEROPENMID, wxstc.wxSTC_MARK_BOXMINUSCONNECTED, wx.wxWHITE, grey)
	editor:MarkerDefine(wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL, wxstc.wxSTC_MARK_TCORNER,  wx.wxWHITE, grey)
	grey:delete()

	editor:Connect(wxstc.wxEVT_STC_MARGINCLICK,
			function (event)
				local line = editor:LineFromPosition(event:GetPosition())
				local margin = event:GetMargin()
				if margin == 1 then
					ToggleDebugMarker(editor, line)
				elseif margin == 2 then
					if wx.wxGetKeyState(wx.WXK_SHIFT) and wx.wxGetKeyState(wx.WXK_CONTROL) then
						FoldSome()
					else
						local level = editor:GetFoldLevel(line)
						if HasBit(level, wxstc.wxSTC_FOLDLEVELHEADERFLAG) then
							editor:ToggleFold(line)
						end
					end
				end
			end)

	editor:Connect(wxstc.wxEVT_STC_CHARADDED,
			function (event)
				-- auto-indent
				local ch = event:GetKey()
				local eol = editor:GetEOLMode()
				local pos = editor:GetCurrentPos()
				local line = editor:GetCurrentLine()
				local linetx = editor:GetLine(line)
				local linestart = editor:PositionFromLine(line)
				local localpos = pos-linestart
				
				for word in linetx:sub(1,localpos):gmatch "([a-zA-Z0-9_]+)[^a-zA-Z0-9_\r\n]" do
					AddDynamicWord(editor.api,word)
				end

				if (ch == char_CR and eol==2) or (ch == char_LF and eol==0) then
					local pos = editor:GetCurrentPos()
					local line = editor:LineFromPosition(pos)
					if (line > 0) then
						local indent = editor:GetLineIndentation(line - 1)
						if indent > 0 then
							editor:SetLineIndentation(line, indent)
							local tw = editor:GetTabWidth()
							local ut = editor:GetUseTabs()
							local indent = ut and (indent / tw) or indent
							editor:GotoPos(pos+indent)
						end
					end
				elseif ch == ("("):byte() then
					-- todo: improve tipinfo 
					local caller = linetx:sub(1,localpos):match("([a-zA-Z_0-9]+)%(%s*$")
					local class  = caller and linetx:sub(1,localpos):match("([a-zA-Z_0-9]+)%."..caller.."%(%s*$")
					
					local tip = caller and GetTipInfo(editor.api,caller,class)
					if tip then
						editor:CallTipShow(pos,tip)
					end
					--DisplayOutput(">  dang \n")
				elseif ide.config.autocomplete then -- code completion prompt
					-- JUST FIRE THE DAMN EVENT! - defined in menu_edit.lua
					local linestart = editor:PositionFromLine(line)
					local cnt = 0
					local state = ""
					for i=localpos,1,-1 do
						local c = linetx:sub(i,i)
						if c:match("[A-Za-z0-9_]") then
							if state == "space" then break end
							cnt = cnt + 1
							state = "word"
						elseif c:match("[%.:]") then
							state = "break"
							cnt = cnt + 1
						elseif c:match "%s" then 
							state = "space"
						else
							break
						end
					end
					-- must have "wx.X" otherwise too many items
					if (cnt > 1)  then
						--local range = editor:GetTextRange(start_pos-3, start_pos)
						--if range == "wx." then
							local commandEvent = wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED,
																   ID_AUTOCOMPLETE)
							wx.wxPostEvent(ide.frame, commandEvent)
						--end
					end
				end
			end)

	editor:Connect(wxstc.wxEVT_STC_USERLISTSELECTION,
			function (event)
				local pos = editor:GetCurrentPos()
				local start_pos = editor:WordStartPosition(pos, true)
				editor:SetSelection(start_pos, pos)
				editor:ReplaceSelection(event:GetText())
			end)

	editor:Connect(wxstc.wxEVT_STC_SAVEPOINTREACHED,
			function (event)
				SetDocumentModified(editor:GetId(), false)
			end)

	editor:Connect(wxstc.wxEVT_STC_SAVEPOINTLEFT,
			function (event)
				SetDocumentModified(editor:GetId(), true)
			end)

	editor:Connect(wxstc.wxEVT_STC_UPDATEUI,
			function (event)
				UpdateStatusText(editor)
			end)

	editor:Connect(wx.wxEVT_SET_FOCUS,
			function (event)
				event:Skip()
				if ide.in_evt_focus or exitingProgram then return end
				ide.in_evt_focus = true
				IsFileAlteredOnDisk(editor)
				ide.in_evt_focus = false
			end)

	if notebook:AddPage(editor, name, true) then
		local id            = editor:GetId()
		local document      = {}
		document.editor     = editor
		document.index      = notebook:GetSelection()
		document.fileName   = nil
		document.filePath   = nil
		document.modTime    = nil
		document.isModified = false
		openDocuments[id]   = document
	end

	return editor
end

function GetSpec(ext,forcespec)
	local spec = forcespec 
	
	-- search proper spec
	-- allow forcespec for "override"
	if ext and not spec then
		for i,curspec in pairs(ide.specs) do
			exts = curspec.exts
			if (exts) then
				for n,curext in ipairs(exts) do
					if (curext == ext) then
						spec = curspec
						break
					end
				end
				if (spec) then
					break
				end
			end
		end
	end
	
	return spec
end

function SetupKeywords(editor, ext, forcespec)
	local lexerstyleconvert = nil
	local spec = forcespec or GetSpec(ext)

	-- found a spec setup lexers and keywords
	if spec then
		editor:SetLexer(spec.lexer or wxstc.wxSTC_LEX_NULL)
		lexerstyleconvert = spec.lexerstyleconvert
		
		if (spec.keywords) then
			for i,words in ipairs(spec.keywords) do
				editor:SetKeyWords(i-1,words)
			end
		end

		if (spec.api == "lua") then
			-- Get the items in the global "wx" table for autocompletion
			if not wxkeywords then
				local keyword_table = {}
				for index, value in pairs(wx) do
					table.insert(keyword_table, "wx."..index.." ")
				end
				
				for index, value in pairs(wxstc) do
					table.insert(keyword_table, "wxstc."..index.." ")
				end
			
				table.sort(keyword_table)
				wxkeywords = table.concat(keyword_table)
			end
			local offset = spec.keywords and #spec.keywords or 5
			editor:SetKeyWords(offset, wxkeywords)
		end
		
		editor.api = GetApi(spec.apitype or "none")
		editor.spec = spec
	else
		editor:SetLexer(wxstc.wxSTC_LEX_NULL)
		editor:SetKeyWords(0, "")
		
		editor.api = GetApi("none")
		editor.spec = ide.specs.none
	end
	
	StylesApplyToEditor(ide.config.styles, editor,
							ide.font,ide.fontItalic,lexerstyleconvert)
end