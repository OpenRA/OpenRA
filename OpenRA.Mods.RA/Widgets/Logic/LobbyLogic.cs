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
			colorPreview.Ramp = Game.Settings.Player.ColorRamp;

			var mapPreview = lobby.Get<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.IsVisible = () => Map != null;
			mapPreview.Map = () => Map;
			mapPreview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint( orderManager, mapPreview, Map, mi );
			mapPreview.OnTooltip = (spawnPoint, pos) => LobbyUtils.ShowSpawnPointTooltip(orderManager, spawnPoint, pos);
			mapPreview.SpawnColors = () => LobbyUtils.GetSpawnColors(orderManager, Map);

			var mapTitle = lobby.GetOrNull<LabelWidget>("MAP_TITLE");
			if (mapTitle != null)
			{
				mapTitle.IsVisible = () => Map != null;
				mapTitle.GetText = () => Map.Title;
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

			var randomMapButton = lobby.GetOrNull<ButtonWidget>("RANDOMMAP_BUTTON");
			if (randomMapButton != null && Game.modData.AvailableMaps.Any())
			{
				randomMapButton.OnClick = () =>
				{
					var mapUid = Game.modData.AvailableMaps.Random(Game.CosmeticRandom).Key;
					orderManager.IssueOrder(Order.Command("map " + mapUid));
					Game.Settings.Server.Map = mapUid;
					Game.Settings.Save();
				};
				randomMapButton.IsVisible = () => mapButton.Visible && Game.IsHost;
			}

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
			startGameButton.IsDisabled = () => gameStarting;
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
					if (slot != null && bot != null)
						orderManager.IssueOrder(Order.Command("slot_bot {0} {1}".F(slot, bot)));
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
			if (MapUid == orderManager.LobbyInfo.GlobalSettings.Map) return;
			MapUid = orderManager.LobbyInfo.GlobalSettings.Map;
			Map = new Map(Game.modData.AvailableMaps[MapUid].Path);

			var title = Ui.Root.Get<LabelWidget>("TITLE");
			title.Text = orderManager.LobbyInfo.GlobalSettings.ServerName;
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
				if (idx < Players.Children.Count)
					template = Players.Children [idx];

				// Empty slot
				if (client == null)
				{
					if (template == null || template.Id != EmptySlotTemplate.Id)
						template = EmptySlotTemplate.Clone();

					Func<string> getText = () => slot.Closed ? "Closed" : "Open";
					var ready = orderManager.LocalClient.IsReady;

					if (Game.IsHost)
					{
						var name = template.Get<DropDownButtonWidget>("NAME_HOST");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						name.GetText = getText;
						name.OnMouseDown = _ => LobbyUtils.ShowSlotDropDown(name, slot, client, orderManager);
					}
					else
					{
						var name = template.Get<LabelWidget>("NAME");
						name.IsVisible = () => true;
						name.GetText = getText;
					}

					var join = template.Get<ButtonWidget>("JOIN");
					join.IsVisible = () => !slot.Closed;
					join.IsDisabled = () => ready;
					join.OnClick = () => orderManager.IssueOrder(Order.Command("slot " + key));
				}
				// Editable player in slot
				else if ((client.Index == orderManager.LocalClient.Index) ||
						 (client.Bot != null && Game.IsHost))
				{
					if (template == null || template.Id != EditablePlayerTemplate.Id)
						template = EditablePlayerTemplate.Clone();

					var botReady = client.Bot != null && Game.IsHost && orderManager.LocalClient.IsReady;
					var ready = botReady || client.IsReady;

					if (client.Bot != null)
					{
						var name = template.Get<DropDownButtonWidget>("BOT_DROPDOWN");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						name.GetText = () => client.Name;
						name.OnMouseDown = _ => LobbyUtils.ShowSlotDropDown(name, slot, client, orderManager);
					}
					else
					{
						var name = template.Get<TextFieldWidget>("NAME");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						LobbyUtils.SetupNameWidget(orderManager, client, name);
					}

					var color = template.Get<DropDownButtonWidget>("COLOR");
					color.IsDisabled = () => slot.LockColor || ready;
					color.OnMouseDown = _ => LobbyUtils.ShowColorDropDown(color, client, orderManager, colorPreview);

					var colorBlock = color.Get<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => client.ColorRamp.GetColor(0);

					var faction = template.Get<DropDownButtonWidget>("FACTION");
					faction.IsDisabled = () => slot.LockRace || ready;
					faction.OnMouseDown = _ => LobbyUtils.ShowRaceDropDown(faction, client, orderManager, CountryNames);

					var factionname = faction.Get<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[client.Country];
					var factionflag = faction.Get<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => client.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.Get<DropDownButtonWidget>("TEAM");
					team.IsDisabled = () => slot.LockTeam || ready;
					team.OnMouseDown = _ => LobbyUtils.ShowTeamDropDown(team, client, orderManager, Map);
					team.GetText = () => (client.Team == 0) ? "-" : client.Team.ToString();

					if (client.Bot == null)
					{
						// local player
						var status = template.Get<CheckboxWidget>("STATUS_CHECKBOX");
						status.IsChecked = () => ready;
						status.IsVisible = () => true;
						status.OnClick += CycleReady;
					}
					else // Bot
						template.Get<ImageWidget>("STATUS_IMAGE").IsVisible = () => true;
				}
				else
				{	// Non-editable player in slot
					if (template == null || template.Id != NonEditablePlayerTemplate.Id)
						template = NonEditablePlayerTemplate.Clone();

					template.Get<LabelWidget>("NAME").GetText = () => client.Name;
					if (client.IsAdmin)
						template.Get<LabelWidget>("NAME").Font = "Bold";
					var color = template.Get<ColorBlockWidget>("COLOR");
					color.GetColor = () => client.ColorRamp.GetColor(0);

					var faction = template.Get<LabelWidget>("FACTION");
					var factionname = faction.Get<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[client.Country];
					var factionflag = faction.Get<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => client.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.Get<LabelWidget>("TEAM");
					team.GetText = () => (client.Team == 0) ? "-" : client.Team.ToString();

					template.Get<ImageWidget>("STATUS_IMAGE").IsVisible = () =>
						client.Bot != null || client.IsReady;

					var kickButton = template.Get<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && client.Index != orderManager.LocalClient.Index;
					kickButton.IsDisabled = () => orderManager.LocalClient.IsReady;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + client.Index));
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
				var ready = c.IsReady;

				// get template for possible reuse
				if (idx < Players.Children.Count)
					template = Players.Children[idx];

				// Editable spectator
				if (c.Index == orderManager.LocalClient.Index)
				{
					if (template == null || template.Id != EditableSpectatorTemplate.Id)
						template = EditableSpectatorTemplate.Clone();

					var name = template.Get<TextFieldWidget>("NAME");
					name.IsDisabled = () => ready;
					LobbyUtils.SetupNameWidget(orderManager, c, name);

					var color = template.Get<DropDownButtonWidget>("COLOR");
					color.IsDisabled = () => ready;
					color.OnMouseDown = _ => LobbyUtils.ShowColorDropDown(color, c, orderManager, colorPreview);

					var colorBlock = color.Get<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.ColorRamp.GetColor(0);

					var status = template.Get<CheckboxWidget>("STATUS_CHECKBOX");
					status.IsChecked = () => ready;
					status.OnClick += CycleReady;
				}
				// Non-editable spectator
				else
				{
					if (template == null || template.Id != NonEditableSpectatorTemplate.Id)
						template = NonEditableSpectatorTemplate.Clone();

					template.Get<LabelWidget>("NAME").GetText = () => c.Name;
					if (client.IsAdmin)
						template.Get<LabelWidget>("NAME").Font = "Bold";
					var color = template.Get<ColorBlockWidget>("COLOR");
					color.GetColor = () => c.ColorRamp.GetColor(0);

					template.Get<ImageWidget>("STATUS_IMAGE").IsVisible = () => c.Bot != null || c.IsReady;

					var kickButton = template.Get<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
					kickButton.IsDisabled = () => orderManager.LocalClient.IsReady;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + c.Index));
				}

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

		void CycleReady()
		{
			orderManager.IssueOrder(Order.Command("ready"));
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
