-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- Create the File menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar
local openDocuments = ide.openDocuments
local debugger = ide.debugger

local fileMenu = wx.wxMenu({
    { ID_NEW, "&New\tCtrl-N", "Create an empty document" },
    { ID_OPEN, "&Open...\tCtrl-O", "Open an existing document" },
    { ID_CLOSE, "&Close page\tCtrl+W", "Close the current editor window" },
    { },
    { ID_SAVE, "&Save\tCtrl-S", "Save the current document" },
    { ID_SAVEAS, "Save &As...\tAlt-Shift-S", "Save the current document to a file with a new name" },
    { ID_SAVEALL, "Save A&ll...\tCtrl-Shift-S", "Save all open documents" },
    { },
    --{ ID "file.recentfiles", "Recent files",},
    { },
    { ID_EXIT, "E&xit\tAlt-X", "Exit Program" }})
menuBar:Append(fileMenu, "&File")

local filehistorymenu = wx.wxMenu({})
local filehistory = wx.wxMenuItem(fileMenu,ID"file.recentfiles","Recent files", "File history", wx.wxITEM_NORMAL,filehistorymenu)
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
    if editor then
      local id = editor:GetId()
      if openDocuments[id] then
        event:Enable(openDocuments[id].isModified or not openDocuments[id].filePath)
      end
    end
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
      if document.isModified then
        atLeastOneModifiedDocument = true
        break
      end
    end
    event:Enable(atLeastOneModifiedDocument)
  end)

frame:Connect(ID_CLOSE, wx.wxEVT_COMMAND_MENU_SELECTED, CloseFile)

frame:Connect(ID_CLOSE, wx.wxEVT_UPDATE_UI,
  function (event)
    event:Enable((GetEditor() ~= nil) and (debugger.server == nil))
  end)

frame:Connect(ID_EXIT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    if not SaveOnExit(true) then return end
    frame:Close() -- will handle wxEVT_CLOSE_WINDOW
    DebuggerCloseWatchWindow()
  end)
