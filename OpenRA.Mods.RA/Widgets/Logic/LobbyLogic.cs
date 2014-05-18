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
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyLogic
	{
		static readonly Action DoNothing = () => { };

		public MapPreview Map = MapCache.UnknownMap;

		readonly Action onStart;
		readonly Action onExit;
		readonly OrderManager orderManager;
		readonly bool skirmishMode;
		readonly Ruleset modRules;

		enum PanelType { Players, Options, Kick, ForceStart }
		PanelType panel = PanelType.Players;

		Widget lobby;

		Widget editablePlayerTemplate, nonEditablePlayerTemplate, emptySlotTemplate,
			editableSpectatorTemplate, nonEditableSpectatorTemplate, newSpectatorTemplate;

		ScrollPanelWidget chatPanel;
		Widget chatTemplate;

		ScrollPanelWidget players;
		Dictionary<string, string> countryNames;

		ColorPreviewManagerWidget colorPreview;

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
						{ "onStart", onStart },
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
		internal LobbyLogic(Widget widget, WorldRenderer worldRenderer, OrderManager orderManager,
			Action onExit, Action onStart, bool skirmishMode, Ruleset modRules)
		{
			lobby = widget;
			this.orderManager = orderManager;
			this.onStart = onStart;
			this.onExit = onExit;
			this.skirmishMode = skirmishMode;
			this.modRules = modRules;

			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.LobbyInfoChanged += UpdatePlayerList;
			Game.BeforeGameStart += OnGameStart;
			Game.AddChatLine += AddChatLine;
			Game.ConnectionStateChanged += ConnectionStateChanged;

			var name = lobby.GetOrNull<LabelWidget>("SERVER_NAME");
			if (name != null)
				name.GetText = () => orderManager.LobbyInfo.GlobalSettings.ServerName;

			Ui.LoadWidget("LOBBY_MAP_PREVIEW", lobby.Get("MAP_PREVIEW_ROOT"), new WidgetArgs
			{
				{ "orderManager", orderManager },
				{ "lobby", this }
			});

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

			countryNames = modRules.Actors["world"].Traits.WithInterface<CountryInfo>()
				.Where(c => c.Selectable)
				.ToDictionary(a => a.Race, a => a.Name);
			countryNames.Add("random", "Any");

			var gameStarting = false;
			Func<bool> configurationDisabled = () => !Game.IsHost || gameStarting ||
				panel == PanelType.Kick || panel == PanelType.ForceStart ||
				orderManager.LocalClient == null || orderManager.LocalClient.IsReady;

			var mapButton = lobby.GetOrNull<ButtonWidget>("CHANGEMAP_BUTTON");
			if (mapButton != null)
			{
				mapButton.IsDisabled = configurationDisabled; 
				mapButton.OnClick = () =>
				{
					var onSelect = new Action<string>(uid =>
					{
						orderManager.IssueOrder(Order.Command("map " + uid));
						Game.Settings.Server.Map = uid;
						Game.Settings.Save();
					});

					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", Map.Uid },
						{ "onExit", DoNothing },
						{ "onSelect", onSelect }
					});
				};
			}

			var slotsButton = lobby.GetOrNull<DropDownButtonWidget>("SLOTS_DROPDOWNBUTTON");
			if (slotsButton != null)
			{
				slotsButton.IsDisabled = () => configurationDisabled() || panel != PanelType.Players ||
					Map.RuleStatus != MapRuleStatus.Cached || !orderManager.LobbyInfo.Slots.Values.Any(s => s.AllowBots || !s.LockTeam);

				var botNames = modRules.Actors["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name);
				slotsButton.OnMouseDown = _ =>
				{
					var options = new Dictionary<string, IEnumerable<DropDownOption>>();

					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					if (orderManager.LobbyInfo.Slots.Values.Any(s => s.AllowBots))
					{
						var botOptions = new List<DropDownOption>()
						{
							new DropDownOption()
							{
								Title = "Add",
								IsSelected = () => false,
								OnClick = () =>
								{
									foreach (var slot in orderManager.LobbyInfo.Slots)
									{
										var bot = botNames.Random(Game.CosmeticRandom);
										var c = orderManager.LobbyInfo.ClientInSlot(slot.Key);
										if (slot.Value.AllowBots == true && (c == null || c.Bot != null))
											orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot.Key, botController.Index, bot)));
									}
								}
							}
						};

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
											orderManager.IssueOrder(Order.Command("slot_open " + slot.Value.PlayerReference));
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
			optionsButton.IsDisabled = () => Map.RuleStatus != MapRuleStatus.Cached || panel == PanelType.Kick || panel == PanelType.ForceStart;
			optionsButton.GetText = () => panel == PanelType.Options ? "Players" : "Options";
			optionsButton.OnClick = () => panel = (panel == PanelType.Options) ? PanelType.Players : PanelType.Options;

			// Force start panel
			Action startGame = () =>
			{
				gameStarting = true;
				orderManager.IssueOrder(Order.Command("startgame"));
			};

			var startGameButton = lobby.GetOrNull<ButtonWidget>("START_GAME_BUTTON");
			if (startGameButton != null)
			{
				startGameButton.IsDisabled = () => configurationDisabled() || Map.RuleStatus != MapRuleStatus.Cached ||
					orderManager.LobbyInfo.Slots.Any(sl => sl.Value.Required && orderManager.LobbyInfo.ClientInSlot(sl.Key) == null);
				startGameButton.OnClick = () =>
				{
					Func<KeyValuePair<string, Session.Slot>, bool> notReady = sl =>
					{
						var cl = orderManager.LobbyInfo.ClientInSlot(sl.Key);

						// Bots and admins don't count
						return cl != null && !cl.IsAdmin && cl.Bot == null && !cl.IsReady;
					};

					if (orderManager.LobbyInfo.Slots.Any(notReady))
						panel = PanelType.ForceStart;
					else
						startGame();
				};
			}

			var forceStartBin = Ui.LoadWidget("FORCE_START_DIALOG", lobby, new WidgetArgs());
			forceStartBin.IsVisible = () => panel == PanelType.ForceStart;
			forceStartBin.Get("KICK_WARNING").IsVisible = () => orderManager.LobbyInfo.Clients.Any(c => c.IsInvalid);
			forceStartBin.Get<ButtonWidget>("OK_BUTTON").OnClick = startGame;
			forceStartBin.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => panel = PanelType.Players;

			// Options panel
			var allowCheats = optionsBin.GetOrNull<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			if (allowCheats != null)
			{
				allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
				allowCheats.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.Cheats.HasValue || configurationDisabled();
				allowCheats.OnClick = () =>	orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));
			}

			var crates = optionsBin.GetOrNull<CheckboxWidget>("CRATES_CHECKBOX");
			if (crates != null)
			{
				crates.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Crates;
				crates.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.Crates.HasValue || configurationDisabled();
				crates.OnClick = () => orderManager.IssueOrder(Order.Command(
					"crates {0}".F(!orderManager.LobbyInfo.GlobalSettings.Crates)));
			}

			var allybuildradius = optionsBin.GetOrNull<CheckboxWidget>("ALLYBUILDRADIUS_CHECKBOX");
			if (allybuildradius != null)
			{
				allybuildradius.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius;
				allybuildradius.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.AllyBuildRadius.HasValue || configurationDisabled();
				allybuildradius.OnClick = () => orderManager.IssueOrder(Order.Command(
					"allybuildradius {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius)));
			}

			var fragileAlliance = optionsBin.GetOrNull<CheckboxWidget>("FRAGILEALLIANCES_CHECKBOX");
			if (fragileAlliance != null)
			{
				fragileAlliance.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.FragileAlliances;
				fragileAlliance.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.FragileAlliances.HasValue || configurationDisabled();
				fragileAlliance.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fragilealliance {0}".F(!orderManager.LobbyInfo.GlobalSettings.FragileAlliances)));
			}

			var difficulty = optionsBin.GetOrNull<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			if (difficulty != null)
			{
				difficulty.IsVisible = () => Map.Status == MapStatus.Available && Map.Map.Options.Difficulties.Any();
				difficulty.IsDisabled = () => Map.Status != MapStatus.Available || configurationDisabled();
				difficulty.GetText = () => orderManager.LobbyInfo.GlobalSettings.Difficulty;
				difficulty.OnMouseDown = _ =>
				{
					var options = Map.Map.Options.Difficulties.Select(d => new DropDownOption
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
				var classNames = new Dictionary<string, string>()
				{
					{ "none", "MCV Only" },
					{ "light", "Light Support" },
					{ "heavy", "Heavy Support" },
				};

				Func<string, string> className = c => classNames.ContainsKey(c) ? classNames[c] : c;
				var classes = modRules.Actors["world"].Traits.WithInterface<MPStartUnitsInfo>()
					.Select(a => a.Class).Distinct();

				startingUnits.IsDisabled = () => Map.Status != MapStatus.Available || !Map.Map.Options.ConfigurableStartingUnits || configurationDisabled();
				startingUnits.GetText = () => Map.Status != MapStatus.Available || !Map.Map.Options.ConfigurableStartingUnits ? "Not Available" : className(orderManager.LobbyInfo.GlobalSettings.StartingUnitsClass);
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
				startingCash.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.StartingCash.HasValue || configurationDisabled();
				startingCash.GetText = () => Map.Status != MapStatus.Available || Map.Map.Options.StartingCash.HasValue ? "Not Available" : "${0}".F(orderManager.LobbyInfo.GlobalSettings.StartingCash);
				startingCash.OnMouseDown = _ =>
				{
					var options = modRules.Actors["player"].Traits.Get<PlayerResourcesInfo>().SelectableCash.Select(c => new DropDownOption
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
				enableShroud.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.Shroud.HasValue || configurationDisabled();
				enableShroud.OnClick = () => orderManager.IssueOrder(Order.Command(
					"shroud {0}".F(!orderManager.LobbyInfo.GlobalSettings.Shroud)));
			}

			var enableFog = optionsBin.GetOrNull<CheckboxWidget>("FOG_CHECKBOX");
			if (enableFog != null)
			{
				enableFog.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Fog;
				enableFog.IsDisabled = () => Map.Status != MapStatus.Available || Map.Map.Options.Fog.HasValue || configurationDisabled();
				enableFog.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fog {0}".F(!orderManager.LobbyInfo.GlobalSettings.Fog)));
			}

			var disconnectButton = lobby.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { CloseWindow(); onExit(); };

			if (skirmishMode)
				disconnectButton.Text = "Cancel";

			bool teamChat = false;
			var chatLabel = lobby.Get<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.Get<TextFieldWidget>("CHAT_TEXTFIELD");

			chatTextField.OnEnterKey = () =>
			{
				if (chatTextField.Text.Length == 0)
					return true;

				// Always scroll to bottom when we've typed something
				chatPanel.ScrollToBottom();

				orderManager.IssueOrder(Order.Chat(teamChat, chatTextField.Text));
				chatTextField.Text = "";
				return true;
			};

			chatTextField.OnTabKey = () =>
			{
				teamChat ^= true;
				chatLabel.Text = teamChat ? "Team:" : "Chat:";
				return true;
			};

			chatPanel = lobby.Get<ScrollPanelWidget>("CHAT_DISPLAY");
			chatTemplate = chatPanel.Get("CHAT_TEMPLATE");
			chatPanel.RemoveChildren();

			var musicButton = lobby.GetOrNull<ButtonWidget>("MUSIC_BUTTON");
			if (musicButton != null)
				musicButton.OnClick = () => Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs { { "onExit", DoNothing } });

			var settingsButton = lobby.GetOrNull<ButtonWidget>("SETTINGS_BUTTON");
			if (settingsButton != null)
			{
				settingsButton.OnClick = () => Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs
				{
					{ "onExit", DoNothing },
					{ "worldRenderer", worldRenderer }
				});
			}

			// Add a bot on the first lobbyinfo update
			if (skirmishMode)
			{
				Game.LobbyInfoChanged += WidgetUtils.Once(() =>
				{
					var slot = orderManager.LobbyInfo.FirstEmptyBotSlot();
					var bot = modRules.Actors["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name).FirstOrDefault();
					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					if (slot != null && bot != null)
						orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot, botController.Index, bot)));
				});
			}
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

			var scrolledToBottom = chatPanel.ScrolledToBottom;
			chatPanel.AddChild(template);
			if (scrolledToBottom)
				chatPanel.ScrollToBottom();

			Sound.PlayNotification(modRules, null, "Sounds", "ChatLine", null);
		}

		void UpdateCurrentMap()
		{
			var uid = orderManager.LobbyInfo.GlobalSettings.Map;
			if (Map.Uid == uid)
				return;

			Map = Game.modData.MapCache[uid];
			if (Map.Status == MapStatus.Available)
			{
				// Maps need to be validated and pre-loaded before they can be accessed
				new Thread(_ => 
				{
					var map = Map;
					map.CacheRules();
					Game.RunAfterTick(() =>
					{
						// Map may have changed in the meantime
						if (map != Map)
							return;

						if (map.RuleStatus != MapRuleStatus.Invalid)
						{
							// Tell the server that we have the map
							orderManager.IssueOrder(Order.Command("state {0}".F(Session.ClientState.NotReady)));

							// Restore default starting cash if the last map set it to something invalid
							var pri = modRules.Actors["player"].Traits.Get<PlayerResourcesInfo>();
							if (!Map.Map.Options.StartingCash.HasValue && !pri.SelectableCash.Contains(orderManager.LobbyInfo.GlobalSettings.StartingCash))
								orderManager.IssueOrder(Order.Command("startingcash {0}".F(pri.DefaultCash)));
						}
					});
				}).Start();
			}
			else if (Game.Settings.Game.AllowDownloading)
				Game.modData.MapCache.QueryRemoteMapDetails(new [] { uid });
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

				if (client == null)
				{
					// Empty slot
					if (template == null || template.Id != emptySlotTemplate.Id)
						template = emptySlotTemplate.Clone();

					if (Game.IsHost)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager, modRules);
					else
						LobbyUtils.SetupSlotWidget(template, slot, client);

					var join = template.Get<ButtonWidget>("JOIN");
					join.IsVisible = () => !slot.Closed;
					join.IsDisabled = () => orderManager.LocalClient.IsReady;
					join.OnClick = () => orderManager.IssueOrder(Order.Command("slot " + key));
				}
				else if ((client.Index == orderManager.LocalClient.Index) ||
						 (client.Bot != null && Game.IsHost))
				{
					// Editable player in slot
					if (template == null || template.Id != editablePlayerTemplate.Id)
						template = editablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);

					if (client.Bot != null)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager, modRules);
					else
						LobbyUtils.SetupEditableNameWidget(template, slot, client, orderManager);

					LobbyUtils.SetupEditableColorWidget(template, slot, client, orderManager, colorPreview);
					LobbyUtils.SetupEditableFactionWidget(template, slot, client, orderManager, countryNames);
					LobbyUtils.SetupEditableTeamWidget(template, slot, client, orderManager, Map);
					LobbyUtils.SetupEditableSpawnWidget(template, slot, client, orderManager, Map);
					LobbyUtils.SetupEditableReadyWidget(template, slot, client, orderManager, Map);
				}
				else
				{
					// Non-editable player in slot
					if (template == null || template.Id != nonEditablePlayerTemplate.Id)
						template = nonEditablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);
					LobbyUtils.SetupNameWidget(template, slot, client);
					LobbyUtils.SetupKickWidget(template, slot, client, orderManager, lobby,
						() => panel = PanelType.Kick, () => panel = PanelType.Players);
					LobbyUtils.SetupColorWidget(template, slot, client);
					LobbyUtils.SetupFactionWidget(template, slot, client, countryNames);
					LobbyUtils.SetupTeamWidget(template, slot, client);
					LobbyUtils.SetupSpawnWidget(template, slot, client);
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
				else
				{
					// Non-editable spectator
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
					() => panel = PanelType.Kick, () => panel = PanelType.Players, skirmishMode);

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

		void OnGameStart()
		{
			CloseWindow();
			onStart();
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
