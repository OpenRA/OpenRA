-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exts = {"htm","html"},
  lexer = wxstc.wxSTC_LEX_HTML,
  apitype = "html",

  stylingbits = 7,

  lexerstyleconvert = {
    text = {
      wxstc.wxSTC_H_DEFAULT,
      wxstc.wxSTC_HJ_DEFAULT,
    },
    comment = {wxstc.wxSTC_H_COMMENT,
      wxstc.wxSTC_HPHP_COMMENT,
      wxstc.wxSTC_HPHP_COMMENTLINE,
      wxstc.wxSTC_HJA_COMMENTLINE,
      wxstc.wxSTC_HJA_COMMENTDOC,
      wxstc.wxSTC_HJ_COMMENT,
      wxstc.wxSTC_HJ_COMMENTLINE,
      wxstc.wxSTC_HJ_COMMENTDOC,},
    stringeol = {wxstc.wxSTC_HJ_STRINGEOL,},
    number = {wxstc.wxSTC_H_NUMBER,
      wxstc.wxSTC_HJ_NUMBER,
      wxstc.wxSTC_HJA_NUMBER,
      wxstc.wxSTC_HPHP_NUMBER,},
    stringtxt = {
      wxstc.wxSTC_H_DOUBLESTRING,
      wxstc.wxSTC_H_SINGLESTRING,
      wxstc.wxSTC_HJ_DOUBLESTRING,
      wxstc.wxSTC_HJ_SINGLESTRING,
      wxstc.wxSTC_HJA_DOUBLESTRING,
      wxstc.wxSTC_HJA_SINGLESTRING,
      wxstc.wxSTC_HPHP_HSTRING,
      wxstc.wxSTC_HPHP_SIMPLESTRING,
    },
    lexerdef= {

      wxstc.wxSTC_H_OTHER,
      wxstc.wxSTC_H_ENTITY,
      wxstc.wxSTC_H_VALUE,

      wxstc.wxSTC_HJA_START,
      wxstc.wxSTC_HJA_DEFAULT,
      wxstc.wxSTC_HJA_COMMENT,

      wxstc.wxSTC_HJA_SYMBOLS,
      wxstc.wxSTC_HJA_STRINGEOL,
      wxstc.wxSTC_HJA_REGEX,

      wxstc.wxSTC_HPHP_DEFAULT,

    },
    keywords0 = {
      wxstc.wxSTC_H_TAG,
      wxstc.wxSTC_H_ATTRIBUTE,

      wxstc.wxSTC_HPHP_OPERATOR,

      wxstc.wxSTC_HJA_KEYWORD,
    },
    keywords1 = {wxstc.wxSTC_H_TAGUNKNOWN,
      wxstc.wxSTC_H_ATTRIBUTEUNKNOWN,
      wxstc.wxSTC_HJ_WORD,
      wxstc.wxSTC_HPHP_WORD,
      wxstc.wxSTC_HJA_WORD,
      wxstc.wxSTC_HPHP_VARIABLE,
      wxstc.wxSTC_HPHP_HSTRING_VARIABLE,},
    keywords2 = {wxstc.wxSTC_H_SCRIPT,
      wxstc.wxSTC_HJ_KEYWORD,
      wxstc.wxSTC_HJ_REGEX,},
    keywords3 = {wxstc.wxSTC_HJ_SYMBOLS,},
    keywords4 = {
      wxstc.wxSTC_HJ_START,},
    keywords5 = {wxstc.wxSTC_LUA_WORD6,},
    keywords6 = {wxstc.wxSTC_LUA_WORD7,},
    keywords7 = {wxstc.wxSTC_LUA_WORD8,},
    preprocessor= {wxstc.wxSTC_LUA_PREPROCESSOR,},
  },

  keywords = {
    -- HTML tags and attributes
    [[ a abbr acronym address applet area b base basefont bdo big blockquote
    body br button caption center cite code col colgroup dd del dfn dir div
    dl dt em fieldset font form frame frameset h1 h2 h3 h4 h5 h6 head hr html
    i iframe img input ins isindex kbd label legend li link map menu meta
    noframes noscript object ol optgroup option p param pre q s samp script
    select small span strike strong style sub sup table tbody td textarea
    tfoot th thead title tr tt u ul var

    class id style title dir lang onclick ondblclick onmousedown onmouseup
    onmouseover onmousemove onmouseout onkeypress onkeydown onkeyup
    accesskey charset coords href hreflang name onblur onfocus rel rev
    shape tabindex target type
    align alt archive code codebase width height hspace name object vspace
    nohref color face size cite alink background bgcolor link onload
    onunload text vlink clear disabled type value char charoff span
    datetime compact color action accept accept-charset enctype method
    onreset onsubmit frameborder longdesc marginwidth marginheight
    noresize scrolling src cols rows profile noshade version
    border ismap usemap checked maxlength readonly onchange onselect
    prompt for http-equiv content scheme classid codetype data declare
    standby start disabled label selected defer event language for
    multiple media cellpadding cellspacing rules summary
    axis headers scope image hidden radio]],
    -- javascript keywords
    [[break else end false for function if
    in var null return true while undefined this]],
    -- VBScript keywords
    "",
    -- Python keywords
    "",
    -- PHP keywords (used for Lua however)
    [[and break do else elseif end false for function if
    in local nil not or repeat return then true until while]],
    -- XML
    "",
    -- SGML
    "",
    -- SGML block
    ""
  },
}
