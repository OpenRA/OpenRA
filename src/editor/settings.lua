-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide

-- ----------------------------------------------------------------------------
-- Initialize the wxConfig for loading/saving the preferences

local settings = wx.wxFileConfig(GetIDEString("settingsapp"),GetIDEString("settingsvendor"))
ide.settings = settings

if settings then
	-- we dont like defaults ?
	--settings:SetRecordDefaults()
end


local function settingsReadSafe(settings,what,default)
	local cr,out = settings:Read(what,default)
	
	if (cr) then
		return out
	else
		return default
	end
end

-- ----------------------------------------------------------------------------
-- wxConfig load/save preferences functions

function SettingsRestoreFramePosition(window, windowName)
	local path = settings:GetPath()
	settings:SetPath("/"..windowName)

	local s = -1
	      s = tonumber(select(2,settings:Read("s", -1)))
	local x = tonumber(select(2,settings:Read("x", 0)))
	local y = tonumber(select(2,settings:Read("y", 0)))
	local w = tonumber(select(2,settings:Read("w", 600)))
	local h = tonumber(select(2,settings:Read("h", 400)))

	if (s ~= 1) and (s ~= 2) then
		local clientX, clientY, clientWidth, clientHeight
		clientX, clientY, clientWidth, clientHeight = wx.wxClientDisplayRect()

		if x < clientX then x = clientX end
		if y < clientY then y = clientY end

		if w > clientWidth  then w = clientWidth end
		if h > clientHeight then h = clientHeight end

		window:SetSize(x, y, w, h)
	elseif s == 1 then
		window:Maximize(true)
	end

	settings:SetPath(path)
end

function SettingsSaveFramePosition(window, windowName)
	local path = settings:GetPath()
	settings:SetPath("/"..windowName)

	local s    = 0
	local w, h = window:GetSizeWH()
	local x, y = window:GetPositionXY()

	if window:IsMaximized() then
		s = 1
	elseif window:IsIconized() then
		s = 2
	end

	settings:Write("s", s==2 and 0 or s) -- iconized maybe - but that shouldnt be saved

	if s == 0 then
		settings:Write("x", x)
		settings:Write("y", y)
		settings:Write("w", w)
		settings:Write("h", h)
	end

	settings:SetPath(path)
end

--- 
-- (table) SettingsRestoreFileHistory (function)
-- restores a list of recently loaded documents from the settings table
-- a table is returned which contains tables each with a filename key, pointing to 
-- the filename
function SettingsRestoreFileHistory(fntab)
	local path = settings:GetPath()
	local listname = "/filehistory"
	settings:SetPath(listname)
	
	local outtab = {}
	local inlist = {}
	for id=1,ide.config.filehistorylength do
		local couldread, name = settings:Read(tostring(id), "")
		if not couldread  or name == "" then break end
		if not inlist[name] then
			inlist[name] = true
			table.insert(outtab,{filename = name})
		end
	end
	
	if fntab then fntab(outtab) end
	
	settings:SetPath(path)
	
	return outtab
end

function SettingsAppendFileToHistory (filename)
	local listname = "/filehistory"
	local oldlist = SettingsRestoreFileHistory(nil,listname)
	
	-- if the file has been in the history before, remove it
	for i=#oldlist,1,-1 do
		if oldlist[i] == filename then table.remove(oldlist,i) end
	end
	
	table.insert(oldlist,1,{filename=filename})
	
	-- remove all entries that are no longer needed
	while #oldlist>ide.config.filehistorylength do table.remove(oldlist) end
	
	local path = settings:GetPath()
	settings:DeleteGroup(listname)
	settings:SetPath(listname)
	
	for i,doc in ipairs(oldlist) do
		settings:Write(tostring(i), doc.filename)
	end
	
	UpdateFileHistoryUI(oldlist)
	
	settings:SetPath(path)
end

---
-- () SettingsRestoreFileSession (function)
-- restores a list of opened files from the file settings
-- calls the given function with the restored table, a list 
-- of tables containing tables like
--  {filename = "filename", cursorpos = <cursor position>}
function SettingsRestoreFileSession(fntab)
	local listname = "/session"
	local path = settings:GetPath()
	settings:SetPath(listname)
	local outtab = {}
	local couldread = true
	local id = 1
	local name 
	while(couldread) do
		couldread, name = settings:Read(tostring(id), "")
		local fname,cursorpos = name:match("^(.+);(.-)$")
		name = fname or name
		cursorpos = tonumber(cursorpos or 0)
		couldread = couldread and name ~= ""
		if (couldread) then
			table.insert(outtab,{filename = name, cursorpos = cursorpos})
			id = id + 1
		end
	end
	
	local index = settingsReadSafe(settings,"index",1)
	
	if fntab then fntab(outtab,index) end
	
	settings:SetPath(path)
	
	return outtab
end

---
-- () SettingsSaveFileList (table opendocs)
-- saves the list of currently opened documents (passed in the opendocs table)
-- in the settings.
function SettingsSaveFileSession(opendocs,index)
	local listname = "/session"
	local path = settings:GetPath()
	settings:DeleteGroup(listname)
	settings:SetPath(listname)
	
	for i,doc in ipairs(opendocs) do
		settings:Write(tostring(i), doc.filename..";"..doc.cursorpos)
	end
	settings:Write("index",index)

	
	settings:SetPath(path)
end

---
-- () SettingsRestoreProjectSession (function)

function SettingsRestoreProjectSession(fntab)
	local listname = "/projectsession"
	local path = settings:GetPath()
	settings:SetPath(listname)
	local outtab = {}
	local couldread = true
	local id = 1
	local name 
	while(couldread) do
		couldread, name = settings:Read(tostring(id), "")
		couldread = couldread and name ~= ""
		if (couldread) then
			if (wx.wxDirExists(name)) then
				table.insert(outtab,name)
			end
			id = id + 1
		end
	end

	
	if fntab then fntab(outtab) end
	
	settings:SetPath(path)
	
	return outtab
end

---
-- () SettingsSaveProjectSession  (table projdirs)
-- saves the list of currently active projects
-- in the settings.
function SettingsSaveProjectSession(projdirs)
	local listname = "/projectsession"
	local path = settings:GetPath()
	settings:DeleteGroup(listname)
	settings:SetPath(listname)
	
	for i,dir in ipairs(projdirs) do
		settings:Write(tostring(i), dir)
	end
	
	settings:SetPath(path)
end

-----------------------------------

function SettingsRestoreView()
	local listname = "/view"
	local path = settings:GetPath()
	settings:SetPath(listname)
	
	local frame = ide.frame
	local vsplitter = frame.vsplitter
	local sidenotebook = vsplitter.sidenotebook
	local splitter = vsplitter.splitter
	
	local treevis   = tonumber(settingsReadSafe(settings,"filetreevis",1))
	local outvis    = tonumber(settingsReadSafe(settings,"outputvis",1))
	
	ide.config.view.vsplitterpos   = tonumber(settingsReadSafe(settings,"filetreewidth",ide.config.view.vsplitterpos))
	ide.config.view.splitterheight = tonumber(settingsReadSafe(settings,"outputheight",ide.config.view.splitterheight))
	
	
	local w, h = frame:GetClientSizeWH()
	
	if (treevis > 0) then
		vsplitter:SplitVertically(sidenotebook,splitter,ide.config.view.vsplitterpos)
	else
		vsplitter:Initialize(splitter)
	end
		
	if (outvis > 0) then
		splitter:SplitHorizontally(splitter.notebook, splitter.bottomnotebook, h-ide.config.view.splitterheight)
	else
		splitter:Initialize(splitter.notebook)
	end
	
	frame.menuBar:Check(ID "view.filetree.show", treevis > 0)
	frame.menuBar:Check(ID "view.output.show", outvis > 0)
	
	settings:SetPath(path)
end

function SettingsSaveView()
	local listname = "/view"
	local path = settings:GetPath()
	settings:DeleteGroup(listname)
	settings:SetPath(listname)
	
	local frame = ide.frame
	local vsplitter = frame.vsplitter
	local splitter = vsplitter.splitter
	
	local w, h = frame:GetClientSizeWH()
	
	settings:Write("filetreevis", 	vsplitter:IsSplit())
	settings:Write("filetreewidth",	vsplitter:IsSplit() and vsplitter:GetSashPosition() or ide.config.view.vsplitterpos)
	settings:Write("outputvis",	 	splitter:IsSplit())
	settings:Write("outputheight",	splitter:IsSplit() and (h-splitter:GetSashPosition()) or ide.config.view.splitterheight)
	
	settings:SetPath(path)
end

function SettingsRestoreEditorSettings()
	local listname = "/editor"
	local path = settings:GetPath()
	settings:SetPath(listname)
	
	ide.config.interpreter = settingsReadSafe(settings,"interpreter",ide.config.interpreter)
	ProjectSetInterpreter(ide.config.interpreter)
end
function SettingsSaveEditorSettings()
	local listname = "/editor"
	local path = settings:GetPath()
	settings:DeleteGroup(listname)
	settings:SetPath(listname)
	
	settings:Write("interpreter", ide.interpreter.fname)
	
	settings:SetPath(path)
end
