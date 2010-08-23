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
			var settings = Game.Settings;

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
				
				var map = Game.modData.AvailableMaps.Keys.FirstOrDefault();
				
				settings.LastServerTitle = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;
				settings.ListenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				settings.ExternalPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				settings.Save();

				Server.Server.ServerMain(Game.modData, settings, map);

				Game.JoinServer(IPAddress.Loopback.ToString(), settings.ListenPort);
				return true;
			};
			
			cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text = settings.LastServerTitle;
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = settings.ExternalPort.ToString();
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").Checked = () => settings.AdvertiseOnline;
			cs.GetWidget("CHECKBOX_ONLINE").OnMouseDown = mi => {
				settings.AdvertiseOnline ^= true;
				settings.Save();
				return true;	
			};
			cs.GetWidget<CheckboxWidget>("CHECKBOX_CHEATS").Checked = () => settings.AllowCheats;
			cs.GetWidget<CheckboxWidget>("CHECKBOX_CHEATS").OnMouseDown = mi => {
				settings.AllowCheats ^=true;
				settings.Save();
				return true;
			};
		}
	}
}
