#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeaconLib;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ServerListLogic : ChromeLogic
	{
		[TranslationReference]
		const string SearchStatusFailed = "label-search-status-failed";

		[TranslationReference]
		const string SearchStatusNoGames = "label-search-status-no-games";

		[TranslationReference("players")]
		const string PlayersOnline = "label-players-online-count";

		[TranslationReference]
		const string NoServerSelected = "label-no-server-selected";

		[TranslationReference]
		const string MapStatusSearching = "label-map-status-searching";

		[TranslationReference]
		const string MapClassificationUnknown = "label-map-classification-unknown";

		[TranslationReference("players")]
		const string PlayersLabel = "label-players-count";

		[TranslationReference("bots")]
		const string BotsLabel = "label-bots-count";

		[TranslationReference("spectators")]
		const string SpectatorsLabel = "label-spectators-count";

		[TranslationReference]
		const string Players = "label-players";

		[TranslationReference("team")]
		const string TeamNumber = "label-team-name";

		[TranslationReference]
		const string NoTeam = "label-no-team";

		[TranslationReference]
		const string Spectators = "label-spectators";

		[TranslationReference("players")]
		const string OtherPlayers = "label-other-players-count";

		[TranslationReference]
		const string Playing = "label-playing";

		[TranslationReference]
		const string Waiting = "label-waiting";

		[TranslationReference("minutes")]
		const string InProgress = "label-in-progress-for";

		[TranslationReference]
		const string PasswordProtected = "label-password-protected";

		[TranslationReference]
		const string WaitingForPlayers = "label-waiting-for-players";

		[TranslationReference]
		const string ServerShuttingDown = "label-server-shutting-down";

		[TranslationReference]
		const string UnknownServerState = "label-unknown-server-state";

		readonly string noServerSelected;
		readonly string mapStatusSearching;
		readonly string mapClassificationUnknown;
		readonly string playing;
		readonly string waiting;

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

		readonly Widget serverList;
		readonly ScrollItemWidget serverTemplate;
		readonly ScrollItemWidget headerTemplate;
		readonly Widget noticeContainer;
		readonly Widget clientContainer;
		readonly ScrollPanelWidget clientList;
		readonly ScrollItemWidget clientTemplate, clientHeader;
		readonly MapPreviewWidget mapPreview;
		readonly ButtonWidget joinButton;
		readonly int joinButtonY;

		readonly Action<GameServer> onJoin;

		GameServer currentServer;
		MapPreview currentMap;
		bool showNotices;
		int playerCount;

		enum SearchStatus { Fetching, Failed, NoGames, Hidden }

		SearchStatus searchStatus = SearchStatus.Fetching;

		bool activeQuery;
		IEnumerable<BeaconLocation> lanGameLocations;

		readonly CachedTransform<int, string> players;
		readonly CachedTransform<int, string> bots;
		readonly CachedTransform<int, string> spectators;

		readonly CachedTransform<double, string> minutes;
		readonly string passwordProtected;
		readonly string waitingForPlayers;
		readonly string serverShuttingDown;
		readonly string unknownServerState;

		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Failed: return modData.Translation.GetString(SearchStatusFailed);
				case SearchStatus.NoGames: return modData.Translation.GetString(SearchStatusNoGames);
				default: return "";
			}
		}

		[ObjectCreator.UseCtor]
		public ServerListLogic(Widget widget, ModData modData, Action<GameServer> onJoin)
		{
			this.modData = modData;
			this.onJoin = onJoin;

			playing = modData.Translation.GetString(Playing);
			waiting = modData.Translation.GetString(Waiting);

			noServerSelected = modData.Translation.GetString(NoServerSelected);
			mapStatusSearching = modData.Translation.GetString(MapStatusSearching);
			mapClassificationUnknown = modData.Translation.GetString(MapClassificationUnknown);

			players = new CachedTransform<int, string>(i => modData.Translation.GetString(PlayersLabel, Translation.Arguments("players", i)));
			bots = new CachedTransform<int, string>(i => modData.Translation.GetString(BotsLabel, Translation.Arguments("bots", i)));
			spectators = new CachedTransform<int, string>(i => modData.Translation.GetString(SpectatorsLabel, Translation.Arguments("spectators", i)));

			minutes = new CachedTransform<double, string>(i => modData.Translation.GetString(InProgress, Translation.Arguments("minutes", i)));
			passwordProtected = modData.Translation.GetString(PasswordProtected);
			waitingForPlayers = modData.Translation.GetString(WaitingForPlayers);
			serverShuttingDown = modData.Translation.GetString(ServerShuttingDown);
			unknownServerState = modData.Translation.GetString(UnknownServerState);

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

			noticeContainer = widget.GetOrNull("NOTICE_CONTAINER");
			if (noticeContainer != null)
			{
				noticeContainer.IsVisible = () => showNotices;
				noticeContainer.Get("OUTDATED_VERSION_LABEL").IsVisible = () => services.ModVersionStatus == ModVersionStatus.Outdated;
				noticeContainer.Get("UNKNOWN_VERSION_LABEL").IsVisible = () => services.ModVersionStatus == ModVersionStatus.Unknown;
				noticeContainer.Get("PLAYTEST_AVAILABLE_LABEL").IsVisible = () => services.ModVersionStatus == ModVersionStatus.PlaytestAvailable;
			}

			var noticeWatcher = widget.Get<LogicTickerWidget>("NOTICE_WATCHER");
			if (noticeWatcher != null && noticeContainer != null)
			{
				var containerHeight = noticeContainer.Bounds.Height;
				noticeWatcher.OnTick = () =>
				{
					var show = services.ModVersionStatus != ModVersionStatus.NotChecked && services.ModVersionStatus != ModVersionStatus.Latest;
					if (show != showNotices)
					{
						var dir = show ? 1 : -1;
						serverList.Bounds.Y += dir * containerHeight;
						serverList.Bounds.Height -= dir * containerHeight;
						showNotices = show;
					}
				};
			}

			joinButton = widget.GetOrNull<ButtonWidget>("JOIN_BUTTON");
			if (joinButton != null)
			{
				joinButton.IsVisible = () => currentServer != null;
				joinButton.IsDisabled = () => !currentServer.IsJoinable;
				joinButton.OnClick = () => onJoin(currentServer);
				joinButtonY = joinButton.Bounds.Y;
			}

			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = widget.Get<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;

			var gs = Game.Settings.Game;
			void ToggleFilterFlag(MPGameFilters f)
			{
				gs.MPGameFilters ^= f;
				Game.Settings.Save();
				RefreshServerList();
			}

			var filtersButton = widget.GetOrNull<DropDownButtonWidget>("FILTERS_DROPDOWNBUTTON");
			if (filtersButton != null)
			{
				// HACK: MULTIPLAYER_FILTER_PANEL doesn't follow our normal procedure for dropdown creation
				// but we still need to be able to set the dropdown width based on the parent
				// The yaml should use PARENT_RIGHT instead of DROPDOWN_WIDTH
				var filtersPanel = Ui.LoadWidget("MULTIPLAYER_FILTER_PANEL", filtersButton, new WidgetArgs());
				filtersButton.Children.Remove(filtersPanel);

				var showWaitingCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("WAITING_FOR_PLAYERS");
				if (showWaitingCheckbox != null)
				{
					showWaitingCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Waiting);
					showWaitingCheckbox.OnClick = () => ToggleFilterFlag(MPGameFilters.Waiting);
				}

				var showEmptyCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("EMPTY");
				if (showEmptyCheckbox != null)
				{
					showEmptyCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Empty);
					showEmptyCheckbox.OnClick = () => ToggleFilterFlag(MPGameFilters.Empty);
				}

				var showAlreadyStartedCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("ALREADY_STARTED");
				if (showAlreadyStartedCheckbox != null)
				{
					showAlreadyStartedCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Started);
					showAlreadyStartedCheckbox.OnClick = () => ToggleFilterFlag(MPGameFilters.Started);
				}

				var showProtectedCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("PASSWORD_PROTECTED");
				if (showProtectedCheckbox != null)
				{
					showProtectedCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Protected);
					showProtectedCheckbox.OnClick = () => ToggleFilterFlag(MPGameFilters.Protected);
				}

				var showIncompatibleCheckbox = filtersPanel.GetOrNull<CheckboxWidget>("INCOMPATIBLE_VERSION");
				if (showIncompatibleCheckbox != null)
				{
					showIncompatibleCheckbox.IsChecked = () => gs.MPGameFilters.HasFlag(MPGameFilters.Incompatible);
					showIncompatibleCheckbox.OnClick = () => ToggleFilterFlag(MPGameFilters.Incompatible);
				}

				filtersButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
				filtersButton.OnMouseDown = _ =>
				{
					filtersButton.RemovePanel();
					filtersButton.AttachPanel(filtersPanel);
				};
			}

			var reloadButton = widget.GetOrNull<ButtonWidget>("RELOAD_BUTTON");
			if (reloadButton != null)
			{
				reloadButton.IsDisabled = () => searchStatus == SearchStatus.Fetching;
				reloadButton.OnClick = RefreshServerList;

				var reloadIcon = reloadButton.GetOrNull<ImageWidget>("IMAGE_RELOAD");
				if (reloadIcon != null)
				{
					var disabledFrame = 0;
					var disabledImage = "disabled-" + disabledFrame.ToString();
					reloadIcon.GetImageName = () => searchStatus == SearchStatus.Fetching ? disabledImage : reloadIcon.ImageName;

					var reloadTicker = reloadIcon.Get<LogicTickerWidget>("ANIMATION");
					if (reloadTicker != null)
					{
						reloadTicker.OnTick = () =>
						{
							disabledFrame = searchStatus == SearchStatus.Fetching ? (disabledFrame + 1) % 12 : 0;
							disabledImage = "disabled-" + disabledFrame.ToString();
						};
					}
				}
			}

			var playersLabel = widget.GetOrNull<LabelWidget>("PLAYER_COUNT");
			if (playersLabel != null)
			{
				var playersText = new CachedTransform<int, string>(p => modData.Translation.GetString(PlayersOnline, Translation.Arguments("players", p)));
				playersLabel.IsVisible = () => playerCount != 0;
				playersLabel.GetText = () => playersText.Update(playerCount);
			}

			mapPreview = widget.GetOrNull<MapPreviewWidget>("SELECTED_MAP_PREVIEW");
			if (mapPreview != null)
				mapPreview.Preview = () => currentMap;

			var mapTitle = widget.GetOrNull<LabelWithTooltipWidget>("SELECTED_MAP");
			if (mapTitle != null)
			{
				var font = Game.Renderer.Fonts[mapTitle.Font];
				var title = new CachedTransform<MapPreview, string>(m =>
				{
					var truncated = WidgetUtils.TruncateText(m.Title, mapTitle.Bounds.Width, font);

					if (m.Title != truncated)
						mapTitle.GetTooltipText = () => m.Title;
					else
						mapTitle.GetTooltipText = null;

					return truncated;
				});

				mapTitle.GetText = () =>
				{
					if (currentMap == null)
						return noServerSelected;

					if (currentMap.Status == MapStatus.Searching)
						return mapStatusSearching;

					if (currentMap.Class == MapClassification.Unknown)
						return mapClassificationUnknown;

					return title.Update(currentMap);
				};
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
				var version = new CachedTransform<GameServer, string>(s => WidgetUtils.TruncateText(s.ModLabel, modVersion.Bounds.Width, font));
				modVersion.GetText = () => version.Update(currentServer);
			}

			var selectedPlayers = widget.GetOrNull<LabelWidget>("SELECTED_PLAYERS");
			if (selectedPlayers != null)
			{
				selectedPlayers.IsVisible = () => currentServer != null && (clientContainer == null || currentServer.Clients.Length == 0);
				selectedPlayers.GetText = () => PlayerLabel(currentServer);
			}

			clientContainer = widget.GetOrNull("CLIENT_LIST_CONTAINER");
			if (clientContainer != null)
			{
				clientList = Ui.LoadWidget("MULTIPLAYER_CLIENT_LIST", clientContainer, new WidgetArgs()) as ScrollPanelWidget;
				clientList.IsVisible = () => currentServer != null && currentServer.Clients.Length > 0;
				clientHeader = clientList.Get<ScrollItemWidget>("HEADER");
				clientTemplate = clientList.Get<ScrollItemWidget>("TEMPLATE");
				clientList.RemoveChildren();
			}

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
		}

		string PlayerLabel(GameServer game)
		{
			var label = players.Update(game.Players);

			if (game.Bots > 0)
				label += " " + bots.Update(game.Bots);

			if (game.Spectators > 0)
				label += " " + spectators.Update(game.Spectators);

			return label;
		}

		public void RefreshServerList()
		{
			// Query in progress
			if (activeQuery)
				return;

			searchStatus = SearchStatus.Fetching;

			var queryURL = new HttpQueryBuilder(services.ServerList)
			{
				{ "protocol", GameServer.ProtocolVersion },
				{ "engine", Game.EngineVersion },
				{ "mod", Game.ModData.Manifest.Id },
				{ "version", Game.ModData.Manifest.Metadata.Version }
			}.ToString();

			Task.Run(async () =>
			{
				List<GameServer> games = null;
				activeQuery = true;

				try
				{
					var client = HttpClientFactory.Create();
					var httpResponseMessage = await client.GetAsync(queryURL);
					var result = await httpResponseMessage.Content.ReadAsStreamAsync();

					var yaml = MiniYaml.FromStream(result);
					games = new List<GameServer>();
					foreach (var node in yaml)
					{
						try
						{
							var gs = new GameServer(node.Value);
							if (gs.Address != null)
								games.Add(gs);
						}
						catch
						{
							// Ignore any invalid games advertised.
						}
					}
				}
				catch (Exception e)
				{
					searchStatus = SearchStatus.Failed;
					Log.Write("debug", $"Failed to query server list with exception: {e}");
				}

				var lanGames = new List<GameServer>();
				foreach (var bl in lanGameLocations)
				{
					try
					{
						if (string.IsNullOrEmpty(bl.Data))
							continue;

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

							game.Nodes.Add(new MiniYamlNode("Location", "Local Network"));

							lanGames.Add(new GameServer(game));
						}
					}
					catch
					{
						// Ignore any invalid LAN games advertised.
					}
				}

				var groupedLanGames = lanGames.GroupBy(gs => gs.Address).Select(g => g.Last());
				if (games != null)
					games.AddRange(groupedLanGames);
				else if (groupedLanGames.Any())
					games = groupedLanGames.ToList();

				Game.RunAfterTick(() => RefreshServerListInner(games));

				activeQuery = false;
			});
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

			// Can only show factions if the server is running the same mod
			if (server != null && mapPreview != null)
			{
				var spawns = currentMap.SpawnPoints;
				var occupants = server.Clients
					.Where(c => (c.SpawnPoint - 1 >= 0) && (c.SpawnPoint - 1 < spawns.Length))
					.ToDictionary(c => c.SpawnPoint, c => new SpawnOccupant(c, server.Mod != modData.Manifest.Id));

				mapPreview.SpawnOccupants = () => occupants;
				mapPreview.DisabledSpawnPoints = () => server.DisabledSpawnPoints;
			}

			if (server == null || server.Clients.Length == 0)
			{
				if (joinButton != null)
					joinButton.Bounds.Y = joinButtonY;

				return;
			}

			if (joinButton != null)
				joinButton.Bounds.Y = clientContainer.Bounds.Bottom;

			if (clientList == null)
				return;

			clientList.RemoveChildren();

			var players = server.Clients
				.Where(c => !c.IsSpectator)
				.GroupBy(p => p.Team)
				.OrderBy(g => g.Key);

			var teams = new Dictionary<string, IEnumerable<GameClient>>();
			var noTeams = players.Count() == 1;
			foreach (var p in players)
			{
				var label = noTeams ? modData.Translation.GetString(Players) : p.Key > 0
					? modData.Translation.GetString(TeamNumber, Translation.Arguments("team", p.Key))
					: modData.Translation.GetString(NoTeam);
				teams.Add(label, p);
			}

			if (server.Clients.Any(c => c.IsSpectator))
				teams.Add(modData.Translation.GetString(Spectators), server.Clients.Where(c => c.IsSpectator));

			var factionInfo = modData.DefaultRules.Actors[SystemActors.World].TraitInfos<FactionInfo>();
			foreach (var kv in teams)
			{
				var group = kv.Key;
				if (group.Length > 0)
				{
					var header = ScrollItemWidget.Setup(clientHeader, () => false, () => { });
					header.Get<LabelWidget>("LABEL").GetText = () => group;
					clientList.AddChild(header);
				}

				foreach (var option in kv.Value)
				{
					var o = option;

					var item = ScrollItemWidget.Setup(clientTemplate, () => false, () => { });
					if (!o.IsSpectator && server.Mod == modData.Manifest.Id)
					{
						var label = item.Get<LabelWidget>("LABEL");
						var font = Game.Renderer.Fonts[label.Font];
						var name = WidgetUtils.TruncateText(o.Name, label.Bounds.Width, font);
						label.GetText = () => name;
						label.GetColor = () => o.Color;

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

						// Force spectator color to prevent spoofing by the server
						var color = o.IsSpectator ? Color.White : o.Color;
						label.GetText = () => name;
						label.GetColor = () => color;
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

				if (rows.Count == 0)
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

				nextServerRow?.OnClick();

				playerCount = games.Sum(g => g.Players);
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

				var header = ScrollItemWidget.Setup(headerTemplate, () => false, () => { });

				var headerTitle = modGames.First().ModLabel;
				header.Get<LabelWidget>("LABEL").GetText = () => headerTitle;
				rows.Add(header);

				int ListOrder(GameServer g)
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
				}

				foreach (var modGamesByState in modGames.GroupBy(ListOrder).OrderBy(g => g.Key))
				{
					// Sort 'Playing' games by Started, others by number of players
					foreach (var game in modGamesByState.Key == 2 ? modGamesByState.OrderByDescending(g => g.Started) : modGamesByState.OrderByDescending(g => g.Players))
					{
						if (Filtered(game))
							continue;

						var canJoin = game.IsJoinable;
						var item = ScrollItemWidget.Setup(serverTemplate, () => currentServer == game, () => SelectServer(game), () => onJoin(game));
						var title = item.GetOrNull<LabelWithTooltipWidget>("TITLE");
						if (title != null)
						{
							WidgetUtils.TruncateLabelToTooltip(title, game.Name);
							title.GetColor = () => canJoin ? title.TextColor : incompatibleGameColor;
						}

						var password = item.GetOrNull<ImageWidget>("PASSWORD_PROTECTED");
						if (password != null)
						{
							password.IsVisible = () => game.Protected;
							password.GetImageName = () => canJoin ? "protected" : "protected-disabled";
						}

						var auth = item.GetOrNull<ImageWidget>("REQUIRES_AUTHENTICATION");
						if (auth != null)
						{
							auth.IsVisible = () => game.Authentication;
							auth.GetImageName = () => canJoin ? "authentication" : "authentication-disabled";

							if (game.Protected && password != null)
								auth.Bounds.X -= password.Bounds.Width + 5;
						}

						var players = item.GetOrNull<LabelWithTooltipWidget>("PLAYERS");
						if (players != null)
						{
							var label = $"{game.Players + game.Bots} / {game.MaxPlayers + game.Bots}"
							            + (game.Spectators > 0 ? $" + {game.Spectators}" : "");

							var color = canJoin ? players.TextColor : incompatibleGameColor;
							players.GetText = () => label;
							players.GetColor = () => color;

							if (game.Clients.Length > 0)
							{
								var displayClients = game.Clients.Select(c => c.Name);
								if (game.Clients.Length > 10)
									displayClients = displayClients
										.Take(9)
										.Append(modData.Translation.GetString(OtherPlayers, Translation.Arguments("players", game.Clients.Length - 9)));

								var tooltip = displayClients.JoinWith("\n");
								players.GetTooltipText = () => tooltip;
							}
							else
								players.GetTooltipText = null;
						}

						var state = item.GetOrNull<LabelWidget>("STATUS");
						if (state != null)
						{
							var label = game.State >= (int)ServerState.GameStarted ? playing : waiting;
							state.GetText = () => label;

							var color = GetStateColor(game, state, !canJoin);
							state.GetColor = () => color;
						}

						var location = item.GetOrNull<LabelWidget>("LOCATION");
						if (location != null)
						{
							var font = Game.Renderer.Fonts[location.Font];
							var label = WidgetUtils.TruncateText(game.Location, location.Bounds.Width, font);
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

		string GetStateLabel(GameServer game)
		{
			if (game == null)
				return string.Empty;

			if (game.State == (int)ServerState.GameStarted)
			{
				var totalMinutes = Math.Ceiling(game.PlayTime / 60.0);
				return minutes.Update(totalMinutes);
			}

			if (game.State == (int)ServerState.WaitingPlayers)
				return game.Protected ? passwordProtected : waitingForPlayers;

			if (game.State == (int)ServerState.ShuttingDown)
				return serverShuttingDown;

			return unknownServerState;
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
				lanGameProbe?.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
