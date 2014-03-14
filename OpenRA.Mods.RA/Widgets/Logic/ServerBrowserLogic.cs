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

		Action onStart;
		Action onExit;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;

		bool showWaiting = true;
		bool showEmpty = true;
		bool showStarted = true;
		bool showIncompatible = false;

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
		public ServerBrowserLogic(Widget widget, Action onStart, Action onExit)
		{
			var panel = widget;
			this.onStart = onStart;
			this.onExit = onExit;

			var sl = panel.Get<ScrollPanelWidget>("SERVER_LIST");

			// Menu buttons
			var refreshButton = panel.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
			refreshButton.OnClick = () => ServerList.Query(games => RefreshServerList(panel, games));

			panel.Get<ButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = OpenDirectConnectPanel;
			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = OpenCreateServerPanel;

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

			// Game.LoadWidget(null, "SERVERBROWSER_IRC", panel.Get("IRC_ROOT"), new WidgetArgs());

			ServerList.Query(games => RefreshServerList(panel, games));
		}

		void OpenLobby()
		{
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", Game.Disconnect },
				{ "onStart", onStart },
				{ "skirmishMode", false }
			});
		}

		void OpenDirectConnectPanel()
		{
			Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", () => { } }
			});
		}

		void OpenCreateServerPanel()
		{
			Ui.OpenWindow("CREATESERVER_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", () => { } }
			});
		}

		void Join(GameServer server)
		{
			if (server == null || !server.CanJoin())
				return;

			var host = server.Address.Split(':')[0];
			var port = int.Parse(server.Address.Split(':')[1]);

			ConnectionLogic.Connect(host, port, "", OpenLobby, onExit);
		}

		string GetPlayersLabel(GameServer game)
		{
			if (game == null || game.Players == 0)
				return "";

			var map = Game.modData.MapCache[game.Map];
			return "{0} / {1}".F(game.Players, map.PlayerCount == 0 ? "?" : map.PlayerCount.ToString());
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

		public static string GenerateModLabel(GameServer s)
		{
			Mod mod;
			var modVersion = s.Mods.Split('@');

			if (modVersion.Length == 2 && Mod.AllMods.TryGetValue(modVersion[0], out mod))
				return "{0} ({1})".F(mod.Title, modVersion[1]);

			return "Unknown mod: {0}".F(s.Mods);
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

			if (!games.Any())
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

				var map = Game.modData.MapCache[game.Map];
				var preview = item.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => map;

				var title = item.Get<LabelWidget>("TITLE");
				title.GetText = () => game.Name;

				// TODO: Use game.MapTitle once the server supports it
				var maptitle = item.Get<LabelWidget>("MAP");
				maptitle.GetText = () => map.Title;

				// TODO: Use game.MaxPlayers once the server supports it
				var players = item.Get<LabelWidget>("PLAYERS");
				players.GetText = () => GetPlayersLabel(game);

				var state = item.Get<LabelWidget>("STATE");
				state.GetText = () => GetStateLabel(game);

				var ip = item.Get<LabelWidget>("IP");
				ip.GetText = () => game.Address;

				var version = item.Get<LabelWidget>("VERSION");
				version.GetText = () => GenerateModLabel(game);
				version.IsVisible = () => !game.CompatibleVersion();

				var location = item.Get<LabelWidget>("LOCATION");
				var cachedServerLocation = LobbyUtils.LookupCountry(game.Address.Split(':')[0]);
				location.GetText = () => cachedServerLocation;
				location.IsVisible = () => game.CompatibleVersion();

				if (!canJoin)
				{
					title.GetColor = () => Color.Gray;
					maptitle.GetColor = () => Color.Gray;
					players.GetColor = () => Color.Gray;
					state.GetColor = () => Color.Gray;
					ip.GetColor = () => Color.Gray;
					version.GetColor = () => Color.Gray;
					location.GetColor = () => Color.Gray;
				}

				if (!Filtered(game))
					sl.AddChild(item);
			}
		}
	}
}
