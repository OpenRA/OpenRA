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
    { ID_CLOSE, TR("&Close page")..KSC(ID_CLOSE), TR("Close the current editor window") },
    { },
    { ID_SAVE, TR("&Save")..KSC(ID_SAVE), TR("Save the current document") },
    { ID_SAVEAS, TR("Save &As...")..KSC(ID_SAVEAS), TR("Save the current document to a file with a new name") },
    { ID_SAVEALL, TR("Save A&ll")..KSC(ID_SAVEALL), TR("Save all open documents") },
    { },
    -- placeholder for ID_RECENTFILES
    { },
    { ID_EXIT, TR("E&xit")..KSC(ID_EXIT), TR("Exit program") }})
menuBar:Append(fileMenu, TR("&File"))

local filehistorymenu = wx.wxMenu({})
local filehistory = wx.wxMenuItem(fileMenu, ID_RECENTFILES,
  TR("Recent Files")..KSC(ID_RECENTFILES), TR("File history"), wx.wxITEM_NORMAL, filehistorymenu)
fileMenu:Insert(8,filehistory)
function UpdateFileHistoryUI(list)
  -- remove all at first
  for i=1,filehistorymenu:GetMenuItemCount() do
    filehistorymenu:Delete( ID("file.recentfiles."..i))
  end
  for i=1,#list do
    local file = list[i].filename
    local item = wx.wxMenuItem(filehistorymenu, ID("file.recentfiles."..i),file,"")
    filehistorymenu:Append(item)
  end
end

for i=1,ide.config.filehistorylength do
  frame:Connect(ID("file.recentfiles."..i), wx.wxEVT_COMMAND_MENU_SELECTED,
    function (event)
      local item = filehistorymenu:FindItemByPosition(i-1)
      local filename = item:GetLabel()
      LoadFile(filename)
    end
  )
end

frame:Connect(ID_NEW, wx.wxEVT_COMMAND_MENU_SELECTED, NewFile)
frame:Connect(ID_OPEN, wx.wxEVT_COMMAND_MENU_SELECTED, OpenFile)
frame:Connect(ID_SAVE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local id = editor:GetId()
    local filePath = openDocuments[id].filePath
    if (filePath) then
      SaveFile(editor, filePath)
    else
      SaveFileAs(editor)
    end
  end)
frame:Connect(ID_SAVE, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    local enabled = false
    if editor then
      local id = editor:GetId()
      enabled = openDocuments[id]
        and (openDocuments[id].isModified or not openDocuments[id].filePath)
    end
    event:Enable(enabled)
  end)

frame:Connect(ID_SAVEAS, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    SaveFileAs(editor)
  end)
frame:Connect(ID_SAVEAS, wx.wxEVT_UPDATE_UI,
  function (event)
    local editor = GetEditor()
    event:Enable(editor ~= nil)
  end)

frame:Connect(ID_SAVEALL, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    SaveAll()
  end)
frame:Connect(ID_SAVEALL, wx.wxEVT_UPDATE_UI,
  function (event)
    local atLeastOneModifiedDocument = false
    for id, document in pairs(openDocuments) do
      if document.isModified or not document.filePath then
        atLeastOneModifiedDocument = true
        break
      end
    end
    event:Enable(atLeastOneModifiedDocument)
  end)

frame:Connect(ID_CLOSE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    ClosePage() -- this will find the current editor
  end)
frame:Connect(ID_CLOSE, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable(GetEditor() ~= nil)
  end)

frame:Connect(ID_EXIT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if not SaveOnExit(true) then return end
    frame:Close() -- this will trigger wxEVT_CLOSE_WINDOW
  end)
