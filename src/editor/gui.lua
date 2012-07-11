-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
-- Lomtik Software (J. Winwood & John Labenski)
---------------------------------------------------------
local ide = ide

-- Global variables
-- Markers for editor marker margin
BREAKPOINT_MARKER = 1
BREAKPOINT_MARKER_VALUE = 2 -- = 2^BREAKPOINT_MARKER
CURRENT_LINE_MARKER = 2
CURRENT_LINE_MARKER_VALUE = 4 -- = 2^CURRENT_LINE_MARKER

-- Globals
local font = nil -- fonts to use for the editor
local fontItalic = nil
local ofont = nil -- fonts to use for the outputshell
local ofontItalic = nil
-- ----------------------------------------------------------------------------
-- Pick some reasonable fixed width fonts to use for the editor
if wx.__WXMSW__ then
  font = wx.wxFont(ide.config.editor.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_NORMAL,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.editor.fontname or "Courier New", ide.config.editor.fontencoding or wx.wxFONTENCODING_DEFAULT)
  fontItalic = wx.wxFont(ide.config.editor.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_ITALIC,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.editor.fontname or "Courier New", ide.config.editor.fontencoding or wx.wxFONTENCODING_DEFAULT)
else
  font = wx.wxFont(ide.config.editor.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_NORMAL,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.editor.fontname or "", ide.config.editor.fontencoding or wx.wxFONTENCODING_DEFAULT)
  fontItalic = wx.wxFont(ide.config.editor.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_ITALIC,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.editor.fontname or "", ide.config.editor.fontencoding or wx.wxFONTENCODING_DEFAULT)
end

if wx.__WXMSW__ then
  ofont = wx.wxFont(ide.config.outputshell.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_NORMAL,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.outputshell.fontname or "Courier New", ide.config.outputshell.fontencoding or wx.wxFONTENCODING_DEFAULT)
  ofontItalic = wx.wxFont(ide.config.outputshell.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_ITALIC,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.outputshell.fontname or "Courier New", ide.config.outputshell.fontencoding or wx.wxFONTENCODING_DEFAULT)
else
  ofont = wx.wxFont(ide.config.outputshell.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_NORMAL,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.outputshell.fontname or "", ide.config.outputshell.fontencoding or wx.wxFONTENCODING_DEFAULT)
  ofontItalic = wx.wxFont(ide.config.outputshell.fontsize or 10, wx.wxFONTFAMILY_MODERN, wx.wxFONTSTYLE_ITALIC,
    wx.wxFONTWEIGHT_NORMAL, false, ide.config.outputshell.fontname or "", ide.config.outputshell.fontencoding or wx.wxFONTENCODING_DEFAULT)
end

ide.font = font
ide.fontItalic = fontItalic
ide.ofont = ofont
ide.ofontItalic = ofontItalic

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
  frame.menuBar = menuBar
  
  local statusBar = frame:CreateStatusBar( 5 )
  frame.statusBar = statusBar
  local status_txt_width = statusBar:GetTextExtent("OVRW")
  statusBar:SetStatusWidths({-1, status_txt_width*6, status_txt_width, status_txt_width, status_txt_width*5})
  statusBar:SetStatusText(GetIDEString("statuswelcome"))
  
  local mgr = wxaui.wxAuiManager()
  frame.uimgr = mgr
  mgr:SetManagedWindow(frame)

  return frame
end

local function createToolBar(frame)
  local toolBar = wx.wxToolBar(frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxTB_FLAT + wx.wxTB_NODIVIDER)
  local funclist = wx.wxChoice.new(toolBar,ID "toolBar.funclist",wx.wxDefaultPosition, wx.wxSize.new(240,16))
  
  -- note: Ususally the bmp size isn't necessary, but the HELP icon is not the right size in MSW
  local getBitmap = (ide.app.createbitmap or wx.wxArtProvider.GetBitmap)
  local toolBmpSize = toolBar:GetToolBitmapSize()
  toolBar:AddTool(ID_NEW, "New", getBitmap(wx.wxART_NORMAL_FILE, wx.wxART_TOOLBAR, toolBmpSize), "Create an empty document")
  toolBar:AddTool(ID_OPEN, "Open", getBitmap(wx.wxART_FILE_OPEN, wx.wxART_TOOLBAR, toolBmpSize), "Open an existing document")
  toolBar:AddTool(ID_SAVE, "Save", getBitmap(wx.wxART_FILE_SAVE, wx.wxART_TOOLBAR, toolBmpSize), "Save the current document")
  toolBar:AddTool(ID_SAVEALL, "Save All", getBitmap(wx.wxART_NEW_DIR, wx.wxART_TOOLBAR, toolBmpSize), "Save all documents")
  toolBar:AddSeparator()
  toolBar:AddTool(ID_CUT, "Cut", getBitmap(wx.wxART_CUT, wx.wxART_TOOLBAR, toolBmpSize), "Cut the selection")
  toolBar:AddTool(ID_COPY, "Copy", getBitmap(wx.wxART_COPY, wx.wxART_TOOLBAR, toolBmpSize), "Copy the selection")
  toolBar:AddTool(ID_PASTE, "Paste", getBitmap(wx.wxART_PASTE, wx.wxART_TOOLBAR, toolBmpSize), "Paste text from the clipboard")
  toolBar:AddSeparator()
  toolBar:AddTool(ID_UNDO, "Undo", getBitmap(wx.wxART_UNDO, wx.wxART_TOOLBAR, toolBmpSize), "Undo last edit")
  toolBar:AddTool(ID_REDO, "Redo", getBitmap(wx.wxART_REDO, wx.wxART_TOOLBAR, toolBmpSize), "Redo last undo")
  toolBar:AddSeparator()
  toolBar:AddTool(ID_FIND, "Find", getBitmap(wx.wxART_FIND, wx.wxART_TOOLBAR, toolBmpSize), "Find text")
  toolBar:AddTool(ID_REPLACE, "Replace", getBitmap(wx.wxART_FIND_AND_REPLACE, wx.wxART_TOOLBAR, toolBmpSize), "Find and replace text")
  toolBar:AddSeparator()
  toolBar:AddTool(ID "debug.projectdir.fromfile", "Update", getBitmap(wx.wxART_GO_DIR_UP , wx.wxART_TOOLBAR, toolBmpSize), "Sets projectdir from file")
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

  frame.notebook = notebook
  return notebook
end

local function createBottomNotebook(frame)
  -- bottomnotebook (errorlog,shellbox)
  local bottomnotebook = wxaui.wxAuiNotebook(frame, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxDefaultSize,
    wxaui.wxAUI_NB_DEFAULT_STYLE + wxaui.wxAUI_NB_TAB_EXTERNAL_MOVE
    - wxaui.wxAUI_NB_CLOSE_ON_ACTIVE_TAB + wx.wxNO_BORDER)

  local errorlog = wxstc.wxStyledTextCtrl(bottomnotebook, ID "output",
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxBORDER_STATIC)

  local shellbox = wxstc.wxStyledTextCtrl(bottomnotebook, ID "shell",
    wx.wxDefaultPosition, wx.wxDefaultSize, wx.wxBORDER_STATIC)

  bottomnotebook:AddPage(errorlog, "Output", true)
  bottomnotebook:AddPage(shellbox, "Local console", false)
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
              Name("projpanel"):Caption("Project"):
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
