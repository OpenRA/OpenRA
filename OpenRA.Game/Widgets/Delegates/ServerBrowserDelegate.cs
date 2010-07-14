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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Server;
using OpenRA.FileFormats;
using System;

namespace OpenRA.Widgets.Delegates
{
	public class ServerBrowserDelegate : IWidgetDelegate
	{
		static List<Widget> GameButtons = new List<Widget>();

		GameServer currentServer = null;
		Widget ServerTemplate;

		public ServerBrowserDelegate()
		{
			var r = Chrome.rootWidget;	
			var bg = r.GetWidget("JOINSERVER_BG");
			var dc = r.GetWidget("DIRECTCONNECT_BG");
						
			MasterServerQuery.OnComplete += games => RefreshServerList(games);

			r.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp = mi =>
			{
				r.OpenWindow("JOINSERVER_BG");

				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				MasterServerQuery.Refresh(Game.Settings.MasterServer);

				return true;
			};
			
			bg.GetWidget("SERVER_INFO").IsVisible = () => currentServer != null;
			var preview = bg.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			preview.Map = () => CurrentMap();
			preview.IsVisible = () => CurrentMap() != null;
			
			bg.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			bg.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => string.Join( ",", currentServer.Mods );
			bg.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			bg.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () =>
			{
				if (currentServer == null)
					return "";
				string ret = currentServer.Players.ToString();
				if (CurrentMap() != null)
					ret += "/"+CurrentMap().PlayerCount.ToString();
				return ret;
			};
			
			
			var sl = bg.GetWidget<ListBoxWidget>("SERVER_LIST");
			ServerTemplate = sl.GetWidget<LabelWidget>("SERVER_TEMPLATE");

			bg.GetWidget("REFRESH_BUTTON").OnMouseUp = mi =>
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				MasterServerQuery.Refresh(Game.Settings.MasterServer);

				return true;
			};

			bg.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
			{
				r.CloseWindow();
				return true;
			};

			bg.GetWidget("DIRECTCONNECT_BUTTON").OnMouseUp = mi =>
			{
				r.CloseWindow();
				
				dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text = Game.Settings.LastServer;
				r.OpenWindow("DIRECTCONNECT_BG");
				return true;
			};
			
			bg.GetWidget("JOIN_BUTTON").OnMouseUp = mi =>
			{
				if (currentServer == null)
					return false;
				
				// Todo: Add an error dialog explaining why we aren't letting them join
				// Or even better, reject them server side and display the error in the connection failed dialog.
				
				// Don't bother joining a server with different mods... its only going to crash
				if (currentServer.Mods.SymmetricDifference(Game.LobbyInfo.GlobalSettings.Mods).Any())
				{
					System.Console.WriteLine("Player has different mods to server; not connecting to avoid crash");
					System.Console.WriteLine("FIX THIS BUG YOU NOOB!");
					return false;
				}
				
				// Prevent user joining a full server
				if (CurrentMap() != null && currentServer.Players >= CurrentMap().PlayerCount)
				{
					System.Console.WriteLine("Server is full; not connecting");
					return false;
				}
				
				r.CloseWindow();
				Game.JoinServer(currentServer.Address.Split(':')[0], int.Parse(currentServer.Address.Split(':')[1]));
				Game.SetGameId(currentServer.Id);
				return true;
			};
			
			// Direct Connect
			dc.GetWidget("JOIN_BUTTON").OnMouseUp = mi => {
				
				var address = dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text;
				var cpts = address.Split(':').ToArray();
				if (cpts.Length != 2)
					return true;
				
				Game.Settings.LastServer = address;
				Game.Settings.Save();
				
				r.CloseWindow();
				Game.JoinServer(cpts[0], int.Parse(cpts[1]));
				return true;
			};
			
			dc.GetWidget("CANCEL_BUTTON").OnMouseUp = mi => {
				r.CloseWindow();
				return r.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp(mi);
			};
		}
		
		MapStub CurrentMap()
		{
			return (currentServer == null || !Game.AvailableMaps.ContainsKey(currentServer.Map)) ? null : Game.AvailableMaps[currentServer.Map];	
		}
		
		void RefreshServerList(IEnumerable<GameServer> games)
		{
			var r = Chrome.rootWidget;	
			var bg = r.GetWidget("JOINSERVER_BG");
			var sl = bg.GetWidget<ListBoxWidget>("SERVER_LIST");

			sl.Children.Clear();
			currentServer = null;
			
			if (games == null)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Failed to contact master server.";
				return;
			}

			if (games.Count() == 0)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "No games found.";
				return;
			}

			r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = false;
	
			int offset = ServerTemplate.Bounds.Y;
			int i = 0;
			foreach (var game in games.Where( g => g.State == 1 ))	/* only "waiting for players" */
			{						
				var template = ServerTemplate.Clone() as LabelWidget;
				template.Id = "JOIN_GAME_{0}".F(i);
				template.GetText = () => "   {0} ({1})".F(			/* /8 = hack */
						game.Name, 
						game.Address);
				template.GetBackground = () => ((currentServer == game) ? "dialog2" : null);
				template.OnMouseDown = mi => {currentServer = game; return true;};
				template.Parent = sl;			
				
				template.Bounds = new Rectangle(template.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				sl.AddChild(template);
				
				if (i == 0) currentServer = game;
				
				offset += template.Bounds.Height;
				sl.ContentHeight += template.Bounds.Height;
				i++;
			}
		}
	}
}
