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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyLogic
	{
		Widget lobby, LocalPlayerTemplate, RemotePlayerTemplate, EmptySlotTemplate, EmptySlotTemplateHost,
			   LocalSpectatorTemplate, RemoteSpectatorTemplate, NewSpectatorTemplate;

		ScrollPanelWidget Players;
		Dictionary<string, string> CountryNames;
		string MapUid;
		Map Map;

        public static ColorRamp CurrentColorPreview;

		readonly OrderManager orderManager;
		readonly WorldRenderer worldRenderer;
		[ObjectCreator.UseCtor]
		internal LobbyLogic([ObjectCreator.Param( "widget" )] Widget lobby,
		                    [ObjectCreator.Param] OrderManager orderManager,
		                    [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.orderManager = orderManager;
			this.worldRenderer = worldRenderer;
			this.lobby = lobby;
			Game.BeforeGameStart += CloseWindow;
			Game.LobbyInfoChanged += UpdateCurrentMap;
			Game.LobbyInfoChanged += UpdatePlayerList;
			UpdateCurrentMap();

			CurrentColorPreview = Game.Settings.Player.ColorRamp;

			Players = lobby.GetWidget<ScrollPanelWidget>("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			EmptySlotTemplate = Players.GetWidget("TEMPLATE_EMPTY");
			EmptySlotTemplateHost = Players.GetWidget("TEMPLATE_EMPTY_HOST");
			LocalSpectatorTemplate = Players.GetWidget("TEMPLATE_LOCAL_SPECTATOR");
			RemoteSpectatorTemplate = Players.GetWidget("TEMPLATE_REMOTE_SPECTATOR");
			NewSpectatorTemplate = Players.GetWidget("TEMPLATE_NEW_SPECTATOR");

			var mapPreview = lobby.GetWidget<MapPreviewWidget>("LOBBY_MAP_PREVIEW");
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

			CountryNames = Rules.Info["world"].Traits.WithInterface<CountryInfo>()
				.ToDictionary(a => a.Race, a => a.Name);
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

				Widget.OpenWindow("MAP_CHOOSER", new WidgetArgs()
				{
					{ "initialMap", MapUid },
					{ "onExit", () => {} },
					{ "onSelect", onSelect }
				});
			};

			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			var disconnectButton = lobby.GetWidget<ButtonWidget>("DISCONNECT_BUTTON");
			disconnectButton.OnClick = () =>
			{
				CloseWindow();
				Game.Disconnect();
				Game.LoadShellMap();
				Widget.OpenWindow("MAINMENU_BG");
			};

			var allowCheats = lobby.GetWidget<CheckboxWidget>("ALLOWCHEATS_CHECKBOX");
			allowCheats.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowCheats;
			allowCheats.OnClick = () =>
			{
				if (Game.IsHost)
					orderManager.IssueOrder(Order.Command(
						"allowcheats {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowCheats)));
			};

			var startGameButton = lobby.GetWidget<ButtonWidget>("START_GAME_BUTTON");
			startGameButton.OnClick = () =>
			{
				mapButton.Visible = false;
				disconnectButton.Visible = false;
				orderManager.IssueOrder(Order.Command("startgame"));
			};

			// Todo: Only show if the map requirements are met for player slots
			startGameButton.IsVisible = () => Game.IsHost;

			bool teamChat = false;
			var chatLabel = lobby.GetWidget<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.GetWidget<TextFieldWidget>("CHAT_TEXTFIELD");
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

			Game.AddChatLine += AddChatLine;
		}

		public void CloseWindow()
		{
			Game.LobbyInfoChanged -= UpdateCurrentMap;
			Game.LobbyInfoChanged -= UpdatePlayerList;
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= CloseWindow;

			Widget.CloseWindow();
		}

		void AddChatLine(Color c, string from, string text)
		{
			lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}

		void UpdatePlayerColor(float hf, float sf, float lf, float r)
		{
			var ramp = new ColorRamp((byte) (hf*255), (byte) (sf*255), (byte) (lf*255), (byte)(r*255));
			Game.Settings.Player.ColorRamp = ramp;
			Game.Settings.Save();
			orderManager.IssueOrder(Order.Command("color {0} {1}".F(orderManager.LocalClient.Index, ramp)));
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

			var title = Widget.RootWidget.GetWidget<LabelWidget>("LOBBY_TITLE");
			title.Text = "OpenRA Multiplayer Lobby - " + orderManager.LobbyInfo.GlobalSettings.ServerName;
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

		void ShowColorDropDown(DropDownButtonWidget color, Session.Client client)
		{
			var colorChooser = Game.modData.WidgetLoader.LoadWidget( new WidgetArgs() { {"worldRenderer", worldRenderer} }, null, "COLOR_CHOOSER" );
			var hueSlider = colorChooser.GetWidget<SliderWidget>("HUE_SLIDER");
			hueSlider.Value = orderManager.LocalClient.ColorRamp.H / 255f;

			var satSlider = colorChooser.GetWidget<SliderWidget>("SAT_SLIDER");
            satSlider.Value = orderManager.LocalClient.ColorRamp.S / 255f;

			var lumSlider = colorChooser.GetWidget<SliderWidget>("LUM_SLIDER");
            lumSlider.Value = orderManager.LocalClient.ColorRamp.L / 255f;

			var rangeSlider = colorChooser.GetWidget<SliderWidget>("RANGE_SLIDER");
            rangeSlider.Value = orderManager.LocalClient.ColorRamp.R / 255f;

			Action updateColorPreview = () => UpdateColorPreview(hueSlider.Value, satSlider.Value, lumSlider.Value, rangeSlider.Value);

			hueSlider.OnChange += _ => updateColorPreview();
			satSlider.OnChange += _ => updateColorPreview();
			lumSlider.OnChange += _ => updateColorPreview();
			rangeSlider.OnChange += _ => updateColorPreview();
			updateColorPreview();

			colorChooser.GetWidget<ButtonWidget>("BUTTON_OK").OnClick = () =>
			{
				updateColorPreview();
				UpdatePlayerColor(hueSlider.Value, satSlider.Value, lumSlider.Value, rangeSlider.Value);
				color.RemovePanel();
			};

			color.AttachPanel(colorChooser);
		}

		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.RemoveChildren();

			foreach (var kv in orderManager.LobbyInfo.Slots)
			{
				var s = kv.Value;
				var c = orderManager.LobbyInfo.ClientInSlot(kv.Key);
				Widget template;

				if (c == null || c.Bot != null)
				{
					if (Game.IsHost)
					{
						template = EmptySlotTemplateHost.Clone();
						var name = template.GetWidget<DropDownButtonWidget>("NAME");
						name.GetText = () => s.Closed ? "Closed" : (c == null) ? "Open" : c.Bot;
						name.OnMouseDown = _ => LobbyUtils.ShowSlotDropDown(name, s, c, orderManager);
					}
					else
					{
						template = EmptySlotTemplate.Clone();
						var name = template.GetWidget<LabelWidget>("NAME");
						name.GetText = () => s.Closed ? "Closed" : (c == null) ? "Open" : c.Bot;
					}

					var join = template.GetWidget<ButtonWidget>("JOIN");
					if (join != null)
					{
						join.OnClick = () => orderManager.IssueOrder(Order.Command("slot " + s.PlayerReference));
						join.IsVisible = () => !s.Closed && c == null && orderManager.LocalClient.State != Session.ClientState.Ready;
					}

					template.GetWidget<LabelWidget>("BOT").IsVisible = () => c != null;
				}
				else if (c.Index == orderManager.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalPlayerTemplate.Clone();
					LobbyUtils.SetupNameWidget(orderManager, c, template.GetWidget<TextFieldWidget>("NAME"));

					var color = template.GetWidget<DropDownButtonWidget>("COLOR");
					color.IsDisabled = () => s.LockColor;
					color.OnMouseDown = _ => ShowColorDropDown(color, c);

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.ColorRamp.GetColor(0);

					var faction = template.GetWidget<DropDownButtonWidget>("FACTION");
					faction.IsDisabled = () => s.LockRace;
					faction.OnMouseDown = _ => ShowRaceDropDown(faction, c);

					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<DropDownButtonWidget>("TEAM");
					team.IsDisabled = () => s.LockTeam;
					team.OnMouseDown = _ => LobbyUtils.ShowTeamDropDown(team, c, orderManager, Map);
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					status.OnClick = CycleReady;
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
						status.OnClick = CycleReady;

					var kickButton = template.GetWidget<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + c.Index));
				}

				template.IsVisible = () => true;
				Players.AddChild(template);
			}

			// Add spectators
			foreach (var client in orderManager.LobbyInfo.Clients.Where(client => client.Slot == null))
			{
				var c = client;
				Widget template;
				// Editable spectator
				if (c.Index == orderManager.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalSpectatorTemplate.Clone();
					LobbyUtils.SetupNameWidget(orderManager, c, template.GetWidget<TextFieldWidget>("NAME"));

					var color = template.GetWidget<DropDownButtonWidget>("COLOR");
					color.OnMouseDown = _ => ShowColorDropDown(color, c);

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.ColorRamp.GetColor(0);

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					status.OnClick += CycleReady;
				}
				// Non-editable spectator
				else
				{
					template = RemoteSpectatorTemplate.Clone();
					template.GetWidget<LabelWidget>("NAME").GetText = () => c.Name;
					var color = template.GetWidget<ColorBlockWidget>("COLOR");
					color.GetColor = () => c.ColorRamp.GetColor(0);

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.IsChecked = () => c.State == Session.ClientState.Ready;
					if (c.Index == orderManager.LocalClient.Index)
						status.OnClick += CycleReady;

					var kickButton = template.GetWidget<ButtonWidget>("KICK");
					kickButton.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
					kickButton.OnClick = () => orderManager.IssueOrder(Order.Command("kick " + c.Index));
				}

				template.IsVisible = () => true;
				Players.AddChild(template);
			}

			// Spectate button
			if (orderManager.LocalClient.Slot != null && orderManager.LocalClient.State != Session.ClientState.Ready)
			{
				var spec = NewSpectatorTemplate.Clone();
				var btn = spec.GetWidget<ButtonWidget>("SPECTATE");
				btn.OnClick = () => orderManager.IssueOrder(Order.Command("spectate"));
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

	public static class LobbyUtils
	{
		public static void SetupNameWidget(OrderManager orderManager, Session.Client c, TextFieldWidget name)
		{
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

		public static void ShowSlotDropDown(DropDownButtonWidget dropdown, Session.Slot slot,
			Session.Client client, OrderManager orderManager)
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
					options.Add(new SlotDropDownOption("Bot: {0}".F(bot),
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

		public static void ShowTeamDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, Map map)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.Team == ii,
					() => orderManager.IssueOrder(Order.Command("team {0} {1}".F(client.Index, ii))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};

			var options = Graphics.Util.MakeArray(map.SpawnPoints.Count() + 1, i => i).ToList();
			dropdown.ShowDropDown("TEAM_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}
	}
}
