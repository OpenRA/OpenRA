-- other tests for fuzzy searches
-- textmate: https://github.com/textmate/textmate/blob/master/Frameworks/text/tests/t_ranker.cc
-- brackets: https://github.com/adobe/brackets/blob/2c1616a6346dfee29a8321b5ecc9e0f636ec42d8/test/spec/StringMatch-test.js

local function check(s, p1, p2)
  local r = CommandBarScoreItems({p1, p2}, s)
  ok(r[1][1] == p1,
    ("'%s' is more similar to '%s' (%d) than to '%s' (%d).")
    :format(s, p1, r[1][1] == p1 and r[1][2] or r[2][2],
               p2, r[2][1] == p2 and r[2][2] or r[1][2]))
end

check("mtv", "MTVStatusBar.txt", "MyTextView.txt")
check("doc", "document.lua", "MyDocument.txt")
check("paste", "Paste Selection Online", "Encrypt With Password")
check("zerobrane", "zerobrane", "ZeroBraneStudio")
check("barfileopen", "BarFileOpen", "BarFinderLabelOpen")
check("readme", "readme", "README")
check("ReadMe", "READme", "readME")
check("f", "fun", "funclist.lua")

ok(1 == #CommandBarScoreItems({"funclist.lua", "f"}, "fun"),
  "Patterns longer than strings don't match.")
