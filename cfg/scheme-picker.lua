--[[
1. Pick a color scheme by clicking on its name:
  - [Tomorrow](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','Tomorrow'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [TomorrowContrast](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','TomorrowContrast'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [TomorrowNight](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','TomorrowNight'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [TomorrowNightBlue](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','TomorrowNightBlue'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [TomorrowNightBright](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','TomorrowNightBright'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [TomorrowNightEighties](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','TomorrowNightEighties'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [Zenburn](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','Zenburn'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [Monokai](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','Monokai'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [Molokai](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','Molokai'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [SolarizedDark](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','SolarizedDark'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [SolarizedLight](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','SolarizedLight'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [Notepad++](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','NotepadPlusPlus'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))
  - [SciTeLuaIDE](macro:inline(ApplyStyleConfig('cfg/tomorrow.lua','SciTeLuaIDE'); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = c.styles.text; ReApplySpecAndStyles()))

  - [ZeroBrane Studio](macro:inline(ide.config.styles = StylesGetDefault(); local c = ide.config; c.stylesoutshell = c.styles; c.styles.auxwindow = {}; ReApplySpecAndStyles()))

2. Add the following code with the scheme you selected to `cfg/user.lua`.
--]]

local G = ...
styles = G.loadfile('cfg/tomorrow.lua')('TomorrowNightBlue')
stylesoutshell = styles -- apply the same scheme to Output/Console windows
styles.auxwindow = styles.text -- apply text colors to auxiliary windows
styles.calltip = styles.text -- apply text colors to tooltips

-- code example
if false and true then func(1, 2, 3) end
