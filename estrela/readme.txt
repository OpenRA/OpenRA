Estrela Editor
-----------------
Estrela Editor is a wxLua based IDE. It supports multiple 
fileformats, "api" for autocompletion and tooltips, and custom 
commandline tools. Focus is extensibility for target applications 
using Lua. Its main purpose is as IDE for the 3D engine "Luxinia".



Commandline:
------------

Open File(s):
	<exe> <filename> [<filename>...]
	any non-option will be treated as filename

Overriding Config:
	<exe> [...] -cfg "<luacode overriding config>" [...]
	e.g.: estrela.exe -cfg "singleinstance=false;" somefile.lua

