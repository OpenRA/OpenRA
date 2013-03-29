# ZeroBrane Studio Changelog

## Current master (Mar 29 2013)

### Highlights
  - Added **Linux binaries with support for 32bit and 64bit systems**.
  - Added **multiple selection and multi-cursor editing**.
  - Enabled **full debugging for Corona on OSX**.
  - Made Stack and Watch windows dockable.

### Special thanks
  - To toiffel for build improvements and continuous work on wxwidgets 2.9 and Linux support.
  - To Marcel van Herk for testing and feedback on Stack and Watch windows behavior.

### Improvements
  - Added auto-show/hide Stack and Watch windows during debugging (closes #110).
  - Added ignoring `-psn...` parameter on OSX when reading file names from command line.
  - Added migration of configuration file on Windows (helps #89).
  - Added check for different spellings of the same folder in the project tree.
  - Added scripts to install build prerequisites on Linux (helps #89).
  - Added linux binaries with support for x86 and x64 (helps #89).
  - Added window list button to the notepad with editor tabs.
  - Added centering of current line during debugging.
  - Added multiple selection and multi-cursor editing (wx2.9.5+).
  - Added dll proxy to make LfW libraries to work with the IDE.
  - Enabled full debugging for Corona on OSX.
  - Improved performance of tab switching and project tree population.
  - Improved handling of upvalues with __tostring method in the Stack window.
  - Increased default font size for OSX; set 'Monaco' as default font (helps #89).
  - Made stack and watch windows dockable (closes #103).
  - Optimized project switching and added notebook freezing where possible (ref #89).
  - Reduced flicker in the project tree when a file is opened (ref #89).
  - Removed binary libraries not currently used.
  - Set 'Courier New' as the default font on Linux (ref #89).
  - Switched to 'native' menu on OSX and added 24x24 icons required (helps #89).
  - Upgraded Mobdebug (0.5222) to add serialization with metamethods and notification on incomplete output (closes #109).
  - Updated error messages from loading configuration files.
  - Updated Linux binaries to use libpng 1.6 with wxwidgets (helps #89).
  - Updated Windows/OSX build files to only build components needed (helps #89).
  - Updated windows executable to show properly scaled icons in the Explorer.
  - Updated status bar to use no border around fields.
  - Updated large icons for "native" toolbar on OSX (helps #89).
  - Updated function call indicator to use round box with wxwidgets upgrade (helps #89).
  - Updated handling of markdown styles to make it more robust (fixes #59).
  - Updated README with Marmalade Quick support and Corona tutorial.

### Incompatibilities
  - Configuration file (.ini) location has changed on Windows. The current file will be copied to the new location.

### Fixes
  - Fixed an issue with Enter used to select an item in project dropdown (ref #89).
  - Fixed an issue with the Project tree when project and app directories are the same.
  - Fixed debugger output not being suppressed on Linux and using wlua.
  - Fixed a static analyzer issue with anonymous functions defined in expressions (fixes #3).

## v0.35 (Feb 10 2013)

### Highlights
  - Added support for **debugging Lua 5.2 scripts**.
  - Added support for **cross-platform remote debugging**.
  - Added support for starting Gideros **on-device debugging**.
  - Added support for live coding, script reloading, and coroutine debugging under LuaJIT.
  - Added **Marmalade Quick** support.
  - Added **live-coding for Corona** interpreter.
  - Added editor tab popup menu with 'Close All', 'Close Other', and other items.

### Special thanks
  - To Roland Yonaba and Fringale for updates to French translation.
  - To Fringale for enhanced line-endings handling.
  - To toiffel for build improvements, continuous work on wxwidgets 2.9 support, and several patches.
  - To Andy Bower for the stream of feedback on live coding and on-device debugging.

### Improvements
  - Added `debugger.runonstart` option to configure if debugging should run immediately after starting.
  - Added `editor.checkeol` option to configure checking for eol encoding in loaded files.
  - Added Marmalade Quick support.
  - Added support for starting Gideros on-device debugging.
  - Added requesting user attention when stopped during debugging.
  - Added example for `defaulteol` configuration option.
  - Added configuration option for compact folding (`editor.`).
  - Added support for cross-platform remote debugging.
  - Added support for script reloading and coroutine debugging under LuaJIT (MobDebug 0.514).
  - Added Russian translation for new messages.
  - Added handling of malformed UTF8 characters in files (fixes #97).
  - Added support for debugging Lua 5.2 scripts.
  - Added workaround for Moai require path issue (fixes #96; fixes #87).
  - Added editor tab popup menu with 'Close All', 'Close Other', and other items.
  - Added C/CPP specification file and a configuration example.
  - Added XCode-like key mapping.
  - Added removing of file: prefix and url decoding of filepaths (helps #89).
  - Added a workaround for returning incorrect project folder (helps #89).
  - Adjusted handling of filepath to work with relative and absolute paths (helps #89).
  - Enabled live-coding for Corona interpreter.
  - Improved line-endings handling (Fringale).
  - Improved reporting of stack and serialization run-time errors.
  - Updated Gideros interpreter to run debugging immediately after starting; Use `debugger.runonstart = false` to disable.
  - Updated Linux startup script to use online versions of wxlua packages (deb files).
  - Updated handling of file: prefix as it's different on Windows and OSX (helps #89).
  - Updated messages in the starter script.
  - Updated function call parsing to recognize "func{}" and "func''" syntax.
  - Updated os.exit() call in local console to exit gracefully.
  - Updated French translation with various fixes and tweaks (Fringale).
  - Updated French translation with newly added strings (Fringale).
  - Updated 'Close page' and its translations.
  - Updated configuration examples and documentation (helps #55).
  - Updated the size of the function list dropdown on Windows (helps #89).
  - Typos fixes, small changes in French translation (Roland Yonaba).

### Incompatibilities
  - Gideros debugging is changed to start immediately; use `debugger.runonstart = false` to disable.
  - Removed mosync support from the debugger into a separate module.
  - Added mixed end-of-line reporting; use `editor.checkeol = false` to disable.
  - Added reporting of malformed UTF-8.

### Fixes
  - Fixed a rare crash on OSX when closing IDE with an application running under debugger.
  - Fixed path handling for metalua to avoid conflicts with other modules that may include lexer.lua file.
  - Fixed executing commands in the Remote console immediately after `mobdebug.start()` (closes #100).
  - Fixed invalid escape sequence that fails under LuaJIT.
  - Fixed an issue with spec files when a separator (sep) is not specified.

## v0.34 (Dec 12 2012)

### Highlights
  - Added **internationalization** support (with Russian, Italian, Spanish, and French translations).
  - Added **key binding** configuration for menu and toolbar items.
  - Added **Corona SDK integration** with debugging and auto-complete.
  - Added **Moai scratchpad support**.
  - Added **color scheme support** with several schemes included.
  - Added **GSL-shell support**.
  - Added ability to pause and resume scratchpad.

### Special thanks
  - To Srdjan Markovic for Corona auto-complete support and Zenburn color scheme.
  - To Roland Yonaba for French translation.
  - To Leo Bartoloni for Italian translation.
  - To Inigo Sola for Spanish translation.
  - To toiffel for Russian translation and several patches.
  - To Fringale for spec and API files cleanup and update for Lua 5.2.
  - To Marcel van Herk for assistance with scratchpad break/resume functionality.

### Improvements
  - Added an option to specify the width of the calltip.
  - Added showing short descriptions in tooltip for functions without lib/package.
  - Added 'Project Directory' menu.
  - Added ability to pause scratchpad when the script is completed (closes #83).
  - Added ability to pause and resume scratchpad.
  - Added GSL-shell support (closes #72).
  - Added more color schemes (Solarized and Monokai).
  - Added customized yield function for debugging (MobDebug v0.508; closes #81).
  - Added proper hiding of current line marker during debugging (except when tracing).
  - Added Corona auto-complete support (thanks to Srdjan Markovic).
  - Added logic to show application windows without explicitly listing their class (closes #80); Windows only.
  - Added Moai scratchpad support.
  - Added missing constants to Moai API.
  - Added setting focus on the window when a file is loaded in the existing instance.
  - Added two more wxwindow window classes to the list of windows to show (Windows).
  - Added scheme picker script to test color schemes from the IDE.
  - Added Zenburn as a color scheme.
  - Added styling for markers and Output/Console windows.
  - Added Italian translation (thanks to Leo Bartoloni).
  - Added Spanish translation (thanks to Inigo Sola).
  - Added reporting of run-time errors from remote processes (helps #73).
  - Added 'tomorrow' color theme.
  - Added redirect of remote 'print' commands for Gideros applications.
  - Added check for 'main.lua' in Corona projects.
  - Added ability to suspend a running application even when there is no editor tab or file to activate.
  - Added serialization of remote 'print' results and an output filter for debugging (helps #73).
  - Added redirect of remote 'print' commands to the Output window in the IDE (helps #73).
  - Added Corona SDK support (closes #73).
  - Added French translation (thanks to Roland Yonaba).
  - Added (more) verbose output to debugger to assist in troubleshooting.
  - Added handling of LUA_DEV to make loading Lua for Windows libraries working out of the box (fixes #71).
  - Added internationalization support (closes #70).
  - Added notes about estrela being gone and compatibility.
  - Added multi-tab support for scratchpad.
  - Added documentation on accelerator/shortcut syntax.
  - Added key map to change key binding for menu and toolbar items from a config file (closes #64).
  - Added hiding IDE while closing and saving configuration.
  - Completed reorganization of front-end processing (closes #67 and #5).
  - Disabled editor autoactivation during debugging when tracing is on.
  - Disabled functions that are not available under Corona on OSX (helps #73).
  - Disabled showing auto-complete when the only option is already typed.
  - Disabled showing a calltip on mouse over when it's already active.
  - Extended list of default folders for interpreters where executables are searched for.
  - Finished Russian translation.
  - Limited auto-showing windows to Lua interpreter only (related to #80).
  - Removed window types from config as they are no longer needed to be listed.
  - Switched to showing short tooltips by default as some of the updated Lua API descriptions are very long.
  - Updated descriptions for io.* functions; moved file:* functions to a pseudo library (f); helps #84.
  - Updated LICENSE to add remdebug license information.
  - Updated tooltip to show shortened descriptions on mouse-over.
  - Updated spec and API files for Lua 5.2 (thanks to Fringale).
  - Updated MobDebug to v0.507 to fix serialization issues (closes #77).
  - Updated style processing to support styling of markup and allow dynamic switching of color schemes.
  - Updated Lua keyword definitions to improve grouping for styling (closes #79).
  - Updated love2d api to an updated version as of 11/1/2012 (fixes #78).

### Incompatibilities
  - The default port in the debugger has been changed from 8171 to 8172 to avoid conflicts with existing RemDebug implementations.

### Fixes
  - Fixed an issue with overlapping auto-complete suggestions.
  - Fixed auto-complete not working after closed brackets.
  - Fixed tooltip being shown when mouse moving outside of the editor area.
  - Fixed a font zooming bug with Ctrl+MouseWheel.
  - Fixed file marking in the project tree after opening a new file.
  - Fixed an issue with breakpoints not available after aborting tracing on Linux/OSX.
  - Fixed an issue with running scratchpad when entry points are configured.
  - Fixed Linux launcher to pass filename parameter to the IDE.
  - Fixed fold style to properly set background color.
  - Fixed an issue with a config file being loaded into the editor when specified in the command line.
  - Fixed an issue with markdown formatting not working after setting styles from a config file (fixes #74).

## v0.33 (Oct 22 2012)

### Highlights
  - Added **Linux support**.
  - Added **Moai auto-complete**.
  - Added **Gideros debugging, auto-complete, and live coding**.
  - Added **syntax aware indentation**.
  - Added **re/storing open files and interpreter** when switching project folders.

### Special thanks
  - To Andy Bower and Atilim Cetin for their assistance with Gideros integration and live coding.
  - To toiffel for Linux/OSX/Windows CMake-based launcher build.
  - To Christoph Kubisch for help with Estrela merge.

### Improvements
  - Added configuration option to specify hostname when the default one is not reachable (fixes #68).
  - Added search for Moai and Love2d executables in default locations on OSX and Windows.
  - Added handling of command line parameters on OSX and Linux; updated documentation.
  - Added auto-recovery to save/restore editor content (fixes #23).
  - Added syntax aware indentation.
  - Added re/storing open files and interpreter when switching project folders.
  - Added auto-activation of files requested during debugging.
  - Added editor.autoactivate parameter (false by default) to enable activation.
  - Added zbstudio.sh script to run on OSX/Linux; updated permissions (closes #15).
  - Added variable ratio for sliders in scratchpad.
  - Added Linux/OSX/Windows CMake-based launcher build (thanks to toiffel).
  - Added configuration parameter for the number of characters typed before displaying auto-complete suggestions for dynamic words.
  - Added proper closing of the application after Shutdown/Logoff events (fixes #57).
  - Added Moai auto-complete (closes #52).
  - Added hiding auto-complete suggestions on losing focus in the editor.
  - Added proper reporting of errors in the stack window.
  - Added wxlua.deb file and install script that pull and build all required dependencies on Linux (covers most of #15).
  - Added osx executables and build files.
  - Added disabling full screen mode when the last editor tab is closed.
  - Added proper reporting of errors in config files.
  - Added toggling folders in project pane by single click (fixes #41).
  - Added examples for configuring tabs and moai entry points.
  - Added reporting compilation success rate (fixes #39)
  - Added shortcuts for adding and removing watches.
  - Added highlighting rows with updated values in the Watch window.
  - Added 'Add Watch Expression' and 'Evaluate in Console' context menu items in the editor.
  - Added handling of canceling editing in adding a watch.
  - Added Gideros auto-complete and live coding support (closes #62).
  - Added Gideros integration on OSX; added search for gideros in default locations on Windows and OSX.
  - Added Gideros integration and debugging support.
  - Added debugging-related buttons to the toolbar.
  - Improved reporting of compilation and run-time errors when running as scratchpad.
  - Made debugger strict.lua and LuaJIT friendly (upgraded to MobDebug v0.502).
  - Updated configuration example to avoid using ide.spec that is not available (fixes #67).
  - Updated CMake build script to handle wildcards in the MANIFEST.
  - Updated configuring toolbar size to improve Linux compatibility.
  - Updated MobDebug to v0.497 to fix issues and improve performance.
  - Updated a warning message about single instance communication.
  - Updated cpath processing to allow Linux version to run side-by-side with Windows and Mac.
  - Updated licensing information.
  - Enabled scratchpad support in the debugger when interpreter is capable of providing it.
  - Disabled auto-complete in comments and strings.
  - Disabled toggling breakpoints while debugger is running (as they wouldn't be changed anyway).
  - Disabled 'not activated file for debugging' message when auto-activation is disabled.
  - Disabled tooltip when scratchpad is on (fixes #51).
  - Disabled showing calltip when the editor is not in focus.
  - Disabled showing calltip over markup in comments.
  - Disabled 'Run as Scratchpad' in the menu for those intepreters that don't support scratchpad.
  - Updated configuration examples to add editor settings and fix user.lua path.
  - Moved all Estrela features into a separate bundle that can be loaded using cfg/estrela.lua config.
  - Removed multiple file types offered in Save As dialog.

### Incompatibilities
  - Changed searching for user config in '$HOME/.zbstudio' from '$HOME/.zbs'.
  - Temporarily removed the ability to modify spec-related configuration parameters from configuration files.

### Fixes
  - Fixed opening a non-existing file from the Recent Files list.
  - Fixed Find/Replace dialog checkboxes that didn't react to clicks on OSX (fixes #63).
  - Fixed an auto-complete issue with mixed case dynamic words (fixes #60).
  - Fixed 'Trying to solve a NULL hostname' warning message.
  - Fixed a typo that prevented a corner case in autocomplete from working.
  - Fixed inconsistent error messages about various config files.
  - Fixed an issue with auto-complete when dot or colon is used (fixes #56).
  - Fixed an issue with debugging scripts started using absolute file path.
  - Fixed setting working directory for interpreters to the file path when the project directory is not set.
  - Fixed an issue with Backspace not closing auto-complete suggestions.
  - Fixed enabling items in the Edit menu (fixes #56).
  - Fixed function list in the toolbar on Mac (helps #14).
  - Fixed deleting of comment blocks with hidden markup (fixes #40).
  - Fixed an issue with function list when all editor tabs are closed.
  - Fixed multiple calltips shown on Linux (helps #15).
  - Fixed an issue with mouse cursor in scratchpad not properly updated on Linux (fixes #49; helps #15).
  - Fixed an issue with static analyzer that failes on function names like a.b.c (fixes #50).
  - Fixed disabling Paste menu on Linux when needed (fixes #46; helps #15).
  - Fixed an issue with context menu on Linux (fixes #47; helps #15).
  - Fixed debugger failure when debugging is initiated externally and there is an unsaved file in one of editor tabs.
  - Fixed stopping the debugger when an editor tab where debugging happens is closed.
  - Fixed enabling of several menu items with no editor tab (fixes #42).
  - Fixed an issue with loading stock icons on Linux.
  - Fixed Cut/Copy menu items to reflect proper status in the editor.
  - Fixed typo in the static analyzer output.
  - Resolved conflict between lua executable names on Windows and Mac.

## v0.32 (Sep 03 2012)

### Highlights
  - Added **Unicode support** for file encoding and file paths on Windows (fixes #30).
  - Added **Moai integration and debugging** (including debugging of Moai threads and callbacks).
  - Added refresh of Stack and Watch windows after executing a statement in remote shell.
  - Added **display of complex values** on multiple lines in shell with '='.
  - Added calltip on mouseover for functions during editing and for variables/expressions during debugging.
  - Added configuration options to set paths to lua and love2d executables.
  - Added support for **coroutine debugging** with stepping through coroutine.resume/.yield calls.
  - Updated wx.dll to wxlua 2.8.12.2 and wxwidgets 2.8.12.
  - Signed zbstudio app and executable to avoid issues with files not being saved without admin privileges and to remove warning about 'unknown publisher' on windows (fixes #25).
    
### Improvements
  - Added calltip on mouseover for functions during editing and for variables/expressions during debugging.
  - Added an IO filter to fix an issue with 0d0d0a line endings on Windows.
  - Added support for debugging moai callbacks (upgraded to mobdebug v0.489).
  - Added refresh of Stack and Watch windows to show updated values after executing a statement in remote shell.
  - Added display of complex values on multiple lines in shell with '='.
  - Added rockspec to the list of extensions for lua (fixes #37).
  - Added a check to avoid evaluating keywords in tooltip.
  - Added current interpreter to the status bar; adding closing debugger when the interpreter is changed.
  - Added aborting scratchpad processing when an interpreter can't start or report a fatal error.
  - Added support for unicode path files on Windows (fixes #30).
  - Added an option to set path to lua executable.
  - Added error handler to trap and display debugger errors.
  - Added search in PATH for love2d executable.
  - Added a workaround for GetExecutablePath() reporting 'wx.dll' instead of a proper exe name with wxlua 2.8.12.2 on Windows.
  - Added reporting of function name of the form a.b and a:b in static analysis (fixes #27).
  - Added ability for user to keep their settings file in their home directory.
  - Added per user settings file. Users can now move their settings file to ~/.zbs/user.lua.
  - Added ignoring Cmd-key combinations on Mac as this should be handled by wxwidgets, but is not (fixes #19).
  - Added support for coroutine debugging with stepping through coroutine.resume/.yield calls.
  - Changed reporting of program execution time from CPU time to user time.
  - Changed the call to unhide windows to the async version (ShowWindowAsync) to avoid blocking the IDE when the application doesn't respond.
  - Upgraded to wxlua 2.8.12.2 (wxwidgets 2.8.12; unicode version); added lua51.dll proxy (fixes #10 and #7).
  - Updated love2d interpreter to use the project folder to check for main.lua.
  - Updated test module to use stringified values for comparison.
  - Updated status bar style to make it consistent across platforms.
  - Removed .bak files from being replaced in when backup copies are saved.
  - Removed explicit path conversions and comparisons.
  - Refactored LUA_PATH/CPATH processing to set it for all interpreters.
  - Signed zbstudio app and executable to avoid issues with files not being saved without admin privileges and to remove warning about 'unknown publisher' on windows (fixes #25).

### Incompatibilities
  - Reassigned hotkeys in the Project menu to minimize conflicts on Mac (reassigned Shift-F12 and F11).

### Fixes
  - Fixed an issue with double click on analylsis results being out-of-sync when the editor switched to another file (fixes #38)
  - Fixed an issue with debugger not activating files with relative path information.
  - Fixed 'break' command to work after coming from debugger calls (like on()).
  - Fixed an issue with highlighting selected item in the project tree.
  - Fixed evaluation of foo:bar in tooltip (now evaluates as foo.bar).
  - Fixed debugger termination after internal errors.
  - Fixed activating current file in the project tree on Mac (closes #29).
  - Fixed running scripts with single quotes in path names.
  - Fixed an issue with Run/Debug commands when IDE path includes exclamation mark ('!').
  - Fixed an issue with the app not starting on those systems that don't have HOME environment variable; fixes #28.
  - Fixed an issue with showing/hiding GUI windows that was occasionally causing a runtime error when the window disappears before it is manipulated.
  - Fixed returning proper name for unsaved files in reporting compilation and static analysis results; moved default names to ide.config (fixes #26).
  - Fixed pasting text into the Find dialog and project path box on Mac (fixes #22).
  - Fixed handling of dashes in paths (upgraded to mobdebug 0.479).
  - Reorganized handling of automcomplete event (to use AddPendingEvent instead of PostEvent) to avoid runtime application error.

## v0.31 (Jul 14 2012)

### Highlights
  - Added **scratchpad support for love2d**.
  - Added tooltip to display variable/expression values during debugging.
  - Added **MacOS support**.

### Improvements
  - Added handling of balanced brackets in markup links.
  - Added unit test module.
  - Added reporting the number of traced lines during debugging.
  - Added setting of PATH and CPATH to find proper libs on windows and mac os platforms.
  - Added scratchpad support for love2d.
  - Added reset of 'modified' status to keep tab names and their config settings correct upon exit.
  - Added window title update and filetree refresh after SaveAs command.
  - Added tooltip to display variable/expression values during debugging.
  - Made 'View Stack Window' and 'View Watch Window' refresh window content if it's already shown.
  - Removed setting the editor font in the config as the default font is different on different platforms.
  - Removed extension from the template to match folders to make it more portable.
  - Reorganized handling of font configuration and added font config for filetree (with a different size default on MacOS).
  - Updated matching logic for function definitions to allow for a.b.c() definitions (fixes #17).

### Fixes
  - Fixed markup styling and file tree drawing on MacOS.
  - Fixed detecting executable name in commands with spaces.
  - Fixed incorrect folders reported in the file tree when no project directory is set and a file is open.
  - Fixed incorrect filename reported in compile errors when the file is not saved.
  - Fixed refresh of filetree on MacOS to get it displayed correctly when the app is started.
  - Fixed an error thrown when a window with debugging is closed before the application being debugged is terminated.
  - Fixed incorrect storing of settings for editor tabs with the same text (filename). This was causing only one tab displayed for multiple StyledText controls with interesting effects.
  - Fixed an issue with launching a process when its output is not redirected to the IDE (fixes #16). 
  - Fixed console to evaluate 'function a() ... end' without errors.
  - Fixed a compilation error caused by shebang in scripts.
  - Fixed an issue with love2d path with spaces.
  - Corrected resetting of project directory when it's already set and doesn't need to be changed.
  - Added checks around ShowFullScreen() calls to avoid failures on those systems that don't provide it (linux/GTK).
  - Added check for debugger calls to avoid errors when debugger is not loaded.
  - Updated matching of links to make them less greedy (to avoid capturing link terminators).
  - Upgraded deprecated constants and logic for compatibility with wxwidgets 2.9.x.
  - Reset project directory if the current one doesn't exist.
  - Removed styling of function calls and capturing definitions in strings and comments (fixed #18).
  - Removed setting focus to the Output window when output is processed as it interfered with Run as Scratchpad.

## v0.30 (Jun 27 2012)

### Highlights
  - Added **love2d support**.
  - Added auto complete for love2d API.
  - Added support for debugging processes running under LuaJIT.
  - Added display of **hierarchical data in Stack window**.
  - Added **pretty printing in Watch and Console** (local and remote) windows and handling of multiple results in Console.
  - Added **Stack window to display stack information** and local/upvalue values for each stack frame.
  - Added ability to **interact with scripts** by allowing text to be entered in the 'Output' window.

### Improvements
  - Added love2d support.
  - Added auto complete for love2d API.
  - Added support for debugging processes running under LuaJIT.
  - Added display of hierarchical data in Stack window.
  - Added execution time and updated messages in the Output window to be more consistent.
  - Added displaying 'nil' values in local console when no result is returned by an expression.
  - Added a check to refuse starting a new debugging session if there is one in progress already.
  - Added handling of tail calls in the Stack window.
  - Added pretty printing in Watch and Console (local and remote) windows and handling of multiple results in Console.
  - Added Stack window to display stack information and local/upvalue values for each stack frame.
  - Added ability to set font encoding in the config.
  - Added restoring cursor position when a modified file is reloaded in the editor.
  - Added ability to interact with scripts by allowing text to be entered in the 'Output' window.
  - Improved logic in love2d integration to distinguish Debug and Run commands (closes #13).
  - Improved reporting in static analysis for functions and global variables.
  - Updated menus to avoid conflicts with MacOS shortcuts.
  - Updated logic creating menubar to make it work correctly on MacOS with special Help/About items.
  - Updated path handling to better detect how the app is started and to avoid loading dlls on non-windows platforms.
  - Updated logic for detecting hostname (used in the debugger) to make sure it is resolvable.
  - Changed order of lualibs/ and bin/ directories in package.path and package.cpath to load included modules first.
  - Removed extensions from launch commands and updated display logic in the Output window.

### Fixes
  - Fixed aborting running/debugged programs on MacOS by adding MAKE_GROUP_LEADER option to wxExecute.
  - Fixed an issue in the logic for setting breakpoints, which ignored breakpoints in luxinia2 debug sessions.
  - Fixed logic in the local/remote console that returned incorrect error message on executing code like '%s':format(1).
  - Fixed IDs for Project menu items to allow them to be removed from the menu if needed.
  - Fixed an issue with remote application not terminating when IDE is closed while debugging is in progress.
  - Fixed refreshing a modified file when the editor is set to read-only mode.
  - Fixed saving/restoring configuration of 'Output'/'Console' tabs when IDE is closed while debugging is in progress.
  - Fixed removing variable name in Watch window after escaping editing.
  - Fixed #9 as it had incorrect logic in one of UTF filters.
  - Fixed edit menu shortcuts to work in the 'Output' window (when allowed).
  - Fixed reporting of processes that failed to start after 'Run' or 'Debug' commands.
  - Fixed executable path matching to work on systems that don't have file extensions.
  - Fixed #3 'unused parameter...' check not to fail on anonymous functions that are part of an expression.
  - Moved processing of `user.lua` to a later phase after tools and specs are already loaded to allow modification of IDE configuration from `user.lua`. Closes #5.
  - Added checks to prevent text modification in 'Output' and 'Console' windows. Fixes #8.
  - Disabled 'Run as Scratchpad' if there is no debugger registered capable of running it.
  - Disabled Stack and Watch updates when scratchpad is active as they interfere with application execution.

## v0.29 (May 31 2012)

### Highlights
  - Added **scratchpad** (running live) functionality.
  - Added **code analyzer** based on lua-inspect.
  - Updated **comment styling** to follow markdown syntax.

### Improvements
  - Added scratchpad (running live) functionality.
  - Added code analyzer based on lua-inspect.
  - Added Ctrl(-Shift)-TAB navigation between tabs in the editor.
  - Added navigation between editor tabs using Ctrl-PgUp and Ctrl-PgDn.
  - Added reporting of assignment to global variables in the code analyzer.
  - Added ability to turn external processes that connect to debugger into a scratchpad.
  - Added exit from full screen mode using ESC key.
  - Added reporting of compilation errors during debugging sessions.
  - Added handling of more errors in the shell to allow calculations like '(1+2)' to be executed correctly.
  - Added moving focus back to the notebook after unhiding/activating a wx window.
  - Added missing mime/code.dll and reorganized socket module files (socket.*) to load correctly with require.
  - Added stopping the debugger when a debugged program exits.
  - Added to static analysis reporting of unused parameters in functions.
  - Disabled warning in static analysis about unused 'self' in methods.
  - Removed 'error during pre-compilation' message from compile errors.
  - Updated comment styling to follow markdown syntax.

### Fixes
  - Fixed handling of scripts with comments in the remote shell.
  - Fixed an issue with Analyze process when the analyzed script has compilation errors.
  - Fixed an issue with scratchpad being on after Save dialog is canceled.
  - Fixed about screen.

## v0.28 (Mar 21 2012)

### Highlights
  - Added full screen mode.

### Improvements
  - Added option to activate output/console when Run/Debug/Compile commands are executed.
  - Added full screen mode.
  - Added killing a running process on IDE exit.
  - Added killing a running process with Shift-F12.
  - Disabled buffering of the output for scripts run from IDE.

### Fixes
  - Fixed 'Trace' command to continue working when a debugged file is not activated.
  - Fixed an issue with saving a file when no project directory is set.
  - Fixed missing semicolon in lualibs path; added path for debugger to search under lualibs.
  - Fixed an issue with a missing path separator, which prevented debugging from executing step commands in some cases.
  - Fixed missing slash on SaveAs by enforcing trailing slash for the project path.

## v0.27 (Feb 14 2012)

### Highlights
  - Added markup formatting in the comments.

### Improvements
  - Added markup formatting in the comments.
  - Added Debug and Run methods to simulate menu commands.
  - Added setting a project folder on initial start.
  - Added style processing for font name, font size, visibility and hotspot attributes.
  - Added setting the current project directory for the shell to allow 'require' commands to work with local modules.
  - Updated markup processing with run and debug commands, http link processing, and opening local files in a new window.
  - Enforced visibility for shell prompt.

### Fixes
  - Fixed activation of a correct tab when one of the editor tabs is closed.
  - Fixed an issue with file activation from a debugger.
  - Fixed the issue of ClosePage method being called with two different parameters.
  - Fixed the issue of the project dir being returned with two trailing slashes.
  - Fixed an issue with activating the currenly edited file in the file tree.
  - Wrapped DragAcceptFiles into a protected call to make it not fail on MacOS (compiled with wxwidgets 2.8.12).

## v0.26 (Jan 18 2012)
