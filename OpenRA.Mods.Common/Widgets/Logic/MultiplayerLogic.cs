#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using BeaconLib;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MultiplayerLogic : ChromeLogic
	{
		static readonly Action DoNothing = () => { };

		readonly Color incompatibleVersionColor;
		readonly Color incompatibleProtectedGameColor;
		readonly Color protectedGameColor;
		readonly Color incompatibleWaitingGameColor;
		readonly Color waitingGameColor;
		readonly Color incompatibleGameStartedColor;
		readonly Color gameStartedColor;
		readonly Color incompatibleGameColor;
		readonly ModData modData;
		readonly WebServices services;
		readonly Probe lanGameProbe;
		readonly ScrollItemWidget serverTemplate;
		readonly ScrollItemWidget headerTemplate;
		readonly Widget clientContainer;
		readonly ScrollPanelWidget clientList;
		readonly ScrollItemWidget clientTemplate, clientHeader;
		readonly MapPreviewWidget mapPreview;
		readonly ButtonWidget joinButton;
		readonly int joinButtonY;

		GameServer currentServer;
		MapPreview currentMap;

		Action onStart;
		Action onExit;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }
		SearchStatus searchStatus = SearchStatus.Fetching;
		Download currentQuery;
		Widget serverList;
		IEnumerable<BeaconLocation> lanGameLocations;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Failed: return "Failed to query server list.";
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

			services = modData.Manifest.Get<WebServices>();

			incompatibleVersionColor = ChromeMetrics.Get<Color>("IncompatibleVersionColor");
			incompatibleGameColor = ChromeMetrics.Get<Color>("IncompatibleGameColor");
			incompatibleProtectedGameColor = ChromeMetrics.Get<Color>("IncompatibleProtectedGameColor");
			protectedGameColor = ChromeMetrics.Get<Color>("ProtectedGameColor");
			waitingGameColor = ChromeMetrics.Get<Color>("WaitingGameColor");
			incompatibleWaitingGameColor = ChromeMetrics.Get<Color>("IncompatibleWaitingGameColor");
			gameStartedColor = ChromeMetrics.Get<Color>("GameStartedColor");
			incompatibleGameStartedColor = ChromeMetrics.Get<Color>("IncompatibleGameStartedColor");

			serverList = widget.Get<ScrollPanelWidget>("SERVER_LIST");
			headerTemplate = serverList.Get<ScrollItemWidget>("HEADER_TEMPLATE");
			serverTemplate = serverList.Get<ScrollItemWidget>("SERVER_TEMPLATE");

			joinButton = widget.Get<ButtonWidget>("JOIN_BUTTON");
			joinButton.IsVisible = () => currentServer != null;
			joinButton.IsDisabled = () => !currentServer.IsJoinable;
			joinButton.OnClick = () => Join(currentServer);
			joinButtonY = joinButton.Bounds.Y;

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
				filtersButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
				filtersButton.OnMouseDown = _ =>
				{
					filtersButton.RemovePanel();
					filtersButton.AttachPanel(filtersPanel);
				};
			}

			var refreshButton = widget.GetOrNull<ButtonWidget>("REFRESH_BUTTON");
			if (refreshButton != null)
			{
				refreshButton.GetText = () => searchStatus == SearchStatus.Fetching ? "Refreshing..." : "Refresh";
				refreshButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
				refreshButton.OnClick = RefreshServerList;
			}

			mapPreview = widget.GetOrNull<MapPreviewWidget>("SELECTED_MAP_PREVIEW");
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
				players.IsVisible = () => currentServer != null && !currentServer.Clients.Any();
				players.GetText = () => PlayersLabel(currentServer);
			}

			clientContainer = widget.Get("CLIENT_LIST_CONTAINER");
			clientList = Ui.LoadWidget("MULTIPLAYER_CLIENT_LIST", clientContainer, new WidgetArgs()) as ScrollPanelWidget;
			clientList.IsVisible = () => currentServer != null && currentServer.Clients.Any();
			clientHeader = clientList.Get<ScrollItemWidget>("HEADER");
			clientTemplate = clientList.Get<ScrollItemWidget>("TEMPLATE");
			clientList.RemoveChildren();

			var directConnectButton = widget.Get<ButtonWidget>("DIRECTCONNECT_BUTTON");
			directConnectButton.OnClick = () =>
			{
				Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
				{
					{ "openLobby", OpenLobby },
					{ "onExit", DoNothing },
					{ "directConnectHost", null },
					{ "directConnectPort", 0 },
				});
			};

			var createServerButton = widget.Get<ButtonWidget>("CREATE_BUTTON");
			createServerButton.OnClick = () =>
			{
				Ui.OpenWindow("MULTIPLAYER_CREATESERVER_PANEL", new WidgetArgs
				{
					{ "openLobby", OpenLobby },
					{ "onExit", DoNothing }
				});
			};

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			lanGameLocations = new List<BeaconLocation>();
			try
			{
				lanGameProbe = new Probe("OpenRALANGame");
				lanGameProbe.BeaconsUpdated += locations => lanGameLocations = locations;
				lanGameProbe.Start();
			}
			catch (Exception ex)
			{
				Log.Write("debug", "BeaconLib.Probe: " + ex.Message);
			}

			RefreshServerList();

			if (directConnectHost != null)
			{
				// The connection window must be opened at the end of the tick for the widget hierarchy to
				// work out, but we also want to prevent the server browser from flashing visible for one tick.
				widget.Visible = false;
				Game.RunAfterTick(() =>
				{
					Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
					{
						{ "openLobby", OpenLobby },
						{ "onExit", DoNothing },
						{ "directConnectHost", directConnectHost },
						{ "directConnectPort", directConnectPort },
					});

					widget.Visible = true;
				});
			}
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

				List<GameServer> games = null;
				if (i.Error == null)
				{
					try
					{
						var data = Encoding.UTF8.GetString(i.Result);
						var yaml = MiniYaml.FromString(data);

						games = yaml.Select(a => new GameServer(a.Value))
							.Where(gs => gs.Address != null)
							.ToList();
					}
					catch
					{
						searchStatus = SearchStatus.Failed;
					}
				}

				var lanGames = new List<GameServer>();
				foreach (var bl in lanGameLocations)
				{
					var game = MiniYaml.FromString(bl.Data)[0].Value;
					var idNode = game.Nodes.FirstOrDefault(n => n.Key == "Id");

					// Skip beacons created by this instance and replace Id by expected int value
					if (idNode != null && idNode.Value.Value != Platform.SessionGUID.ToString())
					{
						idNode.Value.Value = "-1";

						// Rewrite the server address with the correct IP
						var addressNode = game.Nodes.FirstOrDefault(n => n.Key == "Address");
						if (addressNode != null)
							addressNode.Value.Value = bl.Address.ToString().Split(':')[0] + ":" + addressNode.Value.Value.Split(':')[1];

						try
						{
							lanGames.Add(new GameServer(game));
						}
						catch { }
					}
				}

				var groupedLanGames = lanGames.GroupBy(gs => gs.Address).Select(g => g.Last());
				if (games != null)
					games.AddRange(groupedLanGames);
				else if (groupedLanGames.Any())
					games = groupedLanGames.ToList();

				Game.RunAfterTick(() => RefreshServerListInner(games));
			};

			var queryURL = services.ServerList + "?protocol={0}&engine={1}&mod={2}&version={3}".F(
				GameServer.ProtocolVersion,
				Uri.EscapeUriString(Game.EngineVersion),
				Uri.EscapeUriString(Game.ModData.Manifest.Id),
				Uri.EscapeUriString(Game.ModData.Manifest.Metadata.Version));

			currentQuery = new Download(queryURL, _ => { }, onComplete);
		}

		int GroupSortOrder(GameServer testEntry)
		{
			// Games that we can't join are sorted last
			if (!testEntry.IsCompatible)
				return testEntry.Mod == modData.Manifest.Id ? 1 : 0;

			// Games for the current mod+version are sorted first
			if (testEntry.Mod == modData.Manifest.Id)
				return testEntry.Version == modData.Manifest.Metadata.Version ? 4 : 3;

			// Followed by games for different mods that are joinable
			return 2;
		}

		void SelectServer(GameServer server)
		{
			currentServer = server;
			currentMap = server != null ? modData.MapCache[server.Map] : null;

			clientList.RemoveChildren();
			if (server == null || !server.Clients.Any())
			{
				joinButton.Bounds.Y = joinButtonY;
				return;
			}

			joinButton.Bounds.Y = clientContainer.Bounds.Bottom;

			var players = server.Clients
				.Where(c => !c.IsSpectator)
				.GroupBy(p => p.Team)
				.OrderBy(g => g.Key);

			var teams = new Dictionary<string, IEnumerable<GameClient>>();
			var noTeams = players.Count() == 1;
			foreach (var p in players)
			{
				var label = noTeams ? "Players" : p.Key == 0 ? "No Team" : "Team {0}".F(p.Key);
				teams.Add(label, p);
			}

			if (server.Clients.Any(c => c.IsSpectator))
				teams.Add("Spectators", server.Clients.Where(c => c.IsSpectator));

			// Can only show factions if the server is running the same mod
			var disableFactionDisplay = server.Mod != modData.Manifest.Id;

			if (mapPreview != null)
			{
				var spawns = currentMap.SpawnPoints;
				var occupants = server.Clients
					.Where(c => (c.SpawnPoint - 1 >= 0) && (c.SpawnPoint - 1 < spawns.Length))
					.ToDictionary(c => spawns[c.SpawnPoint - 1], c => new SpawnOccupant(c, disableFactionDisplay));

				mapPreview.SpawnOccupants = () => occupants;
			}

			var factionInfo = modData.DefaultRules.Actors["world"].TraitInfos<FactionInfo>();
			foreach (var kv in teams)
			{
				var group = kv.Key;
				if (group.Length > 0)
				{
					var header = ScrollItemWidget.Setup(clientHeader, () => true, () => { });
					header.Get<LabelWidget>("LABEL").GetText = () => group;
					clientList.AddChild(header);
				}

				foreach (var option in kv.Value)
				{
					var o = option;

					var item = ScrollItemWidget.Setup(clientTemplate, () => false, () => { });
					if (!o.IsSpectator && !disableFactionDisplay)
					{
						var label = item.Get<LabelWidget>("LABEL");
						var font = Game.Renderer.Fonts[label.Font];
						var name = WidgetUtils.TruncateText(o.Name, label.Bounds.Width, font);
						label.GetText = () => name;
						label.GetColor = () => o.Color.RGB;

						var flag = item.Get<ImageWidget>("FLAG");
						flag.IsVisible = () => true;
						flag.GetImageCollection = () => "flags";
						flag.GetImageName = () => (factionInfo != null && factionInfo.Any(f => f.InternalName == o.Faction)) ? o.Faction : "Random";
					}
					else
					{
						var label = item.Get<LabelWidget>("NOFLAG_LABEL");
						var font = Game.Renderer.Fonts[label.Font];
						var name = WidgetUtils.TruncateText(o.Name, label.Bounds.Width, font);
						label.GetText = () => name;
						label.GetColor = () => o.Color.RGB;
					}

					clientList.AddChild(item);
				}
			}
		}

		void RefreshServerListInner(List<GameServer> games)
		{
			ScrollItemWidget nextServerRow = null;
			List<Widget> rows = null;

			if (games != null)
				rows = LoadGameRows(games, out nextServerRow);

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
					modData.MapCache.QueryRemoteMapDetails(services.MapRepository, games.Where(g => !Filtered(g)).Select(g => g.Map));

				foreach (var row in rows)
					serverList.AddChild(row);

				if (nextServerRow != null)
					nextServerRow.OnClick();
			});
		}

		List<Widget> LoadGameRows(List<GameServer> games, out ScrollItemWidget nextServerRow)
		{
			nextServerRow = null;
			var rows = new List<Widget>();
			var mods = games.GroupBy(g => g.ModLabel)
				.OrderByDescending(g => GroupSortOrder(g.First()))
				.ThenByDescending(g => g.Count());

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

					// Then servers with spectators
					if (g.State == (int)ServerState.WaitingPlayers && g.Spectators > 0)
						return 1;

					// Then active games
					if (g.State >= (int)ServerState.GameStarted)
						return 2;

					// Empty servers are shown at the end because a flood of empty servers
					// at the top of the game list make the community look dead
					return 3;
				};

				foreach (var modGamesByState in modGames.GroupBy(listOrder).OrderBy(g => g.Key))
				{
					// Sort 'Playing' games by Started, others by number of players
					foreach (var game in modGamesByState.Key == 2 ? modGamesByState.OrderByDescending(g => g.Started) : modGamesByState.OrderByDescending(g => g.Players))
					{
						if (Filtered(game))
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
							var label = "{0} / {1}".F(game.Players + game.Bots, game.MaxPlayers + game.Bots)
								+ (game.Spectators > 0 ? " + {0}".F(game.Spectators) : "");

							var color = canJoin ? players.TextColor : incompatibleGameColor;
							players.GetText = () => label;
							players.GetColor = () => color;
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
							var cachedServerLocation = game.Id != -1 ? GeoIP.LookupCountry(game.Address.Split(':')[0]) : "Local Network";
							var label = WidgetUtils.TruncateText(cachedServerLocation, location.Bounds.Width, font);
							location.GetText = () => label;
							location.GetColor = () => canJoin ? location.TextColor : incompatibleGameColor;
						}

						if (currentServer != null && game.Address == currentServer.Address)
							nextServerRow = item;

						rows.Add(item);
					}
				}
			}

			return rows;
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

				if (game.PlayTime > 0)
				{
					var totalMinutes = Math.Ceiling(game.PlayTime / 60.0);
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

			if ((game.Players + game.Spectators) == 0 && !filters.HasFlag(MPGameFilters.Empty))
				return true;

			if (!game.IsCompatible && !filters.HasFlag(MPGameFilters.Incompatible))
				return true;

			if (game.Protected && !filters.HasFlag(MPGameFilters.Protected))
				return true;

			return false;
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				if (lanGameProbe != null)
					lanGameProbe.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
