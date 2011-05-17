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

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncLobbyLogic : IWidgetDelegate
	{
		Widget LocalPlayerTemplate, RemotePlayerTemplate, EmptySlotTemplate, EmptySlotTemplateHost;
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
		
		static bool staticSetup;
		public static CncLobbyLogic GetHandler()
		{
			var panel = Widget.RootWidget.GetWidget("SERVER_LOBBY");
            if (panel == null)
                return null;
			
			return panel.DelegateObject as CncLobbyLogic;
		}

		static void LobbyInfoChangedStub()
		{
			var handler = GetHandler();
			if (handler == null)
				return;
			
			handler.UpdateCurrentMap();
			handler.UpdatePlayerList();
		}

		static void BeforeGameStartStub()
		{
			var handler = GetHandler();
			if (handler == null)
				return;
			
			handler.OnGameStart();
		}

		static void AddChatLineStub(Color c, string from, string text)
		{
			var handler = GetHandler();
			if (handler == null)
				return;
			
			handler.AddChatLine(c, from, text);
		}

		static void ConnectionStateChangedStub(OrderManager om)
		{
			var handler = GetHandler();
			if (handler == null)
				return;
			
			handler.ConnectionStateChanged(om);
		}

		// Listen for connection failures
		void ConnectionStateChanged(OrderManager om)
		{
			if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				// Show connection failed dialog
				Widget.CloseWindow();
				
				Action onConnect = () => 
				{
					Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
					{
						{ "onExit", onExit },
						{ "onStart", OnGameStart }
					});
				};
				
				Action onRetry = () =>
				{
					Widget.CloseWindow();
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

		[ObjectCreator.UseCtor]
		internal CncLobbyLogic([ObjectCreator.Param( "widget" )] Widget lobby,
		                       [ObjectCreator.Param] World world, // Shellmap world
		                       [ObjectCreator.Param] OrderManager orderManager,
		                       [ObjectCreator.Param] Action onExit,
		                       [ObjectCreator.Param] Action onStart)
		{
			this.orderManager = orderManager;
			this.OnGameStart = () => { Widget.CloseWindow(); onStart(); };
			this.onExit = onExit;
			
			if (!staticSetup)
			{
				staticSetup = true;
				Game.LobbyInfoChanged += LobbyInfoChangedStub;
				Game.BeforeGameStart += BeforeGameStartStub;
				Game.AddChatLine += AddChatLineStub;
				Game.ConnectionStateChanged += ConnectionStateChangedStub;
			}
			
			UpdateCurrentMap();
			PlayerPalettePreview = world.WorldActor.Trait<CncColorPickerPaletteModifier>();
			PlayerPalettePreview.Ramp = Game.Settings.Player.ColorRamp;
			Players = lobby.GetWidget<ScrollPanelWidget>("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			EmptySlotTemplate = Players.GetWidget("TEMPLATE_EMPTY");
			EmptySlotTemplateHost = Players.GetWidget("TEMPLATE_EMPTY_HOST");

			var mapPreview = lobby.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.Map = () => Map;
			mapPreview.OnMouseDown = mi =>
			{
				var map = mapPreview.Map();
				if (map == null || mi.Button != MouseButton.Left
				    || orderManager.LocalClient.State == Session.ClientState.Ready)
					return false;

				var p = map.SpawnPoints
					.Select((sp, i) => Pair.New(mapPreview.ConvertToPreview(map, sp), i))
					.Where(a => (a.First - mi.Location).LengthSquared < 64)
					.Select(a => a.Second + 1)
					.FirstOrDefault();

				var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == p);
				if (p == 0 || !owned)
					orderManager.IssueOrder(Order.Command("spawn {0}".F(p)));
				
				return true;
			};

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

			CountryNames = Rules.Info["world"].Traits.WithInterface<OpenRA.Traits.CountryInfo>().ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Random");

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
			disconnectButton.OnClick = () => { Widget.CloseWindow(); onExit(); };
			
			var gameStarting = false;
			var lockTeamsCheckbox = lobby.GetWidget<CheckboxWidget>("LOCKTEAMS_CHECKBOX");
			lockTeamsCheckbox.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.IsDisabled = () => !Game.IsHost || gameStarting;
			lockTeamsCheckbox.OnClick = () => orderManager.IssueOrder(Order.Command(
						"lockteams {0}".F(!orderManager.LobbyInfo.GlobalSettings.LockTeams)));
		
			var allowCheats = lobby.GetWidget<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
			allowCheats.IsDisabled = () => !Game.IsHost || gameStarting;
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

		Session.Client GetClientInSlot(Session.Slot slot)
		{
			return orderManager.LobbyInfo.ClientInSlot( slot );
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
		
		bool ShowSlotDropDown(CncDropDownButtonWidget dropdown, Session.Slot slot, bool showBotOptions)
		{
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", dropdown.Bounds.Width }};
			var panel = (ScrollPanelWidget)Widget.LoadWidget("LABEL_DROPDOWN_TEMPLATE", null, new WidgetArgs()
			{
				{ "substitutions", substitutions }
			});
			
			var itemTemplate = panel.GetWidget<ScrollItemWidget>("TEMPLATE");
			var options = new List<SlotDropDownOption>()
			{
				new SlotDropDownOption("Open", "slot_open "+slot.Index, () => (!slot.Closed && slot.Bot == null)),
				new SlotDropDownOption("Closed", "slot_close "+slot.Index, () => slot.Closed)
			};
			
			if (showBotOptions)
				foreach (var bot in Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name))
					options.Add(new SlotDropDownOption("Bot: {0}".F(bot), "slot_bot {0} {1}".F(slot.Index, bot), () => slot.Bot == bot));
			
			foreach (var option in options)
			{
				var o = option;
				var item = ScrollItemWidget.Setup(itemTemplate, o.Selected, 
				                                  () => {
														orderManager.IssueOrder(Order.Command(o.Order));
														dropdown.RemovePanel();
												  });
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o.Title;
				panel.AddChild(item);
			}
			
			panel.Bounds.Height = Math.Min(150, panel.ContentHeight);
			dropdown.AttachPanel(panel);
			return true;
		}
		
		bool ShowRaceDropDown(CncDropDownButtonWidget dropdown, Session.Slot slot)
		{
			if (Map.Players[slot.MapPlayer].LockRace)
				return false;
			
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", dropdown.Bounds.Width }};
			var panel = (ScrollPanelWidget)Widget.LoadWidget("RACE_DROPDOWN_TEMPLATE", null, new WidgetArgs()
			{
				{ "substitutions", substitutions }
			});
			
			var itemTemplate = panel.GetWidget<ScrollItemWidget>("TEMPLATE");
			
			foreach (var c in CountryNames)
			{
				var race = c;
				var sr = GetClientInSlot(slot).Country;
				var item = ScrollItemWidget.Setup(itemTemplate, () => sr == race.Key, 
				                                  () => {
														orderManager.IssueOrder(Order.Command("race "+race.Key));
														dropdown.RemovePanel();
												  });
				item.GetWidget<LabelWidget>("LABEL").GetText = () => race.Value;
				var flag = item.GetWidget<ImageWidget>("FLAG");
				flag.GetImageCollection = () => "flags";
				flag.GetImageName = () => race.Key;
				panel.AddChild(item);
			}
			
			panel.Bounds.Height = Math.Min(150, panel.ContentHeight);
			dropdown.AttachPanel(panel);
			return true;
		}
				
		bool ShowTeamDropDown(CncDropDownButtonWidget dropdown, Session.Slot slot)
		{
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", dropdown.Bounds.Width }};
			var panel = (ScrollPanelWidget)Widget.LoadWidget("TEAM_DROPDOWN_TEMPLATE", null, new WidgetArgs()
			{
				{ "substitutions", substitutions }
			});
			
			var itemTemplate = panel.GetWidget<ScrollItemWidget>("TEMPLATE");
			
			for (int i = 0; i <= Map.PlayerCount; i++)
			{
				var ii = i;
				var c = GetClientInSlot(slot);
				var item = ScrollItemWidget.Setup(itemTemplate, () => c.Team == ii, 
				                                  () => {
														orderManager.IssueOrder(Order.Command("team "+ii));
														dropdown.RemovePanel();
												  });
				item.GetWidget<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				panel.AddChild(item);
			};
			
			panel.Bounds.Height = Math.Min(150, panel.ContentHeight);
			dropdown.AttachPanel(panel);
			return true;
		}
		
		bool ShowColorDropDown(Session.Slot s, CncDropDownButtonWidget color)
		{
			if (Map.Players[s.MapPlayer].LockColor)
				return true;
			
			Action<ColorRamp> onSelect = c =>
			{
				Game.Settings.Player.ColorRamp = c;
				Game.Settings.Save();
				color.RemovePanel();
				orderManager.IssueOrder(Order.Command("color {0}".F(c)));
			};
			
			Action<ColorRamp> onChange = c =>
			{
				PlayerPalettePreview.Ramp = c;
			};
			
			var colorChooser = Game.LoadWidget(orderManager.world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", orderManager.LocalClient.ColorRamp }
			});
			
			color.AttachPanel(colorChooser);
			return true;
		}
		
		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.RemoveChildren();
			
			foreach (var slot in orderManager.LobbyInfo.Slots)
			{
				var s = slot;
				var c = GetClientInSlot(s);
				Widget template;

				if (c == null)
				{
					if (Game.IsHost)
					{
						if (slot.Spectator)
						{
							template = EmptySlotTemplateHost.Clone();
							var name = template.GetWidget<CncDropDownButtonWidget>("NAME");
							name.GetText = () => s.Closed ? "Closed" : "Open";
							name.OnMouseDown = _ => ShowSlotDropDown(name, s, false);
							var btn = template.GetWidget<ButtonWidget>("JOIN");
							btn.GetText = () =>  "Spectate in this slot";							
						}
						else
						{
							template = EmptySlotTemplateHost.Clone();
							var name = template.GetWidget<CncDropDownButtonWidget>("NAME");
							name.GetText = () => s.Closed ? "Closed" : (s.Bot == null) ? "Open" : s.Bot;
							name.OnMouseDown = _ => ShowSlotDropDown(name, s, Map.Players[ s.MapPlayer ].AllowBots);
						}
					}
					else
					{
						template = EmptySlotTemplate.Clone();
						var name = template.GetWidget<LabelWidget>("NAME");
						name.GetText = () => s.Closed ? "Closed" : (s.Bot == null) ? "Open" : s.Bot;

						if (slot.Spectator)
						{
							var btn = template.GetWidget<ButtonWidget>("JOIN");
							btn.GetText = () => "Spectate in this slot";
						}
					}

					var join = template.GetWidget<ButtonWidget>("JOIN");
					if (join != null)
					{
						join.OnMouseUp = _ => { orderManager.IssueOrder(Order.Command("slot " + s.Index)); return true; };
						join.IsVisible = () => !s.Closed && s.Bot == null && orderManager.LocalClient.State != Session.ClientState.Ready;
					}
					
					var bot = template.GetWidget<LabelWidget>("BOT");
					if (bot != null)
						bot.IsVisible = () => s.Bot != null;
				}
				else if (c.Index == orderManager.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalPlayerTemplate.Clone();
					var name = template.GetWidget<TextFieldWidget>("NAME");
					name.Text = c.Name;
					name.OnEnterKey = () =>
					{
						name.Text = name.Text.Trim();
						if (name.Text.Length == 0)
							name.Text = c.Name;

						name.LoseFocus();
						if (name.Text == c.Name)
							return true;

						orderManager.IssueOrder(Order.Command("name " + name.Text));
						Game.Settings.Player.Name = name.Text;
						Game.Settings.Save();
						return true;
					};
					name.OnLoseFocus = () => name.OnEnterKey();

					var color = template.GetWidget<CncDropDownButtonWidget>("COLOR");
					color.OnMouseDown = _ => ShowColorDropDown(s, color);
					
					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.ColorRamp.GetColor(0);

					var faction = template.GetWidget<CncDropDownButtonWidget>("FACTION");
					faction.OnMouseDown = _ => ShowRaceDropDown(faction, s);
					
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<CncDropDownButtonWidget>("TEAM");
					team.OnMouseDown = _ => ShowTeamDropDown(team, s);
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					status.OnClick += CycleReady;
					
					var spectator = template.GetWidget<LabelWidget>("SPECTATOR");
					
					Session.Slot slot1 = slot;
					color.IsVisible = () => !slot1.Spectator;
					colorBlock.IsVisible = () => !slot1.Spectator;
					faction.IsVisible = () => !slot1.Spectator;
					factionname.IsVisible = () => !slot1.Spectator;
					factionflag.IsVisible = () => !slot1.Spectator;
					team.IsVisible = () => !slot1.Spectator;
					spectator.IsVisible = () => slot1.Spectator || slot1.Bot != null;
				}
				else
				{
					template = RemotePlayerTemplate.Clone();
					template.GetWidget<LabelWidget>("NAME").GetText = () => c.Name;
					var color = template.GetWidget<ColorBlockWidget>("COLOR");
					color.GetColor = () => c.ColorRamp.GetColor(0);

					var faction = template.GetWidget<LabelWidget>("FACTION");
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<LabelWidget>("TEAM");
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					if (c.Index == orderManager.LocalClient.Index)
						status.OnClick += CycleReady;

					var spectator = template.GetWidget<LabelWidget>("SPECTATOR");
							
					Session.Slot slot1 = slot;
					color.IsVisible = () => !slot1.Spectator;
					faction.IsVisible = () => !slot1.Spectator;
					factionname.IsVisible = () => !slot1.Spectator;
					factionflag.IsVisible = () => !slot1.Spectator;
					team.IsVisible = () => !slot1.Spectator;
					spectator.IsVisible = () => slot1.Spectator || slot1.Bot != null;

					var kickButton = template.GetWidget<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
					kickButton.OnMouseUp = mi =>
						{
							orderManager.IssueOrder(Order.Command("kick " + c.Slot));
							return true;
						};
				}

				template.Id = "SLOT_{0}".F(s.Index);
				template.IsVisible = () => true;
				Players.AddChild(template);
			}				
		}

		bool SpawnPointAvailable(int index) { return (index == 0) || orderManager.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }

		void CycleReady()
		{
			orderManager.IssueOrder(Order.Command("ready"));
		}
	}
	
	public class CncColorPickerLogic : IWidgetDelegate
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
		}
	}
}
