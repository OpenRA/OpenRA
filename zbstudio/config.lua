editor.caretline = true
editor.showfncall = true
editor.autotabs = false
editor.usetabs  = false
editor.tabwidth = 2
editor.usewrap = true
editor.calltipdelay = 500
editor.smartindent = true

local G = ... -- this now points to the global environment
if G.ide.osname == 'Macintosh' then filetree.fontsize = 11 end

filehistorylength = 20

singleinstance = true
singleinstanceport = 0xe493

acandtip.shorttip = true
acandtip.nodynwords = true

activateoutput = true
projectautoopen = true
autorecoverinactivity = 10
allowinteractivescript = true -- allow interaction in the output window

interpreter = "luadeb"
unhidewindow = { -- allow unhiding of GUI windows
  -- 1 - show if hidden, 0 - ignore, 2 -- hide if shown
  ConsoleWindowClass = 0,
  IME = 0,
  ['MSCTFIME UI'] = 0,
  GLUT = 1, -- opengl applications (for example, moai)
}
