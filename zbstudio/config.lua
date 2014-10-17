local G = ... -- this now points to the global environment
local ide, wx = G.ide, G.wx
local mac = ide.osname == 'Macintosh'
local win = ide.osname == "Windows"
if mac then
  local defaultsize = 11
  filetree.fontsize = defaultsize
  if ide.wxver >= "2.9.5" then
    editor.fontsize = defaultsize+1
    outputshell.fontsize = defaultsize
  end

  editor.fontname = "Monaco"
  outputshell.fontname = editor.fontname
else
  local defaultsize = 10
  editor.fontsize = defaultsize+1
  outputshell.fontsize = defaultsize

  local sysid, major, minor = wx.wxGetOsVersion()
  editor.fontname =
    win and (major == 5 and "Courier New" or "Consolas") or "Monospace"
  outputshell.fontname = editor.fontname
end

hidpi = mac -- support Retina displays by default (OSX)
singleinstance = not mac

unhidewindow = { -- allow unhiding of GUI windows
  -- 1 - show if hidden, 0 - ignore, 2 -- hide if shown
  ConsoleWindowClass = 2,
  -- ignore the following windows when "showing all"
  IME = 0,
  ['MSCTFIME UI'] = 0,
  -- GLUT/opengl/SDL applications (for example, moai or love2d)
  GLUT = 1, FREEGLUT = 1, SDL_app = 1,
}
