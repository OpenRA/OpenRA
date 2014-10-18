#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ServerBrowserLogic
	{
		readonly static Action DoNothing = () => { };

		GameServer currentServer;
		ScrollItemWidget serverTemplate;

		Action onStart;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;
		Download currentQuery;
		Widget panel, serverList;

		bool showWaiting = true;
		bool showEmpty = true;
		bool showStarted = true;
		bool showIncompatible = false;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Failed: return "Failed to contact master server.";
				case SearchStatus.NoGames: return "No games found.";
				default: return "";
			}
		}

		[ObjectCreator.UseCtor]
		public ServerBrowserLogic(Widget widget, Action onStart, Action onExit)
		{
			panel = widget;
			this.onStart = onStart;

			serverList = panel.Get<ScrollPanelWidget>("SERVER_LIST");
			serverTemplate = serverList.Get<ScrollItemWidget>("SERVER_TEMPLATE");

			// Menu buttons
			var refreshButton = panel.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
			refreshButton.GetText = () => searchStatus == SearchStatus.Fetching ? "Refreshing..." : "Refresh";
			refreshButton.OnClick = RefreshServerList;

			panel.Get<ButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = OpenDirectConnectPanel;
			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = OpenCreateServerPanel;

			var join = panel.Get<ButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !currentServer.CanJoin();
			join.OnClick = () => Join(currentServer);

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = panel.Get<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			var showWaitingCheckbox = panel.GetOrNull<CheckboxWidget>("WAITING_FOR_PLAYERS");
			if (showWaitingCheckbox != null)
			{
				showWaitingCheckbox.IsChecked = () => showWaiting;
				showWaitingCheckbox.OnClick = () => { showWaiting ^= true; RefreshServerList(); };
			}

			var showEmptyCheckbox = panel.GetOrNull<CheckboxWidget>("EMPTY");
			if (showEmptyCheckbox != null)
			{
				showEmptyCheckbox.IsChecked = () => showEmpty;
				showEmptyCheckbox.OnClick = () => { showEmpty ^= true; RefreshServerList(); };
			}

			var showAlreadyStartedCheckbox = panel.GetOrNull<CheckboxWidget>("ALREADY_STARTED");
			if (showAlreadyStartedCheckbox != null)
			{
				showAlreadyStartedCheckbox.IsChecked = () => showStarted;
				showAlreadyStartedCheckbox.OnClick = () => { showStarted ^= true; RefreshServerList(); };
			}

			var showIncompatibleCheckbox = panel.GetOrNull<CheckboxWidget>("INCOMPATIBLE_VERSION");
			if (showIncompatibleCheckbox != null)
			{
				showIncompatibleCheckbox.IsChecked = () => showIncompatible;
				showIncompatibleCheckbox.OnClick = () => { showIncompatible ^= true; RefreshServerList(); };
			}

			RefreshServerList();
		}

		void RefreshServerList()
		{
			// Query in progress
			if (currentQuery != null)
				return;

			searchStatus = SearchStatus.Fetching;

			Action<DownloadDataCompletedEventArgs, bool> onComplete = (i, cancelled) =>
			{
				currentQuery = null;

				if (i.Error != null || cancelled)
				{
					RefreshServerListInner(null);
					return;
				}

				var data = Encoding.UTF8.GetString(i.Result);
				var yaml = MiniYaml.FromString(data);

				var games = yaml.Select(a => FieldLoader.Load<GameServer>(a.Value))
					.Where(gs => gs.Address != null);

				RefreshServerListInner(games);
				Game.RunAfterTick(() => RefreshServerListInner(games));
			};

			currentQuery = new Download(Game.Settings.Server.MasterServer + "list", _ => {}, onComplete);
		}

		public void RefreshServerListInner(IEnumerable<GameServer> games)
		{
			if (games == null)
				return;

			var rows = new List<Widget>();

			foreach (var loop in games.OrderByDescending(g => g.CanJoin()).ThenByDescending(g => g.Players))
			{
				var game = loop;
				if (game == null)
					continue;

				var canJoin = game.CanJoin();

				var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => currentServer = game, () => Join(game));

				var map = Game.modData.MapCache[game.Map];
				var preview = item.GetOrNull<MapPreviewWidget>("MAP_PREVIEW");
				if (preview != null)
					preview.Preview = () => map;

				var title = item.GetOrNull<LabelWidget>("TITLE");
				if (title != null)
				{
					title.GetText = () => game.Name;
					title.GetColor = () => canJoin ? title.TextColor : Color.Gray;
				}

				var maptitle = item.GetOrNull<LabelWidget>("MAP");
				if (title != null)
				{
					maptitle.GetText = () => map.Title;
					maptitle.GetColor = () => canJoin ? maptitle.TextColor : Color.Gray;
				}

				var players = item.GetOrNull<LabelWidget>("PLAYERS");
				if (players != null)
				{
					players.GetText = () => "{0} / {1}".F(game.Players, map.PlayerCount);
					players.GetColor = () => canJoin ? players.TextColor : Color.Gray;
				}

				var state = item.GetOrNull<LabelWidget>("STATE");
				if (state != null)
				{
					state.GetText = () => GetStateLabel(game);
					state.GetColor = () => canJoin ? state.TextColor : Color.Gray;
				}

				var ip = item.GetOrNull<LabelWidget>("IP");
				if (ip != null)
				{
					ip.GetText = () => game.Address;
					ip.GetColor = () => canJoin ? ip.TextColor : Color.Gray;
				}

				var version = item.GetOrNull<LabelWidget>("VERSION");
				if (version != null)
				{
					version.GetText = () => GenerateModLabel(game);
					version.IsVisible = () => !game.CompatibleVersion();
					version.GetColor = () => canJoin ? version.TextColor : Color.Gray;
				}

				var location = item.GetOrNull<LabelWidget>("LOCATION");
				if (location != null)
				{
					var cachedServerLocation = LobbyUtils.LookupCountry(game.Address.Split(':')[0]);
					location.GetText = () => cachedServerLocation;
					location.IsVisible = () => game.CompatibleVersion();
					location.GetColor = () => canJoin ? location.TextColor : Color.Gray;
				}

				if (!Filtered(game))
					rows.Add(item);
			}

			Game.RunAfterTick(() =>
			{
				serverList.RemoveChildren();
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

				currentServer = games.FirstOrDefault();
				searchStatus = SearchStatus.Hidden;

				// Search for any unknown maps
				if (Game.Settings.Game.AllowDownloading)
					Game.modData.MapCache.QueryRemoteMapDetails(games.Where(g => !Filtered(g)).Select(g => g.Map));

				foreach (var row in rows)
					serverList.AddChild(row);
			});
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
				{ "onExit", DoNothing }
			});
		}

		void OpenCreateServerPanel()
		{
			Ui.OpenWindow("CREATESERVER_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", DoNothing }
			});
		}

		void Join(GameServer server)
		{
			if (server == null || !server.CanJoin())
				return;

			var host = server.Address.Split(':')[0];
			var port = Exts.ParseIntegerInvariant(server.Address.Split(':')[1]);

			ConnectionLogic.Connect(host, port, "", OpenLobby, DoNothing);
		}

		static string GetPlayersLabel(GameServer game)
		{
			if (game == null || game.Players == 0)
				return "";

			var map = Game.modData.MapCache[game.Map];
			return "{0} / {1}".F(game.Players, map.PlayerCount == 0 ? "?" : map.PlayerCount.ToString());
		}

		static string GetStateLabel(GameServer game)
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
			ModMetadata mod;
			var modVersion = s.Mods.Split('@');

			if (modVersion.Length == 2 && ModMetadata.AllMods.TryGetValue(modVersion[0], out mod))
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
