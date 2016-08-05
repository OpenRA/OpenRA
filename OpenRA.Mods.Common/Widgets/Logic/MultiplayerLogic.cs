#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MultiplayerLogic : ChromeLogic
	{
		static readonly Action DoNothing = () => { };

		enum PanelType { Browser, DirectConnect, CreateServer }
		PanelType panel = PanelType.Browser;

		readonly Color incompatibleVersionColor;
		readonly Color incompatibleProtectedGameColor;
		readonly Color protectedGameColor;
		readonly Color incompatibleWaitingGameColor;
		readonly Color waitingGameColor;
		readonly Color incompatibleGameStartedColor;
		readonly Color gameStartedColor;
		readonly Color incompatibleGameColor;
		readonly ModData modData;

		GameServer currentServer;
		MapPreview currentMap;

		ScrollItemWidget serverTemplate;
		ScrollItemWidget headerTemplate;

		Action onStart;
		Action onExit;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;
		Download currentQuery;
		Widget serverList;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Failed: return "Failed to contact master server.";
				case SearchStatus.NoGames: return "No games found. Try changing filters.";
				default: return "";
			}
		}

		[ObjectCreator.UseCtor]
		public MultiplayerLogic(Widget widget, ModData modData, Action onStart, Action onExit, string directConnectHost, int directConnectPort)
		{
			this.modData = modData;
			this.onStart = onStart;
			this.onExit = onExit;

			incompatibleVersionColor = ChromeMetrics.Get<Color>("IncompatibleVersionColor");
			incompatibleGameColor = ChromeMetrics.Get<Color>("IncompatibleGameColor");
			incompatibleProtectedGameColor = ChromeMetrics.Get<Color>("IncompatibleProtectedGameColor");
			protectedGameColor = ChromeMetrics.Get<Color>("ProtectedGameColor");
			waitingGameColor = ChromeMetrics.Get<Color>("WaitingGameColor");
			incompatibleWaitingGameColor = ChromeMetrics.Get<Color>("IncompatibleWaitingGameColor");
			gameStartedColor = ChromeMetrics.Get<Color>("GameStartedColor");
			incompatibleGameStartedColor = ChromeMetrics.Get<Color>("IncompatibleGameStartedColor");

			LoadBrowserPanel(widget);
			LoadDirectConnectPanel(widget);
			LoadCreateServerPanel(widget);

			// Filter and refresh buttons act on the browser panel,
			// but remain visible (disabled) on the other panels
			var refreshButton = widget.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching || panel != PanelType.Browser;

			var filtersButton = widget.Get<DropDownButtonWidget>("FILTERS_DROPDOWNBUTTON");
			filtersButton.IsDisabled = () => searchStatus == SearchStatus.Fetching || panel != PanelType.Browser;

			var browserTab = widget.Get<ButtonWidget>("BROWSER_TAB");
			browserTab.IsHighlighted = () => panel == PanelType.Browser;
			browserTab.OnClick = () => panel = PanelType.Browser;

			var directConnectTab = widget.Get<ButtonWidget>("DIRECTCONNECT_TAB");
			directConnectTab.IsHighlighted = () => panel == PanelType.DirectConnect;
			directConnectTab.OnClick = () => panel = PanelType.DirectConnect;

			var createServerTab = widget.Get<ButtonWidget>("CREATE_TAB");
			createServerTab.IsHighlighted = () => panel == PanelType.CreateServer;
			createServerTab.OnClick = () => panel = PanelType.CreateServer;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
			Game.LoadWidget(null, "GLOBALCHAT_PANEL", widget.Get("GLOBALCHAT_ROOT"), new WidgetArgs());

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

		void LoadBrowserPanel(Widget widget)
		{
			var browserPanel = Game.LoadWidget(null, "MULTIPLAYER_BROWSER_PANEL", widget.Get("TOP_PANELS_ROOT"), new WidgetArgs());
			browserPanel.IsVisible = () => panel == PanelType.Browser;

			serverList = browserPanel.Get<ScrollPanelWidget>("SERVER_LIST");
			headerTemplate = serverList.Get<ScrollItemWidget>("HEADER_TEMPLATE");
			serverTemplate = serverList.Get<ScrollItemWidget>("SERVER_TEMPLATE");

			var join = widget.Get<ButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !currentServer.IsJoinable;
			join.OnClick = () => Join(currentServer);

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = widget.Get<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			var gs = Game.Settings.Game;
			Action<MPGameFilters> toggleFilterFlag = f =>
			{
				gs.MPGameFilters ^= f;
				Game.Settings.Save();
				RefreshServerList();
			};

			var filtersPanel = Ui.LoadWidget("MULTIPLAYER_FILTER_PANEL", null, new WidgetArgs());
			var showWaitingCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("WAITING_FOR_PLAYERS");
			if (showWaitingCheckbox != null)
			{
				showWaitingCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Waiting);
				showWaitingCheckbox.OnClick = () => toggleFilterFlag(MPGameFilters.Waiting);
			}

			var showEmptyCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("EMPTY");
			if (showEmptyCheckbox != null)
			{
				showEmptyCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Empty);
				showEmptyCheckbox.OnClick = () => toggleFilterFlag(MPGameFilters.Empty);
			}

			var showAlreadyStartedCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("ALREADY_STARTED");
			if (showAlreadyStartedCheckbox != null)
			{
				showAlreadyStartedCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Started);
				showAlreadyStartedCheckbox.OnClick = () => toggleFilterFlag(MPGameFilters.Started);
			}

			var showProtectedCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("PASSWORD_PROTECTED");
			if (showProtectedCheckbox != null)
			{
				showProtectedCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Protected);
				showProtectedCheckbox.OnClick = () => toggleFilterFlag(MPGameFilters.Protected);
			}

			var showIncompatibleCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("INCOMPATIBLE_VERSION");
			if (showIncompatibleCheckbox != null)
			{
				showIncompatibleCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Incompatible);
				showIncompatibleCheckbox.OnClick = () => toggleFilterFlag(MPGameFilters.Incompatible);
			}

			var filtersButton = widget.GetOrNull<DropDownButtonWidget>("FILTERS_DROPDOWNBUTTON");
			if (filtersButton != null)
			{
				filtersButton.OnMouseDown = _ =>
				{
					filtersButton.RemovePanel();
					filtersButton.AttachPanel(filtersPanel);
				};
			}

			var refreshButton = widget.Get<ButtonWidget>("REFRESH_BUTTON");
			refreshButton.GetText = () => searchStatus == SearchStatus.Fetching ? "Refreshing..." : "Refresh";
			refreshButton.OnClick = RefreshServerList;

			var mapPreview = widget.GetOrNull<MapPreviewWidget>("SELECTED_MAP_PREVIEW");
			if (mapPreview != null)
				mapPreview.Preview = () => currentMap;

			var mapTitle = widget.GetOrNull<LabelWidget>("SELECTED_MAP");
			if (mapTitle != null)
			{
				var font = Game.Renderer.Fonts[mapTitle.Font];
				var title = new CachedTransform<MapPreview, string>(m => m == null ? "No Server Selected" :
					WidgetUtils.TruncateText(m.Title, mapTitle.Bounds.Width, font));
				mapTitle.GetText = () => title.Update(currentMap);
			}

			var ip = widget.GetOrNull<LabelWidget>("SELECTED_IP");
			if (ip != null)
			{
				ip.IsVisible = () => currentServer != null;
				ip.GetText = () => currentServer.Address;
			}

			var status = widget.GetOrNull<LabelWidget>("SELECTED_STATUS");
			if (status != null)
			{
				status.IsVisible = () => currentServer != null;
				status.GetText = () => GetStateLabel(currentServer);
				status.GetColor = () => GetStateColor(currentServer, status);
			}

			var modVersion = widget.GetOrNull<LabelWidget>("SELECTED_MOD_VERSION");
			if (modVersion != null)
			{
				modVersion.IsVisible = () => currentServer != null;
				modVersion.GetColor = () => currentServer.IsCompatible ? modVersion.TextColor : incompatibleVersionColor;

				var font = Game.Renderer.Fonts[modVersion.Font];
				var version = new CachedTransform<GameServer, string>(s => WidgetUtils.TruncateText(s.ModLabel, mapTitle.Bounds.Width, font));
				modVersion.GetText = () => version.Update(currentServer);
			}

			var players = widget.GetOrNull<LabelWidget>("SELECTED_PLAYERS");
			if (players != null)
			{
				players.IsVisible = () => currentServer != null;
				players.GetText = () => PlayersLabel(currentServer);
			}
		}

		void LoadDirectConnectPanel(Widget widget)
		{
			var directConnectPanel = Game.LoadWidget(null, "MULTIPLAYER_DIRECTCONNECT_PANEL",
				widget.Get("TOP_PANELS_ROOT"), new WidgetArgs());
			directConnectPanel.IsVisible = () => panel == PanelType.DirectConnect;

			var ipField = directConnectPanel.Get<TextFieldWidget>("IP");
			var portField = directConnectPanel.Get<TextFieldWidget>("PORT");

			var last = Game.Settings.Player.LastServer.Split(':');
			ipField.Text = last.Length > 1 ? last[0] : "localhost";
			portField.Text = last.Length == 2 ? last[1] : "1234";

			directConnectPanel.Get<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				var port = Exts.WithDefault(1234, () => Exts.ParseIntegerInvariant(portField.Text));

				Game.Settings.Player.LastServer = "{0}:{1}".F(ipField.Text, port);
				Game.Settings.Save();

				ConnectionLogic.Connect(ipField.Text, port, "", OpenLobby, DoNothing);
			};
		}

		void LoadCreateServerPanel(Widget widget)
		{
			var createServerPanel = Game.LoadWidget(null, "MULTIPLAYER_CREATESERVER_PANEL",
				widget.Get("TOP_PANELS_ROOT"), new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", DoNothing }
			});

			createServerPanel.IsVisible = () => panel == PanelType.CreateServer;
		}

		string PlayersLabel(GameServer game)
		{
			return "{0}{1}{2}".F(
				"{0} Player{1}".F(game.Players > 0 ? game.Players.ToString() : "No", game.Players != 1 ? "s" : ""),
				game.Bots > 0 ? ", {0} Bot{1}".F(game.Bots, game.Bots != 1 ? "s" : "") : "",
				game.Spectators > 0 ? ", {0} Spectator{1}".F(game.Spectators, game.Spectators != 1 ? "s" : "") : "");
		}

		void RefreshServerList()
		{
			// Query in progress
			if (currentQuery != null)
				return;

			searchStatus = SearchStatus.Fetching;

			Action<DownloadDataCompletedEventArgs> onComplete = i =>
			{
				currentQuery = null;

				if (i.Error != null)
				{
					RefreshServerListInner(null);
					return;
				}

				var data = Encoding.UTF8.GetString(i.Result);
				var yaml = MiniYaml.FromString(data);

				var games = yaml.Select(a => new GameServer(a.Value))
					.Where(gs => gs.Address != null);

				Game.RunAfterTick(() => RefreshServerListInner(games));
			};

			var queryURL = Game.Settings.Server.MasterServer + "games?version={0}&mod={1}&modversion={2}".F(
				Uri.EscapeUriString(Game.Mods["modchooser"].Metadata.Version),
				Uri.EscapeUriString(Game.ModData.Manifest.Id),
				Uri.EscapeUriString(Game.ModData.Manifest.Metadata.Version));

			currentQuery = new Download(queryURL, _ => { }, onComplete);
		}

		int GroupSortOrder(GameServer testEntry)
		{
			// Games that we can't join are sorted last
			if (!testEntry.IsCompatible)
				return 0;

			// Games for the current mod+version are sorted first
			if (testEntry.ModId == modData.Manifest.Id)
				return 2;

			// Followed by games for different mods that are joinable
			return 1;
		}

		void SelectServer(GameServer server)
		{
			currentServer = server;
			currentMap = server != null ? modData.MapCache[server.Map] : null;
		}

		void RefreshServerListInner(IEnumerable<GameServer> games)
		{
			if (games == null)
				return;

			var mods = games.GroupBy(g => g.Mods)
				.OrderByDescending(g => GroupSortOrder(g.First()))
				.ThenByDescending(g => g.Count());

			ScrollItemWidget nextServerRow = null;
			var rows = new List<Widget>();
			foreach (var modGames in mods)
			{
				if (modGames.All(Filtered))
					continue;

				var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => { });

				var headerTitle = modGames.First().ModLabel;
				header.Get<LabelWidget>("LABEL").GetText = () => headerTitle;
				rows.Add(header);

				Func<GameServer, int> listOrder = g =>
				{
					// Servers waiting for players are always first
					if (g.State == (int)ServerState.WaitingPlayers && g.Players > 0)
						return 0;

					// Then active games
					if (g.State >= (int)ServerState.GameStarted)
						return 1;

					// Empty servers are shown at the end because a flood of empty servers
					// at the top of the game list make the community look dead
					return 2;
				};

				foreach (var loop in modGames.OrderBy(listOrder).ThenByDescending(g => g.Players))
				{
					var game = loop;
					if (game == null || Filtered(game))
						continue;

					var canJoin = game.IsJoinable;
					var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => SelectServer(game), () => Join(game));
					var title = item.GetOrNull<LabelWidget>("TITLE");
					if (title != null)
					{
						var font = Game.Renderer.Fonts[title.Font];
						var label = WidgetUtils.TruncateText(game.Name, title.Bounds.Width, font);
						title.GetText = () => label;
						title.GetColor = () => canJoin ? title.TextColor : incompatibleGameColor;
					}

					var password = item.GetOrNull<ImageWidget>("PASSWORD_PROTECTED");
					if (password != null)
					{
						password.IsVisible = () => game.Protected;
						password.GetImageName = () => canJoin ? "protected" : "protected-disabled";
					}

					var players = item.GetOrNull<LabelWidget>("PLAYERS");
					if (players != null)
					{
						players.GetText = () => "{0} / {1}".F(game.Players, game.MaxPlayers)
							+ (game.Spectators > 0 ? " + {0}".F(game.Spectators) : "");

						players.GetColor = () => canJoin ? players.TextColor : incompatibleGameColor;
					}

					var state = item.GetOrNull<LabelWidget>("STATUS");
					if (state != null)
					{
						var label = game.State >= (int)ServerState.GameStarted ?
							"Playing" : "Waiting";
						state.GetText = () => label;

						var color = GetStateColor(game, state, !canJoin);
						state.GetColor = () => color;
					}

					var location = item.GetOrNull<LabelWidget>("LOCATION");
					if (location != null)
					{
						var font = Game.Renderer.Fonts[location.Font];
						var cachedServerLocation = GeoIP.LookupCountry(game.Address.Split(':')[0]);
						var label = WidgetUtils.TruncateText(cachedServerLocation, location.Bounds.Width, font);
						location.GetText = () => label;
						location.GetColor = () => canJoin ? location.TextColor : incompatibleGameColor;
					}

					if (currentServer != null && game.Address == currentServer.Address)
						nextServerRow = item;

					rows.Add(item);
				}
			}

			Game.RunAfterTick(() =>
			{
				serverList.RemoveChildren();
				SelectServer(null);

				if (games == null)
				{
					searchStatus = SearchStatus.Failed;
					return;
				}

				if (!rows.Any())
				{
					searchStatus = SearchStatus.NoGames;
					return;
				}

				searchStatus = SearchStatus.Hidden;

				// Search for any unknown maps
				if (Game.Settings.Game.AllowDownloading)
					modData.MapCache.QueryRemoteMapDetails(games.Where(g => !Filtered(g)).Select(g => g.Map));

				foreach (var row in rows)
					serverList.AddChild(row);

				if (nextServerRow != null)
					nextServerRow.OnClick();
			});
		}

		void OpenLobby()
		{
			// Close the multiplayer browser
			Ui.CloseWindow();

			Action onLobbyExit = () =>
			{
				// Open a fresh copy of the multiplayer browser
				Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
				{
					{ "onStart", onStart },
					{ "onExit", onExit },
					{ "directConnectHost", null },
					{ "directConnectPort", 0 },
				});

				Game.Disconnect();
			};

			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onStart", onStart },
				{ "onExit", onLobbyExit },
				{ "skirmishMode", false }
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
			if (server == null || !server.IsJoinable)
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
				var label = "In progress";

				DateTime startTime;
				if (DateTime.TryParse(game.Started, out startTime))
				{
					var totalMinutes = Math.Ceiling((DateTime.UtcNow - startTime).TotalMinutes);
					label += " for {0} minute{1}".F(totalMinutes, totalMinutes > 1 ? "s" : "");
				}

				return label;
			}

			if (game.State == (int)ServerState.WaitingPlayers)
				return game.Protected ? "Password protected" : "Waiting for players";

			if (game.State == (int)ServerState.ShuttingDown)
				return "Server shutting down";

			return "Unknown server state";
		}

		Color GetStateColor(GameServer game, LabelWidget label, bool darkened = false)
		{
			if (!game.Protected && game.State == (int)ServerState.WaitingPlayers)
				return darkened ? incompatibleWaitingGameColor : waitingGameColor;

			if (game.Protected && game.State == (int)ServerState.WaitingPlayers)
				return darkened ? incompatibleProtectedGameColor : protectedGameColor;

			if (game.State == (int)ServerState.GameStarted)
				return darkened ? incompatibleGameStartedColor : gameStartedColor;

			return label.TextColor;
		}

		bool Filtered(GameServer game)
		{
			var filters = Game.Settings.Game.MPGameFilters;
			if (game.State == (int)ServerState.GameStarted && !filters.HasFlag(MPGameFilters.Started))
				return true;

			if (game.State == (int)ServerState.WaitingPlayers && !filters.HasFlag(MPGameFilters.Waiting) && game.Players != 0)
				return true;

			if (game.Players == 0 && !filters.HasFlag(MPGameFilters.Empty))
				return true;

			if (!game.IsCompatible && !filters.HasFlag(MPGameFilters.Incompatible))
				return true;

			if (game.Protected && !filters.HasFlag(MPGameFilters.Protected))
				return true;

			return false;
		}
	}
}
