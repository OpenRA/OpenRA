#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		Widget Players, LocalPlayerTemplate, RemotePlayerTemplate;
		
		Dictionary<string,string> CountryNames;
		
		string MapUid;
		MapStub Map;
		public LobbyDelegate()
		{
			Game.LobbyInfoChanged += UpdateCurrentMap;
			UpdateCurrentMap();

			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			Players = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			
			
			var map = lobby.GetWidget<MapPreviewWidget>("LOBBY_MAP_PREVIEW");
			map.Map = () => {return Map;};
			map.OnSpawnClick = sp =>
			{			
				var owned = Game.LobbyInfo.Clients.Any(c => c.SpawnPoint == sp);
				if (sp == 0 || !owned)
					Game.IssueOrder(Order.Chat("/spawn {0}".F(sp)));
			};
			
			map.SpawnColors = () =>
			{
				var spawns = Map.SpawnPoints;
				var playerColors = Game.world.PlayerColors();
				var sc = new Dictionary<int2,Color>();
				
				for (int i = 1; i <= spawns.Count(); i++)
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == i);
					if (client == null)
						continue;
					sc.Add(spawns.ElementAt(i-1),playerColors[client.PaletteIndex % playerColors.Count()].Color);
				}
				return sc;
			};
			
			CountryNames = Rules.Info["world"].Traits.WithInterface<OpenRA.Traits.CountryInfo>().ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Random");
			
			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi => {
				r.GetWidget("MAP_CHOOSER").SpecialOneArg(MapUid);
				r.OpenWindow("MAP_CHOOSER");
				return true;
			};	
			
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;
			
			var disconnectButton = lobby.GetWidget("DISCONNECT_BUTTON");
			disconnectButton.OnMouseUp = mi => {
				Game.Disconnect();
				return true;
			};

			var lockTeamsCheckbox = lobby.GetWidget<CheckboxWidget>("LOCKTEAMS_CHECKBOX");
			lockTeamsCheckbox.IsVisible = () => true;
			lockTeamsCheckbox.Checked = () => Game.LobbyInfo.GlobalSettings.LockTeams;
			lockTeamsCheckbox.OnMouseDown = mi =>
			{
				if (Game.IsHost)
					Game.IssueOrder(Order.Chat(
						"/lockteams {0}".F(!Game.LobbyInfo.GlobalSettings.LockTeams)));
				return true;
			};

			Game.LobbyInfoChanged += UpdatePlayerList;
			
			Chrome.chatWidget = lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY");
			
			
			bool teamChat = false;
			var chatLabel = lobby.GetWidget<LabelWidget>("LABEL_CHATTYPE");
			var chatTextField = lobby.GetWidget<TextFieldWidget>("CHAT_TEXTFIELD");
			chatTextField.OnEnterKey = () =>
			{
				if (chatTextField.Text.Length == 0)
					return true;
				
				var order = (teamChat) ? Order.TeamChat( chatTextField.Text ) : Order.Chat( chatTextField.Text );
				Game.IssueOrder( order );
				chatTextField.Text = "";
				return true;
			};
			
			chatTextField.OnTabKey = () =>
			{
				teamChat ^= true;
				chatLabel.Text = (teamChat) ? "Team:" : "Chat:";
				return true;
			};
			
		}
		
		void UpdateCurrentMap()
		{
			if (MapUid == Game.LobbyInfo.GlobalSettings.Map) return;
			MapUid = Game.LobbyInfo.GlobalSettings.Map;
			Map = Game.AvailableMaps[ MapUid ];
		}
		
		void UpdatePlayerList()
		{
			// This causes problems for people who are in the process of editing their names (the widgets vanish from beneath them)
			// Todo: handle this nicer
			Players.Children.Clear();
			
			int offset = 0;
			foreach(var client in Game.LobbyInfo.Clients)
			{
				var c = client;
				Widget template;
				
				if(client.Index == Game.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalPlayerTemplate.Clone();
					var name = template.GetWidget<TextFieldWidget>("NAME");
					name.Text = c.Name;
					name.OnEnterKey = () =>
					{
						name.Text = name.Text.Trim();
						if (name.Text.Length == 0)
							name.Text = c.Name;
						
						Chrome.selectedWidget = null;
						if (name.Text == c.Name)
							return true;
						
						Game.IssueOrder(Order.Chat( "/name "+name.Text ));
						Game.Settings.PlayerName = name.Text;
						Game.Settings.Save();
						Chrome.selectedWidget = null;
						return true;
					};
					name.OnLoseFocus = () => name.OnEnterKey();

					var color = template.GetWidget<ButtonWidget>("COLOR");
					color.OnMouseUp = CyclePalette;

					var colorBlock = color.GetWidget<ColorBlockWidget>("COLORBLOCK");
					colorBlock.GetColor = () => Game.world.PlayerColors()[c.PaletteIndex % Game.world.PlayerColors().Count].Color;
					
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
					color.GetColor = () => Game.world.PlayerColors()[c.PaletteIndex % Game.world.PlayerColors().Count].Color;
					
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
		
		bool PaletteAvailable(int index) { return Game.LobbyInfo.Clients.All(c => c.PaletteIndex != index) && Game.world.PlayerColors()[index % Game.world.PlayerColors().Count].Playable; }
		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		
		bool CyclePalette(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.PlayerColors().Count() - 1;

			var newIndex = ((int)Game.LocalClient.PaletteIndex + d) % Game.world.PlayerColors().Count();
				
			while (!PaletteAvailable(newIndex) && newIndex != (int)Game.LocalClient.PaletteIndex)
				newIndex = (newIndex + d) % Game.world.PlayerColors().Count();
			
			Game.IssueOrder(
				Order.Chat("/pal " + newIndex));
			
			return true;
		}
		
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

			Game.IssueOrder(Order.Chat("/race " + nextCountry));
			
			return true;
		}

		bool CycleReady(MouseInput mi)
		{
			//HACK: Can't set this as part of the fuction as LocalClient/State not initalised yet
			Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget<ButtonWidget>("CHANGEMAP_BUTTON").Visible 
				= (Game.IsHost && Game.LocalClient.State == Session.ClientState.Ready);
			Game.IssueOrder(Order.Chat("/ready"));
			return true;
		}
		
		bool CycleTeam(MouseInput mi)
		{		
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.Map.PlayerCount;

			var newIndex = (Game.LocalClient.Team + d) % (Game.world.Map.PlayerCount+1);

			Game.IssueOrder(
				Order.Chat("/team " + newIndex));
			return true;
		}
	}
}
