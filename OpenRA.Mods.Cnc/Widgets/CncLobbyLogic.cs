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
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncLobbyLogic : IWidgetDelegate
	{
		Widget LocalPlayerTemplate, RemotePlayerTemplate, EmptySlotTemplate, EmptySlotTemplateHost;
		CncScrollPanelWidget chatPanel;
		Widget chatTemplate;
		
		ScrollPanelWidget Players;
		Dictionary<string, string> CountryNames;
		string MapUid;
		Map Map;

        public static ColorRamp CurrentColorPreview;
		
		// Must be set only once on game start
		// TODO: This is stupid
		static bool staticSetup;		
		enum StaticCrap { LobbyInfo, BeforeGameStart, AddChatLine }
		static void StaticCrapChanged(StaticCrap type, Color a, string b, string c)
		{
			var panel = Widget.RootWidget.GetWidget("SERVER_LOBBY");
			
			// The panel may not be open anymore
            if (panel == null)
                return;
			
			var lobbyLogic = panel.DelegateObject as CncLobbyLogic;
			if (lobbyLogic == null)
				return;
			
			switch (type)
			{
				case StaticCrap.LobbyInfo:
					lobbyLogic.UpdateCurrentMap();
					lobbyLogic.UpdatePlayerList();
				break;
				case StaticCrap.BeforeGameStart:
					lobbyLogic.onGameStart();
				break;
				case StaticCrap.AddChatLine:
					lobbyLogic.AddChatLine(a,b,c);
				break;
			}	
		}
		
		readonly Action onGameStart;
		readonly OrderManager orderManager;
		readonly WorldRenderer worldRenderer;
		[ObjectCreator.UseCtor]
		internal CncLobbyLogic([ObjectCreator.Param( "widget" )] Widget lobby,
		                       [ObjectCreator.Param] OrderManager orderManager,
		                       [ObjectCreator.Param] Action onExit,
		                       [ObjectCreator.Param] Action onStart,
		                       [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.orderManager = orderManager;
			this.worldRenderer = worldRenderer;
			this.onGameStart = onStart;
			
			if (!staticSetup)
			{
				staticSetup = true;
				Game.LobbyInfoChanged += () => StaticCrapChanged(StaticCrap.LobbyInfo, Color.Beige, null, null);
				Game.BeforeGameStart += () => StaticCrapChanged(StaticCrap.BeforeGameStart, Color.PapayaWhip, null, null);
				Game.AddChatLine += (a,b,c) => StaticCrapChanged(StaticCrap.AddChatLine, a, b, c);
			}
			
			UpdateCurrentMap();
			CurrentColorPreview = Game.Settings.Player.ColorRamp;

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

			var mapButton = lobby.GetWidget<CncMenuButtonWidget>("CHANGEMAP_BUTTON");
			mapButton.OnClick = () => Widget.OpenWindow( "MAP_CHOOSER", new Dictionary<string, object>{ { "orderManager", orderManager }, { "mapName", MapUid } } );
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			var disconnectButton = lobby.GetWidget<CncMenuButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = onExit;
			
			var gameStarting = false;
			var lockTeamsCheckbox = lobby.GetWidget<CncCheckboxWidget>("LOCKTEAMS_CHECKBOX");
			lockTeamsCheckbox.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.IsDisabled = () => !Game.IsHost || gameStarting;
			lockTeamsCheckbox.OnClick = () => orderManager.IssueOrder(Order.Command(
						"lockteams {0}".F(!orderManager.LobbyInfo.GlobalSettings.LockTeams)));
		
			var allowCheats = lobby.GetWidget<CncCheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
			allowCheats.IsDisabled = () => !Game.IsHost || gameStarting;
			allowCheats.OnClick = () =>	orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));
				
			var startGameButton = lobby.GetWidget<CncMenuButtonWidget>("START_GAME_BUTTON");
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
			
			chatPanel = lobby.GetWidget<CncScrollPanelWidget>("CHAT_DISPLAY");
			chatTemplate = chatPanel.GetWidget("CHAT_TEMPLATE");
		}
		
		public void AddChatLine(Color c, string from, string text)
		{
			var name = from+":";
			var font = Game.Renderer.RegularFont;
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
		
		void UpdatePlayerColor(float hf, float sf, float lf, float r)
		{
			var ramp = new ColorRamp((byte) (hf*255), (byte) (sf*255), (byte) (lf*255), (byte)(r*255));
			Game.Settings.Player.ColorRamp = ramp;
			Game.Settings.Save();
			orderManager.IssueOrder(Order.Command("color {0}".F(ramp)));
		}
		
		void UpdateColorPreview(float hf, float sf, float lf, float r)
		{
            CurrentColorPreview = new ColorRamp((byte)(hf * 255), (byte)(sf * 255), (byte)(lf * 255), (byte)(r * 255));
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

		bool ShowSlotDropDown(Session.Slot slot, ButtonWidget name, bool showBotOptions)
		{
			var dropDownOptions = new List<Pair<string, Action>>
			{
				new Pair<string, Action>( "Open",
					() => orderManager.IssueOrder( Order.Command( "slot_open " + slot.Index )  )),
				new Pair<string, Action>( "Closed",
					() => orderManager.IssueOrder( Order.Command( "slot_close " + slot.Index ) )),
			};

			if (showBotOptions)
			{
				var bots = Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name);
				bots.Do(bot =>
					dropDownOptions.Add(new Pair<string, Action>("Bot: {0}".F(bot),
						() => orderManager.IssueOrder(Order.Command("slot_bot {0} {1}".F(slot.Index, bot))))));
			}

			DropDownButtonWidget.ShowDropDown( name,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = "  {0}".F(ac.First),
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
		
		bool ShowRaceDropDown(Session.Slot s, ButtonWidget race)
		{
			if (Map.Players[s.MapPlayer].LockRace)
				return false;

			var dropDownOptions = new List<Pair<string, Action>>();
			foreach (var c in CountryNames)
			{
				var cc = c;
				dropDownOptions.Add(new Pair<string, Action>( cc.Key,
					() => orderManager.IssueOrder( Order.Command("race "+cc.Key) )) );
			};

			DropDownButtonWidget.ShowDropDown( race,
				dropDownOptions,
				(ac, w) =>
			    {
					var ret = new LabelWidget
					{
						Bounds = new Rectangle(0, 0, w, 24),
						Text = "          {0}".F(CountryNames[ac.First]),
						OnMouseUp = mi => { ac.Second(); return true; },
					};
				
					ret.AddChild(new ImageWidget
					{
						Bounds = new Rectangle(5, 5, 40, 15),
						GetImageName = () => ac.First,
						GetImageCollection = () => "flags",
					});
					return ret;
				});
			return true;
		}
		
		bool ShowTeamDropDown(ButtonWidget team)
		{
			var dropDownOptions = new List<Pair<string, Action>>();
			for (int i = 0; i <= Map.PlayerCount; i++)
			{
				var ii = i;
				dropDownOptions.Add(new Pair<string, Action>( ii == 0 ? "-" : ii.ToString(),
					() => orderManager.IssueOrder( Order.Command("team "+ii) )) );
			};

			DropDownButtonWidget.ShowDropDown( team,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = "  {0}".F(ac.First),
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
		
		bool ShowColorDropDown(Session.Slot s, ButtonWidget color)
		{
			if (Map.Players[s.MapPlayer].LockColor)
				return false;
			
			var colorChooser = Game.modData.WidgetLoader.LoadWidget( new Dictionary<string,object>() { {"worldRenderer", worldRenderer} }, null, "COLOR_CHOOSER" );
			var hueSlider = colorChooser.GetWidget<SliderWidget>("HUE_SLIDER");
			hueSlider.SetOffset(orderManager.LocalClient.ColorRamp.H / 255f);
			
			var satSlider = colorChooser.GetWidget<SliderWidget>("SAT_SLIDER");
            satSlider.SetOffset(orderManager.LocalClient.ColorRamp.S / 255f);

			var lumSlider = colorChooser.GetWidget<SliderWidget>("LUM_SLIDER");
            lumSlider.SetOffset(orderManager.LocalClient.ColorRamp.L / 255f);
			
			var rangeSlider = colorChooser.GetWidget<SliderWidget>("RANGE_SLIDER");
            rangeSlider.SetOffset(orderManager.LocalClient.ColorRamp.R / 255f);
			
			hueSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			satSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			lumSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			rangeSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());

			DropDownButtonWidget.ShowDropPanel(color, colorChooser, new List<Widget>() {colorChooser.GetWidget("BUTTON_OK")}, () => {
				UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
				UpdatePlayerColor(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
				return true;
			});
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
							var name = template.GetWidget<ButtonWidget>("NAME");
							name.GetText = () => s.Closed ? "Closed" : "Open";
							name.OnMouseDown = _ => ShowSlotDropDown(s, name, false);
							var btn = template.GetWidget<ButtonWidget>("JOIN");
							btn.GetText = () =>  "Spectate in this slot";							
						}
						else
						{
							template = EmptySlotTemplateHost.Clone();
							var name = template.GetWidget<ButtonWidget>("NAME");
							name.GetText = () => s.Closed ? "Closed" : (s.Bot == null) ? "Open" : s.Bot;
							name.OnMouseDown = _ => ShowSlotDropDown(s, name, Map.Players[ s.MapPlayer ].AllowBots);
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

					var color = template.GetWidget<ButtonWidget>("COLOR");
					color.OnMouseUp = _ => ShowColorDropDown(s, color);
					
					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.ColorRamp.GetColor(0);

					var faction = template.GetWidget<ButtonWidget>("FACTION");
					faction.OnMouseDown = _ => ShowRaceDropDown(s, faction);
					
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<ButtonWidget>("TEAM");
					team.OnMouseDown = _ => ShowTeamDropDown(team);
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					status.OnChange += CycleReady;
					
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
						status.OnChange += CycleReady;

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

		void CycleReady(bool ready)
		{
			orderManager.IssueOrder(Order.Command("ready"));
		}
	}
}
