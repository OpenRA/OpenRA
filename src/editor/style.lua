-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
----------
-- Style
--
-- common style attributes
-- ---------------------------
-- fg foreground - {r,g,b} 0-255
-- bg background - {r,g,b} 0-255
-- u underline - boolean
-- b bold - boolean
-- i italic - boolean
-- fill fill to end - boolean

function StylesGetDefault()
  return {
    -- lexer specific (inherit fg/bg from text)
    lexerdef = {fg = {128, 128, 128},},
    comment = {fg = {0, 127, 0 },bg = {240, 240, 220}, fill= true,},
    stringtxt = {fg = {127, 0, 127},},
    stringeol = {fg = {0, 0, 0 },bg = {224, 192, 224}, fill = true, b = true},
    preprocessor = {fg = {127, 127, 0 },},
    operator = {fg = {0, 0, 0 },},
    number = {fg = {90, 100, 0 },},

    keywords0 = {fg = {0, 0, 127}, b = true,},
    keywords1 = {fg = {127, 0, 0},},
    keywords2 = {fg = {0, 127, 0},},
    keywords3 = {fg = {0, 0, 127},},
    keywords4 = {fg = {127, 0, 95},},
    keywords5 = {fg = {35, 95, 175},},
    keywords6 = {fg = {0, 127, 127},},
    keywords7 = {fg = {240, 255, 255},},

    -- common (inherit fg/bg from text)
    text = nil, -- let os pick
    linenumber = {fg = {192, 192, 192},},
    bracematch = {fg = {0, 0, 255}, b = true},
    bracemiss = {fg = {255, 0, 0 }, b = true},
    escapechar = nil,
    indent = {fg = {192, 192, 192},bg = {255, 255, 255},},
    calltip = nil,

    -- common special (need custom fg & bg )
    calltipbg = nil,
    sel = nil,
    caret = nil,
    caretlinebg = nil,
    fold = nil,
    whitespace = {fg = {180, 180, 180},},

    -- indicators
    fncall = {fg = {175,175,255}, st= wxstc.wxSTC_INDIC_BOX},
  }
end

-- used to fill unset bg colors
local defaultfg = nil
local defaultbg = nil

local function applymarker(editor,marker,clrfg,clrbg)
  if (clrfg) then
    editor:MarkerSetForeground(marker,clrfg)
  end
  if (clrbg) then
    editor:MarkerSetBackground(marker,clrbg)
  end
end
local specialmapping = {
  sel = function(editor,style)
    if (style.fg) then
      editor:SetSelForeground(1,wx.wxColour(unpack(style.fg)))
    else
      editor:SetSelForeground(0)
    end
    if (style.bg) then
      editor:SetSelBackground(1,wx.wxColour(unpack(style.bg)))
    else
      editor:SetSelBackground(0)
    end

  end,

  caret = function(editor,style)
    if (style.fg) then
      editor:SetCaretForeground(wx.wxColour(unpack(style.fg)))
    end
    --if (style.bg) then
    -- editor:SetCaretBackground(wx.wxColour(unpack(style.bg)))
    --end
  end,

  caretlinebg = function(editor,style)
    if (style.bg) then
      editor:SetCaretLineBackground(wx.wxColour(unpack(style.bg)))
    end
  end,

  whitespace = function(editor,style)
    if (style.fg) then
      editor:SetWhitespaceForeground(1,wx.wxColour(unpack(style.fg)))
    else
      --editor:SetWhitespaceForeground(0)
    end
    if (style.bg) then
      editor:SetWhitespaceBackground(1,wx.wxColour(unpack(style.bg)))
    else
      --editor:SetWhitespaceBackground(0)
    end
  end,

  calltipbg = function(editor,style)
    if (style.bg) then
      editor:CallTipSetBackground(wx.wxColour(unpack(style.bg)))
    end
  end,

  fold = function(editor,style)
    local clrfg = style.fg and wx.wxColour(unpack(style.fg))
    local clrbg = style.bg and wx.wxColour(unpack(style.bg))

    if (clrfg or clrbg) then
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEROPEN, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDER, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERSUB, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERTAIL, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEREND, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEROPENMID, clrfg,clrbg)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL, clrfg,clrbg)
    end
  end,
}

local defaultmapping = {
  text = wxstc.wxSTC_STYLE_DEFAULT,
  linenumber = wxstc.wxSTC_STYLE_LINENUMBER,
  bracematch = wxstc.wxSTC_STYLE_BRACELIGHT,
  bracemiss = wxstc.wxSTC_STYLE_BRACEBAD,
  escapechar = wxstc.wxSTC_STYLE_CONTROLCHAR,
  indent = wxstc.wxSTC_STYLE_INDENTGUIDE,
  calltip = wxstc.wxSTC_STYLE_CALLTIP,
}

function StylesApplyToEditor(styles,editor,font,fontitalic,lexerconvert)
  local function applystyle(style,id)
    editor:StyleSetFont(id, style.i and fontitalic or font)
    editor:StyleSetBold(id, style.b or false)
    editor:StyleSetUnderline(id, style.u or false)
    editor:StyleSetEOLFilled(id, style.fill or false)

    if (style.fg or defaultfg) then
      editor:StyleSetForeground(id, style.fg and wx.wxColour(unpack(style.fg)) or defaultfg)
    end
    if (style.bg or defaultbg) then
      editor:StyleSetBackground(id, style.bg and wx.wxColour(unpack(style.bg)) or defaultbg)
    end
  end
  editor:StyleResetDefault()
  editor:SetFont(font)
  if (styles.text) then
    applystyle(styles.text,defaultmapping["text"])
  else
    applystyle({},defaultmapping["text"])
  end
  editor:StyleClearAll()

  defaultfg = styles.text and styles.text.fg and wx.wxColour(unpack(styles.text.fg)) or nil
  defaultbg = styles.text and styles.text.bg and wx.wxColour(unpack(styles.text.bg)) or nil

  for name,style in pairs(styles) do
    if (specialmapping[name]) then
      specialmapping[name](editor,style)
    elseif (defaultmapping[name]) then
      applystyle(style,defaultmapping[name])
    end
    if (lexerconvert and lexerconvert[name]) then
      local targets = lexerconvert[name]

      for n,outid in pairs(targets) do
        applystyle(style,outid)
      end
    end
  end

  do
    editor:IndicatorSetStyle(0,styles.fncall and styles.fncall.st or wxstc.wxSTC_INDIC_BOX)
    editor:IndicatorSetForeground(0,wx.wxColour(unpack(styles.fncall and styles.fncall.fg or {128,128,128})))
  end

  editor:Colourise(0, -1)
end

function ReApplySpecAndStyles()
  local openDocuments = ide.openDocuments
  for i,doc in pairs(openDocuments) do
    if (doc.editor.spec) then
      SetupKeywords(doc.editor,nil,doc.editor.spec)
    end
  end

  local errorlog = ide.frame.vsplitter.splitter.bottomnotebook.errorlog
  local shellbox = ide.frame.vsplitter.splitter.bottomnotebook.shellbox

  SetupKeywords(shellbox.input,"lua",nil,ide.config.stylesoutshell,ide.ofont,ide.ofontItalic)

  StylesApplyToEditor(ide.config.stylesoutshell,errorlog,ide.ofont,ide.ofontItalic)
  StylesApplyToEditor(ide.config.stylesoutshell,shellbox.output,ide.ofont,ide.ofontItalic)
end

function LoadConfigStyle()
  local fileDialog = wx.wxFileDialog(ide.frame, "Open Config File",
    "/cfg",
    "",
    "Lua file (*.lua)|*.lua|All files (*)|*",
    wx.wxOPEN + wx.wxFILE_MUST_EXIST)
  if fileDialog:ShowModal() == wx.wxID_OK then
    local cfg = {wxstc = wxstc, path = {}, editor = {}, view ={}, acandtip = {}, outputshell = {}, debugger={},}
    local cfgfn,err = loadfile(fileDialog:GetPath())
    if cfgfn then
      setfenv(cfgfn,cfg)
      cfgfn = xpcall(cfgfn,function(err)DisplayOutput("Error while executing configuration file: \n",debug.traceback(err))end)
    end

    if not (cfgfn and (cfg.styles or cfg.stylesoutshell)) then
      wx.wxMessageBox("Unable to load config style '"..fileDialog:GetPath().."'.",
        "wxLua Error",
        wx.wxOK + wx.wxCENTRE, ide.frame)
    else
      if (cfg.styles) then
        ide.config.styles = StylesGetDefault()
        -- copy
        for i,s in pairs(cfg.styles) do
          ide.config.styles[i] = s
        end
      end
      if (cfg.stylesoutshell) then
        ide.config.stylesoutshell = StylesGetDefault()
        -- copy
        for i,s in pairs(cfg.stylesoutshell) do
          ide.config.stylesoutshell[i] = s
        end
      end
      ReApplySpecAndStyles()
    end
  end
  fileDialog:Destroy()

end

-- used lexers ?
--[=[
#define wxSTC_POV_DEFAULT 0
#define wxSTC_POV_COMMENT 1
#define wxSTC_POV_COMMENTLINE 2
#define wxSTC_POV_NUMBER 3
#define wxSTC_POV_OPERATOR 4
#define wxSTC_POV_IDENTIFIER 5
#define wxSTC_POV_STRING 6
#define wxSTC_POV_STRINGEOL 7

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

#define wxSTC_LUA_DEFAULT 0
#define wxSTC_LUA_COMMENT 1
#define wxSTC_LUA_COMMENTLINE 2
#define wxSTC_LUA_COMMENTDOC 3
#define wxSTC_LUA_NUMBER 4
#define wxSTC_LUA_WORD 5
#define wxSTC_LUA_STRING 6
#define wxSTC_LUA_CHARACTER 7
#define wxSTC_LUA_LITERALSTRING 8
#define wxSTC_LUA_PREPROCESSOR 9
#define wxSTC_LUA_OPERATOR 10
#define wxSTC_LUA_IDENTIFIER 11
#define wxSTC_LUA_STRINGEOL 12
]=]
