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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyLogic
	{
		enum PanelType { Players, Options, Kick, MapDownload }
		PanelType panel = PanelType.Players;

		Widget lobby;

		Widget editablePlayerTemplate, nonEditablePlayerTemplate, emptySlotTemplate,
			editableSpectatorTemplate, nonEditableSpectatorTemplate, newSpectatorTemplate;

		ScrollPanelWidget chatPanel;
		Widget chatTemplate;

		Widget downloadMapPanel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;

		ScrollPanelWidget players;
		Dictionary<string, string> countryNames;
		string mapUid;
		Map map;
		int mapDownloadAttempts;

		ColorPreviewManagerWidget colorPreview;

		readonly Action OnGameStart;
		readonly Action onExit;
		readonly OrderManager orderManager;
		readonly bool skirmishMode;

		// Listen for connection failures
		void ConnectionStateChanged(OrderManager om)
		{
			if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				// Show connection failed dialog
				CloseWindow();

				Action onConnect = () =>
				{
					Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
					{
						{ "onExit", onExit },
						{ "onStart", OnGameStart },
						{ "skirmishMode", false }
					});
				};

				Action<string> onRetry = password =>
				{
					ConnectionLogic.Connect(om.Host, om.Port, password, onConnect, onExit);
				};

				Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
				{
					{ "orderManager", om },
					{ "onAbort", onExit },
					{ "onRetry", onRetry }
				});
			}
		}

		void CloseWindow()
		{
			Game.LobbyInfoChanged -= UpdateCurrentMap;
			Game.LobbyInfoChanged -= UpdatePlayerList;
			Game.BeforeGameStart -= OnGameStart;
			Game.AddChatLine -= AddChatLine;
			Game.ConnectionStateChanged -= ConnectionStateChanged;

			Ui.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		internal LobbyLogic(Widget widget, World world, OrderManager orderManager,
			Action onExit, Action onStart, bool skirmishMode)
		{
			lobby = widget;
			this.orderManager = orderManager;
			this.OnGameStart = () => { CloseWindow(); onStart(); };
			this.onExit = onExit;
			this.skirmishMode = skirmishMode;

			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.BeforeGameStart += OnGameStart;
			Game.AddChatLine += AddChatLine;
			Game.ConnectionStateChanged += ConnectionStateChanged;

			var name = lobby.GetOrNull<LabelWidget>("SERVER_NAME");
			if (name != null)
				name.GetText = () => orderManager.LobbyInfo.GlobalSettings.ServerName;

			UpdateCurrentMap();
			players = Ui.LoadWidget<ScrollPanelWidget>("LOBBY_PLAYER_BIN", lobby.Get("PLAYER_BIN_ROOT"), new WidgetArgs());
			players.IsVisible = () => panel == PanelType.Players;

			var playerBinHeaders = lobby.GetOrNull<ContainerWidget>("LABEL_CONTAINER");
			if (playerBinHeaders != null)
				playerBinHeaders.IsVisible = () => panel == PanelType.Players;

			editablePlayerTemplate = players.Get("TEMPLATE_EDITABLE_PLAYER");
			nonEditablePlayerTemplate = players.Get("TEMPLATE_NONEDITABLE_PLAYER");
			emptySlotTemplate = players.Get("TEMPLATE_EMPTY");
			editableSpectatorTemplate = players.Get("TEMPLATE_EDITABLE_SPECTATOR");
			nonEditableSpectatorTemplate = players.Get("TEMPLATE_NONEDITABLE_SPECTATOR");
			newSpectatorTemplate = players.Get("TEMPLATE_NEW_SPECTATOR");

			colorPreview = lobby.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = Game.Settings.Player.Color;

			var mapPreview = lobby.Get<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.IsVisible = () => map != null;
			mapPreview.Map = () => map;
			mapPreview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, mapPreview, map, mi);
			mapPreview.SpawnClients = () => LobbyUtils.GetSpawnClients(orderManager, map);

			var mapTitle = lobby.GetOrNull<LabelWidget>("MAP_TITLE");
			if (mapTitle != null)
			{
				mapTitle.IsVisible = () => map != null;
				mapTitle.GetText = () => map.Title;
			}

			var mapType = lobby.GetOrNull<LabelWidget>("MAP_TYPE");
			if (mapType != null)
			{
				mapType.IsVisible = () => map != null;
				mapType.GetText = () => map.Type;
			}

			var mapAuthor = lobby.GetOrNull<LabelWidget>("MAP_AUTHOR");
			if (mapAuthor != null)
			{
				mapAuthor.IsVisible = () => map != null;
				mapAuthor.GetText = () => "Created by {0}".F(map.Author);
			}

			downloadMapPanel = Ui.LoadWidget("DOWNLOAD_MAP_PANEL", lobby, new WidgetArgs());
			downloadMapPanel.IsVisible = () => panel == PanelType.MapDownload;
			progressBar = downloadMapPanel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = downloadMapPanel.Get<LabelWidget>("STATUS_LABEL");

			countryNames = Rules.Info["world"].Traits.WithInterface<CountryInfo>()
				.Where(c => c.Selectable)
				.ToDictionary(a => a.Race, a => a.Name);
			countryNames.Add("random", "Any");

			var gameStarting = false;
			Func<bool> configurationDisabled = () => !Game.IsHost || gameStarting
				|| orderManager.LocalClient == null || orderManager.LocalClient.IsReady
				|| panel == PanelType.MapDownload || panel == PanelType.Kick;

			var mapButton = lobby.GetOrNull<ButtonWidget>("CHANGEMAP_BUTTON");
			if (mapButton != null)
			{
				mapButton.IsDisabled = configurationDisabled; 
				mapButton.OnClick = () =>
				{
					var onSelect = new Action<Map>(m =>
					{
						orderManager.IssueOrder(Order.Command("map " + m.Uid));
						Game.Settings.Server.Map = m.Uid;
						Game.Settings.Save();
					});

					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", map.Uid },
						{ "onExit", () => {} },
						{ "onSelect", onSelect }
					});
				};
			}

			var slotsButton = lobby.GetOrNull<DropDownButtonWidget>("SLOTS_DROPDOWNBUTTON");
			if (slotsButton != null)
			{
				slotsButton.IsDisabled = () => configurationDisabled() || panel != PanelType.Players ||
					!orderManager.LobbyInfo.Slots.Values.Any(s => s.AllowBots || !s.LockTeam);

				var aiModes = Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name);
				slotsButton.OnMouseDown = _ =>
				{
					var options = new Dictionary<string, IEnumerable<DropDownOption>>();

					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					if (orderManager.LobbyInfo.Slots.Values.Any(s => s.AllowBots))
					{
						var botOptions = new List<DropDownOption>(){ new DropDownOption()
						{
							Title = "Add",
							IsSelected = () => false,
							OnClick = () =>
							{
								foreach (var slot in orderManager.LobbyInfo.Slots)
								{
									var bot = aiModes.Random(Game.CosmeticRandom);
									var c = orderManager.LobbyInfo.ClientInSlot(slot.Key);
									if (slot.Value.AllowBots == true && (c == null || c.Bot != null))
										orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot.Key, botController.Index, bot)));
								}
							}
						}};

						if (orderManager.LobbyInfo.Clients.Any(c => c.Bot != null))
						{
							botOptions.Add(new DropDownOption()
							{
								Title = "Remove",
								IsSelected = () => false,
								OnClick = () =>
								{
									foreach (var slot in orderManager.LobbyInfo.Slots)
									{
										var c = orderManager.LobbyInfo.ClientInSlot(slot.Key);
										if (c != null && c.Bot != null)
											orderManager.IssueOrder(Order.Command("slot_open "+slot.Value.PlayerReference));
									}
								}
							});
						}

						options.Add("Configure Bots", botOptions);
					}

					var teamCount = (orderManager.LobbyInfo.Slots.Count(s => !s.Value.LockTeam && orderManager.LobbyInfo.ClientInSlot(s.Key) != null) + 1) / 2;
					if (teamCount >= 1)
					{
						var teamOptions = Enumerable.Range(2, teamCount - 1).Reverse().Select(d => new DropDownOption
						{
							Title = "{0} Teams".F(d),
							IsSelected = () => false,
							OnClick = () => orderManager.IssueOrder(Order.Command("assignteams {0}".F(d.ToString())))
						}).ToList();

						if (orderManager.LobbyInfo.Slots.Any(s => s.Value.AllowBots))
						{
							teamOptions.Add(new DropDownOption
							{
								Title = "Humans vs Bots",
								IsSelected = () => false,
								OnClick = () => orderManager.IssueOrder(Order.Command("assignteams 1"))
							});
						}

						teamOptions.Add(new DropDownOption
						{
							Title = "Free for all",
							IsSelected = () => false,
							OnClick = () => orderManager.IssueOrder(Order.Command("assignteams 0"))
						});

						options.Add("Configure Teams", teamOptions);
					}

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
					slotsButton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 175, options, setupItem);
				};
			}

			var optionsBin = Ui.LoadWidget("LOBBY_OPTIONS_BIN", lobby, new WidgetArgs());
			optionsBin.IsVisible = () => panel == PanelType.Options;

			var optionsButton = lobby.Get<ButtonWidget>("OPTIONS_BUTTON");
			optionsButton.IsDisabled = () => panel == PanelType.Kick || panel == PanelType.MapDownload;
			optionsButton.GetText = () => panel == PanelType.Options ? "Players" : "Options";
			optionsButton.OnClick = () => panel = (panel == PanelType.Options) ? PanelType.Players : PanelType.Options;

			var startGameButton = lobby.GetOrNull<ButtonWidget>("START_GAME_BUTTON");
			if (startGameButton != null)
			{
				startGameButton.IsDisabled = () => configurationDisabled() ||
					orderManager.LobbyInfo.Slots.Any(sl => sl.Value.Required && orderManager.LobbyInfo.ClientInSlot(sl.Key) == null);
				startGameButton.OnClick = () =>
				{
					gameStarting = true;
					orderManager.IssueOrder(Order.Command("startgame"));
				};
			}

			var statusCheckbox = lobby.GetOrNull<CheckboxWidget>("STATUS_CHECKBOX");
			if (statusCheckbox != null)
			{
				statusCheckbox.IsHighlighted = () => !statusCheckbox.IsChecked() &&
					orderManager.LobbyInfo.FirstEmptySlot() == null && 
					world.FrameNumber / 25 % 2 == 0;
			}

			// Options panel
			var allowCheats = optionsBin.GetOrNull<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			if (allowCheats != null)
			{
				allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
				allowCheats.IsDisabled = () => map.Options.Cheats.HasValue || configurationDisabled();
				allowCheats.OnClick = () =>	orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));
			}

			var crates = optionsBin.GetOrNull<CheckboxWidget>("CRATES_CHECKBOX");
			if (crates != null)
			{
				crates.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Crates;
				crates.IsDisabled = () => map.Options.Crates.HasValue || configurationDisabled();
				crates.OnClick = () => orderManager.IssueOrder(Order.Command(
					"crates {0}".F(!orderManager.LobbyInfo.GlobalSettings.Crates)));
			}

			var allybuildradius = optionsBin.GetOrNull<CheckboxWidget>("ALLYBUILDRADIUS_CHECKBOX");
			if (allybuildradius != null)
			{
				allybuildradius.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius;
				allybuildradius.IsDisabled = () => map.Options.AllyBuildRadius.HasValue || configurationDisabled();
				allybuildradius.OnClick = () => orderManager.IssueOrder(Order.Command(
					"allybuildradius {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius)));
			}

			var fragileAlliance = optionsBin.GetOrNull<CheckboxWidget>("FRAGILEALLIANCES_CHECKBOX");
			if (fragileAlliance != null)
			{
				fragileAlliance.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.FragileAlliances;
				fragileAlliance.IsDisabled = () => map.Options.FragileAlliances.HasValue || configurationDisabled();
				fragileAlliance.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fragilealliance {0}".F(!orderManager.LobbyInfo.GlobalSettings.FragileAlliances)));
			}

			var difficulty = optionsBin.GetOrNull<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			if (difficulty != null)
			{
				difficulty.IsVisible = () => map.Options.Difficulties.Any();
				difficulty.IsDisabled = configurationDisabled;
				difficulty.GetText = () => orderManager.LobbyInfo.GlobalSettings.Difficulty;
				difficulty.OnMouseDown = _ =>
				{
					var options = map.Options.Difficulties.Select(d => new DropDownOption
					{
						Title = d,
						IsSelected = () => orderManager.LobbyInfo.GlobalSettings.Difficulty == d,
						OnClick = () => orderManager.IssueOrder(Order.Command("difficulty {0}".F(d)))
					});
					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
					difficulty.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};

				optionsBin.Get<LabelWidget>("DIFFICULTY_DESC").IsVisible = difficulty.IsVisible;
			}

			var startingUnits = optionsBin.GetOrNull<DropDownButtonWidget>("STARTINGUNITS_DROPDOWNBUTTON");
			if (startingUnits != null)
			{
				var classNames = new Dictionary<string,string>()
				{
					{"none", "MCV Only"},
					{"light", "Light Support"},
					{"heavy", "Heavy Support"},
				};

				Func<string, string> className = c => classNames.ContainsKey(c) ? classNames[c] : c;
				var classes = Rules.Info["world"].Traits.WithInterface<MPStartUnitsInfo>()
					.Select(a => a.Class).Distinct();

				startingUnits.IsDisabled =  () => !map.Options.ConfigurableStartingUnits || configurationDisabled();
				startingUnits.GetText = () => !map.Options.ConfigurableStartingUnits ? "Not Available" : className(orderManager.LobbyInfo.GlobalSettings.StartingUnitsClass);
				startingUnits.OnMouseDown = _ =>
				{
					var options = classes.Select(c => new DropDownOption
					{
						Title = className(c),
						IsSelected = () => orderManager.LobbyInfo.GlobalSettings.StartingUnitsClass == c,
						OnClick = () => orderManager.IssueOrder(Order.Command("startingunits {0}".F(c)))
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					startingUnits.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};

				optionsBin.Get<LabelWidget>("STARTINGUNITS_DESC").IsVisible = startingUnits.IsVisible;
			}

			var startingCash = optionsBin.GetOrNull<DropDownButtonWidget>("STARTINGCASH_DROPDOWNBUTTON");
			if (startingCash != null)
			{
				startingCash.IsDisabled = () => map.Options.StartingCash.HasValue || configurationDisabled();
				startingCash.GetText = () => map.Options.StartingCash.HasValue ? "Not Available" : "${0}".F(orderManager.LobbyInfo.GlobalSettings.StartingCash);
				startingCash.OnMouseDown = _ =>
				{
					var options = Rules.Info["player"].Traits.Get<PlayerResourcesInfo>().SelectableCash.Select(c => new DropDownOption
					{
						Title = "${0}".F(c),
						IsSelected = () => orderManager.LobbyInfo.GlobalSettings.StartingCash == c,
						OnClick = () => orderManager.IssueOrder(Order.Command("startingcash {0}".F(c)))
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					startingCash.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			var enableShroud = optionsBin.GetOrNull<CheckboxWidget>("SHROUD_CHECKBOX");
			if (enableShroud != null)
			{
				enableShroud.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Shroud;
				enableShroud.IsDisabled = () => map.Options.Shroud.HasValue || configurationDisabled();
				enableShroud.OnClick = () => orderManager.IssueOrder(Order.Command(
					"shroud {0}".F(!orderManager.LobbyInfo.GlobalSettings.Shroud)));
			}

			var enableFog = optionsBin.GetOrNull<CheckboxWidget>("FOG_CHECKBOX");
			if (enableFog != null)
			{
				enableFog.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Fog;
				enableFog.IsDisabled = () => map.Options.Fog.HasValue || configurationDisabled();
				enableFog.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fog {0}".F(!orderManager.LobbyInfo.GlobalSettings.Fog)));
			}

			var disconnectButton = lobby.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { CloseWindow(); onExit(); };

			bool teamChat = false;
			var chatLabel = lobby.Get<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.Get<TextFieldWidget>("CHAT_TEXTFIELD");

			chatTextField.OnEnterKey = () =>
			{
				if (chatTextField.Text.Length == 0)
					return true;

				orderManager.IssueOrder(Order.Chat(teamChat, chatTextField.Text));
				chatTextField.Text = "";
				return true;
			};

			chatTextField.OnTabKey = () =>
			{
				teamChat ^= true;
				chatLabel.Text = (teamChat) ? "Team:" : "Chat:";
				return true;
			};

			chatPanel = lobby.Get<ScrollPanelWidget>("CHAT_DISPLAY");
			chatTemplate = chatPanel.Get("CHAT_TEMPLATE");
			chatPanel.RemoveChildren();

			var musicButton = lobby.GetOrNull<ButtonWidget>("MUSIC_BUTTON");
			if (musicButton != null)
				musicButton.OnClick = () => Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
					{ { "onExit", () => {} } });

			// Add a bot on the first lobbyinfo update
			if (this.skirmishMode)
				Game.LobbyInfoChanged += WidgetUtils.Once(() =>
				{
					var slot = orderManager.LobbyInfo.FirstEmptySlot();
					var bot = Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name).FirstOrDefault();
					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					if (slot != null && bot != null)
						orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot, botController.Index, bot)));
				});
		}

		void AddChatLine(Color c, string from, string text)
		{
			var template = chatTemplate.Clone();
			var nameLabel = template.Get<LabelWidget>("NAME");
			var timeLabel = template.Get<LabelWidget>("TIME");
			var textLabel = template.Get<LabelWidget>("TEXT");

			var name = from + ":";
			var font = Game.Renderer.Fonts[nameLabel.Font];
			var nameSize = font.Measure(from);

			var time = DateTime.Now;
			timeLabel.GetText = () => "{0:D2}:{1:D2}".F(time.Hour, time.Minute);

			nameLabel.GetColor = () => c;
			nameLabel.GetText = () => name;
			nameLabel.Bounds.Width = nameSize.X;
			textLabel.Bounds.X += nameSize.X;
			textLabel.Bounds.Width -= nameSize.X;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			text = WidgetUtils.WrapText(text, textLabel.Bounds.Width, font);
			textLabel.GetText = () => text;
			var dh = font.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				template.Bounds.Height += dh;
			}

			chatPanel.AddChild(template);
			chatPanel.ScrollToBottom();
			Sound.PlayNotification(null, "Sounds", "ChatLine", null);
		}

		void UpdateCurrentMap()
		{
			if (mapUid == orderManager.LobbyInfo.GlobalSettings.Map)
				return;

			mapUid = orderManager.LobbyInfo.GlobalSettings.Map;

			if (Game.modData.AvailableMaps.ContainsKey(mapUid))
				LoadMap();
			else
				DownloadMap();
		}

		void DownloadMap()
		{
			if (Game.Settings.Game.AllowDownloading) // TODO: wire this up with GUI checkbox
			{
				panel = PanelType.MapDownload;

				statusLabel.GetText = () => "Initializing...";
				progressBar.SetIndeterminate(true);

				var mod = Game.modData.Manifest.Mod;
				var dirPath = new[] { Platform.SupportDir, "maps", mod.Id }.Aggregate(Path.Combine);
				if (!Directory.Exists(dirPath))
					Directory.CreateDirectory(dirPath);

				var mapRepository = Game.Settings.Game.MapRepositories.Skip(mapDownloadAttempts).First();

				try
				{
					mapDownloadAttempts++;
					var url = mapRepository + mapUid;

					var request = WebRequest.Create(url);
					request.Method = "HEAD";
					var res = request.GetResponse();

					// retry another mirror
					if (res.Headers["Content-Disposition"] == null)
						DownloadMap();

					var mapPath = Path.Combine(dirPath, res.Headers ["Content-Disposition"].Replace("attachment; filename = ", ""));
					Log.Write("server", "Trying to download map to '{0}' using {1}", mapPath, mapRepository);

					Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
					{
						if (progressBar.Indeterminate)
							progressBar.SetIndeterminate(false);

						progressBar.Percentage = i.ProgressPercentage;
						statusLabel.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived / 1024, i.TotalBytesToReceive / 1024);
					};

					Action<string> onError = s => Game.RunAfterTick(() => statusLabel.GetText = () => "Error: " + s);

					Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
					{
						if (i.Error != null)
						{
							onError(Download.FormatErrorMessage(i.Error));
							return;
						}
						else if (cancelled)
						{
							onError("Map download cancelled");
							return;
						}

						Game.Debug("A new map has been downloaded...");
						Log.Write("server", "New map has been downloaded to '{0}'", mapPath);
						Game.modData.AvailableMaps.Add(mapUid, new Map(mapPath));
						LoadMap();
					};

					new Download(url, mapPath, onDownloadProgress, onDownloadComplete);
				}
				catch (WebException e)
				{
					Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => e.Message;
					});
					Log.Write("server", "Error: Could not download map '{0}' using {1}".F(mapUid, mapRepository));
					if (mapDownloadAttempts < Game.Settings.Game.MapRepositories.Count())
						DownloadMap();
				}
			}
		}

		void LoadMap()
		{
			map = new Map(Game.modData.AvailableMaps[mapUid].Path);

			panel = PanelType.Players;
			Game.LobbyInfoChanged += UpdatePlayerList;

			// Restore default starting cash if the last map set it to something invalid
			var pri = Rules.Info["player"].Traits.Get<PlayerResourcesInfo>();
			if (!map.Options.StartingCash.HasValue && !pri.SelectableCash.Contains(orderManager.LobbyInfo.GlobalSettings.StartingCash))
				orderManager.IssueOrder(Order.Command("startingcash {0}".F(pri.DefaultCash)));
		}

		void UpdatePlayerList()
		{
			var idx = 0;
			foreach (var kv in orderManager.LobbyInfo.Slots)
			{
				var key = kv.Key;
				var slot = kv.Value;
				var client = orderManager.LobbyInfo.ClientInSlot(key);
				Widget template = null;

				// get template for possible reuse
				if (idx < players.Children.Count)
					template = players.Children[idx];

				// Empty slot
				if (client == null)
				{
					if (template == null || template.Id != emptySlotTemplate.Id)
						template = emptySlotTemplate.Clone();

					if (Game.IsHost)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager);
					else
						LobbyUtils.SetupSlotWidget(template, slot, client);

					var join = template.Get<ButtonWidget>("JOIN");
					join.IsVisible = () => !slot.Closed;
					join.IsDisabled = () => orderManager.LocalClient.IsReady;
					join.OnClick = () => orderManager.IssueOrder(Order.Command("slot " + key));
				}

				// Editable player in slot
				else if ((client.Index == orderManager.LocalClient.Index) ||
						 (client.Bot != null && Game.IsHost))
				{
					if (template == null || template.Id != editablePlayerTemplate.Id)
						template = editablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);

					if (client.Bot != null)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager);
					else
						LobbyUtils.SetupEditableNameWidget(template, slot, client, orderManager);

					LobbyUtils.SetupEditableColorWidget(template, slot, client, orderManager, colorPreview);
					LobbyUtils.SetupEditableFactionWidget(template, slot, client, orderManager, countryNames);
					LobbyUtils.SetupEditableTeamWidget(template, slot, client, orderManager, map.GetSpawnPoints().Length);
					LobbyUtils.SetupEditableReadyWidget(template, slot, client, orderManager);
				}
				else
				{	// Non-editable player in slot
					if (template == null || template.Id != nonEditablePlayerTemplate.Id)
						template = nonEditablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);
					LobbyUtils.SetupNameWidget(template, slot, client);
					LobbyUtils.SetupKickWidget(template, slot, client, orderManager, lobby,
						() => panel = PanelType.Kick, () => panel = PanelType.Players);
					LobbyUtils.SetupColorWidget(template, slot, client);
					LobbyUtils.SetupFactionWidget(template, slot, client, countryNames);
					LobbyUtils.SetupTeamWidget(template, slot, client);
					LobbyUtils.SetupReadyWidget(template, slot, client);
				}

				template.IsVisible = () => true;

				if (idx >= players.Children.Count)
					players.AddChild(template);
				else if (players.Children[idx].Id != template.Id)
					players.ReplaceChild(players.Children[idx], template);

				idx++;
			}

			// Add spectators
			foreach (var client in orderManager.LobbyInfo.Clients.Where(client => client.Slot == null))
			{
				Widget template = null;
				var c = client;

				// get template for possible reuse
				if (idx < players.Children.Count)
					template = players.Children[idx];

				// Editable spectator
				if (c.Index == orderManager.LocalClient.Index)
				{
					if (template == null || template.Id != editableSpectatorTemplate.Id)
						template = editableSpectatorTemplate.Clone();

					LobbyUtils.SetupEditableNameWidget(template, null, c, orderManager);
				}
				// Non-editable spectator
				else
				{
					if (template == null || template.Id != nonEditableSpectatorTemplate.Id)
						template = nonEditableSpectatorTemplate.Clone();

					LobbyUtils.SetupNameWidget(template, null, client);
					LobbyUtils.SetupKickWidget(template, null, client, orderManager, lobby,
						() => panel = PanelType.Kick, () => panel = PanelType.Players);
				}

				LobbyUtils.SetupClientWidget(template, null, c, orderManager, true);
				template.IsVisible = () => true;

				if (idx >= players.Children.Count)
					players.AddChild(template);
				else if (players.Children[idx].Id != template.Id)
					players.ReplaceChild(players.Children[idx], template);

				idx++;
			}

			// Spectate button
			if (orderManager.LocalClient.Slot != null)
			{
				Widget spec = null;
				if (idx < players.Children.Count)
					spec = players.Children[idx];
				if (spec == null || spec.Id != newSpectatorTemplate.Id)
					spec = newSpectatorTemplate.Clone();

				LobbyUtils.SetupKickSpectatorsWidget(spec, orderManager, lobby,
					() => panel = PanelType.Kick, () => panel = PanelType.Players, this.skirmishMode);
					
				var btn = spec.Get<ButtonWidget>("SPECTATE");
				btn.OnClick = () => orderManager.IssueOrder(Order.Command("spectate"));
				btn.IsDisabled = () => orderManager.LocalClient.IsReady;
				btn.IsVisible = () => orderManager.LobbyInfo.GlobalSettings.AllowSpectators
					|| orderManager.LocalClient.IsAdmin;

				spec.IsVisible = () => true;

				if (idx >= players.Children.Count)
					players.AddChild(spec);
				else if (players.Children[idx].Id != spec.Id)
					players.ReplaceChild(players.Children[idx], spec);

				idx++;
			}

			while (players.Children.Count > idx)
				players.RemoveChild(players.Children[idx]);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
