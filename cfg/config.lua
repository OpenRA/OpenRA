
-- path where luxinia resides in, a "luxinia.exe" is appended automaticly
path.luxinia = "../../engine/"

editor.fontname = "Courier New"
editor.caretline = true

interpreter = "EstrelaEditor"

filehistorylength = 20

singleinstance = true
singleinstanceport = 0xe493

styles = {
	-- lexer specific (inherit fg/bg from text)
	lexerdef	 	= {fg = {128, 128, 128},},
	comment 		= {fg = {0,   127, 0  },bg = {240, 240, 220}, fill= true,},
	stringtxt 		= {fg = {127, 0,   127},},
	stringeol 		= {fg = {0,   0,   0  },bg = {224, 192, 224}, fill = true, b = true},
	preprocessor	= {fg = {127, 127, 0  },},
	operator 		= {fg = {0,   0,   0  },},
	number 			= {fg = {90,  100,  0 },},
	
	keywords0		= {fg = {0,   0,   127},	b = true,},
	keywords1		= {fg = {0,   0,   95},},
	keywords2		= {fg = {0,   95,  0},},
	keywords3		= {fg = {127, 0,   0},},
	keywords4		= {fg = {127, 0,   95},},
	keywords5		= {fg = {35,  95,  175},},
	keywords6		= {fg = {0,   127, 127},},
	keywords7		= {fg = {240, 255, 255},},
	
	-- common (inherit fg/bg from text)
	text 			= nil, -- let os pick
	linenumber 		= {fg = {192, 192, 192},},
	bracematch 		= {fg = {0,   0,   255},	b = true},
	bracemiss 		= {fg = {255, 0,   0  },	b = true},
	ctrlchar 		= nil,
	indent 			= {fg = {192, 192, 192},bg = {255, 255, 255},},
	calltip 		= nil,
	
	-- common special (need custom fg & bg)
	calltipbg		= nil,
	sel				= nil,
	caret			= nil,
	caretlinebg		= nil,
	fold			= nil,
	whitespace 		= nil,
	

}