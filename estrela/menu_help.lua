-- authors: Lomtik Software (J. Winwood & John Labenski)
--          Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
-- ---------------------------------------------------------------------------
-- Create the Help menu and attach the callback functions

local frame    = ide.frame
local menuBar  = frame.menuBar

local helpMenu = wx.wxMenu{
		{ ID_ABOUT,      "&About\tF1",       "About Estrela Editor" },
		}
menuBar:Append(helpMenu, "&Help")

local function DisplayAbout(event)
	local page = [[
		<html>
		<body bgcolor = "#ffffff" text='#ffffff'>
		<table border='0' width="100%">
		  <tr><td><img src = "estrela/res/estrela.png"></center></td><td>
		<table cellspacing = 4 cellpadding = 4 width = "400">
		  <tr>
			<td bgcolor = "#010156">
			<center>
				<font size = +2 color = "#FFFFFF"><br><b>]]..
					wxlua.wxLUA_VERSION_STRING..[[</b></font>
				<font size = +1 color = "#FFFFFF">built with</font>
				<font size = +2 color = "#FFFFFF"><b>]]..
					wx.wxVERSION_STRING..[[</b></font>
			</center>
			</td>
		  </tr>
		  <tr>
			<td bgcolor = "#4747A1">
			<b>Estrela Editor</b><br>
			<b><font color='#ffffff'>Copyright &copy; 2008-2011 Luxinia DevTeam</font></b>
			<p>
			<font size=-1>
			  <table cellpadding = 0 cellspacing = 0 width = "100%">
				<tr>
				  <td width = "65%">
					Christoph Kubisch<br>
					Eike Decker<p>
				  </td>
				  <td valign = top>
				  </td>
				</tr>
			  </table>
			<font size = 1>
				Licensed under The MIT License.
			</font>
			</font>
			</td>
		  </tr>
		  <tr>
			<td bgcolor = "#4747A1">
			<b>based on wxLua editor sample</b><br>
			<b>Copyright &copy; 2002-2005 Lomtick Software</b>
			<p>
			<font size=-1>
			  <table cellpadding = 0 cellspacing = 0 width = "100%">
				<tr>
				  <td width = "65%">
					J. Winwood (luascript@thersgb.net)<br>
					John Labenski<p>
				  </td>
				  <td valign = top>
					<img src = "estrela/res/wxlualogo2.png">
				  </td>
				</tr>
			  </table>
			<font size = 1>
				wxLua binding licensed under wxWindows Library License, Version 3.
			</font>
			</font>
			</td>
		  </tr>
		</table>
		</td></tr></table>
		</body>
		</html>
	]]

	local dlg = wx.wxDialog(frame, wx.wxID_ANY, "About Estrela Editor")

	local html = wx.wxLuaHtmlWindow(dlg, wx.wxID_ANY,
									wx.wxDefaultPosition, wx.wxSize(500, 270),
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

	dlg:SetAutoLayout(true)
	dlg:SetSizer(topsizer)
	topsizer:Fit(dlg)

	dlg:ShowModal()
	dlg:Destroy()
end

frame:Connect(ID_ABOUT, wx.wxEVT_COMMAND_MENU_SELECTED, DisplayAbout)
