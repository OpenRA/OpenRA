-- Copyright 2014 Paul Kulchenko, ZeroBrane LLC

ide.config.toolbar.icons = {
  ID_NEW, ID_OPEN, ID_SAVE, ID_SAVEALL, ID_PROJECTDIRFROMFILE, ID_PROJECTDIRCHOOSE,
  ID_SEPARATOR,
  ID_FIND, ID_REPLACE, ID_FINDINFILES,
  ID_SEPARATOR,
  ID_RUN, ID_STARTDEBUG, ID_RUNNOW, ID_STOPDEBUG, ID_DETACHDEBUG, ID_BREAK, ID_STEP, ID_STEPOVER, ID_STEPOUT,
  ID_SEPARATOR,
  ID_TOGGLEBREAKPOINT, ID_BOOKMARKTOGGLE, ID_VIEWCALLSTACK, ID_VIEWWATCHWINDOW,
  [ID_FINDINFILES] = false,
}

ide.config.toolbar.iconmap = {
  [ID_NEW] = {"FILE-NEW", "Create an empty document"},
  [ID_OPEN] = {"FILE-OPEN", "Open an existing document"},
  [ID_SAVE] = {"FILE-SAVE", "Save the current document"},
  [ID_SAVEALL] = {"FILE-SAVE-ALL", "Save all open documents"},
  [ID_PROJECTDIRFROMFILE]= {"DIR-SETUP-FILE", "Set project directory from current file"},
  [ID_PROJECTDIRCHOOSE] = {"DIR-SETUP", "Choose a project directory"},
  [ID_FIND] = {"FIND", "Find text"},
  [ID_REPLACE] = {"FIND-AND-REPLACE", "Find and replace text"},
  [ID_FINDINFILES] = {"FIND-IN-FILES", "Find in files"},
  [ID_RUN] = {"RUN", "Run"},
  [ID_RUNNOW] = {"RUN-NOW", "Run as Scratchpad"},
  [ID_STARTDEBUG] = {"DEBUG-START", "Start or Continue debugging"},
  [ID_STOPDEBUG] = {"DEBUG-STOP", "Stop the currently running process"},
  [ID_DETACHDEBUG]= {"DEBUG-DETACH", "Stop debugging and continue running the process"},
  [ID_BREAK] = {"DEBUG-BREAK", "Break execution at the next executed line of code"},
  [ID_STEP] = {"DEBUG-STEP-INTO", "Step into"},
  [ID_STEPOVER] = {"DEBUG-STEP-OVER", "Step over"},
  [ID_STEPOUT] = {"DEBUG-STEP-OUT", "Step out of the current function"},
  [ID_TOGGLEBREAKPOINT] = {"DEBUG-BREAKPOINT-TOGGLE", "Toggle breakpoint"},
  [ID_BOOKMARKTOGGLE] = {"BOOKMARK-TOGGLE", "Toggle bookmark"},
  [ID_VIEWCALLSTACK] = {"DEBUG-CALLSTACK", "View the stack window"},
  [ID_VIEWWATCHWINDOW] = {"DEBUG-WATCH", "View the watch window"},
}
