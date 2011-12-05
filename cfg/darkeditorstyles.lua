
styles = {
	-- lexer specific (inherit fg/bg from text)
	lexerdef	 	= {fg = {128, 128, 128},},
	comment 		= {fg = {200,   230, 120  },bg = {30,   40, 30 }, fill= true,},
	stringtxt 		= {fg = {240,128,240},},
	stringeol 		= {fg = {240,128,200},bg = {48, 24, 24}, fill = true,},
	preprocessor	= {fg = {220,175,140},},
	operator 		= {fg = {210,  230,  240 },},
	number 			= {fg = {210,  230,  255, },},
	
	keywords0		= {fg = {240,240,0}},
	keywords1		= {fg = {255,64,64},},
	keywords2		= {fg = {0,240,240},},
	keywords3		= {fg = {140,140,240},},
	keywords4		= {fg = {255, 128,   0},},
	keywords5		= {fg = {200,200,240},},
	keywords6		= {fg = {64, 250, 64},},
	keywords7		= {fg = {255, 128, 128},},
	
	-- common (inherit fg/bg from text)
	text 			= {fg = {240,240,240},bg = {24,24,24},},
	linenumber 		= {fg = {30, 30, 40},bg = {230, 230, 230},},
	bracematch 		= {fg = {255, 127,  0},	b = true},
	bracemiss 		= {fg = {255, 0,0},	b = true},
	ctrlchar 		= nil,
	indent 			= {fg = {80, 80, 80},bg = {24,24,24},},
	calltip 		= {fg = {255,255,255},bg = {10,10,10},},
	
	-- common special (need custom fg & bg)
	calltipbg		= {fg = {240,240,240},bg = {10,10,10},},
	sel				= {fg = {180,220,140},bg = {70,105,100},},
	caret			= {fg = {250,250,250},},
	caretlinebg		= {fg = {180,220,140},bg = {24,44,44},},
	fold			= nil,
	whitespace 		= {fg = {54,54,54},},
	
	fncall			= {fg = {140,140,0}, st = wxstc.wxSTC_INDIC_BOX},
}
