return {
	exts = {"cg"},
	lexer = wxstc.wxSTC_LEX_CPP,
	apitype = "cg",
	linecomment = "//",
	lexerstyleconvert = {
		text		= {wxstc.wxSTC_C_IDENTIFIER,
						wxstc.wxSTC_C_VERBATIM,
						wxstc.wxSTC_C_REGEX,
						wxstc.wxSTC_C_REGEX,
						wxstc.wxSTC_C_GLOBALCLASS,},
	
		lexerdef	= {wxstc.wxSTC_C_DEFAULT,},
		comment 	= {wxstc.wxSTC_C_COMMENT, 
						wxstc.wxSTC_C_COMMENTLINE, 
						wxstc.wxSTC_C_COMMENTDOC,
						wxstc.wxSTC_C_COMMENTLINEDOC,
						wxstc.wxSTC_C_COMMENTDOCKEYWORD,
						wxstc.wxSTC_C_COMMENTDOCKEYWORDERROR,},
		stringtxt 	= {wxstc.wxSTC_C_STRING,
						wxstc.wxSTC_C_CHARACTER,
						wxstc.wxSTC_C_UUID,},
		stringeol 	= {wxstc.wxSTC_C_STRINGEOL,},
		preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
		operator 	= {wxstc.wxSTC_C_OPERATOR,},
		number 		= {wxstc.wxSTC_C_NUMBER,
						wxstc.wxSTC_C_WORD},
		
		
		keywords0	= {wxstc.wxSTC_C_WORD,},
		keywords1	= {wxstc.wxSTC_C_WORD2,},
		}
	}