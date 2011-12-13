-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exts = {"sql"},
  lexer = wxstc.wxSTC_LEX_LUA,
  apitype = "sql",
  linecomment = "--",
  isfncall = function(str)
    return string.find(str,"([A-Za-z0-9_]+)%s*%(")
  end,

  lexerstyleconvert = {
    text = {wxstc.wxSTC_LUA_IDENTIFIER,},

    lexerdef = {wxstc.wxSTC_LUA_DEFAULT,},
    comment = {wxstc.wxSTC_LUA_COMMENT,
      wxstc.wxSTC_LUA_COMMENTLINE,
      wxstc.wxSTC_LUA_COMMENTDOC,},
    stringtxt = {wxstc.wxSTC_LUA_STRING,
      wxstc.wxSTC_LUA_CHARACTER,
      wxstc.wxSTC_LUA_LITERALSTRING,},
    stringeol = {wxstc.wxSTC_LUA_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_LUA_PREPROCESSOR,},
    operator = {wxstc.wxSTC_LUA_OPERATOR,},
    number = {wxstc.wxSTC_LUA_NUMBER,},

    keywords0 = {wxstc.wxSTC_LUA_WORD,},
    keywords1 = {wxstc.wxSTC_LUA_WORD2,},
    keywords2 = {wxstc.wxSTC_LUA_WORD3,},
    keywords3 = {wxstc.wxSTC_LUA_WORD4,},
    keywords4 = {wxstc.wxSTC_LUA_WORD5,},
    keywords5 = {wxstc.wxSTC_LUA_WORD6,},
    keywords6 = {wxstc.wxSTC_LUA_WORD7,},
    keywords7 = {wxstc.wxSTC_LUA_WORD8,},
  },

  keywords = {
    [[ SELECT FROM CREATE TABLE WHERE ORDER BY CASE
    WHEN CHECK CONSTRAINT PRIMARY AUTOINCREMENT
    INSERT INTO DELETE DROP DISTINCT GROUP IS DEFAULT
    BEGIN TRANSACTION COMMIT KEY REFERENCES UNIQUE INDEX ON SET RESTRICT
    CASCADE ]],
    -- javascript keywords
    [[ NULL]],
    [[ TEXT BOOLEAN INTEGER]]
  },

}

--[==[
return {
  exts = {"sql"},
  lexer = wxstc.wxSTC_LEX_SQL,
  apitype = "sql",
  linecomment = "--",
  stylingbits = 5,

  lexerstyleconvert = {
    text = {wx.wxSTC_SQL_DEFAULT},
    comment = {wx.wxSTC_SQL_COMMENT,wx.wxSTC_SQL_COMMENTLINE},
    stringeol = {},
    number = {wx.wxSTC_SQL_NUMBER},
    stringtxt = {},
    lexerdef= {},
    keywords0 = {wx.wxSTC_SQL_IDENTIFIER, wx.wxSTC_SQL_QUOTEDIDENTIFIER},
    keywords1 = {wx.wxSTC_SQL_STRING},
    keywords2 = {wx.wxSTC_SQL_COMMENTDOC,wx.wxSTC_SQL_COMMENTDOCKEYWORD},
    keywords3 = {wx.wxSTC_SQL_COMMENTDOCKEYWORDERROR,wx.wxSTC_SQL_COMMENTLINEDOC},
    keywords4 = {wx.wxSTC_SQL_WORD,wx.wxSTC_SQL_WORD2,},
    keywords5 = {wx.wxSTC_SQL_USER1,wx.wxSTC_SQL_USER2,wx.wxSTC_SQL_USER3,wx.wxSTC_SQL_USER4},
    keywords6 = {},
    keywords7 = {},
    preprocessor= {},
  },

  keywords = {
    -- HTML tags and attributes
    [[ SELECT FROM CREATE TABLE WHERE ORDER BY CASE
    WHEN CHECK CONSTRAINT PRIMARY INTEGER AUTOINCREMENT
    INSERT INTO DELETE DROP DISTINCT GROUP IS DEFAULT]],
    -- javascript keywords
    [[ NULL]]
  },

}
]==]
