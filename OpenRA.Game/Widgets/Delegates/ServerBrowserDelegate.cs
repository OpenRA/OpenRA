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

		public ServerBrowserDelegate()
		{
			var r = Chrome.rootWidget;

			MasterServerQuery.OnComplete += games =>
				{
					var bg = r.GetWidget("JOINSERVER_BG");

					if (games == null)
					{
						r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
						r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Failed to contact master server.";
						return;
					}

					if (games.Length == 0)
					{
						r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
						r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "No games found.";
						return;
					}

					r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = false;

					var margin = 20;
					int height = 50;
					int i = 0;

					foreach (var game in games.Where( g => g.State == 1 ))	/* only "waiting for players" */
					{
						var g = game;
						var b = new ButtonWidget
						{
							Bounds = new Rectangle(margin, height, bg.Bounds.Width - 2 * margin, 25),
							Id = "JOIN_GAME_{0}".F(i),
							Text = "{0} ({2}/8, {3}) {1}".F(			/* /8 = hack */
								game.Name, 
								game.Address, 
								game.Players, 
								string.Join( ",", game.Mods )),
							Delegate = "ServerBrowserDelegate",

							OnMouseUp = nmi =>
							{
								r.GetWidget("JOINSERVER_BG").Visible = false;
								Game.JoinServer(g.Address.Split(':')[0], int.Parse(g.Address.Split(':')[1]));
								return true;
							},
						};

						bg.AddChild(b);
						GameButtons.Add(b);

						height += 35;
					}

				};

			r.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp = mi =>
			{
				var bg = r.OpenWindow("JOINSERVER_BG");

				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				MasterServerQuery.Refresh(Game.Settings.MasterServer);

				return true;
			};

			r.GetWidget("JOINSERVER_BUTTON_REFRESH").OnMouseUp = mi =>
			{
				var bg = r.GetWidget("JOINSERVER_BG");

				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				MasterServerQuery.Refresh(Game.Settings.MasterServer);

				return true;
			};

			r.GetWidget("JOINSERVER_BUTTON_CANCEL").OnMouseUp = mi =>
			{
				r.CloseWindow();
				return true;
			};

			r.GetWidget("JOINSERVER_BUTTON_DIRECTCONNECT").OnMouseUp = mi =>
			{			/* rude hack. kill this as soon as we can do a direct connect via the commandline */
				r.CloseWindow();
				Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
				return true;
			};
		}
	}
}
