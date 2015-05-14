-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local ide = ide
local frame = ide.frame
local menuBar = frame.menuBar
local openDocuments = ide.openDocuments

local fileMenu = wx.wxMenu({
    { ID_NEW, TR("&New")..KSC(ID_NEW), TR("Create an empty document") },
    { ID_OPEN, TR("&Open...")..KSC(ID_OPEN), TR("Open an existing document") },
    { ID_CLOSE, TR("&Close Page")..KSC(ID_CLOSE), TR("Close the current editor window") },
    { },
    { ID_SAVE, TR("&Save")..KSC(ID_SAVE), TR("Save the current document") },
    { ID_SAVEAS, TR("Save &As...")..KSC(ID_SAVEAS), TR("Save the current document to a file with a new name") },
    { ID_SAVEALL, TR("Save A&ll")..KSC(ID_SAVEALL), TR("Save all open documents") },
    { },
    -- placeholder for ID_RECENTFILES and ID_RECENTPROJECTS
    { },
    { ID_EXIT, TR("E&xit")..KSC(ID_EXIT), TR("Exit program") }})
menuBar:Append(fileMenu, TR("&File"))

local filehistorymenu = wx.wxMenu({
    { },
    { ID_RECENTFILESCLEAR, TR("Clear Items")..KSC(ID_RECENTFILESCLEAR), TR("Clear items from this list") },
})
local filehistory = wx.wxMenuItem(fileMenu, ID_RECENTFILES,
  TR("Recent Files")..KSC(ID_RECENTFILES), TR("File history"), wx.wxITEM_NORMAL, filehistorymenu)
fileMenu:Insert(8,filehistory)

local projecthistorymenu = wx.wxMenu({
    { },
    { ID_RECENTPROJECTSCLEAR, TR("Clear Items")..KSC(ID_RECENTPROJECTSCLEAR), TR("Clear items from this list") },
})
local projecthistory = wx.wxMenuItem(fileMenu, ID_RECENTPROJECTS,
  TR("Recent &Projects")..KSC(ID_RECENTPROJECTS), TR("Project history"), wx.wxITEM_NORMAL, projecthistorymenu)
fileMenu:Insert(9,projecthistory)

do -- recent file history
  local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
  local function isSameAs(f1, f2)
    return f1 == f2 or iscaseinsensitive and f1:lower() == f2:lower()
  end

  local filehistory = {[0] = 1}

  -- add file to the file history removing duplicates
  local function addFileHistory(filename)
    -- a new (empty) tab is opened; don't change the history
    if not filename then return end

    local fn = wx.wxFileName(filename)
    if fn:Normalize() then filename = fn:GetFullPath() end

    local index = filehistory[0]

    -- special case: selecting the current file (or moving through the history)
    if filehistory[index] and isSameAs(filename, filehistory[index].filename) then return end

    -- something else is selected
    -- (1) flip the history from 1 to the current index
    for i = 1, math.floor(index/2) do
      filehistory[i], filehistory[index-i+1] = filehistory[index-i+1], filehistory[i]
    end

    -- (2) if the file is in the history, remove it
    for i = #filehistory, 1, -1 do
      if isSameAs(filename, filehistory[i].filename) then
        table.remove(filehistory, i)
      end
    end

    -- (3) add the file to the top and update the index
    table.insert(filehistory, 1, {filename=filename})
    filehistory[0] = 1

    -- (4) remove all entries that are no longer needed
    while #filehistory>ide.config.filehistorylength do table.remove(filehistory) end
  end

  local function remFileHistory(filename)
    if not filename then return end

    local fn = wx.wxFileName(filename)
    if fn:Normalize() then filename = fn:GetFullPath() end

    local index = filehistory[0]

    -- special case: removing the current file
    if filehistory[index] and isSameAs(filename, filehistory[index].filename) then
      -- (1) flip the history from 1 to the current index
      for i = 1, math.floor(index/2) do
        filehistory[i], filehistory[index-i+1] = filehistory[index-i+1], filehistory[i]
      end
    end

    -- (2) if the file is in the history, remove it
    for i = #filehistory, 1, -1 do
      if isSameAs(filename, filehistory[i].filename) then
        table.remove(filehistory, i)
      end
    end

    -- (3) update index
    filehistory[0] = 1
  end

  local updateRecentFiles -- need forward declaration because of recursive refs

  local function loadRecent(event)
    local id = event:GetId()
    local item = filehistorymenu:FindItem(id)
    local filename = item:GetLabel()
    local index = filehistory[0]
    filehistory[0] = (
      (index > 1 and id == ID("file.recentfiles."..(index-1)) and index-1) or
      (index < #filehistory) and id == ID("file.recentfiles."..(index+1)) and index+1 or
      1)
    if not LoadFile(filename, nil, true) then
      wx.wxMessageBox(
        TR("File '%s' no longer exists."):format(filename),
        GetIDEString("editormessage"),
        wx.wxOK + wx.wxCENTRE, ide.frame)
      remFileHistory(filename)
      updateRecentFiles(filehistory)
    end
  end

  local items = 0
  updateRecentFiles = function (list)
    -- protect against recent files menu not being present
    if not ide:FindMenuItem(ID_RECENTFILES) then return end

    for i=1, #list do
      local file = list[i].filename
      local id = ID("file.recentfiles."..i)
      local label = file..(
        i == list[0]-1 and KSC(ID_RECENTFILESNEXT) or
        i == list[0]+1 and KSC(ID_RECENTFILESPREV) or
        "")
      if i <= items then -- this is an existing item; update the label
        filehistorymenu:FindItem(id):SetItemLabel(label)
      else -- need to add an item
        local item = wx.wxMenuItem(filehistorymenu, id, label, "")
        filehistorymenu:Insert(i-1, item)
        frame:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, loadRecent)
      end
    end
    for i=items, #list+1, -1 do -- delete the rest if the list got shorter
      filehistorymenu:Delete(filehistorymenu:FindItemByPosition(i-1))
    end
    items = #list -- update the number of items for the next refresh

    -- enable if there are any recent files
    fileMenu:Enable(ID_RECENTFILES, #list > 0)
  end

  -- public methods
  function GetFileHistory() return filehistory end
  function SetFileHistory(fh)
    filehistory = fh
    filehistory[0] = 1
    updateRecentFiles(filehistory)
  end
  function AddToFileHistory(filename)
    addFileHistory(filename)
    updateRecentFiles(filehistory)
  end

  function FileRecentListUpdate(menu)
    local list = filehistory
    for i=#list, 1, -1 do
      local id = ID("file.recentfiles."..i)
      local label = list[i].filename
      local item = wx.wxMenuItem(menu, id, label, "")
      menu:Insert(0, item)
      ide.frame:Connect(id, wx.wxEVT_COMMAND_MENU_SELECTED, loadRecent)
    end
  end
end

frame:Connect(ID_NEW, wx.wxEVT_COMMAND_MENU_SELECTED, function() return NewFile() end)
frame:Connect(ID_OPEN, wx.wxEVT_COMMAND_MENU_SELECTED, OpenFile)
frame:Connect(ID_SAVE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    local editor = ide.findReplace:CanSave(GetEditorWithFocus()) or GetEditor()
    local doc = ide:GetDocument(editor)
    SaveFile(editor, doc and doc:GetFilePath() or nil)
  end)
frame:Connect(ID_SAVE, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(ide.findReplace:CanSave(GetEditorWithFocus())
      and true or EditorIsModified(GetEditor()))
  end)

frame:Connect(ID_SAVEAS, wx.wxEVT_COMMAND_MENU_SELECTED,
  function ()
    SaveFileAs(GetEditor())
  end)
frame:Connect(ID_SAVEAS, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(GetEditor() ~= nil)
  end)

frame:Connect(ID_SAVEALL, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    SaveAll()
  end)
frame:Connect(ID_SAVEALL, wx.wxEVT_UPDATE_UI,
  function (event)
    local atLeastOneModifiedDocument = false
    for _, document in pairs(openDocuments) do
      if document.isModified or not document.filePath then
        atLeastOneModifiedDocument = true
        break
      end
    end
    event:Enable(atLeastOneModifiedDocument)
  end)

frame:Connect(ID_CLOSE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditorWithFocus()
    local nb = ide:GetOutputNotebook()
    local index = editor and nb:GetPageIndex(editor)
    if index and ide.findReplace:IsPreview(editor) and index >= 0 then
      nb:DeletePage(index) -- close preview tab
    else
      ClosePage() -- this will find the current editor tab
    end
  end)
frame:Connect(ID_CLOSE, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(ide.findReplace:IsPreview(GetEditorWithFocus()) or GetEditor() ~= nil)
  end)

frame:Connect(ID_EXIT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    frame:Close() -- this will trigger wxEVT_CLOSE_WINDOW
  end)

frame:Connect(ID_RECENTPROJECTSCLEAR, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) FileTreeProjectListClear() end)

frame:Connect(ID_RECENTFILESCLEAR, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    SetFileHistory({})
    local ed = ide:GetEditor()
    if ed then AddToFileHistory(ide:GetDocument(ed):GetFilePath()) end
  end)

local recentprojects = 0
frame:Connect(ID_RECENTPROJECTS, wx.wxEVT_UPDATE_UI,
  function (event)
    recentprojects = FileTreeProjectListUpdate(projecthistorymenu, recentprojects)
    if not recentprojects then return end
    local pos = 1 -- add shortcut for the previous project (if any)
    if recentprojects > pos then
      local item = projecthistorymenu:FindItemByPosition(pos)
      item:SetItemLabel(item:GetItemLabelText()..KSC(ID_RECENTPROJECTSPREV))
    end
    event:Enable(recentprojects > 0)
  end)
