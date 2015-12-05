ok(not LoadFile(''), "Don't load file with an empty name.")
ok(not LoadFile("\r\n "), "Don't load file with name that only has whitespaces.")
ok(not LoadFile('t'), "Don't load file with directory as the name (1/2).")
ok(not LoadFile('./'), "Don't load file with directory as the name (2/2).")
ok(pcall(function() LoadFile('some-nonexisting-name') end), "Load non-existing files.")
ClosePage()

local fullpath = MergeFullPath(wx.wxFileName.GetCwd(), 't/test.lua')
ok(ActivateFile('t/test.lua:10'), "Load file:line.")
ok(not ActivateFile('t/foo.bar:10'), "Doesn't load non-existent file:line.")
ok(ActivateFile(fullpath..':10'), "Load fullpath/file:line.")
ok(not ActivateFile(fullpath..'/foo.bar:10'), "Doesn't load non-existent fullpath/file:line.")
ClosePage() -- close activated file

local sep = GetPathSeparator()
like(FileSysGetRecursive('t', true, 'test.lua', {path = true})[1], "^t"..sep.."test.lua$",
  "Traversing `t`, including path in the results (1/6)")
like(FileSysGetRecursive('t/', true, 'test.lua', {path = true})[1], "^t"..sep.."test.lua$",
  "Traversing `t/`, including path in the results (2/6)")
like(FileSysGetRecursive('t\\', true, 'test.lua', {path = true})[1], "^t"..sep.."test.lua$",
  "Traversing `t\\`, including path in the results (3/6)")
is(FileSysGetRecursive('t', true, 'test.lua', {path = false})[1], "test.lua",
  "Traversing `t`, not including path in the results (4/6)")
is(FileSysGetRecursive('t/', true, 'test.lua', {path = false})[1], "test.lua",
  "Traversing `t/`, not including path in the results (5/6)")
is(FileSysGetRecursive('t\\', true, 'test.lua', {path = false})[1], "test.lua",
  "Traversing `t\\`, not including path in the results (6/6)")

local luas = FileSysGetRecursive('t', true, '*.lua')
local more = FileSysGetRecursive('t', true, '*.lua; *.more')
cmp_ok(#luas, '>', 0, "List of files is returned for '.lua' extension.")
is(#luas, #more, "Lists of files returned for '.lua' and '.lua; .more' are the same.")

local luasnodir = FileSysGetRecursive('t', true, '*.lua', {folder = false})
is(#luas, #luasnodir, "List of files returned for '.lua' does not include folders.")

local fcopy = "t/copy.lua!"
ok(FileCopy("t/test.lua", fcopy), "File copied successfully.")
local copy = FileRead(fcopy)
ok(copy, "Copied file exists.")
ok(copy == FileRead("t/test.lua"), "Copy matches the original.")

local luasmore = FileSysGetRecursive('t', true, '*.lua')
is(#luasmore, #luas, ("Mask '.lua' doesn't match '%s'"):format(fcopy))
os.remove(fcopy)
ok(not FileRead(fcopy), "File deleted successfully.")

local exlist = ide.config.excludelist
local path = 'zbstudio/res/16'
local bins0 = FileSysGetRecursive(path, true, '*')
local bins1 = FileSysGetRecursive(path, true, '*.png')
ok(#bins0 > 1, "'*.*' mask retrieves binary files.")

ide.config.excludelist = ".png/"
local bins = FileSysGetRecursive(path, true, '*')
is(#bins, #bins0, "Excluding '.png/' still returns 'png' files.")

ide.config.excludelist = ".png"
bins = FileSysGetRecursive(path, true, '*')
is(#bins, 1, "Excluding '.png' skips 'png' files.")

ide.config.excludelist = "*.png"
bins = FileSysGetRecursive(path, true, '*')
is(#bins, 1, "Excluding '*.png' skips 'png' files.")

ide.config.excludelist = "FIND*.png"
bins = FileSysGetRecursive(path, true, '*.png')
ok(#bins < #bins1, "Excluding `FIND*.png` filters out files with that mask.")

ide.config.excludelist = "*.png"
bins = FileSysGetRecursive(path, true, 'FIND*.png')
ok(#bins < #bins1, "Requesting `FIND*.png` filters specific files.")

ide.config.excludelist = ""
local bina = FileSysGetRecursive('src', true, '*.lua')

ide.config.excludelist = "editor"
bins = FileSysGetRecursive('src', true, '*.lua')
is(#bins, #bina, "Excluding `editor` still returns the content of `editor` folder.")

ide.config.excludelist = "editor/"
bins = FileSysGetRecursive('src', true, '*.lua')
ok(#bins < #bina, "Excluding `editor/` skips the content of `editor` folder.")

ide.config.excludelist = "editor\\"
local nosrc = #bins
bins = FileSysGetRecursive('src', true, '*.lua')
ok(#bins < #bina, "Excluding `editor\\` skips the content of `editor` folder.")
is(#bins, nosrc, "Excluding `editor\\` and `editor/` produce the same result.")

nosrc = #FileSysGetRecursive('src', true, '*.lua', {folder = false})
ide.config.excludelist = "editor/**.lua"
bins = FileSysGetRecursive('src', true, '*.lua', {folder = false})
is(#bins, nosrc, "Excluding `editor/**.lua` skips lua files in subfolders.")

ide.config.excludelist = ""
local editor = #FileSysGetRecursive('src/editor', true, '*.lua', {folder = false})

ide.config.excludelist = "src/*.lua"
bins = FileSysGetRecursive('src', true, '*.lua', {folder = false})
is(#bins, editor, "Excluding `src/*.lua` skips lua files only in `src` folder.")

ide.config.excludelist = exlist
bins = FileSysGetRecursive(path, true, '*', {skipbinary = true})
is(#bins, 1, "Default mask excludes `png` files with `skipbinary`.")

bins = FileSysGetRecursive("bin", true, '*.exe', {folder = false})
is(bins, {}, "Default mask excludes `*.exe` files.")
