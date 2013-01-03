-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
-- Lomtik Software (J. Winwood & John Labenski)
---------------------------------------------------------
local ide = ide

-- Pick some reasonable fixed width fonts to use for the editor
local function setFont(style, config, name, size)
  return wx.wxFont(config.fontsize or size or 10, wx.wxFONTFAMILY_MODERN, style,
    wx.wxFONTWEIGHT_NORMAL, false, config.fontname or name,
    config.fontencoding or wx.wxFONTENCODING_DEFAULT)
end
ide.font.eNormal = setFont(wx.wxFONTSTYLE_NORMAL, ide.config.editor, wx.__WXMSW__ and "Courier New" or "")
ide.font.eItalic = setFont(wx.wxFONTSTYLE_ITALIC, ide.config.editor, wx.__WXMSW__ and "Courier New" or "")

ide.font.oNormal = setFont(wx.wxFONTSTYLE_NORMAL, ide.config.outputshell, wx.__WXMSW__ and "Courier New" or "")
ide.font.oItalic = setFont(wx.wxFONTSTYLE_ITALIC, ide.config.outputshell, wx.__WXMSW__ and "Courier New" or "")

-- treeCtrl font requires slightly different handling
local gui, config = wx.wxTreeCtrl():GetFont(), ide.config.filetree
if config.fontsize then gui:SetPointSize(config.fontsize) end
if config.fontname then gui:SetFaceName(config.fontname) end
ide.font.fNormal = gui

-- ----------------------------------------------------------------------------
-- Create the wxFrame
-- ----------------------------------------------------------------------------
local function createFrame()
  frame = wx.wxFrame(wx.NULL, wx.wxID_ANY, GetIDEString("editor"),
    wx.wxDefaultPosition, wx.wxSize(1000, 700))
  -- wrap into protected call as DragAcceptFiles fails on MacOS with
  -- wxwidgets 2.8.12 even though it should work according to change notes
  -- for 2.8.10: "Implemented wxWindow::DragAcceptFiles() on all platforms."
  pcall(function() frame:DragAcceptFiles(true) end)
  frame:Connect(wx.wxEVT_DROP_FILES,function(evt)
      local files = evt:GetFiles()
      if not files or #files == 0 then return end
      for i,f in ipairs(files) do
        LoadFile(f,nil,true)
      end
    end)

  local menuBar = wx.wxMenuBar()
  local statusBar = frame:CreateStatusBar(6)
  local section_width = statusBar:GetTextExtent("OVRW")
  statusBar:SetStatusStyles({wx.wxSB_RAISED, wx.wxSB_RAISED, wx.wxSB_RAISED,
    wx.wxSB_RAISED, wx.wxSB_RAISED, wx.wxSB_RAISED})
  statusBar:SetStatusWidths(
    {-1, section_width*6, section_width, section_width, section_width*4, section_width*4})
  statusBar:SetStatusText(GetIDEString("statuswelcome"))
  
  local mgr = wxaui.wxAuiManager()
  mgr:SetManagedWindow(frame)

  frame.menuBar = menuBar
  frame.statusBar = statusBar
  frame.uimgr = mgr

  return frame
end

local function SCinB(id) -- shortcut in brackets
  local shortcut = KSC(id):gsub("\t","")
  return shortcut and #shortcut > 0 and (" ("..shortcut..")") or ""
end

local function createToolBar(frame)
  local toolBar = wx.wxToolBar(frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxTB_FLAT + wx.wxTB_NODIVIDER)
  local funclist = wx.wxChoice.new(toolBar, ID "toolBar.funclist",
    -- Linux requires a bit larger size for the function list in the toolbar.
    -- Mac also requires a bit larger size, but setting it to 20 resets
    -- back to 16 when the toolbar is refreshed.
    -- Windows with wxwidgets 2.9.x also requires a larger size.
    wx.wxDefaultPosition, wx.wxSize.new(240,
      (ide.osname == "Unix" or ide.osname == "Windows") and 24 or 16))
  
  -- usually the bmp size isn't necessary, but the HELP icon is not the right size in MSW
  local getBitmap = (ide.app.createbitmap or wx.wxArtProvider.GetBitmap)
  local toolBmpSize = wx.wxSize(16, 16)
  toolBar:AddTool(ID_NEW, "New", getBitmap(wx.wxART_NORMAL_FILE, wx.wxART_TOOLBAR, toolBmpSize), TR("Create an empty document")..SCinB(ID_NEW))
  toolBar:AddTool(ID_OPEN, "Open", getBitmap(wx.wxART_FILE_OPEN, wx.wxART_TOOLBAR, toolBmpSize), TR("Open an existing document")..SCinB(ID_OPEN))
  toolBar:AddTool(ID_SAVE, "Save", getBitmap(wx.wxART_FILE_SAVE, wx.wxART_TOOLBAR, toolBmpSize), TR("Save the current document")..SCinB(ID_SAVE))
  toolBar:AddTool(ID_SAVEALL, "Save All", getBitmap(wx.wxART_NEW_DIR, wx.wxART_TOOLBAR, toolBmpSize), TR("Save all open documents")..SCinB(ID_SAVEALL))
  toolBar:AddSeparator()
  toolBar:AddTool(ID_CUT, "Cut", getBitmap(wx.wxART_CUT, wx.wxART_TOOLBAR, toolBmpSize), TR("Cut selected text to clipboard")..SCinB(ID_CUT))
  toolBar:AddTool(ID_COPY, "Copy", getBitmap(wx.wxART_COPY, wx.wxART_TOOLBAR, toolBmpSize), TR("Copy selected text to clipboard")..SCinB(ID_COPY))
  toolBar:AddTool(ID_PASTE, "Paste", getBitmap(wx.wxART_PASTE, wx.wxART_TOOLBAR, toolBmpSize), TR("Paste text from the clipboard")..SCinB(ID_PASTE))
  toolBar:AddSeparator()
  toolBar:AddTool(ID_UNDO, "Undo", getBitmap(wx.wxART_UNDO, wx.wxART_TOOLBAR, toolBmpSize), TR("Undo last edit")..SCinB(ID_UNDO))
  toolBar:AddTool(ID_REDO, "Redo", getBitmap(wx.wxART_REDO, wx.wxART_TOOLBAR, toolBmpSize), TR("Redo last edit undone")..SCinB(ID_REDO))
  toolBar:AddSeparator()
  toolBar:AddTool(ID_FIND, "Find", getBitmap(wx.wxART_FIND, wx.wxART_TOOLBAR, toolBmpSize), TR("Find text")..SCinB(ID_FIND))
  toolBar:AddTool(ID_REPLACE, "Replace", getBitmap(wx.wxART_FIND_AND_REPLACE, wx.wxART_TOOLBAR, toolBmpSize), TR("Find and replace text")..SCinB(ID_REPLACE))
  if ide.app.createbitmap then -- custom handler should handle all bitmaps
    toolBar:AddSeparator()
    toolBar:AddTool(ID_STARTDEBUG, "Start Debugging", getBitmap("wxART_DEBUG_START", wx.wxART_TOOLBAR, toolBmpSize), TR("Start debugging")..SCinB(ID_STARTDEBUG))
    toolBar:AddTool(ID_STOPDEBUG, "Stop Debugging", getBitmap("wxART_DEBUG_STOP", wx.wxART_TOOLBAR, toolBmpSize), TR("Stop the currently running process")..SCinB(ID_STOPDEBUG))
    toolBar:AddTool(ID_BREAK, "Break", getBitmap("wxART_DEBUG_BREAK", wx.wxART_TOOLBAR, toolBmpSize), TR("Break execution at the next executed line of code")..SCinB(ID_BREAK))
    toolBar:AddTool(ID_STEP, "Step into", getBitmap("wxART_DEBUG_STEP_INTO", wx.wxART_TOOLBAR, toolBmpSize), TR("Step into")..SCinB(ID_STEP))
    toolBar:AddTool(ID_STEPOVER, "Step over", getBitmap("wxART_DEBUG_STEP_OVER", wx.wxART_TOOLBAR, toolBmpSize), TR("Step over")..SCinB(ID_STEPOVER))
    toolBar:AddTool(ID_STEPOUT, "Step out", getBitmap("wxART_DEBUG_STEP_OUT", wx.wxART_TOOLBAR, toolBmpSize), TR("Step out of the current function")..SCinB(ID_STEPOUT))
    toolBar:AddSeparator()
    toolBar:AddTool(ID_TOGGLEBREAKPOINT, "Toggle breakpoint", getBitmap("wxART_DEBUG_BREAKPOINT_TOGGLE", wx.wxART_TOOLBAR, toolBmpSize), TR("Toggle breakpoint")..SCinB(ID_TOGGLEBREAKPOINT))
    toolBar:AddTool(ID_VIEWCALLSTACK, "Stack window", getBitmap("wxART_DEBUG_CALLSTACK", wx.wxART_TOOLBAR, toolBmpSize), TR("View the stack window")..SCinB(ID_VIEWCALLSTACK))
    toolBar:AddTool(ID_VIEWWATCHWINDOW, "Watch window", getBitmap("wxART_DEBUG_WATCH", wx.wxART_TOOLBAR, toolBmpSize), TR("View the watch window")..SCinB(ID_VIEWWATCHWINDOW))
  end
  toolBar:AddSeparator()
  toolBar:AddTool(ID_PROJECTDIRFROMFILE, "Update", getBitmap(wx.wxART_GO_DIR_UP , wx.wxART_TOOLBAR, toolBmpSize), TR("Set project directory from current file")..SCinB(ID_PROJECTDIRFROMFILE))
  toolBar:AddSeparator()
  toolBar:AddControl(funclist)
  toolBar:Realize()
  
  toolBar.funclist = funclist
  frame.toolBar = toolBar
  return toolBar
end

local function createNotebook(frame)
  -- notebook for editors
  local notebook = wxaui.wxAuiNotebook(frame, wx.wxID_ANY,
  wx.wxDefaultPosition, wx.wxDefaultSize,
  wxaui.wxAUI_NB_DEFAULT_STYLE + wxaui.wxAUI_NB_TAB_EXTERNAL_MOVE
  + wx.wxNO_BORDER)

  -- the following group of event handlers allows the active editor
  -- to get/keep focus after execution of Run and other commands
  local current -- the currently active editor, needed by the focus selection
  local function onPageChange(event)
    current = event:GetSelection() -- update the active editor reference
    SetEditorSelection(current)
    event:Skip() -- skip to let page change
  end
  notebook:Connect(wx.wxEVT_SET_FOCUS, -- Notepad tabs shouldn't be selectable,
    function (event)
      SetEditorSelection(current) -- select the currently active editor
    end)
  notebook:Connect(wx.wxEVT_COMMAND_NOTEBOOK_PAGE_CHANGED, onPageChange)
  notebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_PAGE_CHANGED, onPageChange)

  notebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_PAGE_CLOSE,
    function (event)
      ClosePage(event:GetSelection())
      event:Veto() -- don't propagate the event as the page is already closed
    end)

  local selection
  notebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_TAB_RIGHT_UP,
    function (event)
      selection = event:GetSelection() -- save tab index the event is for
      local menu = wx.wxMenu()
      menu:Append(ID_CLOSE, TR("&Close Page"))
      menu:Append(ID_CLOSEALL, TR("Close A&ll Pages"))
      menu:Append(ID_CLOSEOTHER, TR("Close &Other Pages"))
      menu:AppendSeparator()
      menu:Append(ID_SAVE, TR("&Save"))
      menu:Append(ID_SAVEAS, TR("Save &As..."))
      notebook:PopupMenu(menu)
    end)

  local function IfAtLeastOneTab(event) event:Enable(notebook:GetPageCount() > 0) end
  local function IfModified(event) event:Enable(EditorIsModified(GetEditor(selection))) end

  notebook:Connect(ID_SAVE, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
      local editor = GetEditor(selection)
      SaveFile(editor, openDocuments[editor:GetId()].filePath)
    end)
  notebook:Connect(ID_SAVE, wx.wxEVT_UPDATE_UI, IfModified)
  notebook:Connect(ID_SAVEAS, wx.wxEVT_COMMAND_MENU_SELECTED, function()
      SaveFileAs(GetEditor(selection))
    end)
  notebook:Connect(ID_SAVEAS, wx.wxEVT_UPDATE_UI, IfAtLeastOneTab)
  notebook:Connect(ID_CLOSE, wx.wxEVT_COMMAND_MENU_SELECTED, function()
      ClosePage(selection)
    end)
  notebook:Connect(ID_CLOSE, wx.wxEVT_UPDATE_UI, IfAtLeastOneTab)
  notebook:Connect(ID_CLOSEALL, wx.wxEVT_COMMAND_MENU_SELECTED, function()
      CloseAllPagesExcept(nil)
    end)
  notebook:Connect(ID_CLOSEALL, wx.wxEVT_UPDATE_UI, IfAtLeastOneTab)
  notebook:Connect(ID_CLOSEOTHER, wx.wxEVT_COMMAND_MENU_SELECTED, function ()
      CloseAllPagesExcept(selection)
    end)
  notebook:Connect(ID_CLOSEOTHER, wx.wxEVT_UPDATE_UI, function (event)
      event:Enable(notebook:GetPageCount() > 1)
    end)

  frame.notebook = notebook
  return notebook
end

local function createBottomNotebook(frame)
  -- bottomnotebook (errorlog,shellbox)
  local bottomnotebook = wxaui.wxAuiNotebook(frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wxaui.wxAUI_NB_DEFAULT_STYLE + wxaui.wxAUI_NB_TAB_EXTERNAL_MOVE
    - wxaui.wxAUI_NB_CLOSE_ON_ACTIVE_TAB + wx.wxNO_BORDER)

  local errorlog = wxstc.wxStyledTextCtrl(bottomnotebook, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxBORDER_STATIC)

  local shellbox = wxstc.wxStyledTextCtrl(bottomnotebook, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxBORDER_STATIC)

  bottomnotebook:AddPage(errorlog, TR("Output"), true)
  bottomnotebook:AddPage(shellbox, TR("Local console"), false)
  bottomnotebook:Connect(wxaui.wxEVT_COMMAND_AUINOTEBOOK_PAGE_CLOSE,
    function (event)
      event:Veto() -- don't allow closing pages in this notebook
    end)
  
  frame.bottomnotebook = bottomnotebook
  bottomnotebook.errorlog = errorlog
  bottomnotebook.shellbox = shellbox
  
  return bottomnotebook
end

local function createProjpanel(frame)
  local projpanel = wx.wxPanel(frame,wx.wxID_ANY)
  frame.projpanel = projpanel
  return projpanel
end

-- ----------------------------------------------------------------------------
-- Add the child windows to the frame

local frame = createFrame()
ide.frame = frame
createToolBar(frame)
createNotebook(frame)
createProjpanel(frame)
createBottomNotebook(frame)

do
  local frame = ide.frame
  local mgr = frame.uimgr

  mgr:AddPane(frame.toolBar, wxaui.wxAuiPaneInfo():
              Name("toolBar"):Caption("Main Toolbar"):
              MinSize(300,16):
              ToolbarPane():Top():CloseButton(false):
              LeftDockable(false):RightDockable(false):Hide())
              
  mgr:AddPane(frame.notebook, wxaui.wxAuiPaneInfo():
              Name("notebook"):
              CenterPane():PaneBorder(false):Hide())
              
  mgr:AddPane(frame.projpanel, wxaui.wxAuiPaneInfo():
              Name("projpanel"):Caption(TR("Project")):
              MinSize(200,200):FloatingSize(200,400):
              Left():Layer(1):Position(1):
              CloseButton(true):MaximizeButton(false):PinButton(true):Hide())
              
  mgr:AddPane(frame.bottomnotebook, wxaui.wxAuiPaneInfo():
              Name("bottomnotebook"):
              MinSize(200,200):FloatingSize(400,250):
              Bottom():Layer(1):Position(1):
              CloseButton(true):MaximizeButton(false):PinButton(true):Hide())
              
  mgr:GetPane("toolBar"):Show(true)
  mgr:GetPane("bottomnotebook"):Show(true)
  mgr:GetPane("projpanel"):Show(true)
  mgr:GetPane("notebook"):Show(true)
  
  local pp = mgr:SavePerspective()
  mgr.defaultPerspective = pp
  
  mgr:Update()
end
