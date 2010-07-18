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
using OpenRA.Traits;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		Widget Players, LocalPlayerTemplate, RemotePlayerTemplate;

		Dictionary<string, string> CountryNames;

		string MapUid;
		MapStub Map;
		
		bool SplitPlayerPalette = false;
		Palette BasePlayerPalette = null;
		public LobbyDelegate()
		{
			Game.LobbyInfoChanged += UpdateCurrentMap;
			UpdateCurrentMap();

			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			Players = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");


			var mapPreview = lobby.GetWidget<MapPreviewWidget>("LOBBY_MAP_PREVIEW");
			mapPreview.Map = () => Map;
			mapPreview.OnSpawnClick = sp =>
			{
				if (Game.LocalClient.State == Session.ClientState.Ready) return;
				var owned = Game.LobbyInfo.Clients.Any(c => c.SpawnPoint == sp);
				if (sp == 0 || !owned)
					Game.IssueOrder(Order.Command("spawn {0}".F(sp)));
			};

			mapPreview.SpawnColors = () =>
			{
				var spawns = Map.SpawnPoints;
				var sc = new Dictionary<int2, Color>();

				for (int i = 1; i <= spawns.Count(); i++)
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == i);
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
				r.GetWidget("MAP_CHOOSER").SpecialOneArg(MapUid);
				r.OpenWindow("MAP_CHOOSER");
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
			lockTeamsCheckbox.IsVisible = () => true;
			lockTeamsCheckbox.Checked = () => Game.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.OnMouseDown = mi =>
			{
				if (Game.IsHost)
					Game.IssueOrder(Order.Command(
						"lockteams {0}".F(!Game.LobbyInfo.GlobalSettings.LockTeams)));
				return true;
			};
			Game.LobbyInfoChanged += JoinedServer;
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
				Game.IssueOrder(order);
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

			hueSlider.OnChange += _ => UpdateColorPreview(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			satSlider.OnChange += _ => UpdateColorPreview(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			lumSlider.OnChange += _ => UpdateColorPreview(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
			rangeSlider.OnChange += _ => UpdateColorPreview(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());

			colorChooser.GetWidget<ButtonWidget>("BUTTON_OK").OnMouseUp = mi =>
			{
				colorChooser.IsVisible = () => false;
				UpdatePlayerColor(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
				return true;
			};
			
			// Copy the base palette for the colorpicker
			var info = Rules.Info["world"].Traits.Get<PlayerColorPaletteInfo>();
			BasePlayerPalette = Game.world.WorldRenderer.GetPalette(info.BasePalette);
			SplitPlayerPalette = info.SplitRamp;
			Game.world.WorldRenderer.AddPalette("colorpicker",BasePlayerPalette);
		}
		
		void UpdatePlayerColor(float hf, float sf, float lf, float r)
		{
			var c1 = ColorFromHSL(hf, sf, lf);
			var c2 = ColorFromHSL(hf, sf, r*lf);
			
			Game.Settings.PlayerColor1 = c1;
			Game.Settings.PlayerColor2 = c2;
			Game.Settings.Save();
			Game.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R,c1.G,c1.B,c2.R,c2.G,c2.B)));
		}
		
		void UpdateColorPreview(float hf, float sf, float lf, float r)
		{
			var c1 = ColorFromHSL(hf, sf, lf);
			var c2 = ColorFromHSL(hf, sf, r*lf);
			Game.world.WorldRenderer.UpdatePalette("colorpicker", new Palette(BasePlayerPalette, new PlayerColorRemap(c1, c2, SplitPlayerPalette)));
		}
		
		Color ColorFromHSL(float h, float s, float l)
		{		
			// Convert from HSL to RGB
			var q = (l < 0.5f) ? l * (1 + s) : l + s - (l * s);
			var p = 2 * l - q;
			var hk = h / 360.0f;
			
			double[] trgb = { hk + 1 / 3.0f,
							  hk,
							  hk - 1/3.0f };
			double[] rgb = { 0, 0, 0 };
			
			for (int k = 0; k < 3; k++)
			{
				while (trgb[k] < 0) trgb[k] += 1.0f;
				while (trgb[k] > 1) trgb[k] -= 1.0f;
			}
			
			for (int k = 0; k < 3; k++)
			{
				if (trgb[k] < 1 / 6.0f) { rgb[k] = (p + ((q - p) * 6 * trgb[k])); }
				else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5) { rgb[k] = q; }
				else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3) { rgb[k] = (p + ((q - p) * 6 * (2.0f / 3 - trgb[k]))); }
				else { rgb[k] = p; }
			}
			
			return Color.FromArgb((int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
		}


		void UpdateCurrentMap()
		{
			if (MapUid == Game.LobbyInfo.GlobalSettings.Map) return;
			MapUid = Game.LobbyInfo.GlobalSettings.Map;
			Map = Game.AvailableMaps[MapUid];
		}

		
		bool hasJoined = false;
		void JoinedServer()
		{
			if (hasJoined)
				return;
			hasJoined = true;
			
			if (Game.LocalClient.Name != Game.Settings.PlayerName)
				Game.IssueOrder(Order.Command("name " + Game.Settings.PlayerName));
			
			if (Game.LocalClient.Color1 != Game.Settings.PlayerColor1 || Game.LocalClient.Color2 != Game.Settings.PlayerColor2)
			{
				var c1 = Game.Settings.PlayerColor1;
				var c2 = Game.Settings.PlayerColor2;
				Game.IssueOrder(Order.Command("color {0},{1},{2},{3},{4},{5}".F(c1.R,c1.G,c1.B,c2.R,c2.G,c2.B)));
			}
		}
		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.Children.Clear();

			int offset = 0;
			foreach (var client in Game.LobbyInfo.Clients)
			{
				var c = client;
				Widget template;

				if (client.Index == Game.LocalClient.Index && c.State != Session.ClientState.Ready)
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

						Game.IssueOrder(Order.Command("name " + name.Text));
						Game.Settings.PlayerName = name.Text;
						Game.Settings.Save();
						return true;
					};
					name.OnLoseFocus = () => name.OnEnterKey();

					var color = template.GetWidget<ButtonWidget>("COLOR");
					color.OnMouseUp = mi =>
					{
						var colorChooser = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("COLOR_CHOOSER");
						var hueSlider = colorChooser.GetWidget<SliderWidget>("HUE_SLIDER");
						hueSlider.Offset = Game.LocalClient.Color1.GetHue()/360f;
						
						var satSlider = colorChooser.GetWidget<SliderWidget>("SAT_SLIDER");
						satSlider.Offset = Game.LocalClient.Color1.GetSaturation();
			
						var lumSlider = colorChooser.GetWidget<SliderWidget>("LUM_SLIDER"); 
						lumSlider.Offset = Game.LocalClient.Color1.GetBrightness();
						
						var rangeSlider = colorChooser.GetWidget<SliderWidget>("RANGE_SLIDER");
						rangeSlider.Offset = Game.LocalClient.Color2.GetBrightness()/Game.LocalClient.Color1.GetBrightness();

						UpdateColorPreview(360*hueSlider.GetOffset(), satSlider.GetOffset(), lumSlider.GetOffset(), rangeSlider.GetOffset());
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
					if (client.Index == Game.LocalClient.Index) status.OnMouseDown = CycleReady;
				}

				template.Id = "PLAYER_{0}".F(c.Index);
				template.Parent = Players;

				template.Bounds = new Rectangle(0, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				Players.AddChild(template);

				offset += template.Bounds.Height;
			}
		}

		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		bool CycleRace(MouseInput mi)
		{
			var countries = CountryNames.Select(a => a.Key);

			if (mi.Button == MouseButton.Right)
				countries = countries.Reverse();

			var nextCountry = countries
				.SkipWhile(c => c != Game.LocalClient.Country)
				.Skip(1)
				.FirstOrDefault();

			if (nextCountry == null)
				nextCountry = countries.First();

			Game.IssueOrder(Order.Command("race " + nextCountry));

			return true;
		}

		bool CycleReady(MouseInput mi)
		{
			//HACK: Can't set this as part of the fuction as LocalClient/State not initalised yet
			Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget<ButtonWidget>("CHANGEMAP_BUTTON").Visible
				= (Game.IsHost && Game.LocalClient.State == Session.ClientState.Ready);
			Game.IssueOrder(Order.Command("ready"));
			return true;
		}

		bool CycleTeam(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.Map.PlayerCount;

			var newIndex = (Game.LocalClient.Team + d) % (Game.world.Map.PlayerCount + 1);

			Game.IssueOrder(
				Order.Command("team " + newIndex));
			return true;
		}
	}
}
