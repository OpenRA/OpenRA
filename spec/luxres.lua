return {
	exts = {"prt","shd","mtl"},
	lexer = wxstc.wxSTC_LEX_POV,
	apitype = "luxres",
	linecomment = "//",
	lexerstyleconvert = {
		text		= {wxstc.wxSTC_POV_IDENTIFIER,},
	
		lexerdef	= {wxstc.wxSTC_POV_DEFAULT,},
		comment 	= {wxstc.wxSTC_POV_COMMENT, 
						wxstc.wxSTC_POV_COMMENTLINE,},
		stringtxt 	= {wxstc.wxSTC_POV_STRING,},
		stringeol 	= {wxstc.wxSTC_POV_STRINGEOL,},
		--preprocessor= {wxstc.wxSTC_POV_PREPROCESSOR,},
		operator 	= {wxstc.wxSTC_POV_OPERATOR,},
		number 		= {wxstc.wxSTC_POV_NUMBER,},
		
		
		keywords0	= {wxstc.wxSTC_POV_WORD,},
		keywords1	= {wxstc.wxSTC_POV_WORD2,},
		keywords2	= {wxstc.wxSTC_POV_WORD3,},
		keywords3	= {wxstc.wxSTC_POV_WORD4,},
		keywords4	= {wxstc.wxSTC_POV_WORD5,},
		keywords5	= {wxstc.wxSTC_POV_WORD6,},
		keywords6	= {wxstc.wxSTC_POV_WORD7,},
		keywords7	= {wxstc.wxSTC_POV_WORD8,},
		}
	}

