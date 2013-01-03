-- Copy the content of this file to user.lua;
-- see the [configuration](http://studio.zerobrane.com/doc-configuration.html) page for details.

local G = ...
-- Alt-Shift-Cmd-X (Alt maps to Option, Ctrl maps to Command)
-- The mapping is largely based on [Xcode Keyboard Shortcuts](http://developer.apple.com/library/mac/#documentation/IDEs/Conceptual/xcode_help-command_shortcuts/MenuCommands/MenuCommands014.html).
local xcode = {
-- File menu
  [G.ID_NEW]              = "Ctrl-N",
  [G.ID_OPEN]             = "Ctrl-O",
  [G.ID_CLOSE]            = "Ctrl-W",
  [G.ID_SAVE]             = "Ctrl-S",
  [G.ID_SAVEAS]           = "Shift-Ctrl-S",
  [G.ID_SAVEALL]          = "Alt-Ctrl-S",
  [G.ID_RECENTFILES]      = "",
  [G.ID_EXIT]             = "Ctrl-Q",
-- Edit menu
  [G.ID_CUT]              = "Ctrl-X",
  [G.ID_COPY]             = "Ctrl-C",
  [G.ID_PASTE]            = "Ctrl-V",
  [G.ID_SELECTALL]        = "Ctrl-A",
  [G.ID_UNDO]             = "Ctrl-Z",
  [G.ID_REDO]             = "Shift-Ctrl-Z",
  [G.ID_SHOWTOOLTIP]      = "Ctrl-T",
  [G.ID_AUTOCOMPLETE]     = "Ctrl-K",
  [G.ID_AUTOCOMPLETEENABLE] = "",
  [G.ID_COMMENT]          = "Ctrl-U",
  [G.ID_FOLD]             = "F12",
  [G.ID_CLEARDYNAMICWORDS] = "",
-- Search menu
  [G.ID_FIND]             = "Ctrl-F",
  [G.ID_FINDNEXT]         = "Ctrl-G",
  [G.ID_FINDPREV]         = "Shift-Ctrl-G",
  [G.ID_REPLACE]          = "Alt-Ctrl-F",
  [G.ID_FINDINFILES]      = "Shift-Ctrl-F",
  [G.ID_REPLACEINFILES]   = "Alt-Shift-Ctrl-F",
  [G.ID_GOTOLINE]         = "",
  [G.ID_SORT]             = "",
-- View menu
  [G.ID_VIEWFILETREE]     = "Shift-Ctrl-P",
  [G.ID_VIEWOUTPUT]       = "Shift-Ctrl-O",
  [G.ID_VIEWWATCHWINDOW]  = "Shift-Ctrl-W",
  [G.ID_VIEWCALLSTACK]    = "Shift-Ctrl-S",
  [G.ID_VIEWDEFAULTLAYOUT] = "",
  [G.ID_VIEWFULLSCREEN]   = "Shift-Ctrl-A",
-- Project menu
  [G.ID_RUN]              = "Ctrl-R",
  [G.ID_RUNNOW]           = "Shift-Ctrl-R",
  [G.ID_COMPILE]          = "Ctrl-B",
  [G.ID_ANALYZE]          = "Shift-Ctrl-B",
  [G.ID_STARTDEBUG]       = "F5",
  [G.ID_ATTACHDEBUG]      = "",
  [G.ID_STOPDEBUG]        = "Ctrl-.",
  [G.ID_STEP]             = "F7",
  [G.ID_STEPOVER]         = "F6",
  [G.ID_STEPOUT]          = "F8",
  [G.ID_TRACE]            = "",
  [G.ID_BREAK]            = "Ctrl-Y",
  [G.ID_TOGGLEBREAKPOINT] = "Ctrl-\\",
  [G.ID_CLEAROUTPUT]      = "",
  [G.ID_INTERPRETER]      = "",
  [G.ID_PROJECTDIR]       = "",
-- Help menu
  [G.ID_ABOUT]            = "F1",
-- Watch window menu items
  [G.ID_ADDWATCH]         = "Ins",
  [G.ID_EDITWATCH]        = "F2",
  [G.ID_REMOVEWATCH]      = "Del",
  [G.ID_EVALUATEWATCH]    = "",
-- Editor popup menu items
  [G.ID_QUICKADDWATCH]    = "",
  [G.ID_QUICKEVAL]        = "",
}

for id, key in G.pairs(xcode) do keymap[id] = key end
