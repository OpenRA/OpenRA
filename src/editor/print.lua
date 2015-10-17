-- Copyright 2015 Paul Kulchenko, ZeroBrane LLC
---------------------------------------------------------

local frame = ide:GetMainFrame()
local margin = {top = 10, left = 10, bottom = 10, right = 10}
local function printScaling(dc, printOut)
  local pageSizeMM_x, pageSizeMM_y = printOut:GetPageSizeMM()

  local ppiScr_x, ppiScr_y = printOut:GetPPIScreen()
  local ppiPrn_x, ppiPrn_y = printOut:GetPPIPrinter()

  local ppi_scale_x = ppiPrn_x/ppiScr_x
  local ppi_scale_y = ppiPrn_y/ppiScr_y

  -- get the size of DC in pixels and the number of pixels in the page
  local dcSize_x, dcSize_y = dc:GetSize()
  local pagePixSize_x, pagePixSize_y = printOut:GetPageSizePixels()

  local dc_pagepix_scale_x = dcSize_x/pagePixSize_x
  local dc_pagepix_scale_y = dcSize_y/pagePixSize_y

  local dc_scale_x = ppi_scale_x * dc_pagepix_scale_x
  local dc_scale_y = ppi_scale_y * dc_pagepix_scale_y

  -- calculate the pixels / mm (25.4 mm = 1 inch)
  local ppmm_x = ppiScr_x / 25.4
  local ppmm_y = ppiScr_y / 25.4

  -- adjust the page size for the pixels / mm scaling factor
  local page_x    = math.floor(pageSizeMM_x * ppmm_x)
  local page_y    = math.floor(pageSizeMM_y * ppmm_y)
  local pageRect  = wx.wxRect(0, 0, page_x, page_y)

  -- get margins informations and convert to printer pixels
  local top    = math.floor(margin.top    * ppmm_y)
  local bottom = math.floor(margin.bottom * ppmm_y)
  local left   = math.floor(margin.left   * ppmm_x)
  local right  = math.floor(margin.right  * ppmm_x)

  dc:SetUserScale(dc_scale_x, dc_scale_y)

  local printRect = wx.wxRect(left, top, page_x-(left+right), page_y-(top+bottom))
  return printRect, pageRect
end

local function connectPrintEvents(printer, printOut)
  local editor = ide:GetEditorWithFocus()
  local cfg = ide.config.print
  local pages

  function printOut:OnPrintPage(pageNum)
    local dc = self:GetDC()
    local printRect, pageRect = printScaling(dc, printOut)

    -- print to an area smaller by the height of the header/footer
    dc:SetFont(editor:GetFont())
    local _, headerHeight = dc:GetTextExtent("qH")
    local textRect = wx.wxRect(printRect)
    if cfg.header then
      textRect:SetY(textRect:GetY() + headerHeight*1.5)
      textRect:SetHeight(textRect:GetHeight() - headerHeight*1.5)
    end
    if cfg.footer then
      textRect:SetHeight(textRect:GetHeight() - headerHeight*1.5)
    end

    local selection = printer:GetPrintDialogData():GetSelection()
    local spos = selection and editor:GetSelectionStart() or 1
    local epos = selection and editor:GetSelectionEnd() or editor:GetLength()
    if pageNum == nil then
      pages = {}
      ide:PushStatus("")
      printOut.startTime = wx.wxNow()
      local pos = spos
      while pos < epos do
        table.insert(pages, pos)
        pos = editor:FormatRange(false, pos, epos, dc, dc, textRect, pageRect)
        ide:PopStatus()
        ide:PushStatus(TR("%s%% formatted..."):format(math.floor((pos-spos)*100.0/(epos-spos))))
      end
      if #pages == 0 then pages = {0} end
      ide:PopStatus()
    else
      ide:SetStatusFor(TR("Formatting page %d..."):format(pageNum))
      editor:FormatRange(true, pages[pageNum], epos, dc, dc, textRect, pageRect)

      local c = wx.wxColour(127, 127, 127)
      dc:SetPen(wx.wxPen(c, 1, wx.wxSOLID))
      dc:SetTextForeground(c)

      local doc = ide:GetDocument(editor)
      local format = "([^\t]*)\t?([^\t]*)\t?([^\t]*)"
      local placeholders = {
        D = printOut.startTime,
        p = pageNum,
        P = #pages,
        S = doc and doc:GetFileName() or "",
      }
      dc:SetFont(editor:GetFont())
      if cfg.header then
        local left, center, right = ExpandPlaceholders(cfg.header, placeholders):match(format)
        dc:DrawText(left, printRect.X, printRect.Y)
        dc:DrawText(center, printRect.Left + (printRect.Left + printRect.Width - dc:GetTextExtentSize(center).Width)/2, printRect.Y)
        dc:DrawText(right, printRect.Left + printRect.Width - dc:GetTextExtentSize(right).Width,  printRect.Y)
        dc:DrawLine(printRect.X, printRect.Y + headerHeight, printRect.Left + printRect.Width, printRect.Y + headerHeight)
      end
      if cfg.footer then
        local footerY = printRect.Y + printRect.Height - headerHeight
        local left, center, right = ExpandPlaceholders(cfg.footer, placeholders):match(format)
        dc:DrawText(left, printRect.X, footerY)
        dc:DrawText(center, printRect.Left + (printRect.Left + printRect.Width - dc:GetTextExtentSize(center).Width)/2, footerY)
        dc:DrawText(right, printRect.Left + printRect.Width - dc:GetTextExtentSize(right).Width,  footerY)
        dc:DrawLine(printRect.X, footerY, printRect.Left + printRect.Width, footerY)
      end
    end
    return true
  end
  function printOut:HasPage(pageNum) return pages[pageNum] ~= nil end
  function printOut:GetPageInfo()
    -- on Linux `GetPageInfo` is called before the canvas is initialized, which prevents
    -- proper calculation of the number of pages (wx2.9.5).
    -- Return defaults here as it's going to be called once more in the right place.
    if ide.osname == "Unix" and not pages then return 1, 9999, 1, 9999 end
    local printDD = printer:GetPrintDialogData()
    -- due to wxwidgets bug (http://trac.wxwidgets.org/ticket/17200), if `to` page is not set explicitly,
    -- only one page is being printed when `selection` option is selected in the print dialog.
    if printDD:GetSelection() then printDD:SetToPage(#pages) end -- set the page as a workaround
    local tofrom = not printDD:GetSelection() and not printDD:GetAllPages()
    return 1, #pages, tofrom and printDD:GetFromPage() or 1, tofrom and printDD:GetToPage() or #pages
  end
  function printOut:OnPreparePrinting() self:OnPrintPage() end
end

frame:Connect(ID_PAGESETUP, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local pageSetupDD = wx.wxPageSetupDialogData()
    pageSetupDD.MarginTopLeft     = wx.wxPoint(margin.left, margin.top)
    pageSetupDD.MarginBottomRight = wx.wxPoint(margin.right, margin.bottom)
    pageSetupDD:EnableOrientation(false)
    pageSetupDD:EnablePaper(false)

    local pageSetupDialog = wx.wxPageSetupDialog(frame, pageSetupDD)
    pageSetupDialog:ShowModal()
    pageSetupDD = pageSetupDialog:GetPageSetupDialogData()
    margin.top, margin.left = pageSetupDD.MarginTopLeft.y, pageSetupDD.MarginTopLeft.x
    margin.bottom, margin.right = pageSetupDD.MarginBottomRight.y, pageSetupDD.MarginBottomRight.x
  end)

frame:Connect(ID_PRINT, wx.wxEVT_COMMAND_MENU_SELECTED,
  function (event)
    local cfg = ide.config.print
    local editor = ide:GetEditorWithFocus()
    editor:SetPrintMagnification(cfg.magnification)
    editor:SetPrintColourMode(cfg.colourmode)
    editor:SetPrintWrapMode(cfg.wrapmode)

    -- only enable selection if there is something selected in the editor (ignore multiple selections)
    local printDD = wx.wxPrintDialogData()
    printDD:EnableSelection(editor:GetSelectionStart() ~= editor:GetSelectionEnd())

    local printer  = wx.wxPrinter(printDD)
    local luaPrintout = wx.wxLuaPrintout()
    connectPrintEvents(printer, luaPrintout)

    -- save and hide indicators
    local indics = {}
    for _, num in pairs(ide:GetIndicators()) do
      indics[num] = editor:IndicatorGetStyle(num)
      editor:IndicatorSetStyle(num, wxstc.wxSTC_INDIC_HIDDEN)
    end
    -- bold keywords
    local keywords = {}
    for _, num in ipairs(ide:IsValidProperty(editor, 'spec') and editor.spec.lexerstyleconvert and editor.spec.lexerstyleconvert.keywords0 or {}) do
      keywords[num] = editor:StyleGetBold(num)
      editor:StyleSetBold(num, true)
    end
    local ok = printer:Print(frame, luaPrintout, true)
    -- restore indicators
    for n, style in pairs(indics) do editor:IndicatorSetStyle(n, style) end
    for n, style in pairs(keywords) do editor:StyleSetBold(n, style) end
    if not ok and printer:GetLastError() == wx.wxPRINTER_ERROR then
      ReportError("There was a problem while printing.\nCheck if your current printer is set correctly.")
    end
  end)

frame:Connect(ID_PRINT, wx.wxEVT_UPDATE_UI, function(event) event:Enable(ide:GetEditorWithFocus() ~= nil) end)

local _, menu, epos = ide:FindMenuItem(ID.EXIT)
-- disable printing on Unix/Linux as it generates incorrect layout (wx2.9.5, wx3.1)
if ide.osname ~= "Unix" and menu and epos then
  -- insert Print-repated menu items (going in the opposite order)
  menu:Insert(epos-1, ID_PAGESETUP, TR("Page Setup..."), "")
  menu:Insert(epos-1, ID_PRINT, TR("&Print..."), TR("Print the current document"))
  menu:InsertSeparator(epos-1)
end
