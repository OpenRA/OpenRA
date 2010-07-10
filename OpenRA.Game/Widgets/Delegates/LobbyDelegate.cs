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
		public LobbyDelegate ()
		{
			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			Players = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			
			CountryNames = Rules.Info["world"].Traits.WithInterface<OpenRA.Traits.CountryInfo>().ToDictionary(a => a.Race, a => a.Name);
			CountryNames.Add("random", "Random");
			
			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi => {
				r.OpenWindow("MAP_CHOOSER");
				return true;
			};	
			
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;
			
			var disconnectButton = lobby.GetWidget("DISCONNECT_BUTTON");
			disconnectButton.OnMouseUp = mi => {
				Game.Disconnect();
				return true;
			};

			var lockTeamsCheckbox = lobby.GetWidget("LOCKTEAMS_CHECKBOX") as CheckboxWidget;
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
			Chrome.chatWidget = lobby.GetWidget("CHAT_DISPLAY") as ChatDisplayWidget;
			
			
			bool teamChat = false;
			var chatLabel = lobby.GetWidget("LABEL_CHATTYPE") as LabelWidget;
			var chatTextField = lobby.GetWidget("CHAT_TEXTFIELD") as TextFieldWidget;
			chatTextField.OnEnterKey = text =>
			{
				var order = (teamChat) ? Order.TeamChat( text ) : Order.Chat( text );
				Game.IssueOrder( order );
			};
			
			chatTextField.OnTabKey = text =>
			{
				teamChat ^= true;
				chatLabel.Text = (teamChat) ? "Team:" : "Chat:";
			};
			
		}
		
		void UpdatePlayerList()
		{
			Players.Children.Clear();
			
			int offset = 0;
			foreach(var client in Game.LobbyInfo.Clients)
			{
				var c = client;
				Widget template;
				
				if(client.Index == Game.LocalClient.Index && c.State != Session.ClientState.Ready)
				{
					template = LocalPlayerTemplate.Clone();
					
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
				template.GetWidget<LabelWidget>("NAME").GetText = () => c.Name;
				
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

		bool CycleSpawnPoint(MouseInput mi)
		{		
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.Map.SpawnPoints.Count();

			var newIndex = (Game.LocalClient.SpawnPoint + d) % (Game.world.Map.SpawnPoints.Count()+1);

			while (!SpawnPointAvailable(newIndex) && newIndex != (int)Game.LocalClient.SpawnPoint)
				newIndex = (newIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			Game.IssueOrder(
				Order.Chat("/spawn " + newIndex));
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
