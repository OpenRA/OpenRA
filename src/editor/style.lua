-- Copyright 2011-15 Paul Kulchenko, ZeroBrane LLC
-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
----------
-- Style
--
-- common style attributes
-- ---------------------------
-- fg foreground - {r,g,b} 0-255
-- bg background - {r,g,b} 0-255
-- alpha translucency - 0-255 (0 - transparent, 255 - opaque, 256 - opaque/faster)
-- sel color of the selected block - {r,g,b} 0-255 (only applies to folds)
-- u underline - boolean
-- b bold - boolean
-- i italic - boolean
-- fill fill to end - boolean
-- fn font Face Name - string ("Lucida Console")
-- fs font size - number (11)
-- hs turn hotspot on - true or {r,g,b} 0-255
-- v visibility for symbols of the current style - boolean

local unpack = table.unpack or unpack

function StylesGetDefault()
  return {
    -- lexer specific (inherit fg/bg from text)
    lexerdef = {fg = {160, 160, 160}},
    comment = {fg = {128, 128, 128}},
    stringtxt = {fg = {128, 32, 16}},
    stringeol = {fg = {128, 32, 16}, bg = {224, 192, 224}, fill = true},
    preprocessor = {fg = {128, 128, 0}},
    operator = {fg = {64, 64, 64}},
    number = {fg = {80, 112, 255}},

    keywords0 = {fg = {32, 32, 192}},
    keywords1 = {fg = {127, 32, 96}},
    keywords2 = {fg = {32, 127, 96}},
    keywords3 = {fg = {64, 32, 96}},
    keywords4 = {fg = {127, 0, 95}},
    keywords5 = {fg = {35, 95, 175}},
    keywords6 = {fg = {0, 127, 127}},
    keywords7 = {fg = {240, 255, 255}},

    -- common (inherit fg/bg from text)
    text = {fg = {64, 64, 64}, bg = {250, 250, 250}},
    linenumber = {fg = {128, 128, 128}, bg = {250, 250, 250}},
    bracematch = {fg = {32, 128, 255}, b = true},
    bracemiss = {fg = {255, 128, 32}, b = true},
    ctrlchar = nil,
    indent = {fg = {192, 192, 230}, bg = {255, 255, 255}},
    calltip = nil,

    -- common special (need custom fg & bg)
    sel = {bg = {208, 208, 208}},
    caret = {fg = {0, 0, 0}},
    caretlinebg = {bg = {240, 240, 230}},
    fold = {fg = {192, 192, 192}, bg = {250, 250, 250}, sel = {160, 128, 224}},
    whitespace = nil,
    edge = {},

    -- deprecated; allowed for backward compatibility in case someone does
    -- fncall.fg = {...}
    fncall = {},

    -- markup
    ['|'] = {fg = {127, 0, 127}},
    ['`'] = {fg = {64, 128, 64}},
    ['['] = {hs = {32, 32, 127}},

    -- markers
    marker = {
      currentline = {},
      breakpoint = {},
      message = {},
      output = {},
      prompt = {},
      error = {},
      searchmatchfile = {},
    },

    -- indicators
    indicator = {
      fncall = {},
      varlocal = {},
      varglobal = {},
      varmasking = {},
      varmasked = {},
      searchmatch = {},
    },
  }
end

local markers = {
  breakpoint = {0, wxstc.wxSTC_MARK_CIRCLE, wx.wxColour(196, 64, 64), wx.wxColour(220, 64, 64)},
  bookmark = {1, wxstc.wxSTC_MARK_SHORTARROW, wx.wxColour(16, 96, 128), wx.wxColour(96, 160, 220)},
  currentline = {2, wxstc.wxSTC_MARK_ARROW, wx.wxColour(16, 128, 16), wx.wxColour(64, 220, 64)},
  message = {3, wxstc.wxSTC_MARK_CHARACTER+(' '):byte(), wx.wxBLACK, wx.wxColour(220, 220, 220)},
  output = {4, wxstc.wxSTC_MARK_BACKGROUND, wx.wxBLACK, wx.wxColour(240, 240, 240)},
  prompt = {5, wxstc.wxSTC_MARK_ARROWS, wx.wxBLACK, wx.wxColour(220, 220, 220)},
  error = {6, wxstc.wxSTC_MARK_BACKGROUND, wx.wxBLACK, wx.wxColour(255, 220, 220)},
  searchmatchfile = {7, wxstc.wxSTC_MARK_EMPTY, wx.wxBLACK, wx.wxColour(196, 0, 0)},
}
function StylesGetMarker(marker) return unpack(markers[marker] or {}) end
function StylesRemoveMarker(marker) markers[marker] = nil end
function StylesAddMarker(marker, ch, fg, bg)
  local num = (markers[marker] or {})[1]
  if not num then -- new marker; find the smallest available marker number
    local nums = {}
    for _, mark in pairs(markers) do nums[mark[1]] = true end
    num = #nums + 1
    if num > 24 then return end -- 24 markers with no pre-defined functions
  end
  markers[marker] = {num, ch, wx.wxColour(unpack(fg)), wx.wxColour(unpack(bg))}
  return num
end

local function applymarker(editor,marker,clrfg,clrbg,clrsel)
  if (clrfg) then editor:MarkerSetForeground(marker,clrfg) end
  if (clrbg) then editor:MarkerSetBackground(marker,clrbg) end
  if (ide.wxver >= "2.9.5" and clrsel) then editor:MarkerSetBackgroundSelected(marker,clrsel) end
end
local specialmapping = {
  sel = function(editor,style)
    if (style.fg) then
      editor:SetSelForeground(1,wx.wxColour(unpack(style.fg)))
    else
      editor:SetSelForeground(0,wx.wxWHITE)
    end
    if (style.bg) then
      editor:SetSelBackground(1,wx.wxColour(unpack(style.bg)))
    else
      editor:SetSelBackground(0,wx.wxWHITE)
    end
    if (style.alpha and ide.wxver >= "2.9.5") then
      editor:SetSelAlpha(style.alpha)
    end

    -- set alpha for additional selecton: 0 - transparent, 255 - opaque
    if ide.wxver >= "2.9.5" then editor:SetAdditionalSelAlpha(127) end
  end,

  seladd = function(editor,style)
    if ide.wxver >= "2.9.5" then
      if (style.fg) then
        editor:SetAdditionalSelForeground(wx.wxColour(unpack(style.fg)))
      end
      if (style.bg) then
        editor:SetAdditionalSelBackground(wx.wxColour(unpack(style.bg)))
      end
      if (style.alpha) then
        editor:SetAdditionalSelAlpha(style.alpha)
      end
    end
  end,

  caret = function(editor,style)
    if (style.fg) then
      editor:SetCaretForeground(wx.wxColour(unpack(style.fg)))
    end
  end,

  caretlinebg = function(editor,style)
    if (style.bg) then
      editor:SetCaretLineBackground(wx.wxColour(unpack(style.bg)))
    end
    if (style.alpha and ide.wxver >= "2.9.5") then
      editor:SetCaretLineBackAlpha(style.alpha)
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

  fold = function(editor,style)
    local clrfg = style.fg and wx.wxColour(unpack(style.fg))
    local clrbg = style.bg and wx.wxColour(unpack(style.bg))
    local clrhi = style.hi and wx.wxColour(unpack(style.hi))
    local clrsel = style.sel and wx.wxColour(unpack(style.sel))

    -- if selected background is set then enable support for it
    if ide.wxver >= "2.9.5" and clrsel then editor:MarkerEnableHighlight(true) end

    if (clrfg or clrbg or clrsel) then
      -- foreground and background are defined as opposite to what I'd expect
      -- for fold markers in Scintilla's terminilogy:
      -- background is the color of fold lines/boxes and foreground is the color
      -- of everything around fold lines or inside fold boxes.
      -- in the following code fg and bg are simply reversed
      local clrfg, clrbg = clrbg, clrfg
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEROPEN, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDER, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERSUB, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERTAIL, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEREND, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDEROPENMID, clrfg, clrbg, clrsel)
      applymarker(editor,wxstc.wxSTC_MARKNUM_FOLDERMIDTAIL, clrfg, clrbg, clrsel)
    end
    if clrbg then
      -- the earlier calls only color the actual markers, but not the
      -- overall fold background; SetFoldMargin calls below do this.
      -- http://community.activestate.com/forum-topic/fold-margin-colors
      -- http://www.scintilla.org/ScintillaDoc.html#SCI_SETFOLDMARGINCOLOUR
      editor:SetFoldMarginColour(true, clrbg)
      editor:SetFoldMarginHiColour(true, clrbg)
    end
    if clrhi then
      editor:SetFoldMarginHiColour(true, clrhi)
    end
  end,

  edge = function(editor,style)
    if style.fg or style.col or style.mode then
      editor:SetEdgeColour(wx.wxColour(unpack(style.fg or {220, 220, 220})))
      editor:SetEdgeMode(style.mode or wxstc.wxSTC_EDGE_LINE)
      editor:SetEdgeColumn(style.col or 80)
    end
  end,

  marker = function(editor,markers)
    for m, style in pairs(markers) do
      local id, ch, fg, bg = StylesGetMarker(m)
      if style.ch then ch = style.ch end
      if style.fg then fg = wx.wxColour(unpack(style.fg)) end
      if style.bg then bg = wx.wxColour(unpack(style.bg)) end
      editor:MarkerDefine(id, ch, fg, bg)
    end
  end,

  auxwindow = function(editor,style)
    if not style then return end

    -- don't color toolbars as they have their own color/style
    local skipcolor = {wxAuiToolBar = true, wxToolBar = true}
    local default = wxstc.wxSTC_STYLE_DEFAULT
    local bg = style.bg and wx.wxColour(unpack(style.bg)) or editor:StyleGetBackground(default)
    local fg = style.fg and wx.wxColour(unpack(style.fg)) or editor:StyleGetForeground(default)

    local uimgr = ide.frame.uimgr
    local panes = uimgr:GetAllPanes()
    for index = 0, panes:GetCount()-1 do
      local wind = uimgr:GetPane(panes:Item(index).name).window

      -- wxlua compiled with STL doesn't provide GetChildren() method
      -- as per http://sourceforge.net/p/wxlua/mailman/message/32500995/
      local ok, children = pcall(function() return wind:GetChildren() end)
      if not ok then break end

      for child = 0, children:GetCount()-1 do
        local data = children:Item(child):GetData()
        local _, window = pcall(function() return data:DynamicCast("wxWindow") end)
        if window and not skipcolor[window:GetClassInfo():GetClassName()] then
          window:SetBackgroundColour(bg)
          window:SetForegroundColour(fg)
          window:Refresh()
        end
      end
    end
  end,
}

local defaultmapping = {
  text = wxstc.wxSTC_STYLE_DEFAULT,
  linenumber = wxstc.wxSTC_STYLE_LINENUMBER,
  bracematch = wxstc.wxSTC_STYLE_BRACELIGHT,
  bracemiss = wxstc.wxSTC_STYLE_BRACEBAD,
  ctrlchar = wxstc.wxSTC_STYLE_CONTROLCHAR,
  indent = wxstc.wxSTC_STYLE_INDENTGUIDE,
  calltip = wxstc.wxSTC_STYLE_CALLTIP,
}

function StylesApplyToEditor(styles,editor,font,fontitalic,lexerconvert)
  local defaultfg = styles.text and styles.text.fg and wx.wxColour(unpack(styles.text.fg)) or nil
  local defaultbg = styles.text and styles.text.bg and wx.wxColour(unpack(styles.text.bg)) or nil

  local function applystyle(style,id)
    editor:StyleSetFont(id, style.i and fontitalic or font)
    editor:StyleSetBold(id, style.b or false)
    editor:StyleSetUnderline(id, style.u or false)
    editor:StyleSetEOLFilled(id, style.fill or false)

    if style.fn then editor:StyleSetFaceName(id, style.fn) end
    if style.fs then editor:StyleSetSize(id, style.fs) end
    if style.v ~= nil then editor:StyleSetVisible(id, style.v) end

    if style.hs then
      editor:StyleSetHotSpot(id, 1)
      -- if passed a color (table) as value, set it as foreground
      if type(style.hs) == 'table' then
        local color = wx.wxColour(unpack(style.hs))
        editor:SetHotspotActiveForeground(1, color)
      end
      editor:SetHotspotActiveUnderline(1)
      editor:SetHotspotSingleLine(1)
    end

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

  -- set the default linenumber font size based on the editor font size
  if styles.linenumber and not styles.linenumber.fs then
    styles.linenumber.fs = ide.config.editor.fontsize and (ide.config.editor.fontsize - 1) or nil
  end

  for name,style in pairs(styles) do
    if (specialmapping[name]) then
      specialmapping[name](editor,style)
    elseif (defaultmapping[name]) then
      applystyle(style,defaultmapping[name])
    end

    if (lexerconvert and lexerconvert[name]) then
      local targets = lexerconvert[name]
      for _, outid in pairs(targets) do
        applystyle(style,outid)
      end
    -- allow to specify style numbers, but exclude those styles
    -- that may conflict with indicator numbers
    elseif (style.st and style.st > 8 and style.st < wxstc.wxSTC_STYLE_DEFAULT) then
      applystyle(style,style.st)
    end
  end

  -- additional selection (seladd) attributes can only be set after
  -- normal selection (sel) attributes are set, so handle them again
  if styles.seladd then specialmapping.seladd(editor, styles.seladd) end

  -- calltip has a special style that needs to be enabled
  if styles.calltip then editor:CallTipUseStyle(2) end

  do
    local defaultfg = {127,127,127}
    local indic = styles.indicator or {}

    -- use styles.fncall if not empty and if indic.fncall is empty
    -- for backward compatibility
    if type(styles.fncall) == 'table' and next(styles.fncall)
    and not (type(indic.fncall) == 'table' and next(indic.fncall)) then indic.fncall = styles.fncall end

    local fncall = ide:AddIndicator("core.fncall")
    local varlocal = ide:AddIndicator("core.varlocal")
    local varglobal = ide:AddIndicator("core.varglobal")
    local varmasking = ide:AddIndicator("core.varmasking")
    local varmasked = ide:AddIndicator("core.varmasked")
    local searchmatch = ide:AddIndicator("core.searchmatch")

    editor:IndicatorSetStyle(fncall, indic.fncall and indic.fncall.st or ide.wxver >= "2.9.5" and wxstc.wxSTC_INDIC_ROUNDBOX or wxstc.wxSTC_INDIC_TT)
    editor:IndicatorSetForeground(fncall, wx.wxColour(unpack(indic.fncall and indic.fncall.fg or {128, 128, 255})))
    editor:IndicatorSetStyle(varlocal, indic.varlocal and indic.varlocal.st or wxstc.wxSTC_INDIC_DOTS or wxstc.wxSTC_INDIC_TT)
    editor:IndicatorSetForeground(varlocal, wx.wxColour(unpack(indic.varlocal and indic.varlocal.fg or defaultfg)))
    editor:IndicatorSetStyle(varglobal, indic.varglobal and indic.varglobal.st or wxstc.wxSTC_INDIC_PLAIN)
    editor:IndicatorSetForeground(varglobal, wx.wxColour(unpack(indic.varglobal and indic.varglobal.fg or defaultfg)))
    editor:IndicatorSetStyle(varmasking, indic.varmasking and indic.varmasking.st or wxstc.wxSTC_INDIC_DASH or wxstc.wxSTC_INDIC_DIAGONAL)
    editor:IndicatorSetForeground(varmasking, wx.wxColour(unpack(indic.varmasking and indic.varmasking.fg or defaultfg)))
    editor:IndicatorSetStyle(varmasked, indic.varmasked and indic.varmasked.st or wxstc.wxSTC_INDIC_STRIKE)
    editor:IndicatorSetForeground(varmasked, wx.wxColour(unpack(indic.varmasked and indic.varmasked.fg or defaultfg)))
    editor:IndicatorSetStyle(searchmatch, indic.searchmatch and indic.searchmatch.st or wxstc.wxSTC_INDIC_BOX)
    editor:IndicatorSetForeground(searchmatch, wx.wxColour(unpack(indic.searchmatch and indic.searchmatch.fg or {196, 0, 0})))
  end
end

function ReApplySpecAndStyles()
  -- re-register markup styles as they are special:
  -- these styles need to be updated as they are based on comment styles
  if MarkupAddStyles then MarkupAddStyles(ide.config.styles) end

  local errorlog = ide.frame.bottomnotebook.errorlog
  local shellbox = ide.frame.bottomnotebook.shellbox
  SetupKeywords(shellbox,"lua",nil,ide.config.stylesoutshell,ide.font.oNormal,ide.font.oItalic)
  StylesApplyToEditor(ide.config.stylesoutshell,errorlog,ide.font.oNormal,ide.font.oItalic)

  for _, doc in pairs(ide:GetDocuments()) do
    if doc.editor.spec then doc.editor:SetupKeywords(nil, doc.editor.spec) end
  end
end

function ApplyStyleConfig(config, style)
  if not wx.wxIsAbsolutePath(config)
    then config = MergeFullPath(GetPathWithSep(ide.editorFilename), config) end

  local cfg = {wxstc = wxstc, math = math, print = DisplayOutputLn,
    path = {}, editor = {}, view ={}, acandtip = {}, outputshell = {}, debugger={}}
  local cfgfn, err = loadfile(config)
  if not cfgfn then
    DisplayOutputLn(TR("Error while loading configuration file: %s"):format(err))
    return
  end

  setfenv(cfgfn,cfg)
  cfgfn, err = pcall(cfgfn,style)
  if not cfgfn then
    DisplayOutputLn(TR("Error while processing configuration file: %s"):format(err))
    return
  end

  -- if no style assigned explicitly, but a table is returned, use it
  if not (cfg.styles or cfg.stylesoutshell) and type(err) == 'table' then
    cfg.styles = err
  end

  if cfg.styles or cfg.stylesoutshell then
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
