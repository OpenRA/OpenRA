#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Net;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMenuLogic
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
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.Desaturated);

			// Root level menu
			var mainMenu = widget.GetWidget("MAIN_MENU");
			mainMenu.IsVisible = () => Menu == MenuType.Main;
			
			mainMenu.GetWidget<ButtonWidget>("SOLO_BUTTON").OnClick = StartSkirmishGame;
			mainMenu.GetWidget<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () => Menu = MenuType.Multiplayer;
			
			mainMenu.GetWidget<ButtonWidget>("REPLAYS_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("REPLAYBROWSER_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Main },
					{ "onStart", RemoveShellmapUI }
				});
			};
			
			mainMenu.GetWidget<ButtonWidget>("SETTINGS_BUTTON").OnClick = () => Menu = MenuType.Settings;
			mainMenu.GetWidget<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;
			
			// Multiplayer menu
			var multiplayerMenu = widget.GetWidget("MULTIPLAYER_MENU");
			multiplayerMenu.IsVisible = () => Menu == MenuType.Multiplayer;
			
			multiplayerMenu.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => Menu = MenuType.Main;
			multiplayerMenu.GetWidget<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("SERVERBROWSER_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Multiplayer },
					{ "openLobby", () => OpenLobbyPanel(MenuType.Multiplayer) }
				});
			};
			
			multiplayerMenu.GetWidget<ButtonWidget>("CREATE_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("CREATESERVER_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Multiplayer },
					{ "openLobby", () => OpenLobbyPanel(MenuType.Multiplayer) }
				});
			};
			
			multiplayerMenu.GetWidget<ButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Multiplayer },
					{ "openLobby", () => OpenLobbyPanel(MenuType.Multiplayer) }
				});
			};		
			
			// Settings menu
			var settingsMenu = widget.GetWidget("SETTINGS_MENU");
			settingsMenu.IsVisible = () => Menu == MenuType.Settings;
			
			settingsMenu.GetWidget<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("MODS_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Settings },
					{ "onSwitch", RemoveShellmapUI }
				});
			};
			
			settingsMenu.GetWidget<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("MUSIC_PANEL", new WidgetArgs()
                {
					{ "onExit", () => Menu = MenuType.Settings },
				});
			};
			
			settingsMenu.GetWidget<ButtonWidget>("PREFERENCES_BUTTON").OnClick = () =>
			{
				Menu = MenuType.None;
				Widget.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
                {
					{ "world", world },
					{ "onExit", () => Menu = MenuType.Settings },
				});
			};
			settingsMenu.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => Menu = MenuType.Main;
			
			widget.GetWidget<ImageWidget>("RECBLOCK").IsVisible = () => world.FrameNumber / 25 % 2 == 0;
		}
		
		void RemoveShellmapUI()
		{
			var root = Widget.RootWidget.GetWidget("MENU_BACKGROUND");
			root.Parent.RemoveChild(root);
		}
		
		void OpenLobbyPanel(MenuType menu)
		{
			Menu = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
			{
				{ "onExit", () => { Game.Disconnect(); Menu = menu; } },
				{ "onStart", RemoveShellmapUI }
			});
		}
		
		void StartSkirmishGame()
		{
			var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
			
			Game.CreateLocalServer(map);
			CncConnectingLogic.Connect(IPAddress.Loopback.ToString(),
			                           Game.Settings.Server.LoopbackPort,
			                           () => OpenLobbyPanel(MenuType.Main),
			                           () => { Game.CloseServer(); Menu = MenuType.Main; });
		}
	}
}
