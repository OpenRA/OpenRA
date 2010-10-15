#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Graphics;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		Widget Players, LocalPlayerTemplate, RemotePlayerTemplate, EmptySlotTemplate, EmptySlotTemplateHost;
		
		Dictionary<string, string> CountryNames;
		string MapUid;
		Map Map;
		
		public static Color CurrentColorPreview1;
		public static Color CurrentColorPreview2;

		readonly OrderManager orderManager;
		[ObjectCreator.UseCtor]
		internal LobbyDelegate( [ObjectCreator.Param( "widget" )] Widget lobby, [ObjectCreator.Param] OrderManager orderManager )
		{
			this.orderManager = orderManager;
			Game.LobbyInfoChanged += UpdateCurrentMap;
			UpdateCurrentMap();
			
			CurrentColorPreview1 = Game.Settings.Player.Color1;
			CurrentColorPreview2 = Game.Settings.Player.Color2;

			Players = lobby.GetWidget("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			EmptySlotTemplate = Players.GetWidget("TEMPLATE_EMPTY");
			EmptySlotTemplateHost = Players.GetWidget("TEMPLATE_EMPTY_HOST");

			var mapPreview = lobby.GetWidget<MapPreviewWidget>("LOBBY_MAP_PREVIEW");
			mapPreview.Map = () => Map;
			mapPreview.OnSpawnClick = sp =>
			{
				if (orderManager.LocalClient.State == Session.ClientState.Ready) return;
				var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == sp);
				if (sp == 0 || !owned)
					orderManager.IssueOrder(Order.Command("spawn {0}".F(sp)));
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
					sc.Add(spawns.ElementAt(i - 1), client.Color1);
				}
				return sc;
			};

			CountryNames = Rules.Info["world"].Traits.WithInterface<OpenRA.Traits.CountryInfo>().ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Random");

			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi =>
			{
				Widget.OpenWindow( "MAP_CHOOSER", new Dictionary<string, object> { { "orderManager", orderManager }, { "mapName", MapUid } } );
				return true;
			};

			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;

			var disconnectButton = lobby.GetWidget("DISCONNECT_BUTTON");
			disconnectButton.OnMouseUp = mi =>
			{
				Game.Disconnect();
				return true;
			};

			var lockTeamsCheckbox = lobby.GetWidget<CheckboxWidget>("LOCKTEAMS_CHECKBOX");
			lockTeamsCheckbox.IsVisible = () => lockTeamsCheckbox.Visible && true;
			lockTeamsCheckbox.Checked = () => orderManager.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.OnMouseDown = mi =>
			{
				if (Game.IsHost)
					orderManager.IssueOrder(Order.Command(
						"lockteams {0}".F(!orderManager.LobbyInfo.GlobalSettings.LockTeams)));
				return true;
			};
			
			var startGameButton = lobby.GetWidget("START_GAME_BUTTON");
			startGameButton.OnMouseUp = mi =>
			{
				mapButton.Visible = false;
				disconnectButton.Visible = false;
				lockTeamsCheckbox.Visible = false;
				orderManager.IssueOrder(Order.Command("startgame"));
				return true;
			};
			
			// Todo: Only show if the map requirements are met for player slots
			startGameButton.IsVisible = () => Game.IsHost;
			
			Game.LobbyInfoChanged += JoinedServer;
			Game.ConnectionStateChanged += ResetConnectionState;
			Game.LobbyInfoChanged += UpdatePlayerList;
			
			Game.AddChatLine += lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine;

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
			
			var colorChooser = lobby.GetWidget("COLOR_CHOOSER");
			var hueSlider = colorChooser.GetWidget<SliderWidget>("HUE_SLIDER");		
			var satSlider = colorChooser.GetWidget<SliderWidget>("SAT_SLIDER");
			var lumSlider = colorChooser.GetWidget<SliderWidget>("LUM_SLIDER");
			var rangeSlider = colorChooser.GetWidget<SliderWidget>("RANGE_SLIDER");

			hueSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			satSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			lumSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			rangeSlider.OnChange += _ => UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());

			colorChooser.GetWidget<ButtonWidget>("BUTTON_OK").OnMouseUp = mi =>
			{
				colorChooser.IsVisible = () => false;
				UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
				UpdatePlayerColor(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
				return true;
			};
		}
		
		void UpdatePlayerColor(float hf, float sf, float lf, float r)
		{
			var c1 = PlayerColorRemap.ColorFromHSL(hf, sf, lf);
			var c2 = PlayerColorRemap.ColorFromHSL(hf, sf, r * lf);
			
			Game.Settings.Player.Color1 = c1;
			Game.Settings.Player.Color2 = c2;
			Game.Settings.Save();
			orderManager.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R,c1.G,c1.B,c2.R,c2.G,c2.B)));
		}
		
		void UpdateColorPreview(float hf, float sf, float lf, float r)
		{
			CurrentColorPreview1 = PlayerColorRemap.ColorFromHSL(hf, sf, lf);
			CurrentColorPreview2 = PlayerColorRemap.ColorFromHSL(hf, sf, r * lf);
		}

		void UpdateCurrentMap()
		{
			if (MapUid == orderManager.LobbyInfo.GlobalSettings.Map) return;
			MapUid = orderManager.LobbyInfo.GlobalSettings.Map;
			Map = new Map(Game.modData.AvailableMaps[MapUid].Package);

			var title = Widget.RootWidget.GetWidget<LabelWidget>("LOBBY_TITLE");
			title.Text = "OpenRA Multiplayer Lobby - " + orderManager.LobbyInfo.GlobalSettings.ServerName;
		}

		bool hasJoined = false;
		void JoinedServer()
		{
			if (hasJoined)
				return;
			hasJoined = true;
			
			if (orderManager.LocalClient.Name != Game.Settings.Player.Name)
				orderManager.IssueOrder(Order.Command("name " + Game.Settings.Player.Name));
			
			var c1 = Game.Settings.Player.Color1;
			var c2 = Game.Settings.Player.Color2;
			
			if (orderManager.LocalClient.Color1 != c1 || orderManager.LocalClient.Color2 != c2)			
				orderManager.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R,c1.G,c1.B,c2.R,c2.G,c2.B)));
		}

		void ResetConnectionState( OrderManager orderManager )
		{
			if( orderManager.Connection.ConnectionState == ConnectionState.PreConnecting )
				hasJoined = false;
		}

		Session.Client GetClientInSlot(Session.Slot slot)
		{
			return orderManager.LobbyInfo.ClientInSlot( slot );
		}
		
		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.Children.Clear();

			int offset = 0;
			foreach (var slot in orderManager.LobbyInfo.Slots)
			{
				var s = slot;
				var c = GetClientInSlot(s);
				Widget template;

				if (c == null)
				{
					if (Game.IsHost)
					{
						template = EmptySlotTemplateHost.Clone();
						var name = template.GetWidget<ButtonWidget>("NAME");
						name.GetText = () => s.Closed ? "Closed" : (s.Bot == null)? "Open" : "Bot: " + s.Bot;
						name.OnMouseUp = _ =>
						{
							if (s.Closed)
							{
								s.Bot = null;
								orderManager.IssueOrder(Order.Command("slot_open " + s.Index));
							}
							else
							{
								if (s.Bot == null && Map.Players[s.MapPlayer].AllowBots)
									orderManager.IssueOrder(Order.Command("slot_bot {0} HackyAI".F(s.Index)));
								else
									orderManager.IssueOrder(Order.Command("slot_close " + s.Index));
							}
							return true;
						};
					}
					else
					{
						template = EmptySlotTemplate.Clone();
						var name = template.GetWidget<LabelWidget>("NAME");
						name.GetText = () => s.Closed ? "Closed" : "Open";
					}

					var join = template.GetWidget<ButtonWidget>("JOIN");
					if (join != null)
					{
						join.OnMouseUp = _ => { orderManager.IssueOrder(Order.Command("slot " + s.Index)); return true; };
						join.IsVisible = () => !s.Closed && s.Bot == null;
					}
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
					color.OnMouseUp = mi =>
					{
						var colorChooser = Widget.RootWidget.GetWidget("SERVER_LOBBY").GetWidget("COLOR_CHOOSER");
						var hueSlider = colorChooser.GetWidget<SliderWidget>("HUE_SLIDER");
						hueSlider.SetOffset(orderManager.LocalClient.Color1.GetHue()/360f);
						
						var satSlider = colorChooser.GetWidget<SliderWidget>("SAT_SLIDER");
						satSlider.SetOffset(orderManager.LocalClient.Color1.GetSaturation());

						var lumSlider = colorChooser.GetWidget<SliderWidget>("LUM_SLIDER"); 
						lumSlider.SetOffset(orderManager.LocalClient.Color1.GetBrightness());
						
						var rangeSlider = colorChooser.GetWidget<SliderWidget>("RANGE_SLIDER");
						rangeSlider.SetOffset(orderManager.LocalClient.Color1.GetBrightness() == 0 ? 0 : orderManager.LocalClient.Color2.GetBrightness()/orderManager.LocalClient.Color1.GetBrightness());

						UpdateColorPreview(hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
						colorChooser.IsVisible = () => true;
						return true;
					};

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => c.Color1;

					var faction = template.GetWidget<ButtonWidget>("FACTION");
					faction.OnMouseUp = CycleRace;
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<ButtonWidget>("TEAM");
					team.OnMouseUp = CycleTeam;
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.Checked = () => c.State == Session.ClientState.Ready;
					status.OnMouseDown = CycleReady;
				}
				else
				{
					template = RemotePlayerTemplate.Clone();
					template.GetWidget<LabelWidget>("NAME").GetText = () => c.Name;
					var color = template.GetWidget<ColorBlockWidget>("COLOR");
					color.GetColor = () => c.Color1;

					var faction = template.GetWidget<LabelWidget>("FACTION");
					var factionname = faction.GetWidget<LabelWidget>("FACTIONNAME");
					factionname.GetText = () => CountryNames[c.Country];
					var factionflag = faction.GetWidget<ImageWidget>("FACTIONFLAG");
					factionflag.GetImageName = () => c.Country;
					factionflag.GetImageCollection = () => "flags";

					var team = template.GetWidget<LabelWidget>("TEAM");
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.Checked = () => c.State == Session.ClientState.Ready;
					if (c.Index == orderManager.LocalClient.Index) status.OnMouseDown = CycleReady;
				}

				template.Id = "SLOT_{0}".F(s.Index);
				template.Parent = Players;

				template.Bounds = new Rectangle(0, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				Players.AddChild(template);

				offset += template.Bounds.Height;
			}
		}

		bool SpawnPointAvailable(int index) { return (index == 0) || orderManager.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		bool CycleRace(MouseInput mi)
		{
			var countries = CountryNames.Select(a => a.Key);

			if (mi.Button == MouseButton.Right)
				countries = countries.Reverse();

			var nextCountry = countries
				.SkipWhile(c => c != orderManager.LocalClient.Country)
				.Skip(1)
				.FirstOrDefault();

			if (nextCountry == null)
				nextCountry = countries.First();

			orderManager.IssueOrder(Order.Command("race " + nextCountry));

			return true;
		}

		bool CycleReady(MouseInput mi)
		{
			orderManager.IssueOrder(Order.Command("ready"));
			return true;
		}

		bool CycleTeam(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Map.PlayerCount;
			var newIndex = (orderManager.LocalClient.Team + d) % (Map.PlayerCount + 1);
			
			orderManager.IssueOrder(
				Order.Command("team " + newIndex));
			return true;
		}
	}
}
