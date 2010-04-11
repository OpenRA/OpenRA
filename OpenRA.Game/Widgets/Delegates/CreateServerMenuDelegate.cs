#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Net;
using System.Linq;
using System.Collections.Generic;
namespace OpenRA.Widgets.Delegates
{
	public class CreateServerMenuDelegate : IWidgetDelegate
	{
		static bool AdvertiseServerOnline = Game.Settings.InternetServer;
		
		public CreateServerMenuDelegate()
		{
			var r = Chrome.rootWidget;
			r.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi => {
				r.OpenWindow("CREATESERVER_BG");
				return true;
			};
			
			r.GetWidget("CREATESERVER_BUTTON_CANCEL").OnMouseUp = mi => {
				r.CloseWindow();
				return true;
			};
			
			r.GetWidget("CREATESERVER_BUTTON_START").OnMouseUp = mi => {
				r.OpenWindow("SERVER_LOBBY");
				Log.Write("Creating server");
				
				// TODO: Get this from a map chooser
				string map = Game.AvailableMaps.Keys.FirstOrDefault();
				
				// TODO: Get this from a mod chooser
				var mods = Game.Settings.InitialMods;
				
				// TODO: Get this from a textbox
				var gameName = Game.Settings.GameName;

				Server.Server.ServerMain(AdvertiseServerOnline, Game.Settings.MasterServer,
										gameName, Game.Settings.ListenPort,
										Game.Settings.ExternalPort, mods, map);

				Log.Write("Joining server");
				Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.ListenPort);
				return true;
			};
			
			r.GetWidget<CheckboxWidget>("CREATESERVER_CHECKBOX_ONLINE").Checked = () => {return AdvertiseServerOnline;};
			r.GetWidget("CREATESERVER_CHECKBOX_ONLINE").OnMouseDown = mi => {
				AdvertiseServerOnline = !AdvertiseServerOnline;
				return true;	
			};
		}
	}
}
