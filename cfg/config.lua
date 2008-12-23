local _G = ...
local assert = _G.assert

assert(_G)

-- path where luxinia resides in, a "luxinia.exe" is appended automaticly
path.luxinia = "../../engine/"

editor.fontname = "Courier New"
editor.caretline = true

interpreter = "EstrelaEditor"

filehistorylength = 20

singleinstance = true
singleinstanceport = 0xe493



-- this function converts some utf8 character output. It's a hack.
-- Since luxinia is not utf8 prepared, this is still necessary.
-- if you wish to turn this off, edit user.lua and set this filefunction to nil
function FileOutputFilter (fpath, content)
	local utf8escape = ("string").char(195)
	-- only simple cases are handled (umlauts)
	local chr = ("string").char
	local charconv = {
		[chr(164)] = chr(132), -- ä
		[chr(182)] = chr(148), -- ö
		[chr(188)] = chr(129), -- ü
		[chr(132)] = chr(142), -- Ä
		[chr(150)] = chr(153), -- Ö
		[chr(156)] = chr(154), -- Ü
		[chr(159)] = chr(225), -- ß
	}
	return content : gsub (utf8escape.."(.)",charconv)
end


-- this function is another hack to read an ANSI encoded 
-- file and converts the umlauts to utf8 chars
function FileInputFilter (fpath, content)
	local utf8escape = ("string").char(195)
	local chr = ("string").char
	local charconv = {
		[chr(132)] = utf8escape..chr(164), -- ä
		[chr(148)] = utf8escape..chr(182), -- ö
		[chr(129)] = utf8escape..chr(188), -- ü
		[chr(142)] = utf8escape..chr(132), -- Ä
		[chr(153)] = utf8escape..chr(150), -- Ö
		[chr(154)] = utf8escape..chr(156), -- Ü
		[chr(225)] = utf8escape..chr(159), -- ß
	}
	local lst = "["
	for k in _G.pairs(charconv) do lst = lst .. k end
	lst = "]"
	
	return content:gsub(lst,charconv)
end

assert(FileOutputFilter("",FileInputFilter("","„”Ž™šá"))=="„”Ž™šá","UTF8-ANSI conversion failed")

styles = {
	-- lexer specific (inherit fg/bg from text)
	lexerdef	 	= {fg = {128, 128, 128},},
	comment 		= {fg = {0,   127, 0  },bg = {240, 240, 220}, fill= true,},
	stringtxt 		= {fg = {127, 0,   127},},
	stringeol 		= {fg = {0,   0,   0  },bg = {224, 192, 224}, fill = true, },
	preprocessor	= {fg = {127, 127, 0  },},
	operator 		= {fg = {0,   0,   0  },},
	number 			= {fg = {90,  100,  0 },},
	
	keywords0		= {fg = {0,   0,   127},	b = true,},
	keywords1		= {fg = {127, 0,   0},},
	keywords2		= {fg = {0,   127,  0},},
	keywords3		= {fg = {0,   0,   127},},
	keywords4		= {fg = {127, 0,   95},},
	keywords5		= {fg = {35,  95,  175},},
	keywords6		= {fg = {0,   127, 127},},
	keywords7		= {fg = {240, 255, 255},},
	
	-- common (inherit fg/bg from text)
	text 			= nil, -- let os pick
	linenumber 		= {fg = {90, 90, 80},},
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