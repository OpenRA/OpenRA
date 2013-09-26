editor.caretline = true
editor.showfncall = true
editor.autotabs = false
editor.usetabs  = false
editor.tabwidth = 2
editor.usewrap = true
editor.calltipdelay = 500
editor.smartindent = true
editor.fold = true

local G = ... -- this now points to the global environment
if G.ide.osname == 'Macintosh' then
  local defaultsize = 11
  filetree.fontsize = defaultsize
  funclist.fontsize = defaultsize
  if G.ide.wxver >= "2.9.5" then
    editor.fontsize = defaultsize
    editor.fontname = "Monaco"
    outputshell.fontsize = editor.fontsize
    outputshell.fontname = editor.fontname
  end
else
  -- set Courier New in all other cases (Linux and Windows), otherwise
  -- a proportional font gets used by default, which doesn't look right.
  editor.fontname = "Courier New"
  outputshell.fontname = editor.fontname
end

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
  ConsoleWindowClass = 2,
  IME = 0,
  ['MSCTFIME UI'] = 0,
  -- GLUT/opengl/SDL applications (for example, moai)
  GLUT = 1, FREEGLUT = 1, SDL_app = 1,
}
