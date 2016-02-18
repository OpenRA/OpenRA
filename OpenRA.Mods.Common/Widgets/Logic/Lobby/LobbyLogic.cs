#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Chat;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LobbyLogic : ChromeLogic
	{
		static readonly Action DoNothing = () => { };

		public MapPreview MapPreview { get; private set; }
		public Map Map { get; private set; }

		readonly Action onStart;
		readonly Action onExit;
		readonly OrderManager orderManager;
		readonly bool skirmishMode;
		readonly Ruleset modRules;
		readonly World shellmapWorld;

		enum PanelType { Players, Options, Music, Kick, ForceStart }
		PanelType panel = PanelType.Players;

		enum ChatPanelType { Lobby, Global }
		ChatPanelType chatPanel = ChatPanelType.Lobby;

		readonly Widget lobby;
		readonly Widget editablePlayerTemplate;
		readonly Widget nonEditablePlayerTemplate;
		readonly Widget emptySlotTemplate;
		readonly Widget editableSpectatorTemplate;
		readonly Widget nonEditableSpectatorTemplate;
		readonly Widget newSpectatorTemplate;

		readonly ScrollPanelWidget lobbyChatPanel;
		readonly Widget chatTemplate;

		readonly ScrollPanelWidget players;

		readonly Dictionary<string, LobbyFaction> factions = new Dictionary<string, LobbyFaction>();

		readonly ColorPreviewManagerWidget colorPreview;

		readonly TabCompletionLogic tabCompletion = new TabCompletionLogic();

		readonly LabelWidget chatLabel;
		bool teamChat;

		int lobbyChatUnreadMessages;
		int globalChatLastReadMessages;
		int globalChatUnreadMessages;

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

				Action<string> onRetry = password => ConnectionLogic.Connect(om.Host, om.Port, password, onConnect, onExit);

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
			orderManager.AddChatLine -= AddChatLine;
			Game.LobbyInfoChanged -= UpdateCurrentMap;
			Game.LobbyInfoChanged -= UpdatePlayerList;
			Game.BeforeGameStart -= OnGameStart;
			Game.ConnectionStateChanged -= ConnectionStateChanged;

			Ui.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		internal LobbyLogic(Widget widget, WorldRenderer worldRenderer, OrderManager orderManager,
			Action onExit, Action onStart, bool skirmishMode, Ruleset modRules)
		{
			MapPreview = MapCache.UnknownMap;
			lobby = widget;
			this.orderManager = orderManager;
			this.onStart = onStart;
			this.onExit = onExit;
			this.skirmishMode = skirmishMode;
			this.modRules = modRules;
			shellmapWorld = worldRenderer.World;

			orderManager.AddChatLine += AddChatLine;
			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.LobbyInfoChanged += UpdatePlayerList;
			Game.BeforeGameStart += OnGameStart;
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

			var playerBin = Ui.LoadWidget("LOBBY_PLAYER_BIN", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs());
			playerBin.IsVisible = () => panel == PanelType.Players;

			players = playerBin.Get<ScrollPanelWidget>("LOBBY_PLAYERS");
			editablePlayerTemplate = players.Get("TEMPLATE_EDITABLE_PLAYER");
			nonEditablePlayerTemplate = players.Get("TEMPLATE_NONEDITABLE_PLAYER");
			emptySlotTemplate = players.Get("TEMPLATE_EMPTY");
			editableSpectatorTemplate = players.Get("TEMPLATE_EDITABLE_SPECTATOR");
			nonEditableSpectatorTemplate = players.Get("TEMPLATE_NONEDITABLE_SPECTATOR");
			newSpectatorTemplate = players.Get("TEMPLATE_NEW_SPECTATOR");
			colorPreview = lobby.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = Game.Settings.Player.Color;

			foreach (var f in modRules.Actors["world"].TraitInfos<FactionInfo>())
				factions.Add(f.InternalName, new LobbyFaction { Selectable = f.Selectable, Name = f.Name, Side = f.Side, Description = f.Description });

			var gameStarting = false;
			Func<bool> configurationDisabled = () => !Game.IsHost || gameStarting ||
				panel == PanelType.Kick || panel == PanelType.ForceStart ||
				Map == null || Map.InvalidCustomRules ||
				orderManager.LocalClient == null || orderManager.LocalClient.IsReady;

			var mapButton = lobby.GetOrNull<ButtonWidget>("CHANGEMAP_BUTTON");
			if (mapButton != null)
			{
				mapButton.IsDisabled = () => gameStarting || panel == PanelType.Kick || panel == PanelType.ForceStart ||
					orderManager.LocalClient == null || orderManager.LocalClient.IsReady;
				mapButton.OnClick = () =>
				{
					var onSelect = new Action<string>(uid =>
					{
						// Don't select the same map again
						if (uid == MapPreview.Uid)
							return;

						orderManager.IssueOrder(Order.Command("map " + uid));
						Game.Settings.Server.Map = uid;
						Game.Settings.Save();
					});

					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", MapPreview.Uid },
						{ "initialTab", MapClassification.System },
						{ "onExit", DoNothing },
						{ "onSelect", Game.IsHost ? onSelect : null },
						{ "filter", MapVisibility.Lobby },
					});
				};
			}

			var slotsButton = lobby.GetOrNull<DropDownButtonWidget>("SLOTS_DROPDOWNBUTTON");
			if (slotsButton != null)
			{
				slotsButton.IsDisabled = () => configurationDisabled() || panel != PanelType.Players ||
					(orderManager.LobbyInfo.Slots.Values.All(s => !s.AllowBots) &&
					orderManager.LobbyInfo.Slots.Count(s => !s.Value.LockTeam && orderManager.LobbyInfo.ClientInSlot(s.Key) != null) == 0);

				var botNames = modRules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name);
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

			var optionsBin = Ui.LoadWidget("LOBBY_OPTIONS_BIN", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs());
			optionsBin.IsVisible = () => panel == PanelType.Options;

			var musicBin = Ui.LoadWidget("LOBBY_MUSIC_BIN", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs
			{
				{ "onExit", DoNothing },
				{ "world", worldRenderer.World }
			});
			musicBin.IsVisible = () => panel == PanelType.Music;

			var optionsTab = lobby.Get<ButtonWidget>("OPTIONS_TAB");
			optionsTab.IsHighlighted = () => panel == PanelType.Options;
			optionsTab.IsDisabled = () => Map == null || Map.InvalidCustomRules || panel == PanelType.Kick || panel == PanelType.ForceStart;
			optionsTab.OnClick = () => panel = PanelType.Options;

			var playersTab = lobby.Get<ButtonWidget>("PLAYERS_TAB");
			playersTab.IsHighlighted = () => panel == PanelType.Players;
			playersTab.IsDisabled = () => panel == PanelType.Kick || panel == PanelType.ForceStart;
			playersTab.OnClick = () => panel = PanelType.Players;

			var musicTab = lobby.Get<ButtonWidget>("MUSIC_TAB");
			musicTab.IsHighlighted = () => panel == PanelType.Music;
			musicTab.IsDisabled = () => panel == PanelType.Kick || panel == PanelType.ForceStart;
			musicTab.OnClick = () => panel = PanelType.Music;

			// Force start panel
			Action startGame = () =>
			{
				gameStarting = true;
				orderManager.IssueOrder(Order.Command("startgame"));
			};

			var startGameButton = lobby.GetOrNull<ButtonWidget>("START_GAME_BUTTON");
			if (startGameButton != null)
			{
				startGameButton.IsDisabled = () => configurationDisabled() ||
					orderManager.LobbyInfo.Slots.Any(sl => sl.Value.Required && orderManager.LobbyInfo.ClientInSlot(sl.Key) == null) ||
					(orderManager.LobbyInfo.GlobalSettings.DisableSingleplayer && orderManager.LobbyInfo.IsSinglePlayer);

				startGameButton.OnClick = () =>
				{
					// Bots and admins don't count
					if (orderManager.LobbyInfo.Clients.Any(c => c.Slot != null && !c.IsAdmin && c.Bot == null && !c.IsReady))
						panel = PanelType.ForceStart;
					else
						startGame();
				};
			}

			var forceStartBin = Ui.LoadWidget("FORCE_START_DIALOG", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs());
			forceStartBin.IsVisible = () => panel == PanelType.ForceStart;
			forceStartBin.Get("KICK_WARNING").IsVisible = () => orderManager.LobbyInfo.Clients.Any(c => c.IsInvalid);
			forceStartBin.Get<ButtonWidget>("OK_BUTTON").OnClick = startGame;
			forceStartBin.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => panel = PanelType.Players;

			// Options panel
			var allowCheats = optionsBin.GetOrNull<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			if (allowCheats != null)
			{
				allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
				allowCheats.IsDisabled = () => configurationDisabled() || Map.Options.Cheats.HasValue;
				allowCheats.OnClick = () => orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));
			}

			var crates = optionsBin.GetOrNull<CheckboxWidget>("CRATES_CHECKBOX");
			if (crates != null)
			{
				crates.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Crates;
				crates.IsDisabled = () => configurationDisabled() || Map.Options.Crates.HasValue;
				crates.OnClick = () => orderManager.IssueOrder(Order.Command(
					"crates {0}".F(!orderManager.LobbyInfo.GlobalSettings.Crates)));
			}

			var creeps = optionsBin.GetOrNull<CheckboxWidget>("CREEPS_CHECKBOX");
			if (creeps != null)
			{
				creeps.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Creeps;
				creeps.IsDisabled = () => configurationDisabled() || Map.Options.Creeps.HasValue;
				creeps.OnClick = () => orderManager.IssueOrder(Order.Command(
					"creeps {0}".F(!orderManager.LobbyInfo.GlobalSettings.Creeps)));
			}

			var allybuildradius = optionsBin.GetOrNull<CheckboxWidget>("ALLYBUILDRADIUS_CHECKBOX");
			if (allybuildradius != null)
			{
				allybuildradius.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius;
				allybuildradius.IsDisabled = () => configurationDisabled() || Map.Options.AllyBuildRadius.HasValue;
				allybuildradius.OnClick = () => orderManager.IssueOrder(Order.Command(
					"allybuildradius {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllyBuildRadius)));
			}

			var shortGame = optionsBin.GetOrNull<CheckboxWidget>("SHORTGAME_CHECKBOX");
			if (shortGame != null)
			{
				shortGame.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.ShortGame;
				shortGame.IsDisabled = () => configurationDisabled() || Map.Options.ShortGame.HasValue;
				shortGame.OnClick = () => orderManager.IssueOrder(Order.Command(
					"shortgame {0}".F(!orderManager.LobbyInfo.GlobalSettings.ShortGame)));
			}

			var difficulty = optionsBin.GetOrNull<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			if (difficulty != null)
			{
				difficulty.IsVisible = () => Map != null && Map.Options.Difficulties.Any();
				difficulty.IsDisabled = configurationDisabled;
				difficulty.GetText = () => orderManager.LobbyInfo.GlobalSettings.Difficulty;
				difficulty.OnMouseDown = _ =>
				{
					var options = Map.Options.Difficulties.Select(d => new DropDownOption
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
				var startUnitsInfo = modRules.Actors["world"].TraitInfos<MPStartUnitsInfo>();
				var classes = startUnitsInfo.Select(a => a.Class).Distinct();
				Func<string, string> className = c =>
				{
					var selectedClass = startUnitsInfo.Where(s => s.Class == c).Select(u => u.ClassName).FirstOrDefault();
					return selectedClass != null ? selectedClass : c;
				};

				startingUnits.IsDisabled = () => configurationDisabled() || !Map.Options.ConfigurableStartingUnits;
				startingUnits.GetText = () => MapPreview.Status != MapStatus.Available ||
					Map == null || !Map.Options.ConfigurableStartingUnits ? "Not Available" : className(orderManager.LobbyInfo.GlobalSettings.StartingUnitsClass);
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
				startingCash.IsDisabled = () => configurationDisabled() || Map.Options.StartingCash.HasValue;
				startingCash.GetText = () => MapPreview.Status != MapStatus.Available ||
					Map == null || Map.Options.StartingCash.HasValue ? "Not Available" : "${0}".F(orderManager.LobbyInfo.GlobalSettings.StartingCash);
				startingCash.OnMouseDown = _ =>
				{
					var options = modRules.Actors["player"].TraitInfo<PlayerResourcesInfo>().SelectableCash.Select(c => new DropDownOption
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

			var techLevel = optionsBin.GetOrNull<DropDownButtonWidget>("TECHLEVEL_DROPDOWNBUTTON");
			if (techLevel != null)
			{
				var techTraits = modRules.Actors["player"].TraitInfos<ProvidesTechPrerequisiteInfo>().ToList();
				techLevel.IsVisible = () => techTraits.Count > 0;

				var techLevelDescription = optionsBin.GetOrNull<LabelWidget>("TECHLEVEL_DESC");
				if (techLevelDescription != null)
					techLevelDescription.IsVisible = () => techTraits.Count > 0;

				techLevel.IsDisabled = () => configurationDisabled() || Map.Options.TechLevel != null || techTraits.Count <= 1;
				techLevel.GetText = () => MapPreview.Status != MapStatus.Available ||
					Map == null || Map.Options.TechLevel != null ? "Not Available" : "{0}".F(orderManager.LobbyInfo.GlobalSettings.TechLevel);
				techLevel.OnMouseDown = _ =>
				{
					var options = techTraits.Select(c => new DropDownOption
					{
						Title = "{0}".F(c.Name),
						IsSelected = () => orderManager.LobbyInfo.GlobalSettings.TechLevel == c.Name,
						OnClick = () => orderManager.IssueOrder(Order.Command("techlevel {0}".F(c.Name)))
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					techLevel.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			var gameSpeed = optionsBin.GetOrNull<DropDownButtonWidget>("GAMESPEED_DROPDOWNBUTTON");
			if (gameSpeed != null)
			{
				var speeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds;

				gameSpeed.IsDisabled = configurationDisabled;
				gameSpeed.GetText = () =>
				{
					if (MapPreview.Status != MapStatus.Available)
						return "Not Available";

					GameSpeed speed;
					if (!speeds.TryGetValue(orderManager.LobbyInfo.GlobalSettings.GameSpeedType, out speed))
						return "Unknown";

					return speed.Name;
				};

				gameSpeed.OnMouseDown = _ =>
				{
					var options = speeds.Select(s => new DropDownOption
					{
						Title = s.Value.Name,
						IsSelected = () => orderManager.LobbyInfo.GlobalSettings.GameSpeedType == s.Key,
						OnClick = () => orderManager.IssueOrder(Order.Command("gamespeed {0}".F(s.Key)))
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					gameSpeed.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			var exploredMap = optionsBin.GetOrNull<CheckboxWidget>("EXPLORED_MAP_CHECKBOX");
			if (exploredMap != null)
			{
				exploredMap.IsChecked = () => !orderManager.LobbyInfo.GlobalSettings.Shroud;
				exploredMap.IsDisabled = () => configurationDisabled() || Map.Options.Shroud.HasValue;
				exploredMap.OnClick = () => orderManager.IssueOrder(Order.Command(
					"shroud {0}".F(!orderManager.LobbyInfo.GlobalSettings.Shroud)));
			}

			var enableFog = optionsBin.GetOrNull<CheckboxWidget>("FOG_CHECKBOX");
			if (enableFog != null)
			{
				enableFog.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Fog;
				enableFog.IsDisabled = () => configurationDisabled() || Map.Options.Fog.HasValue;
				enableFog.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fog {0}".F(!orderManager.LobbyInfo.GlobalSettings.Fog)));
			}

			var disconnectButton = lobby.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { CloseWindow(); onExit(); };

			if (skirmishMode)
				disconnectButton.Text = "Back";

			var globalChat = Game.LoadWidget(null, "LOBBY_GLOBALCHAT_PANEL", lobby.Get("GLOBALCHAT_ROOT"), new WidgetArgs());
			var globalChatInput = globalChat.Get<TextFieldWidget>("CHAT_TEXTFIELD");

			globalChat.IsVisible = () => chatPanel == ChatPanelType.Global;

			var globalChatTab = lobby.Get<ButtonWidget>("GLOBALCHAT_TAB");
			globalChatTab.IsHighlighted = () => chatPanel == ChatPanelType.Global;
			globalChatTab.OnClick = () =>
			{
				chatPanel = ChatPanelType.Global;
				globalChatInput.TakeKeyboardFocus();
			};

			var globalChatLabel = globalChatTab.Text;
			globalChatTab.GetText = () =>
			{
				if (globalChatUnreadMessages == 0 || chatPanel == ChatPanelType.Global)
					return globalChatLabel;

				return globalChatLabel + " ({0})".F(globalChatUnreadMessages);
			};

			globalChatLastReadMessages = Game.GlobalChat.History.Count(m => m.Type == ChatMessageType.Message);

			var lobbyChat = lobby.Get("LOBBYCHAT");
			lobbyChat.IsVisible = () => chatPanel == ChatPanelType.Lobby;

			chatLabel = lobby.Get<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			chatTextField.TakeKeyboardFocus();
			chatTextField.OnEnterKey = () =>
			{
				if (chatTextField.Text.Length == 0)
					return true;

				// Always scroll to bottom when we've typed something
				lobbyChatPanel.ScrollToBottom();

				orderManager.IssueOrder(Order.Chat(teamChat, chatTextField.Text));
				chatTextField.Text = "";
				return true;
			};

			chatTextField.OnTabKey = () =>
			{
				var previousText = chatTextField.Text;
				chatTextField.Text = tabCompletion.Complete(chatTextField.Text);
				chatTextField.CursorPosition = chatTextField.Text.Length;

				if (chatTextField.Text == previousText)
					return SwitchTeamChat();
				else
					return true;
			};

			chatTextField.OnEscKey = () => { chatTextField.Text = ""; return true; };

			var lobbyChatTab = lobby.Get<ButtonWidget>("LOBBYCHAT_TAB");
			lobbyChatTab.IsHighlighted = () => chatPanel == ChatPanelType.Lobby;
			lobbyChatTab.OnClick = () =>
			{
				chatPanel = ChatPanelType.Lobby;
				chatTextField.TakeKeyboardFocus();
			};

			var lobbyChatLabel = lobbyChatTab.Text;
			lobbyChatTab.GetText = () =>
			{
				if (lobbyChatUnreadMessages == 0 || chatPanel == ChatPanelType.Lobby)
					return lobbyChatLabel;

				return lobbyChatLabel + " ({0})".F(lobbyChatUnreadMessages);
			};

			lobbyChatPanel = lobby.Get<ScrollPanelWidget>("CHAT_DISPLAY");
			chatTemplate = lobbyChatPanel.Get("CHAT_TEMPLATE");
			lobbyChatPanel.RemoveChildren();

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
					var bot = modRules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name).FirstOrDefault();
					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					if (slot != null && bot != null)
						orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot, botController.Index, bot)));
				});
			}
		}

		public override void Tick()
		{
			var newMessages = Game.GlobalChat.History.Count(m => m.Type == ChatMessageType.Message);
			globalChatUnreadMessages += newMessages - globalChatLastReadMessages;
			globalChatLastReadMessages = newMessages;

			if (chatPanel == ChatPanelType.Lobby)
				lobbyChatUnreadMessages = 0;

			if (chatPanel == ChatPanelType.Global)
				globalChatUnreadMessages = 0;
		}

		void AddChatLine(Color c, string from, string text)
		{
			lobbyChatUnreadMessages += 1;

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

			var scrolledToBottom = lobbyChatPanel.ScrolledToBottom;
			lobbyChatPanel.AddChild(template);
			if (scrolledToBottom)
				lobbyChatPanel.ScrollToBottom(smooth: true);

			Game.Sound.PlayNotification(modRules, null, "Sounds", "ChatLine", null);
		}

		bool SwitchTeamChat()
		{
			teamChat ^= true;
			chatLabel.Text = teamChat ? "Team:" : "Chat:";
			return true;
		}

		void UpdateCurrentMap()
		{
			var uid = orderManager.LobbyInfo.GlobalSettings.Map;
			if (MapPreview.Uid == uid)
				return;

			MapPreview = Game.ModData.MapCache[uid];
			Map = null;
			if (MapPreview.Status == MapStatus.Available)
			{
				// Maps need to be validated and pre-loaded before they can be accessed
				new Thread(_ =>
				{
					var currentMap = Map = new Map(MapPreview.Path);
					currentMap.PreloadRules();
					Game.RunAfterTick(() =>
					{
						// Map may have changed in the meantime
						if (currentMap != Map)
							return;

						if (!currentMap.InvalidCustomRules)
						{
							// Tell the server that we have the map
							orderManager.IssueOrder(Order.Command("state {0}".F(Session.ClientState.NotReady)));

							// Restore default starting cash if the last map set it to something invalid
							var pri = modRules.Actors["player"].TraitInfo<PlayerResourcesInfo>();
							if (!currentMap.Options.StartingCash.HasValue && !pri.SelectableCash.Contains(orderManager.LobbyInfo.GlobalSettings.StartingCash))
								orderManager.IssueOrder(Order.Command("startingcash {0}".F(pri.DefaultCash)));
						}
					});
				}).Start();
			}
			else if (Game.Settings.Game.AllowDownloading)
				Game.ModData.MapCache.QueryRemoteMapDetails(new[] { uid });
		}

		void UpdatePlayerList()
		{
			if (orderManager.LocalClient == null)
				return;

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

					LobbyUtils.SetupClientWidget(template, client, orderManager, client.Bot == null);

					if (client.Bot != null)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager, modRules);
					else
						LobbyUtils.SetupEditableNameWidget(template, slot, client, orderManager);

					LobbyUtils.SetupEditableColorWidget(template, slot, client, orderManager, shellmapWorld, colorPreview);
					LobbyUtils.SetupEditableFactionWidget(template, slot, client, orderManager, factions);
					LobbyUtils.SetupEditableTeamWidget(template, slot, client, orderManager, MapPreview);
					LobbyUtils.SetupEditableSpawnWidget(template, slot, client, orderManager, MapPreview);
					LobbyUtils.SetupEditableReadyWidget(template, slot, client, orderManager, MapPreview, Map == null || Map.InvalidCustomRules);
				}
				else
				{
					// Non-editable player in slot
					if (template == null || template.Id != nonEditablePlayerTemplate.Id)
						template = nonEditablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, client, orderManager, client.Bot == null);
					LobbyUtils.SetupNameWidget(template, slot, client);
					LobbyUtils.SetupKickWidget(template, slot, client, orderManager, lobby,
						() => panel = PanelType.Kick, () => panel = PanelType.Players);
					LobbyUtils.SetupColorWidget(template, slot, client);
					LobbyUtils.SetupFactionWidget(template, slot, client, factions);
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

				LobbyUtils.SetupClientWidget(template, c, orderManager, true);
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

			tabCompletion.Names = orderManager.LobbyInfo.Clients.Select(c => c.Name).Distinct().ToList();
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

	public class LobbyFaction
	{
		public bool Selectable;
		public string Name;
		public string Description;
		public string Side;
	}
}
