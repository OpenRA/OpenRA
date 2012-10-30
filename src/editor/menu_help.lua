-- author: Paul Kulchenko
---------------------------------------------------------
local ide = ide
-- ---------------------------------------------------------------------------
-- Create the Help menu and attach the callback functions

local frame = ide.frame
local menuBar = frame.menuBar
local mobdebug = require "mobdebug"

local helpMenu = wx.wxMenu{
  { ID_ABOUT, TR("&About")..KSC(ID_ABOUT), TR("About ZeroBrane Studio") },
}
-- do not translate Help menu on Mac as it won't merge with "standard" menus
menuBar:Append(helpMenu, ide.osname == 'Macintosh' and "&Help" or TR("&Help"))

local function DisplayAbout(event)
  local page = [[
    <html>
      <body text="#777777">
	<table border="0" width="100%">
	  <tr><td><img src="zbstudio/res/zerobrane.png"></td></tr>
	  <tr><td>
	<table cellspacing="3" cellpadding="3" width="100%">
	  <tr>
		<td>
		<b>ZeroBrane Studio (]]..ide.VERSION..[[; MobDebug ]]..mobdebug._VERSION..[[)</b><br>
		<b>Copyright &copy; 2011-2012 ZeroBrane LLC</b><br>
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
		<b>Based on wxLua editor (]]..wxlua.wxLUA_VERSION_STRING..[[)</b><br>
		<b>Copyright &copy; 2002-2005 Lomtick Software</b><br>
		J. Winwood, John Labenski<br>
		Licensed under wxWindows Library License, v3.
		</td>
	  </tr>
	  <tr>
		<td>
                <b>Built with ]]..wx.wxVERSION_STRING..[[</b>
		</td>
	  </tr>
	</table>
	</td></tr></table>
      </body>
    </html>]]

  local dlg = wx.wxDialog(frame, wx.wxID_ANY, TR("About ZeroBrane Studio"))
  local html = wx.wxLuaHtmlWindow(dlg, wx.wxID_ANY,
    wx.wxDefaultPosition, wx.wxSize(440, 270),
    wx.wxHW_SCROLLBAR_NEVER)
  local line = wx.wxStaticLine(dlg, wx.wxID_ANY)
  local button = wx.wxButton(dlg, wx.wxID_OK, "OK")

  button:SetDefault()

  html:SetBorders(0)
  html:SetPage(page)
  html:SetSize(html:GetInternalRepresentation():GetWidth(),
    html:GetInternalRepresentation():GetHeight())

  local topsizer = wx.wxBoxSizer(wx.wxVERTICAL)
  topsizer:Add(html, 1, wx.wxALL, 10)
  topsizer:Add(line, 0, wx.wxEXPAND + wx.wxLEFT + wx.wxRIGHT, 10)
  topsizer:Add(button, 0, wx.wxALL + wx.wxALIGN_RIGHT, 10)
  topsizer:Fit(dlg)

  dlg:SetAutoLayout(true)
  dlg:SetSizer(topsizer)
  dlg:ShowModal()
  dlg:Destroy()
end

frame:Connect(ID_ABOUT, wx.wxEVT_COMMAND_MENU_SELECTED, DisplayAbout)
