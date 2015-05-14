-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- Generate a unique new wxWindowID
local ID_IDCOUNTER = wx.wxID_HIGHEST + 1
function NewID()
  ID_IDCOUNTER = ID_IDCOUNTER + 1
  return ID_IDCOUNTER
end

-- some Ubuntu versions (Ubuntu 13.10) ignore labels on stock menu IDs,
-- so don't use stock IDs on Linux
local linux = ide.osname == 'Unix'

ID_SEPARATOR        = NewID()
-- File menu
ID_NEW              = linux and NewID() or wx.wxID_NEW
ID_OPEN             = linux and NewID() or wx.wxID_OPEN
ID_CLOSE            = NewID()
ID_CLOSEALL         = NewID()
ID_CLOSEOTHER       = NewID()
ID_NEWFILE          = NewID()
ID_NEWDIRECTORY     = NewID()
ID_RENAMEFILE       = NewID()
ID_DELETEFILE       = NewID()
ID_HIDEEXTENSION    = NewID()
ID_SETSTARTFILE     = NewID()
ID_UNSETSTARTFILE   = NewID()
ID_SHOWEXTENSION    = NewID()
ID_SHOWEXTENSIONALL = NewID()
ID_MAPDIRECTORY     = NewID()
ID_UNMAPDIRECTORY   = NewID()
ID_OPENEXTENSION    = NewID()
ID_COPYFULLPATH     = NewID()
ID_SHOWLOCATION     = NewID()
ID_REFRESH          = NewID()
ID_SAVE             = linux and NewID() or wx.wxID_SAVE
ID_SAVEAS           = linux and NewID() or wx.wxID_SAVEAS
ID_SAVEALL          = NewID()
ID_RECENTFILES      = NewID()
ID_RECENTFILESCLEAR = NewID()
ID_RECENTFILESPREV  = NewID()
ID_RECENTFILESNEXT  = NewID()
ID_RECENTPROJECTS   = NewID()
ID_RECENTPROJECTSCLEAR = NewID()
ID_RECENTPROJECTSPREV = NewID()
ID_EXIT             = linux and NewID() or wx.wxID_EXIT
-- Edit menu
ID_CUT              = linux and NewID() or wx.wxID_CUT
ID_COPY             = linux and NewID() or wx.wxID_COPY
ID_PASTE            = linux and NewID() or wx.wxID_PASTE
ID_SELECTALL        = linux and NewID() or wx.wxID_SELECTALL
ID_UNDO             = linux and NewID() or wx.wxID_UNDO
ID_REDO             = linux and NewID() or wx.wxID_REDO
ID_SHOWTOOLTIP      = NewID()
ID_AUTOCOMPLETE     = NewID()
ID_AUTOCOMPLETEENABLE = NewID()
ID_COMMENT          = NewID()
ID_FOLD             = NewID()
ID_CLEARDYNAMICWORDS = NewID()
ID_SOURCE           = NewID()
ID_REINDENT         = NewID()
ID_BOOKMARK         = NewID()
ID_BOOKMARKTOGGLE   = NewID()
ID_BOOKMARKNEXT     = NewID()
ID_BOOKMARKPREV     = NewID()
ID_NAVIGATE         = NewID()
ID_NAVIGATETOFILE   = NewID()
ID_NAVIGATETOLINE   = NewID()
ID_NAVIGATETOSYMBOL = NewID()
ID_NAVIGATETOMETHOD = NewID()
-- don't use wx.wxID_PREFERENCES to avoid merging with OSX app menu, because
-- Apple guidelines describe Preferences as a "normal" item without submenus.
ID_PREFERENCES      = NewID()
ID_PREFERENCESSYSTEM = NewID()
ID_PREFERENCESUSER  = NewID()
-- Search menu
ID_FIND             = linux and NewID() or wx.wxID_FIND
ID_FINDNEXT         = NewID()
ID_FINDPREV         = NewID()
ID_FINDSELECTNEXT   = NewID()
ID_FINDSELECTPREV   = NewID()
ID_REPLACE          = NewID()
ID_FINDINFILES      = NewID()
ID_REPLACEINFILES   = NewID()
ID_SORT             = NewID()
-- View menu
ID_VIEWFILETREE     = NewID()
ID_VIEWOUTPUT       = NewID()
ID_VIEWCALLSTACK    = NewID()
ID_VIEWWATCHWINDOW  = NewID()
ID_VIEWOUTLINE      = NewID()
ID_VIEWTOOLBAR      = NewID()
ID_VIEWSTATUSBAR    = NewID()
ID_VIEWDEFAULTLAYOUT = NewID()
ID_VIEWFULLSCREEN   = NewID()
ID_VIEWMINIMIZE     = NewID()
ID_ZOOM             = NewID()
ID_ZOOMRESET        = NewID()
ID_ZOOMIN           = NewID()
ID_ZOOMOUT          = NewID()
-- Project menu
ID_TOGGLEBREAKPOINT = NewID()
ID_COMPILE          = NewID()
ID_ANALYZE          = NewID()
ID_RUN              = NewID()
ID_RUNNOW           = NewID()
ID_ATTACHDEBUG      = NewID()
ID_STARTDEBUG       = NewID()
ID_STOPDEBUG        = NewID()
ID_DETACHDEBUG      = NewID()
ID_STEP             = NewID()
ID_STEPOVER         = NewID()
ID_STEPOUT          = NewID()
ID_RUNTO            = NewID()
ID_BREAK            = NewID()
ID_TRACE            = NewID()
ID_CLEAROUTPUT      = NewID()
ID_COMMANDLINEPARAMETERS = NewID()
ID_INTERPRETER      = NewID()
ID_PROJECTDIR       = NewID()
ID_PROJECTDIRFROMFILE = NewID()
ID_PROJECTDIRCHOOSE = NewID()
-- Help menu
ID_ABOUT            = linux and NewID() or wx.wxID_ABOUT
ID_HELPPROJECT      = NewID()
ID_HELPDOCUMENTATION = NewID()
ID_HELPGETTINGSTARTED = NewID()
ID_HELPTUTORIALS    = NewID()
ID_HELPFAQ          = NewID()
ID_HELPCOMMUNITY    = NewID()
-- Watch window menu items
ID_ADDWATCH         = NewID()
ID_EDITWATCH        = NewID()
ID_DELETEWATCH      = NewID()
ID_COPYWATCHVALUE   = NewID()
-- Editor popup menu items
ID_GOTODEFINITION   = NewID()
ID_RENAMEALLINSTANCES = NewID()
ID_REPLACEALLSELECTIONS = NewID()
ID_QUICKADDWATCH    = NewID()
ID_QUICKEVAL        = NewID()
ID_ADDTOSCRATCHPAD  = NewID()
-- search toolbar
ID_FINDALL          = NewID()
ID_FINDREPLACENEXT  = NewID()
ID_FINDREPLACEALL   = NewID()
ID_FINDSETDIR       = NewID()
ID_FINDSETTOPROJDIR = NewID()
ID_FINDOPTSCOPE     = NewID()
ID_FINDOPTSTATUS    = NewID()
ID_FINDOPTDIRECTION = NewID()
ID_FINDOPTWRAPWROUND = NewID()
ID_FINDOPTWORD      = NewID()
ID_FINDOPTCASE      = NewID()
ID_FINDOPTREGEX     = NewID()
ID_FINDOPTCONTEXT   = NewID()
ID_FINDOPTSUBDIR    = NewID()
ID_FINDOPTMULTIRESULTS = NewID()
ID_RECENTSCOPECLEAR = NewID()

local ids = {}
function IDgen (name)
  ids[name] = ids[name] or NewID()
  return ids[name]
end
function IDget (name) return ids[name] end

ID = setmetatable({}, ide.proto.ID)
