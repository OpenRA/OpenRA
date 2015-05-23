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
		bool showProtected = true;
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
		public ServerBrowserLogic(Widget widget, Action onStart, Action onExit, string directConnectHost, int directConnectPort)
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

			var showProtectedCheckbox = panel.GetOrNull<CheckboxWidget>("PASSWORD_PROTECTED");
			if (showProtectedCheckbox != null)
			{
				showProtectedCheckbox.IsChecked = () => showProtected;
				showProtectedCheckbox.OnClick = () => { showProtected ^= true; RefreshServerList(); };
			}

			var showIncompatibleCheckbox = panel.GetOrNull<CheckboxWidget>("INCOMPATIBLE_VERSION");
			if (showIncompatibleCheckbox != null)
			{
				showIncompatibleCheckbox.IsChecked = () => showIncompatible;
				showIncompatibleCheckbox.OnClick = () => { showIncompatible ^= true; RefreshServerList(); };
			}

			RefreshServerList();

			if (directConnectHost != null)
			{
				// The connection window must be opened at the end of the tick for the widget hierarchy to
				// work out, but we also want to prevent the server browser from flashing visible for one tick.
				widget.Visible = false;
				Game.RunAfterTick(() =>
				{
					ConnectionLogic.Connect(directConnectHost, directConnectPort, "", OpenLobby, DoNothing);
					widget.Visible = true;
				});
			}
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

			currentQuery = new Download(Game.Settings.Server.MasterServer + "games", _ => {}, onComplete);
		}

		void RefreshServerListInner(IEnumerable<GameServer> games)
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
				var compatible = game.CompatibleVersion();

				var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => currentServer = game, () => Join(game));

				var map = Game.modData.MapCache[game.Map];
				var preview = item.GetOrNull<MapPreviewWidget>("MAP_PREVIEW");
				if (preview != null)
					preview.Preview = () => map;

				var title = item.GetOrNull<LabelWidget>("TITLE");
				if (title != null)
				{
					title.GetText = () => game.Name;
					title.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray  : title.TextColor;
				}

				var maptitle = item.GetOrNull<LabelWidget>("MAP");
				if (title != null)
				{
					maptitle.GetText = () => map.Title;
					maptitle.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray  : maptitle.TextColor;
				}

				var players = item.GetOrNull<LabelWidget>("PLAYERS");
				if (players != null)
				{
					players.GetText = () => "{0} / {1}".F(game.Players, game.MaxPlayers)
						+ (game.Spectators > 0 ? "  ({0} Spectator{1})".F(game.Spectators, game.Spectators > 1 ? "s" : "") : "");
					players.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray  : players.TextColor;
				}

				var state = item.GetOrNull<LabelWidget>("STATE");
				if (state != null)
				{
					state.GetText = () => GetStateLabel(game);
					state.GetColor = () => GetStateColor(game, state, !compatible || !canJoin);
				}

				var ip = item.GetOrNull<LabelWidget>("IP");
				if (ip != null)
				{
					ip.GetText = () => game.Address;
					ip.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray  :  ip.TextColor;
				}

				var version = item.GetOrNull<LabelWidget>("VERSION");
				if (version != null)
				{
					version.GetText = () => GenerateModLabel(game);
					version.IsVisible = () => !compatible;
					version.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray : version.TextColor;
				}

				var location = item.GetOrNull<LabelWidget>("LOCATION");
				if (location != null)
				{
					var cachedServerLocation = LobbyUtils.LookupCountry(game.Address.Split(':')[0]);
					location.GetText = () => cachedServerLocation;
					location.IsVisible = () => compatible;
					location.GetColor = () => !compatible ? Color.DarkGray : !canJoin ? Color.LightGray  : location.TextColor;
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

		static string GetStateLabel(GameServer game)
		{
			if (game == null)
				return "";

			if (game.State == (int)ServerState.GameStarted)
			{
				try
				{
					var runTime = DateTime.Now - System.DateTime.Parse(game.Started);
					return "In progress for {0} minute{1}".F(runTime.Minutes, runTime.Minutes > 1 ? "s" : "");
				}
				catch (Exception)
				{
					return "In progress";
				}
			}

			if (game.Protected)
				return "Password protected";

			if (game.State == (int)ServerState.WaitingPlayers)
				return "Waiting for players";

			if (game.State == (int)ServerState.ShuttingDown)
				return "Server shutting down";

			return "Unknown server state";
		}

		static Color GetStateColor(GameServer game, LabelWidget label, bool darkened)
		{
			if (game.Protected && game.State == (int)ServerState.WaitingPlayers)
				return darkened ? Color.DarkRed : Color.Red;

			if (game.State == (int)ServerState.WaitingPlayers)
				return darkened ? Color.LimeGreen : Color.Lime;

			if (game.State == (int)ServerState.GameStarted)
				return darkened ? Color.Chocolate : Color.Orange;

			return label.TextColor;
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

			if (game.Protected && !showProtected)
				return true;

			return false;
		}
	}
}
