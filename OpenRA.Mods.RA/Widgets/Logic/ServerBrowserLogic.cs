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
using OpenRA.Server;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ServerBrowserLogic
	{
		GameServer currentServer;
		ScrollItemWidget serverTemplate;
		Action OpenLobby;
		Action OnExit;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden, Pinging }
		SearchStatus searchStatus = SearchStatus.Fetching;

		bool showWaiting = true;
		bool showEmpty = true;
		bool showStarted = true;
		bool showIncompatible = false;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Fetching:	return "Fetching game list...";
				case SearchStatus.Failed:	return "Failed to contact master server.";
				case SearchStatus.NoGames:	return "No games found.";
				case SearchStatus.Pinging:	return "Pinging compatible servers.";  
				default:					return "";
			}
		}

		[ObjectCreator.UseCtor]
		public ServerBrowserLogic(Widget widget, Action openLobby, Action onExit)
		{
			var panel = widget;
			OpenLobby = openLobby;
			OnExit = onExit;
			var sl = panel.Get<ScrollPanelWidget>("SERVER_LIST");

			// Menu buttons
			var refreshButton = panel.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
			refreshButton.OnClick = () => ServerList.Query(games => RefreshServerList(panel, games));

			var pingButton = panel.GetOrNull<ButtonWidget>("PING_BUTTON");
			if (pingButton != null)
			{
				pingButton.IsDisabled = () => searchStatus == SearchStatus.Pinging ||
					searchStatus == SearchStatus.Fetching || searchStatus == SearchStatus.Failed;
				pingButton.OnClick = () => ServerList.Query(games => PingServerList(panel, games));
			}

			var join = panel.Get<ButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !currentServer.CanJoin();
			join.OnClick = () => Join(currentServer);

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			// Server list
			serverTemplate = sl.Get<ScrollItemWidget>("SERVER_TEMPLATE");

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = panel.Get<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			var showWaitingCheckbox = panel.GetOrNull<CheckboxWidget>("WAITING_FOR_PLAYERS");
			if (showWaitingCheckbox != null)
			{
				showWaitingCheckbox.IsChecked = () => showWaiting;
				showWaitingCheckbox.OnClick = () => { showWaiting ^= true; ServerList.Query(games => RefreshServerList(panel, games)); };
			}

			var showEmptyCheckbox = panel.GetOrNull<CheckboxWidget>("EMPTY");
			if (showEmptyCheckbox != null)
			{
				showEmptyCheckbox.IsChecked = () => showEmpty;
				showEmptyCheckbox.OnClick = () => { showEmpty ^= true; ServerList.Query(games => RefreshServerList(panel, games)); };
			}

			var showAlreadyStartedCheckbox = panel.GetOrNull<CheckboxWidget>("ALREADY_STARTED");
			if (showAlreadyStartedCheckbox != null)
			{
				showAlreadyStartedCheckbox.IsChecked = () => showStarted;
				showAlreadyStartedCheckbox.OnClick = () => { showStarted ^= true; ServerList.Query(games => RefreshServerList(panel, games)); };
			}

			var showIncompatibleCheckbox = panel.GetOrNull<CheckboxWidget>("INCOMPATIBLE_VERSION");
			if (showIncompatibleCheckbox != null)
			{
				showIncompatibleCheckbox.IsChecked = () => showIncompatible;
				showIncompatibleCheckbox.OnClick = () => { showIncompatible ^= true; ServerList.Query(games => RefreshServerList(panel, games)); };
			}

			ServerList.Query(games => RefreshServerList(panel, games));
		}

		void Join(GameServer server)
		{
			if (server == null || !server.CanJoin())
					return;

			var host = server.Address.Split(':')[0];
			var port = int.Parse(server.Address.Split(':')[1]);

			Ui.CloseWindow();
			ConnectionLogic.Connect(host, port, OpenLobby, OnExit);
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

			if (game.State == (int)ServerState.WaitingPlayers)
				return "Waiting for players";
			if (game.State == (int)ServerState.GameStarted)
				return "Playing";
			if (game.State == (int)ServerState.ShuttingDown)
				return "Server shutting down";

			return "Unknown server state";
		}

		Map GetMapPreview(GameServer game)
		{
			return (game == null) ? null : Game.modData.FindMapByUid(game.Map);
		}

		static string GenerateModLabel(KeyValuePair<string,string> mod)
		{
			if (Mod.AllMods.ContainsKey(mod.Key))
				return "{0} ({1})".F(Mod.AllMods[mod.Key].Title, mod.Value);

			return "Unknown Mod: {0}".F(mod.Key);
		}

		public static string GenerateModsLabel(GameServer s)
		{
			return s.UsefulMods.Select(m => GenerateModLabel(m)).JoinWith("\n");
		}

		static string GetPing(GameServer s)
		{
			if (s.Latency > -1)
				return "Ping: {0} ms".F(s.Latency);
			else
				return "Ping: ? ms";
		}

		void PingServerList(Widget panel, IEnumerable<GameServer> games)
		{
			searchStatus = SearchStatus.Pinging;

			foreach (var loop in games.Where(g => g.CanJoin()))
			{
				var game = loop;
				
				if (game == null)
					continue;
				
				game.Ping();
			}

			searchStatus = SearchStatus.Hidden;

			RefreshServerList(panel, games);
		}

		bool Filtered(GameServer game)
		{
			if ((game.State == (int)ServerState.GameStarted) && !showStarted)
				return true;
			
			if ((game.State == (int)ServerState.WaitingPlayers) && !showWaiting)
				return true;
			
			if ((game.Players == 0) && !showEmpty)
				return true;

			if (!game.CompatibleVersion() && !showIncompatible)
				return true;

			return false;
		}

		public void RefreshServerList(Widget panel, IEnumerable<GameServer> games)
		{
			var sl = panel.Get<ScrollPanelWidget>("SERVER_LIST");

			searchStatus = SearchStatus.Fetching;

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

			foreach (var loop in games.OrderByDescending(g => g.CanJoin()).ThenByDescending(g => g.Players))
			{
				var game = loop;

				var canJoin = game.CanJoin();

				var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => currentServer = game, () => Join(game));

				var preview = item.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Map = () => GetMapPreview(game);
				preview.IsVisible = () => GetMapPreview(game) != null;

				var title = item.Get<LabelWidget>("TITLE");
				title.GetText = () => game.Name;

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

				var ping = item.GetOrNull<LabelWidget>("PING");
				if (ping != null)
				{
					ping.GetText = () => GetPing(game);
					ping.IsVisible = () => game.CompatibleVersion();
				}

				if (!canJoin)
				{
					title.GetColor = () => Color.Gray;
					maptitle.GetColor = () => Color.Gray;
					players.GetColor = () => Color.Gray;
					state.GetColor = () => Color.Gray;
					ip.GetColor = () => Color.Gray;
					version.GetColor = () => Color.Gray;
					if (ping != null)
						ping.GetColor = () => Color.Gray;
				}

				if (!Filtered(game))
					sl.AddChild(item);
			}
		}
	}
}
