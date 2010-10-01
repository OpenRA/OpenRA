#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Server;
using System.Net;
using System.IO;

namespace OpenRA.Widgets.Delegates
{
	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public MainMenuButtonsDelegate( [ObjectCreator.Param( "widget" )] Widget widget )
		{
			// Main menu is the default window
			widget.GetWidget( "MAINMENU_BUTTON_JOIN" ).OnMouseUp = mi => { Widget.OpenWindow( "JOINSERVER_BG" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_CREATE" ).OnMouseUp = mi => { Widget.OpenWindow( "CREATESERVER_BG" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_SETTINGS" ).OnMouseUp = mi => { Widget.OpenWindow( "SETTINGS_MENU" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_MUSIC" ).OnMouseUp = mi => { Widget.OpenWindow( "MUSIC_MENU" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_QUIT" ).OnMouseUp = mi => { Game.Exit(); return true; };

			var version = widget.GetWidget<LabelWidget>("VERSION_STRING");

			var motd = widget.GetWidget<ScrollingTextWidget>("MOTD_SCROLLER");

			if (FileSystem.Exists("VERSION"))
			{
				var s = FileSystem.Open("VERSION");
				version.Text = s.ReadAllText();
				s.Close();
				MasterServerQuery.OnVersion += v => { if (!string.IsNullOrEmpty(v)) version.Text += "\nLatest: " + v; };
				MasterServerQuery.GetCurrentVersion(Game.Settings.Server.MasterServer);
			}
			else
			{
				version.Text = "Dev Build";
			}

			if (motd != null)
			{
				motd.Text = "Welcome to OpenRA. MOTD unable to be loaded from server.";

				string URL = "http://open-ra.org/master/motd.php?v=" + version.Text;

				WebRequest req = WebRequest.Create(URL);
				StreamReader reader = null;
				try
				{
					reader = new StreamReader(req.GetResponse().GetResponseStream());
				}
				catch (WebException e)
				{
					reader.Close();
					return;
				}
				var result = reader.ReadToEnd();
				reader.Close();
				
				motd.Text = result;
			}
		}
	}
}
