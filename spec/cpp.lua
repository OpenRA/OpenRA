-- author: Paul Kulchenko
---------------------------------------------------------

local funccall = "([A-Za-z_][A-Za-z0-9_]*)%s*"

if not CMarkSymbols then dofile "spec/cbase.lua" end
return {
  exts = {"cpp", "c", "hpp", "h"},
  lexer = wxstc.wxSTC_LEX_CPP,
  apitype = "cpp",
  linecomment = "//",
  stylingbits = 5,

  isfncall = function(str)
    return string.find(str, funccall .. "%(")
  end,

  marksymbols = CMarkSymbols,

  lexerstyleconvert = {
    text = {wxstc.wxSTC_C_IDENTIFIER,},

    lexerdef = {wxstc.wxSTC_C_DEFAULT,},
    comment = {wxstc.wxSTC_C_COMMENT,
      wxstc.wxSTC_C_COMMENTLINE,
      wxstc.wxSTC_C_COMMENTDOC,},
    stringtxt = {wxstc.wxSTC_C_STRING,
      wxstc.wxSTC_C_CHARACTER,
      wxstc.wxSTC_C_VERBATIM,},
    stringeol = {wxstc.wxSTC_C_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
    operator = {wxstc.wxSTC_C_OPERATOR,},
    number = {wxstc.wxSTC_C_NUMBER,},

    keywords0 = {wxstc.wxSTC_C_WORD,},
    keywords1 = {wxstc.wxSTC_C_WORD2,},
  },

  keywords = {
    [[ alignas alignof and and_eq asm auto bitand bitor break case catch
       class compl const constexpr const_cast continue
       decltype default delete do dynamic_cast else enum explicit export
       extern for friend goto if inline mutable namespace new noexcept not
       not_eq nullptr operator or or_eq private protected public register
       reinterpret_cast return sizeof static static_assert static_cast
       struct switch template this thread_local throw try typedef typeid
       typename union using virtual volatile while xor xor_eq]],
    [[ NULL bool char char16_t char32_t double false float int long 
       short signed true unsigned void wchar_t]]
  },
}

--[==[ C-only keywords
    [[ auto break case const continue default do else enum extern for goto if
       register return sizeof static struct switch typedef union volatile while]],
    [[ NULL char double float int long short signed unsigned void]]
--]==]

--[[
// Lexical states for SCLEX_CPP
%define wxSTC_C_DEFAULT
%define wxSTC_C_COMMENT
%define wxSTC_C_COMMENTLINE
%define wxSTC_C_COMMENTDOC
%define wxSTC_C_NUMBER
%define wxSTC_C_WORD
%define wxSTC_C_STRING
%define wxSTC_C_CHARACTER
%define wxSTC_C_UUID
%define wxSTC_C_PREPROCESSOR
%define wxSTC_C_OPERATOR
%define wxSTC_C_IDENTIFIER
%define wxSTC_C_STRINGEOL
%define wxSTC_C_VERBATIM
%define wxSTC_C_REGEX
%define wxSTC_C_COMMENTLINEDOC
%define wxSTC_C_WORD2
%define wxSTC_C_COMMENTDOCKEYWORD
%define wxSTC_C_COMMENTDOCKEYWORDERROR
%define wxSTC_C_GLOBALCLASS
]]
