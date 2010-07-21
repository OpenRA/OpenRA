#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using System.Net;

namespace OpenRA.Widgets.Delegates
{
	public class CreateServerMenuDelegate : IWidgetDelegate
	{		
		public CreateServerMenuDelegate()
		{
			var r = Widget.RootWidget;
			var cs = r.GetWidget("CREATESERVER_BG");
			r.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi => {
				r.OpenWindow("CREATESERVER_BG");
				return true;
			};
			
			cs.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
			
			cs.GetWidget("BUTTON_START").OnMouseUp = mi => {
				r.OpenWindow("SERVER_LOBBY");
				
				// TODO: Get this from a map chooser
				string map = Game.AvailableMaps.Keys.FirstOrDefault();
				
				// TODO: Get this from a mod chooser
				var mods = Game.Settings.InitialMods;
				
				Game.Settings.LastServerTitle = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;
				Game.Settings.ListenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				Game.Settings.ExternalPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				Game.Settings.Save();

				Server.Server.ServerMain(Game.Settings.AdvertiseOnline, Game.Settings.MasterServer,	Game.Settings.LastServerTitle,
				                         Game.Settings.ListenPort, Game.Settings.ExternalPort, mods, map);

				Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.ListenPort);
				return true;
			};
			
			cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text = Game.Settings.LastServerTitle;
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = Game.Settings.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = Game.Settings.ExternalPort.ToString();
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").Checked = () => Game.Settings.AdvertiseOnline;
			cs.GetWidget("CHECKBOX_ONLINE").OnMouseDown = mi => {
				Game.Settings.AdvertiseOnline ^= true;
				Game.Settings.Save();
				return true;	
			};
		}
	}
}
