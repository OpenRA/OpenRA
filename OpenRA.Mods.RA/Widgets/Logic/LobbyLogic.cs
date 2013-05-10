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
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyLogic
	{
		Widget EditablePlayerTemplate, NonEditablePlayerTemplate, EmptySlotTemplate,
			   EditableSpectatorTemplate, NonEditableSpectatorTemplate, NewSpectatorTemplate;
		ScrollPanelWidget chatPanel;
		Widget chatTemplate;

		ScrollPanelWidget Players;
		Dictionary<string, string> CountryNames;
		string MapUid;
		Map Map;

		ColorPreviewManagerWidget colorPreview;

		readonly Action OnGameStart;
		readonly Action onExit;
		readonly OrderManager orderManager;

		public bool TeamGame;

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
						{ "addBots", false }
					});
				};

				Action onRetry = () =>
				{
					ConnectionLogic.Connect(om.Host, om.Port, onConnect, onExit);
				};

				Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
				{
					{ "onAbort", onExit },
					{ "onRetry", onRetry },
					{ "host", om.Host },
					{ "port", om.Port }
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
			Action onExit, Action onStart, bool addBots)
		{
			var lobby = widget;
			this.orderManager = orderManager;
			this.OnGameStart = () => { CloseWindow(); onStart(); };
			this.onExit = onExit;

			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.LobbyInfoChanged += UpdatePlayerList;
			Game.BeforeGameStart += OnGameStart;
			Game.AddChatLine += AddChatLine;
			Game.ConnectionStateChanged += ConnectionStateChanged;

			UpdateCurrentMap();
			Players = lobby.Get<ScrollPanelWidget>("PLAYERS");
			EditablePlayerTemplate = Players.Get("TEMPLATE_EDITABLE_PLAYER");
			NonEditablePlayerTemplate = Players.Get("TEMPLATE_NONEDITABLE_PLAYER");
			EmptySlotTemplate = Players.Get("TEMPLATE_EMPTY");
			EditableSpectatorTemplate = Players.Get("TEMPLATE_EDITABLE_SPECTATOR");
			NonEditableSpectatorTemplate = Players.Get("TEMPLATE_NONEDITABLE_SPECTATOR");
			NewSpectatorTemplate = Players.Get("TEMPLATE_NEW_SPECTATOR");
			colorPreview = lobby.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = Game.Settings.Player.Color;

			var mapPreview = lobby.Get<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.IsVisible = () => Map != null;
			mapPreview.Map = () => Map;
			mapPreview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint( orderManager, mapPreview, Map, mi );
			mapPreview.SpawnClients = () => LobbyUtils.GetSpawnClients(orderManager, Map);

			var mapTitle = lobby.GetOrNull<LabelWidget>("MAP_TITLE");
			if (mapTitle != null)
			{
				mapTitle.IsVisible = () => Map != null;
				mapTitle.GetText = () => Map.Title;
			}

			var mapType = lobby.GetOrNull<LabelWidget>("MAP_TYPE");
			if (mapType != null)
			{
				mapType.IsVisible = () => Map != null;
				mapType.GetText = () => Map.Type;
			}

			var mapAuthor = lobby.GetOrNull<LabelWidget>("MAP_AUTHOR");
			if (mapAuthor != null)
			{
				mapAuthor.IsVisible = () => Map != null;
				mapAuthor.GetText = () => "Created by {0}".F(Map.Author);
			}

			CountryNames = Rules.Info["world"].Traits.WithInterface<CountryInfo>()
				.Where(c => c.Selectable)
				.ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Any");

			var gameStarting = false;

			var mapButton = lobby.Get<ButtonWidget>("CHANGEMAP_BUTTON");
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
					{ "initialMap", Map.Uid },
					{ "onExit", () => {} },
					{ "onSelect", onSelect }
				});
			};
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			var assignTeams = lobby.GetOrNull<DropDownButtonWidget>("ASSIGNTEAMS_DROPDOWNBUTTON");
			if (assignTeams != null)
			{
				assignTeams.IsVisible = () => Game.IsHost;
				assignTeams.IsDisabled = () => gameStarting || orderManager.LobbyInfo.Clients.Count(c => c.Slot != null) < 2
					|| orderManager.LocalClient == null || orderManager.LocalClient.IsReady;

				assignTeams.OnMouseDown = _ =>
				{
					var options = Enumerable.Range(2, orderManager.LobbyInfo.Clients.Count(c => c.Slot != null).Clamp(2, 8) - 1).Select(d => new DropDownOption
					{
						Title = "{0} Teams".F(d),
						IsSelected = () => false,
						OnClick = () => orderManager.IssueOrder(Order.Command("assignteams {0}".F(d.ToString())))
					});
					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
					assignTeams.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			var disconnectButton = lobby.Get<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { CloseWindow(); onExit(); };

			var allowCheats = lobby.Get<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
			allowCheats.IsDisabled = () => !Game.IsHost || gameStarting || orderManager.LocalClient == null
				|| orderManager.LocalClient.IsReady;
			allowCheats.OnClick = () =>	orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));

			var crates = lobby.GetOrNull<CheckboxWidget>("CRATES_CHECKBOX");
			if (crates != null)
			{
				crates.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.Crates;
				crates.IsDisabled = () => !Game.IsHost || gameStarting || orderManager.LocalClient == null
					|| orderManager.LocalClient.IsReady; // maybe disable the checkbox if a map forcefully removes CrateDrop?
				crates.OnClick = () => orderManager.IssueOrder(Order.Command(
					"crates {0}".F(!orderManager.LobbyInfo.GlobalSettings.Crates)));
			}

			var fragileAlliance = lobby.GetOrNull<CheckboxWidget>("FRAGILEALLIANCES_CHECKBOX");
			if (fragileAlliance != null)
			{
				fragileAlliance.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.FragileAlliances;
				fragileAlliance.IsDisabled = () => !Game.IsHost || gameStarting
					|| orderManager.LocalClient == null	|| orderManager.LocalClient.IsReady;
				fragileAlliance.OnClick = () => orderManager.IssueOrder(Order.Command(
					"fragilealliance {0}".F(!orderManager.LobbyInfo.GlobalSettings.FragileAlliances)));
			};


			var difficulty = lobby.GetOrNull<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			if (difficulty != null)
			{
				difficulty.IsVisible = () => Map != null && Map.Difficulties != null && Map.Difficulties.Any();
				difficulty.IsDisabled = () => !Game.IsHost || gameStarting || orderManager.LocalClient == null || orderManager.LocalClient.IsReady;
				difficulty.GetText = () => orderManager.LobbyInfo.GlobalSettings.Difficulty;
				difficulty.OnMouseDown = _ =>
				{
					var options = Map.Difficulties.Select(d => new DropDownOption
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
			}

			var startGameButton = lobby.Get<ButtonWidget>("START_GAME_BUTTON");
			startGameButton.IsVisible = () => Game.IsHost;
			startGameButton.IsDisabled = () => gameStarting
				|| orderManager.LobbyInfo.Slots.Any(sl => sl.Value.Required && orderManager.LobbyInfo.ClientInSlot(sl.Key) == null);
			startGameButton.OnClick = () =>
			{
				gameStarting = true;
				orderManager.IssueOrder(Order.Command("startgame"));
			};

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
			if (addBots)
				Game.LobbyInfoChanged += WidgetUtils.Once(() =>
				{
					var slot = orderManager.LobbyInfo.FirstEmptySlot();
					var bot = Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name).FirstOrDefault();
					var botController = orderManager.LobbyInfo.Clients.Where(c => c.IsAdmin).FirstOrDefault();
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
			if (MapUid == orderManager.LobbyInfo.GlobalSettings.Map)
				return;
			MapUid = orderManager.LobbyInfo.GlobalSettings.Map;

			if (!Game.modData.AvailableMaps.ContainsKey (MapUid))
				if (Game.Settings.Game.AllowDownloading)
				{
					Game.DownloadMap (MapUid);
					Game.Debug("A new map has been downloaded...");
				}
				else
					throw new InvalidOperationException("Server's new map doesn't exist on your system and Downloading turned off");
			Map = new Map(Game.modData.AvailableMaps[MapUid].Path);

			var title = Ui.Root.Get<LabelWidget>("TITLE");
			title.Text = orderManager.LobbyInfo.GlobalSettings.ServerName;
		}

		void UpdatePlayerList()
		{
			var idx = 0;
			TeamGame = false;

			foreach (var kv in orderManager.LobbyInfo.Slots)
			{
				var key = kv.Key;
				var slot = kv.Value;
				var client = orderManager.LobbyInfo.ClientInSlot(key);
				Widget template = null;

				// get template for possible reuse
				if (idx < Players.Children.Count)
					template = Players.Children[idx];

				// Empty slot
				if (client == null)
				{
					if (template == null || template.Id != EmptySlotTemplate.Id)
						template = EmptySlotTemplate.Clone();

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
					if (template == null || template.Id != EditablePlayerTemplate.Id)
						template = EditablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);

					if (client.Bot != null)
						LobbyUtils.SetupEditableSlotWidget(template, slot, client, orderManager);
					else
						LobbyUtils.SetupEditableNameWidget(template, slot, client, orderManager);

					LobbyUtils.SetupEditableColorWidget(template, slot, client, orderManager, colorPreview);
					LobbyUtils.SetupEditableFactionWidget(template, slot, client, orderManager, CountryNames);
					LobbyUtils.SetupEditableTeamWidget(template, slot, client, orderManager, Map.GetSpawnPoints().Length);
					LobbyUtils.SetupEditableReadyWidget(template, slot, client, orderManager);

					if (slot.LockTeam || client.Team > 0)
						TeamGame = true;
				}
				else
				{	// Non-editable player in slot
					if (template == null || template.Id != NonEditablePlayerTemplate.Id)
						template = NonEditablePlayerTemplate.Clone();

					LobbyUtils.SetupClientWidget(template, slot, client, orderManager, client.Bot == null);
					LobbyUtils.SetupNameWidget(template, slot, client);
					LobbyUtils.SetupKickWidget(template, slot, client, orderManager);
					LobbyUtils.SetupColorWidget(template, slot, client);
					LobbyUtils.SetupFactionWidget(template, slot, client, CountryNames);
					LobbyUtils.SetupTeamWidget(template, slot, client);
					LobbyUtils.SetupReadyWidget(template, slot, client);
				}

				template.IsVisible = () => true;

				if (idx >= Players.Children.Count)
					Players.AddChild(template);
				else if (Players.Children[idx].Id != template.Id)
					Players.ReplaceChild(Players.Children[idx], template);

				idx++;
			}

			// Add spectators
			foreach (var client in orderManager.LobbyInfo.Clients.Where(client => client.Slot == null))
			{
				Widget template = null;
				var c = client;

				// get template for possible reuse
				if (idx < Players.Children.Count)
					template = Players.Children[idx];

				// Editable spectator
				if (c.Index == orderManager.LocalClient.Index)
				{
					if (template == null || template.Id != EditableSpectatorTemplate.Id)
						template = EditableSpectatorTemplate.Clone();

					LobbyUtils.SetupEditableNameWidget(template, null, c, orderManager);
					LobbyUtils.SetupEditableColorWidget(template, null, c, orderManager, colorPreview);
					LobbyUtils.SetupEditableReadyWidget(template, null, client, orderManager);
				}
				// Non-editable spectator
				else
				{
					if (template == null || template.Id != NonEditableSpectatorTemplate.Id)
						template = NonEditableSpectatorTemplate.Clone();

					LobbyUtils.SetupNameWidget(template, null, client);
					LobbyUtils.SetupKickWidget(template, null, client, orderManager);
					LobbyUtils.SetupColorWidget(template, null, client);
					LobbyUtils.SetupReadyWidget(template, null, client);
				}

				LobbyUtils.SetupClientWidget(template, null, c, orderManager, true);
				template.IsVisible = () => true;

				if (idx >= Players.Children.Count)
					Players.AddChild(template);
				else if (Players.Children[idx].Id != template.Id)
					Players.ReplaceChild(Players.Children[idx], template);

				idx++;
			}

			// Spectate button
			if (orderManager.LocalClient.Slot != null)
			{
				Widget spec = null;
				if (idx < Players.Children.Count)
					spec = Players.Children[idx];
				if (spec == null || spec.Id != NewSpectatorTemplate.Id)
					spec = NewSpectatorTemplate.Clone();

				var btn = spec.Get<ButtonWidget>("SPECTATE");
				btn.OnClick = () => orderManager.IssueOrder(Order.Command("spectate"));
				btn.IsDisabled = () => orderManager.LocalClient.IsReady;
				spec.IsVisible = () => true;

				if (idx >= Players.Children.Count)
					Players.AddChild(spec);
				else if (Players.Children[idx].Id != spec.Id)
					Players.ReplaceChild(Players.Children[idx], spec);

				idx++;
			}

			while (Players.Children.Count > idx)
				Players.RemoveChild(Players.Children[idx]);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
