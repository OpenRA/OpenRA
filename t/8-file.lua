ok(not LoadFile(''), "Don't load file with an empty name.")
ok(not LoadFile("\r\n "), "Don't load file with name that only has whitespaces.")
ok(not LoadFile('t'), "Don't load file with directory as the name (1/2).")
ok(not LoadFile('./'), "Don't load file with directory as the name (2/2).")

local fullpath = MergeFullPath(wx.wxFileName.GetCwd(), 't/test.lua')
ok(ActivateFile('t/test.lua:10'), "Load file:line.")
ok(not ActivateFile('t/foo.bar:10'), "Doesn't load non-existent file:line.")
ok(ActivateFile(fullpath..':10'), "Load fullpath/file:line.")
ok(not ActivateFile(fullpath..'/foo.bar:10'), "Doesn't load non-existent fullpath/file:line.")
ClosePage() -- close activated file

local luas = FileSysGetRecursive('.', true, '*.lua')
local more = FileSysGetRecursive('.', true, '*.lua; *.more')
cmp_ok(#luas, '>', 0, "List of files is returned for '.lua' extension")
is(#luas, #more, "List of files is returned for '.lua' and '.lua; .more' is the same.")

local luasnodir = FileSysGetRecursive('.', true, '*.lua', {folder = false})
is(#luas, #luasnodir, "List of files is returned for '.lua' does not include folders.")

local fcopy = "t/copy.lua!"
ok(FileCopy("t/test.lua", fcopy), "File copied successfully.")
local copy = FileRead(fcopy)
ok(copy, "Copied file exists.")
ok(copy == FileRead("t/test.lua"), "Copy matches the original.")

local luasmore = FileSysGetRecursive('.', true, '*.lua')
is(#luasmore, #luas, ("Mask '.lua' doesn't match '%s'"):format(fcopy))
os.remove(fcopy)
ok(not FileRead(fcopy), "File deleted successfully.")

local exlist = ide.config.excludelist
local path = 'zbstudio/res/16'
local bins0 = FileSysGetRecursive(path, true, '*.*')
local bins1 = FileSysGetRecursive(path, true, '*.png')
ok(#bins0 > 1, "'*.*' mask retrieves binary files.")

ide.config.excludelist = ".png/"
local bins = FileSysGetRecursive(path, true, '*.*')
is(#bins, #bins0, "Excluding '.png/' still returns 'png' files.")

ide.config.excludelist = ".png"
bins = FileSysGetRecursive(path, true, '*.*')
is(#bins, 1, "Excluding '.png' skips 'png' files.")

ide.config.excludelist = "*.png"
bins = FileSysGetRecursive(path, true, '*.*')
is(#bins, 1, "Excluding '*.png' skips 'png' files.")

ide.config.excludelist = "FIND*.png"
bins = FileSysGetRecursive(path, true, '*.png')
ok(#bins < #bins1, "Excluding `FIND*.png` filters out files with that mask.")

ide.config.excludelist = "*.png"
bins = FileSysGetRecursive(path, true, 'FIND*.png')
ok(#bins < #bins1, "Requesting `FIND*.png` filters specific files.")

ide.config.excludelist = ""
local bina = FileSysGetRecursive('.', true, '*.lua')

ide.config.excludelist = "src"
bins = FileSysGetRecursive('.', true, '*.lua')
is(#bins, #bina, "Excluding `src` still returns the content of `src` folder.")

ide.config.excludelist = "src/"
bins = FileSysGetRecursive('.', true, '*.lua')
ok(#bins < #bina, "Excluding `src/` skips the content of `src` folder.")

ide.config.excludelist = "src\\"
local nosrc = #bins
bins = FileSysGetRecursive('.', true, '*.lua')
ok(#bins < #bina, "Excluding `src\\` skips the content of `src` folder.")
is(#bins, nosrc, "Excluding `src\\` and `src/` produce the same result.")

nosrc = #FileSysGetRecursive('.', true, '*.lua', {folder = false})
ide.config.excludelist = "src/**.lua"
bins = FileSysGetRecursive('.', true, '*.lua', {folder = false})
is(#bins, nosrc, "Excluding `src/**.lua` skips lua files in subfolders.")

ide.config.excludelist = ""
local editor = #FileSysGetRecursive('src/editor', true, '*.lua', {folder = false})

ide.config.excludelist = "src/*.lua"
bins = FileSysGetRecursive('.', true, '*.lua', {folder = false})
is(#bins, nosrc+editor, "Excluding `src/*.lua` skips lua files only in `src` folder.")

ide.config.excludelist = exlist
bins = FileSysGetRecursive(path, true, '*', {skipbinary = true})
is(#bins, 1, "Default mask excludes `png` files with `skipbinary`.")

bins = FileSysGetRecursive("bin", true, '*.exe', {folder = false})
is(bins, {}, "Default mask excludes `*.exe` files.")
