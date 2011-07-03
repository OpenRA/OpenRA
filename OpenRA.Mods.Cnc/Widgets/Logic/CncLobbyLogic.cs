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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncLobbyLogic
	{
		Widget EditablePlayerTemplate, NonEditablePlayerTemplate, EmptySlotTemplate,
			   EditableSpectatorTemplate, NonEditableSpectatorTemplate, NewSpectatorTemplate;
		ScrollPanelWidget chatPanel;
		Widget chatTemplate;
		
		ScrollPanelWidget Players;
		Dictionary<string, string> CountryNames;
		string MapUid;
		Map Map;
		
		CncColorPickerPaletteModifier PlayerPalettePreview;

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
					CloseWindow();
					CncConnectingLogic.Connect(om.Host, om.Port, onConnect, onExit);
				};
				
				Widget.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
	            {
					{ "onAbort", onExit },
					{ "onRetry", onRetry },
					{ "host", om.Host },
					{ "port", om.Port }
				});	
			}
		}
		
		public void CloseWindow()
		{
			Game.LobbyInfoChanged -= UpdateCurrentMap;
			Game.LobbyInfoChanged -= UpdatePlayerList;
			Game.BeforeGameStart -= OnGameStart;
			Game.AddChatLine -= AddChatLine;
			Game.ConnectionStateChanged -= ConnectionStateChanged;
			
			Widget.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		internal CncLobbyLogic([ObjectCreator.Param( "widget" )] Widget lobby,
		                       [ObjectCreator.Param] World world, // Shellmap world
		                       [ObjectCreator.Param] OrderManager orderManager,
		                       [ObjectCreator.Param] Action onExit,
		                       [ObjectCreator.Param] Action onStart,
		                       [ObjectCreator.Param] bool addBots)
		{
			this.orderManager = orderManager;
			this.OnGameStart = () => { CloseWindow(); onStart(); };
			this.onExit = onExit;
			
			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.LobbyInfoChanged += UpdatePlayerList;
			Game.BeforeGameStart += OnGameStart;
			Game.AddChatLine += AddChatLine;
			Game.ConnectionStateChanged += ConnectionStateChanged;

			UpdateCurrentMap();
			PlayerPalettePreview = world.WorldActor.Trait<CncColorPickerPaletteModifier>();
			PlayerPalettePreview.Ramp = Game.Settings.Player.ColorRamp;
			Players = lobby.GetWidget<ScrollPanelWidget>("PLAYERS");
			EditablePlayerTemplate = Players.GetWidget("TEMPLATE_EDITABLE_PLAYER");
			NonEditablePlayerTemplate = Players.GetWidget("TEMPLATE_NONEDITABLE_PLAYER");
			EmptySlotTemplate = Players.GetWidget("TEMPLATE_EMPTY");
			EditableSpectatorTemplate = Players.GetWidget("TEMPLATE_EDITABLE_SPECTATOR");
			NonEditableSpectatorTemplate = Players.GetWidget("TEMPLATE_NONEDITABLE_SPECTATOR");
			NewSpectatorTemplate = Players.GetWidget("TEMPLATE_NEW_SPECTATOR");

			var mapPreview = lobby.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.IsVisible = () => Map != null;
			mapPreview.Map = () => Map;
			mapPreview.OnMouseDown = mi =>
			{
				var map = mapPreview.Map();
				if (map == null || mi.Button != MouseButton.Left
				    || orderManager.LocalClient.State == Session.ClientState.Ready)
					return;

				var p = map.SpawnPoints
					.Select((sp, i) => Pair.New(mapPreview.ConvertToPreview(map, sp), i))
					.Where(a => (a.First - mi.Location).LengthSquared < 64)
					.Select(a => a.Second + 1)
					.FirstOrDefault();

				var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == p);
				if (p == 0 || !owned)
					orderManager.IssueOrder(Order.Command("spawn {0} {1}".F(orderManager.LocalClient.Index, p)));
			};

			var mapTitle = lobby.GetWidget<LabelWidget>("MAP_TITLE");
			mapTitle.IsVisible = () => Map != null;
			mapTitle.GetText = () => Map.Title;

			mapPreview.SpawnColors = () =>
			{
				var spawns = Map.SpawnPoints;
				var sc = new Dictionary<int2, Color>();

				for (int i = 1; i <= spawns.Count(); i++)
				{
					var client = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == i);
					if (client == null)
						continue;
					sc.Add(spawns.ElementAt(i - 1), client.ColorRamp.GetColor(0));
				}
				return sc;
			};

			CountryNames = Rules.Info["world"].Traits.WithInterface<OpenRA.Traits.CountryInfo>()
				.ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Any");

			var mapButton = lobby.GetWidget<ButtonWidget>("CHANGEMAP_BUTTON");
			mapButton.OnClick = () =>
			{
				var onSelect = new Action<Map>(m =>
				{
					orderManager.IssueOrder(Order.Command("map " + m.Uid));
					Game.Settings.Server.Map = m.Uid;
					Game.Settings.Save();
				});

				Widget.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
				{
					{ "initialMap", Map.Uid },
					{ "onExit", () => {} },
					{ "onSelect", onSelect }
				});
			};
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			var disconnectButton = lobby.GetWidget<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () => { CloseWindow(); onExit(); };

			var gameStarting = false;

			var allowCheats = lobby.GetWidget<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
			allowCheats.IsDisabled = () => !Game.IsHost || gameStarting || orderManager.LocalClient == null
				|| orderManager.LocalClient.State == Session.ClientState.Ready;
			allowCheats.OnClick = () =>	orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));

			var startGameButton = lobby.GetWidget<ButtonWidget>("START_GAME_BUTTON");
			startGameButton.IsVisible = () => Game.IsHost;
			startGameButton.IsDisabled = () => gameStarting;
			startGameButton.OnClick = () =>
			{
				gameStarting = true;
				orderManager.IssueOrder(Order.Command("startgame"));
			};

			bool teamChat = false;
			var chatLabel = lobby.GetWidget<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.GetWidget<TextFieldWidget>("CHAT_TEXTFIELD");
			chatTextField.OnEnterKey = () =>
			{
				if (chatTextField.Text.Length == 0)
					return true;

				var order = (teamChat) ? Order.TeamChat(chatTextField.Text) : Order.Chat(chatTextField.Text);
				orderManager.IssueOrder(order);
				chatTextField.Text = "";
				return true;
			};

			chatTextField.OnTabKey = () =>
			{
				teamChat ^= true;
				chatLabel.Text = (teamChat) ? "Team:" : "Chat:";
				return true;
			};
			
			chatPanel = lobby.GetWidget<ScrollPanelWidget>("CHAT_DISPLAY");
			chatTemplate = chatPanel.GetWidget("CHAT_TEMPLATE");
			
			lobby.GetWidget<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow("MUSIC_PANEL", new WidgetArgs()
                {
					{ "onExit", () => {} },
				});
			};

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
		
		public void AddChatLine(Color c, string from, string text)
		{
			var name = from+":";
			var font = Game.Renderer.Fonts["Regular"];
			var nameSize = font.Measure(from);
			
			var template = chatTemplate.Clone() as ContainerWidget;
			template.IsVisible = () => true;
			
			var time = System.DateTime.Now;
			template.GetWidget<LabelWidget>("TIME").GetText = () => "[{0:D2}:{1:D2}]".F(time.Hour, time.Minute);
			
			var p = template.GetWidget<LabelWidget>("NAME");
			p.Color = c;
			p.GetText = () => name;
			p.Bounds.Width = nameSize.X;
			
			var t = template.GetWidget<LabelWidget>("TEXT");
			t.Bounds.X += nameSize.X;
			t.Bounds.Width -= nameSize.X;
			
			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			text = WidgetUtils.WrapText(text, t.Bounds.Width, font);
			t.GetText = () => text;
			var oldHeight = t.Bounds.Height;
			t.Bounds.Height = font.Measure(text).Y;
			template.Bounds.Height += (t.Bounds.Height - oldHeight);
			
			chatPanel.AddChild(template);
			chatPanel.ScrollToBottom();
		}

		void UpdateCurrentMap()
		{
			if (MapUid == orderManager.LobbyInfo.GlobalSettings.Map) return;
			MapUid = orderManager.LobbyInfo.GlobalSettings.Map;
			Map = new Map(Game.modData.AvailableMaps[MapUid].Path);

			var title = Widget.RootWidget.GetWidget<LabelWidget>("TITLE");
			title.Text = orderManager.LobbyInfo.GlobalSettings.ServerName;
		}

		class SlotDropDownOption
		{
			public string Title;
			public string Order;
			public Func<bool> Selected;
			
			public SlotDropDownOption(string title, string order, Func<bool> selected)
			{
				Title = title;
				Order = order;
				Selected = selected;
			}
		}
		
		void ShowSlotDropDown(DropDownButtonWidget dropdown, Session.Slot slot, Session.Client client)
		{
			var options = new List<SlotDropDownOption>()
			{
				new SlotDropDownOption("Open", "slot_open "+slot.PlayerReference, () => (!slot.Closed && client == null)),
				new SlotDropDownOption("Closed", "slot_close "+slot.PlayerReference, () => slot.Closed)
			};

			if (slot.AllowBots)
				foreach (var b in Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name))
				{
					var bot = b;
					options.Add(new SlotDropDownOption(bot,
				                                   "slot_bot {0} {1}".F(slot.PlayerReference, bot),
				                                   () => client != null && client.Bot == bot));
				}

			Func<SlotDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  o.Selected,
				                                  () => orderManager.IssueOrder(Order.Command(o.Order)));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o.Title;
				return item;
			};
			
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}
		
		void ShowRaceDropDown(DropDownButtonWidget dropdown, Session.Client client)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (race, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => client.Country == race,
				                                  () => orderManager.IssueOrder(Order.Command("race {0} {1}".F(client.Index, race))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => CountryNames[race];
				var flag = item.GetWidget<ImageWidget>("FLAG");
				flag.GetImageCollection = () => "flags";
				flag.GetImageName = () => race;
				return item;
			};
			
			dropdown.ShowDropDown("RACE_DROPDOWN_TEMPLATE", 150, CountryNames.Keys.ToList(), setupItem);
		}
				
		void ShowTeamDropDown(DropDownButtonWidget dropdown, Session.Client client)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => client.Team == ii,
				                                  () => orderManager.IssueOrder(Order.Command("team {0} {1}".F(client.Index, ii))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};
			
			var options = Graphics.Util.MakeArray(Map.SpawnPoints.Count()+1, i => i).ToList();
			dropdown.ShowDropDown("TEAM_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}

		void ShowSpawnDropDown(DropDownButtonWidget dropdown, Session.Client client)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => client.SpawnPoint == ii, 
				                                  () => orderManager.IssueOrder(Order.Command("spawn {0} {1}".F(client.Index, ii))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};

			var taken = orderManager.LobbyInfo.Clients
				.Where(c => c.SpawnPoint != 0 && c.SpawnPoint != client.SpawnPoint && c.Slot != null)
				.Select(c => c.SpawnPoint).ToList();

			var options = Graphics.Util.MakeArray(Map.SpawnPoints.Count() + 1, i => i).Except(taken).ToList();
			dropdown.ShowDropDown("TEAM_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}

		void ShowColorDropDown(DropDownButtonWidget color, Session.Client client)
		{
			Action<ColorRamp> onSelect = c =>
			{
				Game.Settings.Player.ColorRamp = c;
				Game.Settings.Save();
				color.RemovePanel();
				orderManager.IssueOrder(Order.Command("color {0} {1}".F(client.Index, c)));
			};
			
			Action<ColorRamp> onChange = c =>
			{
				PlayerPalettePreview.Ramp = c;
			};
			
			var colorChooser = Game.LoadWidget(orderManager.world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", client.ColorRamp }
			});
			
			color.AttachPanel(colorChooser);
		}
		
		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.RemoveChildren();
			
			foreach (var kv in orderManager.LobbyInfo.Slots)
			{
				var key = kv.Key;
				var slot = kv.Value;
				var client = orderManager.LobbyInfo.ClientInSlot(key);
				Widget template;

				// Empty slot
				if (client == null)
				{
					template = EmptySlotTemplate.Clone();
					Func<string> getText = () => slot.Closed ? "Closed" : "Open";
					var ready = orderManager.LocalClient.State == Session.ClientState.Ready;

					if (Game.IsHost)
					{
						var name = template.GetWidget<DropDownButtonWidget>("NAME_HOST");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						name.GetText = getText;
						name.OnMouseDown = _ => ShowSlotDropDown(name, slot, client);
					}
					else
					{
						var name = template.GetWidget<LabelWidget>("NAME");
						name.IsVisible = () => true;
						name.GetText = getText;
					}

					var join = template.GetWidget<ButtonWidget>("JOIN");
					join.IsVisible = () => !slot.Closed;
					join.IsDisabled = () => ready;
					join.OnClick = () => orderManager.IssueOrder(Order.Command("slot " + key));
				}
				// Editable player in slot
				else if ((client.Index == orderManager.LocalClient.Index) ||
				         (client.Bot != null && Game.IsHost))
				{
					template = EditablePlayerTemplate.Clone();
					var botReady = (client.Bot != null && Game.IsHost
						    && orderManager.LocalClient.State == Session.ClientState.Ready);
					var ready = botReady || client.State == Session.ClientState.Ready;

					if (client.Bot != null)
					{
						var name = template.GetWidget<DropDownButtonWidget>("BOT_DROPDOWN");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						name.GetText = () => client.Name;
						name.OnMouseDown = _ => ShowSlotDropDown(name, slot, client);
					}
					else
					{
						var name = template.GetWidget<TextFieldWidget>("NAME");
						name.IsVisible = () => true;
						name.IsDisabled = () => ready;
						name.Text = client.Name;
						name.OnEnterKey = () =>
						{
							name.Text = name.Text.Trim();
							if (name.Text.Length == 0)
								name.Text = client.Name;

							name.LoseFocus();
							if (name.Text == client.Name)
								return true;

							orderManager.IssueOrder(Order.Command("name " + name.Text));
							Game.Settings.Player.Name = name.Text;
							Game.Settings.Save();
							return true;
						};
						name.OnLoseFocus = () => name.OnEnterKey();
					}

					var color = template.GetWidget<DropDownButtonWidget>("COLOR");
					color.IsDisabled = () => slot.LockColor || ready;
					color.OnMouseDown = _ => ShowColorDropDown(color, client);
					
					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => client.ColorRamp.GetColor(0);

					var faction = template.GetWidget<DropDownButtonWidget>("FACTION");
					faction.IsDisabled = () => slot.LockRace || ready;
					faction.OnMouseDown = _ => ShowRaceDropDown(faction, client);
					
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[client.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => client.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<DropDownButtonWidget>("TEAM");
					team.IsDisabled = () => slot.LockTeam || ready;
					team.OnMouseDown = _ => ShowTeamDropDown(team, client);
					team.GetText = () => (client.Team == 0) ? "-" : client.Team.ToString();

					var spawn = template.GetWidget<DropDownButtonWidget>("SPAWN");
					spawn.IsDisabled = () => slot.LockSpawn || ready;
					spawn.OnMouseDown = _ => ShowSpawnDropDown(spawn, client);
					spawn.GetText = () => (client.SpawnPoint == 0) ? "-" : client.SpawnPoint.ToString();

					if (client.Bot == null)
					{
						// local player
						var status = template.GetWidget<CheckboxWidget>("STATUS_CHECKBOX");
						status.IsChecked = () => ready;
						status.IsVisible = () => true;
						status.OnClick += CycleReady;
					}
					else // Bot
						template.GetWidget<ImageWidget>("STATUS_IMAGE").IsVisible = () => true;
				}
				// Non-editable player in slot
				else
				{
					template = NonEditablePlayerTemplate.Clone();
					template.GetWidget<LabelWidget>("NAME").GetText = () => client.Name;
					var color = template.GetWidget<ColorBlockWidget>("COLOR");
					color.GetColor = () => client.ColorRamp.GetColor(0);

					var faction = template.GetWidget<LabelWidget>("FACTION");
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[client.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => client.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<LabelWidget>("TEAM");
					team.GetText = () => (client.Team == 0) ? "-" : client.Team.ToString();

					var spawn = template.GetWidget<LabelWidget>("SPAWN");
					spawn.GetText = () => (client.SpawnPoint == 0) ? "-" : client.SpawnPoint.ToString();

					template.GetWidget<ImageWidget>("STATUS_IMAGE").IsVisible = () => 
						client.Bot != null || client.State == Session.ClientState.Ready;

					var kickButton = template.GetWidget<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && client.Index != orderManager.LocalClient.Index;
					kickButton.IsDisabled = () => orderManager.LocalClient.State == Session.ClientState.Ready;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + client.Index));
				}

				template.IsVisible = () => true;
				Players.AddChild(template);
			}

			// Add spectators
			foreach (var client in orderManager.LobbyInfo.Clients.Where(client => client.Slot == null))
			{
				Widget template;
				var ready = client.State == Session.ClientState.Ready;
				// Editable spectator
				if (client.Index == orderManager.LocalClient.Index)
				{
					template = EditableSpectatorTemplate.Clone();
					var name = template.GetWidget<TextFieldWidget>("NAME");
					name.IsDisabled = () => ready;
					name.Text = client.Name;
					name.OnEnterKey = () =>
					{
						name.Text = name.Text.Trim();
						if (name.Text.Length == 0)
							name.Text = client.Name;

						name.LoseFocus();
						if (name.Text == client.Name)
							return true;

						orderManager.IssueOrder(Order.Command("name " + name.Text));
						Game.Settings.Player.Name = name.Text;
						Game.Settings.Save();
						return true;
					};
					name.OnLoseFocus = () => name.OnEnterKey();

					var color = template.GetWidget<DropDownButtonWidget>("COLOR");
					color.IsDisabled = () => ready;
					color.OnMouseDown = _ => ShowColorDropDown(color, client);

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => client.ColorRamp.GetColor(0);

					var status = template.GetWidget<CheckboxWidget>("STATUS_CHECKBOX");
					status.IsChecked = () => ready;
					status.OnClick += CycleReady;
				}
				// Non-editable spectator
				else
				{
					template = NonEditableSpectatorTemplate.Clone();
					template.GetWidget<LabelWidget>("NAME").GetText = () => client.Name;
					var color = template.GetWidget<ColorBlockWidget>("COLOR");
					color.GetColor = () => client.ColorRamp.GetColor(0);

					template.GetWidget<ImageWidget>("STATUS_IMAGE").IsVisible = () => 
						client.Bot != null || client.State == Session.ClientState.Ready;

					var kickButton = template.GetWidget<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && client.Index != orderManager.LocalClient.Index;
					kickButton.IsDisabled = () => orderManager.LocalClient.State == Session.ClientState.Ready;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + client.Index));
				}

				template.IsVisible = () => true;
				Players.AddChild(template);
			}

			// Spectate button
			if (orderManager.LocalClient.Slot != null)
			{
				var spec = NewSpectatorTemplate.Clone();
				var btn = spec.GetWidget<ButtonWidget>("SPECTATE");
				btn.OnClick = () => orderManager.IssueOrder(Order.Command("spectate"));
				btn.IsDisabled = () => orderManager.LocalClient.State == Session.ClientState.Ready;
				spec.IsVisible = () => true;
				Players.AddChild(spec);
			}
		}

		bool SpawnPointAvailable(int index) { return (index == 0) || orderManager.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }

		void CycleReady()
		{
			orderManager.IssueOrder(Order.Command("ready"));
		}
	}
	
	public class CncColorPickerLogic
	{
		ColorRamp ramp;
		[ObjectCreator.UseCtor]
		public CncColorPickerLogic([ObjectCreator.Param] Widget widget,
		                           [ObjectCreator.Param] ColorRamp initialRamp,
		                           [ObjectCreator.Param] Action<ColorRamp> onChange,
		                           [ObjectCreator.Param] Action<ColorRamp> onSelect,
		                           [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			var panel = widget.GetWidget("COLOR_CHOOSER");
			ramp = initialRamp;
			var hueSlider = panel.GetWidget<SliderWidget>("HUE_SLIDER");
			var satSlider = panel.GetWidget<SliderWidget>("SAT_SLIDER");
			var lumSlider = panel.GetWidget<SliderWidget>("LUM_SLIDER");
			
			Action sliderChanged = () => 
			{
				ramp = new ColorRamp((byte)(255*hueSlider.GetOffset()),
				                     (byte)(255*satSlider.GetOffset()),
				                     (byte)(255*lumSlider.GetOffset()),
				                     10);
				onChange(ramp);
			};
				         
			hueSlider.OnChange += _ => sliderChanged();
			satSlider.OnChange += _ => sliderChanged();
			lumSlider.OnChange += _ => sliderChanged();
			
			Action updateSliders = () =>
			{
				hueSlider.SetOffset(ramp.H / 255f);
				satSlider.SetOffset(ramp.S / 255f);
				lumSlider.SetOffset(ramp.L / 255f);
			};
			
			panel.GetWidget<ButtonWidget>("SAVE_BUTTON").OnClick = () => onSelect(ramp);
			panel.GetWidget<ButtonWidget>("RANDOM_BUTTON").OnClick = () => 
			{
				var hue = (byte)Game.CosmeticRandom.Next(255);
				var sat = (byte)Game.CosmeticRandom.Next(255);
				var lum = (byte)Game.CosmeticRandom.Next(51,255);
				
				ramp = new ColorRamp(hue, sat, lum, 10);
				updateSliders();
				sliderChanged();
			};

			// Set the initial state
			updateSliders();
			onChange(ramp);
		}
	}
}
