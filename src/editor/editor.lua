-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local wxkeywords       = nil    -- a string of the keywords for scintilla of wxLua's wx.XXX items

local in_evt_focus     = false  -- true when in editor focus event to avoid recursion
local editorID         = 100    -- window id to create editor pages with, incremented for new editors

local openDocuments 	= ide.openDocuments
local ignoredFilesList 	= ide.ignoredFilesList
local statusBar = ide.frame.statusBar
local notebook			= ide.frame.vsplitter.splitter.notebook
local funclist			= ide.frame.toolBar.funclist
local edcfg 			= ide.config.editor


-- ----------------------------------------------------------------------------
-- Update the statusbar text of the frame using the given editor.
--  Only update if the text has changed.
statusTextTable = { "OVR?", "R/O?", "Cursor Pos" }

local function updateStatusText(editor)
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
				statusBar:SetStatusText(texts[n], n+1)
				statusTextTable[n] = texts[n]
			end
		end
	end
end

local function updateBraceMatch(editor)
	local pos  = editor:GetCurrentPos()
	local posp  = pos > 0 and pos-1
	local char = editor:GetCharAt(pos)
	local charp = posp and editor:GetCharAt(posp)
	local match = {	[string.byte("<")] = true, 
		[string.byte(">")] = true,
		[string.byte("(")] = true,
		[string.byte(")")] = true,
		[string.byte("{")] = true,
		[string.byte("}")] = true,
		[string.byte("[")] = true,
		[string.byte("]")] = true,
		}
		
	pos = (match[char] and pos) or (charp and match[charp] and posp)

	if (pos) then
		local pos2 = editor:BraceMatch(pos)
		if (pos2 == wxstc.wxSTC_INVALID_POSITION) then
			editor:BraceBadLight(pos)
		else
			editor:BraceHighlight(pos,pos2)
		end
		editor.matchon = true
	elseif(editor.matchon) then
		editor:BraceBadLight(wxstc.wxSTC_INVALID_POSITION)
		editor:BraceHighlight(wxstc.wxSTC_INVALID_POSITION,-1)
		editor.matchon = false
	end
end

local function getFileTitle (editor)
	if not editor or not openDocuments[editor:GetId()] then return GetIDEString("editor") end
	local id = editor:GetId()
	local filePath   = openDocuments[id].filePath
	local fileName   = openDocuments[id].fileName
	if not filePath or not fileName then return GetIDEString("editor") end
	return GetIDEString("editor").." ["..filePath.."]"
end

-- Check if file is altered, show dialog to reload it
local function isFileAlteredOnDisk(editor)
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
								GetIDEString("editormessage"),
								wx.wxOK + wx.wxCENTRE, ide.frame)
			elseif modTime:IsValid() and oldModTime:IsEarlierThan(modTime) then
				local ret = wx.wxMessageBox(fileName.." has been modified on disk.\nDo you want to reload it?",
											GetIDEString("editormessage"),
											wx.wxYES_NO + wx.wxCENTRE, ide.frame)
				
				if ret ~= wx.wxYES or LoadFile(filePath, editor, true) then
					openDocuments[id].modTime = GetFileModTime(filePath)
				end
			end
		end
	end
end


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
	updateStatusText(editor) -- update even if nil
	statusBar:SetStatusText("",1)
	ide.frame:SetTitle(getFileTitle(editor))
	
	if editor then
		funclist:Clear()
		editor:SetFocus()
		editor:SetSTCFocus(true)
		isFileAlteredOnDisk(editor)
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
	
	editor.matchon = false
	editor.assignscache = false

	editor:SetBufferedDraw(true)
	editor:StyleClearAll()

	editor:SetFont(ide.font)
	editor:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.font)

	editor:SetUseTabs(false)
	editor:SetTabWidth(ide.config.editor.tabwidth or 4)
	editor:SetIndent(ide.config.editor.tabwidth or 4)
	editor:SetUseTabs(ide.config.editor.usetabs and true or false)
	editor:SetIndentationGuides(true)
	editor:SetViewWhiteSpace(ide.config.editor.whitespace and true or false)
	
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
	
	editor:AutoCompSetIgnoreCase(ide.config.acandtip.ignorecase)
	if (ide.config.acandtip.strategy > 0) then
		editor:AutoCompSetAutoHide(0)
		editor:AutoCompStops([[ \n\t=-+():.,;*/!"'$%&~'#°^@?´`<>][|}{]])
	end
	
	editor.ev = {}

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
	
	editor:Connect(wxstc.wxEVT_STC_MODIFIED,
			function (event)
				if (editor.assignscache and editor:GetCurrentLine() ~= editor.assignscache.line) then
					editor.assignscache = false
				end
				if (bit.band(event:GetModificationType(),wxstc.wxSTC_MOD_INSERTTEXT) ~= 0) then
					table.insert(editor.ev,{event:GetPosition(),event:GetLinesAdded()})
				end
				if (bit.band(event:GetModificationType(),wxstc.wxSTC_MOD_DELETETEXT) ~= 0) then
					table.insert(editor.ev,{event:GetPosition(),0})
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
				
				linetxtopos = linetx:sub(1,localpos)
				
				AddDynamicWordsCurrent(editor,linetxtopos)

				if (ch == char_CR and eol==2) or (ch == char_LF and eol==0) then
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
					 
					local tip = GetTipInfo(editor,linetxtopos,ide.config.acandtip.shorttip)
					if tip then
						editor:CallTipShow(pos,tip)
					end
					
				elseif ide.config.autocomplete then -- code completion prompt
					
					local trigger = linetxtopos:match("["..editor.spec.sep.."%w_]+$")
					
					if (trigger and (#trigger > 1 or trigger:match("[%.:]")))  then
						-- defined in menu_edit.lua
						local commandEvent = wx.wxCommandEvent(wx.wxEVT_COMMAND_MENU_SELECTED,
															   ID_AUTOCOMPLETE)
						wx.wxPostEvent(ide.frame, commandEvent)
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
				updateStatusText(editor)
				updateBraceMatch(editor)
				for e,iv in ipairs(editor.ev) do
					local line = editor:LineFromPosition(iv[1])
					--DisplayOutput("modified "..tostring(line).." "..tostring(iv[2]))
					IndicateFunctions(editor,line,line+iv[2])
				end
				editor.ev = {}
			end)

	editor:Connect(wx.wxEVT_SET_FOCUS,
			function (event)
				event:Skip()
				if ide.in_evt_focus or exitingProgram then return end
				ide.in_evt_focus = true
				isFileAlteredOnDisk(editor)
				ide.in_evt_focus = false
			end)
			

	--[[				
	editor:Connect(wxstc.wxEVT_STC_POSCHANGED,
			function (event)
				-- brace checking
				
			end)
			]]
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
--print("SPEC:"..ext..":"..tostring(spec))
	return spec
end

function IndicateFunctions(editor, lines, linee)
	if (not (edcfg.showfncall and editor.spec and editor.spec.isfncall)) then return end

	--DisplayOutput("indicate: "..tostring(lines).." "..tostring(linee).."\n")
	
	local es = editor:GetEndStyled()
	local lines = lines or 0
	local linee = linee or editor:GetLineCount()-1
	
	if (lines < 0) then return end
	
	local isfunc = editor.spec.isfncall
	local iscomment = editor.spec.iscomment
	local iskeyword0 = editor.spec.iskeyword0
	local isinvalid = {}
	for i,v in pairs(iscomment) do
		isinvalid[i] = v
	end
	for i,v in pairs(iskeyword0) do
		isinvalid[i] = v
	end
	
	local INDICS_MASK = wxstc.wxSTC_INDICS_MASK
	local INDIC0_MASK = wxstc.wxSTC_INDIC0_MASK

	
	for line=lines,linee do
		local tx = editor:GetLine(line)
		local ls = editor:PositionFromLine(line)
		
		local from = 1
		local off = -1
		
		
		editor:StartStyling(ls,INDICS_MASK)
		editor:SetStyling(#tx,0)
		while from do
			tx = from==1 and tx or string.sub(tx,from)
			
			local f,t,w = isfunc(tx)
			
			if (f) then
				local p = ls+f+off
				local s = bit.band(editor:GetStyleAt(p),31)
				if (not (isinvalid[s])) then
					
					editor:StartStyling(p,INDICS_MASK)
					editor:SetStyling(t-f,INDIC0_MASK + 1)
				else
					editor:StartStyling(p,INDICS_MASK)
					editor:SetStyling(t-f,0)
				end
				
				off = off + t
			end
			from = t and (t+1)
		end
	end
	editor:StartStyling(es,31)
end

function SetupKeywords(editor, ext, forcespec, styles, font, fontitalic)
	local lexerstyleconvert = nil
	local spec = forcespec or GetSpec(ext)
--print(ext..":"..tostring(spec.apitype))
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
	
	StylesApplyToEditor(styles or ide.config.styles, editor,
							font or ide.font,fontitalic or ide.fontItalic,lexerstyleconvert)
end

----------------------------------------------------
-- function list for current file


funclist:Connect(wx.wxEVT_SET_FOCUS, 
	function (event) 
		event:Skip()
		
		-- parse current file and update list
		funclist:Clear()
		local editor = GetEditor()
				
		if (not (editor and editor.spec and editor.spec.isfndef)) then return end
		
		local lines = 0
		local linee = editor:GetLineCount()-1
		
		for line=lines,linee do
			local tx = editor:GetLine(line)
			local s,e,cap,l = editor.spec.isfndef(tx)
			if (s) then
				funclist:Append((l and "   " or "")..cap,line)
			end
		end
		
	end)

funclist:Connect(wx.wxEVT_COMMAND_CHOICE_SELECTED, 
	function (event) 
		-- test if updated
		-- jump to line
		event:Skip()
		local l = event:GetClientData(s)
		if (l) then
			local editor = GetEditor()
			editor:GotoLine(l)
			editor:SetFocus()
			editor:SetSTCFocus(true)
		end
	end)
