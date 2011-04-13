-- About
-- ----------------------------------------------------
-- This file contains lua table definitons used by
-- automatic loaded files, not part of the 
-- editor source.
--
-- /cfg/config.lua
-- /cfg/user.lua
-- /spec/*.lua
-- /tools/*.lua
-- /api/*<apitype>/*.lua


-- style definition
-- ----------------------------------------------------
-- all entries are optiona
stattr = {
	fg = {r,g,b}, -- foreground color 0-255
	bg = {r,g,b}, -- background color
	i = false,	  -- italic
	b = false,	  -- bold
	u = false,	  -- underline
	fill = true,  -- fill to lineend
}

style = {
	-- lexer specific (inherit fg/bg from text)
	lexerdef	 	= stattr,
	comment 		= stattr,
	stringtxt 		= stattr,
	stringeol 		= stattr,
	preprocessor	= stattr,
	operator 		= stattr,
	number 			= stattr,
	
	keywords0		= stattr,
	keywords1		= stattr,
	keywords2		= stattr,
	keywords3		= stattr,
	keywords4		= stattr,
	keywords5		= stattr,
	keywords6		= stattr,
	keywords7		= stattr,
	
	-- common (inherit fg/bg from text)
	text 			= stattr,
	linenumber 		= stattr,
	bracematch 		= stattr,
	bracemiss 		= stattr,
	escapechar 		= stattr,
	indent 			= stattr,
	calltip 		= stattr,
	
	-- common special (need custom fg & bg )
	calltipbg		= nil,
	sel				= nil,
	caret			= nil,
	caretlinebg		= nil,
	fold			= nil,
	whitespace 		= nil,
	
	-- special, functioncall indicator
	fncall = {
		fg = {r,g,b},
		st = wxstc.wxSTC_INDIC_BOX,
		},
}

-- config definition
-- ----------------------------------------------------
-- tables must exist
-- content is optional
-- config is loaded into existing config table
config = {
	path = {
		-- path for tools/interpreters
		luxinia = "C:/luxbin/",
			-- path to luxinia exe
			
		projectdir = "",
			-- the project directory, used by 
			-- some tools/interpreters
	}, 
	editor = {
		fontname = "Courier New",
			-- default font 
		fontsize = 10,
			-- defailt size
		caretline = true,
			-- show active line
			
			-- input/output filtering of strings
			-- current filters "GermanUtf8Ascii"
		iofilter = nil,
		
			-- use indicator to show function calls
			-- if spec allows
		showfncall = true,
		
		tabwidth = 4,
		usetabs = true, -- if false then spaces are used
		whitespace = false,
		autotabs = true, 	-- if true test for tabs after file load, 
											-- sets "usetabs" to true for this file
	},
	
	
	outputshell = {
		-- output and shell settings
		fontname = "Courier New",
			-- default font 
		fontsize = 10,
			-- defult size
	}
	
	styles = {},
		-- styles table as above for editor
		
	stylesoutshell = {},
		-- styles for output/shell
		
	interpreter = "EstrelaEditor",
		-- the default "project" lua interpreter
		-- EstrelaEditor, Luxinia, Lua
		
	autocomplete = true,
		-- whether autocomplete is on by default
	
	acandtip = {
		shorttip = false,
		-- tooltips are compact during typing
		nodynwords = false,
		-- no dynamic words (user entered words)
		ignorecase = false,
		-- ignores case when performing comparison with autocomplete list
		strategy = 0,
		-- 0: is string comparison 
		-- 1: substring leading characters (camel case or _ separated)
		-- 2: leading + any correctly ordered fragments (default)
	}
		
	savebak = false,
		-- if bak files are created on save
		
	filehistorylength = 20,
		-- historylength for files
		
	projecthistorylength = 15,
		-- historylength for project directories

	singleinstance = true,
		-- if true creates a UDP server to exchange messages
		-- for loading commandline passed files
		
	singleinstanceport = 0xe493,
		-- UDP port for single instance communication
}


-- api definition
-- ----------------------------------------------------
-- hierarchy encoded into children

api = {
	-- global space words, e.g "table"
	identifier = {
		-- "function", "class", "keyword", "value", "lib"
		type = "function",
		
		description = "this does something",
		
		-- only for functions
		args = "(blah,blubb)",
		returns = "(foo)",
		
		-- children in the class hierarchy
		childs = {
		--.. same as identifier
		}
	},
	other = {
	--...
	},
}



-- spec definition
-- ----------------------------------------------------
-- all entries are optional
spec = {
	exts = {"ext","ext2",..},   
		-- compatible extensions
		
	lexer = wxstc.wxSTC_LEX_LUA,
		-- scintilla lexer
		
	lexerstyleconvert = {
		-- table mapping each styles to
		-- appropriate lexer id
		stringeol 	= {wxstc.wxSTC_LUA_STRINGEOL,},
		-- ...
	}
		
	linecomment = "//",			
		-- string for linecomments
		
	sep = "[%.:]",
		--	class.function separator match string,
		-- e.g in lua both . and : are allowed
		-- default is "\1" which should yield no matches
		-- and therefore disable class.func type autocompletion
		
	isfncall = function(str) return from,to end
		-- function that detects positions for a substring that
		-- stands for a functioncall, ie " call(..)" -> 2,5
		
		
	apitype = "api",			
		-- which sub directory of "api" is relevant
		-- api files handle autocomplete and tooltips
		-- api won't affect syntax coloring
		
	keywords = {
		-- up to 8 strings containing space separated keywords
		-- used by the lexer for coloring (NOT for autocomplete).
		-- however each lexer supports varying amount 
		-- of keyword types
		
		"foo bar word",
		"more words",
	}
}

-- tool definition
-- ----------------------------------------------------
-- main entries are optional
tool = {
	fninit = function(frame,menubar) end,	
		-- guarantees that ide is initialized
		-- can be used for init
		-- and adding custom menu
	
	exec = {
		-- quick exec action, listed under "Tools" menu
		name = "",
		description = "",
		fn = function(wxfilename,projectdir) end,
	}
}

-- interpreter definition
-- ----------------------------------------------------
interpreter = {
		name = "",
		description = "",
		api = {"apifile_without_extension"} -- optional to limit loaded apis
		frun = function(self,wfilename) 
				return "execommand"
			end,
		fprojdir = function(self,wfilename)
				return "projpath_from_filename"	-- optional
			end,
}

