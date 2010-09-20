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
				Widget.OpenWindow("CREATESERVER_BG");
				return true;
			};
			
			cs.GetWidget("BUTTON_CANCEL").OnMouseUp = mi => {
				Widget.CloseWindow();
				return true;
			};
			
			cs.GetWidget("BUTTON_START").OnMouseUp = mi => {
				Widget.OpenWindow("SERVER_LOBBY");
				
				var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
				
				settings.Server.Name = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;
				settings.Server.ListenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				settings.Server.ExternalPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				settings.Save();

				Server.Server.ServerMain(Game.modData, settings, map);

				Game.JoinServer(IPAddress.Loopback.ToString(), settings.Server.ListenPort);
				return true;
			};
			
			cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text = settings.Server.Name;
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = settings.Server.ExternalPort.ToString();
			cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE").Checked = () => settings.Server.AdvertiseOnline;
			cs.GetWidget("CHECKBOX_ONLINE").OnMouseDown = mi => {
				settings.Server.AdvertiseOnline ^= true;
				settings.Save();
				return true;	
			};
			cs.GetWidget<CheckboxWidget>("CHECKBOX_CHEATS").Checked = () => settings.Server.AllowCheats;
			cs.GetWidget<CheckboxWidget>("CHECKBOX_CHEATS").OnMouseDown = mi => {
				settings.Server.AllowCheats ^=true;
				settings.Save();
				return true;
			};
		}
	}
}
