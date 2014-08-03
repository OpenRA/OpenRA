-- Copyright 2014 Paul Kulchenko, ZeroBrane LLC

ide.config.toolbar.icons = {
  ID_NEW, ID_OPEN, ID_SAVE, ID_SAVEALL, ID_PROJECTDIRFROMFILE, ID_PROJECTDIRCHOOSE,
  ID_SEPARATOR,
  ID_FIND, ID_REPLACE,
  ID_SEPARATOR,
  ID_STARTDEBUG, ID_STOPDEBUG, ID_DETACHDEBUG, ID_BREAK, ID_STEP, ID_STEPOVER, ID_STEPOUT,
  ID_SEPARATOR,
  ID_TOGGLEBREAKPOINT, ID_BOOKMARKTOGGLE, ID_VIEWCALLSTACK, ID_VIEWWATCHWINDOW,
  ID_SEPARATOR,
}

ide.config.toolbar.iconmap = {
  [ID_NEW] = {wx.wxART_NORMAL_FILE, "Create an empty document"},
  [ID_OPEN] = {wx.wxART_FILE_OPEN, "Open an existing document"},
  [ID_SAVE] = {wx.wxART_FILE_SAVE, "Save the current document"},
  [ID_SAVEALL] = {wx.wxART_NEW_DIR, "Save all open documents"},
  [ID_PROJECTDIRFROMFILE]= {wx.wxART_GO_DIR_UP, "Set project directory from current file"},
  [ID_PROJECTDIRCHOOSE] = {"wxART_DIR_SETUP", "Choose a project directory"},
  [ID_FIND] = {wx.wxART_FIND, "Find text"},
  [ID_REPLACE] = {wx.wxART_FIND_AND_REPLACE, "Find and replace text"},
  [ID_STARTDEBUG] = {"wxART_DEBUG_START", "Start or Continue debugging"},
  [ID_STOPDEBUG] = {"wxART_DEBUG_STOP", "Stop the currently running process"},
  [ID_DETACHDEBUG]= {"wxART_DEBUG_DETACH", "Stop debugging and continue running the process"},
  [ID_BREAK] = {"wxART_DEBUG_BREAK", "Break execution at the next executed line of code"},
  [ID_STEP] = {"wxART_DEBUG_STEP_INTO", "Step into"},
  [ID_STEPOVER] = {"wxART_DEBUG_STEP_OVER", "Step over"},
  [ID_STEPOUT] = {"wxART_DEBUG_STEP_OUT", "Step out of the current function"},
  [ID_TOGGLEBREAKPOINT] = {"wxART_DEBUG_BREAKPOINT_TOGGLE", "Toggle breakpoint"},
  [ID_BOOKMARKTOGGLE] = {"wxART_BOOKMARK_TOGGLE", "Toggle bookmark"},
  [ID_VIEWCALLSTACK] = {"wxART_DEBUG_CALLSTACK", "View the stack window"},
  [ID_VIEWWATCHWINDOW] = {"wxART_DEBUG_WATCH", "View the watch window"},
}
