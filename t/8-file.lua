ok(not LoadFile(''), "Don't load file with an empty name.")
ok(not LoadFile("\r\n "), "Don't load file with name that only has whitespaces.")
ok(not LoadFile('t'), "Don't load file with directory as the name (1/2).")
ok(not LoadFile('./'), "Don't load file with directory as the name (2/2).")
