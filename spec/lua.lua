-- language specification

return {
	exts = {"lua"},
	lexer = wxstc.wxSTC_LEX_LUA,
	apitype = "lua",
	linecomment = "--",
	sep = "[%.:]",
	isfncall = function(str)
					return string.find(str,"([A-Za-z0-9_]+)%s*%(")
				end,
	isfndef = function(str)
					local l
					local s,e,cap = string.find(str,"function%s*([A-Za-z0-9_]+%s*[%.%:]*%s*[A-Za-z0-9_]*%s*%(.*%))")
					if (not s) then
						s,e,cap = string.find(str,"function%s*([A-Za-z0-9_]+%s*[%.%:]*%s*[A-Za-z0-9_]*)%s*")
					end
					if (s) then
						l = string.find(string.sub(str,1,s-1),"local%s+$")
					end
					return s,e,cap,l
				end,

	lexerstyleconvert = {
		text		= {wxstc.wxSTC_LUA_IDENTIFIER,},
	
		lexerdef	= {wxstc.wxSTC_LUA_DEFAULT,},
		comment 	= {wxstc.wxSTC_LUA_COMMENT, 
						wxstc.wxSTC_LUA_COMMENTLINE, 
						wxstc.wxSTC_LUA_COMMENTDOC,},
		stringtxt 	= {wxstc.wxSTC_LUA_STRING,
						wxstc.wxSTC_LUA_CHARACTER,
						wxstc.wxSTC_LUA_LITERALSTRING,},
		stringeol 	= {wxstc.wxSTC_LUA_STRINGEOL,},
		preprocessor= {wxstc.wxSTC_LUA_PREPROCESSOR,},
		operator 	= {wxstc.wxSTC_LUA_OPERATOR,},
		number 		= {wxstc.wxSTC_LUA_NUMBER,},
		
		
		keywords0	= {wxstc.wxSTC_LUA_WORD,},
		keywords1	= {wxstc.wxSTC_LUA_WORD2,},
		keywords2	= {wxstc.wxSTC_LUA_WORD3,},
		keywords3	= {wxstc.wxSTC_LUA_WORD4,},
		keywords4	= {wxstc.wxSTC_LUA_WORD5,},
		keywords5	= {wxstc.wxSTC_LUA_WORD6,},
		keywords6	= {wxstc.wxSTC_LUA_WORD7,},
		keywords7	= {wxstc.wxSTC_LUA_WORD8,},
	},
	
	keywords = {
		[[and break do else elseif end false for function if ipairs pairs
			in local nil not or repeat return then true until while]],

		[[_VERSION assert collectgarbage dofile error gcinfo loadfile loadstring
			print rawget rawset require tonumber tostring type unpack]],

		[[_G getfenv getmetatable loadlib next pcall
			rawequal setfenv setmetatable xpcall
			string table math coroutine io os debug
			load module select]],

		[[string.byte string.char string.dump string.find string.len
			string.lower string.rep string.sub string.upper string.format string.gfind string.gsub
			table.concat table.foreach table.foreachi table.getn table.sort table.insert table.remove table.setn
			math.abs math.acos math.asin math.atan math.atan2 math.ceil math.cos math.deg math.exp
			math.floor math.frexp math.ldexp math.log math.log10 math.max math.min math.mod
			math.pi math.pow math.rad math.random math.randomseed math.sin math.sqrt math.tan
			string.gmatch string.match string.reverse table.maxn
			math.cosh math.fmod math.modf math.sinh math.tanh math.huge]],

		[[coroutine.create coroutine.resume coroutine.status
			coroutine.wrap coroutine.yield
			io.close io.flush io.input io.lines io.open io.output io.read io.tmpfile io.type io.write
			io.stdin io.stdout io.stderr
			os.clock os.date os.difftime os.execute os.exit os.getenv os.remove os.rename
			os.setlocale os.time os.tmpname
			coroutine.running package.cpath package.loaded package.loadlib package.path
			package.preload package.seeall io.popen
			debug.debug debug.getfenv debug.gethook debug.getinfo debug.getlocal
			debug.getmetatable debug.getregistry debug.getupvalue debug.setfenv
			debug.sethook debug.setlocal debug.setmetatable debug.setupvalue debug.traceback]],
	},


}