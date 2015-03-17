-- Copy the content of this file to user.lua;
-- see the [configuration](http://studio.zerobrane.com/doc-configuration.html) page for details.

-- Alt-Shift-Cmd-X (Alt maps to Option, Ctrl maps to Command)
-- The mapping is largely based on [Xcode Keyboard Shortcuts](http://developer.apple.com/library/mac/#documentation/IDEs/Conceptual/xcode_help-command_shortcuts/MenuCommands/MenuCommands014.html).
local xcode = {
-- File menu
  [ID.NEW]              = "Ctrl-N",
  [ID.OPEN]             = "Ctrl-O",
  [ID.CLOSE]            = "Ctrl-W",
  [ID.SAVE]             = "Ctrl-S",
  [ID.SAVEAS]           = "Shift-Ctrl-S",
  [ID.SAVEALL]          = "Alt-Ctrl-S",
  [ID.RECENTFILES]      = "",
  [ID.EXIT]             = "Ctrl-Q",
-- Edit menu
  [ID.CUT]              = "Ctrl-X",
  [ID.COPY]             = "Ctrl-C",
  [ID.PASTE]            = "Ctrl-V",
  [ID.SELECTALL]        = "Ctrl-A",
  [ID.UNDO]             = "Ctrl-Z",
  [ID.REDO]             = "Shift-Ctrl-Z",
  [ID.SHOWTOOLTIP]      = "Ctrl-T",
  [ID.AUTOCOMPLETE]     = "Ctrl-K",
  [ID.AUTOCOMPLETEENABLE] = "",
  [ID.COMMENT]          = "Ctrl-U",
  [ID.FOLD]             = "F12",
  [ID.CLEARDYNAMICWORDS] = "",
-- Search menu
  [ID.FIND]             = "Ctrl-F",
  [ID.FINDNEXT]         = "Ctrl-G",
  [ID.FINDPREV]         = "Shift-Ctrl-G",
  [ID.REPLACE]          = "Alt-Ctrl-F",
  [ID.FINDINFILES]      = "Shift-Ctrl-F",
  [ID.REPLACEINFILES]   = "Alt-Shift-Ctrl-F",
  [ID.SORT]             = "",
-- View menu
  [ID.VIEWFILETREE]     = "Shift-Ctrl-P",
  [ID.VIEWOUTPUT]       = "Shift-Ctrl-O",
  [ID.VIEWWATCHWINDOW]  = "Shift-Ctrl-W",
  [ID.VIEWCALLSTACK]    = "Shift-Ctrl-S",
  [ID.VIEWDEFAULTLAYOUT] = "",
  [ID.VIEWFULLSCREEN]   = "Shift-Ctrl-A",
-- Project menu
  [ID.RUN]              = "Ctrl-R",
  [ID.RUNNOW]           = "Shift-Ctrl-R",
  [ID.COMPILE]          = "Ctrl-B",
  [ID.ANALYZE]          = "Shift-Ctrl-B",
  [ID.STARTDEBUG]       = "F5",
  [ID.ATTACHDEBUG]      = "",
  [ID.STOPDEBUG]        = "Ctrl-.",
  [ID.STEP]             = "F7",
  [ID.STEPOVER]         = "F6",
  [ID.STEPOUT]          = "F8",
  [ID.TRACE]            = "",
  [ID.BREAK]            = "Ctrl-Y",
  [ID.TOGGLEBREAKPOINT] = "Ctrl-\\",
  [ID.CLEAROUTPUT]      = "",
  [ID.INTERPRETER]      = "",
  [ID.PROJECTDIR]       = "",
-- Help menu
  [ID.ABOUT]            = "F1",
-- Watch window menu items
  [ID.ADDWATCH]         = "Ins",
  [ID.EDITWATCH]        = "F2",
  [ID.DELETEWATCH]      = "Del",
-- Editor popup menu items
  [ID.QUICKADDWATCH]    = "",
  [ID.QUICKEVAL]        = "",
}

local G = ...
for id, key in G.pairs(xcode) do keymap[id] = key end
