-- Copyright 2014 Paul Kulchenko, ZeroBrane LLC

local TR = function(...) return ... end

ide.config.toolbar.icons = {
  ID_NEW, ID_OPEN, ID_SAVE, ID_SAVEALL, ID_PROJECTDIRFROMFILE, ID_PROJECTDIRCHOOSE,
  ID_SEPARATOR,
  ID_FIND, ID_REPLACE, ID_FINDINFILES,
  ID_SEPARATOR,
  ID_RUN, ID_STARTDEBUG, ID_RUNNOW, ID_STOPDEBUG, ID_DETACHDEBUG, ID_BREAK,
    ID_STEP, ID_STEPOVER, ID_STEPOUT, ID_RUNTO,
  ID_SEPARATOR,
  ID_TOGGLEBREAKPOINT, ID_BOOKMARKTOGGLE, ID_VIEWCALLSTACK, ID_VIEWWATCHWINDOW,
  [ID_FINDINFILES] = false,
}

ide.config.toolbar.iconmap = {
  [ID_NEW] = {"FILE-NEW", TR("Create an empty document")},
  [ID_OPEN] = {"FILE-OPEN", TR("Open an existing document")},
  [ID_SAVE] = {"FILE-SAVE", TR("Save the current document")},
  [ID_SAVEALL] = {"FILE-SAVE-ALL", TR("Save all open documents")},
  [ID_PROJECTDIRFROMFILE]= {"DIR-SETUP-FILE", TR("Set project directory from current file")},
  [ID_PROJECTDIRCHOOSE] = {"DIR-SETUP", TR("Choose a project directory")},
  [ID_FIND] = {"FIND", TR("Find text")},
  [ID_REPLACE] = {"FIND-AND-REPLACE", TR("Find and replace text")},
  [ID_FINDINFILES] = {"FIND-IN-FILES", TR("Find in files")},
  [ID_RUN] = {"RUN", TR("Execute the current project/file")},
  [ID_RUNNOW] = {"RUN-NOW", TR("Run as Scratchpad")},
  [ID_STARTDEBUG] = {"DEBUG-START", TR("Start or continue debugging")},
  [ID_STOPDEBUG] = {"DEBUG-STOP", TR("Stop the currently running process")},
  [ID_DETACHDEBUG]= {"DEBUG-DETACH", TR("Stop debugging and continue running the process")},
  [ID_BREAK] = {"DEBUG-BREAK", TR("Break execution at the next executed line of code")},
  [ID_RUNTO] = {"DEBUG-RUN-TO", TR("Run to cursor")},
  [ID_STEP] = {"DEBUG-STEP-INTO", TR("Step into")},
  [ID_STEPOVER] = {"DEBUG-STEP-OVER", TR("Step over")},
  [ID_STEPOUT] = {"DEBUG-STEP-OUT", TR("Step out of the current function")},
  [ID_TOGGLEBREAKPOINT] = {"DEBUG-BREAKPOINT-TOGGLE", TR("Toggle breakpoint")},
  [ID_BOOKMARKTOGGLE] = {"BOOKMARK-TOGGLE", TR("Toggle bookmark")},
  [ID_VIEWCALLSTACK] = {"DEBUG-CALLSTACK", TR("View the stack window")},
  [ID_VIEWWATCHWINDOW] = {"DEBUG-WATCH", TR("View the watch window")},
  -- search toolbar
  [ID_FINDNEXT] = {"FIND", TR("Find")},
  [ID_FINDREPLACENEXT] = {"FIND-REPLACE-NEXT", TR("Replace next instance")},
  [ID_FINDREPLACEALL] = {"FIND-AND-REPLACE", TR("Replace all")},
  [ID_FINDSETDIR] = {"FIND-OPT-SETDIR", TR("Set search directory")},
  [ID_FINDOPTDIRECTION] = {"FIND-OPT-DOWN", TR("Search direction")},
  [ID_FINDOPTWRAPWROUND] = {"FIND-OPT-WRAP-AROUND", TR("Wrap around")},
  [ID_FINDOPTWORD] = {"FIND-OPT-WORD", TR("Match whole word")},
  [ID_FINDOPTCASE] = {"FIND-OPT-CASE-SENSITIVE", TR("Match case")},
  [ID_FINDOPTREGEX] = {"FIND-OPT-REGEX", TR("Regular expression")},
  [ID_FINDOPTCONTEXT] = {"FIND-OPT-CONTEXT", TR("Show context")},
  [ID_FINDOPTSUBDIR] = {"FIND-OPT-SUBDIR", TR("Search in subdirectories")},
  [ID_FINDOPTMULTIRESULTS] = {"FIND-OPT-MULTI-RESULTS", TR("Show multiple result windows")},
}
