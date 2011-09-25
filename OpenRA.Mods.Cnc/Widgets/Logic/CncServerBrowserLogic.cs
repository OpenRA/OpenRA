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
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncServerBrowserLogic
	{
		GameServer currentServer;
		ScrollItemWidget serverTemplate;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Fetching:
					return "Fetching game list...";
				case SearchStatus.Failed:
					return "Failed to contact master server.";
				case SearchStatus.NoGames:
					return "No games found.";
				default:
					return "";
			}
		}

		[ObjectCreator.UseCtor]
		public CncServerBrowserLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] Action openLobby,
		                            [ObjectCreator.Param] Action onExit)
		{
			var panel = widget.GetWidget("SERVERBROWSER_PANEL");
			var sl = panel.GetWidget<ScrollPanelWidget>("SERVER_LIST");

			// Menu buttons
			var refreshButton = panel.GetWidget<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
			refreshButton.OnClick = () =>
			{
				searchStatus = SearchStatus.Fetching;
				sl.RemoveChildren();
				currentServer = null;
				ServerList.Query(games => RefreshServerList(panel, games));
			};

			var join = panel.GetWidget<ButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !ServerBrowserLogic.CanJoin(currentServer);
			join.OnClick = () =>
			{
				if (currentServer == null)
					return;

				var host = currentServer.Address.Split(':')[0];
				var port = int.Parse(currentServer.Address.Split(':')[1]);

				Widget.CloseWindow();
				CncConnectingLogic.Connect(host, port, openLobby, onExit);
			};

			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };

			// Server list
			serverTemplate = sl.GetWidget<ScrollItemWidget>("SERVER_TEMPLATE");

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = panel.GetWidget<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			// Map preview
			var preview = panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			preview.Map = () => CurrentMap();
			preview.IsVisible = () => CurrentMap() != null;

			// Server info
			var infoPanel = panel.GetWidget("SERVER_INFO");
			infoPanel.IsVisible = () => currentServer != null;
			infoPanel.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			infoPanel.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => ServerBrowserLogic.GenerateModsLabel(currentServer);
			infoPanel.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			infoPanel.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () => GetPlayersLabel(currentServer);

			ServerList.Query(games => RefreshServerList(panel, games));
		}

		string GetPlayersLabel(GameServer game)
		{
			if (game == null)
				return "";

			var map = GetMap(game.Map);
			return map == null ? "{0}".F(currentServer.Players) : "{0} / {1}".F(currentServer.Players, map.PlayerCount);
		}

		Map CurrentMap()
		{
			return (currentServer == null) ? null : GetMap(currentServer.Map);
		}

		static Map GetMap(string uid)
		{
			return (!Game.modData.AvailableMaps.ContainsKey(uid))
				? null : Game.modData.AvailableMaps[uid];
		}

		public void RefreshServerList(Widget panel, IEnumerable<GameServer> games)
		{
			var sl = panel.GetWidget<ScrollPanelWidget>("SERVER_LIST");

			sl.RemoveChildren();
			currentServer = null;

			if (games == null)
			{
				searchStatus = SearchStatus.Failed;
				return;
			}

            var gamesWaiting = games.Where(g => ServerBrowserLogic.CanJoin(g));

            if (gamesWaiting.Count() == 0)
			{
				searchStatus = SearchStatus.NoGames;
				return;
			}

			searchStatus = SearchStatus.Hidden;
			currentServer = gamesWaiting.FirstOrDefault();

            foreach (var loop in gamesWaiting)
			{
				var game = loop;

				var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => currentServer = game);
				item.GetWidget<LabelWidget>("TITLE").GetText = () => game.Name;
				// TODO: Use game.MapTitle once the server supports it
				item.GetWidget<LabelWidget>("MAP").GetText = () => {var map = GetMap(game.Map); return map == null ? "Unknown" : map.Title;};
				// TODO: Use game.MaxPlayers once the server supports it
				item.GetWidget<LabelWidget>("PLAYERS").GetText = () => GetPlayersLabel(game);
				item.GetWidget<LabelWidget>("IP").GetText = () => game.Address;
				sl.AddChild(item);
			}
		}
	}
}
