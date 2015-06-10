-- Copyright 2014-15 Paul Kulchenko, ZeroBrane LLC

local TR = function(...) return ... end

ide.config.toolbar.icons = {
  ID.NEW, ID.OPEN, ID.SAVE, ID.SAVEALL, ID.PROJECTDIRFROMFILE, ID.PROJECTDIRCHOOSE,
  ID.SEPARATOR,
  ID.FIND, ID.REPLACE, ID.FINDINFILES,
  ID.SEPARATOR,
  ID.RUN, ID.STARTDEBUG, ID.RUNNOW, ID.STOPDEBUG, ID.DETACHDEBUG, ID.BREAK,
    ID.STEP, ID.STEPOVER, ID.STEPOUT, ID.RUNTO,
  ID.SEPARATOR,
  ID.TOGGLEBREAKPOINT, ID.BOOKMARKTOGGLE, ID.VIEWCALLSTACK, ID.VIEWWATCHWINDOW,
  [ID.FINDINFILES] = false,
}

ide.config.toolbar.iconmap = {
  [ID.NEW] = {"FILE-NEW", TR("Create an empty document")},
  [ID.OPEN] = {"FILE-OPEN", TR("Open an existing document")},
  [ID.SAVE] = {"FILE-SAVE", TR("Save the current document")},
  [ID.SAVEALL] = {"FILE-SAVE-ALL", TR("Save all open documents")},
  [ID.PROJECTDIRFROMFILE]= {"DIR-SETUP-FILE", TR("Set project directory from current file")},
  [ID.PROJECTDIRCHOOSE] = {"DIR-SETUP", TR("Choose a project directory")},
  [ID.FIND] = {"FIND", TR("Find text")},
  [ID.REPLACE] = {"FIND-AND-REPLACE", TR("Find and replace text")},
  [ID.FINDINFILES] = {"FIND-IN-FILES", TR("Find in files")},
  [ID.RUN] = {"RUN", TR("Execute the current project/file")},
  [ID.RUNNOW] = {"RUN-NOW", TR("Run as Scratchpad")},
  [ID.STARTDEBUG] = {"DEBUG-START", TR("Start or continue debugging")},
  [ID.STOPDEBUG] = {"DEBUG-STOP", TR("Stop the currently running process")},
  [ID.DETACHDEBUG]= {"DEBUG-DETACH", TR("Stop debugging and continue running the process")},
  [ID.BREAK] = {"DEBUG-BREAK", TR("Break execution at the next executed line of code")},
  [ID.RUNTO] = {"DEBUG-RUN-TO", TR("Run to cursor")},
  [ID.STEP] = {"DEBUG-STEP-INTO", TR("Step into")},
  [ID.STEPOVER] = {"DEBUG-STEP-OVER", TR("Step over")},
  [ID.STEPOUT] = {"DEBUG-STEP-OUT", TR("Step out of the current function")},
  [ID.TOGGLEBREAKPOINT] = {"DEBUG-BREAKPOINT-TOGGLE", TR("Toggle breakpoint")},
  [ID.BOOKMARKTOGGLE] = {"BOOKMARK-TOGGLE", TR("Toggle bookmark")},
  [ID.VIEWCALLSTACK] = {"DEBUG-CALLSTACK", TR("View the stack window")},
  [ID.VIEWWATCHWINDOW] = {"DEBUG-WATCH", TR("View the watch window")},
  -- search toolbar
  [ID.FINDNEXT] = {"FIND", TR("Find")},
  [ID.FINDREPLACENEXT] = {"FIND-REPLACE-NEXT", TR("Replace next instance")},
  [ID.FINDREPLACEALL] = {"FIND-AND-REPLACE", TR("Replace all")},
  [ID.FINDSETDIR] = {"FIND-OPT-SETDIR", TR("Set search directory")},
  [ID.FINDOPTDIRECTION] = {"FIND-OPT-DOWN", TR("Search direction")},
  [ID.FINDOPTWRAPWROUND] = {"FIND-OPT-WRAP-AROUND", TR("Wrap around")},
  [ID.FINDOPTWORD] = {"FIND-OPT-WORD", TR("Match whole word")},
  [ID.FINDOPTCASE] = {"FIND-OPT-CASE-SENSITIVE", TR("Match case")},
  [ID.FINDOPTREGEX] = {"FIND-OPT-REGEX", TR("Regular expression")},
  [ID.FINDOPTCONTEXT] = {"FIND-OPT-CONTEXT", TR("Show context")},
  [ID.FINDOPTSUBDIR] = {"FIND-OPT-SUBDIR", TR("Search in subdirectories")},
  [ID.FINDOPTMULTIRESULTS] = {"FIND-OPT-MULTI-RESULTS", TR("Show multiple result windows")},
}
