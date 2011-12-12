styles = {
  -- lexer specific (inherit fg/bg from text)
  lexerdef = {fg = {128, 128, 128},},
  comment = {fg = {100, 155, 65}, bg = {30, 35, 30 }, fill= true,},
  stringtxt = {fg = {200, 145, 145},},
  stringeol = {fg = {200, 145, 145},bg = {48, 24, 24}, fill = true,},
  preprocessor = {fg = {220, 175, 140},},
  operator = {fg = {210, 230, 240 },},
  number = {fg = {210, 230, 255, },},

  keywords0 = {fg = {128,128,255},},
  keywords1 = {fg = {210,90,90},},
  keywords2 = {fg = {0,220,220},},
  keywords3 = {fg = {220,220,0},},
  keywords4 = {fg = {240, 128,0},},
  keywords5 = {fg = {200,200,240},},
  keywords6 = {fg = {64, 240, 64},},
  keywords7 = {fg = {255, 128, 128},},

  -- common (inherit fg/bg from text)
  text = {fg = {200,200,200},bg = {18,24,26},},
  linenumber = {fg = {30, 30, 40},bg = {230, 230, 230},},
  bracematch = {fg = {255, 127, 0}, b = true},
  bracemiss = {fg = {255, 0,0}, b = true},
  ctrlchar = nil,
  indent = {fg = {80, 80, 80},bg = {24,24,24},},
  calltip = {fg = {255,255,255},bg = {10,10,10},},

  -- common special (need custom fg & bg)
  calltipbg = {fg = {240,240,240},bg = {10,10,10},},
  sel = {fg = {180,220,140},bg = {70,105,100},},
  caret = {fg = {250,250,250},},
  caretlinebg = {fg = {180,220,140},bg = {24,44,44},},
  fold = nil,
  whitespace = {fg = {54,54,54},},

  fncall = {fg = {75,75,150}, st= wxstc.wxSTC_INDIC_BOX},
}
