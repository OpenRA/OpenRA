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
  [ID.NEW]              = "Ctrl-N",
  [ID.OPEN]             = "Ctrl-O",
  [ID.CLOSE]            = "Ctrl-W",
  [ID.SAVE]             = "Ctrl-S",
  [ID.SAVEAS]           = "Alt-Shift-S",
  [ID.SAVEALL]          = "",
  [ID.RECENTFILES]      = "",
  [ID.RECENTFILESPREV]  = "Ctrl-,",
  [ID.RECENTFILESNEXT]  = "Ctrl-.",
  [ID.EXIT]             = "Ctrl-Q",
  [ID.RECENTPROJECTSPREV] = "Ctrl-Shift-<",
-- Edit menu
  [ID.CUT]              = "Ctrl-X",
  [ID.COPY]             = "Ctrl-C",
  [ID.PASTE]            = "Ctrl-V",
  [ID.SELECTALL]        = "Ctrl-A",
  [ID.UNDO]             = "Ctrl-Z",
  [ID.REDO]             = "Ctrl-Y",
  [ID.SHOWTOOLTIP]      = "Ctrl-T",
  [ID.AUTOCOMPLETE]     = "Ctrl-K",
  [ID.AUTOCOMPLETEENABLE] = "",
  [ID.COMMENT]          = "Ctrl-U",
  [ID.FOLD]             = "F12",
  [ID.CLEARDYNAMICWORDS] = "",
  [ID.REINDENT]         = "Ctrl-I",
  [ID.BOOKMARKTOGGLE]   = "Ctrl-F2",
  [ID.BOOKMARKNEXT]     = "F2",
  [ID.BOOKMARKPREV]     = "Shift-F2",
  [ID.NAVIGATETOFILE]   = "Ctrl-P",
  [ID.NAVIGATETOLINE]   = "Ctrl-G",
  [ID.NAVIGATETOSYMBOL] = "Ctrl-B",
  [ID.NAVIGATETOMETHOD] = "Ctrl-;",
-- Search menu
  [ID.FIND]             = "Ctrl-F",
  [ID.FINDNEXT]         = "F3",
  [ID.FINDPREV]         = "Shift-F3",
  [ID.FINDSELECTNEXT]   = "Ctrl-F3",
  [ID.FINDSELECTPREV]   = "Ctrl-Shift-F3",
  [ID.REPLACE]          = "Ctrl-R",
  [ID.FINDINFILES]      = "Ctrl-Shift-F",
  [ID.REPLACEINFILES]   = "Ctrl-Shift-R",
  [ID.SORT]             = "",
-- View menu
  [ID.VIEWFILETREE]     = "Ctrl-Shift-P",
  [ID.VIEWOUTPUT]       = "Ctrl-Shift-O",
  [ID.VIEWWATCHWINDOW]  = "Ctrl-Shift-W",
  [ID.VIEWCALLSTACK]    = "Ctrl-Shift-S",
  [ID.VIEWDEFAULTLAYOUT] = "",
  [ID.VIEWFULLSCREEN]   = "Ctrl-Shift-A",
  [ID.ZOOMRESET]        = "Ctrl-0",
  [ID.ZOOMIN]           = "Ctrl-+",
  [ID.ZOOMOUT]          = "Ctrl--",
-- Project menu
  [ID.RUN]              = "F6",
  [ID.RUNNOW]           = "Ctrl-F6",
  [ID.COMPILE]          = "F7",
  [ID.ANALYZE]          = "Shift-F7",
  [ID.STARTDEBUG]       = "F5",
  [ID.ATTACHDEBUG]      = "",
  [ID.DETACHDEBUG]      = "",
  [ID.STOPDEBUG]        = "Shift-F5",
  [ID.STEP]             = "F10",
  [ID.STEPOVER]         = "Shift-F10",
  [ID.STEPOUT]          = "Ctrl-F10",
  [ID.RUNTO]            = "Ctrl-Shift-F10",
  [ID.TRACE]            = "",
  [ID.BREAK]            = "Shift-F9",
  [ID.TOGGLEBREAKPOINT] = "F9",
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
-- Filetree popup menu items
  [ID.RENAMEFILE]       = "F2",
  [ID.DELETEFILE]       = "Del",
}

function KSC(id, default)
  -- this is only for the rare case of someone assigning a complete list
  -- to ide.config.keymap.
  local keymap = ide.config.keymap
  return (keymap[id] and "\t"..keymap[id]) or (default and "\t"..default) or ""
end

ide.config.editor.keymap = {
  -- key, modifier, command, os: http://www.scintilla.org/ScintillaDoc.html#KeyboardCommands
  -- Cmd+Left/Right moves to start/end of line
  {wxstc.wxSTC_KEY_LEFT, wxstc.wxSTC_SCMOD_CTRL, wxstc.wxSTC_CMD_HOME, "Macintosh"},
  {wxstc.wxSTC_KEY_RIGHT, wxstc.wxSTC_SCMOD_CTRL, wxstc.wxSTC_CMD_LINEEND, "Macintosh"},
  -- Cmd+Shift+Left/Right selects to the beginning/end of the line
  {wxstc.wxSTC_KEY_LEFT, wxstc.wxSTC_SCMOD_CTRL+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_HOMEEXTEND, "Macintosh"},
  {wxstc.wxSTC_KEY_RIGHT, wxstc.wxSTC_SCMOD_CTRL+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_LINEENDEXTEND, "Macintosh"},
  -- Cmd+Shift+Up/Down selects to the beginning/end of the text
  {wxstc.wxSTC_KEY_UP, wxstc.wxSTC_SCMOD_CTRL+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_LINEUPEXTEND, "Macintosh"},
  {wxstc.wxSTC_KEY_DOWN, wxstc.wxSTC_SCMOD_CTRL+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_LINEDOWNEXTEND, "Macintosh"},
  -- Opt+Left/Right moves one word left (to the beginning)/right (to the end)
  {wxstc.wxSTC_KEY_LEFT, wxstc.wxSTC_SCMOD_ALT, wxstc.wxSTC_CMD_WORDLEFT, "Macintosh"},
  {wxstc.wxSTC_KEY_RIGHT, wxstc.wxSTC_SCMOD_ALT, wxstc.wxSTC_CMD_WORDRIGHTEND, "Macintosh"},
}
