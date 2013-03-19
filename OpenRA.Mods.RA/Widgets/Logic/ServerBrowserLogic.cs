#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ServerBrowserLogic
	{
		GameServer currentServer;
		ScrollItemWidget serverTemplate;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Fetching: return "Fetching game list...";
				case SearchStatus.Failed: return "Failed to contact master server.";
				case SearchStatus.NoGames: return "No games found.";
				default: return "";
			}
		}

		[ObjectCreator.UseCtor]
		public ServerBrowserLogic(Widget widget, Action openLobby, Action onExit)
		{
			var panel = widget;
			var sl = panel.Get<ScrollPanelWidget>("SERVER_LIST");

			// Menu buttons
			var refreshButton = panel.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
			refreshButton.OnClick = () =>
			{
				searchStatus = SearchStatus.Fetching;
				sl.RemoveChildren();
				currentServer = null;
				ServerList.Query(games => RefreshServerList(panel, games));
			};

			var join = panel.Get<ButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !currentServer.CanJoin();
			join.OnClick = () =>
			{
				if (currentServer == null)
					return;

				var host = currentServer.Address.Split(':')[0];
				var port = int.Parse(currentServer.Address.Split(':')[1]);

				Ui.CloseWindow();
				ConnectionLogic.Connect(host, port, openLobby, onExit);
			};

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			// Server list
			serverTemplate = sl.Get<ScrollItemWidget>("SERVER_TEMPLATE");

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = panel.Get<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			ServerList.Query(games => RefreshServerList(panel, games));
		}

		string GetPlayersLabel(GameServer game)
		{
			if (game == null || game.Players == 0)
				return "";

			var map = Game.modData.FindMapByUid(game.Map);

			var maxPlayers = map == null ? "?" : (object)map.PlayerCount;
			return "{0} / {1}".F(game.Players, maxPlayers);
		}

		string GetStateLabel(GameServer game)
		{
			if (game == null)
				return "";

			if (game.State == 1) return "Waiting for players";
			if (game.State == 2) return "Playing";
			else return "Unknown";
		}

		Map GetMapPreview(GameServer game)
		{
			return (game == null) ? null : Game.modData.FindMapByUid(game.Map);
		}

		static string GenerateModLabel(KeyValuePair<string, string> mod)
		{
			if (Mod.AllMods.ContainsKey(mod.Key))
				return "{0} ({1})".F(Mod.AllMods[mod.Key].Title, mod.Value);

			return "Unknown Mod: {0}".F(mod.Key);
		}

		public static string GenerateModsLabel(GameServer s)
		{
			return s.UsefulMods.Select(m => GenerateModLabel(m)).JoinWith("\n");
		}

		public void RefreshServerList(Widget panel, IEnumerable<GameServer> games)
		{
			var sl = panel.Get<ScrollPanelWidget>("SERVER_LIST");
			

			sl.RemoveChildren();
			currentServer = null;

			if (games == null)
			{
				searchStatus = SearchStatus.Failed;
				return;
			}

			if (games.Count() == 0)
			{
				searchStatus = SearchStatus.NoGames;
				return;
			}

			searchStatus = SearchStatus.Hidden;
			currentServer = games.FirstOrDefault();

			foreach (var loop in games.OrderBy(g => g.CanJoin() ? 0 : 1))
			{
				var game = loop;

				var canJoin = game.CanJoin();

				Func<string> getName = () =>
					{
						string Name = game.Name;
						int length = game.Name.Length;
						while (Game.Renderer.Fonts["Regular"].Measure(Name).Length > (Convert.ToInt32(serverTemplate.Bounds.Width) - Game.Renderer.Fonts["Regular"].Measure(GetStateLabel(game)).Length - serverTemplate.Get<MapPreviewWidget>("MAP_PREVIEW").Bounds.Width) - Game.Renderer.Fonts["Regular"].Measure("...").Length - 60)
						{
							if (!string.IsNullOrEmpty(game.Name))
							{
								Name = game.Name.Substring(0, length);
								length--;
							}
						}
						if (game.Name.Length != Name.Length)
						{
							return Name + "...";
						}
						else
						{
							return game.Name;
						}
					};

				var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () =>
					{
						currentServer = game;
						
						panel.Children.Remove(panel.Children.Where(c => c is ToolTipWidget).SingleOrDefault());
						if (game.Name.Length > getName().Length)
						{
							ToolTipWidget toolTip = new ToolTipWidget();
							panel.AddChild(toolTip);
							toolTip.Tag =   Ui.MouseOverWidget;
							toolTip.Text = game.Name;
						}
					}
				);
			   
			  
				var preview = item.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Map = () => GetMapPreview(game);
				preview.IsVisible = () => GetMapPreview(game) != null;

				var title = item.Get<LabelWidget>("TITLE");
				title.GetText = () =>     
				{
					return getName();
				};
				
				title.Tag = game.Name;

				// TODO: Use game.MapTitle once the server supports it
				var maptitle = item.Get<LabelWidget>("MAP");
				maptitle.GetText = () =>
				{
					var map = Game.modData.FindMapByUid(game.Map);
					return map == null ? "Unknown Map" : map.Title;
				};

				// TODO: Use game.MaxPlayers once the server supports it
				var players = item.Get<LabelWidget>("PLAYERS");
				players.GetText = () => GetPlayersLabel(game);

				var state = item.Get<LabelWidget>("STATE");
				state.GetText = () => GetStateLabel(game);
	
				var ip = item.Get<LabelWidget>("IP");
				ip.GetText = () => game.Address;

				var version = item.Get<LabelWidget>("VERSION");
				version.GetText = () => GenerateModsLabel(game);
				version.IsVisible = () => !game.CompatibleVersion();

				if (!canJoin)
				{
					title.GetColor = () => Color.Gray;
					maptitle.GetColor = () => Color.Gray;
					players.GetColor = () => Color.Gray;
					state.GetColor = () => Color.Gray;
					ip.GetColor = () => Color.Gray;
					version.GetColor = () => Color.Gray;
				}

				sl.AddChild(item);
			}
		}
	}
}
