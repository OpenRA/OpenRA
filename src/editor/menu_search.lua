-- authors: Lomtik Software (J. Winwood & John Labenski)
-- Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- Create the Search menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar

local findReplace = ide.findReplace

local findMenu = wx.wxMenu{
  { ID_FIND, TR("&Find")..KSC(ID_FIND), TR("Find text") },
  { ID_FINDNEXT, TR("Find &Next")..KSC(ID_FINDNEXT), TR("Find the next text occurrence") },
  { ID_FINDPREV, TR("Find &Previous")..KSC(ID_FINDPREV), TR("Find the earlier text occurence") },
  { ID_REPLACE, TR("&Replace")..KSC(ID_REPLACE), TR("Find and replace text") },
  { },
  { ID_FINDINFILES, TR("Find &In Files")..KSC(ID_FINDINFILES), TR("Find text in files") },
  { ID_REPLACEINFILES, TR("Re&place In Files")..KSC(ID_REPLACEINFILES), TR("Find and replace text in files") },
  { },
  { ID_GOTOLINE, TR("&Goto Line")..KSC(ID_GOTOLINE), TR("Go to a selected line") },
  { },
  { ID_SORT, TR("&Sort")..KSC(ID_SORT), TR("Sort selected lines") }}
menuBar:Append(findMenu, TR("&Search"))

function OnUpdateUISearchMenu(event) event:Enable(GetEditor() ~= nil) end

frame:Connect(ID_FIND, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:GetSelectedString()
    findReplace:Show(false)
  end)
frame:Connect(ID_FIND, wx.wxEVT_UPDATE_UI, OnUpdateUISearchMenu)

frame:Connect(ID_REPLACE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:GetSelectedString()
    findReplace:Show(true)
  end)
frame:Connect(ID_REPLACE, wx.wxEVT_UPDATE_UI, OnUpdateUISearchMenu)

frame:Connect(ID_FINDINFILES, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:GetSelectedString()
    findReplace:Show(false,true)
  end)
frame:Connect(ID_REPLACEINFILES, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    findReplace:GetSelectedString()
    findReplace:Show(true,true)
  end)

frame:Connect(ID_FINDNEXT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) findReplace:GetSelectedString() findReplace:FindString() end)
frame:Connect(ID_FINDNEXT, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(findReplace:GetSelectedString() and findReplace:HasText()) end)

frame:Connect(ID_FINDPREV, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event) findReplace:GetSelectedString() findReplace:FindString(true) end)
frame:Connect(ID_FINDPREV, wx.wxEVT_UPDATE_UI,
  function (event) event:Enable(findReplace:GetSelectedString() and findReplace:HasText()) end)

-------------------- Find replace end

frame:Connect(ID_GOTOLINE, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local linecur = editor:LineFromPosition(editor:GetCurrentPos())
    local linemax = editor:LineFromPosition(editor:GetLength()) + 1
    local linenum = wx.wxGetNumberFromUser(TR("Enter line number"),
      "1 .. "..tostring(linemax),
      TR("Goto Line"),
      linecur, 1, linemax,
      frame)
    if linenum > 0 then
      editor:GotoLine(linenum-1)
    end
  end)
frame:Connect(ID_GOTOLINE, wx.wxEVT_UPDATE_UI, OnUpdateUISearchMenu)

frame:Connect(ID_SORT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local editor = GetEditor()
    local buf = {}
    for line in string.gmatch(editor:GetSelectedText()..'\n', "(.-)\r?\n") do
      table.insert(buf, line)
    end
    if #buf > 0 then
      table.sort(buf)
      editor:ReplaceSelection(table.concat(buf,"\n"))
    end
  end)
frame:Connect(ID_SORT, wx.wxEVT_UPDATE_UI, OnUpdateUISearchMenu)
