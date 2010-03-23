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

namespace OpenRA.Widgets.Delegates
{
	public class CreateServerMenuDelegate : WidgetDelegate
	{
		static bool AdvertiseServerOnline = Game.Settings.InternetServer;

		public override bool GetState(Widget w)
		{
			if (w.Id == "CREATESERVER_CHECKBOX_ONLINE")
				return AdvertiseServerOnline;

			return false;
		}

		public override bool OnMouseDown(Widget w, MouseInput mi)
		{
			if (w.Id == "CREATESERVER_CHECKBOX_ONLINE")
			{
				AdvertiseServerOnline = !AdvertiseServerOnline;
				return true;
			}

			return false;
		}

		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			if (w.Id == "MAINMENU_BUTTON_CREATE")
			{
				Game.chrome.rootWidget.ShowMenu("CREATESERVER_BG");
				return true;
			}

			if (w.Id == "CREATESERVER_BUTTON_CANCEL")
			{
				Game.chrome.rootWidget.ShowMenu("MAINMENU_BG");
				return true;
			}

			if (w.Id == "CREATESERVER_BUTTON_START")
			{
				Game.chrome.rootWidget.ShowMenu(null);
				Log.Write("Creating server");

				Server.Server.ServerMain(AdvertiseServerOnline, Game.Settings.MasterServer,
										Game.Settings.GameName, Game.Settings.ListenPort,
										Game.Settings.ExternalPort, Game.Settings.InitialMods);

				Log.Write("Joining server");
				Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.ListenPort);
				return true;
			}

			return false;
		}
	}
}
