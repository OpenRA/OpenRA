local ide = ide

--[[
Accelerator general syntax is any combination of "CTRL", "ALT" and "SHIFT"
strings (case doesn't matter) separated by either '-' or '+' characters and
followed by the accelerator itself. The accelerator may be any alphanumeric
character, any function key (from F1 to F12) or one of the special characters
listed below (again, case doesn't matter):

  DEL/DELETE   Delete key
  INS/INSERT   Insert key
  ENTER/RETURN Enter key
  PGUP         PageUp key
  PGDN         PageDown key
  LEFT         Left cursor arrow key
  RIGHT        Right cursor arrow key
  UP           Up cursor arrow key
  DOWN         Down cursor arrow key
  HOME         Home key
  END          End key
  SPACE        Space
  TAB          Tab key
  ESC/ESCAPE   Escape key (Windows only)
--]]

ide.config.keymap = {
-- File menu
  [ID_NEW]              = "Ctrl-N",
  [ID_OPEN]             = "Ctrl-O",
  [ID_CLOSE]            = "Ctrl-W",
  [ID_SAVE]             = "Ctrl-S",
  [ID_SAVEAS]           = "Alt-Shift-S",
  [ID_SAVEALL]          = "",
  [ID_RECENTFILES]      = "",
  [ID_EXIT]             = "Ctrl-Q",
-- Edit menu
  [ID_CUT]              = "Ctrl-X",
  [ID_COPY]             = "Ctrl-C",
  [ID_PASTE]            = "Ctrl-V",
  [ID_SELECTALL]        = "Ctrl-A",
  [ID_UNDO]             = "Ctrl-Z",
  [ID_REDO]             = "Ctrl-Y",
  [ID_SHOWTOOLTIP]      = "Ctrl-T",
  [ID_AUTOCOMPLETE]     = "Ctrl-K",
  [ID_AUTOCOMPLETEENABLE] = "",
  [ID_COMMENT]          = "Ctrl-U",
  [ID_FOLD]             = "F12",
  [ID_CLEARDYNAMICWORDS] = "",
-- Search menu
  [ID_FIND]             = "Ctrl-F",
  [ID_FINDNEXT]         = "F3",
  [ID_FINDPREV]         = "Shift-F3",
  [ID_REPLACE]          = "Ctrl-R",
  [ID_FINDINFILES]      = "Ctrl-Shift-F",
  [ID_REPLACEINFILES]   = "Ctrl-Shift-R",
  [ID_GOTOLINE]         = "Ctrl-G",
  [ID_SORT]             = "",
-- View menu
  [ID_VIEWFILETREE]     = "Ctrl-Shift-P",
  [ID_VIEWOUTPUT]       = "Ctrl-Shift-O",
  [ID_VIEWWATCHWINDOW]  = "Ctrl-Shift-W",
  [ID_VIEWCALLSTACK]    = "Ctrl-Shift-S",
  [ID_VIEWDEFAULTLAYOUT] = "",
  [ID_VIEWFULLSCREEN]   = "Ctrl-Shift-A",
-- Project menu
  [ID_RUN]              = "F6",
  [ID_RUNNOW]           = "Ctrl-F6",
  [ID_COMPILE]          = "F7",
  [ID_ANALYZE]          = "Shift-F7",
  [ID_STARTDEBUG]       = "F5",
  [ID_ATTACHDEBUG]      = "",
  [ID_STOPDEBUG]        = "Shift-F5",
  [ID_STEP]             = "F10",
  [ID_STEPOVER]         = "Shift-F10",
  [ID_STEPOUT]          = "Ctrl-F10",
  [ID_TRACE]            = "",
  [ID_BREAK]            = "Shift-F9",
  [ID_TOGGLEBREAKPOINT] = "F9",
  [ID_CLEAROUTPUT]      = "",
  [ID_INTERPRETER]      = "",
  [ID_PROJECTDIR]       = "",
-- Help menu
  [ID_ABOUT]            = "F1",
-- Watch window menu items
  [ID_ADDWATCH]         = "Ins",
  [ID_EDITWATCH]        = "F2",
  [ID_REMOVEWATCH]      = "Del",
  [ID_EVALUATEWATCH]    = "",
-- Editor popup menu items
  [ID_QUICKADDWATCH]    = "",
  [ID_QUICKEVAL]        = "",
}

function KSC(id, default)
  -- this is only for the rare case of someone assigning a complete list
  -- to ide.config.keymap.
  local keymap = ide.config.keymap
  return keymap[id] and "\t"..keymap[id] or default or ""
end
