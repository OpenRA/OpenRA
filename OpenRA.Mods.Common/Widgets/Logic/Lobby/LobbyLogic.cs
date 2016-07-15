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
using System.Threading.Tasks;
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

		public MapPreview Map { get; private set; }

		readonly ModData modData;
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

		bool addBotOnMapLoad;

		int lobbyChatUnreadMessages;
		int globalChatLastReadMessages;
		int globalChatUnreadMessages;

		// Listen for connection failures
		void ConnectionStateChanged(OrderManager om)
		{
			if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				// Show connection failed dialog
				Ui.CloseWindow();

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

		[ObjectCreator.UseCtor]
		internal LobbyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, OrderManager orderManager,
			Action onExit, Action onStart, bool skirmishMode)
		{
			Map = MapCache.UnknownMap;
			lobby = widget;
			this.modData = modData;
			this.orderManager = orderManager;
			this.onStart = onStart;
			this.onExit = onExit;
			this.skirmishMode = skirmishMode;

			// TODO: This needs to be reworked to support per-map tech levels, bots, etc.
			this.modRules = modData.DefaultRules;
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
				!Map.RulesLoaded || Map.InvalidCustomRules ||
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
						if (uid == Map.Uid)
							return;

						orderManager.IssueOrder(Order.Command("map " + uid));
						Game.Settings.Server.Map = uid;
						Game.Settings.Save();
					});

					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", Map.Uid },
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

				slotsButton.OnMouseDown = _ =>
				{
					var botNames = Map.Rules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name);
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
			optionsTab.IsDisabled = () => !Map.RulesLoaded || Map.InvalidCustomRules || panel == PanelType.Kick || panel == PanelType.ForceStart;
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
				startGameButton.IsDisabled = () => configurationDisabled() || Map.Status != MapStatus.Available ||
					orderManager.LobbyInfo.Slots.Any(sl => sl.Value.Required && orderManager.LobbyInfo.ClientInSlot(sl.Key) == null) ||
					(!orderManager.LobbyInfo.GlobalSettings.EnableSingleplayer && orderManager.LobbyInfo.IsSinglePlayer);

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
			var optionCheckboxes = new Dictionary<string, string>()
			{
				{ "EXPLORED_MAP_CHECKBOX", "explored" },
				{ "CRATES_CHECKBOX", "crates" },
				{ "SHORTGAME_CHECKBOX", "shortgame" },
				{ "FOG_CHECKBOX", "fog" },
				{ "ALLYBUILDRADIUS_CHECKBOX", "allybuild" },
				{ "ALLOWCHEATS_CHECKBOX", "cheats" },
				{ "CREEPS_CHECKBOX", "creeps" },
			};

			foreach (var kv in optionCheckboxes)
			{
				var checkbox = optionsBin.GetOrNull<CheckboxWidget>(kv.Key);
				if (checkbox != null)
				{
					var option = new CachedTransform<Session.Global, Session.LobbyOptionState>(
						gs => gs.LobbyOptions[kv.Value]);

					var visible = new CachedTransform<Session.Global, bool>(
						gs => gs.LobbyOptions.ContainsKey(kv.Value));

					checkbox.IsVisible = () => visible.Update(orderManager.LobbyInfo.GlobalSettings);
					checkbox.IsChecked = () => option.Update(orderManager.LobbyInfo.GlobalSettings).Enabled;
					checkbox.IsDisabled = () => configurationDisabled() ||
						option.Update(orderManager.LobbyInfo.GlobalSettings).Locked;
					checkbox.OnClick = () => orderManager.IssueOrder(Order.Command(
						"option {0} {1}".F(kv.Value, !option.Update(orderManager.LobbyInfo.GlobalSettings).Enabled)));
				}
			}

			var optionDropdowns = new Dictionary<string, string>()
			{
				{ "TECHLEVEL", "techlevel" },
				{ "STARTINGUNITS", "startingunits" },
				{ "STARTINGCASH", "startingcash" },
				{ "DIFFICULTY", "difficulty" },
				{ "GAMESPEED", "gamespeed" }
			};

			var allOptions = new CachedTransform<MapPreview, LobbyOption[]>(
				map => map.Rules.Actors["player"].TraitInfos<ILobbyOptions>()
					.Concat(map.Rules.Actors["world"].TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(map.Rules))
					.ToArray());

			foreach (var kv in optionDropdowns)
			{
				var dropdown = optionsBin.GetOrNull<DropDownButtonWidget>(kv.Key + "_DROPDOWNBUTTON");
				if (dropdown != null)
				{
					var optionValue = new CachedTransform<Session.Global, Session.LobbyOptionState>(
						gs => gs.LobbyOptions[kv.Value]);

					var option = new CachedTransform<MapPreview, LobbyOption>(
						map => allOptions.Update(map).FirstOrDefault(o => o.Id == kv.Value));

					var getOptionLabel = new CachedTransform<string, string>(id =>
					{
						string value;
						if (id == null || !option.Update(Map).Values.TryGetValue(id, out value))
							return "Not Available";

						return value;
					});

					dropdown.GetText = () => getOptionLabel.Update(optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value);
					dropdown.IsVisible = () => option.Update(Map) != null;
					dropdown.IsDisabled = () => configurationDisabled() ||
						optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Locked;

					dropdown.OnMouseDown = _ =>
					{
						Func<KeyValuePair<string, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (c, template) =>
						{
							Func<bool> isSelected = () => optionValue.Update(orderManager.LobbyInfo.GlobalSettings).Value == c.Key;
							Action onClick = () => orderManager.IssueOrder(Order.Command("option {0} {1}".F(kv.Value, c.Key)));

							var item = ScrollItemWidget.Setup(template, isSelected, onClick);
							item.Get<LabelWidget>("LABEL").GetText = () => c.Value;
							return item;
						};

						var options = option.Update(Map).Values;
						dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
					};

					var label = optionsBin.GetOrNull(kv.Key + "_DESC");
					if (label != null)
						label.IsVisible = () => option.Update(Map) != null;
				}
			}

			var disconnectButton = lobby.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { Ui.CloseWindow(); onExit(); };

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
				addBotOnMapLoad = true;
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				orderManager.AddChatLine -= AddChatLine;
				Game.LobbyInfoChanged -= UpdateCurrentMap;
				Game.LobbyInfoChanged -= UpdatePlayerList;
				Game.BeforeGameStart -= OnGameStart;
				Game.ConnectionStateChanged -= ConnectionStateChanged;
			}

			base.Dispose(disposing);
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

		void LoadMapPreviewRules(MapPreview map)
		{
			new Task(() =>
			{
				// Force map rules to be loaded on this background thread
				map.PreloadRules();
			}).Start();
		}

		void UpdateCurrentMap()
		{
			var uid = orderManager.LobbyInfo.GlobalSettings.Map;
			if (Map.Uid == uid)
				return;

			Map = modData.MapCache[uid];
			if (Map.Status == MapStatus.Available)
			{
				// Maps need to be validated and pre-loaded before they can be accessed
				var currentMap = Map;
				new Task(() =>
				{
					// Force map rules to be loaded on this background thread
					currentMap.PreloadRules();
					Game.RunAfterTick(() =>
					{
						// Map may have changed in the meantime
						if (currentMap != Map)
							return;

						// Tell the server that we have the map
						if (!currentMap.InvalidCustomRules)
							orderManager.IssueOrder(Order.Command("state {0}".F(Session.ClientState.NotReady)));

						if (addBotOnMapLoad)
						{
							var slot = orderManager.LobbyInfo.FirstEmptyBotSlot();
							var bot = currentMap.Rules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name).FirstOrDefault();
							var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
							if (slot != null && bot != null)
								orderManager.IssueOrder(Order.Command("slot_bot {0} {1} {2}".F(slot, botController.Index, bot)));

							addBotOnMapLoad = false;
						}
					});
				}).Start();
			}
			else if (Map.Status == MapStatus.DownloadAvailable)
				LoadMapPreviewRules(Map);
			else if (Game.Settings.Game.AllowDownloading)
				modData.MapCache.QueryRemoteMapDetails(new[] { uid }, LoadMapPreviewRules);
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
						LobbyUtils.SetupEditableSlotWidget(this, template, slot, client, orderManager);
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
						LobbyUtils.SetupEditableSlotWidget(this, template, slot, client, orderManager);
					else
						LobbyUtils.SetupEditableNameWidget(template, slot, client, orderManager);

					LobbyUtils.SetupEditableColorWidget(template, slot, client, orderManager, shellmapWorld, colorPreview);
					LobbyUtils.SetupEditableFactionWidget(template, slot, client, orderManager, factions);
					LobbyUtils.SetupEditableTeamWidget(template, slot, client, orderManager, Map);
					LobbyUtils.SetupEditableSpawnWidget(template, slot, client, orderManager, Map);
					LobbyUtils.SetupEditableReadyWidget(template, slot, client, orderManager, Map);
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

					if (client.IsAdmin)
						LobbyUtils.SetupEditableReadyWidget(template, null, client, orderManager, Map);
				}
				else
				{
					// Non-editable spectator
					if (template == null || template.Id != nonEditableSpectatorTemplate.Id)
						template = nonEditableSpectatorTemplate.Clone();

					LobbyUtils.SetupNameWidget(template, null, client);
					LobbyUtils.SetupKickWidget(template, null, client, orderManager, lobby,
						() => panel = PanelType.Kick, () => panel = PanelType.Players);

					if (client.IsAdmin)
						LobbyUtils.SetupReadyWidget(template, null, client);
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
			Ui.CloseWindow();
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
