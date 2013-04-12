-- author: Paul Kulchenko
---------------------------------------------------------
local ide = ide
-- ---------------------------------------------------------------------------
-- Create the Help menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar
local mobdebug = require "mobdebug"

local helpMenu = wx.wxMenu{
  { ID_ABOUT, TR("&About")..KSC(ID_ABOUT), TR("About %s"):format(GetIDEString("editor")) },
}
-- do not translate Help menu on Mac as it won't merge with "standard" menus
menuBar:Append(helpMenu, ide.osname == 'Macintosh' and "&Help" or TR("&Help"))

local function DisplayAbout(event)
  local logo = ide.config.path.app.."/"..GetIDEString("logo")
  local logoimg = wx.wxFileName(logo):FileExists() and
    ([[<tr><td><img src="%s"></td></tr>]]):format(logo) or ""
  local page = ([[
    <html>
      <body text="#777777">
	<table border="0" width="100%%">
	  %s
	  <tr><td>
	<table cellspacing="3" cellpadding="3" width="100%%">
	  <tr>
		<td>
		<b>ZeroBrane Studio (%s; MobDebug %s)</b><br>
		<b>Copyright &copy; 2011-2013 ZeroBrane LLC</b><br>
		Paul Kulchenko<br>
		Licensed under the MIT License.
		</td>
	  </tr>
	  <tr>
		<td>
		<b>Based on Estrela Editor</b><br>
		<b>Copyright &copy; 2008-2011 Luxinia DevTeam</b><br>
		Christoph Kubisch, Eike Decker<br>
		Licensed under the MIT License.
		</td>
		<td><img align="right" src="zbstudio/res/estrela.png"></td>
	  </tr>
	  <tr>
		<td>
		<b>Based on wxLua editor (%s)</b><br>
		<b>Copyright &copy; 2002-2005 Lomtick Software</b><br>
		J. Winwood, John Labenski<br>
		Licensed under wxWindows Library License, v3.
		</td>
	  </tr>
	  <tr>
		<td>
                <b>Built with %s</b>
		</td>
	  </tr>
	</table>
	</td></tr></table>
      </body>
    </html>]])
  :format(logoimg, ide.VERSION, mobdebug._VERSION,
    wxlua.wxLUA_VERSION_STRING, wx.wxVERSION_STRING)

  local dlg = wx.wxDialog(frame, wx.wxID_ANY, TR("About %s"):format(GetIDEString("editor")))

  -- this is needed because wxLuaHtmlWindow only seems to take into account
  -- the initial size, but not the one set with SetSize using
  -- wxlua 2.8.12.2 and wxwidgets 2.9.5+.
  local tmp = wx.wxLuaHtmlWindow(dlg, wx.wxID_ANY, wx.wxDefaultPosition, wx.wxSize(450, 260))
  tmp:SetPage(page)
  local w = tmp:GetInternalRepresentation():GetWidth()
  local h = tmp:GetInternalRepresentation():GetHeight()
  tmp:Destroy()

  local html = wx.wxLuaHtmlWindow(dlg, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(w, h), wx.wxHW_SCROLLBAR_NEVER)

  html:SetBorders(0)
  html:SetPage(page)

  local line = wx.wxStaticLine(dlg, wx.wxID_ANY)
  local button = wx.wxButton(dlg, wx.wxID_OK, "OK")
  button:SetDefault()

  local topsizer = wx.wxBoxSizer(wx.wxVERTICAL)
  topsizer:Add(html, 1, wx.wxEXPAND + wx.wxALL, 10)
  topsizer:Add(line, 0, wx.wxEXPAND + wx.wxLEFT + wx.wxRIGHT, 10)
  topsizer:Add(button, 0, wx.wxALL + wx.wxALIGN_RIGHT, 10)

  dlg:SetSizerAndFit(topsizer)
  dlg:ShowModal()
  dlg:Destroy()
end

frame:Connect(ID_ABOUT, wx.wxEVT_COMMAND_MENU_SELECTED, DisplayAbout)
