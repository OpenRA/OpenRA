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
  [ID_RECENTFILESPREV]  = "Ctrl-<",
  [ID_RECENTFILESNEXT]  = "Ctrl->",
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
  [ID_FINDSELECTNEXT]   = "Ctrl-F3",
  [ID_FINDSELECTPREV]   = "Ctrl-Shift-F3",
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
  [ID_ZOOMRESET]        = "Ctrl-0",
  [ID_ZOOMIN]           = "Ctrl-+",
  [ID_ZOOMOUT]          = "Ctrl--",
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
  [ID_DELETEWATCH]      = "Del",
-- Editor popup menu items
  [ID_QUICKADDWATCH]    = "",
  [ID_QUICKEVAL]        = "",
-- Filetree popup menu items
  [ID_RENAMEFILE]       = "F2",
  [ID_DELETEFILE]       = "Del",
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
  -- Opt+Shift+Left/Right selects one word left (to the beginning)/right (to the end)
  {wxstc.wxSTC_KEY_LEFT, wxstc.wxSTC_SCMOD_ALT+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_WORDLEFTEXTEND, "Macintosh"},
  {wxstc.wxSTC_KEY_RIGHT, wxstc.wxSTC_SCMOD_ALT+wxstc.wxSTC_SCMOD_SHIFT, wxstc.wxSTC_CMD_WORDRIGHTENDEXTEND, "Macintosh"},
}
