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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Server;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Widgets.Delegates;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncServerBrowserLogic : IWidgetDelegate
	{
		// Prevent repeated additions of RefreshServerList to the master server
		static bool masterServerSetup;
		
		GameServer currentServer = null;
		Widget serverTemplate;
		
		enum SearchStatus
		{
			Fetching,
			Failed,
			NoGames,
			Hidden
		}
		SearchStatus searchStatus = SearchStatus.Fetching;
		
		public string ProgressLabelText()
		{
			switch (searchStatus)
			{
				case SearchStatus.Fetching:
					return "Fetching game list...";
				case SearchStatus.Failed:
					return "Failed to contact master server.";
				case SearchStatus.NoGames:
					return "No games found.";
				default:
					return "";
			}
		}
		
		[ObjectCreator.UseCtor]
		public CncServerBrowserLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] Action onExit)
		{
			var panel = widget.GetWidget("SERVERBROWSER_PANEL");
			var sl = panel.GetWidget<ScrollPanelWidget>("SERVER_LIST");
			
			// Menu buttons
			panel.GetWidget<CncMenuButtonWidget>("REFRESH_BUTTON").OnClick = () =>
			{
				searchStatus = SearchStatus.Fetching;
				sl.RemoveChildren();
				currentServer = null;

				MasterServerQuery.Refresh(Game.Settings.Server.MasterServer);
			};
			
			var join = panel.GetWidget<CncMenuButtonWidget>("JOIN_BUTTON");
			join.IsDisabled = () => currentServer == null || !ServerBrowserDelegate.CanJoin(currentServer);
			join.OnClick = () =>
			{
				if (currentServer == null)
					return;

				Widget.CloseWindow();
				Game.JoinServer(currentServer.Address.Split(':')[0], int.Parse(currentServer.Address.Split(':')[1]));
			};
			
			panel.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = onExit;
			
			// Server list
			serverTemplate = sl.GetWidget("SERVER_TEMPLATE");
			
			// Display the progress label over the server list
			// The text is only visible when the list is empty
			var progressText = panel.GetWidget<LabelWidget>("PROGRESS_LABEL");
			progressText.IsVisible = () => searchStatus != SearchStatus.Hidden;
			progressText.GetText = ProgressLabelText;
			
			
			// Server info
			var infoPanel = panel.GetWidget("SERVER_INFO");
			infoPanel.IsVisible = () => currentServer != null;
			var preview = infoPanel.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			preview.Map = () => CurrentMap();
			preview.IsVisible = () => CurrentMap() != null;

			infoPanel.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			infoPanel.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => ServerBrowserDelegate.GenerateModsLabel(currentServer);
			infoPanel.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			infoPanel.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () =>
			{
				if (currentServer == null)
					return "";
				string ret = currentServer.Players.ToString();
				if (CurrentMap() != null)
					ret += "/" + CurrentMap().PlayerCount.ToString();
				return ret;
			};
			
			// Master server should be set up *once*
			if (!masterServerSetup)
			{
				masterServerSetup = true;
				MasterServerQuery.OnComplete += games => RefreshServerListStub(games);
			}
			MasterServerQuery.Refresh(Game.Settings.Server.MasterServer);
		}

		Map CurrentMap()
		{
			return (currentServer == null) ? null : GetMap(currentServer.Map);
		}
		
		static Map GetMap(string uid)
		{
			return (!Game.modData.AvailableMaps.ContainsKey(uid))
				? null : Game.modData.AvailableMaps[uid];
		}
		
		public void RefreshServerList(IEnumerable<GameServer> games)
		{
			var sl = Widget.RootWidget.GetWidget("SERVERBROWSER_PANEL")
				.GetWidget<ScrollPanelWidget>("SERVER_LIST");
			
			sl.RemoveChildren();
			currentServer = null;

			if (games == null)
			{
				searchStatus = SearchStatus.Failed;
				return;
			}

            var gamesWaiting = games.Where(g => ServerBrowserDelegate.CanJoin(g));

            if (gamesWaiting.Count() == 0)
			{
				searchStatus = SearchStatus.NoGames;
				return;
			}
			
			searchStatus = SearchStatus.Hidden;

			int i = 0;
            foreach (var loop in gamesWaiting)
			{
				var game = loop;
				
				var template = serverTemplate.Clone() as ContainerWidget;
				template.GetBackground = () => (currentServer == game) ? "dialog2" : null;
				template.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; currentServer = game; return true; };
				template.IsVisible = () => true;
				template.GetWidget<LabelWidget>("TITLE").GetText = () => game.Name;
				template.GetWidget<LabelWidget>("MAP").GetText = () => {var map = GetMap(game.Map); return map == null ? "Unknown" : map.Title;};
				template.GetWidget<LabelWidget>("PLAYERS").GetText = () => "{0} / {1}".F(game.Players, -1);
				template.GetWidget<LabelWidget>("IP").GetText = () => game.Address;
				sl.AddChild(template);

				if (i == 0) currentServer = game;
				i++;
			}
		}
		
		static void RefreshServerListStub(IEnumerable<GameServer> games)
		{
			var panel = Widget.RootWidget.GetWidget("SERVERBROWSER_PANEL");
			
			// The panel may not be open anymore
            if (panel == null)
                return;
			
			var browserLogic = panel.DelegateObject as CncServerBrowserLogic;
			if (browserLogic == null)
				return;
			
			browserLogic.RefreshServerList(games);
		}
	}
	
	public class CncDirectConnectLogic : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public CncDirectConnectLogic([ObjectCreator.Param] Widget widget,
		                             [ObjectCreator.Param] Action onExit,
		                             [ObjectCreator.Param] Action openLobby)
		{
			var panel = widget.GetWidget("DIRECTCONNECT_PANEL");
			var ip = panel.GetWidget<TextFieldWidget>("IP");
			var port = panel.GetWidget<TextFieldWidget>("PORT");
			
			var last = Game.Settings.Player.LastServer.Split(':').ToArray();
			ip.Text = last.Length > 1 ? last[0] : "localhost";
			port.Text = last.Length > 2 ? last[1] : "1234";

            panel.GetWidget<CncMenuButtonWidget>("JOIN_BUTTON").OnClick = () =>
            {
                int p;
				if (!int.TryParse(port.Text, out p))
					p = 1234;
				
				Game.Settings.Player.LastServer = "{0}:{1}".F(ip.Text, p);
                Game.Settings.Save();
				
                Game.JoinServer(ip.Text,p);
				openLobby();
            };

			panel.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = onExit;
		}
	}
}
