dofile 'interpreters/luabase.lua'
local interpreter = MakeLuaInterpreter(5.3, ' 5.3')
interpreter.skipcompile = true
return interpreter
