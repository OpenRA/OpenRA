ok(not LoadFile(''), "Don't load file with an empty name.")
ok(not LoadFile("\r\n "), "Don't load file with name that only has whitespaces.")
ok(not LoadFile('t'), "Don't load file with directory as the name (1/2).")
ok(not LoadFile('./'), "Don't load file with directory as the name (2/2).")

local fullpath = MergeFullPath(wx.wxFileName.GetCwd(), 't/test.lua')
ok(ActivateFile('t/test.lua:10'), "Load file:line.")
ok(not ActivateFile('t/foo.bar:10'), "Doesn't load non-existent file:line.")
ok(ActivateFile(fullpath..':10'), "Load fullpath/file:line.")
ok(not ActivateFile(fullpath..'/foo.bar:10'), "Doesn't load non-existent fullpath/file:line.")

local luas = FileSysGetRecursive('.', true, '*.lua')
local more = FileSysGetRecursive('.', true, '*.lua; *.more')
cmp_ok(#luas, '>', 0, "List of files is returned for '.lua' extension")
is(#luas, #more, "List of files is returned for '.lua' and '.lua; .more' is the same")

local fcopy = "t/copy.lua!"
ok(FileCopy("t/test.lua", fcopy), "File copied successfully.")
local copy = FileRead(fcopy)
ok(copy, "Copied file exists.")
ok(copy == FileRead("t/test.lua"), "Copy matches the original.")

local luasmore = FileSysGetRecursive('.', true, '*.lua')
is(#luasmore, #luas, ("Mask '.lua' doesn't match '%s'"):format(fcopy))
os.remove(fcopy)
ok(not FileRead(fcopy), "File deleted successfully.")
