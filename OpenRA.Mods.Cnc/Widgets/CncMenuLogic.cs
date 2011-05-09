#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;
using System;
using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncMenuLogic : IWidgetDelegate
	{
		enum MenuType
		{
			Main,
			Multiplayer,
			Settings,
			None
		}
		MenuType Menu = MenuType.Main;
		
		[ObjectCreator.UseCtor]
		public CncMenuLogic([ObjectCreator.Param] Widget widget,
		                    [ObjectCreator.Param] World world)
		{
			world.WorldActor.Trait<DesaturatedPaletteEffect>().Active = true;
			
			// Root level menu
			var mainMenu = widget.GetWidget("MAIN_MENU");
			mainMenu.IsVisible = () => Menu == MenuType.Main;
			
			mainMenu.GetWidget<CncMenuButtonWidget>("SOLO_BUTTON").OnClick = StartSkirmishGame;
			mainMenu.GetWidget<CncMenuButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () => Menu = MenuType.Multiplayer;
			
			mainMenu.GetWidget<CncMenuButtonWidget>("REPLAYS_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("REPLAYBROWSER_PANEL", new Dictionary<string, object>()
                {
					{ "onExit", new Action(() => { Menu = MenuType.Main; Widget.CloseWindow(); }) },
					{ "onStart", new Action(RemoveShellmapUI) }
				});
			};
			
			mainMenu.GetWidget<CncMenuButtonWidget>("SETTINGS_BUTTON").OnClick = () => Menu = MenuType.Settings;
			mainMenu.GetWidget<CncMenuButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;
			
			// Multiplayer menu
			var multiplayerMenu = widget.GetWidget("MULTIPLAYER_MENU");
			multiplayerMenu.IsVisible = () => Menu == MenuType.Multiplayer;
			
			multiplayerMenu.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = () => Menu = MenuType.Main;
			multiplayerMenu.GetWidget<CncMenuButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("SERVERBROWSER_PANEL", new Dictionary<string, object>()
                {
					{"onExit", new Action(ReturnToMultiplayerMenu)},
					{ "openLobby", new Action(() => OpenLobbyPanel(MenuType.Main)) }
				});
			};
			
			multiplayerMenu.GetWidget<CncMenuButtonWidget>("CREATE_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("CREATESERVER_PANEL", new Dictionary<string, object>()
                {
					{ "onExit", new Action(ReturnToMultiplayerMenu) },
					{ "openLobby", new Action(() => OpenLobbyPanel(MenuType.Multiplayer)) }
				});
			};
			
			multiplayerMenu.GetWidget<CncMenuButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("DIRECTCONNECT_PANEL", new Dictionary<string, object>()
                {
					{ "onExit", new Action(ReturnToMultiplayerMenu) },
					{ "openLobby", new Action(() => OpenLobbyPanel(MenuType.Multiplayer)) }
				});
			};		
			
			// Settings menu
			var settingsMenu = widget.GetWidget("SETTINGS_MENU");
			settingsMenu.IsVisible = () => Menu == MenuType.Settings;
			
			settingsMenu.GetWidget<CncMenuButtonWidget>("MODS_BUTTON").IsDisabled = () => true;
			settingsMenu.GetWidget<CncMenuButtonWidget>("MUSIC_BUTTON").IsDisabled = () => true;
			settingsMenu.GetWidget<CncMenuButtonWidget>("PREFERENCES_BUTTON").IsDisabled = () => true;
			settingsMenu.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = () => Menu = MenuType.Main;
		}
		
		void ReturnToMultiplayerMenu()
		{
			Menu = MenuType.Multiplayer;
			Widget.CloseWindow();
		}
		
		void RemoveShellmapUI()
		{
			Widget.CloseWindow();
			var root = Widget.RootWidget.GetWidget("MENU_BACKGROUND");
			root.Parent.RemoveChild(root);
		}
		
		void OpenLobbyPanel(MenuType menu)
		{
			// Quit the lobby: disconnect and restore menu
			Action onLobbyClose = () =>
			{
				Game.DisconnectOnly();
				Menu = menu;
				Widget.CloseWindow();
			};
			
			Menu = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new Dictionary<string, object>()
			{
				{ "onExit", onLobbyClose },
				{ "onStart", new Action(RemoveShellmapUI) }
			});
		}
		
		void StartSkirmishGame()
		{
			var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
			
			var settings = Game.Settings;
			settings.Server.Name = "Skirmish Game";
			// TODO: we want to prevent binding a port altogether
			settings.Server.ListenPort = 1234;
			settings.Server.ExternalPort = 1234;
			Game.CreateAndJoinServer(settings, map);
			
			OpenLobbyPanel(MenuType.Main);
		}
	}
}
