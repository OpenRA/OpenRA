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

acandtip.shorttip = false
acandtip.nodynwords = true

activateoutput = true
projectautoopen = true
autorecoverinactivity = 10
allowinteractivescript = true -- allow interaction in the output window

interpreter = "luadeb"
unhidewindow = { -- allow unhiding of GUI windows
  -- 1 - unhide if hidden, 0 - hide if shown
  wxWindowClassNR = 1, -- wxwindows applications
  GLUT = 1, -- opengl applications (for example, moai)
}

styles = {
  -- lexer specific (inherit fg/bg from text)
  lexerdef = {fg = {128, 128, 128}},
  comment = {fg = {32, 127, 32}, bg = {250, 250, 240}, fill= true},
  stringtxt = {fg = {127, 0, 127}},
  stringeol = {fg = {0, 0, 0}, bg = {224, 192, 224}, fill = true},
  preprocessor = {fg = {127, 127,0}},
  operator = {fg = {0, 0, 0}},
  number = {fg = {90, 0, 255}},

  keywords0 = {fg = {0, 0, 127}, b = true},
  keywords1 = {fg = {127, 0, 0}, b = true},
  keywords2 = {fg = {0, 127, 0}, b = true},
  keywords3 = {fg = {0, 0, 127}, b = true},
  keywords4 = {fg = {127, 0, 95}, b = true},
  keywords5 = {fg = {35, 95, 175}, b = true},
  keywords6 = {fg = {0, 127, 127}, b = true},
  keywords7 = {fg = {240, 255, 255}, b = true},

  -- common (inherit fg/bg from text)
  text = nil, -- let os pick
  linenumber = {fg = {90, 90, 80}},
  bracematch = {fg = {0, 0, 255}, b = true},
  bracemiss = {fg = {255, 0, 0 }, b = true},
  ctrlchar = nil,
  indent = {fg = {192, 192, 192}, bg = {255, 255, 255}},
  calltip = nil,

  -- common special (need custom fg & bg)
  calltipbg = nil,
  sel = nil,
  caret = nil,
  caretlinebg = {bg = {240,240,230}},
  fold = nil,
  whitespace = nil,

  fncall = {fg = {175,175,255}, st= wxstc.wxSTC_INDIC_TT},
}
