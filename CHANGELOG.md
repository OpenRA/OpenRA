# ZeroBrane Studio Changelog

## v1.10 (May 13 2015)

### Highlights
  - Redesigned search functionality; added incremental search and replace-in-files preview.
  - Updated love2d API for v0.9.2.
  - Upgraded Mobdebug (0.62) to add vararg processing.
  - Added `excludelist`/`binarylist` to exclude files and folders.
  - Added skipping binary files during find-in-files and fuzzy search.

### Special thanks
  - To [David Feng](https://github.com/DavidFeng) for fixing a broken symbolic link.
  - To [Robert Machmer](https://github.com/rm-code) for updated German translation and fixes for love2d engine name/description.
  - To [Derek Bailey](https://github.com/dbaileychess) for adding context menu item to update the contents of the tree.

### Improvements
  - Added closing search results with `Close Page` for consistency (#416).
  - Added option to show search results in multipe tabs (#416).
  - Added package `IsValidCtrl` method (#166).
  - Added skipping binary files in commandbar (#31, #455).
  - Added handling of `**` in exclusion masks (#455).
  - Added `excludelist`/`binarylist` to exclude files and folders (closes #455).
  - Added `search.autohide` option to hide search panel after search (#416).
  - Added `debugger.requestattention` to configure focus switch (closes #449).
  - Added closing search panel on `Escape` in the editor (closes #450, #416).
  - Added `search.zoom` setting to zoom search results (#416).
  - Added warning when interpreter can't be loaded and the default one is used.
  - Added support for `wxImage` used in `imagemap` (#436).
  - Added icon tint processing (`imagetint` option).
  - Added `search.contextlinesbefore`/`search.contextlinesafter` settings (#416).
  - Added `search.autocomplete` setting do configure search autocomplete (#416).
  - Added auto-complete for find and replace fields (#416).
  - Added saving search settings between restarts (#416).
  - Added deleting of protected text in Cut/Paste operations (closes #412).
  - Added refresh context menu item to update the contents of the tree (thanks to Derek Bailey).
  - Added ignoring assignment in comments for auto-complete.
  - Added preview screen to check changes in replace-in-files (#416).
  - Added search toolbar button to set/unset context in search results (#416).
  - Allowed for larger default size of docked panels (#424).
  - Allowed closing `Search Results` tab while search is in progress (#416).
  - Disabled refresh when `outlineinactivity=nil` and `showonefile=true` (#337).
  - Improved incremental processing to avoid marking table fields as variables.
  - Improved logic to skip binary files during search (#416).
  - Moved cancelling auto-complete popup to idle event (fixed #447).
  - Moved winapi dll to `clibs` folder to avoid conflict with other winapi versions.
  - Optimized `Go To File` file traversal in commandbar (#31).
  - Redesigned the search panel (closes #416; closes #398).
  - Removed option to request attention from breakpoint activation (#449, closes #454).
  - Removed warnings from loose parser to stop polluting stderr on Linux (#425).
  - Removed hardcoded references to menu positions to simplify adding new items.
  - Removed saving .bak files as it's replaced by replace-in-files preview (#416).
  - Reorganized folding to allow more editor-like components to be foldable.
  - Switched to using `wxFileSize` instead of `Length` (fixes #458).
  - Updated event documentation.
  - Upgraded Mobdebug (0.62) to add vararg processing.
  - Updated build files to use release version of Lua 5.3 (#453).
  - Updated Corona interpreter to allow debugging on Linux using Wine.
  - Updated folder traversing logic to limit number of open folders (fixes #441).
  - Update Readme.md to use correct name for the LOVE framework (thanks to Robert Machmer).
  - Updated Linux build scripts to add `debug` option.
  - Updated love2d interpreter to use proper engine name/description (closes #433) (thanks to Robert Machmer).
  - Updated german translation (#432, #70).
  - Updated love2d APU for v0.9.2 (#247).
  - Update german translation (thanks to Robert Machmer).
  - Updated `Select and Find` to capture selection for subsequent use (#416).
  - Updated `Find Next/Prev` to continue search using the current find text (#416).
  - Updated unfolding of folded lines before delete or overwrite.
  - Updated folding to collapse only when clicked on the header.
  - Updated config samples to use `ID.code` instead of obsolete `G.ID_code`.

### Fixes
  - Fixed crash on Windows by disabling events in Outline refresh (fixes #442).
  - Fixed file traversing not to match `foo.bar!` for `*.bar' mask (#416).
  - Fixed showing of not translated messages that require pluralization.
  - fix broken symbolic link (thanks to David Feng).
  - Fixed an empty popup (that could lead to a crash) when dynamic words is on.
  - Fixed commandbar positioning on Linux affected by 5b665477 (#31).
  - Fixed loose parser to handle multiple assignments (fixes #430).
  - Fixed editor activation after dragging of inactive tab (fixes #427).
  - Fixed indentation after comment with markdown markup (closes #428, #324).
  - Fixed auto-complete issue causing looping warning (#143).
  - Fix commandbar position with split editor tabs (#31).
  - Fixed re-indenting of selection with comments at the top of the file (#324).
  - Fixed restoring pane size after hide/show operations (fixes #424).

### Incompatibilities
  - Removed saving .bak files as it's replaced by replace-in-files preview.

## v1.00 (Mar 13 2015)

### Highlights
  - Added directory mapping to the project tree (closes #223).
  - Added `Run to Cursor` (closes #413).
  - Added support to set/unset start file for a project (closes #411).
  - Added opening/creating file from the command bar (#31).
  - Added `staticanalyzer.infervalue` to enable deeper/slower analysis (#414).
  - Updated Corona API for v2015.2576.

### Improvements
  - Added `staticanalyzer.infervalue` to enable deeper/slower analysis (#414).
  - Added project path in the error reporting for love2d/corona interpreters.
  - Added an example with enabling Emacs bindings in the editor.
  - Added Russian translations for new messages (#70).
  - Added a warning on class resolution taking too much time in auto-complete.
  - Added check for empty/comment lines when breakpoints are set.
  - Added directory mapping to the project tree (closes #223).
  - Added `Run to Cursor` (closes #413).
  - Added document method `GetTabText` (#166).
  - Added showing love2d console when requested.
  - Added support to set/unset start file for a project (closes #411).
  - Added requesting attention for debugger even when the file is not activated.
  - Added reporting of location in more cases when debugging is suspended.
  - Added starting debugging even when the file is not opened in the IDE.
  - Added switching project directory from the command bar (#31).
  - Added trimming of trailing spaces in the directory name when switching projects.
  - Added closing preview if the file failed to load in commandbar (#31).
  - Added handling of `Ctrl/Cmd-Enter` in commandbar to force open file (#31).
  - Added skipping loading files with empty and directory names.
  - Added trimming of trailing spaces in the file name when loading a file.
  - Added file selection in the project tree after saving a new file.
  - Added opening/creating file from the command bar (#31).
  - Disabled menu item for renaming/editing for mapped directories (#223).
  - Disabled field checks for local parameters in staic analyzer (closes #421).
  - Improved static analyzer to handle more cases with `infervalue` set (#414).
  - Refactored use of image constants in the project tree.
  - Refactored document modification tracking to remove `SetDocumentModified`.
  - Refactored path normalization for project directory.
  - Updated Corona API for v2015.2576.
  - Updated static analyzer output formatting.
  - Updated analyzer to also check value of `pairs` parameter (#414).
  - Updated `debugging suspended` message to not trigger with `runonstart`.
  - Updated messages in interpreters to fix line endings.
  - Updated lettercase in menu items for consistency.
  - Updated UpdateUI handling to fix multi-view refresh, as in DocumentMap (#352).
  - Updated Outline to use editor tab text (instead of a file name).
  - Updated message on failure to start debugging to add the file name.
  - Updated `debugging suspended` message to put location in quotes.
  - Updated line check in command bar to not trigger on Windows file names.
  - updated glewgl api (glew 1.12.0)

### Fixes
  - Fixed analyzer to check value of `ipairs` parameter (fixes #414).
  - Fixed OS detection on Linux that sets `DYLD_LIBRARY_PATH` (fixes #417).
  - Fixed saving auto-recovery record with no editor tabs opened (fixes #418).
  - Fixed looping in auto-complete when processing recursive assignments.
  - Fixed filename used in the recovery message.
  - Fixed Output/Console window to stay shown after failure to start debugging.
  - Fixed an issue with search initiated from Output/Console panels (fixes #406).
  - Fixed auto-complete for non-default `acandtip.strategy` values (fixed #409).
  - Fixed loading file with absolute name and line number (fixes #408).

## v0.95 (Jan 30 2015)

### Highlights
  - Added fuzzy search with `Go To File`, `Go To Symbol`, `Go To Line`, and `Insert Library Function`.
  - Added auto-complete support for LDoc '@tparam' and '@param[type=...]'.
  - Added armhf architecture support (thanks to Ard van Breemen).
  - Updated static analyzer to support `goto`/labels and bitops for Lua 5.2/5.3.
  - Updated internal parser to support Lua 5.2/5.3 syntax.
  - Updated Mobdebug to improve Lua 5.3 compatibility (thanks to Andrew Starks).
  - Update API descriptions with functions new in Lua 5.3.

### Special thanks
  - To [Ard van Breemen](https://github.com/ardje) for armhf architecture support.
  - To [Evandro Costa](https://github.com/evandro-costa) for Brazilian Portuguese (pt-br) translation.
  - To [Andrew Starks](https://github.com/andrewstarks) for Lua 5.3 compatibility improvements.
  - To [Alexis Jarreau](https://github.com/Fringale) for French translation update.
  - To [Leo Bartoloni](https://github.com/bartoleo) for Italian translation update.
  - To [riidom](https://github.com/riidom) for German translation update.

### Improvements
  - Added showing/hiding Output/Console panel during debugging.
  - Added `bordersize` to configure sash size.
  - Added package `LoadFile` method (#166).
  - Added Russian translation for new messages (#70).
  - Added syntax highlighting for functions new in Lua 5.3.
  - Added `commandbar.prefilter` to commandbar to improve performance (#31).
  - Added custom formatting for APIs.
  - Added `Insert Library Function` to `Navigate` menu.
  - Added hiding tooltip/auto-complete popup after undo/redo/delete operations.
  - Added `api` handling to the config to support custom APIs.
  - Added Lapis and Moonscript debugging links to README.
  - Added `Contributing` section to README.
  - Added LICENSE link to README.
  - Added Lua 5.3 links to README; updated links to remove '.html'.
  - Added `PgUp` and `PgDn` navigation for commandbar (#31).
  - Added auto-complete support for LDoc '@tparam' and '@param[type=...]'.
  - Added Brazilian Portuguese (pt-br) translation.
  - Added showing the symbol under cursor in `Go To Symbol` (#385).
  - Added `editor:ValueFromPosition` method (#166).
  - Added `Go To Symbol` support to the commandbar (#31, closes #385).
  - Allowed renaming of files with different case in the project tree.
  - Added update of file name in the Outline when editor tab is updated (#337).
  - Added check for `styles` being reset in the config file (closes #383).
  - Added loading file from project directory with "proj file" on command line.
  - Added skipping of binary files in commandbar preview (#31).
  - Added CONTRIBUTING file.
  - Added clearing Output window before showing Find-in-Files results.
  - Added default values for Outline config settings (#337).
  - Added package `GetProjectNotebook` method (#166).
  - Added saving Outline tab configuration in the Project notebook (#337).
  - Added `outline.sort` option to sort items in the outline (#337).
  - Added `outline.showflat` option to show flat outline (#337).
  - Added package `GetOutlineTree` method (#166).
  - Add support for armhf architecture
  - Added document `Save` method (#166).
  - Added `init.lua` to the search path (fixes `require 'dist'`).
  - Added forced garbage collection when switching from the app.
  - Added penalty for missing characters in fuzzy search (#31).
  - Added line navigation to `Navigate` menu (#31).
  - Added handling of line numbers in command bar (#31).
  - Added caching to improve commandbar performance on large folders (#31).
  - Added setting explicit focus on preview tab in commandbar (#31).
  - Added preview on first selection in commandbar (#31).
  - Added file preview to commandbar (#31).
  - Added fuzzy search for files in commandbar (closes #31).
  - Added displaying the list of current tabs for commandbar (#31).
  - Added commandbar prototype for file navigation (#31).
  - Added reset of tokens when editor content is reloaded (#337).
  - Enabled html and C/cpp specs by default (#395).
  - Improved handling of complex types in LDoc expressions in auto-complete.
  - Improved failed search 'shake' visualization by forcing window redraw.
  - Improved command line file check for relative files on Windows.
  - Split console output into shorter lines to improve memory usage and handling.
  - Renamed `debugger.stackmax*` settings to `debugger.maxdata*`.
  - Removed double check for invalid UTF-8 output in console.
  - Refactored ID handling to allow using `ID.COMMENT` in config files.
  - Removed `wxwidgets` from the list of default APIs for Lua interpreters.
  - Refactored handling of special symbols in commandbar (#31).
  - Removed border from tree controls (#305).
  - Updated language files with new messages (#70).
  - Update API descriptions with functions new in Lua 5.3.
  - Updated loose parser to support Lua 5.2+ labels.
  - Updated loose parser to support `goto` and Lua 5.3 bitops.
  - Updated Metalua to support Lua 5.3 bitops.
  - Updated Metalua to support `goto`/labels in static analysis for Lua 5.2/5.3.
  - Upgraded Mobdebug (v0.611) to fix tooltips during Corona debugging (closes #387).
  - Updated `CommandBarShow` to accept text fragment to select (#31).
  - Updated `GetKnownExtensions` to accept an optional extension to match against.
  - Updated `FileRead` to accept optional length.
  - Updated layout settings to use constants and new package API.
  - Updated parser to avoid splitting `foo.bar` in incremental processing.
  - Upgraded MobDebug (0.61) to add `basedir` support.
  - Updated status refresh to only happen for the active editor.
  - Updated toolbar UI checks to happen when running/debugging (#352).
  - Updated Corona integration to hide console on OSX (2014.2393+).
  - Upgraded Mobdebug to 0.613 to improve Lua 5.3 compatibility (#401); thanks to @andrewstarks.
  - Updated usage instructions in README.
  - Update de.lua

### Incompatibilities
  - Renamed `debugger.stackmax*` settings to `debugger.maxdata*`.
  - Removed `wxwidgets` from the list of default APIs for Lua interpreters; use `api` config setting to include it.

### Fixes
  - Fixed search navigation to shift horizontally when `usewrap` is off.
  - Fixed indentation for lines with mixed string separators (#324, #388).
  - Fixed indentation for lines with escaped slashes (#324, closes #388).
  - Fixed find-and-replace to replace in already selected fragment if matched.
  - Fixed refresh of files in command bar preview when selected (#31).
  - Fixed recovery of empty (`untitled`) tabs.
  - Fixed an auto-complete issue after `repeat until <var>`.
  - Fixed setting focus to the editor in commandbar with one tab (#31).
  - Fixed auto-complete after text not separated by a whitespace or bracket.
  - Fixed an error when disabling outline by setting `outlineinactivity=nil`.

## v0.90 (Nov 08 2014)

### Highlights
  - Added function outline.
  - Added Lua 5.3 (beta) binaries and debugging support.
  - Added scope-aware auto-complete for local/global variables.
  - Added hiding/showing files by type in the project/filetree.
  - Added Esperanto (eo) translation.
  - Improved compatibility with Lua 5.2 interpreter.
  - Improved compatibility with system/custom Lua interpreter.

### Special thanks
  - To [cosmotect](https://github.com/cosmotect) for added Esperanto translation.
  - To [riidom](https://github.com/riidom) for updated German translation.
  - To [Christoph Kubisch](https://github.com/pixeljetstream) for glsl improvements.
  - To [Wojciech Milkowski](https://github.com/milkowski) for making indentation guide configurable.
  - To [sclark39](https://github.com/sclark39) for adding project dir to find dialog paths.

### Improvements
  - Added Lua 5.3 (beta) support and binaries.
  - Added Russian translation for new messages (#70).
  - Added `AddPackage` and `RemovePackage` methods (#166).
  - Added `CreateBareEditor` package method (#166).
  - Added `GetAPI` method for interpreter (#166).
  - Added `GetOutputNotebook` package method (#166).
  - Added `IsPanelDocked` package method (#166).
  - Added `Run` and `Run as Scratchpad` buttons to the toolbar.
  - Added `acandtip.maxlength` option for setting the length of a tooltip.
  - Added `function` handling to the token processing.
  - Added `imagemap` setting to support custom images.
  - Added `onEditorCallTip` method (#166).
  - Added `showonefile` option for the outline to always show one file (#337).
  - Added an Esperanto (eo) translation.
  - Added build support for Lua 5.3-alpha and luasocket for Lua 5.3.
  - Added check for existing process id before stopping the process.
  - Added check for pending data to improve re-starting debugging session.
  - Added collapsing outlines for files in inactive tabs (#337).
  - Added creating italic font if only the main one is provided.
  - Added document `SetActive` method (#166).
  - Added drag-and-drop support for the Outline tab (#337).
  - Added example of enabling `Opt+Shift+Left/Right` shortcut on OSX.
  - Added function outline (closes #337, closes #222).
  - Added handling of `~` in launch command path.
  - Added hiding/showing files by type in the project/filetree (closes #375).
  - Added local/global indicators to function outline (#337).
  - Added marking file after showing files in the tree (#375).
  - Added navigation based on 'filename:line:pos' in the Output window.
  - Added option for not/showing anonymous functions in the outline (#337).
  - Added package `AddTool` and `RemoveTool` methods (#166).
  - Added package `CreateImageList` method (#166).
  - Added package `ExecuteCommand` method (#166).
  - Added package `FindTopMenu` method (#166).
  - Added package `GetAppName` method and removed hardcoded name references (#166).
  - Added package `GetConsole` method (#166).
  - Added package `GetKnownExtensions` method (#166).
  - Added project dir to find dialog paths; thanks to @sclark39 (closes #358).
  - Added rule to enable `Set From Current File` only when available.
  - Added scope-aware auto-complete for local/global variables (closes #291).
  - Added scrolling to the top of the outline when `showonefile` is set (#337).
  - Added sending Corona SDK simulator output to the Output window on Windows.
  - Added translation label for `Toggle Bookmark` toolbar icon (#70, #373).
  - Added unindent on backspace (controlled by `editor.backspaceunindent`).
  - Added view menu for the Outline window (#337).
  - bugfix in output callback for commandline tools
  - bugfix on extension change save-as, related to new indication handling
  - cg/hlsl/glsl refine isfndef capture, mostly to react on GLSL's layout mechanism
  - Disabled moving of Output/Console/Project tabs between panels.
  - Disabled closing tabs in floating panels.
  - Improved `ffitoapi` tool logic when no replacement is made.
  - Improved compatibility with Lua 5.2 interpreter (closes #357).
  - Improved scroll positioning in the outline after tab changes (#337).
  - Improved support for non-lua specs in the outline (#337).
  - Minor update to indentation guides handling (#371).
  - Moved 'default' search path to be searched first (#357).
  - Reduced rate of toolbar UI checks to improve performance (fixes #352).
  - Reduced the number of focus changes in the outline (#337).
  - Reduced unnecessary editor processing to improve performance (#352).
  - Refactored `GetBitmap` package method (#166).
  - Refactored `tools` interface to make it easy to add/remove tools.
  - Refactored adding editor tab to ensure callbacks have document data.
  - Refactored default `fprojdir` and `fworkdir` from the interpreter code.
  - Refactored drag-and-drop processing for Project/Output window tabs (#377).
  - Refactored panel docking; added `AddPanelDocked` package method (#166).
  - Refactored timer usage for consistency.
  - Remove function dropdown from the toolbar (#337).
  - Removed menu separator from the Tools menu.
  - Removed prepending libraries for debugging to LUA_CPATH when custom interpreter is specified.
  - Removed reference to `funclist`, which is no longer needed.
  - Removed unused image files.
  - Renamed `markvars` method used in spec files to `marksymbols`.
  - Renamed image files to have names correspond to the content.
  - Reorganized default config settings.
  - Reorganized token list processing to keep it within the editor.
  - Restored removed function in Lua spec (partial revert of 713d0935).
  - Switched to using `Is{Input|Error}Available` instead of `stream:CanRead`.
  - updated luxinia2 related files
  - Update de.lua
  - Updated C-based specs to handle function calls without parameters.
  - Updated C-based specs to use `marksymbols` to provide outline for C functions.
  - Updated Lua 5.3 build scripts.
  - Updated `AddPackage` to assign package file name (#166).
  - Updated `RemoveMenuItem` to disconnect handlers attached to the main frame (#166).
  - Updated `package.config` description to remove reference to Lua 5.2.
  - Updated `showanonymous` to a label for anon functions in the outline (#337).
  - Updated code based on static analysis suggestions.
  - Updated command launch handling to allow output suppression.
  - Updated function call indicator to support `isfncall` and `marksymbols`.
  - Updated function indicator processing to use ranges.
  - Updated handling of extensions to allow more symbols in extensions.
  - Updated images in the outline; added `showmethodindicator` option (#337).
  - Updated indicator processing to improve performance on large files.
  - Updated interpreter processing to run after packages are loaded.
  - Updated label for anonymous functions in the outline (#337).
  - Updated language files with new messages (#70).
  - Updated markup processing to allow for 3+ markup sequences.
  - Updated markup processing to support links in non-editor documents.
  - Updated messages for to match translations (#70).
  - Updated method of collapsing outline to fix crash on OSX (#337, fixes #368).
  - Updated outline logic to show on the very first launch (#337).
  - Updated outline to always expand functions in the current file (#337).
  - Updated outline to show files without functions (#337).
  - Updated outline to track filename changes after `Save As` (#337).
  - Updated outline to use `AddPackage` method (#337).
  - Updated output callback processing not to run when nothing to process.
  - Updated package `GetRootPath` to accept file/directory name (#166).
  - Updated parser to avoid 'breaking' statements during incremental processing.
  - Updated parser to handle `...` in function parameters.
  - Updated parser to report function token before parameters (#337).
  - Updated parser to store position for not-quite-valid function names.
  - Updated processing of function indicators when auto-analyzer is off.
  - Updated search/replace to always use the current editor/output/console tab.
  - Updated static analizer to accept typedlua parser in addition to metalua.
  - Updated tooltip processing to make it more consistent and better use space.
  - Updated translation building script to handle non-string parameters (#70).
  - Updated un/comment to keep the current selection and caret position (#360).
  - Upgraded MobDebug (0.606) for `Detach Process` to correctly close debugging.
  - Upgraded Mobdebug (0.607) to fix debugging after `Detach Process` command.

### Incompatibilities
  - Renamed `markvars` method used in spec files to `marksymbols`.

### Fixes
  - Fixed 'slow' mode of static analysis to work with Metalua 0.7.2.
  - Fixed `Output` tab name after stopping/completing remote debugging.
  - Fixed `Project` label shown untranslated in other languages (#70, #373).
  - Fixed `Run` toolbar label not being translated (#70, #373).
  - Fixed activation in `tree:FindItem` when new editor tab is opened (#166).
  - Fixed an error when dragging Stack/Watch/other tabs between notebooks.
  - Fixed an issue with removing first menu item in RemoveMenuItem (#166).
  - Fixed an issue with searching in Output and Console windows.
  - Fixed column indicator on lines with tabs (fixes #379).
  - Fixed disabling Stack/Watch icons in the toolbar.
  - Fixed error after using Enter multiple times in `Find in Files` on OSX.
  - Fixed file renaming in the filetree after using `SaveAs`.
  - Fixed flicker in the outline when auto-complete is shown (#337).
  - Fixed focus switch after selecting a function in the outline and editing (#337).
  - Fixed handling of remapped image files; improved error reporting.
  - Fixed incorrect `binary not` calculation with wxlua and LuaJIT 2.1.
  - Fixed index check during tab name update.
  - Fixed keeping toolbar status after hiding it.
  - Fixed localization in function outline (#337).
  - Fixed localization to avoid error in `SaveAs` processing.
  - Fixed navigation in function outline when `showonefile` is set (#337).
  - Fixed not hiding directories when files without extension are hidden (#375).
  - Fixed off-by-one error in function outline position tracking (#337).
  - Fixed outline refresh after quick tab switches (#337).
  - Fixed refresh of 'background' markers during debugging.
  - Fixed replacement when selection doesn't match the text being searched for.
  - Fixed search in files/directories with `%` in the name (fixes #369).
  - Fixed storing position in `function` handling.
  - Fixed stream reading for the Output to only include actually read chars.
  - Fixed unused variables and constants based on static analysis.

## v0.80 (Aug 31 2014)

### Highlights
  - Added support for expanding table elements in Watch window.
  - Added editing of values in Watch window.
  - Added highlighting all instances of selected text.
  - Added replacing all selected instances using a dialog.
  - Added saving (one-line) layout for editor tabs.
  - Added support for `filename:<line>` and `filename:p<pos>` on the command line.
  - Added search in Console and Output windows.
  - Improved compatibility with Lua 5.2 to run the IDE.

### Special thanks
  - To [Li Jia](https://github.com/tiwb) for fixing remote path map when 'runonstart' option is set.

### Improvements
  - Added default values for `hotexit` and `saveallonrun` settings.
  - Added debugger `GetHostName` and `GetPortNumber` methods (#166).
  - Added a check for a local shortcut (F2/Del) being enabled before triggering.
  - Added refresh of expanded Watch values.
  - Added support for expanding table elements in Watch window.
  - Added package `AddWatch` method (#166).
  - Added `toolbar.iconsize` to configure toolbar icon size.
  - Added `run-as-scratchpad` toolbar icon (hidden by default).
  - Added `run` toolbar icon (hidden by default).
  - Added `find-in-files` toolbar icon (hidden by default).
  - Added support for disabling individual icons in the toolbar.
  - Added replacing all selected instances using a dialog (closes #342).
  - Added highlighting all instances of selected text (closes #344).
  - Added `filetree.mousemove` option to disable drag-n-drop (closes #351).
  - Added `suspended` to Output panel title when debugger is stopped (closes #350).
  - Added a warning when remote console can't evaluate an expression (#350).
  - Added handling of `osname` to package dependencies (#166).
  - Added `onIdle` event (#166).
  - Added `tree:FindItem` method (#166).
  - Added package `Yield` method (#166).
  - Added ability to set location of `ini` file from config.
  - Added ability to load bitmap as toolbar icon.
  - Added package `RemoveMenuItem` method (#166).
  - Added ability to customize toolbar.
  - Added saving (one-line) layout for editor tabs.
  - Added centering of the screen after re-indenting and sorting (#337).
  - Added local to variable 'activated' in function mapRemotePath
  - Added centering of the screen after 'go to definition' and back (#337).
  - Added centering of the screen after selection from the function list (#337).
  - Added package `onEditorUpdateUI` event (#166).
  - Added package `AddPanel` method (#166).
  - Added package `GetUIManager` method (#166).
  - Added editor `SetupKeywords` method (#166).
  - Added document `GetFileExit` method (#166).
  - Added `onEditorPainted` event (#166).
  - Added support for `name:<line>` and `name:p<pos>` on the command line.
  - Added error reporting on failure to load file from the command line.
  - Added metalua components to MANIFEST (missing in packaging on OSX).
  - Added saving auto-recovery record on switching from the application.
  - Added `hotexit` option to exit without forcing to save files.
  - Added setting of margin properties to support their reordering.
  - Added error reporting on failure to delete directory from project tree.
  - Added check for shortcut in conflict being enabled before activating (#233).
  - Added workaround for missing `GetChildren` call in some wxlua configurations.
  - Added unfolding modified lines to avoid leaving hidden lines in the editor.
  - Added search in Console and Output windows (closes #313).
  - Allowed double-click selection in the Output window (#313).
  - Avoided system lib conflict when debugging by using bundled libs (fixes #355).
  - Disabled editing on non-root watch elements.
  - Disabled smart indentation for multi-line comments and strings (#324).
  - Disabled re-indentation of multi-line comments/strings (#324).
  - Disabled `Opt+Shift+Left/Right` shortcut as it conflicts with block selection.
  - Enabled editing of values in Watch window.
  - Enabled `editor.autoreload` by default.
  - Improved config handling when `editor` configuration is removed/empty.
  - Improved `autotabs` logic when the file starts with indentation.
  - Improved auto-complete logic that tracks variable assignments (fixes #343).
  - Improved cursor positioning after re-indenting or sorting.
  - Improved compatibility with Lua5.2 to run the IDE.
  - Increased default project history length to 20.
  - Removed check for multiple references in stack values.
  - Refactored stack processing to use methods to handle expandable table values.
  - Refactored file name generation for compilation and static analysis.
  - Removed erroneous message about failure to open '-psn...' file on OSX.
  - Renamed all image files to remove cruft from their names.
  - Simplified logic for watch processing.
  - Switched from using TreeItemData to Lua tables for watch expressions.
  - Switched to using tree control for watches.
  - Updated copas library to support non-blocking requests using socket.http.
  - Updated Stack and Watch views to better stringify keys.
  - Updated watch menu to handle item under mouse cursor.
  - Updated constants for image lists.
  - Updated `FindMenuItem` method to search in the main and specified menus (#166).
  - Updated `ide.config` to access wx, wxstc, and os through metatable.
  - Updated recent projects/files handling to allow menus to be removed.
  - Updated package `FindMenuItem` method (#166).
  - Updated `autotabs` to respect `usetabs` when no indentation is present.
  - Updated copy/cut to capture one instance when all are the same (closes #345).
  - Updated default marker colors for lighter border (#305).
  - Updated auto-recovery logic to skip missing files (fixes #323).

### Fixes
  - Fixed disabling auto-recovery on app switching.
  - Fixed find-in-files error when used with editor not in focus (fixes #354).
  - Fixed package `GetStack` method to return proper control (#166).
  - Fixed Watch window background color on some Mint Linux systems.
  - Fixed debugging error when `debugger.runonstart` is specified (fixes #348, #341).
  - Fixed keybinding for `Ctrl-<punctuation>` working on Linux (fixes #346).
  - Fixed localization based on static analysis.
  - Fixed remote path map when 'runonstart' option is set.
  - Fixed error reporting during Analyze (fixes #340).
  - Fixed using image lists for stack/filetree to keep them in memory.
  - Fixed indentation when Enter is hit at the middle of a line.
  - Fixed formatting of `until` statements (fixes #335).
  - Fixed formatting of strings including comments '--' (#335).
  - Fixed restoring proper file names for unsaved tabs during auto-recovery.
  - Fixed deleting 'dynamic words' when multiple lines are removed.
  - Fixed `love.update` description (#247).
  - Fixed indentation of strings starting from `endSomething` (#324).
  - Fixed use of '%' in replacement for Lua5.2 compatibility (#153, #156, #143).
  - Fixed warnings from static analysis.

## v0.70 (Jun 18 2014)

### Highlights
  - Added support for OpenResty/Nginx, moonscript, and Lapis debugging.
  - Added re-indentation of selected fragment or entire file.
  - Added line mapping support for debugging Lua-based languages (e.g. moonscript).
  - Added `editor.wrapindentmode` and `editor.wrapstartindent` settings.
  - Fixed debugger compatibility with Lua 5.2.
  - Fixed `F2` shortcut not working in file tree and watch panel.
  - Fixed replace-in-files when saving backup copy is turned off.

### Special thanks
  - To [sclark39](https://github.com/sclark39) for `format.apptitle` option to format IDE title.
  - To [Christoph Kubisch](https://github.com/pixeljetstream) for glslc improvements.
  - To [Yonaba](https://github.com/Yonaba/) for updated French translation.

### Improvements
  - Added support for nginx debugging (Mobdebug 0.564).
  - Added support for custom debugger initializer (global or interpreter-based).
  - Added line mapping support for debugging Lua-based languages (e.g. moonscript).
  - Added support to force local execution in console by prepending `!` (#326).
  - Added setting proper `arg[0]` value during debugging (fixes #329).
  - Added double click navigation in the Output window for unnamed files.
  - Added centering of line after double click in the Output window.
  - Added `editor.wrapindentmode` and `editor.wrapstartindent` settings.
  - Added a workaround for focus switching between controls on OSX (#89, #327).
  - Added assertion to ensure inserted editor is not in the notebook already.
  - Added `format.apptitle` option to format IDE title (thanks to @sclark39).
  - Added restoring cursor position after sorting/re-indenting.
  - Added `onEditorUserlistSelection` event for userlist selection (#166).
  - Added `onEditorAction` event for cut/copy/paste actions (#166).
  - Added package `GetEditorWithFocus` method (#166).
  - Added `editor.extradescent` option for line spacing (#305).
  - Added centering of line on page after re-loading file with a known position.
  - Added re-indentation of selected fragment or entire file (closes #324).
  - Added sorting of the entire file if nothing is selected.
  - Added `Edit | Source` sub-menu.
  - Added centering line on page after bookmark navigation.
  - Added `GetProjectTree`, `GetWatch`, and `GetStack` package calls (#166).
  - Added bookmark-toggle toolbar icon (#233).
  - Disabled message on failure to read symlinked folder content on Windows.
  - Disabled breakpoint toggling when editor is not in focus.
  - Disabled changing toolbar color with `auxwindow` as it only works for the dropdown.
  - Increase font size for code fragments in markup (#305).
  - glslc: change domain detection to be compatible with file.comp.glsl and file.tese
  - Removed checks for specific errors in Local/Remote console.
  - Removed focus handling workaround for editor tab changes (#89, #327).
  - Renamed `menuformatrecentprojects` to `format.menurecentprojects` (#305).
  - Removed handling of project dropdown in menu as it's no longer needed (#305).
  - Reorganized menu shortcut conflict handling (#233).
  - simplified glslc usage (compile and link based on file extensions)
  - treat unreal shaders as hlsl
  - Updated auto-complete logic to use configured spec separators.
  - Updated logic for populating placeholders in dropdown menus.
  - Updated french translation (thanks to @Yonaba)
  - Updated menu items to stay enabled only when appropriate object has focus.
  - Updated indentation logic for if/elseif/while/for split into 2+ lines (#324).
  - Updated indentation logic to ignore comments (#324).
  - Updated README with supported engines and installation instructions.
  - Updated breakpoint-toggle toolbar icon to better match other icons (#305).
  - Updated bookmark navigation to wrap around (#233).
  - Updating sorting to keep original line endings.
  - Upgraded metalua to v0.7.2.

### Fixes
  - Fixed setting control focus when the main frame is hidden.
  - Fixed loading packages with dashes in filenames (fixes #330).
  - Fixed toolbar to stay shown after failure to start debugging.
  - Fixed focus on the editor after closing a dialog on OSX (fixes #328).
  - Fixed crash on OSX when changing focus while the app is being closed (#327).
  - Fixed some toolbar buttons being enabled with no editor tab open.
  - Fixed toolbar stealing focus after closing floating panels and dropdowns (#327).
  - Fixed restoring control focus when the app gets focus on OSX (fixes #327).
  - Fixed activating editor when starting the app on OSX (#327).
  - Fixed auto-complete to not offer the word the cursor is on.
  - Fixed hiding auto-complete when the only option matches what's typed.
  - Fixed an error when all editor tabs are closed.
  - Fixed replace-in-files when saving backup copy is turned off.
  - Fixed re-indenting of anonymous functions in tables (#324).
  - Fixed `F2` shortcut not working in file tree and watch panel (#233).
  - Fixed debugger compatibility with Lua 5.2 (Mobdebug 0.561).

## v0.60 (May 11 2014)

### Highlights
  - Added support for switching breakpoints at run-time.
  - Added bookmark handling.
  - Added `Detach process` command to stop debugging and continue process.
  - Added detaching debugger server.
  - Added showing/hiding toolbar and status bar.
  - Simplified user interface and updated application icons.
  - Updated love2d API for v0.9.1.
  - Updated Moai API for v1.5.
  - Added `outputshell.usewrap` to set Output wrapping; on by default.
  - Added `editor.wrapflags` to configure line wrapping indicators.
  - Added `editor.foldflags`; set default to draw one line when folded.
  - Added `editor.foldtype` with box, cirle, arrow, and plus types.
  - Added `editor.extraascent` option to add line spacing.

### Special thanks
  - To [bartoleo](https://github.com/bartoleo) for italian translation update.
  - To [riidom](https://github.com/riidom) for german translation update.
  - To [sclark39](https://github.com/sclark39) for Copy Full Path implementation.
  - To [DanielSWolf](https://github.com/DanielSWolf) for Moai API update for v1.5.
  - To [madmaxoft](https://github.com/madmaxoft) for AnalyzeString patch.
  - To [crumblingstatue](https://github.com/crumblingstatue) for Zoom update.
  - To [SiENcE](https://github.com/SiENcE) for notepad++ colorscheme update.

### Improvements
  - Added new italian translations (thanks to @bartoleo)
  - Added Russian translation for new messages (#70).
  - Adding Copy Full Path to editor tabs, and a Clear Output Window option to the Output tab (thanks to @sclark39)
  - Added support for packages in config files (#166).
  - Added formatting for Recent Projects menu (#305).
  - Added `Detach process` command to stop debugging and continue process.
  - Added re/docking of Watch/Stack notebooks on tab background doubleclick (#305).
  - Added bookmark handling (closes #233).
  - Added `Clear items` to the Recent Files menu (ref #305).
  - Added recent files dropdown to the toolbar (ref #305).
  - Added applying new UI settings after upgrade (ref #305).
  - Added toolbar button dropdown with recent projects (ref #305).
  - Added `Choose Project Directory` to the toolbar (ref #305).
  - Added floating/docking of notebooks on tab background doubleclick (ref #305).
  - Added Recent Project menu refresh after switching projects (ref #305).
  - Added setting project directory by renaming the filetree root element (#305).
  - Added filetree popup menu with the list of projects (ref #305).
  - Added 'Recent Projects' menu (ref #305).
  - Added package `GetLaunchedProcess` call (ref #166).
  - Added `IsRunning` and `IsConnected` API calls for the debugger (ref #166).
  - Added `editor.wrapflags` to configure line wrapping indicators (ref #305).
  - Added explicit sorting of files in the filetree.
  - Added showing/hiding of the status bar (ref #305).
  - Added auto-showing toolbar when debugging starts (ref #305).
  - Added showing/hiding of the toolbar (ref #305).
  - Added `outputshell.usewrap` to set Output wrapping; on by default (ref #305).
  - Added `editor.foldflags`; set default to draw one line when folded (ref #305).
  - Added `editor.extraascent` option to add line spacing (ref #305).
  - Added explicit conversion to number for numeric settings.
  - Added `editor.foldtype` with box, cirle, arrow, and plus types (ref #305).
  - Added opening a new tab on double click on tab background (ref #305).
  - Added ActivateItem method to the filetree API (ref #166).
  - Added onFiletree* package events (ref #166).
  - Added setting margin mask to allow for different margin order.
  - Added support for switching breakpoints at run-time (closes #288).
  - Added stopping debugging when debugger server is detached/stopped.
  - Added opening file on one-click in icon/padding area in the filetree.
  - Added AnalyzeString function (thanks to @madmaxoft).
  - Added zooming for Output/Console windows (ref #290).
  - Added IDs for Zoom menu items (ref #290).
  - Add zoom actions with appropriate keyboard shortcuts to View menu (thanks to @crumblingstatue)
  - Added detaching debugger server.
  - Added skipping reporting for known globals in static analysis (closes #286).
  - Added support for running zbstudio script from any folder on OSX.
  - Adjusted `code` color in the comment markup for better visibility (#305).
  - Changed order of stopping debugger and saving settings (ref #305).
  - Cleaned unused variables and functions based on static analysis.
  - Disallowed closing Output/Console/Project tabs (fixes #310).
  - Disabled current project on the recent projects list (ref #305).
  - Disable function call indicator by default to reduce clutter (ref #305).
  - Disabled startng multiple find-in-files searches.
  - Disabled editing/dragging of the project directory in the filetree.
  - Enabled editor width auto-adjustment when wrapping is off.
  - Enable retina support (`hidpi=true`) by default on OSX (#305).
  - Increased default font size in the editor (ref #305).
  - Increased wait time for Gideros player to start for more reliable launching.
  - Made fold and marker margins wider (ref #305).
  - Made jump-to-line in the Output window to work faster and more reliably.
  - Moved `Project Directory` menu item lower to not activate on OSX (ref #305).
  - Moved code to populate `wx` and `wxstc` descriptions to API files.
  - Rearranged global functions in lua spec for simpler grouping (ref #79).
  - Reduced sash (border between subsections) in all notebooks (ref #305).
  - Reduced the line number margin width and default font size (ref #305).
  - Refactored editor config references.
  - Removed `Clear Dynamic Words` menu as it's rarely used.
  - Removed the gripper on the toolbar (ref #305).
  - Removed project selection dropdown from the filetree (ref #305).
  - Removed paragraph conversion from love2d API conversion script (ref #247).
  - Removed border around Output/Console panels (ref #305).
  - Removed deprecated `startfile` interpreter option.
  - Removed explicit margin numbers to make configuraton simpler.
  - Removed border around editor components.
  - Reordered markers to keep the curent line marker on top (#305).
  - Reorganized and updated configuration examples.
  - Set def linenumber font size as one smaller than editor font size (ref #305).
  - Switched to plain background for the toolbar (ref #305).
  - Switched to AuiToolBar as it provides buttons with dropdowns (ref #305).
  - Upgraded Mobdebug (0.56).
  - Upgraded debugger (mobdebug 0.553) to fix an issue with STEP command.
  - Upgraded copas to the current version (v1.2.1).
  - Updated default fonts for Windows and Linux for better looking ones (#305).
  - Update de.lua (thanks to @riidom)
  - Updated language files with new messages (#70).
  - Updated copyright messages.
  - Updated `Go To Line` menu item and its translations.
  - Updated build scripts with a fix for a wxlua compilation issue (#260).
  - Updated build prerequisites Linux install script.
  - Updated default indicator color to more neutral one (#305).
  - Updated OSX build script to use 10.7 SDK with 10.6 min-version (#260).
  - Updated Mobdebug (0.555) to add support for `pause` debugger call.
  - Updated lua interpreter to remove caching of executable path.
  - Updated resetting pid only after completing non-debbugged process.
  - Updated shortcut for Recent File navigation (ref #305).
  - Updated application icons (ref #305).
  - Updated stack/watch panel captions to be invisible (ref #305).
  - Updated interpreters to check `ProgramFiles` env variable on Windows.
  - Updated panel captions to be invisible (rev #305).
  - Updated 'window unhide' logic (Windows only) to be called less frequently.
  - Updated love2d interpreter to not hide the application window.
  - Updated file sorting to be case-insensitive on all platforms.
  - Updated filetree menu to use 'Edit Project Directory' on root element (#305).
  - Updated love2d API to fix typos in descriptions (ref #247).
  - Updated love2d API for v0.9.1 (ref #247).
  - Updated love2d API conversion script to handle top-level functions (ref #247).
  - Updated `PackageUnRegister` call to return the package on success (ref #166).
  - Updated fold/wrap flag handling to work with wxwidgets 2.8 (ref #305).
  - Updated breakpoint/currentline markers for less contrast colors (ref #305).
  - Updated default folding to use lighter colors (ref #305).
  - Updated default colors to have less contrast (ref #305).
  - Updated Open file dialog to use current file or project location (closes #303).
  - Updated Moai API for v1.5 (thanks to @DanielSWolf).
  - Updated `autoanalyzer` option to more common spelling (analizer -> analyzer).
  - Updated auto-complete to show in IDLE event for smoother processing.
  - -minor color changes to notepad++ colorscheme (thanks to @SiENcE).

### Incompatibilities
  - Added opening a new tab on double click on tab background.
  - Added re/docking of Watch/Stack notebooks on tab background doubleclick.
  - Enabled retina support (`hidpi=true`) by default on OSX.
  - Removed deprecated `startfile` interpreter option; use `startwith` option instead.
  - Updated file sorting to be case-insensitive on all platforms.
  - Updated `autoanalyzer` option to more common spelling (analizer -> analyzer).
  - wxwidgets 2.8 is no longer supported (wxwidgets 2.9.5+ required).

### Fixes
  - Fixed Corona interpreter to clean debugger in `plugin` folder (Win).
  - Fixed file tree activation of a deleted file.
  - Fixed switching to full screen and restoring status bar on OSX (ref #305).
  - Fixed right-click handling in filetree on OSX broken by 3709f61f (ref #166).
  - Fixed usage of `self` in one of package API calls (ref #166).
  - Fixed find dialog to stay on top after search directory selection on OSX.
  - Fixed search result navigation after clicking beyond the end of line.
  - Fixed an issue with running processes not terminated when closing IDE.
  - Fixed an error after manual canceling Find-in-files dialog.
  - Fixed an issue with deleting column-based selection (fixes #300).
  - Fixed an error in variable indicator processing.
  - Fixed looping when `valuetype` creates self-reference in APIs (ref #297).
  - Fixed `elseif` auto-indentation (fixes #294).
  - Fixed focus for Find field in the find dialog on some instances of OSX.

## v0.50 (Mar 10 2014)

### Highlights
  - Fixed opening files and launching on OSX 10.6.x and 10.9.1+.
  - Improved CPU utilization when idle on OSX.
  - Added handling of command-line parameters.
  - Implemented various auto-complete and tooltip improvements.
  - Updated Love2d API for 0.9.0.
  - Updated Corona API auto-complete/description to match v2014.2189 (G2.0).
  - Updated Marmalade Quick API for v7.2.
  - Updated French, German, Italian, and Russian translations.

### Special thanks
  - To [Asmageddon](https://github.com/Asmageddon) for fixed launching zbstudio from folders with spaces.
  - To [Christoph Kubisch](https://github.com/pixeljetstream) for various luxinia2 fixes.
  - To [Yonaba](https://github.com/Yonaba/) for updated French translation.
  - To [riidom](https://github.com/riidom/) for updated German translation.
  - To [bartoleo](https://github.com/bartoleo/) for updated Italian translations.

### Improvements
  - Added explicit focus for Find field in the find dialog on OSX.
  - Added version dependency check for loaded plugins.
  - Added `auxwindow` attribute to style auxiliary windows.
  - Added sha2 library to provide sha256 hashing.
  - Added package GetRootPath/GetPackagePath calls (ref #166).
  - Added package FindMenuItem API call (ref #166).
  - Added API call to analyze one file.
  - Added restoring markers after external modification and file reload.
  - Added displaying number of selected characters and instances (closes #274).
  - Added using safe load for external data instead of loadstring.
  - Added check for editor state during Edit menu display (ref #70).
  - added italian translations; thanks to [bartoleo](https://github.com/bartoleo/).
  - Added Russian translation for new items in the main menu (ref #70).
  - Added package GetApp method (ref #166).
  - Added package GetDebugger API call (ref #166).
  - Added setting project directory passed as a parameter (second instance).
  - Added Shift+Zoom to zoom all editors (closes #269).
  - Added `alpha` setting for sel/seladd/caretlinebg styles.
  - Added package GetToolBar API call (ref #166).
  - Added `seladd` setting for styling additional selections.
  - Added `Select and Find Next/Prev` (closes #268).
  - Added showing search dialog for Quick Find on first search (closes #265).
  - Added `nomousezoom` option for Console/Output windows (closes #266).
  - Added error reporting when debugger server fails to start (closes #263).
  - Added support for command line parameters for GSL-shell (ref #251).
  - Added support for editor shortcuts and included standard OSX ones (closes #252).
  - Added auto-complete for metamethods (closes #256).
  - Added Minimize shortcut on OSX (closes #254).
  - Added handling of command line parameters for love2d (ref #251).
  - Added handling of command line parameters (closes #251).
  - Added calling of end callback for detached processes.
  - Added skipping compile check for non-lua files in scratchpad.
  - Added handling of (optional) BOM in UTF-8 encoded files (closes #246).
  - Added hint about removing backslash for invalid escape sequences.
  - Adjusted tooltip position and content to always fit on the screen.
  - allow tool exe paths to be set in config prior load
  - Changed the order of applying styles to apply the main style last.
  - Disabled singleinstance check by default on OSX (ref #204).
  - Disable debugging termination when stack trace received from remote process.
  - glsl: added imageSize to spec and api
  - luxinia2 define some global vars if debugger is present
  - luxinia2 support 32 and 64 bit runtime
  - luxinia2: new setup, added support for debugging with main.lua and rudimentary scratchpad functionality
  - Reduced the number of inactivity checks and timer calls (ref #204).
  - Removed `calltipbg` attribute as it's replaced by `calltip.bg`.
  - Removed unused files from metalua.
  - Removed paragraph-to-newline conversion for API tooltips.
  - Resolved conflict for Opt+Left (ref #252 and #203).
  - Removed jumping to the current line after compilation errors.
  - Switched to using temp files instead of -e option for debugging (ref #251).
  - shader tools: allow relative directories for binaries
  - shader specs: add isfncall definition to allow indication style
  - Updated AddConfig/RemoveConfig to refresh styles after changes (ref #166).
  - Updated Marmalade Quick API for v7.2.
  - Updated Corona API for v2014.2189.
  - Updated scheme picker to apply styles to all windows.
  - Updated LfW interpreter to use project directory as current one (fixes #276).
  - Updated README.
  - Updated comment markup separators to apply markup in fewer cases.
  - Updated French translation; thanks to [Yonaba](https://github.com/Yonaba/).
  - Update de.lua; thanks to [riidom](https://github.com/riidom/).
  - Updated Linux scripts to remove custom libpng/zlib as it's fixed in wxwidgets.
  - Updated check for editor state during Edit menu display (ref #70).
  - Updated Corona API auto-complete/description to match v2013.2100 (ref #73).
  - Updated all language files with new messages (ref #70).
  - Updated messages for better translation (ref #70).
  - Updated love2d api with LineJoin and few other calls (ref #247).
  - Updated OSX build script for wxwidgets 3.x (ref #260).
  - Updated build script to compile wxwidgets 3.x on Windows (ref #260).
  - Updated love2d api with some missing calls (ref #247).
  - updated luxinia2 interpreter to reflect new luxinia2 structure
  - Updated Love2d API for 0.9.0. (closes #247).
  - Upgraded Mobdebug (0.551) to fix serialization of table/array indexes.
  - updates to shader apis (bugfix in GLSL atomic description)

### Incompatibilities
  - Updated LfW interpreter to use project directory as current one (fixes #276).
  - Removed `styles.calltipbg` as it is replaced by `styles.calltip.bg`.

### Fixes
  - Fixed shortcut menu generator to display default shortcuts.
  - Fixed removing focus from editor when IDE loses focus on OSX (ref #204).
  - Fixed hiding calltip and auto-complete when switching between tabs on OSX.
  - Fixed handling of getenv returning general message (Mobdebug v0.5511).
  - Fixed launching zbstudio from folders with spaces; thanks to @Asmageddon.
  - Fixed `calltip` attribute to use/enable proper style.
  - Fixed visibility of wrapped lines in console when going through history.
  - Fixed syntax issues in definitions of IDE tables.
  - Fixed an issue in metalua files when syntax error is reported.
  - Fixed arrow key handling in Local console (fixes #279).
  - Fixed removing temporary files in GSL-shell interpreter.
  - Fixed tooltip positioning for long messages.
  - Fixed current line in debugging after activation of files with wrapped lines.
  - Fixed spurious ESC after activation on Windows when modifiers are pressed.
  - Fixed skipping empty lines in tooltip formatting.
  - Fixed comment markup at the end of a file.
  - Fixed formatting calculations for tooltip to better fill the window.
  - Fixed stopping debugging when switching projects with the same interpreter.
  - Fixed auto-complete for classes with more than two levels.
  - Fixed removal of paragraph breaks in tooltips after interpreter switch.
  - Fixed API reloading that caused removal of paragraph breaks in tooltips.
  - Fixed translations for stock menu items on Ubuntu 13.10 (ref #70).
  - Fixed an issue with spec/tools/interpreters filters not working from config.
  - Fixed messages script to work with LuaJIT.
  - Fixed console output with multiple new lines at the end.
  - Fixed issues on OSX 10.6.x and 10.9.1+ caused by flat namespace (fix #270, fix #264).
  - Fixed an issue with `isfncall` spec property not being checked.
  - Fixed function localization in menu handlers.
  - Fixed default selection for search in case of multiple selections.
  - Fixed dependency of lfs/git dlls on lualib.dll.
  - Fixed an issue with activating proper tab after dragging.
  - Fixed displaying local console output with invalid unicode characters.
  - Fixed displaying script output with invalid unicode characters.
  - Fixed drawing artifacts on Windows when line wrapping disabled (fixes #250).
  - Fixed setting bom value for a new editor (fixes #258).
  - Fixed auto-complete for values returned by 'core' functions (ref #256).

## v0.40 (Dec 14 2013)

### Highlights
  - Added LuaDist integration.
  - Added live coding support for GSL-shell.
  - Added support for project settings.
  - Added filetree operations.
  - Added Busted interpreter.

### Special thanks
  - To Jayanth Acharya for SciTeLuaIDE color scheme.
  - To Mike Richards for adding edge handling and styles.
  - To [adamdmoss](https://github.com/adamdmoss) for Mobdebug API update.

### Improvements
  - Added live coding support for GSL-shell (closes #239).
  - Added support for product-specific links in the Help menu.
  - Added 'edge' style to the color schemes (ref #237).
  - Added ability to set 'edge' style properties individually (ref #237).
  - Add edge to styles
  - Add fold margin checker color to styles
  - Add edge line and fold margin checker color
  - Added changing directory when launching on Linux (closes #157).
  - Added setting PATH for LfW to allow loading of DLL dependencies.
  - Added logic to set architecture dynamically for luadist (ref #225).
  - Added luadist bootstrap dependencies for Linux (ref #225).
  - Added option to load luadist as a module (ref #225).
  - Added luadist bootstrap dependencies for OSX (ref #225).
  - Added proxy dll for Lua 5.2 (ref #225).
  - Added luadist bootstrap dependencies for Windows (ref #225).
  - Added package GetInterpreters method (ref #166, #225).
  - Added package AddConsoleAlias/RemoveConsoleAlias methods (ref #166, #225).
  - Added version property to the Lua interpreters.
  - Added new location for Marmalade Quick v7+ and s3e path logic (fixes #226).
  - Added directory creation (if needed) to file saving.
  - Added support for symlinks in the filetree (with recursion protection).
  - Added package AddConfig/RemoveConfig methods (ref #166).
  - Added package GetProject method (ref #166).
  - Added package onProjectPreLoad method (ref #166).
  - Added workaround for conflict with Scintilla shortcuts on Linux.
  - Added 'Open with Default Program' to file tree operations (ref #123).
  - Added toggling directory on Enter (ref #123).
  - Added 'Copy Full Path' to file tree operations (ref #123).
  - Added deleting file/directory to file tree operations (ref #123).
  - Added processing of packages from `$HOME/.zbstudio/packages` folder (#166).
  - Added 'New File' and 'New Directory' to file operations (ref #123).
  - Added error reporting for failed rename operations (ref #123).
  - Added re-opening editor tabs affected by directory move/rename (ref #123).
  - Added package FindDocumentsByPartialPath method (ref #166).
  - Added existing file overwrite confirmation when renaming (ref #123).
  - Added existing file overwrite confirmation when saving.
  - Added creating intermediate directories during file rename (ref #123).
  - Added in-place editing of file and folder names (ref #123).
  - Added refreshing editor tab after drag-n-drop operation (ref #123).
  - Added drag-n-drop operation to move files in the project tree (ref #123).
  - Added package AddMarker/RemoveMarker methods (ref #166).
  - Added package GetStatusBar method (ref #166).
  - Added package GetDocuments and document methods (ref #166).
  - Added `EscapeMagic` function to escape magic characters.
  - Added SciTeLuaIDE color scheme (thanks to Jayanth Acharya).
  - Changed glslc option to reflect new version
  - Disabled compilation check for scratchpad when `skipcompile` is set (ref #239).
  - Disabled output activation for messages redirected to Console (ref #225).
  - Disabled commenting for file types that don't specify line comments.
  - Moved restoring project before loading files (ref #107).
  - Reorganized loading configuration files (ref #107).
  - Removed 'file no longer exists' message after moving opened files (ref #123).
  - Removed some of the snippets as they are available as plugins.
  - Store os specific clibs path to make it available to plugins.
  - Tidy up estrela tools a bit (remove key strokes from cgc, remove luxinia res viewer, rename perforce files to get loaded again)
  - Updated README.
  - Updated samples with missing indicator constants (closes #243).
  - Updated OSX executables and added 'fake' proxy for lua-dist support (ref #225).
  - Updated handling of case-insensitive names during debugging on OSX (Mobdebug v0.545).
  - Updated package onEditorPreSave to be called on SaveAs events (ref #166).
  - Updated icon bundle to eliminate large icons.
  - Updated application icon to a bit brighter one on OSX (closes #196).
  - Updated build script on OSX to not require 10.6 SDK (closes #231).
  - Updated menu definitions for consistency.
  - Updated use of `unpack` for consistency and Lua 5.2 compatibility.
  - Updated 'Open with Default Program' on Windows to work with spaces in names (#123).
  - Updated cmake installation script to install to '/Applications' on OSX.
  - Updated OSX build script to revert wxwidgets commit to fix auto-complete crash.
  - Updated `Start debugging` hint to clarify.
  - Updated single-click toggle to allow rename/move directories (ref #123).
  - Updated normalization flags as some path parts were changed to dots.
  - Updated editor tab processing using FindDocument method.
  - Updated shortcut for Replace All to avoid conflict on OSX (fixes #220).
  - Updated `SetDocumentModified` to use current tab text.

### Fixes
  - Fixed values 'captured' by redirected 'print' and not collected (fixes #240).
  - Fixed typo in fold style definition (ref #237).
  - Fixed console output being limited in addition to stack result limit.
  - Fixed hang in auto-complete on expressions involving '...' (fixes #235).
  - Fixed auto-complete for string values (broken by 933aacc2).
  - Fixed crash when LUA_DEV environmental variable is not set (fixes #228).
  - Fixed cmake cache filename (ref #225).
  - Fixed incorrect UTF-8 sequence in UTF-8 validity check.
  - Fixed Un/Comment menu for Output/Console windows.
  - Fixed format of the file name reported after compilation errors.
  - Fixed jumping to compilation error (if any) after Run/Debug.
  - Fixed disabling 'Open with Default Program' on OSX (ref #123).
  - Fixed file tree update after changes on OSX (ref #123).
  - Fixed copying full path on OSX (ref #123).
  - Fixed 'Open with Default Program' for paths with spaces on Windows (ref #123).
  - Fixed folding issue (caused by `math.mod` not available in LuaJIT).
  - Fixed debugger marker calculations to avoid marker conflicts.
  - Fixed color references in marker config examples.
  - Fixed Step Over/Out to stay in the same coroutine; Mobdebug 0.543 (closes #217).
  - Fixed case sensitivity in matching of file name in error messages (fixes #216).
  - Fixed tab text after SaveAs and loading files into the same tab.

## v0.39 (Oct 06 2013)

### Highlights
  - Added Lua 5.2 support out of the box.
  - Added suggesting dynamic words as fields in auto-complete.
  - Added 'go to definition' (Ctrl/Cmd+Alt+Click) and 'jump to previous location' (Alt+Left).
  - Added abbreviation of project directories to keep unique parts visible.
  - Fixed breakpoints with Marmalade Quick.
  - Switched to using LuaJIT interpreter by default.
  - Upgraded Luasocket (3.0-rc1), copas, and coxpcall libraries.

### Special thanks
  - To Chow CheeWen for Chinese translation.
  - To [Enrique García](https://github.com/kikito) for fixing `fixutf8` function.
  - To [Riidom](https://github.com/Riidom) for German translation.
  - To [ardente](https://github.com/ardente) for user home patch for Windows and separating Lua 5.1 and 5.2 paths in user config.
  - To [Mika Attila](https://github.com/crumblingstatue) for code folding patch.
  - To [Tim Mensch](https://github.com/TimMensch) for auto-save, auto-reload, and debugger improvements.
  - To [Florian](https://github.com/SiENcE) for Notepad++ color scheme.
  - To [Michal Kottman](https://github.com/mkottman) for 'go to definition' and Alt+Left navigation patch.
  - To [Christoph Kubisch](https://github.com/CrazyButcher) for dx11 and glslc updates.
  - To [jpoag](https://github.com/jpoag) for improved activation during debugging on Windows.

### Improvements
  - Added setting project directory when passed as a parameter.
  - Added activation of windows with SDL_app class name (Moai support).
  - Added support for joining/splitting Watch/Stack with Output/Console tabs.
  - Added package GetSetting method (ref #166).
  - Added selected index to package onMenuEditorTab event (ref #166).
  - Added activation of windows with FREEGLUT class name (Moai support).
  - Added hiding console window for Corona (2013.8.28+) applications (Windows).
  - Added suggesting dynamic words as fields in auto-complete.
  - Added socket.connect for compatibility with socket.core <3.0 (fixes #208).
  - Added recalculating line number margin width after zooming (fixes #207).
  - Added margin constants and removed unused variables.
  - Added reporting of socket error for initial debugger calls (Mobdebug 0.5403).
  - Added error handling/reporting for `debugger.outputfilter`.
  - Added 'debug' option to OSX build script.
  - Added ability to modify exe path in base interpreter (ref #197).
  - Added package GetEditorNotebook method (ref #166).
  - Added 'molokai' color scheme (ref #200).
  - added hlsl spec and basic api (note: currently autocomplete doesn't work on object functions, need fix). also fixed cg syntax lexer settings
  - Added file activation for abbreviated file names in error messages.
  - Added abbreviation of project directories to keep unique parts visible.
  - Added `debugger.redirect` configuration option.
  - Added `editor.saveallonrun` configuration option.
  - Added package GetOutput method (ref #166).
  - Added package onAppLoad/onAppClose events (ref #166).
  - Added package onIdleOnce event (ref #166).
  - Added manifest to the Windows executable and re-signed.
  - Added Notepad++ color scheme (thanks to Florian/SiENcE; closes #193).
  - Added clearing document styles after saving file with a different extension.
  - Added workaround to avoid crash on OSX after `Close All Pages` (closes #190).
  - Added return type for string.* functions to assist in auto-complete (ref #189).
  - Added handling of string literals in type assignments (closes #189).
  - Added support for captures in regexp replacement (\1-\9).
  - Added ability to cancel FindInFiles search by closing search dialog (ref #162).
  - Added activating Output window before showing search results (ref #162).
  - Added support for packages from different platforms to co-exist (ref #166).
  - Added ability to save complex data in package settings (ref #166).
  - Added support for multiple inheritance in auto-complete (ref #101).
  - Added ability to add/remove API descriptions from plugins (ref #166).
  - Added package GetSettings/SetSettings methods (ref #166).
  - Added methods to save/restore package settings (ref #166).
  - Added ability to add/remove specs from plugins (ref #166).
  - Added ability to add/remove interpreters from plugins (ref #166).
  - Added wxlua patch for twoface compatibility.
  - Added `setfenv` for Lua 5.2 compatibility.
  - Added links to project page and documentation (closes #180).
  - Added German translation (thanks to Riidom; ref #70).
  - Added default value to package config (ref #176).
  - Added handling of ?51.dll and required DLLs for LuaForWindows interpreter.
  - Added plugin::GetConfig method (ref #166).
  - Added erasing current line in Console (ref #173).
  - Added search/completion in the local and remote console (closes #173).
  - Added package onAppFocus* events (ref #166; ref #172).
  - Added Chinese translation (thanks to Chow CheeWen; ref #70).
  - Added `editor.autoreload` to enable reload of updated files (ref #172).
  - Added creating (missing) folders when saving a file (fixes #171).
  - Added an example of styling individual keywords.
  - Added fold indication of a current block (ref #168).
  - Added reporting of process id for a conflicting process.
  - allow to define separate lua 5.1 and 5.2 paths in user config
  - Changed 'go to definition' to Ctrl/Cmd+Alt+Click (ref #203).
  - Changed `un/comment` to act from the beginning of the line for multi-line selection.
  - Disabled refreshing Watch/Stack windows when they get focus.
  - Disabled markup styling for specs without comment styles.
  - Disabled showing tooltip when auto-complete suggestions are shown.
  - Disabled error reporting after debugging has been terminated.
  - Disabled 'Fold' menu instead of removing when no folding is allowed (ref #169).
  - dx11 and glslc updates
  - Enabled editing watches with doubleclick or Enter.
  - Enable Ctrl+Click and Alt+Left navigation on local variables
  - Enabled support for xml/html folding.
  - Enabled path remapping for local debugging.
  - Enabled slower and more thorough static analysis (ref #149; ref #168).
  - Improved file/debugger activation on Windows (ref #199).
  - Improved IDE activation during debugging on Windows (closes #199); thanks to jpoag.
  - Improved logic to jump to file/line indicated in error messages.
  - Limited activation of code fragments to the beginning of debugging session.
  - Make code folding optional (thanks to [Mika Attila](https://github.com/crumblingstatue))
  - Moved 'Sort' menu to 'Edit'.
  - OpenGL 4.4 and ARB extensions added as well as NV_gpu_shader5 functions
  - Optimized handling of large tables in stack results.
  - Optimized line count calculation for dynamic words when text is deleted.
  - Optimized dynamic word processing for large files.
  - Reduced CPU usage while idle (ref #204, #206).
  - Renamed package onEditorPostSave event to onEditorSave for consistency (ref #166).
  - Removed comment from default spec as it forces undesired markup styling.
  - Removed auto-complete suggestion when it is already typed (ref #101).
  - Reorganized auto-complete handling; should fix #164.
  - Reorganized path separator handling to minimize use of global variables.
  - Reorganized API processing to allow loading API description from a plugin.
  - Replaced package onEditorActivated event with onEditorFocus* events (ref #166).
  - Set search in subdirectories as default in Find in Files dialog (ref #162).
  - Switched to using POSIX compatible regexp with '()' used for captures.
  - Updated LICENSE information.
  - Updated Windows build file for wxwidgets 3.0.
  - Updated support for MOAI coroutine debugging (Mobdebug 0.541).
  - Updated type assignment logic to remove spurious types.
  - Updated Windows build script to enable gdb debugging.
  - Updated OSX build script to not strip debug builds.
  - Updated Corona interpreter to handle failure to copy debugger to Resources/ folder.
  - Updated build scripts with proper INSTALL_PREFIX option.
  - Updated CFBundleIdentifier in plist files to allow references from OSX programs.
  - Updated un/comment to toggle selection as a group rather than line by line.
  - Updated `NewFile` to accept a file name.
  - Updated 'get hostname' logic to avoid using non-resolvable names (mostly on OSX).
  - Updated tooltip to use the same type inference as auto-complete (ref #101).
  - Updated Estrela reference in README.
  - Updated build script on Windows to enable debugging (ref #164).
  - Updated build script with a (temporary) fix for wxlua issue (mingw32).
  - updated glewgl api for OpenGL4.4 and removed non-core duplicate functions/enums
  - Updated static analyzer to report only first instance of 'unknown field'.
  - Updated filename/source code heuristic in the debugger (Mobdebug 0.5362).
  - Updated `SaveAll` to allow saving (only) files with filenames (ref #172).
  - Upgraded copas and coxpcall libraries (closes #144).
  - windows: adopt native user home (thanks to [ardente](https://github.com/ardente))

### Fixes
  - Fixed Lua 5.2 crash on OSX (added -O1).
  - Fixed onInterpreterLoad to only be called when interpreter changes.
  - fix the fixutf8 function (thanks to Enrique García).
  - Fixed handling of source code fragments in the Stack view.
  - Fixed Watch/Stack windows to refresh when shown.
  - Fixed incorrect editor tab acted upon in split notebook situations.
  - Fixed auto-complete suggestions for indentifiers matching partial function names.
  - Fixed hiding launched windows when running/debugging (Windows).
  - Fixed showing known functions in auto-complete.
  - Fixed showing output with invalid UTF8 characters in Stack and Console windows.
  - Fixed debugging on/off handling in 'main' thread for LuaJIT (MobDebug 0.5402).
  - Fixed having duplicate tabs after SaveAs with existing file name.
  - Fixed showing redirected 'print' messages after debugging is terminated.
  - Fixed using default interpreter when no interpreter is selected.
  - Fixed stepping through blocks with undefined variables when 'strict' is in effect (upgraded Mobdebug to 0.5401).
  - Fixed loading of files with incorrect UTF-8 encoding and control characters (fixes #198).
  - Fixed package sample to take into account new documents.
  - Fixed crash on OSX after opening 'application' in 'Open File' dialog.
  - Fixed windows resource file to properly reference the manifest.
  - Fixed missing default api for files with unknown extensions.
  - Fix spurious replacement after 'search, clear selection, replace' actions.
  - Fixed using auto-complete with multiple selections (fixes #188).
  - Fixed looping in auto-complete with array references (ref #143).
  - Fixed showing auto-complete after comma.
  - Fixed 'cannot get official hostname' message on low privilege accounts (fixes #183).
  - Fixed displaying variable instances when code has invalid blocks (fixes #182).
  - Fixed tooltip to ignore string parameters (ref #101).
  - Fixed tooltip display between empty brackets (ref #101).
  - Fixed indentation after lines with brackets in strings.
  - Fixed indentation after lines with anonymous functions.
  - Fixed indicator showing at the end of not terminated long comment.
  - Fixed an issue with LUA_DEV including files instead of directories.
  - Fixed project switching to close all files when switching to a subdirectory.
  - Fixed saving projects that do not have any open files.
  - Fixed debugger to accept filenames without '@'; thanks to Tim Mensch (closes #174).
  - Fixed sorting when the sorted fragment ends with a newline.

## v0.38 (Jun 21 2013)

### Highlights
  - Added source code debugging (to support LuaJava and other engines).
  - Added scope-aware global/local/masked/masking variable highlighting.
  - Added 'Go To Definition' and 'Rename All Instances'.
  - Added package/plugin API.
  - Added Retina display support (can be enabled with `hidpi` option).
  - Improved auto-complete API with inheritance and table index support.

### Special thanks
  - To George Pimm for line of code navigation in the Stack Window.
  - To Fringale for updated French translation.
  - To Tom Burgel for LuaForWindows interpreter.

### Improvements
  - Added sorting of file lists on Linux (used in the filetree and file search).
  - Added LuaForWindows interpreter (thanks to Tom Burgel).
  - Added package onEditorCharAdded/onEditorKeyDown events (ref #166).
  - Added support for changing the Corona simulator skin (closes #151).
  - Added inheritance support for auto-complete API.
  - Added package onEditor* events (closes #166).
  - Added package onInterpreterLoad/onInterpreterClose events (ref #166).
  - Added package onProjectLoad/onProjectClose events (ref #166).
  - Added package onMenu* events (ref #166).
  - Added package onRegister/onUnRegister events (ref #166).
  - Added 'Show Location' to the Project/Filetree menu.
  - Added `hidpi` option to enable HiDPI/Retina display support (closes #160).
  - Added breakpoint support for unnamed code fragments.
  - Added support for debugging (stepping through) unnamed code fragments.
  - Added LuaSec to win32 build script.
  - Added package/plugin processing.
  - Added support for 'silent' execution of shell commands in markup.
  - Added `Find Next/Previous` over selected variable instances (ref #163).
  - Added `debugger.allowediting` option to allow editing while debugging.
  - Added skiping binary files during file search (ref #162).
  - Added yield to update search results during file search (ref #162).
  - Added showing default extensions in the file search (ref #162).
  - Added support for multiple file extensions in the file search (ref #162).
  - Added saving folder to search files in (ref #162).
  - Added selecting all instances with a double-click on a variable.
  - Added `autoanalizer` option for dynamic static analysis.
  - Added 'Go To Definition' and 'Rename All Instances'.
  - Added initial support for indicating local/global variables.
  - Added showing tooltip in any position over a term and showing 'values' (ref #101).
  - Added disabling tooltip when context menu is shown.
  - Added 'fixing' path returned by wxDirDialog; may be incorrect in 2.9.x.
  - Added constant initialization missing on ArchLinux with wxlua 2.8.12.2 (fixes #155; ref #128).
  - Added support for table valuetypes in auto-complete for `foo[index]:` (ref #101).
  - Added navigation to the line of code in the Stack Window (thanks to George Pimm; closes #134).
  - Added `Show Location` to the editor tab menu.
  - Added support for absolute filenames in Markdown links.
  - Allowed tab width and indentation to be set independently.
  - Allowed closing editor tabs while debugger is running.
  - Disabled following symlinks during directory scanning to avoid infinite loops.
  - Disabled showing tooltip when the app is in the background (fixes #158).
  - Disabled 'value' tooltip over variables that match known function names (ref #101).
  - Improved error reporting in interpreters on failures to copy the debugger.
  - Improved focus on the debugger when a breakpoint hits on OSX (fixes #141).
  - Removed sorting in the Stack view to keep the order of table elements.
  - Set default directory for Find in Files opened from a new file (#ref 162).
  - Updated configuration example for variable indicators.
  - Updated Stack view navigation to use clicked on instead of active item.
  - Updated Gideros API/auto-complete reference.
  - Updated markup to allow opening files from new buffers ('untitled').
  - Updated build scripts to use luasocket 2.0.3.
  - Updated linux build files to use latest zlib/libpng to fix png load on Gentoo.
  - Updated 'Find in Files' to start in the project folder by default.
  - Updated French translation with latest string changes (mostly for find/replace dialogs).
  - Updated Marmalade Quick auto-complete API to use class inheritance.
  - Updated Gideros auto-complete API to use class inheritance.
  - Upgraded Mobdebug to 0.534 for debugging of source code fragments.

### Fixes
  - Fixed looping in auto-complete when indexes are used (fixes #143).
  - Fixed possible duplicate paths in the filetree.
  - Fixed missing numerical keys in serialized tables (upgraded Mobdebug to 0.535).
  - Fixed styling of markup that can be affected by folded lines.
  - Fixed value selection with multiple active selections.
  - Fixed style compatibility with wxwidgets 2.8 (ref #128).
  - Fixed error reporting by (internal) file operations.
  - Fixed styling comments that start with markup symbols.
  - Fixed restoring a session with one of the files deleted on disk (fixes #161).
  - Fixed reporting variable instances for comment/string fragments.
  - Fixed 'Rename Instances' to always reset previous selection.
  - Fixed auto-complete error for '%dddd' strings (fixes #156).
  - Fixed an issue with showing filenames that include '"?*:<>' on OSX/Linux.
  - Fixed current line marker being shown using 'Run as Scratchpad' with 'runonstart' option.
  - Fixed looping in auto-complete (fixes #151).
  - Fixed incorrect localization that led to an error in 'Save' from tab menu.

## v0.37 (May 09 2013)

### Highlights
  - Added Marmalade Quick auto-complete support and API documentation.
  - Added full Marmalade Quick debugging support (requires Quick 1.1+).
  - Improved Find/Replace behavior and functionality.
  - Added Recent File history navigation.
  - Added Preferences menu to simplify access to system/user settings.

### Special thanks
  - To Samuel Dionne-Riel for wxwidgets 2.8 compatibility updates.
  - To Mat Hopwood for assistance with Marmalade Quick integration.

### Improvements
  - Added Preferences menu to simplify access to system/user settings.
  - Added Russian translation for Find/Replace dialog and (ref #70).
  - Added Russian translation for the Preferences menu (ref #70).
  - Added 'shaking' Find/Replace window when text is not found (closes #146).
  - Added 'wlua' to the list of recognized Lua extensions.
  - Added disabling Recent Files menu if the list is empty.
  - Added TomorrowContrast color scheme (thanks to Sergey Lerg).
  - Added detaching a child process to avoid crash when exiting during debugging.
  - Added Recent File history navigation (closes #66).
  - Added Marmalade auto-complete support and API documentation.
  - Added processing of `runonstart` when using remote debugging (closes #138).
  - Added suggesting proper extension after 'Save/Save As' based on current spec.
  - Added translation setup for Find/Replace dialog (closes #133).
  - Added `nomousezoom` option to disable zoom with mouse wheel in the editor.
  - Added selecting text and Cmd-F shortcut in Find dialog on OSX (ref #127).
  - Improved file activation when debugging is started (closes #137).
  - Reduced the minimum size of the Output/Console panel.
  - Refactored Recent Files history to make it faster and simpler.
  - Refactored and optimized directory scanning when loading IDE files.
  - Separated settings for function dropdown and project tree fonts (fixes #148).
  - Updated documentation about default EOL on OSX (ref #102).
  - Updated highlighting in Watch windows to not use editor styles.
  - Updated documentation for user settings (ref #113, #55).
  - Updated Monokai color scheme to fix current line color.

### Incompatibilities
  - (dev) `FileSysGet` has been replaced with `FileSysGetRecursive` with a different signature.

### Fixes
  - Fixed hiding all panels when switching to Full Screen mode.
  - Fixed loading a non-existing file.
  - Fixed activation of non-existing files/folders in the Project tree.
  - Fixed search results for lines without newline.
  - Fixed Find/Replace in folders with Unicode names (fixes #147); improved performance.
  - Fixed Un/Comment commands executed for empty lines.
  - Fixed fold/unfold for files starting with block/comment.
  - Fixed history after activating non-existing file in Recent Files.
  - Fixed scrolling to restored cursor position on OSX (when `usewrap` = false).
  - Fixed Find/Replace dialog to take Enter on OSX (fixes #140).
  - Fixed 'breaking' after executing OUT command that never reaches the target level.
  - Fixed stopping at a breakpoint at the initial line when `startwith` option is specified.
  - Fixed activation of a file loaded into active tab.
  - Fixed incorrect tab activation on OSX after using 'Open File'.
  - Fixed editor activation when file is loaded into an existing tab.
  - Fixed an error after opening non-existing file from 'Recent Files'.
  - Fixed blocking on reading app output without processing other events.
  - Fixed an issue with duplicate lines shown in the editor.
  - Fixed 'Replace All' to take 'Wrap Around' into account (fixes #132).
  - Fixed off-by-one error in searching consecutive matches.
  - Fixed 'Quick Find' not working without current selection (fixes #131).
  - Fixed looping in auto-complete on mistyped class (fixes #130).
  - Fixed compatibility with wx2.8 (thanks to Samuel Dionne-Riel; closes #128).
  - Fixed replacement logic in Find/Replace that could replace selected fragment (ref #127).
  - Fixed an error caused by allowing multiple Search/Replace windows (fixes #127).

## v0.361 (Apr 12 2013)

### Improvements
  - Added handling of Ctrl-Home and Ctrl-End on OSX (ref #89).
  - Added line copy/cut for Ctrl-C/Ctrl-X with no selection.
  - Updated About screen to be more configurable and flexible.
  - Updated Russian translation (thanks to toiffel).

### Fixes
  - Fixed launch command for Corona debugging on Windows.
  - Fixed 'control' check on OSX that changed with wx2.9.2+ (ref #89).
  - Fixed wrong tab activated on OSX after using New file in some cases.
  - Fixed cursor not being visible in some cases after file is loaded (ref #116).

## v0.36 (Apr 08 2013)

### Highlights
  - Added 32bit and 64bit **Linux binaries**.
  - Enabled **full debugging for Corona on OSX**.
  - Improved **debugger performance**.
  - Improved **performance of tab and project switching**.
  - Added **multiple selection and multi-cursor editing**.
  - Made Stack and Watch windows dockable and toggleable.

### Special thanks
  - To toiffel for build improvements and continuous work on wxwidgets 2.9 and Linux support.
  - To Marcel van Herk for testing and feedback on Stack and Watch windows behavior.
  - To Leo Bartoloni for Italian translation update.
  - To Fringale for updated French translation.
  - To neomantra for adding cdata processing in the serializer.

### Improvements
  - Added handling of case-insensitive filenames on OSX.
  - Added cdata processing (thanks to neomantra).
  - Added universal binaries for luasocket on OSX to allow debugging of 64bit applications (for example, LuaJIT) on OSX.
  - Added update of Stack and Watch windows after 'Debugging suspended' message.
  - Added toggling for View menu items.
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
  - Disabled showing 'value' in auto-complete after 'a:' (helps #101).
  - Enabled full debugging for Corona on OSX.
  - Improved debugging performance.
  - Improved performance of tab switching and project tree population.
  - Improved handling of upvalues with __tostring method in the Stack window.
  - Increased default font size for OSX; set 'Monaco' as default font (helps #89).
  - Made stack and watch windows dockable (closes #103).
  - Optimized project switching and added notebook freezing where possible (ref #89).
  - Reduced flicker in the project tree when a file is opened (ref #89).
  - Removed binary libraries not currently used.
  - Set 'Courier New' as the default font on Linux (ref #89).
  - Switched to 'native' menu on OSX and added 24x24 icons required (helps #89).
  - Updated Italian translation (thanks to Leo Bartoloni)
  - Updated 'method' type in auto-complete to only allow a:b syntax (closes #101).
  - Updated language files (es, it, ru) with new messages (ref #70).
  - Updated French translation with latest string changes, fixed a few typos (thanks to Fringale).
  - Updated Stack and Watch window to not refresh when not visible.
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
  - The debugger now stops on the next executable line after `.start()` call.

### Fixes
  - Fixed activating files in the project tree on a case insensitive system.
  - Fixed the Stack view being partially hidden when the root item is too wide (ref #110).
  - Fixed left side of the project panel being hidden when a file is activated (fixes #122).
  - Fixed breakpoint not firing on the first executable line in debugging (helps #121).
  - Fixed terminating debugging of an empty script.
  - Fixed reporting of initial line during debugging.
  - Fixed editor tab activation after closing another tab on Linux (ref #89).
  - Fixed 'Show tooltip' shortcut not working on Linux (fixes #118; ref #89).
  - Fixed cursor position being incorrectly restored (fixes #116; ref #89).
  - Fixed a warning about empty project directory in local console.
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
  - To [Christoph Kubisch](https://github.com/CrazyButcher) for help with Estrela merge.

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
